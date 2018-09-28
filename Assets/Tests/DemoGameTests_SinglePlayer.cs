using NUnit.Framework;

// Single-player mode tests for the demo game.

public partial class DemoGameTests
{
    [Test]
    [Category("Demo")]
    public void Demo_CanStartSinglePlayerGame()
    {
        Click("SinglePlayerButton");

        Assert.That(game.state, Is.EqualTo(DemoGame.State.InGame));
        Assert.That(game.players, Has.Count.EqualTo(1));
        Assert.That(game.players[0].score, Is.Zero);
        Assert.That(game.fish, Is.Not.Null);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanReturnToMainMenu()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_ChoosesVRControlSchemeIfVRHeadsetIsPresent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanSwitchBetweenDevicesOnTheFly()
    {
        Assert.Fail();
    }
}
