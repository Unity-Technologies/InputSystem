using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.HID;


[InitializeOnLoad]
static class EditorInitialization {
    static EditorInitialization() {
        
        UnityEngine.InputSystem.InputSystem.SetBuildSettingsConfigKey(InputSettingsProvider.kEditorBuildSettingsConfigKey); 

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        UnityEngine.InputSystem.InputSystem.SetActionSetEvent((actionsAsset) => { UnityEngine.InputSystem.Editor.ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = actionsAsset;});
#endif

        UnityEngine.InputSystem.InputSystem.m_InputDebuggerWindowReviveAfterDomainReload = () => InputDebuggerWindow.ReviveAfterDomainReload();

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        UnityEngine.InputSystem.InputSystem.m_ProjectWideActionsBuildProviderActionsToIncludeInPlayerBuild = () => { return UnityEngine.InputSystem.Editor.ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild; };
#endif        
        UnityEngine.InputSystem.InputSystem.m_EditorPlayerSettingHelpersGetNewSystemBackendsEnabled = () => { return EditorPlayerSettingHelpers.newSystemBackendsEnabled; };

        UnityEngine.InputSystem.InputSystem.m_EditorPlayerSettingHelpersSetNewSystemBackendsEnabled = (val) => { EditorPlayerSettingHelpers.newSystemBackendsEnabled  = val; };

        
        InputAnalytics.StartupEventAnalytic.m_EditorPlayerSettingHelpersGetOldSystemBackendsEnabled = () => { return EditorPlayerSettingHelpers.oldSystemBackendsEnabled; };

        UnityEngine.InputSystem.InputSystem.m_EditorHelpersRestartEditorAndRecompileScripts = () => { EditorHelpers.RestartEditorAndRecompileScripts(); };

        UnityEngine.InputSystem.InputSystem.m_InputSettingsProviderForceReload = () => { InputSettingsProvider.ForceReload(); };

        UnityEngine.InputSystem.InputSystem.m_OnDestroyCallback = () => {
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
        };

        HIDSupport.m_InitializeInputDeviceDebuggerWindowOnToolbarGUI = () => { InputDeviceDebuggerWindow.onToolbarGUI += HIDSupportEditor.InputDeviceDebuggerWindowToolbarGUI; };
 
        InputManager.m_InputEditorUserSettingsAddDevicesNotSupportedByProject = () => { return InputEditorUserSettings.addDevicesNotSupportedByProject; };
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        InputManager.m_ProjectWideActionsBuildProviderActionsToIncludeInPlayerBuild = () => { return UnityEngine.InputSystem.Editor.ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild; };
#endif

         UnityEngine.InputSystem.InputSystem.InitializeInEditor(); 

#if UNITY_INPUT_SYSTEM_ENABLE_UI
        
        UnityEngine.InputSystem.UI.InputSystemUIInputModule.m_Reset = (inputModule) => {
            var asset = (InputActionAsset)AssetDatabase.LoadAssetAtPath(
                UnityEditor.InputSystem.Editor.PlayerInputEditor.kDefaultInputActionsAssetPath,
                typeof(InputActionAsset));
            // Setting default asset and actions when creating via inspector
            UnityEngine.InputSystem.UI.Editor.InputSystemUIInputModuleEditor.ReassignActions(inputModule, asset);
        };
#endif
        
    }
}