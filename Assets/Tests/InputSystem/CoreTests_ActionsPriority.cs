using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

internal static class PriorityTestExtensions
{
    internal static InputAction SetupTestAction(this InputActionMap map, string binding)
    {
        // just a typical binding
        var action = map.AddAction("Action1:" + binding  + " " + Guid.NewGuid());
        action.AddBinding("<Keyboard>/" + binding);
        return action;
    }

    internal static InputAction SetupTestAction(this InputActionMap map,  string modifier1,  string binding)
    {
        // A shortcut with one modifier
        var action = map.AddAction("Action2:" + modifier1 + " " + binding + " " + Guid.NewGuid());

        action.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/" + modifier1)
            .With("Binding", "<Keyboard>/" + binding);

        return action;
    }

    internal static InputAction SetupTestAction(this InputActionMap map, string modifier1, string modifier2, string binding)
    {
        var action = map.AddAction("Action3:"  + modifier1 + " " + modifier2 + " " + binding +  " " + Guid.NewGuid());

        // A shortcut with two modifiers
        action.AddCompositeBinding("TwoModifiers")
            .With("Modifier1", "<Keyboard>/" + modifier1)
            .With("Modifier2", "<Keyboard>/" + modifier2)
            .With("Binding", "<Keyboard>/" + binding);

        return action;
    }
}

internal partial class CoreTests
{
    private static IEnumerable<TwoInputActionDataWrapper<InputAction, InputAction>> TwoInputActionTestCases()
    {
        InputActionMap map = new InputActionMap("map");
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 =  map.SetupTestAction("ctrl", "x"),
            Action2 = map.SetupTestAction("x")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("shift", "n"),
            Action2 = map.SetupTestAction("n")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("shift", "h"),
            Action2 = map.SetupTestAction("shift", "ctrl", "h")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("ctrl", "shift", "v"),
            Action2 = map.SetupTestAction("shift", "v")
        };
    }

    public class TwoInputActionDataWrapper<TInputAction1, TInputAction2>
    {
        public TInputAction1 Action1;
        public TInputAction2 Action2;
    }

    private void PressBindingsForInputActions(Keyboard keyboard, InputAction action1, InputAction action2)
    {
        for (int i = 0; i < action1.controls.Count; i++)
        {
            Debug.Log("action 1 binding pressed: " + action1.controls[i].path);
            Press((ButtonControl)keyboard[action1.controls[i].name], queueEventOnly: true);
        }

        for (int i = 0; i < action2.controls.Count; i++)
        {
            Debug.Log("action 2 binding pressed: " + action2.controls[i].name);
            Press((ButtonControl)keyboard[action2.controls[i].name], queueEventOnly: true);
        }

        InputSystem.Update();
    }

    private void ReleaseBindingsForActions(Keyboard keyboard, InputAction action1, InputAction action2)
    {
        // Cleanup key presses
        for (int i = 0; i < action1.controls.Count; i++)
        {
            Release((ButtonControl)keyboard[action1.controls[i].name], queueEventOnly: true);
        }

        for (int i = 0; i < action2.controls.Count; i++)
        {
            Release((ButtonControl)keyboard[action2.controls[i].name], queueEventOnly: true);
        }

        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_OnlyOneActionIsPerformed_WhenOnePriorityIsHigherThanOther(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        //var map = new InputActionMap("map");
        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        // action 1's priority higher so it takes precedence
        action1.Priority = 2;
        action2.Priority = 1;

        action1.m_ActionMap.Enable();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // action1 is performed because action1 has a higher priority than action2.
        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        // Cleanup key presses
        ReleaseBindingsForActions(keyboard, action1, action2);

        // Update again to be sure released is true.
        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_OnlyOneActionIsPerformed_WhenOnePriorityIsHigherThanOtherInversePriorityOrder(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        //var map = new InputActionMap("map");
        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        // action 1's priority higher so it takes precedence
        action1.Priority = 1;
        action2.Priority = 2;

        action1.m_ActionMap.Enable();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // action2 is performed because action1 has a higher priority than action2.
        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        ReleaseBindingsForActions(keyboard, action1, action2);

        // Update again to be sure released is true.
        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_FirstActionFires_WhenPriorityIsEqual(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions) // TODO: This shouldn't be the case. This should fire both!!
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 5;
        action2.Priority = 5;

        action1.m_ActionMap.Enable();

        PressBindingsForInputActions(keyboard, action1, action2);

        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_BothActionsFire_WhenPriorityIsZero(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 0;
        action2.Priority = 0;

        action1.m_ActionMap.Enable();

        var action1WasPerformed = false;
        var action2WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;
        action2.performed += _ => action2WasPerformed = true;

        PressBindingsForInputActions(keyboard, action1, action2);

        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2WasPerformed, Is.True);
    }

    [Test] // TODO: Darren, Is this what we want in the future? Possibly not after talking to Hakan, we shouldn't disable priority with settings.
    [Category("Actions Priority")]
    [TestCase("ctrl", "shift", "x", false)]
    [TestCase("ctrl", "shift", "x", true)]
    public void Actions_Priority_ShortcutConsumeDisabled_BothPerformDespiteDifferentPriorities(string sharedModifier, string sharedModifier2, string binding1, bool legacyComposites)
    {
        var previousShortcutConsume = InputSystem.settings.shortcutKeysConsumeInput;
        try
        {
            InputSystem.settings.shortcutKeysConsumeInput = false;
            var keyboard = InputSystem.AddDevice<Keyboard>();

            var map = new InputActionMap("map");
            var action1 = map.AddAction("action1");
            action1.AddCompositeBinding((legacyComposites ? "ButtonWithOneModifier" : "OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
                .With("Modifier", "<Keyboard>/" + sharedModifier)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding1);

            var action2 = map.AddAction("action2");
            action2.AddCompositeBinding((legacyComposites ? "ButtonWithOneModifier" : "OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
                .With("Modifier", "<Keyboard>/" + sharedModifier2)
                .With(legacyComposites ? "Button" : "Binding", "<Keyboard>/" + binding1);

            action1.Priority = 0;
            action2.Priority = 10;

            map.Enable();

            var action1WasPerformed = false;
            var action2WasPerformed = false;
            action1.performed += _ => action1WasPerformed = true;
            action2.performed += _ => action2WasPerformed = true;

            Press((ButtonControl)keyboard[sharedModifier], queueEventOnly: true);
            Press((ButtonControl)keyboard[sharedModifier2], queueEventOnly: true);
            Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
            InputSystem.Update();

            Assert.That(action1WasPerformed, Is.True);
            Assert.That(action2WasPerformed, Is.True);
        }
        finally
        {
            InputSystem.settings.shortcutKeysConsumeInput = previousShortcutConsume;
        }
    }

    private static IEnumerable<TwoInputActionDataWrapper<InputAction, InputAction>> TwoInputActionNoConflictingBindingTestCases()
    {
        InputActionMap map = new InputActionMap("map");
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 =  map.SetupTestAction("ctrl", "x"),
            Action2 = map.SetupTestAction("k")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("shift", "n"),
            Action2 = map.SetupTestAction("l")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("shift", "h"),
            Action2 = map.SetupTestAction("ctrl", "shift", "o")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("ctrl", "shift", "v"),
            Action2 = map.SetupTestAction("shift", "z")
        };
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithDifferentPriorityFire_WhenThereIsNoConflictingBinding(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 0;
        action2.Priority = 1;

        action1.m_ActionMap.Enable();
        action2.m_ActionMap.Enable();
        //
        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // Different letter keys: no conflict on the same control, so both shortcuts can perform despite different priorities.
        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        // TODO: Darren, trigger just the bindings again to be sure the shortcut doesn't trigger for a second time
        // Press((ButtonControl)keyboard[action1.GetBind], queueEventOnly: true);
        // Press((ButtonControl)keyboard[action2.controls[i].name], queueEventOnly: true);
        //
        // Assert.That(action1.WasPerformedThisFrame(), Is.False);
        // Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [UnityTest]
    [Category("Actions Priority")]
    public IEnumerator Actions_Priority_TwoNonConflictingShortcuts_ReversedPriorityOrder_BothStillPerform()
    {
        string sharedModifier = "ctrl";
        string binding1 = "x";
        string binding2 = "c";

        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1");
        action1.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding1);

        var action2 = map.AddAction("action2");
        action2.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding2);

        // Higher priority on the first action; letter keys still differ so both should run.
        action1.Priority = 10;
        action2.Priority = 0;

        map.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        Press((ButtonControl)keyboard[sharedModifier], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);
        InputSystem.Update();

        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        yield return null;

        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);

        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [UnityTest]
    [Category("Actions Priority")]
    public IEnumerator Actions_Priority_TwoNonConflictingShortcuts_EqualHighPriority_BothPerform()
    {
        string sharedModifier = "ctrl";
        string binding1 = "x";
        string binding2 = "c";

        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1");
        action1.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding1);

        var action2 = map.AddAction("action2");
        action2.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding2);

        action1.Priority = 500;
        action2.Priority = 500;

        map.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Press((ButtonControl)keyboard[sharedModifier], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);
        InputSystem.Update();

        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        yield return null;

        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);

        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [UnityTest]
    [Category("Actions Priority")]
    public IEnumerator Actions_Priority_TwoNonConflictingShortcuts_ReverseActionDeclarationOrder_BothPerform()
    {
        string sharedModifier = "ctrl";
        string binding1 = "x";
        string binding2 = "c";

        InputSystem.settings.shortcutKeysConsumeInput = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap("map");
        // Add the second shortcut's action first so registration order differs from the original test.
        var action2 = map.AddAction("action2");
        action2.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding2);

        var action1 = map.AddAction("action1");
        action1.AddCompositeBinding(("OneModifier") + "(overrideModifiersNeedToBePressedFirst)")
            .With("Modifier", "<Keyboard>/" + sharedModifier)
            .With("Binding", "<Keyboard>/" + binding1);

        action1.Priority = 0;
        action2.Priority = 1;

        map.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Press((ButtonControl)keyboard[sharedModifier], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);
        InputSystem.Update();

        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        yield return null;

        Press((ButtonControl)keyboard[binding1], queueEventOnly: true);
        Press((ButtonControl)keyboard[binding2], queueEventOnly: true);

        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }
}
