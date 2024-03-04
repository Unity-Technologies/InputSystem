#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal class ProjectWideActionsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private Object m_Asset;
        public int callbackOrder => 0;

        // In the editor, we keep track of the appointed project-wide action asset through EditorBuildSettings.
        private const string kEditorBuildSettingsActionsConfigKey = "com.unity.input.settings.actions";

        /// <summary>
        /// Holds an editor build setting for which InputActionAsset to be included as a preloaded asset in
        /// player builds.
        /// </summary>
        internal static InputActionAsset actionsToIncludeInPlayerBuild
        {
            get
            {
                // Attempt to get any persisted configuration
                EditorBuildSettings.TryGetConfigObject(kEditorBuildSettingsActionsConfigKey, out InputActionAsset value);
                return value;
            }
            set
            {
                // Get asset path (note that this will fail if this is an in-memory object)
                var path = AssetDatabase.GetAssetPath(value);
                if (string.IsNullOrEmpty(path))
                {
                    // Remove the object to not keep a broken reference
                    EditorBuildSettings.RemoveConfigObject(kEditorBuildSettingsActionsConfigKey);
                }
                else
                {
                    // Add configuration object as a persisted setting
                    EditorBuildSettings.AddConfigObject(kEditorBuildSettingsActionsConfigKey, value, true);
                }
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Make sure flag is set to indicate project-wide in build
            if (InputSystem.actions != null)
                InputSystem.actions.m_IsProjectWide = true;

            // Add asset
            m_Asset = BuildProviderHelpers.PreProcessSinglePreloadedAsset(InputSystem.actions);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildProviderHelpers.PostProcessSinglePreloadedAsset(ref m_Asset);
        }
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
