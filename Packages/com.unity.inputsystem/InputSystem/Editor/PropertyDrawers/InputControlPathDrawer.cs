#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom property drawer for string type fields that represent input control paths.
    /// </summary>
    /// <remarks>
    /// To use this drawer on a property, apply <see cref="InputControlAttribute"/> on it. To constrain
    /// what type of control is picked, set the <see cref="InputControlAttribute.layout"/> field on the
    /// attribute.
    ///
    /// <example>
    /// <code>
    /// // A string representing a control path. Constrain it to picking Button-type controls.
    /// [InputControl(layout = "Button")]
    /// [SerializeField] private string m_ControlPath;
    /// </code>
    /// </example>
    /// </remarks>
    [CustomPropertyDrawer(typeof(InputControlAttribute))]
    internal sealed class InputControlPathDrawer : PropertyDrawer, IDisposable
    {
        private InputControlPickerState m_PickerState;
        private InputControlPathEditor m_Editor;

        public void Dispose()
        {
            m_Editor?.Dispose();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (m_PickerState == null)
                m_PickerState = new InputControlPickerState();
            var editor = new InputControlPathEditor(property, m_PickerState,
                () => property.serializedObject.ApplyModifiedProperties(),
                new GUIContent(property.displayName, property.tooltip));
            editor.SetExpectedControlLayoutFromAttribute();
            return editor.CreateVisualElement(property, () => property.serializedObject.ApplyModifiedProperties());
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_PickerState == null)
                m_PickerState = new InputControlPickerState();
            if (m_Editor == null)
            {
                m_Editor = new InputControlPathEditor(property, m_PickerState,
                    () => property.serializedObject.ApplyModifiedProperties(),
                    label: label);
            }

            EditorGUI.BeginProperty(position, label, property);
            m_Editor.OnGUI(position, label, property, () => property.serializedObject.ApplyModifiedProperties());
            EditorGUI.EndProperty();
        }
    }
}
#endif // UNITY_EDITOR
