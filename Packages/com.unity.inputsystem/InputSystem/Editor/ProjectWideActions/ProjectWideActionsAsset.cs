#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    // Placeholder for converting InputManager.asset actions into regular asset to support conversion from 1.8.0-pre1 and 1.8.0-pre2 to asset based Project-wide actions
    // TODO Currently not used
    internal static class ProjectSettingsProjectWideActionsAssetConverter
    {
        internal const string kAssetPath = "ProjectSettings/InputManager.asset";

        // TODO 1. Implement reading the kAssetPath into InputActionAsset.
        // TODO 2. Serialize as JSON and write as an .inputactions file into Asset directory.
        // TODO Consider preserving GUIDs to potentially enable references to stay intact.
        // TODO 3. Let InputActionImporter do its job on importing and configuring the asset.
        // TODO 4. Assign to InputSystem.actions

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

    internal static class ProjectWideActionsAsset
    {
        internal const string kDefaultAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";

        internal static void CreateNewAsset(string relativePath, string sourcePath)
        {
            // Note that we only copy file here and let the InputActionImporter handle the asset management
            File.Copy(FileUtil.GetPhysicalPath(sourcePath), FileUtil.GetPhysicalPath(relativePath), overwrite: true);
        }

        internal static void CreateNewAsset(string relativePath)
        {
            CreateNewAsset(relativePath, kDefaultAssetPath);
        }

        internal static InputActionMap GetDefaultUIActionMap()
        {
            var json = File.ReadAllText(FileUtil.GetPhysicalPath(kDefaultAssetPath));
            var actionMaps = InputActionMap.FromJson(json);
            return actionMaps[actionMaps.IndexOf(x => x.name == "UI")];
        }

#if UNITY_2023_2_OR_NEWER
        /// <summary>
        /// Checks if the default UI action map has been modified or removed, to let the user know if their changes will
        /// break the UI input at runtime, when using the UI Toolkit.
        /// </summary>
        internal static void CheckForDefaultUIActionMapChanges(InputActionAsset asset)
        {
            if (asset != null)
            {
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
                }
            }
        }

#endif
        /// <summary>
        /// Reset the given asset to Project-wide Input Action asset defaults
        /// </summary>
        internal static void ResetActionAsset(InputActionAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            // TODO Overwrite and let importer handle it?
        }
    }
}
#endif
