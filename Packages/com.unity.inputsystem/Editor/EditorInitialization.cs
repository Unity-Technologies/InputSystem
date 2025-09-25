using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [InitializeOnLoad]
    class EditorInitialization
    {
        static EditorInitialization() 
        {
            EditorHooks.m_SettingsProviderConfigKeyGetter = () => InputSettingsProvider.kEditorBuildSettingsConfigKey;

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            EditorHooks.m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildSetter = (value) =>
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = value;
            EditorHooks.m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildGetter = () =>
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
#endif
            
            EditorHooks.m_InputSettingsProviderForceReload = InputSettingsProvider.ForceReload;

            EditorHooks.m_PlayerInputEditorDefaultInputActionsAssetPath = () =>
                UnityEditor.InputSystem.Editor.PlayerInputEditor.kDefaultInputActionsAssetPath;

            InputSystem.InitializeInEditor(); 
        }
    }
}