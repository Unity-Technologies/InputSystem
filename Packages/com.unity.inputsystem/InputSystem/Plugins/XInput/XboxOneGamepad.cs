#if UNITY_EDITOR || UNITY_XBOXONE
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.XInput.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: player ID

namespace UnityEngine.Experimental.Input.Plugins.XInput.LowLevel
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

        [InputControl(name = "start", bit = (uint)Button.Menu, displayName = "Menu")]
        [InputControl(name = "select", bit = (uint)Button.View, displayName = "View")]
        [InputControl(name = "buttonWest", bit = (uint)Button.X, displayName = "X")]
        [InputControl(name = "buttonSouth", bit = (uint)Button.A, displayName = "A")]
        [InputControl(name = "buttonEast", bit = (uint)Button.B, displayName = "B")]
        [InputControl(name = "buttonNorth", bit = (uint)Button.Y, displayName = "Y")]
        [InputControl(name = "leftShoulder", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)Button.RightShoulder)]
        [InputControl(name = "leftStickPress", bit = (uint)Button.LeftThumbstick)]
        [InputControl(name = "rightStickPress", bit = (uint)Button.RightThumbstick)]
        [InputControl(name = "dpad", layout = "Dpad", sizeInBits = 4, bit = 8)]
        [InputControl(name = "dpad/up", bit = (uint)Button.DPadUp)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DPadRight)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DPadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DPadLeft)]
        [InputControl(name = "paddle1", layout = "Button", bit = (uint)Button.Paddle1)]
        [InputControl(name = "paddle2", layout = "Button", bit = (uint)Button.Paddle2)]
        [InputControl(name = "paddle3", layout = "Button", bit = (uint)Button.Paddle3)]
        [InputControl(name = "paddle4", layout = "Button", bit = (uint)Button.Paddle4)]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position.
        /// </summary>
        [InputControl(layout = "Stick")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position.
        /// </summary>
        [InputControl(layout = "Stick")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        /// <summary>
        /// Position of the left trigger.
        /// </summary>
        [InputControl]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger.
        /// </summary>
        [InputControl]
        [FieldOffset(24)]
        public float rightTrigger;

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public XboxOneGamepadState WithButton(Button button)
        {
            buttons |= (uint)1 << (int)button;
            return this;
        }
    }

    /// <summary>
    /// XBOX output report sent as command to backend.
    /// </summary>
    // IMPORTANT: Struct must match the GamepadOutputReport in native
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct XboxOneGamepadRumbleCommand : IInputDeviceCommandInfo
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

        public static XboxOneGamepadRumbleCommand Create()
        {
            return new XboxOneGamepadRumbleCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }

    /// <summary>
    /// Retrieve the slot index, default color and user ID of the controller.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryXboxControllerInfo : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'N', 'F', 'O'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 12;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public ulong gamepadId;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        public int userId;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public QueryXboxControllerInfo WithGamepadId(ulong id)
        {
            gamepadId = id;
            return this;
        }

        public static QueryXboxControllerInfo Create()
        {
            return new QueryXboxControllerInfo()
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                gamepadId = 0,
                userId = -1
            };
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.XInput
{
    [InputControlLayout(stateType = typeof(XboxOneGamepadState))]
    public class XboxOneGamepad : XInputController, IXboxOneRumble
    {
        private ulong m_GamepadId = 0;
        private int m_XboxOneUserId = -1;

        private static XboxOneGamepad[] s_Devices;

        public ButtonControl paddle1 { get; private set; }
        public ButtonControl paddle2 { get; private set; }
        public ButtonControl paddle3 { get; private set; }
        public ButtonControl paddle4 { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            paddle1 = builder.GetControl<ButtonControl>(this, "paddle1");
            paddle2 = builder.GetControl<ButtonControl>(this, "paddle2");
            paddle3 = builder.GetControl<ButtonControl>(this, "paddle3");
            paddle4 = builder.GetControl<ButtonControl>(this, "paddle4");
        }

        public ulong gamepadId
        {
            get
            {
                UpdatePadSettings();
                return m_GamepadId;
            }
        }

        public int xboxOneUserId
        {
            get
            {
                UpdatePadSettings();
                return m_XboxOneUserId;
            }
        }

        public new static ReadOnlyArray<XboxOneGamepad> all
        {
            get { return new ReadOnlyArray<XboxOneGamepad>(s_Devices); }
        }

        public static XboxOneGamepad GetByGamepadId(ulong gamepadId)
        {
            for (int i = 0; i < s_Devices.Length; i++)
            {
                if (s_Devices[i] != null && s_Devices[i].gamepadId == gamepadId)
                {
                    return s_Devices[i];
                }
            }

            return null;
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            ArrayHelpers.Append(ref s_Devices, this);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            ArrayHelpers.Erase(ref s_Devices, this);
        }

        private void UpdatePadSettings()
        {
            var command = QueryXboxControllerInfo.Create();

            if (ExecuteCommand(ref command) > 0)
            {
                m_GamepadId = command.gamepadId;
                m_XboxOneUserId = command.userId;
            }
        }

        public new static XboxOneGamepad current { get; set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        public override void PauseHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadRumbleCommand.Create();
            command.SetMotorSpeeds(0f, 0f, 0f, 0f);

            ExecuteCommand(ref command);
        }

        public override void ResetHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadRumbleCommand.Create();
            command.SetMotorSpeeds(0f, 0f, 0f, 0f);

            ExecuteCommand(ref command);

            m_LeftMotor = null;
            m_RightMotor = null;
            m_LeftTriggerMotor = null;
            m_RightTriggerMotor = null;
        }

        public override void ResumeHaptics()
        {
            if (!m_LeftMotor.HasValue && !m_RightMotor.HasValue && !m_LeftTriggerMotor.HasValue && !m_RightTriggerMotor.HasValue)
                return;

            var command = XboxOneGamepadRumbleCommand.Create();

            if (m_LeftMotor.HasValue || m_RightMotor.HasValue || m_LeftTriggerMotor.HasValue || m_RightTriggerMotor.HasValue)
                command.SetMotorSpeeds(m_LeftMotor.Value, m_RightMotor.Value, m_LeftTriggerMotor.Value, m_RightTriggerMotor.Value);

            ExecuteCommand(ref command);
        }

        public void SetMotorSpeeds(float leftMotor, float rightMotor, float leftTriggerMotor, float rightTriggerMotor)
        {
            var command = XboxOneGamepadRumbleCommand.Create();
            command.SetMotorSpeeds(leftMotor, rightMotor, leftTriggerMotor, rightTriggerMotor);

            ExecuteCommand(ref command);

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
}
#endif // UNITY_EDITOR || UNITY_XBOXONE
