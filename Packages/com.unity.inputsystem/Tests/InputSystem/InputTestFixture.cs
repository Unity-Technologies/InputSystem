using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Collections;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////TODO: must allow running UnityTests which means we have to be able to get per-frame updates yet not receive input from native

////TODO: when running tests in players, make sure that remoting is turned off

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
    /// </remarks>
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
    public class InputTestFixture
    {
        /// <summary>
        /// Put InputSystem into a known state where it only has a basic set of
        /// layouts and does not have any input devices.
        /// </summary>
        /// <remarks>
        /// If you derive your own test fixture directly from InputTestFixture, this
        /// method will automatically be called. If you embed InputTestFixture into
        /// your fixture, you have to explicitly call this method yourself.
        /// </remarks>
        [SetUp]
        public virtual void Setup()
        {
            try
            {
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

                var testProperties = TestContext.CurrentContext.Test.Properties;
                if (testProperties.ContainsKey("TimesliceEvents") && testProperties["TimesliceEvents"][0].Equals("Off"))
                    InputSystem.settings.timesliceEvents = false;

                // We use native collections in a couple places. We when leak them, we want to know where exactly
                // the allocation came from so enable full leak detection in tests.
                NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to set up input system for test " + TestContext.CurrentContext.Test.Name);
                Debug.LogException(exception);
                throw exception;
            }

            if (InputSystem.devices.Count > 0)
                Assert.Fail("Input system should not have devices after reset");
        }

        /// <summary>
        /// Restore the state of the input system it had when the test was started.
        /// </summary>
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
                throw exception;
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

        public ActionConstraint Started(InputAction action, InputControl control = null)
        {
            return new ActionConstraint(InputActionPhase.Started, action, control);
        }

        public ActionConstraint Performed(InputAction action, InputControl control = null)
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control);
        }

        public ActionConstraint Canceled(InputAction action, InputControl control = null)
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control);
        }

        public ActionConstraint Started<TInteraction>(InputAction action, InputControl control = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Started, action, control, interaction: typeof(TInteraction));
        }

        public ActionConstraint Performed<TInteraction>(InputAction action, InputControl control = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Performed, action, control, interaction: typeof(TInteraction));
        }

        public ActionConstraint Canceled<TInteraction>(InputAction action, InputControl control = null)
            where TInteraction : IInputInteraction
        {
            return new ActionConstraint(InputActionPhase.Canceled, action, control, interaction: typeof(TInteraction));
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void Press(ButtonControl button, double absoluteTime = -1, double timeOffset = 0)
        {
            Set(button, 1, absoluteTime, timeOffset);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void Release(ButtonControl button, double absoluteTime = -1, double timeOffset = 0)
        {
            Set(button, 0, absoluteTime, timeOffset);
        }

        public void PressAndRelease(ButtonControl button, double absoluteTime = -1, double timeOffset = 0)
        {
            Press(button, absoluteTime, timeOffset);
            Release(button, absoluteTime, timeOffset);
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
        public void Set<TValue>(InputControl<TValue> control, TValue state, double absoluteTime = -1, double timeOffset = 0)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!control.device.added)
                throw new ArgumentException(
                    $"Device of control '{control}' has not been added to the system", nameof(control));

            using (StateEvent.From(control.device, out var eventPtr))
            {
                ////REVIEW: should we by default take the time from the device here?
                if (absoluteTime >= 0)
                    eventPtr.time = absoluteTime;
                eventPtr.time += timeOffset;
                control.WriteValueIntoEvent(state, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }

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
        public InputTestRuntime runtime { get; private set; }

        public class ActionConstraint : Constraint
        {
            public InputActionPhase phase { get; set; }
            public InputAction action { get; set; }
            public InputControl control { get; set; }
            public object value { get; set; }
            public Type interaction { get; set; }

            private readonly List<ActionConstraint> m_AndThen = new List<ActionConstraint>();

            public ActionConstraint(InputActionPhase phase, InputAction action, InputControl control, object value = null, Type interaction = null)
            {
                this.phase = phase;
                this.action = action;
                this.control = control;
                this.value = value;
                this.interaction = interaction;

                var interactionText = string.Empty;
                if (interaction != null)
                    interactionText = $"{InputInteraction.s_Interactions.FindNameForType(interaction).ToLower()} of ";

                Description = $"{phase} {interactionText}'{action}'";
                if (control != null)
                    Description += $" from '{control}'";
                if (value != null)
                    Description += $" with value {value}";

                foreach (var constraint in m_AndThen)
                {
                    Description += " and\n";
                    Description += constraint.Description;
                }
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
                    if (!constraint.Verify(actions[i]))
                        return new ConstraintResult(this, actual, false);
                    ++i;
                }

                if (i != actions.Length)
                    return new ConstraintResult(this, actual, false);

                return new ConstraintResult(this, actual, true);
            }

            private bool Verify(InputActionTrace.ActionEventPtr eventPtr)
            {
                if (eventPtr.action != action ||
                    eventPtr.phase != phase)
                    return false;

                // Check control.
                if (control != null && eventPtr.control != control)
                    return false;

                // Check interaction.
                if (interaction != null && (eventPtr.interaction == null ||
                                            !interaction.IsInstanceOfType(eventPtr.interaction)))
                    return false;

                // Check value.
                if (value != null && !value.Equals(eventPtr.control.ReadValueAsObject()))
                    return false;

                return true;
            }

            public ActionConstraint AndThen(ActionConstraint constraint)
            {
                m_AndThen.Add(constraint);
                return this;
            }
        }
    }
}
