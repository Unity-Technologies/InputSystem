using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

internal partial class CoreTests
{
    [Preserve]
    class InteractionThatOnlyPerforms : IInputInteraction<float>
    {
        // Get rid of unused field warning.
        #pragma warning disable CS0649
        public bool stayPerformed;
        #pragma warning restore CS0649

        public void Process(ref InputInteractionContext context)
        {
            if (context.ControlIsActuated())
            {
                if (stayPerformed)
                    context.PerformedAndStayPerformed();
                else
                    context.Performed();
            }
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartedAndCanceledAreEnforcedImplicitly()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterInteraction<InteractionThatOnlyPerforms>();

        var action1 = new InputAction(name: "action1", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth", interactions: "interactionThatOnlyPerforms(stayPerformed=true)");
        var action2 = new InputAction(name: "action2", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth", interactions: "interactionThatOnlyPerforms(stayPerformed=false)");
        var action3 = new InputAction(name: "action3", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
        var action4 = new InputAction(name: "action4", type: InputActionType.Value, binding: "<Gamepad>/buttonSouth");

        // Pass-Through is special (as always).
        var action5 = new InputAction(name: "action5", type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");
        var action6 = new InputAction(name: "action6", type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth", interactions: "press");

        action1.Enable();
        action2.Enable();
        action3.Enable();
        action4.Enable();
        action5.Enable();
        action6.Enable();

        using (var trace1 = new InputActionTrace(action1))
        using (var trace2 = new InputActionTrace(action2))
        using (var trace3 = new InputActionTrace(action3))
        using (var trace4 = new InputActionTrace(action4))
        using (var trace5 = new InputActionTrace(action5))
        using (var trace6 = new InputActionTrace(action6))
        {
            Press(gamepad.buttonSouth);

            Assert.That(trace1, Started(action1).AndThen(Performed(action1)));
            Assert.That(trace2, Started(action2).AndThen(Performed(action2)).AndThen(Canceled(action2)));
            Assert.That(trace3, Started(action3).AndThen(Performed(action3)));
            Assert.That(trace4, Started(action4).AndThen(Performed(action4)));
            Assert.That(trace5, Performed(action5));
            Assert.That(trace6, Started(action6).AndThen(Performed(action6)));

            trace1.Clear();
            trace2.Clear();
            trace3.Clear();
            trace4.Clear();
            trace5.Clear();
            trace6.Clear();

            Release(gamepad.buttonSouth);

            Assert.That(trace1, Is.Empty);
            Assert.That(trace2, Is.Empty);
            Assert.That(trace3, Canceled(action3));
            Assert.That(trace4, Canceled(action4));
            Assert.That(trace5, Performed(action5)); // Any value change performs.
            Assert.That(trace6, Canceled(action6));
        }
    }

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
        var pressOnlyAction = new InputAction("PressOnly", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=0)");
        pressOnlyAction.AddBinding("<Keyboard>/a");
        var releaseOnlyAction = new InputAction("ReleaseOnly", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=1)");
        releaseOnlyAction.AddBinding("<Keyboard>/s");
        var pressAndReleaseAction = new InputAction("PressAndRelease", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=2)");
        pressAndReleaseAction.AddBinding("<Keyboard>/d");

        pressOnlyAction.Enable();
        releaseOnlyAction.Enable();
        pressAndReleaseAction.Enable();

        using (var pressOnly = new InputActionTrace(pressOnlyAction))
        using (var releaseOnly = new InputActionTrace(releaseOnlyAction))
        using (var pressAndRelease = new InputActionTrace(pressAndReleaseAction))
        {
            runtime.currentTime = 1;
            Press(gamepad.buttonSouth);

            Assert.That(pressOnly,
                Started<PressInteraction>(pressOnlyAction, gamepad.buttonSouth, value: 1.0, time: 1)
                    .AndThen(Performed<PressInteraction>(pressOnlyAction, gamepad.buttonSouth, time: 1, duration: 0, value: 1.0)));
            Assert.That(releaseOnly, Started<PressInteraction>(releaseOnlyAction, gamepad.buttonSouth, time: 1, value: 1.0));
            Assert.That(pressAndRelease,
                Started<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, time: 1, value: 1.0)
                    .AndThen(Performed<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, time: 1, duration: 0, value: 1.0)));

            pressOnly.Clear();
            releaseOnly.Clear();
            pressAndRelease.Clear();

            runtime.currentTime = 2;
            Release(gamepad.buttonSouth);

            Assert.That(pressOnly, Canceled<PressInteraction>(pressOnlyAction, gamepad.buttonSouth, value: 0.0, time: 2, duration: 1));
            Assert.That(releaseOnly,
                Performed<PressInteraction>(releaseOnlyAction, gamepad.buttonSouth, value: 0.0, time: 2, duration: 1)
                    .AndThen(Canceled<PressInteraction>(releaseOnlyAction, gamepad.buttonSouth, value: 0.0, time: 2, duration: 1)));
            Assert.That(pressAndRelease,
                Performed<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, value: 0.0, time: 2, duration: 1)
                    .AndThen(Canceled<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, value: 0.0, time: 2, duration: 1)));

            pressOnly.Clear();
            releaseOnly.Clear();
            pressAndRelease.Clear();

            runtime.currentTime = 5;
            Press(gamepad.buttonSouth);

            Assert.That(pressOnly,
                Started<PressInteraction>(pressOnlyAction, gamepad.buttonSouth, value: 1.0, time: 5)
                    .AndThen(Performed<PressInteraction>(pressOnlyAction, gamepad.buttonSouth, time: 5, duration: 0, value: 1.0)));
            Assert.That(releaseOnly, Started<PressInteraction>(releaseOnlyAction, gamepad.buttonSouth, time: 5, value: 1.0));
            Assert.That(pressAndRelease,
                Started<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, time: 5, value: 1.0)
                    .AndThen(Performed<PressInteraction>(pressAndReleaseAction, gamepad.buttonSouth, time: 5, duration: 0, value: 1.0)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/{primaryAction}", interactions: "hold(duration=0.4)");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            // Press.
            Press(gamepad.buttonSouth, time: 10);

            Assert.That(trace, Started<HoldInteraction>(action, gamepad.buttonSouth, time: 10, value: 1.0));
            Assert.That(action.ReadValue<float>(), Is.EqualTo(1));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

            trace.Clear();

            // Release in less than hold time.
            Release(gamepad.buttonSouth, time: 10.25);

            Assert.That(trace, Canceled<HoldInteraction>(action, gamepad.buttonSouth, duration: 0.25, time: 10.25, value: 0.0));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
            Assert.That(action.ReadValue<float>(), Is.Zero);

            trace.Clear();

            // Press again.
            Press(gamepad.buttonSouth, time: 10.5);

            Assert.That(trace, Started<HoldInteraction>(action, gamepad.buttonSouth, time: 10.5, value: 1.0));
            Assert.That(action.ReadValue<float>(), Is.EqualTo(1));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

            trace.Clear();

            // Let time pass but stay under hold time.
            currentTime = 10.75;
            InputSystem.Update();

            Assert.That(trace, Is.Empty);

            // Now exceed hold time. Make sure action performs and *stays* performed.
            currentTime = 11;
            InputSystem.Update();

            Assert.That(trace,
                Performed<HoldInteraction>(action, gamepad.buttonSouth, time: 11, duration: 0.5, value: 1.0));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(action.ReadValue<float>(), Is.EqualTo(1));

            trace.Clear();

            // Release button.
            Release(gamepad.buttonSouth, time: 11.5);

            Assert.That(trace, Canceled<HoldInteraction>(action, gamepad.buttonSouth, time: 11.5, duration: 1, value: 0.0));
        }
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

    [Preserve]
    private class CancelingTestInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            if (context.ControlIsActuated())
            {
                context.Performed();
                context.Canceled();
            }
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_ValueIsDefaultWhenActionIsCanceled()
    {
        InputSystem.RegisterInteraction<CancelingTestInteraction>();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction("test", binding: "<Gamepad>/leftTrigger", interactions: "CancelingTest");
        var performedCount = 0;
        float performedValue = -1;
        action.performed += ctx =>
        {
            performedValue = ctx.ReadValue<float>();
            performedCount++;
        };

        var canceledCount = 0;
        float canceledValue = -1;
        action.canceled += ctx =>
        {
            canceledValue = ctx.ReadValue<float>();
            canceledCount++;
        };

        action.Enable();

        Set(gamepad.leftTrigger, 1.0f);

        Assert.That(performedCount, Is.EqualTo(1));
        Assert.That(performedValue, Is.EqualTo(1.0));

        Assert.That(canceledCount, Is.EqualTo(1));
        Assert.That(canceledValue, Is.EqualTo(0.0));
    }
}
