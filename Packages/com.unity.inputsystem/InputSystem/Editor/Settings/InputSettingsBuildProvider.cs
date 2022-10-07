#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (InputSystem.settings == null)
                return;

            // If we operate on temporary object instead of input setting asset,
            // adding temporary asset would result in preloadedAssets containing null object "{fileID: 0}".
            // Hence we ignore adding temporary objects to preloaded assets.
            if (!EditorUtility.IsPersistent(InputSystem.settings))
                return;

            // Add InputSettings object assets, if it's not in there already.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (!preloadedAssets.Contains(InputSystem.settings))
            {
                ArrayHelpers.Append(ref preloadedAssets, InputSystem.settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Revert back to original state by removing all input settings from preloaded assets.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            while (preloadedAssets != null && preloadedAssets.Length > 0)
            {
                var index = preloadedAssets.IndexOf(x => x is InputSettings);
                if (index != -1)
                {
                    ArrayHelpers.EraseAt(ref preloadedAssets, index);
                    PlayerSettings.SetPreloadedAssets(preloadedAssets);
                }
                else
                    break;
            }
        }
    }
}
#endif // UNITY_EDITOR
