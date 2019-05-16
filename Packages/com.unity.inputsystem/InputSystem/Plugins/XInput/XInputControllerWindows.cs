#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Plugins.XInput.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Plugins.XInput.LowLevel
{
    // IMPORTANT: State layout is XINPUT_GAMEPAD
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct XInputControllerWindowsState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('X', 'I', 'N', 'P'); }
        }

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

        [FieldOffset(0)]
        public ushort buttons;

        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(2)] public byte leftTrigger;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(3)] public byte rightTrigger;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "leftStick/x", offset = 0, format = "SHRT", parameters = "clamp=false,invert=false,normalize=false")]
        [InputControl(name = "leftStick/left", offset = 0, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "leftStick/right", offset = 0, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "leftStick/y", offset = 2, format = "SHRT", parameters = "clamp=false,invert=false,normalize=false")]
        [InputControl(name = "leftStick/up", offset = 2, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "leftStick/down", offset = 2, format = "SHRT", parameters = "invert=false,normalize=false")]
        [FieldOffset(4)] public short leftStickX;
        [FieldOffset(6)] public short leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2S")]
        [InputControl(name = "rightStick/x", offset = 0, format = "SHRT", parameters = "clamp=false,invert=false,normalize=false")]
        [InputControl(name = "rightStick/left", offset = 0, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "rightStick/right", offset = 0, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "rightStick/y", offset = 2, format = "SHRT", parameters = "clamp=false,invert=false,normalize=false")]
        [InputControl(name = "rightStick/up", offset = 2, format = "SHRT", parameters = "invert=false,normalize=false")]
        [InputControl(name = "rightStick/down", offset = 2, format = "SHRT", parameters = "invert=false,normalize=false")]
        [FieldOffset(8)] public short rightStickX;
        [FieldOffset(10)] public short rightStickY;

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public XInputControllerWindowsState WithButton(Button button)
        {
            buttons |= (ushort)((uint)1 << (int)button);
            return this;
        }
    }
}

namespace UnityEngine.InputSystem.Plugins.XInput
{
    [InputControlLayout(stateType = typeof(XInputControllerWindowsState), hideInUI = true)]
    public class XInputControllerWindows : XInputController
    {
    }
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
