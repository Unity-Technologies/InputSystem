#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        internal const string kDefaultAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";
        internal const string kAssetPath = "ProjectSettings/InputManager.asset";
        internal const string kProjectWideActionsAssetName = "ProjectWideInputActions";

        private static InputActionAsset s_Instance;

        /// <summary>
        /// Reference to the current asset representing ProjectWideActions used by both the Editor and Player.
        /// </summary>
        /// <remarks>
        /// Although not technically a Singleton, the InputActionAsset returned by this class is effectively used according
        /// to the Singleton pattern, and therefore, in the interest of tighter cohesion, this property is used by both the
        /// Player and Editor to retrieve or load the Asset.
        /// </remarks>
        public static InputActionAsset instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

#if UNITY_EDITOR
                s_Instance = ProjectWideActionsAsset.GetOrCreate();
#else
                s_Instance = Resources.FindObjectsOfTypeAll<InputActionAsset>().FirstOrDefault(o => o != null && o.name == kProjectWideActionsAssetName);
#endif

                if (s_Instance == null)
                    Debug.LogError($"Couldn't initialize project-wide input actions");
                return s_Instance;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (s_Instance == value)
                    return;

                s_Instance?.Disable();
                s_Instance = value;
                s_Instance.Enable();
            }
        }

        /// <summary>
        /// If necessary, initializes and enables InputActionsAsset for both the Editor and Player.
        /// </summary>
        public static void EnsureInitialized()
        {
            // Touching the Singleton instance will create it if necessary
            instance?.Enable();
        }

#if UNITY_INCLUDE_TESTS
        static string s_DefaultAssetPath = kDefaultAssetPath;
        static string s_AssetPath = kAssetPath;

        internal static void TestHook_SetAssetPaths(string defaultAssetPath, string assetPath)
        {
            s_DefaultAssetPath = defaultAssetPath;
            s_AssetPath = assetPath;
        }

        internal static void TestHook_Reset()
        {
            s_DefaultAssetPath = kDefaultAssetPath;
            s_AssetPath = kAssetPath;
        }

        internal static void TestHook_Disable()
        {
            // Avoid touching the `actions` property directly here, to prevent unwanted initialisation.
            if (s_Instance)
            {
                s_Instance.Disable();
                s_Instance?.OnSetupChanged();  // Cleanup ActionState (remove unused controls after disabling)
                s_Instance = null;
            }
        }

        internal static void TestHook_Enable()
        {
            EnsureInitialized();
            s_Instance.Enable();
        }
#endif // UNITY_INCLUDE_TESTS

        // The remainder of the class functionality is Editor only
#if UNITY_EDITOR
        private static InputActionAsset GetOrCreate()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(s_AssetPath);
            if (objects != null)
            {
                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kProjectWideActionsAssetName) as InputActionAsset;
                if (inputActionsAsset != null)
                    return inputActionsAsset;
            }

            return CreateNewActionAsset();
        }

        internal static InputActionAsset CreateNewActionAsset()
        {
            var json = File.ReadAllText(FileUtil.GetPhysicalPath(s_DefaultAssetPath));

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            asset.name = kProjectWideActionsAssetName;

            AssetDatabase.AddObjectToAsset(asset, s_AssetPath);

            // Make sure all the elements in the asset have GUIDs and that they are indeed unique.
            var maps = asset.actionMaps;
            foreach (var map in maps)
            {
                // Make sure action map has GUID.
                if (string.IsNullOrEmpty(map.m_Id) || asset.actionMaps.Count(x => x.m_Id == map.m_Id) > 1)
                    map.GenerateId();

                // Make sure all actions have GUIDs.
                foreach (var action in map.actions)
                {
                    var actionId = action.m_Id;
                    if (string.IsNullOrEmpty(actionId) || asset.actionMaps.Sum(m => m.actions.Count(a => a.m_Id == actionId)) > 1)
                        action.GenerateId();
                }

                // Make sure all bindings have GUIDs.
                for (var i = 0; i < map.m_Bindings.LengthSafe(); ++i)
                {
                    var bindingId = map.m_Bindings[i].m_Id;
                    if (string.IsNullOrEmpty(bindingId) || asset.actionMaps.Sum(m => m.bindings.Count(b => b.m_Id == bindingId)) > 1)
                        map.m_Bindings[i].GenerateId();
                }
            }

            CreateInputActionReferences(asset);

            AssetDatabase.SaveAssets();

            return asset;
        }

        internal static InputActionMap GetDefaultUIActionMap()
        {
            var json = File.ReadAllText(FileUtil.GetPhysicalPath(s_DefaultAssetPath));
            var actionMaps = InputActionMap.FromJson(json);
            return actionMaps[actionMaps.IndexOf(x => x.name == "UI")];
        }

        private static void CreateInputActionReferences(InputActionAsset asset)
        {
            var maps = asset.actionMaps;
            foreach (var map in maps)
            {
                foreach (var action in map.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(action);
                    AssetDatabase.AddObjectToAsset(actionReference, asset);
                }
            }
        }

#if UNITY_2023_2_OR_NEWER
        /// <summary>
        /// Checks if the default UI action map has been modified or removed, to let the user know if their changes will
        /// break the UI input at runtime, when using the UI Toolkit.
        /// </summary>
        internal static void CheckForDefaultUIActionMapChanges()
        {
            var asset = GetOrCreate();
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
#endif // UNITY_2023_2_OR_NEWER

        /// <summary>
        /// Updates the input action references in the asset by updating names, removing dangling references
        /// and adding new ones.
        /// </summary>
        /// <param name="asset"></param>
        internal static void UpdateInputActionReferences()
        {
            var asset = GetOrCreate();
            var existingReferences = InputActionImporter.LoadInputActionReferencesFromAsset(asset).ToList();

            // Check if referenced input action exists in the asset and remove the reference if it doesn't.
            foreach (var actionReference in existingReferences)
            {
                if (actionReference.action != null && asset.FindAction(actionReference.action.id) == null)
                {
                    actionReference.Set(null);
                    AssetDatabase.RemoveObjectFromAsset(actionReference);
                }
            }

            // Check if all actions have a reference
            foreach (var action in asset)
            {
                // Catch error that's possible to appear in previous versions of the package.
                if (action.actionMap.m_Asset == null)
                    action.actionMap.m_Asset = asset;

                var actionReference = existingReferences.FirstOrDefault(r => r.m_ActionId == action.id.ToString());
                // The input action doesn't have a reference, create a new one.
                if (actionReference == null)
                {
                    var actionReferenceNew = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReferenceNew.Set(action);
                    AssetDatabase.AddObjectToAsset(actionReferenceNew, asset);
                }
                else
                {
                    // Update the name of the reference if it doesn't match the action name.
                    if (actionReference.name != InputActionReference.GetDisplayName(action))
                    {
                        AssetDatabase.RemoveObjectFromAsset(actionReference);
                        actionReference.name = InputActionReference.GetDisplayName(action);
                        AssetDatabase.AddObjectToAsset(actionReference, asset);
                    }
                }
            }
        }
#endif // UNITY_EDITOR
    }
}
#endif // UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
