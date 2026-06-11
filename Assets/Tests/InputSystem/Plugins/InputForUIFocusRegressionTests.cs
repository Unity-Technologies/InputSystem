#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Plugins.InputForUI;
using Event = UnityEngine.InputForUI.Event;
using EventProvider = UnityEngine.InputForUI.EventProvider;

// Regression tests for UUM-139662:
// "UI Elements cannot be interacted with in a build when using a touchscreen monitor after using Alt+Tab"
//
// Root cause: InputSystemProvider.OnFocusChanged() does not reset pointer state (m_TouchState, m_MouseState,
// m_PenState, m_SeenTouchEvents, m_SeenPenEvents, m_ResetSeenEventsOnUpdate, m_Events) on focus loss.
// After an Alt+Tab cycle, stale values corrupt event processing for subsequent touch and mouse input.
//
// Fix: Reset all pointer state in OnFocusChanged(false), matching how InputManagerProvider behaves
// (it polls fresh state every frame so stale state is never possible there).
[Category("InputForUI")]
public class InputForUIFocusRegressionTests : InputTestFixture
{
    readonly List<Event> m_RecordedEvents = new List<Event>();
    InputSystemProvider m_Provider;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        m_Provider = new InputSystemProvider();
        EventProvider.SetMockProvider(m_Provider);
        EventProvider.Subscribe(OnEvent);
    }

    [TearDown]
    public override void TearDown()
    {
        EventProvider.Unsubscribe(OnEvent);
        EventProvider.ClearMockProvider();
        m_RecordedEvents.Clear();
        base.TearDown();
    }

    bool OnEvent(in Event ev)
    {
        m_RecordedEvents.Add(ev);
        return true;
    }

    static void FlushEvents()
    {
        // Process pending input events first, then let the provider dispatch them.
        // Mirrors the Update() helper in InputForUITests.
        EventProvider.NotifyUpdate();
        InputSystem.Update();
    }

    // ─── Stale ClickCount ─────────────────────────────────────────────────────
    //
    // Scenario: user taps the touchscreen, Alt+Tabs away and back, then taps again.
    // BackgroundBehavior resets the device on focus loss, but the InputSystemProvider's
    // internal m_TouchState is never cleared — OnFocusChanged(false) only delegates to
    // m_InputEventPartialProvider and touches nothing else.
    //
    // PointerState.Reset() clears ButtonsState and LastPosition but intentionally
    // preserves ClickCount for within-session double-tap detection. After an Alt+Tab
    // that ClickCount is stale: the tap before focus loss left it at 1, so the first
    // tap after focus regain calls OnButtonChange(false→true) starting from 1, producing
    // clickCount=2 in the ButtonPressed event. UIToolkit uses clickCount to distinguish
    // single/double/triple taps — a wrong value causes the element to receive the wrong
    // interaction type and ignore the input.
    //
    // Expected (after fix): OnFocusChanged(false) assigns m_TouchState = default,
    // zeroing every field including ClickCount. The first post-regain tap starts from 0
    // and correctly produces clickCount == 1.
    [Test]
    [Description("Verifies that m_TouchState is reset on focus loss so that the first " +
        "touch press after an Alt+Tab cycle generates a clean ButtonPressed event " +
        "rather than one with stale (stuck-pressed) button state.")]
    public void AfterAltTab_FirstTouchPress_GeneratesCleanButtonPressedEvent()
    {
        InputSystem.settings.backgroundBehavior =
            InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;

        InputSystem.AddDevice<Touchscreen>();

        // ── Phase 1: complete touch tap (press + release) ────────────────────────
        // Use a full tap so the device is in a clean state (no active touch) before the
        // focus cycle. The only accumulated state we care about is ClickCount, which
        // PointerState.Reset() does NOT clear — that is the stale value the fix must address.
        BeginTouch(1, new Vector2(100f, 100f));
        EndTouch(1, new Vector2(100f, 100f));
        FlushEvents();

        // Confirm the tap was recorded so we know the provider is wired up.
        Assert.That(m_RecordedEvents.Count >= 1 &&
            m_RecordedEvents[0] is
            {
                type: Event.Type.PointerEvent,
                asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Touch }
            }, "Pre-condition failed: expected initial ButtonPressed from Touch");

        m_RecordedEvents.Clear();

        // ── Phase 2: Alt+Tab cycle ────────────────────────────────────────────────
        // ScheduleFocusChangedEvent triggers the InputManager's BackgroundBehavior
        // (device disable/reset/re-enable) but does NOT call IEventProviderImpl.OnFocusChanged —
        // that is called by the UnityEngine.InputForUI EventProvider module in response to
        // Application.focusChanged, which cannot be triggered from a unit test. We call it
        // directly on the provider here to reflect what actually happens at runtime.
        m_Provider.OnFocusChanged(false);

        ScheduleFocusChangedEvent(applicationHasFocus: false);
        currentTime += 0.5f;
        ScheduleFocusChangedEvent(applicationHasFocus: true);
        currentTime += 0.5f;

#if UNITY_EDITOR
        // InputUpdateType.Editor is only valid inside the editor; in a player build
        // the focus events are processed through the Dynamic update path.
        InputSystem.Update(InputUpdateType.Editor);
#endif
        InputSystem.Update(InputUpdateType.Dynamic);
        EventProvider.NotifyUpdate();

        m_Provider.OnFocusChanged(true);

        m_RecordedEvents.Clear();

        // ── Phase 3: new touch at a different position after focus regain ─────────
        var freshTouchPosition = new Vector2(200f, 200f);
        BeginTouch(1, freshTouchPosition);
        EndTouch(1, freshTouchPosition);
        FlushEvents();

        // Expect exactly one ButtonPressed event followed by one ButtonReleased event,
        // both with EventSource.Touch and clickCount == 1 (a fresh single tap).
        var pressEvents = m_RecordedEvents.FindAll(e =>
            e.type == Event.Type.PointerEvent &&
            e.asPointerEvent.type == PointerEvent.Type.ButtonPressed &&
            e.asPointerEvent.eventSource == EventSource.Touch);

        Assert.AreEqual(1, pressEvents.Count,
            "Expected exactly one Touch ButtonPressed event after focus regain. " +
            "If stale ButtonsState shows the button as already pressed, OnButtonChange(true,true) " +
            "produces no transition and UIToolkit will not recognise this as a new press.");

        Assert.AreEqual(1, pressEvents[0].asPointerEvent.clickCount,
            "clickCount should be 1 for a fresh single tap after focus regain. " +
            "A stale clickCount indicates m_TouchState was not reset on focus loss.");
    }
}
#endif
