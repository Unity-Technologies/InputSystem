using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools.Utils;

internal partial class CoreTests
{
    [Test]
    [Category("Actions")]
    public void Actions_CanApplyBindingOverridesToMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", binding: "/<keyboard>/enter");
        var action2 = map.AddAction("action2", binding: "/<gamepad>/buttonSouth");

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
    public void Actions_CanApplyBindingOverridesToMap_WhenEnabled()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action1", binding: "<Keyboard>/enter");
        map.Enable();

        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.enterKey}));

        map.ApplyBindingOverrides(new List<InputBinding>
        {
            new InputBinding {action = "action1", overridePath = "<Gamepad>/leftTrigger"}
        });

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftTrigger"));
        Assert.That(action.controls, Is.EquivalentTo(new[] {gamepad.leftTrigger}));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveBindingOverridesFromMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", binding: "/<keyboard>/enter");
        var action2 = map.AddAction("action2", binding: "/<gamepad>/buttonSouth");

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
    public void Actions_CanRemoveBindingOverridesFromMap_WhenEnabled()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action1", binding: "<Keyboard>/enter");

        var overrides = new List<InputBinding>
        {
            new InputBinding {action = "action1", overridePath = "<Gamepad>/leftTrigger"}
        };

        map.ApplyBindingOverrides(overrides);

        map.Enable();

        Assert.That(action.controls, Is.EquivalentTo(new[] {gamepad.leftTrigger}));

        map.RemoveBindingOverrides(overrides);

        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.enterKey}));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveAllBindingOverridesFromMaps()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1", binding: "/<keyboard>/enter");
        var action2 = map.AddAction("action2", binding: "/<gamepad>/buttonSouth");

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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
            Assert.That(rebind.canceled, Is.False);
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                       .WithCancelingThrough(keyboard.escapeKey)
                       .Start())
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { keyboard.spaceKey }));
            Assert.That(action.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(action.bindings[0].overridePath, Is.Null);
            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.canceled, Is.True);
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
            Assert.That(rebind.canceled, Is.True);
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
                       .WithExpectedControlType("Stick")
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                        ""name"" : ""noisyButton"",
                        ""layout"" : ""Button"",
                        ""format"" : ""FLT"",
                        ""noisy"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestLayout");

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation()
                   .WithAction(action)
                   .OnMatchWaitForAnother(0)
                   .Start())
        {
            Set((ButtonControl)device["noisyButton"], 0.678f);

            Assert.That(rebind.completed, Is.False);
            Assert.That(action.bindings[0].overridePath, Is.Null);

            Set((ButtonControl)device["noisyButton"], 0f);

            // Can disable the behavior. This is most useful in combination with a custom
            // OnPotentialMatch() callback or when the selection-by-magnitude logic will do
            // a good enough job.
            rebind.Cancel();
            rebind
                .WithoutIgnoringNoisyControls()
                .Start();

            Set((ButtonControl)device["noisyButton"], 0.789f);

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<TestLayout>/noisyButton"));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_UsesSyntheticControlsOnlyWhenBestMatch()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.expectedControlType = "Axis";
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation()
                   .WithAction(action)
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
            // win. Note that if we set expectedControlType to "Button", leftStick/x will get ignored
            // and leftStick/left will get picked.
            Set(gamepad.leftStick, Vector2.right);

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.EquivalentTo(new[] {gamepad.leftStick.x, gamepad.leftStick.right}));
            Assert.That(rebind.scores, Has.Count.EqualTo(2));
            Assert.That(rebind.scores[0], Is.GreaterThan(rebind.scores[1]));

            // Reset.
            Set(gamepad.leftStick, Vector2.zero);

            // Switch to looking only for buttons. leftStick/x will no longer be a suitable pick.
            rebind.Cancel();
            rebind
                .WithExpectedControlType("Button")
                .Start();

            Set(gamepad.leftStick, Vector2.right);

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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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

    // InputAction.expectedControlType, if set, will guide the rebinding process as to which
    // controls we are looking for.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_RespectsExpectedControlLayoutIfSet()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth")
        {
            expectedControlType = "Button",
        };

        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation()
                   .WithAction(action)
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
            Assert.That(rebind.canceled, Is.False);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.Null);

            // Gamepad leftTrigger should bind.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f});
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(rebind.canceled, Is.False);
            Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftTrigger"));
        }
    }

    // We want to be able to deal with controls that are already actuated when the rebinding starts and
    // also with controls that don't usually go back to default values at all.
    //
    // What we require is that when we detect sufficient actuation on a control in an event, we compare
    // it to the control's current actuation level when we first considered it. This is expected to work
    // regardless of whether we are suppressing events or not.
    //
    // https://fogbugz.unity3d.com/f/cases/1215784/
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_WhenControlAlreadyActuated_HasToCrossMagnitudeThresholdFromCurrentActuation()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Actuate some controls.
        Press(gamepad.buttonNorth);
        Set(gamepad.leftTrigger, 0.75f);

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation()
                   .WithAction(action)
                   .WithMagnitudeHavingToBeGreaterThan(0.25f)
                   .Start())
        {
            Release(gamepad.buttonNorth);

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            Set(gamepad.leftTrigger, 0.9f);

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            Set(gamepad.leftTrigger, 0f);

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            Set(gamepad.leftTrigger, 0.7f);

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/leftTrigger"));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanGetActuationMagnitudeOfCandidateControls()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation()
                   .WithAction(action)
                   .WithMagnitudeHavingToBeGreaterThan(0.25f)
                   .OnMatchWaitForAnother(1)
                   .Start())
        {
            Set(gamepad.leftTrigger, 0.75f);

            Assert.That(rebind.candidates, Has.Count.EqualTo(1));
            Assert.That(rebind.magnitudes, Has.Count.EqualTo(rebind.candidates.Count));
            Assert.That(rebind.candidates[0], Is.SameAs(gamepad.leftTrigger));
            Assert.That(rebind.magnitudes[0], Is.EqualTo(0.75).Within(0.00001));
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

        using (new InputActionRebindingExtensions.RebindingOperation().WithAction(action).Start())
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

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation().WithAction(action).Start())
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
                       .OnMatchWaitForAnother(1) // Wait one second for a better match.
                       .WithExpectedControlType("Stick")
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
                       .WithMagnitudeHavingToBeGreaterThan(0.5f)
                       .Start())
        {
            Set(gamepad.leftTrigger, 0.4f);

            Assert.That(rebind.completed, Is.False);
            Assert.That(rebind.candidates, Is.Empty);

            Set(gamepad.leftTrigger, 0.6f);

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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
            Assert.That(rebind.candidates[2], Is.SameAs(keyboard.anyKey));
            Assert.That(rebind.candidates[3], Is.SameAs(mouse.press));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanExcludeSpecificControlPaths()
    {
        var action = new InputAction(binding: "<Gamepad>/leftStick");
        var mouse = InputSystem.AddDevice<Mouse>();

        using (var rebind =
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
                   new InputActionRebindingExtensions.RebindingOperation()
                       .WithAction(action)
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
    // within the given time, the operation is automatically canceled.
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
    public void Actions_InteractiveRebinding_CanReuseRebindOperationMultipleTimes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var rebind = new InputActionRebindingExtensions.RebindingOperation())
        {
            InputControl[] candidates = null;

            rebind
                .WithExpectedControlType("Button")
                .OnPotentialMatch(ctx => candidates = ctx.candidates.ToArray())
                .OnApplyBinding((operation, s) => {});

            rebind.Start();
            PressAndRelease(gamepad.buttonSouth);

            Assert.That(candidates, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

            rebind.Cancel();
            candidates = null;
            rebind.Start();
            PressAndRelease(gamepad.buttonNorth);

            Assert.That(candidates, Is.EquivalentTo(new[] { gamepad.buttonNorth }));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_IfDeviceHasMultipleUsages_UsagesAreAppliedToOverridePath()
    {
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        var rightHandVertical = InputSystem.AddDevice<Gamepad>();

        InputSystem.SetDeviceUsage(rightHandVertical, CommonUsages.RightHand);
        InputSystem.AddDeviceUsage(rightHandVertical, CommonUsages.Vertical);

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation().WithAction(action).Start())
        {
            InputSystem.QueueStateEvent(rightHandVertical, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();

            Assert.That(rebind.completed, Is.True);
            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>{RightHand}{Vertical}/buttonSouth"));
        }
    }

    // It can be desirable to not let the event through that we're rebinding from. This, for example, prevents the event
    // from triggering UI actions. Note, however, that it also prevents the state of the device from updating correctly.
    //
    // NOTE: Hopefully, when we have a system in place that allows coordinating event consumption between actions, we have
    //       have a more elegant solution at our hand for solving the problem here.
    [Test]
    [Category("Actions")]
    public void Actions_InteractiveRebinding_CanSuppressEventsWhileListening()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var action = new InputAction(binding: "<Gamepad>/buttonNorth");

        using (new InputActionRebindingExtensions.RebindingOperation()
               .WithAction(action)
               .WithControlsExcluding("<Pointer>/position")
               .WithMatchingEventsBeingSuppressed().Start())
        {
            Set(mouse.position, new Vector2(123, 234));
            Press(gamepad.buttonSouth);

            Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        }
    }
}
