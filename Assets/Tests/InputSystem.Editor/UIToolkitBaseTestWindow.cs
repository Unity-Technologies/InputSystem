// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS && UNITY_6000_0_OR_NEWER

using NUnit.Framework;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UIToolkitBaseTestWindow<T> where T : EditorWindow
{
    protected T m_Window;

    #region setup and teardown
    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        TestUtils.MockDialogs();
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        TestUtils.RestoreDialogs();
    }

    [UnitySetUp]
    public virtual IEnumerator UnitySetup()
    {
        if (m_Window == null) yield break;
        yield return WaitForNotDirty();
    }

    [TearDown]
    public virtual void TearDown()
    {
        m_Window?.Close();
    }

    #endregion

    #region Helper methods

    /// <summary>
    /// Simulate a click at the center of the target element
    /// </summary>
    /// <param name="target"></param>
    protected void SimulateClickOn(VisualElement target)
    {
        Event evtd = new Event();
        evtd.type = EventType.MouseDown;
        evtd.mousePosition = target.worldBound.center;
        evtd.clickCount = 1;
        using var pde = PointerDownEvent.GetPooled(evtd);
        target.SendEvent(pde);

        Event evtu = new Event();
        evtu.type = EventType.MouseUp;
        evtu.mousePosition = target.worldBound.center;
        evtu.clickCount = 1;
        using var pue = PointerUpEvent.GetPooled(evtu);
        target.SendEvent(pue);
    }

    /// <summary>
    /// Simulate typing text into the window
    /// It will be dispatched by the panel the currently focused element
    /// </summary>
    /// <param name="text">The text to sned</param>
    /// <param name="sendReturnKeyAfterTyping">Send a Return key after typing the last character</param>
    protected void SimulateTypingText(string text, bool sendReturnKeyAfterTyping = true)
    {
        foreach (var character in text)
        {
            var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = character };
            using var kde = KeyDownEvent.GetPooled(evtd);
            m_Window.rootVisualElement.SendEvent(kde);

            var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.None, character = character };
            using var kue = KeyUpEvent.GetPooled(evtu);
            m_Window.rootVisualElement.SendEvent(kue);
        }
        if (sendReturnKeyAfterTyping)
        {
            SimulateReturnKey();
        }
    }

    /// <summary>
    /// Simulate a Return key press
    /// It will be dispatched by the panel the currently focused element
    /// </summary>
    protected void SimulateReturnKey()
    {
        var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.Return };
        using var kde = KeyDownEvent.GetPooled(evtd);
        var evtd2 = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = '\n' };
        using var kde2 = KeyDownEvent.GetPooled(evtd2);
        m_Window.rootVisualElement.SendEvent(kde);
        m_Window.rootVisualElement.SendEvent(kde2);

        var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.Return };
        using var kue = KeyUpEvent.GetPooled(evtu);
        m_Window.rootVisualElement.SendEvent(kue);
    }

    /// <summary>
    /// Simulate sending a Delete Command
    /// It will be dispatched by the panel the currently focused element
    /// </summary>
    protected void SimulateDeleteCommand()
    {
        var evt = new Event() { type = EventType.ExecuteCommand, commandName = "Delete" };
        using var ce = ExecuteCommandEvent.GetPooled(evt);
        m_Window.rootVisualElement.SendEvent(ce);
    }

    /// <summary>
    /// Wait for UI toolkit scheduler to process the frame
    /// </summary>
    /// <param name="timeoutSecs">Maximum time to wait in seconds.</param>
    protected IEnumerator WaitForSchedulerLoop(double timeoutSecs = 5.0)
    {
        bool done = false;
        m_Window.rootVisualElement.schedule.Execute(() => done = true);
        return WaitUntil(() => done == true, "WaitForSchedulerLoop", timeoutSecs);
    }

    /// <summary>
    /// Wait for the visual element to be focused
    /// </summary>
    /// <param name="ve">VisualElement to be focused</param>
    /// <param name="timeoutSecs">Maximum time to wait in seconds.</param>
    protected IEnumerator WaitForFocus(VisualElement ve, double timeoutSecs = 5.0)
    {
        return WaitUntil(() => ve.focusController.focusedElement == ve, "WaitForFocus", timeoutSecs);
    }

    /// <summary>
    /// Wait for the windows to be not dirty
    /// </summary>
    /// <param name="timeoutSecs">Maximum time to wait in seconds.</param>
    protected IEnumerator WaitForNotDirty(double timeoutSecs = 5.0)
    {
        return WaitUntil(() => m_Window.rootVisualElement.panel.isDirty == false, "WaitForNotDirty", timeoutSecs);
    }

    /// <summary>
    /// Wait until the action is true or the timeout is reached with an assertion
    /// </summary>
    /// <param name="action">Lambda to call between frame</param>
    /// <param name="assertMessage">Assert Message</param>
    /// <param name="timeoutSecs">Maximum time to wait in seconds.</param>
    protected IEnumerator WaitUntil(Func<bool> action, string assertMessage, double timeoutSecs = 5.0)
    {
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            if (action()) yield break;
            yield return null;
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(action(), assertMessage);
    }

    #endregion
}
#endif
