#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Editor;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen.Editor
{
    public abstract class OnScreenControlEditor : UnityEditor.Editor
    {
        [SerializeField] private bool m_ManualPathEditMode;
        [SerializeField] private TreeViewState m_ControlPickerTreeViewState;

        private SerializedProperty m_ControlPathProperty;
        private InputBindingPropertiesView m_PropertyView;

        public void OnEnable()
        {
            m_ControlPathProperty = serializedObject.FindProperty("m_ControlPath");
            if (m_ControlPickerTreeViewState == null)
                m_ControlPickerTreeViewState = new TreeViewState();
            m_PropertyView = new InputBindingPropertiesView(m_ControlPathProperty, null, m_ControlPickerTreeViewState);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            ////TODO: line up "Binding" so it conforms to width of property names used in other inspectors
            m_PropertyView.DrawBindingGUI(m_ControlPathProperty, ref m_ManualPathEditMode, m_ControlPickerTreeViewState,
                s => { m_ManualPathEditMode = false; });
            EditorGUILayout.Space();
        }
    }
}
#endif // UNITY_EDITOR
