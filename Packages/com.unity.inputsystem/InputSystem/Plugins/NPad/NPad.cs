#if UNITY_EDITOR || UNITY_SWITCH
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Plugins.Switch
{
    /// <summary>
    /// Structure of HID input reports for Switch NPad controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct NPadInputState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC('N', 'P', 'A', 'D');
        }

        /// <summary>
        /// Button bit mask.
        /// </summary>
        /// <seealso cref="Button"/>
        [InputControl(name = "dpad", template = "Dpad", usage = "Hatswitch")]
        [InputControl(name = "buttonNorth", displayName = "X", bit = (uint)Button.North)]
        [InputControl(name = "buttonSouth", displayName = "B", bit = (uint)Button.South, usage = "Back")]
        [InputControl(name = "buttonWest", displayName = "Y", bit = (uint)Button.West, usage = "SecondaryAction")]
        [InputControl(name = "buttonEast", displayName = "A", bit = (uint)Button.East, usage = "PrimaryAction")]
        [InputControl(name = "leftStickPress", displayName = "StickL", bit = (uint)Button.StickL)]
        [InputControl(name = "rightStickPress", displayName = "StickR", bit = (uint)Button.StickR)]
        [InputControl(name = "leftShoulder", displayName = "L", bit = (uint)Button.L)]
        [InputControl(name = "rightShoulder", displayName = "R", bit = (uint)Button.R)]
        [InputControl(name = "leftTriggerButton", template = "Button", displayName = "ZL", bit = (uint)Button.ZL)]
        [InputControl(name = "rightTriggerButton", template = "Button", displayName = "ZR", bit = (uint)Button.ZR)]
        [InputControl(name = "start", displayName = "Plus", bit = (uint)Button.Plus, usage = "Menu")]
        [InputControl(name = "select", displayName = "Minus", bit = (uint)Button.Minus)]
        [InputControl(name = "LSL", template = "Button", bit = (uint)Button.LSL)]
        [InputControl(name = "LSR", template = "Button", bit = (uint)Button.LSR)]
        [InputControl(name = "RSL", template = "Button", bit = (uint)Button.RSL)]
        [InputControl(name = "RSR", template = "Button", bit = (uint)Button.RSR)]
        [InputControl(name = "VK_LUp", template = "Button", bit = (uint)Button.VKey_LUp)]
        [InputControl(name = "VK_LDown", template = "Button", bit = (uint)Button.VKey_LDown)]
        [InputControl(name = "VK_LLeft", template = "Button", bit = (uint)Button.VKey_LLeft)]
        [InputControl(name = "VK_LRight", template = "Button", bit = (uint)Button.VKey_LRight)]
        [InputControl(name = "VK_RUp", template = "Button", bit = (uint)Button.VKey_RUp)]
        [InputControl(name = "VK_RDown", template = "Button", bit = (uint)Button.VKey_RDown)]
        [InputControl(name = "VK_RLeft", template = "Button", bit = (uint)Button.VKey_RLeft)]
        [InputControl(name = "VK_RRight", template = "Button", bit = (uint)Button.VKey_RRight)]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(name = "leftStick", template = "Stick", format = "VC2S")]
        [InputControl(variant = "Default", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(name = "rightStick", template = "Stick", format = "VC2S")]
        [InputControl(variant = "Default", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        public enum Button
        {
            // Dpad buttons. Important to be first in the bitfield as we'll
            // point the DpadControl to it.
            // IMPORTANT: Order has to match what is expected by DpadControl.
            Up,
            Down,
            Left,
            Right,

            // Face buttons. We go with a north/south/east/west naming as that
            // clearly disambiguates where we expect the respective button to be.
            North,
            South,
            West,
            East,

            StickL,
            StickR,
            L,
            R,

            ZL,
            ZR,
            Plus,
            Minus,

            LSL,
            LSR,
            RSL,
            RSR,

            VKey_LUp,
            VKey_LDown,
            VKey_LLeft,
            VKey_LRight,

            VKey_RUp,
            VKey_RDown,
            VKey_RLeft,
            VKey_RRight,

            X = North,
            B = South,
            Y = West,
            A = East,
        }
    }

    /// <summary>
    /// Switch output report sent as command to the backend.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct NPadOutputReport : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('N', 'P', 'D', 'O'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 16;

        [Flags]
        public enum Flags
        {
            SetPosition = (1 << 0),
        }

        [FieldOffset(0)] public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public uint flags;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)] public byte controllerId;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 5)] public byte npadId;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 6)] public byte position;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 7)] public byte pudding0;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)] public uint styleMask;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 12)] public int color;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public void SetPosition(NPad.Position pos)
        {
            flags |= (byte)Flags.SetPosition;
            position = (byte)pos;
        }

        public static NPadOutputReport Create()
        {
            return new NPadOutputReport
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct NPadShowControllerSupportUI : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('N', 'P', 'D', 'U'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static NPadShowControllerSupportUI Create()
        {
            return new NPadShowControllerSupportUI
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }

    /// <summary>
    /// An NPad controller for Switch, which can be a Joy-Con.
    /// </summary>
    /// <seealso cref="NPadInputState"/>
    [InputTemplate(stateType = typeof(NPadInputState))]
    public class NPad : Gamepad
    {
        public enum Position
        {
            Vertical,
            Sideways,
            Default = Vertical,
        }

        public enum NpadId : int
        {
            No1 = 0x00,
            No2 = 0x01,
            No3 = 0x02,
            No4 = 0x03,
            No5 = 0x04,
            No6 = 0x05,
            No7 = 0x06,
            No8 = 0x07,
            Handheld = 0x20,
            Debug = 0xFF,
        }

        [Flags]
        public enum NpadStyle
        {
            FullKey = (1 << 0),
            Handheld = (1 << 1),
            JoyDual = (1 << 2),
            JoyLeft = (1 << 3),
            JoyRight = (1 << 4),
        }

        public long ShowControllerSupportUI()
        {
            var command = NPadShowControllerSupportUI.Create();

            return OnDeviceCommand(ref command);
        }

        public void SetPosition(Position position)
        {
            var command = NPadOutputReport.Create();

            command.SetPosition(position);
            OnDeviceCommand(ref command);
        }

        public Position GetPosition()
        {
            var command = NPadOutputReport.Create();

            if (OnDeviceCommand(ref command) < 0)
                return Position.Default;
            return (Position)command.position;
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
