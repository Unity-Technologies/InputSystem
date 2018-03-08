using ISX.Controls;
using ISX.Haptics;
using ISX.LowLevel;
using ISX.Utilities;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ISX.XboxOne
{
    // IMPORTANT: State layout must match with GamepadInputStateXBOX in native.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct XboxOneGamepadState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('X', '1', 'G', 'P'); }
        }

        public enum Button
        {
            Menu = 2,
            View = 3,
            A = 4,
            B = 5,
            X = 6,
            Y = 7,
            DPadUp = 8,
            DPadDown = 9,
            DPadLeft = 10,
            DPadRight = 11,
            LeftShoulder = 12,
            RightShoulder = 13,
            LeftThumbstick = 14,
            RightThumbstick = 15,
            Paddle1 = 16,
            Paddle2 = 17,
            Paddle3 = 18,
            Paddle4 = 19,
        }

        [InputControl(name = "menu", template = "Button", bit = (uint)Button.Menu)]
        [InputControl(name = "view", template = "Button", bit = (uint)Button.View)]
        [InputControl(name = "buttonWest", template = "Button", bit = (uint)Button.X, displayName = "X")]
        [InputControl(name = "buttonSouth", template = "Button", bit = (uint)Button.A, displayName = "A", usage = "PrimaryAction")]
        [InputControl(name = "buttonEast", template = "Button", bit = (uint)Button.B, displayName = "B", usage = "SecondaryAction")]
        [InputControl(name = "buttonNorth", template = "Button", bit = (uint)Button.Y, displayName = "Y")]
        [InputControl(name = "dpad", template = "Dpad", sizeInBits = 4)]
        [InputControl(name = "dpad/up", bit = (uint)Button.DPadUp)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DPadRight)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DPadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DPadLeft)]
        [InputControl(name = "leftShoulder", template = "Button", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", template = "Button", bit = (uint)Button.RightShoulder)]
        [InputControl(name = "leftThumbstick", template = "Button", bit = (uint)Button.LeftThumbstick)]
        [InputControl(name = "rightThumbstick", template = "Button", bit = (uint)Button.RightThumbstick)]
        [InputControl(name = "paddle1", template = "Button", bit = (uint)Button.Paddle1)]
        [InputControl(name = "paddle2", template = "Button", bit = (uint)Button.Paddle2)]
        [InputControl(name = "paddle3", template = "Button", bit = (uint)Button.Paddle3)]
        [InputControl(name = "paddle4", template = "Button", bit = (uint)Button.Paddle4)]
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

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputTemplate(stateType = typeof(XboxOneGamepadState))]
    public class XboxOneGamepad : InputDevice, IHaptics   //Gamepad
    {
        public ButtonControl aButton { get; private set; }
        public ButtonControl xButton { get; private set; }
        public ButtonControl bButton { get; private set; }
        public ButtonControl yButton { get; private set; }

        public DpadControl dpad { get; private set; }

        public ButtonControl menu { get; private set; }

        public ButtonControl view { get; private set; }

        public ButtonControl leftShoulder { get; private set; }

        public ButtonControl rightShoulder { get; private set; }

        public ButtonControl leftThumbstick { get; private set; }

        public ButtonControl rightThumbstick { get; private set; }

        public ButtonControl paddle1 { get; private set; }

        public ButtonControl paddle2 { get; private set; }

        public ButtonControl paddle3 { get; private set; }

        public ButtonControl paddle4 { get; private set; }

        public StickControl leftStick { get; private set; }

        public StickControl rightStick { get; private set; }

        public ButtonControl leftTrigger { get; private set; }

        public ButtonControl rightTrigger { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            aButton = setup.GetControl<ButtonControl>(this, "buttonSouth");
            bButton = setup.GetControl<ButtonControl>(this, "buttonEast");
            yButton = setup.GetControl<ButtonControl>(this, "buttonNorth");
            xButton = setup.GetControl<ButtonControl>(this, "buttonWest");

            dpad = setup.GetControl<DpadControl>(this, "dpad");

            menu = setup.GetControl<ButtonControl>(this, "menu");
            view = setup.GetControl<ButtonControl>(this, "view");

            leftShoulder = setup.GetControl<ButtonControl>(this, "leftShoulder");
            rightShoulder = setup.GetControl<ButtonControl>(this, "rightShoulder");

            leftThumbstick = setup.GetControl<ButtonControl>(this, "leftThumbstick");
            rightThumbstick = setup.GetControl<ButtonControl>(this, "rightThumbstick");

            paddle1 = setup.GetControl<ButtonControl>(this, "paddle1");
            paddle2 = setup.GetControl<ButtonControl>(this, "paddle2");
            paddle3 = setup.GetControl<ButtonControl>(this, "paddle3");
            paddle4 = setup.GetControl<ButtonControl>(this, "paddle4");

            leftStick = setup.GetControl<StickControl>(this, "leftStick");
            rightStick = setup.GetControl<StickControl>(this, "rightStick");

            leftTrigger = setup.GetControl<ButtonControl>(this, "leftTrigger");
            rightTrigger = setup.GetControl<ButtonControl>(this, "rightTrigger");

            base.FinishSetup(setup);
        }

        public static XboxOneGamepad current { get; set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        public XboxOneGamepad() : base()
        {

        }

        public void PauseHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadOutputReport.Create();
            command.SetMotorSpeeds(0f, 0f, 0f, 0f);

            OnDeviceCommand(ref command);
        }

        public void ResetHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadOutputReport.Create();
            command.SetMotorSpeeds(0f, 0f, 0f, 0f);

            OnDeviceCommand(ref command);

            m_LeftMotor = null;
            m_RightMotor = null;
            m_LeftTriggerMotor = null;
            m_RightTriggerMotor = null;
        }

        public void ResumeHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadOutputReport.Create();

            if (m_LeftMotor.HasValue || m_RightMotor.HasValue || m_LeftTriggerMotor.HasValue || m_RightTriggerMotor.HasValue)
                command.SetMotorSpeeds(m_LeftMotor.Value, m_RightMotor.Value, m_LeftTriggerMotor.Value, m_RightTriggerMotor.Value);

            OnDeviceCommand(ref command);
        }

        public void SetMotorSpeeds(float leftMotor, float rightMotor, float leftTriggerMotor, float rightTriggerMotor)
        {
            var command = XboxOneGamepadOutputReport.Create();
            command.SetMotorSpeeds(leftMotor, rightMotor, leftTriggerMotor, rightTriggerMotor);

            OnDeviceCommand(ref command);

            m_LeftMotor = leftMotor;
            m_RightMotor = rightMotor;
            m_LeftTriggerMotor = leftTriggerMotor;
            m_RightTriggerMotor = rightTriggerMotor;
        }

        private float? m_LeftMotor;
        private float? m_RightMotor;
        private float? m_LeftTriggerMotor;
        private float? m_RightTriggerMotor;
    }

    /// <summary>
    /// XBOX output report sent as command to backend.
    /// </summary>
    /// // IMPORTANT: Struct must match the GamepadOutputReport in native
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct XboxOneGamepadOutputReport : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('X', '1', 'G', 'O'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 16;

        [FieldOffset(0)] public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public float leftMotor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)] public float rightMotor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)] public float leftTriggerMotor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 12)] public float rightTriggerMotor;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public void SetMotorSpeeds(float leftMotorLevel, float rightMotorLevel, float leftTriggerMotorLevel, float rightTriggerMotorLevel)
        {
            leftMotor = Mathf.Clamp(leftMotorLevel, 0.0f, 1.0f);
            rightMotor = Mathf.Clamp(rightMotorLevel, 0.0f, 1.0f);
            leftTriggerMotor = Mathf.Clamp(leftTriggerMotorLevel, 0.0f, 1.0f);
            rightTriggerMotor = Mathf.Clamp(rightTriggerMotorLevel, 0.0f, 1.0f);
        }

        public static XboxOneGamepadOutputReport Create()
        {
            return new XboxOneGamepadOutputReport
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }

}
