using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;

// Multi-player mode tests for the demo game.

partial class DemoGameTests
{
    [Test]
    [Category("Demo")]
    public void Demo_MultiPlayer_CanStartGame()
    {
        Click("MultiPlayerButton");

        Assert.That(game.state, Is.EqualTo(DemoGame.State.InGame));
        Assert.That(game.isSinglePlayer, Is.False);
        Assert.That(game.isMultiPlayer, Is.True);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_MultiPlayer_CanEndGame()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    public void Demo_MultiPlayer_PlayerCanJoinByPressingButton()
    {
        Click("MultiPlayerButton");
        Press(gamepad.buttonSouth);

        Assert.That(game.players, Has.Count.EqualTo(1));
        Assert.That(game.players[0].GetControlScheme(), Is.EqualTo(game.players[0].controls.GamepadScheme));
        Assert.That(game.players[0].GetAssignedInputDevices(), Is.EquivalentTo(new[] { gamepad }));
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_MultiPlayer_PlayerCanJoinByTouchingScreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_MultiPlayer_PlayerCannotJoinWithDeviceNotUsableWithDemoControls()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    public void Demo_MultiPlayer_MultiplePlayersCanJoinWithSameTypeOfDevice()
    {
        Click("MultiPlayerButton");

        Press(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[1].buttonSouth);

        Assert.That(game.players, Has.Count.EqualTo(2));
        Assert.That(game.players[0].controls.asset, Is.Not.SameAs(game.players[1].controls.asset));
        Assert.That(game.players[0].GetAssignedInputDevices(), Is.EquivalentTo(new[] {Gamepad.all[0]}));
        Assert.That(game.players[1].GetAssignedInputDevices(), Is.EquivalentTo(new[] {Gamepad.all[1]}));

        Assert.That(game.players[0].controls.gameplay.move.controls,
            Is.Not.EquivalentTo(game.players[1].controls.gameplay.move.controls));

        // Press menu button on gamepad #2 to bring up in-game menu for second player.
        Press(Gamepad.all[1].startButton);

        Assert.That(game.players[0].isInMenu, Is.False);
        Assert.That(game.players[1].isInMenu, Is.True);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_MultiPlayer_PlayersGetSplitScreenArea()
    {
        Assert.Fail();
    }
}
