#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen.Editor
{
    public abstract class OnScreenControlEditor : UnityEditor.Editor
    {
        [SerializeField] private bool m_ManualPathEditMode;
        [SerializeField] private InputControlPickerState m_ControlPickerState;

        private SerializedProperty m_ControlPathProperty;
        private InputBindingPropertiesView m_PropertyView;
        private InputControlPickerPopup m_PathSelector;

        public void OnEnable()
        {
            if (m_ControlPickerState == null)
                m_ControlPickerState = new InputControlPickerState();

            m_PathSelector = new InputControlPickerPopup(
                serializedObject.FindProperty("m_ControlPath"),
                m_ControlPickerState,
                s =>
                {
                    serializedObject.ApplyModifiedProperties();
                    m_ManualPathEditMode = false;
                    Repaint();
                },
                null);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            m_PathSelector.DrawBindingGUI(ref m_ManualPathEditMode);
            EditorGUILayout.Space();
        }
    }
}
#endif // UNITY_EDITOR
