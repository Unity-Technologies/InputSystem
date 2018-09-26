using NUnit.Framework;
using UnityEngine.Experimental.Input;

public partial class DemoGameTests
{
    #if UNITY_STANDALONE_OSX || UNITY_EDITOR
    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_ChoosesGamepadAsDefaultScheme_IfGamepadIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(game.players[0].user.controlScheme, Is.EqualTo("Gamepad"));
    }

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_ChoosesKeyboardMouseAsDefaultScheme_IfNoGamepadIsPresent()
    {
        InputSystem.RemoveDevice(ps4Gamepad);
        InputSystem.RemoveDevice(xboxGamepad);

        Click("SinglePlayerButton");

        Assert.That(game.players[0].user.controlScheme, Is.EqualTo("Keyboard&Mouse"));
    }

    #endif
}
