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
        InputActionAsset m_ProjectWideActions;
        Object[] m_OriginalPreloadedAssets;
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_OriginalPreloadedAssets = PlayerSettings.GetPreloadedAssets();
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsActionsConfigKey,
                out m_ProjectWideActions);

            if (m_ProjectWideActions != null)
            {
                if (!preloadedAssets.Contains(InputSystem.actions))
                {
                    ArrayHelpers.Append(ref preloadedAssets, InputSystem.actions);
                    PlayerSettings.SetPreloadedAssets(preloadedAssets);
                }
            }
#endif
            if (InputSystem.settings == null)
                return;

            if (!preloadedAssets.Contains(InputSystem.settings))
            {
                ArrayHelpers.Append(ref preloadedAssets, InputSystem.settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Revert back to original state
            PlayerSettings.SetPreloadedAssets(m_OriginalPreloadedAssets);
        }
    }
}
#endif // UNITY_EDITOR
