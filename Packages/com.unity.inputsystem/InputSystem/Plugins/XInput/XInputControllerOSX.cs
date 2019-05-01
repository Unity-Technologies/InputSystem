#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Plugins.XInput.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.XInput.LowLevel
{
    // Xbox one controller on OSX. State layout can be found here:
    // https://github.com/360Controller/360Controller/blob/master/360Controller/ControlStruct.h
    // struct InputReport
    // {
    //     byte command;
    //     byte size;
    //     short buttons;
    //     byte triggerLeft;
    //     byte triggerRight;
    //     short leftX;
    //     short leftY;
    //     short rightX;
    //     short rightY;
    // }
    // Report size is 14 bytes. First two bytes are header information for the report.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct XInputControllerOSXState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('H', 'I', 'D');

        public enum Button
        {
            DPadUp = 0,
            DPadDown = 1,
            DPadLeft = 2,
            DPadRight = 3,
            Start = 4,
            Select = 5,
            LeftThumbstickPress = 6,
            RightThumbstickPress = 7,
            LeftShoulder = 8,
            RightShoulder = 9,
            A = 12,
            B = 13,
            X = 14,
            Y = 15,
        }

        [InputControl(name = "dpad", layout = "Dpad", sizeInBits = 4, bit = 0)]
        [InputControl(name = "dpad/up", bit = (uint)Button.DPadUp)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DPadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DPadLeft)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DPadRight)]
        [InputControl(name = "start", bit = (uint)Button.Start, displayName = "Start")]
        [InputControl(name = "select", bit = (uint)Button.Select, displayName = "Select")]
        [InputControl(name = "leftStickPress", bit = (uint)Button.LeftThumbstickPress)]
        [InputControl(name = "rightStickPress", bit = (uint)Button.RightThumbstickPress)]
        [InputControl(name = "leftShoulder", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)Button.RightShoulder)]
        [InputControl(name = "buttonSouth", bit = (uint)Button.A, displayName = "A")]
        [InputControl(name = "buttonEast", bit = (uint)Button.B, displayName = "B")]
        [InputControl(name = "buttonWest", bit = (uint)Button.X, displayName = "X")]
        [InputControl(name = "buttonNorth", bit = (uint)Button.Y, displayName = "Y")]

        [FieldOffset(2)]
        public ushort buttons;

        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(4)] public byte leftTrigger;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(5)] public byte rightTrigger;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "leftStick/x", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "leftStick/left", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "leftStick/right", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "leftStick/y", offset = 2, format = "SHRT", parameters = "invert")]
        [InputControl(name = "leftStick/up", offset = 2, format = "SHRT", parameters = "clamp,clampMin=-1,clampMax=0,invert=true")]
        [InputControl(name = "leftStick/down", offset = 2, format = "SHRT", parameters = "clamp,clampMin=0,clampMax=1,invert=false")]
        [FieldOffset(6)] public short leftStickX;
        [FieldOffset(8)] public short leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "rightStick/x", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "rightStick/left", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "rightStick/right", offset = 0, format = "SHRT", parameters = "")]
        [InputControl(name = "rightStick/y", offset = 2, format = "SHRT", parameters = "invert")]
        [InputControl(name = "rightStick/up", offset = 2, format = "SHRT", parameters = "clamp,clampMin=-1,clampMax=0,invert=true")]
        [InputControl(name = "rightStick/down", offset = 2, format = "SHRT", parameters = "clamp,clampMin=0,clampMax=1,invert=false")]
        [FieldOffset(10)] public short rightStickX;
        [FieldOffset(12)] public short rightStickY;

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public XInputControllerOSXState WithButton(Button button)
        {
            buttons |= (ushort)((uint)1 << (int)button);
            return this;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct XInputControllerWirelessOSXState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('H', 'I', 'D'); }
        }

        public enum Button
        {
            Start = 11,
            Select = 16,
            LeftThumbstickPress = 13,
            RightThumbstickPress = 14,
            LeftShoulder = 6,
            RightShoulder = 7,
            A = 0,
            B = 1,
            X = 3,
            Y = 4,
        }

        [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4, defaultState = 8)]
        [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=8,maxValue=2,nullValue=0,wrapAtValue=9", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=2,maxValue=4", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=4,maxValue=6", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=6, maxValue=8", bit = 0, sizeInBits = 4)]
        [FieldOffset(13)]
        public byte dpad;

        [InputControl(name = "start", bit = (uint)Button.Start, displayName = "Start")]
        [InputControl(name = "select", bit = (uint)Button.Select, displayName = "Select")]
        [InputControl(name = "leftStickPress", bit = (uint)Button.LeftThumbstickPress)]
        [InputControl(name = "rightStickPress", bit = (uint)Button.RightThumbstickPress)]
        [InputControl(name = "leftShoulder", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)Button.RightShoulder)]
        [InputControl(name = "buttonSouth", bit = (uint)Button.A, displayName = "A")]
        [InputControl(name = "buttonEast", bit = (uint)Button.B, displayName = "B")]
        [InputControl(name = "buttonWest", bit = (uint)Button.X, displayName = "X")]
        [InputControl(name = "buttonNorth", bit = (uint)Button.Y, displayName = "Y")]

        [FieldOffset(14)]
        public uint buttons;

        [InputControl(name = "leftTrigger", format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=0.01560998")]
        [FieldOffset(9)] public ushort leftTrigger;
        [InputControl(name = "rightTrigger", format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=0.01560998")]
        [FieldOffset(11)] public ushort rightTrigger;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "leftStick/x", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
        [InputControl(name = "leftStick/y", offset = 2, format = "USHT", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/up", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(1)] public ushort leftStickX;
        [FieldOffset(3)] public ushort leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "rightStick/x", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 2, format = "USHT", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/up", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 2, format = "USHT", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(5)] public ushort rightStickX;
        [FieldOffset(7)] public ushort rightStickY;

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public XInputControllerWirelessOSXState WithButton(Button button)
        {
            buttons |= (uint)1 << (int)button;
            return this;
        }

        public XInputControllerWirelessOSXState WithDpad(byte value)
        {
            dpad = value;
            return this;
        }

        public static XInputControllerWirelessOSXState defaultState => new XInputControllerWirelessOSXState
        {
            rightStickX = 32767,
            rightStickY = 32767,
            leftStickX = 32767,
            leftStickY = 32767
        };
    }
}
namespace UnityEngine.Experimental.Input.Plugins.XInput
{
    [InputControlLayout(stateType = typeof(XInputControllerOSXState), hideInUI = true)]
    public class XInputControllerOSX : XInputController
    {
    }

    [InputControlLayout(stateType = typeof(XInputControllerWirelessOSXState), hideInUI = true)]
    public class XInputControllerWirelessOSX : XInputController
    {
    }
}
#endif // UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
