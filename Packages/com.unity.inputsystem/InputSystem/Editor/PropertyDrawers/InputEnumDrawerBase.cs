#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.InputSystem.Editor
{
    public abstract class InputEnumDrawerBase<T> : PropertyDrawer where T : Enum
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (TryGetPopupOptions(property, out string[] popupOptions))
            {
                property.intValue = EditorGUILayout.Popup(label, property.intValue, popupOptions);
            }
            else
            {
                DisplayDefaultEnum(property, label);
            }
            EditorGUI.EndProperty();
        }

        protected abstract bool TryGetPopupOptions(SerializedProperty property, out string[] popupOptions);
        protected abstract void DisplayDefaultEnum(SerializedProperty property, GUIContent label);
    }
}
#endif