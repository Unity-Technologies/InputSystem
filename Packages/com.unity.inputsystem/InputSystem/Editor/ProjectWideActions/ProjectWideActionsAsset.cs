#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        internal const string kDefaultAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.inputactions";
        internal const string kInputManagerAssetPath = "ProjectSettings/InputManager.asset";
        internal const string kAssetPath = "Assets/InputSystem/actions.InputSystemActionsAPIGenerator.additionalfile";
        internal const string kAssetName = InputSystem.kProjectWideActionsAssetName;

        static string s_DefaultAssetPath = kDefaultAssetPath;
        static string s_AssetPath = kAssetPath;

        public static string assetPath { get { return s_AssetPath; } }

#if UNITY_INCLUDE_TESTS
        internal static void SetAssetPaths(string defaultAssetPath, string assetPath)
        {
            s_DefaultAssetPath = defaultAssetPath;
            s_AssetPath = assetPath;
        }

        internal static void Reset()
        {
            s_DefaultAssetPath = kDefaultAssetPath;
            s_AssetPath = kAssetPath;
        }

#endif

        [InitializeOnLoadMethod]
        internal static void InstallProjectWideActions()
        {
            GetOrCreate();
        }

        internal static InputActionAsset LoadFromProjectSettings()
        {
            string text;
            try
            {
                text = File.ReadAllText(s_AssetPath);
            }
            catch
            {
                return null;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = kAssetName;
            try
            {
                asset.LoadFromJson(text);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not parse input actions in JSON format from '{s_AssetPath}' ({exception})");
                Object.DestroyImmediate(asset);
                return null;
            }
            return asset;
        }

        internal static InputActionAsset LoadFromInputManagerSettings()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(kInputManagerAssetPath);
            if (objects != null)
            {
                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kAssetName) as InputActionAsset;
                if (inputActionsAsset != null)
                    return inputActionsAsset;
            }
            return null;
        }

        internal static InputActionAsset GetOrCreate()
        {
            InputActionAsset asset;

            asset = LoadFromProjectSettings();
            if (asset != null) return asset;

            // v1.8.0-pre1 stored the Actions in ProjectSettings/InputManager.asset.
            // Check if we have a project that was saved on that version and migrate it
            asset = LoadFromInputManagerSettings();
            if (asset != null)
            {
                // Migrate the pre1 asset
                var assetJson = asset.ToJson();
                File.WriteAllText(s_AssetPath, assetJson);

                // Load from the newly migrated asset.
                asset = LoadFromProjectSettings();
                if (asset != null) return asset;
            }

            // Create a new one if we couldn't find any existing one to load.
            return CreateNewActionAsset();
        }

        private static InputActionAsset CreateNewActionAsset()
        {
            var templateAssetPath = Path.Combine(Environment.CurrentDirectory, s_DefaultAssetPath);
            var projectAssetPath = Path.Combine(Environment.CurrentDirectory, s_AssetPath);
            File.Copy(templateAssetPath, projectAssetPath);

            return LoadFromProjectSettings();
        }
    }
}
#endif
