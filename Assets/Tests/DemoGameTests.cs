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
    [Ignore("TODO")]
    public void TODO_Demo_ShowsUIHintsAccordingToCurrentControlScheme()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(GO<Text>("ControlsHint").text, Is.EqualTo("Tap A button to fire, hold to charge."));

        Press(mouse.leftButton);

        ////REVIEW: should we display a different hint while charging?

        Assert.That(player1.GetControlScheme(), Is.EqualTo(player1.controls.KeyboardMouseScheme));
        Assert.That(GO<Text>("ControlsHint").text, Is.EqualTo("Tap LMB to fire, hold to charge."));
    }

    [Test]
    [Category("Demo")]
    [Property("Device", "Gamepad")]
    [Ignore("TODO")]
    public void TODO_Demo_CanFireSingleProjectile()
    {
        Click("SinglePlayerButton");

        Press(gamepad.buttonSouth);
        Release(gamepad.buttonSouth);

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
    public void TODO_Demo_FiringShot_RumblesDevice()
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
    public void TODO_Demo_CanEnterInGameMenu()
    {
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
}
