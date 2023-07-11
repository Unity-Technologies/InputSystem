#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.HighLevel.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    public class InputActionsEditorWindowUtils
    {
        public static StyleSheet theme => EditorGUIUtility.isProSkin
        ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
        : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

        public static void SaveAsset(SerializedObject serializedAsset)
        {
            var asset = (InputActionAsset)serializedAsset.targetObject;
            // for the global actions asset: save differently (as it is a yaml file and not a json)
            if (asset.name == HighLevel.Input.kGlobalActionsAssetName)
            {
                AssetDatabase.SaveAssets();
                return;
            }
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var assetJson = asset.ToJson();
            var existingJson = File.Exists(assetPath) ? File.ReadAllText(assetPath) : "";
            if (assetJson != existingJson)
            {
                EditorHelpers.CheckOut(assetPath);
                File.WriteAllText(assetPath, assetJson);
                AssetDatabase.ImportAsset(assetPath);
            }
        }
    }
}
#endif
