#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: For some of the parameters (like SlowTap.duration) it is confusing to see any value at all while not yet having
////        entered a value and seeing a value that doesn't seem to make sense (0 in this case means "no value, use default").
////        Can we do this better? Maybe display "<default>" as text while the control is at default value?

namespace UnityEngine.Experimental.Input.Editor.Lists
{
    /// <summary>
    /// Inspector-like functionality for editing parameter lists as used in <see cref="InputControlLayout"/>.
    /// </summary>
    /// <remarks>
    /// This can be used for parameters on interactions, processors, and composites.
    ///
    /// Call <see cref="Initialize"/> to set up (can be done repeatedly on the same instance). Call
    /// <see cref="OnGUI"/> to render.
    /// </remarks>
    internal class ParameterListView
    {
        /// <summary>
        /// Invoked whenever a parameter is changed.
        /// </summary>
        public Action onChange { get; set; }

        /// <summary>
        /// Get the current parameter values according to the editor state.
        /// </summary>
        /// <returns>An array of parameter values.</returns>
        public InputControlLayout.ParameterValue[] GetParameters()
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
            var result = new InputControlLayout.ParameterValue[countOfParametersNotAtDefaultValue];
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
        public void Initialize(Type registeredType, ReadOnlyArray<InputControlLayout.ParameterValue> existingParameters)
        {
            if (registeredType == null)
            {
                // No registered type. This usually happens when data references a registration that has
                // been removed in the meantime (e.g. an interaction that is no longer supported). We want
                // to accept this case and simply pretend that the given type has no parameters.

                Clear();
                return;
            }

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
                if (fieldType == typeof(bool))
                {
                    parameter.value.type = InputControlLayout.ParameterType.Boolean;

                    // Determine default.
                    if (instance != null)
                        parameter.defaultValue = new InputControlLayout.ParameterValue(name, (bool)field.GetValue(instance));
                }
                else if (fieldType == typeof(int))
                {
                    parameter.value.type = InputControlLayout.ParameterType.Integer;

                    // Determine default.
                    if (instance != null)
                        parameter.defaultValue = new InputControlLayout.ParameterValue(name, (int)field.GetValue(instance));
                }
                else if (fieldType == typeof(float))
                {
                    parameter.value.type = InputControlLayout.ParameterType.Float;

                    // Determine default.
                    if (instance != null)
                        parameter.defaultValue = new InputControlLayout.ParameterValue(name, (float)field.GetValue(instance));
                }
                else if (fieldType.IsEnum)
                {
                    parameter.value.type = InputControlLayout.ParameterType.Integer;
                    parameter.enumNames = Enum.GetNames(fieldType).Select(x => new GUIContent(x)).ToArray();

                    ////REVIEW: this probably falls apart if multiple members have the same value
                    var list = new List<int>();
                    foreach (var value in Enum.GetValues(fieldType))
                        list.Add((int)value);
                    parameter.enumValues = list.ToArray();

                    // Determine default.
                    if (instance != null)
                    {
                        var defaultValue = field.GetValue(instance);
                        var defaultValueInt = Convert.ToInt32(defaultValue);
                        parameter.defaultValue = new InputControlLayout.ParameterValue(name, defaultValueInt);
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

                foreach (var parameter in m_Parameters)
                {
                    if (parameter.isEnum)
                    {
                        var enumValue = Enum.ToObject(parameter.field.FieldType, parameter.value.GetIntValue());
                        parameter.field.SetValue(instance, enumValue);
                    }
                    else
                    {
                        switch (parameter.value.type)
                        {
                            case InputControlLayout.ParameterType.Float:
                                parameter.field.SetValue(instance, parameter.value.GetFloatValue());
                                break;

                            case InputControlLayout.ParameterType.Boolean:
                                parameter.field.SetValue(instance, parameter.value.GetBoolValue());
                                break;

                            case InputControlLayout.ParameterType.Integer:
                                parameter.field.SetValue(instance, parameter.value.GetIntValue());
                                break;
                        }
                    }
                }

                m_ParameterEditor = (InputParameterEditor)Activator.CreateInstance(parameterEditorType);
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

                string result = null;
                if (parameter.isEnum)
                {
                    var intValue = parameter.value.GetIntValue();
                    result = EditorGUILayout.IntPopup(label, intValue, parameter.enumNames, parameter.enumValues).ToString();
                }
                else if (parameter.value.type == InputControlLayout.ParameterType.Integer)
                {
                    var intValue = parameter.value.GetIntValue();
                    result = EditorGUILayout.IntField(label, intValue).ToString();
                }
                else if (parameter.value.type == InputControlLayout.ParameterType.Float)
                {
                    var floatValue = parameter.value.GetFloatValue();
                    result = EditorGUILayout.FloatField(label, floatValue).ToString();
                }
                else if (parameter.value.type == InputControlLayout.ParameterType.Boolean)
                {
                    var boolValue = parameter.value.GetBoolValue();
                    result = EditorGUILayout.Toggle(label, boolValue).ToString();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    parameter.value.SetValue(result);
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

                var newValue = new InputControlLayout.ParameterValue();
                newValue.name = parameter.value.name;

                var value = parameter.field.GetValue(target);
                if (parameter.isEnum)
                {
                    var intValue = Convert.ToInt32(value);
                    newValue.SetValue(intValue);
                }
                else
                {
                    switch (parameter.value.type)
                    {
                        case InputControlLayout.ParameterType.Float:
                        {
                            var floatValue = Convert.ToSingle(value);
                            newValue.SetValue(floatValue);
                            break;
                        }

                        case InputControlLayout.ParameterType.Boolean:
                        {
                            var intValue = Convert.ToInt32(value);
                            newValue.SetValue(intValue);
                            break;
                        }

                        case InputControlLayout.ParameterType.Integer:
                        {
                            var boolValue = Convert.ToBoolean(value);
                            newValue.SetValue(boolValue);
                            break;
                        }
                    }
                }

                m_Parameters[i].value = newValue;
            }
        }

        private InputParameterEditor m_ParameterEditor;
        private EditableParameterValue[] m_Parameters;
        private GUIContent[] m_ParameterLabels;

        private struct EditableParameterValue
        {
            public InputControlLayout.ParameterValue value;
            public InputControlLayout.ParameterValue? defaultValue;
            public int[] enumValues;
            public GUIContent[] enumNames;
            public FieldInfo field;

            public bool isEnum => enumValues != null;
            public bool isAtDefault => defaultValue != null && value == defaultValue.Value;
        }
    }
}

#endif // UNITY_EDITOR
