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
        [Tooltip("The associated input action asset to be serialized to player preferences (Required).")]
        public InputActionAsset actions;

        [Tooltip("The player preference key to be used when serializing binding overrides to player preferences (Required).")]
        public string playerPreferenceKey;

        [Tooltip("Specifies whether to load and apply binding overrides when the component is enabled")]
        public bool loadOnEnable = true;

        [Tooltip("Specifies whether to save binding overrides when the component is disabled")]
        public bool saveOnDisable = true;

        /// <summary>
        /// Loads binding overrides from player preferences and applies them to the associated input action asset.
        /// </summary>
        public void Load()
        {
            if (!IsValidConfiguration())
                return;

            var rebinds = PlayerPrefs.GetString(playerPreferenceKey);
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
            if (loadOnEnable)
                Load();
        }

        private void OnDisable()
        {
            if (saveOnDisable)
                Save();
        }

        private bool IsValidConfiguration()
        {
            if (actions == null)
            {
                Debug.LogWarning("Unable to apply binding overrides from player preferences without an associated action asset.");
                return false;
            }

            if (string.IsNullOrEmpty(playerPreferenceKey))
            {
                Debug.LogWarning("Unable to load binding overrides from player preferences without a non-empty preference key.");
                return false;
            }

            return true;
        }
    }
}
