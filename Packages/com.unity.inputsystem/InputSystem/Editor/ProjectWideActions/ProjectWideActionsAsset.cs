#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    // Placeholder for converting InputManager.asset actions into regular asset to support conversion from 1.8.0-pre1 and 1.8.0-pre2 to asset based Project-wide actions

    internal static class ProjectWideActionsAsset
    {
        private const string kDefaultAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string kDefaultTemplateAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";

        internal static class ProjectSettingsProjectWideActionsAssetConverter
        {
            internal const string kAssetPath = "ProjectSettings/InputManager.asset";
            internal const string kAssetName = InputSystem.kProjectWideActionsAssetName;

            // DONE 1. Implement reading the kAssetPath into InputActionAsset.
            // DONE 2. Serialize as JSON and write as an .inputactions file into Asset directory.
            // TODO Consider preserving GUIDs to potentially enable references to stay intact.
            // TODO 3. Let InputActionImporter do its job on importing and configuring the asset.
            // TODO 4. Assign to InputSystem.actions

            class ProjectSettingsPostprocessor : AssetPostprocessor
            {
                static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
                {
                    MoveInputManagerAssetActionsToProjectWideInputActionAsset();
                }
            }

            internal static void MoveInputManagerAssetActionsToProjectWideInputActionAsset()
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(kAssetPath);
                if (objects != null)
                {
                    var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kAssetName) as InputActionAsset;
                    if (inputActionsAsset != null)
                    {
                        var json = JsonUtility.ToJson(inputActionsAsset, prettyPrint: true);
                        File.WriteAllText(ProjectWideActionsAsset.kDefaultAssetPath, json);
                    }
                }

                DeleteActionAssetAndActionReferences();
            }

            internal static void DeleteActionAssetAndActionReferences()
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(kAssetPath);
                if (objects == null)
                    return;

                // Handle deleting all InputActionAssets as older 1.8.0 pre release could create more than one project wide input asset in the file
                foreach (var obj in objects)
                {
                    if (obj is InputActionReference)
                    {
                        var actionReference = obj as InputActionReference;
                        AssetDatabase.RemoveObjectFromAsset(obj);
                        Object.DestroyImmediate(actionReference);
                    }
                    else if (obj is InputActionAsset)
                    {
                        AssetDatabase.RemoveObjectFromAsset(obj);
                    }
                }
            }
        }

        // Returns the default asset path for where to create project-wide actions asset.
        internal static string defaultAssetPath => kDefaultAssetPath;

        // Returns the default template JSON content.
        internal static string GetDefaultAssetJson()
        {
            return EditorHelpers.ReadAllText(kDefaultTemplateAssetPath);
        }

        // Creates an asset at the given path containing the given JSON content.
        private static InputActionAsset CreateAssetAtPathFromJson(string assetPath, string json)
        {
            // Note that the extra work here is to override the JSON name from the source asset
            var inputActionAsset = InputActionAsset.FromJson(json);
            inputActionAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            InputActionAssetManager.SaveAsset(assetPath, inputActionAsset.ToJson());
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }

        // Creates an asset at the given path containing the default template JSON.
        internal static InputActionAsset CreateDefaultAssetAtPath(string assetPath = kDefaultAssetPath)
        {
            return CreateAssetAtPathFromJson(assetPath, EditorHelpers.ReadAllText(kDefaultTemplateAssetPath));
        }

        // Returns the default UI action map as represented by the default template JSON.
        internal static InputActionMap GetDefaultUIActionMap()
        {
            var actionMaps = InputActionMap.FromJson(GetDefaultAssetJson());
            return actionMaps[actionMaps.IndexOf(x => x.name == "UI")];
        }

#if UNITY_2023_2_OR_NEWER
        /// <summary>
        /// Checks if the default UI action map has been modified or removed, to let the user know if their changes will
        /// break the UI input at runtime, when using the UI Toolkit.
        /// </summary>
        internal static void CheckForDefaultUIActionMapChanges(InputActionAsset asset)
        {
            if (asset == null)
                return;

            var defaultUIActionMap = GetDefaultUIActionMap();
            var uiMapIndex = asset.actionMaps.IndexOf(x => x.name == "UI");

            // "UI" action map has been removed or renamed.
            if (uiMapIndex == -1)
            {
                Debug.LogWarning("The action map named 'UI' does not exist.\r\n " +
                    "This will break the UI input at runtime. Please revert the changes to have an action map named 'UI'.");
                return;
            }
            var uiMap = asset.m_ActionMaps[uiMapIndex];
            foreach (var action in defaultUIActionMap.actions)
            {
                // "UI" actions have been modified.
                if (uiMap.FindAction(action.name) == null)
                {
                    Debug.LogWarning($"The UI action '{action.name}' name has been modified.\r\n" +
                        $"This will break the UI input at runtime. Please make sure the action name with '{action.name}' exists.");
                }

                // TODO Check expected action type etc. this is currently missing
            }
        }

#endif // UNITY_2023_2_OR_NEWER
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
