#if UNITY_WEBGL || UNITY_EDITOR
using System;
using System.ComponentModel;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.WebGL.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.WebGL.LowLevel
{
    public unsafe struct WebGLGamepadState : IInputStateTypeInfo
    {
        public const int kNumAxes = 4;
        public const int kNumButtons = 16;
        public const int kButtonOffset = kNumAxes * 4;

        // Stick default format is already two floats so all we need to do is move the sticks and
        // put inverts on Y.
        [InputControl(name = "leftStick", offset = 0)]
        [InputControl(name = "rightStick", offset = 8)]
        [InputControl(name = "leftStick/y", parameters = "invert")]
        [InputControl(name = "rightStick/y", parameters = "invert")]
        // All the buttons we need to bump from single bits to full floats and reset bit offsets.
        [InputControl(name = "buttonSouth", offset = kButtonOffset + 0 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "buttonEast", offset = kButtonOffset + 1 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "buttonWest", offset = kButtonOffset + 2 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "buttonNorth", offset = kButtonOffset + 3 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "leftShoulder", offset = kButtonOffset + 4 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "rightShoulder", offset = kButtonOffset + 5 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "leftTrigger", offset = kButtonOffset + 6 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "rightTrigger", offset = kButtonOffset + 7 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "select", offset = kButtonOffset + 8 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "start", offset = kButtonOffset + 9 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "leftStickPress", offset = kButtonOffset + 10 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "rightStickPress", offset = kButtonOffset + 11 * 4, bit = 0, format = "FLT")]
        [InputControl(name = "dpad", offset = kButtonOffset + 12 * 4, bit = 0, sizeInBits = 4 * 4 * 8)]
        [InputControl(name = "dpad/up", offset = 0, bit = 0, format = "FLT")]
        [InputControl(name = "dpad/down", offset = 4, bit = 0, format = "FLT")]
        [InputControl(name = "dpad/left", offset = 8, bit = 0, format = "FLT")]
        [InputControl(name = "dpad/right", offset = 12, bit = 0, format = "FLT")]
        public fixed float values[kNumButtons + kNumAxes];

        public float leftTrigger
        {
            get { return GetValue(kNumAxes + 6); }
            set { SetValue(kNumAxes + 6, value); }
        }

        public float rightTrigger
        {
            get { return GetValue(kNumAxes + 7); }
            set { SetValue(kNumAxes + 7, value); }
        }

        public Vector2 leftStick
        {
            get { return new Vector2(GetValue(0), GetValue(1)); }
            set
            {
                SetValue(0, value.x);
                SetValue(1, value.y);
            }
        }

        public Vector2 rightStick
        {
            get { return new Vector2(GetValue(2), GetValue(3)); }
            set
            {
                SetValue(2, value.x);
                SetValue(3, value.y);
            }
        }

        public FourCC GetFormat()
        {
            return new FourCC('H', 'T', 'M', 'L');
        }

        public WebGLGamepadState WithButton(GamepadButton button, float value = 1)
        {
            int index;
            switch (button)
            {
                case GamepadButton.South: index = 0; break;
                case GamepadButton.East: index = 1; break;
                case GamepadButton.West: index = 2; break;
                case GamepadButton.North: index = 3; break;
                case GamepadButton.LeftShoulder: index = 4; break;
                case GamepadButton.RightShoulder: index = 5; break;
                case GamepadButton.Select: index = 8; break;
                case GamepadButton.Start: index = 9; break;
                case GamepadButton.LeftStick: index = 10; break;
                case GamepadButton.RightStick: index = 11; break;
                case GamepadButton.DpadUp: index = 12; break;
                case GamepadButton.DpadDown: index = 13; break;
                case GamepadButton.DpadLeft: index = 14; break;
                case GamepadButton.DpadRight: index = 15; break;

                default:
                    throw new InvalidEnumArgumentException("button");
            }

            SetValue(kNumAxes + index, value);
            return this;
        }

        private float GetValue(int index)
        {
            fixed(float* valuePtr = values)
            return valuePtr[index];
        }

        private void SetValue(int index, float value)
        {
            fixed(float* valuePtr = values)
            valuePtr[index] = value;
        }
    }

    public struct WebGLDeviceCapabilities
    {
        public int numAxes;
        public int numButtons;
        public string mapping;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static WebGLDeviceCapabilities FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException("json");
            return JsonUtility.FromJson<WebGLDeviceCapabilities>(json);
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.WebGL
{
    /// <summary>
    /// Gamepad on WebGL that uses the "standard" mapping.
    /// </summary>
    /// <seealso href="https://w3c.github.io/gamepad/#remapping"/>
    [InputControlLayout(stateType = typeof(WebGLGamepadState))]
    public class WebGLGamepad : Gamepad
    {
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
