using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Property drawer for <see cref = "GamepadButton" />
    /// </summary >
    [CustomPropertyDrawer(typeof(GamepadButton))]
    internal class GamepadButtonPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            CreateEnumList();
            return base.CreatePropertyGUI(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (m_EnumDisplayNames == null)
            {
                CreateEnumList();
            }

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.intValue = m_EnumValues[EditorGUI.Popup(position, label.text, GetEnumIndex(property.intValue), m_EnumDisplayNames)];
            }

            EditorGUI.EndProperty();
        }

        private void CreateEnumList()
        {
            string[] enumDisplayNames = Enum.GetNames(typeof(GamepadButton));
            var enumValues = Enum.GetValues(typeof(GamepadButton));
            var enumNamesAndValues = new Dictionary<string, int>(enumDisplayNames.Length);

            for (var i = 0; i < enumDisplayNames.Length; ++i)
            {
                string enumName;
                switch (enumDisplayNames[i])
                {
                    case nameof(GamepadButton.Y):
                    case nameof(GamepadButton.Triangle):
                    case nameof(GamepadButton.A):
                    case nameof(GamepadButton.Cross):
                    case nameof(GamepadButton.B):
                    case nameof(GamepadButton.Circle):
                    case nameof(GamepadButton.X):
                    case nameof(GamepadButton.Square):
                        continue;
                    case nameof(GamepadButton.North):
                        enumName = "North, Y, Triangle, X";
                        break;
                    case nameof(GamepadButton.South):
                        enumName = "South, A, Cross, B";
                        break;
                    case nameof(GamepadButton.East):
                        enumName = "East, B, Circle, A";
                        break;
                    case nameof(GamepadButton.West):
                        enumName = "West, X, Square, Y";
                        break;
                    default:
                        enumName = enumDisplayNames[i];
                        break;
                }
                enumNamesAndValues.Add(enumName, (int)enumValues.GetValue(i));
            }
            SetEnumDisplayNames(enumNamesAndValues);
        }

        // Sorts the values so that they get displayed consistently, and assigns them for being drawn.
        private void SetEnumDisplayNames(Dictionary<string, int> enumNamesAndValues)
        {
            m_EnumValues = new int[enumNamesAndValues.Count];
            enumNamesAndValues.Values.CopyTo(m_EnumValues, 0);

            m_EnumDisplayNames = new string[enumNamesAndValues.Count];
            enumNamesAndValues.Keys.CopyTo(m_EnumDisplayNames, 0);

            Array.Sort(m_EnumValues, m_EnumDisplayNames);
        }

        // Ensures mapping between displayed value and actual value is consistent. Issues arise when there are gaps in the enum values (ie 0, 1, 13).
        private int GetEnumIndex(int enumValue)
        {
            for (int i = 0; i < m_EnumValues.Length; i++)
            {
                if (enumValue == m_EnumValues[i])
                {
                    return i;
                }
            }
            return 0;
        }

        private int[] m_EnumValues;
        private string[] m_EnumDisplayNames;
    }
}
 #endif // UNITY_EDITOR
