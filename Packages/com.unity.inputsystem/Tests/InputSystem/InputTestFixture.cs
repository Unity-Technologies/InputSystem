using System;
using System.Linq;
using UnityEngine.Experimental.Input.Controls;
using NUnit.Framework;
using UnityEngine.Animations;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Editor;
#endif

////TODO: when running tests in players, make sure that remoting is turned off

namespace UnityEngine.Experimental.Input
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
    ///         InputSystem.RegisterLayout<MyDevice>();
    ///     }
    ///
    ///     [Test]
    ///     public void CanCreateMyDevice()
    ///     {
    ///         InputSystem.AddDevice<MyDevice>();
    ///         Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<MyDevice>());
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

                testRuntime = new InputTestRuntime();

                // Push current input system state on stack.
                InputSystem.SaveAndReset(enableRemoting: false, runtime: testRuntime);

                #if UNITY_EDITOR
                // Make sure we're not affected by the user giving focus away from the
                // game view.
                InputConfiguration.LockInputToGame = true;
                #endif
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
                testRuntime.Dispose();

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

        public void AssertButtonPress<TState>(InputDevice device, TState state, params ButtonControl[] buttons)
            where TState : struct, IInputStateTypeInfo
        {
            // Update state.
            InputSystem.QueueStateEvent(device, state);
            InputSystem.Update();

            // Now verify that only the buttons we expect to be pressed are pressed.
            foreach (var control in device.allControls)
            {
                var controlAsButton = control as ButtonControl;
                if (controlAsButton == null)
                    continue;

                var isInList = buttons.Contains(controlAsButton);
                if (!isInList)
                    Assert.That(controlAsButton.isPressed, Is.False,
                        string.Format("Expected button {0} to NOT be pressed", controlAsButton));
                else
                    Assert.That(controlAsButton.isPressed, Is.True,
                        string.Format("Expected button {0} to be pressed", controlAsButton));
            }
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
                throw new ArgumentNullException("action");

            if (!action.enabled)
                throw new ArgumentException(
                    string.Format("Action '{0}' must be enabled in order to be able to trigger it", action), "action");

            var controls = action.controls;
            if (controls.Count == 0)
                throw new ArgumentException(
                    string.Format("Action '{0}' must be bound to controls in order to be able to trigger it", action), "action");

            // See if we have a button we can trigger.
            for (var i = 0; i < controls.Count; ++i)
            {
                var button = controls[i] as ButtonControl;
                if (button == null)
                    continue;

                // We do, so flip its state and we're done.
                var device = button.device;
                InputEventPtr inputEvent;
                using (StateEvent.From(device, out inputEvent))
                {
                    button.WriteValueInto(inputEvent, button.isPressed ? 0 : 1);
                    InputSystem.QueueEvent(inputEvent);
                    InputSystem.Update();
                }

                return;
            }

            // See if we have an axis we can slide a bit.
            for (var i = 0; i < controls.Count; ++i)
            {
                var axis = controls[i] as AxisControl;
                if (axis == null)
                    continue;

                // We do, so nudge its value a bit.
                var device = axis.device;
                InputEventPtr inputEvent;
                using (StateEvent.From(device, out inputEvent))
                {
                    var currentValue = axis.ReadValue();
                    var newValue = currentValue + 0.01f;

                    if (axis.clamp && newValue > axis.clampMax)
                        newValue = axis.clampMin;

                    axis.WriteValueInto(inputEvent, newValue);
                    InputSystem.QueueEvent(inputEvent);
                    InputSystem.Update();
                }

                return;
            }

            ////TODO: support a wider range of controls
            throw new NotImplementedException();
        }

        /// <summary>
        /// The input runtime used during testing.
        /// </summary>
        public InputTestRuntime testRuntime { get; private set; }
    }
}
