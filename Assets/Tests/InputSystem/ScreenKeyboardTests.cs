using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools.Utils;

internal class ScreenKeybordTests
{
    [InputControlLayout(stateType = typeof(ScreenKeyboardState))]
    class TestScreenKeyboard : ScreenKeyboard
    {

    }

    [Test]
    public void CorrectlyHandleStatus()
    {
       var keyboard = InputSystem.AddDevice<TestScreenKeyboard>();
       Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status.ReadValue());
       InputSystem.RemoveDevice(keyboard);
    }
}