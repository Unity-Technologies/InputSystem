#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA || PACKAGE_DOCS_GENERATION
using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch.LowLevel;
using UnityEngine.InputSystem.Utilities;

////REVIEW: The Switch controller can be used to point at things; can we somehow help leverage that?

namespace UnityEngine.InputSystem.Switch.LowLevel
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA
    /// <summary>
    /// Structure of HID input reports for Switch Pro controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    internal struct SwitchProControllerHIDInputState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        [InputControl(name = "dpad", format = "BIT", layout = "Dpad",  bit = 24, sizeInBits = 4, defaultState = 8)]
        [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 24, sizeInBits = 4)]
        [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 24, sizeInBits = 4)]
        [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 24, sizeInBits = 4)]
        [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 24, sizeInBits = 4)]
        [InputControl(name = "buttonNorth", displayName = "X", shortDisplayName = "X", bit = (uint)Button.North)]
        [InputControl(name = "buttonSouth", displayName = "B", shortDisplayName = "B", bit = (uint)Button.South, usage = "Back")]
        [InputControl(name = "buttonWest", displayName = "Y", shortDisplayName = "Y", bit = (uint)Button.West, usage = "SecondaryAction")]
        [InputControl(name = "buttonEast", displayName = "A", shortDisplayName = "A", bit = (uint)Button.East, usage = "PrimaryAction")]
        [InputControl(name = "leftStickPress", displayName = "Left Stick", bit = (uint)Button.StickL)]
        [InputControl(name = "rightStickPress", displayName = "Right Stick", bit = (uint)Button.StickR)]
        [InputControl(name = "leftShoulder", displayName = "L", shortDisplayName = "L", bit = (uint)Button.L)]
        [InputControl(name = "rightShoulder", displayName = "R", shortDisplayName = "R", bit = (uint)Button.R)]
        [InputControl(name = "leftTrigger", displayName = "ZL", shortDisplayName = "ZL", format = "BIT", bit = (uint)Button.ZL)]
        [InputControl(name = "rightTrigger", displayName = "ZR", shortDisplayName = "ZR", format = "BIT", bit = (uint)Button.ZR)]
        [InputControl(name = "start", displayName = "Plus", bit = (uint)Button.Plus, usage = "Menu")]
        [InputControl(name = "select", displayName = "Minus", bit = (uint)Button.Minus)]
        [FieldOffset(0)]
        public uint buttons;

        [InputControl(name = "leftStick", format = "VC2S", layout = "Stick")]
        [InputControl(name = "leftStick/x", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85")]
        [InputControl(name = "leftStick/y", offset = 2, format = "USHT", parameters = "invert,normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "leftStick/up", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85,invert=false")]
        [FieldOffset(4)] public ushort leftStickX;
        [FieldOffset(6)] public ushort leftStickY;

        [InputControl(name = "rightStick", format = "VC2S", layout = "Stick")]
        [InputControl(name = "rightStick/x", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 2, format = "USHT", parameters = "invert,normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "rightStick/up", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85,invert=false")]
        [FieldOffset(8)] public ushort rightStickX;
        [FieldOffset(10)] public ushort rightStickY;

        public float leftTrigger => ((buttons & (1U << (int)Button.ZL)) != 0) ? 1f : 0f;

        public float rightTrigger => ((buttons & (1U << (int)Button.ZR)) != 0) ? 1f : 0f;

        public enum Button
        {
            North = 11,
            South = 8,
            West = 10,
            East = 9,

            StickL = 18,
            StickR = 19,
            L = 12,
            R = 13,

            ZL = 14,
            ZR = 15,
            Plus = 17,
            Minus = 16,

            X = North,
            B = South,
            Y = West,
            A = East,
        }

        public SwitchProControllerHIDInputState WithButton(Button button, bool value = true)
        {
            Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
            var bit = 1U << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;
            // dpad default state
            buttons |= 8 << 24;
            leftStickX = 0x8000;
            leftStickY = 0x8000;
            rightStickX = 0x8000;
            rightStickY = 0x8000;
            return this;
        }
    }
#endif
}

namespace UnityEngine.InputSystem.Switch
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA || PACKAGE_DOCS_GENERATION
    /// <summary>
    /// A Nintendo Switch Pro controller connected to a desktop mac/windows PC using the HID interface.
    /// </summary>
    /// <remarks>
    /// This controller currently only works when connected via Bluetooth but not when connected over USB.
    /// </remarks>
    [InputControlLayout(stateType = typeof(SwitchProControllerHIDInputState), displayName = "Switch Pro Controller")]
    public class SwitchProControllerHID : Gamepad
    {
    }
#endif
}
#endif // UNITY_EDITOR || UNITY_SWITCH
