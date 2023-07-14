#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

////TODO: show description of interaction or processor when selected

namespace UnityEngine.InputSystem.Editor.Lists
{
    /// <summary>
    /// Inspector-like functionality for editing parameter lists as used in <see cref="InputControlLayout"/>.
    /// </summary>
    /// <remarks>
    /// This can be used for parameters on interactions, processors, and composites.
    ///
    /// Call <see cref="Initialize"/> to set up (can be done repeatedly on the same instance). Call
    /// <see cref="OnGUI"/> to render.
    ///
    /// Custom parameter GUIs can be defined by deriving from <see cref="InputParameterEditor{TObject}"/>.
    /// This class will automatically incorporate custom GUIs and fall back to default GUIs where no custom
    /// ones are defined.
    /// </remarks>
    internal class ParameterListView
    {
        /// <summary>
        /// Invoked whenever a parameter is changed.
        /// </summary>
        public Action onChange { get; set; }

        public bool hasUIToShow => (m_Parameters != null && m_Parameters.Length > 0) || m_ParameterEditor != null;
        public bool visible { get; set; }
        public string name { get; set; }

        /// <summary>
        /// Get the current parameter values according to the editor state.
        /// </summary>
        /// <returns>An array of parameter values.</returns>
        public NamedValue[] GetParameters()
        {
            if (m_Parameters == null)
                return null;

            // See if we have parameters that aren't at their default value.
            var countOfParametersNotAtDefaultValue = 0;
            for (var i = 0; i < m_Parameters.Length; ++i)
            {
                if (!m_Parameters[i].isAtDefault)
                    ++countOfParametersNotAtDefaultValue;
            }

            // If not, we return null.
            if (countOfParametersNotAtDefaultValue == 0)
                return null;

            // Collect non-default parameter values.
            var result = new NamedValue[countOfParametersNotAtDefaultValue];
            var index = 0;
            for (var i = 0; i < m_Parameters.Length; ++i)
            {
                var parameter = m_Parameters[i];
                if (parameter.isAtDefault)
                    continue;

                result[index++] = parameter.value;
            }

            return result;
        }

        /// <summary>
        /// Initialize the parameter list view based on the given registered type that has parameters to edit. This can be
        /// things such as interactions, processors, or composites.
        /// </summary>
        /// <param name="registeredType">Type of object that the parameters will be passed to at runtime.
        /// We need this to be able to determine the possible set of parameters and their possible values. This
        /// can be a class implementing <see cref="IInputInteraction"/>, for example.</param>
        /// <param name="existingParameters">List of existing parameters. Can be empty.</param>
        public void Initialize(Type registeredType, ReadOnlyArray<NamedValue> existingParameters)
        {
            if (registeredType == null)
            {
                // No registered type. This usually happens when data references a registration that has
                // been removed in the meantime (e.g. an interaction that is no longer supported). We want
                // to accept this case and simply pretend that the given type has no parameters.

                Clear();
                return;
            }

            visible = true;

            // Try to instantiate object so that we can determine defaults.
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(registeredType);
            }
            catch (Exception)
            {
                // Swallow. If we can't create an instance, we simply assume no defaults.
            }

            var parameters = new List<EditableParameterValue>();

            ////REVIEW: support properties here?
            // Go through public instance fields and add every parameter found on the registered
            // type.
            var fields = registeredType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Skip all fields that have an [InputControl] attribute. This is relevant
                // only for composites, but we just always do it here.
                if (field.GetCustomAttribute<InputControlAttribute>(false) != null)
                    continue;

                // Determine parameter name from field.
                var parameter = new EditableParameterValue {field = field};
                var name = field.Name;
                parameter.value.name = name;

                // Determine parameter type from field.
                var fieldType = field.FieldType;
                if (fieldType.IsEnum)
                {
                    // For enums, we want the underlying integer type.
                    var underlyingType = fieldType.GetEnumUnderlyingType();
                    var underlyingTypeCode = Type.GetTypeCode(underlyingType);

                    parameter.value = parameter.value.ConvertTo(underlyingTypeCode);

                    // Read enum names and values.
                    parameter.enumNames = Enum.GetNames(fieldType).Select(x => new GUIContent(x)).ToArray();
                    ////REVIEW: this probably falls apart if multiple members have the same value
                    var list = new List<int>();
                    foreach (var value in Enum.GetValues(fieldType))
                        list.Add((int)value);
                    parameter.enumValues = list.ToArray();
                }
                else
                {
                    var typeCode = Type.GetTypeCode(fieldType);
                    parameter.value = parameter.value.ConvertTo(typeCode);
                }

                // Determine default value.
                if (instance != null)
                {
                    try
                    {
                        var value = field.GetValue(instance);
                        parameter.defaultValue = new NamedValue
                        {
                            name = name,
                            value = PrimitiveValue.FromObject(value)
                        };
                    }
                    catch
                    {
                        // If the getter throws, ignore. All we lose is the actual default value from
                        // the field.
                    }
                }

                // If the parameter already exists in the given list, maintain its value.
                var existingParameterIndex = existingParameters.IndexOf(x => x.name == field.Name);
                if (existingParameterIndex >= 0)
                {
                    // Make sure we're preserving the right type.
                    parameter.value = existingParameters[existingParameterIndex].ConvertTo(parameter.value.type);
                }
                else
                {
                    // Not assigned. Set to default.
                    if (parameter.defaultValue != null)
                        parameter.value = parameter.defaultValue.Value;
                }

                // Add.
                parameters.Add(parameter);
            }

            m_Parameters = parameters.ToArray();

            // See if we have a dedicated parameter editor.
            var parameterEditorType = InputParameterEditor.LookupEditorForType(registeredType);
            if (parameterEditorType != null)
            {
                // Create an editor instance and hand it the instance we created. Unlike our default
                // editing logic, on this path we will be operating on an object instance that contains
                // the parameter values. So on this path, we actually need to update the object to reflect
                // the current parameter values.

                NamedValue.ApplyAllToObject(instance, m_Parameters.Select(x => x.value));

                m_ParameterEditor = (InputParameterEditor)Activator.CreateInstance(parameterEditorType);

                // We have to jump through some hoops here to create instances of any CustomOrDefaultSetting fields on the
                // parameter editor. This is because those types changed from structs to classes when UIToolkit was
                // introduced, and we don't want to force users to have to create those instances manually on any of their
                // own editors.
                var genericArgumentType = TypeHelpers.GetGenericTypeArgumentFromHierarchy(parameterEditorType,
                    typeof(InputParameterEditor<>), 0);
                if (genericArgumentType != null)
                {
                    var fieldInfos = parameterEditorType
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var customOrDefaultGenericType = typeof(InputParameterEditor<>.CustomOrDefaultSetting);
                    var customOrDefaultType = customOrDefaultGenericType.MakeGenericType(genericArgumentType);
                    foreach (var customOrDefaultEditorField in fieldInfos.Where(f => f.FieldType == customOrDefaultType))
                    {
                        customOrDefaultEditorField.SetValue(m_ParameterEditor, Activator.CreateInstance(customOrDefaultEditorField.FieldType));
                    }
                }
                m_ParameterEditor.SetTarget(instance);
            }
            else
            {
                m_ParameterEditor = null;

                // Create parameter labels.
                m_ParameterLabels = new GUIContent[m_Parameters.Length];
                for (var i = 0; i < m_Parameters.Length; ++i)
                {
                    // Look up tooltip from field.
                    var tooltip = string.Empty;
                    var field = m_Parameters[i].field;
                    var tooltipAttribute = field.GetCustomAttribute<TooltipAttribute>();
                    if (tooltipAttribute != null)
                        tooltip = tooltipAttribute.tooltip;

                    // Create label.
                    var niceName = ObjectNames.NicifyVariableName(m_Parameters[i].value.name);
                    m_ParameterLabels[i] = new GUIContent(niceName, tooltip);
                }
            }
        }

        public void Clear()
        {
            m_Parameters = null;
            m_ParameterEditor = null;
        }

#if UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
        public void OnDrawVisualElements(VisualElement root)
        {
            if (m_ParameterEditor != null)
            {
                m_ParameterEditor.OnDrawVisualElements(root, OnValuesChanged);
                return;
            }

            if (m_Parameters == null)
                return;

            void OnValueChanged(ref EditableParameterValue parameter, object result, int i)
            {
                parameter.value.value = PrimitiveValue.FromObject(result).ConvertTo(parameter.value.type);
                m_Parameters[i] = parameter;
            }

            void OnEditEnd()
            {
                onChange?.Invoke();
            }

            for (var i = 0; i < m_Parameters.Length; i++)
            {
                var parameter = m_Parameters[i];
                var label = m_ParameterLabels[i];
                var closedIndex = i;

                if (parameter.isEnum)
                {
                    var intValue = parameter.value.value.ToInt32();
                    var field = new DropdownField(label.text, parameter.enumNames.Select(x => x.text).ToList(), intValue);
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterCallback<BlurEvent>(_ => OnEditEnd());
                    root.Add(field);
                }
                else if (parameter.value.type == TypeCode.Int64 || parameter.value.type == TypeCode.UInt64)
                {
                    var longValue = parameter.value.value.ToInt64();
                    var field = new LongField(label.text) { value = longValue };
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterCallback<BlurEvent>(_ => OnEditEnd());
                    root.Add(field);
                }
                else if (parameter.value.type.IsInt())
                {
                    var intValue = parameter.value.value.ToInt32();
                    var field = new IntegerField(label.text) { value = intValue };
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterCallback<BlurEvent>(_ => OnEditEnd());
                    root.Add(field);
                }
                else if (parameter.value.type == TypeCode.Single)
                {
                    var floatValue = parameter.value.value.ToSingle();
                    var field = new FloatField(label.text) { value = floatValue };
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterCallback<BlurEvent>(_ => OnEditEnd());
                    root.Add(field);
                }
                else if (parameter.value.type == TypeCode.Double)
                {
                    var floatValue = parameter.value.value.ToDouble();
                    var field = new DoubleField(label.text) { value = floatValue };
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterCallback<BlurEvent>(_ => OnEditEnd());
                    root.Add(field);
                }
                else if (parameter.value.type == TypeCode.Boolean)
                {
                    var boolValue = parameter.value.value.ToBoolean();
                    var field = new Toggle(label.text) { value = boolValue };
                    field.RegisterValueChangedCallback(evt => OnValueChanged(ref parameter, evt.newValue, closedIndex));
                    field.RegisterValueChangedCallback(_ => OnEditEnd());
                    root.Add(field);
                }
            }
        }

#endif

        private void OnValuesChanged()
        {
            ReadParameterValuesFrom(m_ParameterEditor.target);
            onChange?.Invoke();
        }

        public void OnGUI()
        {
            // If we have a dedicated parameter editor, let it do all the work.
            if (m_ParameterEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                m_ParameterEditor.OnGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    ReadParameterValuesFrom(m_ParameterEditor.target);
                    onChange?.Invoke();
                }
                return;
            }

            // Otherwise, fall back to our default logic.
            if (m_Parameters == null)
                return;
            for (var i = 0; i < m_Parameters.Length; i++)
            {
                var parameter = m_Parameters[i];
                var label = m_ParameterLabels[i];

                EditorGUI.BeginChangeCheck();

                object result = null;
                if (parameter.isEnum)
                {
                    var intValue = parameter.value.value.ToInt32();
                    result = EditorGUILayout.IntPopup(label, intValue, parameter.enumNames, parameter.enumValues);
                }
                else if (parameter.value.type == TypeCode.Int64 || parameter.value.type == TypeCode.UInt64)
                {
                    var longValue = parameter.value.value.ToInt64();
                    result = EditorGUILayout.LongField(label, longValue);
                }
                else if (parameter.value.type.IsInt())
                {
                    var intValue = parameter.value.value.ToInt32();
                    result = EditorGUILayout.IntField(label, intValue);
                }
                else if (parameter.value.type == TypeCode.Single)
                {
                    var floatValue = parameter.value.value.ToSingle();
                    result = EditorGUILayout.FloatField(label, floatValue);
                }
                else if (parameter.value.type == TypeCode.Double)
                {
                    var floatValue = parameter.value.value.ToDouble();
                    result = EditorGUILayout.DoubleField(label, floatValue);
                }
                else if (parameter.value.type == TypeCode.Boolean)
                {
                    var boolValue = parameter.value.value.ToBoolean();
                    result = EditorGUILayout.Toggle(label, boolValue);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    parameter.value.value = PrimitiveValue.FromObject(result).ConvertTo(parameter.value.type);
                    m_Parameters[i] = parameter;
                    onChange?.Invoke();
                }
            }
        }

        ////REVIEW: check whether parameters have *actually* changed?
        /// <summary>
        /// Refresh <see cref="m_Parameters"/> from the current parameter values in <paramref name="target"/>.
        /// </summary>
        /// <param name="target">An instance of the current type we are editing parameters on.</param>
        private void ReadParameterValuesFrom(object target)
        {
            if (m_Parameters == null)
                return;

            for (var i = 0; i < m_Parameters.Length; ++i)
            {
                var parameter = m_Parameters[i];

                object value = null;
                try
                {
                    value = parameter.field.GetValue(target);
                }
                catch
                {
                    // Ignore exceptions from getters.
                }

                m_Parameters[i].value.value = PrimitiveValue.FromObject(value).ConvertTo(parameter.value.type);
            }
        }

        private InputParameterEditor m_ParameterEditor;
        private EditableParameterValue[] m_Parameters;
        private GUIContent[] m_ParameterLabels;

        private struct EditableParameterValue
        {
            public NamedValue value;
            public NamedValue? defaultValue;
            public int[] enumValues;
            public GUIContent[] enumNames;
            public FieldInfo field;

            public bool isEnum => enumValues != null;
            public bool isAtDefault => defaultValue != null && value == defaultValue.Value;
        }
    }
}

#endif // UNITY_EDITOR
