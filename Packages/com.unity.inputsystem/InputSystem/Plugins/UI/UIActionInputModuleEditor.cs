#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.Experimental.Input.Editor;

namespace UnityEngine.Experimental.Input.Plugins.UI.Editor
{
    [CustomEditor(typeof(UIActionInputModule))]
    internal class UIActionInputModuleEditor : UnityEditor.Editor
    {
        private InputActionProperty GetActionReferenceFromAssets(InputActionAsset actions, object[] childAssets, InputActionProperty defaultValue, params string[] actionNames)
        {
            InputAction action = null;
            foreach (var actionName in actionNames)
            {
                action = actions.FindAction(actionName);
                if (action != null)
                {
                    foreach (var asset in childAssets)
                    {
                        if (asset is InputActionReference reference)
                        {
                            if (reference.m_ActionId == action.m_Id)
                                return new InputActionProperty(reference);
                        }
                    }
                }
            }
            return defaultValue;
        }

        private enum ActionReferenceType
        {
            Reference,
            SerializedData
        };

        private string[] m_ActionNames = new[]
        {
            "Point",
            "LeftClick",
            "MiddleClick",
            "RightClick",
            "ScrollWheel",
            "Move",
            "Submit",
            "Cancel"
        };

        private ActionReferenceType[] m_ActionTypes;
        private SerializedProperty[] m_ReferenceProperties;
        private SerializedProperty[] m_DataProperties;
        private bool m_ActionsFoldout;

        public void OnEnable()
        {
            var numActions = m_ActionNames.Length;
            m_ActionTypes = new ActionReferenceType[numActions];
            m_ReferenceProperties = new SerializedProperty[numActions];
            m_DataProperties = new SerializedProperty[numActions];
            for (var i = 0; i < numActions; i++)
            {
                m_ReferenceProperties[i] = serializedObject.FindProperty($"m_{m_ActionNames[i]}ActionReference");
                m_DataProperties[i] = serializedObject.FindProperty($"m_{m_ActionNames[i]}ActionData");
                m_ActionTypes[i] = m_ReferenceProperties[i].objectReferenceValue != null ? ActionReferenceType.Reference : ActionReferenceType.SerializedData;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (Event.current.type == EventType.ExecuteCommand)
            {
                if (Event.current.commandName == "ObjectSelectorUpdated")
                {
                    if (EditorGUIUtility.GetObjectPickerControlID() == GetInstanceID())
                    {
                        var module = target as UIActionInputModule;
                        var actions = (InputActionAsset)EditorGUIUtility.GetObjectPickerObject();
                        var path = AssetDatabase.GetAssetPath(actions);
                        var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                        module.point = GetActionReferenceFromAssets(actions, assets, module.point, "Point", "MousePosition", "Mouse Position");
                        module.leftClick = GetActionReferenceFromAssets(actions, assets, module.leftClick, "Click", "LeftClick", "Left Click");
                        module.rightClick = GetActionReferenceFromAssets(actions, assets, module.rightClick, "RightClick", "Right Click", "ContextClick", "Context Click", "ContextMenu", "Context Menu");
                        module.middleClick = GetActionReferenceFromAssets(actions, assets, module.middleClick, "MiddleClick", "Middle Click");
                        module.scrollWheel = GetActionReferenceFromAssets(actions, assets, module.scrollWheel, "ScrollWheel", "Scroll Wheel", "Scroll", "Wheel");
                        module.move = GetActionReferenceFromAssets(actions, assets, module.move, "Navigate", "Move");
                        module.submit = GetActionReferenceFromAssets(actions, assets, module.submit, "Submit");
                        module.cancel = GetActionReferenceFromAssets(actions, assets, module.cancel, "Cancel", "Esc", "Escape");

                        serializedObject.Update();

                        // reinitialize action types
                        OnEnable();
                    }
                }
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            m_ActionsFoldout = EditorGUILayout.Foldout(m_ActionsFoldout, "Actions", Styles.s_FoldoutStyle);
            EditorGUI.indentLevel--;

            if (m_ActionsFoldout)
            {
                const string buttonLabel = "Link Actions from Asset…";
                EditorGUILayout.HelpBox($"You can assign input actions to generate UI events here. Actions can either be stored as serialized data on this component, or as references to actions in an Input Action Asset. Click '{buttonLabel}' below to automatically assign all actions from an input action asset if they match common names for the UI actions.", MessageType.Info);
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button(buttonLabel, EditorStyles.miniButton))
                    EditorGUIUtility.ShowObjectPicker<InputActionAsset>(null, false, "", GetInstanceID());
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                var numActions = m_ActionNames.Length;
                for (var i = 0; i < numActions; i++)
                {
                    GUILayout.Space(2);
                    GUIHelpers.DrawLineSeparator();
                    GUILayout.Space(2);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(m_ActionNames[i], EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                    m_ActionTypes[i] = (ActionReferenceType)EditorGUILayout.EnumPopup(m_ActionTypes[i]);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    EditorGUILayout.PropertyField(m_ActionTypes[i] == ActionReferenceType.Reference ? m_ReferenceProperties[i] : m_DataProperties[i], GUIContent.none);
                    if (m_ActionTypes[i] == ActionReferenceType.SerializedData)
                        m_ReferenceProperties[i].objectReferenceValue = null;
                }
            }
            GUILayout.EndVertical();
            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        private static class Styles
        {
            public static readonly GUIStyle s_FoldoutStyle = new GUIStyle("foldout");

            static Styles()
            {
                s_FoldoutStyle.fontStyle = FontStyle.Bold;
            }
        }
    }
}
#endif
