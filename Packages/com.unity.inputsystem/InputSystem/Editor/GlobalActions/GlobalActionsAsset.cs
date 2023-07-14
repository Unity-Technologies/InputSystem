// Global Actions cannot be edited without the new UITK based editor
// which only works for Unity 2020.2+
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_ENABLE_GLOBAL_ACTIONS_API

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class GlobalActionsAsset
    {
        internal const string kDefaultGlobalActionsPath = "Packages/com.unity.inputsystem/InputSystem/API/GlobalInputActions.inputactions";
        internal const string kGlobalActionsAssetPath = "ProjectSettings/InputManager.asset";

        [InitializeOnLoadMethod]
        internal static void InstallGlobalActions()
        {
            GetOrCreateGlobalActionsAsset();
        }

        internal static InputActionAsset GetOrCreateGlobalActionsAsset(string assetPath = kGlobalActionsAssetPath,
            string templateAssetPath = kDefaultGlobalActionsPath)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (objects == null)
                throw new InvalidOperationException("Couldn't load global input system actions because the InputManager.asset file is missing or corrupt.");

            var globalInputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == Input.kGlobalActionsAssetName) as InputActionAsset;
            if (globalInputActionsAsset != null)
                return globalInputActionsAsset;

            return CreateNewGlobalActionAsset(templateAssetPath, assetPath);
        }

        private static InputActionAsset CreateNewGlobalActionAsset(string templateAssetPath, string assetPath)
        {
            var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, templateAssetPath));

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            asset.name = Input.kGlobalActionsAssetName;

            AssetDatabase.AddObjectToAsset(asset, assetPath);

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

            // Create sub-asset for each action. This is so that users can select individual input actions from the asset when they're
            // trying to assign to a field that accepts only one action.
            foreach (var map in maps)
            {
                foreach (var action in map.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(action);
                    AssetDatabase.AddObjectToAsset(actionReference, asset);
                }
            }

            AssetDatabase.SaveAssets();

            // output the temp file for source generators to pick up
            var libraryPath = Path.Combine(Application.dataPath, "..", "Library", "com.unity.inputsystem");
            if (Directory.Exists(libraryPath) == false)
                Directory.CreateDirectory(libraryPath);

            File.WriteAllText(Path.Combine(libraryPath, "GlobalActionsAsset.json"), asset.ToJson());

            return asset;
        }
    }
}
#endif
