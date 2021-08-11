using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

[SuppressMessage("ReSharper", "CheckNamespace")]
internal class UserTests : CoreTestsFixture
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
        Assert.That(InputUser.listenForUnpairedDeviceActivity, Is.Zero);
    }

    [Test]
    [Category("Users")]
    public void Users_CanCreateUserWithoutPairingDevices()
    {
        var user = InputUser.CreateUserWithoutPairedDevices();

        Assert.That(user.valid, Is.True);
        Assert.That(InputUser.all, Is.EquivalentTo(new[] { user }));
        Assert.That(user.pairedDevices, Is.Empty);

        user.UnpairDevicesAndRemoveUser();

        Assert.That(user.valid, Is.False);
        Assert.That(InputUser.all, Is.Empty);
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
    public unsafe void Users_CanPairDevice_WhenDeviceNeedsPlatformLevelUserAccountSelection()
    {
        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        // Start out with device not being paired.
        var returnUserAccountHandle = 0;
        var returnUserAccountName = "";
        var returnUserAccountId = "";
        var returnUserAccountSelectionCanceled = false;

        var gamepadId = runtime.AllocateDeviceId();
        var receivedPairingRequest = false;
        var receivedUserIdRequest = false;
        runtime.SetDeviceCommandCallback(gamepadId,
            (id, command) =>
            {
                if (command->type == QueryPairedUserAccountCommand.Type)
                {
                    receivedUserIdRequest = true;
                    var result = InputDeviceCommand.GenericSuccess;
                    if (returnUserAccountHandle != 0)
                    {
                        var queryPairedUser = (QueryPairedUserAccountCommand*)command;
                        queryPairedUser->handle = (uint)returnUserAccountHandle;
                        queryPairedUser->name = returnUserAccountName;
                        queryPairedUser->id = returnUserAccountId;
                        result |= (long)QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount;
                    }

                    if (returnUserAccountSelectionCanceled)
                        result |= (long)QueryPairedUserAccountCommand.Result.UserAccountSelectionCanceled;
                    return result;
                }
                if (command->type == InitiateUserAccountPairingCommand.Type)
                {
                    Assert.That(receivedPairingRequest, Is.False);
                    receivedPairingRequest = true;
                    return (long)InitiateUserAccountPairingCommand.Result.SuccessfullyInitiated;
                }

                return InputDeviceCommand.GenericFailure;
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
        returnUserAccountSelectionCanceled = true;

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
            new UserChange(user, InputUserChange.AccountSelectionCanceled, gamepad)
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
        returnUserAccountSelectionCanceled = false;

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
    public void Users_PairingDeviceToUserWithLostDevice_DoesNotCauseLostDevicesToBeCleared_ExceptWhenAlsoUnpairingExistingDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad1);

        InputSystem.RemoveDevice(gamepad1);

        Assert.That(user.pairedDevices, Is.Empty);
        Assert.That(user.lostDevices, Is.EquivalentTo(new[] { gamepad1 }));

        InputUser.PerformPairingWithDevice(gamepad2, user: user);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(user.lostDevices, Is.EquivalentTo(new[] { gamepad1 }));

        InputUser.PerformPairingWithDevice(gamepad3, user: user, options: InputUserPairingOptions.UnpairCurrentDevicesFromUser);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(user.lostDevices, Is.Empty);
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
    public void Users_CanUnpairDevices_WhenUserHasLostDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        InputSystem.RemoveDevice(gamepad1);
        InputSystem.RemoveDevice(gamepad2);

        Assert.That(user1.pairedDevices, Is.Empty);
        Assert.That(user1.lostDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(user2.pairedDevices, Is.Empty);
        Assert.That(user2.lostDevices, Is.EquivalentTo(new[] { gamepad2 }));

        user1.UnpairDevices();

        Assert.That(user1.pairedDevices, Is.Empty);
        Assert.That(user1.lostDevices, Is.Empty);
        Assert.That(user2.pairedDevices, Is.Empty);
        Assert.That(user2.lostDevices, Is.EquivalentTo(new[] { gamepad2 }));

        InputUser.PerformPairingWithDevice(gamepad3, user: user2, options: InputUserPairingOptions.UnpairCurrentDevicesFromUser);

        Assert.That(user1.pairedDevices, Is.Empty);
        Assert.That(user1.lostDevices, Is.Empty);
        Assert.That(user2.pairedDevices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(user2.lostDevices, Is.Empty);
    }

    [Test]
    [Category("Users")]
    public void Users_CanUnpairDevices_FromCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var user = InputUser.PerformPairingWithDevice(gamepad);
        InputUser.PerformPairingWithDevice(mouse); // Noise.

        InputUser.onChange +=
            (inputUser, change, arg3) =>
        {
            if (change == InputUserChange.DeviceLost)
                inputUser.UnpairDevices();
        };

        InputSystem.RemoveDevice(gamepad);

        Assert.That(user.pairedDevices, Is.Empty);
        Assert.That(user.lostDevices, Is.Empty);
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

        var originalUser = user;
        user.UnpairDevicesAndRemoveUser();

        Assert.That(user, Is.EqualTo(default(InputUser)));
        Assert.That(user.valid, Is.False);
        Assert.That(InputUser.all, Is.Empty);
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(originalUser, InputUserChange.Added),
            new UserChange(originalUser, InputUserChange.DevicePaired, gamepad1),
            new UserChange(originalUser, InputUserChange.DevicePaired, gamepad2),
            new UserChange(originalUser, InputUserChange.DeviceUnpaired, gamepad1),
            new UserChange(originalUser, InputUserChange.DeviceUnpaired, gamepad2),
            new UserChange(originalUser, InputUserChange.Removed),
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

        Assert.That(user1.id, Is.Not.EqualTo(InputUser.InvalidId));
        Assert.That(user2.id, Is.Not.EqualTo(InputUser.InvalidId));
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
        Assert.That(user1.hasMissingRequiredDevices, Is.True);
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
        Assert.That(user1.hasMissingRequiredDevices, Is.False);
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
        Assert.That(user.hasMissingRequiredDevices, Is.True);
        Assert.That(user.controlSchemeMatch.isSuccessfulMatch, Is.False);
        Assert.That(user.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user.controlSchemeMatch[1].control, Is.Null);

        InputUser.PerformPairingWithDevice(gamepad2, user: user);

        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
        Assert.That(actions.devices, Is.EquivalentTo(new[] { gamepad1, gamepad2 }));
        Assert.That(user.hasMissingRequiredDevices, Is.False);
        Assert.That(user.controlSchemeMatch.isSuccessfulMatch, Is.True);
        Assert.That(user.controlSchemeMatch[0].control, Is.SameAs(gamepad1));
        Assert.That(user.controlSchemeMatch[1].control, Is.SameAs(gamepad2));
    }

    [Test]
    [Category("Users")]
    public void Users_CanFindUserPairedToSpecificDevice()
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
    public void Users_CanDetectUseOfUnpairedDevice_FromControlThatDoesNotSupportMagnitude()
    {
        ++InputUser.listenForUnpairedDeviceActivity;

        var receivedControls = new List<InputControl>();
        InputUser.onUnpairedDeviceUsed +=
            (control, eventPtr) => { receivedControls.Add(control); };

        var mouse = InputSystem.AddDevice<Mouse>();

        Set(mouse.delta, new Vector2(0, 0.234f));

        Assert.That(receivedControls, Is.EquivalentTo(new[] { mouse.delta.y }));
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

        ++InputUser.listenForUnpairedDeviceActivity;

        var receivedControls = new List<InputControl>();
        InputUser.onUnpairedDeviceUsed +=
            (control, eventPtr) => { receivedControls.Add(control); };

        InputSystem.RegisterLayout(gamepadWithNoisyGyro);
        var gamepad = (Gamepad)InputSystem.AddDevice("GamepadWithNoisyGyro");

        // First send some noise on the gyro.
        Set((QuaternionControl)gamepad["gyro"], new Quaternion(1, 2, 3, 4));

        Assert.That(receivedControls, Is.Empty);

        // Now send some real interaction.
        PressAndRelease(gamepad.buttonSouth);

        Assert.That(receivedControls, Is.EquivalentTo(new[] { gamepad.aButton }));

        receivedControls.Clear();

        // Now pair the device to a user and try the same thing again.
        var user = InputUser.PerformPairingWithDevice(gamepad);

        PressAndRelease(gamepad.buttonEast);

        Assert.That(receivedControls, Is.Empty);

        receivedControls.Clear();

        // Unpair the device and turn off the feature to make sure we're not getting a notification.
        user.UnpairDevice(gamepad);
        --InputUser.listenForUnpairedDeviceActivity;

        PressAndRelease(gamepad.buttonSouth);

        Assert.That(receivedControls, Is.Empty);

        // Turn it back on and actuate two controls on the gamepad. Make sure we get *both* actuations.
        // NOTE: This is important for when use of an unpaired device only does something when certain
        //       controls are used but doesn't do anything if others are used. For example, if the use
        //       of unpaired devices is driving player joining logic but only button presses lead to joins,
        //       then if we were to only send the first actuation we come across, it may be for an
        //       irrelevant control (e.g. sticks on gamepad).

        ++InputUser.listenForUnpairedDeviceActivity;

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(1, 0)}.WithButton(GamepadButton.A));
        InputSystem.Update();

        Assert.That(receivedControls, Has.Count.EqualTo(2));
        Assert.That(receivedControls, Has.Exactly(1).SameAs(gamepad.leftStick.x));
        Assert.That(receivedControls, Has.Exactly(1).SameAs(gamepad.aButton));
    }

    // Touchscreens, because of the unusual TouchState events they receive, are trickier to handle than other
    // types of devices. Make use that InputUser.listenForUnpairedDeviceActivity doesn't choke on such events.
    // (case 1196522)
    [Test]
    [Category("Users")]
    public void Users_CanDetectUseOfUnpairedDevice_WhenDeviceIsTouchscreen()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        var activityWasDetected = false;

        ++InputUser.listenForUnpairedDeviceActivity;
        InputUser.onUnpairedDeviceUsed +=
            (control, eventPtr) =>
        {
            // Because of Touchscreen's state trickery, there's no saying which actual TouchControl
            // the event is for until Touchscreen has actually gone and done its thing and processed
            // the event. So, all we can say here is that 'control' should be part of any of the
            // TouchControls on our Touchscreen.
            Assert.That(control.FindInParentChain<TouchControl>(), Is.Not.Null);
            Assert.That(control.device, Is.SameAs(touchscreen));

            activityWasDetected = true;
        };

        BeginTouch(1, new Vector2(123, 234));

        Assert.That(activityWasDetected);
    }

    // Make sure that if we pair a device from InputUser.onUnpairedDeviceUsed, we don't get any further
    // callbacks.
    [Test]
    [Category("Users")]
    public void Users_CanDetectUseOfUnpairedDevice_AndPairFromCallback()
    {
        ++InputUser.listenForUnpairedDeviceActivity;

        var receivedControls = new List<InputControl>();
        InputUser.onUnpairedDeviceUsed +=
            (control, eventPtr) =>
        {
            InputUser.PerformPairingWithDevice(control.device);
            receivedControls.Add(control);
        };

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(1, 0)}.WithButton(GamepadButton.South));
        InputSystem.Update();

        Assert.That(receivedControls, Has.Count.EqualTo(1));
        Assert.That(receivedControls, Has.Exactly(1).SameAs(gamepad.leftStick.x).Or.SameAs(gamepad.buttonSouth));
    }

    [Test]
    [Category("Users")]
    [TestCase(true)]
    [TestCase(false)]
    public void Users_CanDetectLossOfAndRegainingOfDevice(bool deviceIsOptional)
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        InputUser.PerformPairingWithDevice(gamepad1);
        var user2 = InputUser.PerformPairingWithDevice(gamepad2);

        var gamepadScheme = new InputControlScheme("Gamepad");
        gamepadScheme = gamepadScheme.WithDevice("<Gamepad>", required: !deviceIsOptional);

        var actions = new InputActionMap();
        user2.AssociateActionsWithUser(actions);
        user2.ActivateControlScheme(gamepadScheme);

        var receivedChanges = new List<UserChange>();
        InputUser.onChange +=
            (usr, change, device) => { receivedChanges.Add(new UserChange(usr, change, device)); };

        // Remove unrelated device. Shouldn't affect the user.
        InputSystem.RemoveDevice(gamepad3);

        Assert.That(receivedChanges, Is.Empty);
        Assert.That(user2.hasMissingRequiredDevices, Is.False);
        Assert.That(user2.lostDevices, Is.Empty);
        Assert.That(user2.controlSchemeMatch[0].control, Is.SameAs(gamepad2));

        // Now make the user lose a device.
        InputSystem.RemoveDevice(gamepad2);

        Assert.That(user2.hasMissingRequiredDevices, Is.EqualTo(!deviceIsOptional));
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

        Assert.That(user2.hasMissingRequiredDevices, Is.False);
        Assert.That(user2.lostDevices, Is.Empty);
        Assert.That(user2.actions.devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(user2.controlSchemeMatch[0].control, Is.SameAs(gamepad2));
        Assert.That(receivedChanges, Is.EquivalentTo(new[]
        {
            new UserChange(user2, InputUserChange.DeviceRegained, gamepad2),
            new UserChange(user2, InputUserChange.DevicePaired, gamepad2),
        }));
    }

    // Designed to cause indexing problems in InputUser code, see report/contrib:
    // https://github.com/Unity-Technologies/InputSystem/pull/1359
    [Test]
    [Category("Users")]
    public void Users_CanReacquireDevicesIfSingleDeviceIsPairedWithMultipleUsers()
    {
        // Arrange: Setup three users sharing a single device
        var user1 = InputUser.CreateUserWithoutPairedDevices();
        var user2 = InputUser.CreateUserWithoutPairedDevices();
        var user3 = InputUser.CreateUserWithoutPairedDevices();

        var keyboard = InputSystem.AddDevice<Keyboard>("keyboard");

        InputUser.PerformPairingWithDevice(keyboard, user1);
        InputUser.PerformPairingWithDevice(keyboard, user2);
        InputUser.PerformPairingWithDevice(keyboard, user3);

        // Act: device is lost and goes back to being available
        InputSystem.RemoveDevice(keyboard);
        InputSystem.AddDevice(keyboard);

        // Assert: Lost devices are reacquired
        Assert.That(user1.lostDevices, Is.Empty);
        Assert.That(user2.lostDevices, Is.Empty);
        Assert.That(user3.lostDevices, Is.Empty);

        Assert.AreEqual(1, user1.pairedDevices.Count);
        Assert.AreEqual(1, user2.pairedDevices.Count);
        Assert.AreEqual(1, user3.pairedDevices.Count);

        Assert.AreEqual(keyboard, user1.pairedDevices[0]);
        Assert.AreEqual(keyboard, user2.pairedDevices[0]);
        Assert.AreEqual(keyboard, user3.pairedDevices[0]);
    }

    [Test]
    [Category("Users")]
    public void Users_CanDetectChangeInBindings()
    {
        var actions = new InputActionMap();
        var action = actions.AddAction("action", binding: "<Gamepad>/leftTrigger");
        action.Enable();

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.PerformPairingWithDevice(gamepad1);

        user.AssociateActionsWithUser(actions);

        InputUser? receivedUser = null;
        InputUserChange? receivedChange = null;
        InputDevice receivedDevice = null;

        InputUser.onChange +=
            (u, c, d) =>
        {
            if (c != InputUserChange.ControlsChanged)
                return;

            Assert.That(receivedUser, Is.Null);
            Assert.That(receivedChange, Is.Null);
            Assert.That(receivedDevice, Is.Null);

            receivedUser = u;
            receivedChange = c;
            receivedDevice = d;
        };

        // Rebind.
        action.ApplyBindingOverride("<Gamepad>/rightTrigger");

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(user));
        Assert.That(receivedDevice, Is.Null);

        receivedChange = null;
        receivedUser = null;
        receivedDevice = null;

        // Pair new device.
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputUser.PerformPairingWithDevice(gamepad2, user: user);

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(user));
        Assert.That(receivedDevice, Is.Null);

        receivedChange = null;
        receivedUser = null;
        receivedDevice = null;

        // Unpair device.
        user.UnpairDevice(gamepad1);

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(user));
        Assert.That(receivedDevice, Is.Null);

        receivedChange = null;
        receivedUser = null;
        receivedDevice = null;

        // Remove user and then add new one.
        var oldUser = user;
        user.UnpairDevicesAndRemoveUser();

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(oldUser));
        Assert.That(receivedDevice, Is.Null);

        receivedChange = null;
        receivedUser = null;
        receivedDevice = null;

        user = InputUser.PerformPairingWithDevice(gamepad1);
        user.AssociateActionsWithUser(actions);

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(user));
        Assert.That(receivedDevice, Is.Null);

        receivedChange = null;
        receivedUser = null;
        receivedDevice = null;

        action.ApplyBindingOverride("<Gamepad>/leftTrigger");

        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlsChanged));
        Assert.That(receivedUser, Is.EqualTo(user));
        Assert.That(receivedDevice, Is.Null);
    }

    [Test]
    [Category("Users")]
    public void Users_CanDisconnectAndReconnectDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var user = InputUser.PerformPairingWithDevice(gamepad);

        var changes = new List<InputUserChange>();
        InputUser.onChange +=
            (u, change, device) =>
        {
            Assert.That(u, Is.EqualTo(user));
            Assert.That(device, Is.SameAs(gamepad));
            changes.Add(change);
        };

        InputSystem.RemoveDevice(gamepad);

        Assert.That(changes, Is.EquivalentTo(new[] { InputUserChange.DeviceLost, InputUserChange.DeviceUnpaired }));
        Assert.That(user.lostDevices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(user.pairedDevices, Is.Empty);

        changes.Clear();

        InputSystem.AddDevice(gamepad);

        Assert.That(changes, Is.EquivalentTo(new[] { InputUserChange.DeviceRegained, InputUserChange.DevicePaired }));
        Assert.That(user.lostDevices, Is.Empty);
        Assert.That(user.pairedDevices, Is.EquivalentTo(new[] { gamepad }));
    }

    // https://fogbugz.unity3d.com/f/cases/1327628/
    [Test]
    [Category("Users")]
    public void Users_WhenAddingDevicesToUsers_PairedDevicesOfExistingUsersAreUnaffected()
    {
        var user1 = InputUser.CreateUserWithoutPairedDevices();

        var user1pad1 = InputSystem.AddDevice<Gamepad>("user1pad1");
        var user1pad2 = InputSystem.AddDevice<Gamepad>("user1pad2");

        InputUser.PerformPairingWithDevice(user1pad1, user1);
        InputUser.PerformPairingWithDevice(user1pad2, user1);

        var user2pad = InputSystem.AddDevice<Gamepad>("user2pad");
        var user2 = InputUser.PerformPairingWithDevice(user2pad);

        var user3pad = InputSystem.AddDevice<Gamepad>("user3pad");
        var user3 = InputUser.PerformPairingWithDevice(user3pad);

        var user1pad3 = InputSystem.AddDevice<Gamepad>("user1pad3");
        InputUser.PerformPairingWithDevice(user1pad3, user1);

        Assert.That(user1.pairedDevices, Is.EquivalentTo(new[] { user1pad1, user1pad2, user1pad3 }));
        Assert.That(user2.pairedDevices, Is.EquivalentTo(new[] { user2pad }));
        Assert.That(user3.pairedDevices, Is.EquivalentTo(new[] { user3pad }));
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Users")]
    public void Users_DoNotReactToEditorInput()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        ++InputUser.listenForUnpairedDeviceActivity;
        InputUser.onUnpairedDeviceUsed += (control, eventPtr) => Assert.Fail("Should not react!");

        runtime.PlayerFocusLost();

        Press(gamepad.buttonSouth);

        Assert.That(gamepad.buttonSouth.isPressed, Is.True);
    }

    #endif

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanApplySettings_WithCustomBindings()
    {
        Assert.Fail();
    }

    /*
    TODO: implement InputUser.settings
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
    }*/

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
