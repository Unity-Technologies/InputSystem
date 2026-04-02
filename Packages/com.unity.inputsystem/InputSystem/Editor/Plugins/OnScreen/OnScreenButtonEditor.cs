#if UNITY_EDITOR && (PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI)
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.OnScreen
{
    [UnityEditor.CustomEditor(typeof(OnScreenButton))]
    internal class OnScreenButtonEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty m_ControlPathInternal;

        public void OnEnable()
        {
            m_ControlPathInternal = serializedObject.FindProperty(nameof(OnScreenButton.m_ControlPath));
        }

        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.OnScreenButton).Send();
        }

        public override void OnInspectorGUI()
        {
            // Current implementation has UGUI dependencies (ISXB-915, ISXB-916)
            UGUIOnScreenControlEditorUtils.ShowWarningIfNotPartOfCanvasHierarchy((OnScreenButton)target);

            UnityEditor.EditorGUILayout.PropertyField(m_ControlPathInternal);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
