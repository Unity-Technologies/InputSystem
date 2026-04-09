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
            Press((ButtonControl)action1.controls[i], queueEventOnly: true);
        }

        for (int i = 0; i < action2.controls.Count; i++)
        {
            Debug.Log("action 2 binding pressed: " + action2.controls[i].name);
            Press((ButtonControl)action2.controls[i], queueEventOnly: true);
        }

        if (action3 != null)
        {
            for (int i = 0; i < action3.controls.Count; i++)
            {
                Debug.Log("action 3 binding pressed: " + action3.controls[i].name);
                Press((ButtonControl)action3.controls[i], queueEventOnly: true);
            }
        }

        InputSystem.Update();
    }

    private void ReleaseBindingsForActions(Keyboard keyboard, InputAction action1, InputAction action2)
    {
        // Cleanup key presses
        for (int i = 0; i < action1.controls.Count; i++)
        {
            Release((ButtonControl)action1.controls[i], queueEventOnly: true);
        }

        for (int i = 0; i < action2.controls.Count; i++)
        {
            Release((ButtonControl)action2.controls[i], queueEventOnly: true);
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
        threeInputActions.Action1.m_ActionMap.Enable();
        PressBindingsForInputActions(keyboard, threeInputActions.Action1, threeInputActions.Action2);


        Assert.That(threeInputActions.Action1.WasPerformedThisFrame(), Is.True);

        //Assert.IsTrue(threeInputActions.Action1.WasPerformedThisFrame(), "Team chat should be activated by Alt+Shift+W.");
        // Assert.IsFalse(moveAction.IsPressed(), "Move should not be activated when Team chat takes priority.");
        // Assert.IsFalse(runFastAction.IsPressed(), "Run Fast should not be activated when Team chat takes priority.");

        // Release(keyboard.wKey);
        // Release(keyboard.leftShiftKey);
        // Release(keyboard.altKey);
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_ControlGroupingTable_StrideAndElementIndicesMatchInterleavedLayout()
    {
        Assert.That(InputActionState.ControlGroupingTable.Stride, Is.EqualTo(2));
        Assert.That(InputActionState.ControlGroupingTable.GroupElementIndex(3), Is.EqualTo(6));
        Assert.That(InputActionState.ControlGroupingTable.PriorityElementIndex(3), Is.EqualTo(7));
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_InputActionStateMonitorIndex_RoundTripsComponents()
    {
        var index = InputActionStateMonitorIndex.Create(mapIndex: 7, controlIndex: 0x00abcdef, bindingIndex: 0x0bcd,
            priority: 200);

        Assert.That(index.MapIndex, Is.EqualTo(7));
        Assert.That(index.ControlIndex, Is.EqualTo(0x00abcdef));
        Assert.That(index.BindingIndex, Is.EqualTo(0x0bcd));
        Assert.That(index.Priority, Is.EqualTo(200));
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_InputActionStateMonitorIndex_FromPacked_MatchesCreateOutput()
    {
        var created = InputActionStateMonitorIndex.Create(3, 100, 200, 42);
        var roundTrip = InputActionStateMonitorIndex.FromPacked(created.Packed);

        Assert.That(roundTrip.MapIndex, Is.EqualTo(created.MapIndex));
        Assert.That(roundTrip.ControlIndex, Is.EqualTo(created.ControlIndex));
        Assert.That(roundTrip.BindingIndex, Is.EqualTo(created.BindingIndex));
        Assert.That(roundTrip.Priority, Is.EqualTo(created.Priority));
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_InputActionStateMonitorIndex_PriorityUsesLowEightBitsInPackedRepresentation()
    {
        var index = InputActionStateMonitorIndex.Create(0, 1, 0, priority: 300);
        Assert.That(index.Priority, Is.EqualTo(300 & 0xff));
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_InputActionStateMonitorIndex_ImplicitConversionToLongMatchesPackedProperty()
    {
        var index = InputActionStateMonitorIndex.Create(1, 2, 3, 4);
        long asLong = index;
        Assert.That(asLong, Is.EqualTo(index.Packed));
    }

    [Test]
    [Category("Actions Priority")]
    public unsafe void Actions_Priority_ControlGrouping_SamePhysicalControlSharesGroupId()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("priority_group_test");
        map.AddAction("a", binding: "<Keyboard>/z");
        map.AddAction("b", binding: "<Keyboard>/z");
        map.Enable();

        var state = map.m_State;
        Assert.That(state, Is.Not.Null);
        Assert.That(state.memory.controlGroupingInitialized, Is.True);

        for (var i = 0; i < state.totalControlCount; ++i)
        {
            for (var j = i + 1; j < state.totalControlCount; ++j)
            {
                if (state.controls[i] != state.controls[j])
                    continue;

                var gi = InputActionState.ControlGroupingTable.GroupElementIndex(i);
                var gj = InputActionState.ControlGroupingTable.GroupElementIndex(j);
                Assert.That(state.memory.controlGroupingAndPriority[gi], Is.EqualTo(state.memory.controlGroupingAndPriority[gj]));
                Assert.That(state.memory.controlGroupingAndPriority[gi], Is.Not.EqualTo(0));
                return;
            }
        }

        Assert.Fail("Expected two control slots bound to the same physical control.");
    }

    [Test]
    [Category("Actions Priority")]
    public unsafe void Actions_Priority_ControlGrouping_WritesPerControlSlotPriorityFromAction()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("priority_per_slot_test");
        var actionLow = map.AddAction("low", binding: "<Keyboard>/x");
        var actionHigh = map.AddAction("high", binding: "<Keyboard>/x");
        actionLow.Priority = 4;
        actionHigh.Priority = 11;
        map.Enable();

        var state = map.m_State;
        Assert.That(state, Is.Not.Null);

        var lowIndex = -1;
        var highIndex = -1;
        for (var i = 0; i < state.totalControlCount; ++i)
        {
            if (state.controls[i] != keyboard.xKey)
                continue;
            var bindingIndex = state.controlIndexToBindingIndex[i];
            var actionIndex = state.bindingStates[bindingIndex].actionIndex;
            if (actionIndex == actionLow.m_ActionIndexInState)
                lowIndex = i;
            else if (actionIndex == actionHigh.m_ActionIndexInState)
                highIndex = i;
        }

        Assert.That(lowIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(highIndex, Is.GreaterThanOrEqualTo(0));

        var pLow = InputActionState.ControlGroupingTable.PriorityElementIndex(lowIndex);
        var pHigh = InputActionState.ControlGroupingTable.PriorityElementIndex(highIndex);
        Assert.That(state.memory.controlGroupingAndPriority[pLow], Is.EqualTo(4));
        Assert.That(state.memory.controlGroupingAndPriority[pHigh], Is.EqualTo(11));
    }
}
