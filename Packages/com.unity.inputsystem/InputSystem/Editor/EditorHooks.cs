#if UNITY_EDITOR
using System;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorHooks
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS 
        //Inverse of control to allow the Editor to register a way to get/set the project wide actions
        internal static Action<InputActionAsset> m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildSetter;
        internal static Func<InputActionAsset> m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildGetter;
        internal static InputActionAsset ActionsToIncludeInPlayerBuild
        {
            get => m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildGetter.Invoke();
            set => m_ProjectWideActionsBuildProvideractionsToIncludeInPlayerBuildSetter.Invoke(value);
        }
#endif
        
        internal static Func<string> m_SettingsProviderConfigKeyGetter;
        internal static string SettingsProviderConfigKey => m_SettingsProviderConfigKeyGetter.Invoke();

        internal static Action m_InputSettingsProviderForceReload;
        public static void InputSettingsProviderForceReload()
        {
            m_InputSettingsProviderForceReload.Invoke();
        }

        internal static Func<string> m_PlayerInputEditorDefaultInputActionsAssetPath;
        internal static string DefaultInputActionsAssetPath => m_PlayerInputEditorDefaultInputActionsAssetPath.Invoke();

    }
    

}
#endif