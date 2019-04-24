#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Plugins.UI.Editor
{
    [CustomEditor(typeof(UIActionInputModule))]
    public class UIActionInputModuleEditor: UnityEditor.Editor
    {
        InputActionProperty GetActionReferenceFromAssets(InputActionAsset actions, object[] childAssets, InputActionProperty defaultValue, params string[] actionNames)
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

        public override void OnInspectorGUI()
        { 
            base.OnInspectorGUI();
            if (Event.current.type == EventType.ExecuteCommand)
            {
                if (Event.current.commandName == "ObjectSelectorUpdated")
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
                }
            }
            if (GUILayout.Button("Read actions from asset…"))
                EditorGUIUtility.ShowObjectPicker<InputActionAsset>(null, false, "", 0);

        }
    }
}
#endif