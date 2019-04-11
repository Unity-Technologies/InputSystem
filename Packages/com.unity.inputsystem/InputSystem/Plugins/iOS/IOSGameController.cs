#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.iOS.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.iOS.LowLevel
{
    public enum iOSButton
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
        B,
        Start,
        Select

        // Note: If you'll add an element here, be sure to update kMaxButtons const below
    };

    public enum iOSAxis
    {
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY

        // Note: If you'll add an element here, be sure to update kMaxAxis const below
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct iOSGameControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'G', 'C', ' ');
        public const int kMaxButtons = (int)iOSButton.Select + 1;
        public const int kMaxAxis = (int)iOSAxis.RightStickY + 1;

        [InputControl(name = "dpad")]
        [InputControl(name = "dpad/up", bit = (uint)iOSButton.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint)iOSButton.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint)iOSButton.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint)iOSButton.DpadLeft)]
        [InputControl(name = "buttonSouth", bit = (uint)iOSButton.A)]
        [InputControl(name = "buttonWest", bit = (uint)iOSButton.X)]
        [InputControl(name = "buttonNorth", bit = (uint)iOSButton.Y)]
        [InputControl(name = "buttonEast", bit = (uint)iOSButton.B)]
        [InputControl(name = "leftStickPress", bit = (uint)iOSButton.LeftStick)]
        [InputControl(name = "rightStickPress", bit = (uint)iOSButton.RightStick)]
        [InputControl(name = "leftShoulder", bit = (uint)iOSButton.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)iOSButton.RightShoulder)]
        [InputControl(name = "start", bit = (uint)iOSButton.Start)]
        [InputControl(name = "select", bit = (uint)iOSButton.Select)]
        public uint buttons;

        [InputControl(name = "leftTrigger", offset = sizeof(uint) + sizeof(float) * (uint)iOSButton.LeftTrigger)]
        [InputControl(name = "rightTrigger", offset = sizeof(uint) + sizeof(float) * (uint)iOSButton.RightTrigger)]
        public fixed float buttonValues[kMaxButtons];

        private const uint kAxisOffset = sizeof(uint) + sizeof(float) * kMaxButtons;
        [InputControl(name = "leftStick", offset = (uint)iOSAxis.LeftStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick", offset = (uint)iOSAxis.RightStickX * sizeof(float) + kAxisOffset)]
        public fixed float axisValues[kMaxAxis];

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public iOSGameControllerState WithButton(iOSButton button, bool value = true, float rawValue = 1.0f)
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

        public iOSGameControllerState WithAxis(iOSAxis axis, float value)
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
    [InputControlLayout(stateType = typeof(iOSGameControllerState), displayName = "iOS Gamepad")]
    public class iOSGameController : Gamepad
    {
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
