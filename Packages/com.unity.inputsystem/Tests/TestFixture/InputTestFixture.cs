using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Collections;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////TODO: must allow running UnityTests which means we have to be able to get per-frame updates yet not receive input from native

////TODO: when running tests in players, make sure that remoting is turned off

////REVIEW: always enable event diagnostics in InputTestFixture?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A test fixture for writing tests that use the input system. Can be derived from
    /// or simply instantiated from another test fixture.
    /// </summary>
    /// <remarks>
    /// The fixture will put the input system into a known state where it has only the
    /// built-in set of basic layouts and no devices. The state of the system before
    /// starting a test is recorded and restored when the test finishes.
    ///
    /// <example>
    /// <code>
    /// public class MyInputTests : InputTestFixture
    /// {
    ///     public override void Setup()
    ///     {
    ///         base.Setup();
    ///
    ///         InputSystem.RegisterLayout&lt;MyDevice&gt;();
    ///     }
    ///
    ///     [Test]
    ///     public void CanCreateMyDevice()
    ///     {
    ///         InputSystem.AddDevice&lt;MyDevice&gt;();
    ///         Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf&lt;MyDevice&gt;());
    ///     }
    /// }
    /// </code>
    /// </example>
    ///
    /// The test fixture will also sever the tie of the input system to the Unity runtime.
    /// This means that while the test fixture is active, the input system will not receive
    /// input and device discovery or removal notifications from platform code. This ensures
    /// that while the test is running, input that may be generated on the machine running
    /// the test will not infer with it.
    /// </remarks>
    public class InputTestFixture
    {
        /// <summary>
        /// Put <see cref="InputSystem"/> into a known state where it only has a basic set of
        /// layouts and does not have any input devices.
        /// </summary>
        /// <remarks>
        /// If you derive your own test fixture directly from InputTestFixture, this
        /// method will automatically be called. If you embed InputTestFixture into
        /// your fixture, you have to explicitly call this method yourself.
        /// </remarks>
        /// <seealso cref="TearDown"/>
        [SetUp]
        public virtual void Setup()
        {
            try
            {
                // Apparently, NUnit is reusing instances :(
                m_KeyInfos = default;

                // Disable input debugger so we don't waste time responding to all the
                // input system activity from the tests.
                #if UNITY_EDITOR
                InputDebuggerWindow.Disable();
                #endif

                runtime = new InputTestRuntime();

                // Push current input system state on stack.
                InputSystem.SaveAndReset(enableRemoting: false, runtime: runtime);

                #if UNITY_EDITOR
                // Make sure we're not affected by the user giving focus away from the
                // game view.
                InputEditorUserSettings.lockInputToGameView = true;
                #endif

                // We use native collections in a couple places. We when leak them, we want to know where exactly
                // the allocation came from so enable full leak detection in tests.
                NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to set up input system for test " + TestContext.CurrentContext.Test.Name);
                Debug.LogException(exception);
                throw;
            }

            if (InputSystem.devices.Count > 0)
                Assert.Fail("Input system should not have devices after reset");
        }

        /// <summary>
        /// Restore the state of the input system it had when the test was started.
        /// </summary>
        /// <seealso cref="Setup"/>
        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                // Destroy any GameObject in the current scene that isn't hidden and isn't the
                // test runner object. Do this first so that any cleanup finds the system in the
                // state it expects.
                var scene = SceneManager.GetActiveScene();
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.hideFlags != 0 || go.name.Contains("tests runner"))
                        continue;
                    Object.DestroyImmediate(go);
                }

                InputSystem.Restore();
                runtime.Dispose();

                // Re-enable input debugger.
                #if UNITY_EDITOR
                InputDebuggerWindow.Enable();
                #endif
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to shut down and restore input system after test " + TestContext.CurrentContext.Test.Name);
                Debug.LogException(exception);
                throw;
            }
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public static void AssertButtonPress<TState>(InputDevice device, TState state, params ButtonControl[] buttons)
            where TState : struct, IInputStateTypeInfo
        {
            // Update state.
            InputSystem.QueueStateEvent(device, state);
            InputSystem.Update();

            // Now verify that only the buttons we expect to be pressed are pressed.
            foreach (var control in device.allControls)
            {
                if (!(control is ButtonControl controlAsButton))
                    continue;

                var isInList = buttons.Contains(controlAsButton);
                if (!isInList)
                    Assert.That(controlAsButton.isPressed, Is.False,
                        $"Expected button {controlAsButton} to NOT be pressed");
                else
                    Assert.That(controlAsButton.isPressed, Is.True,
                        $"Expected button {controlAsButton} to be pressed");
            }
        }

        private Dictionary<Key, Tuple<string, int>> m_KeyInfos;

        /// <summary>
        /// Set <see cref="Keyboard.keyboardLayout"/> of the given keyboard.
        /// </summary>
        /// <param name="name">Name of the keyboard layout to switch to.</param>
        /// <param name="keyboard">Keyboard to switch layout on. If <c>null</c>, <see cref="Keyboard.current"/> is used.</param>
        /// <exception cref="ArgumentException"><paramref name="keyboard"/> and <see cref="Keyboard.current"/> are both <c>null</c>.</exception>
        /// <remarks>
        /// Also queues and immediately processes an <see cref="DeviceConfigurationEvent"/> for the keyboard.
        /// </remarks>
        public unsafe void SetKeyboardLayout(string name, Keyboard keyboard = null)
        {
            if (keyboard == null)
            {
                keyboard = Keyboard.current;
                if (keyboard == null)
                    throw new ArgumentException("No keyboard has been created and no keyboard has been given", nameof(keyboard));
            }

            runtime.SetDeviceCommandCallback(keyboard, (id, command) =>
            {
                if (id == QueryKeyboardLayoutCommand.Type)
                {
                    var commandPtr = (QueryKeyboardLayoutCommand*)command;
                    commandPtr->WriteLayoutName(name);
                    return InputDeviceCommand.GenericSuccess;
                }
                return InputDeviceCommand.GenericFailure;
            });

            // Make sure caches on keys are flushed.
            InputSystem.QueueConfigChangeEvent(Keyboard.current);
            InputSystem.Update();
        }

        /// <summary>
        /// Set the <see cref="InputControl.displayName"/> of <paramref name="key"/> on the current
        /// <see cref="Keyboard"/> to be <paramref name="displayName"/>.
        /// </summary>
        /// <param name="key">Key to set the display name for.</param>
        /// <param name="displayName">Display name for the key.</param>
        /// <param name="scanCode">Optional <see cref="KeyControl.scanCode"/> to report for the key.</param>
        /// <remarks>
        /// Automatically adds a <see cref="Keyboard"/> if none has been added yet.
        /// </remarks>
        public unsafe void SetKeyInfo(Key key, string displayName, int scanCode = 0)
        {
            if (Keyboard.current == null)
                InputSystem.AddDevice<Keyboard>();

            if (m_KeyInfos == null)
            {
                m_KeyInfos = new Dictionary<Key, Tuple<string, int>>();

                runtime.SetDeviceCommandCallback(Keyboard.current,
                    (id, commandPtr) =>
                    {
                        if (commandPtr->type == QueryKeyNameCommand.Type)
                        {
                            var keyNameCommand = (QueryKeyNameCommand*)commandPtr;

                            if (m_KeyInfos.TryGetValue((Key)keyNameCommand->scanOrKeyCode, out var info))
                            {
                                keyNameCommand->scanOrKeyCode = info.Item2;
                                StringHelpers.WriteStringToBuffer(info.Item1, (IntPtr)keyNameCommand->nameBuffer,
                                    QueryKeyNameCommand.kMaxNameLength);
                            }

                            return QueryKeyNameCommand.kSize;
                        }

                        return InputDeviceCommand.GenericFailure;
                    });
            }

            m_KeyInfos[key] = new Tuple<string, int>(displayName, scanCode);

            // Make sure caches on keys are flushed.
            InputSystem.QueueConfigChangeEvent(Keyboard.current);
            InputSystem.Update();
        }

        /// <summary>
        /// Add support for <see cref="QueryCanRunInBackground"/> to <paramref name="device"/> and return
        /// <paramref name="value"/> as <see cref="QueryCanRunInBackground.canRunInBackground"/>.
        /// </summary>
        /// <param name="device"></param>
        internal unsafe void SetCanRunInBackground(InputDevice device, bool canRunInBackground = true)
        {
            runtime.SetDeviceCommandCallback(device, (id, command) =>
            {
                if (command->type == QueryCanRunInBackground.Type)
                {
                    ((QueryCanRunInBackground*)command)->canRunInBackground = canRunInBackground;
                    return InputDeviceCommand.GenericSuccess;
                }
                return InputDeviceCommand.GenericFailure;
            });
        }

        public ActionConstraint Started(InputAction action, InputControl control = null, double? time = null)
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, time: time, duration: 0);
        }

        public ActionConstraint Started<TValue>(InputAction action, InputControl<TValue> control, TValue value, double? time = null)
            where TValue : struct
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, value, time: time, duration: 0);
        }

        public ActionConstraint Performed(InputAction action, InputControl control = null, double? time = null, double? duration = null)
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, time: time, duration: duration);
        }

        public ActionConstraint Performed<TValue>(InputAction action, InputControl<TValue> control, TValue value, double? time = null, double? duration = null)
            where TValue : struct
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, value, time: time, duration: duration);
        }

        public ActionConstraint Canceled(InputAction action, InputControl control = null, double? time = null, double? duration = null)
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control, time: time, duration: duration);
        }

        public ActionConstraint Canceled<TValue>(InputAction action, InputControl<TValue> control, TValue value, double? time = null, double? duration = null)
            where TValue : struct
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control, value, time: time, duration: duration);
        }

        public ActionConstraint Started<TInteraction>(InputAction action, InputControl control = null, object value = null, double? time = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, interaction: typeof(TInteraction), time: time,
                duration: 0, value: value);
        }

        public ActionConstraint Performed<TInteraction>(InputAction action, InputControl control = null, object value = null, double? time = null, double? duration = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, interaction: typeof(TInteraction), time: time,
                duration: duration, value: value);
        }

        public ActionConstraint Canceled<TInteraction>(InputAction action, InputControl control = null, object value = null, double? time = null, double? duration = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control, interaction: typeof(TInteraction), time: time,
                duration: duration, value: value);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void Press(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            Set(button, 1, time, timeOffset, queueEventOnly: queueEventOnly);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void Release(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            Set(button, 0, time, timeOffset, queueEventOnly: queueEventOnly);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void PressAndRelease(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            Press(button, time, timeOffset, queueEventOnly: true);  // This one is always just a queue.
            Release(button, time, timeOffset, queueEventOnly: queueEventOnly);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void Click(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            PressAndRelease(button, time, timeOffset, queueEventOnly: queueEventOnly);
        }

        /// <summary>
        /// Set the control to the given value by sending a state event with the value to the
        /// control's device.
        /// </summary>
        /// <param name="control">An input control on a device that has been added to the system.</param>
        /// <param name="state">New value for the input control.</param>
        /// <typeparam name="TValue">Value type of the given control.</typeparam>
        /// <example>
        /// <code>
        /// var gamepad = InputSystem.AddDevice&lt;Gamepad&gt;();
        /// Set(gamepad.leftButton, 1);
        /// </code>
        /// </example>
        public void Set<TValue>(InputControl<TValue> control, TValue state, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!control.device.added)
                throw new ArgumentException(
                    $"Device of control '{control}' has not been added to the system", nameof(control));

            void SetUpAndQueueEvent(InputEventPtr eventPtr)
            {
                ////REVIEW: should we by default take the time from the device here?
                if (time >= 0)
                    eventPtr.time = time;
                eventPtr.time += timeOffset;
                control.WriteValueIntoEvent(state, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }

            // Touchscreen does not support delta events involving TouchState.
            if (control is TouchControl)
            {
                using (StateEvent.From(control.device, out var eventPtr))
                    SetUpAndQueueEvent(eventPtr);
            }
            else
            {
                // We use delta state events rather than full state events here to mitigate the following problem:
                // Grabbing state from the device will preserve the current values of controls covered in the state.
                // However, running an update may alter the value of one or more of those controls. So with a full
                // state event, we may be writing outdated data back into the device. For example, in the case of delta
                // controls which will reset in OnBeforeUpdate().
                //
                // Using delta events, we may still grab state outside of just the one control in case we're looking at
                // bit-addressed controls but at least we can avoid the problem for the majority of controls.
                using (DeltaStateEvent.From(control, out var eventPtr))
                    SetUpAndQueueEvent(eventPtr);
            }

            if (!queueEventOnly)
                InputSystem.Update();
        }

        public void Move(InputControl<Vector2> positionControl, Vector2 position, Vector2? delta = null, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            Set(positionControl, position, time: time, timeOffset: timeOffset, queueEventOnly: true);

            var deltaControl = (Vector2Control)positionControl.device.TryGetChildControl("delta");
            if (deltaControl != null)
                Set(deltaControl,  delta ?? position - positionControl.ReadValue(), time: time, timeOffset: timeOffset, queueEventOnly: true);

            if (!queueEventOnly)
                InputSystem.Update();
        }

        public void BeginTouch(int touchId, Vector2 position, bool queueEventOnly = false, Touchscreen screen = null,
            double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Began, position, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void MoveTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Moved, position, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void EndTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Ended, position, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void CancelTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Canceled, position, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void SetTouch(int touchId, TouchPhase phase, Vector2 position, Vector2 delta = default, bool queueEventOnly = true,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            if (screen == null)
            {
                screen = Touchscreen.current;
                if (screen == null)
                    throw new InvalidOperationException("No touchscreen has been added");
            }

            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = touchId,
                phase = phase,
                position = position,
                delta = delta,
            }, (time >= 0 ? time : InputRuntime.s_Instance.currentTime) + timeOffset);
            if (!queueEventOnly)
                InputSystem.Update();
        }

        public void Trigger<TValue>(InputAction action, InputControl<TValue> control, TValue value)
            where TValue : struct
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Perform the input action without having to know what it is bound to.
        /// </summary>
        /// <param name="action">An input action that is currently enabled and has controls it is bound to.</param>
        /// <remarks>
        /// Blindly triggering an action requires making a few assumptions. Actions are not built to be able to trigger
        /// without any input. This means that this method has to generate input on a control that the action is bound to.
        ///
        /// Note that this method has no understanding of the interactions that may be present on the action and thus
        /// does not know how they may affect the triggering of the action.
        /// </remarks>
        public void Trigger(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!action.enabled)
                throw new ArgumentException(
                    $"Action '{action}' must be enabled in order to be able to trigger it", nameof(action));

            var controls = action.controls;
            if (controls.Count == 0)
                throw new ArgumentException(
                    $"Action '{action}' must be bound to controls in order to be able to trigger it", nameof(action));

            // See if we have a button we can trigger.
            for (var i = 0; i < controls.Count; ++i)
            {
                if (!(controls[i] is ButtonControl button))
                    continue;

                // Press and release button.
                Set(button, 1);
                Set(button, 0);

                return;
            }

            // See if we have an axis we can slide a bit.
            for (var i = 0; i < controls.Count; ++i)
            {
                if (!(controls[i] is AxisControl axis))
                    continue;

                // We do, so nudge its value a bit.
                Set(axis, axis.ReadValue() + 0.01f);

                return;
            }

            ////TODO: support a wider range of controls
            throw new NotImplementedException();
        }

        /// <summary>
        /// The input runtime used during testing.
        /// </summary>
        internal InputTestRuntime runtime { get; private set; }

        /// <summary>
        /// Get or set the current time used by the input system.
        /// </summary>
        /// <value>Current time used by the input system.</value>
        public double currentTime
        {
            get => runtime.currentTime;
            set
            {
                runtime.currentTime = value;
                runtime.dontAdvanceTimeNextDynamicUpdate = true;
            }
        }

        public class ActionConstraint : Constraint
        {
            public InputActionPhase phase { get; set; }
            public double? time { get; set; }
            public double? duration { get; set; }
            public InputAction action { get; set; }
            public InputControl control { get; set; }
            public object value { get; set; }
            public Type interaction { get; set; }

            private readonly List<ActionConstraint> m_AndThen = new List<ActionConstraint>();

            public ActionConstraint(InputActionPhase phase, InputAction action, InputControl control, object value = null, Type interaction = null, double? time = null, double? duration = null)
            {
                this.phase = phase;
                this.time = time;
                this.duration = duration;
                this.action = action;
                this.control = control;
                this.value = value;
                this.interaction = interaction;

                var interactionText = string.Empty;
                if (interaction != null)
                    interactionText = InputInteraction.GetDisplayName(interaction);

                var actionName = action.actionMap != null ? $"{action.actionMap}/{action.name}" : action.name;
                // Use same text format as InputActionTrace for easier comparison.
                var description = $"{{ action={actionName} phase={phase}";
                if (time != null)
                    description += $" time={time}";
                if (control != null)
                    description += $" control={control}";
                if (value != null)
                    description += $" value={value}";
                if (interaction != null)
                    description += $" interaction={interactionText}";
                if (duration != null)
                    description += $" duration={duration}";
                description += " }";
                Description = description;
            }

            public override ConstraintResult ApplyTo(object actual)
            {
                var trace = (InputActionTrace)actual;
                var actions = trace.ToArray();

                if (actions.Length == 0)
                    return new ConstraintResult(this, actual, false);

                if (!Verify(actions[0]))
                    return new ConstraintResult(this, actual, false);

                var i = 1;
                foreach (var constraint in m_AndThen)
                {
                    if (i >= actions.Length || !constraint.Verify(actions[i]))
                        return new ConstraintResult(this, actual, false);
                    ++i;
                }

                if (i != actions.Length)
                    return new ConstraintResult(this, actual, false);

                return new ConstraintResult(this, actual, true);
            }

            private bool Verify(InputActionTrace.ActionEventPtr eventPtr)
            {
                // NOTE: Using explicit "return false" branches everywhere for easier setting of breakpoints.

                if (eventPtr.action != action ||
                    eventPtr.phase != phase)
                    return false;

                // Check time.
                if (time != null && !Mathf.Approximately((float)time.Value, (float)eventPtr.time))
                    return false;

                // Check duration.
                if (duration != null && !Mathf.Approximately((float)duration.Value, (float)eventPtr.duration))
                    return false;

                // Check control.
                if (control != null && eventPtr.control != control)
                    return false;

                // Check interaction.
                if (interaction != null && (eventPtr.interaction == null ||
                                            !interaction.IsInstanceOfType(eventPtr.interaction)))
                    return false;

                // Check value.
                if (value != null)
                {
                    var val = eventPtr.ReadValueAsObject();
                    if (value is float f)
                    {
                        if (!Mathf.Approximately(f, Convert.ToSingle(val)))
                            return false;
                    }
                    else if (value is double d)
                    {
                        if (!Mathf.Approximately((float)d, (float)Convert.ToDouble(val)))
                            return false;
                    }
                    else if (value is Vector2 v2)
                    {
                        if (!Vector2EqualityComparer.Instance.Equals(v2, (Vector2)val))
                            return false;
                    }
                    else if (value is Vector3 v3)
                    {
                        if (!Vector3EqualityComparer.Instance.Equals(v3, (Vector3)val))
                            return false;
                    }
                    else if (!value.Equals(val))
                        return false;
                }

                return true;
            }

            public ActionConstraint AndThen(ActionConstraint constraint)
            {
                m_AndThen.Add(constraint);
                Description += " and\n";
                Description += constraint.Description;
                return this;
            }
        }

        #if UNITY_EDITOR
        internal void SimulateDomainReload()
        {
            // This quite invasively goes into InputSystem internals. Unfortunately, we
            // have no proper way of simulating domain reloads ATM. So we directly call various
            // internal methods here in a sequence similar to what we'd get during a domain reload.

            InputSystem.s_SystemObject.OnBeforeSerialize();
            InputSystem.s_SystemObject = null;
            InputSystem.InitializeInEditor(runtime);
        }

        #endif
    }
}
