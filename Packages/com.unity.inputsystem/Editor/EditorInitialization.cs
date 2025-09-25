using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [InitializeOnLoad]
    class EditorInitialization
    {
        static EditorInitialization() 
        {
            Debug.Log("Input System: Initializing Editor Hooks");
            EditorHooks.m_SettingsProviderConfigKeyGetter = () => InputSettingsProvider.kEditorBuildSettingsConfigKey;

            EditorHooks.m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildSetter = (value) =>
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = value;
            EditorHooks.m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildGetter = () =>
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
            
            EditorHooks.m_InputSettingsProviderForceReload = InputSettingsProvider.ForceReload;

            EditorHooks.m_PlayerInputEditorDefaultInputActionsAssetPath = () =>
                UnityEditor.InputSystem.Editor.PlayerInputEditor.kDefaultInputActionsAssetPath;

            InputSystem.InitializeInEditor(); 
        }
    }
}