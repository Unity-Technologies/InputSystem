#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class BuildProviderHelpers
    {
        // Adds the given object to the list of preloaded asset if not already present and
        // returns the argument given if the object was added to the list or null if already present.
        public static Object PreProcessSinglePreloadedAsset(Object assetToPreload)
        {
            // Avoid including any null asset
            if (assetToPreload == null)
                return null;

            // If we operate on temporary object instead of a properly persisted asset, adding that temporary asset
            // would result in preloadedAssets containing null object "{fileID: 0}". Hence we ignore these.
            if (EditorUtility.IsPersistent(assetToPreload))
            {
                // Add asset object, if it's not in there already.
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

        // Removes the given object from preloaded assets if present.
        // The object passed as argument if set to null by this function regardless if existing in preloaded
        // assets or not.
        public static void PostProcessSinglePreloadedAsset(ref Object assetAddedByThisProvider)
        {
            if (assetAddedByThisProvider == null)
                return;

            // Revert back to original state by removing all object(s) from preloaded assets that was added by this processor.
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
