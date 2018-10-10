using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;

// Platform-specific tests for the demo game.

public partial class DemoGameTests
{
    #if UNITY_STANDALONE_OSX || UNITY_EDITOR

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    public void Demo_SinglePlayer_OSX_ChoosesGamepadAsDefaultScheme_IfGamepadIsPresent()
    {
        Assert.That(ps4Gamepad, Is.Not.Null);
        Assert.That(xboxGamepad, Is.Not.Null);

        Click("SinglePlayerButton");

        ////REVIEW: should this assign *both* gamepads to the player?

        Assert.That(game.players[0].GetControlScheme(), Is.EqualTo(game.players[0].controls.GamepadScheme));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).AssignableTo<Gamepad>()); // Free to pick either one.
    }

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    public void Demo_SinglePlayer_OSX_ChoosesKeyboardMouseAsDefaultScheme_IfNoGamepadIsPresent()
    {
        InputSystem.RemoveDevice(ps4Gamepad);
        InputSystem.RemoveDevice(xboxGamepad);

        Click("SinglePlayerButton");

        Assert.That(game.players[0].GetControlScheme(), Is.EqualTo(game.players[0].controls.KeyboardMouseScheme));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).SameAs(keyboard));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).SameAs(mouse));
    }

    ////TODO: the VR scheme should have the same bindings on left and right hand and only make it mandatory to have one of them
    ////      (this will likely require extending the control scheme stuff)

    // Ideally, this should also detect whether the VR headset is actually worn and if not, shouldn't choose
    // VR as the default.
    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    [Property("VR", "any")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_OSX_ChoosesVRControlSchemeIfVRHeadsetIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(game.players[0].GetControlScheme(), Is.EqualTo(game.players[0].controls.VRScheme));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).SameAs(hmd));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).SameAs(leftHand));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).SameAs(rightHand));
    }

    #endif
}
