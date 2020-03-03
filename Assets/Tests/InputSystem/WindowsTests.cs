#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

partial class WindowsTests
{
    [UnityTest]
    [Category("WindowsInput")]
    public IEnumerator WindowsInput_RemoteDesktopMouseMovements_AreDetected()
    {
        var mouse = InputSystem.GetDevice<Mouse>();
        var currentPosition = mouse.position.ReadValue();

        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to not change when no input was sent. Please do not move the mouse during this test.");

        WinUserInput.SendRDPMouseMoveEvent(10, 10);
        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending RDP/absolute values.");
        currentPosition = mouse.position.ReadValue();

        WinUserInput.SendRDPMouseMoveEvent(100, 100);
        yield return new WaitForSeconds(0.1f);
        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending RDP/absolute values.");
    }

    [UnityTest]
    [Category("WindowsInput")]
    public IEnumerator WindowsInput_MouseMovements_AreDetected()
    {
        var mouse = InputSystem.GetDevice<Mouse>();
        var currentPosition = mouse.position.ReadValue();

        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to not change when no input was sent. Please do not move the mouse during this test.");

        WinUserInput.SendMouseMoveEvent(10, 10);
        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending relative values.");
        currentPosition = mouse.position.ReadValue();

        WinUserInput.SendMouseMoveEvent(100, 100);
        yield return new WaitForSeconds(0.1f);
        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending relative values.");
    }
}

#endif
