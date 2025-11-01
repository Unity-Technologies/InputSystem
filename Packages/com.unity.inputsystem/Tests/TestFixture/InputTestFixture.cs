using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem.Controls;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.InputSystem.XR;

#if UNITY_EDITOR
using UnityEditor;
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
    ///
    /// Be cautious when using <c>NUnit.Framework.OneTimeSetUpAttribute</c> and
    /// <c>NUnit.Framework.OneTimeTearDownAttribute</c> in combination with this test fixture.
    /// For example, any devices created prior to execution of <see cref="Setup()"/> would be added to the actual
    /// Input System instead of the test fixture system and after <see cref="Setup()"/> has executed such devices
    /// will no longer be valid. You may of course use these NUnit features, but it is advised to not attempt affecting
    /// the Input System under test from those methods since it would affect the real system and not the system
    /// under test.
    ///
    /// This test fixture is designed for play-mode tests and is generally not supported for edit-mode tests.
    /// Both <c>[Test]</c> and <c>[UnityTest]</c> are supported, but only in play-mode.
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
                m_IsUnityTest = default;
                m_CurrentTest = default;

                // Disable input debugger so we don't waste time responding to all the
                // input system activity from the tests.
                #if UNITY_EDITOR
                InputDebuggerWindow.Disable();
                #endif

                runtime = new InputTestRuntime();

                // Push current input system state on stack.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                InputSystem.SaveAndReset(enableRemoting: false, runtime: runtime);
#endif
                // Override the editor messing with logic like canRunInBackground and focus and
                // make it behave like in the player.
                #if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                #endif

                // For a [UnityTest] play mode test, we don't want editor updates interfering with the test,
                // so turn them off.
                #if UNITY_EDITOR
                if (Application.isPlaying && IsUnityTest())
                    InputSystem.s_Manager.m_UpdateMask &= ~InputUpdateType.Editor;
                #endif

                // We use native collections in a couple places. We when leak them, we want to know where exactly
                // the allocation came from so enable full leak detection in tests.
                NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;

                // For [UnityTest]s, we need to process input in sync with the player loop. However, InputTestRuntime
                // is divorced from the player loop by virtue of not being tied into NativeInputSystem. Listen
                // for NativeInputSystem.Update here and trigger input processing in our isolated InputSystem.
                // This is irrelevant for normal [Test]s but for [UnityTest]s that run over several frames, it's crucial.
                // NOTE: We're severing the tie the previous InputManager had to NativeInputRuntime here. This means that
                //       device removal events that happen to occur while tests are running will get lost.
                NativeInputRuntime.instance.onUpdate =
                    (InputUpdateType updateType, ref InputEventBuffer buffer) =>
                {
                    if (InputSystem.s_Manager.ShouldRunUpdate(updateType))
                        InputSystem.Update(updateType);
                    // We ignore any input coming from native.
                    buffer.Reset();
                };
                NativeInputRuntime.instance.onShouldRunUpdate =
                    updateType => true;

                #if UNITY_EDITOR
                m_OnPlayModeStateChange = OnPlayModeStateChange;
                EditorApplication.playModeStateChanged += m_OnPlayModeStateChange;
                #endif

                // Always want to merge by default
                InputSystem.settings.disableRedundantEventsMerging = false;

                // Turn on all optimizations and checks
                InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseOptimizedControls, true);
                InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseReadValueCaching, true);
                InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kParanoidReadValueCachingChecks, true);

                #if UNITY_EDITOR
                // Default mock dialogs to avoid unexpected cancellation of standard flows
                Dialog.InputActionAsset.SetSaveChanges((_) => Dialog.Result.Discard);
                Dialog.InputActionAsset.SetDiscardUnsavedChanges((_) => Dialog.Result.Discard);
                Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset((_) => Dialog.Result.Discard);
                Dialog.ControlScheme.SetDeleteControlScheme((_) => Dialog.Result.Delete);
                #endif
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to set up input system for test " + TestContext.CurrentContext.Test.Name);
                Debug.LogException(exception);
                throw;
            }

            m_Initialized = true;

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
            if (!m_Initialized)
                return;

            try
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                InputSystem.Restore();
#endif
                runtime.Dispose();

                // Unhook from play mode state changes.
                #if UNITY_EDITOR
                if (m_OnPlayModeStateChange != null)
                    EditorApplication.playModeStateChanged -= m_OnPlayModeStateChange;
                #endif

                // Re-enable input debugger.
                #if UNITY_EDITOR
                InputDebuggerWindow.Enable();
                #endif

                #if UNITY_EDITOR
                // Re-enable dialogs.
                Dialog.InputActionAsset.SetSaveChanges(null);
                Dialog.InputActionAsset.SetDiscardUnsavedChanges(null);
                Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset(null);
                Dialog.ControlScheme.SetDeleteControlScheme(null);
                #endif
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to shut down and restore input system after test " + TestContext.CurrentContext.Test.Name);
                Debug.LogException(exception);
                throw;
            }

            m_Initialized = false;
        }

        private bool? m_IsUnityTest;
        private Test m_CurrentTest;

        // True if the current test is a [UnityTest].
        private bool IsUnityTest()
        {
            // We cache this value so that any call after the first in a test no
            // longer allocates GC memory. Otherwise we'll run into trouble with
            // DoesNotAllocate tests.
            var test = TestContext.CurrentTestExecutionContext.CurrentTest;
            if (m_IsUnityTest.HasValue && m_CurrentTest == test)
                return m_IsUnityTest.Value;

            var className = test.ClassName;
            var methodName = test.MethodName;

            // Doesn't seem like there's a proper way to get the current test method based on
            // the information provided by NUnit (see https://github.com/nunit/nunit/issues/3354).

            var type = Type.GetType(className);
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(className);
                    if (type != null)
                        break;
                }
            }

            if (type == null)
            {
                m_IsUnityTest = false;
            }
            else
            {
                var method = type.GetMethod(methodName);
                m_IsUnityTest = method?.GetCustomAttribute<UnityTestAttribute>() != null;
            }

            m_CurrentTest = test;
            return m_IsUnityTest.Value;
        }

        #if UNITY_EDITOR
        private Action<PlayModeStateChange> m_OnPlayModeStateChange;
        private void OnPlayModeStateChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode && m_Initialized)
                TearDown();
        }

        #endif

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

        public static void AssertStickValues(StickControl stick, Vector2 stickValue, float up, float down, float left,
            float right)
        {
            Assert.That(stick.ReadUnprocessedValue(), Is.EqualTo(stickValue));

            Assert.That(stick.up.ReadUnprocessedValue(), Is.EqualTo(up).Within(0.0001), "Incorrect 'up' value");
            Assert.That(stick.down.ReadUnprocessedValue(), Is.EqualTo(down).Within(0.0001), "Incorrect 'down' value");
            Assert.That(stick.left.ReadUnprocessedValue(), Is.EqualTo(left).Within(0.0001), "Incorrect 'left' value");
            Assert.That(stick.right.ReadUnprocessedValue(), Is.EqualTo(right).Within(0.0001), "Incorrect 'right' value");
        }

        private Dictionary<Key, Tuple<string, int>> m_KeyInfos;
        private bool m_Initialized;

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

        public ActionConstraint Started(InputAction action, InputControl control = null, double? time = null, object value = null)
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, time: time, duration: 0, value: value);
        }

        public ActionConstraint Started<TValue>(InputAction action, InputControl<TValue> control, TValue value, double? time = null)
            where TValue : struct
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, value, time: time, duration: 0);
        }

        public ActionConstraint Performed(InputAction action, InputControl control = null, double? time = null, double? duration = null, object value = null)
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, time: time, duration: duration, value: value);
        }

        public ActionConstraint Performed<TValue>(InputAction action, InputControl<TValue> control, TValue value, double? time = null, double? duration = null)
            where TValue : struct
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, value, time: time, duration: duration);
        }

        public ActionConstraint Canceled(InputAction action, InputControl control = null, double? time = null, double? duration = null, object value = null)
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control, time: time, duration: duration, value: value);
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

        ////REVIEW: Should we determine queueEventOnly automatically from whether we're in a UnityTest?

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
        /// Set the control with the given <paramref name="path"/> on <paramref name="device"/> to the given <paramref name="state"/>
        /// by sending a state event with the value to the device.
        /// </summary>
        /// <param name="device">Device on which to find a control.</param>
        /// <param name="path">Path of the control on the device.</param>
        /// <param name="state">New state for the control.</param>
        /// <param name="time">Timestamp to use for the state event. If -1 (default), current time is used (see <see cref="InputTestFixture.currentTime"/>).</param>
        /// <param name="timeOffset">Offset to apply to the current time. This is an alternative to <paramref name="time"/>. By default, no offset is applied.</param>
        /// <param name="queueEventOnly">If true, no <see cref="InputSystem.Update"/> will be performed after queueing the event. This will only put
        /// the state event on the event queue and not do anything else. The default is to call <see cref="InputSystem.Update"/> after queuing the event.
        /// Note that not issuing an update means the state of the device will not change yet. This may affect subsequent Set/Press/Release/etc calls
        /// as they will not yet see the state change.
        ///
        /// Note that this parameter will be ignored if the test is a <c>[UnityTest]</c>. Multi-frame
        /// playmode tests will automatically process input as part of the Unity player loop.</param>
        /// <typeparam name="TValue">Value type of the control.</typeparam>
        /// <example>
        /// <code>
        /// var device = InputSystem.AddDevice("TestDevice");
        /// Set&lt;ButtonControl&gt;(device, "button", 1);
        /// Set&lt;AxisControl&gt;(device, "{Primary2DMotion}/x", 123.456f);
        /// </code>
        /// </example>
        public void Set<TValue>(InputDevice device, string path, TValue state, double time = -1, double timeOffset = 0,
            bool queueEventOnly = false)
            where TValue : struct
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var control = (InputControl<TValue>)device[path];
            Set(control, state, time, timeOffset, queueEventOnly);
        }

        /// <summary>
        /// Set the control to the given value by sending a state event with the value to the
        /// control's device.
        /// </summary>
        /// <param name="control">An input control on a device that has been added to the system.</param>
        /// <param name="state">New value for the input control.</param>
        /// <param name="time">Timestamp to use for the state event. If -1 (default), current time is used (see <see cref="InputTestFixture.currentTime"/>).</param>
        /// <param name="timeOffset">Offset to apply to the current time. This is an alternative to <paramref name="time"/>. By default, no offset is applied.</param>
        /// <param name="queueEventOnly">If true, no <see cref="InputSystem.Update"/> will be performed after queueing the event. This will only put
        /// the state event on the event queue and not do anything else. The default is to call <see cref="InputSystem.Update"/> after queuing the event.
        /// Note that not issuing an update means the state of the device will not change yet. This may affect subsequent Set/Press/Release/etc calls
        /// as they will not yet see the state change.
        ///
        /// Note that this parameter will be ignored if the test is a <c>[UnityTest]</c>. Multi-frame
        /// playmode tests will automatically process input as part of the Unity player loop.</param>
        /// <typeparam name="TValue">Value type of the given control.</typeparam>
        /// <exception cref="ArgumentNullException">If control is null.</exception>
        /// <exception cref="ArgumentException">If the device associated with <paramref name="control"/> has not
        /// been added to the system or if the control does not have any associated state. The latter may only
        /// happen if attempting to set a control of a device created outside the test context.</exception>
        /// <exception cref="NotSupportedException">If attempting to set a control of a test device in an
        /// editor assembly. [UnityTest] in editor assemblies is not supported by this test fixture.</exception>
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
            CheckValidity(control.device, control);

            if (IsUnityTest())
            {
                if (IsEditMode())
                    throw new NotSupportedException("InputTestFixture.Set does not support edit mode (editor assembly) [UnityTest].");
                queueEventOnly = true;
            }

            void SetUpAndQueueEvent(InputEventPtr eventPtr)
            {
                eventPtr.time = (time >= 0 ? time : InputState.currentTime) + timeOffset;
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

        ////TODO: obsolete this one in 2.0 and use pressure=1 default value
        public void BeginTouch(int touchId, Vector2 position, bool queueEventOnly = false, Touchscreen screen = null,
            double time = -1, double timeOffset = 0, byte displayIndex = 0)
        {
            SetTouch(touchId, TouchPhase.Began, position, 1, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset, displayIndex: displayIndex);
        }

        public void BeginTouch(int touchId, Vector2 position, float pressure, bool queueEventOnly = false, Touchscreen screen = null,
            double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Began, position, pressure, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        ////TODO: obsolete this one in 2.0 and use pressure=1 default value
        public void MoveTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Moved, position, 1, delta, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void MoveTouch(int touchId, Vector2 position, float pressure, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Moved, position, pressure, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        ////TODO: obsolete this one in 2.0 and use pressure=1 default value
        public void EndTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0, byte displayIndex = 0)
        {
            SetTouch(touchId, TouchPhase.Ended, position, 1, delta, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset, displayIndex: displayIndex);
        }

        public void EndTouch(int touchId, Vector2 position, float pressure, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Ended, position, pressure, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        ////TODO: obsolete this one in 2.0 and use pressure=1 default value
        public void CancelTouch(int touchId, Vector2 position, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Canceled, position, delta, queueEventOnly: queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        public void CancelTouch(int touchId, Vector2 position, float pressure, Vector2 delta = default, bool queueEventOnly = false,
            Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, TouchPhase.Canceled, position, pressure, delta, queueEventOnly, screen: screen, time: time, timeOffset: timeOffset);
        }

        ////TODO: obsolete this one in 2.0 and use pressure=1 default value
        public void SetTouch(int touchId, TouchPhase phase, Vector2 position, Vector2 delta = default,
            bool queueEventOnly = true, Touchscreen screen = null, double time = -1, double timeOffset = 0)
        {
            SetTouch(touchId, phase, position, 1, delta: delta, queueEventOnly: queueEventOnly, screen: screen, time: time,
                timeOffset: timeOffset);
        }

        public void SetTouch(int touchId, TouchPhase phase, Vector2 position, float pressure, Vector2 delta = default, bool queueEventOnly = true,
            Touchscreen screen = null, double time = -1, double timeOffset = 0, byte displayIndex = 0)
        {
            if (screen == null)
            {
                screen = Touchscreen.current;
                if (screen == null)
                    screen = InputSystem.AddDevice<Touchscreen>();
            }
            CheckValidity(screen);

            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = touchId,
                phase = phase,
                position = position,
                delta = delta,
                pressure = pressure,
                displayIndex = displayIndex,
            }, (time >= 0 ? time : InputState.currentTime) + timeOffset);
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

            // We iterate controls to see if there are any that we know how to trigger
            for (var i = 0; i < controls.Count; ++i)
            {
                var control = controls[i];

                // for buttons, we literally trigger them pressed and released
                if (control is ButtonControl buttonControl)
                {
                    Set(buttonControl, 1);
                    Set(buttonControl, 0);

                    return;
                }

                // for the other types of controls we simply perform a small change in value as applicable
                const float minorChange = 0.01f;

                if (control is AxisControl axisControl)
                {
                    Set(axisControl, axisControl.ReadValue() + minorChange);

                    return;
                }

                if (control is Vector2Control vec2Control)
                {
                    Set(vec2Control, vec2Control.ReadValue() + Vector2.one * minorChange);

                    return;
                }

                if (control is Vector3Control vec3Control)
                {
                    Set(vec3Control, vec3Control.ReadValue() + Vector3.one * minorChange);

                    return;
                }

                if (control is QuaternionControl quatControl)
                {
                    var q = quatControl.ReadValue();
                    q.ToAngleAxis(out float angle, out Vector3 axis);
                    Set(quatControl, Quaternion.AngleAxis(angle + minorChange, axis));

                    return;
                }

                if (control is IntegerControl integerControl)
                {
                    Set(integerControl, integerControl.ReadValue() + 1);

                    return;
                }

                if (control is TouchControl touchControl)
                {
                    var state = touchControl.ReadValue();
                    state.position += Vector2.one * minorChange;
                    Set(touchControl, state);

                    return;
                }

                // this one is a bit weird, but there's not much to change other than picking the next phase in the list
                if (control is TouchPhaseControl touchPhaseControl)
                {
                    var phase = touchPhaseControl.ReadValue();
                    var values = Enum.GetValues(typeof(TouchPhase));
                    var index = Array.IndexOf(values, phase);
                    var newIndex = (index + 1) % values.Length;
                    Set(touchPhaseControl, (TouchPhase)values.GetValue(newIndex));

                    return;
                }

                if (control is BoneControl boneControl)
                {
                    var bone = boneControl.ReadValue();
                    bone.position += minorChange * Vector3.one;
                    Set(boneControl, bone);

                    return;
                }

                if (control is EyesControl eyesControl)
                {
                    var eyes = eyesControl.ReadValue();
                    eyes.leftEyePosition += minorChange * Vector3.one;
                    Set(eyesControl, eyes);

                    return;
                }
            }

            // Initially I wanted to implement PoseControl too, but it's got a very complicated ifdef that enables it.
            // Didn't want to carry that #ifdef here. Let's see how many people really need to trigger PoseControl.

            // If it's not a control that we know how to trigger - it's not implemented yet
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
            get => runtime.currentTime - runtime.currentTimeOffsetToRealtimeSinceStartup;
            set
            {
                runtime.currentTime = value + runtime.currentTimeOffsetToRealtimeSinceStartup;
                runtime.dontAdvanceTimeNextDynamicUpdate = true;
            }
        }

        internal float unscaledGameTime
        {
            get => runtime.unscaledGameTime;
            set
            {
                runtime.unscaledGameTime = value;
                runtime.dontAdvanceUnscaledGameTimeNextDynamicUpdate = true;
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
                    if (val is float f)
                    {
                        if (!Mathf.Approximately(f, Convert.ToSingle(value)))
                            return false;
                    }
                    else if (val is double d)
                    {
                        if (!Mathf.Approximately((float)d, (float)Convert.ToDouble(value)))
                            return false;
                    }
                    else if (val is Vector2 v2)
                    {
                        if (!Vector2EqualityComparer.Instance.Equals(v2, value.As<Vector2>()))
                            return false;
                    }
                    else if (val is Vector3 v3)
                    {
                        if (!Vector3EqualityComparer.Instance.Equals(v3, value.As<Vector3>()))
                            return false;
                    }
                    else if (!val.Equals(value))
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

        private static void CheckValidity(InputDevice device, InputControl control)
        {
            if (!device.added)
            {
                throw new ArgumentException(
                    $"Device '{device}' has not been added to the system", nameof(device));
            }

            // Guards against a device from another scope being used. This is a direct way to evaluate whether
            // the device is associated with the current manager state or not since device state isn't consistently
            // pushed/popped in the current design.
            var manager = InputSystem.s_Manager;
            if (manager == null || !manager.HasDevice(device))
            {
                throw new ArgumentException($"Control '{control}' does not have any associated state. " +
                    "Make sure the control or device was added after executing Setup().",  nameof(control));
            }
        }

        private static void CheckValidity(InputControl control)
        {
            CheckValidity(control.device, control);
        }

        /// <summary>
        /// Returns true if running inside an Edit Mode test (Editor assembly).
        /// Returns false if running in Play Mode.
        /// </summary>
        private static bool IsEditMode()
        {
            return Application.isEditor && !Application.isPlaying;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Represents an analytics registration event captured by test harness.
        /// </summary>
        protected struct AnalyticsRegistrationEventData
        {
            public AnalyticsRegistrationEventData(string name, int maxPerHour, int maxPropertiesPerEvent)
            {
                this.name = name;
                this.maxPerHour = maxPerHour;
                this.maxPropertiesPerEvent = maxPropertiesPerEvent;
            }

            public readonly string name;
            public readonly int maxPerHour;
            public readonly int maxPropertiesPerEvent;
        }

        /// <summary>
        /// Represents an analytics data event captured by test harness.
        /// </summary>
        protected struct AnalyticsEventData
        {
            public AnalyticsEventData(string name, object data)
            {
                this.name = name;
                this.data = data;
            }

            public readonly string name;
            public readonly object data;
        }

        private List<AnalyticsRegistrationEventData> m_RegisteredAnalytics;
        private List<AnalyticsEventData> m_SentAnalyticsEvents;

        /// <summary>
        /// Returns a read-only list of all analytics events registred by enabling capture via <see cref="CollectAnalytics(System.Predicate{string})"/>.
        /// </summary>
        protected IReadOnlyList<AnalyticsRegistrationEventData> registeredAnalytics => m_RegisteredAnalytics;

        /// <summary>
        /// Returns a read-only list of all analytics events captured by enabling capture via <see cref="CollectAnalytics(System.Predicate{string})"/>.
        /// </summary>
        protected IReadOnlyList<AnalyticsEventData> sentAnalyticsEvents => m_SentAnalyticsEvents;

        /// <summary>
        /// Set up the test fixture to collect analytics registrations and events
        /// </summary>
        /// <param name="analyticsNameFilter">A filter predicate evaluating whether the given analytics name should be accepted to be stored in test fixture.</param>
        protected void CollectAnalytics(Predicate<string> analyticsNameFilter)
        {
            // Make sure containers are initialized and create them if not. Otherwise just clear to avoid allocation.
            if (m_RegisteredAnalytics == null)
                m_RegisteredAnalytics = new List<AnalyticsRegistrationEventData>();
            else
                m_RegisteredAnalytics.Clear();
            if (m_SentAnalyticsEvents == null)
                m_SentAnalyticsEvents = new List<AnalyticsEventData>();
            else
                m_SentAnalyticsEvents.Clear();

            // Store registered analytics when called if filter applies
            runtime.onRegisterAnalyticsEvent = (name, maxPerHour, maxPropertiesPerEvent) =>
            {
                if (analyticsNameFilter(name))
                    m_RegisteredAnalytics.Add(new AnalyticsRegistrationEventData(name: name, maxPerHour: maxPerHour, maxPropertiesPerEvent: maxPropertiesPerEvent));
            };

            // Store sent analytic events when called if filter applies
            runtime.onSendAnalyticsEvent = (name, data) =>
            {
                if (analyticsNameFilter(name))
                    m_SentAnalyticsEvents.Add(new AnalyticsEventData(name: name, data: data));
            };
        }

        /// <summary>
        /// Set up the test fixture to collect filtered analytics registrations and events.
        /// </summary>
        /// <param name="acceptedName">The analytics name to be accepted, all other registrations and data
        /// will be discarded.</param>
        protected void CollectAnalytics(string acceptedName)
        {
            CollectAnalytics((name) => name.Equals(acceptedName));
        }

        /// <summary>
        /// Set up the test fixture to collect ALL analytics registrations and events.
        /// </summary>
        protected void CollectAnalytics()
        {
            CollectAnalytics((_) => true);
        }

        #endif
    }
}
