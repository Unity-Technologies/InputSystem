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
    [CustomEditor(typeof(ActionLabel))]
    public class ActionLabelEditor : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            m_BindingTextProperty = serializedObject.FindProperty("m_BindingText");
            m_UpdateBindingUIEventProperty = serializedObject.FindProperty("m_UpdateBindingUIEvent");
            m_BindingUI = new BindingUI(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Binding section.
            m_BindingUI.Draw();

            // UI section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_UILabel, Styles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_BindingTextProperty);
            }

            // Events section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_EventsLabel, Styles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_UpdateBindingUIEventProperty);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                m_BindingUI.Refresh();
            }
        }

        private SerializedProperty m_BindingTextProperty;
        private SerializedProperty m_UpdateBindingUIEventProperty;

        private GUIContent m_UILabel = new GUIContent("UI");
        private GUIContent m_EventsLabel = new GUIContent("Events");
        private BindingUI m_BindingUI;

        private static class Styles
        {
            public static GUIStyle boldLabel = new GUIStyle("MiniBoldLabel");
        }
    }
}
#endif
