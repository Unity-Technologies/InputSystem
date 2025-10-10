using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Test suite to verify test fixture API published in <see cref="InputTestFixture"/>.
/// </summary>
/// <remarks>
/// This test fixture captures confusion around usage reported in
/// https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1637.
/// </remarks>
internal class InputTestFixtureTests : InputTestFixture
{
    private Keyboard correctlyUsedKeyboard;

    private Keyboard incorrectlyUsedDevice;
    private Gamepad incorrectlyUsedGamepad;
    private Touchscreen incorrectlyUsedTouchscreen;

    [OneTimeSetUp]
    public void UnitySetup()
    {
        // This is incorrect use since it will add a device to the actual input system since it executes before
        // the InputTestFixture.Setup() method. Hence, after Setup() has executed the device is not part of
        // the test input system instance.
        incorrectlyUsedDevice = InputSystem.AddDevice<Keyboard>();
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);

        incorrectlyUsedGamepad = InputSystem.AddDevice<Gamepad>();
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedGamepad), Is.True);

        incorrectlyUsedTouchscreen = InputSystem.AddDevice<Touchscreen>();
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedTouchscreen), Is.True);
    }

    [OneTimeTearDown]
    public void UnityTearDown()
    {
        // Once InputTestFixture.TearDown() has executed, the state stack will have been popped and the correctlyUsedKeyboard
        // we added before entering the test fixture should have been restored.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedGamepad), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedTouchscreen), Is.True);
    }

    [SetUp]
    public override void Setup()
    {
        // At this point we are still using the actual system so our device created from UnitySetup() should still
        // exist with the context.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedGamepad), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedTouchscreen), Is.True);

        // This is expected usage pattern, first calling base.Setup() when overriding Setup like this, then
        // creating a fake device via the test fixture instance that only lives with the test context.
        base.Setup();

        // Since we have now entered a temporary test state our device created in UnitySetup() will no longer exist
        // with this context.
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.False);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedGamepad), Is.False);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedTouchscreen), Is.False);
    }

    [TearDown]
    public override void TearDown()
    {
        // Restore state
        base.TearDown();

        // Our test device should no longer exist with the system since we are back to real instance
        Assert.That(InputSystem.devices.Contains(correctlyUsedKeyboard), Is.False);

        Assert.That(InputSystem.devices.Contains(incorrectlyUsedDevice), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedGamepad), Is.True);
        Assert.That(InputSystem.devices.Contains(incorrectlyUsedTouchscreen), Is.True);
    }

    #region Editor playmode tests

    [Test]
    public void Press_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Press(correctlyUsedKeyboard.spaceKey);
        Assert.That(correctlyUsedKeyboard.spaceKey.isPressed, Is.True);
    }

    [Test]
    public void Press_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => Press(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void Release_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Press(correctlyUsedKeyboard.spaceKey);
        Release(correctlyUsedKeyboard.spaceKey);
        Assert.That(correctlyUsedKeyboard.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void Release_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => Release(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void PressAndRelease_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        PressAndRelease(correctlyUsedKeyboard.spaceKey);
        Assert.That(correctlyUsedKeyboard.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void PressAndRelease_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => PressAndRelease(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void Click_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Click(correctlyUsedKeyboard.spaceKey);
        Assert.That(correctlyUsedKeyboard.spaceKey.isPressed, Is.False);
    }

    [Test]
    public void Click_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => Click(incorrectlyUsedDevice.spaceKey));
    }

    [Test]
    public void Move_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Move(gamepad.leftStick, new Vector2(1.0f, 0.0f));
        Assert.That(gamepad.leftStick.value, Is.EqualTo(new Vector2(1.0f, 0.0f)));
    }

    [Test]
    public void Move_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => Move(incorrectlyUsedGamepad.leftStick, new Vector2(1.0f, 0.0f)));
    }

    [Test]
    public void Touch_ShouldMutateDeviceState_WithinPlayModeTestFixtureContext()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();
        BeginTouch(1, Vector2.zero, screen: touchscreen);
        Assert.That(touchscreen.touches.Count, Is.EqualTo(10));
        Assert.That(touchscreen.touches[0].touchId.value, Is.EqualTo(1));
        Assert.That(touchscreen.touches[0].position.value, Is.EqualTo(Vector2.zero));
        Assert.That(touchscreen.touches[0].phase.value, Is.EqualTo(TouchPhase.Began));

        MoveTouch(1, Vector2.one, screen: touchscreen);
        Assert.That(touchscreen.touches.Count, Is.EqualTo(10));
        Assert.That(touchscreen.touches[0].touchId.value, Is.EqualTo(1));
        Assert.That(touchscreen.touches[0].position.value, Is.EqualTo(Vector2.one));
        Assert.That(touchscreen.touches[0].phase.value, Is.EqualTo(TouchPhase.Moved));

        EndTouch(1, Vector2.zero, screen: touchscreen);
        Assert.That(touchscreen.touches.Count, Is.EqualTo(10));
        Assert.That(touchscreen.touches[0].touchId.value, Is.EqualTo(1));
        Assert.That(touchscreen.touches[0].position.value, Is.EqualTo(Vector2.zero));
        Assert.That(touchscreen.touches[0].phase.value, Is.EqualTo(TouchPhase.Ended));
    }

    [Test]
    public void Touch_ShouldThrow_WithinPlayModeTestFixtureContextIfInvalidDevice()
    {
        Assert.Throws<ArgumentException>(() => BeginTouch(1, Vector2.zero, screen: incorrectlyUsedTouchscreen));
        Assert.Throws<ArgumentException>(() => MoveTouch(1, Vector2.zero, screen: incorrectlyUsedTouchscreen));
        Assert.Throws<ArgumentException>(() => EndTouch(1, Vector2.zero, screen: incorrectlyUsedTouchscreen));
    }

    #endregion // Playmode tests

    #region // Edit-mode tests

    [UnityTest]
    public IEnumerator Press_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Assert.Throws<NotSupportedException>(() => Press(correctlyUsedKeyboard.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator Release_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Assert.Throws<NotSupportedException>(() => Release(correctlyUsedKeyboard.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator PressAndRelease_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Assert.Throws<NotSupportedException>(() => PressAndRelease(correctlyUsedKeyboard.spaceKey));
        yield break;
    }

    [UnityTest]
    public IEnumerator Click_ShouldThrow_WithinEditModeTestFixtureContext()
    {
        correctlyUsedKeyboard = InputSystem.AddDevice<Keyboard>();
        Assert.Throws<NotSupportedException>(() => Click(correctlyUsedKeyboard.spaceKey));
        yield break;
    }

    #endregion // Edit-mode tests
}
