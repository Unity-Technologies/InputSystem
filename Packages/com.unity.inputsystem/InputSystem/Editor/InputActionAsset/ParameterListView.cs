#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;
#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////REVIEW: can we collects tooltips from the fields we're looking at?

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
    public class ParameterListView
    {
        public Action onChange { get; set; }

        public InputControlLayout.ParameterValue[] GetParameters()
        {
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
        ///
        /// </summary>
        /// <param name="registeredType">Type of object that the parameters will be passed to at runtime.
        /// We need this to be able to determine the possible set of parameters and their possible values. This
        /// can be a class implementing <see cref="IInputInteraction"/>, for example.</param>
        /// <param name="existingParameters">List of existing parameters. Can be empty/null.</param>
        public void Initialize(Type registeredType, ReadOnlyArray<InputControlLayout.ParameterValue> existingParameters)
        {
            if (registeredType == null)
                throw new ArgumentNullException("registeredType");

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
                var parameter = new EditableParameterValue();
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
                    parameter.enumNames = Enum.GetNames(fieldType);

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
        }

        public void Clear()
        {
            m_Parameters = null;
        }

        public void OnGUI()
        {
            if (m_Parameters == null)
                return;

            for (var i = 0; i < m_Parameters.Length; i++)
            {
                var parameter = m_Parameters[i];
                var label = ObjectNames.NicifyVariableName(parameter.value.name);

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
                    if (onChange != null)
                        onChange();
                }
            }
        }

        private EditableParameterValue[] m_Parameters;

        public struct EditableParameterValue
        {
            public InputControlLayout.ParameterValue value;
            public InputControlLayout.ParameterValue? defaultValue;
            public int[] enumValues;
            public string[] enumNames;

            public bool isEnum
            {
                get { return enumValues != null; }
            }

            public bool isAtDefault
            {
                get { return defaultValue != null && value == defaultValue.Value; }
            }
        }
    }
}

#endif // UNITY_EDITOR
