#if UNITY_INPUT_SYSTEM_ENABLE_UI && UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEngine.InputSystem.Editor;

////TODO: add button to automatically set up gamepad mouse cursor support

namespace UnityEngine.InputSystem.UI.Editor
{
    [CustomEditor(typeof(InputSystemUIInputModule))]
    [InitializeOnLoad]
    internal class InputSystemUIInputModuleEditor : UnityEditor.Editor
    {
        static InputSystemUIInputModuleEditor()
        {
#if UNITY_6000_0_OR_NEWER && ENABLE_INPUT_SYSTEM
            InputModuleComponentFactory.SetInputModuleComponentOverride(
                go => ObjectFactory.AddComponent<InputSystemUIInputModule>(go));
#endif
        }

        private static InputActionReference GetActionReferenceFromAssets(InputActionReference[] actions, params string[] actionNames)
        {
            foreach (var actionName in actionNames)
            {
                foreach (var action in actions)
                {
                    if (action.action != null && string.Compare(action.action.name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return action;
                }
            }
            return null;
        }

        private static InputActionReference[] GetAllAssetReferencesFromAssetDatabase(InputActionAsset actions)
        {
            if (actions == null)
                return null;

            var path = AssetDatabase.GetAssetPath(actions);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets.Where(asset => asset is InputActionReference)
                .Cast<InputActionReference>()
                .OrderBy(x => x.name)
                .ToArray();
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
            "TrackedDeviceOrientation"
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
            "Tracked Orientation"
        };

        private SerializedProperty[] m_ReferenceProperties;
        private SerializedProperty m_ActionsAsset;
        private InputActionReference[] m_AvailableActionReferencesInAssetDatabase;
        private string[] m_AvailableActionsInAssetNames;
        private bool m_AdvancedFoldoutState;

        private string MakeActionReferenceNameUsableInGenericMenu(string name)
        {
            // Ugly hack: GenericMenu interprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
            return name.Replace("/", "\uFF0F");
        }

        public void OnEnable()
        {
            var numActions = s_ActionNames.Length;
            m_ReferenceProperties = new SerializedProperty[numActions];
            for (var i = 0; i < numActions; i++)
                m_ReferenceProperties[i] = serializedObject.FindProperty($"m_{s_ActionNames[i]}Action");

            m_ActionsAsset = serializedObject.FindProperty("m_ActionsAsset");
            m_AvailableActionReferencesInAssetDatabase = GetAllAssetReferencesFromAssetDatabase(m_ActionsAsset.objectReferenceValue as InputActionAsset);
            m_AvailableActionsInAssetNames = new[] { "None" }
                .Concat(m_AvailableActionReferencesInAssetDatabase?.Select(x => MakeActionReferenceNameUsableInGenericMenu(x.name)) ?? new string[0]).ToArray();
        }

        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.InputSystemUIInputModule).Send();
        }

        public static void ReassignActions(InputSystemUIInputModule module, InputActionAsset action)
        {
            module.actionsAsset = action;
            var assets = GetAllAssetReferencesFromAssetDatabase(action);
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
            if ((m_AvailableActionReferencesInAssetDatabase != null && m_AvailableActionReferencesInAssetDatabase.Length > 0) || m_ActionsAsset.objectReferenceValue == null)
            {
                for (var i = 0; i < numActions; i++)
                {
                    // find the input action reference from the asset that matches the input action reference from the
                    // InputSystemUIInputModule that is currently selected. Note we can't use reference equality of the
                    // two InputActionReference objects here because in ReassignActions above, we create new instances
                    // every time it runs.
                    var index = IndexOfInputActionInAsset(
                        ((InputActionReference)m_ReferenceProperties[i]?.objectReferenceValue)?.action);

                    EditorGUI.BeginChangeCheck();
                    index = EditorGUILayout.Popup(s_ActionNiceNames[i], index, m_AvailableActionsInAssetNames);

                    if (EditorGUI.EndChangeCheck())
                        m_ReferenceProperties[i].objectReferenceValue =
                            index > 0 ? m_AvailableActionReferencesInAssetDatabase[index - 1] : null;
                }
            }
            else
            {
                // Somehow we have an asset but no asset references from the database, pull out references manually and show them in read only UI
                EditorGUILayout.HelpBox("Showing fields as read-only because current action asset seems to be created by a script and assigned programmatically.", MessageType.Info);

                EditorGUI.BeginDisabledGroup(true);
                for (var i = 0; i < numActions; i++)
                {
                    var retrievedName = "None";
                    if (m_ReferenceProperties[i].objectReferenceValue != null &&
                        (m_ReferenceProperties[i].objectReferenceValue is InputActionReference reference))
                        retrievedName = MakeActionReferenceNameUsableInGenericMenu(reference.ToDisplayName());

                    EditorGUILayout.Popup(s_ActionNiceNames[i], 0, new[] {retrievedName});
                }
                EditorGUI.EndDisabledGroup();
            }

            m_AdvancedFoldoutState = EditorGUILayout.Foldout(m_AdvancedFoldoutState, new GUIContent("Advanced"), true);
            if (m_AdvancedFoldoutState)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CursorLockBehavior"),
                    EditorGUIUtility.TrTextContent("Cursor Lock Behavior",
                        $"Controls the origin point of UI raycasts when the cursor is locked. {InputSystemUIInputModule.CursorLockBehavior.OutsideScreen} " +
                        $"is the default behavior and will force the raycast to miss all objects. {InputSystemUIInputModule.CursorLockBehavior.ScreenCenter} " +
                        $"will cast the ray from the center of the screen."));

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        private int IndexOfInputActionInAsset(InputAction inputAction)
        {
            // return 0 instead of -1 here because the zero-th index refers to the 'None' binding.
            if (inputAction == null)
                return 0;
            if (m_AvailableActionReferencesInAssetDatabase == null)
                return 0;

            var index = 0;
            for (var j = 0; j < m_AvailableActionReferencesInAssetDatabase.Length; j++)
            {
                if (m_AvailableActionReferencesInAssetDatabase[j].action != null &&
                    m_AvailableActionReferencesInAssetDatabase[j].action == inputAction)
                {
                    index = j + 1;
                    break;
                }
            }

            return index;
        }
    }
}
#endif
