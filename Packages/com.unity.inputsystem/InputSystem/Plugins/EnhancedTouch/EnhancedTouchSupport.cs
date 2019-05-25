using UnityEngine.Experimental.Input.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;
#endif

////TODO: gesture support
////TODO: mouse/touch simulation support
////TODO: high-frequency touch support

namespace UnityEngine.Experimental.Input.Touch
{
    [AddComponentMenu("Input/Enhanced Touch Support")]
    public class EnhancedTouchSupport : MonoBehaviour
    {
        public static bool enabled => s_Enabled > 0;

        private static int s_Enabled;
        private static InputSettings.UpdateMode s_UpdateMode;
        #if UNITY_EDITOR
        private static readonly GUIContent s_TouchInspectorButton = new GUIContent("Touch Inspector");
        #endif

        private void OnEnable()
        {
            Enable();
        }

        private void OnDisable()
        {
            Disable();
        }

        public static void Enable()
        {
            ++s_Enabled;
            if (s_Enabled > 1)
                return;

            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onBeforeUpdate += Touch.BeginUpdate;
            InputSystem.onSettingsChange += OnSettingsChange;

            // Add toolbar button to any Touchscreen devices to open a window for
            // visualizing Touch data.
            #if UNITY_EDITOR
            InputDeviceDebuggerWindow.onToolbarGUI += OnDeviceToolbarGUI;
            #endif

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

            #if UNITY_EDITOR
            InputDeviceDebuggerWindow.onToolbarGUI -= OnDeviceToolbarGUI;
            #endif

            TearDownState();
        }

        private static void SetUpState()
        {
            Touch.s_ActiveState.updateMask = InputUpdateType.Dynamic | InputUpdateType.Manual;
            Touch.s_InactiveState.updateMask = InputUpdateType.Fixed;
            #if UNITY_EDITOR
            Touch.s_EditorState.updateMask = InputUpdateType.Editor;
            #endif

            s_UpdateMode = InputSystem.settings.updateMode;
            if (s_UpdateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly)
                MemoryHelpers.Swap(ref Touch.s_ActiveState, ref Touch.s_InactiveState);

            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Added);
        }

        private static void TearDownState()
        {
            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Removed);

            Touch.s_ActiveState.Destroy();
            Touch.s_InactiveState.Destroy();
            #if UNITY_EDITOR
            Touch.s_EditorState.Destroy();
            #endif

            Touch.s_ActiveState = default;
            Touch.s_InactiveState = default;
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

        #if UNITY_EDITOR
        private static void OnDeviceToolbarGUI(InputDevice device)
        {
            if (device is Touchscreen screen &&
                GUILayout.Button(s_TouchInspectorButton, EditorStyles.toolbarButton))
            {
                EditorWindow.GetWindow<TouchInspectorWindow>();
            }
        }

        #endif
    }
}
