using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

#if UNITY_2018_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
#endif

partial class CoreTests
{
    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSingleControl()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftStick");

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetMultipleControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/*stick");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightStick));

        // Make sure that if we actuate both controls in a single event, we still
        // get them to come through as two separate triggers.

        var receivedControls = new List<InputControl>();
        action.performed += ctx => { receivedControls.Add(ctx.control); };

        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.left, rightStick = Vector2.right});
        InputSystem.Update();

        Assert.That(receivedControls, Has.Count.EqualTo(2));
        Assert.That(receivedControls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(receivedControls, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_WhenSeveralBindingsResolveToSameControl_ThenWhatDoWeDoXXX()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanLayerMapsOnTopOfEachOther()
    {
        // Make up a layered control scheme three levels deep.
        var fpsControls = new InputActionMap("fpsControls");
        var moveAction = fpsControls.AddAction("move");
        var shootAction = fpsControls.AddAction("shoot");
        var lookAction = fpsControls.AddAction("look");
        var sniperControls = new InputActionMap("sniper", extend: fpsControls);
        var scopeAction = sniperControls.AddAction("scope");
        var swapScopeControls = new InputActionMap("swapScope", extend: sniperControls);
        /*
        swapScopeControls.AddBinding();

        // Information from 'baseMap' coming through on 'derivedMap'.
        Assert.That(sniperControls.actions, Has.Count.EqualTo(1));
        Assert.That(sniperControls["action"], Is.TypeOf<InputAction>());
        Assert.That(sniperControls["action"], Is.Not.SameAs(baseAction));
        Assert.That(sniperControls["action"].actionMap, Is.SameAs(derivedMap));
        Assert.That(sniperControls["action"].bindings, Has.Count.EqualTo(1));
        Assert.That(sniperControls["action"].bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));

        // Information from 'baseMap' coming through on 'derivedFromDerivedMap'.
        Assert.That(derivedFromDerivedMap.actions, Has.Count.EqualTo(1));
        Assert.That(derivedFromDerivedMap["action"], Is.TypeOf<InputAction>());
        Assert.That(derivedFromDerivedMap["action"], Is.Not.SameAs(baseAction));
        Assert.That(derivedFromDerivedMap["action"].actionMap, Is.SameAs(derivedFromDerivedMap));
        Assert.That(derivedFromDerivedMap["action"].bindings, Has.Count.EqualTo(1));
        Assert.That(derivedFromDerivedMap["action"].bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
        */

        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_MapsCanBeBasedOnOtherMaps()
    {
        var baseMap = new InputActionMap("Base");
        var baseAction = baseMap.AddAction("action", binding: "<Gamepad>/buttonSouth");
        var derivedMap = new InputActionMap("Derived", extend: baseMap);
        var derivedFromDerivedMap = new InputActionMap("DerivedFromDerived", extend: derivedMap);

        //NO! We want to set up bindings on the *existing* action
        // But how can we have the same action answer with different bindings depending on context?
        // Should we remove the per-action control and device arrays? How do singleton actions deal with that then?
        // Alternatively, we can change how you connect to endpoints such that you don't go to an individual action
        // to connect to it.
        // It may really make sense to completely divorce delivery from InputAction and make that a pure configuration object.

        // Or... we simply don't list actions from the base in the derived map... I.e. a map will only list the
        // information *directly* defined in the map. Explicit lookups could still take bases into account.
        // But then, how do you query the bindings for an action in a derived map?

        // Or... we actually *do* replicate the information from the base in the derived map but triggering actions in
        // a derived map also triggers actions in the base map

        // Information from 'baseMap' coming through on 'derivedMap'.
        Assert.That(derivedMap.actions, Has.Count.EqualTo(1));
        Assert.That(derivedMap["action"], Is.TypeOf<InputAction>());
        Assert.That(derivedMap["action"], Is.Not.SameAs(baseAction));
        Assert.That(derivedMap["action"].actionMap, Is.SameAs(derivedMap));
        Assert.That(derivedMap["action"].bindings, Has.Count.EqualTo(1));
        Assert.That(derivedMap["action"].bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));

        // Information from 'baseMap' coming through on 'derivedFromDerivedMap'.
        Assert.That(derivedFromDerivedMap.actions, Has.Count.EqualTo(1));
        Assert.That(derivedFromDerivedMap["action"], Is.TypeOf<InputAction>());
        Assert.That(derivedFromDerivedMap["action"], Is.Not.SameAs(baseAction));
        Assert.That(derivedFromDerivedMap["action"].actionMap, Is.SameAs(derivedFromDerivedMap));
        Assert.That(derivedFromDerivedMap["action"].bindings, Has.Count.EqualTo(1));
        Assert.That(derivedFromDerivedMap["action"].bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_MapsCanBeBasedOnOtherMaps_AndPickUpChangesMadeToTheirBaseMaps()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_MapsCanBeBasedOnOtherMaps_AndAddBindingsToActionsDefinedInBaseMaps()
    {
        var baseMap = new InputActionMap("Base");
        var action = baseMap.AddAction("action", binding: "<Gamepad>/buttonSouth");

        var derivedMap = new InputActionMap("Derived", extend: baseMap);
        derivedMap.AddBinding("<Gamepad>/buttonNorth", action: "action");

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var actionWasPerformed = false;
        action.performed += _ => actionWasPerformed = true;

        derivedMap.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.North));
        InputSystem.Update();

        Assert.That(actionWasPerformed);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_TriggerNotification()
    {
        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        new InputActionMap("map2").AddAction("action3"); // Noise.

        InputActionChange? receivedChange = null;
        object receivedObject = null;
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            Assert.That(receivedChange, Is.Null);
            receivedChange = change;
            receivedObject = obj;
        };

        // Enable map.
        // Does to trigger a notification for each action in the map.
        map.Enable();

        Assert.That(receivedChange.HasValue);
        Assert.That(receivedChange.Value, Is.EqualTo(InputActionChange.ActionMapEnabled));
        Assert.That(receivedObject, Is.SameAs(map));

        receivedChange = null;
        receivedObject = null;

        // Enabling action in map should not trigger notification.
        action1.Enable();

        Assert.That(receivedChange, Is.Null);

        // Disable map.
        map.Disable();

        Assert.That(receivedChange.HasValue);
        Assert.That(receivedChange.Value, Is.EqualTo(InputActionChange.ActionMapDisabled));
        Assert.That(receivedObject, Is.SameAs(map));

        receivedChange = null;
        receivedObject = null;

        // Enable single action.
        action2.Enable();

        Assert.That(receivedChange.HasValue);
        Assert.That(receivedChange.Value, Is.EqualTo(InputActionChange.ActionEnabled));
        Assert.That(receivedObject, Is.SameAs(action2));

        receivedChange = null;
        receivedObject = null;

        // Disable single action.
        action2.Disable();

        Assert.That(receivedChange.HasValue);
        Assert.That(receivedChange.Value, Is.EqualTo(InputActionChange.ActionDisabled));
        Assert.That(receivedObject, Is.SameAs(action2));

        receivedChange = null;
        receivedObject = null;

        // Disabling single action that isn't enabled should not trigger notification.
        action2.Disable();

        Assert.That(receivedChange, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_GoesIntoWaitingPhase()
    {
        InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanReadValueFromAction()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        action.Enable();

        float? receivedValue = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedValue, Is.Null);
            receivedValue = ctx.ReadValue<float>();
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.South));
        InputSystem.Update();

        Assert.That(receivedValue, Is.EqualTo(1).Within(0.00001));
    }

    // Some code needs to be able to just generically transfer values from A to B. For this, the
    // generic ReadValue<TValue>() API isn't sufficient.
    [Test]
    [Category("Actions")]
    public unsafe void Actions_CanReadValueFromAction_WithoutKnowingValueType()
    {
        var action = new InputAction();
        action.AddBinding("<Gamepad>/leftStick");
        action.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        action.Enable();

        byte[] receivedValueData = null;

        action.performed +=
            ctx =>
        {
            Assert.That(receivedValueData, Is.Null);
            Assert.That(ctx.valueType, Is.EqualTo(typeof(Vector2)));
            Assert.That(ctx.valueSizeInBytes, Is.EqualTo(sizeof(Vector2)));

            var sizeInBytes = ctx.valueSizeInBytes;
            receivedValueData = new byte[sizeInBytes];
            fixed(byte* dataPtr = receivedValueData)
            {
                ctx.ReadValue(dataPtr, sizeInBytes);
            }
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.234f)});
        InputSystem.Update();

        Assert.That(receivedValueData, Has.Length.EqualTo(sizeof(Vector2)));
        Assert.That(BitConverter.ToSingle(receivedValueData, 0),
            Is.EqualTo(new DeadzoneProcessor().Process(new Vector2(0.123f, 0.234f), gamepad.leftStick).x).Within(0.00001));
        Assert.That(BitConverter.ToSingle(receivedValueData, 4),
            Is.EqualTo(new DeadzoneProcessor().Process(new Vector2(0.123f, 0.234f), gamepad.leftStick).y).Within(0.00001));

        receivedValueData = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        Assert.That(receivedValueData, Has.Length.EqualTo(sizeof(Vector2)));
        Assert.That(BitConverter.ToSingle(receivedValueData, 0),
            Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(BitConverter.ToSingle(receivedValueData, 4),
            Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ReadingValueOfIncorrectType_ThrowsHelpfulException()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        action.Enable();

        var receivedCall = false;
        action.performed +=
            ctx =>
        {
            receivedCall = true;
            Assert.That(() => ctx.ReadValue<Vector2>(),
                Throws.InvalidOperationException.With.Message.Contains("buttonSouth")
                    .And.With.Message.Contains("float")
                    .And.With.Message.Contains("Vector2"));
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.South));
        InputSystem.Update();

        Assert.That(receivedCall, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionMap()
    {
        var action = new InputAction();

        Assert.That(action.actionMap, Is.Null);
    }

    ////REVIEW: not sure whether this is the best behavior
    [Test]
    [Category("Actions")]
    public void Actions_PathLeadingNowhereIsIgnored()
    {
        var action = new InputAction(binding: "nothing");

        Assert.DoesNotThrow(() => action.Enable());
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartOutInDisabledPhase()
    {
        var action = new InputAction();

        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
    }

    [Test]
    [Category("Actions")]
    public void Actions_LoseActionHasNoMap()
    {
        var action = new InputAction();
        action.Enable(); // Force to create private action set.

        Assert.That(action.actionMap, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionIsPerformedWhenSourceControlChangesValue()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputAction receivedAction = null;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.performed +=
            ctx =>
        {
            ++receivedCalls;
            receivedAction = ctx.action;
            receivedControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
        };
        action.Enable();

        var state = new GamepadState
        {
            leftStick = new Vector2(0.5f, 0.5f)
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanListenForStateChangeOnEntireDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad");
        action.performed +=
            ctx =>
        {
            ++receivedCalls;
            receivedControl = ctx.control;
        };
        action.Enable();

        var state = new GamepadState
        {
            rightTrigger = 0.5f
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl,
            Is.SameAs(gamepad)); // We do not drill down to find the actual control that changed.
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanMonitorTriggeredActionsOnActionMap()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action", "/<Gamepad>/leftTrigger");

        var wasTriggered = false;
        InputAction receivedAction = null;
        InputControl receivedControl = null;
        map.actionTriggered +=
            ctx =>
        {
            Assert.That(wasTriggered, Is.False);
            wasTriggered = true;
            receivedAction = ctx.action;
            receivedControl = ctx.control;
        };

        map.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(wasTriggered);
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftTrigger));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordActionsAsEvents()
    {
        var action = new InputAction();
        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/rightStick");
        action.AddCompositeBinding("dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        action.Enable();

        using (var queue = new InputActionQueue())
        {
            action.performed += queue.RecordAction;

            var state = new GamepadState {leftStick = new Vector2(0.123f, 0.234f)};
            InputSystem.QueueStateEvent(gamepad, state, 0.1234);
            state.rightStick = new Vector2(0.345f, 0.456f);
            InputSystem.QueueStateEvent(gamepad, state, 0.2345);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W), 0.0987);
            InputSystem.Update();

            Assert.That(queue.count, Is.EqualTo(3));

            var events = queue.ToArray();

            Assert.That(events, Has.Length.EqualTo(3));
            Assert.That(events[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(events[1].control, Is.SameAs(gamepad.rightStick));
            Assert.That(events[2].control, Is.SameAs(keyboard.wKey));
            Assert.That(events[0].time, Is.EqualTo(0.1234).Within(0.00001));
            Assert.That(events[1].time, Is.EqualTo(0.2345).Within(0.00001));
            Assert.That(events[2].time, Is.EqualTo(0.0987).Within(0.00001));
            Assert.That(events[0].action, Is.SameAs(action));
            Assert.That(events[1].action, Is.SameAs(action));
            Assert.That(events[2].action, Is.SameAs(action));
            Assert.That(events[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(events[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(events[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(events[0].ReadValue<Vector2>(),
                Is.EqualTo(new DeadzoneProcessor().Process(new Vector2(0.123f, 0.234f), gamepad.leftStick))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(events[1].ReadValue<Vector2>(),
                Is.EqualTo(new DeadzoneProcessor().Process(new Vector2(0.345f, 0.456f), gamepad.rightStick))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(events[2].ReadValue<Vector2>(), Is.EqualTo(Vector2.up).Using(Vector2EqualityComparer.Instance));

            queue.Clear();

            Assert.That(queue.count, Is.Zero);
            Assert.That(queue.ToArray(), Is.Empty);
        }
    }

    // Actions are able to observe every state change, even if the changes occur within
    // the same frame.
    [Test]
    [Category("Actions")]
    public void Actions_PressingAndReleasingButtonInSameUpdate_StillTriggersAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/<gamepad>/<button>", interactions: "press");

        var receivedCalls = 0;
        action.performed +=
            ctx => { ++receivedCalls; };
        action.Enable();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", interactions: "hold(duration=0.4)");
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

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 0.5);
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

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), InputConfiguration.TapTime);
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
    public void Actions_CanPerformPressAndReleaseInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        var startedReceivedCalls = 0;

        var action = new InputAction(binding: "/<Gamepad>/buttonSouth", interactions: "pressAndRelease");
        action.performed +=
            ctx => ++ performedReceivedCalls;
        action.started +=
            ctx => ++ startedReceivedCalls;
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.South), 1);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);

        startedReceivedCalls = 0;
        performedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 2);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformStickInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        var startedReceivedCalls = 0;
        var cancelledReceivedCalls = 0;

        var action = new InputAction(binding: "/<Gamepad>/leftStick", interactions: "stick");
        action.performed +=
            ctx => ++ performedReceivedCalls;
        action.started +=
            ctx => ++ startedReceivedCalls;
        action.cancelled +=
            ctx => ++ cancelledReceivedCalls;
        action.Enable();

        // Go out of deadzone.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.345f, 0.456f)});
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(cancelledReceivedCalls, Is.Zero);

        startedReceivedCalls = 0;
        performedReceivedCalls = 0;
        cancelledReceivedCalls = 0;

        // Move around.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.456f, 0.567f)});
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(cancelledReceivedCalls, Is.EqualTo(0));

        startedReceivedCalls = 0;
        performedReceivedCalls = 0;
        cancelledReceivedCalls = 0;

        // Move around some more.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.789f, 0.765f)});
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(cancelledReceivedCalls, Is.EqualTo(0));

        startedReceivedCalls = 0;
        performedReceivedCalls = 0;
        cancelledReceivedCalls = 0;

        // Go back into deadzone.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.011f, 0.011f)});
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(0));
        Assert.That(cancelledReceivedCalls, Is.EqualTo(1));

        startedReceivedCalls = 0;
        performedReceivedCalls = 0;
        cancelledReceivedCalls = 0;

        // Make sure nothing happens if we move around in deadzone.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.012f, 0.012f)});
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(0));
        Assert.That(cancelledReceivedCalls, Is.EqualTo(0));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformStickInteraction_OnDpadComposite()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("dpad", interactions: "stick")
            .With("up", "<Keyboard>/w")
            .With("down", "<Keyboard>/s")
            .With("left", "<Keyboard>/a")
            .With("right", "<Keyboard>/d");

        var startedReceivedCalls = 0;
        action.started +=
            ctx => ++ startedReceivedCalls;
        action.Enable();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.W));
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionsToMap()
    {
        var map = new InputActionMap();

        map.AddAction("action1");
        map.AddAction("action2");

        Assert.That(map.actions, Has.Count.EqualTo(2));
        Assert.That(map.actions[0], Has.Property("name").EqualTo("action1"));
        Assert.That(map.actions[1], Has.Property("name").EqualTo("action2"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRenameActionInMap()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        map.AddAction("action2");

        action1.Rename("newName");

        Assert.That(action1.name, Is.EqualTo("newName"));
        Assert.That(map["newName"], Is.SameAs(action1));
        Assert.That(map.TryGetAction("action1"), Is.Null);

        Assert.That(() => action1.Rename("action2"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRenameAction()
    {
        var action = new InputAction("oldName");

        action.Rename("newName");

        Assert.That(action.name, Is.EqualTo("newName"));
    }

    ////TODO: add test to ensure that if adding an action after controls have been resolved, does the right thing

    [Test]
    [Category("Actions")]
    public void Actions_CanAddBindingsToActionsInMap()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddBinding("/gamepad/leftStick");
        action2.AddBinding("/gamepad/rightStick");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action2.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddUnnamedActionToMap()
    {
        var map = new InputActionMap();
        Assert.That(() => map.AddAction(""), Throws.ArgumentException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddTwoActionsWithTheSameNameToMap()
    {
        var map = new InputActionMap();
        map.AddAction("action");

        Assert.That(() => map.AddAction("action"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionInMap()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        Assert.That(map.TryGetAction("action1"), Is.SameAs(action1));
        Assert.That(map.TryGetAction("action2"), Is.SameAs(action2));

        // Lookup is case-insensitive.
        Assert.That(map.TryGetAction("Action1"), Is.SameAs(action1));
        Assert.That(map.TryGetAction("Action2"), Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertActionMapToAndFromJson()
    {
        //By default, serialize as if there's no base map
        //Solve baseMap correlation on the InputActionAsset level
        //Give action maps stable internal names (just like actions)
        var map = new InputActionMap("test");

        map.AddAction(name: "action1", expectedControlLayout: "Button", binding: "/gamepad/leftStick")
            .AddBinding("/gamepad/rightStick")
            .WithGroup("group")
            .WithProcessor("deadzone");
        map.AddAction(name: "action2", binding: "/gamepad/buttonSouth", interactions: "tap,slowTap(duration=0.1)");

        var json = map.ToJson();
        var maps = InputActionMap.FromJson(json);

        Assert.That(maps, Has.Length.EqualTo(1));
        Assert.That(maps[0], Has.Property("name").EqualTo("test"));
        Assert.That(maps[0], Has.Property("id").EqualTo(map.id));
        Assert.That(maps[0].actions, Has.Count.EqualTo(2));
        Assert.That(maps[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(maps[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(maps[0].actions[0].id, Is.EqualTo(map["action1"].id));
        Assert.That(maps[0].actions[1].id, Is.EqualTo(map["action2"].id));
        Assert.That(maps[0].actions[0].expectedControlLayout, Is.EqualTo("Button"));
        Assert.That(maps[0].actions[1].expectedControlLayout, Is.Null);
        Assert.That(maps[0].actions[0].bindings, Has.Count.EqualTo(2));
        Assert.That(maps[0].actions[1].bindings, Has.Count.EqualTo(1));
        Assert.That(maps[0].actions[0].bindings[0].groups, Is.Null);
        Assert.That(maps[0].actions[0].bindings[1].groups, Is.EqualTo("group"));
        Assert.That(maps[0].actions[0].bindings[0].processors, Is.Null);
        Assert.That(maps[0].actions[0].bindings[1].processors, Is.EqualTo("deadzone"));
        Assert.That(maps[0].actions[0].bindings[0].interactions, Is.Null);
        Assert.That(maps[0].actions[0].bindings[1].interactions, Is.Null);
        Assert.That(maps[0].actions[1].bindings[0].groups, Is.Null);
        Assert.That(maps[0].actions[1].bindings[0].processors, Is.Null);
        Assert.That(maps[0].actions[1].bindings[0].interactions, Is.EqualTo("tap,slowTap(duration=0.1)"));
        Assert.That(maps[0].actions[0].actionMap, Is.SameAs(maps[0]));
        Assert.That(maps[0].actions[1].actionMap, Is.SameAs(maps[0]));
    }

    ////TODO: test that if we apply overrides, it changes the controls we get

    // This is the JSON format that action maps had in the earliest versions of the system.
    // It's a nice and simple format and while we no longer write out action maps in that format,
    // there's no good reason not to be able to read it. It contains a flat list of actions with
    // each action listing the map it is contained in as part of its name. Also, bindings are
    // directly on the actions and thus implicitly refer to the actions they trigger.
    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionMapsInSimplifiedJsonFormat()
    {
        // Uses both 'modifiers' (old name) and 'interactions' (new name).
        const string json = @"
            {
                ""actions"" : [
                    {
                        ""name"" : ""map1/action1"",
                        ""bindings"" : [
                            {
                                ""path"" : ""<Gamepad>/leftStick""
                            }
                        ]
                    },
                    {
                        ""name"" : ""map1/action2"",
                        ""bindings"" : [
                            {
                                ""path"" : ""<Gamepad>/rightStick""
                            },
                            {
                                ""path"" : ""<Gamepad>/leftShoulder"",
                                ""modifiers"" : ""tap""
                            }
                        ]
                    },
                    {
                        ""name"" : ""map2/action1"",
                        ""bindings"" : [
                            {
                                ""path"" : ""<Gamepad>/buttonSouth"",
                                ""modifiers"" : ""slowTap""
                            }
                        ]
                    }
                ]
            }
        ";

        var maps = InputActionMap.FromJson(json);

        Assert.That(maps.Length, Is.EqualTo(2));
        Assert.That(maps[0].name, Is.EqualTo("map1"));
        Assert.That(maps[1].name, Is.EqualTo("map2"));
        Assert.That(maps[0].actions.Count, Is.EqualTo(2));
        Assert.That(maps[1].actions.Count, Is.EqualTo(1));
        Assert.That(maps[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(maps[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(maps[1].actions[0].name, Is.EqualTo("action1"));
        Assert.That(maps[0].bindings.Count, Is.EqualTo(3));
        Assert.That(maps[1].bindings.Count, Is.EqualTo(1));
        Assert.That(maps[0].bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(maps[0].bindings[1].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(maps[0].bindings[2].path, Is.EqualTo("<Gamepad>/leftShoulder"));
        Assert.That(maps[1].bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(maps[0].bindings[2].interactions, Is.EqualTo("tap"));
        Assert.That(maps[1].bindings[0].interactions, Is.EqualTo("slowTap"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionMapJsonCanBeEmpty()
    {
        var maps = InputActionMap.FromJson("{}");
        Assert.That(maps, Is.Not.Null);
        Assert.That(maps, Has.Length.EqualTo(0));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertMultipleActionMapsToAndFromJson()
    {
        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");

        map1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        map2.AddAction(name: "action2", binding: "/gamepad/rightStick");

        var json = InputActionMap.ToJson(new[] {map1, map2});
        var sets = InputActionMap.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(2));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("map1"));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("map2"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertAssetToAndFromJson()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "TestAsset";

        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");

        map1.AddAction("action1", binding: "<Gamepad>/leftStick");
        map1.AddAction("action2", binding: "<Gamepad>/rightStick");
        map2.AddAction("action3", binding: "<Gamepad>/leftTrigger");

        asset.AddControlScheme("scheme1").WithBindingGroup("group1").WithRequiredDevice("<Gamepad>");
        asset.AddControlScheme("scheme2").BasedOn("scheme1").WithBindingGroup("group2")
            .WithOptionalDevice("<Keyboard>").WithRequiredDevice("<Mouse>").OrWithRequiredDevice("<Pen>");

        var json = asset.ToJson();

        // Re-create asset from JSON.
        UnityEngine.Object.DestroyImmediate(asset);
        asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.LoadFromJson(json);

        Assert.That(asset.name, Is.EqualTo("TestAsset"));

        Assert.That(asset.actionMaps, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo("map1"));
        Assert.That(asset.actionMaps[1].name, Is.EqualTo("map2"));
        Assert.That(asset.actionMaps[0].asset, Is.SameAs(asset));
        Assert.That(asset.actionMaps[1].asset, Is.SameAs(asset));
        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[1].actions, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(asset.actionMaps[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(asset.actionMaps[1].actions[0].name, Is.EqualTo("action3"));
        Assert.That(asset.actionMaps[0].bindings, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[1].bindings, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(asset.actionMaps[0].bindings[0].action, Is.EqualTo("action1"));
        Assert.That(asset.actionMaps[0].bindings[1].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(asset.actionMaps[0].bindings[1].action, Is.EqualTo("action2"));
        Assert.That(asset.actionMaps[1].bindings[0].path, Is.EqualTo("<Gamepad>/leftTrigger"));
        Assert.That(asset.actionMaps[1].bindings[0].action, Is.EqualTo("action3"));

        Assert.That(asset.controlSchemes, Has.Count.EqualTo(2));
        Assert.That(asset.controlSchemes[0].name, Is.EqualTo("scheme1"));
        Assert.That(asset.controlSchemes[1].name, Is.EqualTo("scheme2"));
        Assert.That(asset.controlSchemes[0].bindingGroup, Is.EqualTo("group1"));
        Assert.That(asset.controlSchemes[1].bindingGroup, Is.EqualTo("group2"));
        Assert.That(asset.controlSchemes[0].baseScheme, Is.Null);
        Assert.That(asset.controlSchemes[1].baseScheme, Is.EqualTo("scheme1"));
        Assert.That(asset.controlSchemes[0].deviceRequirements, Has.Count.EqualTo(1));
        Assert.That(asset.controlSchemes[1].deviceRequirements, Has.Count.EqualTo(3));
        Assert.That(asset.controlSchemes[0].deviceRequirements[0].controlPath, Is.EqualTo("<Gamepad>"));
        Assert.That(asset.controlSchemes[0].deviceRequirements[0].isOptional, Is.False);
        Assert.That(asset.controlSchemes[0].deviceRequirements[0].isOR, Is.False);
        Assert.That(asset.controlSchemes[1].deviceRequirements[0].controlPath, Is.EqualTo("<Keyboard>"));
        Assert.That(asset.controlSchemes[1].deviceRequirements[0].isOptional, Is.True);
        Assert.That(asset.controlSchemes[1].deviceRequirements[0].isOR, Is.False);
        Assert.That(asset.controlSchemes[1].deviceRequirements[1].controlPath, Is.EqualTo("<Mouse>"));
        Assert.That(asset.controlSchemes[1].deviceRequirements[1].isOptional, Is.False);
        Assert.That(asset.controlSchemes[1].deviceRequirements[1].isOR, Is.False);
        Assert.That(asset.controlSchemes[1].deviceRequirements[2].controlPath, Is.EqualTo("<Pen>"));
        Assert.That(asset.controlSchemes[1].deviceRequirements[2].isOptional, Is.False);
        Assert.That(asset.controlSchemes[1].deviceRequirements[2].isOR, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryAllEnabledActions()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var enabledActions = InputSystem.ListEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAction()
    {
        var action = new InputAction(name: "MyAction", binding: "/gamepad/leftStick");

        // Unity's JSON serializer goes through Unity's normal serialization machinery so if
        // this works, we should have a pretty good shot that binary and YAML serialization
        // are also working.
        var json = JsonUtility.ToJson(action);
        var deserializedAction = JsonUtility.FromJson<InputAction>(json);

        Assert.That(deserializedAction.name, Is.EqualTo(action.name));
        Assert.That(deserializedAction.bindings, Has.Count.EqualTo(1));
        Assert.That(deserializedAction.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeActionMap()
    {
        var map = new InputActionMap("set");

        map.AddAction("action1", binding: "/gamepad/leftStick");
        map.AddAction("action2", binding: "/gamepad/rightStick");

        var json = JsonUtility.ToJson(map);
        var deserializedSet = JsonUtility.FromJson<InputActionMap>(json);

        Assert.That(deserializedSet.name, Is.EqualTo("set"));
        Assert.That(deserializedSet.actions, Has.Count.EqualTo(2));
        Assert.That(deserializedSet.actions[0].name, Is.EqualTo("action1"));
        Assert.That(deserializedSet.actions[1].name, Is.EqualTo("action2"));
        Assert.That(deserializedSet.actions[0].bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(deserializedSet.actions[1].bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
        Assert.That(deserializedSet.actions[0].actionMap, Is.SameAs(deserializedSet));
        Assert.That(deserializedSet.actions[1].actionMap, Is.SameAs(deserializedSet));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddMultipleBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftStick");
        action.AddBinding("/gamepad/rightStick");

        action.Enable();

        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action.bindings[1].path, Is.EqualTo("/gamepad/rightStick"));

        var performedReceivedCalls = 0;
        InputControl performedControl = null;

        action.performed +=
            ctx =>
        {
            ++performedReceivedCalls;
            performedControl = ctx.control;
        };

        var state = new GamepadState {leftStick = new Vector2(0.5f, 0.5f)};
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.leftStick));

        performedReceivedCalls = 0;

        state.rightStick = new Vector2(0.5f, 0.5f);
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.rightStick));
    }

    class ConstantVector2TestProcessor : IInputControlProcessor<Vector2>
    {
        public Vector2 Process(Vector2 value, InputControl control)
        {
            return new Vector2(0.1234f, 0.5678f);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddProcessorsToBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterControlProcessor<ConstantVector2TestProcessor>();
        var action = new InputAction();
        action.AddBinding("/<Gamepad>/leftStick").WithProcessor<ConstantVector2TestProcessor>();
        action.Enable();

        Vector2? receivedVector = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedVector, Is.Null);
            receivedVector = ctx.ReadValue<Vector2>();
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.one });
        InputSystem.Update();

        Assert.That(receivedVector, Is.Not.Null);
        Assert.That(receivedVector.Value.x, Is.EqualTo(0.1234).Within(0.00001));
        Assert.That(receivedVector.Value.y, Is.EqualTo(0.5678).Within(0.00001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenNewDeviceIsAdded()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "/<gamepad>/buttonSouth");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad1.buttonSouth));

        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.buttonSouth));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenDeviceIsRemoved()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "/<Gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Has.Count.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenDeviceIsRemoved_WhileActionIsDisabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "/<Gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));

        action.Disable();

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Has.Count.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdateWhileEnabled_NotificationIsTriggered()
    {
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        action.Enable();

        InputActionChange? receivedChange = null;
        object receivedObject = null;
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            Assert.That(receivedChange, Is.Null);
            receivedChange = change;
            receivedObject = obj;
        };

        InputSystem.AddDevice<Gamepad>();

        Assert.That(receivedChange, Is.Not.Null);
        Assert.That(receivedChange.Value, Is.EqualTo(InputActionChange.BoundControlsHaveChangedWhileEnabled));
        Assert.That(receivedObject, Is.SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanFindEnabledActions()
    {
        var action1 = new InputAction(name: "a");
        var action2 = new InputAction(name: "b");

        action1.Enable();
        action2.Enable();

        var enabledActions = InputSystem.ListEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(2));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action2));
    }

    private class TestInteraction : IInputInteraction
    {
#pragma warning disable CS0649
        public float parm1; // Assigned through reflection
#pragma warning restore CS0649

        public static bool s_GotInvoked;

        public void Process(ref InputInteractionContext context)
        {
            Assert.That(parm1, Is.EqualTo(5.0).Within(0.000001));
            s_GotInvoked = true;
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRegisterNewInteraction()
    {
        InputSystem.RegisterInteraction<TestInteraction>();
        TestInteraction.s_GotInvoked = false;

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick/x", interactions: "test(parm1=5.0)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(TestInteraction.s_GotInvoked, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTriggerActionFromPartialStateUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var receivedCalls = 0;
        InputControl receivedControl = null;
        action.performed += ctx =>
        {
            ++receivedCalls;
            receivedControl = ctx.control;
        };

        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, Vector2.one);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDistinguishTapAndSlowTapOnSameAction()
    {
        // Bindings can have more than one interaction. Depending on the interaction happening on the bound
        // controls one of the interactions may initiate a phase shift and which interaction initiated the
        // shift is visible on the callback.
        //
        // This is most useful for allowing variations of the same action. For example, you can have a
        // "Fire" action, bind it to the "PrimaryAction" button, and then put both a TapInteraction and a
        // SlowTapInteraction on the same binding. In the 'performed' callback you can then detect whether
        // the button was slow-pressed or fast-pressed. Depending on that, you can perform a normal
        // fire action or a charged fire action.

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/buttonSouth",
            interactions: "tap(duration=0.1),slowTap(duration=0.5)");
        action.Enable();

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

        action.started += ctx => started.Add(ctx);
        action.performed += ctx => performed.Add(ctx);
        action.cancelled += ctx => cancelled.Add(ctx);

        // Perform tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0}, 0.05);
        InputSystem.Update();

        // Only tap was started.
        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(started[0].interaction, Is.TypeOf<TapInteraction>());

        // Only tap was performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].interaction, Is.TypeOf<TapInteraction>());

        // Nothing was cancelled.
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();
        cancelled.Clear();

        // Perform slow tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 2.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0},
            2.0 + InputConfiguration.SlowTapTime + 0.0001);
        InputSystem.Update();

        // First tap was started, then slow tap was started.
        Assert.That(started, Has.Count.EqualTo(2));
        Assert.That(started[0].interaction, Is.TypeOf<TapInteraction>());
        Assert.That(started[1].interaction, Is.TypeOf<SlowTapInteraction>());

        // Tap got cancelled.
        Assert.That(cancelled, Has.Count.EqualTo(1));
        Assert.That(cancelled[0].interaction, Is.TypeOf<TapInteraction>());

        // Slow tap got performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].interaction, Is.TypeOf<SlowTapInteraction>());
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanSetUpBindingsOnActionMap()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var map = new InputActionMap();
        var fire = map.AddAction("fire");
        var reload = map.AddAction("reload");

        map.AddBinding("<Keyboard>/space")
            .WithChild("<Mouse>/leftButton").Triggering(fire)
            .And.WithChild("<Mouse>/rightButton").Triggering(reload);

        map.Enable();

        var firePerformed = false;
        var reloadPerformed = false;

        fire.performed += ctx =>
        {
            Assert.That(firePerformed, Is.False);
            firePerformed = true;
        };
        reload.performed += ctx =>
        {
            Assert.That(reloadPerformed, Is.False);
            reloadPerformed = true;
        };

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();

        Assert.That(firePerformed, Is.False);
        Assert.That(reloadPerformed, Is.False);

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseState.Button.Left));
        InputSystem.Update();

        Assert.That(firePerformed, Is.True);
        Assert.That(reloadPerformed, Is.False);

        firePerformed = false;
        reloadPerformed = false;

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseState.Button.Right));
        InputSystem.Update();

        Assert.That(firePerformed, Is.False);
        Assert.That(reloadPerformed, Is.True);
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanQueryBindingsTriggeringAction()
    {
        var map = new InputActionMap();
        var fire = map.AddAction("fire");
        var reload = map.AddAction("reload");

        map.AddBinding("<Keyboard>/space")
            .WithChild("<Mouse>/leftButton").Triggering(fire)
            .And.WithChild("<Mouse>/rightButton").Triggering(reload);
        map.AddBinding("<Keyboard>/leftCtrl").Triggering(fire);

        Assert.That(map.bindings.Count, Is.EqualTo(3));
        Assert.That(fire.bindings.Count, Is.EqualTo(2));
        Assert.That(reload.bindings.Count, Is.EqualTo(1));
        Assert.That(fire.bindings[0].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(fire.bindings[1].path, Is.EqualTo("<Keyboard>/leftCtrl"));
        Assert.That(reload.bindings[0].path, Is.EqualTo("<Mouse>/rightButton"));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanChainBindings()
    {
        // Set up an action that requires the left trigger to be held when pressing the A button.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").ChainedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        // Last control in combination is considered the trigger control.
        Assert.That(performed[0].control, Is.SameAs(gamepad.buttonSouth));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ChainedBindingsTriggerIfControlsActivateAtSameTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").ChainedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ChainedBindingsDoNotTriggerIfControlsActivateInWrongOrder()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").ChainedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);
    }

    // The ability to combine bindings and have interactions on them is crucial to be able to perform
    // most gestures as they usually require a button-like control that indicates whether a possible
    // gesture has started and then a positional control of some kind that gives the motion data for
    // the gesture.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanChainBindingsWithInteractions()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        // Tap or slow tap on A button when left trigger is held.
        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").ChainedWith("/gamepad/buttonSouth", interactions: "tap,slowTap");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 0}, InputConfiguration.SlowTapTime + 0.1);
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].interaction, Is.TypeOf<SlowTapInteraction>());
    }

    ////REVIEW: don't think this one makes sense to have
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanPerformContinuousAction()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick", interactions: "continuous");
        action.Enable();

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

        action.started += ctx => performed.Add(ctx);
        action.cancelled += ctx => performed.Add(ctx);
        action.performed +=
            ctx =>
        {
            performed.Add(ctx);
            Assert.That(ctx.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.123f, 0.456f)));
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.456f)});
        InputSystem.Update();
        InputSystem.Update();

        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(performed, Has.Count.EqualTo(2));
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(started, Has.Count.Zero);
        Assert.That(performed, Has.Count.Zero);
        Assert.That(cancelled, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_AddingDeviceWillUpdateControlsOnAction()
    {
        var action = new InputAction(binding: "/<gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.Zero);

        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad1.leftTrigger));

        // Make sure it actually triggers correctly.
        InputSystem.QueueStateEvent(gamepad1, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad1.leftTrigger));

        // Also make sure that this device creation path gets it right.
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription {product = "Test", deviceClass = "Gamepad"}.ToJson());
        InputSystem.Update();
        var gamepad2 = (Gamepad)InputSystem.devices.First(x => x.description.product == "Test");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftTrigger));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftTrigger));
    }

    [Test]
    [Category("Actions")]
    public void Actions_RemovingDeviceWillUpdateControlsOnAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Contains.Item(gamepad.leftStick));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableAction()
    {
        InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");

        action.Enable();

        Assert.That(action.enabled, Is.True);
        Assert.That(action.controls.Count, Is.EqualTo(1));

        action.Disable();

        Assert.That(InputSystem.ListEnabledActions(), Has.Exactly(0).SameAs(action));
        Assert.That(action.controls.Count, Is.EqualTo(1));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableActionThatHasNoControls()
    {
        var action1 = new InputAction(binding: "<Gamepad>/buttonSouth");
        var action2 = new InputActionMap().AddAction("test", binding: "<Keyboard>/a");

        action1.Enable();
        action2.Enable();

        Assert.That(action1.enabled, Is.True);
        Assert.That(action2.enabled, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSingleDeviceWithMultipleActions()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/leftStick");
        var action3 = new InputAction(binding: "/gamepad/rightStick");

        var action1Performed = 0;
        var action2Performed = 0;
        var action3Performed = 0;

        action1.performed += _ => ++ action1Performed;
        action2.performed += _ => ++ action2Performed;
        action3.performed += _ => ++ action3Performed;

        action1.Enable();
        action2.Enable();
        action3.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one, rightStick = Vector2.one});
        InputSystem.Update();

        Assert.That(action1Performed, Is.EqualTo(1));
        Assert.That(action2Performed, Is.EqualTo(1));
        Assert.That(action3Performed, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ButtonTriggersActionOnlyAfterCrossingPressThreshold()
    {
        // Axis controls trigger for every value change whereas buttons only trigger
        // when crossing the press threshold.

        //should this depend on the interactions being used?
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryStartAndPerformTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftTrigger", interactions: "slowTap");
        action.Enable();

        var receivedStartTime = 0.0;
        var receivedTime = 0.0;

        action.performed +=
            ctx =>
        {
            receivedStartTime = ctx.startTime;
            receivedTime = ctx.time;
        };

        var startTime = 0.123;
        var endTime = 0.123 + InputConfiguration.SlowTapTime + 1.0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f}, startTime);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.0f}, endTime);
        InputSystem.Update();

        Assert.That(receivedStartTime, Is.EqualTo(startTime).Within(0.000001));
        Assert.That(receivedTime, Is.EqualTo(endTime).Within(0.000001));
    }

    // Make sure that if we target "*/{ActionAction}", for example, and the gamepad's A button
    // goes down and starts the action, then whatever happens with the mouse's left button
    // shouldn't matter until the gamepad's A button comes back up.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_StartingOfActionCapturesControl()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddMapsToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        Assert.That(asset.actionMaps, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps, Has.Exactly(1).SameAs(map1));
        Assert.That(asset.actionMaps, Has.Exactly(1).SameAs(map2));
        Assert.That(map1.asset, Is.SameAs(asset));
        Assert.That(map2.asset, Is.SameAs(asset));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddSameMapToTwoDifferentAssets()
    {
        var asset1 = ScriptableObject.CreateInstance<InputActionAsset>();
        var asset2 = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("map");

        asset1.AddActionMap(map);

        Assert.That(() => asset2.AddActionMap(map), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_MapsInAssetMustHaveName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap();

        Assert.That(() => asset.AddActionMap(map), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_MapsInAssetsMustHaveUniqueNames()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("same");
        var map2 = new InputActionMap("same");

        asset.AddActionMap(map1);
        Assert.That(() => asset.AddActionMap(map2), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpMapInAssetByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("test");
        asset.AddActionMap(map);

        Assert.That(asset.TryGetActionMap("test"), Is.SameAs(map));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionInAssetByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");

        Assert.That(asset.FindAction("action1"), Is.SameAs(action1));
        Assert.That(asset.FindAction("action2"), Is.SameAs(action2));
        Assert.That(asset.FindAction("action3"), Is.SameAs(action3));

        Assert.That(asset.FindAction("map1/action1"), Is.SameAs(action1));
        Assert.That(asset.FindAction("map1/action2"), Is.SameAs(action2));
        Assert.That(asset.FindAction("map2/action3"), Is.SameAs(action3));

        // Shouldn't allocate.
        #if UNITY_2018_3_OR_NEWER
        var map1action1 = "map1/action1";
        Assert.That(() =>
        {
            asset.FindAction(map1action1);
        }, Is.Not.AllocatingGCMemory());
        #endif
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveActionMapFromAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        asset.AddActionMap(new InputActionMap("test"));
        asset.RemoveActionMap("test");

        Assert.That(asset.actionMaps, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddControlSchemeToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme("scheme1");

        Assert.That(asset.controlSchemes, Has.Count.EqualTo(1));
        Assert.That(asset.controlSchemes, Has.Exactly(1).With.Property("name").EqualTo("scheme1"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddControlSchemeWithoutName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        Assert.That(() => asset.AddControlScheme(string.Empty), Throws.ArgumentNullException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddControlSchemeWithDuplicateNameToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme("scheme");

        // Case is ignored.
        Assert.That(() => asset.AddControlScheme("SCHEME"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanFindControlSchemeInAssetByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme("scheme1");
        asset.AddControlScheme("scheme2");

        Assert.That(asset.GetControlScheme("SCHEme1").name, Is.EqualTo("scheme1"));
        Assert.That(asset.GetControlScheme("scheme2").name, Is.EqualTo("scheme2"));
        Assert.That(asset.TryGetControlScheme("doesNotExist"), Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveControlSchemeFromAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme("scheme");
        Assert.That(asset.controlSchemes, Is.Not.Empty);

        asset.RemoveControlScheme("scheme");
        Assert.That(asset.controlSchemes, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanRenameControlSchemeInAsset()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlSchemeGroupDefaultsToNameOfScheme()
    {
        var scheme = new InputControlScheme("test");
        Assert.That(scheme.bindingGroup, Is.EqualTo("test"));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanBaseOneControlSchemeOnAnother()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRequireSpecificDevicesForControlScheme()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        asset.AddControlScheme("scheme")
            .WithRequiredDevice("<XRController>{LeftHand}")
            .WithRequiredDevice("<XRController>{RightHand}")
            .WithOptionalDevice("<Gamepad>");

        Assert.That(asset.GetControlScheme("scheme").deviceRequirements, Has.Count.EqualTo(3));
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[0].controlPath, Is.EqualTo("<XRController>{LeftHand}"));
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[0].isOptional, Is.False);
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[1].controlPath, Is.EqualTo("<XRController>{RightHand}"));
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[1].isOptional, Is.False);
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[2].controlPath, Is.EqualTo("<Gamepad>"));
        Assert.That(asset.GetControlScheme("scheme").deviceRequirements[2].isOptional, Is.True);
    }

    ////REVIEW: this one probably warrants breaking it up a little
    [Test]
    [Category("Actions")]
    public void Actions_CanPickDevicesThatMatchGivenControlScheme()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var keyboardScheme = new InputControlScheme("keyboard")
            .WithRequiredDevice("<Keyboard>");
        var gamepadOptionalGamepadScheme = new InputControlScheme("gamepadOptionalGamepad")
            .WithRequiredDevice("<Gamepad>")
            .WithOptionalDevice("<Gamepad>");
        var emptyScheme = new InputControlScheme("empty");
        var twoGamepadScheme = new InputControlScheme("twoGamepad")
            .WithRequiredDevice("<Gamepad>")
            .WithRequiredDevice("<Gamepad>");
        var gyroGamepadScheme = new InputControlScheme("gyroGamepad")
            .WithRequiredDevice("<Gamepad>/<Gyroscope>");
        var twinStickScheme = new InputControlScheme("twinStick")
            .WithRequiredDevice("*/leftStick")
            .WithRequiredDevice("*/rightStick");
        var keyboardMouseOrPenOrTouchscreenScheme = new InputControlScheme("keyboardMouseOrPenOrTouchscreen")
            .WithRequiredDevice("<Keyboard>")
            .WithRequiredDevice("<Mouse>")
            .OrWithRequiredDevice("<Pen>")
            .OrWithRequiredDevice("<Touchscreen>");

        var empty = new InputControlList<InputDevice>();
        using (var keyboardOnly = new InputControlList<InputDevice>(keyboard))
        using (var gamepad1And2AndKeyboard = new InputControlList<InputDevice>(gamepad1, gamepad2, keyboard))
        using (var gamepadOnly = new InputControlList<InputDevice>(gamepad1))
        using (var keyboardMouseAndPen = new InputControlList<InputDevice>(keyboard, mouse, pen))
        using (var keyboardAndPen = new InputControlList<InputDevice>(keyboard, pen))
        {
            // Fail picking <Keyboard> from [].
            using (var match = keyboardScheme.PickDevicesFrom(empty))
            {
                Assert.That(match.isSuccessfulMatch, Is.False);
                Assert.That(match.hasMissingRequiredDevices);
                Assert.That(match.devices, Is.Empty);
                Assert.That(match.ToList(), Has.Count.EqualTo(1));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.Null);
                Assert.That(match.ToList()[0].device, Is.Null);
            }

            // Pick <Keyboard> from [<Keyboard>].
            using (var match = keyboardScheme.PickDevicesFrom(keyboardOnly))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new[] {keyboard}));
                Assert.That(match.ToList(), Has.Count.EqualTo(1));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(keyboard));
                Assert.That(match.ToList()[0].device, Is.SameAs(keyboard));
            }

            // Pick <Keyboard> from [<Gamepad>,<Gamepad>,<Keyboard>].
            using (var match = keyboardScheme.PickDevicesFrom(gamepad1And2AndKeyboard))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new[] {keyboard}));
                Assert.That(match.ToList(), Has.Count.EqualTo(1));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(keyboard));
                Assert.That(match.ToList()[0].device, Is.SameAs(keyboard));
            }

            // Pick <Gamepad> from [<Gamepad>].
            using (var match = gamepadOptionalGamepadScheme.PickDevicesFrom(gamepadOnly))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices);
                Assert.That(match.devices, Is.EquivalentTo(new[] {gamepad1}));
                Assert.That(match.ToList(), Has.Count.EqualTo(2));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[0].device, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.Null);
                Assert.That(match.ToList()[1].device, Is.Null);
            }

            // Pick [<Gamepad>,optional <Gamepad>] from [<Gamepad>,<Gamepad>,<Keyboard>].
            using (var match = gamepadOptionalGamepadScheme.PickDevicesFrom(gamepad1And2AndKeyboard))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new[] {gamepad1, gamepad2}));
                Assert.That(match.ToList(), Has.Count.EqualTo(2));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[0].device, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.SameAs(gamepad2));
                Assert.That(match.ToList()[1].device, Is.SameAs(gamepad2));
            }

            // Fail picking [<Gamepad>,<Gamepad>] from [<Gamepad>].
            using (var match = twoGamepadScheme.PickDevicesFrom(gamepadOnly))
            {
                Assert.That(match.isSuccessfulMatch, Is.False);
                Assert.That(match.hasMissingRequiredDevices);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.Empty);
                Assert.That(match.ToList(), Has.Count.EqualTo(2));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[0].device, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.Null);
                Assert.That(match.ToList()[1].device, Is.Null);
            }

            // Fail picking [<Gamepad>/<Gyroscope>] from [<Gamepad>].
            using (var match = gyroGamepadScheme.PickDevicesFrom(gamepadOnly))
            {
                Assert.That(match.isSuccessfulMatch, Is.False);
                Assert.That(match.hasMissingRequiredDevices);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.Empty);
                Assert.That(match.ToList(), Has.Count.EqualTo(1));
                Assert.That(match.ToList(), Has.Exactly(1)
                    .With.Property("requirementIndex").EqualTo(0)
                    .And.With.Property("control").Null
                    .And.With.Property("device").Null);
            }

            // Pick [<Keyboard>,<Mouse>] from [<Keyboard>,<Mouse>,<Pen>].
            using (var match = keyboardMouseOrPenOrTouchscreenScheme.PickDevicesFrom(keyboardMouseAndPen))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
                Assert.That(match.ToList(), Has.Count.EqualTo(4));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(keyboard));
                Assert.That(match.ToList()[0].device, Is.SameAs(keyboard));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.SameAs(mouse));
                Assert.That(match.ToList()[1].device, Is.SameAs(mouse));
                Assert.That(match.ToList()[2].requirementIndex, Is.EqualTo(2));
                Assert.That(match.ToList()[2].control, Is.Null);
                Assert.That(match.ToList()[2].device, Is.Null);
                Assert.That(match.ToList()[3].requirementIndex, Is.EqualTo(3));
                Assert.That(match.ToList()[3].control, Is.Null);
                Assert.That(match.ToList()[3].device, Is.Null);
            }

            // Pick [<Keyboard>,<Pen>] from [<Keyboard>,<Mouse>,<Pen>].
            using (var match = keyboardMouseOrPenOrTouchscreenScheme.PickDevicesFrom(keyboardAndPen))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new InputDevice[] { keyboard, pen }));
                Assert.That(match.ToList(), Has.Count.EqualTo(4));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(keyboard));
                Assert.That(match.ToList()[0].device, Is.SameAs(keyboard));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.Null);
                Assert.That(match.ToList()[1].device, Is.Null);
                Assert.That(match.ToList()[2].requirementIndex, Is.EqualTo(2));
                Assert.That(match.ToList()[2].control, Is.SameAs(pen));
                Assert.That(match.ToList()[2].device, Is.SameAs(pen));
                Assert.That(match.ToList()[3].requirementIndex, Is.EqualTo(3));
                Assert.That(match.ToList()[3].control, Is.Null);
                Assert.That(match.ToList()[3].device, Is.Null);
            }

            // Fail picking [<Keyboard>,<Mouse> or <Pen>] from [<Keyboard>].
            using (var match = keyboardMouseOrPenOrTouchscreenScheme.PickDevicesFrom(keyboardOnly))
            {
                Assert.That(match.isSuccessfulMatch, Is.False);
                Assert.That(match.hasMissingRequiredDevices);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.Empty);
                Assert.That(match.ToList(), Has.Count.EqualTo(4));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(keyboard));
                Assert.That(match.ToList()[0].device, Is.SameAs(keyboard));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.Null);
                Assert.That(match.ToList()[1].device, Is.Null);
                Assert.That(match.ToList()[2].requirementIndex, Is.EqualTo(2));
                Assert.That(match.ToList()[2].control, Is.Null);
                Assert.That(match.ToList()[2].device, Is.Null);
                Assert.That(match.ToList()[3].requirementIndex, Is.EqualTo(3));
                Assert.That(match.ToList()[3].control, Is.Null);
                Assert.That(match.ToList()[3].device, Is.Null);
            }

            // Pick [<Gamepad>] from [<Gamepad>,<Gamepad>,<Keyboard>].
            using (var match = twinStickScheme.PickDevicesFrom(gamepad1And2AndKeyboard))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.EquivalentTo(new[] {gamepad1}));
                Assert.That(match.ToList(), Has.Count.EqualTo(2));
                Assert.That(match.ToList()[0].requirementIndex, Is.EqualTo(0));
                Assert.That(match.ToList()[0].control, Is.SameAs(gamepad1.leftStick));
                Assert.That(match.ToList()[0].device, Is.SameAs(gamepad1));
                Assert.That(match.ToList()[1].requirementIndex, Is.EqualTo(1));
                Assert.That(match.ToList()[1].control, Is.SameAs(gamepad1.rightStick));
                Assert.That(match.ToList()[1].device, Is.SameAs(gamepad1));
            }

            // Pick [] from [<Gamepad>,<Gamepad>,<Keyboard>].
            using (var match = emptyScheme.PickDevicesFrom(gamepad1And2AndKeyboard))
            {
                Assert.That(match.isSuccessfulMatch);
                Assert.That(match.hasMissingRequiredDevices, Is.False);
                Assert.That(match.hasMissingOptionalDevices, Is.False);
                Assert.That(match.devices, Is.Empty);
                Assert.That(match.ToList(), Is.Empty);
            }
        }
    }

    #if UNITY_2018_3_OR_NEWER
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanPickDevicesThatMatchGivenControlScheme_WithoutAllocatingGCMemory()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var scheme = new InputControlScheme("scheme")
            .WithRequiredDevice("<Gamepad>")
            .WithRequiredDevice("<Gamepad>");

        using (var gamepad1And2AndKeyboard = new InputControlList<InputDevice>(gamepad1, gamepad2, keyboard))
        {
            ////FIXME: Even when stripping the method down to basically just returning a "new MatchResult", it's
            ////       still flagged as allocating; no clue what's going on here
            Assert.That(() =>
            {
                var match = scheme.PickDevicesFrom(gamepad1And2AndKeyboard);
                match.Dispose();
            }, Is.Not.AllocatingGCMemory());
        }
    }

    #endif

    // The bindings targeting an action can be masked out such that only specific
    // bindings take effect and others are ignored.
    [Test]
    [Category("Actions")]
    public void Actions_CanMaskOutBindingsByBindingGroup_OnAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var action = new InputAction();

        action.AddBinding("<Gamepad>/buttonSouth").WithGroup("gamepad");
        action.AddBinding("<Keyboard>/a").WithGroup("keyboard");
        action.AddBinding("<Mouse>/leftButton");

        Assert.That(action.controls, Has.Count.EqualTo(3));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(mouse.leftButton));

        action.SetBindingMask("gamepad");

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.bindingMask, Is.EqualTo(new InputBinding {groups = "gamepad"}));

        action.ClearBindingMask();

        Assert.That(action.controls, Has.Count.EqualTo(3));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(mouse.leftButton));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanMaskOutBindingsByBindingGroup_OnActionMap()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddBinding("<Gamepad>/buttonSouth").WithGroup("gamepad");
        action1.AddBinding("<Keyboard>/a").WithGroup("keyboard");
        action2.AddBinding("<Mouse>/leftButton");

        Assert.That(action1.controls, Has.Count.EqualTo(2));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(mouse.leftButton));

        map.SetBindingMask("gamepad");

        Assert.That(action1.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Count.Zero);
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanMaskOutBindingsByBindingGroup_OnAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map = new InputActionMap("map");
        asset.AddActionMap(map);

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddBinding("<Gamepad>/leftStick").WithGroup("gamepad");
        action2.AddBinding("<Gamepad>/rightStick").WithGroup("gamepad");
        action1.AddBinding("<Keyboard>/a").WithGroup("keyboard");
        action2.AddBinding("<Keyboard>/b").WithGroup("keyboard");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        asset.SetBindingMask("gamepad");

        Assert.That(action1.controls, Has.Count.EqualTo(1));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(gamepad.rightStick));

        asset.SetBindingMask("keyboard");

        Assert.That(action1.controls, Has.Count.EqualTo(1));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(keyboard.bKey));

        asset.ClearBindingMask();

        Assert.That(action1.controls, Has.Count.EqualTo(2));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action2.controls, Has.Count.EqualTo(2));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(gamepad.rightStick));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(keyboard.bKey));
    }

    // When we have an .inputactions asset, at runtime we should end up with a single array of resolved
    // controls, single array of trigger states, and so on. The expectation is that users won't generally
    // go and configure each map in an asset in a wildly different way. Rather, the maps will usually perform
    // different actions based against the same set of devices. So combining all state into one will give
    // us the most efficient representation.
    [Test]
    [Category("Actions")]
    public void Actions_AllMapsInAssetShareSingleExecutionState()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");

        map1.AddAction("action1");
        map2.AddAction("action2");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        map1.Enable();
        map2.Enable();

        Assert.That(map1.m_State, Is.SameAs(map2.m_State));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryLastTrigger()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/gamepad/rightTrigger", interactions: "slowTap(duration=1)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1}, 2);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerInteraction, Is.TypeOf<SlowTapInteraction>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0}, 4);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(4).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerInteraction, Is.TypeOf<SlowTapInteraction>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));
    }

    ////TODO: add tests for new matching of InputBindings against one another (e.g. separated lists of paths and actions)

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride("/gamepad/rightTrigger");
        action.Enable();

        var wasPerformed = false;
        action.performed += ctx => wasPerformed = true;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1});
        InputSystem.Update();

        Assert.That(wasPerformed);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDeactivateBindingsUsingOverrides()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride("");
        action.Enable();

        var wasPerformed = false;
        action.performed += ctx => wasPerformed = true;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1});
        InputSystem.Update();

        Assert.That(wasPerformed, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateAxisComposite()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("Axis")
            .With("Negative", "/<Gamepad>/leftShoulder")
            .With("Positive", "/<Gamepad>/rightShoulder");
        action.Enable();

        float? value = null;
        action.performed += ctx => { value = ctx.ReadValue<float>(); };

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.LeftShoulder));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(-1).Within(0.00001));

        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.RightShoulder));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(1).Within(0.00001));

        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.Zero.Within(0.00001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateDpadComposite()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up classic WASD control.
        var action = new InputAction();
        action.AddCompositeBinding("Dpad")
            .With("Up", "/<Keyboard>/w")
            .With("Down", "/<Keyboard>/s")
            .With("Left", "/<Keyboard>/a")
            .With("Right", "/<Keyboard>/d");
        action.Enable();

        Vector2? value = null;
        action.performed += ctx => { value = ctx.ReadValue<Vector2>(); };

        // Up.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.left));

        // Down left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.left + Vector2.down).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.left + Vector2.down).normalized.y).Within(0.00001));

        // Down.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.down));

        // Down right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.D));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.right));

        // Up right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.right + Vector2.up).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.right + Vector2.up).normalized.y).Within(0.00001));
    }

    private class LogInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            Debug.LogAssertion("LogInteraction.Process");
            context.Performed();
        }

        public void Reset()
        {
        }
    }

    // This is a bit of an edgy case. Actions trigger in response to controls they are bound to changing state.
    // However, in the case of composites, multiple controls may act in unison so if more than one control changes
    // state at the same time, each state monitor on the part controls will trigger independently one after the
    // other (in indeterminate order). But then, do we trigger the the action more than once or only a single time?
    // We err on the side of no surprises here and trigger the action only once.
    //
    // Note that this behavior is different from triggering the action multiple times from a single binding or
    // multiple times from different bindings that aren't part of a composite.
    //
    // Meaning for any single event:
    //      "<Keyboard>/*" -> triggers once for each pressed key
    //      "<Keyboard>/a", "<Keyboard>/b" -> triggers for both A and B if both are pressed in the event
    //      WASD composite -> triggers only once for the entire composite regardless of how many keys of WASD are pressed in the event
    [Test]
    [Category("Actions")]
    public void Actions_DpadComposite_TriggersActionOnlyOnceWhenMultipleComponentBindingsTriggerInSingleEvent()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.RegisterInteraction<LogInteraction>();

        var action = new InputAction();
        action.AddCompositeBinding("Dpad", interactions: "log")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.Enable();

        var performedCount = 0;
        action.performed += ctx => ++ performedCount;

        // Interaction should be processed only once.
        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        Assert.That(performedCount, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanUseGyroWithVector2Actions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableNormalizationOfDpadComposites()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("Dpad(normalize=false)")
            .With("Up", "/<Keyboard>/w")
            .With("Down", "/<Keyboard>/s")
            .With("Left", "/<Keyboard>/a")
            .With("Right", "/<Keyboard>/d");
        action.Enable();

        Vector2? value = null;
        action.performed += ctx => { value = ctx.ReadValue<Vector2>(); };

        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up + Vector2.left).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanSetGravityOnDpadComposites()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_WhenPartOfCompositeResolvesToMultipleControls_WhatHappensXXX()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAndDeserializeActionMapsWithCompositeBindings()
    {
        var map = new InputActionMap(name: "test");
        map.AddAction("test")
            .AddCompositeBinding("dpad")
            .With("Up", "/<Keyboard>/w")
            .With("Down", "/<Keyboard>/s")
            .With("Left", "/<Keyboard>/a")
            .With("Right", "/<Keyboard>/d");

        var json = map.ToJson();
        var deserialized = InputActionMap.FromJson(json);

        ////REVIEW: The code currently puts the composite binding itself plus all its component bindings
        ////        on the action (i.e. sets the target of each binding to the action). Should only the composite
        ////        itself reference the action?

        Assert.That(deserialized.Length, Is.EqualTo(1));
        Assert.That(deserialized[0].actions.Count, Is.EqualTo(1));
        Assert.That(deserialized[0].actions[0].bindings.Count, Is.EqualTo(5));
        Assert.That(deserialized[0].actions[0].bindings[0].path, Is.EqualTo("dpad"));
        Assert.That(deserialized[0].actions[0].bindings[0].isComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[0].isPartOfComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[1].name, Is.EqualTo("Up"));
        Assert.That(deserialized[0].actions[0].bindings[1].path, Is.EqualTo("/<Keyboard>/w"));
        Assert.That(deserialized[0].actions[0].bindings[1].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[1].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[2].name, Is.EqualTo("Down"));
        Assert.That(deserialized[0].actions[0].bindings[2].path, Is.EqualTo("/<Keyboard>/s"));
        Assert.That(deserialized[0].actions[0].bindings[2].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[2].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[3].name, Is.EqualTo("Left"));
        Assert.That(deserialized[0].actions[0].bindings[3].path, Is.EqualTo("/<Keyboard>/a"));
        Assert.That(deserialized[0].actions[0].bindings[3].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[3].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[4].name, Is.EqualTo("Right"));
        Assert.That(deserialized[0].actions[0].bindings[4].path, Is.EqualTo("/<Keyboard>/d"));
        Assert.That(deserialized[0].actions[0].bindings[4].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[4].isPartOfComposite, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotApplyOverride_WhileActionIsEnabled()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.Enable();

        Assert.That(() => action.ApplyBindingOverride("/gamepad/rightTrigger"),
            Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_OverridingWithoutGroupOrPath_OverridesAll()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger").WithGroup("b");

        action.ApplyBindingOverride("/gamepad/buttonSouth");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_CanTargetBindingByGroup()
    {
        var action = new InputAction();

        action.AddBinding("/gamepad/leftTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger").WithGroup("b");

        action.ApplyBindingOverride("/gamepad/buttonSouth", @group: "a");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[1].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_CanTargetBindingByPath()
    {
        var action = new InputAction();

        action.AddBinding("/gamepad/buttonNorth");
        action.AddBinding("/gamepad/leftTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger").WithGroup("a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", path: "/gamepad/rightTrigger");

        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.bindings[1].overridePath, Is.Null);
        Assert.That(action.bindings[2].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_CanTargetBindingByPathAndGroup()
    {
        var action = new InputAction();

        action.AddBinding("/gamepad/leftTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger");

        action.ApplyBindingOverride("/gamepad/buttonSouth", @group: "a", path: "/gamepad/rightTrigger");

        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[2].overridePath, Is.Null);
    }

    // We don't do anything smart when groups are ambiguous. If an override matches, it'll override.
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_IfGroupIsAmbiguous_OverridesAllBindingsInTheGroup()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger").WithGroup("a");
        action.AddBinding("/gamepad/rightTrigger").WithGroup("a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", @group: "a");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultsAfterOverridingBinding()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride("/gamepad/rightTrigger");
        action.RemoveAllBindingOverrides();

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ApplyingNullOverride_IsSameAsRemovingOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");

        action.ApplyBindingOverride(new InputBinding {path = "/gamepad/rightTrigger", interactions = "tap"});
        action.ApplyBindingOverride(new InputBinding());
        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.bindings[0].overrideInteractions, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ApplyingOverride_UpdatesControls()
    {
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));

        action.ApplyBindingOverride("<Gamepad>/rightTrigger");

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightTrigger));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveOverrides()
    {
        var action = new InputAction(name: "foo");
        action.Enable();
        Assert.That(() => action.RemoveAllBindingOverrides(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultForSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBinding {path = "/gamepad/rightTrigger"};

        action.ApplyBindingOverride(bindingOverride);
        action.RemoveBindingOverride(bindingOverride);

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBinding {path = "/gamepad/rightTrigger"};
        action.ApplyBindingOverride(bindingOverride);
        action.Enable();
        Assert.That(() => action.RemoveBindingOverride(bindingOverride), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices()
    {
        // Action that matches leftStick on *any* gamepad in the system.
        var action = new InputAction(binding: "/<gamepad>/leftStick");
        action.AddBinding("/keyboard/enter"); // Add unrelated binding which should not be touched.

        InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        // Add overrides to make bindings specific to #2 gamepad.
        var numOverrides = action.ApplyBindingOverridesOnMatchingControls(gamepad2);
        action.Enable();

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad2.leftStick));
        Assert.That(action.bindings[0].overridePath, Is.EqualTo(gamepad2.leftStick.path));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanOverrideBindingsWithControlsFromSpecificDevices_AndSuppressBindingsToOtherDevices()
    {
        Assert.Fail();
    }

    // The following functionality is meant in a way where you have a base action set that
    // you then clone multiple times and put overrides on each of the clones to associate them
    // with specific devices.
    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices_OnActionsInMap()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", "/<keyboard>/enter");
        var action2 = map.AddAction("action2", "/<gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var numOverrides = map.ApplyBindingOverridesOnMatchingControls(gamepad);

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo(gamepad.buttonSouth.path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableEntireMap()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        map.Enable();

        Assert.That(map.enabled);
        Assert.That(action1.enabled);
        Assert.That(action2.enabled);

        map.Disable();

        Assert.That(map.enabled, Is.False);
        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableSingleActionFromMap()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.Enable();

        Assert.That(map.enabled, Is.True); // Map is considered enabled when any of its actions are enabled.
        Assert.That(action1.enabled, Is.True);
        Assert.That(action2.enabled, Is.False);

        action1.Disable();

        Assert.That(map.enabled, Is.False);
        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneAction()
    {
        var action = new InputAction(name: "action");
        action.AddBinding("/gamepad/leftStick").WithInteraction("tap").WithGroup("group");
        action.AddBinding("/gamepad/rightStick");

        var clone = action.Clone();

        Assert.That(clone, Is.Not.SameAs(action));
        Assert.That(clone.name, Is.EqualTo(action.name));
        Assert.That(clone.id, Is.Not.EqualTo(action.id));
        Assert.That(clone.bindings, Has.Count.EqualTo(action.bindings.Count));
        Assert.That(clone.bindings[0].path, Is.EqualTo(action.bindings[0].path));
        Assert.That(clone.bindings[0].interactions, Is.EqualTo(action.bindings[0].interactions));
        Assert.That(clone.bindings[0].groups, Is.EqualTo(action.bindings[0].groups));
        Assert.That(clone.bindings[1].path, Is.EqualTo(action.bindings[1].path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningActionContainedInMap_ProducesSingletonAction()
    {
        var set = new InputActionMap("set");
        var action = set.AddAction("action1");

        var clone = action.Clone();

        Assert.That(clone.actionMap, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningEnabledAction_ProducesDisabledAction()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var clone = action.Clone();

        Assert.That(clone.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionMaps()
    {
        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1", binding: "/gamepad/leftStick", interactions: "tap");
        var action2 = map.AddAction("action2", binding: "/gamepad/rightStick", interactions: "tap");

        var clone = map.Clone();

        Assert.That(clone, Is.Not.SameAs(map));
        Assert.That(clone.name, Is.EqualTo(map.name));
        Assert.That(clone.id, Is.Not.EqualTo(map.id));
        Assert.That(clone.actions, Has.Count.EqualTo(map.actions.Count));
        Assert.That(clone.actions, Has.None.SameAs(action1));
        Assert.That(clone.actions, Has.None.SameAs(action2));
        Assert.That(clone.actions[0].name, Is.EqualTo(map.actions[0].name));
        Assert.That(clone.actions[1].name, Is.EqualTo(map.actions[1].name));
        Assert.That(clone.actions[0].id, Is.Not.EqualTo(map.actions[0].id));
        Assert.That(clone.actions[1].id, Is.Not.EqualTo(map.actions[1].id));
        Assert.That(clone.actions[0].actionMap, Is.SameAs(clone));
        Assert.That(clone.actions[1].actionMap, Is.SameAs(clone));
        Assert.That(clone.actions[0].bindings.Count, Is.EqualTo(1));
        Assert.That(clone.actions[1].bindings.Count, Is.EqualTo(1));
        Assert.That(clone.actions[0].bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(clone.actions[1].bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionAssets()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "Asset";
        var set1 = new InputActionMap("set1");
        var set2 = new InputActionMap("set2");
        asset.AddActionMap(set1);
        asset.AddActionMap(set2);

        var clone = asset.Clone();

        Assert.That(clone, Is.Not.SameAs(asset));
        Assert.That(clone.GetInstanceID(), Is.Not.EqualTo(asset.GetInstanceID()));
        Assert.That(clone.actionMaps, Has.Count.EqualTo(2));
        Assert.That(clone.actionMaps, Has.None.SameAs(set1));
        Assert.That(clone.actionMaps, Has.None.SameAs(set2));
        Assert.That(clone.actionMaps[0].name, Is.EqualTo("set1"));
        Assert.That(clone.actionMaps[1].name, Is.EqualTo("set2"));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanRebindFromUserInput()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        //var gamepad = InputSystem.AddDevice("Gamepad");

        using (var rebind = InputActionRebindingExtensions.PerformUserRebind(action))
        {
        }

        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanResolveActionReference()
    {
        var map = new InputActionMap("map");
        map.AddAction("action1");
        var action2 = map.AddAction("action2");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map", "action2");

        var referencedAction = reference.action;

        Assert.That(referencedAction, Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanResolveActionReference_EvenAfterActionHasBeenRenamed()
    {
        var map = new InputActionMap("map");
        var action = map.AddAction("oldName");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map", "oldName");

        action.Rename("newName");

        var referencedAction = reference.action;

        Assert.That(referencedAction, Is.SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAllEnabledActionsInOneGo()
    {
        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/rightStick");
        var set = new InputActionMap();
        var action3 = set.AddAction("action", "/gamepad/buttonSouth");

        action1.Enable();
        action2.Enable();
        set.Enable();

        InputSystem.DisableAllEnabledActions();

        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
        Assert.That(action3.enabled, Is.False);
        Assert.That(set.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_DisablingAllActions_RemovesAllTheirStateMonitors()
    {
        InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction(binding: "/<Gamepad>/leftStick");
        var action2 = new InputAction(binding: "/<Gamepad>/rightStick");
        var action3 = new InputAction(binding: "/<Gamepad>/buttonSouth");

        action1.Enable();
        action2.Enable();
        action3.Enable();

        InputSystem.DisableAllEnabledActions();

        // Not the most elegant test as we reach into internals here but with the
        // current API, it's not possible to enumerate monitors from outside.
        Assert.That(InputSystem.s_Manager.m_StateChangeMonitors,
            Has.All.Matches((InputManager.StateChangeMonitorsForDevice x) => x.count == 0));
    }

    // This test requires that pointer deltas correctly snap back to 0 when the pointer isn't moved.
    [Test]
    [Category("Actions")]
    public void Actions_CanDriveFreeLookFromGamepadStickAndPointerDelta()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Deadzoning alters values on the stick. For this test, get rid of it.
        InputConfiguration.DeadzoneMin = 0f;
        InputConfiguration.DeadzoneMax = 1f;

        // Same for pointer sensitivity.
        InputConfiguration.PointerDeltaSensitivity = 1f;

        var action = new InputAction();

        action.AddBinding("/<Gamepad>/leftStick");
        action.AddBinding("/<Pointer>/delta");

        Vector2? movement = null;
        action.performed +=
            ctx => { movement = ctx.ReadValue<Vector2>(); };

        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0.5).Within(0.000001));

        movement = null;
        InputSystem.QueueStateEvent(mouse, new MouseState {delta = new Vector2(0.25f, 0.25f)});
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0.25).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0.25).Within(0.000001));

        movement = null;
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0).Within(0.000001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanApplyBindingOverridesToMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", "/<keyboard>/enter");
        var action2 = map.AddAction("action2", "/<gamepad>/buttonSouth");

        var overrides = new List<InputBinding>(3)
        {
            new InputBinding {action = "action3", overridePath = "/gamepad/buttonSouth"}, // Noise.
            new InputBinding {action = "action2", overridePath = "/gamepad/rightTrigger"},
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);

        action1.Enable();
        action2.Enable();

        Assert.That(action1.bindings[0].path, Is.EqualTo("/<keyboard>/enter"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/<gamepad>/buttonSouth"));
        Assert.That(action1.bindings[0].overridePath, Is.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo("/gamepad/rightTrigger"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotApplyBindingOverridesToMap_WhenEnabled()
    {
        var map = new InputActionMap();
        map.AddAction("action1", "/<keyboard>/enter").Enable();

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        Assert.That(() => map.ApplyBindingOverrides(overrides), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveBindingOverridesFromMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", "/<keyboard>/enter");
        var action2 = map.AddAction("action2", "/<gamepad>/buttonSouth");

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action2", overridePath = "/gamepad/rightTrigger"},
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);
        overrides.RemoveAt(1); // Leave only override for action2.
        map.RemoveBindingOverrides(overrides);

        Assert.That(action1.bindings[0].overridePath, Is.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].overridePath, Is.Null); // Should have been removed.
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotRemoveBindingOverridesFromMap_WhenEnabled()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", "/<keyboard>/enter");

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);

        action1.Enable();

        Assert.That(() => map.RemoveBindingOverrides(overrides), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveAllBindingOverridesFromMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", "/<keyboard>/enter");
        var action2 = map.AddAction("action2", "/<gamepad>/buttonSouth");

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action2", overridePath = "/gamepad/rightTrigger"},
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);
        map.RemoveAllBindingOverrides();

        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.Null);
        Assert.That(action1.bindings[0].path, Is.Not.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].path, Is.Not.EqualTo("/gamepad/rightTrigger"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotRemoveAllBindingOverridesFromMap_WhenEnabled()
    {
        var map = new InputActionMap();
        var action = map.AddAction("action1", "/<keyboard>/enter");

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action1", overridePath = "/gamepad/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);

        action.Enable();

        Assert.That(() => map.RemoveAllBindingOverrides(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ExceptionsInCallbacksAreCaughtAndLogged()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap("testMap");
        var action = map.AddAction(name: "testAction", binding: "<Gamepad>/buttonSouth");
        action.performed += ctx => { throw new InvalidOperationException("TEST EXCEPTION FROM ACTION"); };
        map.actionTriggered += ctx => { throw new InvalidOperationException("TEST EXCEPTION FROM MAP"); };
        action.Enable();

        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException thrown during execution of 'Performed' callback on action 'testMap/testAction'.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM ACTION.*"));

        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException thrown during execution of callback for 'Performed' phase of 'testAction' action in map 'testMap'.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM MAP.*"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.South));
        InputSystem.Update();
    }

    class TestInteractionCheckingDefaultState : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            Debug.Log("TestInteractionCheckingDefaultState.Process");
            Assert.That(context.controlHasDefaultValue);
            Assert.That(context.control.ReadValueAsObject(), Is.EqualTo(0.1234).Within(0.00001));
        }

        public void Reset()
        {
        }
    }

    // Interactions can ask whether a trigger control is in its default state. This should respect
    // custom default state values that may be specified on controls.
    [Test]
    [Category("Actions")]
    public void Actions_InteractionContextRespectsCustomDefaultStates()
    {
        InputSystem.RegisterInteraction<TestInteractionCheckingDefaultState>();

        const string json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick/x"", ""defaultState"" : ""0.1234"" }
                ]
            }
        ";

        // Create gamepad and put leftStick/x in non-default state.
        InputSystem.RegisterLayout(json);
        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        var action = new InputAction(binding: "/<Gamepad>/leftStick/x", interactions: "testInteractionCheckingDefaultState");
        action.Enable();

        LogAssert.Expect(LogType.Log, "TestInteractionCheckingDefaultState.Process");

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.1234f, 0f)});
        InputSystem.Update();
    }

    // It's possible to associate a control layout name with an action. This is useful both for
    // characterizing the expected input behavior as well as to make control picking (both at
    // edit time and in the game) easier.
    [Test]
    [Category("Actions")]
    public void Actions_CanHaveExpectedControlLayout()
    {
        var action = new InputAction();

        Assert.That(action.expectedControlLayout, Is.Null);

        action.expectedControlLayout = "Button";

        Assert.That(action.expectedControlLayout, Is.EqualTo("Button"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_HaveStableIDs()
    {
        var action1 = new InputAction();
        var action2 = new InputAction();

        var action1Id = action1.id;
        var action2Id = action2.id;

        Assert.That(action1.id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(action2.id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(action1.id, Is.Not.EqualTo(action2.id));
        Assert.That(action1.id, Is.EqualTo(action1Id)); // Should not change.
        Assert.That(action2.id, Is.EqualTo(action2Id)); // Should not change.
    }

    [Test]
    [Category("Actions")]
    public void Actions_MapsHaveStableIDs()
    {
        var map1 = new InputActionMap();
        var map2 = new InputActionMap();

        var map1Id = map1.id;
        var map2Id = map2.id;

        Assert.That(map1.id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(map2.id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(map1.id, Is.Not.EqualTo(map2.id));
        Assert.That(map1.id, Is.EqualTo(map1Id)); // Should not change.
        Assert.That(map2.id, Is.EqualTo(map2Id)); // Should not change.
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanReferenceActionsByStableIDs()
    {
        var map = new InputActionMap();
        var action = map.AddAction("action");
        map.AddBinding("<Gamepad>/leftStick", action: action.id);

        Assert.That(action.bindings, Has.Count.EqualTo(1));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanBeArrangedInStack()
    {
        var stack = new InputActionStack();
        var action1 = new InputAction("action1");
        var action2 = new InputAction("action2");
        var action3 = new InputAction("action3");

        var map = new InputActionMap();
        var action4 = map.AddAction("action4");
        var action5 = map.AddAction("action5");

        stack.Push(action1);
        stack.Push(action2);

        Assert.That(stack.actions, Has.Count.EqualTo(2));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action2));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action2 }));

        stack.Push(map);

        Assert.That(stack.actions, Has.Count.EqualTo(4));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action2));
        Assert.That(stack.actions[2], Is.SameAs(action4));
        Assert.That(stack.actions[3], Is.SameAs(action5));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action2, action4, action5 }));

        stack.Push(action3);

        Assert.That(stack.actions, Has.Count.EqualTo(5));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action2));
        Assert.That(stack.actions[2], Is.SameAs(action4));
        Assert.That(stack.actions[3], Is.SameAs(action5));
        Assert.That(stack.actions[4], Is.SameAs(action3));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action2, action4, action5, action3 }));

        stack.Pop();

        Assert.That(stack.actions, Has.Count.EqualTo(4));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action2));
        Assert.That(stack.actions[2], Is.SameAs(action4));
        Assert.That(stack.actions[3], Is.SameAs(action5));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action2, action4, action5 }));

        stack.Pop(action2);

        Assert.That(stack.actions, Has.Count.EqualTo(3));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action4));
        Assert.That(stack.actions[2], Is.SameAs(action5));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action4, action5 }));

        stack.Pop();

        Assert.That(stack.actions, Has.Count.EqualTo(2));
        Assert.That(stack.actions[0], Is.SameAs(action1));
        Assert.That(stack.actions[1], Is.SameAs(action4));
        Assert.That(stack.ToList(), Is.EquivalentTo(new[] { action1, action4 }));

        stack.Clear();

        Assert.That(stack.actions, Is.Empty);
        Assert.That(stack.ToList(), Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ArrangedInStack_CanBeEnabledAndDisabledInBulk()
    {
        var stack = new InputActionStack();
        var action1 = new InputAction("action1");
        var action2 = new InputAction("action2");
        var action3 = new InputAction("action3");

        stack.Push(action1);
        stack.Push(action2);

        stack.Enable();

        Assert.That(stack.enabled);
        Assert.That(action1.enabled);
        Assert.That(action2.enabled);

        stack.Push(action3);

        Assert.That(action3.enabled);

        stack.Disable();

        Assert.That(stack.enabled, Is.False);
        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
        Assert.That(action3.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ArrangedInStack_OverrideEachOthersBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var stack = new InputActionStack();
        var action1 = new InputAction("action1", binding: "<Gamepad>/buttonSouth");
        var action2 = new InputAction("action2", binding: "<Gamepad>/buttonSouth");

        var action1Performed = false;
        var action2Performed = false;

        action1.performed += ctx => action1Performed = true;
        action2.performed += ctx => action2Performed = true;

        stack.Push(action1);
        stack.Push(action2);

        stack.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.South));
        InputSystem.Update();

        Assert.That(action1Performed, Is.False);
        Assert.That(action2Performed, Is.True);

        stack.Pop(action2);

        action1Performed = false;
        action2Performed = false;

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(action1Performed, Is.True);
        Assert.That(action2Performed, Is.False);
    }
}
