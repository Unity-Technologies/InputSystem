#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorWindowUtils
    {
        public static StyleSheet theme => EditorGUIUtility.isProSkin
        ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
        : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

        public static void SaveAsset(SerializedObject serializedAsset)
        {
            var asset = (InputActionAsset)serializedAsset.targetObject;
            // For project-wide actions asset save works differently. The asset is in YAML format, not JSON.
            if (asset.name == ProjectWideActionsAsset.kAssetName)
            {
                Debug.Log("Project Wide Actions Asset saved");
                ProjectWideActionsAsset.UpdateInputActionReferences();
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

            Debug.Log("Asset saved");
        }
    }
}
#endif
