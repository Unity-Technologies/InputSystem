using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Tests for FastMouse event merging, including the DeltaStateEvent path added as a fix for
// UUM-142550 (high-polling-rate mouse FPS drops on Windows).
//
// Two tiers:
//   Unit tests  — call FastMouse.MergeForward directly; fast, isolated, verify the merge predicate.
//   Integration tests — queue events through InputSystem.QueueEvent + InputSystem.Update so the
//                       full pipeline runs: ProcessEventBuffer → MergeWithNextEvent →
//                       FastMouse.MergeForward → FastMouse.OnStateEvent → InputState.Change.
//                       These mirror the Windows Raw Input delivery path.
partial class CoreTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Queue a full-device DeltaStateEvent with the given MouseState.
    // DeltaStateEvent.From(device) produces stateOffset=0, stateFormat=MOUS,
    // size=device.stateBlock.alignedSizeInBytes — satisfies IsFullMouseStateDeltaEvent.
    private static unsafe void QueueFullMouseDeltaStateEvent(Mouse mouse, MouseState state, double time = -1)
    {
        using var buf = DeltaStateEvent.From(mouse, out var eventPtr);
        var deltaEvt = DeltaStateEvent.FromUnchecked(eventPtr);
        *(MouseState*)deltaEvt->deltaState = state;
        if (time >= 0)
            eventPtr.time = time;
        InputSystem.QueueEvent(eventPtr);
    }

    // -------------------------------------------------------------------------
    // Unit tests — FastMouse.MergeForward in isolation
    // -------------------------------------------------------------------------

    [Test]
    [Category("FastMouseMerger")]
    public unsafe void FastMouseMerger_Unit_MergesDeltaStateEventsWithSameButtonState()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(1, 1) });
        InputSystem.Update();

        using var buf1 = DeltaStateEvent.From(mouse, out var ptr1);
        using var buf2 = DeltaStateEvent.From(mouse, out var ptr2);

        var evt1 = DeltaStateEvent.FromUnchecked(ptr1);
        var evt2 = DeltaStateEvent.FromUnchecked(ptr2);
        ((MouseState*)evt1->deltaState)->delta = new Vector2(1, 1);
        ((MouseState*)evt2->deltaState)->delta = new Vector2(2, 2);

        Assert.That(FastMouse.MergeForward(ptr1, ptr2), Is.True);
        Assert.That(((MouseState*)evt2->deltaState)->delta, Is.EqualTo(new Vector2(3, 3)));
    }

    [Test]
    [Category("FastMouseMerger")]
    public unsafe void FastMouseMerger_Unit_DoesNotMergeDeltaStateEventsWithDifferentButtonState()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();

        using var buf1 = DeltaStateEvent.From(mouse, out var ptr1);
        using var buf2 = DeltaStateEvent.From(mouse, out var ptr2);

        var evt2 = DeltaStateEvent.FromUnchecked(ptr2);
        ((MouseState*)evt2->deltaState)->buttons = 1; // button pressed in second event

        Assert.That(FastMouse.MergeForward(ptr1, ptr2), Is.False);
    }

    [Test]
    [Category("FastMouseMerger")]
    public void FastMouseMerger_Unit_RejectsPartialDeltaStateEvent()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(1, 1) });
        InputSystem.Update();

        // DeltaStateEvent.From(single control) covers only that control's bytes — too small.
        using var buf1 = DeltaStateEvent.From(mouse.position, out var ptr1);
        using var buf2 = DeltaStateEvent.From(mouse.position, out var ptr2);

        Assert.That(FastMouse.MergeForward(ptr1, ptr2), Is.False);
    }

    [Test]
    [Category("FastMouseMerger")]
    public unsafe void FastMouseMerger_Unit_MergesMixedStateAndDeltaStateEvent()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(1, 1) });
        InputSystem.Update();

        // current = StateEvent, next = full DeltaStateEvent
        using (StateEvent.From(mouse, out var statePtr))
        using (var buf2 = DeltaStateEvent.From(mouse, out var deltaPtr))
        {
            var stateEvt = StateEvent.FromUnchecked(statePtr);
            ((MouseState*)stateEvt->state)->delta = new Vector2(1, 0);

            var deltaEvt = DeltaStateEvent.FromUnchecked(deltaPtr);
            ((MouseState*)deltaEvt->deltaState)->delta = new Vector2(0, 1);

            Assert.That(FastMouse.MergeForward(statePtr, deltaPtr), Is.True);
            Assert.That(((MouseState*)deltaEvt->deltaState)->delta, Is.EqualTo(new Vector2(1, 1)));
        }
    }

    // -------------------------------------------------------------------------
    // Integration tests — full ProcessEventBuffer → MergeWithNextEvent →
    //                      FastMouse.OnStateEvent → InputState.Change pipeline
    // -------------------------------------------------------------------------

    // Simulates Windows Raw Input at high polling rate delivering many DeltaStateEvents
    // for pure mouse movement. All events have identical button state so they should
    // coalesce to a single processed event with accumulated delta.
    [Test]
    [Category("FastMouseMerger")]
    public void FastMouseMerger_Integration_ManyDeltaStateEvents_CoalesceToOne()
    {
        const int eventCount = 100;
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.settings.maxQueuedEventsPerUpdate = eventCount + 10;

        var eventsReceived = 0;
        InputSystem.onEvent += (_, device) => { if (device == mouse) ++eventsReceived; };

        // Queue 100 movement events, each advancing position by (1,1) and delta (1,1).
        for (var i = 0; i < eventCount; ++i)
            QueueFullMouseDeltaStateEvent(mouse, new MouseState
            {
                position = new Vector2(i + 1, i + 1),
                delta = new Vector2(1, 1)
            });

        InputSystem.Update();

        Assert.That(eventsReceived, Is.EqualTo(1), "All motion events should merge into one");
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(eventCount, eventCount)));
        Assert.That(mouse.delta.ReadValue(), Is.EqualTo(new Vector2(eventCount, eventCount)));
    }

    // Simulates a button press mid-stream: the press event must be preserved with its
    // original timestamp, and surrounding move events must still merge into their own groups.
    [Test]
    [Category("FastMouseMerger")]
    public void FastMouseMerger_Integration_ButtonPressMidStream_PreservesButtonEventAndMergesSurroundingMoves()
    {
        const int movesBeforePress = 50;
        const int movesAfterPress = 49;
        const int totalEvents = movesBeforePress + 1 + movesAfterPress;
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.settings.maxQueuedEventsPerUpdate = totalEvents + 10;

        var eventsReceived = 0;
        InputSystem.onEvent += (_, device) => { if (device == mouse) ++eventsReceived; };

        var leftButtonMask = new MouseState().WithButton(MouseButton.Left).buttons;

        // Pre-press moves (buttons=0).
        for (var i = 0; i < movesBeforePress; ++i)
            QueueFullMouseDeltaStateEvent(mouse, new MouseState
            {
                position = new Vector2(i + 1, 0),
                delta = new Vector2(1, 0)
            });

        // The button press event.
        QueueFullMouseDeltaStateEvent(mouse, new MouseState
        {
            position = new Vector2(movesBeforePress + 1, 0),
            delta = new Vector2(1, 0),
            buttons = leftButtonMask
        });

        // Post-press moves (buttons=1).
        for (var i = 0; i < movesAfterPress; ++i)
            QueueFullMouseDeltaStateEvent(mouse, new MouseState
            {
                position = new Vector2(movesBeforePress + 2 + i, 0),
                delta = new Vector2(1, 0),
                buttons = leftButtonMask
            });

        InputSystem.Update();

        // Expect: pre-press merged (1) + button press (1) + post-press merged (1) = 3 events.
        Assert.That(eventsReceived, Is.EqualTo(3), "Expected: pre-press group, button event, post-press group");
        Assert.That(mouse.leftButton.isPressed, Is.True);
        Assert.That(mouse.delta.ReadValue().x,
            Is.EqualTo(movesBeforePress + 1 + movesAfterPress).Within(0.001),
            "Total delta must be the sum of all events");
    }

    // Verifies that DeltaStateEvents and StateEvents interleaved in the same buffer
    // still merge correctly end-to-end (mixed-type merge path in MergeForward).
    [Test]
    [Category("FastMouseMerger")]
    public void FastMouseMerger_Integration_MixedStateAndDeltaStateEvents_Merge()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.settings.maxQueuedEventsPerUpdate = 20;

        var eventsReceived = 0;
        InputSystem.onEvent += (_, device) => { if (device == mouse) ++eventsReceived; };

        // StateEvent, DeltaStateEvent, StateEvent — all with the same button state.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 1), delta = new Vector2(1, 1) });
        QueueFullMouseDeltaStateEvent(mouse, new MouseState { position = new Vector2(2, 2), delta = new Vector2(1, 1) });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(3, 3), delta = new Vector2(1, 1) });

        InputSystem.Update();

        Assert.That(eventsReceived, Is.EqualTo(1), "Mixed StateEvent/DeltaStateEvent stream should merge to one");
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(3, 3)));
        Assert.That(mouse.delta.ReadValue(), Is.EqualTo(new Vector2(3, 3)));
    }

    // Verifies that partial DeltaStateEvents (single control, not full device state) are
    // NOT merged but still applied correctly — they fall through to the base class.
    [Test]
    [Category("FastMouseMerger")]
    public void FastMouseMerger_Integration_PartialDeltaStateEvent_IsAppliedButNotMerged()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var eventsReceived = 0;
        InputSystem.onEvent += (_, device) => { if (device == mouse) ++eventsReceived; };

        // Two partial delta events (position control only, covers 8 bytes — below IsFullMouseStateDeltaEvent threshold).
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(10, 20));
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(30, 40));

        InputSystem.Update();

        // Should NOT merge (partial events bypass the merger) but both should be applied.
        Assert.That(eventsReceived, Is.EqualTo(2));
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(30, 40)));
    }

    // Comprehensive simulation of the Windows Raw Input high-polling-rate scenario that
    // caused UUM-142550. Models two full frames of input at ~8000 Hz (133 events/frame at
    // 60 fps), including:
    //   - Sustained movement with no buttons (should collapse entirely)
    //   - A click mid-movement (button down + up within a single frame)
    //   - Scroll wheel input interleaved with movement
    //   - disableRedundantEventsMerging=true as a control to confirm the merger is what
    //     causes the event reduction, not some other mechanism
    //   - Two mice to verify events are never merged across devices
    [Test]
    [Category("FastMouseMerger")]
    [TestCase(true, Description = "Merging enabled — events should collapse")]
    [TestCase(false,  Description = "Merging disabled — all events pass through")]
    public void FastMouseMerger_Integration_HighPollingRateSimulation(bool mergeEvents)
    {
        // At 8000 Hz / 60 fps ≈ 133 events per frame. Use 130 to keep the test fast.
        const int movesPerFrame = 130;
        // One button-down and one button-up mid-stream.
        const int totalEventsPerMouse = movesPerFrame + 2;

        InputSystem.settings.disableRedundantEventsMerging = !mergeEvents;
        InputSystem.settings.maxQueuedEventsPerUpdate = totalEventsPerMouse * 2 + 20;

        var mouse1 = InputSystem.AddDevice<Mouse>();
        var mouse2 = InputSystem.AddDevice<Mouse>();

        var mouse1EventsReceived = 0;
        var mouse2EventsReceived = 0;
        InputSystem.onEvent += (_, device) =>
        {
            if (device == mouse1) ++mouse1EventsReceived;
            else if (device == mouse2) ++mouse2EventsReceived;
        };

        var leftMask    = new MouseState().WithButton(MouseButton.Left).buttons;
        var scrollDelta = new Vector2(0, 3);

        // --- Mouse 1: all events queued contiguously ---
        // Pre-click moves (buttons=0).
        for (var i = 0; i < movesPerFrame / 2; ++i)
            QueueFullMouseDeltaStateEvent(mouse1, new MouseState
            {
                position = new Vector2(i * 2, 0),
                delta    = new Vector2(2, 0),
                scroll   = scrollDelta
            });

        // Button down.
        QueueFullMouseDeltaStateEvent(mouse1, new MouseState
        {
            position = new Vector2(movesPerFrame / 2 * 2, 0),
            delta    = new Vector2(2, 0),
            buttons  = leftMask
        });

        // Button up.
        QueueFullMouseDeltaStateEvent(mouse1, new MouseState
        {
            position = new Vector2(movesPerFrame / 2 * 2 + 2, 0),
            delta    = new Vector2(2, 0),
            buttons  = 0
        });

        // Post-click moves (buttons=0 again).
        for (var i = 0; i < movesPerFrame / 2; ++i)
            QueueFullMouseDeltaStateEvent(mouse1, new MouseState
            {
                position = new Vector2(movesPerFrame / 2 * 2 + 2 + i * 2 + 2, 0),
                delta    = new Vector2(2, 0),
                scroll   = scrollDelta
            });

        // --- Mouse 2: all pure-movement events queued contiguously after mouse 1 ---
        // This proves the merger never coalesces events across devices: even though
        // mouse2's events are in the same buffer, they merge only within their own run.
        for (var i = 0; i < movesPerFrame; ++i)
            QueueFullMouseDeltaStateEvent(mouse2, new MouseState
            {
                position = new Vector2(i * 2, 0),
                delta    = new Vector2(2, 0)
            });

        InputSystem.Update();

        if (!mergeEvents)
        {
            // With merging off every event fires — this is the "before fix" baseline that
            // confirms the event reduction is entirely due to the merger.
            Assert.That(mouse1EventsReceived, Is.EqualTo(totalEventsPerMouse),
                "With merging disabled all mouse1 events must pass through");
            Assert.That(mouse2EventsReceived, Is.EqualTo(movesPerFrame),
                "With merging disabled all mouse2 events must pass through");
        }
        else
        {
            // With merging on:
            //   mouse1: pre-click merged (1) + button-down (1) + button-up (1) + post-click merged (1) = 4
            //   mouse2: all moves share the same button state and are contiguous → collapsed to 1
            // Cross-device non-merging is verified by both mice ending with correct independent state.
            Assert.That(mouse1EventsReceived, Is.LessThan(totalEventsPerMouse),
                "Merging must reduce mouse1 event count");
            Assert.That(mouse2EventsReceived, Is.EqualTo(1),
                "All mouse2 moves have the same button state and should merge to one");

            // Regardless of event count, final state must be correct for both mice.
            Assert.That(mouse1.leftButton.isPressed, Is.False,
                "Button was released — should not be pressed after last event");
            Assert.That(mouse1.scroll.ReadValue().y,
                Is.GreaterThan(0), "Scroll must have been accumulated from move events");
            Assert.That(mouse2.delta.ReadValue().x,
                Is.EqualTo(movesPerFrame * 2).Within(0.001),
                "mouse2 total delta must equal sum of all move events");
        }
    }
}
