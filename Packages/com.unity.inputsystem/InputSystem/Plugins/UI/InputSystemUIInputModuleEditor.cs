#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

////TODO: add button to automatically set up gamepad mouse cursor support

namespace UnityEngine.InputSystem.UI.Editor
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
                    if (string.Compare(action.action.name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0)
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

        private static readonly string[] s_ActionNames =
        {
            "Point",
            "LeftClick",
            "MiddleClick",
            "RightClick",
            "ScrollWheel",
            "Move",
            "Submit",
            "Cancel",
            "TrackedDevicePosition",
            "TrackedDeviceOrientation",
            "TrackedDeviceSelect"
        };

        private static readonly string[] s_ActionNiceNames =
        {
            "Point",
            "Left Click",
            "Middle Click",
            "Right Click",
            "Scroll Wheel",
            "Move",
            "Submit",
            "Cancel",
            "Tracked Position",
            "Tracked Orientation",
            "Tracked Select"
        };

        private SerializedProperty[] m_ReferenceProperties;
        private SerializedProperty m_ActionsAsset;
        private InputActionReference[] m_AvailableActionsInAsset;
        private string[] m_AvailableActionsInAssetNames;

        public void OnEnable()
        {
            var numActions = s_ActionNames.Length;
            m_ReferenceProperties = new SerializedProperty[numActions];
            for (var i = 0; i < numActions; i++)
                m_ReferenceProperties[i] = serializedObject.FindProperty($"m_{s_ActionNames[i]}Action");

            m_ActionsAsset = serializedObject.FindProperty("m_ActionsAsset");
            m_AvailableActionsInAsset = GetAllActionsFromAsset(m_ActionsAsset.objectReferenceValue as InputActionAsset);
            // Ugly hack: GenericMenu interprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
            m_AvailableActionsInAssetNames = new[] { "None" }.Concat(m_AvailableActionsInAsset?.Select(x => x.name.Replace("/", "\uFF0F")) ?? new string[0]).ToArray();
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
                module.trackedDevicePosition = GetActionReferenceFromAssets(assets, module.trackedDevicePosition?.action?.name, "TrackedDevicePosition", "Position");
                module.trackedDeviceOrientation = GetActionReferenceFromAssets(assets, module.trackedDeviceOrientation?.action?.name, "TrackedDeviceOrientation", "Orientation");
                module.trackedDeviceSelect = GetActionReferenceFromAssets(assets, module.trackedDeviceSelect?.action?.name, "TrackedDeviceSelect", "Select");
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

            var numActions = s_ActionNames.Length;
            for (var i = 0; i < numActions; i++)
            {
                if (m_AvailableActionsInAsset == null)
                    continue;

                var index = Array.IndexOf(m_AvailableActionsInAsset, m_ReferenceProperties[i].objectReferenceValue) + 1;
                EditorGUI.BeginChangeCheck();
                index = EditorGUILayout.Popup(s_ActionNiceNames[i], index, m_AvailableActionsInAssetNames);

                if (EditorGUI.EndChangeCheck())
                    m_ReferenceProperties[i].objectReferenceValue = index > 0 ? m_AvailableActionsInAsset[index - 1] : null;
            }

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
