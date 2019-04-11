using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Property = NUnit.Framework.PropertyAttribute;

public partial class DemoGameTests : DemoGameTestFixture
{
    [Test]
    [Category("Demo")]
    public void Demo_StartsWithMainMenuActiveAndStartGameButtonSelected()
    {
        Assert.That(game.state, Is.EqualTo(DemoGame.State.InMainMenu));
        Assert.That(game.mainMenuCamera.gameObject.activeSelf, Is.True);
        Assert.That(game.mainMenuCanvas.gameObject.activeSelf, Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(GO("StartGameButton")));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_CanLaunchIntoLobby_ThroughSubmit()
    {
        Press(gamepad.aButton);

        Assert.That(game.state, Is.EqualTo(DemoGame.State.InLobby));
        Assert.That(game.isInSinglePlayerGame, Is.False);
        Assert.That(game.isInMultiPlayerGame, Is.False);

        // Should no longer display main menu.
        Assert.That(game.mainMenuCamera.gameObject.activeSelf, Is.False);
        Assert.That(game.mainMenuCanvas.gameObject.activeSelf, Is.False);

        ////TODO: check that game displays prompt for additional users to join (should be up as long as we are in the lobby)
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Mouse")]
    [Ignore("TODO")]
    public void TODO_Demo_CanLaunchIntoLobby_ThroughClick()
    {
        ////TODO: click button through click action
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_AutomaticallyJoinsPlayerWhoClickedStartGameButton()
    {
        Press(gamepad.aButton);

        Assert.That(game.players, Has.Count.EqualTo(1));
        Assert.That(game.players[0].state, Is.EqualTo(DemoPlayerController.State.Joined));
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_AdditionalPlayersCanJoinInLobbyByPressingButton()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_AdditionalPlayersCanJoinInLobbyByPressingTouchingScreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_PlayerCannotJoinWithDeviceNotUsableWithDemoControls()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_MultiplePlayersCanJoinWithSameTypeOfDevice()
    {
        Click("MultiPlayerButton");

        Press(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[1].buttonSouth);

        Assert.That(game.players, Has.Count.EqualTo(2));
        Assert.That(game.players[0].controls, Is.Not.SameAs(game.players[1].controls));
        Assert.That(game.players[0].user.pairedDevices, Is.EquivalentTo(new[] {Gamepad.all[0]}));
        Assert.That(game.players[1].user.pairedDevices, Is.EquivalentTo(new[] {Gamepad.all[1]}));

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
    public void TODO_Demo_EachPlayerGetsSplitscreenArea()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_CanLaunchGameWithSinglePlayer()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanLaunchGameWithMultiplePlayers()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_GameIsTerminatedWhenLastPlayerQuits()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanStartNewGameAfterQuittingPreviousOne()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanEnterAndExitInGameMenu()
    {
        Click("SinglePlayerButton");

        Assert.That(game.players[0].isInMenu, Is.False);

        Trigger(game.players[0].controls.gameplay.menu);

        Assert.That(game.players[0].isInMenu, Is.True);

        Trigger(game.players[0].controls.gameplay.menu);

        Assert.That(game.players[0].isInMenu, Is.False);
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CannotControlPlayerWhileInLobby()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CannotControlPlayerWhileInMenu()
    {
        Click("SinglePlayerButton");

        Trigger(game.players[0].controls.gameplay.menu);

        Assert.That(game.players[0].controls.gameplay.move.enabled, Is.False);
        Assert.That(game.players[0].controls.gameplay.look.enabled, Is.False);
        Assert.That(game.players[0].controls.gameplay.fire.enabled, Is.False);
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Keyboard")]
    [Property("Device", "Mouse")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_CanSwitchBetweenControlSchemesOnTheFly()
    {
        // Press A button on gamepad #0 twice. First to start game, then to signal
        // we're ready.
        Press(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[0].buttonSouth);

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.user.pairedDevices, Is.EquivalentTo(new[] {gamepad}));

        Press(Key.A);

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.KeyboardMouseScheme));
        Assert.That(player1.user.pairedDevices, Is.EquivalentTo(new InputDevice[] {keyboard, mouse}));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_WhenInSinglePlayer_CanSwitchBetweenMultipleGamepads()
    {
        // Press A button on gamepad #0 twice. First to start game, then to signal
        // we're ready.
        Press(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[0].buttonSouth);

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.user.pairedDevices, Is.EquivalentTo(new[] {Gamepad.all[0]}));

        ////REVIEW: because of the interactions on the fire button, we need to release before we can press
        ////        again; should this be dealt with and the switching be allowed?

        // Release A button on gamepad #0 and press it on gamepad #1.
        Release(Gamepad.all[0].buttonSouth);
        Press(Gamepad.all[1].buttonSouth);

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.user.pairedDevices, Is.EquivalentTo(new[] {Gamepad.all[1]}));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Keyboard")]
    [Property("Device", "Mouse")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_ShowsUIHintsAccordingToCurrentControlScheme()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(GO<Text>("ControlsHint").text, Is.EqualTo("Tap A to fire, hold to charge"));

        Press(mouse.leftButton);

        ////REVIEW: should we display a different hint while charging? maybe just display "Charging..." instead of
        ////        having a dedicated charging UI?

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.KeyboardMouseScheme));
        Assert.That(GO<Text>("ControlsHint").text, Is.EqualTo("Tap LMB to fire, hold to charge"));

        ////TODO: switch back and make sure we're not allocating GC memory
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_CanLookAround()
    {
        Click("SinglePlayerButton");

        var initialRotation = game.players[0].transform.eulerAngles;

        // Move leftStick all the way up.
        Set(gamepad.leftStick.y, 1);

        game.players[0].Update();

        var newRotation = game.players[0].transform.eulerAngles;

        Assert.Fail();
    }

    ////FIXME: there's a problem with running Unity updates; we're probably getting interferences from the InputManager we have pushed on the stack
    [UnityTest]
    [Category("Demo")]
    [Ignore("TODO")]
    public IEnumerator TODO_Demo_CanFireSingleProjectile()
    {
        Click("SinglePlayerButton");
        Trigger(game.players[0].controls.gameplay.fire);

        // Wait for physics update so we can check the projectile's Rigidbody properties.
        yield return new WaitForFixedUpdate();

        Assert.That(projectiles, Has.Length.EqualTo(1));
        Assert.That(projectiles[0].GetComponent<Rigidbody>().velocity.magnitude, Is.GreaterThan(0));
    }

    [UnityTest]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public IEnumerator TODO_Demo_CanFireChargedSetOfProjectiles()
    {
        Click("SinglePlayerButton");

        var kShotsPerSecond = player1.burstSpeed;
        var kTimeToCompleteBurst = kShotsPerSecond * DemoPlayerController.DelayBetweenBurstProjectiles;

        // Press button, hold for 1 second, then release.
        Press(gamepad.buttonSouth);
        Release(gamepad.buttonSouth, 1);

        // Charged shots are released over time so wait
        yield return new WaitForSeconds(kTimeToCompleteBurst + 0.1f);

        Assert.That(projectiles, Has.Length.EqualTo(kShotsPerSecond));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public unsafe void TODO_Demo_RumblesDeviceWhenFiringShot()
    {
        float? highFreqMotor = null;
        float? lowFreqMotor = null;

        input.runtime.SetDeviceCommandCallback(gamepad,
            (id, command) =>
            {
                unsafe
                {
                    if (command->type == DualMotorRumbleCommand.Type)
                    {
                        Assert.That(highFreqMotor, Is.Null);
                        Assert.That(lowFreqMotor, Is.Null);

                        var rumbleCommand = (DualMotorRumbleCommand*)command;

                        highFreqMotor = rumbleCommand->highFrequencyMotorSpeed;
                        lowFreqMotor = rumbleCommand->lowFrequencyMotorSpeed;
                    }
                }
                return InputDeviceCommand.kGenericFailure;
            });

        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanQuitGame()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanRebindControls()
    {
        Click("SingePlayerButton");
        Trigger(game.players[0].controls.gameplay.menu);
        Click("OptionsButton");

        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_CanProcess4PlayersInLessThan2PercentFrameTime()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_DoesNotProduceGarbageWhenDeviceIsUnpluggedAndPluggedBackIn()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_WhenPlayersDeviceIsDisconnected_ShowsControllerLostStatus()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Ignore("TODO")]
    public void TODO_Demo_WhenPlayersDeviceIsRecisconnected_()
    {
        Assert.Fail();
    }
}
