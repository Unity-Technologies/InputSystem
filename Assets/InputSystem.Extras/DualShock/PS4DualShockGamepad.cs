using ISX.Controls;
using ISX.LowLevel;
using ISX.Plugins.DualShock;
using ISX.Utilities;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ISX.PS4DualShock
{
    // IMPORTANT: State layout must match with GamepadInputTouchStatePS4 in native.
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct PS4Touch
    {
        [FieldOffset(0)] public int touchId;
        [FieldOffset(4)] public Vector2 position;
    }

    public class PS4TouchControl : InputControl<PS4Touch>
    {
        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        /// </remarks>
        [InputControl(alias = "touchId", offset = 0)]
        public DiscreteControl touchId { get; private set; }
        [InputControl(usage = "position", offset = 4)]
        public Vector2Control position { get; private set; }
 
        public PS4TouchControl()
        {
            m_StateBlock.format = new FourCC('P', '4', 'T', 'C');
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            touchId = setup.GetControl<DiscreteControl>(this, "touchId");
            position = setup.GetControl<Vector2Control>(this, "position");
            base.FinishSetup(setup);
        }

        protected override unsafe PS4Touch ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = (PS4Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }
        protected override unsafe void WriteRawValueInto(IntPtr statePtr, PS4Touch value)
        {
            var valuePtr = (PS4Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<PS4Touch>());
        }
    }

    // IMPORTANT: State layout must match with GamepadInputStatePS4 in native.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct PS4DualShockGamepadState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('P', '4', 'G', 'P'); }
        }

        public enum Button
        {
            L3 = 1,
            R3 = 2,
            Options = 3,
            DpadUp = 4,
            DpadRight = 5,
            DpadDown = 6,
            DpadLeft = 7,
            L2 = 8,
            R2 = 9,
            L1 = 10,
            R1 = 11,
            Triangle = 12,
            Circle = 13,
            Cross = 14,
            Square = 15,
            TouchPad = 20,
        }

        [InputControl(name = "L3", template = "Button", bit = (uint)Button.L3)]
        [InputControl(name = "R3", template = "Button", bit = (uint)Button.R3)]
        [InputControl(name = "options", template = "Button", bit = (uint)Button.Options)]
        [InputControl(name = "dpad", template = "Dpad", sizeInBits = 4)]
        [InputControl(name = "dpad/up", bit = (uint)Button.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DpadLeft)]
        [InputControl(name = "L2", template = "Button", bit = (uint)Button.L2)]
        [InputControl(name = "R2", template = "Button", bit = (uint)Button.R2)]
        [InputControl(name = "L1", template = "Button", bit = (uint)Button.L1)]
        [InputControl(name = "R1", template = "Button", bit = (uint)Button.R1)]
        [InputControl(name = "buttonWest", template = "Button", bit = (uint)Button.Square, displayName = "Square")]
        [InputControl(name = "buttonSouth", template = "Button", bit = (uint)Button.Cross, displayName = "Cross", usage = "PrimaryAction" )]
        [InputControl(name = "buttonEast", template = "Button", bit = (uint)Button.Circle, displayName = "Circle", usage = "SecondaryAction")]
        [InputControl(name = "buttonNorth", template = "Button", bit = (uint)Button.Triangle, displayName = "Triangle")]
        [InputControl(name = "touchPadButton", template = "Button", bit = (uint)Button.TouchPad, displayName = "TouchPad")]
        [FieldOffset(0)]
        public Int32 buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(variant = "Default", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(variant = "Default", template = "Stick", usage = "Secondary2DMotion", processors = "deadzone")]
        [InputControl(variant = "Lefty", template = "Stick", usage = "Primary2DMotion", processors = "deadzone")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        /// <summary>
        /// Position of the left trigger.
        /// </summary>
        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger.
        /// </summary>
        [InputControl(variant = "Default", template = "Button", format = "FLT", usage = "PrimaryTrigger")]
        [InputControl(variant = "Lefty", template = "Button", format = "FLT", usage = "SecondaryTrigger")]
        [FieldOffset(24)]
        public float rightTrigger;

        [InputControl(name = "acceleration", template = "Vector3")]
        [FieldOffset(28)]
        public Vector3 acceleration;

        [InputControl(name = "orientation", template = "Vector3")]
        [FieldOffset(40)]
        public Vector3 orientation;

        [InputControl(name = "angularVelocity", template = "Vector3")]
        [FieldOffset(52)]
        public Vector3 angularVelocity;

        [InputControl(template = "PS4Touch")]
        [FieldOffset(64)]
        public PS4Touch touch0;

        [InputControl(template = "PS4Touch")]
        [FieldOffset(76)]
        public PS4Touch touch1;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputTemplate(stateType = typeof(PS4DualShockGamepadState))]
    public class PS4DualShockGamepad : InputDevice, IDualShockHaptics   //Gamepad
    {
        public ButtonControl crossButton { get; private set; }
        public ButtonControl circleButton { get; private set; }
        public ButtonControl triangleButton { get; private set; }
        public ButtonControl squareButton { get; private set; }

        public DpadControl dpad { get; private set; }

        public ButtonControl L3 { get; private set; }

        public ButtonControl R3 { get; private set; }

        public ButtonControl L2 { get; private set; }

        public ButtonControl R2 { get; private set; }

        public ButtonControl L1 { get; private set; }

        public ButtonControl R1 { get; private set; }

        public ButtonControl optionsButton { get; private set; }

        public ButtonControl touchPadButton { get; private set; }

        public ButtonControl leftShoulder { get; private set; }

        public ButtonControl rightShoulder { get; private set; }

        public StickControl leftStick { get; private set; }

        public StickControl rightStick { get; private set; }

        public ButtonControl leftTrigger { get; private set; }

        public ButtonControl rightTrigger { get; private set; }

        public Vector3Control acceleration { get; private set; }
        public Vector3Control orientation { get; private set; }

        public Vector3Control angularVelocity { get; private set; }

        public ReadOnlyArray<PS4TouchControl> touches { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            crossButton = setup.GetControl<ButtonControl>(this, "buttonSouth");
            circleButton = setup.GetControl<ButtonControl>(this, "buttonEast");
            triangleButton = setup.GetControl<ButtonControl>(this, "buttonNorth");
            squareButton = setup.GetControl<ButtonControl>(this, "buttonWest");

            dpad = setup.GetControl<DpadControl>(this, "dpad");

            L1 = setup.GetControl<ButtonControl>(this, "L1");
            R1 = setup.GetControl<ButtonControl>(this, "R1");
            L2 = setup.GetControl<ButtonControl>(this, "L2");
            R2 = setup.GetControl<ButtonControl>(this, "R2");
            L3 = setup.GetControl<ButtonControl>(this, "L3");
            R3 = setup.GetControl<ButtonControl>(this, "R3");

            optionsButton = setup.GetControl<ButtonControl>(this, "options");
            touchPadButton = setup.GetControl<ButtonControl>(this, "touchPadButton");

            leftStick = setup.GetControl<StickControl>(this, "leftStick");
            rightStick = setup.GetControl<StickControl>(this, "rightStick");

            leftTrigger = setup.GetControl<ButtonControl>(this, "leftTrigger");
            rightTrigger = setup.GetControl<ButtonControl>(this, "rightTrigger");

            acceleration = setup.GetControl<Vector3Control>(this, "acceleration");
            orientation = setup.GetControl<Vector3Control>(this, "orientation");
            angularVelocity = setup.GetControl<Vector3Control>(this, "angularVelocity");

            var touchArray = new PS4TouchControl[2];

            touchArray[0] = setup.GetControl<PS4TouchControl>(this, "touch0");
            touchArray[1] = setup.GetControl<PS4TouchControl>(this, "touch1");

            touches = new ReadOnlyArray<PS4TouchControl>(touchArray);

            base.FinishSetup(setup);
        }

        public static PS4DualShockGamepad current { get; set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        public PS4DualShockGamepad() : base()
        {

        }

        public void PauseHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OutputReport.Create();
            command.SetMotorSpeeds(0f, 0f);
            if (m_LightBarColor.HasValue)
                command.SetColor(Color.black);

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);
        }

        public void ResetHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OutputReport.Create();
            command.SetMotorSpeeds(0f, 0f);

            if (m_LightBarColor.HasValue)
                command.ResetColor();

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);

            m_LargeMotor = null;
            m_SmallMotor = null;
            m_LightBarColor = null;
        }

        public void ResumeHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OutputReport.Create();

            if (m_LargeMotor.HasValue || m_SmallMotor.HasValue)
                command.SetMotorSpeeds(m_LargeMotor.Value, m_SmallMotor.Value);
            if (m_LightBarColor.HasValue)
                command.SetColor(m_LightBarColor.Value);

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);
        }

        public void SetLightBarColor(Color color)
        {
            var command = DualShockPS4OutputReport.Create();
            command.SetColor(color);

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);

            m_LightBarColor = color;
        }

        public void ResetLightBarColor()
        {
            var command = DualShockPS4OutputReport.Create();
            command.ResetColor();

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);

            m_LightBarColor = null;
        }

        public void SetMotorSpeeds(float largeMotor, float smallMotor)
        {
            var command = DualShockPS4OutputReport.Create();
            command.SetMotorSpeeds(largeMotor, smallMotor);

            OnDeviceCommand<DualShockPS4OutputReport>(ref command);

            m_LargeMotor = largeMotor;
            m_SmallMotor = smallMotor;
        }

        private float? m_LargeMotor;
        private float? m_SmallMotor;
        private Color? m_LightBarColor;
    }

    /// <summary>
    /// PS4 output report sent as command to backend.
    /// </summary>
    /// // IMPORTANT: Struct must make the DualShockPS4OutputReport in native
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct DualShockPS4OutputReport : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('P', 'S', 'G', 'O'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 9;

        [Flags]
        public enum Flags
        {
            Rumble = 0x1,
            Color = 0x2,
            ResetColor = 0x4
        }

        [FieldOffset(0)] public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public byte flags;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 1)] public byte largeMotorSpeed;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 2)] public byte smallMotorSpeed;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 3)] public byte redColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)] public byte greenColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 5)] public byte blueColor;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public void SetMotorSpeeds(float largeMotor, float smallMotor)
        {
            flags |= (byte)Flags.Rumble;
            largeMotorSpeed = (byte)Mathf.Clamp(largeMotor * 255, 0, 255);
            smallMotorSpeed = (byte)Mathf.Clamp(smallMotor * 255, 0, 255);
        }

        public void SetColor(Color color)
        {
            flags |= (byte)Flags.Color;
            redColor = (byte)Mathf.Clamp(color.r * 255, 0, 255);
            greenColor = (byte)Mathf.Clamp(color.g * 255, 0, 255);
            blueColor = (byte)Mathf.Clamp(color.b * 255, 0, 255);
        }

        public void ResetColor()
        {
            flags |= (byte)Flags.ResetColor;
        }

        public static DualShockPS4OutputReport Create()
        {
            return new DualShockPS4OutputReport
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }

}
