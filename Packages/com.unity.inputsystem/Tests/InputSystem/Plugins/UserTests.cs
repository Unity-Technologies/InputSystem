using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;

//on platforms, we probably want to hook this up to system stuff; look at the Xbox API

public class UserTests : InputTestFixture
{
    public override void Setup()
    {
        base.Setup();

        // Add support for use management to the system.
        InputUserSupport.Initialize();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_HaveSingleUserByDefault()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_CanDiscoverUsersFromDevicesReportedByRuntime()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_CanManuallyAddAndRemoveUsers()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        Assert.That(InputUser.all, Has.Count.EqualTo(2));
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user1));
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user2));

        InputUser.Remove(user1);

        Assert.That(InputUser.all, Has.Count.EqualTo(1));
        Assert.That(InputUser.all, Has.None.SameAs(user1));
        Assert.That(InputUser.all, Has.Exactly(1).SameAs(user2));
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_WhenUserIsAdded_TriggersNotification()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_HaveIndices()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_HaveUniqueIds()
    {
        var user1 = InputUser.Add();
        var user2 = InputUser.Add();

        Assert.That(user1.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user2.id, Is.Not.EqualTo(InputUser.kInvalidId));
        Assert.That(user1.id, Is.Not.EqualTo(user2.id));

        // Try to add user with same ID.
        Assert.That(() => InputUser.Add(user1.id), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_CanAssignDevicesToUsers()
    {
        var user = InputUser.Add();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        //user.AssignDevices(gamepad);

        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_CanAssignActionMapsToUsers()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_WhenDeviceIsAssigned_TriggersNotification()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_WhenActionMapIsAssigned_TriggersNotification()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Users")]
    public void TODO_Users_CanDetectSwitchesInControlScheme()
    {
        Assert.Fail();
    }
}
