namespace UnityEngine.InputSystem
{
    internal static class InputFeatureNames
    {
        public const string kRunPlayerUpdatesInEditMode = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
        public const string kDisableUnityRemoteSupport = "DISABLE_UNITY_REMOTE_SUPPORT";
        public const string kUseWindowsGamingInputBackend = "USE_WINDOWS_GAMING_INPUT_BACKEND";
        public const string kUseOptimizedControls = "USE_OPTIMIZED_CONTROLS";
        public const string kUseReadValueCaching = "USE_READ_VALUE_CACHING";
        public const string kParanoidReadValueCachingChecks = "PARANOID_READ_VALUE_CACHING_CHECKS";

#if UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
        public const string kUseUIToolkitEditor = "USE_UITOOLKIT_EDITOR";
#endif
    }
}
