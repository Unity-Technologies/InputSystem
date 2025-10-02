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
        public static void RestoreEditorPrefs()
        {
            EditorPrefs.SetBool(EnterPlayModeOptionsEnabledKey, _savedEnterPlayModeOptionsEnabled);
            EditorPrefs.SetInt(EnterPlayModeOptionsKey, _savedEnterPlayModeOptions);
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
            EditorPrefs.SetBool(EnterPlayModeOptionsEnabledKey, true);
            EditorPrefs.SetInt(EnterPlayModeOptionsKey, (int)(EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload));
        }
    }
}
