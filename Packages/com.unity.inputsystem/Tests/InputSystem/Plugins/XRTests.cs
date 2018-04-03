using NUnit.Framework;
using UnityEngine.Experimental.Input;

public class XRTests : InputTestFixture
{
    /*
    ////TODO: make the same kind of functionality work for aliases
    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        var controller = InputSystem.AddDevice("XRController");

        Assert.That(controller.usages, Has.Count.EqualTo(0));

        InputSystem.SetUsage(controller, CommonUsages.LeftHand);

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(XRController.leftHand, Is.SameAs(controller));

        InputSystem.SetUsage(controller, CommonUsages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.SameAs(controller));
        Assert.That(XRController.leftHand, Is.Not.SameAs(controller));
    }
    */
}
