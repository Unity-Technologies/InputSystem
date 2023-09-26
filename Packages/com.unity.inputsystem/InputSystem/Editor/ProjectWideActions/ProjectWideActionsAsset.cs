#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        internal const string kTemplateAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.inputactions";
        internal const string kInputManagerAssetPath = "ProjectSettings/InputManager.asset";

        internal const string kDefaultAssetDirectory = "Assets";
        internal const string kDefaultAssetFilename = "ProjectActions.inputactions";
        internal const string kDefaultAssetPath = kDefaultAssetDirectory + "/" + kDefaultAssetFilename;

        static InputActionAsset s_TestAsset = null;

        public const string kAdditionalFilename = "actions.InputSystemActionsAPIGenerator.additionalfile"; // Copy of asset that is fed to the SourceGenerator

#if UNITY_INCLUDE_TESTS
        internal static void SetTestAsset(InputActionAsset testAsset)
        {
            s_TestAsset = testAsset;
        }

        internal static void ResetTestAsset()
        {
            s_TestAsset = null;
        }

#endif

        [InitializeOnLoadMethod]
        internal static void InstallProjectWideActions()
        {
            GetOrCreate();
        }

        internal static InputActionAsset LoadFromPath(string assetPath)
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
            asset.name = InputSystem.kProjectWideActionsAssetName;
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
                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == InputSystem.kProjectWideActionsAssetName) as InputActionAsset;
                if (inputActionsAsset != null)
                    return inputActionsAsset;
            }
            return null;
        }

        internal static InputActionAsset GetOrCreate()
        {
#if UNITY_INCLUDE_TESTS
            if (s_TestAsset != null) return s_TestAsset;
#endif

            // Asset which is designated as _the_ Project Actions Asset
            if (EditorBuildSettings.TryGetConfigObject(
                InputActionsEditorSettingsProvider.kProjectActionsConfigKey,
                out InputActionAsset actionsAsset))
            {
                // @TODO: Test what happens if this asset was deleted from the filesystem but is still in BuildSettings
                actionsAsset.name = InputSystem.kProjectWideActionsAssetName;
                return actionsAsset;
            }

            // v1.8.0-pre1 stored the Actions in ProjectSettings/InputManager.asset.
            // Check if we have a project that was saved on that version and migrate it
            // to save in the default location used from the pre2 release.
            InputActionAsset asset = LoadFromInputManagerSettings();
            if (asset != null)
                asset = CreateAndLoadNewAsset(asset);

            // Create a new one if we couldn't find any existing one to load.
            if (asset == null)
                asset = CreateAndLoadNewAsset();

            // Mark it as being _the_ project-wide actions asset.
            if (asset != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                EditorBuildSettings.AddConfigObject(
                    InputActionsEditorSettingsProvider.kProjectActionsConfigKey,
                    asset,
                    true);
            }

            return asset;
        }

        /// <summary>
        /// Automatically creates the file ProjectActions.inputactions file in the user's Asset directory and loads it.
        /// </summary>
        /// <param name="asset">Content to populate asset with. If null, the project-wide actions template content will be used.</param>
        static InputActionAsset CreateAndLoadNewAsset(InputActionAsset assetContent = null)
        {
            try
            {
                // Never overwrite a user's file
                if (File.Exists(kDefaultAssetPath))
                    throw new Exception("File already exists.");

                if (!Directory.Exists(kDefaultAssetDirectory))
                    Directory.CreateDirectory(kDefaultAssetDirectory);

                if (assetContent == null)
                {
                    File.Copy(kTemplateAssetPath, kDefaultAssetPath);
                }
                else
                {
                    var assetJson = assetContent.ToJson();
                    File.WriteAllText(kDefaultAssetPath, assetJson);
                }

                return LoadFromPath(kDefaultAssetPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not install Project Wide Actions to {kDefaultAssetPath}: ({exception})");
                return null;
            }
        }
    }
}
#endif
