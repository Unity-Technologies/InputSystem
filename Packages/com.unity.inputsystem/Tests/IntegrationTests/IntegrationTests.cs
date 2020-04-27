using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

// Disable irrelevant warning about there not being underscores in method names.
#pragma warning disable CA1707

// These tests are the only ones that we put *in* the package. The rest of our tests live in Assets/Tests and run separately
// from our CI and not through upm-ci. This also means that IntegrationTests is the only thing we put on trunk through our
// verified package.
//
// Rationale:
// (1) Our APIVerificationTests have extra package requirements and thus need a custom package manifest.json. This will not
//     work with upm-ci.
// (2) The tests we have in Assets/Tests exercise the input system in isolation. Having these run on trunk in addition to our
//     CI in the input system repo adds little value while adding extra execution time to trunk QV runs. This is unlike
//     the integration tests here which add value to trunk by making sure the input system is intact all the way through
//     to the native input module.
// (3) If we added everything in Assets/Tests to the package, we would add more stuff to user projects that has no value to users.
//
// NOTE: The tests here are necessary to pass the requirement imposed by upm-ci that a package MUST have tests in it.

public class IntegrationTests
{
    [Test]
    [Category("Integration")]
    public void Integration_CanSendAndReceiveEvents()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();

            Assert.That(keyboard.aKey.isPressed, Is.True);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
    }

    #if UNITY_2019_3_OR_NEWER && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)

    [UnityTest]
    [Category("Integration")]
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
    [Category("Integration")]
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

    #endif // UNITY_2019_3_OR_NEWER && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
}
