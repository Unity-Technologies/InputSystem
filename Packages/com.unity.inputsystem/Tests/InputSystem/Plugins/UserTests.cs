using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.TestTools.Utils;
using Gyroscope = UnityEngine.Experimental.Input.Gyroscope;

[SuppressMessage("ReSharper", "CheckNamespace")]
internal class UserTests : InputTestFixture
{
    [Test]
    [Category("Users")]
    public void Users_HaveNoUsersByDefault()
    {
        Assert.That(InputUser.all, Has.Count.Zero);
    }

    [Test]
    [Category("Users")]
    public void Users_DoesNotListenToUnpairedDeviceActivityByDefault()
    {
        Assert.That(InputUser.listenForUnpairedDeviceActivity, Is.False);
    }

    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WithNewUser()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (user, change, device) => { receivedChanges.Add(new UserChange(user, change, device)); };

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        Assert.That(user1.valid, Is.True);
        Assert.That(user2.valid, Is.True);
        Assert.That(user1, Is.Not.EqualTo(user2));
        Assert.That(InputUser.all, Is.EquivalentTo(new[] {user1, user2}));
        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(user2.pairedDevices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user1, InputUserChange.Added),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad1),
            new UserChange(user2, InputUserChange.Added),
            new UserChange(user2, InputUserChange.DevicePaired, gamepad2),
        }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WithGivenUser()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (user, change, device) => { receivedChanges.Add(new UserChange(user, change, device)); };

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2, user: user1);

        Assert.That(user1.valid, Is.True);
        Assert.That(user2.valid, Is.True);
        Assert.That(user1, Is.EqualTo(user2));
        Assert.That(InputUser.all, Is.EquivalentTo(new[] {user1}));
        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user1, InputUserChange.Added),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad1),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad2),
        }));

        receivedChanges.Clear();

        // Pairing with already paired device should do nothing.
        InputUser.PerformPairingWithDevice(gamepad1, user: user1);

        Assert.That(receivedChanges, Is.Empty);
        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WithGivenUser_AndUnpairCurrentDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad1);

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        InputUser.PerformPairingWithDevice(gamepad2, user: user,
            options: InputUserPairingOptions.UnpairCurrentDevicesFromUser);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.DeviceUnpaired, gamepad1),
            new UserChange(user, InputUserChange.DevicePaired, gamepad2),
        }));
    }

    // Case 1: Platform requires application to initiate user account pairing explicitly for devices.
    // Example: Xbox, Switch
    // Outcome: PerformPairingWithDevice() will kick of asynchronous operation that requires the application to wait for the result
    //          of the user selecting an account. Completion is indicated by a DeviceConfigurationEvent.
    // Note: The platform *may* still have the ability to provide existing associations of devices with users. This is the
    //       case on Xbox but not the case on Switch. On Switch, it may make sense for the application to intercept
    //       QueryPairedUserAccountCommand and see if, based on a previous run, the application remembers which user was
    //       associated with the device.
    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WhenDeviceNeedsPlatformLevelUserAccountSelection()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        // Start out with device not being paired.
        var returnUserAccountHandle = 0;
        var returnUserAccountName = "";
        var returnUserAccountId = "";
        var returnUserAccountSelectionCancelled = false;

        var gamepadId = runtime.AllocateDeviceId();
        var receivedPairingRequest = false;
        var receivedUserIdRequest = false;
        runtime.SetDeviceCommandCallback(gamepadId,
            (id, command) =>
            {
                unsafe
                {
                    if (command->type == QueryPairedUserAccountCommand.Type)
                    {
                        receivedUserIdRequest = true;
                        var result = InputDeviceCommand.kGenericSuccess;
                        if (returnUserAccountHandle != 0)
                        {
                            var queryPairedUser = (QueryPairedUserAccountCommand*)command;
                            queryPairedUser->handle = (uint)returnUserAccountHandle;
                            queryPairedUser->name = returnUserAccountName;
                            queryPairedUser->id = returnUserAccountId;
                            result |= (long)QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount;
                        }

                        if (returnUserAccountSelectionCancelled)
                            result |= (long)QueryPairedUserAccountCommand.Result.UserAccountSelectionCancelled;
                        return result;
                    }
                    if (command->type == InitiateUserAccountPairingCommand.Type)
                    {
                        Assert.That(receivedPairingRequest, Is.False);
                        receivedPairingRequest = true;
                        return (long)InitiateUserAccountPairingCommand.Result.SuccessfullyInitiated;
                    }
                }

                return InputDeviceCommand.kGenericFailure;
            });

        runtime.ReportNewInputDevice<Gamepad>(gamepadId);

        InputSystem.Update();
        var gamepad = InputSystem.GetDevice<Gamepad>();

        Assert.That(InputUser.all, Has.Count.Zero);
        Assert.That(receivedUserIdRequest, Is.False);
        Assert.That(receivedPairingRequest, Is.False);
        Assert.That(receivedChanges, Is.Empty);

        // Initiate pairing.
        var user = InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(user.valid, Is.True);
        Assert.That(user.platformUserAccountHandle, Is.Null);
        Assert.That(user.platformUserAccountId, Is.Null);
        Assert.That(user.platformUserAccountName, Is.Null);
        ////REVIEW: is this really the most useful behavior? should we always pair?
        Assert.That(user.pairedDevices, Is.Empty); // We pair only on completion of account selection.
        Assert.That(receivedUserIdRequest, Is.True);
        Assert.That(receivedPairingRequest, Is.True);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.Added),
            new UserChange(user, InputUserChange.AccountSelectionInProgress, gamepad),
        }));

        receivedUserIdRequest = false;
        receivedPairingRequest = false;
        receivedChanges.Clear();

        // Pretend it's complete.
        returnUserAccountHandle = 1;
        returnUserAccountName = "TestUser";
        returnUserAccountId = "TestId";

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 1)));
        Assert.That(user.platformUserAccountId, Is.EqualTo("TestId"));
        Assert.That(user.platformUserAccountName, Is.EqualTo("TestUser"));
        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(receivedUserIdRequest, Is.True);
        Assert.That(receivedPairingRequest, Is.False);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountSelectionComplete, gamepad),
            new UserChange(user, InputUserChange.DevicePaired, gamepad),
        }));

        receivedUserIdRequest = false;
        receivedPairingRequest = false;
        receivedChanges.Clear();

        // Force another pairing.
        var secondPairingUser = InputUser.PerformPairingWithDevice(gamepad,
            user: user,
            options: InputUserPairingOptions.ForcePlatformUserAccountSelection);

        Assert.That(secondPairingUser, Is.EqualTo(user));
        Assert.That(receivedUserIdRequest, Is.False); // When we force, shouldn't ask OS for currently paired user.
        Assert.That(receivedPairingRequest, Is.True);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountSelectionInProgress, gamepad),
        }));

        receivedUserIdRequest = false;
        receivedPairingRequest = false;
        receivedChanges.Clear();

        // Cancel account selection.
        returnUserAccountSelectionCancelled = true;

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 1)));
        Assert.That(user.platformUserAccountId, Is.EqualTo("TestId"));
        Assert.That(user.platformUserAccountName, Is.EqualTo("TestUser"));
        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(receivedUserIdRequest, Is.True);
        Assert.That(receivedPairingRequest, Is.False);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountSelectionCancelled, gamepad)
        }));

        receivedUserIdRequest = false;
        receivedPairingRequest = false;
        receivedChanges.Clear();

        // Force another pairing.
        InputUser.PerformPairingWithDevice(gamepad,
            user: user,
            options: InputUserPairingOptions.ForcePlatformUserAccountSelection);

        Assert.That(receivedUserIdRequest, Is.False); // When we force, shouldn't ask OS for currently paired user.
        Assert.That(receivedPairingRequest, Is.True);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountSelectionInProgress, gamepad)
        }));

        receivedUserIdRequest = false;
        receivedPairingRequest = false;
        receivedChanges.Clear();

        // Complete it.
        returnUserAccountHandle = 2;
        returnUserAccountName = "OtherUser";
        returnUserAccountId = "OtherId";
        returnUserAccountSelectionCancelled = false;

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 2)));
        Assert.That(user.platformUserAccountId, Is.EqualTo("OtherId"));
        Assert.That(user.platformUserAccountName, Is.EqualTo("OtherUser"));
        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(receivedUserIdRequest, Is.True);
        Assert.That(receivedPairingRequest, Is.False);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountSelectionComplete, gamepad)
        }));
    }

    // Case 2: Platform has no concept of pairing devices to user accounts.
    // Example: Desktop, Mobile, WebGL
    // Outcome: PerformPairingWithDevice() will not set platform user account information.
    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WhenPlatformHasNoUserAccountDevicePairing()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        // Create a gamepad that does not respond to either QueryPairedUserAccountCommand or InitiateUserAccountPairingCommand.
        runtime.ReportNewInputDevice<Gamepad>();
        InputSystem.Update();
        var gamepad = InputSystem.GetDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(user.valid, Is.True);
        Assert.That(user.platformUserAccountHandle, Is.Null);
        Assert.That(user.platformUserAccountId, Is.Null);
        Assert.That(user.platformUserAccountName, Is.Null);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.Added),
            new UserChange(user, InputUserChange.DevicePaired, gamepad),
        }));
    }

    // Case 3: Platform handles device and user account pairing without application taking part in the process.
    // Example: PS4
    // Outcome: PerformPairingWithDevice() returns user with account details filled in for the device.
    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_WhenDeviceHasPairedUserAccountAtPlatformLevel()
    {
        // Report a gamepad paired to a user at the platform level.
        runtime.ReportNewInputDevice<Gamepad>(userHandle: 1, userName: "TestUser",
            userId: "TestId");
        InputSystem.Update();
        var gamepad = InputSystem.GetDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(user.valid, Is.True);
        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 1)));
        Assert.That(user.platformUserAccountName, Is.EqualTo("TestUser"));
        Assert.That(user.platformUserAccountId, Is.EqualTo("TestId"));

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        // Change the user account on the device.
        runtime.AssociateInputDeviceWithUser(gamepad, userHandle: 2, userName: "OtherUser", userId: "OtherId");
        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 2)));
        Assert.That(user.platformUserAccountName, Is.EqualTo("OtherUser"));
        Assert.That(user.platformUserAccountId, Is.EqualTo("OtherId"));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.AccountChanged, gamepad)
        }));

        receivedChanges.Clear();

        // Send config change event but do *NOT* change user account. Make sure
        // the system won't claim the account has changed.

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(user.platformUserAccountHandle, Is.EqualTo(new InputUserAccountHandle("Test", 2)));
        Assert.That(user.platformUserAccountName, Is.EqualTo("OtherUser"));
        Assert.That(user.platformUserAccountId, Is.EqualTo("OtherId"));
        Assert.That(receivedChanges, Is.Empty);
    }

    [Test]
    [Category("Users")]
    public void Users_CanPairDevice_ToMoreThanOneUser()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad);
        var user2 = InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(user1, Is.Not.EqualTo(user2));
        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] {gamepad}));
        Assert.That(user2.pairedDevices, Is.EquivalentTo(new[] {gamepad}));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user1, InputUserChange.Added),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad),
            new UserChange(user2, InputUserChange.Added),
            new UserChange(user2, InputUserChange.DevicePaired, gamepad),
        }));
    }

    [Test]
    [Category("Users")]
    public void Users_CannotPairSameDeviceToUserMoreThanOnce()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.PerformPairingWithDevice(gamepad);

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        InputUser.PerformPairingWithDevice(gamepad, user: user);

        Assert.That(receivedChanges, Is.Empty);
    }

    [Test]
    [Category("Users")]
    public void Users_CanUnpairDevices()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        InputUser.PerformPairingWithDevice(gamepad2, user: user1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad1);

        user1.UnpairDevice(gamepad1);

        Assert.That(user1.valid, Is.True);
        Assert.That(user2.valid, Is.True);
        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(user2.pairedDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user1, InputUserChange.Added),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad1),
            new UserChange(user1, InputUserChange.DevicePaired, gamepad2),
            new UserChange(user2, InputUserChange.Added),
            new UserChange(user2, InputUserChange.DevicePaired, gamepad1),
            new UserChange(user1, InputUserChange.DeviceUnpaired, gamepad1),
        }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanUnpairDevicesAndRemoveUser()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad1);
        InputUser.PerformPairingWithDevice(gamepad2, user: user);

        user.UnpairDevicesAndRemoveUser();

        Assert.That(user.valid, Is.False);
        Assert.That(InputUser.all, Is.Empty);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.Added),
            new UserChange(user, InputUserChange.DevicePaired, gamepad1),
            new UserChange(user, InputUserChange.DevicePaired, gamepad2),
            new UserChange(user, InputUserChange.DeviceUnpaired, gamepad1),
            new UserChange(user, InputUserChange.DeviceUnpaired, gamepad2),
            new UserChange(user, InputUserChange.Removed),
        }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanQueryUnpairedDevices()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var touch = InputSystem.AddDevice<Touchscreen>();
        var gyro = InputSystem.AddDevice<Gyroscope>();

        InputUser.PerformPairingWithDevice(gamepad);
        InputUser.PerformPairingWithDevice(keyboard);
        InputUser.PerformPairingWithDevice(mouse, user: InputUser.all[1]);

        using (var unusedDevices = InputUser.GetUnpairedInputDevices())
        {
            Assert.That(unusedDevices, Has.Count.EqualTo(2));
            Assert.That(unusedDevices, Has.Exactly(1).SameAs(touch));
            Assert.That(unusedDevices, Has.Exactly(1).SameAs(gyro));
        }
    }

    [Test]
    [Category("Users")]
    public void Users_HaveIndices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        Assert.That(user1.index, Is.EqualTo(0));
        Assert.That(user2.index, Is.EqualTo(1));
        Assert.That(InputUser.all, Is.EquivalentTo(new[] {user1, user2}));

        // Remove first user.
        user1.UnpairDevicesAndRemoveUser();

        Assert.That(InputUser.all, Is.EquivalentTo(new[] { user2 }));
        Assert.That(user2.index, Is.EqualTo(0));
    }

    [Test]
    [Category("Users")]
    public void Users_HaveUniqueIds()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        Assert.That(user1.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user2.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user1.id, Is.Not.EqualTo(user2.id));

        user1.UnpairDevicesAndRemoveUser();

        var user3 = InputUser.PerformPairingWithDevice(gamepad1);

        Assert.That(user3.id, Is.Not.EqualTo(user1.id));
        Assert.That(user3.id, Is.Not.EqualTo(user2.id));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssociatedActionsWithUser()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(InputUser.all[0].actions, Is.Null);

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        InputUser.all[0].AssociateActionsWithUser(asset);

        Assert.That(InputUser.all[0].actions, Is.SameAs(asset));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssociateActionsWithUser_UsingAssetReference()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(InputUser.all[0].actions, Is.Null);

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var reference = new InputActionAssetReference(asset);
        InputUser.all[0].AssociateActionsWithUser(reference);

        Assert.That(InputUser.all[0].actions, Is.SameAs(asset));
    }

    [Test]
    [Category("Users")]
    public void Users_CannotActivateControlSchemeWithoutActions()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.PerformPairingWithDevice(gamepad);

        Assert.That(() => user.ActivateControlScheme("scheme"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Users")]
    public void Users_CanActivateAndDeactivateControlScheme()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.PerformPairingWithDevice(gamepad);

        var actions = new InputActionMap();
        user.AssociateActionsWithUser(actions);

        var gamepadScheme = new InputControlScheme("Gamepad")
            .WithRequiredDevice("<Gamepad>");

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        Assert.That(user.controlScheme, Is.Null);

        user.ActivateControlScheme(gamepadScheme);

        Assert.That(user.controlScheme, Is.EqualTo(gamepadScheme));
        Assert.That(actions.bindingMask, Is.EqualTo(new InputBinding { groups = "Gamepad"}));
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.ControlSchemeChanged)
        }));

        receivedChanges.Clear();

        user.ActivateControlScheme(null);

        Assert.That(user.controlScheme, Is.Null);
        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(actions.bindingMask, Is.Null);
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad })); // Devices should be unaffected by control scheme; should be affected by paired devices only.
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.ControlSchemeChanged)
        }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanActivateControlSchemeByName()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.PerformPairingWithDevice(gamepad);

        var actions = new InputActionMap("TestActions");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(actions);
        var scheme = asset.AddControlScheme("scheme").WithRequiredDevice<Gamepad>().Done();
        user.AssociateActionsWithUser(actions);

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        user.ActivateControlScheme("scheme");

        Assert.That(user.controlScheme, Is.EqualTo(scheme));
        Assert.That(actions.bindingMask, Is.EqualTo(new InputBinding { groups = "scheme" }));
        Assert.That(actions.devices, Is.EqualTo(new[] { gamepad }));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user, InputUserChange.ControlSchemeChanged)
        }));

        Assert.That(() => user.ActivateControlScheme("doesNotExist"),
            Throws.ArgumentException.With.Message.Contains("Cannot find").And.Message.Contains("doesNotExist").And
                .Message.Contains("TestActions"));
    }

    [Test]
    [Category("Users")]
    public void Users_CanActivateControlScheme_AndAutomaticallyPairRemainingDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Mouse>(); // Noise.

        var gamepadScheme = new InputControlScheme("Gamepad")
            .WithRequiredDevice("<Gamepad>")
            .WithRequiredDevice("<Gamepad>")
            .WithOptionalDevice("<Gamepad>");

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        InputUser.PerformPairingWithDevice(gamepad2); // Should not get picked as already used.

        var actions = new InputActionMap();
        user1.AssociateActionsWithUser(actions);

        // First try it without having the devices we need.
        user1.ActivateControlScheme(gamepadScheme)
            .AndPairRemainingDevices();

        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(user1.hasMissingDevices, Is.True);
        Assert.That(user1.controlSchemeMatch.isSuccessfulMatch, Is.False);
        Assert.That(user1.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user1.controlSchemeMatch[1].control, Is.Null);
        Assert.That(user1.controlSchemeMatch[2].control, Is.Null);

        // Now add another gamepad and try again.
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        user1.ActivateControlScheme(gamepadScheme)
            .AndPairRemainingDevices();

        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { gamepad1, gamepad3 }));
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad1, gamepad3 }));
        Assert.That(user1.hasMissingDevices, Is.False);
        Assert.That(user1.controlSchemeMatch.isSuccessfulMatch, Is.True);
        Assert.That(user1.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user1.controlSchemeMatch[1].control, Is.SameAs(gamepad3));
        Assert.That(user1.controlSchemeMatch[2].control, Is.Null); // Still don't have the optional one.
    }

    [Test]
    [Category("Users")]
    public void Users_CanActivateControlScheme_AndManuallyPairMissingDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Mouse>(); // Noise.

        var gamepadScheme = new InputControlScheme("Gamepad")
            .WithRequiredDevice("<Gamepad>")
            .WithRequiredDevice("<Gamepad>");

        var user = InputUser.PerformPairingWithDevice(gamepad1);

        var actions = new InputActionMap();
        user.AssociateActionsWithUser(actions);

        user.ActivateControlScheme(gamepadScheme);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(user.hasMissingDevices, Is.True);
        Assert.That(user.controlSchemeMatch.isSuccessfulMatch, Is.False);
        Assert.That(user.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user.controlSchemeMatch[1].control, Is.Null);

        InputUser.PerformPairingWithDevice(gamepad2, user: user);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
        Assert.That(user.hasMissingDevices, Is.False);
        Assert.That(user.controlSchemeMatch.isSuccessfulMatch, Is.True);
        Assert.That(user.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user.controlSchemeMatch[1].control, Is.SameAs(gamepad2));
    }

    [Test]
    [Category("Users")]
    public void Users_CanFindUserThatPairedToSpecificDevice()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);
        var user3 = InputUser.PerformPairingWithDevice(gamepad2); // Same gamepad on two users.

        Assert.That(InputUser.FindUserPairedToDevice(gamepad1), Is.EqualTo(user1));
        Assert.That(InputUser.FindUserPairedToDevice(gamepad2), Is.EqualTo(user2).Or.EqualTo(user3)); // Either is acceptable.
        Assert.That(InputUser.FindUserPairedToDevice(gamepad3), Is.Null);
    }

    [Test]
    [Category("Users")]
    public void Users_CanDetectUseOfUnpairedDevice()
    {
        // Instead of adding a standard Gamepad, add a custom one that has a noisy gyro
        // control added to it so that we can test whether the system can differentiate the
        // noise of the gyro from real user input.
        const string gamepadWithNoisyGyro = @"
            {
                ""name"" : ""GamepadWithNoisyGyro"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""gyro"", ""layout"" : ""Quaternion"", ""noisy"" : true }
                ]
            }
        ";

        InputUser.listenForUnpairedDeviceActivity = true;

        InputControl receivedControl = null;
        InputUser.onUnpairedDeviceUsed +=
            control =>
        {
            Assert.That(control, Is.Not.Null);
            Assert.That(receivedControl, Is.Null);
            receivedControl = control;
        };

        InputSystem.RegisterLayout(gamepadWithNoisyGyro);
        var gamepad = (Gamepad)InputSystem.AddDevice("GamepadWithNoisyGyro");

        // First send some noise on the gyro.
        InputSystem.QueueDeltaStateEvent((QuaternionControl)gamepad["gyro"], new Quaternion(1, 2, 3, 4));
        InputSystem.Update();

        Assert.That(receivedControl, Is.Null);

        // Now send some real interaction.
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.A));
        InputSystem.Update();

        Assert.That(receivedControl, Is.SameAs(gamepad.aButton));

        receivedControl = null;

        // Now pair the device to a user and try the same thing again.
        var user = InputUser.PerformPairingWithDevice(gamepad);

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.B));
        InputSystem.Update();

        Assert.That(receivedControl, Is.Null);

        receivedControl = null;

        // Unpair the device and turn off the feature to make sure we're not getting a notification.
        user.UnpairDevice(gamepad);
        InputUser.listenForUnpairedDeviceActivity = false;

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.A));
        InputSystem.Update();

        Assert.That(receivedControl, Is.Null);
    }

    [Test]
    [Category("Users")]
    public void Users_CanDetectLossOfAndRegainingDevice()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        var gamepadScheme = new InputControlScheme("Gamepad")
            .WithRequiredDevice("<Gamepad>");

        var actions = new InputActionMap();
        user2.AssociateActionsWithUser(actions);
        user2.ActivateControlScheme(gamepadScheme);

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        InputSystem.RemoveDevice(gamepad3);

        Assert.That(receivedChanges, Is.Empty);
        Assert.That(user2.hasMissingDevices, Is.False);
        Assert.That(user2.lostDevices, Is.Empty);
        Assert.That(user2.controlSchemeMatch[0].control, Is.SameAs(gamepad2));

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(user2.hasMissingDevices, Is.True);
        Assert.That(user2.lostDevices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(user2.actions.devices, Is.Empty);
        Assert.That(user2.controlSchemeMatch[0].control, Is.Null);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user2, InputUserChange.DeviceLost, gamepad2),
            new UserChange(user2, InputUserChange.DeviceUnpaired, gamepad2),
        }));

        receivedChanges.Clear();

        // Re-add the device.
        InputSystem.AddDevice(gamepad2);

        Assert.That(user2.hasMissingDevices, Is.False);
        Assert.That(user2.lostDevices, Is.Empty);
        Assert.That(user2.actions.devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(user2.controlSchemeMatch[0].control, Is.SameAs(gamepad2));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user2, InputUserChange.DeviceRegained, gamepad2),
            new UserChange(user2, InputUserChange.DevicePaired, gamepad2),
        }));
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanApplySettings_WithCustomBindings()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanApplySettings_WithInvertedMouseAxes()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var actionMap = new InputActionMap();
        var positionAction = actionMap.AddAction("position", binding: "<Mouse>/position");
        var deltaAction = actionMap.AddAction("delta", binding: "<Mouse>/delta");

        Vector2? receivedPosition = null;
        Vector2? receivedDelta = null;

        positionAction.performed += ctx => receivedPosition = ctx.ReadValue<Vector2>();
        deltaAction.performed += ctx => receivedDelta = ctx.ReadValue<Vector2>();

        var user = InputUser.PerformPairingWithDevice(mouse);

        user.settings = new InputUserSettings
        {
            invertMouseX = true,
            invertMouseY = true,
        };

        actionMap.Enable();

        InputSystem.QueueStateEvent(mouse, new MouseState
        {
            position = new Vector2(0.123f, 0.234f),
            delta = new Vector2(0.345f, 0.456f),
        });
        InputSystem.Update();

        //Assert.That(receivedPosition, Is.EqualTo(new Vector2());
        Assert.That(receivedDelta, Is.EqualTo(new Vector2(-0.345f, -0.456f)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanApplySettings_WithCustomMouseSensitivity()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanPauseAndResumeHaptics()
    {
        Assert.Fail();
    }

    private struct UserChange
    {
        public InputUser user;
        public InputUserChange change;
        public InputDevice device;

        public UserChange(InputUser user, InputUserChange change, InputDevice device = null)
        {
            this.user = user;
            this.change = change;
            this.device = device;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}({2})", user.id, change, device);
        }
    }
}
