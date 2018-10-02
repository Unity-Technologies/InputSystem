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
        InputUser.s_AllUserData = null;
        InputUser.s_AllDeviceCount = 0;
        InputUser.s_AllDevices = null;
        InputUser.s_OnChange = new InlinedArray<Action<IInputUser, InputUserChange>>();

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
    public void Users_CanAddAndRemoveUsers()
    {
        var user1 = new TestUser();
        var user2 = new TestUser();

        InputUser.Add(user1);
        InputUser.Add(user2);

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
        var user1 = new TestUser();
        var user2 = new TestUser();

        Assert.That(user1.GetUserIndex(), Is.EqualTo(-1));

        InputUser.Add(user1);
        InputUser.Add(user2);

        Assert.That(user1.GetUserIndex(), Is.EqualTo(0));
        Assert.That(user2.GetUserIndex(), Is.EqualTo(1));

        InputUser.Remove(user1);

        Assert.That(user1.GetUserIndex(), Is.EqualTo(-1));
        Assert.That(user2.GetUserIndex(), Is.EqualTo(0));
    }

    [Test]
    [Category("Users")]
    public void Users_HaveUniqueIds()
    {
        var user1 = new TestUser();
        var user2 = new TestUser();

        Assert.That(user1.GetUserId(), Is.EqualTo(InputUser.kInvalidId));
        Assert.That(user2.GetUserId(), Is.EqualTo(InputUser.kInvalidId));

        InputUser.Add(user1);
        InputUser.Add(user2);

        Assert.That(user1.GetUserId(), Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user2.GetUserId(), Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user1.GetUserId(), Is.Not.EqualTo(user2.GetUserId()));

        InputUser.Remove(user1);

        Assert.That(user1.GetUserId(), Is.EqualTo(InputUser.kInvalidId));
    }

    [Test]
    [Category("Users")]
    public void Users_CanHaveUserNames()
    {
        var user = new TestUser();

        Assert.That(user.GetUserName(), Is.Null);

        InputUser.Add(user);

        Assert.That(user.GetUserName(), Is.Null);

        user.SetUserName("A");

        Assert.That(user.GetUserName(), Is.EqualTo("A"));

        user.SetUserName("B");

        Assert.That(user.GetUserName(), Is.EqualTo("B"));
    }

    [Test]
    [Category("Users")]
    public void Users_CanMonitorForChanges()
    {
        InputUser.Add(new TestUser()); // Noise.
        InputUser.Add(new TestUser()); // Noise.
        var user = new TestUser();

        IInputUser receivedUser = null;
        InputUserChange? receivedChange = null;

        InputUser.onChange +=
            (usr, change) =>
        {
            Assert.That(receivedChange == null);
            receivedUser = usr;
            receivedChange = change;
        };

        // Added.
        InputUser.Add(user);

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.Added));

        receivedUser = null;
        receivedChange = null;

        // NameChanged.
        user.SetUserName("NewName");

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.NameChanged));

        receivedUser = null;
        receivedChange = null;

        // Same name, no notification.
        user.SetUserName("NewName");

        Assert.That(receivedChange, Is.Null);

        // DevicesChanged.
        var device = InputSystem.AddDevice<Gamepad>();
        user.AssignInputDevice(device);

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.DevicesChanged));

        receivedUser = null;
        receivedChange = null;

        // Same device, no notification.
        user.AssignInputDevice(device);

        Assert.That(receivedChange, Is.Null);

        // ControlSchemeChanged.
        user.SetControlScheme("gamepad");

        Assert.That(receivedUser, Is.SameAs(user));
        Assert.That(receivedChange, Is.EqualTo(InputUserChange.ControlSchemeChanged));

        receivedUser = null;
        receivedChange = null;

        // Same control scheme, no notification.
        user.SetControlScheme("gamepad");

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
        var user1 = new TestUser();
        var user2 = new TestUser();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        Assert.That(user1.GetAssignedInputDevices(), Is.Empty);
        Assert.That(user2.GetAssignedInputDevices(), Is.Empty);

        InputUser.Add(user1);
        InputUser.Add(user2);

        user1.AssignInputDevices(new InputDevice[] {keyboard, mouse});
        user2.AssignInputDevice(gamepad);

        Assert.That(user1.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(user2.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] { gamepad }));
    }

    [Test]
    [Category("Users")]
    public void Users_CannotAssignDevicesToUserThatHasNotBeenAdded()
    {
        var user = new TestUser();
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(() => user.AssignInputDevice(device), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssignSameDeviceToMoreThanOneUser()
    {
        var user1 = new TestUser();
        var user2 = new TestUser();

        InputUser.Add(user1);
        InputUser.Add(user2);

        var gamepad = InputSystem.AddDevice<Gamepad>();

        user1.AssignInputDevice(gamepad);
        user2.AssignInputDevice(gamepad);

        Assert.That(user1.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] { gamepad }));
        Assert.That(user2.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] { gamepad }));
    }

    [Test]
    [Category("Users")]
    public void Users_CanAssignDevicesToUserStepByStep()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Gamepad>();
        var device3 = InputSystem.AddDevice<Gamepad>();

        var user1 = new TestUser();
        var user2 = new TestUser();

        InputUser.Add(user1);
        InputUser.Add(user2);

        user1.AssignInputDevice(device1);
        user2.AssignInputDevice(device2);
        user1.AssignInputDevice(device3);

        Assert.That(user1.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] { device1, device3}));
        Assert.That(user2.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] {device2}));
    }

    [Test]
    [Category("Users")]
    public void Users_AssigningSameDeviceToSameUserMoreThanOnce_IsIgnored()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        var user = new TestUser();
        InputUser.Add(user);

        user.AssignInputDevice(device);
        user.AssignInputDevice(device);
        user.AssignInputDevice(device);

        Assert.That(user.GetAssignedInputDevices(), Is.EquivalentTo(new InputDevice[] {device}));
    }

    [Test]
    [Category("Users")]
    public void Users_AssignedDevices_AreLostWhenUserIsRemoved()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Gamepad>();

        var user = new TestUser();
        InputUser.Add(user);

        user.AssignInputDevice(device1);
        user.AssignInputDevice(device2);

        InputUser.Remove(user);

        Assert.That(user.GetAssignedInputDevices(), Has.Count.Zero);
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanAssignActionsToUsers()
    {
        var user = new TestUser();
        InputUser.Add(user);

        var action = new InputAction();

        user.GetInputActions().Push(action);

        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void Users_CanSwitchControlSchemes()
    {
        var user = new TestUser();

        Assert.That(user.GetControlScheme(), Is.Null);

        InputUser.Add(user);

        user.SetControlScheme("scheme");

        Assert.That(user.GetControlScheme(), Is.EqualTo(new InputControlScheme("scheme")));

        user.SetControlScheme(null);

        Assert.That(user.GetControlScheme(), Is.Null);
    }

    [Test]
    [Category("Users")]
    [Ignore("TODO")]
    public void TODO_Users_CanDetectSwitchesInControlScheme()
    {
        Assert.Fail();
    }

    public class TestUser : IInputUser
    {
    }
}
