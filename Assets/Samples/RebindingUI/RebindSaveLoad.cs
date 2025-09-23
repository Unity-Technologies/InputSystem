using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Handles persisting binding overrides which implies that customizations of controls will be persisted
    /// between runs.
    /// </summary>
    public class RebindSaveLoad : MonoBehaviour
    {
        /// <summary>
        /// The associated input action asset (Required).
        /// </summary>
        [Tooltip("The associated input action asset to be serialized to player preferences (Required).")]
        public InputActionAsset actions;

        /// <summary>
        /// The associated player preference key.
        /// </summary>
        [Tooltip("The player preference key to be used when serializing binding overrides to player preferences (Required).")]
        public string playerPreferenceKey;

        /// <summary>
        /// Loads binding overrides from player preferences and applies them to the associated input action asset.
        /// </summary>
        public void Load()
        {
            if (!IsValidConfiguration())
                return;

            var rebinds = PlayerPrefs.GetString("rebinds");
            if (string.IsNullOrEmpty(rebinds))
                return; // OK, we may not have saved any binding overrides yet.

            actions.LoadBindingOverridesFromJson(rebinds);
        }

        /// <summary>
        /// Saves binding overrides from the associated input action asset and persists them to player preferences.
        /// </summary>
        public void Save()
        {
            if (!IsValidConfiguration())
                return;

            var rebinds = actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(playerPreferenceKey, rebinds);
        }

        private void OnEnable()
        {
            Load();
        }

        private void OnDisable()
        {
            Save();
        }

        private bool IsValidConfiguration()
        {
            if (actions == null)
            {
                Debug.LogWarning("Unable to apply binding overrides from player preferences without an associated " +
                    "action asset.");
                return false;
            }

            if (string.IsNullOrEmpty(playerPreferenceKey))
            {
                Debug.LogWarning("Unable to load binding overrides from player preferences without a key");
                return false;
            }

            return true;
        }
    }
}
