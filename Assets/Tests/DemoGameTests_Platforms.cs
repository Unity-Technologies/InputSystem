using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;

// Platform-specific tests for the demo game.

partial class DemoGameTests
{
    #if UNITY_XBOXONE || UNITY_EDITOR

    [Test]
    [Category("Demo")]
    [Property("Platform", "XboxOne")]
    [Ignore("TODO")]
    public unsafe void TODO_Demo_XboxOne_ShowsUserNameWhenJoined()
    {
        input.runtime.SetDeviceCommandCallback(xboxGamepad,
            (id, command) =>
            {
                if (command->type == QueryPairedUserAccountCommand.Type)
                {
                    var queryPairedUser = (QueryPairedUserAccountCommand*)command;
                    queryPairedUser->handle = 1;
                    queryPairedUser->name = "TestUser";
                    return InputDeviceCommand.kGenericSuccess;
                }

                return InputDeviceCommand.kGenericFailure;
            });

        Press(xboxGamepad.aButton);

        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Demo")]
    [Property("Platform", "XboxOne")]
    [Ignore("TODO")]
    public unsafe void TODO_Demo_XboxOne_ShowsAccountPickerIfDeviceIsNotPairedToUser()
    {
        int? returnUserId = null;
        string returnUserName = null;
        var receivedPairingCommand = false;

        input.runtime.SetDeviceCommandCallback(xboxGamepad,
            (id, command) =>
            {
                if (command->type == QueryPairedUserAccountCommand.Type)
                {
                    if (returnUserId != null)
                    {
                        var queryPairUserCommand = (QueryPairedUserAccountCommand*)command;
                        queryPairUserCommand->handle = (ulong)returnUserId.Value;
                        queryPairUserCommand->name = returnUserName;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }
                else if (command->type == InitiateUserAccountPairingCommand.Type)
                {
                    Assert.That(receivedPairingCommand, Is.False);
                    receivedPairingCommand = true;
                    return InputDeviceCommand.kGenericSuccess;
                }
                return InputDeviceCommand.kGenericFailure;
            });

        Press(xboxGamepad.aButton);

        Assert.That(receivedPairingCommand, Is.True);

        ////TODO
        Assert.Fail();
    }

    #endif // UNITY_XBOXONE || UNITY_EDITOR

    #if UNITY_STANDALONE_OSX || UNITY_EDITOR

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_OSX_ChoosesGamepadAsDefaultScheme_IfGamepadIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.GamepadScheme));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).AssignableTo<Gamepad>()); // Free to pick either one.
    }

    [Test]
    [Category("Demo")]
    [Property("Platform", "OSX")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_OSX_ChoosesKeyboardMouseAsDefaultScheme_IfNoGamepadIsPresent()
    {
        Gamepad.all.Each(x => InputSystem.RemoveDevice(x));

        Click("SinglePlayerButton");

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.KeyboardMouseScheme));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).SameAs(keyboard));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).SameAs(mouse));
    }

    ////TODO: the VR scheme should have the same bindings on left and right hand and only make it mandatory to have one of them
    ////      (this will likely require extending the control scheme stuff)

    // Ideally, this should also detect whether the VR headset is actually worn and if not, shouldn't choose
    // VR as the default.
    [Test]
    [Category("Demo")]
    [Property("Platform", "Desktop")]
    [Property("VR", "any")]
    [Ignore("TODO")]
    public void TODO_Demo_SinglePlayer_Desktop_ChoosesVRControlSchemeIfVRHeadsetIsPresent()
    {
        Click("SinglePlayerButton");

        Assert.That(player1.user.controlScheme, Is.EqualTo(player1.controls.VRScheme));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).SameAs(hmd));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).SameAs(leftHand));
        Assert.That(player1.user.pairedDevices, Has.Exactly(1).SameAs(rightHand));
    }

    #endif // UNITY_STANDALONE_OSX || UNITY_EDITOR
}
