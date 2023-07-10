#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Plugins.InputForUI;
using Event = UnityEngine.InputForUI.Event;
using EventProvider = UnityEngine.InputForUI.EventProvider;

// These tests are not meant to test the InputForUI module itself, but rather the integration between the InputForUI
// module and the InputSystem package.
// Be aware that these tests don't account for events dispatched by the InputEventPartialProvider. Those events are
// already tested in the Input Manager provider.
// Also, the internals to test InputEventPartialProvider are not exposed publicly, so we can't test them here.
public class InputForUITests : InputTestFixture
{
    readonly List<Event> m_InputForUIEvents = new List<Event>();
    InputSystemProvider m_InputSystemProvider;

    [SetUp]
    public void SetUp()
    {
        base.Setup();

        var defaultActions = new DefaultInputActions();
        defaultActions.Enable();

        m_InputSystemProvider = new InputSystemProvider();
        EventProvider.SetMockProvider(m_InputSystemProvider);
        // Register at least one consumer so the mock update gets invoked
        EventProvider.Subscribe(InputForUIOnEvent);
    }

    [TearDown]
    public void TearDown()
    {
        EventProvider.Unsubscribe(InputForUIOnEvent);
        EventProvider.ClearMockProvider();
        m_InputForUIEvents.Clear();
    }

    private bool InputForUIOnEvent(in Event ev)
    {
        m_InputForUIEvents.Add(ev);
        return true;
    }

    [Test]
    [Category("InputForUI")]
    public void PointerEventsAreDispatchedFromMouse()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Update();

        PressAndRelease(mouse.leftButton);

        Update();

        Assert.IsTrue(m_InputForUIEvents.Count == 2);
        Assert.That(m_InputForUIEvents[0].type, Is.EqualTo(Event.Type.PointerEvent));
        Assert.That(m_InputForUIEvents[0].asPointerEvent.type, Is.EqualTo(PointerEvent.Type.ButtonPressed));
        Assert.That(m_InputForUIEvents[1].type, Is.EqualTo(Event.Type.PointerEvent));
        Assert.That(m_InputForUIEvents[1].asPointerEvent.type, Is.EqualTo(PointerEvent.Type.ButtonReleased));
    }

    [Test]
    [Category("InputForUI")]
    // Checks that mouse events are ignored when a touch is active.
    // This is to workaround the issue ISXB-269 on Windows.
    public void TouchIsPressedAndMouseEventsAreIgnored()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();
        var mouse = InputSystem.AddDevice<Mouse>();
        // Set initial mouse position to (0.5, 0.5) so that we get a delta when the mouse is moved, to dispatch
        // a pointer move event
        Set(mouse.position, new Vector2(0.5f, 0.5f));
        Update();

        // Start touch and move mouse to the same position to replicate the issue of duplicated Mouse events for
        // Touch events on Windows.
        BeginTouch(1, new Vector2(100f, 0.5f));
        Move(mouse.position, new Vector2(100f, 0.5f));
        Update();

        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0] is Event
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed,
                              eventSource: EventSource.Touch }
        });
    }

    [Test]
    [Category("InputForUI")]
    // Presses a gamepad left stick left and verifies that a navigation move event is dispatched
    public void NavigationMoveWorks()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Update();
        Press(gamepad.leftStick.left);
        Update();
        Release(gamepad.leftStick.left);
        Update();

        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0] is Event
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move,
                                 direction: NavigationEvent.Direction.Left,
                                 eventSource: EventSource.Gamepad}
        });
    }

    [Test]
    [Category("InputForUI")]
    public void SendWheelEvent()
    {
        var kScrollUGUIScaleFactor = 3.0f; // See InputSystemProvider OnScrollWheelPerformed() callback
        var mouse = InputSystem.AddDevice<Mouse>();
        Update();
        // Make the minimum step of scroll delta to be ±1.0f
        Set(mouse.scroll.y, -1f / kScrollUGUIScaleFactor);
        Update();
        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0].asPointerEvent.scroll, Is.EqualTo(new Vector2(0, 1)));
    }

    static void Update()
    {
        EventProvider.NotifyUpdate();
        InputSystem.Update();
    }
}
#endif
