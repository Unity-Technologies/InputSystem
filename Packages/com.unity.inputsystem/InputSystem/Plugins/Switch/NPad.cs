#if UNITY_EDITOR || UNITY_SWITCH
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Switch.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: gyro and accelerometer support

namespace UnityEngine.Experimental.Input.Plugins.Switch.LowLevel
{
    /// <summary>
    /// Structure of HID input reports for Switch NPad controllers.
    /// </summary>
    /// <rem
    /// <seealso href="http://en-americas-support.nintendo.com/app/answers/detail/a_id/22634/~/joy-con-controller-diagram"/>
    [StructLayout(LayoutKind.Explicit, Size = 60)]
    public unsafe struct NPadInputState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC('N', 'P', 'A', 'D');
        }

        [InputControl(name = "dpad")]
        [InputControl(name = "buttonNorth", displayName = "X", bit = (uint)Button.North)]
        [InputControl(name = "buttonSouth", displayName = "B", bit = (uint)Button.South, usage = "Back")]
        [InputControl(name = "buttonWest", displayName = "Y", bit = (uint)Button.West, usage = "SecondaryAction")]
        [InputControl(name = "buttonEast", displayName = "A", bit = (uint)Button.East, usage = "PrimaryAction")]
        [InputControl(name = "leftStickPress", displayName = "Left Stick", bit = (uint)Button.StickL)]
        [InputControl(name = "rightStickPress", displayName = "Right Stick", bit = (uint)Button.StickR)]
        [InputControl(name = "leftShoulder", displayName = "L", bit = (uint)Button.L)]
        [InputControl(name = "rightShoulder", displayName = "R", bit = (uint)Button.R)]
        [InputControl(name = "leftTrigger", displayName = "ZL", format = "BIT", bit = (uint)Button.ZL)]
        [InputControl(name = "rightTrigger", displayName = "ZR", format = "BIT", bit = (uint)Button.ZR)]
        [InputControl(name = "start", displayName = "Plus", bit = (uint)Button.Plus, usage = "Menu")]
        [InputControl(name = "select", displayName = "Minus", bit = (uint)Button.Minus)]
        [InputControl(name = "leftSL", displayName = "SL (Left)", layout = "Button", bit = (uint)Button.LSL)]
        [InputControl(name = "leftSR", displayName = "SR (Left)", layout = "Button", bit = (uint)Button.LSR)]
        [InputControl(name = "rightSL", displayName = "SL (Right)", layout = "Button", bit = (uint)Button.RSL)]
        [InputControl(name = "rightSR", displayName = "SR (Right)", layout = "Button", bit = (uint)Button.RSR)]
        [FieldOffset(0)]
        public uint buttons;

        [FieldOffset(4)]
        public Vector2 leftStick;

        [FieldOffset(12)]
        public Vector2 rightStick;

        [InputControl(name = "acceleration")]
        [FieldOffset(20)]
        public Vector3 acceleration;

        [InputControl(name = "attitude")]
        [FieldOffset(32)]
        public Quaternion attitude;

        [InputControl(name = "angularVelocity")]
        [FieldOffset(48)]
        public Vector3 angularVelocity;

        public float leftTrigger { get { return ((buttons & (1 << (int)Button.ZL)) != 0) ? 1f : 0f; } }

        public float rightTrigger { get { return ((buttons & (1 << (int)Button.ZR)) != 0) ? 1f : 0f; } }

        public enum Button
        {
            // Dpad buttons. Important to be first in the bitfield as we'll
            // point the DpadControl to it.
            // IMPORTANT: Order has to match what is expected by DpadControl.
            Up,
            Down,
            Left,
            Right,

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

            X = North,
            B = South,
            Y = West,
            A = East,
        }

        public NPadInputState WithButton(Button button, bool value = true)
        {
            var bit = (uint)1 << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;
            return this;
        }
    }

    /// <summary>
    /// Switch output report sent as command to the backend.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NPadStatusReport : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('N', 'P', 'D', 'S'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 24;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)]
        public NPad.NpadId npadId;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 1)]
        public NPad.Orientation orientation;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 2)]
        public short padding0;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public NPad.NpadStyle styleMask;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        public int colorLeftMain;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 12)]
        public int colorLeftSub;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 16)]
        public int colorRightMain;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 20)]
        public int colorRightSub;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static NPadStatusReport Create()
        {
            return new NPadStatusReport
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NPadControllerSupportCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('N', 'P', 'D', 'U'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 8;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)]
        public int command;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public int option;

        public enum Command : int
        {
            kShowUI,
            kSetHorizontalLayout,
            kStartSixAxisSensor,
            kStopSixAxisSensor,
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static NPadControllerSupportCommand Create(NPadControllerSupportCommand.Command _command, int _option = 0)
        {
            return new NPadControllerSupportCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                command = (int)_command,
                option = _option,
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NpadDeviceIOCTLShowUI : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC("NSUI"); } }
        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic() { return Type; }
        public static NpadDeviceIOCTLShowUI Create()
        {
            return new NpadDeviceIOCTLShowUI
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NpadDeviceIOCTLSetOrientation : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC("NSOR"); } }
        public const int kSize = InputDeviceCommand.kBaseCommandSize + 1;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)]
        public NPad.Orientation orientation;

        public FourCC GetTypeStatic() { return Type; }
        public static NpadDeviceIOCTLSetOrientation Create(NPad.Orientation _orientation)
        {
            return new NpadDeviceIOCTLSetOrientation
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                orientation = _orientation,
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NpadDeviceIOCTLStartSixAxisSensor : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC("SXST"); } }
        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic() { return Type; }
        public static NpadDeviceIOCTLStartSixAxisSensor Create()
        {
            return new NpadDeviceIOCTLStartSixAxisSensor
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct NpadDeviceIOCTLStopSixAxisSensor : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC("SXSP"); } }
        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic() { return Type; }
        public static NpadDeviceIOCTLStopSixAxisSensor Create()
        {
            return new NpadDeviceIOCTLStopSixAxisSensor
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.Switch
{
    /// <summary>
    /// An NPad controller for Switch, which can be a Joy-Con.
    /// </summary>
    /// <seealso cref="NPadInputState"/>
    [InputControlLayout(stateType = typeof(NPadInputState))]
    public class NPad : Gamepad
    {
        public ButtonControl leftSL { get; private set; }
        public ButtonControl leftSR { get; private set; }
        public ButtonControl rightSL { get; private set; }
        public ButtonControl rightSR { get; private set; }

        public Vector3Control acceleration { get; private set; }
        public QuaternionControl attitude { get; private set; }
        public Vector3Control angularVelocity { get; private set; }

        public enum Orientation : byte
        {
            Vertical,
            Horizontal,
            Default = Vertical,
        }

        public enum NpadId : byte
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
            Debug = 0xF0,
            Invalid = 0xFF,
        }

        [Flags]
        public enum NpadStyle : int
        {
            FullKey = (1 << 0),
            Handheld = (1 << 1),
            JoyDual = (1 << 2),
            JoyLeft = (1 << 3),
            JoyRight = (1 << 4),
        }

        public struct JoyConColor
        {
            public Color32 Main;
            public Color32 Sub;
        }

        public Orientation orientation
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_Orientation;
            }
        }
        public NpadId npadId
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_NpadId;
            }
        }
        public NpadStyle styleMask
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_StyleMask;
            }
        }
        public JoyConColor leftControllerColor
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_LeftControllerColor;
            }
        }

        public JoyConColor rightControllerColor
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_RightControllerColor;
            }
        }

        private bool m_IsDirty;

        private Orientation m_Orientation;
        private NpadId m_NpadId = NpadId.Invalid;
        private NpadStyle m_StyleMask;
        private JoyConColor m_LeftControllerColor;
        private JoyConColor m_RightControllerColor;

        protected override void RefreshConfiguration()
        {
            base.RefreshConfiguration();

            var command = NPadStatusReport.Create();

            if (ExecuteCommand(ref command) > 0)
            {
                m_NpadId = (NpadId)command.npadId;
                m_Orientation = (Orientation)command.orientation;
                m_StyleMask = (NpadStyle)command.styleMask;
                ReadNNColorIntoJoyConColor(ref m_LeftControllerColor, command.colorLeftMain, command.colorLeftSub);
                ReadNNColorIntoJoyConColor(ref m_RightControllerColor, command.colorRightMain, command.colorRightSub);
            }
        }

        // NOTE: This function should be static
        public long SetOrientationToSingleJoyCon(Orientation _orientation)
        {
            var supportCommand = NpadDeviceIOCTLSetOrientation.Create(_orientation);

            return ExecuteCommand(ref supportCommand);
        }

        public long StartSixAxisSensor()
        {
            var supportCommand = NpadDeviceIOCTLStartSixAxisSensor.Create();

            return ExecuteCommand(ref supportCommand);
        }

        public long StopSixAxisSensor()
        {
            var supportCommand = NpadDeviceIOCTLStopSixAxisSensor.Create();

            return ExecuteCommand(ref supportCommand);
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            leftSL = builder.GetControl<ButtonControl>(this, "leftSL");
            leftSR = builder.GetControl<ButtonControl>(this, "leftSR");
            rightSL = builder.GetControl<ButtonControl>(this, "rightSL");
            rightSR = builder.GetControl<ButtonControl>(this, "rightSR");

            acceleration = builder.GetControl<Vector3Control>(this, "acceleration");
            attitude = builder.GetControl<QuaternionControl>(this, "attitude");
            angularVelocity = builder.GetControl<Vector3Control>(this, "angularVelocity");
        }

        private static void ReadNNColorIntoJoyConColor(ref JoyConColor controllerColor, int mainColor, int subColor)
        {
            controllerColor.Main = ConvertNNColorToColor32(mainColor);
            controllerColor.Sub = ConvertNNColorToColor32(subColor);
        }

        private static Color32 ConvertNNColorToColor32(int color)
        {
            return new Color32((byte)(color & 0xFF), (byte)((color >> 8) & 0xFF), (byte)((color >> 16) & 0xFF), (byte)((color >> 24) & 0xFF));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
