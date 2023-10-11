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
        internal const string kTemplateAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.inputactions";
        internal const string kInputManagerAssetPath = "ProjectSettings/InputManager.asset";

        internal const string kDefaultAssetDirectory = "Assets";
        internal const string kDefaultAssetFilename = InputSystem.kProjectWideActionsAssetName + ".inputactions"; // NB. Importer will change the asset name to the filename, so they must match.
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

        // Store which asset is _the_ Project-Wide Actions asset.
        internal static void SetAsProjectWideActions(InputActionAsset newActionsAsset)
        {
            var newAssetPath = AssetDatabase.GetAssetPath(newActionsAsset);
            if (!string.IsNullOrEmpty(newAssetPath))
            {
                EditorBuildSettings.AddConfigObject(
                    InputActionsEditorSettingsProvider.kProjectActionsConfigKey,
                    newActionsAsset,
                    true);

                RefreshRoslynAdditionalFile(newAssetPath);
            }
        }

        internal static void RefreshRoslynAdditionalFile(string sourceAssetPath)
        {
            // @TODO: Delete all other InputSystemActionsAPIGenerator.additionalfiles in the assets directory
            // @TODO: Move location to always be next to the sourceAsset
            const string destFilePath = "Assets/actions.InputSystemActionsAPIGenerator.additionalfile";

            try
            {
                if (File.Exists(sourceAssetPath))
                {
                    File.Copy(sourceAssetPath, destFilePath, true);
                    AssetDatabase.ImportAsset(destFilePath); // Invoke importer and therefore source generator
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not save actions additional file: '{destFilePath}' ({exception})");
            }
        }

        internal static InputActionAsset LoadFromPath(string assetPath)
        {
            try
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                if (objects == null)
                    throw new FileNotFoundException();

                // This can happen when opening a project and the AssetDatabase is not ready yet.
                // Loading without the AssetDatabase as a temporary stop-gap to prop up `InputSystem.actions` until then.
                if (objects.Length == 0)
                    return LoadDirectFromPath(assetPath);

                var inputActionsAsset = objects.FirstOrDefault(o => o is InputActionAsset) as InputActionAsset;
                inputActionsAsset.name = InputSystem.kProjectWideActionsAssetName;
                return inputActionsAsset;
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not load actions asset: '{assetPath}' ({exception})");
                return null;
            }
        }

        // Load without using the AssetDatabase.
        // There will be no GUID available for the returned asset, so you cannot reference it in EditorBuildSettings.
        internal static InputActionAsset LoadDirectFromPath(string assetPath)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = InputSystem.kProjectWideActionsAssetName;
            try
            {
                string text = File.ReadAllText(assetPath);
                asset.LoadFromJson(text);
                return asset;
            }
            catch (FileNotFoundException exception)
            {
                Debug.LogError($"InputSystem could not load actions asset: '{assetPath}' ({exception})");
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not parse input actions in JSON format from '{assetPath}' ({exception})");
            }

            Object.DestroyImmediate(asset);
            return null;
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

            InputActionAsset asset = TryLoadingFromDefaultLocation();

            // v1.8.0-pre1 stored the Actions in ProjectSettings/InputManager.asset.
            // Check if we have a project that was saved on that version and migrate it
            // to save in the default location used from the pre2 release.
            if (asset == null)
            {
                asset = LoadFromInputManagerSettings();
                if (asset != null)
                    asset = CreateAndLoadNewAsset(content: asset);
            }

            // Create a new one if we couldn't find any existing one to load.
            if (asset == null)
                asset = CreateAndLoadNewAsset();

            if (asset == null)
            {
                Debug.LogError("Inputsystem could not create a Project-Wide InputActionAsset");
                return null;
            }

            // Mark it as being _the_ project-wide actions asset.
            // If AssetDatabase was busy, we cannot do this here and will need to try again later.
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                EditorBuildSettings.AddConfigObject(
                    InputActionsEditorSettingsProvider.kProjectActionsConfigKey,
                    asset,
                    true);
            }

            return asset;
        }

        static InputActionAsset TryLoadingFromDefaultLocation()
        {
            try
            {
                if (File.Exists(kDefaultAssetPath))
                    return LoadFromPath(kDefaultAssetPath);
            }
            catch {}
            return null;
        }

        /// <summary>
        /// Automatically creates the file ProjectActions.inputactions file in the user's Asset directory and loads it.
        /// </summary>
        /// <param name="content">Content to populate the asset file with. If null, the project-wide actions template content will be used.</param>
        static InputActionAsset CreateAndLoadNewAsset(InputActionAsset content = null)
        {
            try
            {
                if (!Directory.Exists(kDefaultAssetDirectory))
                    Directory.CreateDirectory(kDefaultAssetDirectory);

                if (content == null)
                {
                    // Read the template actions actions and regenerate the guids.
                    var text = File.ReadAllText(kTemplateAssetPath);
                    content = InputActionAsset.FromJson(text);
                    foreach (var map in content.actionMaps)
                    {
                        map.m_Id = Guid.NewGuid().ToString();
                        foreach (var action in map.actions)
                            action.m_Id = Guid.NewGuid().ToString();
                    }
                }

                // Write the file to disk
                content.name = InputSystem.kProjectWideActionsAssetName;
                var assetJson = content.ToJson();
                File.WriteAllText(kDefaultAssetPath, assetJson);

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
