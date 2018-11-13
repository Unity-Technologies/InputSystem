using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Property = NUnit.Framework.PropertyAttribute;

public partial class DemoGameTests : DemoGameTestFixture
{
    [Test]
    [Category("Demo")]
    [Property("Device", "Keyboard")]
    [Property("Device", "Mouse")]
    [Property("Device", "Gamepad")]
    [Property("Device", "Gamepad")]
    public void Demo_ShowsUIHintsAccordingToCurrentControlScheme()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(GO<Text>("ControlsHint").text, Is.EqualTo("Tap A to fire, hold to charge"));

        Press(mouse.leftButton);

        ////REVIEW: should we display a different hint while charging? maybe just display "Charging..." instead of
        ////        having a dedicated charging UI?

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.KeyboardMouseScheme));
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
    public unsafe void TODO_Demo_FiringShot_RumblesDevice()
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
    public void Demo_CanEnterAndExitInGameMenu()
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
    public void Demo_CannotControlPlayerWhileInMenu()
    {
        Click("SinglePlayerButton");

        Trigger(game.players[0].controls.gameplay.menu);

        Assert.That(game.players[0].controls.gameplay.move.enabled, Is.False);
        Assert.That(game.players[0].controls.gameplay.look.enabled, Is.False);
        Assert.That(game.players[0].controls.gameplay.fire.enabled, Is.False);
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
}
