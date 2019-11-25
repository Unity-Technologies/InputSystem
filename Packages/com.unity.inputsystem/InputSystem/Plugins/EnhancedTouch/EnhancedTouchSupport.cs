using UnityEngine.InputSystem.LowLevel;
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
    /// <remarks>
    /// Enhanced touch support provides automatic finger tracking and touch history recording.
    /// It is an API designed for polling, i.e. for querying touch state directly in methods
    /// such as <c>MonoBehaviour.Update</c>. Enhanced touch support cannot be used in combination
    /// with <see cref="InputAction"/>s though both can be used side-by-side.
    ///
    /// <example>
    /// <code>
    /// public class MyBehavior : MonoBehaviour
    /// {
    ///     protected void OnEnable()
    ///     {
    ///         EnhancedTouchSupport.Enable();
    ///     }
    ///
    ///     protected void OnDisable()
    ///     {
    ///         EnhancedTouchSupport.Disable();
    ///     }
    ///
    ///     protected void Update()
    ///     {
    ///         var activeTouches = Touch.activeTouches;
    ///         for (var i = 0; i &lt; activeTouches.Count; ++i)
    ///             Debug.Log("Active touch: " + activeTouches[i]);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Touch"/>
    /// <seealso cref="Finger"/>
    public static class EnhancedTouchSupport
    {
        /// <summary>
        /// Whether enhanced touch support is currently enabled.
        /// </summary>
        /// <value>True if EnhancedTouch support has been enabled.</value>
        public static bool enabled => s_Enabled > 0;

        private static int s_Enabled;
        private static InputSettings.UpdateMode s_UpdateMode;

        /// <summary>
        /// Enable enhanced touch support.
        /// </summary>
        /// <remarks>
        /// Calling this method is necessary to enable the functionality provided
        /// by <see cref="Touch"/> and <see cref="Finger"/>. These APIs add extra
        /// processing to touches and are thus disabled by default.
        ///
        /// Calls to <c>Enable</c> and <see cref="Disable"/> balance each other out.
        /// If <c>Enable</c> is called repeatedly, it will take as many calls to
        /// <see cref="Disable"/> to disable the system again.
        /// </remarks>
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

        /// <summary>
        /// Disable enhanced touch support.
        /// </summary>
        /// <remarks>
        /// This method only undoes a single call to <see cref="Enable"/>.
        /// </remarks>
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
