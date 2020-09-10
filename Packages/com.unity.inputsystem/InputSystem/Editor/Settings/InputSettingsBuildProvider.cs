#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private Object[] m_OriginalPreloadedAssets;

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (InputSystem.settings == null)
                return;

            m_OriginalPreloadedAssets = PlayerSettings.GetPreloadedAssets();
            bool wasDirty = IsPlayerSettingsDirty();

            if (!m_OriginalPreloadedAssets.Contains(InputSystem.settings))
            {
                var preloadedAssets = m_OriginalPreloadedAssets.ToList();
                preloadedAssets.Add(InputSystem.settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
            }

            if (!wasDirty)
                ClearPlayerSettingsDirtyFlag();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (InputSystem.settings == null)
                return;

            bool wasDirty = IsPlayerSettingsDirty();

            // Revert back to original state
            PlayerSettings.SetPreloadedAssets(m_OriginalPreloadedAssets);

            if (!wasDirty)
                ClearPlayerSettingsDirtyFlag();
        }

        static bool IsPlayerSettingsDirty()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                return EditorUtility.IsDirty(settings[0]);
            return false;
        }

        static void ClearPlayerSettingsDirtyFlag()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                EditorUtility.ClearDirty(settings[0]);
        }
    }
}
#endif // UNITY_EDITOR
