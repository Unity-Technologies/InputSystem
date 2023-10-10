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
        internal const string kAssetPath = "ProjectSettings/InputSystemActions.inputactions";

        static InputActionAsset s_TestAsset = null;

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

        // Ensures the project-wide settings are initialized early.
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
                text = File.ReadAllText(kAssetPath);
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
                Debug.LogError($"Could not parse input actions in JSON format from '{kAssetPath}' ({exception})");
                Object.DestroyImmediate(asset);
                return null;
            }
            return asset;
        }

        internal static InputActionAsset LoadFromInputManagerSettings()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(kInputManagerAssetPath);
            if (objects != null)
                return objects.FirstOrDefault(o => o != null && o.name == InputSystem.kProjectWideActionsAssetName) as InputActionAsset;
            return null;
        }

        internal static InputActionAsset GetOrCreate()
        {
#if UNITY_INCLUDE_TESTS
            if (s_TestAsset != null) return s_TestAsset;
#endif

            InputActionAsset asset;

            asset = LoadFromProjectSettings();
            if (asset != null) return asset;

            // v1.8.0-pre1 stored the Actions in ProjectSettings/InputManager.asset.
            // Check if we have a project that was saved on that version and migrate it
            // to save in the default location used from the pre2 release.
            asset = LoadFromInputManagerSettings();
            if (asset != null)
                asset = CreateAndLoadNewAsset(content: asset);
            if (asset != null) return asset;

            // Create a new one if we couldn't find any existing one to load.
            asset = CreateAndLoadNewAsset();
            if (asset != null) return asset;

            Debug.LogError("Inputsystem could not create a Project-Wide InputActionAsset");
            return null;
        }

        /// <summary>
        /// Creates the asset file in the ProjectSettings directory and loads it.
        /// </summary>
        /// <param name="content">Content to populate the asset file with. If null, the project-wide actions template content will be used.</param>
        static InputActionAsset CreateAndLoadNewAsset(InputActionAsset content = null)
        {
            try
            {
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

                // Write the file to disk.
                content.name = InputSystem.kProjectWideActionsAssetName;
                var assetJson = content.ToJson();
                File.WriteAllText(kAssetPath, assetJson);

                return LoadFromProjectSettings();
            }
            catch (Exception exception)
            {
                Debug.LogError($"InputSystem could not install Project Wide Actions to {kAssetPath}: ({exception})");
                return null;
            }
        }
    }
}
#endif
