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
    internal static InputAction SetupTestAction(this InputActionMap map, string[] bindings)
    {
        var actionTag = string.Join("+", bindings);
        switch (bindings.Length)
        {
            case 1:
            {
                var action = map.AddAction($"Action {actionTag}");
                action.AddBinding("<Keyboard>/" + bindings[0]);
                return action;
            }

            case 2:
            {
                var modifier = bindings[0];
                var binding = bindings[1];

                var action = map.AddAction($"Action {actionTag}");

                action.AddCompositeBinding("OneModifier")
                    .With("Modifier", "<Keyboard>/" + modifier)
                    .With("Binding", "<Keyboard>/" + binding);

                return action;
            }

            case 3:
            {
                var modifier1 = bindings[0];
                var modifier2 = bindings[1];
                var binding = bindings[2];

                var action = map.AddAction($"Action {actionTag}");

                action.AddCompositeBinding("TwoModifiers")
                    .With("Modifier1", "<Keyboard>/" + modifier1)
                    .With("Modifier2", "<Keyboard>/" + modifier2)
                    .With("Binding", "<Keyboard>/" + binding);

                return action;
            }

            default:
                return null;
        }
    }
}

internal partial class CoreTests
{
    /// <summary>
    /// Overlap resolution uses <see cref="InputAction.Priority"/> and per-control grouping written from actions.
    /// </summary>
    private static void EnableActionPriorityShortcutResolution()
    {
        InputSystem.settings.shortcutKeysUseActionPriority = true;
        InputSystem.settings.shortcutKeysConsumeInput = false;
    }

    /// <summary>
    /// Overlap resolution uses composite binding complexity; <see cref="InputAction.Priority"/> is not applied at runtime.
    /// Requires shortcut consumption on so control grouping merges slots on the same physical control.
    /// </summary>
    private static void EnableComplexityShortcutResolution()
    {
        InputSystem.settings.shortcutKeysConsumeInput = true;
        InputSystem.settings.shortcutKeysUseActionPriority = false;
    }

    private static readonly List<(string[], string[])> k_TwoInputActionTestCases = new()
    {
        (new[] {"ctrl", "x"}, new[] {"x"}),
        (new[] {"shift", "n"}, new[] {"n"}),
        (new[] {"ctrl", "shift", "h"}, new[] {"shift", "h"}),
        (new[] {"ctrl", "shift", "v"}, new[] {"shift", "v"}),
    };

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Setter_ClampsToRepresentableRange()
    {
        var map = new InputActionMap("m");
        var action = map.AddAction("a", binding: "<Keyboard>/x");

        action.Priority = -1;
        Assert.That(action.Priority, Is.EqualTo(0));

        action.Priority = 70000;
        Assert.That(action.Priority, Is.EqualTo(65535));

        action.Priority = 100;
        Assert.That(action.Priority, Is.EqualTo(100));
    }

    private void PressBindingsForInputActions(Keyboard keyboard, InputAction action1, InputAction action2, InputAction action3 = null)
    {
        for (int i = 0; i < action1.controls.Count; i++)
        {
            Press((ButtonControl)action1.controls[i], queueEventOnly: true);
        }

        for (int i = 0; i < action2.controls.Count; i++)
        {
            Press((ButtonControl)action2.controls[i], queueEventOnly: true);
        }

        if (action3 != null)
        {
            for (int i = 0; i < action3.controls.Count; i++)
            {
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
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Priority_OnlyOneActionIsFired_WhenOnePriorityIsHigherThanOther((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Priority_OnlyOneActionIsFired_WhenOnePriorityIsHigherThanOtherInversePriorityOrder((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Priority_BothActionsArePerformed_DueToKeyPressOrderForShortcut((string[] larger, string[] smaller) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        // We swap the order here of Action1 & Action2 so key presses are done backwards, binding before modifiers.
        // This causes the opposite keys foreach test case inside TwoInputActionTestCases to be pressed first.

        var smallerBindingAction = map.SetupTestAction(actions.smaller);
        var largerBindingAction = map.SetupTestAction(actions.larger);

        // Even though the priority is higher for action2 here, due to the order of the keys being pressed only Action1 will be fired.
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
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Priority_BothActionFires_WhenPriorityIsEqual((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

        action1.Priority = 5;
        action2.Priority = 5;

        action1.m_ActionMap.Enable();

        PressBindingsForInputActions(keyboard, action1, action2);

        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Priority_BothActionsFire_WhenPriorityIsZero((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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

    private static readonly List<(string[], string[])> k_TwoInputActionNoConflictingBindingTestCases = new()
    {
        (new[] {"ctrl", "x"}, new[] {"k"}),
        (new[] {"shift", "n"}, new[] {"l"}),
        (new[] {"shift", "h"}, new[] {"l"}),
        (new[] {"shift", "h"}, new[] {"ctrl", "shift", "o"}),
        (new[] {"ctrl", "shift", "v"}, new[] {"shift", "z"})
    };

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(k_TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithDifferentPriorityFire_WhenThereIsNoConflictingBinding((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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
    [TestCaseSource(nameof(k_TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithDifferentPriorityFire_WhenThereIsNoConflictingBindingInverseOrder((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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
    [TestCaseSource(nameof(k_TwoInputActionNoConflictingBindingTestCases))]
    public void Actions_Priority_BothActionsWithEqualPriorityFire_WhenThereIsNoConflictingBinding((string[] a1, string[] a2) actions)
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputActionMap map = new InputActionMap("map");

        var action1 = map.SetupTestAction(actions.a1);
        var action2 = map.SetupTestAction(actions.a2);

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
        InputActionStateMonitorIndex index = InputActionStateMonitorIndex.Create(mapIndex: 7, controlIndex: 0x00abcdef, bindingIndex: 0x0bcd,
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
    // Priority is stored as a 16-bit field in the packed index. This test ensures values above byte.MaxValue (255)
    // are not silently truncated, confirming the field is ushort-wide end-to-end.
    public void Actions_Priority_InputActionStateMonitorIndex_PriorityRoundTripsFullSixteenBits()
    {
        var index300 = InputActionStateMonitorIndex.Create(0, 1, 0, priority: 300);
        Assert.That(index300.Priority, Is.EqualTo(300));
        Assert.That(InputActionState.GetComplexityFromMonitorIndex(index300.Packed), Is.EqualTo(300));

        var index65535 = InputActionStateMonitorIndex.Create(0, 1, 0, priority: 65535);
        Assert.That(index65535.Priority, Is.EqualTo(65535));
    }

    [Test]
    [Category("Actions Priority")]
    // Priority is a ushort (0–65535). This verifies that values above byte.MaxValue (255) still resolve in
    // the correct order, guarding against accidental byte-truncation in the sort path.
    public void Actions_Priority_PrioritiesExceedingByteRange_ResolveInOrder()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");
        var lower = map.AddAction("lower", binding: "<Keyboard>/x");
        var higher = map.AddAction("higher", binding: "<Keyboard>/x");
        lower.Priority = 300;
        higher.Priority = 400;
        map.Enable();

        Press((ButtonControl)keyboard.xKey, queueEventOnly: true);
        InputSystem.Update();

        Assert.That(higher.WasPerformedThisFrame(), Is.True);
        Assert.That(lower.WasPerformedThisFrame(), Is.False);

        Release((ButtonControl)keyboard.xKey);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_ChangingPriorityWhileEnabled_ReplacesStateMonitorInsteadOfDuplicating()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");
        var action = map.AddAction("a", binding: "<Keyboard>/x");
        action.Priority = 1;
        map.Enable();

        var state = map.m_State;
        Assert.That(state, Is.Not.Null);

        var control = keyboard.xKey;
        var deviceIndex = keyboard.m_DeviceIndex;
        Assert.That(deviceIndex, Is.GreaterThanOrEqualTo(0));

        int CountMonitorsForActionStateOnControl()
        {
            ref var bucket = ref InputSystem.manager.m_StateMonitors.m_MonitorsPerDevice[deviceIndex];
            var c = 0;
            for (var i = 0; i < bucket.count; ++i)
            {
                if (bucket.memoryRegions[i].sizeInBits == 0)
                    continue;
                if (ReferenceEquals(bucket.listeners[i].monitor, state) && bucket.listeners[i].control == control)
                    ++c;
            }

            return c;
        }

        Assert.That(CountMonitorsForActionStateOnControl(), Is.EqualTo(1));

        action.Priority = 5;
        action.Priority = 10;
        action.Priority = 20;

        Assert.That(CountMonitorsForActionStateOnControl(), Is.EqualTo(1));

        var performedCount = 0;
        action.performed += _ => performedCount++;
        Press((ButtonControl)keyboard.xKey, queueEventOnly: true);
        InputSystem.Update();
        Assert.That(performedCount, Is.EqualTo(1));

        Release((ButtonControl)keyboard.xKey);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_ChangingPriorityOnCompositeAction_UpdatesMonitorPackedPriorityOnPartControls()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");
        var shiftB = map.AddAction("shiftB");
        shiftB.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/leftShift")
            .With("Binding", "<Keyboard>/b");
        shiftB.Priority = 3;
        map.Enable();

        var state = map.m_State;
        Assert.That(state, Is.Not.Null);

        static int PackedPriorityForMonitor(InputActionState actionState, InputControl control)
        {
            var deviceIndex = control.device.m_DeviceIndex;
            ref var bucket = ref InputSystem.manager.m_StateMonitors.m_MonitorsPerDevice[deviceIndex];
            for (var i = 0; i < bucket.count; ++i)
            {
                if (bucket.memoryRegions[i].sizeInBits == 0)
                    continue;
                if (!ReferenceEquals(bucket.listeners[i].monitor, actionState) ||
                    bucket.listeners[i].control != control)
                    continue;
                return InputActionStateMonitorIndex.FromPacked(bucket.listeners[i].monitorIndex).Priority;
            }

            return int.MinValue;
        }

        Assert.That(PackedPriorityForMonitor(state, keyboard.bKey), Is.EqualTo(3));
        Assert.That(PackedPriorityForMonitor(state, keyboard.leftShiftKey), Is.EqualTo(3));

        shiftB.Priority = 7;

        Assert.That(PackedPriorityForMonitor(state, keyboard.bKey), Is.EqualTo(7));
        Assert.That(PackedPriorityForMonitor(state, keyboard.leftShiftKey), Is.EqualTo(7));
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_InputActionStateMonitorIndex_ImplicitConversionToLongMatchesPackedProperty()
    {
        InputActionStateMonitorIndex index = InputActionStateMonitorIndex.Create(1, 2, 3, 4);
        long asLong = index;
        Assert.That(asLong, Is.EqualTo(index.Packed));
    }

    [Test]
    [Category("Actions Priority")]
    public unsafe void Actions_Priority_ControlGrouping_SamePhysicalControlSharesGroupId()
    {
        EnableActionPriorityShortcutResolution();
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
        EnableActionPriorityShortcutResolution();
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

    /// <summary>
    /// Shift+B with <c>hold(duration=2)</c> reaches <see cref="InputActionPhase.Performed"/> after two seconds of
    /// continuous hold (real time on the test runtime clock).
    /// </summary>
    [UnityTest]
    [Category("Actions Priority")]
    public IEnumerator Actions_Priority_BothActionsArePerformed_WhenAHoldAndBasicActionHaveDifferentTiming()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = new InputActionMap("HoldChord");

        var plainB = map.AddAction("PlainB", InputActionType.Button, "<Keyboard>/b");
        var shiftBHold = map.AddAction("ShiftBHold", InputActionType.Button, binding: null, interactions: "hold(duration=2)");
        shiftBHold.AddCompositeBinding("OneModifier(modifiersOrder=2)")
            .With("modifier", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/b");
        plainB.Priority = 0;
        shiftBHold.Priority = 1;

        var plainBPerformed = false;
        plainB.performed += _ => plainBPerformed = true;

        map.Enable();

        var t0 = currentTime;
        Press(keyboard.leftShiftKey);
        Press(keyboard.bKey);
        InputSystem.Update();
        yield return null;

        Assert.AreNotEqual(
            InputActionPhase.Performed,
            shiftBHold.phase,
            "Hold should not be Performed until the hold duration elapses.");

        currentTime = t0 + 2.1;
        InputSystem.Update();
        yield return null;

        Assert.IsTrue(plainBPerformed);
        Assert.IsTrue(
            shiftBHold.phase == InputActionPhase.Performed,
            "Hold should complete to Performed after the hold duration with keys still down.");

        Release(keyboard.bKey);
        Release(keyboard.leftShiftKey);
        map.Disable();
    }

    /// <summary>
    /// Shift+B with <c>hold(duration=2)</c> reaches <see cref="InputActionPhase.Performed"/> after two seconds of
    /// continuous hold (real time on the test runtime clock).
    /// </summary>
    [UnityTest]
    [Category("Actions Priority")]
    public IEnumerator Actions_Priority_OnlyOneHoldActionIsPerformed_WhenOnePriorityIsHigher()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = new InputActionMap("HoldChord");

        var plainB = map.AddAction("PlainB", InputActionType.Button, "<Keyboard>/b",  interactions: "hold(duration=2)");
        var shiftBHold = map.AddAction("ShiftBHold", InputActionType.Button, binding: null, interactions: "hold(duration=2)");
        shiftBHold.AddCompositeBinding("OneModifier(modifiersOrder=2)")
            .With("modifier", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/b");
        plainB.Priority = 0;
        shiftBHold.Priority = 1;

        var plainBPerformed = false;
        plainB.performed += _ => plainBPerformed = true;

        map.Enable();

        var t0 = currentTime;
        Press(keyboard.leftShiftKey);
        Press(keyboard.bKey);
        InputSystem.Update();
        yield return null;

        Assert.AreNotEqual(
            InputActionPhase.Performed,
            shiftBHold.phase,
            "Hold should not be Performed until the hold duration elapses.");

        currentTime = t0 + 2.1;
        InputSystem.Update();
        yield return null;

        Assert.IsTrue(plainBPerformed);
        Assert.IsTrue(
            shiftBHold.phase == InputActionPhase.Performed,
            "Hold should complete to Performed after the hold duration with keys still down.");

        Release(keyboard.bKey);
        Release(keyboard.leftShiftKey);
        map.Disable();
    }

    [Test]
    [Category("Actions Priority")]
    public unsafe void Actions_Complexity_ControlGrouping_SamePhysicalControlSharesGroupId_WhenShortcutConsumptionEnabled()
    {
        EnableComplexityShortcutResolution();

        InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("complexity_group_test");
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
    // In complexity mode the secondary column of the control-grouping table holds binding-chain depth (composite
    // complexity), not the action's Priority value. Two simple (non-composite) bindings on the same key each have
    // depth 1 regardless of what Priority is set on their actions, because Priority is irrelevant in this mode.
    public unsafe void Actions_Complexity_ControlGrouping_WritesPerControlSlotComplexity_NotActionPriority()
    {
        EnableComplexityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("complexity_per_slot_test");
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
        // Secondary column stores composite complexity; two simple bindings on the same key both have depth 1.
        Assert.That(state.memory.controlGroupingAndPriority[pLow], Is.EqualTo(1));
        Assert.That(state.memory.controlGroupingAndPriority[pHigh], Is.EqualTo(1));
    }

    [Test]
    [Category("Actions Priority")]
    public unsafe void Actions_Complexity_ControlGrouping_WritesHigherComplexityOnSharedControlVersusSimpleBinding()
    {
        EnableComplexityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("complexity_composite_vs_simple");
        var composite = map.AddAction("chord", binding: null);
        composite.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/ctrl")
            .With("Binding", "<Keyboard>/x");
        var simple = map.AddAction("plain", binding: "<Keyboard>/x");
        composite.Priority = 0;
        simple.Priority = 99;
        map.Enable();

        var state = map.m_State;
        Assert.That(state, Is.Not.Null);

        var compositeXIndex = -1;
        var simpleXIndex = -1;
        for (var i = 0; i < state.totalControlCount; ++i)
        {
            if (state.controls[i] != keyboard.xKey)
                continue;
            var bindingIndex = state.controlIndexToBindingIndex[i];
            var actionIndex = state.bindingStates[bindingIndex].actionIndex;
            if (actionIndex == composite.m_ActionIndexInState)
                compositeXIndex = i;
            else if (actionIndex == simple.m_ActionIndexInState)
                simpleXIndex = i;
        }

        Assert.That(compositeXIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(simpleXIndex, Is.GreaterThanOrEqualTo(0));

        var pComposite = InputActionState.ControlGroupingTable.PriorityElementIndex(compositeXIndex);
        var pSimple = InputActionState.ControlGroupingTable.PriorityElementIndex(simpleXIndex);
        Assert.That(state.memory.controlGroupingAndPriority[pSimple], Is.EqualTo(1));
        Assert.That(
            state.memory.controlGroupingAndPriority[pComposite],
            Is.GreaterThan(state.memory.controlGroupingAndPriority[pSimple]),
            "Composite binding chain depth should exceed a simple binding on the same physical control.");
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Complexity_CompositeWinsOverlappingSimple_IgnoresActionPriority((string[] a1, string[] a2) actions)
    {
        EnableComplexityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");

        var actionComposite = map.SetupTestAction(actions.a1);
        var actionSimple = map.SetupTestAction(actions.a2);

        // Deliberately favor the simple binding in the Priority field; complexity resolution must still prefer the composite.
        actionComposite.Priority = 0;
        actionSimple.Priority = 100;

        map.Enable();

        Assert.That(actionComposite.WasPerformedThisFrame(), Is.False);
        Assert.That(actionSimple.WasPerformedThisFrame(), Is.False);

        PressBindingsForInputActions(keyboard, actionComposite, actionSimple);

        Assert.That(actionComposite.WasPerformedThisFrame(), Is.True);
        Assert.That(actionSimple.WasPerformedThisFrame(), Is.False);

        ReleaseBindingsForActions(keyboard, actionComposite, actionSimple);

        InputSystem.Update();

        Assert.That(actionComposite.WasPerformedThisFrame(), Is.False);
        Assert.That(actionSimple.WasPerformedThisFrame(), Is.False);
    }

    [Test]
    [Category("Actions Priority")]
    [TestCaseSource(nameof(k_TwoInputActionTestCases))]
    public void Actions_Complexity_CompositeWinsOverlappingSimple_EvenWhenCompositeHasHigherPriorityField(
        (string[] a1, string[] a2) actions)
    {
        EnableComplexityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");

        var actionComposite = map.SetupTestAction(actions.a1);
        var actionSimple = map.SetupTestAction(actions.a2);

        actionComposite.Priority = 100;
        actionSimple.Priority = 1;

        map.Enable();

        PressBindingsForInputActions(keyboard, actionComposite, actionSimple);

        Assert.That(actionComposite.WasPerformedThisFrame(), Is.True);
        Assert.That(actionSimple.WasPerformedThisFrame(), Is.False);

        ReleaseBindingsForActions(keyboard, actionComposite, actionSimple);
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Complexity_BothSimpleActionsOnSameControlPerform_WhenEqualComplexity()
    {
        EnableComplexityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var map = new InputActionMap("map");
        var action1 = map.AddAction("a", binding: "<Keyboard>/y");
        var action2 = map.AddAction("b", binding: "<Keyboard>/y");
        action1.Priority = 2;
        action2.Priority = 99;
        map.Enable();

        Press((ButtonControl)action1.controls[0], queueEventOnly: true);
        Press((ButtonControl)action2.controls[0], queueEventOnly: true);
        InputSystem.Update();

        Assert.That(action1.WasPerformedThisFrame(), Is.True);
        Assert.That(action2.WasPerformedThisFrame(), Is.True);

        Release((ButtonControl)action1.controls[0], queueEventOnly: true);
        Release((ButtonControl)action2.controls[0], queueEventOnly: true);
        InputSystem.Update();
    }
}
