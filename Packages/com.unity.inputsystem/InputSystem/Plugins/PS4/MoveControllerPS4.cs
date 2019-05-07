#if UNITY_EDITOR || UNITY_PS4
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Plugins.PS4.LowLevel;

////TODO: player ID

namespace UnityEngine.InputSystem.Plugins.PS4.LowLevel
{
    // IMPORTANT: State layout must match with GamepadInputStatePS4 in native.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct MoveControllerStatePS4 : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('P', '4', 'M', 'V');

        public enum Button
        {
            Select = 0,
            TButton = 1,
            Move = 2,
            Start = 3,
            Triangle = 4,
            Circle = 5,
            Cross = 6,
            Square = 7,
        }

        [InputControl(name = "select", layout = "Button", bit = (uint)Button.Select, displayName = "Select")]
        [InputControl(name = "triggerButton", layout = "Button", bit = (uint)Button.TButton, displayName = "Trigger")]
        [InputControl(name = "move", layout = "Button", bit = (uint)Button.Move, displayName = "Move")]
        [InputControl(name = "start", layout = "Button", bit = (uint)Button.Start, displayName = "Start")]
        [InputControl(name = "triangle", layout = "Button", bit = (uint)Button.Triangle, displayName = "Triangle")]
        [InputControl(name = "circle", layout = "Button", bit = (uint)Button.Circle, displayName = "Circle")]
        [InputControl(name = "cross", layout = "Button", bit = (uint)Button.Cross, displayName = "Cross")]
        [InputControl(name = "square", layout = "Button", bit = (uint)Button.Square, displayName = "Square")]
        [FieldOffset(0)]
        public uint buttons;

        [InputControl(name = "trigger", layout = "Button", format = "FLT")]
        [FieldOffset(4)]
        public float trigger;

        [InputControl(name = "accelerometer", noisy = true)]
        [FieldOffset(8)]
        public Vector3 accelerometer;

        [InputControl(name = "gyro", noisy = true)]
        [FieldOffset(20)]
        public Vector3 gyro;

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
    public struct MoveControllerPS4OuputCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('P', 'S', 'M', 'C');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + 5;

        [Flags]
        public enum Flags
        {
            Rumble = 0x1,
            Color = 0x2,
        }

        [FieldOffset(0)] public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public byte flags;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 1)] public byte motorSpeed;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 2)] public byte redColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 3)] public byte greenColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)] public byte blueColor;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public void SetMotorSpeed(float motor)
        {
            flags |= (byte)Flags.Rumble;
            motorSpeed = (byte)Mathf.Clamp(motor * 255, 0, 255);
        }

        public void SetColor(Color color)
        {
            flags |= (byte)Flags.Color;
            redColor = (byte)Mathf.Clamp(color.r * 255, 0, 255);
            greenColor = (byte)Mathf.Clamp(color.g * 255, 0, 255);
            blueColor = (byte)Mathf.Clamp(color.b * 255, 0, 255);
        }

        public static MoveControllerPS4OuputCommand Create()
        {
            return new MoveControllerPS4OuputCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}

namespace UnityEngine.InputSystem.Plugins.PS4
{
    //Sync to PS4MoveDeviceDefinition in sixaxis.cpp
    [Serializable]
    class PS4MoveDeviceDescriptor
    {
        public uint slotId = 0;
        public uint moveIndex = 0;
        public uint defaultColorId = 0;
        public uint userId = 0;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        internal static PS4MoveDeviceDescriptor FromJson(string json)
        {
            return JsonUtility.FromJson<PS4MoveDeviceDescriptor>(json);
        }
    }

    [InputControlLayout(stateType = typeof(MoveControllerStatePS4))]
    public class MoveControllerPS4 : InputDevice, IHaptics
    {
        public ButtonControl selectButton { get; private set; }
        public ButtonControl triggerButton { get; private set; }
        public ButtonControl moveButton { get; private set; }
        public ButtonControl startButton { get; private set; }

        public ButtonControl squareButton { get; private set; }
        public ButtonControl triangleButton { get; private set; }
        public ButtonControl circleButton { get; private set; }
        public ButtonControl crossButton { get; private set; }

        public ButtonControl trigger { get; private set; }

        public Vector3Control accelerometer { get; private set; }

        public Vector3Control gyro { get; private set; }

        public new static ReadOnlyArray<MoveControllerPS4> all => new ReadOnlyArray<MoveControllerPS4>(s_Devices);

        public Color lightSphereColor
        {
            get
            {
                if (m_LightSphereColor.HasValue == false)
                {
                    return PS4ColorIdToColor(m_DefaultColorId);
                }

                return m_LightSphereColor.Value;
            }
        }

        public int slotIndex => m_SlotId;

        public int moveIndex => m_MoveIndex;

        public int ps4UserId => m_PS4UserId;

        public static MoveControllerPS4 GetBySlotIndex(int slotIndex, int moveIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4)
                throw new ArgumentException("Slot index out of range: " + slotIndex, "slotIndex");

            if (moveIndex < 0 || moveIndex >= 2)
                throw new ArgumentException("Move index out of range: " + slotIndex, "moveIndex");

            int realIndex = (slotIndex * 2) + moveIndex;

            if (s_Devices[realIndex] != null && s_Devices[realIndex].slotIndex == slotIndex)
            {
                return s_Devices[realIndex];
            }

            return null;
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            AddDeviceToList();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            RemoveDeviceFromList();
        }

        private void AddDeviceToList()
        {
            int realIndex = (slotIndex * 2) + moveIndex;

            if (realIndex >= 0 && realIndex < s_Devices.Length)
            {
#if !UNITY_EDITOR
                Debug.Assert(s_Devices[realIndex] == null, "PS4 move controller with same slotIndex/moveIndex already added");
#endif
                s_Devices[realIndex] = this;
            }
        }

        private void RemoveDeviceFromList()
        {
            int realIndex = (slotIndex * 2) + moveIndex;

            if (realIndex >= 0 && realIndex < s_Devices.Length && s_Devices[realIndex] == this)
                s_Devices[realIndex] = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            selectButton = builder.GetControl<ButtonControl>(this, "select");
            triggerButton = builder.GetControl<ButtonControl>(this, "triggerButton");
            moveButton = builder.GetControl<ButtonControl>(this, "move");
            startButton = builder.GetControl<ButtonControl>(this, "start");

            squareButton = builder.GetControl<ButtonControl>(this, "square");
            triangleButton = builder.GetControl<ButtonControl>(this, "triangle");
            circleButton = builder.GetControl<ButtonControl>(this, "circle");
            crossButton = builder.GetControl<ButtonControl>(this, "cross");

            trigger = builder.GetControl<ButtonControl>(this, "trigger");

            accelerometer = builder.GetControl<Vector3Control>(this, "accelerometer");
            gyro = builder.GetControl<Vector3Control>(this, "gyro");

            base.FinishSetup(builder);

            var capabilities = description.capabilities;
            var deviceDescriptor = PS4MoveDeviceDescriptor.FromJson(capabilities);

            if (deviceDescriptor != null)
            {
                m_SlotId = (int)deviceDescriptor.slotId;
                m_MoveIndex = (int)deviceDescriptor.moveIndex;
                m_DefaultColorId = (int)deviceDescriptor.defaultColorId;
                m_PS4UserId = (int)deviceDescriptor.userId;
            }
        }

        public void PauseHaptics()
        {
            if (!m_Motor.HasValue && !m_LightSphereColor.HasValue)
                return;

            var command = MoveControllerPS4OuputCommand.Create();
            command.SetMotorSpeed(0f);
            if (m_LightSphereColor.HasValue)
                command.SetColor(Color.black);

            ExecuteCommand(ref command);
        }

        public void ResetHaptics()
        {
            if (!m_Motor.HasValue && !m_LightSphereColor.HasValue)
                return;

            var command = MoveControllerPS4OuputCommand.Create();
            command.SetMotorSpeed(0f);

            ExecuteCommand(ref command);

            m_Motor = null;
            m_LightSphereColor = null;
        }

        public void ResumeHaptics()
        {
            if (!m_Motor.HasValue && !m_LightSphereColor.HasValue)
                return;

            var command = MoveControllerPS4OuputCommand.Create();

            if (m_Motor.HasValue)
                command.SetMotorSpeed(m_Motor.Value);
            if (m_LightSphereColor.HasValue)
                command.SetColor(m_LightSphereColor.Value);

            ExecuteCommand(ref command);
        }

        public void SetLightSphereColor(Color color)
        {
            var command = MoveControllerPS4OuputCommand.Create();
            command.SetColor(color);

            ExecuteCommand(ref command);

            m_LightSphereColor = color;
        }

        public void SetMotorSpeed(float motor)
        {
            var command = MoveControllerPS4OuputCommand.Create();
            command.SetMotorSpeed(motor);

            ExecuteCommand(ref command);

            m_Motor = motor;
        }

        private float? m_Motor;
        private Color? m_LightSphereColor;

        // Slot id for the gamepad. Once set will never change.
        private int m_SlotId = -1;
        private int m_MoveIndex = -1;
        private int m_DefaultColorId = -1;
        private int m_PS4UserId = -1;

        private static MoveControllerPS4[] s_Devices = new MoveControllerPS4[8];

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
