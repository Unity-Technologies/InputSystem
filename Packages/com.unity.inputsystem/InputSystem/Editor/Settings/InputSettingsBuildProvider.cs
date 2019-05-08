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

            if (!m_OriginalPreloadedAssets.Contains(InputSystem.settings))
            {
                var preloadedAssets = m_OriginalPreloadedAssets.ToList();
                preloadedAssets.Add(InputSystem.settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (InputSystem.settings == null)
                return;

            // Revert back to original state
            PlayerSettings.SetPreloadedAssets(m_OriginalPreloadedAssets);
        }
    }
}
#endif // UNITY_EDITOR
