using System;
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
            if (ArrayHelpers.ContainsReference(m_Sources, m_NumSources, pointer))
                return;

            var numPositions = m_NumSources;
            ArrayHelpers.AppendWithCapacity(ref m_CurrentPositions, ref numPositions, Vector2.zero);
            var index = ArrayHelpers.AppendWithCapacity(ref m_Sources, ref m_NumSources, pointer);

            InstallStateChangeMonitors(index);
        }

        protected void RemovePointer(Pointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException(nameof(pointer));

            // Ignore if not added.
            var index = ArrayHelpers.IndexOfReference(m_Sources, pointer, m_NumSources);
            if (index == -1)
                return;

            // Removing the pointer will shift indices of all pointers coming after it. So we uninstall all
            // monitors starting with the device we're about to remove and then re-install whatever is left
            // starting at the same index.
            UninstallStateChangeMonitors(index);

            // Cancel all ongoing touches from the pointer.
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                if (m_Touches[i].touchId == 0 || m_Touches[i].sourceIndex != index)
                    continue;

                var isPrimary = m_PrimaryTouchIndex == i;
                var touch = new TouchState
                {
                    phase = TouchPhase.Canceled,
                    position = m_CurrentPositions[index],
                    touchId = m_Touches[i].touchId,
                };

                if (isPrimary)
                {
                    InputState.Change(simulatedTouchscreen.primaryTouch, touch);
                    m_PrimaryTouchIndex = -1;
                }

                InputState.Change(simulatedTouchscreen.touches[i], touch);

                m_Touches[i].touchId = 0;
                m_Touches[i].sourceIndex = 0;
            }

            // Remove from arrays.
            var numPositions = m_NumSources;
            ArrayHelpers.EraseAtWithCapacity(m_CurrentPositions, ref numPositions, index);
            ArrayHelpers.EraseAtWithCapacity(m_Sources, ref m_NumSources, index);

            if (index != m_NumSources)
                InstallStateChangeMonitors(index);
        }

        protected void InstallStateChangeMonitors(int startIndex = 0)
        {
            ////REVIEW: just bind to the entire pointer state instead of to individual controls?
            for (var i = startIndex; i < m_NumSources; ++i)
            {
                var pointer = m_Sources[i];

                // Monitor position.
                InputState.AddChangeMonitor(pointer.position, this, i);

                // Monitor any button that isn't synthetic.
                var buttonIndex = 0;
                foreach (var control in pointer.allControls)
                    if (control is ButtonControl button && !button.synthetic)
                    {
                        InputState.AddChangeMonitor(button, this, ((long)(uint)buttonIndex << 32) | (uint)i);
                        ++buttonIndex;
                    }
            }
        }

        protected void UninstallStateChangeMonitors(int startIndex = 0)
        {
            for (var i = startIndex; i < m_NumSources; ++i)
            {
                var pointer = m_Sources[i];

                InputState.RemoveChangeMonitor(pointer.position, this, i);

                var buttonIndex = 0;
                foreach (var control in pointer.allControls)
                    if (control is ButtonControl button && !button.synthetic)
                    {
                        InputState.RemoveChangeMonitor(button, this, ((long)(uint)buttonIndex << 32) | (uint)i);
                        ++buttonIndex;
                    }
            }
        }

        protected void OnSourceControlChangedValue(InputControl control, double time, InputEventPtr eventPtr, long sourceDeviceAndButtonIndex)
        {
            var sourceDeviceIndex = sourceDeviceAndButtonIndex & 0xffffffff;
            if (sourceDeviceIndex < 0 && sourceDeviceIndex >= m_NumSources)
                throw new ArgumentOutOfRangeException(nameof(sourceDeviceIndex), $"Index {sourceDeviceIndex} out of range; have {m_NumSources} sources");

            ////TODO: this can be simplified a lot if we use events instead of InputState.Change() but doing so requires work on buffering events while processing; also
            ////       needs extra handling to not lag into the next frame

            if (control is ButtonControl button)
            {
                var buttonIndex = (int)(sourceDeviceAndButtonIndex >> 32);
                var isPressed = button.isPressed;
                if (isPressed)
                {
                    // Start new touch.
                    for (var i = 0; i < m_Touches.Length; ++i)
                    {
                        // Find unused touch.
                        if (m_Touches[i].touchId != 0)
                            continue;

                        var touchId = ++m_LastTouchId;
                        m_Touches[i] = new SimulatedTouch
                        {
                            touchId = touchId,
                            buttonIndex = buttonIndex,
                            sourceIndex = (int)sourceDeviceIndex,
                        };

                        var isPrimary = m_PrimaryTouchIndex == -1;
                        var position = m_CurrentPositions[sourceDeviceIndex];
                        var oldTouch = simulatedTouchscreen.touches[i].ReadValue();

                        var touch = new TouchState
                        {
                            touchId = touchId,
                            position = position,
                            phase = TouchPhase.Began,
                            startTime = time,
                            startPosition = position,
                            isPrimaryTouch = isPrimary,
                            tapCount = oldTouch.tapCount,
                        };

                        if (isPrimary)
                        {
                            InputState.Change(simulatedTouchscreen.primaryTouch, touch, eventPtr: eventPtr);
                            m_PrimaryTouchIndex = i;
                        }
                        InputState.Change(simulatedTouchscreen.touches[i], touch, eventPtr: eventPtr);

                        break;
                    }
                }
                else
                {
                    // End ongoing touch.
                    for (var i = 0; i < m_Touches.Length; ++i)
                    {
                        if (m_Touches[i].buttonIndex != buttonIndex || m_Touches[i].sourceIndex != sourceDeviceIndex ||
                            m_Touches[i].touchId == 0)
                            continue;

                        // Detect taps.
                        var position = m_CurrentPositions[sourceDeviceIndex];
                        var oldTouch = simulatedTouchscreen.touches[i].ReadValue();
                        var isTap = time - oldTouch.startTime <= Touchscreen.s_TapTime &&
                            (position - oldTouch.startPosition).sqrMagnitude <= Touchscreen.s_TapRadiusSquared;

                        var touch = new TouchState
                        {
                            touchId = m_Touches[i].touchId,
                            phase = TouchPhase.Ended,
                            position = position,
                            tapCount = (byte)(oldTouch.tapCount + (isTap ? 1 : 0)),
                            isTap = isTap,
                            startPosition = oldTouch.startPosition,
                            startTime = oldTouch.startTime,
                        };

                        if (m_PrimaryTouchIndex == i)
                        {
                            InputState.Change(simulatedTouchscreen.primaryTouch, touch, eventPtr: eventPtr);
                            ////TODO: check if there's an ongoing touch that can take over
                            m_PrimaryTouchIndex = -1;
                        }
                        InputState.Change(simulatedTouchscreen.touches[i], touch, eventPtr: eventPtr);

                        m_Touches[i].touchId = 0;
                        break;
                    }
                }
            }
            else
            {
                Debug.Assert(control is InputControl<Vector2>, "Expecting control to be either a button or a position");
                var positionControl = (InputControl<Vector2>)control;

                // Update recorded position.
                var position = positionControl.ReadValue();
                var delta = position - m_CurrentPositions[sourceDeviceIndex];
                m_CurrentPositions[sourceDeviceIndex] = position;

                // Update position of ongoing touches from this pointer.
                for (var i = 0; i < m_Touches.Length; ++i)
                {
                    if (m_Touches[i].sourceIndex != sourceDeviceIndex || m_Touches[i].touchId == 0)
                        continue;

                    var oldTouch = simulatedTouchscreen.touches[i].ReadValue();
                    var isPrimary = m_PrimaryTouchIndex == i;
                    var touch = new TouchState
                    {
                        touchId = m_Touches[i].touchId,
                        phase = TouchPhase.Moved,
                        position = position,
                        delta = delta,
                        isPrimaryTouch = isPrimary,
                        tapCount = oldTouch.tapCount,
                        isTap = false, // Can't be tap as it's a move.
                        startPosition = oldTouch.startPosition,
                        startTime = oldTouch.startTime,
                    };

                    if (isPrimary)
                        InputState.Change(simulatedTouchscreen.primaryTouch, touch, eventPtr: eventPtr);
                    InputState.Change(simulatedTouchscreen.touches[i], touch, eventPtr: eventPtr);
                }
            }
        }

        void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
        {
            OnSourceControlChangedValue(control, time, eventPtr, monitorIndex);
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
        {
            // We don't use timers on our monitors.
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
                m_Touches = new SimulatedTouch[simulatedTouchscreen.touches.Count];

            foreach (var device in InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Added);

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        protected void OnDisable()
        {
            if (simulatedTouchscreen != null && simulatedTouchscreen.added)
                InputSystem.RemoveDevice(simulatedTouchscreen);

            UninstallStateChangeMonitors();

            m_Sources.Clear(m_NumSources);
            m_CurrentPositions.Clear(m_NumSources);
            m_Touches.Clear();

            m_NumSources = 0;
            m_LastTouchId = 0;
            m_PrimaryTouchIndex = -1;

            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        [NonSerialized] private int m_NumSources;
        [NonSerialized] private Pointer[] m_Sources;
        [NonSerialized] private Vector2[] m_CurrentPositions;
        [NonSerialized] private SimulatedTouch[] m_Touches;
        [NonSerialized] private int m_LastTouchId;
        [NonSerialized] private int m_PrimaryTouchIndex = -1;

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

        #endif

        /// <summary>
        /// An ongoing simulated touch.
        /// </summary>
        private struct SimulatedTouch
        {
            public int sourceIndex;
            public int buttonIndex;
            public int touchId;
        }
    }
}
