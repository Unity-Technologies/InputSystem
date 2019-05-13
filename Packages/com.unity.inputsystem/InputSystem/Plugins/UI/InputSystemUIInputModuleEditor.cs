#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Plugins.UI.Editor
{
    [CustomEditor(typeof(InputSystemUIInputModule))]
    internal class InputSystemUIInputModuleEditor : UnityEditor.Editor
    {
        private static InputActionReference GetActionReferenceFromAssets(InputActionReference[] actions, params string[] actionNames)
        {
            foreach (var actionName in actionNames)
            {
                foreach (var action in actions)
                {
                    if (string.Compare(action.action.name, actionName, true) == 0)
                        return action;
                }
            }
            return null;
        }

        private static InputActionReference[] GetAllActionsFromAsset(InputActionAsset actions)
        {
            if (actions != null)
            {
                var path = AssetDatabase.GetAssetPath(actions);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                return assets.Where(asset => asset is InputActionReference).Cast<InputActionReference>().OrderBy(x => x.name).ToArray();
            }
            return null;
        }

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

        private SerializedProperty[] m_ReferenceProperties;
        private SerializedProperty m_ActionsAsset;
        private InputActionReference[] m_AvailableActionsInAsset;
        private string[] m_AvailableActionsInAssetNames;

        public void OnEnable()
        {
            var numActions = m_ActionNames.Length;
            m_ReferenceProperties = new SerializedProperty[numActions];
            for (var i = 0; i < numActions; i++)
                m_ReferenceProperties[i] = serializedObject.FindProperty($"m_{m_ActionNames[i]}Action");

            m_ActionsAsset = serializedObject.FindProperty("m_ActionsAsset");
            m_AvailableActionsInAsset = GetAllActionsFromAsset(m_ActionsAsset.objectReferenceValue as InputActionAsset);
            // Ugly hack: GenericMenu iterprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
            m_AvailableActionsInAssetNames = new[] { "None" }.Concat(m_AvailableActionsInAsset?.Select(x => x.name.Replace("/", "\u2215")) ?? new string[0]).ToArray();
        }

        public static void ReassignActions(InputSystemUIInputModule module, InputActionAsset action)
        {
            module.actionsAsset = action;
            var assets = GetAllActionsFromAsset(action);
            if (assets != null)
            {
                module.point = GetActionReferenceFromAssets(assets, module.point?.action?.name, "Point", "MousePosition", "Mouse Position");
                module.leftClick = GetActionReferenceFromAssets(assets, module.leftClick?.action?.name, "Click", "LeftClick", "Left Click");
                module.rightClick = GetActionReferenceFromAssets(assets, module.rightClick?.action?.name, "RightClick", "Right Click", "ContextClick", "Context Click", "ContextMenu", "Context Menu");
                module.middleClick = GetActionReferenceFromAssets(assets, module.middleClick?.action?.name, "MiddleClick", "Middle Click");
                module.scrollWheel = GetActionReferenceFromAssets(assets, module.scrollWheel?.action?.name, "ScrollWheel", "Scroll Wheel", "Scroll", "Wheel");
                module.move = GetActionReferenceFromAssets(assets, module.move?.action?.name, "Navigate", "Move");
                module.submit = GetActionReferenceFromAssets(assets, module.submit?.action?.name, "Submit");
                module.cancel = GetActionReferenceFromAssets(assets, module.cancel?.action?.name, "Cancel", "Esc", "Escape");
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ActionsAsset);
            if (EditorGUI.EndChangeCheck())
            {
                var actions = m_ActionsAsset.objectReferenceValue as InputActionAsset;
                if (actions != null)
                {
                    serializedObject.ApplyModifiedProperties();

                    ReassignActions(target as InputSystemUIInputModule, actions);

                    serializedObject.Update();
                }

                // reinitialize action types
                OnEnable();
            }

            var numActions = m_ActionNames.Length;
            for (var i = 0; i < numActions; i++)
            {
                if (m_AvailableActionsInAsset != null)
                {
                    int index = Array.IndexOf(m_AvailableActionsInAsset, m_ReferenceProperties[i].objectReferenceValue) + 1;
                    EditorGUI.BeginChangeCheck();
                    index = EditorGUILayout.Popup(m_ActionNames[i], index, m_AvailableActionsInAssetNames);

                    if (EditorGUI.EndChangeCheck())
                        m_ReferenceProperties[i].objectReferenceValue = index > 0 ? m_AvailableActionsInAsset[index - 1] : null;
                }
            }

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
