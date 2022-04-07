#if true // UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Apple.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Apple.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AppleGameControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('A', 'G', 'C', 'S');
        public FourCC format => kFormat;

        public enum Button
        {
            DpadUp = 0,
            DpadDown = 1,
            DpadLeft = 2,
            DpadRight = 3,
            LeftThumbstickButton = 4,
            RightThumbstickButton = 5,
            LeftShoulder = 6,
            RightShoulder = 7,
            LeftTrigger = 8,
            RightTrigger = 9,
            ButtonX = 10,
            ButtonY = 11,
            ButtonA = 12,
            ButtonB = 13,
            ButtonMenu = 14,
            ButtonOptions = 15,
            LeftThumbstickUp = 16,
            LeftThumbstickDown = 17,
            LeftThumbstickLeft = 18,
            LeftThumbstickRight = 19,
            RightThumbstickUp = 20,
            RightThumbstickDown = 21,
            RightThumbstickLeft = 22,
            RightThumbstickRight = 23,
            TouchpadButton = 24,
            ButtonHome = 25,
            ButtonShare = 26,
            PaddleButton1 = 27,
            PaddleButton2 = 28,
            PaddleButton3 = 29,
            PaddleButton4 = 30,
            MaxButtons = 40 // extra space for futureproofing
        }

        public enum Axis
        {
            LeftThumbstickX = 0,
            LeftThumbstickY = 1,
            RightThumbstickX = 2,
            RightThumbstickY = 3,
            DpadX = 4,
            DpadY = 5,
            TouchpadPrimaryX = 6,
            TouchpadPrimaryY = 7,
            TouchpadSecondaryX = 8,
            TouchpadSecondaryY = 9,
            MotionAttitudeX = 10,
            MotionAttitudeY = 11,
            MotionAttitudeZ = 12,
            MotionAttitudeW = 13,
            MotionRotationRateX = 14,
            MotionRotationRateY = 15,
            MotionRotationRateZ = 16,
            MotionGravityX = 17,
            MotionGravityY = 18,
            MotionGravityZ = 19,
            MotionUserAccelerationX = 20,
            MotionUserAccelerationY = 21,
            MotionUserAccelerationZ = 22,
            MotionAccelerationX = 23,
            MotionAccelerationY = 24,
            MotionAccelerationZ = 25,
            BatteryLevel = 26,
            DeviceLightR = 27,
            DeviceLightG = 28,
            DeviceLightB = 29,
            MaxAxes = 36 // extra space for futureproofing
        }

        public enum IntAxis
        {
            PlayerIndex = 0,
            BatteryState = 1,
            MaxIntAxes = 8 // extra space for futureproofing
        }

        public enum BatteryState
        {
            kBatteryStateUnknown = -1,
            kBatteryStateDischarging = 0,
            kBatteryStateCharging = 1,
            kBatteryStateFull = 2
        }

        public const int MaxButtons = (int) Button.MaxButtons;
        public const int MaxAxes = (int) Axis.MaxAxes;
        public const int MaxIntAxes = (int) IntAxis.MaxIntAxes;

        [InputControl(name = "dpad")]
        [InputControl(name = "dpad/up", bit = (uint) Button.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint) Button.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint) Button.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint) Button.DpadLeft)]
        [InputControl(name = "buttonSouth", bit = (uint) Button.ButtonA)]
        [InputControl(name = "buttonWest", bit = (uint) Button.ButtonX)]
        [InputControl(name = "buttonNorth", bit = (uint) Button.ButtonY)]
        [InputControl(name = "buttonEast", bit = (uint) Button.ButtonB)]
        [InputControl(name = "leftStickPress", bit = (uint) Button.LeftThumbstickButton)]
        [InputControl(name = "rightStickPress", bit = (uint) Button.RightThumbstickButton)]
        [InputControl(name = "leftShoulder", bit = (uint) Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint) Button.RightShoulder)]
        [InputControl(name = "start", bit = (uint) Button.ButtonMenu)]
        [InputControl(name = "select", bit = (uint) Button.ButtonOptions)]

        [InputControl(name = "touchpadButton", layout = "Button", bit = (uint)Button.TouchpadButton)]
        [InputControl(name = "home", layout = "Button", bit = (uint)Button.ButtonHome)]
        [InputControl(name = "share", layout = "Button", bit = (uint)Button.ButtonShare)]
        [InputControl(name = "paddle1", layout = "Button", bit = (uint)Button.PaddleButton1)]
        [InputControl(name = "paddle2", layout = "Button", bit = (uint)Button.PaddleButton2)]
        [InputControl(name = "paddle3", layout = "Button", bit = (uint)Button.PaddleButton3)]
        [InputControl(name = "paddle4", layout = "Button", bit = (uint)Button.PaddleButton4)]
        public ulong buttons;

        private const uint kButtonOffset = sizeof(ulong);
        [InputControl(name = "leftTrigger", offset = kButtonOffset + sizeof(float) * (uint) Button.LeftTrigger)]
        [InputControl(name = "rightTrigger", offset = kButtonOffset + sizeof(float) * (uint) Button.RightTrigger)]
        public fixed float buttonValue[MaxButtons];

        private const uint kAxisOffset = sizeof(ulong) + sizeof(float) * MaxButtons;
        [InputControl(name = "leftStick", offset = kAxisOffset + sizeof(float) * (uint) Axis.LeftThumbstickX)]
        [InputControl(name = "rightStick", offset = kAxisOffset + sizeof(float) * (uint) Axis.RightThumbstickX)]
        public fixed float axisValue[MaxAxes];

        private const uint kIntAxisOffset = sizeof(ulong) + sizeof(float) * MaxButtons + sizeof(float) * MaxAxes;
        public fixed int intAxisValue[MaxIntAxes];


        // public iOSGameControllerState WithButton(iOSButton button, bool value = true, float rawValue = 1.0f)
        // {
        //     buttonValues[(int)button] = rawValue;
        //
        //     Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
        //     var bit = 1U << (int)button;
        //     if (value)
        //         buttons |= bit;
        //     else
        //         buttons &= ~bit;
        //
        //     return this;
        // }
        //
        // public iOSGameControllerState WithAxis(iOSAxis axis, float value)
        // {
        //     axisValues[(int)axis] = value;
        //     return this;
        // }
    }
}

namespace UnityEngine.InputSystem.Apple
{
    [InputControlLayout(stateType = typeof(AppleGameControllerState), displayName = "Apple Gamepad")]
    public class AppleGameController : Gamepad
    {
    }
}
#endif