#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class DirtyAssetTracker
    {
        /// <summary>
        /// Keep track of InputActionAsset assets that you want to re-load. This is useful because some user actions,
        /// such as adding a new input binding at runtime, change the in-memory representation of the input action asset and
        /// those changes survive when exiting Play mode. If you re-open an Input Action Asset in the Editor that has been changed
        /// this way, you see the new bindings that have been added during Play mode which you might not typically want to happen.
        ///
        /// You can avoid this by force re-loading from disk any asset that has been marked as dirty.
        /// </summary>
        /// <param name="asset"></param>
        public static void TrackDirtyInputActionAsset(InputActionAsset asset)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long _) == false)
                return;

            s_TrackedDirtyAssets.Add(assetGuid);
        }

        public static void ReloadDirtyAssets()
        {
            foreach (var assetGuid in s_TrackedDirtyAssets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (string.IsNullOrEmpty(assetPath))
                    continue;

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            s_TrackedDirtyAssets.Clear();
        }

        private static HashSet<string> s_TrackedDirtyAssets = new HashSet<string>();
    }
}
#endif