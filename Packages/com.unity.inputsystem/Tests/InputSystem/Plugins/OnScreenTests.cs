using NUnit.Framework;
using UnityEngine.Experimental.Input;

public class OnScreenTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanCreateOnScreenDevice()
    {
        Assert.Fail();
        /*
        var gameObject = new GameObject();
        var button1 = gameObject.AddComponent<OnScreenButton>();
        var button2 = gameObject.AddComponent<OnScreenButton>();
        var stick1 = gameObject.AddComponent<OnSceenStick>();
        var stick2 = gameObject.AddComponent<OnSceenStick>();

        var device = InputSystem.devices.FirstOrDefault(x => x is OnScreenDevice);
        Assert.That(device, Is.Not.Null);

        Assert.That(device["button1"], Is.Not.Null);
        Assert.That(device["button2"], Is.Not.Null);
        Assert.That(device["stick1"], Is.Not.Null);
        Assert.That(device["stick2"], Is.Not.Null);
        */
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanShowAndHideOnScreenKeyboard()
    {
        Assert.Fail();
    }
}
