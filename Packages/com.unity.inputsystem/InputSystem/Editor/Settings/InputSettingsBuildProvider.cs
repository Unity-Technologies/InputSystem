#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    // TODO This class is incorrectly named if not single-purpose for settings, either create a separate one for project-wide actions or rename this and relocate it
    internal class InputSettingsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        Object[] m_OriginalPreloadedAssets;
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_OriginalPreloadedAssets = PlayerSettings.GetPreloadedAssets();
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            Debug.Assert(!ReferenceEquals(m_OriginalPreloadedAssets, preloadedAssets));

            var oldSize = preloadedAssets.Length;
            var newSize = oldSize;

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            // Determine if we need to preload project-wide InputActionsAsset.
            // Also make sure we set project wide flag on object that gets serialized into build.
            // It will allow us to find the right object if multiple preloaded assets
            // Input System asset + user assets.
            var actions = InputSystem.actions;
            var actionsMissing = NeedsToBeAdded(preloadedAssets, actions, ref newSize);
#endif

            // Determine if we need to preload InputSettings asset.
            var settings = InputSystem.settings;
            var settingsMissing = NeedsToBeAdded(preloadedAssets, settings, ref newSize);

            // Return immediately if all assets are already present
            if (newSize == oldSize)
                return;

            // Modify array so allocation only happens once
            Array.Resize(ref preloadedAssets, newSize);
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (actionsMissing)
                ArrayHelpers.Append(ref preloadedAssets, actions);
#endif
            if (settingsMissing)
                ArrayHelpers.Append(ref preloadedAssets, settings);

            // Update preloaded assets (once)
            PlayerSettings.SetPreloadedAssets(preloadedAssets);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Revert back to original state
            PlayerSettings.SetPreloadedAssets(m_OriginalPreloadedAssets);
            m_OriginalPreloadedAssets = null;
        }

        private static bool NeedsToBeAdded(Object[] preloadedAssets, Object asset, ref int extraCapacity)
        {
            var isMissing = (asset != null) && !preloadedAssets.Contains(asset);
            if (isMissing)
                ++extraCapacity;
            return isMissing;
        }
    }
}
#endif // UNITY_EDITOR
