using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools.Utils;

internal class ScreenKeybordTests : InputTestFixture
{
    // TODO:
    // See if callbacks are received at the same frame, callback count
    // Disable selection callbacks, when no input field is present
    // Return selection 0, 0, whhen inmput field is not present
    [Test]
    public void DeviceCanBeEnabledDisabled()
    {
        var keyboard = (FakeScreenKeyboard)runtime.screenKeyboard;
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);

        // Check Show method
        keyboard.Show(new ScreenKeyboardShowParams());
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);

        // Check Hide method
        keyboard.Hide();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);

        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);

        // Check if keyboard class behaves correctly if user closes the keyboard via UI
        keyboard.SimulateKeybordClose();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);

        keyboard.SimulateKeybordOpen();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);
    }
}
