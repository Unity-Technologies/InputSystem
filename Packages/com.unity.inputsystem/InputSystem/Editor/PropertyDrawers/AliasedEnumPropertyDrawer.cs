#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Abstract base class for a generic property drawer for aliased enums.
    /// </summary>
    internal abstract class AliasedEnumPropertyDrawer<T> : PropertyDrawer where T : Enum
    {
        private string[] m_EnumDisplayNames;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ProcessDisplayNamesForAliasedEnums();
            return base.CreatePropertyGUI(property);
        }

        protected abstract bool TryGetNonAliasedNames(string enumName, string displayName, out string outputName);

        private void ProcessDisplayNamesForAliasedEnums()
        {
            var enumNamesAndValues = new Dictionary<string, int>();
            var enumDisplayNames = Enum.GetNames(typeof(T));
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var enumStringValues = enumValues.Select(v => v.ToString()).ToArray();
            for (var i = 0; i < enumDisplayNames.Length; ++i)
            {
                var enumName = enumDisplayNames[i];

                if (TryGetNonAliasedNames(enumStringValues[i], enumDisplayNames[i], out string aliasedName))
                {
                    if (!string.IsNullOrEmpty(aliasedName))
                    {
                        enumName = $"{enumName} ({aliasedName})";
                    }
                    else
                    {
                        continue;
                    }
                }

                enumNamesAndValues.Add(enumName, (int)enumValues.GetValue(i));
            }

            var sortedEntries = enumNamesAndValues
                .OrderBy(x => x.Value)
                .ThenBy(x => x.Key.Contains("("));

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
}
#endif // UNITY_EDITOR
