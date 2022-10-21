using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevel;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools.Utils;
using Input = UnityEngine.InputSystem.HighLevel.Input;

internal partial class CoreTests
{
    // TODO rename HighLevelAPI to something that makes sense
    // one perspective is to see this API as an evolution of input manager API, aka input manager 2.0
    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQueryControl()
    {
        // gamepad sticks can only be engaged in two directions
        // at a same time (either left+up or right+down or permutation of both)
        // hence we need to ignore some buttons
        var buttonsToIgnore = new HashSet<Inputs>
        {
            Inputs.Gamepad_LeftStickDown,
            Inputs.Gamepad_LeftStickRight,
            Inputs.Gamepad_RightStickUp,
            Inputs.Gamepad_RightStickLeft
        };

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var joystick = InputSystem.AddDevice<Joystick>();

        // check that all controls are not actuated
        foreach(var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            Assert.That(Input.IsControlPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.IsControlDown(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.IsControlUp(input), Is.False, $"Input '{input}' should be 'not up'");
        }
        
        // press all buttons
        var keyboardState = new KeyboardState();
        foreach (var value in typeof(Key).GetEnumValues())
            keyboardState.Press((Key)value);
        InputSystem.QueueStateEvent(keyboard, keyboardState);

        var mouseState = new MouseState()
            .WithButton(MouseButton.Left)
            .WithButton(MouseButton.Right)
            .WithButton(MouseButton.Middle)
            .WithButton(MouseButton.Forward)
            .WithButton(MouseButton.Back);
        InputSystem.QueueStateEvent(mouse, mouseState);

        var gamepadState = new GamepadState()
            .WithButton(GamepadButton.DpadUp)
            .WithButton(GamepadButton.DpadDown)
            .WithButton(GamepadButton.DpadLeft)
            .WithButton(GamepadButton.DpadRight)
            .WithButton(GamepadButton.North)
            .WithButton(GamepadButton.East)
            .WithButton(GamepadButton.South)
            .WithButton(GamepadButton.West)
            .WithButton(GamepadButton.LeftStick)
            .WithButton(GamepadButton.RightStick)
            .WithButton(GamepadButton.LeftShoulder)
            .WithButton(GamepadButton.RightShoulder)
            .WithButton(GamepadButton.Start)
            .WithButton(GamepadButton.Select);
        gamepadState.leftStick = new Vector2(-1, 1);
        gamepadState.rightStick = new Vector2(1, -1);
        gamepadState.leftTrigger = 1.0f;
        gamepadState.rightTrigger = 1.0f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);

        var joystickState = new JoystickState();
        joystickState.buttons |= (int)(1U << (int)JoystickState.Button.Trigger);
        InputSystem.QueueStateEvent(joystick, joystickState);

        // check that all buttons are pressed, and control down is true for the first frame
        InputSystem.Update();
        foreach(var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;
            
            Assert.That(Input.IsControlPressed(input), Is.True, $"Input '{input}' should be 'pressed'");
            Assert.That(Input.IsControlDown(input), Is.True, $"Input '{input}' should be 'down'");
            Assert.That(Input.IsControlUp(input), Is.False, $"Input '{input}' should be 'not up'");
        }
        
        // check that IsControlDown became false after one frame
        InputSystem.Update();
        foreach(var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;

            Assert.That(Input.IsControlPressed(input), Is.True, $"Input '{input}' should be 'pressed'");
            Assert.That(Input.IsControlDown(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.IsControlUp(input), Is.False, $"Input '{input}' should be 'not up'");
        }
        
        // release everything
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.QueueStateEvent(joystick, new JoystickState());

        // check that all controls are not pressed, and control up became true
        InputSystem.Update();
        foreach(var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;

            Assert.That(Input.IsControlPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.IsControlDown(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.IsControlUp(input), Is.True, $"Input '{input}' should be 'up'");
        }

        // check control up became false after one frame
        InputSystem.Update();
        foreach(var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            Assert.That(Input.IsControlPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.IsControlDown(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.IsControlUp(input), Is.False, $"Input '{input}' should be 'not up'");
        }
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQueryGetAxis()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        
        var keyboardState = new KeyboardState();
        keyboardState.Press(Key.W);
        keyboardState.Press(Key.A);
        InputSystem.QueueStateEvent(keyboard, keyboardState);

        var gamepadState = new GamepadState()
            .WithButton(GamepadButton.North)
            .WithButton(GamepadButton.West);
        gamepadState.leftStick = new Vector2(-1, -1);
        gamepadState.rightStick = new Vector2(1, 1);
        gamepadState.leftTrigger = 0.7f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();
        
        // normal buttons should return 0.0f or 1.0f
        Assert.That(Input.GetAxis(Inputs.Key_W), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_S), Is.EqualTo(0.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_South), Is.EqualTo(0.0f));
        
        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_D), Is.EqualTo(-1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_S, Inputs.Key_W), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_W), Is.EqualTo(0.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North, Inputs.Gamepad_East), Is.EqualTo(-1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_East, Inputs.Gamepad_West), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North, Inputs.Gamepad_West), Is.EqualTo(0.0f));

        // triggers should return [0.0f, 1.0f]
        Assert.That(Input.GetAxis(Inputs.Gamepad_LeftTrigger), Is.EqualTo(gamepadState.leftTrigger));
        Assert.That(Input.GetAxis(Inputs.Gamepad_RightTrigger), Is.EqualTo(0.0f));

        // check normalization
        Assert.That(Input.GetAxisRaw(Inputs.Key_A, Inputs.Key_D, Inputs.Key_W, Inputs.Key_S), Is.EqualTo(new Vector2(-1, 1)));
        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_D, Inputs.Key_W, Inputs.Key_S),
            Is.EqualTo(new Vector2(-0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(Input.GetAxisRaw(Inputs.Gamepad_West, Inputs.Gamepad_East, Inputs.Gamepad_North, Inputs.Gamepad_East), Is.EqualTo(new Vector2(-1, 1)));
        Assert.That(Input.GetAxis(Inputs.Gamepad_West, Inputs.Gamepad_East, Inputs.Gamepad_North, Inputs.Gamepad_East),
            Is.EqualTo(new Vector2(-0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));

        // sticks go via different path
        Assert.That(Input.GetAxis(GamepadAxis.LeftStick), Is.EqualTo(new Vector2(-0.71f, -0.71f)).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(Input.GetAxis(GamepadAxis.RightStick), Is.EqualTo(new Vector2(0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanSetGamepadTriggerPoint()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var gamepadState = new GamepadState();
        gamepadState.leftTrigger = 0.3f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();
        
        Input.SetGamepadTriggerPressPoint(0.5f);
        Assert.That(Input.IsControlPressed(Inputs.Gamepad_LeftTrigger), Is.False);

        Input.SetGamepadTriggerPressPoint(0.3f);
        Assert.That(Input.IsControlPressed(Inputs.Gamepad_LeftTrigger), Is.True);
    }
    
    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanSetGamepadDeadZone()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var gamepadState = new GamepadState();
        gamepadState.leftStick = new Vector2(-0.3f, 0.3f);
        gamepadState.rightStick = new Vector2(0.7f, -0.7f);
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();
        
        Input.SetGamepadStickDeadzone(0.0f);
        Assert.That(Input.GetAxis(GamepadAxis.LeftStick), Is.EqualTo(new Vector2(-0.26f, 0.26f)).Using(new Vector2EqualityComparer(0.01f)));

        Input.SetGamepadStickDeadzone(0.3f);
        Assert.That(Input.GetAxis(GamepadAxis.RightStick), Is.EqualTo(new Vector2(0.71f, -0.71f)).Using(new Vector2EqualityComparer(0.01f)));
    }
}