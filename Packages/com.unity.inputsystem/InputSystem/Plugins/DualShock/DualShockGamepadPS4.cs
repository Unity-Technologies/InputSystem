#if UNITY_EDITOR || UNITY_PS4
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.DualShock.LowLevel;
using Unity.Collections.LowLevel.Unsafe;

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
        [InputControl(name = "options", layout = "Button", bit = (uint)Button.Options)]
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
        public Vector3 orientation;

        [InputControl(name = "angularVelocity")]
        [FieldOffset(52)]
        public Vector3 angularVelocity;

        [InputControl]
        [FieldOffset(64)]
        public PS4Touch touch0;

        [InputControl]
        [FieldOffset(76)]
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

        public static DualShockPS4OuputCommand Create()
        {
            return new DualShockPS4OuputCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
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
        [InputControl(usage = "position", offset = 4)]
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

        public override unsafe PS4Touch ReadRawValueFrom(IntPtr statePtr)
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


    [InputLayout(stateType = typeof(DualShockGamepadStatePS4))]
    public class DualShockGamepadPS4 : DualShockGamepad, IDualShockHaptics
    {
        ////TODO: move up into base
        public ReadOnlyArray<PS4TouchControl> touches { get; private set; }

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

            OnDeviceCommand(ref command);
        }

        public override void ResetHaptics()
        {
            if (!m_LargeMotor.HasValue && !m_SmallMotor.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockPS4OuputCommand.Create();
            command.SetMotorSpeeds(0f, 0f);

            if (m_LightBarColor.HasValue)
                command.ResetColor();

            OnDeviceCommand(ref command);

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

            OnDeviceCommand(ref command);
        }

        public override void SetLightBarColor(Color color)
        {
            var command = DualShockPS4OuputCommand.Create();
            command.SetColor(color);

            OnDeviceCommand(ref command);

            m_LightBarColor = color;
        }

        public void ResetLightBarColor()
        {
            var command = DualShockPS4OuputCommand.Create();
            command.ResetColor();

            OnDeviceCommand(ref command);

            m_LightBarColor = null;
        }

        public override void SetMotorSpeeds(float largeMotor, float smallMotor)
        {
            var command = DualShockPS4OuputCommand.Create();
            command.SetMotorSpeeds(largeMotor, smallMotor);

            OnDeviceCommand(ref command);

            m_LargeMotor = largeMotor;
            m_SmallMotor = smallMotor;
        }

        private float? m_LargeMotor;
        private float? m_SmallMotor;
        private Color? m_LightBarColor;
    }
}
#endif // UNITY_EDITOR || UNITY_PS4
