using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

#pragma warning disable CS0649
[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]

// As should be obvious from the number of tests in here, the action system rivals the entire combined rest of the system
// in terms of complexity.
partial class CoreTests
{
    // ISXB-925: Feature flag values should live with containing settings instance.
    [TestCase(InputFeatureNames.kUseReadValueCaching)]
    [TestCase(InputFeatureNames.kUseOptimizedControls)]
    [TestCase(InputFeatureNames.kParanoidReadValueCachingChecks)]
    [TestCase(InputFeatureNames.kDisableUnityRemoteSupport)]
    [TestCase(InputFeatureNames.kRunPlayerUpdatesInEditMode)]
    #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
    [TestCase(InputFeatureNames.kUseIMGUIEditorForAssets)]
    #endif
    public void Settings_ShouldStoreSettingsAndFeatureFlags(string featureName)
    {
        using (var settings = Scoped.Object(InputSettings.CreateInstance<InputSettings>()))
        {
            InputSystem.settings = settings.value;

            Assert.That(InputSystem.settings.IsFeatureEnabled(featureName), Is.False);
            settings.value.SetInternalFeatureFlag(featureName, true);
            Assert.That(InputSystem.settings.IsFeatureEnabled(featureName), Is.True);

            using (var other = Scoped.Object(InputSettings.CreateInstance<InputSettings>()))
            {
                InputSystem.settings = other.value;

                Assert.That(InputSystem.settings.IsFeatureEnabled(featureName), Is.False);
            }
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenShortcutsDisabled_AllConflictingActionsTrigger()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map1 = new InputActionMap("map1");
        var action1 = map1.AddAction(name: "action1");
        action1.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        var map2 = new InputActionMap("map2");
        var action2 = map2.AddAction(name: "action2");
        action2.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        var action3 = map2.AddAction(name: "action3", binding: "<Keyboard>/w");

        map1.Enable();
        map2.Enable();

        Press(keyboard.wKey);

        // All Actions were triggered
        Assert.That(action1.WasPerformedThisFrame());
        Assert.That(action2.WasPerformedThisFrame());
        Assert.That(action3.WasPerformedThisFrame());
    }

    // Premise: Binding the same control multiple times in different ways from multiple concurrently active
    //          actions should result in the input system figuring out which *one* action gets to act on the input.
    [Test]
    [Category("Actions")]
    [TestCase(true)]
    [TestCase(false)]
    public void Actions_WhenShortcutsEnabled_CanConsumeInput(bool legacyComposites)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();

        var action1 = map.AddAction("action1", type: InputActionType.Button);
        var action2 = map.AddAction("action2", type: InputActionType.Button);
        var action3 = map.AddAction("action3", type: InputActionType.Button);
        var action4 = map.AddAction("action4", type: InputActionType.Value);
        var action5 = map.AddAction("action5", type: InputActionType.Button);
        var action6 = map.AddAction("action6", type: InputActionType.Button);
        var action7 = map.AddAction("action7", type: InputActionType.Button);
        var action8 = map.AddAction("action8", type: InputActionType.Button);

        // Enable some actions individually to make sure the code that deals
        // with re-resolution of already enabled bindings handles the enabling
        // of just individual actions out of the whole set correctly.
        action1.Enable();
        action2.Enable();

        // State monitors on the space key (all end up in the same group):
        //   action3 complexity=3
        //   action2 complexity=2
        //   action1 complexity=1
        //   action4 complexity=1
        //   action5 complexity=1

        action1.AddBinding("<Keyboard>/space");
        action2.AddCompositeBinding(legacyComposites ? "ButtonWithOneModifier" : "OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/space");
        action3.AddCompositeBinding(legacyComposites ? "ButtonWithTwoModifiers" : "TwoModifiers")
            .With("Modifier1", "<Keyboard>/ctrl")
            .With("Modifier2", "<Keyboard>/shift")
            .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/space");
        action4.AddBinding("<Keyboard>/space");

        // This one is a clear conflict. Binds SPC exactly the same way as action1.
        action5.AddBinding("<Keyboard>/space");

        map.Enable();

        Press(keyboard.spaceKey);

        Assert.That(action1.WasPerformedThisFrame());
        Assert.That(action4.WasPerformedThisFrame());
        Assert.That(action5.WasPerformedThisFrame());

        Assert.That(!action2.WasPerformedThisFrame());
        Assert.That(!action3.WasPerformedThisFrame());

        Release(keyboard.spaceKey);

        ////REVIEW: Pressing LSHIFT does *not* lead to the OneModifier and TwoModifiers actions
        ////        going to Started phase because actuation remains at 0; intuitively, I would expect the actions to start
        Press(keyboard.leftShiftKey);
        Press(keyboard.spaceKey);

        Assert.That(!action1.WasPerformedThisFrame());
        Assert.That(!action4.WasPerformedThisFrame());
        Assert.That(!action5.WasPerformedThisFrame());

        Assert.That(action2.WasPerformedThisFrame());
        Assert.That(!action3.WasPerformedThisFrame());

        Release(keyboard.leftShiftKey);
        Release(keyboard.spaceKey);

        Press(keyboard.spaceKey);

        Assert.That(action1.WasPerformedThisFrame());
        Assert.That(action4.WasPerformedThisFrame());
        Assert.That(action5.WasPerformedThisFrame());

        Assert.That(!action2.WasPerformedThisFrame());
        Assert.That(!action3.WasPerformedThisFrame());

        Press(keyboard.leftShiftKey);

        Assert.That(!action1.WasPerformedThisFrame());
        Assert.That(!action4.WasPerformedThisFrame());
        Assert.That(!action5.WasPerformedThisFrame());

        Assert.That(!action2.WasPerformedThisFrame());
        Assert.That(!action3.WasPerformedThisFrame());
    }

    [Test]
    [Category("Actions")]
    public void Actions_ShortcutSupportDisabledByDefault()
    {
        Assert.That(InputSystem.settings.shortcutKeysConsumeInput, Is.False);

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddBinding("<Keyboard>/space");
        action2.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/ctrl")
            .With("Binding", "<Keyboard>/space");

        map.Enable();

        Press(keyboard.leftCtrlKey, queueEventOnly: true);
        Press(keyboard.spaceKey);

        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanBindShortcutsInvolvingMultipleDevices()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var action = new InputAction(type: InputActionType.Value);
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Binding", "<Mouse>/position");

        action.Enable();

        Press(keyboard.leftShiftKey);

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(default(Vector2)));

        Set(mouse.position, new Vector2(123, 234));

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(123, 234)));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanBindMultipleShortcutSequencesBasedOnSameModifiers()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Binding", "<Keyboard>/a");
        action2.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Binding", "<Keyboard>/b");

        map.Enable();

        Press(keyboard.leftShiftKey);

        Assert.That(!action1.WasPerformedThisFrame());
        Assert.That(!action2.WasPerformedThisFrame());

        Press(keyboard.aKey);

        Assert.That(action1.WasPerformedThisFrame());
        Assert.That(!action2.WasPerformedThisFrame());

        Press(keyboard.bKey);

        Assert.That(!action1.WasPerformedThisFrame());
        Assert.That(action2.WasPerformedThisFrame());
    }

    [Test]
    [Category("Actions")]
    [TestCase("leftShift", null, "space", true)]
    [TestCase("leftShift", "leftAlt", "space", true)]
    [TestCase("leftShift", null, "space", false)]
    [TestCase("leftShift", "leftAlt", "space", false)]
    public void Actions_WhenShortcutsEnabled_PressingShortcutSequenceInWrongOrder_DoesNotTriggerShortcut(string modifier1, string modifier2, string binding, bool legacyComposites)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        if (!string.IsNullOrEmpty(modifier2))
        {
            action.AddCompositeBinding(legacyComposites ? "ButtonWithTwoModifiers" : "TwoModifiers")
                .With("Modifier1", "<Keyboard>/" + modifier1)
                .With("Modifier2", "<Keyboard>/" + modifier2)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding);
        }
        else
        {
            action.AddCompositeBinding(legacyComposites ? "ButtonWithOneModifier" : "OneModifier")
                .With("Modifier", "<Keyboard>/" + modifier1)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding);
        }

        action.Enable();

        var wasPerformed = false;
        action.performed += _ => wasPerformed = true;

        // Press binding first, then modifiers.
        Press((ButtonControl)keyboard[binding]);
        Press((ButtonControl)keyboard[modifier1]);
        if (!string.IsNullOrEmpty(modifier2))
            Press((ButtonControl)keyboard[modifier2]);

        Assert.That(wasPerformed, Is.False);
    }

    [Test]
    [Category("Actions")]
    [TestCase("leftShift", null, "space", true)]
    [TestCase("leftShift", "leftAlt", "space", true)]
    [TestCase("leftShift", null, "space", false)]
    [TestCase("leftShift", "leftAlt", "space", false)]
    public void Actions_PressingShortcutSequenceInWrongOrder_DoesNotTriggerShortcut_ExceptIfOverridden(string modifier1, string modifier2, string binding,
        bool legacyComposites)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        if (!string.IsNullOrEmpty(modifier2))
        {
            action.AddCompositeBinding((legacyComposites ? "ButtonWithTwoModifiers" : "TwoModifiers") + "(overrideModifiersNeedToBePressedFirst)")
                .With("Modifier1", "<Keyboard>/" + modifier1)
                .With("Modifier2", "<Keyboard>/" + modifier2)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding);
        }
        else
        {
            action.AddCompositeBinding((legacyComposites ? "ButtonWithOneModifier" : "OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
                .With("Modifier", "<Keyboard>/" + modifier1)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding);
        }

        action.Enable();

        // Press binding first, then modifiers.
        Press((ButtonControl)keyboard[binding]);
        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Press((ButtonControl)keyboard[modifier1]);
        if (!string.IsNullOrEmpty(modifier2))
        {
            Assert.That(action.WasPerformedThisFrame(), Is.False);
            Press((ButtonControl)keyboard[modifier2]);
        }

        Assert.That(action.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenShortcutsAreEnabled_CanHaveShortcutsWithButtonsUsingInitialStateChecks()
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddBinding("<Keyboard>/space");
        action2.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Binding", "<Keyboard>/space");

        action1.wantsInitialStateCheck = true;
        action2.wantsInitialStateCheck = true;

        // Order is wrong but the ordering is lost when relying on initial state checks.
        Press(keyboard.spaceKey);
        Press(keyboard.leftShiftKey);

        map.Enable();

        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    //--------------BUT: can use press times to detect holds!!

    // elden ring:
    // - "hold Y" is implicit in it being used as a modifier (has to be held)
    // - "tap" can be put on Y action to make sure it doesn't trigger when you press and hold and then change your mind

    // imagine opposite case; can't use modifier composite to model the "modifier+button" input

    // So, right now we don't support interactions on part bindings. Which makes sense because interactions
    // talk to the action directly, they don't participate in the value chain.
    //
    // For shortcuts, this means you can't use "hold X" as a modifier. Which would be very cool (Elden Ring,
    // for example, does that with the Y button on the gamepad).
    //
    // When we redesign interactions as part of the gesture feature, we should make sure we can handle
    // such a scenario. With interactions being able to directly drive values, there should be no reason
    // why you couldn't have "hold Y" as a modifier.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanUseInteractionsOnShortcutModifiers()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddBinding("<Keyboard>/space");
        action2.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift") //
            .With("Binding", "<Keyboard>/space");

        action1.wantsInitialStateCheck = true;
        action2.wantsInitialStateCheck = true;

        // Order is wrong but the ordering is lost when relying on initial state checks.
        Press(keyboard.spaceKey);
        Press(keyboard.leftShiftKey);

        map.Enable();

        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ReadingValueRightAfterEnabling_AppliesProcessorsFromFirstBinding()
    {
        InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap("map");
        map.AddAction("action2", binding: "<Gamepad>/buttonNorth");
        var action1 = map.AddAction("action1", binding: "<Gamepad>/leftStick/x", processors: "normalize(min=-1,max=1,zero=-1)");
        action1.AddBinding("<Gamepad>/rightStick/x", processors: "normalize(min=0,max=1)");

        map.Enable();

        Assert.That(action1.ReadValue<float>(), Is.EqualTo(0.5f));
    }

    public void Actions_ReadingValueRightAfterResetting_AppliesProcessorsFromFirstBinding()
    {
        InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap("map");
        map.AddAction("action2", binding: "<Gamepad>/buttonNorth");
        var action1 = map.AddAction("action1", binding: "<Gamepad>/leftStick/x", processors: "normalize(min=-1,max=1,zero=-1)");
        action1.AddBinding("<Gamepad>/rightStick/x", processors: "normalize(min=0,max=1)");

        map.Enable();

        action1.Reset();

        Assert.That(action1.ReadValue<float>(), Is.EqualTo(0.5f));
    }

#if UNITY_EDITOR
    [Test]
    [Category("Actions")]
    public void Actions_DoNotGetTriggeredByEditorUpdates()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            runtime.PlayerFocusLost();
            Set(gamepad.leftTrigger, 0.123f, queueEventOnly: true);
            InputSystem.Update(InputUpdateType.Editor);

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_TimeoutsDoNotGetTriggeredInEditorUpdates()
    {
        ResetTime();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Devices get reset when losing focus so when we switch from the player to the editor,
        // actions would get cancelled anyway. To avoid, create a device that runs in the background
        // and enabled runInBackground.
        SetCanRunInBackground(gamepad, true);
        runtime.runInBackground = true;

        var action = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.buttonSouth, 1);

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));

            trace.Clear();

            runtime.PlayerFocusLost();
            currentTime = 10;

            InputSystem.Update(InputUpdateType.Editor);

            Assert.That(trace, Is.Empty);

            runtime.PlayerFocusGained();
            InputSystem.Update(InputUpdateType.Dynamic);

            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Performed));
        }
    }

#endif // UNITY_EDITOR

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
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

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

    // We may be looking at a situation of having more than one binding path on the same action
    // matches the same control in a system.
    // https://fogbugz.unity3d.com/f/cases/1293808/
    [Test]
    [Category("Actions")]
    public void Actions_WhenSeveralBindingsResolveToSameControl_SameControlFeedsIntoActionMultipleTimes_ButIsListedInControlsOnlyOnce()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction();
        var action2 = new InputAction();

        var actionMap = new InputActionMap();
        var action3 = actionMap.AddAction("action3");
        var action4 = actionMap.AddAction("action4");
        var action5 = actionMap.AddAction("action5");
        var action6 = actionMap.AddAction("action6");

        action1.AddBinding("<Gamepad>/buttonSouth");
        action1.AddBinding("<Gamepad>/buttonSouth");

        action2.AddBinding("<Gamepad>/buttonSouth");
        action2.AddBinding("<Gamepad>/button*");

        action3.AddBinding("<Gamepad>/buttonNorth");
        action3.AddBinding("<Gamepad>/buttonSouth");
        action3.AddBinding("<Gamepad>/buttonSouth");
        action4.AddBinding("<Gamepad>/buttonSouth"); // Should not be removed; different action.

        ////REVIEW: This setup here doesn't seem to make any sense; there's probably no need to support something like this.
        action5.AddBinding("<Gamepad>/buttonSouth", interactions: "press(behavior=0)");
        action5.AddBinding("<Gamepad>/buttonSouth", interactions: "press(behavior=1)");
        action5.AddBinding("<Gamepad>/buttonSouth", processors: "invert");

        action6.AddCompositeBinding("Dpad")
            .With("Left", "<Gamepad>/leftStick/y", processors: "clamp(min=0,max=1)")
            .With("Right", "<Gamepad>/leftStick/y", processors: "clamp(min=-1,max=0),invert");

        var action6Performed = 0;
        action6.performed += ctx => action6Performed += ctx.performed ? 1 : 0;

        Assert.That(action1.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action2.controls, Is.EquivalentTo(new[] { gamepad.buttonNorth, gamepad.buttonSouth, gamepad.buttonEast, gamepad.buttonWest }));
        Assert.That(action3.controls, Is.EquivalentTo(new[] { gamepad.buttonNorth, gamepad.buttonSouth }));
        Assert.That(action4.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(action5.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(action6.controls, Is.EquivalentTo(new[] { gamepad.leftStick.y })); // Only mentioned once.

        Assert.That(action1.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));
        Assert.That(action2.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));
        Assert.That(action3.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(1));
        Assert.That(action4.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));
        Assert.That(action5.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));

        // Go through a bit of pressing and releasing to make sure that the action state
        // processing wasn't thrown off its track.

        action1.Enable();
        action2.Enable();
        action3.Enable();
        action4.Enable();
        action5.Enable();
        action6.Enable();

        Press(gamepad.buttonSouth);

        Assert.That(action1.triggered, Is.True);
        Assert.That(action2.triggered, Is.True);
        Assert.That(action3.triggered, Is.True);
        Assert.That(action4.triggered, Is.True);

        InputSystem.Update();

        Assert.That(action1.triggered, Is.False);
        Assert.That(action2.triggered, Is.False);
        Assert.That(action3.triggered, Is.False);
        Assert.That(action4.triggered, Is.False);

        Release(gamepad.buttonSouth);

        Assert.That(action1.triggered, Is.False);
        Assert.That(action2.triggered, Is.False);
        Assert.That(action3.triggered, Is.False);
        Assert.That(action4.triggered, Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(action1.triggered, Is.True);
        Assert.That(action2.triggered, Is.True);
        Assert.That(action3.triggered, Is.True);
        Assert.That(action4.triggered, Is.True);

        InputSystem.Update();

        Assert.That(action1.triggered, Is.False);
        Assert.That(action2.triggered, Is.False);
        Assert.That(action3.triggered, Is.False);
        Assert.That(action4.triggered, Is.False);

        Set(gamepad.leftStick, new Vector2(0, -1));

        Assert.That(action6Performed, Is.EqualTo(1));
        Assert.That(action6.ReadValue<Vector2>(), Is.EqualTo(new Vector2(1,  0)));
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
        ResetTime();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction("action1", InputActionType.Button, binding: "<Gamepad>/buttonSouth", interactions: "Hold");
        var action2 = new InputAction("action2", InputActionType.Button, binding: "<Gamepad>/leftStick");
        var action3 = new InputAction("action3", InputActionType.Button, binding: "<Gamepad>/rightStick");

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

            currentTime = 0.234f;
            runtime.advanceTimeEachDynamicUpdate = 0;

            action1.Disable();
            action2.Disable();
            action3.Disable();

            var actions = trace.ToArray();

            Assert.That(actions.Length, Is.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].time, Is.EqualTo(0.234).Within(0.00001));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[1].time, Is.EqualTo(0.234).Within(0.00001));
            Assert.That(actions[1].action, Is.SameAs(action2));
            Assert.That(actions[1].control, Is.SameAs(gamepad.leftStick));
            Assert.That(actions[1].interaction, Is.Null);

            trace.Clear();

            // Re-enable an action and make sure that it indeed starts from scratch again.

            action1.Enable();

            Assert.That(action1.phase, Is.EqualTo(InputActionPhase.Waiting));

            currentTime = 0.345f;

            Release(gamepad.buttonSouth);
            Press(gamepad.buttonSouth);

            actions = trace.ToArray();

            Assert.That(actions.Length, Is.EqualTo(1));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].time, Is.EqualTo(0.345).Within(0.00001));
            Assert.That(actions[0].action, Is.SameAs(action1));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].interaction, Is.TypeOf<HoldInteraction>());

            trace.Clear();

            currentTime = 1;
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

    [Test]
    [Category("Actions")]
    public void Actions_ButtonActions_RespectButtonPressPoints()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.9f; // Release at 90% of press point puts release at 0.45.

        // Customize gamepad with specific button press point on right trigger (but not on left).
        InputSystem.RegisterLayoutOverride(@"
            {
                ""name"" : ""GamepadRightTriggerWithPressPoint"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""rightTrigger"", ""parameters"" : ""pressPoint=0.75"" }
                ]
            }
        ");

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var rightTriggerButton = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/rightTrigger");
        var rightTriggerValue = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/rightTrigger");
        var leftTriggerButton = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/leftTrigger");
        var leftTriggerValue = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftTrigger");

        rightTriggerButton.Enable();
        rightTriggerValue.Enable();
        leftTriggerButton.Enable();
        leftTriggerValue.Enable();

        using (var rightTriggerButtonTrace = new InputActionTrace(rightTriggerButton))
        using (var rightTriggerValueTrace = new InputActionTrace(rightTriggerValue))
        using (var leftTriggerButtonTrace = new InputActionTrace(leftTriggerButton))
        using (var leftTriggerValueTrace = new InputActionTrace(leftTriggerValue))
        {
            Set(gamepad.leftTrigger, 0.25f);
            Set(gamepad.rightTrigger, 0.25f);

            // Have crossed neither button threshold. Actions start but don't perform.
            Assert.That(leftTriggerButtonTrace, Started(leftTriggerButton, gamepad.leftTrigger, value: 0.25f));
            Assert.That(rightTriggerButtonTrace, Started(rightTriggerButton, gamepad.rightTrigger, value: 0.25f));

            Assert.That(leftTriggerValueTrace,
                Started(leftTriggerValue, gamepad.leftTrigger, value: 0.25f)
                    .AndThen(Performed(leftTriggerValue, gamepad.leftTrigger, value: 0.25f)));
            Assert.That(rightTriggerValueTrace,
                Started(rightTriggerValue, gamepad.rightTrigger, value: 0.25f)
                    .AndThen(Performed(rightTriggerValue, gamepad.rightTrigger, value: 0.25f)));

            rightTriggerButtonTrace.Clear();
            rightTriggerValueTrace.Clear();
            leftTriggerButtonTrace.Clear();
            leftTriggerValueTrace.Clear();

            Set(gamepad.leftTrigger, 0.6f);
            Set(gamepad.rightTrigger, 0.6f);

            // Have grossed global but not right trigger threshold.
            Assert.That(leftTriggerButtonTrace, Performed(leftTriggerButton, gamepad.leftTrigger, value: 0.6f));
            Assert.That(rightTriggerButtonTrace, Is.Empty);

            Assert.That(leftTriggerValueTrace, Performed(leftTriggerValue, gamepad.leftTrigger, value: 0.6f));
            Assert.That(rightTriggerValueTrace, Performed(rightTriggerValue, gamepad.rightTrigger, value: 0.6f));

            rightTriggerButtonTrace.Clear();
            rightTriggerValueTrace.Clear();
            leftTriggerButtonTrace.Clear();
            leftTriggerValueTrace.Clear();

            Set(gamepad.leftTrigger, 0.9f);
            Set(gamepad.rightTrigger, 0.9f);

            // No change on left trigger action, right trigger crossed threshold.
            Assert.That(leftTriggerButtonTrace, Is.Empty);
            Assert.That(rightTriggerButtonTrace, Performed(rightTriggerButton, gamepad.rightTrigger, value: 0.9f));

            Assert.That(leftTriggerValueTrace, Performed(leftTriggerValue, gamepad.leftTrigger, value: 0.9f));
            Assert.That(rightTriggerValueTrace, Performed(rightTriggerValue, gamepad.rightTrigger, value: 0.9f));

            rightTriggerButtonTrace.Clear();
            rightTriggerValueTrace.Clear();
            leftTriggerButtonTrace.Clear();
            leftTriggerValueTrace.Clear();

            Set(gamepad.leftTrigger, 0.6f);
            Set(gamepad.rightTrigger, 0.6f);

            // No change on left trigger action, right trigger action cancels.
            Assert.That(leftTriggerButtonTrace, Is.Empty);
            Assert.That(rightTriggerButtonTrace, Started(rightTriggerButton, gamepad.rightTrigger, value: 0.6f));

            Assert.That(leftTriggerValueTrace, Performed(leftTriggerValue, gamepad.leftTrigger, value: 0.6f));
            Assert.That(rightTriggerValueTrace, Performed(rightTriggerValue, gamepad.rightTrigger, value: 0.6f));

            rightTriggerButtonTrace.Clear();
            rightTriggerValueTrace.Clear();
            leftTriggerButtonTrace.Clear();
            leftTriggerValueTrace.Clear();

            Set(gamepad.leftTrigger, 0.4f);
            Set(gamepad.rightTrigger, 0.4f);

            // Left trigger cancels, right trigger *starts* again (but doesn't perform).
            Assert.That(leftTriggerButtonTrace, Started(leftTriggerButton, gamepad.leftTrigger, value: 0.4f));
            Assert.That(rightTriggerButtonTrace, Is.Empty);

            Assert.That(leftTriggerValueTrace, Performed(leftTriggerValue, gamepad.leftTrigger, value: 0.4f));
            Assert.That(rightTriggerValueTrace, Performed(rightTriggerValue, gamepad.rightTrigger, value: 0.4f));

            rightTriggerButtonTrace.Clear();
            rightTriggerValueTrace.Clear();
            leftTriggerButtonTrace.Clear();
            leftTriggerValueTrace.Clear();

            Set(gamepad.leftTrigger, 0f);
            Set(gamepad.rightTrigger, 0f);

            // No change on left and right trigger actions.
            Assert.That(leftTriggerButtonTrace, Canceled(leftTriggerButton, gamepad.leftTrigger, 0f));
            Assert.That(rightTriggerButtonTrace, Canceled(rightTriggerButton, gamepad.rightTrigger, 0f));

            Assert.That(leftTriggerValueTrace, Canceled(leftTriggerValue, gamepad.leftTrigger, value: 0f));
            Assert.That(rightTriggerValueTrace, Canceled(rightTriggerValue, gamepad.rightTrigger, value: 0f));

            rightTriggerButtonTrace.Clear();
            leftTriggerButtonTrace.Clear();

            // Make sure that we start and cancel if we press the buttons just a little and then release.
            Set(gamepad.leftTrigger, 0.25f);
            Set(gamepad.rightTrigger, 0.25f);

            Assert.That(leftTriggerButtonTrace, Started(leftTriggerButton, gamepad.leftTrigger, value: 0.25f));
            Assert.That(rightTriggerButtonTrace, Started(rightTriggerButton, gamepad.rightTrigger, value: 0.25f));

            rightTriggerButtonTrace.Clear();
            leftTriggerButtonTrace.Clear();

            Set(gamepad.leftTrigger, 0f);
            Set(gamepad.rightTrigger, 0f);

            Assert.That(leftTriggerButtonTrace, Canceled(leftTriggerButton, gamepad.leftTrigger, value: 0f));
            Assert.That(rightTriggerButtonTrace, Canceled(rightTriggerButton, gamepad.rightTrigger, value: 0f));
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1393330/
    [Test]
    [Category("Actions")]
    public void Actions_ButtonActions_GoBackToStartedWhenHeldBelowReleasePoint()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Make sure both PressInteraction and the default interaction handles this exactly the same way.
        var buttonAction = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/rightTrigger");
        var pressAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/rightTrigger", interactions: "press");

        buttonAction.Enable();
        pressAction.Enable();

        using (var buttonTrace = new InputActionTrace(buttonAction))
        using (var pressTrace = new InputActionTrace(pressAction))
        {
            // Started.
            Set(gamepad.rightTrigger, 0.3f);

            Assert.That(buttonTrace, Started(buttonAction, value: 0.3f));
            Assert.That(pressTrace, Started(pressAction, value: 0.3f));

            buttonTrace.Clear();
            pressTrace.Clear();

            // Started.
            Set(gamepad.rightTrigger, 0.1f);

            Assert.That(buttonTrace, Is.Empty);
            Assert.That(pressTrace, Is.Empty);

            // Performed.
            Set(gamepad.rightTrigger, 0.6f);

            Assert.That(buttonTrace, Performed(buttonAction, value: 0.6f));
            Assert.That(pressTrace, Performed(pressAction, value: 0.6f));

            buttonTrace.Clear();
            pressTrace.Clear();

            // Performed.
            Set(gamepad.rightTrigger, 0.7f);

            Assert.That(buttonTrace, Is.Empty);
            Assert.That(pressTrace, Is.Empty);

            // Started.
            Set(gamepad.rightTrigger, 0.3f);

            Assert.That(buttonTrace, Started(buttonAction, value: 0.3f));
            Assert.That(pressTrace, Started(pressAction, value: 0.3f));

            buttonTrace.Clear();
            pressTrace.Clear();

            // Canceled.
            Set(gamepad.rightTrigger, 0);

            Assert.That(buttonTrace, Canceled(buttonAction, value: 0));
            Assert.That(pressTrace, Canceled(pressAction, value: 0));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_ButtonActions_DoNotReactToCurrentStateOfControlWhenEnabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Press(gamepad.buttonSouth);

        var action = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            action.Enable();
            InputSystem.Update();

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_ButtonAndPassThroughActions_CanTurnOnInitialStateCheck()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Press(gamepad.buttonSouth);

        var buttonAction = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
        var passThroughAction = new InputAction(type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");

        buttonAction.wantsInitialStateCheck = true;
        passThroughAction.wantsInitialStateCheck = true;

        using (var buttonTrace = new InputActionTrace(buttonAction))
        using (var passThroughTrace = new InputActionTrace(passThroughAction))
        {
            buttonAction.Enable();
            passThroughAction.Enable();

            InputSystem.Update();

            Assert.That(buttonTrace, Started(buttonAction).AndThen(Performed(buttonAction)));
            Assert.That(passThroughTrace, Performed(passThroughAction));
        }
    }

    // It can be useful to react to the value of a control immediately when an action is enabled rather
    // than wait for the first time the control changes value. To do so, "Initial State Check" needs to
    // be enabled on an action. If this is done and a bound is actuated at the time an action is enabled,
    // the action pretends for the control to *just* have changed to the state it already has.
    [Test]
    [Category("Actions")]
    public void Actions_ValueActionsReactToCurrentStateOfControlWhenEnabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));
        Press(gamepad.buttonSouth);

        var actionWithoutInteraction = new InputAction("ActionWithoutInteraction", InputActionType.Value, binding: "<Gamepad>/leftStick");
        var actionWithHold = new InputAction("ActionWithHold", InputActionType.Value, binding: "<Gamepad>/buttonSouth", interactions: "Hold");
        var actionThatShouldNotTrigger = new InputAction("ActionThatShouldNotTrigger", InputActionType.Value, binding: "<Gamepad>/rightStick");

        actionWithHold.performed += ctx => Assert.Fail("Hold should not complete");
        actionThatShouldNotTrigger.started += ctx => Assert.Fail("Action should not start");
        actionThatShouldNotTrigger.performed += ctx => Assert.Fail("Action should not be performed");

        using (var trace1 = new InputActionTrace(actionWithoutInteraction))
        using (var trace2 = new InputActionTrace(actionWithHold))
        {
            actionWithoutInteraction.Enable();
            actionWithHold.Enable();
            actionThatShouldNotTrigger.Enable();

            Assert.That(trace1, Is.Empty);
            Assert.That(trace2, Is.Empty);

            InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.345f, 0.456f));
            InputSystem.Update();

            Assert.That(trace1, Started(actionWithoutInteraction, control: gamepad.leftStick, value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                .AndThen(Performed(actionWithoutInteraction, control: gamepad.leftStick, value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f))))
                .AndThen(Performed(actionWithoutInteraction, control: gamepad.leftStick, value: new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f)))));
            Assert.That(trace2, Started<HoldInteraction>(actionWithHold, control: gamepad.buttonSouth, value: 1f));
        }
    }

    // Composites have logic to ignore control state changes coming from the same event. We had a bug where
    // this threw off initial value checks for actions as we made up a fake event. Specifically test
    // for this here.
    // https://fogbugz.unity3d.com/f/cases/1274977/
    [Test]
    [Category("Actions")]
    public void Actions_ValueActionsReactToCurrentStateOfControlWhenEnabled_WithCompositeBinding()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();

        var action = map.AddAction("action", type: InputActionType.Value);
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        Press(keyboard.dKey);

        map.Enable();
        InputSystem.Update();

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));

        map.Disable();
        InputSystem.Update();

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        map.Enable();
        InputSystem.Update();

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));

        map.Disable();
        InputSystem.Update();

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        map.Enable();
        InputSystem.Update();

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));
    }

    // Value actions perform an initial state check when enabled. These state checks are performed
    // from InputSystem.onBeforeUpdate. However, if we enable an action as part of event processing,
    // we will react to the state of a control right away and should then not ALSO perform an
    // initial state check in the next update.
    //
    // This is relevant mainly for InputUser.onUnpairedDeviceUsed which will trigger from
    // InputSystem.onEvent and by means of PlayerInput frequently lead to actions being disabled
    // and enabled as part of event processing (e.g. when switching control schemes in single player).
    [Test]
    [Category("Actions")]
    public void Actions_ValueActionsEnabledInOnEvent_DoNotReactToCurrentStateOfControlTwice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");

        InputSystem.onEvent +=
            (eventPtr, device) => { action.Enable(); };

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftStick, new Vector2(0.234f, 0.345f));

            Assert.That(trace,
                Started(action, control: gamepad.leftStick)
                    .AndThen(Performed(action, control: gamepad.leftStick)));

            trace.Clear();

            // This one should not trigger anything on the action.
            InputSystem.Update();

            Assert.That(trace, Is.Empty);
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1192972/
    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveCallback_FromCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/buttonSouth");
        action.Enable();

        var callback1Triggered = false;
        var callback2Triggered = false;

        Action<InputAction.CallbackContext> callback1 = null;
        callback1 = _ =>
        {
            callback1Triggered = true;
            action.performed -= callback1;
        };

        action.performed += callback1;
        action.performed += _ => callback2Triggered = true;

        Press(gamepad.buttonSouth);

        Assert.That(callback1Triggered, Is.True);
        Assert.That(callback2Triggered, Is.True);
    }

    [Test]
    [Category("Actions")]
    [TestCase("started")]
    [TestCase("performed")]
    [TestCase("canceled")]
    public void Actions_CanDisableAction_FromCallback(string callback)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        if (callback == "started")
            action.started += _ => action.Disable();
        else if (callback == "performed")
            action.performed += _ => action.Disable();
        else if (callback == "canceled")
            action.canceled += _ => action.Disable();
        else
            Assert.Fail();

        action.Enable();

        PressAndRelease(gamepad.buttonSouth);

        Assert.That(action.enabled, Is.False);

        using (var trace = new InputActionTrace())
        {
            PressAndRelease(gamepad.buttonSouth);

            Assert.That(trace, Is.Empty);
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1322530/
    [Test]
    [Category("Actions")]
    [TestCase("started")]
    [TestCase("performed")]
    [TestCase("canceled")]
    public void Actions_CanAddAndRemoveCallbacks_FromCallback(string callback)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        var invocations = new List<string>();
        Action<InputAction.CallbackContext> delegate2 =
            _ => invocations.Add("delegate2");
        Action<InputAction.CallbackContext> delegate1 = null;
        delegate1 =
            _ =>
        {
            invocations.Add("delegate1");
            switch (callback)
            {
                case "started":
                    action.started -= delegate1;
                    action.started += delegate2;
                    break;
                case "performed":
                    action.performed -= delegate1;
                    action.performed += delegate2;
                    break;
                case "canceled":
                    action.canceled -= delegate1;
                    action.canceled += delegate2;
                    break;
            }
        };

        switch (callback)
        {
            case "started":
                action.started += _ => invocations.Add("first");
                action.started += delegate1;
                action.started += _ => invocations.Add("last");
                break;
            case "performed":
                action.performed += _ => invocations.Add("first");
                action.performed += delegate1;
                action.performed += _ => invocations.Add("last");
                break;
            case "canceled":
                action.canceled += _ => invocations.Add("first");
                action.canceled += delegate1;
                action.canceled += _ => invocations.Add("last");
                break;
            default:
                Assert.Fail();
                break;
        }

        action.Enable();

        PressAndRelease(gamepad.buttonSouth);

        Assert.That(invocations, Is.EqualTo(new[] { "first", "delegate1", "last" }));

        invocations.Clear();

        PressAndRelease(gamepad.buttonSouth);

        Assert.That(invocations, Is.EquivalentTo(new[] { "first", "last", "delegate2" }));
    }

    // https://fogbugz.unity3d.com/f/cases/1242406/
    // Binding resolution destroys/recreates InputActionState data. When triggering this from within
    // an action callback, we must ensure that we're not pulling the rug from under an InputActionState
    // while it is still processing or we'll risk corrupting memory.
    [Test]
    [Category("Actions")]
    [TestCase(true)]
    [TestCase(false)]
    public unsafe void Actions_CanTriggerBindingResolutionOnAction_FromCallback(bool withEnableDisable)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Value);
        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Mouse>/delta");

        Mouse mouse = null;
        action.performed += _ =>
        {
            Assert.That(action.GetOrCreateActionMap().m_State.isProcessingControlStateChange, Is.True);
            var basePtrBefore = new IntPtr(action.GetOrCreateActionMap().m_State.memory.basePtr);

            if (withEnableDisable)
                action.Disable();
            mouse = InputSystem.AddDevice<Mouse>();
            if (withEnableDisable)
                action.Enable();

            Assert.That(basePtrBefore, Is.EqualTo(new IntPtr(action.GetOrCreateActionMap().m_State.memory.basePtr)),
                "Unmanaged memory must not have been touched while action is executing");
        };

        action.Enable();

        Set(gamepad.leftStick, new Vector2(0.234f, 0.345f));

        Assert.That(action.controls, Is.EquivalentTo(new InputControl[] {gamepad.leftStick, mouse.delta}));
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ConstantFloatTestProcessor : InputProcessor<float>
    {
        public const float EXPECTED_VALUE = 0.5f;
        public override float Process(float value, InputControl control)
        {
            return EXPECTED_VALUE;
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActiveBindingsHaveCorrectBindingIndicesAfterBindingResolution()
    {
        // Attaching a processor to a particular binding is how
        // we test that we are correctly referencing the same binding
        // from before and after the binding resolution.
        // This is a tricky issue to tease out. Testing with just ReadValue(),
        // with the control (i.e. mouse.leftButton) or with action callbacks
        // could all appear correct because those don't actually use bindingIndex.
        // This issue originally manifested itself as an assert in another place in the code.
        InputSystem.RegisterProcessor<ConstantFloatTestProcessor>();

        // This test is sensitive to binding order.
        // It's important that the active binding is not in the first
        // position of the action (i.e. not at the default index).
        var map = new InputActionMap("map");
        var action = map.AddAction("action1", binding: "<Gamepad>/buttonSouth");
        action.AddBinding("<Mouse>/leftButton").WithProcessor<ConstantFloatTestProcessor>(); // binding in 2nd position.
        map.Enable();

        var mouse = InputSystem.AddDevice<Mouse>();
        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0));

        // Put the action into an active state using the 2nd binding.
        Press(mouse.leftButton);
        Assert.That(action.activeControl, Is.SameAs(mouse.leftButton));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(ConstantFloatTestProcessor.EXPECTED_VALUE));

        // Perform binding resolution while the action is active.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Assert.That(action.activeControl, Is.SameAs(mouse.leftButton));

        // Check the active control is still triggered by the 2nd binding (with the processor attached).
        Assert.That(action.ReadValue<float>(), Is.EqualTo(ConstantFloatTestProcessor.EXPECTED_VALUE));

        // Perform binding resolution again.
        // This would cause an assert if the binding indices were incorrect
        // as the binding resolver would choke on the bad data at this point.
        InputSystem.RemoveDevice(gamepad);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAndEnableOtherAction_FromCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.performed +=
            ctx =>
        {
            ++receivedCalls;
        };
        action.Enable();

        var disableAction = new InputAction(binding: "<Gamepad>/buttonEast");
        disableAction.performed +=
            ctx =>
        {
            action.Disable();
        };
        disableAction.Enable();

        var enableAction = new InputAction(binding: "<Gamepad>/buttonWest");
        enableAction.performed +=
            ctx =>
        {
            action.Enable();
        };
        enableAction.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.South));
        InputSystem.Update();
        Assert.That(receivedCalls, Is.EqualTo(1));

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.East));
        InputSystem.Update();
        Assert.That(action.enabled, Is.False);

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.South));
        InputSystem.Update();
        Assert.That(receivedCalls, Is.EqualTo(1));

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.West));
        InputSystem.Update();
        Assert.That(action.enabled, Is.True);

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.South));
        InputSystem.Update();
        Assert.That(receivedCalls, Is.EqualTo(2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAndEnable_FromCallbackWhileOtherCompositeBindingIsProgress()
    {
        // Enables "Modifier must be pressed first" behavior on all Composite Bindings
        InputSystem.settings.shortcutKeysConsumeInput = true;

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");

        var withModiferReceivedCalls = 0;
        var actionWithModifier = map.AddAction("OneModifier", type: InputActionType.Button);
        actionWithModifier.AddCompositeBinding("OneModifier")
            .With("Binding", "<Keyboard>/space")
            .With("Modifier", "<Keyboard>/ctrl");
        actionWithModifier.performed += _ => ++ withModiferReceivedCalls;

        var actionWithoutModifier = map.AddAction("One", type: InputActionType.Button, binding: "<Keyboard>/space");
        actionWithoutModifier.performed += _ => actionWithModifier.Disable();

        map.Enable();

        // Press the SPACE key used by both binding
        // actionWithModifier : SPACE key binding state will have current time but without a preceding CTRL key it will not trigger
        // actionWithoutModifier : SPACE key binding will trigger the performed lambda which will disable actionWithModifier
        PressAndRelease(keyboard.spaceKey);
        InputSystem.Update();
        Assume.That(actionWithModifier.enabled, Is.False);

        // Re-enable action which has been disabled by actionWithoutModifier
        actionWithModifier.Enable();

        // Press the CTRL+SPACE to trigger actionWithModifier and not actionWithoutModifier
        Press(keyboard.leftCtrlKey, queueEventOnly: true);
        Press(keyboard.spaceKey);
        InputSystem.Update();
        Assert.That(withModiferReceivedCalls, Is.EqualTo(1));
        Assert.That(actionWithModifier.enabled, Is.True);
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

    // InputAction.triggered is superseded by WasPerformedInFrame().
    [Test]
    [Category("Actions")]
    public void Actions_CanDetermineIfActionTriggeredInFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth", type: InputActionType.Button);
        action.Enable();

        Assert.That(action.triggered, Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(action.triggered, Is.True);

        InputSystem.Update();

        Assert.That(action.triggered, Is.False);

        Release(gamepad.buttonSouth);

        Assert.That(action.triggered, Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(action.triggered, Is.True);

        action.Disable();

        // Disabling the action should *not* reset triggered.
        Assert.That(action.triggered, Is.True);

        action.Enable();

        InputSystem.Update();

        Assert.That(action.triggered, Is.False);

        Press(gamepad.buttonSouth, queueEventOnly: true);
        Release(gamepad.buttonSouth, queueEventOnly: true);
        Press(gamepad.buttonSouth, queueEventOnly: true);
        Release(gamepad.buttonSouth);

        Assert.That(action.triggered, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryIfPerformedInCurrentFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var simpleAction = new InputAction(binding: "<Gamepad>/buttonSouth");
        var holdAction = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.5)");

        simpleAction.Enable();
        holdAction.Enable();

        Assert.That(simpleAction.WasPerformedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(simpleAction.WasPerformedThisFrame(), Is.True);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        currentTime += 1;
        InputSystem.Update();

        Assert.That(simpleAction.WasPerformedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.True);

        holdAction.Disable();

        Assert.That(holdAction.WasPerformedThisFrame(), Is.True);

        holdAction.Enable();

        Assert.That(holdAction.WasPerformedThisFrame(), Is.True);

        InputSystem.Update();

        Assert.That(simpleAction.WasPerformedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryIfHoldInteractionCompletedInCurrentFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var holdAction = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.5)");

        holdAction.Enable();

        Assert.That(holdAction.WasPressedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(holdAction.WasPressedThisFrame(), Is.True);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        InputSystem.Update();

        Assert.That(holdAction.WasPressedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        // Release before the hold duration threshold was met.
        Release(gamepad.buttonSouth);

        Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(holdAction.WasPressedThisFrame(), Is.True);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        currentTime += 1;
        InputSystem.Update();

        Assert.That(holdAction.WasPressedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.True);

        InputSystem.Update();

        Assert.That(holdAction.WasPressedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);

        // Release after the hold duration threshold was met.
        Release(gamepad.buttonSouth);

        Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.True);

        InputSystem.Update();

        Assert.That(holdAction.WasPressedThisFrame(), Is.False);
        Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenDisabled_DoesNotBecomeCompleted()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var simpleAction = new InputAction(binding: "<Gamepad>/buttonSouth");
        var holdAction = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.5)");

        simpleAction.Enable();
        holdAction.Enable();

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.False);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        Press(gamepad.buttonSouth);

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.False);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        currentTime += 1;
        InputSystem.Update();

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.False);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        holdAction.Disable();

        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        holdAction.Enable();

        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        InputSystem.Update();

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.False);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        Release(gamepad.buttonSouth);

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.True);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        simpleAction.Disable();
        holdAction.Disable();

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.True);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

        simpleAction.Enable();
        holdAction.Enable();

        Assert.That(simpleAction.WasReleasedThisFrame(), Is.True);
        Assert.That(simpleAction.WasCompletedThisFrame(), Is.False);
        Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
        Assert.That(holdAction.WasCompletedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Button)]
    [TestCase(InputActionType.PassThrough)]
    public void Actions_CanDistinguishCanceledAndCompletedInCurrentFrame(InputActionType actionType)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var defaultAction = new InputAction(type: actionType, binding: "<Gamepad>/buttonSouth");
        var pressAction = new InputAction(type: actionType, binding: "<Gamepad>/buttonSouth", interactions: "press");
        var holdAction = new InputAction(type: actionType, binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.5)");

        defaultAction.Enable();
        pressAction.Enable();
        holdAction.Enable();

        Assert.That(defaultAction.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(pressAction.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(holdAction.phase, Is.EqualTo(InputActionPhase.Waiting));

        using (var defaultTrace = new InputActionTrace(defaultAction))
        using (var pressTrace = new InputActionTrace(pressAction))
        using (var holdTrace = new InputActionTrace(holdAction))
        {
            // Press button. Actions should be considered pressed, but the
            // hold action should not be considered performed yet.
            Press(gamepad.buttonSouth);

            Assert.That(defaultTrace, actionType != InputActionType.PassThrough
                ? Started(defaultAction).AndThen(Performed(defaultAction))
                : Performed(defaultAction));
            Assert.That(defaultAction.WasPressedThisFrame(), Is.True);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.False);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.True);
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.False);

            Assert.That(pressTrace, Started(pressAction).AndThen(Performed(pressAction)));
            Assert.That(pressAction.WasPressedThisFrame(), Is.True);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.False);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.True);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.False);

            Assert.That(holdTrace, Started(holdAction));
            Assert.That(holdAction.WasPressedThisFrame(), Is.True);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Keep holding button but for less than the hold duration needed.
            InputSystem.Update();

            Assert.That(defaultTrace, Is.Empty);
            Assert.That(defaultAction.WasPressedThisFrame(), Is.False);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.False);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.False);
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.False);

            Assert.That(pressTrace, Is.Empty);
            Assert.That(pressAction.WasPressedThisFrame(), Is.False);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.False);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.False);

            Assert.That(holdTrace, Is.Empty);
            Assert.That(holdAction.WasPressedThisFrame(), Is.False);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Release button, the actions should cancel. The hold action should not be considered completed
            // since it was not held long enough to be performed.
            Release(gamepad.buttonSouth);

            Assert.That(defaultTrace, actionType != InputActionType.PassThrough
                ? Canceled(defaultAction)
                : Performed(defaultAction));
            Assert.That(defaultAction.WasPressedThisFrame(), Is.False);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.True);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.EqualTo(actionType == InputActionType.PassThrough));
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.EqualTo(actionType == InputActionType.Button));

            Assert.That(pressTrace, Canceled(pressAction));
            Assert.That(pressAction.WasPressedThisFrame(), Is.False);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.True);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.True);

            Assert.That(holdTrace, Canceled(holdAction));
            Assert.That(holdAction.WasPressedThisFrame(), Is.False);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Press button again. Same assertions as before.
            Press(gamepad.buttonSouth);

            Assert.That(defaultTrace, actionType != InputActionType.PassThrough
                ? Started(defaultAction).AndThen(Performed(defaultAction))
                : Performed(defaultAction));
            Assert.That(defaultAction.WasPressedThisFrame(), Is.True);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.False);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.True);
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.False);

            Assert.That(pressTrace, Started(pressAction).AndThen(Performed(pressAction)));
            Assert.That(pressAction.WasPressedThisFrame(), Is.True);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.False);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.True);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.False);

            Assert.That(holdTrace, Started(holdAction));
            Assert.That(holdAction.WasPressedThisFrame(), Is.True);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Hold button for long enough.
            currentTime += 1;
            InputSystem.Update();

            Assert.That(defaultTrace, Is.Empty);
            Assert.That(defaultAction.WasPressedThisFrame(), Is.False);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.False);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.False);
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.False);

            Assert.That(pressTrace, Is.Empty);
            Assert.That(pressAction.WasPressedThisFrame(), Is.False);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.False);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.False);

            Assert.That(holdTrace, Performed(holdAction));
            Assert.That(holdAction.WasPressedThisFrame(), Is.False);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.True);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Keep holding button.
            InputSystem.Update();

            Assert.That(defaultTrace, Is.Empty);
            Assert.That(defaultAction.WasPressedThisFrame(), Is.False);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.False);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.False);
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.False);

            Assert.That(pressTrace, Is.Empty);
            Assert.That(pressAction.WasPressedThisFrame(), Is.False);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.False);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.False);

            Assert.That(holdTrace, Is.Empty);
            Assert.That(holdAction.WasPressedThisFrame(), Is.False);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.False);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.False);

            defaultTrace.Clear();
            pressTrace.Clear();
            holdTrace.Clear();

            // Release button, the actions should cancel. The hold action should now be considered completed
            // since it was held long enough to be performed.
            Release(gamepad.buttonSouth);

            Assert.That(defaultTrace, actionType != InputActionType.PassThrough
                ? Canceled(defaultAction)
                : Performed(defaultAction));
            Assert.That(defaultAction.WasPressedThisFrame(), Is.False);
            Assert.That(defaultAction.WasReleasedThisFrame(), Is.True);
            Assert.That(defaultAction.WasPerformedThisFrame(), Is.EqualTo(actionType == InputActionType.PassThrough));
            Assert.That(defaultAction.WasCompletedThisFrame(), Is.EqualTo(actionType == InputActionType.Button));

            Assert.That(pressTrace, Canceled(pressAction));
            Assert.That(pressAction.WasPressedThisFrame(), Is.False);
            Assert.That(pressAction.WasReleasedThisFrame(), Is.True);
            Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
            Assert.That(pressAction.WasCompletedThisFrame(), Is.True);

            Assert.That(holdTrace, Canceled(holdAction));
            Assert.That(holdAction.WasPressedThisFrame(), Is.False);
            Assert.That(holdAction.WasReleasedThisFrame(), Is.True);
            Assert.That(holdAction.WasPerformedThisFrame(), Is.False);
            Assert.That(holdAction.WasCompletedThisFrame(), Is.True);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanReadValueFromAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var buttonAction = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
        var triggerAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftTrigger");
        var stickAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");

        // Should get all defaults when not enabled.
        Assert.That(buttonAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(triggerAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(stickAction.ReadValue<Vector2>(),
            Is.EqualTo(Vector2.zero)
                .Using(Vector2EqualityComparer.Instance));
        ////REVIEW: would be great to get boxed versions of the default values but that's much harder to do
        Assert.That(buttonAction.ReadValueAsObject(), Is.Null);
        Assert.That(triggerAction.ReadValueAsObject(), Is.Null);
        Assert.That(stickAction.ReadValueAsObject(), Is.Null);

        buttonAction.Enable();
        triggerAction.Enable();
        stickAction.Enable();

        // Should get all defaults when there was no input yet.
        Assert.That(buttonAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(triggerAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(stickAction.ReadValue<Vector2>(),
            Is.EqualTo(Vector2.zero)
                .Using(Vector2EqualityComparer.Instance));
        Assert.That(buttonAction.ReadValueAsObject(), Is.Null);
        Assert.That(triggerAction.ReadValueAsObject(), Is.Null);
        Assert.That(stickAction.ReadValueAsObject(), Is.Null);

        Press(gamepad.buttonSouth, queueEventOnly: true);
        Set(gamepad.leftTrigger, 0.234f, queueEventOnly: true);
        Set(gamepad.leftStick, new Vector2(0.234f, 0.345f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(buttonAction.ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
        Assert.That(triggerAction.ReadValue<float>(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(stickAction.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                .Using(Vector2EqualityComparer.Instance));
        Assert.That(buttonAction.ReadValueAsObject(), Is.EqualTo(1).Within(0.00001));
        Assert.That(triggerAction.ReadValueAsObject(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(stickAction.ReadValueAsObject(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                .Using(Vector2EqualityComparer.Instance));

        InputSystem.Update();

        Assert.That(buttonAction.ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
        Assert.That(triggerAction.ReadValue<float>(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(stickAction.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                .Using(Vector2EqualityComparer.Instance));
        Assert.That(buttonAction.ReadValueAsObject(), Is.EqualTo(1).Within(0.00001));
        Assert.That(triggerAction.ReadValueAsObject(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(stickAction.ReadValueAsObject(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
                .Using(Vector2EqualityComparer.Instance));

        // Disabling an action should result in all default values.
        buttonAction.Disable();
        triggerAction.Disable();
        stickAction.Disable();

        Assert.That(buttonAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(triggerAction.ReadValue<float>(), Is.EqualTo(0).Within(0.00001));
        Assert.That(stickAction.ReadValue<Vector2>(),
            Is.EqualTo(Vector2.zero)
                .Using(Vector2EqualityComparer.Instance));
        Assert.That(buttonAction.ReadValueAsObject(), Is.Null);
        Assert.That(triggerAction.ReadValueAsObject(), Is.Null);
        Assert.That(stickAction.ReadValueAsObject(), Is.Null);
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Value, "press")]
    [TestCase(InputActionType.Value, "hold(duration=0.5)")]
    [TestCase(InputActionType.Button)]
    [TestCase(InputActionType.Button, "press")]
    [TestCase(InputActionType.Button, "hold(duration=0.5)")]
    [TestCase(InputActionType.PassThrough)]
    public void Actions_CanReadValueFromAction_AsButton(InputActionType actionType, string interactions = null)
    {
        // Set global press and release points to known values.
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f; // 80% puts the release point at 0.4.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: actionType, binding: "<Gamepad>/leftTrigger", interactions: interactions);
        action.Enable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Press such that it stays below press threshold.
        Set(gamepad.leftTrigger, 0.25f);

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Press some more such that it crosses the press threshold.
        Set(gamepad.leftTrigger, 0.75f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Disabling an action at this point should affect IsPressed() but should
        // not affect WasPressedThisFrame() and WasReleasedThisFrame().
        action.Disable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Re-enabling it should have no effect on WasPressedThisFrame() and
        // WasReleasedThisFrame() either. Also IsPressed() should remain false
        // as the button may have been released and the action wouldn't see
        // the update while disabled.
        action.Enable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Advance one frame.
        InputSystem.Update();

        // Value actions perform an initial state check which flips the press state
        // back on.
        if (action.type == InputActionType.Value)
        {
            Assert.That(action.IsPressed(), Is.True);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.False);
        }
        else
        {
            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            Set(gamepad.leftTrigger, 0.6f);

            Assert.That(action.IsPressed(), Is.True);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.False);
        }

        // Release a bit but remain above release threshold.
        Set(gamepad.leftTrigger, 0.41f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Go below release threshold.
        Set(gamepad.leftTrigger, 0.2f);

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.True);

        // Disabling should not affect WasReleasedThisFrame().
        action.Disable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.True);

        // So should re-enabling.
        action.Enable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.True);

        // Advance one frame. Should reset WasReleasedThisFrame().
        InputSystem.Update();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Press-and-release in same frame.
        Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
        Set(gamepad.leftTrigger, 0.25f);

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.True);

        // Advance one frame.
        InputSystem.Update();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);

        // Press-and-release-and-press-again in same frame.
        Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
        Set(gamepad.leftTrigger, 0.25f, queueEventOnly: true);
        Set(gamepad.leftTrigger, 0.75f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Value, "press")]
    [TestCase(InputActionType.Value, "hold(duration=0.5)")]
    [TestCase(InputActionType.Button)]
    [TestCase(InputActionType.Button, "press")]
    [TestCase(InputActionType.Button, "hold(duration=0.5)")]
    [TestCase(InputActionType.PassThrough)]
    public void Actions_CanReadPerformedFromAction_AsButton(InputActionType actionType, string interactions = null)
    {
        // This test is structured the same as Actions_CanReadValueFromAction_AsButton above,
        // but with additional testing that the phase changes are correct for the given action type and interaction,
        // and additionally test functionality of WasPerformedThisFrame() and WasCompletedThisFrame(), which can
        // be different than WasPressedThisFrame() and WasReleasedThisFrame().

        // Set global press and release points to known values.
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f; // 80% puts the release point at 0.4.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: actionType, binding: "<Gamepad>/leftTrigger", interactions: interactions);

        var isHold = interactions?.StartsWith("hold") ?? false;
        var isPress = interactions?.StartsWith("press") ?? false;
        var isButtonLike = (action.type == InputActionType.Value && isPress) ||
            (action.type == InputActionType.Button && !isHold);

        using (var trace = new InputActionTrace(action))
        {
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));

            action.Enable();

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            Assert.That(trace, Is.Empty);
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
            Assert.That(action.WasPerformedThisFrame(), Is.False);
            Assert.That(action.WasCompletedThisFrame(), Is.False);

            // Press such that it stays below press threshold.
            Set(gamepad.leftTrigger, 0.25f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | W -> S, P, S*    | T/F |              |        |
            // | Value  | Press       | W -> S           | F/F | true         |        |
            // | Value  | Hold        | W    (No Change) | F/F |              | true   |
            // | Button | Default     | W -> S           | F/F | true         |        |
            // | Button | Press       | W -> S           | F/F | true         |        |
            // | Button | Hold        | W    (No Change) | F/F |              | true   |
            // | Pass   | Default     | W -> P           | T/F |              |        |

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Started(action).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(trace, Started(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isHold)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (action.type == InputActionType.PassThrough)
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Press some more such that it crosses the press threshold.
            Set(gamepad.leftTrigger, 0.75f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | S -> P, S*       | T/F |              |        |
            // | Value  | Press       | S -> P           | T/F | true         |        |
            // | Value  | Hold        | W -> S           | F/F |              | true   |
            // | Button | Default     | S -> P           | T/F | true         |        |
            // | Button | Press       | S -> P           | T/F | true         |        |
            // | Button | Hold        | W -> S           | F/F |              | true   |
            // | Pass   | Default     | P -> P           | T/F |              |        |

            Assert.That(action.IsPressed(), Is.True);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isHold)
            {
                Assert.That(trace, Started(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Disabling an action at this point should affect IsPressed() but should
            // not affect WasPressedThisFrame() and WasReleasedThisFrame().
            action.Disable();

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            Assert.That(trace, Canceled(action));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isHold)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Re-enabling it should have no effect on WasPressedThisFrame() and
            // WasReleasedThisFrame() either. Also IsPressed() should remain false
            // as the button may have been released and the action wouldn't see
            // the update while disabled.
            action.Enable();

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            Assert.That(trace, Is.Empty);
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isHold)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Advance one frame.
            InputSystem.Update();

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | W -> S, P, S*    | T/F |              |        |
            // | Value  | Press       | W -> S, P        | T/F | true         |        |
            // | Value  | Hold        | W -> S           | F/F |              | true   |
            // | Button | Default     | W    (No Change) | F/F | true         |        |
            // | Button | Press       | W    (No Change) | F/F | true         |        |
            // | Button | Hold        | W    (No Change) | F/F |              | true   |
            // | Pass   | Default     | W    (No Change) | F/F |              |        |

            // Value actions perform an initial state check which flips the press state
            // back on.
            if (action.type == InputActionType.Value)
            {
                Assert.That(action.IsPressed(), Is.True);
                Assert.That(action.WasPressedThisFrame(), Is.True);
                Assert.That(action.WasReleasedThisFrame(), Is.False);

                if (interactions == null)
                {
                    Assert.That(trace, Started(action).AndThen(Performed(action)));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                    Assert.That(action.WasPerformedThisFrame(), Is.True);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
                else if (isPress)
                {
                    Assert.That(trace, Started(action).AndThen(Performed(action)));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                    Assert.That(action.WasPerformedThisFrame(), Is.True);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
                else if (isHold)
                {
                    Assert.That(trace, Started(action));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                    Assert.That(action.WasPerformedThisFrame(), Is.False);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
            }
            else
            {
                Assert.That(action.IsPressed(), Is.False);
                Assert.That(action.WasPressedThisFrame(), Is.False);
                Assert.That(action.WasReleasedThisFrame(), Is.False);

                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);

                trace.Clear();

                Set(gamepad.leftTrigger, 0.6f);

                // W = Waiting, S = Started, P = Performed, C = Canceled
                // P/C = Performed/Completed, T = True, F = False
                // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
                // |--------|-------------|------------------|-----|--------------|--------|
                // | Button | Default     | W -> S, P        | T/F | true         |        |
                // | Button | Press       | W -> S, P        | T/F | true         |        |
                // | Button | Hold        | W -> S           | F/F |              | true   |
                // | Pass   | Default     | W -> P           | T/F |              |        |

                Assert.That(action.IsPressed(), Is.True);
                Assert.That(action.WasPressedThisFrame(), Is.True);
                Assert.That(action.WasReleasedThisFrame(), Is.False);

                if (isButtonLike)
                {
                    Assert.That(trace, Started(action).AndThen(Performed(action)));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                    Assert.That(action.WasPerformedThisFrame(), Is.True);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
                else if (isHold)
                {
                    Assert.That(trace, Started(action));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                    Assert.That(action.WasPerformedThisFrame(), Is.False);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
                else
                {
                    Assert.That(trace, Performed(action));
                    Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                    Assert.That(action.WasPerformedThisFrame(), Is.True);
                    Assert.That(action.WasCompletedThisFrame(), Is.False);
                }
            }

            trace.Clear();

            // Release a bit but remain above release threshold.
            Set(gamepad.leftTrigger, 0.41f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | S -> P, S*       | T/F |              |        |
            // | Value  | Press       | P    (No Change) | F/F | true         |        |
            // | Value  | Hold        | S    (No Change) | F/F |              | true   |
            // | Button | Default     | P    (No Change) | F/F | true         |        |
            // | Button | Press       | P    (No Change) | F/F | true         |        |
            // | Button | Hold        | S    (No Change) | F/F |              | true   |
            // | Pass   | Default     | P -> P           | T/F |              |        |

            Assert.That(action.IsPressed(), Is.True);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isHold)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Go below release threshold.
            Set(gamepad.leftTrigger, 0.2f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | S -> P, S*       | T/F |              |        |
            // | Value  | Press       | P -> S           | F/T | true         |        |
            // | Value  | Hold        | S    (No Change) | F/F |              | true   |
            // | Button | Default     | P -> S           | F/T | true         |        |
            // | Button | Press       | P -> S           | F/T | true         |        |
            // | Button | Hold        | S    (No Change) | F/F |              | true   |
            // | Pass   | Default     | P -> P           | T/F |              |        |

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.True);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(trace, Started(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else if (isHold)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(trace, Performed(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Disabling should not affect WasReleasedThisFrame().
            action.Disable();

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.True);

            Assert.That(trace, Canceled(action));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else if (isHold)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // So should re-enabling.
            action.Enable();

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.True);

            Assert.That(trace, Is.Empty);
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else if (isHold)
            {
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Advance one frame. Should reset WasReleasedThisFrame().
            InputSystem.Update();

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | W -> S, P, S*    | T/F |              |        |
            // | Value  | Press       | W -> S           | F/F | true         |        |
            // | Value  | Hold        | W    (No Change) | F/F |              | true   |
            // | Button | Default     | W    (No Change) | F/F | true         |        |
            // | Button | Press       | W    (No Change) | F/F | true         |        |
            // | Button | Hold        | W    (No Change) | F/F |              | true   |
            // | Pass   | Default     | W    (No Change) | F/F |              |        |

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Started(action).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (action.type == InputActionType.Value && isPress)
            {
                Assert.That(trace, Started(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Press-and-release in same frame.
            Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
            Set(gamepad.leftTrigger, 0.25f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // P/C = Performed/Completed, T = True, F = False
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | S -> P, S*, P, S*| T/F |              |        |
            // | Value  | Press       | S -> P, S        | T/T | true         |        |
            // | Value  | Hold        | W -> S           | F/F |              | true   |
            // | Button | Default     | W -> S, P, S     | T/T | true         |        |
            // | Button | Press       | W -> S, P, S     | T/T | true         |        |
            // | Button | Hold        | W -> S           | F/F |              | true   |
            // | Pass   | Default     | W -> P, P        | T/F |              |        |

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.True);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Performed(action).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (action.type == InputActionType.Value && isPress)
            {
                Assert.That(trace, Performed(action).AndThen(Started(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else if (isHold)
            {
                Assert.That(trace, Started(action));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (action.type == InputActionType.Button && isButtonLike)
            {
                Assert.That(trace, Started(action).AndThen(Performed(action)).AndThen(Started(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else
            {
                Assert.That(trace, Performed(action).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Advance one frame.
            InputSystem.Update();

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // | Type   | Interaction | Phase Change     | P/C | isButtonLike | isHold |
            // |--------|-------------|------------------|-----|--------------|--------|
            // | Value  | Default     | S    (No Change) | F/F |              |        |
            // | Value  | Press       | S    (No Change) | F/F | true         |        |
            // | Value  | Hold        | S    (No Change) | F/F |              | true   |
            // | Button | Default     | S    (No Change) | F/F | true         |        |
            // | Button | Press       | S    (No Change) | F/F | true         |        |
            // | Button | Hold        | S    (No Change) | F/F |              | true   |
            // | Pass   | Default     | P    (No Change) | F/F |              |        |

            Assert.That(action.IsPressed(), Is.False);
            Assert.That(action.WasPressedThisFrame(), Is.False);
            Assert.That(action.WasReleasedThisFrame(), Is.False);

            if (action.type != InputActionType.PassThrough)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }

            trace.Clear();

            // Press-and-release-and-press-again in same frame.
            Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
            Set(gamepad.leftTrigger, 0.25f, queueEventOnly: true);
            Set(gamepad.leftTrigger, 0.75f);

            // W = Waiting, S = Started, P = Performed, C = Canceled
            // (* means listeners not invoked)
            // | Type   | Interaction | Phase Change             | P/C | isButtonLike | isHold |
            // |--------|-------------|--------------------------|-----|--------------|--------|
            // | Value  | Default     | S -> P, S*, P, S*, P, S* | T/F |              |        |
            // | Value  | Press       | S -> P, S, P             | T/T | true         |        |
            // | Value  | Hold        | S    (No Change)         | F/F |              | true   |
            // | Button | Default     | S -> P, S, P             | T/T | true         |        |
            // | Button | Press       | S -> P, S, P             | T/T | true         |        |
            // | Button | Hold        | S    (No Change)         | F/F |              | true   |
            // | Pass   | Default     | P -> P, P, P             | T/F |              |        |

            Assert.That(action.IsPressed(), Is.True);
            Assert.That(action.WasPressedThisFrame(), Is.True);
            Assert.That(action.WasReleasedThisFrame(), Is.True);

            if (action.type == InputActionType.Value && interactions == null)
            {
                Assert.That(trace, Performed(action).AndThen(Performed(action)).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (isButtonLike)
            {
                Assert.That(trace, Performed(action).AndThen(Started(action)).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.True);
            }
            else if (isHold)
            {
                Assert.That(trace, Is.Empty);
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
                Assert.That(action.WasPerformedThisFrame(), Is.False);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
            else if (action.type == InputActionType.PassThrough)
            {
                Assert.That(trace, Performed(action).AndThen(Performed(action)).AndThen(Performed(action)));
                Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
                Assert.That(action.WasPerformedThisFrame(), Is.True);
                Assert.That(action.WasCompletedThisFrame(), Is.False);
            }
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanReadValueFromAction_InCallback()
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

    [Test]
    [Category("Actions")]
    public void Actions_CanReadValueFromAction_InCallback_AsButton()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var action = new InputAction(binding: "<Gamepad>/leftTrigger");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        action.Enable();

        bool? receivedValue = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedValue, Is.Null);
            receivedValue = ctx.ReadValueAsButton();
        };

        Set(gamepad.leftTrigger, 0.25f);

        Assert.That(receivedValue, Is.False);

        receivedValue = null;

        Set(gamepad.leftTrigger, 0.75f);

        Assert.That(receivedValue, Is.True);

        receivedValue = null;

        Set(gamepad.leftTrigger, 0.15f);

        Assert.That(receivedValue, Is.False);
    }

    // Some code needs to be able to just generically transfer values from A to B. For this, the
    // generic ReadValue<TValue>() API isn't sufficient.
    [Test]
    [Category("Actions")]
    public unsafe void Actions_CanReadValueFromAction_InCallback_WithoutKnowingValueType()
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
    public void Actions_CanReadValueTypeFromAction()
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

        action.performed +=
            ctx =>
        {
            Assert.That(ctx.valueType, Is.EqualTo(typeof(Vector2)));
            Assert.That(ctx.action.activeValueType, Is.EqualTo(typeof(Vector2)));
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.123f, 0.234f) });
        InputSystem.Update();

        Assert.That(action.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action.activeControl.valueType, Is.EqualTo(typeof(Vector2)));
        Assert.That(action.activeValueType, Is.EqualTo(typeof(Vector2)));

        Assert.That(action.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                .Using(Vector2EqualityComparer.Instance));

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        // The active control is one of the two keyboard keys held, which has a value type of float.
        // But since the composite has type Vector2, the action's value type is Vector2.
        Assert.That(new[] { keyboard.wKey, keyboard.aKey }, Contains.Item(action.activeControl));
        Assert.That(action.activeControl.valueType, Is.EqualTo(typeof(float)));
        Assert.That(action.activeValueType, Is.EqualTo(typeof(Vector2)));

        Assert.That(action.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process((Vector2.up + Vector2.left).normalized))
                .Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanReadValueTypeFromAction_WithDynamicCompositeType()
    {
        var action = new InputAction();
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Gamepad>/leftTrigger")
            .With("Binding", "<Gamepad>/leftStick");

        var gamepad = InputSystem.AddDevice<Gamepad>();

        action.Enable();

        action.performed +=
            ctx =>
        {
            Assert.That(ctx.valueType, Is.EqualTo(typeof(Vector2)));
            Assert.That(ctx.action.activeValueType, Is.EqualTo(typeof(Vector2)));
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.123f, 0.234f) });
        InputSystem.Update();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.activeValueType, Is.Null);

        Assert.That(action.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(Vector2.zero))
                .Using(Vector2EqualityComparer.Instance));

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.123f, 0.234f), leftTrigger = 1f });
        InputSystem.Update();

        // The active control is the most recent change (left trigger), which has a value type of float.
        // But since the composite has evaluated type Vector2, the action's value type is Vector2.
        Assert.That(action.activeControl, Is.SameAs(gamepad.leftTrigger));
        Assert.That(action.activeControl.valueType, Is.EqualTo(typeof(float)));
        Assert.That(action.activeValueType, Is.EqualTo(typeof(Vector2)));

        Assert.That(action.ReadValue<Vector2>(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                .Using(Vector2EqualityComparer.Instance));
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
    public void Actions_CanQueryActiveControl()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Button);
        action.AddBinding(gamepad.buttonSouth);
        action.AddBinding(gamepad.buttonNorth);
        action.Enable();

        Assert.That(action.activeControl, Is.Null);

        Press(gamepad.buttonSouth);

        Assert.That(action.activeControl, Is.SameAs(gamepad.buttonSouth));

        Release(gamepad.buttonSouth);

        Assert.That(action.activeControl, Is.Null);

        Press(gamepad.buttonNorth);

        Assert.That(action.activeControl, Is.SameAs(gamepad.buttonNorth));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryActiveValueType()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Button);
        action.AddBinding(gamepad.buttonSouth);
        action.AddBinding(gamepad.buttonNorth);
        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.activeValueType, Is.Null);

        Press(gamepad.buttonSouth);

        Assert.That(action.activeControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action.activeValueType, Is.EqualTo(gamepad.buttonSouth.valueType));

        Release(gamepad.buttonSouth);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.activeValueType, Is.Null);

        Press(gamepad.buttonNorth);

        Assert.That(action.activeControl, Is.SameAs(gamepad.buttonNorth));
        Assert.That(action.activeValueType, Is.EqualTo(gamepad.buttonNorth.valueType));
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Value, "Scale(factor=2)")]
    [TestCase(InputActionType.Button)]
    [TestCase(InputActionType.Button, "Scale(factor=2)")]
    public void Actions_CanQueryMagnitudeFromAction_WithAxisControl(InputActionType actionType, string processors = null)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: actionType, binding: "<Gamepad>/leftTrigger", processors: processors);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        Set(gamepad.leftTrigger, 0.123f);

        const float factor = 2f;
        var expectedValue = processors == null ? 0.123f : 0.123f * factor;

        Assert.That(action.activeControl, Is.SameAs(gamepad.leftTrigger));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(expectedValue).Within(0.00001));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0.123f).Within(0.00001));

        Set(gamepad.leftTrigger, 0f);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryMagnitudeFromAction_WithStickControl()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftStick");

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

        var expectedValue = new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f));

        Assert.That(action.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(expectedValue).Using(Vector2EqualityComparer.Instance));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(expectedValue.magnitude));

        Set(gamepad.leftStick, Vector2.zero);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Value, "Scale(factor=2)")]
    public void Actions_CanQueryMagnitudeFromAction_WithCompositeAxisControl(InputActionType actionType, string processors = null)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Adding the Scale processor to the composite binding, not the action, to make sure
        // the scaling is only applied once instead of scaling each part binding in addition to scaling the output
        // of the Axis composite.
        var action = new InputAction(type: actionType);
        action.AddCompositeBinding($"Axis(minValue=-5,maxValue=5)", processors: processors)
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        Press(keyboard.dKey);

        const float factor = 2f;
        var expectedValue = processors == null ? 5f : 5f * factor;

        Assert.That(action.activeControl, Is.SameAs(keyboard.dKey));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(expectedValue).Within(0.00001));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(1f).Within(0.00001));

        Release(keyboard.dKey);
        Press(keyboard.aKey);

        Assert.That(action.activeControl, Is.SameAs(keyboard.aKey));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(-expectedValue).Within(0.00001));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(1f).Within(0.00001));

        Release(keyboard.aKey);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0f));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));
    }

    [Test]
    [Category("Actions")]
    [TestCase(InputActionType.Value)]
    [TestCase(InputActionType.Value, "ScaleVector2(x=2,y=2)")]
    public void Actions_CanQueryMagnitudeFromAction_WithComposite2DVectorControl(InputActionType actionType, string processors = null)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Adding the Scale processor to the composite binding, not the action, to make sure
        // the scaling is only applied once instead of scaling each part binding in addition to scaling the output
        // of the Axis composite.
        var action = new InputAction(type: actionType);
        action.AddCompositeBinding("2DVector(mode=1)", processors: processors) // Mode.Digital
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        Press(keyboard.sKey);

        const float factor = 2f;
        var expectedValue = processors == null ? Vector2.down : Vector2.down * factor;

        Assert.That(action.activeControl, Is.SameAs(keyboard.sKey));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(expectedValue).Using(Vector2EqualityComparer.Instance));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(1f).Within(0.00001));

        Press(keyboard.dKey);

        expectedValue = processors == null ? new Vector2(1f, -1f) : new Vector2(1f, -1f) * factor;

        Assert.That(action.activeControl, Is.SameAs(keyboard.dKey));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(expectedValue).Using(Vector2EqualityComparer.Instance));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(new Vector2(1f, -1f).magnitude).Within(0.00001));

        Release(keyboard.dKey);
        Release(keyboard.sKey);

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryMagnitudeFromAction_WithQuaternionControl_ReturnsInvalidMagnitude()
    {
        var sensor = InputSystem.AddDevice<AttitudeSensor>();

        var action = new InputAction(binding: "<AttitudeSensor>/attitude");

        // Verify that the default value is not Quaternion.identity but instead a zero quaternion.
        // When the control changes to the default value, the phase changes back to Waiting,
        // and the magnitude is explicitly cleared.
        Assert.That(sensor.attitude.ReadDefaultValue(), Is.EqualTo(default(Quaternion)));

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Disabled));
        Assert.That(action.ReadValue<Quaternion>(), Is.EqualTo(default(Quaternion)));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        action.Enable();

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Quaternion>(), Is.EqualTo(default(Quaternion)));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));

        Set(sensor.attitude, Quaternion.Euler(30f, 60f, 45f));

        Assert.That(action.activeControl, Is.SameAs(sensor.attitude));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Quaternion>(), Is.EqualTo(Quaternion.Euler(30f, 60f, 45f)).Using(QuaternionEqualityComparer.Instance));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(-1f));

        Set(sensor.attitude, Quaternion.identity);

        Assert.That(action.activeControl, Is.SameAs(sensor.attitude));
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Quaternion>(), Is.EqualTo(Quaternion.identity));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(-1f));

        Set(sensor.attitude, default(Quaternion));

        Assert.That(action.activeControl, Is.Null);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(action.ReadValue<Quaternion>(), Is.EqualTo(default(Quaternion)));
        Assert.That(action.GetControlMagnitude(), Is.EqualTo(0f));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ResettingDevice_CancelsOngoingActionsThatAreDrivenByIt()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Create an action that performs on button *up*. This way we can tell whether
        // the action is truly cancelled or whether it simply gets triggered by us
        // resetting the corresponding device state.
        var buttonReleaseAction = new InputAction(name: "button", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth",
            interactions: "press(behavior=1)");
        buttonReleaseAction.Enable();

        var valueAction = new InputAction(name: "value", type: InputActionType.Value, binding: "<Gamepad>/buttonSouth");
        valueAction.Enable();

        var passThroughAction = new InputAction(name: "passthrough", type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");
        passThroughAction.Enable();

        Press(gamepad.buttonSouth);

        Assert.That(buttonReleaseAction.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(buttonReleaseAction.activeControl, Is.SameAs(gamepad.buttonSouth));

        Assert.That(valueAction.phase, Is.EqualTo(InputActionPhase.Started)); // Goes back to Started after Performed.
        Assert.That(valueAction.activeControl, Is.SameAs(gamepad.buttonSouth));

        Assert.That(passThroughAction.phase, Is.EqualTo(InputActionPhase.Performed));
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.buttonSouth));

        using (var buttonReleaseActionTrace = new InputActionTrace(buttonReleaseAction))
        using (var valueActionTrace = new InputActionTrace(valueAction))
        using (var passThroughActionTrace = new InputActionTrace(passThroughAction))
        {
            InputSystem.ResetDevice(gamepad);

            Assert.That(buttonReleaseActionTrace, Canceled(buttonReleaseAction));
            Assert.That(valueActionTrace, Canceled(valueAction));

            // This case is quirky. For button and value actions, the reset of the control value
            // does not cause the action to start back up. For pass-through actions, that is different
            // as *any* value change performs the action. So here, we see *both* a cancellation and then
            // immediately a performing of the action.
            Assert.That(passThroughActionTrace, Canceled(passThroughAction).AndThen(Performed(passThroughAction, value: 0f)));
        }
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
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");
        var map3 = new InputActionMap("map3");
        var map4 = new InputActionMap("map4");

        var action1 = map1.AddAction("action1", InputActionType.Value);
        var action2 = map1.AddAction("action2", InputActionType.Button);
        var action3 = map2.AddAction("action3", InputActionType.Button);
        var action4 = map3.AddAction("action4", InputActionType.Value);
        var action5 = map3.AddAction("action5", InputActionType.Button);
        var action6 = map3.AddAction("action6", InputActionType.Value);
        var action7 = map4.AddAction("action7", InputActionType.Value);

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

        var startTime = currentTime;
        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            // Enable only map1.
            map1.Enable();

            // Creating gamepad after maps are enabled to test trace catching binding resolve. Case ISXB-29.
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f), startTime + 0.123);

            // map1/action1 should have been started and performed.
            Assert.That(trace,
                Started(action1, value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(-1, 1),
                    control: gamepad.leftStick, time: startTime + 0.123)
                    .AndThen(Performed(action1,
                    value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(-1, 1),
                    control: gamepad.leftStick, time: startTime + 0.123)));

            trace.Clear();

            currentTime = startTime + 0.234f;

            // Disable map1 and enable map2+map3.
            map1.Disable();
            map2.Enable();
            map3.Enable();

            Press(gamepad.buttonSouth, startTime + 0.345f);
            Set(gamepad.leftStick, new Vector2(0.234f, 0.345f), startTime + 0.456f);

            Assert.That(trace,
                // map1/action1 should have been canceled.
                Canceled(action1, value: default(Vector2), control: gamepad.leftStick, time: startTime + 0.234)
                // map3/action4 should immediately start as the stick was already actuated
                // when we enabled the action.
                // NOTE: We get a different value here than what action1 got as we have a different
                //       processor on the binding.
                    .AndThen(Started(action4,
                    value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(1, -1),
                    control: gamepad.leftStick, time: startTime + 0.234))
                    .AndThen(Performed(action4,
                        value: new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)) * new Vector2(1, -1),
                        control: gamepad.leftStick, time: startTime + 0.234))
                    // map2/action3 should have been started.
                    .AndThen(Started<TapInteraction>(action3, value: 1f, control: gamepad.buttonSouth, time: startTime + 0.345))
                    // map3/action5 should have been started.
                    .AndThen(Started<TapInteraction>(action5, value: 1f, control: gamepad.buttonSouth, time: startTime + 0.345))
                    // map3/action4 should have been performed as the stick has been moved
                    // beyond where it had already moved.
                    .AndThen(Performed(action4,
                        value: new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)) * new Vector2(1, -1),
                        control: gamepad.leftStick, time: startTime + 0.456)));

            trace.Clear();

            // Disable all maps.
            map2.Disable();
            map3.Disable();

            Assert.That(trace,
                // map2/action3 should have been canceled.
                Canceled<TapInteraction>(action3, value: 0f, control: gamepad.buttonSouth, time: currentTime)
                // map3/action4 should have been canceled.
                    .AndThen(Canceled(action4, value: default(Vector2), control: gamepad.leftStick,
                    time: currentTime))
                    // map3/action5 should have been canceled.
                    .AndThen(Canceled<TapInteraction>(action5, value: 0f, control: gamepad.buttonSouth, time: currentTime)));

            trace.Clear();

            Set(gamepad.buttonSouth, 0, startTime + 1.23);
            Set(gamepad.buttonSouth, 0, startTime + 1.34);

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
    public void Actions_CanByPassControlActuationChecks_UsingPasshtroughAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.PassThrough, binding: "<Gamepad>/*stick");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

            Assert.That(trace,
                Performed(action, gamepad.leftStick, new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f))));

            trace.Clear();

            Set(gamepad.leftStick, new Vector2(0.234f, 0.345f));

            Assert.That(trace,
                Performed(action, gamepad.leftStick, new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f))));

            trace.Clear();

            Set(gamepad.rightStick, new Vector2(0.123f, 0.234f));

            Assert.That(trace,
                Performed(action, gamepad.rightStick, new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f))));

            trace.Clear();

            Set(gamepad.rightStick, Vector2.zero);

            Assert.That(trace,
                Performed(action, gamepad.rightStick, Vector2.zero));
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
    public void Actions_WithMultipleBoundControls_DriveInteractionsFromControlWithGreatestActuation()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

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

            Assert.That(trace, Started(stickAction, control: gamepad.leftStick,
                value: new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f)))
                    .AndThen(Performed(stickAction, control: gamepad.leftStick,
                    value: new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f)))));

            trace.Clear();

            // Next move the right stick out of its deadzone but below the value we moved the left stick
            // to. Make sure the action is UNAFFECTED.
            Set(gamepad.rightStick, new Vector2(0.234f, 0.345f));

            Assert.That(trace, Is.Empty);

            // Now actuate the right stick more than the left stick. We should see the right stick
            // taking over stickAction.
            Set(gamepad.rightStick, new Vector2(0.456f, 0.567f));

            Assert.That(trace, Performed(stickAction, control: gamepad.rightStick,
                value: new StickDeadzoneProcessor().Process(new Vector2(0.456f, 0.567f))));

            trace.Clear();

            // Reset the left stick. stickAction should be UNAFFECTED.
            Set(gamepad.leftStick, Vector2.zero);

            Assert.That(trace, Is.Empty);

            // Finally, reset the right stick. stickAction should be canceled.
            Set(gamepad.rightStick, Vector2.zero);

            Assert.That(trace, Canceled(stickAction, control: gamepad.rightStick, value: Vector2.zero));

            trace.Clear();

            // Actuate the left trigger and then dpad/right. This should result in compositeAction being
            // started from the trigger and then be UNAFFECTED by dpad/right (the value of the composite
            // points the opposite way but the magnitude is identical).
            Set(gamepad.leftTrigger, 1.0f);
            Press(gamepad.dpad.right);

            Assert.That(trace,
                Started(compositeAction, control: gamepad.leftTrigger, value: -1f)
                    .AndThen(Performed(compositeAction, control: gamepad.leftTrigger, value: -1f)));

            trace.Clear();

            // Now release the left trigger. dpad/right should take over and keep the action going.
            Set(gamepad.leftTrigger, 0);

            Assert.That(trace, Performed(compositeAction, control: gamepad.dpad.left, value: 1f));

            trace.Clear();

            // Finally, release dpad/right, too. The action should cancel.
            Release(gamepad.dpad.right);

            Assert.That(trace, Canceled(compositeAction, control: gamepad.dpad.right, value: 0f));

            trace.Clear();

            // Press both shoulder buttons and then release one of them. This should leave
            // the hold still going.
            Press(gamepad.leftShoulder);
            Assert.That(holdAction.activeControl, Is.SameAs(gamepad.leftShoulder));
            Press(gamepad.rightShoulder);
            Assert.That(holdAction.activeControl, Is.SameAs(gamepad.leftShoulder));
            Release(gamepad.leftShoulder);
            Assert.That(holdAction.activeControl, Is.SameAs(gamepad.rightShoulder));

            Assert.That(trace, Started(holdAction, control: gamepad.leftShoulder, value: 1f));

            trace.Clear();

            // Now release the other shoulder button such that it performs a hold. This
            // should complete the action.
            runtime.currentTime += 10;
            Release(gamepad.rightShoulder);

            Assert.That(trace,
                Performed(holdAction, control: gamepad.rightShoulder, value: 0f)
                    .AndThen(Canceled(holdAction, gamepad.rightShoulder, 0f)));

            trace.Clear();

            // Press all face buttons and then release them one by one. After the last was released,
            // buttonAction should be canceled.
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
            //
            // However, it also turns out that our stripping code may not keep the order of controls when rewriting the
            // updated assemblies, which makes this test indeterministic in players. So we test for the specific callbacks
            // only in the editor. In players, we just make sure that the first and last callbacks match our expectations.

            var actions = trace.ToArray();

            #if UNITY_EDITOR
            Assert.That(actions, Has.Length.EqualTo(5));
            #endif

            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[0].action, Is.SameAs(buttonAction));
            Assert.That(actions[0].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad.buttonSouth));
            Assert.That(actions[1].action, Is.SameAs(buttonAction));
            Assert.That(actions[1].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));

            #if UNITY_EDITOR
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[2].control, Is.SameAs(gamepad.buttonWest)); // Control immediately following buttonSouth in list of controls.
            Assert.That(actions[2].action, Is.SameAs(buttonAction));
            Assert.That(actions[2].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            #endif

            Assert.That(actions[actions.Length - 2].phase, Is.EqualTo(InputActionPhase.Performed)); // Last control to be actuated.
            Assert.That(actions[actions.Length - 2].control, Is.SameAs(gamepad.buttonNorth));
            Assert.That(actions[actions.Length - 2].action, Is.SameAs(buttonAction));
            Assert.That(actions[actions.Length - 2].ReadValue<float>(), Is.EqualTo(1).Within(0.00001));
            Assert.That(actions[actions.Length - 1].control, Is.SameAs(gamepad.buttonNorth));
            Assert.That(actions[actions.Length - 1].action, Is.SameAs(buttonAction));
            Assert.That(actions[actions.Length - 1].ReadValue<float>(), Is.Zero.Within(0.00001));
        }
    }

    private class ReleaseOnlyTestInteraction : IInputInteraction<float>
    {
        private bool m_WaitingForRelease;
        public void Process(ref InputInteractionContext context)
        {
            var actuated = context.ControlIsActuated();
            if (!actuated && m_WaitingForRelease)
                context.Performed();
            else if (actuated)
                m_WaitingForRelease = true;
        }

        public void Reset()
        {
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1364667/
    [Test]
    [Category("Actions")]
    public void Actions_WithMultipleBoundControls_ProcessesInteractionsOnAllActiveBindings()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddBinding("<Keyboard>/a", interactions: "press(behavior=1)");
        action.AddBinding("<Keyboard>/s", interactions: "press(behavior=1)");
        action.Enable();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(!action.WasPerformedThisFrame());

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
        InputSystem.Update();

        Assert.That(!action.WasPerformedThisFrame());

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
        InputSystem.Update();

        Assert.That(!action.WasPerformedThisFrame());

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        Assert.That(action.WasPerformedThisFrame());
    }

    // https://fogbugz.unity3d.com/f/cases/1309797/
    [Test]
    [Category("Actions")]
    public void Actions_WithMultipleBoundControls_ProcessesInteractionsOnAllActiveBindings_AcrossDevices()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var pressAction = new InputAction();
        pressAction.AddBinding("<Keyboard>/space", interactions: "press(behavior=0)");
        pressAction.AddBinding("<Mouse>/leftButton", interactions: "press(behavior=0)");
        pressAction.Enable();

        var releaseAction = new InputAction();
        releaseAction.AddBinding("<Keyboard>/space", interactions: "press(behavior=1)");
        releaseAction.AddBinding("<Mouse>/leftButton", interactions: "press(behavior=1)");
        releaseAction.Enable();

        Press(mouse.leftButton);

        Assert.That(pressAction.WasPerformedThisFrame(), Is.True);
        Assert.That(releaseAction.WasPerformedThisFrame(), Is.False);
        Assert.That(pressAction.activeControl, Is.SameAs(mouse.leftButton));
        Assert.That(releaseAction.activeControl, Is.SameAs(mouse.leftButton)); // Was still started.

        Press(keyboard.spaceKey);

        Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
        Assert.That(releaseAction.WasPerformedThisFrame(), Is.False);
        Assert.That(pressAction.activeControl, Is.SameAs(mouse.leftButton));
        Assert.That(releaseAction.activeControl, Is.SameAs(mouse.leftButton));

        Release(mouse.leftButton);

        Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
        Assert.That(releaseAction.WasPerformedThisFrame(), Is.False); // !!
        Assert.That(pressAction.activeControl, Is.SameAs(keyboard.spaceKey));
        Assert.That(releaseAction.activeControl, Is.SameAs(keyboard.spaceKey));

        Release(keyboard.spaceKey);

        Assert.That(pressAction.WasPerformedThisFrame(), Is.False);
        Assert.That(releaseAction.WasPerformedThisFrame(), Is.True);
        Assert.That(pressAction.activeControl, Is.Null);
        Assert.That(releaseAction.activeControl, Is.Null);

        Press(mouse.leftButton);

        Assert.That(pressAction.WasPerformedThisFrame(), Is.True);
        Assert.That(releaseAction.WasPerformedThisFrame(), Is.False);
        Assert.That(pressAction.activeControl, Is.SameAs(mouse.leftButton));
        Assert.That(releaseAction.activeControl, Is.SameAs(mouse.leftButton));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WithMultipleBoundControls_CanHandleInteractionsThatTriggerOnlyOnButtonRelease()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterInteraction<ReleaseOnlyTestInteraction>();

        var action = new InputAction(interactions: "releaseOnlyTest");

        action.AddBinding("<Keyboard>/a");
        action.AddBinding("<Gamepad>/buttonNorth");

        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            Press(gamepad.buttonNorth);

            Assert.That(trace, Is.Empty);

            Release(gamepad.buttonNorth);

            Assert.That(trace, Started(action, gamepad.buttonNorth).AndThen(Performed(action, gamepad.buttonNorth)).AndThen(Canceled(action, gamepad.buttonNorth)));

            trace.Clear();

            // Test same button again with full press and release cycle to make sure
            // there's no state that's gotten stuck on the button.
            Press(gamepad.buttonNorth);

            Assert.That(trace, Is.Empty);

            Release(gamepad.buttonNorth);

            Assert.That(trace, Started(action, gamepad.buttonNorth).AndThen(Performed(action, gamepad.buttonNorth)).AndThen(Canceled(action, gamepad.buttonNorth)));

            trace.Clear();

            // Make sure that using the other binding is unaffected by the previous use of buttonSouth.
            Press(keyboard.aKey);

            Assert.That(trace, Is.Empty);

            Release(keyboard.aKey);

            Assert.That(trace, Started(action, keyboard.aKey).AndThen(Performed(action, keyboard.aKey)).AndThen(Canceled(action, keyboard.aKey)));

            trace.Clear();

            // Make sure that if we press both buttonSouth *and* the key and then release them both,
            // we get two releases on the action.
            //
            // NOTE: This differs very slightly from what one might expect from the built-in disambiguation
            //       code that basically dictates that the control with the largest actuation is being tracked
            //       and "locked on". However, PressInteraction never *starts* an interaction on a button press
            //       when it is set to "ReleaseOnly". This means that from the perspective of the disambiguation
            //       code, there is nothing in progress when the second button goes down.
            Press(keyboard.aKey);
            Press(gamepad.buttonNorth);

            Assert.That(trace, Is.Empty);

            Release(keyboard.aKey);

            Assert.That(trace, Started(action, keyboard.aKey).AndThen(Performed(action, keyboard.aKey)).AndThen(Canceled(action, keyboard.aKey)));

            trace.Clear();

            Release(gamepad.buttonNorth);

            Assert.That(trace, Started(action, gamepad.buttonNorth).AndThen(Performed(action, gamepad.buttonNorth)).AndThen(Canceled(action, gamepad.buttonNorth)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_WithMultipleBoundControls_CanHandleButtonPressesAndReleases()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/*trigger");
        action.Enable();

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);
        Assert.That(action.WasCompletedThisFrame(), Is.False);
        Assert.That(action.activeControl, Is.Null);

        Set(gamepad.leftTrigger, 1f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.True);
        Assert.That(action.WasPerformedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.False);
        Assert.That(action.WasCompletedThisFrame(), Is.False);
        Assert.That(action.activeControl, Is.SameAs(gamepad.leftTrigger));

        Set(gamepad.rightTrigger, 0.6f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.False);
        Assert.That(action.WasCompletedThisFrame(), Is.False);
        Assert.That(action.activeControl, Is.SameAs(gamepad.leftTrigger));

        Set(gamepad.leftTrigger, 0f);

        Assert.That(action.IsPressed(), Is.True);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasPerformedThisFrame(), Is.True);
        Assert.That(action.WasReleasedThisFrame(), Is.False);
        Assert.That(action.WasCompletedThisFrame(), Is.False);
        Assert.That(action.activeControl, Is.SameAs(gamepad.rightTrigger));

        Set(gamepad.rightTrigger, 0f);

        Assert.That(action.IsPressed(), Is.False);
        Assert.That(action.WasPressedThisFrame(), Is.False);
        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Assert.That(action.WasReleasedThisFrame(), Is.True);
        Assert.That(action.WasCompletedThisFrame(), Is.False);
        Assert.That(action.activeControl, Is.Null);
    }

    // https://fogbugz.unity3d.com/f/cases/1389858/
    [Test]
    [Category("Actions")]
    public void Actions_WithMultipleBoundControls_ValueChangesOfEqualMagnitudeAreNotIgnored()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/dpad");

        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.dpad, Vector2.left);

            Assert.That(trace, Started(action, value: Vector2.left, control: gamepad.dpad)
                .AndThen(Performed(action, value: Vector2.left, control: gamepad.dpad)));

            trace.Clear();

            Set(gamepad.dpad, Vector2.right);

            Assert.That(trace, Performed(action, value: Vector2.right, control: gamepad.dpad));
        }
    }

    // There can be situations where two different controls are driven from the same state. Most prominently, this is
    // the case with the Pointer.button control that subclasses usually rewrite to whatever their primary button is.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_WithMultipleActuationsFromSameState_()
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
        var action = map.AddAction("action", binding: "/<Gamepad>/leftTrigger");

        var wasStarted = false;
        var wasPerformed = false;
        var wasCanceled = false;

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
                    Assert.That(wasCanceled, Is.False);
                    wasStarted = true;
                    break;
                case InputActionPhase.Performed:
                    Assert.That(wasStarted, Is.True);
                    Assert.That(wasPerformed, Is.False);
                    Assert.That(wasCanceled, Is.False);
                    wasPerformed = true;
                    break;
                case InputActionPhase.Canceled:
                    Assert.That(wasStarted, Is.True);
                    Assert.That(wasPerformed, Is.True);
                    Assert.That(wasCanceled, Is.False);
                    wasCanceled = true;
                    break;
            }
        };

        map.Enable();

        Set(gamepad.leftTrigger, 0.5f);

        Assert.That(wasStarted, Is.True);
        Assert.That(wasPerformed, Is.True);
        Assert.That(wasCanceled, Is.False);

        Set(gamepad.leftTrigger, 0);

        Assert.That(wasCanceled, Is.True);
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
        var receivedCanceled = false;

        action.started += ctx => receivedStarted = true;
        action.performed += ctx => receivedPerformed = true;
        action.canceled += ctx => receivedCanceled = true;

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
                case InputActionPhase.Canceled:
                    Assert.That(receivedCanceled, Is.False);
                    break;
                case InputActionPhase.Performed:
                    Assert.That(receivedPerformed, Is.False);
                    break;
            }

            receivedChanges.Add(c);
        };

        Set(gamepad.leftTrigger, 0.5f);

        Assert.That(receivedChanges,
            Is.EquivalentTo(new[] {InputActionChange.ActionStarted, InputActionChange.ActionPerformed}));

        receivedChanges.Clear();
        receivedStarted = false;
        receivedPerformed = false;
        receivedCanceled = false;

        Set(gamepad.leftTrigger, 0);

        Assert.That(receivedChanges,
            Is.EquivalentTo(new[] {InputActionChange.ActionCanceled}));
    }

    [Test]
    [Category("Actions")]
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
            Assert.That(trace.buffer.capacityInBytes, Is.Zero);

            action.performed += trace.RecordAction;

            var state = new GamepadState {leftStick = new Vector2(0.123f, 0.234f)};
            InputSystem.QueueStateEvent(gamepad, state, 0.1234);
            state.rightStick = new Vector2(0.345f, 0.456f);
            InputSystem.QueueStateEvent(gamepad, state, 0.2345);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W), 0.0987);
            InputSystem.Update();

            Assert.That(trace.buffer.capacityInBytes, Is.EqualTo(2048)); // Default capacity increment.
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
        action.AddBinding("<Mouse>/leftButton").WithGroup("C");

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

            Assert.That(action.controls, Is.EquivalentTo(new[] {mouse.leftButton}));

            Assert.That(trace,
                Started(action, gamepad.buttonSouth)
                    .AndThen(Performed(action, gamepad.buttonSouth))
                    .AndThen(Canceled(action, gamepad.buttonSouth))
                    // The second start-perform-cancel cycle comes from the fact that we are changing the
                    // binding mask. Doing so will cancel all ongoing actions. But because the gamepad button
                    // is still pressed and still bound after the binding mask change, the next update will
                    // restart the action from the gamepad button.
                    .AndThen(Started(action, gamepad.buttonSouth))
                    .AndThen(Performed(action, gamepad.buttonSouth))
                    .AndThen(Canceled(action, gamepad.buttonSouth))
                    .AndThen(Started(action, keyboard.aKey))
                    .AndThen(Performed(action, keyboard.aKey))
                    .AndThen(Canceled(action, keyboard.aKey)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordActions_AndReadValueAsObject()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction(binding: "<Gamepad>/leftTrigger");
        var action2 = new InputAction();
        action2.AddCompositeBinding("Axis")
            .With("Negative", "<Gamepad>/leftTrigger")
            .With("Positive", "<Gamepad>/rightTrigger");

        action1.Enable();
        action2.Enable();

        using (var trace = new InputActionTrace())
        {
            action1.performed += trace.RecordAction;
            action2.performed += trace.RecordAction;

            Set(gamepad.leftTrigger, 0.123f);

            // Disable actions and alter trigger value to make sure
            // we're not picking up values from the control.
            action1.Disable();
            action2.Disable();
            Set(gamepad.leftTrigger, 0.234f);

            Assert.That(trace, Performed(action1, value: 0.123f).AndThen(Performed(action2, value: -0.123f)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRecordAllActionsInTheSystem()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

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
        Assert.That(map.FindAction("action1"), Is.Null);

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

    [Test]
    [Category("Actions")]
    public void Actions_RenamingActionUpdatesBindings()
    {
        var map = new InputActionMap();
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        action1.AddBinding("<Gamepad>/buttonSouth");
        action2.AddBinding("<Gamepad>/buttonNorth");
        action1.AddBinding("<Keyboard>/space");

        action1.Rename("newName");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(action1.bindings[1].path, Is.EqualTo("<Keyboard>/space"));
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

        Assert.That(map.FindAction("action1"), Is.SameAs(action1));
        Assert.That(map.FindAction("action2"), Is.SameAs(action2));
        Assert.That(map.FindAction("action3"), Is.Null);

        // Lookup is case-insensitive.
        Assert.That(map.FindAction("Action1"), Is.SameAs(action1));
        Assert.That(map.FindAction("Action2"), Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionsInMapById()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        Assert.That(map.FindAction(action1.id), Is.SameAs(action1));
        Assert.That(map.FindAction(action2.id), Is.SameAs(action2));
        Assert.That(map.FindAction(Guid.NewGuid()), Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionsInMapByStringId()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        Assert.That(map.FindAction(action1.id.ToString()), Is.SameAs(action1));
        Assert.That(map.FindAction(action2.id.ToString()), Is.SameAs(action2));
        Assert.That(map.FindAction(Guid.NewGuid().ToString()), Is.Null);
    }

    // We used to require string GUIDs to be using a "{...}" format when looking up actions. We no
    // longer do this but there's still data that may be using this format so make sure it works for now.
    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionsInMapByStringId_UsingOldBracedFormat()
    {
        var map = new InputActionMap();

        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");

        Assert.That(map.FindAction($"{{{action1.id.ToString()}}}"), Is.SameAs(action1));
        Assert.That(map.FindAction($"{{{action2.id.ToString()}}}"), Is.SameAs(action2));
        Assert.That(map.FindAction($"{{{Guid.NewGuid().ToString()}}}"), Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertActionMapToAndFromJson()
    {
        //By default, serialize as if there's no base map
        //Solve baseMap correlation on the InputActionAsset level
        //Give action maps stable internal names (just like actions)
        var map = new InputActionMap("test");

        var action1 = map.AddAction(name: "action1", expectedControlLayout: "Button", binding: "/gamepad/leftStick");
        action1
            .AddBinding("/gamepad/rightStick")
            .WithGroup("group")
            .WithProcessor("deadzone");
        map.AddAction(name: "action2", binding: "/gamepad/buttonSouth", interactions: "tap,slowTap(duration=0.1)");

        // Add binding with an empty path and make sure we persist that correctly.
        // https://fogbugz.unity3d.com/f/cases/1231968/
        action1.AddBinding("");

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
        Assert.That(maps[0].actions[0].expectedControlType, Is.EqualTo("Button"));
        Assert.That(maps[0].actions[1].expectedControlType, Is.Null);
        Assert.That(maps[0].actions[0].bindings, Has.Count.EqualTo(3));
        Assert.That(maps[0].actions[1].bindings, Has.Count.EqualTo(1));
        Assert.That(maps[0].actions[0].bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(maps[0].actions[0].bindings[1].path, Is.EqualTo("/gamepad/rightStick"));
        Assert.That(maps[0].actions[0].bindings[2].path, Is.Not.Null);
        Assert.That(maps[0].actions[0].bindings[2].path, Is.Empty);
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
                                ""interactions"" : ""tap""
                            }
                        ]
                    },
                    {
                        ""name"" : ""map2/action1"",
                        ""bindings"" : [
                            {
                                ""path"" : ""<Gamepad>/buttonSouth"",
                                ""interactions"" : ""slowTap""
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
        asset.AddControlScheme("scheme2").WithBindingGroup("group2")
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

    static string MinimalJson(string name = null)
    {
        if (name != null)
            return "{\n    \"name\": \"" + name + "\",\n    \"maps\": [],\n    \"controlSchemes\": []\n}";
        return "{\n    \"maps\": [],\n    \"controlSchemes\": []\n}";
    }

    [Test]
    [Category("Actions")]
    public void Actions_NameIsSetToNameFromJson_IfCreatedFromJsonWithName()
    {
        // When constructed from JSON containing name, ScriptableObject.name is set based on name property
        const string name = "My Actions";
        var json = MinimalJson(name);
        var asset = InputActionAsset.FromJson(json);
        var assetName = asset.name;
        ScriptableObject.Destroy(asset);
        Assert.That(asset.name, Is.EqualTo(name));
    }

    [Test]
    [Category("Actions")]
    public void Actions_NameIsSetToEmptyString_IfCreatedFromJsonWithoutName()
    {
        // When constructed from JSON without a name, ScriptableObject.name is set to empty string.
        var json = MinimalJson();
        var asset = InputActionAsset.FromJson(json);
        var name = asset.name;
        ScriptableObject.Destroy(asset);
        Assert.That(asset.name, Is.EqualTo(string.Empty));
    }

    [Test]
    [Category("Actions")]
    public void Actions_NameInJsonIsSetToObjectName_IfCreatedFromObjectWithGivenName()
    {
        // When serializing JSON from object with a given name, name is preserved in JSON.
        var json = MinimalJson("Your Actions");
        var asset = InputActionAsset.FromJson(json);
        var content = asset.ToJson();
        ScriptableObject.Destroy(asset);
        Assert.That(content, Is.EqualTo(json));
    }

    [Test]
    [Category("Actions")]
    public void Actions_NameInJsonIsSetToEmptyString_IfCreatedFromObjectWithEmptyName()
    {
        // When serializing JSON from object empty string as given name, name property is set to empty string.
        var asset = InputActionAsset.FromJson(MinimalJson());
        asset.name = string.Empty; // null is not allowed when converting to JSON
        var content = asset.ToJson();
        var expected = MinimalJson(string.Empty);
        ScriptableObject.Destroy(asset);
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    [Category("Actions")]
    public void Actions_NameInJsonIsSetToEmptyString_IfCreatedFromObjectWithNullName()
    {
        // When serializing JSON from object without a given name, Unity forces the name to be an empty string.
        // Basically Unity prevents us from doing serialization an omit optional members in a convenient way.
        // Hence this test just verifies this behavior since its expected.
        // Hence its not possible in an easy way to provide bidirectional transformation to/from JSON.
        // Also note that any additional user augmentation in JSON is currently not supported.
        var asset = InputActionAsset.FromJson(MinimalJson());
        asset.name = null;
        var content = asset.ToJson();
        var expected = MinimalJson(string.Empty);
        ScriptableObject.Destroy(asset);
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryAllEnabledActions()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Remove from `ListEnabledActions`
#endif

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
    public void Actions_CanAddBindingsToActions()
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
    public void Actions_CanRemoveBindingsFromActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/rightStick");

        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/Control")
            .With("Button", "<Keyboard>/a");

        // Total number of bindings should be 5: 2 normal bindings and 3 from the composite
        // bindings
        Assert.That(action.bindings, Has.Count.EqualTo(5));

        // Since we have a composite bindings, all bindings will be removed at once
        action.ChangeBinding(2).Erase();
        Assert.That(action.bindings, Has.Count.EqualTo(2));

        // Remove all remaining bindings
        action.ChangeBinding(0).Erase();
        action.ChangeBinding(0).Erase();
        Assert.That(action.bindings, Has.Count.EqualTo(0));

        // Add a binding to be removed
        action.AddBinding("<Gamepad>/rightStick");
        Assert.That(action.bindings, Has.Count.EqualTo(1));
        action.ChangeBinding(0).Erase();
        Assert.That(action.bindings, Has.Count.EqualTo(0));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddBindingsToActions_ToExistingComposite()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        Assert.That(action.bindings, Has.Count.EqualTo(3));
        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.aKey, keyboard.dKey}));

        var composite = action.ChangeCompositeBinding("Axis");

        composite.InsertPartBinding("Negative", "<Keyboard>/leftArrow");
        composite.InsertPartBinding("Positive", "<Keyboard>/rightArrow");

        Assert.That(action.bindings, Has.Count.EqualTo(5));
        Assert.That(action.bindings,
            Has.Exactly(1).With.Property("isComposite").EqualTo(true).And.With.Property("isPartOfComposite").EqualTo(false).And.With
                .Property("path").EqualTo("Axis"));
        Assert.That(action.bindings,
            Has.Exactly(1).With.Property("isComposite").EqualTo(false).And.With.Property("isPartOfComposite").EqualTo(true).And.With
                .Property("path").EqualTo("<Keyboard>/a"));
        Assert.That(action.bindings,
            Has.Exactly(1).With.Property("isComposite").EqualTo(false).And.With.Property("isPartOfComposite").EqualTo(true).And.With
                .Property("path").EqualTo("<Keyboard>/d"));
        Assert.That(action.bindings,
            Has.Exactly(1).With.Property("isComposite").EqualTo(false).And.With.Property("isPartOfComposite").EqualTo(true).And.With
                .Property("path").EqualTo("<Keyboard>/leftArrow"));
        Assert.That(action.bindings,
            Has.Exactly(1).With.Property("isComposite").EqualTo(false).And.With.Property("isPartOfComposite").EqualTo(true).And.With
                .Property("path").EqualTo("<Keyboard>/rightArrow"));
    }

    [Serializable]
    public enum Modification
    {
        AddBinding,
        RemoveBinding,
        ModifyBinding,
        ApplyBindingOverride,
        AddAction,
        RemoveAction,
        AddMap,
        RemoveMap,
        ChangeBindingMask,
        AddDevice,
        RemoveDevice,
        AddDeviceGlobally,
        RemoveDeviceGlobally,
    }

    public class ModificationCases : IEnumerable
    {
        [Preserve]
        public ModificationCases() {}

        public IEnumerator GetEnumerator()
        {
            bool ModificationAppliesToSingletonAction(Modification modification)
            {
                switch (modification)
                {
                    case Modification.AddBinding:
                    case Modification.RemoveBinding:
                    case Modification.ModifyBinding:
                    case Modification.ApplyBindingOverride:
                    case Modification.AddDeviceGlobally:
                    case Modification.RemoveDeviceGlobally:
                        return true;
                }
                return false;
            }

            bool ModificationAppliesToSingleActionMap(Modification modification)
            {
                switch (modification)
                {
                    case Modification.AddMap:
                    case Modification.RemoveMap:
                        return false;
                }
                return true;
            }

            // NOTE: This executes *outside* of our test fixture during test discovery.

            // Creates a matrix of all permutations of Modifications combined with assets, maps, and singleton actions.
            foreach (var func in new Func<IInputActionCollection2>[] { () => new DefaultInputActions().asset, CreateMap, CreateSingletonAction })
            {
                foreach (var value in Enum.GetValues(typeof(Modification)))
                {
                    var actions = func();
                    if (actions is InputActionMap map)
                    {
                        if (map.m_SingletonAction != null)
                        {
                            if (!ModificationAppliesToSingletonAction((Modification)value))
                                continue;
                        }
                        else if (!ModificationAppliesToSingleActionMap((Modification)value))
                        {
                            continue;
                        }
                    }

                    yield return new TestCaseData(value, actions);
                }
            }
        }

        private InputActionMap CreateMap()
        {
            var map = new InputActionMap("SingleActionMap");
            var action1 = map.AddAction("action1");
            var action2 = map.AddAction("action2");
            var action3 = map.AddAction("action3");
            action1.AddBinding("<Keyboard>/space", groups: "Keyboard");
            action1.AddBinding("<Gamepad>/buttonNorth", groups: "Gamepad");
            action2.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
            action3.AddBinding("<Mouse>/leftButton", groups: "Mouse");
            action3.AddBinding("<Gamepad>/rightTrigger", groups: "Gamepad");
            return map;
        }

        private InputActionMap CreateSingletonAction()
        {
            var action = new InputAction("SingletonAction");
            action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
            action.AddBinding("<Keyboard>/space", groups: "Keyboard");
            return action.GetOrCreateActionMap();
        }
    }

    [Test]
    [Category("Actions")]
    [TestCaseSource(typeof(ModificationCases))]
    public void Actions_CanHandleModification(Modification modification, IInputActionCollection2 actions)
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable();
        InputActionState.DestroyAllActionMapStates(); // Required for `onActionChange` to report correct number of changes
#endif

        var gamepad = InputSystem.AddDevice<Gamepad>();

        if (modification == Modification.AddDevice || modification == Modification.RemoveDevice)
            actions.devices = new[] { gamepad };
        else if (modification == Modification.ChangeBindingMask)
            actions.bindingMask = InputBinding.MaskByGroup("Gamepad");

        List<InputControl> listOfControlsBeforeModification = null;
        InputAction enabledAction = null;

        bool NeedsFullResolve()
        {
            switch (modification)
            {
                case Modification.AddDevice:
                case Modification.RemoveDevice:
                case Modification.AddDeviceGlobally:
                case Modification.RemoveDeviceGlobally:
                    return false;
            }
            return true;
        }

        bool NeedsActionsToBeDisabled()
        {
            switch (modification)
            {
                case Modification.AddAction:
                case Modification.RemoveAction:
                case Modification.AddMap:
                case Modification.RemoveMap:
                    return true;
            }
            return false;
        }

        void ApplyAndVerifyModification()
        {
            switch (modification)
            {
                case Modification.AddBinding:
                {
                    var action = actions.Last(a => a.controls.Contains(gamepad.buttonSouth));
                    action.AddBinding("<Gamepad>/buttonNorth");
                    Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
                    Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonNorth));
                    Assert.That(actions.SelectMany(a => a.controls).Distinct(),
                        Is.EquivalentTo(listOfControlsBeforeModification.Append(gamepad.buttonNorth).Distinct()));
                    break;
                }

                case Modification.RemoveBinding:
                {
                    var action = actions.First(a => a.controls.Contains(gamepad.buttonSouth));
                    action.ChangeBinding(0).Erase();
                    Assert.That(action.controls, Has.Exactly(0).SameAs(gamepad.buttonSouth));
                    break;
                }

                case Modification.ModifyBinding:
                {
                    var action = actions.First(a => a.controls.Contains(gamepad.buttonSouth));
                    action.ChangeBinding(0).WithPath("<Gamepad>/buttonNorth");
                    Assert.That(action.controls, Does.Contain(gamepad.buttonNorth));
                    break;
                }

                case Modification.ApplyBindingOverride:
                {
                    var action = actions.Last(a => a.controls.Contains(gamepad.buttonSouth));
                    action.ApplyBindingOverride("<Gamepad>/buttonNorth");
                    Assert.That(action.controls, Does.Contain(gamepad.buttonNorth));
                    break;
                }

                case Modification.AddAction:
                {
                    foreach (var map in actions.Select(a => a.actionMap).Distinct())
                        map.AddAction("NewAction", binding: "<Gamepad>/leftTrigger");
                    Assert.That(actions.FindAction("NewAction"), Is.Not.Null);
                    Assert.That(actions.FindAction("NewAction").enabled, Is.False);
                    Assert.That(actions.FindAction("NewAction").controls, Is.EqualTo(new[] { gamepad.leftTrigger }));
                    break;
                }

                case Modification.RemoveAction:
                {
                    var action = actions.First(a => a.controls.Contains(gamepad.buttonSouth));
                    action.RemoveAction();
                    Assert.That(actions.FindAction(action.name), Is.Not.SameAs(action));
                    break;
                }

                case Modification.AddMap:
                {
                    var map = new InputActionMap("NewMap");
                    var action = map.AddAction("NewAction", binding: "<Gamepad>/buttonEast");
                    ((InputActionAsset)actions).AddActionMap(map);
                    Assert.That(action.controls, Is.EqualTo(new[] { gamepad.buttonEast }));
                    Assert.That(actions.SelectMany(a => a.controls).Distinct(),
                        Is.EquivalentTo(listOfControlsBeforeModification.Append(gamepad.buttonEast).Distinct()));
                    break;
                }

                case Modification.RemoveMap:
                {
                    var asset = (InputActionAsset)actions;
                    var map = asset.actionMaps[0];
                    asset.RemoveActionMap(map);
                    Assert.That(actions.SelectMany(a => a.controls), Is.Not.Empty);
                    break;
                }

                case Modification.ChangeBindingMask:
                {
                    actions.bindingMask = InputBinding.MaskByGroup("Nothing");
                    Assert.That(actions.SelectMany(a => a.controls), Is.Empty);
                    break;
                }

                case Modification.AddDevice:
                {
                    var addedDevice = InputSystem.AddDevice<Gamepad>();
                    actions.devices = new[] { gamepad, addedDevice };
                    Assert.That(actions.SelectMany(a => a.controls), Is.SupersetOf(listOfControlsBeforeModification));
                    Assert.That(actions.SelectMany(a => a.controls).Select(c => c.device), Does.Contain(addedDevice));
                    break;
                }

                case Modification.RemoveDevice:
                {
                    InputSystem.RemoveDevice(gamepad);
                    Assert.That(actions.devices, Is.Not.Null);
                    Assert.That(actions.devices, Is.Empty);
                    Assert.That(actions.SelectMany(a => a.controls), Is.Empty);
                    Assert.That(actions.Where(a => a.enabled), Is.EquivalentTo(new[] { enabledAction }));
                    break;
                }

                case Modification.AddDeviceGlobally:
                {
                    var addedDevice = InputSystem.AddDevice<Gamepad>();
                    Assert.That(actions.Where(a => a.controls.Any(c => c.device == addedDevice)), Is.Not.Empty,
                        "Expected at least one action to bind to a control on the newly added device");
                    break;
                }

                case Modification.RemoveDeviceGlobally:
                {
                    InputSystem.RemoveDevice(gamepad);
                    Assert.That(actions.Where(a => a.controls.Any(c => c.device == gamepad)), Is.Empty,
                        "Expected that none of the actions binds to a control on the removed device anymore");
                    Assert.That(actions.Where(a => a.enabled), Is.EquivalentTo(new[] { enabledAction }));
                    break;
                }
            }
        }

        var changes = new List<InputActionChange>();
        InputSystem.onActionChange += (_, change) => changes.Add(change);

        // Initial resolve.
        Assert.That(actions.Where(x => x.controls.Contains(gamepad.buttonSouth)), Is.Not.Empty,
            "Expecting at least one action bound to the A button");
        Assert.That(changes, Is.EqualTo(new[] { InputActionChange.BoundControlsChanged }));
        listOfControlsBeforeModification = actions.SelectMany(a => a.controls).Distinct().ToList();

        // Put one action into enabled and in-progress state.
        var aButtonAction = actions.First(x => x.controls.Contains(gamepad.buttonSouth));
        aButtonAction.Enable();
        Press(gamepad.buttonSouth);
        Assert.That(aButtonAction.IsInProgress(), Is.True);
        Assert.That(aButtonAction.activeControl, Is.SameAs(gamepad.buttonSouth));

        // If actions need to be disabled for the modification to be valid,
        // make sure we throw if we attempt the modification now.
        if (NeedsActionsToBeDisabled())
        {
            Assert.That(() => ApplyAndVerifyModification(), Throws.InvalidOperationException);
            aButtonAction.Disable();
        }
        else
        {
            enabledAction = aButtonAction;
        }

        changes.Clear();

        // Apply.
        ApplyAndVerifyModification();

        // If we have removed the gamepad, the action will have been cancelled (but NOT disabled!) by virtue of
        // losing its active control.
        if (modification == Modification.RemoveDevice || modification == Modification.RemoveDeviceGlobally)
        {
            Assert.That(changes, Is.EqualTo(new[] { InputActionChange.ActionCanceled, InputActionChange.BoundControlsAboutToChange, InputActionChange.BoundControlsChanged }));
            Assert.That(aButtonAction.phase.IsInProgress(), Is.False);
            Assert.That(aButtonAction.activeControl, Is.Null);
            Assert.That(aButtonAction.ReadValue<float>(), Is.EqualTo(0f));
        }
        // If the modification doesn't need a full resolve, the action
        // should have kept going uninterrupted.
        else if (!NeedsFullResolve())
        {
            Assert.That(changes, Is.EqualTo(new[] { InputActionChange.BoundControlsAboutToChange, InputActionChange.BoundControlsChanged }));
            Assert.That(aButtonAction.phase.IsInProgress(), Is.True);
            Assert.That(aButtonAction.activeControl, Is.SameAs(gamepad.buttonSouth));
            Assert.That(aButtonAction.ReadValue<float>(), Is.EqualTo(1f));
        }
        else if (!NeedsActionsToBeDisabled())
        {
            // Action should have been automatically disabled and re-enabled.
            Assert.That(changes,
                Is.EqualTo(new[]
                {
                    InputActionChange.ActionCanceled, InputActionChange.ActionDisabled, InputActionChange.BoundControlsAboutToChange,
                    InputActionChange.BoundControlsChanged, InputActionChange.ActionEnabled
                }));
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1218544
    [Test]
    [Category("Actions")]
    public void Actions_CanAddBindingsToActions_AfterActionHasAlreadyResolvedControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(name: "test", binding: "<Gamepad>/leftStick");

        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.leftStick }));
        Assert.That(action.bindings, Has.Count.EqualTo(1));
        Assert.That(action.bindings[0].effectivePath, Is.EqualTo("<Gamepad>/leftStick"));

        action.AddBinding("<Gamepad>/rightStick");

        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.leftStick, gamepad.rightStick }));
        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddCompositeBindingsToActions_AfterActionHasAlreadyResolvedControls()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/leftCtrl")
            .With("Binding", "<Keyboard>/space");

        Assert.That(action.controls, Is.EquivalentTo(new[] { keyboard.spaceKey, keyboard.leftCtrlKey }));

        action.Enable();

        // Replace the composite binding wholesale.
        action.ChangeBinding(0).Erase();
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/rightCtrl")
            .With("Binding", "<Keyboard>/a");

        Assert.That(action.controls, Is.EquivalentTo(new[] { keyboard.aKey, keyboard.rightCtrlKey }));

        Press(keyboard.rightCtrlKey);

        Assert.That(!action.WasPerformedThisFrame());

        Press(keyboard.aKey);

        Assert.That(action.WasPerformedThisFrame());
    }

    [Test]
    [Category("Actions")]
    public void Actions_BindingsHaveUniqueIDs()
    {
        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/leftStick");

        Assert.That(action.bindings[0].m_Id, Is.Not.Null.And.Not.Empty);
        Assert.That(action.bindings[1].m_Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestrictMapsToSpecificDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action = map.AddAction("action", binding: "<Gamepad>/leftStick");

        Assert.That(map.devices, Is.Null);
        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        map.devices = new[] {gamepad2};

        Assert.That(map.devices, Is.EquivalentTo(new[] { gamepad2}));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.None.SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        map.devices = null;

        Assert.That(map.devices, Is.Null);
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

        Assert.That(asset.devices, Is.Null);
        Assert.That(map.devices, Is.Null);
        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        asset.devices = new[] {gamepad2};

        Assert.That(asset.devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(map.devices, Is.EquivalentTo(asset.devices));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.None.SameAs(gamepad1.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftStick));

        asset.devices = null;

        Assert.That(asset.devices, Is.Null);
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(Vector2.zero) // Canceled always gives default value.
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftStick));
            Assert.That(actions[0].ReadValue<Vector2>(),
                Is.EqualTo(Vector2.zero) // Canceled always gives default value.
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

        Press(gamepad.buttonSouth);
        currentTime += 0.1f;
        Release(gamepad.buttonSouth);

        Assert.That(wasPerformed, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddProcessorsToActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterProcessor<ConstantVector2TestProcessor>();
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

    [Test]
    [Category("Actions")]
    public void Actions_IncompatibleProcessorIsIgnored()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterProcessor<ConstantVector2TestProcessor>();
        var action = new InputAction(processors: "ConstantVector2Test");
        action.AddBinding("<Gamepad>/leftStick/x");
        action.Enable();

        float? receivedFloat = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedFloat, Is.Null);
            // ConstantVector2TestProcessor processes Vector2s. It would throw an exception when
            // trying to use it reading a float if not ignored.
            receivedFloat = ctx.ReadValue<float>();
        };

        Set(gamepad.leftStick, Vector2.one);

        Assert.That(receivedFloat, Is.Not.Null);
        Assert.That(receivedFloat.Value, Is.EqualTo(new AxisDeadzoneProcessor().Process(1)));
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ConstantVector2TestProcessor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return new Vector2(0.1234f, 0.5678f);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddProcessorsToBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterProcessor<ConstantVector2TestProcessor>();
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

    // https://fogbugz.unity3d.com/f/cases/1207082/
    [Test]
    [Category("Actions")]
    public void Actions_CanAddProcessorsToCompositeBindings()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("2DVector", processors: "invertVector2(invertX=true,invertY=true)")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        action.Enable();

        // Left -> Right.
        Press(keyboard.aKey);

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(1, 0)));

        // Right -> Left.
        Release(keyboard.aKey);
        Press(keyboard.dKey);

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(-1, 0)));

        // Up -> Down.
        Release(keyboard.dKey);
        Press(keyboard.wKey);

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, -1)));

        // Down -> Up.
        Release(keyboard.wKey);
        Press(keyboard.sKey);

        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, 1)));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddScaleProcessor()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.PassThrough);
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

    private class ConstantFloat1TestProcessor : InputProcessor<float>
    {
        public override float Process(float value, InputControl control)
        {
            return 1.0f;
        }
    }
    private class ConstantFloat2TestProcessor : InputProcessor<float>
    {
        public override float Process(float value, InputControl control)
        {
            return 2.0f;
        }
    }

#if UNITY_EDITOR
    [Test]
    [Category("Actions")]
    public void Actions_AddingSameProcessorTwice_DoesntImpactUIHideState()
    {
        InputSystem.RegisterProcessor<ConstantFloat1TestProcessor>();
        Assert.That(InputSystem.TryGetProcessor("ConstantFloat1Test"), Is.Not.EqualTo(null));

        bool hide = InputSystem.s_Manager.processors.ShouldHideInUI("ConstantFloat1Test");
        Assert.That(hide, Is.EqualTo(false));

        InputSystem.RegisterProcessor<ConstantFloat1TestProcessor>();
        // Check we haven't caused this to alias with itself and cause it to be hidden in the UI
        hide = InputSystem.s_Manager.processors.ShouldHideInUI("ConstantFloat1Test");
        Assert.That(hide, Is.EqualTo(false));
    }

#endif

    [Test]
    [Category("Actions")]
    public void Actions_AddingSameNamedProcessorWithDifferentResult_OverridesOriginal()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterProcessor<ConstantFloat1TestProcessor>("ConstantFloatTest");
        InputSystem.RegisterProcessor<ConstantFloat2TestProcessor>("ConstantFloatTest");

        var action = new InputAction(processors: "ConstantFloatTest");
        action.AddBinding("<Gamepad>/leftTrigger");
        action.Enable();

        float? receivedValue = null;
        action.performed +=
            ctx =>
        {
            Assert.That(receivedValue, Is.Null);
            receivedValue = ctx.ReadValue<float>();
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(receivedValue, Is.Not.Null);
        Assert.That(receivedValue.Value, Is.EqualTo(2.0).Within(0.00001));
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
    public void Actions_WhenDeviceIsAdded_OngoingActionsAreUnaffected()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        var buttonAction = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
        var valueAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftTrigger");

        buttonAction.Enable();
        valueAction.Enable();

        Press(gamepad1.buttonSouth);
        Set(gamepad1.leftTrigger, 0.5f);

        Assert.That(buttonAction.IsPressed, Is.True);
        Assert.That(valueAction.ReadValue<float>(), Is.EqualTo(0.5f));

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            var gamepad2 = InputSystem.AddDevice<Gamepad>();

            Assert.That(trace, Is.Empty);

            // Make sure we execute initial state checks.
            InputSystem.Update();

            Assert.That(trace, Is.Empty);

            Set(gamepad2.leftTrigger, 1f);

            Assert.That(valueAction.ReadValue<float>(), Is.EqualTo(1f));
            Assert.That(valueAction.activeControl, Is.SameAs(gamepad2.leftTrigger));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenDeviceIsRemoved_BoundControlsAreUpdated()
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
    public void Actions_WhenDeviceIsRemoved_ReadingValueInActionListenersWillNotThrow()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var triggerValue = 0.0f;
        var canceled = false;
        var performed = false;
        var action = new InputAction();
        action.AddBinding("<Gamepad>/leftTrigger");

        action.canceled += ctx =>
        {
            canceled = true;
            triggerValue = ctx.ReadValue<float>();
        };

        action.started += ctx =>
        {
            performed = true;
            triggerValue = ctx.ReadValue<float>();
        };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f});
        InputSystem.Update();

        Assert.That(triggerValue, Is.EqualTo(1.0f));
        Assert.That(performed, Is.True);
        Assert.That(canceled, Is.False);
        performed = false;

        InputSystem.RemoveDevice(gamepad);

        Assert.That(triggerValue, Is.EqualTo(0.0f));
        Assert.That(performed, Is.False);
        Assert.That(canceled, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenDeviceIsRemoved_DeviceIsRemovedFromDeviceMask()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable();
        InputActionState.DestroyAllActionMapStates(); // Required for `onActionChange` to report correct number of changes
#endif

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        var action1 = map.AddAction("action", binding: "<Gamepad>/buttonSouth");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var action2 = asset.AddActionMap("map").AddAction("action", binding: "<Gamepad>/buttonSouth");

        map.devices = new[] { gamepad };
        asset.devices = new[] { gamepad };

        Assert.That(action1.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(action2.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

        var controlsChanged = new List<IInputActionCollection>();
        InputSystem.onActionChange +=
            (o, change) =>
        {
            if (change == InputActionChange.BoundControlsChanged)
                controlsChanged.Add((IInputActionCollection)o);
        };

        InputSystem.RemoveDevice(gamepad);

        Assert.That(map.devices, Is.Not.Null); // Empty mask is different from no mask at all.
        Assert.That(map.devices, Is.EquivalentTo(new InputDevice[0]));
        Assert.That(action1.controls, Is.Empty);
        Assert.That(asset.devices, Is.Not.Null); // Empty mask is different from no mask at all.
        Assert.That(asset.devices, Is.EquivalentTo(new InputDevice[0]));
        Assert.That(action2.controls, Is.Empty);

        // We want to have gotten two notifications only for BoundControlsChanged, i.e. only
        // a single binding resolution pass for each collection and not multiple.
        Assert.That(controlsChanged, Has.Count.EqualTo(2));
        Assert.That(controlsChanged, Has.Exactly(1).SameAs(map));
        Assert.That(controlsChanged, Has.Exactly(1).SameAs(asset));

        controlsChanged.Clear();

        // When adding the device back, we do not restore the previous mask.
        InputSystem.AddDevice(gamepad);

        Assert.That(map.devices, Is.Not.Null); // Empty mask is different from no mask at all.
        Assert.That(map.devices, Is.EquivalentTo(new InputDevice[0]));
        Assert.That(action1.controls, Is.Empty);
        Assert.That(asset.devices, Is.Not.Null); // Empty mask is different from no mask at all.
        Assert.That(asset.devices, Is.EquivalentTo(new InputDevice[0]));
        Assert.That(action2.controls, Is.Empty);

        // Adding the device should *not* have triggered binding re-resolution in
        // this case.
        Assert.That(controlsChanged, Is.Empty);
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
    public void Actions_ControlsUpdate_WhenDeviceUsagesChange()
    {
        var device1 = InputSystem.AddDevice<Mouse>();
        var device2 = InputSystem.AddDevice<Mouse>();

        InputSystem.SetDeviceUsage(device1, "Test");

        var action = new InputAction(binding: "<Mouse>{Test}/leftButton");

        Assert.That(action.controls, Is.EquivalentTo(new[] {device1.leftButton}));

        InputSystem.SetDeviceUsage(device2, "Test");

        Assert.That(action.controls, Is.EquivalentTo(new[] {device1.leftButton, device2.leftButton}));

        InputSystem.SetDeviceUsage(device1, null);

        Assert.That(action.controls, Is.EquivalentTo(new[] {device2.leftButton}));
    }

    // This case is important for keyboards as a configuration change on the keyboard may imply a change in keyboard
    // layout which in turn affects bindings to keys by "display name" (i.e. text character).
    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdate_WhenDeviceConfigurationChanges()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Bind to key generating a 'q' character.
        var action = new InputAction(binding: "<Keyboard>/#(Q)");

        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.qKey}));

        // Swap 'a' and 'q'.
        SetKeyInfo(Key.A, "Q");
        SetKeyInfo(Key.Q, "A");

        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.aKey}));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdate_WhenDeviceConfigurationChanges_AndControlIsNotFound()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Bind to key generating a 'ö' character from Swedish layout.
        // It doesn't exist in English layout so initial controls list should be empty.
        var action = new InputAction(binding: "<Keyboard>/#(ö)");

        Assert.That(action.controls, Is.Empty);

        // Rebind the key.
        SetKeyInfo(Key.Semicolon, "ö");

        Assert.That(action.controls, Is.EquivalentTo(new[] {keyboard.semicolonKey}));

        // Rebind the key back.
        SetKeyInfo(Key.Semicolon, ";");

        Assert.That(action.controls, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdate_NotificationIsTriggered_ButOnlyAfterBindingsHaveFirstBeenResolved()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable();
        InputActionState.DestroyAllActionMapStates(); // Required for `onActionChange` to report correct number of changes
 #endif

        var enabledAction = new InputAction("enabledAction", binding: "<Gamepad>/leftTrigger");

        // Enabling an action resolves its bindings. From the on, we get notifications for when
        // bound controls change.
        enabledAction.Enable();

        // On this action we don't trigger binding resolution and thus should not see notifications.
        new InputAction("disabledAction", binding: "<Gamepad>/leftTrigger");

        var controlsQueriedAction = new InputAction("controlsQueriedAction", binding: "<Keyboard>/space");

        var received = new List<object>();
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            received.Add(obj);
            received.Add(change);
        };

        InputSystem.AddDevice<Gamepad>();

        Assert.That(received,
            Is.EqualTo(new object[]
            {
                enabledAction, InputActionChange.BoundControlsAboutToChange,
                enabledAction, InputActionChange.BoundControlsChanged
            }));

        received.Clear();

        // Querying controls from an action resolves its bindings so we should see notifications
        // after doing this.
        _ = controlsQueriedAction.controls;

        Assert.That(received,
            Is.EqualTo(new object[]
            {
                controlsQueriedAction, InputActionChange.BoundControlsChanged
            }));

        received.Clear();

        InputSystem.AddDevice<Keyboard>();

        Assert.That(received,
            Is.EqualTo(new object[]
            {
                controlsQueriedAction, InputActionChange.BoundControlsAboutToChange,
                controlsQueriedAction, InputActionChange.BoundControlsChanged
            }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdateInActionMap_NotificationIsTriggered()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable();
        InputActionState.DestroyAllActionMapStates(); // Required for `onActionChange` to report correct number of changes
#endif

        var actionMap = new InputActionMap("map");
        actionMap.AddAction("action", binding: "<Gamepad>/leftTrigger");
        actionMap.Enable();

        var received = new List<object>();
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            received.Add(obj);
            received.Add(change);
        };

        InputSystem.AddDevice<Gamepad>();

        Assert.That(received,
            Is.EqualTo(new object[]
            {
                actionMap, InputActionChange.BoundControlsAboutToChange,
                actionMap, InputActionChange.BoundControlsChanged,
            }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdateInActionAsset_NotificationIsTriggered()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable();
        InputActionState.DestroyAllActionMapStates(); // Required for `onActionChange` to report correct number of changes
#endif

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "asset";
        var actionMap = new InputActionMap("map");
        asset.AddActionMap(actionMap);
        actionMap.AddAction("action", binding: "<Gamepad>/leftTrigger");
        actionMap.Enable();

        var received = new List<object>();
        InputSystem.onActionChange +=
            (obj, change) =>
        {
            received.Add(obj);
            received.Add(change);
        };

        InputSystem.AddDevice<Gamepad>();

        Assert.That(received, Is.EqualTo(new object[]
        {
            asset, InputActionChange.BoundControlsAboutToChange,
            asset, InputActionChange.BoundControlsChanged,
        }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdate_TimeoutsAreCarriedOver()
    {
        currentTime = 0;
        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=3)");
        action.Enable();

        currentTime = 1;
        Press(gamepad1.buttonSouth);

        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Assert.That(action.IsInProgress(), Is.True);
        Assert.That(action.activeControl, Is.SameAs(gamepad1.buttonSouth));
        Assert.That(action.GetTimeoutCompletionPercentage(), Is.EqualTo(0).Within(0.0001));

        currentTime = 2;
        InputSystem.Update();

        Assert.That(action.GetTimeoutCompletionPercentage(), Is.EqualTo(1 / 3f).Within(0.0001));

        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(action.WasPerformedThisFrame(), Is.False);
        Assert.That(action.IsInProgress(), Is.True);
        Assert.That(action.activeControl, Is.SameAs(gamepad1.buttonSouth));
        Assert.That(action.GetTimeoutCompletionPercentage(), Is.EqualTo(1 / 3f).Within(0.0001));

        currentTime = 5;
        InputSystem.Update();

        Assert.That(action.WasPerformedThisFrame(), Is.True);
        Assert.That(action.IsInProgress(), Is.True);
        Assert.That(action.activeControl, Is.SameAs(gamepad1.buttonSouth));
        Assert.That(action.GetTimeoutCompletionPercentage(), Is.EqualTo(1f).Within(0.0001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenControlsUpdate_InProgressActionsKeepGoing()
    {
        currentTime = 0;
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action1 = new InputAction(binding: "<Gamepad>/leftStick");
        var action2 = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=3)");
        var action3 = new InputAction(binding: "<Gamepad>/buttonNorth");

        action1.Enable();
        action2.Enable();
        action3.Enable();

        Set(gamepad.leftStick, new Vector2(0.6f, 0.6f));
        currentTime = 1;
        Press(gamepad.buttonSouth);

        Assert.That(action1.IsInProgress(), Is.True);
        Assert.That(action2.IsInProgress(), Is.True);
        Assert.That(action3.IsInProgress(), Is.False);
        Assert.That(action1.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action2.activeControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action3.activeControl, Is.Null);
        Assert.That(action2.GetTimeoutCompletionPercentage(), Is.EqualTo(0f));

        currentTime = 2;
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.Update();

        Assert.That(action1.IsInProgress(), Is.True);
        Assert.That(action2.IsInProgress(), Is.True);
        Assert.That(action3.IsInProgress(), Is.False);
        Assert.That(action1.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action2.activeControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action3.activeControl, Is.Null);
        Assert.That(action2.GetTimeoutCompletionPercentage(), Is.EqualTo(1 / 3f).Within(0.001));

        InputSystem.RemoveDevice(gamepad2);
        currentTime = 3;
        Set(gamepad.leftStick, new Vector2(0.7f, 0.7f));

        Assert.That(action1.IsInProgress(), Is.True);
        Assert.That(action2.IsInProgress(), Is.True);
        Assert.That(action3.IsInProgress(), Is.False);
        Assert.That(action1.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action2.activeControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action3.activeControl, Is.Null);
        Assert.That(action2.GetTimeoutCompletionPercentage(), Is.EqualTo(2 * (1 / 3f)).Within(0.001));

        currentTime = 5;
        InputSystem.Update();

        Assert.That(action1.IsInProgress(), Is.True);
        Assert.That(action2.IsInProgress(), Is.True);
        Assert.That(action3.IsInProgress(), Is.False);
        Assert.That(action1.activeControl, Is.SameAs(gamepad.leftStick));
        Assert.That(action2.activeControl, Is.SameAs(gamepad.buttonSouth));
        Assert.That(action3.activeControl, Is.Null);
        Assert.That(action2.GetTimeoutCompletionPercentage(), Is.EqualTo(1f));
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenInProgress_AddingAndRemovingUnusedDevice_DoesNotAffectActionInProgress()
    {
        InputSystem.settings.defaultDeadzoneMax = 1f;
        InputSystem.settings.defaultDeadzoneMin = 0f;

        var usedGamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftStick");
        action.Enable();

        Set(usedGamepad.leftStick, new Vector2(0.6f, 0.6f));

        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
        Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.6f, 0.6f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(action.activeControl, Is.SameAs(usedGamepad.leftStick));

        using (var trace = new InputActionTrace(action))
        {
            // Adding an unused gamepad should re-resolve but should not
            // alter the progression state of the action.
            var unusedGamepad = InputSystem.AddDevice<Gamepad>();
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { usedGamepad.leftStick, unusedGamepad.leftStick }));
            Assert.That(trace, Is.Empty);
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.6f, 0.6f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(action.activeControl, Is.SameAs(usedGamepad.leftStick));

            InputSystem.RemoveDevice(unusedGamepad);
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { usedGamepad.leftStick }));
            Assert.That(trace, Is.Empty);
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.6f, 0.6f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(action.activeControl, Is.SameAs(usedGamepad.leftStick));

            // Finally, add an unused device but then remove the actively used one.
            InputSystem.AddDevice(unusedGamepad);
            InputSystem.RemoveDevice(usedGamepad);
            InputSystem.Update();

            Assert.That(action.controls, Is.EquivalentTo(new[] { unusedGamepad.leftStick }));
            Assert.That(trace, Canceled(action, control: usedGamepad.leftStick, value: Vector2.zero));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
            Assert.That(action.activeControl, Is.Null);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanFindEnabledActions()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Remove from `ListEnabledActions`
#endif

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
        action.canceled += ctx => phases.Add(InputActionPhase.Canceled);

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

        Assert.That(phases, Is.EquivalentTo(new[] { InputActionPhase.Canceled }));
    }

    // Triggers (any analog axis really) may jitter. Make sure that we can perform a hold
    // even if the control wiggles around.
    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldOnTrigger()
    {
        ResetTime();

        InputSystem.settings.defaultButtonPressPoint = 0.1f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/leftTrigger", interactions: "hold(duration=0.4)");
        action.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeTo(action);

            Set(gamepad.leftTrigger, 0.123f, time: 0.1f);
            Set(gamepad.leftTrigger, 0.234f, time: 0.2f);

            Assert.That(trace,
                Started<HoldInteraction>(action, time: 0.1, value: 0.123f));

            trace.Clear();

            Set(gamepad.leftTrigger, 0.345f, time: 0.6f);
            Set(gamepad.leftTrigger, 0.456f, time: 0.7f);

            Assert.That(trace,
                Performed<HoldInteraction>(action, time: 0.6, value: 0.345));
            Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));
        }
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

            // First tap was started, then canceled, then slow tap was started, and then performed.
            actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(4));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[1].interaction, Is.TypeOf<TapInteraction>());
            Assert.That(actions[2].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[2].interaction, Is.TypeOf<SlowTapInteraction>());
            Assert.That(actions[3].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[3].interaction, Is.TypeOf<SlowTapInteraction>());
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1261462/
    [Test]
    [Category("Actions")]
    public void Actions_CanDistinguishMultiTapAndSingleTapOnSameAction()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        // This is a bit of a tricky setup for actions. We will be pressing and releasing a button with 0.1 seconds. This means
        // that at that point, TapInteraction will recognize a tap and perform. However, since it's not driving the action
        // (MultiTapInteraction is since it's coming first and it when into Started phase on mouse down), that has no effect on
        // the action.
        //
        // When we then release the mouse button 0.21 seconds later (i.e. exceeding tapDelay and thus making it not qualify
        // as a multi-tap), the system then needs to correctly go back to TapInteraction which triggered in the *past* and
        // have it "delay-perform" the action.
        //
        // Sounds complicated but what it comes down to is that the system must not reset an interaction's state when it
        // performed until when the whole action performs or cancels.
        var action = new InputAction(binding: "<Mouse>/leftButton", interactions: "multitap(tapTime=0.2,tapDelay=0.2),tap(duration=0.2)");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            currentTime = 1;

            Press(mouse.leftButton);

            Assert.That(trace, Started<MultiTapInteraction>(action, control: mouse.leftButton));

            currentTime = 1.1f;

            trace.Clear();

            Release(mouse.leftButton);

            // At this point, MultiTapInteraction doesn't know yet whether there's another tap
            // coming so it must be waiting until we've exceeded tapDelay.
            Assert.That(trace, Is.Empty);

            currentTime = 1.31f;
            InputSystem.Update();

            Assert.That(trace, Canceled<MultiTapInteraction>(action, control: mouse.leftButton)
                .AndThen(Started<TapInteraction>(action, control: mouse.leftButton, time: 1f)) // Note timestamp here!
                .AndThen(Performed<TapInteraction>(action, control: mouse.leftButton, time: 1.1f))); // Note timestamp here!

            trace.Clear();

            // Make sure nothing got stuck and that we can do the same thing again.
            currentTime = 2;
            Press(mouse.leftButton);

            Assert.That(trace, Started<MultiTapInteraction>(action, control: mouse.leftButton));

            currentTime = 2.1f;

            trace.Clear();

            Release(mouse.leftButton);

            Assert.That(trace, Is.Empty);

            currentTime = 2.31f;
            InputSystem.Update();
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1291334/
    [Test]
    [Category("Actions")]
    public void Actions_SingletonActions_IgnoreActionNameInBindings()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();

        // This can't actually be done through the public API but it can be done
        // with serialized data.
        action.GetOrCreateActionMap().AddBinding("<Gamepad>/leftStick", action: "DoesNotExist");
        action.GetOrCreateActionMap().AddBinding("<Gamepad>/rightStick", action: "");

        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.leftStick, gamepad.rightStick }));
        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].action, Is.EqualTo("DoesNotExist"));
        Assert.That(action.bindings[1].action, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_SingletonActions_CanBeRenamed()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        Assert.That(action.name, Is.Null);
        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

        action.Rename("first");

        Assert.That(action.name, Is.EqualTo("first"));
        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

        action.Rename("second");

        Assert.That(action.name, Is.EqualTo("second"));
        Assert.That(action.controls, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
    }

    /*
    TODO: Implement WithChild and ChainedWith
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
    */

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
        using (var trace = new InputActionTrace(action))
        {
            InputSystem.QueueStateEvent(gamepad1, new GamepadState {leftTrigger = 0.5f});
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad1.leftTrigger));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad1.leftTrigger));
        }

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
    public void Actions_CanMixEnablingSingleActionsAndEntireActionMaps()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");
        var action1 = map1.AddAction("action1", binding: "<Gamepad>/buttonSouth");
        var action2 = map1.AddAction("action2", binding: "<Gamepad>/buttonNorth");
        var action3 = map2.AddAction("action3", binding: "<Gamepad>/buttonSouth");
        var action4 = map2.AddAction("action4", binding: "<Gamepad>/buttonNorth");
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        action3.Enable();
        map1.Enable();

        using (var trace1 = new InputActionTrace(action1))
        using (var trace2 = new InputActionTrace(action2))
        using (var trace3 = new InputActionTrace(action3))
        using (var trace4 = new InputActionTrace(action4))
        {
            PressAndRelease(gamepad.buttonSouth);

            Assert.That(trace1, Started(action1).AndThen(Performed(action1)).AndThen(Canceled(action1)));
            Assert.That(trace2, Is.Empty);
            Assert.That(trace3, Started(action3).AndThen(Performed(action3)).AndThen(Canceled(action3)));
            Assert.That(trace4, Is.Empty);

            trace1.Clear();
            trace2.Clear();
            trace3.Clear();
            trace4.Clear();

            map1.Disable();
            map2.Enable();

            PressAndRelease(gamepad.buttonSouth);

            Assert.That(trace1, Is.Empty);
            Assert.That(trace2, Is.Empty);
            Assert.That(trace3, Started(action3).AndThen(Performed(action3)).AndThen(Canceled(action3)));
            Assert.That(trace4, Is.Empty);

            trace1.Clear();
            trace2.Clear();
            trace3.Clear();
            trace4.Clear();

            map1.Enable();
            map2.Disable();

            PressAndRelease(gamepad.buttonSouth);

            Assert.That(trace1, Started(action1).AndThen(Performed(action1)).AndThen(Canceled(action1)));
            Assert.That(trace2, Is.Empty);
            Assert.That(trace3, Is.Empty);
            Assert.That(trace4, Is.Empty);
        }
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
    public void Actions_CanIterateThroughBindings_WithAccessor()
    {
        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick/x");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        action.AddBinding("<Mouse>/scroll/x");

        Assert.That(action.bindings, Has.Count.EqualTo(5));

        var accessor = action.ChangeBinding(0);

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(0));
        Assert.That(accessor.binding.path, Is.EqualTo("<Gamepad>/leftStick/x"));
        Assert.That(accessor.binding.isComposite, Is.False);
        Assert.That(accessor.binding.isPartOfComposite, Is.False);

        accessor = accessor.NextBinding();

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(1));
        Assert.That(accessor.binding.path, Is.EqualTo("Axis"));
        Assert.That(accessor.binding.isComposite, Is.True);
        Assert.That(accessor.binding.isPartOfComposite, Is.False);

        accessor = accessor.NextBinding();

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(2));
        Assert.That(accessor.binding.path, Is.EqualTo("<Keyboard>/a"));
        Assert.That(accessor.binding.isComposite, Is.False);
        Assert.That(accessor.binding.isPartOfComposite, Is.True);

        accessor = accessor.NextBinding();

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(3));
        Assert.That(accessor.binding.path, Is.EqualTo("<Keyboard>/d"));
        Assert.That(accessor.binding.isComposite, Is.False);
        Assert.That(accessor.binding.isPartOfComposite, Is.True);

        accessor = accessor.NextBinding();

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(4));
        Assert.That(accessor.binding.path, Is.EqualTo("<Mouse>/scroll/x"));
        Assert.That(accessor.binding.isComposite, Is.False);
        Assert.That(accessor.binding.isPartOfComposite, Is.False);

        accessor = accessor.PreviousBinding();

        Assert.That(accessor.valid, Is.True);
        Assert.That(accessor.bindingIndex, Is.EqualTo(3));
        Assert.That(accessor.binding.path, Is.EqualTo("<Keyboard>/d"));
        Assert.That(accessor.binding.isComposite, Is.False);
        Assert.That(accessor.binding.isPartOfComposite, Is.True);

        accessor = accessor.NextBinding().NextBinding();

        Assert.That(accessor.valid, Is.False);
        Assert.That(accessor.bindingIndex, Is.EqualTo(-1));
        Assert.That(() => accessor.binding, Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanIterateThroughCompositeBindings_WithAccessor()
    {
        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick/x");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/w")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/s");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        action.AddBinding("<Mouse>/scroll/x");

        Assert.That(action.ChangeCompositeBinding("Axis").bindingIndex, Is.EqualTo(1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextCompositeBinding("Axis").bindingIndex, Is.EqualTo(6));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Negative").bindingIndex, Is.EqualTo(2));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").bindingIndex, Is.EqualTo(4));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Negative").NextPartBinding("Negative").bindingIndex, Is.EqualTo(3));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").NextPartBinding("Positive").bindingIndex, Is.EqualTo(5));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Negative").NextPartBinding("Negative").NextPartBinding("Negative").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").NextPartBinding("Positive").NextPartBinding("Positive").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").NextPartBinding("Positive").NextBinding().bindingIndex, Is.EqualTo(6));

        Assert.That(action.ChangeCompositeBinding("Axis").PreviousCompositeBinding("Axis").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextCompositeBinding("Axis").PreviousCompositeBinding("Axis").bindingIndex, Is.EqualTo(1));
        Assert.That(action.ChangeCompositeBinding("Axis").PreviousPartBinding("Negative").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").PreviousPartBinding("Positive").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Negative").PreviousPartBinding("Negative").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").PreviousPartBinding("Positive").bindingIndex, Is.EqualTo(-1));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Negative").NextPartBinding("Negative").PreviousPartBinding("Negative").bindingIndex, Is.EqualTo(2));
        Assert.That(action.ChangeCompositeBinding("Axis").NextPartBinding("Positive").NextPartBinding("Positive").PreviousPartBinding("Positive").bindingIndex, Is.EqualTo(4));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanIterateThroughBindings_OfSingleAction_WithAccessor()
    {
        var actionMap = new InputActionMap();

        var action1 = actionMap.AddAction("action1");
        var action2 = actionMap.AddAction("action2");
        var action3 = actionMap.AddAction("action3");

        action1.AddBinding("<Gamepad>/leftStick/x");
        action2.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        action3.AddBinding("<Mouse>/scroll/x");

        Assert.That(action1.ChangeBinding(0).valid, Is.True);
        Assert.That(action1.ChangeBinding(0).bindingIndex, Is.EqualTo(0));
        Assert.That(action1.ChangeBinding(0).binding.path, Is.EqualTo("<Gamepad>/leftStick/x"));
        Assert.That(action1.ChangeBinding(0).NextBinding().valid, Is.False);
        Assert.That(action1.ChangeBinding(0).PreviousBinding().valid, Is.False);

        Assert.That(action2.ChangeBinding(0).valid, Is.True);
        Assert.That(action2.ChangeBinding(0).bindingIndex, Is.EqualTo(0));
        Assert.That(action2.ChangeBinding(0).binding.path, Is.EqualTo("Axis"));
        Assert.That(action2.ChangeBinding(0).NextBinding().valid, Is.True);
        Assert.That(action2.ChangeBinding(0).NextBinding().bindingIndex, Is.EqualTo(1));
        Assert.That(action2.ChangeBinding(0).NextBinding().binding.path, Is.EqualTo("<Keyboard>/a"));
        Assert.That(action2.ChangeBinding(0).NextBinding().NextBinding().valid, Is.True);
        Assert.That(action2.ChangeBinding(0).NextBinding().NextBinding().valid, Is.True);
        Assert.That(action2.ChangeBinding(0).NextBinding().NextBinding().binding.path, Is.EqualTo("<Keyboard>/d"));
        Assert.That(action2.ChangeBinding(0).NextBinding().NextBinding().NextBinding().valid, Is.False);
        Assert.That(action2.ChangeBinding(0).PreviousBinding().valid, Is.False);

        Assert.That(action3.ChangeBinding(0).valid, Is.True);
        Assert.That(action3.ChangeBinding(0).bindingIndex, Is.EqualTo(0));
        Assert.That(action3.ChangeBinding(0).binding.path, Is.EqualTo("<Mouse>/scroll/x"));
        Assert.That(action3.ChangeBinding(0).NextBinding().valid, Is.False);
        Assert.That(action3.ChangeBinding(0).PreviousBinding().valid, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanIterateThroughBindings_OfActionMap_WithAccessor()
    {
        var actionMap = new InputActionMap();

        var action1 = actionMap.AddAction("action1");
        var action2 = actionMap.AddAction("action2");
        var action3 = actionMap.AddAction("action3");

        action1.AddBinding("<Gamepad>/leftStick/x");
        action2.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        action3.AddBinding("<Mouse>/scroll/x");

        Assert.That(actionMap.ChangeBinding(0).valid, Is.True);
        Assert.That(actionMap.ChangeBinding(0).bindingIndex, Is.EqualTo(0));
        Assert.That(actionMap.ChangeBinding(0).binding.path, Is.EqualTo("<Gamepad>/leftStick/x"));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().valid, Is.True);
        Assert.That(actionMap.ChangeBinding(0).NextBinding().bindingIndex, Is.EqualTo(1));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().binding.path, Is.EqualTo("Axis"));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().valid, Is.True);
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().bindingIndex, Is.EqualTo(2));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().binding.path, Is.EqualTo("<Keyboard>/a"));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().valid, Is.True);
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().bindingIndex, Is.EqualTo(3));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().binding.path, Is.EqualTo("<Keyboard>/d"));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().NextBinding().valid, Is.True);
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().NextBinding().bindingIndex, Is.EqualTo(4));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().NextBinding().binding.path, Is.EqualTo("<Mouse>/scroll/x"));
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().NextBinding().NextBinding().valid, Is.False);
        Assert.That(actionMap.ChangeBinding(0).NextBinding().NextBinding().NextBinding().NextBinding().NextBinding().bindingIndex, Is.EqualTo(-1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanChangeExistingBindingOnAction()
    {
        var action = new InputAction();
        action.AddBinding("<Gamepad>/buttonSouth");
        action.AddBinding("<Mouse>/leftButton", groups: "other;mouse");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a", groups: "keyboard")
            .With("Negative", "<Keyboard>/b");

        action.ChangeBindingWithPath("<Keyboard>/a")
            .WithPath("<Keyboard>/1")
            .WithInteraction("Press");
        action.ChangeBindingWithGroup("mouse")
            .WithProcessor("Invert");
        action.ChangeBinding(4)
            .WithName("Positive");
        action.ChangeBindingWithId(action.bindings[2].id)
            .WithProcessor("Test");
        action.ChangeBindingWithPath("<Gamepad>/buttonSouth")
            .To(new InputBinding {path = "test"}); // No action but given it's a singleton action, the binding will stay associated with the action.

        Assert.That(action.bindings[3].path, Is.EqualTo("<Keyboard>/1"));
        Assert.That(action.bindings[3].interactions, Is.EqualTo("Press"));
        Assert.That(action.bindings[1].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(action.bindings[1].processors, Is.EqualTo("Invert"));
        Assert.That(action.bindings[4].name, Is.EqualTo("Positive"));
        Assert.That(action.bindings[2].processors, Is.EqualTo("Test"));
        Assert.That(action.bindings[0].path, Is.EqualTo("test"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanChangeExistingCompositeBindingsOnAction()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftStick/x"); // Noise.
        action.AddCompositeBinding("Axis(whichSideWins=1)")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");

        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.dKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.leftArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.rightArrowKey));

        action.ChangeCompositeBinding("Axis")
            .NextPartBinding("Negative").WithPath("<Keyboard>/q")
            .NextPartBinding("Positive").WithPath("<Keyboard>/e");

        Assert.That(action.bindings[2].path, Is.EqualTo("<Keyboard>/q"));
        Assert.That(action.bindings[3].path, Is.EqualTo("<Keyboard>/e"));

        Assert.That(action.controls, Has.None.SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.None.SameAs(keyboard.dKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.qKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.eKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.leftArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.rightArrowKey));

        action.ChangeCompositeBinding("Axis")
            .NextCompositeBinding("Axis")
            .NextPartBinding("Negative").WithPath("<Keyboard>/downArrow")
            .NextPartBinding("Positive").WithPath("<Keyboard>/upArrow");

        Assert.That(action.bindings[5].path, Is.EqualTo("<Keyboard>/downArrow"));
        Assert.That(action.bindings[6].path, Is.EqualTo("<Keyboard>/upArrow"));

        Assert.That(action.controls, Has.None.SameAs(keyboard.aKey));
        Assert.That(action.controls, Has.None.SameAs(keyboard.dKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.qKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.eKey));
        Assert.That(action.controls, Has.None.SameAs(keyboard.leftArrowKey));
        Assert.That(action.controls, Has.None.SameAs(keyboard.rightArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.upArrowKey));
        Assert.That(action.controls, Has.Exactly(1).SameAs(keyboard.downArrowKey));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanChangeExistingBindingOnActionMap()
    {
        var actionMap = new InputActionMap();

        var action1 = actionMap.AddAction("action1");
        var action2 = actionMap.AddAction("action2");

        action1.AddBinding("<Keyboard>/space");
        action1.AddBinding("<Mouse>/leftButton");
        action2.AddBinding("<Keyboard>/a");

        Assert.That(actionMap.bindings, Has.Count.EqualTo(3));
        Assert.That(actionMap.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
        Assert.That(actionMap.bindings[1].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(actionMap.bindings[2].path, Is.EqualTo("<Keyboard>/a"));

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action2.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("<Keyboard>/space"));
        Assert.That(action1.bindings[1].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("<Keyboard>/a"));

        actionMap.ChangeBinding(0).WithPath("<Keyboard>/enter");

        Assert.That(actionMap.bindings, Has.Count.EqualTo(3));
        Assert.That(actionMap.bindings[0].path, Is.EqualTo("<Keyboard>/enter"));
        Assert.That(actionMap.bindings[1].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(actionMap.bindings[2].path, Is.EqualTo("<Keyboard>/a"));

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action2.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("<Keyboard>/enter"));
        Assert.That(action1.bindings[1].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("<Keyboard>/a"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveExistingBindingOnAction()
    {
        var action = new InputAction();
        action.AddBinding("<Gamepad>/buttonSouth");
        action.AddBinding("<Mouse>/leftButton");
        action.AddCompositeBinding("Axis(whichSideWins=1)")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        Assert.That(action.bindings, Has.Count.EqualTo(5));

        action.ChangeBindingWithPath("<Gamepad>/buttonSouth").Erase();

        Assert.That(action.bindings, Has.Count.EqualTo(4));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Mouse>/leftButton"));
        Assert.That(action.bindings[1].path, Is.EqualTo("Axis(whichSideWins=1)"));
        Assert.That(action.bindings[2].path, Is.EqualTo("<Keyboard>/a"));
        Assert.That(action.bindings[3].path, Is.EqualTo("<Keyboard>/d"));

        action.ChangeBinding(1).Erase();

        Assert.That(action.bindings, Has.Count.EqualTo(1));
        Assert.That(action.bindings[0].path, Is.EqualTo("<Mouse>/leftButton"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_DestroyingAssetClearsCallbacks()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("map");

        asset.AddActionMap(map);

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = map.AddAction("action", binding: "/gamepad/leftTrigger");
        asset.Enable();

        var wasPerformed = false;
        action.performed += ctx => wasPerformed = true;

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 1 });
        InputSystem.Update();

        Assert.That(wasPerformed);
        wasPerformed = false;

        UnityEngine.Object.DestroyImmediate(asset);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0 });
        // There must be no exceptions here from trying to call any callbacks on the destroyed asset.
        InputSystem.Update();

        Assert.That(wasPerformed, Is.False);
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

        Assert.That(asset.FindActionMap("test"), Is.SameAs(map));
        Assert.That(asset.FindActionMap("other"), Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpMapInAssetById()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("test");
        asset.AddActionMap(map);

        Assert.That(asset.FindActionMap(map.id.ToString()), Is.SameAs(map));
        Assert.That(asset.FindActionMap(Guid.NewGuid().ToString()), Is.Null);
    }

    // Legacy format where we use "{...}" notation to indicate we have a GUID string. No longer necessary but
    // we may have some old data that uses it.
    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpMapInAssetById_UsingOldBracedFormat()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("test");
        asset.AddActionMap(map);

        Assert.That(asset.FindActionMap($"{{{map.id}}}"), Is.SameAs(map));
        Assert.That(asset.FindActionMap($"{{{Guid.NewGuid().ToString()}}}"), Is.Null);
    }

    [Test]
    [Category("Actions")]
    [Description("ISXB-895 Can attempt to lookup non-existent action in asset by path")]
    public void Actions_CanAttemptToLookUpNonExistentActionInAssetByPath()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        Assert.That(asset.FindAction("Map/Action1"), Is.Null);

        var map = asset.AddActionMap("Map");
        Assert.That(asset.FindAction("Map/Action1"), Is.Null); // ISXB-895 using a path to find non-existent (NullReferenceException)
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
        var map1action1 = "map1/action1";
        Assert.That(() =>
        {
            asset.FindAction(map1action1);
        }, Is.Not.AllocatingGCMemory());
    }

    // Since we allow looking up by action name without any map qualification, ambiguities result when several
    // actions are named the same. We choose to not do anything special other than generally preferring an
    // enabled action over a disabled one. Other than that, we just return the first hit.
    // https://fogbugz.unity3d.com/f/cases/1207550/
    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionInAssetByName_WithMultipleActionsHavingTheSameName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        var map2 = new InputActionMap("map2");
        var map3 = new InputActionMap("map3");

        asset.AddActionMap(map1);
        asset.AddActionMap(map2);
        asset.AddActionMap(map3);

        var action1 = map1.AddAction("action");
        var action2 = map2.AddAction("action");
        var action3 = map3.AddAction("action");

        Assert.That(asset.FindAction("action"), Is.SameAs(action1));

        action2.Enable();

        Assert.That(asset.FindAction("action"), Is.SameAs(action2));

        action3.Enable();

        // No difference. Returns first enabled action.
        Assert.That(asset.FindAction("action"), Is.SameAs(action2));

        action2.Disable();

        Assert.That(asset.FindAction("action"), Is.SameAs(action3));

        action1.Enable();

        Assert.That(asset.FindAction("action"), Is.SameAs(action1));

        asset.Disable();

        Assert.That(asset.FindAction("action"), Is.SameAs(action1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveActionFromMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map = new InputActionMap("test");
        asset.AddActionMap(map);

        var action1 = map.AddAction("action1", binding: "<Gamepad>/buttonSouth");
        var action2 = map.AddAction("action2", binding: "<Gamepad>/buttonNorth");
        var action3 = map.AddAction("action3", binding: "<Gamepad>/buttonWest");

        asset.RemoveAction("action2");

        Assert.That(action2.actionMap, Is.Null);
        Assert.That(asset.FindAction("action2"), Is.Null);
        Assert.That(map.actions, Has.Count.EqualTo(2));
        Assert.That(map.actions, Has.Exactly(1).SameAs(action1));
        Assert.That(map.actions, Has.Exactly(1).SameAs(action3));
        Assert.That(action1.bindings, Is.EquivalentTo(new[] {new InputBinding("<Gamepad>/buttonSouth", action: "action1")}));
        Assert.That(action2.bindings, Is.EquivalentTo(new[] {new InputBinding("<Gamepad>/buttonNorth", action: "action2")}));
        Assert.That(action3.bindings, Is.EquivalentTo(new[] {new InputBinding("<Gamepad>/buttonWest", action: "action3")}));
        Assert.That(map.bindings, Is.EquivalentTo(new[]
        {
            new InputBinding("<Gamepad>/buttonSouth", action: "action1"),
            new InputBinding("<Gamepad>/buttonWest", action: "action3")
        }));
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

        Assert.That(asset.FindControlScheme("SCHEme1").Value.name, Is.EqualTo("scheme1"));
        Assert.That(asset.FindControlScheme("scheme2").Value.name, Is.EqualTo("scheme2"));
        Assert.That(asset.FindControlScheme("doesNotExist"), Is.Null);
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
    public void Actions_CanRequireSpecificDevicesForControlScheme()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        asset.AddControlScheme("scheme")
            .WithRequiredDevice("<XRController>{LeftHand}")
            .WithRequiredDevice("<XRController>{RightHand}")
            .WithOptionalDevice("<Gamepad>");

        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements, Has.Count.EqualTo(3));
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[0].controlPath, Is.EqualTo("<XRController>{LeftHand}"));
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[0].isOptional, Is.False);
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[1].controlPath, Is.EqualTo("<XRController>{RightHand}"));
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[1].isOptional, Is.False);
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[2].controlPath, Is.EqualTo("<Gamepad>"));
        Assert.That(asset.FindControlScheme("scheme").Value.deviceRequirements[2].isOptional, Is.True);
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

    [Test]
    [Category("Actions")]
    public void Actions_CanPickDevicesThatMatchGivenControlScheme_ReturningAccurateScoreForEachMatch_HID()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
        var genericGamepad = InputSystem.AddDevice<Gamepad>();
        var ps4Gamepad = InputSystem.AddDevice<DualShock4GamepadHID>();

        var genericGamepadScheme = new InputControlScheme("GenericGamepad")
            .WithRequiredDevice("<Gamepad>");
        var ps4GamepadScheme = new InputControlScheme("PS4Gamepad")
            .WithRequiredDevice("<DualShockGamepad>");

        using (var genericToGeneric = genericGamepadScheme.PickDevicesFrom(new[] { genericGamepad }))
        using (var genericToPS4 = genericGamepadScheme.PickDevicesFrom(new[] { ps4Gamepad }))
        using (var ps4ToPS4 = ps4GamepadScheme.PickDevicesFrom(new[] { ps4Gamepad }))
        {
            Assert.That(genericToPS4.score, Is.GreaterThan(1));
            Assert.That(ps4ToPS4.score, Is.GreaterThan(1));

            // Generic gamepad is a more precise match for generic gamepad scheme than PS4 *HID* controller
            // is for PS4 gamepad scheme.
            Assert.That(genericToGeneric.score, Is.GreaterThan(ps4ToPS4.score));

            // PS4 *HID* gamepad to PS4 gamepad scheme is a 50% match as the HID layout is one step removed
            // from the base PS4 gamepad layout.
            Assert.That(ps4ToPS4.score, Is.EqualTo(1 + 0.5f));
        }
#endif
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPickDevicesThatMatchGivenControlScheme_ReturningAccurateScoreForEachMatch()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_ANDROID || UNITY_IOS
        var genericGamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var genericGamepadScheme = new InputControlScheme("GenericGamepad")
            .WithRequiredDevice("<Gamepad>");
        var ps4GamepadScheme = new InputControlScheme("PS4Gamepad")
            .WithRequiredDevice("<DualShockGamepad>");

        using (var genericToGeneric = genericGamepadScheme.PickDevicesFrom(new[] { genericGamepad }))
        using (var ps4ToGeneric = ps4GamepadScheme.PickDevicesFrom(new[] { genericGamepad }))
        using (var genericToMouse = genericGamepadScheme.PickDevicesFrom(new[] { mouse }))
        using (var ps4ToMouse = ps4GamepadScheme.PickDevicesFrom(new[] { mouse }))
        {
            Assert.That(genericToGeneric.score, Is.GreaterThan(1));
            Assert.That(ps4ToGeneric.score, Is.Zero); // Generic gamepad is no match for PS4 scheme.
            Assert.That(genericToMouse.score, Is.Zero);
            Assert.That(ps4ToMouse.score, Is.Zero);

            // Generic gawmepad is a more precise match for generic gamepad scheme than PS4 controller is
            // for generic gamepad scheme.
            Assert.That(genericToGeneric.score, Is.GreaterThan(ps4ToGeneric.score));

            // Generic gamepad to generic gamepad scheme is a 100% match so score is one for matching the
            // requirement plus 1 for matching it 100%.
            Assert.That(genericToGeneric.score, Is.EqualTo(1 + 1));
        }
#endif
    }

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
        Assert.That(InputControlScheme.FindControlSchemeForDevice(keyboard, new[] { scheme1, scheme2 }),
            Is.Null);
        Assert.That(InputControlScheme.FindControlSchemeForDevice(mouse, new[] { scheme1, scheme2 }),
            Is.Null);
        Assert.That(InputControlScheme.FindControlSchemeForDevice(touch, new[] {scheme1, scheme2}),
            Is.Null);
        Assert.That(InputControlScheme.FindControlSchemeForDevices(new InputDevice[] { keyboard, mouse }, new[] { scheme1, scheme2 }),
            Is.EqualTo(scheme2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenFindingControlSchemeUsingGivenDevice_MostSpecificControlSchemeIsChosen()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
        var genericGamepadScheme = new InputControlScheme("GenericGamepad")
            .WithRequiredDevice("<Gamepad>");
        var ps4GamepadScheme = new InputControlScheme("PS4")
            .WithRequiredDevice("<DualShockGamepad>");
        var xboxGamepadScheme = new InputControlScheme("Xbox")
            .WithRequiredDevice("<XInputController>");
        var mouseScheme = new InputControlScheme("Mouse") // Noise.
            .WithRequiredDevice("<Mouse>");

        var genericGamepad = InputSystem.AddDevice<Gamepad>();
        var ps4Controller = InputSystem.AddDevice<DualShockGamepad>();
        var xboxController = InputSystem.AddDevice<XInputController>();

        Assert.That(InputControlScheme.FindControlSchemeForDevice(genericGamepad, new[] { genericGamepadScheme, ps4GamepadScheme, xboxGamepadScheme, mouseScheme }),
            Is.EqualTo(genericGamepadScheme));
        Assert.That(InputControlScheme.FindControlSchemeForDevice(ps4Controller, new[] { genericGamepadScheme, ps4GamepadScheme, xboxGamepadScheme, mouseScheme }),
            Is.EqualTo(ps4GamepadScheme));
        Assert.That(InputControlScheme.FindControlSchemeForDevice(xboxController, new[] { genericGamepadScheme, ps4GamepadScheme, xboxGamepadScheme, mouseScheme }),
            Is.EqualTo(xboxGamepadScheme));
#endif
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

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(mouse.leftButton));
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

        using (var trace = new InputActionTrace(action))
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();

            Assert.That(trace, Is.Empty);

            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
            InputSystem.Update();

            var actions = trace.ToArray();
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Started));
            Assert.That(actions[0].control, Is.SameAs(gamepad.buttonNorth));
            Assert.That(actions[1].phase, Is.EqualTo(InputActionPhase.Performed));
            Assert.That(actions[1].control, Is.SameAs(gamepad.buttonNorth));
        }
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
        Assert.That(action1.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action2.controls, Has.Count.EqualTo(1));
        Assert.That(action2.controls, Has.Exactly(1).SameAs(mouse.leftButton));
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

    [Test]
    [Category("Actions")]
    public void Actions_WhenMaskingByGroup_BindingsNotInAnyGroupWillBeActive()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
        action.AddBinding("<Pointer>/press");

        action.bindingMask = InputBinding.MaskByGroup("Gamepad");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(mouse.press));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanGetDisplayStringForBindings()
    {
        Assert.That(new InputBinding("<Keyboard>/space").ToDisplayString(), Is.EqualTo("Space"));
        Assert.That(new InputBinding("<Gamepad>/buttonSouth").ToDisplayString(), Is.EqualTo(GamepadState.ButtonSouthShortDisplayName));
        Assert.That(new InputBinding("<Mouse>/leftButton").ToDisplayString(), Is.EqualTo("LMB"));
        Assert.That(new InputBinding().ToDisplayString(), Is.EqualTo(""));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanGetDisplayStringForBindings_UsingDisplayNamesFromActualDevice()
    {
        var psController = InputSystem.AddDevice<DualShockGamepad>();

        Assert.That(new InputBinding("<Gamepad>/buttonSouth").ToDisplayString(control: psController), Is.EqualTo("Cross"));
    }

    // The following functionality is the basis for associating images with binding strings. For example, when looking at the
    // binding string "A", we need to know the context of it to be able to choose an image. Is it a keyboard "A" key or is it the
    // "A" button on a gamepad? Two different images. So, we need to be able to not just get the "A" string but also the name
    // of the device layout that gives context to the display string.
    //
    // The input system currently has no built-in mechanism for actually managing such imagery but with the functionality here,
    // it is possible to easily build custom mechanisms.
    [Test]
    [Category("Actions")]
    public void Actions_CanGetDisplayStringForBindings_AndGetDeviceLayoutAndControlPath()
    {
        var psController = InputSystem.AddDevice<DualShockGamepad>();

        new InputBinding("<Gamepad>/buttonSouth").ToDisplayString(out var layoutA, out var controlA, control: psController);
        new InputBinding("<Gamepad>/buttonSouth").ToDisplayString(out var layoutB, out var controlB);

        Assert.That(layoutA, Is.EqualTo("DualShockGamepad"));
        Assert.That(layoutB, Is.EqualTo("Gamepad"));
        Assert.That(controlA, Is.EqualTo("buttonSouth"));
        Assert.That(controlB, Is.EqualTo("buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanGetDisplayStringForBindings_AndIgnoreBindingOverrides()
    {
        Assert.That(
            new InputBinding { path = "<Mouse>/leftButton", overridePath = "<Keyboard>/space" }.ToDisplayString(InputBinding
                .DisplayStringOptions.IgnoreBindingOverrides), Is.EqualTo("LMB"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotGetDisplayStringForCompositeBindingsDirectlyFromBinding()
    {
        var action = new InputAction();

        action.AddCompositeBinding("1DAxis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/d");

        Assert.That(action.bindings[0].ToDisplayString(), Is.EqualTo(""));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpBindingIndexByMask()
    {
        var action = new InputAction();

        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/upArrow")
            .With("Positive", "<Keyboard>/downArrow");

        Assert.That(action.GetBindingIndex(group: "Keyboard"), Is.EqualTo(0));
        Assert.That(action.GetBindingIndex(group: "Gamepad"), Is.EqualTo(1));
        Assert.That(action.GetBindingIndex(group: "Mouse"), Is.EqualTo(2));

        Assert.That(action.GetBindingIndex(path: "<Keyboard>/space"), Is.EqualTo(0));
        Assert.That(action.GetBindingIndex(path: "<Gamepad>/buttonSouth"), Is.EqualTo(1));
        Assert.That(action.GetBindingIndex(path: "<Mouse>/leftButton"), Is.EqualTo(2));
        Assert.That(action.GetBindingIndex(path: "<Keyboard>/leftArrow"), Is.EqualTo(4));
        Assert.That(action.GetBindingIndex(path: "<Keyboard>/rightArrow"), Is.EqualTo(5));
        Assert.That(action.GetBindingIndex(path: "<Keyboard>/upArrow"), Is.EqualTo(7));
        Assert.That(action.GetBindingIndex(path: "<Keyboard>/downArrow"), Is.EqualTo(8));

        Assert.That(action.GetBindingIndex("DoesNotExist"), Is.EqualTo(-1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpBindingIndexOnActionMapByMask()
    {
        var actionMap = new InputActionMap();

        var action1 = actionMap.AddAction("action1");
        var action2 = actionMap.AddAction("action2");
        var action3 = actionMap.AddAction("action3");

        action1.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action1.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action2.AddBinding("<Mouse>/leftButton", groups: "Keyboard");
        action3.AddBinding("<Gamepad>/leftTrigger", groups: "Gamepad");

        Assert.That(actionMap.GetBindingIndex(new InputBinding { groups = "Keyboard" }), Is.EqualTo(0));
        Assert.That(actionMap.GetBindingIndex(new InputBinding { groups = "Gamepad" }), Is.EqualTo(1));
        Assert.That(actionMap.GetBindingIndex(new InputBinding { path = "<Mouse>/leftButton" }), Is.EqualTo(2));

        Assert.That(actionMap.GetBindingIndex(new InputBinding { groups = "DoesNotExist" }), Is.EqualTo(-1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpBindingIndexByBoundControl()
    {
        var action = new InputAction();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        action.AddBinding("<Keyboard>/space");
        action.AddBinding("<Gamepad>/buttonSouth");
        action.AddBinding("<Mouse>/leftButton");

        Assert.That(action.GetBindingIndexForControl(keyboard.spaceKey), Is.EqualTo(0));
        Assert.That(action.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(1));
        Assert.That(action.GetBindingIndexForControl(gamepad.buttonNorth), Is.EqualTo(-1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpBindingIndexByBoundControl_FromPartBinding()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();

        action.AddBinding("<Gamepad>/leftTrigger");
        action.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        Assert.That(action.GetBindingIndexForControl(keyboard.qKey), Is.EqualTo(2)); // Composite is at #1.
        Assert.That(action.GetBindingIndexForControl(keyboard.eKey), Is.EqualTo(3));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpBindingIndexByBoundControl_InComplexSetup()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");

        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");
        map2.AddAction("action4");

        map1.AddBinding("<Gamepad>/buttonSouth", action: "action1");
        map2.AddBinding("<Gamepad>/buttonSouth", action: "action3");
        map2.AddBinding("<Gamepad>/buttonNorth", action: "action4");
        map2.AddBinding("<Gamepad>/buttonWest", action: "action3");
        map2.AddBinding("<Gamepad>/buttonEast", action: "action3");

        Assert.That(action1.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));
        Assert.That(action2.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(-1));
        Assert.That(action3.GetBindingIndexForControl(gamepad.buttonWest), Is.EqualTo(1));
        Assert.That(action3.GetBindingIndexForControl(gamepad.buttonSouth), Is.EqualTo(0));
        Assert.That(action3.GetBindingIndexForControl(gamepad.buttonNorth), Is.EqualTo(-1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanGetDisplayTextForBindingsOnAction()
    {
        var action = new InputAction();

        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        Assert.That(action.GetBindingDisplayString(), Is.EqualTo("Space | A | LMB"));
        Assert.That(action.GetBindingDisplayString(group: "Keyboard"), Is.EqualTo("Space"));
        Assert.That(action.GetBindingDisplayString(group: "Gamepad"), Is.EqualTo("A"));
        Assert.That(action.GetBindingDisplayString(group: "Mouse"), Is.EqualTo("LMB"));

        // No binding for scheme simply returns empty string.
        Assert.That(action.GetBindingDisplayString(group: "DoesNotExist"), Is.EqualTo(""));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenGettingDisplayTextForBindingsOnAction_BindingMaskActsAsDefault()
    {
        var action = new InputAction();

        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        var actionMap = new InputActionMap();
        var actionInMap = actionMap.AddAction("action");
        actionInMap.AddBinding("<Keyboard>/space", groups: "Keyboard");
        actionInMap.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        actionInMap.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMapInAsset = asset.AddActionMap("map");
        var actionInAsset = actionMapInAsset.AddAction("action");
        actionInAsset.AddBinding("<Keyboard>/space", groups: "Keyboard");
        actionInAsset.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        actionInAsset.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        action.bindingMask = InputBinding.MaskByGroup("Keyboard");
        actionInMap.bindingMask = InputBinding.MaskByGroup("Keyboard");
        actionInAsset.bindingMask = InputBinding.MaskByGroup("Keyboard");

        Assert.That(action.GetBindingDisplayString(), Is.EqualTo("Space"));
        Assert.That(actionInMap.GetBindingDisplayString(), Is.EqualTo("Space"));
        Assert.That(actionInAsset.GetBindingDisplayString(), Is.EqualTo("Space"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenGettingDisplayTextForBindingsOnAction_ResolvedControlsAreUsedForDisplayNames()
    {
        var action = new InputAction();

        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        action.AddBinding("<Mouse>/leftButton", groups: "Mouse");

        InputSystem.AddDevice<Keyboard>();
        SetKeyInfo(Key.Space, "Leertaste");

        Assert.That(action.GetBindingDisplayString(group: "Keyboard"), Is.EqualTo("Leertaste"));
        Assert.That(action.GetBindingDisplayString(group: "Mouse"), Is.EqualTo("LMB"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenGettingDisplayTextForBindingsOnAction_CompositesAreFormattedAsWhole()
    {
        var action = new InputAction();

        action.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("ButtonWithOneModifier")
            .With("Modifier", "<Keyboard>/LeftShift")
            .With("Modifier", "<Keyboard>/RightShift")
            .With("Button", "<Keyboard>/a");
        action.AddBinding("<Gamepad>/leftStick/x"); // Noise.

        Assert.That(action.GetBindingDisplayString(0), Is.EqualTo("A/D"));
        Assert.That(action.GetBindingDisplayString(3), Is.EqualTo("W/A/S/D"));
        Assert.That(action.GetBindingDisplayString(8), Is.EqualTo("Left Shift|Right Shift+A"));
    }

    // https://fogbugz.unity3d.com/f/cases/1321175/
    [Test]
    [Category("Actions")]
    public void Actions_WhenGettingDisplayTextForBindingsOnAction_EmptyBindingsOnComposites_ArePrintedAsSpaces()
    {
        var action = new InputAction();

        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "");

        Assert.That(action.GetBindingDisplayString(), Is.EqualTo("Up Arrow/ / / "));
    }

    ////TODO: this will need to take localization into account (though this is part of a broader integration that also affects other features of the input system)
    [Test]
    [Category("Actions")]
    public void Actions_WhenGettingDisplayTextForBindingsOnAction_InteractionsAreShownByDefault()
    {
        var action1 = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.4)");
        var action2 = new InputAction(binding: "<Gamepad>/buttonSouth", interactions: "hold(duration=0.4);press");

        // An action where the interaction sits directly on the action and not on bindings.
        var action3 = new InputAction(interactions: "hold");
        action3.AddBinding("<Gamepad>/buttonSouth");

        Assert.That(action1.GetBindingDisplayString(), Is.EqualTo("Hold " + GamepadState.ButtonSouthShortDisplayName));
        Assert.That(action2.GetBindingDisplayString(), Is.EqualTo("Hold or Press " + GamepadState.ButtonSouthShortDisplayName));
        Assert.That(action3.GetBindingDisplayString(), Is.EqualTo("Hold " + GamepadState.ButtonSouthShortDisplayName));

        // Can suppress.
        Assert.That(action1.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions),
            Is.EqualTo(GamepadState.ButtonSouthShortDisplayName));
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_WhenGettingDisplayTextForBindingsOnAction_CanGetQualificationsForEachIndividualStringComponent()
    {
        //interaction:Hold gamepad:A
        //is this actually useful? the use case here is localization but not sure this is the most usable approach
        Assert.Fail();
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
    [TestCase("", "<Gamepad>/buttonSouth", "<Gamepad>/buttonWest", "<Gamepad>/buttonEast")]
    [TestCase("<Gamepad>/buttonNorth", "", "<Gamepad>/buttonWest", "<Gamepad>/buttonEast")]
    [TestCase("<Gamepad>/buttonNorth", "<Gamepad>/buttonSouth", "", "<Gamepad>/buttonEast")]
    [TestCase("<Gamepad>/buttonNorth", "<Gamepad>/buttonSouth", "<Gamepad>/buttonWest", "")]
    public void Actions_CanHaveCompositesWithPartsThatAreNotBound(string up, string down, string left, string right)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("2DVector")
            .With("Up", up)
            .With("Down", down)
            .With("Left", left)
            .With("Right", right);

        action.Enable();

        if (!string.IsNullOrEmpty(up))
        {
            Press(gamepad.buttonNorth);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.up).Using(Vector2EqualityComparer.Instance));
            Release(gamepad.buttonNorth);
        }

        if (!string.IsNullOrEmpty(down))
        {
            Press(gamepad.buttonSouth);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.down).Using(Vector2EqualityComparer.Instance));
            Release(gamepad.buttonSouth);
        }

        if (!string.IsNullOrEmpty(left))
        {
            Press(gamepad.buttonWest);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.left).Using(Vector2EqualityComparer.Instance));
            Release(gamepad.buttonWest);
        }

        if (!string.IsNullOrEmpty(right))
        {
            Press(gamepad.buttonEast);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));
            Release(gamepad.buttonEast);
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1244988/
    [Test]
    [Category("Actions")]
    public void Actions_CanHaveCompositesWithoutControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Value);
        action.AddBinding("<Gamepad>/leftTrigger");
        action.AddBinding("<Gamepad>/rightTrigger");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<DoesNotExist>/leftButton")
            .With("Negative", "<DoesNotExist>/rightButton");

        action.Enable();

        // Actuate both triggers and make sure the disambiguation code isn't stumbling over
        // the composite that in fact has no controls bound to its parts.
        Set(gamepad.leftTrigger, 0.6f);
        Set(gamepad.rightTrigger, 0.7f);
        Set(gamepad.rightTrigger, 0.4f); // Disambiguation now needs to find leftTrigger; should not be thrown off track by the empty composite.

        Assert.That(action.ReadValue<float>(), Is.EqualTo(0.6f));
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
    public void Actions_CanGetCompositeNameFromBinding()
    {
        var action = new InputAction();
        action.AddCompositeBinding("1DAxis(normalize=false)")
            .With("Positive", "<Gamepad>/leftTrigger")
            .With("Negative", "<Gamepad>/rightTrigger");

        Assert.That(action.bindings[0].GetNameOfComposite(), Is.EqualTo("1DAxis"));
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

            Assert.That(trace, Started(action, control: gamepad.leftTrigger, value: -0.345f)
                .AndThen(Performed(action, control: gamepad.leftTrigger, value: -0.345f)));

            trace.Clear();

            // Positive.
            InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.456f});
            InputSystem.Update();

            // Bit of an odd case. leftTrigger and rightTrigger have both changed state here so
            // in a way, it's up to the system which one to pick. Might be useful if it was deliberately
            // picking the control with the highest magnitude but not sure it's worth the effort.
            Assert.That(trace, Performed(action, control: gamepad.leftTrigger, value: 0.456f));

            trace.Clear();

            // Neither.
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.Update();

            Assert.That(trace, Canceled(action, control: gamepad.rightTrigger, value: 0f));
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

            Assert.That(trace, Started(action, value: -0.345f)
                .AndThen(Performed(action, value: -0.345f)));

            trace.Clear();

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.345f, rightTrigger = 0.543f});
            InputSystem.Update();

            Assert.That(trace, Canceled(action, value: 0f));

            trace.Clear();

            // Positive wins.
            action.bindingMask = InputBinding.MaskByGroup("positive");

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.123f, rightTrigger = 0.234f});
            InputSystem.Update();

            // We get a started and performed when switching to the right trigger and then another performed
            // when the right trigger changes value.
            Assert.That(trace,
                Started(action, value: 0.543f).AndThen(Performed(action, value: 0.543f)).AndThen(Performed(action, value: 0.234f)));

            trace.Clear();

            // Negative wins.
            action.bindingMask = InputBinding.MaskByGroup("negative");

            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.567f, rightTrigger = 0.765f});
            InputSystem.Update();

            Assert.That(trace,
                Canceled(action, value: 0f).AndThen(Started(action, value: -0.123f)).AndThen(Performed(action, value: -0.123f))
                    .AndThen(Performed(action, value: -0.567f)));
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1335838/
    [Test]
    [Category("Actions")]
    public void Actions_CanCreateAxisComposite_WithCustomMinMax()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("1DAxis(minValue=1,maxValue=2)")
            .With("Positive", "<Gamepad>/rightTrigger")
            .With("Negative", "<Gamepad>/leftTrigger");

        action.Enable();

        // Put left trigger at half value. Should push us from mid-poing (1.5) half-way
        // towards minValue (1).
        Set(gamepad.leftTrigger, 0.5f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(1.25f).Within(0.00001));

        // Push left trigger all the way. Should put us at minValue (1).
        Set(gamepad.leftTrigger, 1f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(1).Within(0.00001));

        Set(gamepad.leftTrigger, 0);

        Assert.That(action.ReadValue<float>(), Is.Zero.Within(0.00001));

        // Now go the opposite way.
        Set(gamepad.rightTrigger, 0.5f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(1.75f).Within(0.00001));

        // And all the way.
        Set(gamepad.rightTrigger, 1f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(2).Within(0.00001));
    }

    // https://fogbugz.unity3d.com/f/cases/1398942/
    [Test]
    [Category("Actions")]
    public void Actions_CanCreateAxisComposite_AndApplyProcessorsToPartBindings()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var action = new InputAction();
        action.AddCompositeBinding("1DAxis(minValue=0,maxValue=10)")
            .With("Positive", "<Mouse>/scroll/y", processors: "clamp(min=0,max=1)")
            .With("Negative", "<Mouse>/scroll/y", processors: "invert,clamp(min=0,max=1)");
        action.Enable();

        Set(mouse.scroll.y, 2f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(10));

        Set(mouse.scroll.y, -2f);

        Assert.That(action.ReadValue<float>(), Is.EqualTo(0));
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
    public void Actions_CanCreateVector2Composite_FromAnalogControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Get rid of deadzoning for simpler test.
        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        // Lower button press threshold for digital version.
        InputSystem.settings.defaultButtonPressPoint = 0.2f;

        var analogAction = new InputAction("analog", type: InputActionType.Value);
        var digitalAction = new InputAction("digital", type: InputActionType.Value);

        analogAction.AddCompositeBinding("2DVector(mode=2)") // Mode.Analog
            .With("Up", "<Gamepad>/leftStick/up")
            .With("Down", "<Gamepad>/leftStick/down")
            .With("Left", "<Gamepad>/leftStick/left")
            .With("Right", "<Gamepad>/leftStick/right");
        digitalAction.AddCompositeBinding("2DVector(mode=1)") // Mode.Digital
            .With("Up", "<Gamepad>/leftStick/up")
            .With("Down", "<Gamepad>/leftStick/down")
            .With("Left", "<Gamepad>/leftStick/left")
            .With("Right", "<Gamepad>/leftStick/right");

        analogAction.Enable();
        digitalAction.Enable();

        Set(gamepad.leftStick, new Vector2(-0.234f, 0.345f));

        Assert.That(analogAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(-0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(digitalAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(-1, 1)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector2Composite_RespectsButtonPressurePoint()
    {
        // The stick has deadzones on the up/down/left/right buttons to get rid of stick
        // noise. For this test, simplify things by getting rid of deadzones.
        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        var gamepad = InputSystem.AddDevice<Gamepad>();

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
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up (slightly above press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up (slightly below press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.zero));

        // Up left.
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Up left (up slightly above press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Up left (up slightly below press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.left));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector2Composite_WithKeyboardKeys_CancelOnRelease()
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

        bool wasCanceled = false;
        action.canceled += ctx => { wasCanceled = true; };

        // Test all directions to ensure they are correctly canceled
        var keys = new Key[] { Key.W, Key.A, Key.S, Key.D };
        foreach (var key in keys)
        {
            wasCanceled = false;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();

            Assert.That(wasCanceled, Is.EqualTo(true));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateComposite_WithMultipleBindingsPerPart()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up classic WASD control and additional arrow key support on the same composite.
        var action = new InputAction();
        action.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
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

        // Up.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow, Key.LeftArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.left));

        // Down left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftArrow, Key.DownArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.left + Vector2.down).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.left + Vector2.down).normalized.y).Within(0.00001));

        // Down.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.down));

        // Down right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow, Key.RightArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.right));

        // Up right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow, Key.UpArrow));
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
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
            context.Performed();
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
        LogAssert.NoUnexpectedReceived();
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
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
        //       to drive the associated action is whichever had the last input event.
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
    public void Actions_WithMultipleComposites_CancelsIfCompositeIsReleased()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterInteraction<LogInteraction>();

        var action = new InputAction();
        action.AddCompositeBinding("Dpad(normalize=0)")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        action.Enable();

        InputControl performedControl = null;
        InputControl canceledControl = null;
        var value = Vector2.zero;
        action.performed += ctx =>
        {
            performedControl = ctx.control;
            value = ctx.ReadValue<Vector2>();
        };
        action.canceled += ctx =>
        {
            canceledControl = ctx.control;
            value = ctx.ReadValue<Vector2>();
        };

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(canceledControl, Is.Null);
        Assert.That(performedControl, Is.EqualTo(keyboard.aKey));
        Assert.That(value, Is.EqualTo(Vector2.left));
        performedControl = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.W));
        InputSystem.Update();

        Assert.That(canceledControl, Is.Null);
        Assert.That(performedControl, Is.EqualTo(keyboard.wKey));
        Assert.That(value, Is.EqualTo(Vector2.up + Vector2.left));
        performedControl = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();

        Assert.That(canceledControl, Is.Null);
        Assert.That(performedControl, Is.EqualTo(keyboard.aKey));
        Assert.That(value, Is.EqualTo(Vector2.up));
        performedControl = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        Assert.That(canceledControl, Is.EqualTo(keyboard.wKey));
        Assert.That(performedControl, Is.Null);
        Assert.That(value, Is.EqualTo(Vector2.zero));
        canceledControl = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
        InputSystem.Update();

        Assert.That(canceledControl, Is.Null);
        Assert.That(performedControl, Is.EqualTo(keyboard.rightArrowKey));
        Assert.That(value, Is.EqualTo(Vector2.right));
        performedControl = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        Assert.That(canceledControl, Is.EqualTo(keyboard.rightArrowKey));
        Assert.That(performedControl, Is.Null);
        Assert.That(value, Is.EqualTo(Vector2.zero));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CompositesReportControlThatTriggeredTheCompositeInCallback()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterInteraction<LogInteraction>();

        var action = new InputAction();
        action.AddBinding("<Gamepad>/leftStick");
        action.AddCompositeBinding("Dpad", interactions: "log")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.Enable();

        InputControl performedControl = null;
        action.performed += ctx => performedControl = ctx.control;

        // Interaction should be processed only once.
        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();

        Assert.That(performedControl, Is.EqualTo(keyboard.wKey));
        performedControl = null;

        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(performedControl, Is.EqualTo(keyboard.aKey));
        performedControl = null;

        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        LogAssert.Expect(LogType.Assert, "LogInteraction.Process");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
        InputSystem.Update();

        Assert.That(performedControl, Is.EqualTo(keyboard.sKey));

        LogAssert.NoUnexpectedReceived();
    }

    // https://fogbugz.unity3d.com/f/cases/1183314
    [Test]
    [Category("Actions")]
    public void Actions_CompositesInDifferentMapsTiedToSameControlsWork()
    {
        // This test relies on the same single input getting picked up by two different composites.
        InputSystem.settings.shortcutKeysConsumeInput = false;

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.RegisterInteraction<LogInteraction>();

        var map1 = new InputActionMap("map1");
        var action1 = map1.AddAction("action");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        var map2 = new InputActionMap("map2");
        var action2 = map2.AddAction("action");
        action2.AddBinding("<Gamepad>/leftStick");
        action2.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);
        asset.Enable();

        InputControl performedControl1 = null;
        InputControl performedControl2 = null;
        action1.performed += ctx => performedControl1 = ctx.control;
        action2.performed += ctx => performedControl2 = ctx.control;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(performedControl1, Is.EqualTo(keyboard.aKey));
        performedControl1 = null;
        Assert.That(performedControl2, Is.EqualTo(keyboard.aKey));
        performedControl2 = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.W));
        InputSystem.Update();

        Assert.That(performedControl1, Is.EqualTo(keyboard.wKey));
        performedControl1 = null;
        Assert.That(performedControl2, Is.EqualTo(keyboard.wKey));
        performedControl2 = null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        LogAssert.NoUnexpectedReceived();
    }

    private class CompositeWithVector2Part : InputBindingComposite<Vector2>
    {
        [InputControlAttribute(layout = "Vector2")]
        public int part;

        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            return context.ReadValue<Vector2, Vector2MagnitudeComparer>(part);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateCompositeWithVector2PartBinding()
    {
        InputSystem.RegisterBindingComposite<CompositeWithVector2Part>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("CompositeWithVector2Part")
            .With("part", "<Gamepad>/leftStick");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

            Assert.That(trace,
                Started(action, gamepad.leftStick, new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
                    .AndThen(Performed(action, gamepad.leftStick, new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))));
        }
    }

    private class CompositeAskingForSourceControl : InputBindingComposite<float>
    {
        [InputControl(layout = "Button")]
        public int button;

        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            var value = context.ReadValue<float>(button, out var control);
            Debug.Log(control.path);
            return value;
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanGetSourceControlWhenReadingValueFromCompositePart()
    {
        InputSystem.RegisterBindingComposite<CompositeAskingForSourceControl>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("CompositeAskingForSourceControl")
            .With("button", "<Gamepad>/buttonSouth")
            .With("button", "<Gamepad>/buttonNorth");
        action.Enable();

        // Need a callback to trigger reading.
        action.performed += ctx => ctx.ReadValue<float>();

        LogAssert.Expect(LogType.Log, gamepad.buttonNorth.path);

        Press(gamepad.buttonNorth);

        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateButtonWithOneModifierComposite()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.1f;

        // Using gamepad so we can use the triggers and make sure
        // that the composite preserves the full button value instead
        // of just going 0 and 1.
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(type: InputActionType.Button);
        action.AddCompositeBinding("ButtonWithOneModifier(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Gamepad>/leftTrigger")
            .With("Modifier", "<Gamepad>/dpad/up")
            .With("Button", "<Gamepad>/rightTrigger");

        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftTrigger, 0.5f);
            Assert.That(trace, Is.Empty);

            Set(gamepad.rightTrigger, 0.75f);
            Assert.That(trace,
                Started(action, value: 0.75f, control: gamepad.rightTrigger)
                    .AndThen(Performed(action, value: 0.75f, control: gamepad.rightTrigger)));

            trace.Clear();

            Set(gamepad.leftTrigger, 0);
            Assert.That(trace,
                Canceled(action, value: 0f, control: gamepad.leftTrigger));

            trace.Clear();

            Press(gamepad.dpad.up);
            Assert.That(trace,
                Started(action, value: 0.75f, control: gamepad.dpad.up)
                    .AndThen(Performed(action, value: 0.75f, control: gamepad.dpad.up)));

            trace.Clear();

            Set(gamepad.rightTrigger, 0);
            Assert.That(trace,
                Canceled(action, value: 0f, control: gamepad.rightTrigger));

            trace.Clear();

            Release(gamepad.dpad.up);
            Set(gamepad.rightTrigger, 0.456f);

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateButtonWithTwoModifiersComposite()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.1f;

        // Using gamepad so we can use the triggers and make sure
        // that the composite preserves the full button value instead
        // of just going 0 and 1.
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("ButtonWithTwoModifiers(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier1", "<Gamepad>/leftTrigger")
            .With("Modifier1", "<Gamepad>/dpad/up")
            .With("Modifier2", "<Gamepad>/rightTrigger")
            .With("Modifier2", "<Gamepad>/dpad/down")
            .With("Button", "<Gamepad>/leftStick/up");

        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftTrigger, 0.345f);
            Assert.That(trace, Is.Empty);

            Set(gamepad.rightTrigger, 0.456f);
            Assert.That(trace, Is.Empty);

            Set(gamepad.leftStick, new Vector2(0, 0.75f));
            Assert.That(trace,
                Started(action,
                    value: new AxisDeadzoneProcessor().Process(0.75f),
                    control: gamepad.leftStick.up)
                    .AndThen(Performed(action,
                    value: new AxisDeadzoneProcessor().Process(0.75f),
                    control: gamepad.leftStick.up)));

            trace.Clear();

            Press(gamepad.dpad.up);
            Set(gamepad.leftTrigger, 0);

            // Bit counter-intuitive but the composite yields a value every time
            // one of the constituents change.
            Assert.That(trace,
                Performed(action,
                    value: new AxisDeadzoneProcessor().Process(0.75f),
                    control: gamepad.dpad.up)
                    .AndThen(Performed(action,
                    value: new AxisDeadzoneProcessor().Process(0.75f),
                    control: gamepad.leftTrigger)));

            trace.Clear();

            Set(gamepad.rightTrigger, 0);

            Assert.That(trace,
                Canceled(action, value: 0f, control: gamepad.rightTrigger));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateBindingWithOneModifier()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Modifier", "<Keyboard>/ctrl")
            .With("Binding", "<Mouse>/position");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(mouse.position, new Vector2(123, 234));

            Assert.That(trace, Is.Empty);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(default(Vector2)));

            Press(keyboard.leftCtrlKey);

            Assert.That(trace,
                Started(action, value: new Vector2(123, 234))
                    .AndThen(Performed(action, value: new Vector2(123, 234))));

            trace.Clear();

            Set(mouse.position, new Vector2(234, 345));

            Assert.That(trace,
                Performed(action, value: new Vector2(234, 345)));

            trace.Clear();

            Release(keyboard.leftCtrlKey);

            Assert.That(trace,
                Canceled(action, value: Vector2.zero));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateBindingWithOneModifier_AndReadValueFromActionCallback()
    {
        InputSystem.AddDevice<Touchscreen>();

        var action = new InputAction();
        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Touchscreen>/press")
            .With("Binding", "<Touchscreen>/primaryTouch/position");

        var values = new List<Vector2>();
        action.started += ctx => values.Add(ctx.ReadValue<Vector2>());
        action.performed += ctx => values.Add(ctx.ReadValue<Vector2>());
        action.canceled += ctx => values.Add(ctx.ReadValue<Vector2>());

        action.Enable();

        BeginTouch(1, new Vector2(123, 234));

        Assert.That(values, Is.EquivalentTo(new[] { new Vector2(123, 234), new Vector2(123, 234) }));

        values.Clear();

        EndTouch(1, new Vector2(234, 345));

        Assert.That(values, Is.EquivalentTo(new[] { default(Vector2) }));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateBindingWithTwoModifiers()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddCompositeBinding("TwoModifiers")
            .With("Modifier1", "<Keyboard>/shift")
            .With("Modifier2", "<Keyboard>/ctrl")
            .With("Binding", "<Mouse>/position");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(mouse.position, new Vector2(123, 234));

            Assert.That(trace, Is.Empty);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(default(Vector2)));

            Press(keyboard.leftCtrlKey);

            Assert.That(trace, Is.Empty);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(default(Vector2)));

            Press(keyboard.rightShiftKey);

            Assert.That(trace,
                Started(action, value: new Vector2(123, 234))
                    .AndThen(Performed(action, value: new Vector2(123, 234))));

            trace.Clear();

            Set(mouse.position, new Vector2(234, 345));

            Assert.That(trace,
                Performed(action, value: new Vector2(234, 345)));

            trace.Clear();

            Release(keyboard.leftCtrlKey);

            Assert.That(trace,
                Canceled(action, value: Vector2.zero));

            trace.Clear();

            Release(keyboard.rightShiftKey);

            Assert.That(trace, Is.Empty);
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateVector3Composite()
    {
        // For simplicity's sake, give us a device with six analog buttons.
        const string layout = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""up"", ""layout"" : ""Button"", ""format"" : ""FLT"" },
                    { ""name"" : ""down"", ""layout"" : ""Button"", ""format"" : ""FLT"" },
                    { ""name"" : ""left"", ""layout"" : ""Button"", ""format"" : ""FLT"" },
                    { ""name"" : ""right"", ""layout"" : ""Button"", ""format"" : ""FLT"" },
                    { ""name"" : ""forward"", ""layout"" : ""Button"", ""format"" : ""FLT"" },
                    { ""name"" : ""backward"", ""layout"" : ""Button"", ""format"" : ""FLT"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestDevice");

        InputSystem.settings.defaultButtonPressPoint = 0.4f;

        var analog = new InputAction();
        var digitalNormalized = new InputAction();
        var digital = new InputAction();

        analog.AddCompositeBinding("3DVector(mode=0)")
            .With("up", "<TestDevice>/up")
            .With("down", "<TestDevice>/down")
            .With("left", "<TestDevice>/left")
            .With("right", "<TestDevice>/right")
            .With("forward", "<TestDevice>/forward")
            .With("backward", "<TestDevice>/backward");
        digitalNormalized.AddCompositeBinding("3DVector(mode=1)")
            .With("up", "<TestDevice>/up")
            .With("down", "<TestDevice>/down")
            .With("left", "<TestDevice>/left")
            .With("right", "<TestDevice>/right")
            .With("forward", "<TestDevice>/forward")
            .With("backward", "<TestDevice>/backward");
        digital.AddCompositeBinding("3DVector(mode=2)")
            .With("up", "<TestDevice>/up")
            .With("down", "<TestDevice>/down")
            .With("left", "<TestDevice>/left")
            .With("right", "<TestDevice>/right")
            .With("forward", "<TestDevice>/forward")
            .With("backward", "<TestDevice>/backward");

        analog.Enable();
        digitalNormalized.Enable();
        digital.Enable();

        // Below button press threshold.
        Set((ButtonControl)device["up"], 0.123f);
        Set((ButtonControl)device["left"], 0.234f);
        Set((ButtonControl)device["forward"], 0.345f);

        Assert.That(analog.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(-0.234f, 0.123f, 0.345f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(digitalNormalized.ReadValue<Vector3>(),
            Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));
        Assert.That(digital.ReadValue<Vector3>(),
            Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));

        Set((ButtonControl)device["up"], 0.456f);
        Set((ButtonControl)device["left"], 0.567f);
        Set((ButtonControl)device["forward"], 0.789f);

        Assert.That(analog.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(-0.567f, 0.456f, 0.789f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(digitalNormalized.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(-1, 1, 1).normalized).Using(Vector3EqualityComparer.Instance));
        Assert.That(digital.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(-1, 1, 1)).Using(Vector3EqualityComparer.Instance));

        Set((ButtonControl)device["down"], 0.890f);
        Set((ButtonControl)device["right"], 0.901f);
        Set((ButtonControl)device["backward"], 1f);

        Assert.That(analog.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(0.901f - 0.567f, 0.456f - 0.890f, 0.789f - 1f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(digitalNormalized.ReadValue<Vector3>(),
            Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));
        Assert.That(digital.ReadValue<Vector3>(),
            Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));

        Set((ButtonControl)device["up"], 0f);
        Set((ButtonControl)device["left"], 0f);
        Set((ButtonControl)device["forward"], 0f);

        Assert.That(analog.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(0.901f, -0.890f, -1f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(digitalNormalized.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(1, -1, -1).normalized).Using(Vector3EqualityComparer.Instance));
        Assert.That(digital.ReadValue<Vector3>(),
            Is.EqualTo(new Vector3(1, -1, -1)).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector3Composite_WithKeyboardKeys_CancelOnRelease()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up classic WASD control (with QE for forward / backward).
        var action = new InputAction();
        action.AddCompositeBinding("3DVector")
            .With("Forward", "<Keyboard>/q")
            .With("Backward", "<Keyboard>/e")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.Enable();

        bool wasCanceled = false;
        action.canceled += ctx => { wasCanceled = true; };

        // Test all directions to ensure they are correctly canceled
        var keys = new Key[] { Key.Q, Key.E, Key.W, Key.A, Key.S, Key.D };
        foreach (var key in keys)
        {
            wasCanceled = false;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();

            Assert.That(wasCanceled, Is.EqualTo(true));
        }
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
    public void Actions_OnActionWithMultipleBindings_CanTransitionFromOneActuatedControlToAnother()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var wasCanceled = false;
        var action = new InputAction("test", InputActionType.Button);
        action.canceled += _ => wasCanceled = true;
        action.AddBinding("<Keyboard>/s");
        action.AddBinding("<Keyboard>/a");
        action.Enable();

        Press(keyboard.sKey);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));

        Press(keyboard.aKey);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));

        Release(keyboard.sKey);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Performed));

        Release(keyboard.aKey);
        Assert.That(action.phase, Is.EqualTo(InputActionPhase.Waiting));
        Assert.That(wasCanceled, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_ControlWithHighestActuationIsTrackedAsActiveControl()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var buttonAction = new InputAction(type: InputActionType.Button,
            binding: "<Gamepad>/*Trigger");
        var passThroughAction = new InputAction(type: InputActionType.PassThrough,
            binding: "<Gamepad>/*Trigger");

        buttonAction.Enable();
        passThroughAction.Enable();

        Set(gamepad.leftTrigger, 1f);

        Assert.That(buttonAction.WasPerformedThisFrame(), Is.True);
        Assert.That(buttonAction.activeControl, Is.SameAs(gamepad.leftTrigger));
        Assert.That(passThroughAction.WasPerformedThisFrame(), Is.True);
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.leftTrigger));

        Set(gamepad.rightTrigger, 0.5f);

        Assert.That(buttonAction.WasPerformedThisFrame(), Is.False);
        Assert.That(buttonAction.activeControl, Is.SameAs(gamepad.leftTrigger));
        Assert.That(passThroughAction.WasPerformedThisFrame(), Is.True);
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.rightTrigger));

        Set(gamepad.leftTrigger,  0f);

        Assert.That(buttonAction.WasPerformedThisFrame(), Is.False);
        Assert.That(buttonAction.WasReleasedThisFrame(), Is.False);
        Assert.That(buttonAction.WasCompletedThisFrame(), Is.False);
        Assert.That(buttonAction.activeControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(passThroughAction.WasPerformedThisFrame(), Is.True);
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.leftTrigger));

        Set(gamepad.rightTrigger, 0.6f);

        Assert.That(buttonAction.WasPerformedThisFrame(), Is.False);
        Assert.That(buttonAction.activeControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(passThroughAction.WasPerformedThisFrame(), Is.True);
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.rightTrigger));

        Set(gamepad.rightTrigger, 0f);

        Assert.That(buttonAction.WasReleasedThisFrame(), Is.True);
        Assert.That(buttonAction.WasCompletedThisFrame(), Is.True);
        Assert.That(buttonAction.activeControl, Is.Null);
        Assert.That(passThroughAction.WasPerformedThisFrame(), Is.True);
        Assert.That(passThroughAction.activeControl, Is.SameAs(gamepad.rightTrigger));
    }

    // https://fogbugz.unity3d.com/f/cases/1267805/
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_InteractionIgnoringInput_DoesNotCauseDisambiguationToGetStuck()
    {
        // This test checks whether InputActionState.ShouldIgnoreStateChange() does
        // the right thing even if an interaction does not trigger any phase change
        // on an input. In that situation, we still need to update the action state
        // or the "disambiguation" code will end up looking at outdated data.

        InputSystem.settings.defaultTapTime = 1;
        InputSystem.settings.multiTapDelayTime = 1;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction(interactions: "multitap(tapCount=2)");
        action.AddBinding("<Keyboard>/space");
        action.AddBinding("<Keyboard>/a");
        action.Enable();

        currentTime = 1;
        Press(keyboard.spaceKey);
        currentTime = 1.5f;
        Release(keyboard.spaceKey);
        currentTime = 2;
        Press(keyboard.spaceKey);
        currentTime = 2.5f;
        Release(keyboard.spaceKey);

        Assert.That(action.WasPerformedThisFrame());
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
    public void Actions_ApplyingEmptyStringOverride_IsSameAsDisablingBinding()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");

        var performed = false;
        action.performed += _ => performed = true;

        action.Enable();

        Press(gamepad.leftTrigger);

        Assert.That(performed);
        performed = false;

        action.Disable();
        action.ApplyBindingOverride(0, "");
        action.Enable();

        Press(gamepad.leftTrigger);

        Assert.That(performed, Is.False);

        // We had a bug (case 1187163) where InputActionState would cause an exception by not
        // respecting the empty path when checking if a newly added device is affecting the state.
        // Just add a device here to make sure that's handled correctly.
        Assert.That(() => InputSystem.AddDevice<Gamepad>(), Throws.Nothing);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanApplyOverrideToActionWithEmptyBinding()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction();
        action.AddBinding("");

        var performed = false;
        action.performed += _ => performed = true;

        action.Enable();

        Press(gamepad.leftTrigger);

        Assert.That(performed, Is.False);

        action.Disable();
        action.ApplyBindingOverride(0, "/gamepad/leftTrigger");
        action.Enable();

        Press(gamepad.leftTrigger);

        Assert.That(performed);
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

    // We may want to perform a rebind on just one specific control scheme. For this, the rebinding
    // machinery allows specifying a binding mask to respect.
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
        var action1 = map.AddAction("action1", binding: "/<keyboard>/enter");
        var action2 = map.AddAction("action2", binding: "/<gamepad>/buttonSouth");
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

    // https://fogbugz.unity3d.com/f/cases/1213085 (bindings that refer to non-existent actions should not lead to errors)
    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableEntireMap_EvenWhenBindingsReferToNonExistentActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var map = new InputActionMap();
        map.AddAction("action", binding: "<Gamepad>/buttonSouth");
        map.AddBinding("<Gamepad>/buttonNorth", action: "DoesNotExist");

        // Also try the same for a composite binding.
        map.AddBinding(new InputBinding { path = "1DAxis", isComposite = true, action = "DoesNotExist" });
        map.AddBinding(new InputBinding { name = "Positive", path = "<Gamepad>/leftTrigger", isPartOfComposite = true });
        map.AddBinding(new InputBinding { name = "Negative", path = "<Gamepad>/rightTrigger", isPartOfComposite = true });

        Assert.That(() => map.Enable(), Throws.Nothing);

        Assert.That(() => Press(gamepad.buttonNorth), Throws.Nothing);
        Assert.That(() => Press(gamepad.leftTrigger), Throws.Nothing);
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
        var action = new InputAction(name: "action", type: InputActionType.PassThrough, interactions: "foo",
            processors: "bar", expectedControlType: "ctrltype");

        action.AddBinding("/gamepad/leftStick").WithInteraction("tap").WithGroup("group");
        action.AddBinding("/gamepad/rightStick");

        var clone = action.Clone();

        Assert.That(clone, Is.Not.SameAs(action));
        Assert.That(clone.name, Is.EqualTo(action.name));
        Assert.That(clone.id, Is.Not.EqualTo(action.id));
        Assert.That(clone.interactions, Is.EqualTo("foo"));
        Assert.That(clone.processors, Is.EqualTo("bar"));
        Assert.That(clone.expectedControlType, Is.EqualTo("ctrltype"));
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
        var action3 = map.AddAction("action", binding: "<Gamepad>/buttonSouth");

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
            Has.All.Matches(
                (InputManager.StateChangeMonitorsForDevice x) => x.memoryRegions.All(r => r.sizeInBits == 0)));
    }

    // https://fogbugz.unity3d.com/f/cases/1367442/
    [Test]
    [Category("Actions")]
    public void Actions_EnablingAndDisablingRepeatedly_DoesNotAllocate()
    {
        InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();

        // Warm up JIT.
        var actions = new DefaultInputActions();
        actions.Enable();
        actions.Disable();

        // Now for real.
        actions = new DefaultInputActions();

        actions.Enable();

        var kProfilerRegion1 = "Actions_EnablingAndDisablingRepeatedly_DoesNotAllocate_DISABLE";
        var kProfilerRegion2 = "Actions_EnablingAndDisablingRepeatedly_DoesNotAllocate_ENABLE";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion1);
            actions.Disable();
            Profiler.EndSample();
        },
            Is.Not.AllocatingGCMemory());

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion2);
            actions.Enable();
            Profiler.EndSample();
        },
            Is.Not.AllocatingGCMemory());
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
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
            Assert.That(actions[0].phase, Is.EqualTo(InputActionPhase.Canceled));
            Assert.That(actions[0].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
            Assert.That(actions[0].control, Is.SameAs(mouse.delta));
        }
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

        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM MAP.*"));
        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException while executing 'started' callbacks of 'testMap'"));

        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM ACTION.*"));
        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException while executing 'performed' callbacks of 'testMap/testAction.*'"));

        LogAssert.Expect(LogType.Exception, new Regex(".*TEST EXCEPTION FROM MAP.*"));
        LogAssert.Expect(LogType.Error,
            new Regex(
                ".*InvalidOperationException while executing 'performed' callbacks of 'testMap'"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
        InputSystem.Update();

        LogAssert.NoUnexpectedReceived();
    }

    class TestInteractionCheckingDefaultState : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            if (context.control.CheckStateIsAtDefault())
            {
                Debug.Log("TestInteractionCheckingDefaultState.Process(default)");
                Assert.That(context.control.ReadValueAsObject(), Is.EqualTo(new AxisDeadzoneProcessor().Process(0.1234f)).Within(0.00001));
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

        LogAssert.NoUnexpectedReceived();
    }

    // It's possible to associate a control layout name with an action. This is useful both for
    // characterizing the expected input behavior as well as to make control picking (both at
    // edit time and in the game) easier.
    [Test]
    [Category("Actions")]
    public void Actions_CanHaveExpectedControlLayout()
    {
        var action = new InputAction();

        Assert.That(action.expectedControlType, Is.Null);

        action.expectedControlType = "Button";

        Assert.That(action.expectedControlType, Is.EqualTo("Button"));
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
    public void Actions_CanUseTouchWithActions()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        var primaryTouchAction = new InputAction("PrimaryTouch" , binding: "<Touchscreen>/primaryTouch/position");
        var touch0Action = new InputAction("Touch0", binding: "<Touchscreen>/touch0/position");
        var touch1Action = new InputAction("Touch1", binding: "<Touchscreen>/touch1/position");
        var positionAction = new InputAction("Position", binding: "<Touchscreen>/position");
        var tapAction = new InputAction("Tap", binding: "<Touchscreen>/primaryTouch/tap");

        Assert.That(primaryTouchAction.controls, Is.EquivalentTo(new[] { touchscreen.primaryTouch.position }));
        Assert.That(touch0Action.controls, Is.EquivalentTo(new[] { touchscreen.touches[0].position }));
        Assert.That(touch1Action.controls, Is.EquivalentTo(new[] { touchscreen.touches[1].position }));
        Assert.That(positionAction.controls, Is.EquivalentTo(new[] { touchscreen.position }));
        Assert.That(tapAction.controls, Is.EquivalentTo(new[] { touchscreen.primaryTouch.tap }));

        primaryTouchAction.Enable();
        touch0Action.Enable();
        touch1Action.Enable();
        positionAction.Enable();
        tapAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            // Begin primary touch.
            BeginTouch(1, new Vector2(0.123f, 0.234f));

            Assert.That(trace,
                Started(primaryTouchAction, touchscreen.primaryTouch.position, new Vector2(0.123f, 0.234f))
                    .AndThen(Performed(primaryTouchAction, touchscreen.primaryTouch.position, new Vector2(0.123f, 0.234f)))
                    .AndThen(Started(positionAction, touchscreen.position, new Vector2(0.123f, 0.234f)))
                    .AndThen(Performed(positionAction, touchscreen.position, new Vector2(0.123f, 0.234f)))
                    .AndThen(Started(touch0Action, touchscreen.touches[0].position, new Vector2(0.123f, 0.234f)))
                    .AndThen(Performed(touch0Action, touchscreen.touches[0].position, new Vector2(0.123f, 0.234f))));

            trace.Clear();

            // Begin secondary touch.
            BeginTouch(2, new Vector2(0.234f, 0.345f));

            Assert.That(trace,
                Started(touch1Action, touchscreen.touches[1].position, new Vector2(0.234f, 0.345f))
                    .AndThen(Performed(touch1Action, touchscreen.touches[1].position, new Vector2(0.234f, 0.345f))));

            trace.Clear();

            // End primary touch.
            EndTouch(1, new Vector2(0.345f, 0.456f));

            Assert.That(trace,
                Performed(primaryTouchAction, touchscreen.primaryTouch.position, new Vector2(0.345f, 0.456f))
                    .AndThen(Performed(positionAction, touchscreen.position, new Vector2(0.345f, 0.456f)))
                    .AndThen(Performed(touch0Action, touchscreen.touches[0].position, new Vector2(0.345f, 0.456f))));

            trace.Clear();

            // End secondary touch.
            EndTouch(2, new Vector2(0.234f, 0.345f));

            Assert.That(trace, Is.Empty);
        }
    }

    // Mouse, Pen, and Touchscreen are meant to all be able to function as a Pointer. While there a slight differences
    // in how the devices support pointer-style interactions, it should be possible to bind an action using the Pointer
    // abstraction and get consistent behavior out of all three types of devices.
    [Test]
    [Category("Actions")]
    public void Actions_CanDrivePointerInputFromTouchPenAndMouse()
    {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Exclude project-wide actions from this test
        InputSystem.actions?.Disable(); // Prevent these actions appearing in the `InputActionTrace`
#endif

        // Give us known parameters for tap detection.
        InputSystem.settings.defaultTapTime = 0.5f;
        InputSystem.settings.tapRadius = 5;

        var touchscreen = InputSystem.AddDevice<Touchscreen>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();

        var positionAction = new InputAction("Position", binding: "<Pointer>/position");
        var pressAction = new InputAction("Press", binding: "<Pointer>/press");
        var primaryAction = new InputAction("Primary", binding: "<Pointer>/{PrimaryAction}");
        var deltaAction = new InputAction("Delta", binding: "<Pointer>/delta");
        var pressureAction = new InputAction("Pressure", binding: "<Pointer>/pressure");
        var radiusAction = new InputAction("Radius", binding: "<Pointer>/radius");

        // Each of the bindings should match exactly one control from each device.
        Assert.That(new[] { positionAction, pressAction, primaryAction, deltaAction, pressureAction, radiusAction },
            Has.All.Property("controls").Count.EqualTo(3)
                .And.All.Property("controls").Exactly(1).Property("device").SameAs(touchscreen)
                .And.All.Property("controls").Exactly(1).Property("device").SameAs(mouse)
                .And.All.Property("controls").Exactly(1).Property("device").SameAs(pen));

        positionAction.Enable();
        pressAction.Enable();
        primaryAction.Enable();
        deltaAction.Enable();
        pressureAction.Enable();
        radiusAction.Enable();

        using (var trace = new InputActionTrace())
        {
            trace.SubscribeToAll();

            // Perform mouse move and click.
            Move(mouse.position, new Vector2(0.123f, 0.234f), time: 0.1);
            Click(mouse.leftButton, time: 0.2);

            Assert.That(trace,
                Started(positionAction, mouse.position, new Vector2(0.123f, 0.234f), time: 0.1)
                    .AndThen(Performed(positionAction, mouse.position, new Vector2(0.123f, 0.234f), time: 0.1))
                    .AndThen(Started(deltaAction, mouse.delta, new Vector2(0.123f, 0.234f), time: 0.1))
                    .AndThen(Performed(deltaAction, mouse.delta, new Vector2(0.123f, 0.234f), time: 0.1))
                    // Update in-between Move() and Click() resets delta.
                    .AndThen(Canceled(deltaAction, mouse.delta, Vector2.zero))
                    .AndThen(Started(pressAction, mouse.press, 1, time: 0.2))
                    .AndThen(Performed(pressAction, mouse.press, 1, time: 0.2))
                    .AndThen(Started(primaryAction, mouse.leftButton, 1, time: 0.2))
                    .AndThen(Performed(primaryAction, mouse.leftButton, 1, time: 0.2))
                    .AndThen(Canceled(pressAction, mouse.press, 0, time: 0.2))
                    .AndThen(Canceled(primaryAction, mouse.leftButton, 0, time: 0.2)));

            trace.Clear();

            // Perform pen move and click.
            Move(pen.position, new Vector2(0.234f, 0.345f), time: 0.2);
            Click(pen.tip, time: 0.3);

            Assert.That(trace,
                // Position action is already started.
                Performed(positionAction, pen.position, new Vector2(0.234f, 0.345f), time: 0.2)
                    .AndThen(Started(deltaAction, pen.delta, new Vector2(0.234f, 0.345f), time: 0.2))
                    .AndThen(Performed(deltaAction, pen.delta, new Vector2(0.234f, 0.345f), time: 0.2))
                    .AndThen(Canceled(deltaAction, pen.delta, Vector2.zero))
                    .AndThen(Started(pressAction, pen.press, 1, time: 0.3))
                    .AndThen(Performed(pressAction, pen.press, 1, time: 0.3))
                    .AndThen(Started(primaryAction, pen.tip, 1, time: 0.3))
                    .AndThen(Performed(primaryAction, pen.tip, 1, time: 0.3))
                    .AndThen(Canceled(pressAction, pen.press, 0, time: 0.3))
                    .AndThen(Canceled(primaryAction, pen.tip, 0, time: 0.3)));

            trace.Clear();

            // Perform touch move.
            BeginTouch(1, new Vector2(1, 2), time: 0.3, queueEventOnly: true); // Spare us one extra delta reset.
            MoveTouch(1, new Vector2(10, 20), time: 0.4, queueEventOnly: true);  // Same here.
            EndTouch(1, new Vector2(10, 20), time: 0.5, queueEventOnly: true); // Also releases press.
            InputSystem.Update();
            InputSystem.Update(); // Reset delta.

            Assert.That(trace,
                Performed(positionAction, touchscreen.position, new Vector2(1, 2), time: 0.3)
                    .AndThen(Started(pressAction, touchscreen.press, 1, time: 0.3))
                    .AndThen(Performed(pressAction, touchscreen.press, 1, time: 0.3))
                    .AndThen(Performed(positionAction, touchscreen.position, new Vector2(10, 20), time: 0.4))
                    .AndThen(Performed(pressAction, touchscreen.press, 1, time: 0.4)) // Because `press` is tied to `phase` which changes state twice (but not value).
                    .AndThen(Started(deltaAction, touchscreen.delta, new Vector2(9, 18), time: 0.4))
                    .AndThen(Performed(deltaAction, touchscreen.delta, new Vector2(9, 18), time: 0.4))
                    .AndThen(Canceled(pressAction, touchscreen.press, 0, time: 0.5))
                    .AndThen(Canceled(deltaAction, touchscreen.delta, Vector2.zero)));

            trace.Clear();

            // Perform touch tap.
            BeginTouch(1, new Vector2(10, 20), time: 0.5);
            EndTouch(1, new Vector2(10, 20), time: 0.5);

            Assert.That(trace,
                // No performed on positionAction has we've kept the position in place.
                Started(pressAction, touchscreen.press, 1, 0.5)
                    .AndThen(Performed(pressAction, touchscreen.press, 1, 0.5))
                    .AndThen(Canceled(pressAction, touchscreen.press, 0, 0.5))
                    .AndThen(Started(primaryAction, touchscreen.primaryTouch.tap, 1, 0.5))
                    .AndThen(Performed(primaryAction, touchscreen.primaryTouch.tap, 1, 0.5))
                    .AndThen(Canceled(primaryAction, touchscreen.primaryTouch.tap, 0, 0.5)));

            trace.Clear();

            // Perform concurrent move with mouse and pen.
            Move(mouse.position, new Vector2(100, 200), delta: Vector2.zero, time: 0.6);
            Move(pen.position, new Vector2(300, 400), delta: Vector2.zero, time: 0.7);

            Assert.That(trace,
                Performed(positionAction, mouse.position, new Vector2(100, 200), time: 0.6)
                    .AndThen(Performed(positionAction, pen.position, new Vector2(300, 400), time: 0.7)));
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_AxisControlWithoutLimitsCanTriggerActionsWithMultipleBindings()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    { ""name"" : ""SingleAxis"", ""layout"" : ""Analog"", ""format"" : ""FLT"" }
                ]
            }
        ";

        // Create base device with unclamped axis
        InputSystem.RegisterLayout(json);
        InputDevice device = InputSystem.AddDevice("TestLayout");
        AxisControl singleAxis = device["SingleAxis"] as AxisControl;

        // Add a second device to create 2 bindings
        InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "TestLayout/SingleAxis");
        action.AddBinding("<Gamepad>/buttonSouth");

        int performedCallCount = 0;
        float lastPerformedValue = 0.0f;
        action.performed += ctx =>
        {
            performedCallCount++;
            lastPerformedValue = ctx.ReadValue<float>();
        };

        action.Enable();

        // Assert there are multiple bindings with multiple controls
        // This triggers conflict resolution.
        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Count.EqualTo(2));

        InputSystem.Update();

        // Set Initial Value to start action.
        Set(singleAxis, 0.123f);
        Assert.That(performedCallCount, Is.EqualTo(1));
        Assert.That(lastPerformedValue, Is.EqualTo(0.123f));

        // Update action to new peformed value
        Set(singleAxis, 0.456f);
        Assert.That(performedCallCount, Is.EqualTo(2));
        Assert.That(lastPerformedValue, Is.EqualTo(0.456f));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanResetAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        var action = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
        action.Enable();

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftStick, new Vector2(0.2f, 0.3f));

            Assert.That(action.inProgress, Is.True);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.2f, 0.3f)));
            Assert.That(trace,
                Started(action, value: new Vector2(0.2f, 0.3f))
                    .AndThen(Performed(action, value: new Vector2(0.2f, 0.3f))));

            trace.Clear();

            action.Reset();

            Assert.That(action.inProgress, Is.False);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(default(Vector2)));
            Assert.That(trace,
                Canceled(action, value: default(Vector2)));

            trace.Clear();

            Set(gamepad.leftStick, new Vector2(0.3f, 0.4f));

            Assert.That(action.inProgress, Is.True);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.3f, 0.4f)));
            Assert.That(trace,
                Started(action, value: new Vector2(0.3f, 0.4f))
                    .AndThen(Performed(action, value: new Vector2(0.3f, 0.4f))));

            trace.Clear();

            // Reset and then go through disable and enable cycle to make sure
            // that the action comes back up just fine.
            action.Reset();
            action.Disable();
            action.Enable();

            // Initial state check is performed in next update.
            InputSystem.Update();

            Assert.That(action.inProgress, Is.True);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.3f, 0.4f)));
            Assert.That(trace,
                Canceled(action, value: default(Vector2))
                    .AndThen(Started(action, value: new Vector2(0.3f, 0.4f)))
                    .AndThen(Performed(action, value: new Vector2(0.3f, 0.4f))));
        }
    }

    // Corresponds to bug report ticket 1370732.
    [Test]
    [Category("Actions")]
    public void Actions_ResetShouldPreserveEnabledState__IfResetWhileInDisabledState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        var action = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
        action.Enable();
        Assert.That(action.enabled, Is.True);

        action.Disable();
        Assert.That(action.enabled, Is.False);

        action.Reset();
        Assert.That(action.enabled, Is.False);

        action.Enable();
        Assert.That(action.enabled, Is.True);

        using (var trace = new InputActionTrace(action))
        {
            Set(gamepad.leftStick, new Vector2(0.2f, 0.3f));

            Assert.That(action.inProgress, Is.True);
            Assert.That(action.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0.2f, 0.3f)));
            Assert.That(trace,
                Started(action, value: new Vector2(0.2f, 0.3f))
                    .AndThen(Performed(action, value: new Vector2(0.2f, 0.3f))));
        }
    }

    private class MonoBehaviourWithActionProperty : MonoBehaviour
    {
        public InputActionProperty actionProperty;
    }

    [Test]
    [Category("Actions")]
    public void Actions_Property_CanGetAction_WithNullReferenceType()
    {
        var go = new GameObject();
        var component = go.AddComponent<MonoBehaviourWithActionProperty>();
        component.actionProperty = new InputActionProperty((InputActionReference)null);

        Assert.DoesNotThrow(() => _ = component.actionProperty.action);
        Assert.That(component.actionProperty.action, Is.Null);

        Assert.DoesNotThrow(() => component.actionProperty.GetHashCode());
    }

    [Test]
    [Category("Actions")]
    public void Actions_Property_CanGetAction_WithNullActionType()
    {
        var go = new GameObject();
        var component = go.AddComponent<MonoBehaviourWithActionProperty>();
        component.actionProperty = new InputActionProperty((InputAction)null);

        Assert.DoesNotThrow(() => _ = component.actionProperty.action);
        Assert.That(component.actionProperty.action, Is.Null);

        Assert.DoesNotThrow(() => component.actionProperty.GetHashCode());
    }

    [Test]
    [Category("Actions")]
    public void Actions_Property_CanGetAction_WithDestroyedReferenceType()
    {
        var map = new InputActionMap("map");
        map.AddAction("action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map", "action1");

        var go = new GameObject();
        var component = go.AddComponent<MonoBehaviourWithActionProperty>();
        component.actionProperty = new InputActionProperty(reference);

        Assert.That(component.actionProperty.action, Is.Not.Null);

        UnityEngine.Object.DestroyImmediate(reference);

        Assert.DoesNotThrow(() => _ = component.actionProperty.action);
        Assert.That(component.actionProperty.action, Is.Null);

        Assert.DoesNotThrow(() => component.actionProperty.GetHashCode());
    }

    [Test]
    [Category("Actions")]
    public void Actions_AddingActionToAssetWithEnabledActions_ThrowsException()
    {
        var map1 = new InputActionMap("map1");
        map1.AddAction(name: "action1", binding: "<Gamepad>/leftStick");

        var map2 = new InputActionMap("map2");
        map2.AddAction(name: "action2", binding: "<Gamepad>/rightStick");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        // toggle enable so we get a similar state as user would when they're trying to add an action in runtime
        asset.Enable();
        asset.Disable();

        // adding an action to a map when asset is disable works
        map1.AddAction("action3");
        Assert.That(map1.actions, Has.Count.EqualTo(2));
        Assert.That(map1.actions[1].name, Is.EqualTo("action3"));

        // enable action, but disable first map
        asset.Enable();
        map1.Disable();

        // adding an action now should fail with a descriptive exception
        Assert.That(() => map1.AddAction("action4"),
            Throws.InvalidOperationException);
        Assert.That(map1.actions, Has.Count.EqualTo(2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_RebindingCandidatesShouldBeSorted_IfAddingNewCandidate()
    {
        // Designed to trigger issue reported as part of:
        // https://github.com/Unity-Technologies/InputSystem/pull/1359

        using (var rebind = new InputActionRebindingExtensions.RebindingOperation())
        {
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad1"), 2.0f, 10.0f);
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad2"), 3.0f, 8.0f);
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad3"), 1.0f, 22.0f);
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad4"), 1.5f, 35.0f);
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad5"), 0.1f, 40.0f);
            rebind.AddCandidate(InputSystem.AddDevice<Gamepad>("gamepad6"), 8.0f, 80.0f);

            // Expecting scores in descending order
            var scores = rebind.scores;
            Assert.AreEqual(6, scores.Count);
            Assert.AreEqual(8.0f, scores[0]);
            Assert.AreEqual(3.0f, scores[1]);
            Assert.AreEqual(2.0f, scores[2]);
            Assert.AreEqual(1.5f, scores[3]);
            Assert.AreEqual(1.0f, scores[4]);
            Assert.AreEqual(0.1f, scores[5]);

            // Expecting magnitudes sorted based on descending score as well
            var magnitudes = rebind.magnitudes;
            Assert.AreEqual(6, magnitudes.Count);
            Assert.AreEqual(80.0f, magnitudes[0]);
            Assert.AreEqual(8.0f, magnitudes[1]);
            Assert.AreEqual(10.0f, magnitudes[2]);
            Assert.AreEqual(35.0f, magnitudes[3]);
            Assert.AreEqual(22.0f, magnitudes[4]);
            Assert.AreEqual(40.0f, magnitudes[5]);
        }
    }

    // Straight from demo project
    public struct PointerInput
    {
        public bool Contact;
        public int InputId;
        public Vector2 Position;
        public Vector2? Tilt;
        public float? Pressure;
        public Vector2? Radius;
        public float? Twist;
    }

    public class PointerInputComposite : InputBindingComposite<PointerInput>
    {
        [InputControl(layout = "Button")]
        public int contact;

        [InputControl(layout = "Vector2")]
        public int position;

        [InputControl(layout = "Vector2")]
        public int tilt;

        [InputControl(layout = "Vector2")]
        public int radius;

        [InputControl(layout = "Axis")]
        public int pressure;

        [InputControl(layout = "Axis")]
        public int twist;

        [InputControl(layout = "Integer")]
        public int inputId;

        public override PointerInput ReadValue(ref InputBindingCompositeContext context)
        {
            var contact = context.ReadValueAsButton(this.contact);
            var pointerId = context.ReadValue<int>(inputId);
            var pressure = context.ReadValue<float>(this.pressure);
            var radius = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.radius);
            var tilt = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.tilt);
            var position = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.position);
            var twist = context.ReadValue<float>(this.twist);

            return new PointerInput
            {
                Contact = contact,
                InputId = pointerId,
                Position = position,
                Tilt = tilt != default ? tilt : (Vector2?)null,
                Pressure = pressure > 0 ? pressure : (float?)null,
                Radius = radius.sqrMagnitude > 0 ? radius : (Vector2?)null,
                Twist = twist > 0 ? twist : (float?)null,
            };
        }
    }

    // https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-98
    [Test]
    [Category("Actions")]
    [TestCase(true)]
    [TestCase(false)]
    public void Actions_WithMultipleCompositeBindings_WithoutEvaluateMagnitude_Works(bool prepopulateTouchesBeforeEnablingAction)
    {
        InputSystem.RegisterBindingComposite<PointerInputComposite>();

        InputSystem.AddDevice<Touchscreen>();

        var actionMap = new InputActionMap("test");
        var action = actionMap.AddAction("point", InputActionType.Value);
        for (var i = 0; i < 5; ++i)
            action.AddCompositeBinding("PointerInput")
                .With("contact", $"<Touchscreen>/touch{i}/press")
                .With("position", $"<Touchscreen>/touch{i}/position")
                .With("radius", $"<Touchscreen>/touch{i}/radius")
                .With("pressure", $"<Touchscreen>/touch{i}/pressure")
                .With("inputId", $"<Touchscreen>/touch{i}/touchId");

        var values = new List<PointerInput>();
        action.started += ctx => values.Add(ctx.ReadValue<PointerInput>());
        action.performed += ctx => values.Add(ctx.ReadValue<PointerInput>());
        action.canceled += ctx => values.Add(ctx.ReadValue<PointerInput>());

        if (!prepopulateTouchesBeforeEnablingAction) // normally actions are enabled before any control actuations happen
            actionMap.Enable();

        // Start 5 touches, so we fill all slots [touch0, touch4] in Touchscreen with some valid touchId
        for (var i = 0; i < 2; ++i)
            BeginTouch(100 + i, new Vector2(100 * (i + 1), 100 * (i + 1)));
        for (var i = 0; i < 2; ++i)
            EndTouch(100 + i, new Vector2(100 * (i + 1), 100 * (i + 1)));
        Assert.That(values.Count, Is.EqualTo(prepopulateTouchesBeforeEnablingAction ? 0 : 3));
        values.Clear();

        // Now when enabling actionMap ..
        actionMap.Enable();
        // On the following update we will trigger OnBeforeUpdate which will rise started/performed
        // from InputActionState.OnBeforeInitialUpdate as controls are "actuated"
        InputSystem.Update();
        Assert.That(values.Count, Is.EqualTo(prepopulateTouchesBeforeEnablingAction ? 2 : 0)); // started+performed arrive from OnBeforeUpdate
        values.Clear();

        // Now subsequent touches should not be ignored
        BeginTouch(200, new Vector2(1, 1));
        Assert.That(values.Count, Is.EqualTo(1));
        Assert.That(values[0].InputId, Is.EqualTo(200));
        Assert.That(values[0].Position, Is.EqualTo(new Vector2(1, 1)));
    }

    // FIX: This test is currently checking if shortcut support is enabled by testing that the unwanted behaviour exists.
    // This test should be repurposed once that behaviour is fixed.
    [Test]
    [Category("Actions")]
    [TestCase(true)]
    [TestCase(false)]
    public void Actions_ImprovedShortcutSupport_ConsumesWASD(bool shortcutsEnabled)
    {
        InputSystem.settings.shortcutKeysConsumeInput = shortcutsEnabled;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map1 = new InputActionMap("map1");
        var action1 = map1.AddAction(name: "action1");
        action1.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        var map2 = new InputActionMap("map2");
        var action2 = map2.AddAction(name: "action2");
        action2.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        var action3 = map2.AddAction(name: "action3", binding: "<Keyboard>/w");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        map1.Enable();
        LogAssert.NoUnexpectedReceived();

        map2.Enable();

        int action1Count = 0;
        int action2Count = 0;
        int action3Count = 0;
        action1.started += ctx => action1Count++;
        action2.started += ctx => action2Count++;
        action3.started += ctx => action3Count++;

        Press(keyboard.wKey);
        if (shortcutsEnabled)
        {
            // First action with the most bindings is the ONLY one to trigger
            Assert.That(action1Count, Is.EqualTo(1));
            Assert.That(action2Count, Is.EqualTo(0));
            Assert.That(action3Count, Is.EqualTo(0));
        }
        else
        {
            // All actions were triggered
            Assert.That(action1Count, Is.EqualTo(1));
            Assert.That(action2Count, Is.EqualTo(1));
            Assert.That(action3Count, Is.EqualTo(1));
        }
    }

    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_ReResolvingBindings_DoesNotAllocate_IfXXX()
    {
        Assert.Fail();
    }

    // Validate OnAfterDeserialize() completely disables ActionMap
    // https://jira.unity3d.com/browse/ISXB-737
    [Test]
    [Category("Actions")]
    public void Actions_ActionMapDisabledDuringOnAfterSerialization()
    {
        var map = new InputActionMap("MyMap");
        var action = map.AddAction("MyAction");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        map.Enable();

        Assert.That(map.enabled, Is.True);
        Assert.That(map.FindAction("MyAction", true).enabled, Is.True);
        Assert.Throws<System.InvalidOperationException>(() => map.AddAction("Something"));

        // Calling this directly is contrived (and not supported usage) but it should
        // effectively cover this regression scenario.
        map.OnAfterDeserialize();

        Assert.That(map.enabled, Is.False);

        map.Enable();

        Assert.That(map.enabled, Is.True);
        Assert.That(map.FindAction("MyAction", true).enabled, Is.True);
    }

    // ResetDevice wasn't properly clearly Composite key state, i.e. BindingState.pressTime
    // https://jira.unity3d.com/browse/ISXB-746
    [Test]
    [TestCase(false)]
    [TestCase(true)]
    [Category("Actions")]
    public void Actions_CompositeBindingResetWhenResetDeviceCalledWhileExecutingAction(bool useTwoModifierComposite)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        bool actionPerformed;

        // Enables "Modifier must be pressed first" behavior on all Composite Bindings
        InputSystem.settings.shortcutKeysConsumeInput = true;

        const string modifier1 = "<Keyboard>/shift";
        const string modifier2 = "<Keyboard>/ctrl";
        const string key = "<Keyboard>/F1";

        var map = new InputActionMap();
        var resetAction = map.AddAction("resetAction");

        if (!useTwoModifierComposite)
        {
            resetAction.AddCompositeBinding("OneModifier")
                .With("Modifier", modifier1)
                .With("Binding", key);
        }
        else
        {
            resetAction.AddCompositeBinding("TwoModifiers")
                .With("Modifier1", modifier1)
                .With("Modifier2", modifier2)
                .With("Binding", key);
        }

        resetAction.performed += (InputAction.CallbackContext ctx) =>
        {
            // Disable the Keyboard while action is being performed.
            // This simulates an "OnFocusLost" event occurring while processing the Action, e.g. when switching primary displays or moving the main window
            actionPerformed = true;
            InputSystem.s_Manager.EnableOrDisableDevice(keyboard.device, false, InputManager.DeviceDisableScope.TemporaryWhilePlayerIsInBackground);
        };

        map.Enable();

        actionPerformed = false;
        Press(keyboard.leftShiftKey);
        Press(keyboard.leftCtrlKey);
        Press(keyboard.f1Key);

        Assert.IsTrue(actionPerformed);

        // Re enable the Keyboard (before keys are released) and execute Action again
        InputSystem.s_Manager.EnableOrDisableDevice(keyboard.device, true, InputManager.DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

        actionPerformed = false;
        Release(keyboard.leftShiftKey);
        Release(keyboard.leftCtrlKey);
        Release(keyboard.f1Key);

        Press(keyboard.leftCtrlKey);
        Press(keyboard.leftShiftKey);
        Press(keyboard.f1Key);

        Assert.IsTrue(actionPerformed);

        actionPerformed = false;
        Release(keyboard.leftCtrlKey);
        Release(keyboard.leftShiftKey);
        Release(keyboard.f1Key);

        // Re enable the Keyboard (after keys are released) and execute Action one more time
        InputSystem.s_Manager.EnableOrDisableDevice(keyboard.device, true, InputManager.DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

        Press(keyboard.leftCtrlKey);
        Press(keyboard.leftShiftKey);
        Press(keyboard.f1Key);

        Assert.IsTrue(actionPerformed);

        actionPerformed = false;
        Press(keyboard.leftShiftKey);
        Press(keyboard.leftCtrlKey);
        Press(keyboard.f1Key);

        // Re enable the Keyboard (before keys are released) and verify Action isn't triggered when Key pressed first
        InputSystem.s_Manager.EnableOrDisableDevice(keyboard.device, true, InputManager.DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

        Press(keyboard.f1Key);
        Press(keyboard.leftCtrlKey);
        Press(keyboard.leftShiftKey);

        Assert.IsFalse(actionPerformed);
    }
}
