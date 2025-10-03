using UnityEditor;

namespace Tests.InputSystem.Editor
{
    /// <summary>
    /// Utility to simplify editor tests with respect to editor preferences.
    /// </summary>
    internal static class EditorPrefsTestUtils
    {
        private const string EnterPlayModeOptionsEnabledKey = "EnterPlayModeOptionsEnabled";
        private const string EnterPlayModeOptionsKey = "EnterPlayModeOptions";

        private static bool _savedEnterPlayModeOptionsEnabled;
        private static int _savedEnterPlayModeOptions;

        /// <summary>
        /// Call this from a tests SetUp routine to save editor preferences so they can be restored after the test.
        /// </summary>
        public static void SaveEditorPrefs()
        {
            _savedEnterPlayModeOptionsEnabled = EditorPrefs.GetBool(EnterPlayModeOptionsEnabledKey, false);
            _savedEnterPlayModeOptions = EditorPrefs.GetInt(EnterPlayModeOptionsKey, (int)EnterPlayModeOptions.None);
        }

        /// <summary>
        /// Call this from a tests TearDown routine to restore editor preferences to the state it had before the test.
        /// </summary>
        /// <remarks>Note that if domain reloads have not been disabled and you have a domain reload mid-test,
        /// this utility will fail to restore editor preferences since the saved data will be lost.</remarks>
        public static void RestoreEditorPrefs()
        {
            EditorPrefs.SetBool(EnterPlayModeOptionsEnabledKey, _savedEnterPlayModeOptionsEnabled);
            EditorPrefs.SetInt(EnterPlayModeOptionsKey, _savedEnterPlayModeOptions);
        }

        /// <summary>
        /// Returns whether domain reloads are disabled.
        /// </summary>
        /// <returns>true if domain reloads have been disabled, else false.</returns>
        public static bool IsDomainReloadsDisabled()
        {
            return EditorPrefs.GetBool(EnterPlayModeOptionsEnabledKey, false) &&
                (EditorPrefs.GetInt(EnterPlayModeOptionsKey, (int)EnterPlayModeOptions.None) &
                    (int)EnterPlayModeOptions.DisableDomainReload) != 0;
        }

        /// <summary>
        /// Returns whether scene reloads are disabled.
        /// </summary>
        /// <returns>true if scene reloads have been disabled, else false.</returns>
        public static bool IsSceneReloadsDisabled()
        {
            return EditorPrefs.GetBool(EnterPlayModeOptionsEnabledKey, false) &&
                (EditorPrefs.GetInt(EnterPlayModeOptionsKey, (int)EnterPlayModeOptions.None) &
                    (int)EnterPlayModeOptions.DisableSceneReload) != 0;
        }

        /// <summary>
        /// Call this from within a test to temporarily enable domain reload.
        /// </summary>
        public static void EnableDomainReload()
        {
            EditorPrefs.SetBool(EnterPlayModeOptionsEnabledKey, false);
        }

        /// <summary>
        /// Call this from within a test to temporarily disable domain reload (and scene reloads).
        /// </summary>
        public static void DisableDomainReload()
        {
            EditorPrefs.SetInt(EnterPlayModeOptionsKey, (int)(EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload));
            EditorPrefs.SetBool(EnterPlayModeOptionsEnabledKey, true);
        }
    }
}
