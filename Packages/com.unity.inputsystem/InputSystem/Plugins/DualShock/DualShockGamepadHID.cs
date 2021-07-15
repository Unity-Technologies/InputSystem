#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA || PACKAGE_DOCS_GENERATION
using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: figure out sensor formats and add support for acceleration, angularVelocity, and orientation (also add to base layout then)

namespace UnityEngine.InputSystem.DualShock.LowLevel
{
    /// <summary>
    /// Structure of HID input reports for PS4 DualShock 4 controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct DualShock4HIDInputReport : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte reportId;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "leftStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "leftStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(1)] public byte leftStickX;
        [FieldOffset(2)] public byte leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "rightStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(3)] public byte rightStickX;
        [FieldOffset(4)] public byte rightStickY;

        [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4, defaultState = 8)]
        [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 0, sizeInBits = 4)]
        [InputControl(name = "buttonWest", displayName = "Square", bit = 4)]
        [InputControl(name = "buttonSouth", displayName = "Cross", bit = 5)]
        [InputControl(name = "buttonEast", displayName = "Circle", bit = 6)]
        [InputControl(name = "buttonNorth", displayName = "Triangle", bit = 7)]
        [FieldOffset(5)] public byte buttons1;
        [InputControl(name = "leftShoulder", bit = 0)]
        [InputControl(name = "rightShoulder", bit = 1)]
        [InputControl(name = "leftTriggerButton", layout = "Button", bit = 2, synthetic = true)]
        [InputControl(name = "rightTriggerButton", layout = "Button", bit = 3, synthetic = true)]
        [InputControl(name = "select", displayName = "Share", bit = 4)]
        [InputControl(name = "start", displayName = "Options", bit = 5)]
        [InputControl(name = "leftStickPress", bit = 6)]
        [InputControl(name = "rightStickPress", bit = 7)]
        [FieldOffset(6)] public byte buttons2;
        [InputControl(name = "systemButton", layout = "Button", displayName = "System", bit = 0)]
        [InputControl(name = "touchpadButton", layout = "Button", displayName = "Touchpad Press", bit = 1)]
        [FieldOffset(7)] public byte buttons3;

        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(8)] public byte leftTrigger;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(9)] public byte rightTrigger;

        [FieldOffset(30)] public byte batteryLevel;

        ////TODO: touchpad

        public FourCC format => new FourCC('H', 'I', 'D');
    }

    /// <summary>
    /// Structure of HID input reports for PS3 DualShock 3 controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal unsafe struct DualShock3HIDInputReport : IInputStateTypeInfo
    {
        [FieldOffset(0)] private ushort padding1;

        [InputControl(name = "select", displayName = "Share", bit = 0)]
        [InputControl(name = "leftStickPress", bit = 1)]
        [InputControl(name = "rightStickPress", bit = 2)]
        [InputControl(name = "start", displayName = "Options", bit = 3)]
        [InputControl(name = "dpad", format = "BIT", layout = "Dpad", bit = 4, sizeInBits = 4)]
        [InputControl(name = "dpad/up", bit = 4)]
        [InputControl(name = "dpad/right", bit = 5)]
        [InputControl(name = "dpad/down", bit = 6)]
        [InputControl(name = "dpad/left", bit = 7)]
        [FieldOffset(2)] public byte buttons1;
        [InputControl(name = "leftTriggerButton", layout = "Button", bit = 0, synthetic = true)]
        [InputControl(name = "rightTriggerButton", layout = "Button", bit = 1, synthetic = true)]
        [InputControl(name = "leftShoulder", bit = 2)]
        [InputControl(name = "rightShoulder", bit = 3)]
        [InputControl(name = "buttonNorth", displayName = "Triangle", bit = 4)]
        [InputControl(name = "buttonEast", displayName = "Circle", bit = 5)]
        [InputControl(name = "buttonSouth", displayName = "Cross", bit = 6)]
        [InputControl(name = "buttonWest", displayName = "Square", bit = 7)]
        [FieldOffset(3)] public byte buttons2;

        [InputControl(name = "systemButton", layout = "Button", displayName = "System", bit = 0)]
        [InputControl(name = "touchpadButton", layout = "Button", displayName = "Touchpad Press", bit = 1)] // always 0, does not exist on DualShock 3
        [FieldOffset(4)] public byte buttons3;

        [FieldOffset(5)] private byte padding2;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "leftStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "leftStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(6)] public byte leftStickX;
        [FieldOffset(7)] public byte leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "rightStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(8)] public byte rightStickX;
        [FieldOffset(9)] public byte rightStickY;

        [FieldOffset(10)] private fixed byte padding3[8];

        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(18)] public byte leftTrigger;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(19)] public byte rightTrigger;

        public FourCC format
        {
            get { return new FourCC('H', 'I', 'D'); }
        }
    }

    /// <summary>
    /// PS4 output report sent as command to HID backend.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal unsafe struct DualShockHIDOutputReport : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('H', 'I', 'D', 'O');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + 32;
        internal const int kReportId = 5;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "No better term for underlying data.")]
        [Flags]
        public enum Flags
        {
            Rumble = 0x1,
            Color = 0x2
        }

        [FieldOffset(0)] public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public byte reportId;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flags", Justification = "No better term for underlying data.")]
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 1)] public byte flags;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 2)] public fixed byte unknown1[2];
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)] public byte highFrequencyMotorSpeed;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 5)] public byte lowFrequencyMotorSpeed;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 6)] public byte redColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 7)] public byte greenColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)] public byte blueColor;
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 9)] public fixed byte unknown2[23];

        public FourCC typeStatic => Type;

        public void SetMotorSpeeds(float lowFreq, float highFreq)
        {
            flags |= (byte)Flags.Rumble;
            lowFrequencyMotorSpeed = (byte)Mathf.Clamp(lowFreq * 255, 0, 255);
            highFrequencyMotorSpeed = (byte)Mathf.Clamp(highFreq * 255, 0, 255);
        }

        public void SetColor(Color color)
        {
            flags |= (byte)Flags.Color;
            redColor = (byte)Mathf.Clamp(color.r * 255, 0, 255);
            greenColor = (byte)Mathf.Clamp(color.g * 255, 0, 255);
            blueColor = (byte)Mathf.Clamp(color.b * 255, 0, 255);
        }

        public static DualShockHIDOutputReport Create()
        {
            return new DualShockHIDOutputReport
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                reportId = kReportId,
            };
        }
    }
}

namespace UnityEngine.InputSystem.DualShock
{
    /// <summary>
    /// PS4 DualShock controller that is interfaced to a HID backend.
    /// </summary>
    [InputControlLayout(stateType = typeof(DualShock4HIDInputReport), hideInUI = true)]
    [Scripting.Preserve]
    public class DualShock4GamepadHID : DualShockGamepad
    {
        public ButtonControl leftTriggerButton { get; protected set; }
        public ButtonControl rightTriggerButton { get; protected set; }
        public ButtonControl playStationButton { get; protected set; }

        protected override void FinishSetup()
        {
            leftTriggerButton = GetChildControl<ButtonControl>("leftTriggerButton");
            rightTriggerButton = GetChildControl<ButtonControl>("rightTriggerButton");
            playStationButton = GetChildControl<ButtonControl>("systemButton");

            base.FinishSetup();
        }

        public override void PauseHaptics()
        {
            if (!m_LowFrequencyMotorSpeed.HasValue && !m_HighFrequenceyMotorSpeed.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockHIDOutputReport.Create();
            command.SetMotorSpeeds(0f, 0f);
            ////REVIEW: when pausing&resuming haptics, you probably don't want the lightbar color to change
            if (m_LightBarColor.HasValue)
                command.SetColor(Color.black);

            ExecuteCommand(ref command);
        }

        public override void ResetHaptics()
        {
            if (!m_LowFrequencyMotorSpeed.HasValue && !m_HighFrequenceyMotorSpeed.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockHIDOutputReport.Create();
            command.SetMotorSpeeds(0f, 0f);
            if (m_LightBarColor.HasValue)
                command.SetColor(Color.black);

            ExecuteCommand(ref command);

            m_HighFrequenceyMotorSpeed = null;
            m_LowFrequencyMotorSpeed = null;
            m_LightBarColor = null;
        }

        public override void ResumeHaptics()
        {
            if (!m_LowFrequencyMotorSpeed.HasValue && !m_HighFrequenceyMotorSpeed.HasValue && !m_LightBarColor.HasValue)
                return;

            var command = DualShockHIDOutputReport.Create();

            if (m_LowFrequencyMotorSpeed.HasValue || m_HighFrequenceyMotorSpeed.HasValue)
                command.SetMotorSpeeds(m_LowFrequencyMotorSpeed.Value, m_HighFrequenceyMotorSpeed.Value);
            if (m_LightBarColor.HasValue)
                command.SetColor(m_LightBarColor.Value);

            ExecuteCommand(ref command);
        }

        ////FIXME: SetLightBarColor and SetMotorSpeeds to not mutually respect their settings

        public override void SetLightBarColor(Color color)
        {
            var command = DualShockHIDOutputReport.Create();
            command.SetColor(color);

            ExecuteCommand(ref command);

            m_LightBarColor = color;
        }

        public override void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            var command = DualShockHIDOutputReport.Create();
            command.SetMotorSpeeds(lowFrequency, highFrequency);

            ExecuteCommand(ref command);

            m_LowFrequencyMotorSpeed = lowFrequency;
            m_HighFrequenceyMotorSpeed = highFrequency;
        }

        /// <summary>
        /// Set motor speeds of both motors and the light bar color simultaneously.
        /// </summary>
        /// <param name="lowFrequency"><see cref="Haptics.IDualMotorRumble.SetMotorSpeeds"/></param>
        /// <param name="highFrequency"><see cref="Haptics.IDualMotorRumble.SetMotorSpeeds"/></param>
        /// <param name="color"><see cref="IDualShockHaptics.SetLightBarColor"/></param>
        /// <returns>True if the command succeeded. Will return false if another command is currently being processed.</returns>
        /// <remarks>
        /// Use this method to set both the motor speeds and the light bar color in the same call. This method exists
        /// because it is currently not possible to process an input/output control (IOCTL) command while another one
        /// is in flight. For example, calling <see cref="SetMotorSpeeds"/> immediately after calling
        /// <see cref="SetLightBarColor"/> might result in only the light bar color changing. The <see cref="SetMotorSpeeds"/>
        /// call could fail. It is however possible to combine multiple IOCTL instructions into a single command, which
        /// is what this method does.
        ///
        /// See <see cref="Haptics.IDualMotorRumble.SetMotorSpeeds"/> and <see cref="IDualShockHaptics.SetLightBarColor"/>
        /// for the respective documentation regarding setting rumble and light bar color.</remarks>
        public bool SetMotorSpeedsAndLightBarColor(float lowFrequency, float highFrequency, Color color)
        {
            var command = DualShockHIDOutputReport.Create();
            command.SetMotorSpeeds(lowFrequency, highFrequency);
            command.SetColor(color);

            var result = ExecuteCommand(ref command);

            m_LowFrequencyMotorSpeed = lowFrequency;
            m_HighFrequenceyMotorSpeed = highFrequency;
            m_LightBarColor = color;

            return result >= 0;
        }

        private float? m_LowFrequencyMotorSpeed;
        private float? m_HighFrequenceyMotorSpeed;
        private Color? m_LightBarColor;
    }

    [InputControlLayout(stateType = typeof(DualShock3HIDInputReport), hideInUI = true, displayName = "PS3 Controller")]
    [Scripting.Preserve]
    public class DualShock3GamepadHID : DualShockGamepad
    {
        public ButtonControl leftTriggerButton { get; private set; }
        public ButtonControl rightTriggerButton { get; private set; }
        public ButtonControl playStationButton { get; private set; }

        protected override void FinishSetup()
        {
            leftTriggerButton = GetChildControl<ButtonControl>("leftTriggerButton");
            rightTriggerButton = GetChildControl<ButtonControl>("rightTriggerButton");
            playStationButton = GetChildControl<ButtonControl>("systemButton");

            base.FinishSetup();
        }

        // TODO: see if we can implement rumble support on DualShock 3
    }
}

// PS4 HID structures:
//
// struct PS4InputReport1
// {
//     byte reportId;             // #0
//     byte leftStickX;           // #1
//     byte leftStickY;           // #2
//     byte rightStickX;          // #3
//     byte rightStickY;          // #4
//     byte dpad : 4;             // #5 bit #0 (0=up, 2=right, 4=down, 6=left)
//     byte squareButton : 1;     // #5 bit #4
//     byte crossButton : 1;      // #5 bit #5
//     byte circleButton : 1;     // #5 bit #6
//     byte triangleButton : 1;   // #5 bit #7
//     byte leftShoulder : 1;     // #6 bit #0
//     byte rightShoulder : 1;    // #6 bit #1
//     byte leftTriggerButton : 2;// #6 bit #2
//     byte rightTriggerButton : 2;// #6 bit #3
//     byte shareButton : 1;      // #6 bit #4
//     byte optionsButton : 1;    // #6 bit #5
//     byte leftStickPress : 1;   // #6 bit #6
//     byte rightStickPress : 1;  // #6 bit #7
//     byte psButton : 1;         // #7 bit #0
//     byte touchpadPress : 1;    // #7 bit #1
//     byte padding : 6;
//     byte leftTrigger;          // #8
//     byte rightTrigger;         // #9
// }
//
// struct PS4OutputReport5
// {
//     byte reportId;             // #0
//     byte flags;                // #1
//     byte unknown1[2];
//     byte highFrequencyMotor;   // #4
//     byte lowFrequencyMotor;    // #5
//     byte redColor;             // #6
//     byte greenColor;           // #7
//     byte blueColor;            // #8
//     byte unknown2[23];
// }
//
// Raw HID report descriptor:
//
// Usage Page (Generic Desktop) 05 01
// Usage (Game Pad) 09 05
// Collection (Application) A1 01
//     Report ID (1) 85 01
//     Usage (X) 09 30
//     Usage (Y) 09 31
//     Usage (Z) 09 32
//     Usage (Rz) 09 35
//     Logical Minimum (0) 15 00
//     Logical Maximum (255) 26 FF 00
//     Report Size (8) 75 08
//     Report Count (4) 95 04
//     Input (Data,Var,Abs,NWrp,Lin,Pref,NNul,Bit) 81 02
//     Usage (Hat Switch) 09 39
//     Logical Minimum (0) 15 00
//     Logical Maximum (7) 25 07
//     Physical Minimum (0) 35 00
//     Physical Maximum (315) 46 3B 01
//     Unit (Eng Rot: Degree) 65 14
//     Report Size (4) 75 04
//     Report Count (1) 95 01
//     Input (Data,Var,Abs,NWrp,Lin,Pref,Null,Bit) 81 42
//     Unit (None) 65 00
//     Usage Page (Button) 05 09
//     Usage Minimum (Button 1) 19 01
//     Usage Maximum (Button 14) 29 0E
//     Logical Minimum (0) 15 00
//     Logical Maximum (1) 25 01
//     Report Size (1) 75 01
//     Report Count (14) 95 0E
//     Input (Data,Var,Abs,NWrp,Lin,Pref,NNul,Bit) 81 02
//     Usage Page (Vendor-Defined 1) 06 00 FF
//     Usage (Vendor-Defined 32) 09 20
//     Report Size (6) 75 06
//     Report Count (1) 95 01
//     Logical Minimum (0) 15 00
//     Logical Maximum (127) 25 7F
//     Input (Data,Var,Abs,NWrp,Lin,Pref,NNul,Bit) 81 02
//     Usage Page (Generic Desktop) 05 01
//     Usage (Rx) 09 33
//     Usage (Ry) 09 34
//     Logical Minimum (0) 15 00
//     Logical Maximum (255) 26 FF 00
//     Report Size (8) 75 08
//     Report Count (2) 95 02
//     Input (Data,Var,Abs,NWrp,Lin,Pref,NNul,Bit) 81 02
//     Usage Page (Vendor-Defined 1) 06 00 FF
//     Usage (Vendor-Defined 33) 09 21
//     Report Count (54) 95 36
//     Input (Data,Var,Abs,NWrp,Lin,Pref,NNul,Bit) 81 02
//     Report ID (5) 85 05
//     Usage (Vendor-Defined 34) 09 22
//     Report Count (31) 95 1F
//     Output (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) 91 02
//     Report ID (4) 85 04
//     Usage (Vendor-Defined 35) 09 23
//     Report Count (36) 95 24
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (2) 85 02
//     Usage (Vendor-Defined 36) 09 24
//     Report Count (36) 95 24
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (8) 85 08
//     Usage (Vendor-Defined 37) 09 25
//     Report Count (3) 95 03
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (16) 85 10
//     Usage (Vendor-Defined 38) 09 26
//     Report Count (4) 95 04
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (17) 85 11
//     Usage (Vendor-Defined 39) 09 27
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (18) 85 12
//     Usage Page (Vendor-Defined 3) 06 02 FF
//     Usage (Vendor-Defined 33) 09 21
//     Report Count (15) 95 0F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (19) 85 13
//     Usage (Vendor-Defined 34) 09 22
//     Report Count (22) 95 16
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (20) 85 14
//     Usage Page (Vendor-Defined 6) 06 05 FF
//     Usage (Vendor-Defined 32) 09 20
//     Report Count (16) 95 10
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (21) 85 15
//     Usage (Vendor-Defined 33) 09 21
//     Report Count (44) 95 2C
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Usage Page (Vendor-Defined 129) 06 80 FF
//     Report ID (128) 85 80
//     Usage (Vendor-Defined 32) 09 20
//     Report Count (6) 95 06
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (129) 85 81
//     Usage (Vendor-Defined 33) 09 21
//     Report Count (6) 95 06
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (130) 85 82
//     Usage (Vendor-Defined 34) 09 22
//     Report Count (5) 95 05
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (131) 85 83
//     Usage (Vendor-Defined 35) 09 23
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (132) 85 84
//     Usage (Vendor-Defined 36) 09 24
//     Report Count (4) 95 04
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (133) 85 85
//     Usage (Vendor-Defined 37) 09 25
//     Report Count (6) 95 06
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (134) 85 86
//     Usage (Vendor-Defined 38) 09 26
//     Report Count (6) 95 06
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (135) 85 87
//     Usage (Vendor-Defined 39) 09 27
//     Report Count (35) 95 23
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (136) 85 88
//     Usage (Vendor-Defined 40) 09 28
//     Report Count (34) 95 22
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (137) 85 89
//     Usage (Vendor-Defined 41) 09 29
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (144) 85 90
//     Usage (Vendor-Defined 48) 09 30
//     Report Count (5) 95 05
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (145) 85 91
//     Usage (Vendor-Defined 49) 09 31
//     Report Count (3) 95 03
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (146) 85 92
//     Usage (Vendor-Defined 50) 09 32
//     Report Count (3) 95 03
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (147) 85 93
//     Usage (Vendor-Defined 51) 09 33
//     Report Count (12) 95 0C
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (160) 85 A0
//     Usage (Vendor-Defined 64) 09 40
//     Report Count (6) 95 06
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (161) 85 A1
//     Usage (Vendor-Defined 65) 09 41
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (162) 85 A2
//     Usage (Vendor-Defined 66) 09 42
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (163) 85 A3
//     Usage (Vendor-Defined 67) 09 43
//     Report Count (48) 95 30
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (164) 85 A4
//     Usage (Vendor-Defined 68) 09 44
//     Report Count (13) 95 0D
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (165) 85 A5
//     Usage (Vendor-Defined 69) 09 45
//     Report Count (21) 95 15
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (166) 85 A6
//     Usage (Vendor-Defined 70) 09 46
//     Report Count (21) 95 15
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (240) 85 F0
//     Usage (Vendor-Defined 71) 09 47
//     Report Count (63) 95 3F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (241) 85 F1
//     Usage (Vendor-Defined 72) 09 48
//     Report Count (63) 95 3F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (242) 85 F2
//     Usage (Vendor-Defined 73) 09 49
//     Report Count (15) 95 0F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (167) 85 A7
//     Usage (Vendor-Defined 74) 09 4A
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (168) 85 A8
//     Usage (Vendor-Defined 75) 09 4B
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (169) 85 A9
//     Usage (Vendor-Defined 76) 09 4C
//     Report Count (8) 95 08
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (170) 85 AA
//     Usage (Vendor-Defined 78) 09 4E
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (171) 85 AB
//     Usage (Vendor-Defined 79) 09 4F
//     Report Count (57) 95 39
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (172) 85 AC
//     Usage (Vendor-Defined 80) 09 50
//     Report Count (57) 95 39
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (173) 85 AD
//     Usage (Vendor-Defined 81) 09 51
//     Report Count (11) 95 0B
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (174) 85 AE
//     Usage (Vendor-Defined 82) 09 52
//     Report Count (1) 95 01
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (175) 85 AF
//     Usage (Vendor-Defined 83) 09 53
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (176) 85 B0
//     Usage (Vendor-Defined 84) 09 54
//     Report Count (63) 95 3F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (177) 85 B1
//     Usage (Vendor-Defined 85) 09 55
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (178) 85 B2
//     Usage (Vendor-Defined 86) 09 56
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (224) 85 E0
//     Usage (Vendor-Defined 87) 09 57
//     Report Count (2) 95 02
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (179) 85 B3
//     Usage (Vendor-Defined 85) 09 55
//     Report Count (63) 95 3F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
//     Report ID (180) 85 B4
//     Usage (Vendor-Defined 85) 09 55
//     Report Count (63) 95 3F
//     Feature (Data,Var,Abs,NWrp,Lin,Pref,NNul,NVol,Bit) B1 02
// End Collection C0

#endif // UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
