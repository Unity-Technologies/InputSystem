using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

////TODO: add pressure support

////REVIEW: extend this beyond simulating from Pointers only? theoretically, we could simulate from any means of generating positions and presses

////REVIEW: I think this is a workable first attempt but overall, not a sufficient take on input simulation. ATM this uses InputState.Change
////        to shove input directly into Touchscreen. Also, it uses state change notifications to set off the simulation. The latter leads
////        to touch input potentially changing multiple times in response to a single pointer event. And the former leads to the simulated
////        touch input not being visible at the event level -- which leaves Touch and Finger slightly unhappy, for example.
////        I think being able to cycle simulated input fully through the event loop would result in a setup that is both simpler and more robust.
////        Also, it would allow *disabling* the source devices as long as we don't disable them in the backend, too.
////        Finally, the fact that we spin off input *from* events here and feed that into InputState.Change() by passing the event along
////        means that places that make sure we process input only once (e.g. binding composites which will remember the event ID they have
////        been triggered from) may reject the simulated input when they have already seen the non-simulated input (which may be okay
////        behavior).

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    /// Adds a <see cref="Touchscreen"/> with input simulated from other types of <see cref="Pointer"/> devices (e.g. <see cref="Mouse"/>
    /// or <see cref="Pen"/>).
    /// </summary>
    [AddComponentMenu("Input/Debug/Touch Simulation")]
    [ExecuteInEditMode]
    [HelpURL(InputSystem.kDocUrl + "/manual/Touch.html#touch-simulation")]
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public class TouchSimulation : MonoBehaviour, IInputStateChangeMonitor
    {
        public Touchscreen simulatedTouchscreen { get; private set; }

        public static TouchSimulation instance => s_Instance;

        public static void Enable()
        {
            if (instance == null)
            {
                ////TODO: find instance
                var hiddenGO = new GameObject();
                hiddenGO.SetActive(false);
                hiddenGO.hideFlags = HideFlags.HideAndDontSave;
                s_Instance = hiddenGO.AddComponent<TouchSimulation>();
                instance.gameObject.SetActive(true);
            }
            instance.enabled = true;
        }

        public static void Disable()
        {
            if (instance != null)
                instance.enabled = false;
        }

        public static void Destroy()
        {
            Disable();

            if (s_Instance != null)
            {
                Destroy(s_Instance.gameObject);
                s_Instance = null;
            }
        }

        protected void AddPointer(Pointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException(nameof(pointer));

            // Ignore if already added.
            if (m_Pointers.ContainsReference(m_NumPointers, pointer))
                return;

            // Add to list.
            ArrayHelpers.AppendWithCapacity(ref m_Pointers, ref m_NumPointers, pointer);
            ArrayHelpers.Append(ref m_CurrentPositions, default(Vector2));
            ArrayHelpers.Append(ref m_CurrentDisplayIndices, default(int));

            InputSystem.DisableDevice(pointer, keepSendingEvents: true);
        }

        protected void RemovePointer(Pointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException(nameof(pointer));

            // Ignore if not added.
            var pointerIndex = m_Pointers.IndexOfReference(pointer, m_NumPointers);
            if (pointerIndex == -1)
                return;

            // Cancel all ongoing touches from the pointer.
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                var button = m_Touches[i];
                if (button != null && button.device != pointer)
                    continue;

                UpdateTouch(i, pointerIndex, TouchPhase.Canceled);
            }

            // Remove from list.
            m_Pointers.EraseAtWithCapacity(ref m_NumPointers, pointerIndex);
            ArrayHelpers.EraseAt(ref m_CurrentPositions, pointerIndex);
            ArrayHelpers.EraseAt(ref m_CurrentDisplayIndices, pointerIndex);

            // Re-enable the device (only in case it's still added to the system).
            if (pointer.added)
                InputSystem.EnableDevice(pointer);
        }

        private unsafe void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == simulatedTouchscreen)
            {
                // Avoid processing events queued by this simulation device
                return;
            }

            var pointerIndex = m_Pointers.IndexOfReference(device, m_NumPointers);
            if (pointerIndex < 0)
                return;

            var eventType = eventPtr.type;
            if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                return;

            ////REVIEW: should we have specialized paths for MouseState and PenState here? (probably can only use for StateEvents)

            Pointer pointer = m_Pointers[pointerIndex];

            // Read pointer position.
            var positionControl = pointer.position;
            var positionStatePtr = positionControl.GetStatePtrFromStateEventUnchecked(eventPtr, eventType);
            if (positionStatePtr != null)
                m_CurrentPositions[pointerIndex] = positionControl.ReadValueFromState(positionStatePtr);

            // Read display index.
            var displayIndexControl = pointer.displayIndex;
            var displayIndexStatePtr = displayIndexControl.GetStatePtrFromStateEventUnchecked(eventPtr, eventType);
            if (displayIndexStatePtr != null)
                m_CurrentDisplayIndices[pointerIndex] = displayIndexControl.ReadValueFromState(displayIndexStatePtr);

            // End touches for which buttons are no longer pressed.
            ////REVIEW: There must be a better way to do this
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                var button = m_Touches[i];
                if (button == null || button.device != device)
                    continue;

                var buttonStatePtr = button.GetStatePtrFromStateEventUnchecked(eventPtr, eventType);
                if (buttonStatePtr == null)
                {
                    // Button is not contained in event. If we do have a position update, issue
                    // a move on the button's corresponding touch. This makes us deal with delta
                    // events that only update pointer positions.
                    if (positionStatePtr != null)
                        UpdateTouch(i, pointerIndex, TouchPhase.Moved, eventPtr);
                }
                else if (button.ReadValueFromState(buttonStatePtr) < (ButtonControl.s_GlobalDefaultButtonPressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold))
                    UpdateTouch(i, pointerIndex, TouchPhase.Ended, eventPtr);
            }

            // Add/update touches for buttons that are pressed.
            foreach (var control in eventPtr.EnumerateControls(InputControlExtensions.Enumerate.IgnoreControlsInDefaultState, device))
            {
                if (!control.isButton)
                    continue;

                // Check if it's pressed.
                var buttonStatePtr = control.GetStatePtrFromStateEventUnchecked(eventPtr, eventType);
                Debug.Assert(buttonStatePtr != null, "Button returned from EnumerateControls() must be found in event");
                var value = 0f;
                control.ReadValueFromStateIntoBuffer(buttonStatePtr, UnsafeUtility.AddressOf(ref value), 4);
                if (value <= ButtonControl.s_GlobalDefaultButtonPressPoint)
                    continue; // Not in default state but also not pressed.

                // See if we have an ongoing touch for the button.
                var touchIndex = m_Touches.IndexOfReference(control);
                if (touchIndex < 0)
                {
                    // No, so add it.
                    touchIndex = m_Touches.IndexOfReference((ButtonControl)null);
                    if (touchIndex >= 0) // If negative, we're at max touch count and can't add more.
                    {
                        m_Touches[touchIndex] = (ButtonControl)control;
                        UpdateTouch(touchIndex, pointerIndex, TouchPhase.Began, eventPtr);
                    }
                }
                else
                {
                    // Yes, so update it.
                    UpdateTouch(touchIndex, pointerIndex, TouchPhase.Moved, eventPtr);
                }
            }

            eventPtr.handled = true;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            // If someone removed our simulated touchscreen, disable touch simulation.
            if (device == simulatedTouchscreen && change == InputDeviceChange.Removed)
            {
                Disable();
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                {
                    if (device is Pointer pointer)
                    {
                        if (device is Touchscreen)
                            return; ////TODO: decide what to do

                        AddPointer(pointer);
                    }
                    break;
                }

                case InputDeviceChange.Removed:
                {
                    if (device is Pointer pointer)
                        RemovePointer(pointer);
                    break;
                }
            }
        }

        protected void OnEnable()
        {
            if (simulatedTouchscreen != null)
            {
                if (!simulatedTouchscreen.added)
                    InputSystem.AddDevice(simulatedTouchscreen);
            }
            else
            {
                simulatedTouchscreen = InputSystem.GetDevice("Simulated Touchscreen") as Touchscreen;
                if (simulatedTouchscreen == null)
                    simulatedTouchscreen = InputSystem.AddDevice<Touchscreen>("Simulated Touchscreen");
            }

            if (m_Touches == null)
                m_Touches = new ButtonControl[simulatedTouchscreen.touches.Count];

            if (m_TouchIds == null)
                m_TouchIds = new int[simulatedTouchscreen.touches.Count];

            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Added);

            if (m_OnDeviceChange == null)
                m_OnDeviceChange = OnDeviceChange;
            if (m_OnEvent == null)
                m_OnEvent = OnEvent;

            InputSystem.onDeviceChange += m_OnDeviceChange;
            InputSystem.onEvent += m_OnEvent;
        }

        protected void OnDisable()
        {
            if (simulatedTouchscreen != null && simulatedTouchscreen.added)
                InputSystem.RemoveDevice(simulatedTouchscreen);

            // Re-enable all pointers we disabled.
            for (var i = 0; i < m_NumPointers; ++i)
                InputSystem.EnableDevice(m_Pointers[i]);

            m_Pointers.Clear(m_NumPointers);
            m_Touches.Clear();

            m_NumPointers = 0;
            m_LastTouchId = 0;

            InputSystem.onDeviceChange -= m_OnDeviceChange;
            InputSystem.onEvent -= m_OnEvent;
        }

        private unsafe void UpdateTouch(int touchIndex, int pointerIndex, TouchPhase phase, InputEventPtr eventPtr = default)
        {
            Vector2 position = m_CurrentPositions[pointerIndex];
            Debug.Assert(m_CurrentDisplayIndices[pointerIndex] <= byte.MaxValue, "Display index was larger than expected");
            byte displayIndex = (byte)m_CurrentDisplayIndices[pointerIndex];

            // We need to partially set TouchState in a similar way that the Native side would do, but deriving that
            // data from the Pointer events.
            // The handling of the remaining fields is done by the Touchscreen.OnStateEvent() callback.
            var touch = new TouchState
            {
                phase = phase,
                position = position,
                displayIndex = displayIndex
            };

            if (phase == TouchPhase.Began)
            {
                touch.startTime = eventPtr.valid ? eventPtr.time : InputState.currentTime;
                touch.startPosition = position;
                touch.touchId = ++m_LastTouchId;
                m_TouchIds[touchIndex] = m_LastTouchId;
            }
            else
            {
                touch.touchId = m_TouchIds[touchIndex];
            }

            //NOTE: Processing these events still happen in the current frame.
            InputSystem.QueueStateEvent(simulatedTouchscreen, touch);

            if (phase.IsEndedOrCanceled())
            {
                m_Touches[touchIndex] = null;
            }
        }

        [NonSerialized] private int m_NumPointers;
        [NonSerialized] private Pointer[] m_Pointers;
        [NonSerialized] private Vector2[] m_CurrentPositions;
        [NonSerialized] private int[] m_CurrentDisplayIndices;
        [NonSerialized] private ButtonControl[] m_Touches;
        [NonSerialized] private int[] m_TouchIds;

        [NonSerialized] private int m_LastTouchId;
        [NonSerialized] private Action<InputDevice, InputDeviceChange> m_OnDeviceChange;
        [NonSerialized] private Action<InputEventPtr, InputDevice> m_OnEvent;

        internal static TouchSimulation s_Instance;

        #if UNITY_EDITOR
        static TouchSimulation()
        {
            // We're a MonoBehaviour so our cctor may get called as part of the MonoBehaviour being
            // created. We don't want to trigger InputSystem initialization from there so delay-execute
            // the code here.
            EditorApplication.delayCall +=
                () =>
            {
                InputSystem.onSettingsChange += OnSettingsChanged;
                InputSystem.onBeforeUpdate += ReEnableAfterDomainReload;
            };
        }

        private static void ReEnableAfterDomainReload()
        {
            OnSettingsChanged();
            InputSystem.onBeforeUpdate -= ReEnableAfterDomainReload;
        }

        private static void OnSettingsChanged()
        {
            if (InputEditorUserSettings.simulateTouch)
                Enable();
            else
                Disable();
        }

        [CustomEditor(typeof(TouchSimulation))]
        private class TouchSimulationEditor : UnityEditor.Editor
        {
            public void OnDisable()
            {
                new InputComponentEditorAnalytic(InputSystemComponent.TouchSimulation).Send();
            }
        }

        #endif // UNITY_EDITOR

        ////TODO: Remove IInputStateChangeMonitor from this class when we can break the API
        void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
        {
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
        {
        }

        // Disable warnings about unused parameters.
        #pragma warning disable CA1801

        ////TODO: [Obsolete]
        protected void InstallStateChangeMonitors(int startIndex = 0)
        {
        }

        ////TODO: [Obsolete]
        protected void OnSourceControlChangedValue(InputControl control, double time, InputEventPtr eventPtr,
            long sourceDeviceAndButtonIndex)
        {
        }

        ////TODO: [Obsolete]
        protected void UninstallStateChangeMonitors(int startIndex = 0)
        {
        }
    }
}
