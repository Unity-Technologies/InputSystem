#if UNITY_EDITOR || UNITY_PS4
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.DualShock.LowLevel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;

////TODO: player ID

namespace UnityEngine.Experimental.Input.Plugins.DualShock.LowLevel
{
    // IMPORTANT: State layout must match with GamepadInputStatePS4 in native.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct DualShockGamepadStatePS4 : IInputStateTypeInfo
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

        [InputControl(name = "leftStickPress", bit = (uint)Button.L3, displayName = "L3")]
        [InputControl(name = "rightStickPress", bit = (uint)Button.R3, displayName = "R3")]
        [InputControl(name = "start", layout = "Button", bit = (uint)Button.Options)]
        [InputControl(name = "dpad", layout = "Dpad", sizeInBits = 4)]
        [InputControl(name = "dpad/up", bit = (uint)Button.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DpadLeft)]
        [InputControl(name = "leftTriggerButton", layout = "Button", bit = (uint)Button.L2, displayName = "L2")]
        [InputControl(name = "rightTriggerButton", layout = "Button", bit = (uint)Button.R2, displayName = "R2")]
        [InputControl(name = "leftShoulder", bit = (uint)Button.L1, displayName = "L1")]
        [InputControl(name = "rightShoulder", bit = (uint)Button.R1, displayName = "R1")]
        [InputControl(name = "buttonWest", bit = (uint)Button.Square, displayName = "Square")]
        [InputControl(name = "buttonSouth", bit = (uint)Button.Cross, displayName = "Cross")]
        [InputControl(name = "buttonEast", bit = (uint)Button.Circle, displayName = "Circle")]
        [InputControl(name = "buttonNorth", bit = (uint)Button.Triangle, displayName = "Triangle")]
        [InputControl(name = "touchpadButton", layout = "Button", bit = (uint)Button.TouchPad, displayName = "TouchPad")]
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

        [InputControl(name = "acceleration")]
        [FieldOffset(28)]
        public Vector3 acceleration;

        [InputControl(name = "orientation")]
        [FieldOffset(40)]
        public Quaternion orientation;

        [InputControl(name = "angularVelocity")]
        [FieldOffset(56)]
        public Vector3 angularVelocity;

        [InputControl]
        [FieldOffset(68)]
        public PS4Touch touch0;

        [InputControl]
        [FieldOffset(80)]
        public PS4Touch touch1;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    /// <summary>
    /// PS4 output report sent as command to backend.
    /// </summary>
    // IMPORTANT: Struct must match the DualShockPS4OutputReport in native
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct DualShockPS4OuputCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('P', 'S', 'G', 'O'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 6;

        [Flags]
        public enum Flags
        {
            Rumble = 0x1,
            Color = 0x2,
            ResetColor = 0x4,
            ResetOrientation = 0x8
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

        public void ResetOrientation()
        {
            flags |= (byte)Flags.ResetOrientation;
        }

        public static DualShockPS4OuputCommand Create()
        {
            return new DualShockPS4OuputCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }

    ////REVIEW: this is probably better transmitted as part of the InputDeviceDescription
    /// <summary>
    /// Retrieve the slot index, default color and user ID of the controller.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryPS4ControllerInfo : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'L', 'I', 'D'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + 12;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public int slotIndex;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public int defaultColorId;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        public int userId;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public QueryPS4ControllerInfo WithSlotIndex(int index)
        {
            slotIndex = index;
            return this;
        }

        public static QueryPS4ControllerInfo Create()
        {
            return new QueryPS4ControllerInfo()
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                slotIndex = -1,
                defaultColorId = -1,
                userId = -1
            };
        }
    }

    // IMPORTANT: State layout must match with GamepadInputTouchStatePS4 in native.
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct PS4Touch
    {
        [FieldOffset(0)] public int touchId;
        [FieldOffset(4)] public Vector2 position;
    }
}

namespace UnityEngine.Experimental.Input.Plugins.DualShock
{
    ////TODO: Unify this with general touch support
    public class PS4TouchControl : InputControl<PS4Touch>
    {
        /// <summary>
        /// The ID of the touch contact as reported by the underlying system.
        /// </summary>
        /// </remarks>
        [InputControl(alias = "touchId", offset = 0)]
        public IntegerControl touchId { get; private set; }
        [InputControl(usage = "Position", offset = 4)]
        public Vector2Control position { get; private set; }

        public PS4TouchControl()
        {
            m_StateBlock.format = new FourCC('P', '4', 'T', 'C');
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            touchId = builder.GetControl<IntegerControl>(this, "touchId");
            position = builder.GetControl<Vector2Control>(this, "position");
            base.FinishSetup(builder);
        }

        public override unsafe PS4Touch ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            var valuePtr = (PS4Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            return *valuePtr;
        }

        protected override unsafe void WriteUnprocessedValueInto(IntPtr statePtr, PS4Touch value)
        {
            var valuePtr = (PS4Touch*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            UnsafeUtility.MemCpy(valuePtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<PS4Touch>());
        }
    }


    [InputControlLayout(stateType = typeof(DualShockGamepadStatePS4))]
    public class DualShockGamepadPS4 : DualShockGamepad
    {
        ////TODO: move up into base
        public ReadOnlyArray<PS4TouchControl> touches { get; private set; }

        public new static ReadOnlyArray<DualShockGamepadPS4> all
        {
            get { return new ReadOnlyArray<DualShockGamepadPS4>(s_Devices); }
        }

        public Color lightBarColor
        {
            get
            {
                if (m_LightBarColor.HasValue == false)
                {
                    return PS4ColorIdToColor(m_DefaultColorId);
                }

                return m_LightBarColor.Value;
            }
        }

        public int slotIndex
        {
            get
            {
                UpdatePadSettingsIfNeeded();
                return m_SlotId;
            }
        }

        public int ps4UserId
        {
            get
            {
                UpdatePadSettingsIfNeeded();
                return m_PS4UserId;
            }
        }

        public static DualShockGamepadPS4 GetBySlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= s_Devices.Length)
                throw new ArgumentException("Slot index out of range: " + slotIndex, "slotIndex");

            if (s_Devices[slotIndex] != null && s_Devices[slotIndex].slotIndex == slotIndex)
            {
                return s_Devices[slotIndex];
            }

            return null;
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            var index = slotIndex;
            if (index >= 0 && index < s_Devices.Length)
            {
                Debug.Assert(s_Devices[index] == null, "PS4 gamepad with same slotIndex already added");
                s_Devices[index] = this;
            }
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            if (m_SlotId == -1)
                return;

            var index = slotIndex;
            if (index >= 0 && index < s_Devices.Length && s_Devices[index] == this)
                s_Devices[index] = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            var touchArray = new PS4TouchControl[2];

            touchArray[0] = builder.GetControl<PS4TouchControl>(this, "touch0");
            touchArray[1] = builder.GetControl<PS4TouchControl>(this, "touch1");

            touches = new ReadOnlyArray<PS4TouchControl>(touchArray);
        }

        public override void PauseHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OuputCommand.Create();
            command.SetMotorSpeeds(0f, 0f);
            if (m_LightBarColor.HasValue)
                command.SetColor(Color.black);

            ExecuteCommand(ref command);
        }

        public override void ResetHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OuputCommand.Create();
            command.SetMotorSpeeds(0f, 0f);

            if (m_LightBarColor.HasValue)
                command.ResetColor();

            ExecuteCommand(ref command);

            m_LargeMotor = null;
            m_SmallMotor = null;
            m_LightBarColor = null;
        }

        public override void ResumeHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OuputCommand.Create();

            if (m_LargeMotor.HasValue || m_SmallMotor.HasValue)
                command.SetMotorSpeeds(m_LargeMotor.Value, m_SmallMotor.Value);
            if (m_LightBarColor.HasValue)
                command.SetColor(m_LightBarColor.Value);

            ExecuteCommand(ref command);
        }

        public override void SetLightBarColor(Color color)
        {
            var command = DualShockPS4OuputCommand.Create();
            command.SetColor(color);

            ExecuteCommand(ref command);

            m_LightBarColor = color;
        }

        public void ResetLightBarColor()
        {
            var command = DualShockPS4OuputCommand.Create();
            command.ResetColor();

            ExecuteCommand(ref command);

            m_LightBarColor = null;
        }

        public override void SetMotorSpeeds(float largeMotor, float smallMotor)
        {
            var command = DualShockPS4OuputCommand.Create();
            command.SetMotorSpeeds(largeMotor, smallMotor);

            ExecuteCommand(ref command);

            m_LargeMotor = largeMotor;
            m_SmallMotor = smallMotor;
        }

        public void ResetOrientation()
        {
            var command = DualShockPS4OuputCommand.Create();
            command.ResetOrientation();

            ExecuteCommand(ref command);
        }

        private float? m_LargeMotor;
        private float? m_SmallMotor;
        private Color? m_LightBarColor;

        // Slot id for the gamepad. Once set will never change.
        private int m_SlotId = -1;
        private int m_DefaultColorId = -1;
        private int m_PS4UserId = -1;

        private static DualShockGamepadPS4[] s_Devices = new DualShockGamepadPS4[4];

        private void UpdatePadSettingsIfNeeded()
        {
            if (m_SlotId == -1)
            {
                var command = QueryPS4ControllerInfo.Create();

                if (ExecuteCommand(ref command) > 0)
                {
                    m_SlotId = command.slotIndex;
                    m_DefaultColorId = command.defaultColorId;
                    m_PS4UserId = command.userId;

                    if (!m_LightBarColor.HasValue)
                    {
                        m_LightBarColor = PS4ColorIdToColor(m_DefaultColorId);
                    }
                }
            }
        }

        private static Color PS4ColorIdToColor(int colorId)
        {
            switch (colorId)
            {
                case 0:
                    return Color.blue;
                case 1:
                    return Color.red;
                case 2:
                    return Color.green;
                case 3:
                    return Color.magenta;
                default:
                    return Color.black;
            }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_PS4
