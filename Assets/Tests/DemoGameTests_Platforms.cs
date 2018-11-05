using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;

// Platform-specific tests for the demo game.

partial class DemoGameTests
{
    #if UNITY_STANDALONE_OSX || UNITY_EDITOR

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    public void Demo_SinglePlayer_OSX_ChoosesGamepadAsDefaultScheme_IfGamepadIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).AssignableTo<Gamepad>()); // Free to pick either one.
    }

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    public void Demo_SinglePlayer_OSX_ChoosesKeyboardMouseAsDefaultScheme_IfNoGamepadIsPresent()
    {
        Gamepad.all.Each(x => InputSystem.RemoveDevice(x));

        Click("SinglePlayerButton");

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.KeyboardMouseScheme));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).SameAs(keyboard));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).SameAs(mouse));
    }

    ////TODO: the VR scheme should have the same bindings on left and right hand and only make it mandatory to have one of them
    ////      (this will likely require extending the control scheme stuff)

    // Ideally, this should also detect whether the VR headset is actually worn and if not, shouldn't choose
    // VR as the default.
    [Test]
    [Category("Demo")]
    [Property("Platform", "Desktop")]
    [Property("VR", "any")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_Desktop_ChoosesVRControlSchemeIfVRHeadsetIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.VRScheme));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).SameAs(hmd));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).SameAs(leftHand));
        Assert.That(player1.GetAssignedInputDevices(), Has.Exactly(1).SameAs(rightHand));
    }

    #endif
}
