#if UNITY_EDITOR && (PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI)
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.OnScreen
{
    [CustomEditor(typeof(OnScreenStick))]
    internal class OnScreenStickEditor : UnityEditor.Editor
    {
        private AnimBool m_ShowDynamicOriginOptions;
        private AnimBool m_ShowIsolatedInputActions;

        private SerializedProperty m_UseIsolatedInputActions;
        private SerializedProperty m_Behaviour;
        private SerializedProperty m_ControlPathInternal;
        private SerializedProperty m_MovementRange;
        private SerializedProperty m_DynamicOriginRange;
        private SerializedProperty m_PointerDownAction;
        private SerializedProperty m_PointerMoveAction;

        public void OnEnable()
        {
            m_ShowDynamicOriginOptions = new AnimBool(false);
            m_ShowIsolatedInputActions = new AnimBool(false);

            m_UseIsolatedInputActions = serializedObject.FindProperty(nameof(OnScreenStick.m_UseIsolatedInputActions));

            m_Behaviour = serializedObject.FindProperty(nameof(OnScreenStick.m_Behaviour));
            m_ControlPathInternal = serializedObject.FindProperty(nameof(OnScreenStick.m_ControlPath));
            m_MovementRange = serializedObject.FindProperty(nameof(OnScreenStick.m_MovementRange));
            m_DynamicOriginRange = serializedObject.FindProperty(nameof(OnScreenStick.m_DynamicOriginRange));
            m_PointerDownAction = serializedObject.FindProperty(nameof(OnScreenStick.m_PointerDownAction));
            m_PointerMoveAction = serializedObject.FindProperty(nameof(OnScreenStick.m_PointerMoveAction));
        }

        public void OnDisable()
        {
            // Report analytics
            new InputComponentEditorAnalytic(InputSystemComponent.OnScreenStick).Send();
            new OnScreenStickEditorAnalytic(this).Send();
        }

        public override void OnInspectorGUI()
        {
            // Current implementation has UGUI dependencies (ISXB-915, ISXB-916)
            UGUIOnScreenControlEditorUtils.ShowWarningIfNotPartOfCanvasHierarchy((OnScreenStick)target);

            EditorGUILayout.PropertyField(m_MovementRange);
            EditorGUILayout.PropertyField(m_ControlPathInternal);
            EditorGUILayout.PropertyField(m_Behaviour);

            m_ShowDynamicOriginOptions.target = ((OnScreenStick)target).behaviour ==
                OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin;
            if (EditorGUILayout.BeginFadeGroup(m_ShowDynamicOriginOptions.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_DynamicOriginRange);
                if (EditorGUI.EndChangeCheck())
                {
                    ((OnScreenStick)target).UpdateDynamicOriginClickableArea();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_UseIsolatedInputActions);
            m_ShowIsolatedInputActions.target = m_UseIsolatedInputActions.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_ShowIsolatedInputActions.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PointerDownAction);
                EditorGUILayout.PropertyField(m_PointerMoveAction);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
