using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

internal partial class CoreTests
{
    [Test]
    [Category("Actions")]
    public void Actions_WhenTransitionFromOneInteractionToNext_GetCallbacks()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction("test", InputActionType.Button, binding: "<Gamepad>/buttonSouth",
            interactions: "tap(duration=1),slowTap");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Press(gamepad.buttonSouth);

            Assert.That(trace,
                Started<TapInteraction>(action, gamepad.buttonSouth, time: 0));

            trace.Clear();

            // Expire the tap. The system should transitioning from the tap to a slowtap.
            // Note the starting time of the slowTap will be 0 not 2.
            runtime.currentTime = 2;
            InputSystem.Update();

            Assert.That(trace,
                Canceled<TapInteraction>(action, gamepad.buttonSouth)
                    .AndThen(Started<SlowTapInteraction>(action, gamepad.buttonSouth, time: 0)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformPressInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // We add a second input device (and bind to it), to test that the binding
        // conflict resolution will not interfere with the interaction handling.
        InputSystem.AddDevice<Keyboard>();

        // Test all three press behaviors concurrently.
        var pressOnlyAction = new InputAction("PressOnly", binding: "<Gamepad>/buttonSouth", interactions: "press");
        pressOnlyAction.AddBinding("<Keyboard>/a");
        var releaseOnlyAction = new InputAction("ReleaseOnly", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=1)");
        releaseOnlyAction.AddBinding("<Keyboard>/s");
        var pressAndReleaseAction = new InputAction("PressAndRelease", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=2)");
        pressAndReleaseAction.AddBinding("<Keyboard>/d");

        pressOnlyAction.Enable();
        releaseOnlyAction.Enable();
        pressAndReleaseAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            runtime.currentTime = 1;
            Press(gamepad.buttonSouth);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(5));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(releaseOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));

            trace.Clear();

            runtime.currentTime = 2;
            Release(gamepad.buttonSouth);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(3));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(releaseOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(1));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(0));

            trace.Clear();

            runtime.currentTime = 5;
            Press(gamepad.buttonSouth);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(5));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed).And.With.Property("duration")
                    .EqualTo(0));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(releaseOnlyAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Started).And.With.Property("duration")
                    .EqualTo(0));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldInteraction()
    {
        const int timeOffset = 123;
        runtime.currentTimeOffsetToRealtimeSinceStartup = timeOffset;
        runtime.currentTime = 10 + timeOffset;
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var canceledReceivedCalls = 0;
        InputAction canceledAction = null;
        InputControl canceledControl = null;

        var action = new InputAction(binding: "<Gamepad>/{primaryAction}", interactions: "hold(duration=0.4)");
        action.performed +=
            ctx =>
        {
            ++performedReceivedCalls;
            performedAction = ctx.action;
            performedControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(ctx.duration, Is.GreaterThanOrEqualTo(0.4));
        };
        action.started +=
            ctx =>
        {
            ++startedReceivedCalls;
            startedAction = ctx.action;
            startedControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(ctx.duration, Is.EqualTo(0.0));
        };
        action.canceled +=
            ctx =>
        {
            ++canceledReceivedCalls;
            canceledAction = ctx.action;
            canceledControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(ctx.duration, Is.GreaterThan(0.0));
            Assert.That(ctx.duration, Is.LessThan(0.4));
        };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 10.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(canceledReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 10.25);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.Zero);
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(canceledReceivedCalls, Is.EqualTo(1));
        Assert.That(canceledAction, Is.SameAs(action));
        Assert.That(canceledControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));

        canceledReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 10.5);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(canceledReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

        startedReceivedCalls = 0;

        runtime.currentTime = 10.75 + timeOffset;
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.Zero);
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(canceledReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

        runtime.currentTime = 11 + timeOffset;
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.Zero);
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(canceledReceivedCalls, Is.Zero);
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformTapInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", interactions: "tap");
        action.performed +=
            ctx =>
        {
            ++performedReceivedCalls;
            performedAction = ctx.action;
            performedControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
        };
        action.started +=
            ctx =>
        {
            ++startedReceivedCalls;
            startedAction = ctx.action;
            startedControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadButton.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), InputSystem.settings.defaultTapTime);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.buttonSouth));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformDoubleTapInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        runtime.advanceTimeEachDynamicUpdate = 0;

        var action = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "multitap(tapTime=0.5,tapDelay=0.75,tapCount=2)");
        action.Enable();
        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Press button.
            runtime.currentTime = 1;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 1);
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(1).Within(0.00001));

            trace.Clear();

            // Release before tap time and make sure the double tap cancels.
            runtime.currentTime = 12;
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 1.75);
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(1.75).Within(0.00001));

            trace.Clear();

            // Press again and then release before tap time. Should see only the start from
            // the initial press.
            runtime.currentTime = 2.5;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 2);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 2.25);
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(2).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));

            trace.Clear();

            // Wait for longer than tapDelay and make sure we're seeing a cancellation.
            runtime.currentTime = 4;
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(4).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0).Within(0.00001));// Button isn't pressed currently.

            trace.Clear();

            // Now press and release within tap time. Then press again within delay time but release
            // only after tap time. Should we started and canceled.
            runtime.currentTime = 6;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 4.7);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 4.9);
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 5);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 5.9);
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(4.7).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[1].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[1].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[1].time, Is.EqualTo(5.9).Within(0.00001));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(0).Within(0.00001));

            trace.Clear();

            // Finally perform a full, proper double tap cycle.
            runtime.currentTime = 8;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 7);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 7.25);
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 7.5);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 7.75);
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(7).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[1].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[1].time, Is.EqualTo(7.75).Within(0.00001));
            Assert.That(actions[1].ReadValue<float>(), Is.Zero.Within(0.00001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCustomizeButtonPressPointsOfInteractions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var pressAction = new InputAction("PressAction", binding: "<Gamepad>/leftTrigger", interactions: "press(pressPoint=0.234)");
        var tapAction = new InputAction("TapAction", binding: "<Gamepad>/leftTrigger", interactions: "tap(pressPoint=0.345)");
        var slowTapAction = new InputAction("SlowTapAction", binding: "<Gamepad>/leftTrigger", interactions: "slowtap(pressPoint=0.456)");
        var multiTapAction = new InputAction("MultiTapAction", binding: "<Gamepad>/leftTrigger", interactions: "multitap(pressPoint=0.567)");
        var holdAction = new InputAction("HoldAction", binding: "<Gamepad>/leftTrigger", interactions: "hold(pressPoint=0.678)");

        pressAction.Enable();
        tapAction.Enable();
        slowTapAction.Enable();
        multiTapAction.Enable();
        holdAction.Enable();

        // Render the global default inactive.
        InputSystem.settings.defaultButtonPressPoint = 0;

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            Set(gamepad.leftTrigger, 0.123f);

            Assert.That(trace, Is.Empty);

            Set(gamepad.leftTrigger, 0.3f);

            Assert.That(trace, Started<PressInteraction>(pressAction).AndThen(Performed<PressInteraction>(pressAction)));

            trace.Clear();

            Set(gamepad.leftTrigger, 0.4f);

            Assert.That(trace, Started<TapInteraction>(tapAction));

            trace.Clear();

            Set(gamepad.leftTrigger, 0.5f);

            Assert.That(trace, Started<SlowTapInteraction>(slowTapAction));

            trace.Clear();

            Set(gamepad.leftTrigger, 0.6f);

            Assert.That(trace, Started<MultiTapInteraction>(multiTapAction));

            trace.Clear();

            Set(gamepad.leftTrigger, 0.7f);

            Assert.That(trace, Started<HoldInteraction>(holdAction));
        }
    }
}
