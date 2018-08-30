using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.Controls;
using NUnit.Framework;
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

        /// <summary>
        /// The input runtime used during testing.
        /// </summary>
        public InputTestRuntime testRuntime { get; private set; }

        private Vector3Comparer m_Vector3Comparer;
        public Vector3Comparer vector3Comparer
        {
            get
            {
                if (m_Vector3Comparer == null)
                    m_Vector3Comparer = new Vector3Comparer();
                return m_Vector3Comparer;
            }
        }

        private Vector2Comparer m_Vector2Comparer;
        public Vector2Comparer vector2Comparer
        {
            get
            {
                if (m_Vector2Comparer == null)
                    m_Vector2Comparer = new Vector2Comparer();
                return m_Vector2Comparer;
            }
        }

        public class Vector3Comparer : IComparer<Vector3>
        {
            private float m_Epsilon;

            public Vector3Comparer(float epsilon = 0.0001f)
            {
                m_Epsilon = epsilon;
            }

            public int Compare(Vector3 a, Vector3 b)
            {
                return Math.Abs(a.x - b.x) < m_Epsilon && Math.Abs(a.y - b.y) < m_Epsilon && Math.Abs(a.z - b.z) < m_Epsilon ? 0 : 1;
            }
        }

        public class Vector2Comparer : IComparer<Vector2>
        {
            private float m_Epsilon;

            public Vector2Comparer(float epsilon = 0.0001f)
            {
                m_Epsilon = epsilon;
            }

            public int Compare(Vector2 a, Vector2 b)
            {
                return Math.Abs(a.x - b.x) < m_Epsilon && Math.Abs(a.y - b.y) < m_Epsilon ? 0 : 1;
            }
        }
    }
}
