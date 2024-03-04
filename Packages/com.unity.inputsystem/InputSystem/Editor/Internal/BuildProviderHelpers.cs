#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class BuildProviderHelpers
    {
        public static Object PreProcessSinglePreloadedAsset(Object assetToPreload)
        {
            // Precondition
            Debug.Assert(assetToPreload == null);

            // If we operate on temporary object instead of a properly persisted asset, adding that temporary asset
            // would result in preloadedAssets containing null object "{fileID: 0}". Hence we ignore these.
            if (EditorUtility.IsPersistent(assetToPreload))
            {
                // Add InputSettings object assets, if it's not in there already.
                var preloadedAssets = PlayerSettings.GetPreloadedAssets();
                if (preloadedAssets != null && preloadedAssets.IndexOf(assetToPreload) == -1)
                {
                    ArrayHelpers.Append(ref preloadedAssets, assetToPreload);
                    PlayerSettings.SetPreloadedAssets(preloadedAssets);
                    return assetToPreload;
                }
            }

            return null;
        }

        public static void PostProcessSinglePreloadedAsset(ref Object assetAddedByThisProvider)
        {
            if (assetAddedByThisProvider == null)
                return;

            // Revert back to original state by removing all input settings from preloaded assets that was added by this processor.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            while (preloadedAssets != null && preloadedAssets.Length > 0)
            {
                var index = Array.IndexOf(preloadedAssets, assetAddedByThisProvider);
                if (index != -1)
                {
                    ArrayHelpers.EraseAt(ref preloadedAssets, index);
                    PlayerSettings.SetPreloadedAssets(preloadedAssets);
                    break;
                }
            }

            assetAddedByThisProvider = null;
        }
    }
}

#endif // UNITY_EDITOR
