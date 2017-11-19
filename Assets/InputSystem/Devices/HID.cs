#if UNITY_STANDALONE || UNITY_EDITOR // ATM we only use HID on desktop.
using System;

namespace ISX
{
    // A generic HID device.
    //
    // This class represents a best effort to mirror the control setup of a HID
    // discovered in the system. It is used only as a fallback where we cannot
    // match the device to a specific product we know of. Wherever possible we
    // construct more specific device representations such as Gamepad.
    public class HID : InputDevice
    {
        public const string kHIDInterface = "HID";

        // The HID device descriptor as received from the device driver.
        public HIDDeviceDescriptor hidDescriptor => new HIDDeviceDescriptor();

        internal static void InitializeHIDSupport(InputManager manager)
        {
            throw new NotImplementedException();
        }

        ////REVIEW: ideally this would work with just InputControlSetup and not just when doing AddDevice in InputManager
        ////        In fact, I think the whole extension point here is wrong. This should be about template
        ////        extensibility!
        ////        Turn "template constructor" mechanism into "template generator" mechanism?
        internal static string OnDeviceDiscovered(InputDeviceDescription description, string matchedTemplate)
        {
            // If the system found a matching template, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedTemplate))
                return null;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != kHIDInterface)
                return null;

            return null;
        }

        // NOTE: Must match HIDReportType in native.
        public enum HIDReportType
        {
            Input,
            Output,
            Feature
        }

        // NOTE: Must match up with the serialization represention of HIDInputElementDescriptor in native.
        [Serializable]
        public struct HIDElementDescriptor
        {
            public string name;
            public int usageId;
            public int usagePageId;
            public int unit;
            public int unitExponent;
            public int logicalMin;
            public int logicalMax;
            public int physicalMin;
            public int physicalMax;
            public HIDReportType reportType;
            public int reportId;
            public int reportCount;
            public int reportSizeInBits;
            public bool hasNullState;
            public bool hasPreferredState;
            public bool isArray;
            public bool isNonLinear;
            public bool isRelative;
            public bool isVirtual;
            public bool isWrapping;
        }

        // NOTE: Must match up with the serialized representation of HIDInputDeviceDescriptor in native.
        [Serializable]
        public struct HIDDeviceDescriptor
        {
            public int vendorId;
            public int productId;
            public int usageId;
            public int usagePageId;
            public HIDElementDescriptor[] elements;
        }

        public enum UsagePage
        {
            GenericDesktop = 0x01,
            Simulation = 0x02,
            VRControls = 0x03,
            SportControls = 0x04,
            GameControls = 0x05,
            GenericDeviceControls = 0x06,
            Keyboard = 0x07,
            LEDs = 0x08,
            Button = 0x09,
            Ordinal = 0x0A,
            Telephony = 0x0B,
            Consumer = 0x0C,
            Digitizer = 0x0D,
            PID = 0x0F,
            Unicode = 0x10,
            AlphanumericDisplay = 0x14,
            MedicalInstruments = 0x40,
        }

        // See http://www.usb.org/developers/hidpage/Hut1_12v2.pdf.
        public enum GenericDesktop
        {
            Pointer = 0x01,
            Mouse = 0x02,
            Joystick = 0x04,
            Gamepad = 0x05,
            Keyboard = 0x06,
            Keypad = 0x07,
            MultiAxisController = 0x08,
            TabletPCControls = 0x09,
            X = 0x30,
            Y = 0x31,
            Z = 0x32,
            Rx = 0x33,
            Ry = 0x34,
            Rz = 0x35,
            Slider = 0x36,
            Dial = 0x37,
            Wheel = 0x38,
            HatSwitch = 0x39,
            CountedBuffer = 0x3A,
            ByteCount = 0x3B,
            MotionWakeup = 0x3C,
            Start = 0x3D,
            Selectd = 0x3E,
            Vx = 0x40,
            Vy = 0x41,
            Vz = 0x42,
            Vbrx = 0x43,
            Vbry = 0x44,
            Vbrz = 0x45,
            Vno = 0x46,
            FeatureNotification = 0x47,
            ResolutionMultiplier = 0x48,
            SystemControl = 0x80,
            SystemPowerDown = 0x81,
            SystemSleep = 0x82,
            SystemWakeUp = 0x83,
            SystemContextMenu = 0x84,
            SystemMainMenu = 0x85,
            SystemAppMenu = 0x86,
            SystemMenuHelp = 0x87,
            SystemMenuExit = 0x88,
            SystemMenuSelect = 0x89,
            SystemMenuRight = 0x8A,
            SystemMenuLeft = 0x8B,
            SystemMenuUp = 0x8C,
            SystemMenuDown = 0x8D,
            SystemColdRestart = 0x8E,
            SystemWarmRestart = 0x8F,
            DpadUp = 0x90,
            DpadDown = 0x91,
            DpadRight = 0x92,
            DpadLeft = 0x93,
            SystemDock = 0xA0,
            SystemUndock = 0xA1,
            SystemSetup = 0xA2,
            SystemBreak = 0xA3,
            SystemDebuggerBreak = 0xA4,
            ApplicationBreak = 0xA5,
            ApplicationDebuggerBreak = 0xA6,
            SystemSpeakerMute = 0xA7,
            SystemHibernate = 0xA8,
            SystemDisplayInvert = 0xB0,
            SystemDisplayInternal = 0xB1,
            SystemDisplayExternal = 0xB2,
            SystemDisplayBoth = 0xB3,
            SystemDisplayDual = 0xB4,
            SystemDisplayToggleIntExt = 0xB5,
            SystemDisplaySwapPrimarySecondary = 0xB6,
            SystemDisplayLCDAutoScale = 0xB7
        }

        public enum Simulation
        {
            FlightSimulationDevice = 0x01,
            AutomobileSimulationDevice = 0x02,
            TankSimulationDevice = 0x03,
            SpaceshipSimulationDevice = 0x04,
            SubmarineSimulationDevice = 0x05,
            SailingSimulationDevice = 0x06,
            MotorcycleSimulationDevice = 0x07,
            SportsSimulationDevice = 0x08,
            AirplaneSimulationDevice = 0x09,
            HelicopterSimulationDevice = 0x0A,
            MagicCarpetSimulationDevice = 0x0B,
            BicylcleSimulationDevice = 0x0C,
            FlightControlStick = 0x20,
            FlightStick = 0x21,
            CyclicControl = 0x22,
            CyclicTrim = 0x23,
            FlightYoke = 0x24,
            TrackControl = 0x25,
            Aileron = 0xB0,
            AileronTrim = 0xB1,
            AntiTorqueControl = 0xB2,
            AutopilotEnable = 0xB3,
            ChaffRelease = 0xB4,
            CollectiveControl = 0xB5,
            DiveBreak = 0xB6,
            ElectronicCountermeasures = 0xB7,
            Elevator = 0xB8,
            ElevatorTrim = 0xB9,
            Rudder = 0xBA,
            Throttle = 0xBB,
            FlightCommunications = 0xBC,
            FlareRelease = 0xBD,
            LandingGear = 0xBE,
            ToeBreak = 0xBF,
            Trigger = 0xC0,
            WeaponsArm = 0xC1,
            WeaponsSelect = 0xC2,
            WingFlags = 0xC3,
            Accelerator = 0xC4,
            Brake = 0xC5,
            Clutch = 0xC6,
            Shifter = 0xC7,
            Steering = 0xC8,
            TurretDirection = 0xC9,
            BarrelElevation = 0xCA,
            DivePlane = 0xCB,
            Ballast = 0xCC,
            BicycleCrank = 0xCD,
            HandleBars = 0xCE,
            FrontBrake = 0xCF,
            RearBrake = 0xD0
        }

        public enum Button
        {
            Primary = 1,
            Secondary,
            Tertiary
        }
    }
}

#endif // UNITY_STANDALONE || UNITY_EDITOR
