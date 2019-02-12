using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Composites;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Property = NUnit.Framework.PropertyAttribute;

#pragma warning disable CS0649
[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]

// As should be obvious from the number of tests in here, the action system rivals the entire combined rest of the system
// in terms of complexity.
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
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSameControlWithMultipleActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction("action1", binding: "<Gamepad>/buttonSouth");
        var action2 = new InputAction("action2", binding: "<Gamepad>/buttonSouth");

        var map = new InputActionMap("map");
        var action3 = map.AddAction("action3", binding: "<Gamepad>/buttonSouth");
        var action4 = map.AddAction("action4", binding: "<Gamepad>/buttonSouth");

        action1.Enable();
        action2.Enable();
        map.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            Press(gamepad.buttonSouth);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(4 * 2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .SameAs(action1).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .SameAs(action1).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .SameAs(action2).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .SameAs(action2).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .SameAs(action3).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .SameAs(action3).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .SameAs(action4).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .SameAs(action4).And.Property("control").SameAs(gamepad.buttonSouth));
        }
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
    public void Actions_CanDetermineIfMapIsUsableWithGivenDevice()
    {
        var map = new InputActionMap();
        var action = map.AddAction("action1");
        action.AddBinding("<Gamepad>/leftStick");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(map.IsUsableWithDevice(gamepad), Is.True);
        Assert.That(map.IsUsableWithDevice(keyboard), Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenDisabled_CancelAllStartedInteractions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction("action1", binding: "<Gamepad>/buttonSouth", interactions: "Hold");
        var action2 = new InputAction("action2", binding: "<Gamepad>/leftStick");
        var action3 = new InputAction("action3", binding: "<Gamepad>/rightStick");

        action1.Enable();
        action2.Enable();
        action3.Enable();

        Press(gamepad.buttonSouth);
        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action1);
            trace.SubscribeTo(action2);
            trace.SubscribeTo(action3);

            runtime.currentTime = 0.234f;
            runtime.advanceTimeEachDynamicUpdate = 0;

            action1.Disable();
            action2.Disable();
            action3.Disable();

            var actions = trace.ToArray();

            Assert.That(actions.Length, Is.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].time, Is.EqualTo(0.234).Within(0.00001));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[1].time, Is.EqualTo(0.234).Within(0.00001));
            Assert.That(actions[1].action, Is.SameAs(action2));
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].interaction, Is.Null);

            trace.Clear();

            // Re-enable an action and make sure that it indeed starts from scratch again.
            // Note that the button is still held so no input required.

            action1.Enable();

            Assert.That(action1.phase, Is.EqualTo(InputActionPhase.Waiting));

            runtime.currentTime = 0.345f;
            InputSystem.Update();

            actions = trace.ToArray();

            Assert.That(actions.Length, Is.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].time, Is.EqualTo(0.345).Within(0.00001));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());

            trace.Clear();

            runtime.currentTime = 1;
            InputSystem.Update();

            actions = trace.ToArray();

            Assert.That(actions.Length, Is.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].time, Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());
        }
    }

    // Controls may already be actuated when we enable an action. To deal with this, we pretend that at
    // the time an action is enabled, any bound control that isn't at default
    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_ReactToCurrentValueOfControlsInNextUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));
        Press(gamepad.buttonSouth);

        var actionWithoutInteraction = new InputAction("ActionWithoutInteraction", binding: "<Gamepad>/leftStick");
        var actionWithHold = new InputAction("ActionWithHold", binding: "<Gamepad>/buttonSouth", interactions: "Hold");
        var actionThatShouldNotTrigger = new InputAction("ActionThatShouldNotTrigger", binding: "<Gamepad>/rightStick");

        actionWithHold.performed += ctx => Assert.Fail("Hold should not complete");
        actionThatShouldNotTrigger.started += ctx => Assert.Fail("Action should not start");
        actionThatShouldNotTrigger.performed += ctx => Assert.Fail("Action should not be performed");

        using (var trace1 = new InputActionTrace())
        using (var trace2 = new InputActionTrace())
        {
            trace1.SubscribeTo(actionWithoutInteraction);
            trace2.SubscribeTo(actionWithHold);

            actionWithoutInteraction.Enable();
            actionWithHold.Enable();
            actionThatShouldNotTrigger.Enable();

            Assert.That(trace1, Is.Empty);
            Assert.That(trace2, Is.Empty);

            InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.234f, 0.345f));
            InputSystem.Update();

            var actions1 = trace1.ToArray();
            var actions2 = trace2.ToArray();

            Assert.That(actions1, Has.Length.EqualTo(3));
            Assert.That(actions2, Has.Length.EqualTo(1));

            Assert.That(actions1[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions1[0].action, Is.SameAs(actionWithoutInteraction));
            Assert.That(actions1[0].interaction, Is.Null);
            Assert.That(actions1[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions1[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions1[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions1[1].action, Is.SameAs(actionWithoutInteraction));
            Assert.That(actions1[1].interaction, Is.Null);
            Assert.That(actions1[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions1[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions1[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions1[2].action, Is.SameAs(actionWithoutInteraction));
            Assert.That(actions1[2].interaction, Is.Null);
            Assert.That(actions1[2].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions1[2].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));

            Assert.That(actions2[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions2[0].action, Is.SameAs(actionWithHold));
            Assert.That(actions2[0].interaction, Is.TypeOf<HoldInteraction>());
            Assert.That(actions2[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions2[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_TriggerNotification()
    {
        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        new InputActionMap("map2").AddAction("action3"); // Noise.

        var received = new List<object>();
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            received.Add(change);
            received.Add(obj);
        };

        // Enable map.
        map.Enable();

        Assert.That(received, Is.EquivalentTo(new object[]
        {
            InputActionChange.ActionMapEnabled,
            map,
            InputActionChange.BoundControlsChanged,
            map
        }));

        received.Clear();

        // Enabling action in map should not trigger notification.
        action1.Enable();

        Assert.That(received, Is.Empty);

        // Disable map.
        map.Disable();

        Assert.That(received, Is.EquivalentTo(new object[]
        {
            InputActionChange.ActionMapDisabled,
            map,
        }));

        received.Clear();

        // Enable single action.
        action2.Enable();

        Assert.That(received, Is.EquivalentTo(new object[]
        {
            InputActionChange.ActionEnabled,
            action2,
        }));

        received.Clear();

        // Disable single action.
        action2.Disable();

        Assert.That(received, Is.EquivalentTo(new object[]
        {
            InputActionChange.ActionDisabled,
            action2,
        }));

        received.Clear();

        // Disabling single action that isn't enabled should not trigger notification.
        action2.Disable();

        Assert.That(received, Is.Empty);
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

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
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
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)).x).Within(0.00001));
        Assert.That(BitConverter.ToSingle(receivedValueData, 4),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)).y).Within(0.00001));

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

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
        InputSystem.Update();

        Assert.That(receivedCall, Is.True);
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ReadingValueOfIncorrectType_FromComposite_ThrowsHelpfulException()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction("TestAction");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/1")
            .With("Positive", "<Keyboard>/2");

        action.Enable();

        var performedWasCalled = false;
        action.performed += ctx =>
        {
            Assert.That(ctx.ReadValue<Vector2>, Throws.Exception.With
                .Message.Contains("TestAction").And.With
                .Message.Contains("Cannot read value of type 'Vector2'").And.With
                .Message.Contains($"from control '{keyboard.digit1Key}'").And.With
                .Message.Contains("with value type 'float'"));
            performedWasCalled = true;
        };

        Press(keyboard.digit2Key);

        Assert.That(performedWasCalled);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionMap()
    {
        var action = new InputAction();

        Assert.That(action.actionMap, Is.Null);
    }

    // This test is chiefly to make sure that the various pieces of indexing we do in InputActionState
    // end up leading to the right data once we have a setup that involves several action maps each with
    // a separate set of actions and enabled/disabled at different times. We go through a relatively complex
    // number of steps to make sure we hit a good number of code paths so in a way this is a bit of a catch-all
    // kind of sanity test.
    //
    // Another thing this test is useful for is that it has to spell out some fairly complex state changes
    // to the action system and spell out what exactly happens when we progress from one kind of setup to another.
    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionAssetWithMultipleActionMaps()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");
        var map3 = new InputActionMap("map3");
        var map4 = new InputActionMap("map4");

        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");
        var action4 = map3.AddAction("action4");
        var action5 = map3.AddAction("action5");
        var action6 = map3.AddAction("action6");
        var action7 = map4.AddAction("action7");

        action1.AddBinding("<Gamepad>/leftStick").WithProcessor("invertVector2(invertY=false)");
        action2.AddBinding("<Gamepad>/buttonSouth", interactions: "Tap");
        action3.AddBinding("<Gamepad>/buttonSouth", interactions: "Tap");
        action4.AddBinding("<Gamepad>/leftStick").WithProcessor("invertVector2(invertX=false)");
        action5.AddBinding("<Gamepad>/buttonSouth", interactions: "Tap");
        action6.AddBinding("<Gamepad>/leftTrigger").WithProcessor("invert");
        action7.AddBinding("<Gamepad>/leftTrigger").WithProcessor("clamp(min=0,max=0.5)");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);
        asset.AddActionMap(map3);
        asset.AddActionMap(map4);

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            // Enable only map1.
            map1.Enable();

            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f), 0.123);

            var actions = trace.ToArray();

            // map1/action1 should have been started and performed.
            Assert.That(actions.Length, Is.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(-1, 1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].interaction, Is.Null);
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[0].time, Is.EqualTo(0.123).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].action, Is.SameAs(action1));
            Assert.That(actions[1].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(-1, 1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].interaction, Is.Null);
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].time, Is.EqualTo(0.123).Within(0.00001));

            trace.Clear();

            runtime.currentTime = 0.234f;

            // Disable map1 and enable map2+map3.
            map1.Disable();
            map2.Enable();
            map3.Enable();

            Press(gamepad.buttonSouth, 0.345f);
            Set(gamepad.leftStick, new Vector2(0.234f, 0.345f), 0.456f);

            actions = trace.ToArray();
            Assert.That(actions.Length, Is.EqualTo(6));

            // map1/action1 should have been cancelled.
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(-1, 1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].interaction, Is.Null);
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[0].time, Is.EqualTo(0.234f));

            // map3/action4 should immediately start as the stick was already actuated
            // when we enabled the action.
            // NOTE: We get a different value here than what action1 got as we have a different
            //       processor on the binding.
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[1].action, Is.SameAs(action4));
            Assert.That(actions[1].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(1, -1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].interaction, Is.Null);
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].time, Is.EqualTo(0.234).Within(0.00001));

            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].action, Is.SameAs(action4));
            Assert.That(actions[2].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(1, -1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[2].interaction, Is.Null);
            Assert.That(actions[2].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[2].time, Is.EqualTo(0.234).Within(0.00001));

            // map2/action3 should have been started.
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[3].action, Is.SameAs(action3));
            Assert.That(actions[3].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[3].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[3].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[3].time, Is.EqualTo(0.345).Within(0.00001));

            // map3/action5 should have been started.
            Assert.That(actions[4].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[4].action, Is.SameAs(action5));
            Assert.That(actions[4].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[4].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[4].interaction, Is.Not.SameAs(actions[1].interaction));
            Assert.That(actions[4].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[4].time, Is.EqualTo(0.345).Within(0.00001));

            // map3/action4 should have been performed as the stick has been moved
            // beyond where it had already moved.
            Assert.That(actions[5].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[5].action, Is.SameAs(action4));
            Assert.That(actions[5].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)) * new Vector2(1, -1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[5].interaction, Is.Null);
            Assert.That(actions[5].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[5].time, Is.EqualTo(0.456).Within(0.00001));

            trace.Clear();

            // Disable all maps.
            map2.Disable();
            map3.Disable();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(3));

            // map2/action3 should have been cancelled.
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].action, Is.SameAs(action3));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[0].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(runtime.currentTime).Within(0.00001));

            // map3/action3 should have been cancelled.
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[1].action, Is.SameAs(action4));
            Assert.That(actions[1].ReadValue<Vector2>,
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)) * new Vector2(1, -1))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].interaction, Is.Null);
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].time, Is.EqualTo(runtime.currentTime).Within(0.00001));

            // map3/action5 should have been cancelled.
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[2].action, Is.SameAs(action5));
            Assert.That(actions[2].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[2].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[2].interaction, Is.Not.SameAs(actions[1].interaction));
            Assert.That(actions[2].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[2].time, Is.EqualTo(runtime.currentTime).Within(0.00001));

            trace.Clear();

            Set(gamepad.buttonSouth, 0, 1.23);
            Set(gamepad.buttonSouth, 0, 1.34);

            Assert.That(trace, Is.Empty);
        }
    }

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
    public void Actions_LooseActionHasNoMap()
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

        var action = new InputAction(binding: "<Gamepad>/leftStick");
        action.performed +=
            ctx =>
        {
            ++receivedCalls;
            receivedAction = ctx.action;
            receivedControl = ctx.control;

            Assert.That(ctx.phase, Is.EqualTo(InputActionPhase.Performed));
        };
        action.Enable();

        // Actuate stick.
        Set(gamepad.leftStick, new Vector2(0.5f, 0.5f));

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        // Action should be started.
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

        receivedCalls = 0;

        // Reset stick.
        Set(gamepad.leftStick, Vector2.zero);

        Assert.That(receivedCalls, Is.Zero);

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
    }

    // It can be very desirable to not let actions try to do anything smart with the control state changes
    // they are seeing but rather just pass them on as is. By enabling 'passhthrough' mode on an action,
    [Test]
    [Category("Actions")]
    public void Actions_CanByPassControlActuationChecks_UsingPasshtroughMode()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/*stick");
        action.passThrough = true;
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            Set(gamepad.leftStick, new Vector2(0.234f, 0.345f));

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            Set(gamepad.rightStick, new Vector2(0.123f, 0.234f));

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            Set(gamepad.rightStick, Vector2.zero);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightStick));
            Assert.That(actions[0].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        }
    }

    ////REVIEW: Is this kind of conflict resolution enough? If, for example, you bind two gyros to an action,
    ////        the preferable resolution would probably be to ignore input from one. Do we want to be able
    ////        to choose the style of conflict resolution on a per action basis?

    // If an action has multiple bound controls, we're looking at the possibility of seeing them
    // actuated concurrently. If that happens, we don't want the controls to stomp over each and
    // drive interactions in a conflicting way.
    //
    // What we do is always stick to the control that has the greatest amount of actuation. So,
    // say you have an action that has two buttons bound to it. If you press one and it starts
    // the action and then you press the other and release the first, the action remains started.
    //
    // In other words, actions care more about values coming in than they do about who/what generates
    // those values.
    //
    // NOTE: The behavior here is more important than one might initially expect. One example of a
    //       use case is having primary and alternate bindings and not having them conflict with each
    //       other. Another use case is having both a left-hand and a right-hand device and allowing the
    //       same action to be triggered from either side. Without the conflict resolution here, whenever
    //       there is concurrent activity the result would be indeterminate response to player interaction
    //       and the player thus not knowing which control is authoritative at a given point. Conflict
    //       resolution removes the ambiguity by going with greatest actuation.
    //
    // NOTE: Conflict resolution will not help with controls that have no inherent level of actuation (i.e.
    //       that do not implement EvaluateMagnitude() in a meaningful way). For these kinds of input, it
    //       probably best to enable pass-through mode to disable conflict resolution altogether.
    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
    public void Actions_WithMultipleBoundControls_DriveInteractionsFromControlWithGreatestActuation()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // We go through several permutations of the same behavior all in one test. Makes the
        // test bulky but the underlying behavior is all the same.
        var stickAction = new InputAction("stickAction", binding: "<Gamepad>/*stick");
        var buttonAction = new InputAction("buttonAction", binding: "<Gamepad>/button*"); // Grabs face buttons only.
        var holdAction = new InputAction("holdAction", binding: "<Gamepad>/*shoulder", interactions: "hold");
        var compositeAction = new InputAction("compositeAction");
        compositeAction.AddCompositeBinding("Axis")
            .With("Negative", "<Gamepad>/leftTrigger")
            .With("Positive", "<Gamepad>/rightTrigger");
        compositeAction.AddCompositeBinding("Axis")
            .With("Negative", "<Gamepad>/dpad/left")
            .With("Positive", "<Gamepad>/dpad/right");

        stickAction.Enable();
        buttonAction.Enable();
        holdAction.Enable();
        compositeAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            // First actuate both sticks below deadzone threshold and make sure we do NOT
            // get any action as a result.
            Set(gamepad.leftStick, new Vector2(0.01f, 0.02f));
            Set(gamepad.rightStick, new Vector2(0.02f, 0.01f));

            Assert.That(trace, Is.Empty);

            // Now move the left stick out of its deadzone and make sure the stick action
            // got started.
            Set(gamepad.leftStick, new Vector2(0.345f, 0.456f));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[0].action, Is.SameAs(stickAction));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f))).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].action, Is.SameAs(stickAction));
            Assert.That(actions[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f))).Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            // Next move the right stick out of its deadzone but below the value we moved the left stick
            // to. Make sure the action is UNAFFECTED.
            Set(gamepad.rightStick, new Vector2(0.234f, 0.345f));

            Assert.That(trace, Is.Empty);

            // Now actuate the right stick more than the left stick. We should see the right stick
            // taking over stickAction.
            Set(gamepad.rightStick, new Vector2(0.456f, 0.567f));

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightStick));
            Assert.That(actions[0].action, Is.SameAs(stickAction));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.456f, 0.567f))).Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            // Reset the left stick. stickAction should be UNAFFECTED.
            Set(gamepad.leftStick, Vector2.zero);

            Assert.That(trace, Is.Empty);

            // Finally, reset the right stick. stickAction should be cancelled.
            Set(gamepad.rightStick, Vector2.zero);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightStick));
            Assert.That(actions[0].action, Is.SameAs(stickAction));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            // Actuate the left trigger and then dpad/right. This should result in compositeAction being
            // started from the trigger and then be UNAFFECTED by dpad/right (the value of the composite
            // points the opposite way but the magnitude is identical).
            Set(gamepad.leftTrigger, 1.0f);
            Press(gamepad.dpad.right);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftTrigger));
            Assert.That(actions[0].action, Is.SameAs(compositeAction));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(-1).Within(0.00001)); // Negative because of axis composite.
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftTrigger));
            Assert.That(actions[1].action, Is.SameAs(compositeAction));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(-1).Within(0.00001));

            trace.Clear();

            // Now release the left trigger. dpad/right should take over and keep the action going.
            Set(gamepad.leftTrigger, 0);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            // Conflict resolution for composites does NOT keep track of which individual control in a composite
            // triggered last but rather locks on to the first control in the composite.
            ////REVIEW: do we care enough to want the right control here?
            Assert.That(actions[0].control, Is.SameAs(gamepad.dpad.left));
            Assert.That(actions[0].action, Is.SameAs(compositeAction));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));

            trace.Clear();

            // Finally, release dpad/right, too. The action should cancel.
            Release(gamepad.dpad.right);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            // Same as above. Conflict resolution locks us to first control in composite.
            Assert.That(actions[0].control, Is.SameAs(gamepad.dpad.left));
            Assert.That(actions[0].action, Is.SameAs(compositeAction));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.00001));

            trace.Clear();

            // Press both shoulder buttons and then release one of them. This should leave
            // the hold still going.
            Press(gamepad.leftShoulder);
            Press(gamepad.rightShoulder);
            Release(gamepad.leftShoulder);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftShoulder));
            Assert.That(actions[0].action, Is.SameAs(holdAction));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));

            trace.Clear();

            // Now release the other shoulder button such that it performs a hold. This
            // should complete the action.
            runtime.currentTime += 10;
            Release(gamepad.rightShoulder);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            // Again, conflict resolution locking us to first control in composite.
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftShoulder));
            Assert.That(actions[0].action, Is.SameAs(holdAction));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.00001));

            trace.Clear();

            // Press all face buttons and then release them one by one. After the last was released,
            // buttonAction should be cancelled.
            Press(gamepad.buttonSouth);
            Press(gamepad.buttonNorth);
            Press(gamepad.buttonEast);
            Press(gamepad.buttonWest);

            Release(gamepad.buttonSouth);
            Release(gamepad.buttonEast);
            Release(gamepad.buttonWest);
            Release(gamepad.buttonNorth);

            // This one is a little tricky. When we release one control and the system switches to another
            // that now has a higher actuation level, it will process that switch like a normal control actuation.
            // It does so as the *value* of the control that we're switching to may be entirely different than
            // the one that was driving the action before (the system only compares magnitudes which do not necessarily
            // say anything about the effective value of a control).
            //
            // So what happens when we release buttonSouth is that the system now searches for another control
            // with higher actuation. Which one it finds is entirely driven by ordering of controls. Our "<Gamepad>/*button"
            // binding makes that order dependent on the order of controls in Gamepad.
            //
            // So in effect, we get a sequence of Performed calls here that is a little surprising.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(5));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].action, Is.SameAs(buttonAction));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[1].action, Is.SameAs(buttonAction));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].control, Is.SameAs(gamepad.buttonWest)); // Control immediately following buttonSouth in list of controls.
            Assert.That(actions[2].action, Is.SameAs(buttonAction));
            Assert.That(actions[2].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Performed)); // Control following buttonWest in list of controls.
            Assert.That(actions[3].control, Is.SameAs(gamepad.buttonNorth));
            Assert.That(actions[3].action, Is.SameAs(buttonAction));
            Assert.That(actions[3].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[4].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[4].control, Is.SameAs(gamepad.buttonNorth));
            Assert.That(actions[4].action, Is.SameAs(buttonAction));
            Assert.That(actions[4].ReadValue<float>(), Is.Zero.Within(0.00001));
        }
    }

    // There can be situations where two different controls are driven from the state. Most prominently, this is
    // the case with the Pointer.button control that subclasses usually rewrite to whatever their primary button is.
    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
    [Ignore("TODO")]
    public void TODO_Actions_WithMultipleActuationsFromStateState_()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var action = new InputAction(binding: "<Mouse>/*button");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Press(mouse.leftButton);
        }

        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_WithMultipleBoundComposites_DriveInteractionsFromCompositeWithGreatestActuation()
    {
        Assert.Fail();
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

        var wasStarted = false;
        var wasPerformed = false;
        var wasCancelled = false;

        map.actionTriggered +=
            ctx =>
        {
            Assert.That(ctx.action, Is.SameAs(action));
            Assert.That(ctx.control, Is.SameAs(gamepad.leftTrigger));

            switch (ctx.phase)
            {
                case InputActionPhase.Started:
                    Assert.That(wasStarted, Is.False);
                    Assert.That(wasPerformed, Is.False);
                    Assert.That(wasCancelled, Is.False);
                    wasStarted = true;
                    break;
                case InputActionPhase.Performed:
                    Assert.That(wasStarted, Is.True);
                    Assert.That(wasPerformed, Is.False);
                    Assert.That(wasCancelled, Is.False);
                    wasPerformed = true;
                    break;
                case InputActionPhase.Cancelled:
                    Assert.That(wasStarted, Is.True);
                    Assert.That(wasPerformed, Is.True);
                    Assert.That(wasCancelled, Is.False);
                    wasCancelled = true;
                    break;
            }
        };

        map.Enable();

        Set(gamepad.leftTrigger, 0.5f);

        Assert.That(wasStarted, Is.True);
        Assert.That(wasPerformed, Is.True);
        Assert.That(wasCancelled, Is.False);

        Set(gamepad.leftTrigger, 0);

        Assert.That(wasCancelled, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenTriggered_TriggerGlobalNotification()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        action.Enable();

        var receivedStarted = false;
        var receivedPerformed = false;
        var receivedCancelled = false;

        action.started += ctx => receivedStarted = true;
        action.performed += ctx => receivedPerformed = true;
        action.cancelled += ctx => receivedCancelled = true;

        var receivedChanges = new List<InputActionChange>();
        InputSystem.onActionChange +=
            (a, c) =>
        {
            Assert.That(a, Is.SameAs((InputAction)a));

            // Notification must come *before* callback.
            switch (((InputAction)a).phase)
            {
                case InputActionPhase.Started:
                    Assert.That(receivedStarted, Is.False);
                    break;
                case InputActionPhase.Cancelled:
                    Assert.That(receivedCancelled, Is.False);
                    break;
                case InputActionPhase.Performed:
                    Assert.That(receivedPerformed, Is.False);
                    break;
            }

            receivedChanges.Add(c);

            // lastXXX state on action must have been updated.
            Assert.That(((InputAction)a).lastTriggerControl, Is.SameAs(gamepad.leftTrigger));
        };

        Set(gamepad.leftTrigger, 0.5f);

        Assert.That(receivedChanges,
            Is.EquivalentTo(new[] {InputActionChange.ActionStarted, InputActionChange.ActionPerformed}));

        receivedChanges.Clear();
        receivedStarted = false;
        receivedPerformed = false;
        receivedCancelled = false;

        Set(gamepad.leftTrigger, 0);

        Assert.That(receivedChanges,
            Is.EquivalentTo(new[] {InputActionChange.ActionCancelled}));
    }

    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
    public void Actions_CanRecordActions()
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

        using (var trace = new InputActionTrace())
        {
            action.performed += trace.RecordAction;

            var state = new GamepadState {leftStick = new Vector2(0.123f, 0.234f)};
            InputSystem.QueueStateEvent(gamepad, state, 0.1234);
            state.rightStick = new Vector2(0.345f, 0.456f);
            InputSystem.QueueStateEvent(gamepad, state, 0.2345);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W), 0.0987);
            InputSystem.Update();

            Assert.That(trace.count, Is.EqualTo(3));

            var events = trace.ToArray();

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
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(events[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(events[2].ReadValue<Vector2>(), Is.EqualTo(Vector2.up).Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            Assert.That(trace.count, Is.Zero);
            Assert.That(trace.ToArray(), Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordActions_FromMultipleMaps()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map1 = new InputActionMap();
        var map2 = new InputActionMap();

        var action1_1 = map1.AddAction("action1", binding: "<Gamepad>/leftStick");
        var action1_2 = map1.AddAction("action2", binding: "<Keyboard>/a");
        var action2_1 = map2.AddAction("action3", binding: "<Gamepad>/buttonSouth");

        action1_1.Enable();
        action1_2.Enable();
        action2_1.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(map1);
            trace.SubscribeTo(map2);

            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.one }.WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(3 * 2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .EqualTo(action1_1).And.Property("control").SameAs(gamepad.leftStick));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .EqualTo(action1_1).And.Property("control").SameAs(gamepad.leftStick));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .EqualTo(action1_2).And.Property("control").SameAs(keyboard.aKey));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .EqualTo(action1_2).And.Property("control").SameAs(keyboard.aKey));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Started).And.Property("action")
                    .EqualTo(action2_1).And.Property("control").SameAs(gamepad.buttonSouth));
            Assert.That(actions,
                Has.Exactly(1).With.Property("phase").EqualTo(InputActionPhase.Performed).And.Property("action")
                    .EqualTo(action2_1).And.Property("control").SameAs(gamepad.buttonSouth));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordActions_AndReadTheDataEvenIfBindingsHaveChanged()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var action = new InputAction();
        action.AddBinding("<Keyboard>/a").WithGroup("B");
        action.AddBinding("<Gamepad>/buttonSouth").WithGroup("A").WithGroup("B");
        action.AddBinding("<Mouse>/button").WithGroup("C");

        // Enable only gamepad binding.
        action.bindingMask = new InputBinding {groups = "A"};
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            Assert.That(action.controls, Is.EquivalentTo(new[] {gamepad.buttonSouth}));

            Press(gamepad.buttonSouth);

            // Enable both keyboard and gamepad binding.
            action.bindingMask = new InputBinding {groups = "B"};

            Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.aKey, gamepad.buttonSouth}));

            Release(gamepad.buttonSouth);
            Press(keyboard.aKey);

            // Disable both keyboard and gamepad binding by switching to mouse binding.
            action.bindingMask = new InputBinding {groups = "C"};

            Assert.That(action.controls, Is.EquivalentTo(new[] {mouse.button}));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(9));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.EqualTo(gamepad.buttonSouth));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.EqualTo(gamepad.buttonSouth));
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[2].control, Is.EqualTo(gamepad.buttonSouth));
            // The second start-perform-cancel cycle comes from the fact that we are changing the
            // binding mask. Doing so will cancel all ongoing actions. But because the gamepad button
            // is still pressed and still bound after the binding mask change, the next update will
            // restart the action from the gamepad button.
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[3].control, Is.EqualTo(gamepad.buttonSouth));
            Assert.That(actions[4].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[4].control, Is.EqualTo(gamepad.buttonSouth));
            Assert.That(actions[5].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[5].control, Is.EqualTo(gamepad.buttonSouth));
            Assert.That(actions[6].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[6].control, Is.EqualTo(keyboard.aKey));
            Assert.That(actions[7].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[7].control, Is.EqualTo(keyboard.aKey));
            Assert.That(actions[8].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[8].control, Is.EqualTo(keyboard.aKey));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordActions_AndReadValueAsObject()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            action.performed += trace.RecordAction;

            Set(gamepad.leftTrigger, 0.123f);

            var actions = trace.ToArray();

            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordAllActionsInTheSystem()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();

        var action1 = map.AddAction("action1", binding: "<Gamepad>/leftStick");
        var action2 = map.AddAction("action2", binding: "<Gamepad>/leftStick");
        var action3 = new InputAction("action3", binding: "<Gamepad>/leftStick");

        action1.Enable();
        action2.Enable();
        action3.Enable();

        using (var trace = new InputActionTrace())
        {
            // This will record any action being triggered anywhere.
            trace.SubscribeToAll();

            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(6));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action1).And.Property("phase").EqualTo(InputActionPhase.Started));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action1).And.Property("phase").EqualTo(InputActionPhase.Performed));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action2).And.Property("phase").EqualTo(InputActionPhase.Started));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action2).And.Property("phase").EqualTo(InputActionPhase.Performed));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action3).And.Property("phase").EqualTo(InputActionPhase.Started));
            Assert.That(actions, Has.Exactly(1).With.Property("action").SameAs(action3).And.Property("phase").EqualTo(InputActionPhase.Performed));

            trace.Clear();

            trace.UnsubscribeFromAll();

            Set(gamepad.leftStick, Vector2.zero);

            Assert.That(trace, Is.Empty);
        }
    }

    // Actions are able to observe every state change, even if the changes occur within
    // the same frame.
    [Test]
    [Category("Actions")]
    public void Actions_PressingAndReleasingButtonInSameUpdate_StillTriggersAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/<button>");

        var receivedCalls = 0;
        action.performed +=
            ctx => { ++receivedCalls; };
        action.Enable();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadButton.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformPressInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Test all three press behaviors concurrently.
        var pressAction = new InputAction("PressOnly", binding: "<Gamepad>/buttonSouth", interactions: "press");
        var releaseAction = new InputAction("ReleaseOnly", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=1)");
        var pressAndReleaseAction = new InputAction("PressAndRelease", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=2)");

        pressAction.Enable();
        releaseAction.Enable();
        pressAndReleaseAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            Press(gamepad.buttonSouth);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));

            trace.Clear();

            Release(gamepad.buttonSouth);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(releaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Cancelled));

            trace.Clear();

            Press(gamepad.buttonSouth);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformContinuousPressInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var pressAction = new InputAction("PressOnly", binding: "<Gamepad>/buttonSouth", interactions: "press");
        var releaseAction = new InputAction("ReleaseOnly", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=1)");
        var pressAndReleaseAction = new InputAction("PressAndRelease", binding: "<Gamepad>/buttonSouth", interactions: "press(behavior=2)");

        pressAction.continuous = true;
        releaseAction.continuous = true; // ReleaseOnly doesn't care about continuous.
        pressAndReleaseAction.continuous = true;

        pressAction.Enable();
        releaseAction.Enable();
        pressAndReleaseAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            Press(gamepad.buttonSouth);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));

            trace.Clear();

            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));

            trace.Clear();

            Release(gamepad.buttonSouth);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(releaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Performed));
            Assert.That(actions,
                Has.Exactly(1).With.Property("action").SameAs(pressAndReleaseAction).And.With.Property("phase")
                    .EqualTo(InputActionPhase.Cancelled));

            trace.Clear();

            InputSystem.Update();

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
    public void Actions_CanPerformHoldInteraction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var cancelledReceivedCalls = 0;
        InputAction cancelledAction = null;
        InputControl cancelledControl = null;

        var action = new InputAction(binding: "<Gamepad>/{primaryAction}", interactions: "hold(duration=0.4)");
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
        action.cancelled +=
            ctx =>
        {
            ++cancelledReceivedCalls;
            cancelledAction = ctx.action;
            cancelledControl = ctx.control;

            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Cancelled));
        };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(cancelledReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 0.25);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.Zero);
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(cancelledReceivedCalls, Is.EqualTo(1));
        Assert.That(cancelledAction, Is.SameAs(action));
        Assert.That(cancelledControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));

        cancelledReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South), 0.5);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(cancelledReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 10);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.Zero);
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(cancelledReceivedCalls, Is.Zero);
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
    }

    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].interaction, Is.TypeOf<MultiTapInteraction>());
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].time, Is.EqualTo(4).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0).Within(0.00001));// Button isn't pressed currently.

            trace.Clear();

            // Now press and release within tap time. Then press again within delay time but release
            // only after tap time. Should we started and cancelled.
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
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Cancelled));
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
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        action.Enable();

        var enabledActions = InputSystem.ListEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAction()
    {
        var action = new InputAction(name: "MyAction", binding: "<Gamepad>/leftStick");

        // Unity's JSON serializer goes through Unity's normal serialization machinery so if
        // this works, we should have a pretty good shot that binary and YAML serialization
        // are also working.
        var json = JsonUtility.ToJson(action);
        var deserializedAction = JsonUtility.FromJson<InputAction>(json);

        Assert.That(deserializedAction.name, Is.EqualTo(action.name));
        Assert.That(deserializedAction.bindings, Has.Count.EqualTo(1));
        Assert.That(deserializedAction.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeActionMap()
    {
        var map = new InputActionMap("set");

        map.AddAction("action1", binding: "<Gamepad>/leftStick");
        map.AddAction("action2", binding: "<Gamepad>/rightStick");

        var json = JsonUtility.ToJson(map);
        var deserializedSet = JsonUtility.FromJson<InputActionMap>(json);

        Assert.That(deserializedSet.name, Is.EqualTo("set"));
        Assert.That(deserializedSet.actions, Has.Count.EqualTo(2));
        Assert.That(deserializedSet.actions[0].name, Is.EqualTo("action1"));
        Assert.That(deserializedSet.actions[1].name, Is.EqualTo("action2"));
        Assert.That(deserializedSet.actions[0].bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(deserializedSet.actions[1].bindings[0].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(deserializedSet.actions[0].actionMap, Is.SameAs(deserializedSet));
        Assert.That(deserializedSet.actions[1].actionMap, Is.SameAs(deserializedSet));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddMultipleBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(name: "test");

        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/rightStick");

        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(action.bindings[1].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestrictMapsToSpecificDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action", binding: "<Gamepad>/leftStick");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        map.devices = new[] {gamepad2};

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.None.SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        map.devices = null;

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestrictMapsToSpecificDevices_WhileEnabled()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action", binding: "<Gamepad>/leftStick");
        map.devices = new[] {gamepad1};

        map.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));

        map.devices = new[] {gamepad2};

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestrictAssetsToSpecificDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap("map");
        var action = map.AddAction("action", binding: "<Gamepad>/leftStick");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        Assert.That(map.devices, Is.Null);
        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        asset.devices = new[] {gamepad2};

        Assert.That(map.devices, Is.EquivalentTo(asset.devices));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.None.SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        asset.devices = null;

        Assert.That(map.devices, Is.Null);
        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));
    }

    // This test is important to ensure that control scheme switching is seamless. Switching devices
    // *will* lead to a re-resolve on the actions but after switching, the actions should pick up where
    // they left off before the re-resolve.

    // This test relates to control scheme switching. If we switch devices on a map *while* the map is enabled,
    // we have to decide what to do about actions that are currently in progress. Rather than trying to somehow
    // migrate the current action state from the previous binding state over to the new binding state, we opt
    // to stay simple and just cancel any ongoing action whenever we alter the binding setup while an action
    // is enabled. This will result in a setup semantically equivalent to manually disabling all actions before
    // the transition and then manually re-enabling them after.
    [Test]
    [Category("Actions")]
    public void Actions_ChangingDevicesWhileEnabled_CancelsOngoingActions()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action", binding: "<Gamepad>/leftStick");

        map.devices = new[] {gamepad1};
        map.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Start action by actuating left stick of first gamepad.
            Set(gamepad1.leftStick, new Vector2(0.123f, 0.234f));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            // Actuate left stick on *second* gamepad and then switch to the second gamepad.
            // Doing so should cancel the action that's already going and then restart it
            // in the next update.
            Set(gamepad2.leftStick, new Vector2(0.234f, 0.345f));

            map.devices = new[] {gamepad2};

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad2.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad2.leftStick));
            Assert.That(actions[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));
        }
    }

    // This is basically the same deal as Actions_ChangingDevicesWhileEnabled_CancelsOngoingActions above.
    [Test]
    [Category("Actions")]
    public void Actions_ChangingBindingMaskWhileEnabled_CancelsOngoingActions()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action");
        action.AddBinding("<Gamepad>/leftStick", groups: "Default");
        action.AddBinding("<Gamepad>/rightStick", groups: "Lefty");

        map.bindingMask = new InputBinding {groups = "Default"};
        map.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Start action by actuating left stick of first gamepad.
            Set(gamepad1.leftStick, new Vector2(0.123f, 0.234f));

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            Set(gamepad2.rightStick, new Vector2(0.234f, 0.345f));
            map.bindingMask = new InputBinding {groups = "Lefty"};

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .Using(Vector2EqualityComparer.Instance));

            trace.Clear();

            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad2.rightStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad2.rightStick));
            Assert.That(actions[1].ReadValue<Vector2>(),
                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                    .Using(Vector2EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddInteractionsToActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterControlProcessor<ConstantVector2TestProcessor>();
        var action = new InputAction(interactions: "Tap(duration=0.123)");
        action.AddBinding("<Gamepad>/buttonSouth");
        action.Enable();

        var wasPerformed = false;
        action.performed +=
            ctx =>
        {
            Assert.That(wasPerformed, Is.False);
            Assert.That(ctx.interaction, Is.TypeOf<TapInteraction>());
            Assert.That(((TapInteraction)ctx.interaction).duration, Is.EqualTo(0.123).Within(0.00001));
            wasPerformed = true;
        };

        Press(gamepad.buttonSouth, absoluteTime: 0);
        Release(gamepad.buttonSouth, absoluteTime: 0.1);

        Assert.That(wasPerformed, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddProcessorsToActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterControlProcessor<ConstantVector2TestProcessor>();
        var action = new InputAction(processors: "ConstantVector2Test");
        action.AddBinding("<Gamepad>/leftStick");
        action.Enable();

        Vector2? receivedVector = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedVector, Is.Null);
            receivedVector = ctx.ReadValue<Vector2>();
        };

        Set(gamepad.leftStick, Vector2.one);

        Assert.That(receivedVector, Is.Not.Null);
        Assert.That(receivedVector.Value.x, Is.EqualTo(0.1234).Within(0.00001));
        Assert.That(receivedVector.Value.y, Is.EqualTo(0.5678).Within(0.00001));
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ConstantVector2TestProcessor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 value, InputControl<Vector2> control)
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
        action.AddBinding("<Gamepad>/leftStick").WithProcessor<ConstantVector2TestProcessor>();
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
    public void Actions_CanAddScaleProcessor()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction { passThrough = true };
        action.AddBinding("<Gamepad>/leftStick").WithProcessor("scaleVector2(x=2,y=3)");
        action.AddBinding("<Gamepad>/leftTrigger").WithProcessor("scale(factor=2)");
        action.Enable();

        var receivedValues = new List<object>();
        action.performed +=
            ctx =>
        {
            if (ctx.control is ButtonControl)
                receivedValues.Add(ctx.ReadValue<float>());
            else
                receivedValues.Add(ctx.ReadValue<Vector2>());
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f), leftTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(receivedValues, Has.Count.EqualTo(2));
        Assert.That(receivedValues, Has.Exactly(1).EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.5f, 0.5f)) * new Vector2(2, 3)));
        Assert.That(receivedValues, Has.Exactly(1).EqualTo(0.5f * 2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenNewDeviceIsAdded()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
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

        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
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

        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));

        action.Disable();

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Has.Count.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdate_NotificationIsTriggered()
    {
        var action = new InputAction("action", binding: "<Gamepad>/leftTrigger");
        action.Enable();

        var received = new List<object>();
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            received.Add(obj);
            received.Add(change);
        };

        InputSystem.AddDevice<Gamepad>();

        Assert.That(received,
            Is.EquivalentTo(new object[]
            {
                // When the action map re-resolves it will temporarily disable the action
                // which we see surface through the notifications.
                action, InputActionChange.ActionDisabled,
                action, InputActionChange.BoundControlsAboutToChange,
                action, InputActionChange.BoundControlsChanged,
                action, InputActionChange.ActionEnabled,
            }));
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

    // ReSharper disable once ClassNeverInstantiated.Local
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
        var action = new InputAction(binding: "<Gamepad>/leftStick/x", interactions: "test(parm1=5.0)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(TestInteraction.s_GotInvoked, Is.True);
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Actions")]
    public void Actions_RegisteringExistingInteractionUnderNewName_CreatesAlias()
    {
        InputSystem.RegisterInteraction<HoldInteraction>("TestTest");

        Assert.That(InputSystem.s_Manager.interactions.aliases.Contains(new InternedString("TestTest")));
    }

    #endif // UNITY_EDITOR

    [Test]
    [Category("Actions")]
    public void Actions_CanTriggerActionFromPartialStateUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftStick");
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
    public void Actions_WithoutInteraction_TriggerInResponseToMagnitude()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftStick");
        action.Enable();

        var phases = new List<InputActionPhase>();

        action.started += ctx => phases.Add(InputActionPhase.Started);
        action.performed += ctx => phases.Add(InputActionPhase.Performed);
        action.cancelled += ctx => phases.Add(InputActionPhase.Cancelled);

        // Actuate leftStick below deadzone threshold.
        Set(gamepad.leftStick, new Vector2(0.01f, 0.002f));

        Assert.That(phases, Is.Empty);

        // Actuate above threshold.
        Set(gamepad.leftStick, new Vector2(0.5f, 0.5f));

        Assert.That(phases, Is.EquivalentTo(new[] { InputActionPhase.Started, InputActionPhase.Performed }));

        phases.Clear();

        // Actuate a little more.
        Set(gamepad.leftStick, new Vector2(0.75f, 0.75f));

        Assert.That(phases, Is.EquivalentTo(new[] { InputActionPhase.Performed }));

        phases.Clear();

        // And a little more.
        Set(gamepad.leftStick, new Vector2(0.85f, 0.85f));
        Set(gamepad.leftStick, new Vector2(0.95f, 0.95f));

        Assert.That(phases, Is.EquivalentTo(new[] { InputActionPhase.Performed, InputActionPhase.Performed }));

        phases.Clear();

        // And go back to default.
        Set(gamepad.leftStick, Vector2.zero);

        Assert.That(phases, Is.EquivalentTo(new[] { InputActionPhase.Cancelled }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTriggerActionContinuously()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        runtime.advanceTimeEachDynamicUpdate = 0;

        // Set up an action that will trigger continuously for as long as the right trigger
        // is held down on the gamepad.
        var action = new InputAction(binding: "<Gamepad>/rightTrigger") {continuous = true};
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            runtime.currentTime = 0.123f;
            Set(gamepad.rightTrigger, 0.123f);

            // Initial actuation should start and then perform the action.
            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[0].time, Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[1].time, Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[1].control, Is.SameAs(gamepad.rightTrigger));

            trace.Clear();

            runtime.currentTime = 0.234f;
            InputSystem.Update();
            runtime.currentTime = 0.345f;
            InputSystem.Update();

            // No actuation in update should result in action being performed again.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[0].time, Is.EqualTo(0.234).Within(0.0001));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(0.123).Within(0.0001));
            Assert.That(actions[1].time, Is.EqualTo(0.345).Within(0.0001));
            Assert.That(actions[1].control, Is.SameAs(gamepad.rightTrigger));

            trace.Clear();

            runtime.currentTime = 0.456f;
            Set(gamepad.rightTrigger, 0.234f);

            // Further actuation should lead to one single performed.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.234).Within(0.0001));
            Assert.That(actions[0].time, Is.EqualTo(0.456).Within(0.0001));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));

            trace.Clear();

            runtime.currentTime = 0.567f;
            Set(gamepad.rightTrigger, 0);

            // Reset to default state should result in a single cancellation.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.0001));
            Assert.That(actions[0].time, Is.EqualTo(0.567).Within(0.0001));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));

            trace.Clear();

            runtime.currentTime = 0.678f;
            InputSystem.Update();

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformContinuousHold()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddBinding("<Gamepad>/rightTrigger").WithInteraction("Hold(duration=0.5)");
        action.continuous = true;
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            runtime.currentTime = 0;
            runtime.advanceTimeEachDynamicUpdate = 0;

            Set(gamepad.rightTrigger, 0.7f);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.7).Within(0.00001));

            trace.Clear();

            runtime.currentTime = 0.25f;
            InputSystem.Update();

            // We haven't yet reached the hold time so there shouldn't have been a change in state.
            Assert.That(trace, Is.Empty);

            runtime.currentTime = 0.6f;
            InputSystem.Update();

            // Now we've exceeded the hold time so the hold should have been performed.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.7).Within(0.00001));

            trace.Clear();

            // When we run another update now, we should get another triggering of the action.
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.7).Within(0.00001));

            trace.Clear();

            Set(gamepad.rightTrigger, 0);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].control, Is.SameAs(gamepad.rightTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.00001));

            trace.Clear();

            InputSystem.Update();

            Assert.That(trace, Is.Empty);
        }
    }

    // Triggers (any analog axis really) may jitter. Make sure that we can perform a hold
    // even if the control wiggles around.
    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldOnTrigger()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftTrigger", interactions: "hold(duration=0.4)");
        action.Enable();

        runtime.advanceTimeEachDynamicUpdate = 0;

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            runtime.currentTime = 0.1f;
            Set(gamepad.leftTrigger, 0.123f);
            runtime.currentTime = 0.2f;
            Set(gamepad.leftTrigger, 0.234f);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].time, Is.EqualTo(0.1).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>, Is.EqualTo(0.123).Within(0.00001));

            trace.Clear();

            runtime.currentTime = 0.6f;
            Set(gamepad.leftTrigger, 0.345f);
            runtime.currentTime = 0.7f;
            Set(gamepad.leftTrigger, 0.456f);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].time, Is.EqualTo(0.6).Within(0.00001));
            Assert.That(actions[0].ReadValue<float>, Is.EqualTo(0.345).Within(0.00001));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
        }
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanPerformContinuousStartsOnHold()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    [Property("TimesliceEvents", "Off")]
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
        var action = new InputAction(binding: "<Gamepad>/buttonSouth",
            interactions: "tap(duration=0.1),slowTap(duration=0.5)");
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Perform tap.
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.A), 0.0);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(), 0.05);
            InputSystem.Update();

            // Only tap was started and performed.
            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[0].time, Is.Zero.Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[1].time, Is.EqualTo(0.05).Within(0.00001));

            trace.Clear();

            // Perform slow tap.
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.A), 2.0);
            InputSystem.QueueStateEvent(gamepad, new GamepadState(),
                2.0 + InputSystem.settings.defaultSlowTapTime + 0.0001);
            InputSystem.Update();

            // First tap was started, then cancelled, then slow tap was started, and then performed.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(4));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[1].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[2].interaction, Is.TypeOf<SlowTapInteraction>());
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[3].interaction, Is.TypeOf<SlowTapInteraction>());
        }
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

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update();

        Assert.That(firePerformed, Is.True);
        Assert.That(reloadPerformed, Is.False);

        firePerformed = false;
        reloadPerformed = false;

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
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
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadButton.A});
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
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadButton.A});
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
            new GamepadState {buttons = 1 << (int)GamepadButton.A});
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadButton.A});
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
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadButton.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 0}, InputSystem.settings.defaultSlowTapTime + 0.1);
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].interaction, Is.TypeOf<SlowTapInteraction>());
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
        runtime.ReportNewInputDevice(
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
    [Property("TimesliceEvents", "Off")]
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
        var endTime = 0.123 + InputSystem.settings.defaultSlowTapTime + 1.0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f}, startTime);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.0f}, endTime);
        InputSystem.Update();

        Assert.That(receivedStartTime, Is.EqualTo(startTime).Within(0.000001));
        Assert.That(receivedTime, Is.EqualTo(endTime).Within(0.000001));
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
    public void Actions_CanLookUpMapInAssetById()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("test");
        asset.AddActionMap(map);

        Assert.That(asset.TryGetActionMap($"{{{map.id}}}"), Is.SameAs(map));
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

        Assert.That(asset.FindAction($"{{{action1.id.ToString()}}}"), Is.SameAs(action1));
        Assert.That(asset.FindAction($"{{{action2.id.ToString()}}}"), Is.SameAs(action2));
        Assert.That(asset.FindAction($"{{{action3.id.ToString()}}}"), Is.SameAs(action3));

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

    [Test]
    [Category("Actions")]
    public void Actions_CanFindControlSchemeUsingGivenDevice()
    {
        var scheme1 = new InputControlScheme()
            .WithRequiredDevice("<Gamepad>");
        var scheme2 = new InputControlScheme()
            .WithRequiredDevice("<Keyboard>")
            .WithRequiredDevice("<Mouse>");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var touch = InputSystem.AddDevice<Touchscreen>();

        Assert.That(InputControlScheme.FindControlSchemeForDevice(gamepad, new[] {scheme1, scheme2}),
            Is.EqualTo(scheme1));
        Assert.That(InputControlScheme.FindControlSchemeForDevice(keyboard, new[] {scheme1, scheme2}),
            Is.EqualTo(scheme2));
        Assert.That(InputControlScheme.FindControlSchemeForDevice(mouse, new[] {scheme1, scheme2}),
            Is.EqualTo(scheme2));
        Assert.That(InputControlScheme.FindControlSchemeForDevice(touch, new[] {scheme1, scheme2}),
            Is.Null);
    }

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

        action.bindingMask = new InputBinding {groups = "gamepad"};

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.bindingMask, Is.EqualTo(new InputBinding {groups = "gamepad"}));

        action.bindingMask = null;

        Assert.That(action.controls, Has.Count.EqualTo(3));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(mouse.leftButton));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanMaskOutBindingsByBindingGroup_OnAction_WhenEnabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction
        {
            bindingMask = new InputBinding {groups = "a"}
        };

        action.AddBinding("<Gamepad>/buttonSouth").WithGroup("a");
        action.AddBinding("<Gamepad>/buttonNorth").WithGroup("b");

        action.Enable();

        action.bindingMask = new InputBinding {groups = "b"};

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.Null);

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.buttonNorth));
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

        map.bindingMask = new InputBinding {groups = "gamepad"};

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

        asset.bindingMask = new InputBinding {groups = "gamepad"};

        Assert.That(action1.controls, Has.Count.EqualTo(1));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(gamepad.rightStick));

        asset.bindingMask = new InputBinding {groups = "keyboard"};

        Assert.That(action1.controls, Has.Count.EqualTo(1));
        Assert.That(action1.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(keyboard.bKey));

        asset.bindingMask = null;

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
    [Property("TimesliceEvents", "Off")]
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

    #if UNITY_EDITOR
    [Test]
    [Category("Actions")]
    public void Actions_RegisteringExistingCompositeUnderNewName_CreatesAlias()
    {
        InputSystem.RegisterBindingComposite<Vector2Composite>("TestTest");

        Assert.That(InputSystem.s_Manager.composites.aliases.Contains(new InternedString("TestTest")));
    }

    #endif // UNITY_EDITOR

    #pragma warning disable CS0649
    private class CompositeWithParameters : InputBindingComposite<float>
    {
        public int intParameter;
        public float floatParameter;
        public bool boolParameter;
        public EnumParameter enumParameter;

        public static CompositeWithParameters s_Instance;

        public CompositeWithParameters()
        {
            s_Instance = this;
        }

        public enum EnumParameter
        {
            A,
            B,
            C,
        }

        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            return 0;
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            return -1;
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanHaveParametersOnComposites()
    {
        InputSystem.RegisterBindingComposite<CompositeWithParameters>();

        // NOTE: Enums aren't supported at the JSON level. The editor uses reflection to display textual names rather
        //       than plain integer values but underneath, enums are treated as ints.
        var action = new InputAction();
        action.AddCompositeBinding(
            "CompositeWithParameters(intParameter=123,floatParameter=0.234,boolParameter=true,enumParameter=1)");

        CompositeWithParameters.s_Instance = null;
        action.Enable();

        Assert.That(CompositeWithParameters.s_Instance, Is.Not.Null);
        Assert.That(CompositeWithParameters.s_Instance.intParameter, Is.EqualTo(123));
        Assert.That(CompositeWithParameters.s_Instance.floatParameter, Is.EqualTo(0.234).Within(0.00001));
        Assert.That(CompositeWithParameters.s_Instance.boolParameter, Is.True);
        Assert.That(CompositeWithParameters.s_Instance.enumParameter,
            Is.EqualTo(CompositeWithParameters.EnumParameter.B));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CompositesWithMissingBindings_ThrowExceptions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateAxisComposite()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Gamepad>/leftTrigger")
            .With("Positive", "<Gamepad>/rightTrigger");
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Negative.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.345f});
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.EqualTo(gamepad.leftTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(-0.345).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.EqualTo(gamepad.leftTrigger));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(-0.345).Within(0.00001));

            trace.Clear();

            // Positive.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.456f});
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            // Bit of an odd case. leftTrigger and rightTrigger have both changed state here so
            // in a way, it's up to the system which one to pick. Might be useful if it was deliberately
            // picking the control with the highest magnitude but not sure it's worth the effort.
            Assert.That(actions[0].control, Is.EqualTo(gamepad.leftTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.456).Within(0.00001));

            trace.Clear();

            // Neither.
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].control, Is.EqualTo(gamepad.rightTrigger));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.00001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateAxisComposite_AndDetermineWhichSideWins()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding($"Axis(whichSideWins={(int) AxisComposite.WhichSideWins.Neither})")
            .With("Negative", "<Gamepad>/leftTrigger", groups: "neither")
            .With("Positive", "<Gamepad>/rightTrigger", groups: "neither");
        action.AddCompositeBinding($"Axis(whichSideWins={(int) AxisComposite.WhichSideWins.Positive})")
            .With("Negative", "<Gamepad>/leftTrigger", groups: "positive")
            .With("Positive", "<Gamepad>/rightTrigger", groups: "positive");
        action.AddCompositeBinding($"Axis(whichSideWins={(int) AxisComposite.WhichSideWins.Negative})")
            .With("Negative", "<Gamepad>/leftTrigger", groups: "negative")
            .With("Positive", "<Gamepad>/rightTrigger", groups: "negative");

        action.bindingMask = InputBinding.MaskByGroup("neither");
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            // Neither wins.
            // Start with one side actuated, then actuate both.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.345f});
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(-0.345).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(-0.345).Within(0.00001));

            trace.Clear();

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.345f, rightTrigger = 0.543f});
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.00001));

            trace.Clear();

            // Positive wins.
            action.bindingMask = InputBinding.MaskByGroup("positive");

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.123f, rightTrigger = 0.234f});
            InputSystem.Update();

            // We get a started and performed when switching to the right trigger and then another performed
            // when we right trigger changes value.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(3));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.543f).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(0.543f).Within(0.00001));
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].ReadValue<float>(), Is.EqualTo(0.234f).Within(0.00001));

            trace.Clear();

            // Negative wins.
            action.bindingMask = InputBinding.MaskByGroup("negative");

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.567f, rightTrigger = 0.765f});
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(4));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0.234f).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(-0.123f).Within(0.00001));
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].ReadValue<float>(), Is.EqualTo(-0.123).Within(0.00001));
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[3].ReadValue<float>(), Is.EqualTo(-0.567).Within(0.00001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateVector2Composite()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up classic WASD control.
        var action = new InputAction();
        action.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
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

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateComposite_WithPartsBeingOutOfOrder()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("Axis")
            .With("positive", "<keyboard>/f")
            .With("negative", "<keyboard>/c")
            .With("positive", "<gamepad>/leftTrigger")
            .With("negative", "<gamepad>/rightTrigger");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftTrigger, 1);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(1));

            trace.Clear();

            Set(gamepad.rightTrigger, 1);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(0));

            trace.Clear();

            Set(gamepad.leftTrigger, 0);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(-1));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(-1));
        }
    }

    private class LogInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            Debug.LogAssertion("LogInteraction.Process");
            context.PerformedAndGoBackToWaiting();
        }

        public void Reset()
        {
        }
    }

    // This is a bit of an edgy case. Actions trigger in response to controls they are bound to changing state.
    // However, in the case of composites, multiple controls may act in unison so if more than one control changes
    // state at the same time, each state monitor on the part controls will trigger independently one after the
    // other (in indeterminate order). But then, do we trigger the action more than once or only a single time?
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
    public void Actions_Vector2Composite_TriggersActionOnlyOnceWhenMultipleComponentBindingsTriggerInSingleEvent()
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
    public void Actions_CanCreateCompositesWithBindingsResolvingToMultipleControls()
    {
        var keyboard1 = InputSystem.AddDevice<Keyboard>();
        var keyboard2 = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/RightArrow") // These bindings will pick up both keyboards.
            .With("Negative", "<Keyboard>/LeftArrow");
        action.Enable();

        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard1.rightArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard2.rightArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard1.leftArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard2.leftArrowKey));

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            InputSystem.QueueStateEvent(keyboard1, new KeyboardState(Key.RightArrow));
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.000001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(1).Within(0.000001));

            trace.Clear();

            // Going to keyboard #2 should make it take over.
            InputSystem.QueueStateEvent(keyboard2, new KeyboardState(Key.RightArrow));
            InputSystem.Update();

            ////REVIEW: should this even result in a change?

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.000001));

            trace.Clear();

            InputSystem.QueueStateEvent(keyboard2, new KeyboardState(Key.LeftArrow));
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<float>(), Is.Zero.Within(0.000001));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateCompositesWithMultipleBindings()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up directional controls that work both with WASD and arrows.
        // NOTE: This sets up a single Dpad composite that works with either of the keys meaning
        //       the WASD and arrow block can be mixed. An alternative setup would be to set up
        //       to separate Dpad composites, one for WASD and one for the arrow block. In that setup,
        //       the two will not mix but rather produce two independent 2D vectors. Which one gets
        //       to drive the associated action is whichver had the last input event.
        var action = new InputAction();
        action.AddCompositeBinding("Dpad")
            .With("Up", "/<Keyboard>/w")
            .With("Up", "/<Keyboard>/upArrow")
            .With("Down", "/<Keyboard>/s")
            .With("Down", "/<Keyboard>/downArrow")
            .With("Left", "/<Keyboard>/a")
            .With("Left", "/<Keyboard>/leftArrow")
            .With("Right", "/<Keyboard>/d")
            .With("Right", "/<Keyboard>/rightArrow");
        action.Enable();

        Vector2? value = null;
        action.performed += ctx => { value = ctx.ReadValue<Vector2>(); };

        // Up arrow.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
        InputSystem.Update();

        Assert.That(value, Is.EqualTo(new Vector2(0, 1)).Using(Vector2EqualityComparer.Instance));

        // Down arrow + 'a'.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow, Key.A));
        InputSystem.Update();

        Assert.That(value, Is.EqualTo(new Vector2(-1, -1).normalized).Using(Vector2EqualityComparer.Instance));
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
    public void Actions_OnActionWithMultipleBindings_CanTargetBindingByIndex()
    {
        var action = new InputAction();

        action.AddBinding("<Keyboard>/escape");
        action.AddBinding("<Keyboard>/pause");
        action.AddBinding("<Gamepad>/start");

        action.ApplyBindingOverride(0, "<Keyboard>/u");
        action.ApplyBindingOverride(2, "<Gamepad>/select");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Keyboard>/u"));
        Assert.That(action.bindings[1].overridePath, Is.Null);
        Assert.That(action.bindings[2].overridePath, Is.EqualTo("<Gamepad>/select"));
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

    ////REVIEW: can we can this work with chained bindings and e.g. bind "Shift+W" successfully?

    ////TODO: allow restricting by control paths so that we can restrict it by device requirements found in control schemes
    ////      (this will implicitly allow restricting rebinds to specific types of devices)

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformInteractiveRebinding()
    {
        // Most straightforward test:
        // - Take action with existing binding to A button
        // - Initiate rebind
        // - Press Y button

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCompleteCallback = false;

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnComplete(
                           operation =>
                           {
                               Assert.That(receivedCompleteCallback, Is.False);
                               Assert.That(operation.started);
                               Assert.That(operation.completed);
                               Assert.That(operation.action, Is.SameAs(action));
                               Assert.That(operation.selectedControl, Is.SameAs(gamepad.buttonNorth));
                               receivedCompleteCallback = true;
                           })
                       .Start())
        {
            Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.buttonNorth }));
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/buttonNorth"));
            Assert.That(rebind.completed, Is.True);
            Assert.That(rebind.cancelled, Is.False);
            Assert.That(receivedCompleteCallback, Is.True);
        }
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanCancelInteractiveRebinding_ThroughAction()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCancelInteractiveRebinding_ThroughBinding()
    {
        var action = new InputAction(binding: "<Keyboard>/space");
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var receivedCancelCallback = false;

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnComplete(
                           operation =>
                           {
                               Assert.Fail("Should not complete");
                           })
                       .OnCancel(
                           operation =>
                           {
                               Assert.That(receivedCancelCallback, Is.False);
                               receivedCancelCallback = true;
                           })
                       .WithCancellingThrough(keyboard.escapeKey)
                       .Start())
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { keyboard.spaceKey }));
            Assert.That(action.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(action.bindings[0].overridePath, Is.Null);
            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.cancelled, Is.True);
            Assert.That(receivedCancelCallback, Is.True);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCancelInteractiveRebinding_Manually()
    {
        var action = new InputAction(binding: "<Keyboard>/space");

        var receivedCancelCallback = false;

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnComplete(
                           operation =>
                           {
                               Assert.Fail("Should not complete");
                           })
                       .OnCancel(
                           operation =>
                           {
                               Assert.That(receivedCancelCallback, Is.False);
                               receivedCancelCallback = true;
                           })
                       .Start())
        {
            rebind.Cancel();

            Assert.That(action.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(action.bindings[0].overridePath, Is.Null);
            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.cancelled, Is.True);
            Assert.That(receivedCancelCallback, Is.True);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanRestrictToSpecificBinding()
    {
        var action = new InputAction();
        action.AddCompositeBinding("dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        var keyboard = InputSystem.AddDevice<Keyboard>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithTargetBinding(3) // Left
                       .Start())
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.U));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].path, Is.EqualTo("dpad"));
            Assert.That(action.bindings[1].path, Is.EqualTo("<Keyboard>/w"));
            Assert.That(action.bindings[2].path, Is.EqualTo("<Keyboard>/s"));
            Assert.That(action.bindings[3].path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(action.bindings[4].path, Is.EqualTo("<Keyboard>/d"));
            Assert.That(action.bindings[1].overridePath, Is.Null);
            Assert.That(action.bindings[2].overridePath, Is.Null);
            Assert.That(action.bindings[3].overridePath, Is.EqualTo("<Keyboard>/u"));
            Assert.That(action.bindings[4].overridePath, Is.Null);
        }
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_InteractiveRebinding_IgnoresUnrelatedInput()
    {
        Assert.Fail();
    }

    ////FIXME: doesn't yet work because of small floating-point differences throwing off the MemCmp;
    ////       for this here we we actually do want the "significant value change" logic
    // Make sure we take things like deadzone processors into account and don't react to controls that
    // are below their threshold.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_IgnoresControlsWithNoEffectiveValueChange()
    {
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithExpectedControlLayout("Stick")
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad,
                new GamepadState
                {
                    rightStick = new Vector2(InputSystem.settings.defaultDeadzoneMin - 0.0001f, InputSystem.settings.defaultDeadzoneMin - 0.0001f)
                });
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);

            InputSystem.QueueStateEvent(gamepad,
                new GamepadState
                {
                    rightStick = Vector2.one
                });
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    // Interactive rebinding can be used to add entirely new bindings.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanAddNewBinding()
    {
        var action = new InputAction();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithRebindAddingNewBinding(group: "testGroup")
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings, Has.Count.EqualTo(1));
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].groups, Is.EqualTo("testGroup"));
            Assert.That(action.bindings[0].overridePath, Is.Null);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_ByDefault_RequiresAtLeastOneBindingToBePresent()
    {
        var action = new InputAction();

        using (var rebind = action.PerformInteractiveRebinding())
        {
            Assert.That(() => rebind.Start(), Throws.InvalidOperationException);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_RequiresActionToBeDisabled()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.Enable();

        Assert.That(() => action.PerformInteractiveRebinding(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_IgnoresNoisyControls()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        const string layout = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button"",
                        ""noisy"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestLayout");

        using (var rebind = action.PerformInteractiveRebinding().Start())
        {
            Set((ButtonControl)device["button"], 0.678f);

            Assert.That(rebind.completed, Is.False);
            Assert.That(action.bindings[0].overridePath, Is.Null);

            // Can disable the behavior. This is most useful in combination with a custom
            // OnPotentialMatch() callback or when the selection-by-magnitude logic will do
            // a good enough job.
            rebind.WithoutIgnoringNoisyControls();

            Set((ButtonControl)device["button"], 0f);
            Set((ButtonControl)device["button"], 0.789f);

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<TestLayout>/button"));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_UsesSyntheticControlsOnlyWhenBestMatch()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.expectedControlLayout = "Axis";
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind = action.PerformInteractiveRebinding()
                   .OnPotentialMatch(
                       operation =>
                       {
                           // Complete only when leftStick/right has been picked.
                           if (operation.selectedControl == gamepad.leftStick.right)
                               operation.Complete();
                       })
                   .Start())
        {
            // Actuate X axis on left stick. This makes both the leftStick/right button (buttons are axes)
            // a candidate as well as leftStick/x. However, leftStick/right is synthetic so X axis should
            // win. Note that if we set expectedControlLayout to "Button", leftStick/x will get ignored
            // and leftStick/left will get picked.
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(1, 0)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.EquivalentTo(new[] {gamepad.leftStick.x, gamepad.leftStick.right}));
            Assert.That(rebind.scores, Has.Count.EqualTo(2));
            Assert.That(rebind.scores[0], Is.GreaterThan(rebind.scores[1]));

            // Reset.
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();
            rebind.RemoveCandidate(gamepad.leftStick.x);
            rebind.RemoveCandidate(gamepad.leftStick.right);

            // Switch to looking only for buttons. leftStick/x will no longer be a suitable pick.
            rebind.WithExpectedControlLayout("Button");

            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(1, 0)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftStick/right"));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanManuallyAcceptAndRejectControls()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnPotentialMatch(
                           operation =>
                           {
                               Assert.That(operation.candidates, Has.Count.EqualTo(1));
                               var candidate = operation.candidates[0];

                               // Reject anything other than rightTrigger.
                               if (candidate != gamepad.rightTrigger)
                                   operation.RemoveCandidate(candidate);
                               else
                                   operation.Complete();
                           })
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.5f});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/rightTrigger"));
        }
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_InteractiveRebinding_CanAutomaticallyRejectComponentControls()
    {
        Assert.Fail();
    }

    // InputAction.expectedControlLayout, if set, will guide the rebinding process as to which
    // controls we are looking for.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_RespectsExpectedControlLayoutIfSet()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth")
        {
            expectedControlLayout = "Button",
        };

        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind = action.PerformInteractiveRebinding()
                   .OnPotentialMatch(
                       operation =>
                       {
                           ////REVIEW: is there a better way to deal with this?
                           // Sticks have buttons for each of the directions. We want to ignore them
                           // for the sake of this test.
                           operation.RemoveCandidate(gamepad.leftStick.up);
                           operation.RemoveCandidate(gamepad.leftStick.down);
                           operation.RemoveCandidate(gamepad.leftStick.left);
                           operation.RemoveCandidate(gamepad.leftStick.right);

                           if (operation.candidates.Count > 0)
                               operation.Complete();
                       })
                   .Start())
        {
            // Gamepad leftStick should get ignored.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.cancelled, Is.False);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.Null);

            // Gamepad leftTrigger should bind.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(rebind.cancelled, Is.False);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftTrigger"));
        }
    }

    // If a control is already actuated when we initiate a rebind, we first require it to go
    // back to its default value.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_RequiresControlToBeActuatedStartingWithDefaultValue()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Put buttonNorth in pressed state.
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
        InputSystem.Update();

        using (var rebind = action.PerformInteractiveRebinding().Start())
        {
            // Reset buttonNorth to unpressed state.
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);

            // Now press it again.
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/buttonNorth"));
        }
    }

    ////TODO: figure out how we can rebind to, say, "leftStick/up"
    ////      (has to be smart enough to know it's looking for a button and that the stick has buttons and that up has been actuated)

    // Say the user has a DualShock gamepad and performs an interactive rebind. We generally don't want to bind
    // specifically to controls on the DualShock. Instead, if, after rebinding from buttonNorth to buttonSouth,
    // the user then picks up an Xbox gamepad, no rebinding should be required.
    //
    // To achieve this, the system looks for the topmost layout in the base layout chain that still has the control
    // we are looking for. E.g. if we start with buttonSouth on DualShockGamepadHID, we should trace it all the way
    // back to Gamepad which introduces the control.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_ChoosesBaseLayoutThatIntroducesSelectedControl()
    {
        // Define a device with a stick. Then define another device that's based on it.
        // Finally, rebind to X on the stick.
        // The system has to be smart enough to realize that controlFromBase is coming
        // from the base device even though the X control is not defined in the device
        // (but rather comes from the Stick layout).
        const string baseLayout = @"
            {
                ""name"" : ""BaseLayout"",
                ""controls"" : [
                    { ""name"" : ""controlFromBase"", ""layout"" : ""Stick"" }
                ]
            }
        ";
        const string derivedLayout = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    { ""name"" : ""controlFromBase/x"", ""format"" : ""FLT"" },
                    { ""name"" : ""controlFromBase/y"", ""format"" : ""FLT"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(baseLayout);
        InputSystem.RegisterLayout(derivedLayout);

        var action = new InputAction(binding: "<Gamepad>/leftStick/x");
        var derived = InputSystem.AddDevice("DerivedLayout");

        using (action.PerformInteractiveRebinding().Start())
        {
            using (StateEvent.From(derived, out var eventPtr))
            {
                derived["controlFromBase/x"].WriteValueFromObjectIntoEvent(eventPtr, 0.5f);

                InputSystem.QueueEvent(eventPtr);
                InputSystem.Update();
            }

            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<BaseLayout>/controlFromBase"));
        }
    }

    // Say we actuate a button on the XRController marked as LeftHand, then we want the override we generate
    // to take handedness into account and actually mention LeftHand in the override.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_IfDeviceHasUsages_UsagesAreAppliedToOverridePath()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        var rightHand = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetDeviceUsage(rightHand, CommonUsages.RightHand);

        using (var rebind = action.PerformInteractiveRebinding().Start())
        {
            InputSystem.QueueStateEvent(rightHand, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>{RightHand}/buttonSouth"));
        }
    }

    // We may want to perform a rebind on just one specific control scheme. For this, the rebinding
    // machinery allows specifying a binding mask to respect.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanBeRestrictedToSpecificBindingGroups()
    {
        var action = new InputAction();
        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");

        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithBindingGroup("Gamepad")
                       .Start())
        {
            Assert.That(rebind.bindingMask, Is.EqualTo(new InputBinding { groups = "Gamepad"}));

            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(action.bindings[0].overridePath, Is.Null);
            Assert.That(action.bindings[1].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[1].overridePath, Is.EqualTo("<Gamepad>/buttonNorth"));
        }
    }

    // By default, override paths will refer to devices by their type. Meaning that instead of getting
    // a concrete path like "/Gamepad1/buttonNorth", you get "<Gamepad>/buttonNorth". Alternatively,
    // rebinding can be configured to not do this but rather take the path of the chosen control as is.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanSetOverridesToReferToSpecificDevices()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithoutGeneralizingPathOfSelectedControl()
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("/Gamepad/buttonNorth"));
        }
    }

    // A timeout can be specified to wait after we have a match to see if there's more matches and if so,
    // have them get picked instead. This is useful when trying to bind to just one axis of the stick,
    // for example. We'll invariably get motion on both axes but we want to pick the motion axis with the
    // greatest amount of movement.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanWaitForAndPickBetterMatch()
    {
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnMatchWaitForAnother(1) // Wait one second for a better match.
                       .WithExpectedControlLayout("Stick")
                       .Start())
        {
            // Actuate leftStick above deadzone.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.3f, 0.3f)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.EquivalentTo(new[] {gamepad.leftStick}));

            // Advance time by half a second.
            runtime.currentTime += 0.5f;
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.EquivalentTo(new[] {gamepad.leftStick}));

            // Actuate rightStick even further than leftStick.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {rightStick = new Vector2(0.7f, 0.7f)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Has.Count.EqualTo(2));
            Assert.That(rebind.candidates[0], Is.SameAs(gamepad.rightStick));

            // Advance time by a full second.
            runtime.currentTime += 1;
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    // Magnitude is a useful indicator for how much a control is actuated and, if we have ambiguity between two
    // possible candidates, can be used to decide one way or the other. As a threshold it can also be used to
    // cull off control motion entirely and require very clear actuation of controls in order for them to register.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanSpecifyMagnitudeThreshold()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithMagnitudeHavingToBeGreaterThan(0.5f)
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.4f});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.6f});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftTrigger"));
        }
    }

    // Candidate controls can be restricted to match certain paths. This is useful, for example,
    // to constrain controls to devices required by a specific control scheme.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanBeRestrictedToSpecificControlPaths()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonNorth");
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithControlsHavingToMatchPath("<Keyboard>")
                       .WithControlsHavingToMatchPath("<Mouse>")
                       .OnPotentialMatch(operation => {})  // Don't complete. Just keep going.
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();

            Assert.That(rebind.candidates, Is.Empty);

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            InputSystem.Update();

            // The keyboard's synthetic AnyKey control and the mouse's button will get picked, too,
            // but will end up with the lowest scores.

            Assert.That(rebind.candidates, Has.Count.EqualTo(4));
            Assert.That(rebind.candidates, Has.Exactly(1).SameAs(keyboard.spaceKey));
            Assert.That(rebind.candidates, Has.Exactly(1).SameAs(mouse.leftButton));
            Assert.That(rebind.candidates[2], Is.SameAs(mouse.button));
            Assert.That(rebind.candidates[3], Is.SameAs(keyboard.anyKey)); // Last place for AnyKey.
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanExcludeSpecificControlPaths()
    {
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        var mouse = InputSystem.AddDevice<Mouse>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .WithControlsExcluding("<Mouse>/position")
                       .Start())
        {
            InputSystem.QueueStateEvent(mouse, new MouseState {position = new Vector2(123, 345)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            InputSystem.QueueStateEvent(mouse, new MouseState {delta = new Vector2(123, 345)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Pointer>/delta"));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_PicksControlWithHigherMagnitude()
    {
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
                   action.PerformInteractiveRebinding()
                       .OnComplete(
                           operation =>
                           {
                               // leftStick, rightStick, and rightTrigger should all be candidates.
                               // However, rightStick should come first as it has a higher magnitude in our event.
                               // Note that because we're not filtering for any specific control type or layout,
                               // we end up with a bunch of possible selections all based on the sticks.
                               Assert.That(operation.candidates,
                                   Is.EquivalentTo(new InputControl[]
                                   {
                                       gamepad.rightStick,
                                       gamepad.rightStick.x,
                                       gamepad.rightStick.y,
                                       gamepad.rightTrigger,
                                       gamepad.leftStick,
                                       gamepad.leftStick.x,
                                       gamepad.leftStick.y,

                                       // Synthetic controls receive least priority.
                                       gamepad.rightStick.up,
                                       gamepad.rightStick.right,
                                       gamepad.leftStick.up,
                                       gamepad.leftStick.right,
                                   }));
                           })
                       .Start())
        {
            InputSystem.QueueStateEvent(gamepad,
                new GamepadState
                {
                    leftStick = new Vector2(0.4f, 0.4f),
                    rightStick = new Vector2(0.6f, 0.6f),
                    rightTrigger = 0.5f,
                });
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    // Optionally, a fixed timeout on the entire operation can be specified. If no relevant input registers
    // within the given time, the operation is automatically cancelled.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_InteractiveRebinding_CanBeMadeToTimeOut()
    {
        Assert.Fail();
    }

    // By default, rebinds non-destructively apply as overrides. Optionally, they can be made to destructively
    // edit the path on bindings.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_InteractiveRebinding_CanBeMadeToOverwritePath()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanRebindWithoutAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var rebind = new InputActionRebindingExtensions.RebindingOperation())
        {
            // Must have OnApplyBinding() callback when not having an action as otherwise
            // RebindOperation doesn't know where to put the binding.
            Assert.That(() => rebind.Start(),
                Throws.InvalidOperationException.With.Message.Contains("OnApplyBinding"));

            var receivedOnApplyBindingCall = false;
            rebind.OnApplyBinding(
                (operation, path) =>
                {
                    receivedOnApplyBindingCall = true;
                    Assert.That(path, Is.EqualTo("<Gamepad>/leftStick"));
                })
                .Start();

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(1, 0)});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(receivedOnApplyBindingCall, Is.True);
        }
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
        var action1 = new InputAction(binding: "<Gamepad>/leftStick");
        var action2 = new InputAction(binding: "<Gamepad>/rightStick");
        var map = new InputActionMap();
        var action3 = map.AddAction("action", "<Gamepad>/buttonSouth");

        action1.Enable();
        action2.Enable();
        map.Enable();

        InputSystem.DisableAllEnabledActions();

        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
        Assert.That(action3.enabled, Is.False);
        Assert.That(map.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_DisablingAllActions_RemovesAllTheirStateMonitors()
    {
        InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction(binding: "<Gamepad>/leftStick");
        var action2 = new InputAction(binding: "<Gamepad>/rightStick");
        var action3 = new InputAction(binding: "<Gamepad>/buttonSouth");

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
        InputSystem.settings.defaultDeadzoneMin = 0f;
        InputSystem.settings.defaultDeadzoneMax = 1f;

        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Pointer>/delta");

        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.234f)});
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));

            trace.Clear();

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.QueueStateEvent(mouse, new MouseState {delta = new Vector2(0.234f, 0.345f)});
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(3));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[1].ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[1].control, Is.SameAs(mouse.delta));
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[2].control, Is.SameAs(mouse.delta));

            trace.Clear();

            ////REVIEW: This behavior is somewhat unfortunate. It means that an action bound to <mouse>/delta will constantly
            ////        restart every frame when there is mouse deltas. Also, the accumulation of deltas is really bad for actions.
            // Update should reset mouse delta to zero which should cancel the action.
            InputSystem.Update();

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Cancelled));
            Assert.That(actions[0].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].control, Is.SameAs(mouse.delta));
        }
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
                ".*InvalidOperationException thrown during execution of callback for 'Started' phase of 'testAction' action in map 'testMap'.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM MAP.*"));

        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException thrown during execution of 'Performed' callback on action 'testMap/testAction'.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM ACTION.*"));

        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException thrown during execution of callback for 'Performed' phase of 'testAction' action in map 'testMap'.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM MAP.*"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
        InputSystem.Update();
    }

    class TestInteractionCheckingDefaultState : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            if (context.control.CheckStateIsAtDefault())
            {
                Debug.Log("TestInteractionCheckingDefaultState.Process(default)");
                Assert.That(context.control.ReadValueAsObject(), Is.EqualTo(0.1234).Within(0.00001));
            }
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

        // Create gamepad and put leftStick/x in *non-default* state. If we start out in default
        // state, the action won't trigger. So we get it to trigger on a non-default state first and then
        // cancel it by going back to default state.
        InputSystem.RegisterLayout(json);
        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        Set(gamepad.leftStick, Vector2.zero); // This is non-default for this gamepad.

        var action = new InputAction(binding: "<Gamepad>/leftStick/x", interactions: "testInteractionCheckingDefaultState");
        action.Enable();

        LogAssert.Expect(LogType.Log, "TestInteractionCheckingDefaultState.Process(default)");

        Set(gamepad.leftStick, new Vector2(0.1234f, 0f));
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
    public void Actions_CanIterateOverActionsInAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");

        Assert.That(asset.ToList(), Is.EquivalentTo(new[] { action1, action2, action3 }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanIterateOverActionsInMap()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        var action3 = map.AddAction("action3");

        Assert.That(map.ToList(), Is.EquivalentTo(new[] { action1, action2, action3 }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateReferenceToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var reference = new InputActionAssetReference(asset);

        ////REVIEW: would be great to test serializability

        Assert.That(reference.asset, Is.SameAs(asset));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanMakePrivateCopyOfActionsThroughAssetReference()
    {
        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");
        map1.AddAction("action1", "<Gamepad>/leftStick");
        map1.AddAction("action2", "<Gamepad>/rightStick");
        map2.AddAction("action3", "<Keyboard>/space");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        var reference = new InputActionAssetReference(asset);
        reference.MakePrivateCopyOfActions();

        Assert.That(reference.asset, Is.Not.SameAs(asset));
        Assert.That(reference.asset.actionMaps, Has.Count.EqualTo(2));
        Assert.That(reference.asset.actionMaps[0].name, Is.EqualTo("map1"));
        Assert.That(reference.asset.actionMaps[1].name, Is.EqualTo("map2"));
        Assert.That(reference.asset.actionMaps[0].actions, Has.Count.EqualTo(2));
        Assert.That(reference.asset.actionMaps[1].actions, Has.Count.EqualTo(1));
        Assert.That(reference.asset.actionMaps[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(reference.asset.actionMaps[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(reference.asset.actionMaps[1].actions[0].name, Is.EqualTo("action3"));
        Assert.That(reference.asset.actionMaps[0].bindings, Has.Count.EqualTo(2));
        Assert.That(reference.asset.actionMaps[1].bindings, Has.Count.EqualTo(1));
    }
}
