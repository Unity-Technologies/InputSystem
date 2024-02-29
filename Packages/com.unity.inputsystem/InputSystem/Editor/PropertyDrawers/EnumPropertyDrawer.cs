#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
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

        protected abstract string GetNonAliasedNames(string enumValue);

        private void Initialize()
        {
            var enumNamesAndValues = new Dictionary<string, int>();
            var enumDisplayNames = Enum.GetNames(typeof(T));
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var enumStringValues = enumValues.Select(v => v.ToString()).ToArray();

            for (var i = 0; i < enumDisplayNames.Length; ++i)
            {
                var enumName = enumDisplayNames[i];
                var aliasedName = GetNonAliasedNames(enumStringValues[i]);
                if (!string.IsNullOrEmpty(aliasedName) && enumName != aliasedName)
                    enumName = $"{enumName} ({aliasedName})";

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
                if (m_EnumDisplayNames == null)
                    Initialize();

                property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, m_EnumDisplayNames);
            }

            EditorGUI.EndProperty();
        }
    }
}
 #endif // UNITY_EDITOR
