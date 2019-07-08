using UnityEngine.InputSystem.Utilities;

////TODO: gesture support
////TODO: mouse/touch simulation support
////TODO: high-frequency touch support

////REVIEW: have TouchTap, TouchSwipe, etc. wrapper MonoBehaviours like LeanTouch?

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    /// API to control enhanced touch facilities like <see cref="Touch"/> that are not
    /// enabled by default.
    /// </summary>
    public static class EnhancedTouchSupport
    {
        public static bool enabled => s_Enabled > 0;

        private static int s_Enabled;
        private static InputSettings.UpdateMode s_UpdateMode;

        public static void Enable()
        {
            ++s_Enabled;
            if (s_Enabled > 1)
                return;

            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onBeforeUpdate += Touch.BeginUpdate;
            InputSystem.onSettingsChange += OnSettingsChange;

            SetUpState();
        }

        public static void Disable()
        {
            if (!enabled)
                return;
            --s_Enabled;
            if (s_Enabled > 0)
                return;

            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onBeforeUpdate -= Touch.BeginUpdate;
            InputSystem.onSettingsChange -= OnSettingsChange;

            TearDownState();
        }

        private static void SetUpState()
        {
            Touch.s_PlayerState.updateMask = InputUpdateType.Dynamic | InputUpdateType.Manual;
            #if UNITY_EDITOR
            Touch.s_EditorState.updateMask = InputUpdateType.Editor;
            #endif

            s_UpdateMode = InputSystem.settings.updateMode;

            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Added);
        }

        private static void TearDownState()
        {
            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Removed);

            Touch.s_PlayerState.Destroy();
            #if UNITY_EDITOR
            Touch.s_EditorState.Destroy();
            #endif

            Touch.s_PlayerState = default;
            #if UNITY_EDITOR
            Touch.s_EditorState = default;
            #endif
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                {
                    if (device is Touchscreen touchscreen)
                        Touch.AddTouchscreen(touchscreen);
                    break;
                }

                case InputDeviceChange.Removed:
                {
                    if (device is Touchscreen touchscreen)
                        Touch.RemoveTouchscreen(touchscreen);
                    break;
                }
            }
        }

        private static void OnSettingsChange()
        {
            var currentUpdateMode = InputSystem.settings.updateMode;
            if (s_UpdateMode == currentUpdateMode)
                return;
            TearDownState();
            SetUpState();
        }
    }
}
