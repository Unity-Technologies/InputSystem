#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        internal const string kDefaultAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.inputactions";
        internal const string kInputManagerAssetPath = "ProjectSettings/InputManager.asset";

        internal const string kAssetDirectory = "Assets/InputSystem";
        internal const string kAssetFilename = "actions.InputSystemActionsAPIGenerator.additionalfile";
        internal const string kAssetName = InputSystem.kProjectWideActionsAssetName;

        static string s_DefaultAssetPath = kDefaultAssetPath;
        static string s_AssetPath = Path.Combine(kAssetDirectory, kAssetFilename);

        public static string assetPath { get { return s_AssetPath; } }

#if UNITY_INCLUDE_TESTS
        internal static void SetAssetPaths(string defaultAssetPath, string runtimeAssetPath)
        {
            s_DefaultAssetPath = defaultAssetPath;
            s_AssetPath = runtimeAssetPath;
        }

        internal static void Reset()
        {
            s_DefaultAssetPath = kDefaultAssetPath;
            s_AssetPath = Path.Combine(kAssetDirectory, kAssetFilename);
        }

#endif

        [InitializeOnLoadMethod]
        internal static void InstallProjectWideActions()
        {
            GetOrCreate();
        }

        internal static InputActionAsset LoadFromAsset()
        {
            string text;
            try
            {
                text = File.ReadAllText(assetPath);
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
                Debug.LogError($"InputSystem could not parse input actions in JSON format from '{assetPath}' ({exception})");
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

            asset = LoadFromAsset();
            if (asset != null) return asset;

            // v1.8.0-pre1 stored the Actions in ProjectSettings/InputManager.asset.
            // Check if we have a project that was saved on that version and migrate it
            asset = LoadFromInputManagerSettings();
            if (asset != null)
            {
                // Migrate the pre1 asset
                SaveAsset(asset);

                // Load from the newly migrated asset.
                asset = LoadFromAsset();
                if (asset != null) return asset;
            }

            // Create a new one if we couldn't find any existing one to load.
            return CreateNewActionAsset();
        }

        static void SaveAsset(InputActionAsset asset)
        {
            try
            {
                if (!Directory.Exists(kAssetDirectory))
                    Directory.CreateDirectory(kAssetDirectory);

                var assetJson = asset.ToJson();
                File.WriteAllText(assetPath, assetJson);

            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not save project-wide actions to {assetPath}:  ({exception})");
            }
        }

        static InputActionAsset CreateNewActionAsset()
        {
            try
            {
                if (!Directory.Exists(kAssetDirectory))
                    Directory.CreateDirectory(kAssetDirectory);

                File.Copy(s_DefaultAssetPath, assetPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not instal Project Wide Actions to {assetPath}: ({exception})");
                return null;
            }

            return LoadFromAsset();
        }
    }
}
#endif
