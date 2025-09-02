using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Functional testing of built-in device UnityEngine.InputSystem.Keyboard.

partial class CoreTests
{
    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_CanGetKeyCodeFromKeyboardKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard.aKey.keyCode, Is.EqualTo(Key.A));
        Assert.That(keyboard.bKey.keyCode, Is.EqualTo(Key.B));
        Assert.That(keyboard.cKey.keyCode, Is.EqualTo(Key.C));
        Assert.That(keyboard.dKey.keyCode, Is.EqualTo(Key.D));
        Assert.That(keyboard.eKey.keyCode, Is.EqualTo(Key.E));
        Assert.That(keyboard.fKey.keyCode, Is.EqualTo(Key.F));
        Assert.That(keyboard.gKey.keyCode, Is.EqualTo(Key.G));
        // TODO Make this complete
    }
    
    [Test, Description("https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1541")]
    [Category("Devices")]
    public void Devices_Keyboard_AllKeysEnumeratesAllKeyControls()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var index = 0;
        foreach (var key in keyboard.allKeys)
        {
            if (index == 109)
                Debug.Log("TEMP");
            Assert.NotNull(key, $"Key at index {index++} was null");
        }
    }
    
    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_AllKeysShouldContainKeyControlsCorrespondingToAllKeys()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var allKeys = keyboard.allKeys;
        foreach (var key in Enum.GetValues(typeof(Key)).Cast<Key>())
        {
            Assert.That(allKeys.Contains(keyboard.spaceKey), Is.True);
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_SubscriptOperatorCanLookupKeyControlOfCorrespondingKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        foreach (var key in Enum.GetValues(typeof(Key)).Cast<Key>())
        {
            // Key.None is documented as an invalid key so skip it in this test and instead we verify in test below
            // that exception is thrown if attempting to lookup key-control using it.
            if (key == Key.None)
                continue;
            if (key == Key.IMESelected)
                continue;
            
            var keyControl = keyboard[key];
            Assert.That(keyControl, Is.Not.Null);
            Assert.That(keyControl.keyCode, Is.EqualTo(key));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_Keyboard_SubscriptOperatorThrowsForInvalidOrOutOfRangeKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var minKey = Enum.GetValues(typeof(Key)).Cast<Key>().Min();
        var invalidMin = (Key)((int)minKey)-1;
        var maxKey = Enum.GetValues(typeof(Key)).Cast<Key>().Max();
        var invalidMax = (Key)((int)maxKey)+1;
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[Key.None]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[invalidMin]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = keyboard[invalidMax]; });
    }
}
