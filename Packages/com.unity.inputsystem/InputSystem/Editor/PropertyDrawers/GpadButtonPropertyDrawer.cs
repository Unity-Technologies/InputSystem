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
    internal class GpadButtonPropertyDrawer : PropertyDrawer
    {
        private string[] m_EnumDisplayNames;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumNamesAndValues = new Dictionary<string, int>();
            var enumDisplayNames = Enum.GetNames(typeof(GamepadButton));
            var enumValues = Enum.GetValues(typeof(GamepadButton)).Cast<GamepadButton>().ToArray();

            for (var i = 0; i < enumDisplayNames.Length; ++i)
            {
                string enumName = enumDisplayNames[i];

                switch (enumName)
                {
                    case nameof(GamepadButton.Y):
                    case nameof(GamepadButton.Triangle):
                    case nameof(GamepadButton.A):
                    case nameof(GamepadButton.Cross):
                    case nameof(GamepadButton.B):
                    case nameof(GamepadButton.Circle):
                    case nameof(GamepadButton.X):
                    case nameof(GamepadButton.Square):
                        enumName = null;
                        break;
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
                        break;
                }

                if (enumName != null)
                {
                    enumNamesAndValues.Add(enumName, (int)enumValues.GetValue(i));
                }
            }
            var sortedEntries = enumNamesAndValues.OrderBy(x => x.Value);

            m_EnumDisplayNames = sortedEntries.Select(x => x.Key).ToArray();
            return base.CreatePropertyGUI(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, m_EnumDisplayNames);
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif // UNITY_EDITOR
