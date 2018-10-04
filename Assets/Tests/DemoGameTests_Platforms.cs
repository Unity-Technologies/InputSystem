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

        Assert.That(game.players[0].GetControlScheme(), Is.EqualTo(game.players[0].controls.GamepadScheme));
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).AssignableTo<Gamepad>());
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
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).AssignableTo<Keyboard>());
        Assert.That(game.players[0].GetAssignedInputDevices(), Has.Exactly(1).AssignableTo<Mouse>());
    }

    #endif
}
