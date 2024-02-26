#if UNITY_EDITOR
using Codice.Client.BaseCommands;
using Codice.CM.WorkspaceServer.Tree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    ///Property drawer for <see cref = "GamepadButton" />.s
    //// </ summary >
    [CustomPropertyDrawer(typeof(GamepadButton))]
    internal class GpadButtonDrawer : PropertyDrawer
    {
        string[] m_EnumDisplayNames;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ProcessDisplayNamesForAliasedEnums();
            return base.CreatePropertyGUI(property);
        }

        private string GetNonAliasedNames(string gpadValue)
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
        private void ProcessDisplayNamesForAliasedEnums()
        {
            // Dictionary to hold the enum display names and their corresponding values
            Dictionary<string, int> dictEnumNamesAndValues = new Dictionary<string, int>();

            // Get the display names of enum entries
            string[] enumDisplayNames = Enum.GetNames(typeof(GamepadButton));

            //Get the enum values. In case there are alaises in the enum, we will get a list with duplicate values
            Array enumValues = Enum.GetValues(typeof(GamepadButton));
            List<string> enumStringValues = enumValues.Cast<GamepadButton>().Select(v => v.ToString()).ToList();

            for (int iEnumCounter = 0; iEnumCounter < enumDisplayNames.Length; iEnumCounter++)
            {
                string enumName = enumDisplayNames[iEnumCounter];

                string aliasedName = GetNonAliasedNames(enumStringValues[iEnumCounter]);

                
                // When the aliased name is different from display name, append the aliased name to the actual displayname
                if (!string.IsNullOrEmpty(aliasedName) &&  enumName != aliasedName)
                {
                    enumName = enumName + " (" + aliasedName.ToString() + ")";
                }

                dictEnumNamesAndValues.Add(enumName, (int)enumValues.GetValue(iEnumCounter));
            }

            // Sort the dictionary such that the non aliased names are always first in a given sequence
            var sortedEntries = dictEnumNamesAndValues.OrderBy(x => x.Value)
                               .ThenBy(x => x.Key.Contains("("));
                               

            // Populate the Display names list 
            m_EnumDisplayNames = sortedEntries.Select(x => x.Key).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            // Ensure the enum type is GamepadButton
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                // Draw a custom enum popup
                property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, m_EnumDisplayNames);
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif // UNITY_EDITOR