using NUnit.Framework;

// Single-player mode tests for the demo game.

public partial class DemoGameTests
{
    [Test]
    [Category("Demo")]
    public void Demo_SinglePlayer_CanStartGame()
    {
        Click("SinglePlayerButton");

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
    public void TODO_Demo_SinglePlayer_CanBringUpInGameMenu()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/escape");

        Assert.That(game.players[0].ui.enabled, Is.True);
        Assert.That(game.players[0].controls.gameplay.enabled, Is.False);
        Assert.That(game.players[0].controls.menu.enabled, Is.True);
        Assert.That(game.mainMenuCanvas.enabled, Is.False);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanReturnToMainMenu()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/escape");
        Click("ExitButton");

        Assert.That(game.state, Is.EqualTo(DemoGame.State.InMainMenu));
        Assert.That(game.players, Has.Count.Zero);
        Assert.That(game.mainMenuCanvas.enabled, Is.True);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanReturnToMainMenu_AndStartNewGame()
    {
        Click("SinglePlayerButton");
        Trigger("gameplay/escape");
        Click("ExitButton");
        Click("SinglePlayerButton");

        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanSwitchBetweenDevicesOnTheFly()
    {
        Click("SinglePlayerButton");

        Assert.Fail();
    }
}
