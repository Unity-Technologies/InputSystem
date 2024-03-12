#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private Object m_Asset;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_Asset = BuildProviderHelpers.PreProcessSinglePreloadedAsset(InputSystem.settings);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildProviderHelpers.PostProcessSinglePreloadedAsset(ref m_Asset);
        }
    }
}
#endif // UNITY_EDITOR
