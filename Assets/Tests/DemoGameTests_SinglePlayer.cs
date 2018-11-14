using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;

// Single-player mode tests for the demo game.

partial class DemoGameTests
{
    [Test]
    [Category("Demo")]
    public void Demo_SinglePlayer_CanStartGame()
    {
        Click("SinglePlayerButton");

        Assert.That(game.isSinglePlayer, Is.True);
        Assert.That(game.isMultiPlayer, Is.False);
        Assert.That(game.state, Is.EqualTo(DemoGame.State.InGame));
        Assert.That(game.players, Has.Count.EqualTo(1));
        Assert.That(game.players[0].score, Is.Zero);
        Assert.That(game.players[0].controls.gameplay.enabled);
        Assert.That(!game.players[0].controls.menu.enabled);
        Assert.That(game.fish, Is.Not.Null);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanEndGame()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/menu");
        Click("ExitButton");

        Assert.That(game.state, Is.EqualTo(DemoGame.State.InMainMenu));
        Assert.That(game.players, Has.Count.Zero);
        Assert.That(game.mainMenuCanvas.enabled, Is.True);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanEndGame_AndStartNewGame()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/menu");
        Click("ExitButton");
        Click("SinglePlayerButton");

        Assert.Fail();
    }

    ////REVIEW: a number of these tests we can probably just share between single and multiplayer

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanBringUpInGameMenu()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/menu");

        Assert.That(game.players[0].menuUI.activeSelf, Is.True);
        Assert.That(game.players[0].inGameUI.activeSelf, Is.False);
        Assert.That(game.players[0].controls.gameplay.enabled, Is.False);
        Assert.That(game.players[0].controls.menu.enabled, Is.True);
        Assert.That(game.mainMenuCanvas.enabled, Is.False);
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Keyboard")]
    [Property("Device", "Mouse")]
    public void Demo_SinglePlayer_CanSwitchBetweenControlSchemesOnTheFly()
    {
        Click("SinglePlayerButton");

        Press(Gamepad.all[0].buttonSouth);

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));

        Press(Key.A);

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.KeyboardMouseScheme));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    public void Demo_SinglePlayer_CanSwitchBetweenMultipleGamepads()
    {
        Click("SinglePlayerButton");

        // Press A button on gamepad #0.
        Press(Gamepad.all[0].buttonSouth);

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.GetAssignedInputDevices(), Is.EquivalentTo(new[] {Gamepad.all[0]}));

        ////REVIEW: because of the interactions on the fire button, we need to release before we can press
        ////        again; should this be dealt with and the switching be allowed?

        // Release A button on gamepad #0 and press it on gamepad #1.
        Release(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[1].buttonSouth);

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.GetAssignedInputDevices(), Is.EquivalentTo(new[] {Gamepad.all[1]}));
    }
}
