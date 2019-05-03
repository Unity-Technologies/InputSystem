#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;

namespace UnityEngine.Experimental.Input.Plugins.UI.Editor
{
    [CustomEditor(typeof(InputSystemUIInputModule))]
    internal class InputSystemUIInputModuleEditor : UnityEditor.Editor
    {
        private InputActionProperty GetActionReferenceFromAssets(InputActionReference[] actions, params string[] actionNames)
        {
            foreach (var actionName in actionNames)
            {
                foreach (var action in actions)
                {
                    if (string.Compare(action.action.name, actionName, true) == 0)
                        return new InputActionProperty(action);
                }
            }
            return null;
        }

        private InputActionReference[] GetAllActionsFromAsset()
        {
            var actions = m_ActionsAsset.objectReferenceValue as InputActionAsset;
            if (actions != null)
            {
                var module = target as InputSystemUIInputModule;
                var path = AssetDatabase.GetAssetPath(actions);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                return assets.Where(asset => asset is InputActionReference).Cast<InputActionReference>().OrderBy(x => x.name).ToArray();
            }
            return null;
        }

        private enum ActionReferenceType
        {
            Reference,
            SerializedData
        };

        static private string[] m_ActionNames = new[]
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
        private SerializedProperty m_ActionsAsset;
        private InputActionReference[] m_AvailableActionsInAsset;
        private string[] m_AvailableActionsInAssetNames;

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
            m_ActionsAsset = serializedObject.FindProperty("m_ActionsAsset");
            m_AvailableActionsInAsset = GetAllActionsFromAsset();
            // Ugly hack: GenericMenu iterprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
            m_AvailableActionsInAssetNames = new[] { "None" }.Concat(m_AvailableActionsInAsset?.Select(x => x.name.Replace("/", "\u2215")) ?? new string[0]).ToArray();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            m_ActionsFoldout = EditorGUILayout.Foldout(m_ActionsFoldout, "Actions", Styles.s_FoldoutStyle);
            EditorGUI.indentLevel--;

            if (m_ActionsFoldout)
            {
                EditorGUILayout.HelpBox("You can optionally specify an Input Action Asset here. Then all actions will be taken from that asset, and if there is a `PlayerInput` component on this game object using the same Input Action Asset, it will keep the actions on the UI modules synched to the same player.", MessageType.Info);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ActionsAsset);
                if (EditorGUI.EndChangeCheck())
                {
                    var assets = GetAllActionsFromAsset();
                    if (assets != null)
                    {
                        serializedObject.ApplyModifiedProperties();

                        var module = target as InputSystemUIInputModule;
                        module.point = GetActionReferenceFromAssets(assets, module.point.action.name, "Point", "MousePosition", "Mouse Position");
                        module.leftClick = GetActionReferenceFromAssets(assets, module.leftClick.action.name, "Click", "LeftClick", "Left Click");
                        module.rightClick = GetActionReferenceFromAssets(assets, module.rightClick.action.name, "RightClick", "Right Click", "ContextClick", "Context Click", "ContextMenu", "Context Menu");
                        module.middleClick = GetActionReferenceFromAssets(assets, module.middleClick.action.name, "MiddleClick", "Middle Click");
                        module.scrollWheel = GetActionReferenceFromAssets(assets, module.scrollWheel.action.name, "ScrollWheel", "Scroll Wheel", "Scroll", "Wheel");
                        module.move = GetActionReferenceFromAssets(assets, module.move.action.name, "Navigate", "Move");
                        module.submit = GetActionReferenceFromAssets(assets, module.submit.action.name, "Submit");
                        module.cancel = GetActionReferenceFromAssets(assets, module.cancel.action.name, "Cancel", "Esc", "Escape");

                        serializedObject.Update();
                    }

                    // reinitialize action types
                    OnEnable();
                }

                var numActions = m_ActionNames.Length;
                for (var i = 0; i < numActions; i++)
                {
                    GUILayout.Space(2);
                    GUIHelpers.DrawLineSeparator();
                    GUILayout.Space(2);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(m_ActionNames[i], EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                    if (m_AvailableActionsInAsset == null)
                        m_ActionTypes[i] = (ActionReferenceType)EditorGUILayout.EnumPopup(m_ActionTypes[i]);
                    else
                    {
                        int index = Array.IndexOf(m_AvailableActionsInAsset, m_ReferenceProperties[i].objectReferenceValue) + 1;
                        EditorGUI.BeginChangeCheck();
                        index = EditorGUILayout.Popup(index, m_AvailableActionsInAssetNames);
                        if (EditorGUI.EndChangeCheck())
                            m_ReferenceProperties[i].objectReferenceValue = index > 0 ? m_AvailableActionsInAsset[index - 1] : null;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    if (m_AvailableActionsInAsset == null)
                    {
                        EditorGUILayout.PropertyField(m_ActionTypes[i] == ActionReferenceType.Reference ? m_ReferenceProperties[i] : m_DataProperties[i], GUIContent.none);
                        if (m_ActionTypes[i] == ActionReferenceType.SerializedData)
                            m_ReferenceProperties[i].objectReferenceValue = null;
                    }
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
