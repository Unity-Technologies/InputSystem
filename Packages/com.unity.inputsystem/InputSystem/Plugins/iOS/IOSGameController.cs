#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.iOS.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.iOS.LowLevel
{
    public enum IOSButton
    {
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,
        LeftTrigger,
        RightTrigger,
        X,
        Y,
        A,
        B
    };

    public enum IOSAxis
    {
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IOSGameControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'G', 'C', ' ');
        public const int kMaxButtons = 14;
        public const int kMaxAxis = 4;

        [InputControl(name = "dpad")]
        [InputControl(name = "dpad/up", bit = (uint)IOSButton.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint)IOSButton.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint)IOSButton.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint)IOSButton.DpadLeft)]
        [InputControl(name = "buttonSouth", bit = (uint)IOSButton.A)]
        [InputControl(name = "buttonWest", bit = (uint)IOSButton.X)]
        [InputControl(name = "buttonNorth", bit = (uint)IOSButton.Y)]
        [InputControl(name = "buttonEast", bit = (uint)IOSButton.B)]
        [InputControl(name = "leftStickPress", bit = (uint)IOSButton.LeftStick)]
        [InputControl(name = "rightStickPress", bit = (uint)IOSButton.RightStick)]
        [InputControl(name = "leftShoulder", bit = (uint)IOSButton.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)IOSButton.RightShoulder)]
        public uint buttons;

        [InputControl(name = "leftTrigger", offset = sizeof(uint) + sizeof(float) * (uint)IOSButton.LeftTrigger)]
        [InputControl(name = "rightTrigger", offset = sizeof(uint) + sizeof(float) * (uint)IOSButton.RightTrigger)]
        public fixed float buttonValues[kMaxButtons];

        private const uint kAxisOffset = sizeof(uint) + sizeof(float) * kMaxButtons;
        [InputControl(name = "leftStick", offset = (uint)IOSAxis.LeftStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick", offset = (uint)IOSAxis.RightStickX * sizeof(float) + kAxisOffset)]
        public fixed float axisValues[kMaxAxis];

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public IOSGameControllerState WithButton(IOSButton button, bool value = true, float rawValue = 1.0f)
        {
            fixed(float* buttonsPtr = buttonValues)
            {
                buttonsPtr[(int)button] = rawValue;
            }

            if (value)
                buttons |= (uint)1 << (int)button;
            else
                buttons &= ~(uint)1 << (int)button;

            return this;
        }

        public IOSGameControllerState WithAxis(IOSAxis axis, float value)
        {
            fixed(float* axisPtr = this.axisValues)
            {
                axisPtr[(int)axis] = value;
            }
            return this;
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    [InputTemplate(stateType = typeof(IOSGameControllerState))]
    public class IOSGameController : Gamepad
    {
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
