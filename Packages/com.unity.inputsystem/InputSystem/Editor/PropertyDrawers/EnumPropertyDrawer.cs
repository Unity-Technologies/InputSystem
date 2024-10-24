#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Abstract base class for a generic property drawer for enums.
    /// </summary>
    internal abstract class EnumDrawer<T> : PropertyDrawer where T : Enum
    {
        protected string[] m_EnumDisplayNames;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ProcessDisplayNamesForAliasedEnums();
            return base.CreatePropertyGUI(property);
        }

        protected abstract string GetNonAliasedNames(string enumValue);

        private void ProcessDisplayNamesForAliasedEnums()
        {
            Dictionary<string, int> dictEnumNamesAndValues = new Dictionary<string, int>();
            string[] enumDisplayNames = Enum.GetNames(typeof(T));
            Array enumValues = Enum.GetValues(typeof(T));
            List<string> enumStringValues = enumValues.Cast<T>().Select(v => v.ToString()).ToList();

            for (int iEnumCounter = 0; iEnumCounter < enumDisplayNames.Length; iEnumCounter++)
            {
                string enumName = enumDisplayNames[iEnumCounter];
                string aliasedName = GetNonAliasedNames(enumStringValues[iEnumCounter]);

                if (!string.IsNullOrEmpty(aliasedName) && enumName != aliasedName)
                {
                    enumName = enumName + " (" + aliasedName + ")";
                }

                dictEnumNamesAndValues.Add(enumName, (int)enumValues.GetValue(iEnumCounter));
            }

            var sortedEntries = dictEnumNamesAndValues.OrderBy(x => x.Value).ThenBy(x => x.Key.Contains("("));

            m_EnumDisplayNames = sortedEntries.Select(x => x.Key).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, m_EnumDisplayNames);
            }

            EditorGUI.EndProperty();
        }
    }

    /// <summary>
    ///Property drawer for <see cref = "GamepadButton" />.s
    //// </ summary >
    [CustomPropertyDrawer(typeof(GamepadButton))]
    internal class GpadButtonDrawer : EnumDrawer<GamepadButton>
    {
        protected override string GetNonAliasedNames(string gpadValue)
        {
            switch(gpadValue) 
            {
                case "North":
                case "Y":
                case "Triangle":
                    return "North";

                case "South":
                case "A":
                case "Cross":
                    return "South";
                case "East":
                case "B":
                case "Circle":
                    return "East";
                case "West":
                case "X":
                case "Square":
                    return "West";
                    

                default: return string.Empty;
            }
        }      
    }
}
#endif // UNITY_EDITOR