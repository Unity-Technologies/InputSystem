using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevel;
using UnityEngine.InputSystem.LowLevel;
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
}