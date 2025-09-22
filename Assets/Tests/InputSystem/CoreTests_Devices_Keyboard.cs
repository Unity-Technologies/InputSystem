using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Functional testing of built-in device UnityEngine.InputSystem.Keyboard.

partial class CoreTests
{
    private static readonly Key[] sKeys = Enum.GetValues(typeof(Key)).Cast<Key>().ToArray();

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_CanGetKeyCodeFromKeyboardKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard.spaceKey.keyCode, Is.EqualTo(Key.Space));
        Assert.That(keyboard.enterKey.keyCode, Is.EqualTo(Key.Enter));
        Assert.That(keyboard.tabKey.keyCode, Is.EqualTo(Key.Tab));
        Assert.That(keyboard.backquoteKey.keyCode, Is.EqualTo(Key.Backquote));
        Assert.That(keyboard.quoteKey.keyCode, Is.EqualTo(Key.Quote));
        Assert.That(keyboard.semicolonKey.keyCode, Is.EqualTo(Key.Semicolon));
        Assert.That(keyboard.commaKey.keyCode, Is.EqualTo(Key.Comma));
        Assert.That(keyboard.periodKey.keyCode, Is.EqualTo(Key.Period));
        Assert.That(keyboard.slashKey.keyCode, Is.EqualTo(Key.Slash));
        Assert.That(keyboard.backslashKey.keyCode, Is.EqualTo(Key.Backslash));
        Assert.That(keyboard.leftBracketKey.keyCode, Is.EqualTo(Key.LeftBracket));
        Assert.That(keyboard.rightBracketKey.keyCode, Is.EqualTo(Key.RightBracket));
        Assert.That(keyboard.minusKey.keyCode, Is.EqualTo(Key.Minus));
        Assert.That(keyboard.equalsKey.keyCode, Is.EqualTo(Key.Equals));

        Assert.That(keyboard.aKey.keyCode, Is.EqualTo(Key.A));
        Assert.That(keyboard.bKey.keyCode, Is.EqualTo(Key.B));
        Assert.That(keyboard.cKey.keyCode, Is.EqualTo(Key.C));
        Assert.That(keyboard.dKey.keyCode, Is.EqualTo(Key.D));
        Assert.That(keyboard.eKey.keyCode, Is.EqualTo(Key.E));
        Assert.That(keyboard.fKey.keyCode, Is.EqualTo(Key.F));
        Assert.That(keyboard.gKey.keyCode, Is.EqualTo(Key.G));
        Assert.That(keyboard.hKey.keyCode, Is.EqualTo(Key.H));
        Assert.That(keyboard.iKey.keyCode, Is.EqualTo(Key.I));
        Assert.That(keyboard.jKey.keyCode, Is.EqualTo(Key.J));
        Assert.That(keyboard.kKey.keyCode, Is.EqualTo(Key.K));
        Assert.That(keyboard.lKey.keyCode, Is.EqualTo(Key.L));
        Assert.That(keyboard.mKey.keyCode, Is.EqualTo(Key.M));
        Assert.That(keyboard.nKey.keyCode, Is.EqualTo(Key.N));
        Assert.That(keyboard.oKey.keyCode, Is.EqualTo(Key.O));
        Assert.That(keyboard.pKey.keyCode, Is.EqualTo(Key.P));
        Assert.That(keyboard.qKey.keyCode, Is.EqualTo(Key.Q));
        Assert.That(keyboard.rKey.keyCode, Is.EqualTo(Key.R));
        Assert.That(keyboard.sKey.keyCode, Is.EqualTo(Key.S));
        Assert.That(keyboard.tKey.keyCode, Is.EqualTo(Key.T));
        Assert.That(keyboard.uKey.keyCode, Is.EqualTo(Key.U));
        Assert.That(keyboard.vKey.keyCode, Is.EqualTo(Key.V));
        Assert.That(keyboard.wKey.keyCode, Is.EqualTo(Key.W));
        Assert.That(keyboard.xKey.keyCode, Is.EqualTo(Key.X));
        Assert.That(keyboard.yKey.keyCode, Is.EqualTo(Key.Y));
        Assert.That(keyboard.zKey.keyCode, Is.EqualTo(Key.Z));

        Assert.That(keyboard.digit1Key.keyCode, Is.EqualTo(Key.Digit1));
        Assert.That(keyboard.digit2Key.keyCode, Is.EqualTo(Key.Digit2));
        Assert.That(keyboard.digit3Key.keyCode, Is.EqualTo(Key.Digit3));
        Assert.That(keyboard.digit4Key.keyCode, Is.EqualTo(Key.Digit4));
        Assert.That(keyboard.digit5Key.keyCode, Is.EqualTo(Key.Digit5));
        Assert.That(keyboard.digit6Key.keyCode, Is.EqualTo(Key.Digit6));
        Assert.That(keyboard.digit7Key.keyCode, Is.EqualTo(Key.Digit7));
        Assert.That(keyboard.digit8Key.keyCode, Is.EqualTo(Key.Digit8));
        Assert.That(keyboard.digit9Key.keyCode, Is.EqualTo(Key.Digit9));
        Assert.That(keyboard.digit0Key.keyCode, Is.EqualTo(Key.Digit0));

        Assert.That(keyboard.leftShiftKey.keyCode, Is.EqualTo(Key.LeftShift));
        Assert.That(keyboard.rightShiftKey.keyCode, Is.EqualTo(Key.RightShift));
        Assert.That(keyboard.leftAltKey.keyCode, Is.EqualTo(Key.LeftAlt));
        Assert.That(keyboard.rightAltKey.keyCode, Is.EqualTo(Key.RightAlt));
        Assert.That(keyboard.leftCtrlKey.keyCode, Is.EqualTo(Key.LeftCtrl));
        Assert.That(keyboard.rightCtrlKey.keyCode, Is.EqualTo(Key.RightCtrl));
        Assert.That(keyboard.leftMetaKey.keyCode, Is.EqualTo(Key.LeftMeta));
        Assert.That(keyboard.rightMetaKey.keyCode, Is.EqualTo(Key.RightMeta));
        Assert.That(keyboard.leftWindowsKey.keyCode, Is.EqualTo(Key.LeftWindows));
        Assert.That(keyboard.rightWindowsKey.keyCode, Is.EqualTo(Key.RightWindows));
        Assert.That(keyboard.leftAppleKey.keyCode, Is.EqualTo(Key.LeftApple));
        Assert.That(keyboard.rightAppleKey.keyCode, Is.EqualTo(Key.RightApple));
        Assert.That(keyboard.leftCommandKey.keyCode, Is.EqualTo(Key.LeftCommand));
        Assert.That(keyboard.rightCommandKey.keyCode, Is.EqualTo(Key.RightCommand));
        Assert.That(keyboard.contextMenuKey.keyCode, Is.EqualTo(Key.ContextMenu));
        Assert.That(keyboard.escapeKey.keyCode, Is.EqualTo(Key.Escape));
        Assert.That(keyboard.leftArrowKey.keyCode, Is.EqualTo(Key.LeftArrow));
        Assert.That(keyboard.rightArrowKey.keyCode, Is.EqualTo(Key.RightArrow));
        Assert.That(keyboard.upArrowKey.keyCode, Is.EqualTo(Key.UpArrow));
        Assert.That(keyboard.downArrowKey.keyCode, Is.EqualTo(Key.DownArrow));
        Assert.That(keyboard.backspaceKey.keyCode, Is.EqualTo(Key.Backspace));
        Assert.That(keyboard.pageDownKey.keyCode, Is.EqualTo(Key.PageDown));
        Assert.That(keyboard.pageUpKey.keyCode, Is.EqualTo(Key.PageUp));
        Assert.That(keyboard.homeKey.keyCode, Is.EqualTo(Key.Home));
        Assert.That(keyboard.endKey.keyCode, Is.EqualTo(Key.End));
        Assert.That(keyboard.insertKey.keyCode, Is.EqualTo(Key.Insert));
        Assert.That(keyboard.deleteKey.keyCode, Is.EqualTo(Key.Delete));
        Assert.That(keyboard.capsLockKey.keyCode, Is.EqualTo(Key.CapsLock));
        Assert.That(keyboard.scrollLockKey.keyCode, Is.EqualTo(Key.ScrollLock));
        Assert.That(keyboard.numLockKey.keyCode, Is.EqualTo(Key.NumLock));
        Assert.That(keyboard.printScreenKey.keyCode, Is.EqualTo(Key.PrintScreen));
        Assert.That(keyboard.pauseKey.keyCode, Is.EqualTo(Key.Pause));
        Assert.That(keyboard.numpadEnterKey.keyCode, Is.EqualTo(Key.NumpadEnter));
        Assert.That(keyboard.numpadDivideKey.keyCode, Is.EqualTo(Key.NumpadDivide));
        Assert.That(keyboard.numpadMultiplyKey.keyCode, Is.EqualTo(Key.NumpadMultiply));
        Assert.That(keyboard.numpadMinusKey.keyCode, Is.EqualTo(Key.NumpadMinus));
        Assert.That(keyboard.numpadPlusKey.keyCode, Is.EqualTo(Key.NumpadPlus));
        Assert.That(keyboard.numpadPeriodKey.keyCode, Is.EqualTo(Key.NumpadPeriod));
        Assert.That(keyboard.numpadEqualsKey.keyCode, Is.EqualTo(Key.NumpadEquals));
        Assert.That(keyboard.numpad0Key.keyCode, Is.EqualTo(Key.Numpad0));
        Assert.That(keyboard.numpad1Key.keyCode, Is.EqualTo(Key.Numpad1));
        Assert.That(keyboard.numpad2Key.keyCode, Is.EqualTo(Key.Numpad2));
        Assert.That(keyboard.numpad3Key.keyCode, Is.EqualTo(Key.Numpad3));
        Assert.That(keyboard.numpad4Key.keyCode, Is.EqualTo(Key.Numpad4));
        Assert.That(keyboard.numpad5Key.keyCode, Is.EqualTo(Key.Numpad5));
        Assert.That(keyboard.numpad6Key.keyCode, Is.EqualTo(Key.Numpad6));
        Assert.That(keyboard.numpad7Key.keyCode, Is.EqualTo(Key.Numpad7));
        Assert.That(keyboard.numpad8Key.keyCode, Is.EqualTo(Key.Numpad8));
        Assert.That(keyboard.numpad9Key.keyCode, Is.EqualTo(Key.Numpad9));
        Assert.That(keyboard.f1Key.keyCode, Is.EqualTo(Key.F1));
        Assert.That(keyboard.f2Key.keyCode, Is.EqualTo(Key.F2));
        Assert.That(keyboard.f3Key.keyCode, Is.EqualTo(Key.F3));
        Assert.That(keyboard.f4Key.keyCode, Is.EqualTo(Key.F4));
        Assert.That(keyboard.f5Key.keyCode, Is.EqualTo(Key.F5));
        Assert.That(keyboard.f6Key.keyCode, Is.EqualTo(Key.F6));
        Assert.That(keyboard.f7Key.keyCode, Is.EqualTo(Key.F7));
        Assert.That(keyboard.f8Key.keyCode, Is.EqualTo(Key.F8));
        Assert.That(keyboard.f9Key.keyCode, Is.EqualTo(Key.F9));
        Assert.That(keyboard.f10Key.keyCode, Is.EqualTo(Key.F10));
        Assert.That(keyboard.f11Key.keyCode, Is.EqualTo(Key.F11));
        Assert.That(keyboard.f12Key.keyCode, Is.EqualTo(Key.F12));
        Assert.That(keyboard.oem1Key.keyCode, Is.EqualTo(Key.OEM1));
        Assert.That(keyboard.oem2Key.keyCode, Is.EqualTo(Key.OEM2));
        Assert.That(keyboard.oem3Key.keyCode, Is.EqualTo(Key.OEM3));
        Assert.That(keyboard.oem4Key.keyCode, Is.EqualTo(Key.OEM4));
        Assert.That(keyboard.oem5Key.keyCode, Is.EqualTo(Key.OEM5));
        Assert.That(keyboard.f13Key.keyCode, Is.EqualTo(Key.F13));
        Assert.That(keyboard.f14Key.keyCode, Is.EqualTo(Key.F14));
        Assert.That(keyboard.f15Key.keyCode, Is.EqualTo(Key.F15));
        Assert.That(keyboard.f16Key.keyCode, Is.EqualTo(Key.F16));
        Assert.That(keyboard.f17Key.keyCode, Is.EqualTo(Key.F17));
        Assert.That(keyboard.f18Key.keyCode, Is.EqualTo(Key.F18));
        Assert.That(keyboard.f19Key.keyCode, Is.EqualTo(Key.F19));
        Assert.That(keyboard.f20Key.keyCode, Is.EqualTo(Key.F20));
        Assert.That(keyboard.f21Key.keyCode, Is.EqualTo(Key.F21));
        Assert.That(keyboard.f22Key.keyCode, Is.EqualTo(Key.F22));
        Assert.That(keyboard.f23Key.keyCode, Is.EqualTo(Key.F23));
        Assert.That(keyboard.f24Key.keyCode, Is.EqualTo(Key.F24));
        Assert.That(keyboard.mediaPlayPause.keyCode, Is.EqualTo(Key.MediaPlayPause));
        Assert.That(keyboard.mediaRewind.keyCode, Is.EqualTo(Key.MediaRewind));
        Assert.That(keyboard.mediaForward.keyCode, Is.EqualTo(Key.MediaForward));
    }

    [Test, Description("https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1541")]
    [Category("Devices")]
    public void Devices_Keyboard_AllKeysEnumeratesAllKeyControls()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var index = 0;
        foreach (var key in keyboard.allKeys)
        {
            Assert.NotNull(key, $"Key at index {index++} was null");
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_AllKeysShouldContainKeyControlsCorrespondingToAllKeys()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var allKeys = keyboard.allKeys;
        foreach (var key in sKeys)
        {
            // Key.None is documented as an invalid key so skip it in this test.
            // Sub-script operator is documented to through for invalid key.
            if (key == Key.None)
                continue;

            Assert.That(allKeys.Contains(keyboard[key]), Is.True);
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_SubscriptOperatorCanLookupKeyControlOfCorrespondingKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        foreach (var key in sKeys)
        {
            // Key.None is documented as an invalid key so skip it in this test.
            // Sub-script operator is documented to through for invalid key.
            if (key == Key.None)
                continue;

            var keyControl = keyboard[key];
            Assert.That(keyControl, Is.Not.Null);
            Assert.That(keyControl.keyCode, Is.EqualTo(key));
        }
    }

    [Test(Description = "Verifies documented requirement 'This is equivalent to <c>allKeys[(int)key - 1]'")]
    [Category("Devices")]
    public void Devices_Keyboard_SubscriptOperatorShouldBeEquivalentToShiftedAllKeysLookup()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var allKeys = keyboard.allKeys;
        foreach (var key in sKeys)
        {
            // Key.None is documented as an invalid key so skip it in this test.
            // Sub-script operator is documented to through for invalid key.
            if (key == Key.None)
                continue;

            var keyControl = keyboard[key];
            Assert.That(keyControl, Is.EqualTo(allKeys[(int)key - 1]));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_SubscriptOperatorThrowsForInvalidOrOutOfRangeKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var minKey = Enum.GetValues(typeof(Key)).Cast<Key>().Min();
        var invalidMin = (Key)((int)minKey) - 1;
        var maxKey = Enum.GetValues(typeof(Key)).Cast<Key>().Max();
        var invalidMax = (Key)((int)maxKey) + 1;
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[Key.None]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[invalidMin]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[invalidMax]; });
    }
}
