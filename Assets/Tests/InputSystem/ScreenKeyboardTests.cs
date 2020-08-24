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
    [InputControlLayout(stateType = typeof(ScreenKeyboardState))]
    class TestScreenKeyboard : ScreenKeyboard
    {
        protected override void InternalShow()
        {
            ReportStatusChange(ScreenKeyboardStatus.Visible);
        }

        protected override void InternalHide()
        {
            ReportStatusChange(ScreenKeyboardStatus.Done);
        }

        /// <summary>
        /// Simulates a method as if user would close the keyboard from UI
        /// </summary>
        internal void SimulateKeybordClose()
        {
            ReportStatusChange(ScreenKeyboardStatus.Done);
        }

        /// <summary>
        /// Simulates a method as if user would would open the keyboard from UI
        /// </summary>
        internal void SimulateKeybordOpen()
        {
            ReportStatusChange(ScreenKeyboardStatus.Visible);
        }
    }

    // TODO:
    // See if callbacks are received at the same frame, callback count
    // Disable selection callbacks, when no input field is present
    // Return selection 0, 0, whhen inmput field is not present
    [Test]
    public void DeviceCanBeEnabledDisabled()
    {
        var keyboard = InputSystem.AddDevice<TestScreenKeyboard>();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
        Assert.AreEqual(false, keyboard.enabled);

        // Check Show method
        keyboard.Show(new ScreenKeyboardShowParams());
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);
        Assert.AreEqual(true, keyboard.enabled);

        // Check Hide method
        keyboard.Hide();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
        Assert.AreEqual(false, keyboard.enabled);

        // Check EnableDevice method, should behave the same as Show
        // But since EnableDevice cannot pass ShowParams, Show method must be present
        InputSystem.EnableDevice(keyboard);
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);
        Assert.AreEqual(true, keyboard.enabled);

        // Check if keyboard class behaves correctly if user closes the keyboard via UI
        keyboard.SimulateKeybordClose();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
        Assert.AreEqual(false, keyboard.enabled);

        keyboard.SimulateKeybordOpen();
        InputSystem.Update();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);
        Assert.AreEqual(true, keyboard.enabled);


        InputSystem.RemoveDevice(keyboard);
    }
}
