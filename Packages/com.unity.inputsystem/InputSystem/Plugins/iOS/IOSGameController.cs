#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.iOS.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.iOS.LowLevel
{
    internal enum iOSButton
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

    internal enum iOSAxis
    {
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY

        // Note: If you'll add an element here, be sure to update kMaxAxis const below
    };

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct iOSGameControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'G', 'C', ' ');
        public const int MaxButtons = (int)iOSButton.Select + 1;
        public const int MaxAxis = (int)iOSAxis.RightStickY + 1;

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
        public fixed float buttonValues[MaxButtons];

        private const uint kAxisOffset = sizeof(uint) + sizeof(float) * MaxButtons;
        [InputControl(name = "leftStick", offset = (uint)iOSAxis.LeftStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick", offset = (uint)iOSAxis.RightStickX * sizeof(float) + kAxisOffset)]
        public fixed float axisValues[MaxAxis];

        public FourCC format => kFormat;

        public iOSGameControllerState WithButton(iOSButton button, bool value = true, float rawValue = 1.0f)
        {
            buttonValues[(int)button] = rawValue;

            Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
            var bit = 1U << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;

            return this;
        }

        public iOSGameControllerState WithAxis(iOSAxis axis, float value)
        {
            axisValues[(int)axis] = value;
            return this;
        }
    }
}

namespace UnityEngine.InputSystem.iOS
{
    /// <summary>
    /// A generic Gamepad connected to an iOS device.
    /// </summary>
    /// <remarks>
    /// Any MFi-certified Gamepad which is not an <see cref="XboxOneGampadiOS"/> or <see cref="DualShock4GampadiOS"/> will
    /// be represented as an iOSGameController.
    /// </remarks>
    [InputControlLayout(stateType = typeof(iOSGameControllerState), displayName = "iOS Gamepad")]
    public class iOSGameController : Gamepad
    {
    }

    /// <summary>
    /// An Xbox One Bluetooth controller connected to an iOS device.
    /// </summary>
    [InputControlLayout(stateType = typeof(iOSGameControllerState), displayName = "iOS Xbox One Gamepad")]
    public class XboxOneGampadiOS : XInput.XInputController
    {
    }

    /// <summary>
    /// A PlayStation DualShock 4 controller connected to an iOS device.
    /// </summary>
    [InputControlLayout(stateType = typeof(iOSGameControllerState), displayName = "iOS DualShock 4 Gamepad")]
    public class DualShock4GampadiOS : DualShockGamepad
    {
    }

    /// <summary>
    /// A PlayStation DualSense controller connected to an iOS device.
    /// </summary>
    [InputControlLayout(stateType = typeof(iOSGameControllerState), displayName = "iOS DualSense Gamepad")]
    public class DualSenseGampadiOS : DualShockGamepad
    {
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS
