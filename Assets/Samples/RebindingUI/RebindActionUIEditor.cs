#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

////TODO: support multi-object editing

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A custom inspector for <see cref="RebindActionUI"/> which provides a more convenient way for
    /// picking the binding which to rebind.
    /// </summary>
    [CustomEditor(typeof(RebindActionUI))]
    public class RebindActionUIEditor : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            m_ActionLabelProperty = serializedObject.FindProperty("m_ActionLabel");
            m_BindingTextProperty = serializedObject.FindProperty("m_BindingText");
            m_RebindOverlayProperty = serializedObject.FindProperty("m_RebindOverlay");
            m_RebindTextProperty = serializedObject.FindProperty("m_RebindText");
            m_RebindInfoProperty = serializedObject.FindProperty("m_RebindInfo");
            m_RebindCancelButtonProperty = serializedObject.FindProperty("m_RebindCancelButton");
            m_RebindTimeoutProperty = serializedObject.FindProperty("m_RebindTimeout");
            m_UpdateBindingUIEventProperty = serializedObject.FindProperty("m_UpdateBindingUIEvent");
            m_RebindStartEventProperty = serializedObject.FindProperty("m_RebindStartEvent");
            m_RebindStopEventProperty = serializedObject.FindProperty("m_RebindStopEvent");

            m_BindingUI = new BindingUI(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Binding section.
            m_BindingUI.Draw();

            // UI section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_UILabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_ActionLabelProperty);
                EditorGUILayout.PropertyField(m_BindingTextProperty);
                EditorGUILayout.PropertyField(m_RebindOverlayProperty);
                EditorGUILayout.PropertyField(m_RebindTextProperty);
                EditorGUILayout.PropertyField(m_RebindInfoProperty);
                EditorGUILayout.PropertyField(m_RebindCancelButtonProperty);
            }

            // Rebind options section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_RebindOptionsLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_RebindTimeoutProperty);
            }

            // Events section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_EventsLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_RebindStartEventProperty);
                EditorGUILayout.PropertyField(m_RebindStopEventProperty);
                EditorGUILayout.PropertyField(m_UpdateBindingUIEventProperty);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                m_BindingUI.Refresh();
            }
        }

        private SerializedProperty m_ActionLabelProperty;
        private SerializedProperty m_BindingTextProperty;
        private SerializedProperty m_RebindOverlayProperty;
        private SerializedProperty m_RebindTextProperty;
        private SerializedProperty m_RebindInfoProperty;
        private SerializedProperty m_RebindCancelButtonProperty;
        private SerializedProperty m_RebindTimeoutProperty;
        private SerializedProperty m_RebindStartEventProperty;
        private SerializedProperty m_RebindStopEventProperty;
        private SerializedProperty m_UpdateBindingUIEventProperty;

        private GUIContent m_UILabel = new GUIContent("UI");
        private GUIContent m_RebindOptionsLabel = new GUIContent("Rebind Options");
        private GUIContent m_EventsLabel = new GUIContent("Events");
        private BindingUI m_BindingUI;
    }
}
#endif
