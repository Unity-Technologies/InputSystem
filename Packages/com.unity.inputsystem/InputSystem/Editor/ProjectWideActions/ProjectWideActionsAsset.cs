#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        private const string kDefaultAssetName = "InputSystem_Actions";
        private const string kDefaultAssetPath = "Assets/" + kDefaultAssetName + ".inputactions";
        private const string kDefaultTemplateAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";

        internal static class ProjectSettingsProjectWideActionsAssetConverter
        {
            private const string kAssetPathInputManager = "ProjectSettings/InputManager.asset";
            private const string kAssetNameProjectWideInputActions = "ProjectWideInputActions";

            class ProjectSettingsPostprocessor : AssetPostprocessor
            {
                private static bool migratedInputActionAssets = false;

#if UNITY_2021_2_OR_NEWER
                private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
                private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#endif
                {
                    if (!migratedInputActionAssets)
                    {
                        MoveInputManagerAssetActionsToProjectWideInputActionAsset();
                        migratedInputActionAssets = true;
                    }
                }
            }

            private static void MoveInputManagerAssetActionsToProjectWideInputActionAsset()
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(EditorHelpers.GetPhysicalPath(kAssetPathInputManager));
                if (objects == null)
                    return;

                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kAssetNameProjectWideInputActions) as InputActionAsset;
                if (inputActionsAsset != default)
                {
                    // Found some actions in the InputManager.asset file
                    //
                    string path = ProjectWideActionsAsset.kDefaultAssetPath;

                    if (File.Exists(EditorHelpers.GetPhysicalPath(path)))
                    {
                        // We already have a path containing inputactions, find a new unique filename
                        //
                        //  eg  Assets/InputSystem_Actions.inputactions ->
                        //      Assets/InputSystem_Actions (1).inputactions ->
                        //      Assets/InputSystem_Actions (2).inputactions ...
                        //
                        string[] files = Directory.GetFiles("Assets", "*.inputactions");
                        List<string> names = new List<string>();
                        for (int i = 0; i < files.Length; i++)
                        {
                            names.Add(System.IO.Path.GetFileNameWithoutExtension(files[i]));
                        }
                        string unique = ObjectNames.GetUniqueName(names.ToArray(), kDefaultAssetName);
                        path = "Assets/" + unique + ".inputactions";
                    }

                    var json = inputActionsAsset.ToJson();
                    InputActionAssetManager.SaveAsset(EditorHelpers.GetPhysicalPath(path), json);

                    Debug.Log($"Migrated Project-wide Input Actions from '{kAssetPathInputManager}' to '{path}' asset");

                    // Update current project-wide settings if needed (don't replace if already set to something else)
                    //
                    if (InputSystem.actions == null || InputSystem.actions.name == kAssetNameProjectWideInputActions)
                    {
                        InputSystem.actions = (InputActionAsset)AssetDatabase.LoadAssetAtPath(path, typeof(InputActionAsset));
                        Debug.Log($"Loaded Project-wide Input Actions from '{path}' asset");
                    }
                }

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

                AssetDatabase.SaveAssets();
            }
        }

        // Returns the default asset path for where to create project-wide actions asset.
        internal static string defaultAssetPath => kDefaultAssetPath;

        // Returns the default template JSON content.
        internal static string GetDefaultAssetJson()
        {
            return File.ReadAllText(EditorHelpers.GetPhysicalPath(kDefaultTemplateAssetPath));
        }

        // Creates an asset at the given path containing the default template JSON.
        internal static InputActionAsset CreateDefaultAssetAtPath(string assetPath = kDefaultAssetPath)
        {
            return CreateAssetAtPathFromJson(assetPath, File.ReadAllText(EditorHelpers.GetPhysicalPath(kDefaultTemplateAssetPath)));
        }

        // These may be moved out to internal types if decided to extend validation at a later point.

        internal interface IReportInputActionAssetValidationErrors
        {
            bool OnValidationError(InputAction action, string message);
        }

        private class DefaultInputActionAssetValidationReporter : IReportInputActionAssetValidationErrors
        {
            public bool OnValidationError(InputAction action, string message)
            {
                Debug.LogWarning(message);
                return true;
            }
        }

        internal static bool Validate(InputActionAsset asset, IReportInputActionAssetValidationErrors reporter = null)
        {
#if UNITY_2023_2_OR_NEWER
            reporter ??= new DefaultInputActionAssetValidationReporter();
            CheckForDefaultUIActionMapChanges(asset, reporter);
#endif // UNITY_2023_2_OR_NEWER
            return true;
        }

        private static bool ReportError(IReportInputActionAssetValidationErrors reporter, InputAction action, string message)
        {
            return reporter.OnValidationError(action, message);
        }

#if UNITY_2023_2_OR_NEWER
        /// <summary>
        /// Checks if the default InputForUI UI action map has been modified or removed, to let the user know if their changes will
        /// break the UI input at runtime, when using the UI Toolkit.
        /// </summary>
        internal static bool CheckForDefaultUIActionMapChanges(InputActionAsset asset, IReportInputActionAssetValidationErrors reporter = null)
        {
            reporter ??= new DefaultInputActionAssetValidationReporter();

            var defaultUIActionMap = GetDefaultUIActionMap();
            var uiMapIndex = asset.actionMaps.IndexOf(x => x.name == "UI");

            // "UI" action map has been removed or renamed.
            if (uiMapIndex == -1)
            {
                ReportError(reporter, null,
                    "The action map named 'UI' does not exist.\r\n " +
                    "This will break the UI input at runtime. Please revert the changes to have an action map named 'UI'.");
                return false;
            }
            var uiMap = asset.m_ActionMaps[uiMapIndex];
            foreach (var action in defaultUIActionMap.actions)
            {
                // "UI" actions have been modified.
                if (uiMap.FindAction(action.name) == null)
                {
                    var abort = !ReportError(reporter, action,
                        $"The UI action '{action.name}' name has been modified.\r\n" +
                        $"This will break the UI input at runtime. Please make sure the action name with '{action.name}' exists.");
                    if (abort)
                        return false;
                }

                // TODO Add additional validation here, e.g. check expected action type etc. this is currently missing.
            }

            return true;
        }

#endif // UNITY_2023_2_OR_NEWER

        // Returns the default UI action map as represented by the default template JSON.
        private static InputActionMap GetDefaultUIActionMap()
        {
            var actionMaps = InputActionMap.FromJson(GetDefaultAssetJson());
            return actionMaps[actionMaps.IndexOf(x => x.name == "UI")];
        }

        // Creates an asset at the given path containing the given JSON content.
        private static InputActionAsset CreateAssetAtPathFromJson(string assetPath, string json)
        {
            // Note that the extra work here is to override the JSON name from the source asset
            var inputActionAsset = InputActionAsset.FromJson(json);
            inputActionAsset.name = InputActionImporter.NameFromAssetPath(assetPath);
            InputActionAssetManager.SaveAsset(assetPath, inputActionAsset.ToJson());
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
