using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// Test suite to verify test fixture API published in <see cref="InputTestFixture"/>.
/// </summary>
/// <remarks>
/// This test fixture captures confusion around usage reported in
/// https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1637.
/// </remarks>
internal class InputTestFixtureTests : InputTestFixture
{
    private Keyboard correctlyUsedDevice;
    private Keyboard incorrectlyUsedDevice;

    [OneTimeSetUp]
    public void UnitySetup()
    {
        // This is incorrect use since it will add a device to the actual input system since it executes before
        // the InputTestFixture.Setup() method. Hence, after Setup() has executed the device is not part of
        // the test input system instance.
        incorrectlyUsedDevice = InputSystem.AddDevice<Keyboard>();
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);
    }

    [OneTimeTearDown]
    public void UnityTearDown()
    {
        // Once InputTestFixture.TearDown() has executed, the state stack will have been popped and the keyboard
        // we added before entering the test fixture should have been restored.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);
    }

    [SetUp]
    public override void Setup()
    {
        // At this point we are still using the actual system so our device created from UnitySetup() should still
        // exist with the context.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);

        // This is expected usage pattern, first calling base.Setup() when overriding Setup like this, then
        // creating a fake device via the test fixture instance that only lives with the test context.
        base.Setup();
        correctlyUsedDevice = InputSystem.AddDevice<Keyboard>();

        // Since we have now entered a temporary test state our device created in UnitySetup() will no longer exist
        // with this context.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.False);
    }

    [TearDown]
    public override void TearDown()
    {
        // This is expected usage pattern, we might want to do something with the device here, but it needs
        // to happen before base.TearDown() since it would delete the fake device.
        Assert.That(InputSystem.devices.Contains(correctlyUsedDevice), Is.True);
        InputSystem.RemoveDevice(correctlyUsedDevice);

        // Restore state
        base.TearDown();

        // Our test device should no longer exist with the system since we are back to real instance
        Assert.That(InputSystem.devices.Contains(correctlyUsedDevice), Is.False);
    }

    #region Editor playmode tests

    [Test]
    public void Press_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        Press(correctlyUsedDevice.spaceKey);
        Assert.That(correctlyUsedDevice.spaceKey.isPressed, Is.True);
    }

    [Test]
    public void Press_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.That(incorrectlyUsedDevice.spaceKey.isPressed, Is.False);
        Assert.Throws<ArgumentException>(() => Press(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void Release_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        Press(correctlyUsedDevice.spaceKey);
        Release(correctlyUsedDevice.spaceKey);
        Assert.That(correctlyUsedDevice.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void Release_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.That(incorrectlyUsedDevice.spaceKey.isPressed, Is.False);
        Assert.Throws<ArgumentException>(() => Release(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void PressAndRelease_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        PressAndRelease(correctlyUsedDevice.spaceKey);
        Assert.That(correctlyUsedDevice.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void PressAndRelease_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => PressAndRelease(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void Click_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        Click(correctlyUsedDevice.spaceKey);
        Assert.That(correctlyUsedDevice.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void Click_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Click(correctlyUsedDevice.spaceKey);
        Assert.Throws<ArgumentException>(() => Click(incorrectlyUsedDevice.spaceKey));
    }

    // TODO Add remaining

    #endregion // Playmode tests

    #region // Edit-mode tests

    [UnityTest]
    public IEnumerator Press_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        Assert.Throws<NotSupportedException>(() => Press(correctlyUsedDevice.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator Release_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        Assert.Throws<NotSupportedException>(() => Release(correctlyUsedDevice.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator PressAndRelease_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        Assert.Throws<NotSupportedException>(() => PressAndRelease(correctlyUsedDevice.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator Click_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        Assert.Throws<NotSupportedException>(() => Click(correctlyUsedDevice.spaceKey));
        yield break;
    }

    #endregion // Edit-mode tests
}
