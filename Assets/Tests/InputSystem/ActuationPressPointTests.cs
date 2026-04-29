using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Covers IActuationPressPoint, defaultButtonPressPoint, InputAction.IsPressed, and InputControl.IsPressed().
[TestFixture]
[Category("ActuationPressPoint")]
internal class ActuationPressPointTests : CoreTestsFixture
{
    #region InputAction.IsPressed (Vector2 + Press interaction / control pressPoint)

    [Test]
    [Category("Actions")]
    public void Actions_Vector2IsPressed_UsesPressInteractionPressPoint()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();

        var action = new InputAction(
            type: InputActionType.Value,
            expectedControlType: "Vector2",
            binding: "<Gamepad>/leftStick",
            interactions: "press(pressPoint=0.6)");
        action.Enable();

        Set(gamepad.leftStick, new Vector2(0.55f, 0f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0f, 0.65f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector2IsPressed_UsesVector2ControlPressPoint()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();

        var action = new InputAction(
            type: InputActionType.Value,
            expectedControlType: "Vector2",
            binding: "<Gamepad>/leftStick");
        gamepad.leftStick.pressPoint = 0.7f;
        action.Enable();

        Set(gamepad.leftStick, new Vector2(0.65f, 0f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0.75f, 0f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.True);

        gamepad.leftStick.pressPoint = -1f;
    }

    [Test]
    [Category("Actions")]
    public void Actions_ButtonIsPressed_UsesPressInteractionWhenControlPressPointUnset()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(
            type: InputActionType.Button,
            binding: "<Gamepad>/leftTrigger",
            interactions: "press(pressPoint=0.6)");
        action.Enable();

        Set(gamepad.leftTrigger, 0.55f);
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.False);

        Set(gamepad.leftTrigger, 0.65f);
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector2IsPressed_ControlPressPointOverridesPressInteraction()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();

        var action = new InputAction(
            type: InputActionType.Value,
            expectedControlType: "Vector2",
            binding: "<Gamepad>/leftStick",
            interactions: "press(pressPoint=0.6)");
        gamepad.leftStick.pressPoint = 0.85f;
        action.Enable();

        // Above interaction threshold (0.6) but below control threshold (0.85).
        Set(gamepad.leftStick, new Vector2(0.7f, 0f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0.9f, 0f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.True);

        gamepad.leftStick.pressPoint = -1f;
    }

    [Test]
    [Category("Actions")]
    public void Actions_CompositeVector2IsPressed_UsesPressInteractionOnCompositeBinding()
    {
        // Interaction lives on the composite binding while state changes arrive on part bindings.
        // Analog 2DVector + stick half-axes yields composite magnitudes between 0 and 1 so we can
        // distinguish defaultButtonPressPoint (0.5) from press(pressPoint=0.6).
        InputSystem.settings.defaultButtonPressPoint = 0.5f;
        InputSystem.settings.buttonReleaseThreshold = 0.8f;
        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();

        var action = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");
        action.AddCompositeBinding("2DVector(mode=2)", interactions: "press(pressPoint=0.6)")
            .With("Up", "<Gamepad>/leftStick/up")
            .With("Down", "<Gamepad>/leftStick/down")
            .With("Left", "<Gamepad>/leftStick/left")
            .With("Right", "<Gamepad>/leftStick/right");
        action.Enable();

        Set(gamepad.leftStick, new Vector2(0f, 0.55f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0f, 0.65f));
        InputSystem.Update();
        Assert.That(action.IsPressed(), Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_Vector2Composite_RespectsButtonPressurePoint()
    {
        // The stick has deadzones on the up/down/left/right buttons to get rid of stick
        // noise. For this test, simplify things by getting rid of deadzones.
        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Set up classic WASD control.
        var action = new InputAction();
        action.AddCompositeBinding("Dpad")
            .With("Up", "<Gamepad>/leftstick/up")
            .With("Down", "<Gamepad>/leftstick/down")
            .With("Left", "<Gamepad>/leftstick/left")
            .With("Right", "<Gamepad>/leftstick/right");
        action.Enable();

        Vector2? value = null;
        action.performed += ctx => { value = ctx.ReadValue<Vector2>(); };
        action.canceled += ctx => { value = ctx.ReadValue<Vector2>(); };

        var pressPoint = gamepad.leftStick.up.pressPointOrDefault;

        // Up.
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up (slightly above press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up (slightly below press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.zero));

        // Up left.
        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Up left (up slightly above press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState { leftStick = Vector2.up * pressPoint * 1.01f + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Up left (up slightly below press point)
        value = null;
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState { leftStick = Vector2.up * pressPoint * 0.99f + Vector2.left });
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.left));
    }

    #endregion

    #region Control extension IsPressed + settings (IActuationPressPoint / default threshold)

    [Test]
    [Category("Controls")]
    public void Controls_CanDetermineIfControlIsPressed()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.one);
        Set(gamepad.leftTrigger, 0.6f);
        Press(gamepad.buttonSouth);

        //// https://jira.unity3d.com/browse/ISX-926
        ////REVIEW: IsPressed() should probably be renamed. As is apparent from the calls here, it's not always
        ////        readily apparent that the way it is defined ("actuation level at least at button press threshold")
        ////        does not always connect to what it intuitively means for the specific control.

        Assert.That(gamepad.leftTrigger.IsPressed(), Is.True);
        Assert.That(gamepad.rightTrigger.IsPressed(), Is.False);
        Assert.That(gamepad.buttonSouth.IsPressed(), Is.True);
        Assert.That(gamepad.buttonNorth.IsPressed(), Is.False);
        Assert.That(gamepad.leftStick.IsPressed(),
            Is.True); // Note how this diverges from the actual meaning of "is the left stick pressed?"
        Assert.That(gamepad.rightStick.IsPressed(), Is.False);

        // https://fogbugz.unity3d.com/f/cases/1374024/
        // Calling it on the entire device should be false.
        Assert.That(gamepad.IsPressed(), Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCustomizeDefaultButtonPressPoint()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.settings.defaultButtonPressPoint = 0.4f;

        Set(gamepad.leftTrigger, 0.39f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        Set(gamepad.leftTrigger, 0.4f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        InputSystem.settings.defaultButtonPressPoint = 0;

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        // Setting the trigger to 0 requires the system to be "smart" enough to
        // figure out that 0 as a default button press point doesn't make sense
        // and that instead the press point should clamp off at some low, non-zero value.
        // https://fogbugz.unity3d.com/f/cases/1349002/
        Set(gamepad.leftTrigger, 0f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        Set(gamepad.leftTrigger, 0.001f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        InputSystem.settings.defaultButtonPressPoint = -1;
        Set(gamepad.leftTrigger, 0f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCustomizePressPointOfGamepadTriggers()
    {
        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""pressPoint=0.2""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var gamepad = InputDevice.Build<Gamepad>("CustomGamepad");

        Assert.That(gamepad.rightTrigger.pressPoint, Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    [Category("Controls")]
    public void Controls_Vector2ExtensionIsPressed_UsesPressPointOrDefault()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        gamepad.leftStick.pressPoint = 0.75f;
        Set(gamepad.leftStick, new Vector2(0.6f, 0f));
        Assert.That(gamepad.leftStick.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0.8f, 0f));
        Assert.That(gamepad.leftStick.IsPressed(), Is.True);

        gamepad.leftStick.pressPoint = -1f;
        Set(gamepad.leftStick, new Vector2(0.4f, 0f));
        Assert.That(gamepad.leftStick.IsPressed(), Is.False);

        Set(gamepad.leftStick, new Vector2(0.55f, 0f));
        Assert.That(gamepad.leftStick.IsPressed(), Is.True);
    }

    #endregion
}