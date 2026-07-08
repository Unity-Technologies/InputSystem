using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

// Covers IActuationPressPoint, defaultButtonPressPoint, InputAction.IsPressed, and InputControl.IsPressed().
namespace Tests.InputSystem
{
    [TestFixture]
    [Category("ActuationPressPoint")]
    internal class ActuationPressPointTests : CoreTestsFixture
    {
        #region InputAction.IsPressed (Vector2 + Press interaction / control pressPoint)

        [Test]
        [Category("Actions")]
        public void Actions_Vector2IsPressed_UsesPressInteractionPressPoint()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;
            UnityEngine.InputSystem.InputSystem.settings.buttonReleaseThreshold = 0.8f;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, Vector2.zero);
            UnityEngine.InputSystem.InputSystem.Update();

            var action = new InputAction(
                type: InputActionType.Value,
                expectedControlType: "Vector2",
                binding: "<Gamepad>/leftStick",
                interactions: "press(pressPoint=0.6)");
            action.Enable();

            Set(gamepad.leftStick, new Vector2(0.55f, 0f));
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.False);

            Set(gamepad.leftStick, new Vector2(0f, 0.65f));
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.True);
        }

        [Test]
        [Category("Actions")]
        public void Actions_ButtonIsPressed_UsesPressInteractionWhenControlPressPointUnset()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;
            UnityEngine.InputSystem.InputSystem.settings.buttonReleaseThreshold = 0.8f;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            var action = new InputAction(
                type: InputActionType.Button,
                binding: "<Gamepad>/leftTrigger",
                interactions: "press(pressPoint=0.6)");
            action.Enable();

            Set(gamepad.leftTrigger, 0.55f);
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.False);

            Set(gamepad.leftTrigger, 0.65f);
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.True);
        }

        [Test]
        [Category("Actions")]
        public void Actions_CompositeVector2IsPressed_UsesPressInteractionOnCompositeBinding()
        {
            // Interaction lives on the composite binding while state changes arrive on part bindings.
            // Analog 2DVector + stick half-axes yields composite magnitudes between 0 and 1 so we can
            // distinguish defaultButtonPressPoint (0.5) from press(pressPoint=0.6).
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;
            UnityEngine.InputSystem.InputSystem.settings.buttonReleaseThreshold = 0.8f;
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMin = 0;
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMax = 1;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, Vector2.zero);
            UnityEngine.InputSystem.InputSystem.Update();

            var action = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");
            action.AddCompositeBinding("2DVector(mode=2)", interactions: "press(pressPoint=0.6)")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");
            action.Enable();

            Set(gamepad.leftStick, new Vector2(0f, 0.55f));
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.False);

            Set(gamepad.leftStick, new Vector2(0f, 0.65f));
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(action.IsPressed(), Is.True);
        }

        [Test]
        [Category("Actions")]
        public void Actions_Vector2Composite_RespectsButtonPressurePoint()
        {
            // The stick has deadzones on the up/down/left/right buttons to get rid of stick
            // noise. For this test, simplify things by getting rid of deadzones.
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMin = 0;
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMax = 1;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            // Set up classic WASD control.
            var action = new InputAction();
            action.AddCompositeBinding("Dpad")
                .With("Up", "<Gamepad>/leftstick/up")
                .With("Down", "<Gamepad>/leftstick/down")
                .With("Left", "<Gamepad>/leftstick/left")
                .With("Right", "<Gamepad>/leftstick/right");
            action.Enable();

            Vector2? value = null;
            action.performed += ctx => { value = ctx.ReadValue<Vector2>(); };
            action.canceled += ctx => { value = ctx.ReadValue<Vector2>(); };

            var pressPoint = gamepad.leftStick.up.pressPointOrDefault;

            // Up.
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value, Is.EqualTo(Vector2.up));

            // Up (slightly above press point)
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value, Is.EqualTo(Vector2.up));

            // Up (slightly below press point)
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value, Is.EqualTo(Vector2.zero));

            // Up left.
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up + Vector2.left });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
            Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

            // Up left (up slightly above press point)
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad,
                new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f + Vector2.left });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
            Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

            // Up left (up slightly below press point)
            value = null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(gamepad,
                new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f + Vector2.left });
            UnityEngine.InputSystem.InputSystem.Update();

            Assert.That(value, Is.Not.Null);
            Assert.That(value.Value, Is.EqualTo(Vector2.left));
        }

        #endregion

        #region Control extension IsPressed + settings (IActuationPressPoint / default threshold)

        [Test]
        [Category("Controls")]
        public void Controls_CanDetermineIfControlIsPressed()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, Vector2.one);
            Set(gamepad.leftTrigger, 0.6f);
            Press(gamepad.buttonSouth);

            Assert.That(gamepad.leftTrigger.IsPressed(), Is.True);
            Assert.That(gamepad.rightTrigger.IsPressed(), Is.False);
            Assert.That(gamepad.buttonSouth.IsPressed(), Is.True);
            Assert.That(gamepad.buttonNorth.IsPressed(), Is.False);
            Assert.That(gamepad.leftStick.IsPressed(),
                Is.True); // Note how this diverges from the actual meaning of "is the left stick pressed?"
            Assert.That(gamepad.rightStick.IsPressed(), Is.False);

            // https://fogbugz.unity3d.com/f/cases/1374024/
            // Calling it on the entire device should be false.
            Assert.That(gamepad.IsPressed(), Is.False);
        }

        [Test]
        [Category("Controls")]
        public void Controls_CanCustomizeDefaultButtonPressPoint()
        {
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.4f;

            Set(gamepad.leftTrigger, 0.39f);

            Assert.That(gamepad.leftTrigger.isPressed, Is.False);

            Set(gamepad.leftTrigger, 0.4f);

            Assert.That(gamepad.leftTrigger.isPressed, Is.True);

            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;

            Assert.That(gamepad.leftTrigger.isPressed, Is.False);

            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0;

            Assert.That(gamepad.leftTrigger.isPressed, Is.True);

            // Setting the trigger to 0 requires the system to be "smart" enough to
            // figure out that 0 as a default button press point doesn't make sense
            // and that instead the press point should clamp off at some low, non-zero value.
            // https://fogbugz.unity3d.com/f/cases/1349002/
            Set(gamepad.leftTrigger, 0f);

            Assert.That(gamepad.leftTrigger.isPressed, Is.False);

            Set(gamepad.leftTrigger, 0.001f);

            Assert.That(gamepad.leftTrigger.isPressed, Is.True);

            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = -1;
            Set(gamepad.leftTrigger, 0f);

            Assert.That(gamepad.leftTrigger.isPressed, Is.False);
        }

        [Test]
        [Category("Controls")]
        public void Controls_CanCustomizePressPointOfGamepadTriggers()
        {
            var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""pressPoint=0.2""
                    }
                ]
            }
        ";

            UnityEngine.InputSystem.InputSystem.RegisterLayout(json);
            var gamepad = InputDevice.Build<Gamepad>("CustomGamepad");

            Assert.That(gamepad.rightTrigger.pressPoint, Is.EqualTo(0.2f).Within(0.0001f));
        }

        #endregion

        #region Performance (GetActuationPressThreshold vs legacy resolution)

        // Run these with Window > Analysis > Test Report (Performance) or test-framework-performance.
        // Sample groups use the default time unit (milliseconds) in the performance report.
        // Each test uses MeasurementCount(1000) for comparable sample counts across runs.
        // GetActuationPressThreshold.* samples only exist on branches that include that API; Legacy.*
        // mirror develop's control-only resolution (no binding interaction scan) for side-by-side timing.

        // Matches InputActionState.ProcessButtonState on develop (before actuation press alignment work):
        // press point from ButtonControl when control is flagged as a button, otherwise global default.
        private static float LegacyProcessButtonStateStylePressPoint(InputControl control)
        {
            return control.isButton
                ? ((ButtonControl)control).pressPointOrDefault
                : ButtonControl.s_GlobalDefaultButtonPressPoint;
        }

        // Matches InputActionState.ProcessDefaultInteraction button branches on develop:
        // concrete ButtonControl pattern, else global default (Vector2 / stick used this path too).
        private static float LegacyDefaultInteractionButtonStylePressPoint(InputControl control)
        {
            return control is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
        }

        private static unsafe bool TryGetStateBindingForControl(InputAction action, InputControl control,
            out InputActionState state, out int bindingIndexInState)
        {
            state = null;
            bindingIndexInState = -1;

            var map = action.GetOrCreateActionMap();
            map.ResolveBindingsIfNecessary();
            state = map.m_State;
            if (state == null)
                return false;

            var actionIndex = action.m_ActionIndexInState;
            for (var i = 0; i < state.totalControlCount; ++i)
            {
                if (state.controls[i] != control)
                    continue;
                var bindingIndex = state.controlIndexToBindingIndex[i];
                if (state.bindingStates[bindingIndex].actionIndex != actionIndex)
                    continue;
                bindingIndexInState = bindingIndex;
                return true;
            }

            return false;
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public unsafe void Performance_GetActuationPressThreshold_GamepadButton_NoInteractions()
        {
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();
            var action = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
            action.Enable();
            Assert.That(TryGetStateBindingForControl(action, gamepad.buttonSouth, out var state, out var bindingIndex),
                Is.True);

            Measure.Method(() =>
            {
                var bindingPtr = &state.bindingStates[bindingIndex];
                _ = state.GetActuationPressThreshold(gamepad.buttonSouth, bindingPtr);
            })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("GetActuationPressThreshold.Button.NoInteractions")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public void Performance_Legacy_ProcessButtonStateStyle_GamepadButton()
        {
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Measure.Method(() => { _ = LegacyProcessButtonStateStylePressPoint(gamepad.buttonSouth); })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("Legacy.ProcessButtonStateStyle.Button")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public void Performance_Legacy_DefaultInteractionStyle_GamepadButton()
        {
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Measure.Method(() => { _ = LegacyDefaultInteractionButtonStylePressPoint(gamepad.buttonSouth); })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("Legacy.DefaultInteractionStyle.Button")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public unsafe void Performance_GetActuationPressThreshold_GamepadStick_NoInteractions()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();
            var action = new InputAction(
                type: InputActionType.Value,
                expectedControlType: "Vector2",
                binding: "<Gamepad>/leftStick");
            action.Enable();
            Assert.That(TryGetStateBindingForControl(action, gamepad.leftStick, out var state, out var bindingIndex),
                Is.True);

            Measure.Method(() =>
            {
                var bindingPtr = &state.bindingStates[bindingIndex];
                _ = state.GetActuationPressThreshold(gamepad.leftStick, bindingPtr);
            })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("GetActuationPressThreshold.Vector2.NoInteractions")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public void Performance_Legacy_ProcessButtonStateStyle_GamepadStick()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Measure.Method(() => { _ = LegacyProcessButtonStateStylePressPoint(gamepad.leftStick); })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("Legacy.ProcessButtonStateStyle.Vector2")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public unsafe void Performance_GetActuationPressThreshold_TriggerWithPressInteraction_ScansInteractions()
        {
            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();
            var action = new InputAction(
                type: InputActionType.Button,
                binding: "<Gamepad>/leftTrigger",
                interactions: "press(pressPoint=0.65),SlowTap(duration=0.4)");
            action.Enable();
            Assert.That(TryGetStateBindingForControl(action, gamepad.leftTrigger, out var state, out var bindingIndex),
                Is.True);

            Measure.Method(() =>
            {
                var bindingPtr = &state.bindingStates[bindingIndex];
                _ = state.GetActuationPressThreshold(gamepad.leftTrigger, bindingPtr);
            })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("GetActuationPressThreshold.WithInteractions.Scan")
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("ActuationPressPoint")]
        public void Performance_Legacy_DefaultInteractionStyle_GamepadTrigger()
        {
            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            Measure.Method(() => { _ = LegacyDefaultInteractionButtonStylePressPoint(gamepad.leftTrigger); })
                .MeasurementCount(1000)
                .WarmupCount(5)
                .SampleGroup("Legacy.DefaultInteractionStyle.Axis")
                .Run();
        }

        #endregion

        #region End-to-end allocation (press threshold resolution during steady-state updates)

        /// <summary>
        /// After warm-up, repeatedly queues gamepad state and runs <see cref="InputSystem.Update"/> while two
        /// enabled actions exercise press-threshold resolution (interaction scan on an axis-as-button binding
        /// and composite binding redirect for <c>GetActuationPressThreshold</c>). The steady-state path must
        /// not allocate GC memory.
        /// </summary>
        [Test]
        [Category("ActuationPressPoint")]
        [Retry(2)] // Warm up JIT.
        public void Actions_EndToEndGamepadUpdates_WithPressInteractionBindings_DoNotAllocateGCMemory()
        {
            UnityEngine.InputSystem.InputSystem.actions?.Disable();
            InputActionState.DestroyAllActionMapStates();

            UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint = 0.5f;
            UnityEngine.InputSystem.InputSystem.settings.buttonReleaseThreshold = 0.8f;
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMin = 0;
            UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMax = 1;

            var gamepad = UnityEngine.InputSystem.InputSystem.AddDevice<Gamepad>();

            var triggerAction = new InputAction(
                type: InputActionType.Button,
                binding: "<Gamepad>/leftTrigger",
                interactions: "press(pressPoint=0.65),SlowTap(duration=0.4)");

            var compositeAction = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");
            compositeAction.AddCompositeBinding("2DVector(mode=2)", interactions: "press(pressPoint=0.6)")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");

            triggerAction.Enable();
            compositeAction.Enable();

            Set(gamepad.leftTrigger, 0f);
            Set(gamepad.leftStick, Vector2.zero);
            UnityEngine.InputSystem.InputSystem.Update();

            const int kIterations = 32;
            var kProfilerRegion = "Actions_EndToEndGamepadUpdates_WithPressInteractionBindings_DoNotAllocateGCMemory";

            // Warm up the same path we measure (JIT, internal caches, first-update effects).
            for (var w = 0; w < 2; ++w)
            {
                Profiler.BeginSample(kProfilerRegion);
                for (var i = 0; i < kIterations; ++i)
                {
                    Set(gamepad.leftTrigger, 0f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftTrigger, 1f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftTrigger, 0f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, new Vector2(0f, 0.55f));
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, new Vector2(0f, 0.65f));
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, Vector2.zero);
                    UnityEngine.InputSystem.InputSystem.Update();
                }

                Profiler.EndSample();
            }

            Assert.That(() =>
            {
                Profiler.BeginSample(kProfilerRegion);
                for (var i = 0; i < kIterations; ++i)
                {
                    Set(gamepad.leftTrigger, 0f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftTrigger, 1f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftTrigger, 0f);
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, new Vector2(0f, 0.55f));
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, new Vector2(0f, 0.65f));
                    UnityEngine.InputSystem.InputSystem.Update();

                    Set(gamepad.leftStick, Vector2.zero);
                    UnityEngine.InputSystem.InputSystem.Update();
                }

                Profiler.EndSample();
            }, Is.Not.AllocatingGCMemory());
        }

        #endregion
    }
}
