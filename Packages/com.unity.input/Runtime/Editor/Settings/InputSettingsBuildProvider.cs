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
        private InputSettings m_SettingsAddedToPreloadedAssets;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (InputSystem.settings == null)
                return;

            var wasDirty = IsPlayerSettingsDirty();
            m_SettingsAddedToPreloadedAssets = null;

            // Add InputSettings object assets, if it's not in there already.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (!preloadedAssets.Contains(InputSystem.settings))
            {
                m_SettingsAddedToPreloadedAssets = InputSystem.settings;
                ArrayHelpers.Append(ref preloadedAssets, m_SettingsAddedToPreloadedAssets);
                PlayerSettings.SetPreloadedAssets(preloadedAssets);
            }

            if (!wasDirty)
                ClearPlayerSettingsDirtyFlag();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (m_SettingsAddedToPreloadedAssets == null)
                return;

            var wasDirty = IsPlayerSettingsDirty();

            // Revert back to original state.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            var index = preloadedAssets.IndexOfReference<Object, Object>(m_SettingsAddedToPreloadedAssets);
            if (index != -1)
            {
                ArrayHelpers.EraseAt(ref preloadedAssets, index);
                PlayerSettings.SetPreloadedAssets(preloadedAssets);
            }
            m_SettingsAddedToPreloadedAssets = null;

            if (!wasDirty)
                ClearPlayerSettingsDirtyFlag();
        }

        private static bool IsPlayerSettingsDirty()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                return EditorUtility.IsDirty(settings[0]);
            return false;
        }

        private static void ClearPlayerSettingsDirtyFlag()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                EditorUtility.ClearDirty(settings[0]);
        }
    }
}
#endif // UNITY_EDITOR
