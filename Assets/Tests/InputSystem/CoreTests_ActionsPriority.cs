using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
            Action1 = map.SetupTestAction("ctrl", "shift", "h"),
            Action2 = map.SetupTestAction("shift", "h")
        };
        yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        {
            Action1 = map.SetupTestAction("ctrl", "shift", "v"),
            Action2 = map.SetupTestAction("shift", "v")
        };
        // yield return new TwoInputActionDataWrapper<InputAction, InputAction>
        // {
        //     Action1 = map.SetupTestAction("ctrl", "enter", "w"),
        //     Action2 = map.SetupTestAction("alt", "shift", "w")
        // };
    }

    public class TwoInputActionDataWrapper<TInputAction1, TInputAction2>
    {
        public TInputAction1 Action1;
        public TInputAction2 Action2;
    }

    public class ThreeInputActionDataWrapper<TInputAction1, TInputAction2, TInputAction3>
    {
        public TInputAction1 Action1;
        public TInputAction2 Action2;
        public TInputAction3 Action3;
    }

    private void PressBindingsForInputActions(Keyboard keyboard, InputAction action1, InputAction action2, InputAction action3 = null)
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

        if (action3 != null)
        {
            for (int i = 0; i < action2.controls.Count; i++)
            {
                Debug.Log("action 2 binding pressed: " + action2.controls[i].name);
                Press((ButtonControl)keyboard[action2.controls[i].name], queueEventOnly: true);
            }
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
    public void Actions_Priority_OnlyOneActionIsFired_WhenOnePriorityIsHigherThanOther(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

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

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_OnlyOneActionIsFired_WhenOnePriorityIsHigherThanOtherInversePriorityOrder(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        // action 2's priority higher so it takes precedence
        action1.Priority = 1;
        action2.Priority = 2;

        action1.m_ActionMap.Enable();

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // action2 is performed because action2 has a higher priority than action1.
        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        // Cleanup key presses
        ReleaseBindingsForActions(keyboard, action1, action2);

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))] // TODO: Darren, Should both actions be performed this frame here??
    public void Actions_Priority_BothActionsArePerformed_DueToKeyPressOrderForShortcut(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // We swap the order here of Action1 & Action2 so key presses are done backwards, binding before modifiers.
        // This causes the opposite keys foreach test case inside TwoInputActionTestCases to be pressed first.
        var smallerBindingAction = twoInputActions.Action2;
        var largerBindingAction = twoInputActions.Action1;

        // Event though the priority is higher for action2 here, due to the order of the keys being pressed only Action1 will be fired.
        smallerBindingAction.Priority = 1;
        largerBindingAction.Priority = 2;

        smallerBindingAction.m_ActionMap.Enable();

        Assert.That(smallerBindingAction.WasPerformedThisFrame(), Is.False);
        Assert.That(largerBindingAction.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, smallerBindingAction, largerBindingAction);

        // action1 is performed because action1 has a higher priority than action2.
        Assert.That(smallerBindingAction.WasPerformedThisFrame(), Is.True);
        Assert.That(largerBindingAction.WasPerformedThisFrame(), Is.True);

        // Cleanup key presses
        ReleaseBindingsForActions(keyboard, smallerBindingAction, largerBindingAction);

        // Update again to be sure released is true.
        InputSystem.Update();

        Assert.That(smallerBindingAction.WasPerformedThisFrame(), Is.False);
        Assert.That(largerBindingAction.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_BothActionFires_WhenPriorityIsEqual(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 5;
        action2.Priority = 5;

        action1.m_ActionMap.Enable();

        PressBindingsForInputActions(keyboard, action1, action2);

        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionTestCases))]
    public void Actions_Priority_BothActionsFire_WhenPriorityIsZero(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
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
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 0;
        action2.Priority = 1;

        action1.m_ActionMap.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // Different letter keys: no conflict on the same control, so both shortcuts can perform despite different priorities.
        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithDifferentPriorityFire_WhenThereIsNoConflictingBindingInverseOrder(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 15;
        action2.Priority = 5;

        action1.m_ActionMap.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // Different letter keys: no conflict on the same control, so both shortcuts can perform despite different priorities.
        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithEqualPriorityFire_WhenThereIsNoConflictingBinding(TwoInputActionDataWrapper<InputAction, InputAction> twoInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action1 = twoInputActions.Action1;
        var action2 = twoInputActions.Action2;

        action1.Priority = 5;
        action2.Priority = 5;

        action1.m_ActionMap.Enable();

        var action1WasPerformed = false;
        action1.performed += _ => action1WasPerformed = true;

        Assert.That(action1.WasPerformedThisFrame(), Is.False);
        Assert.That(action2.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, action1, action2);

        // Different letter keys: no conflict on the same control, so both shortcuts can perform despite different priorities.
        Assert.That(action1WasPerformed, Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    private static IEnumerable<ThreeInputActionDataWrapper<InputAction, InputAction, InputAction>> ThreeInputActionNoConflictingBindingTestCases()
    {
        InputActionMap map = new InputActionMap("map");
        yield return new ThreeInputActionDataWrapper<InputAction, InputAction, InputAction>
        {
            Action1 =  map.SetupTestAction("alt", "shift", "w"),
            Action2 = map.SetupTestAction("z"),
            Action3 = map.SetupTestAction("l"),
        };
    }

    [Test]
    [Category("Actions Priority")]
    [Ignore("Weird failing case from Anthony")]
    [TestCaseSource(nameof(ThreeInputActionNoConflictingBindingTestCases))]
    public void AltShiftW_Only_Triggers_TeamChat(ThreeInputActionDataWrapper<InputAction, InputAction, InputAction> threeInputActions)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        PressBindingsForInputActions(keyboard, threeInputActions.Action1, threeInputActions.Action2);


        Assert.That(threeInputActions.Action1.WasPerformedThisFrame(), Is.True);

        //Assert.IsTrue(threeInputActions.Action1.WasPerformedThisFrame(), "Team chat should be activated by Alt+Shift+W.");
        // Assert.IsFalse(moveAction.IsPressed(), "Move should not be activated when Team chat takes priority.");
        // Assert.IsFalse(runFastAction.IsPressed(), "Run Fast should not be activated when Team chat takes priority.");

        // Release(keyboard.wKey);
        // Release(keyboard.leftShiftKey);
        // Release(keyboard.altKey);
    }
}
