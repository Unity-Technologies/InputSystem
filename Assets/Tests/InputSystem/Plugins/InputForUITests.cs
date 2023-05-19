#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Plugins.InputForUI;
using Event = UnityEngine.InputForUI.Event;
using EventProvider = UnityEngine.InputForUI.EventProvider;

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
        Assert.That(m_InputForUIEvents[1].type, Is.EqualTo(Event.Type.PointerEvent));
    }

    static void Update()
    {
        EventProvider.NotifyUpdate();
        InputSystem.Update();
    }
}
#endif
