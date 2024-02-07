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
            SaveAsset(asset);
        }

        public static void SaveAsset(InputActionAsset asset)
        {
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
