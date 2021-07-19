#if UNITY_EDITOR
using System;
using UnityEditor;
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
        private InputControlPathEditor m_Editor;
        private InputControlPickerState m_PickerState;

        public void Dispose()
        {
            m_Editor?.Dispose();
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
                m_Editor.SetExpectedControlLayoutFromAttribute();
            }

            EditorGUI.BeginProperty(position, label, property);
            m_Editor.OnGUI(position);
            EditorGUI.EndProperty();
        }
    }
}
#endif // UNITY_EDITOR
