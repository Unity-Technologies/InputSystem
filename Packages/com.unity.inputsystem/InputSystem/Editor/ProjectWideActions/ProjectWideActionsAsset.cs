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
        internal const string kAssetPath = "ProjectSettings/InputManager.asset";
        internal const string kAssetName = "ProjectWideInputActions";

        static string s_DefaultAssetPath = kDefaultAssetPath;
        static string s_AssetPath = kAssetPath;

        // For Testing
        internal static void SetAssetPaths(string defaultAssetPath, string assetPath)
        {
            s_DefaultAssetPath = defaultAssetPath;
            s_AssetPath = assetPath;
        }

        [InitializeOnLoadMethod]
        internal static void InstallProjectWideActions()
        {
            GetOrCreate();
        }

        internal static InputActionAsset GetOrCreate()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(s_AssetPath);
            if (objects != null)
            {
                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kAssetName) as InputActionAsset;
                if (inputActionsAsset != null)
                    return inputActionsAsset;
            }

            return CreateNewActionAsset();
        }

        private static InputActionAsset CreateNewActionAsset()
        {
            var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, s_DefaultAssetPath));

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            asset.name = kAssetName;

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

            return asset;
        }
    }
}
#endif
