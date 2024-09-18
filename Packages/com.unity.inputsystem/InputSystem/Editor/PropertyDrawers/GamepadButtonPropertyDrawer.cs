#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEngine.UIElements;

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
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, m_EnumDisplayNames);
            }

            EditorGUI.EndProperty();
        }

        private void CreateEnumList()
        {
            var enumNamesAndValues = new Dictionary<string, int>();
            var enumDisplayNames = Enum.GetNames(typeof(GamepadButton));
            var enumValues = Enum.GetValues(typeof(GamepadButton)).Cast<GamepadButton>().ToArray();

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
            var sortedEntries = enumNamesAndValues.OrderBy(x => x.Value);

            m_EnumDisplayNames = sortedEntries.Select(x => x.Key).ToArray();
        }

        private string[] m_EnumDisplayNames;
    }
}
#endif // UNITY_EDITOR
