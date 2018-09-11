using System;
using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;

//on platforms, we probably want to hook this up to system stuff; look at the Xbox API

public class UserTests : InputTestFixture
{
    public override void TearDown()
    {
        InputUser.s_AllUserCount = 0;
        InputUser.s_AllUsers = null;
        InputUser.s_OnChange = new InlinedArray<Action<InputUser, InputUserChange>>();

        base.TearDown();
    }

    [Test]
    [Category("Users")]
    public void Users_HaveNoUsersByDefault()
    {
        Assert.That(InputUser.all, Has.Count.Zero);
    }

    [Test]
    [Category("Users")]
    public void Users_CanManuallyAddAndRemoveUsers()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        Assert.That(InputUser.all, Has.Count.EqualTo(2)); // Plus default user.
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user1));
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user2));

        InputUser.Remove(user1);

        Assert.That(InputUser.all, Has.Count.EqualTo(1));
        Assert.That(InputUser.all, Has.None.SameAs(user1));
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user2));
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanDiscoverUsersFromDevicesReportedByRuntime()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void Users_HaveIndices()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        Assert.That(user1.index, Is.EqualTo(0));
        Assert.That(user2.index, Is.EqualTo(1));

        InputUser.Remove(user1);

        Assert.That(user2.index, Is.EqualTo(0));
    }

    [Test]
    [Category("Users")]
    public void Users_HaveUniqueIds()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        Assert.That(user1.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user2.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user1.id, Is.Not.EqualTo(user2.id));
    }

    [Test]
    [Category("Users")]
    public void Users_CanHaveUserNames()
    {
        var user = InputUser.Add("A");

        Assert.That(user.userName, Is.EqualTo("A"));

        user.userName = "B";

        Assert.That(user.userName, Is.EqualTo("B"));
    }

    [Test]
    [Category("Users")]
    public void Users_CanMonitorForChanges()
    {
        InputUser receivedUser = null;
        InputUserChange? receivedChange = null;

        InputUser.onChange +=
            (usr, change) =>
        {
            Assert.That(receivedChange == null);
            receivedUser = usr;
            receivedChange = change;
        };

        // Added.
        var user = InputUser.Add();

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.Added));

        receivedUser = null;
        receivedChange = null;

        // NameChanged.
        user.userName = "NewName";

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.NameChanged));

        receivedUser = null;
        receivedChange = null;

        // DevicesChanged.
        var device = InputSystem.AddDevice<Gamepad>();
        user.AssignDevice(device);

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.DevicesChanged));

        receivedUser = null;
        receivedChange = null;

        // Same device, no notification.
        user.AssignDevice(device);

        Assert.That(receivedChange, Is.Null);

        // Removed.
        InputUser.Remove(user);

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.Removed));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssignDevicesToUsers()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        user1.AssignDevices(new InputDevice[] {keyboard, mouse});
        user2.AssignDevice(gamepad);

        Assert.That(user1.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(user2.devices, Is.EquivalentTo(new InputDevice[] { gamepad }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssignSameDeviceToMoreThanOneUser()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        user1.AssignDevice(gamepad);
        user2.AssignDevice(gamepad);

        Assert.That(user1.devices, Is.EquivalentTo(new InputDevice[] { gamepad }));
        Assert.That(user2.devices, Is.EquivalentTo(new InputDevice[] { gamepad }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssignDevicesToUserStepByStep()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Gamepad>();
        var device3 = InputSystem.AddDevice<Gamepad>();

        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        user1.AssignDevice(device1);
        user2.AssignDevice(device2);
        user1.AssignDevice(device3);

        Assert.That(user1.devices, Is.EquivalentTo(new InputDevice[] { device1, device3}));
        Assert.That(user2.devices, Is.EquivalentTo(new InputDevice[] {device2}));
    }

    [Test]
    [Category("Users")]
    public void Users_AssigningSameDeviceToSameUserMoreThanOnce_IsIgnored()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.Add();

        user.AssignDevice(device);
        user.AssignDevice(device);
        user.AssignDevice(device);

        Assert.That(user.devices, Is.EquivalentTo(new InputDevice[] {device}));
    }

    [Test]
    [Category("Users")]
    public void Users_AssignedDevices_AreLostWhenUserIsRemoved()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Gamepad>();
        var user = InputUser.Add();

        user.AssignDevice(device1);
        user.AssignDevice(device2);

        InputUser.Remove(user);

        Assert.That(user.devices, Has.Count.Zero);
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanAssignActionMapsToUsers()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_WhenDeviceIsAssigned_TriggersNotification()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_WhenActionMapIsAssigned_TriggersNotification()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanDetectSwitchesInControlScheme()
    {
        Assert.Fail();
    }
}
