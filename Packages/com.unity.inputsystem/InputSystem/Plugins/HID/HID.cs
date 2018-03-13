using System;
using ISX.Editor;
using ISX.LowLevel;
using ISX.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using ISX.Plugins.HID.Editor;
#endif

////REVIEW: how are we dealing with multiple different input reports on the same device?

////REVIEW: move the enums and structs out of here and into ISX.HID? Or remove the "HID" name prefixes from them?

////TODO: add blacklist for devices we really don't want to use (like apple's internal trackpad)

namespace ISX.Plugins.HID
{
    /// <summary>
    /// A generic HID input device.
    /// </summary>
    /// <remarks>
    /// This class represents a best effort to mirror the control setup of a HID
    /// discovered in the system. It is used only as a fallback where we cannot
    /// match the device to a specific product we know of. Wherever possible we
    /// construct more specific device representations such as Gamepad.
    /// </remarks>
    public class HID : InputDevice
#if UNITY_EDITOR
        , IInputDeviceDebugUI
#endif
    {
        public const string kHIDInterface = "HID";
        public const string kHIDNamespace = "HID";

        /// <summary>
        /// Command code for querying the HID report descriptor from a device.
        /// </summary>
        /// <seealso cref="InputDevice.OnDeviceCommand{TCommand}"/>
        public static FourCC QueryHIDReportDescriptorDeviceCommandType { get { return new FourCC('H', 'I', 'D', 'D'); } }

        /// <summary>
        /// Command code for querying the HID report descriptor size in bytes from a device.
        /// </summary>
        /// <seealso cref="InputDevice.OnDeviceCommand{TCommand}"/>
        public static FourCC QueryHIDReportDescriptorSizeDeviceCommandType { get { return new FourCC('H', 'I', 'D', 'S'); } }

        /// <summary>
        /// The HID device descriptor as received from the system.
        /// </summary>
        public HIDDeviceDescriptor hidDescriptor
        {
            get
            {
                if (!m_HaveParsedHIDDescriptor)
                {
                    if (!string.IsNullOrEmpty(description.capabilities))
                        m_HIDDescriptor = JsonUtility.FromJson<HIDDeviceDescriptor>(description.capabilities);
                    m_HaveParsedHIDDescriptor = true;
                }
                return m_HIDDescriptor;
            }
        }

        #if UNITY_EDITOR
        public void OnToolbarGUI()
        {
            if (GUILayout.Button(s_HIDDescriptor, EditorStyles.toolbarButton))
            {
                HIDDescriptorWindow.CreateOrShowExisting(this);
            }
        }

        private static GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");
        #endif

        private bool m_HaveParsedHIDDescriptor;
        private HIDDeviceDescriptor m_HIDDescriptor;

        // This is the workhorse for figuring out fallback options for HIDs attached to the system.
        // If the system cannot find a more specific template for a given HID, this method will try
        // to produce a template constructor on the fly based on the HID descriptor received from
        // the device.
        internal static unsafe string OnFindTemplateForDevice(int deviceId, ref InputDeviceDescription description, string matchedTemplate, IInputRuntime runtime)
        {
            // If the system found a matching template, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedTemplate))
                return null;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != kHIDInterface)
                return null;

            // We require *some* product name to be supplied.
            if (string.IsNullOrEmpty(description.product))
                return null;

            // If the description doesn't come with a HIDDeviceDescriptor in its capabilities field,
            // we're not interested either.
            if (string.IsNullOrEmpty(description.capabilities))
                return null;

            // Try to parse the HIDDeviceDescriptor.
            HIDDeviceDescriptor hidDeviceDescriptor;
            try
            {
                hidDeviceDescriptor = HIDDeviceDescriptor.FromJson(description.capabilities);
            }
            catch (Exception exception)
            {
                Debug.Log(string.Format("Could not parse HID descriptor (exception: {0})", exception));
                return null;
            }

            // If the descriptor has no elements associated with it, try to get a report descriptor
            // directly from the device.
            if (hidDeviceDescriptor.elements == null || hidDeviceDescriptor.elements.Length == 0)
            {
                // If the device has no assigned ID yet, we're not interested either.
                // This isn't a device coming from the runtime.
                if (deviceId == InputDevice.kInvalidDeviceId)
                    return null;

                // Try to get the size of the HID descriptor from the device.
                var sizeOfDescriptorCommand = new InputDeviceCommand(QueryHIDReportDescriptorSizeDeviceCommandType);
                var sizeOfDescriptorInBytes = runtime.DeviceCommand(deviceId, ref sizeOfDescriptorCommand);
                if (sizeOfDescriptorInBytes <= 0)
                    return null;

                // Now try to fetch the HID descriptor.
                using (var buffer =
                           InputDeviceCommand.AllocateNative(QueryHIDReportDescriptorDeviceCommandType, (int)sizeOfDescriptorInBytes))
                {
                    var commandPtr = (InputDeviceCommand*)NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
                    if (runtime.DeviceCommand(deviceId, ref *commandPtr) != sizeOfDescriptorInBytes)
                        return null;

                    // Try to parse the HID report descriptor.
                    if (!HIDParser.ParseReportDescriptor((byte*)commandPtr->payloadPtr, (int)sizeOfDescriptorInBytes, ref hidDeviceDescriptor))
                        return null;
                }

                // Update the descriptor on the device with the information we got.
                description.capabilities = hidDeviceDescriptor.ToJson();
            }

            // Determine if there's any usable elements on the device.
            var hasUsableElements = false;
            if (hidDeviceDescriptor.elements != null)
            {
                foreach (var element in hidDeviceDescriptor.elements)
                {
                    if (element.DetermineTemplate() != null)
                    {
                        hasUsableElements = true;
                        break;
                    }
                }
            }

            if (!hasUsableElements)
                return null;

            // Determine base template.
            var baseTemplate = "HID";
            if (hidDeviceDescriptor.usagePage == UsagePage.GenericDesktop)
            {
                /*
                ////TODO: there's some work to be done to make the HID *actually* compatible with these devices
                if (hidDeviceDescriptor.usage == (int)GenericDesktop.Joystick)
                    baseTemplate = "Joystick";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Gamepad)
                    baseTemplate = "Gamepad";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Mouse)
                    baseTemplate = "Mouse";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Pointer)
                    baseTemplate = "Pointer";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Keyboard)
                    baseTemplate = "Keyboard";
                */
            }

            // We don't want the capabilities field in the description to be matched
            // when the input system is looking for matching templates so null it out.
            var deviceDescriptionForTemplate = description;
            deviceDescriptionForTemplate.capabilities = null;

            ////TODO: make sure we don't produce name conflicts on the template name

            // Register template constructor that will turn the HID descriptor into an
            // InputTemplate instance.
            var templateName = string.Format("{0}::{1}", kHIDNamespace, description.product);
            var template = new HIDTemplate {hidDescriptor = hidDeviceDescriptor, deviceDescription = deviceDescriptionForTemplate};
            InputSystem.RegisterTemplateConstructor(() => template.Build(), templateName, baseTemplate, deviceDescriptionForTemplate);

            return templateName;
        }

        public static bool UsageToString(UsagePage usagePage, int usage, out string usagePageString, out string usageString)
        {
            const string kVendorDefined = "Vendor-Defined";

            if ((int)usagePage >= 0xFF00)
            {
                usagePageString = kVendorDefined;
                usageString = kVendorDefined;
                return true;
            }

            usagePageString = usagePage.ToString();
            usageString = null;

            switch (usagePage)
            {
                case UsagePage.GenericDesktop:
                    usageString = ((GenericDesktop)usage).ToString();
                    break;
                case UsagePage.Simulation:
                    usageString = ((Simulation)usage).ToString();
                    break;
                default:
                    return false;
            }

            return true;
        }

        [Serializable]
        private class HIDTemplate
        {
            public HIDDeviceDescriptor hidDescriptor;
            public InputDeviceDescription deviceDescription;

            public InputTemplate Build()
            {
                var builder = new InputTemplate.Builder
                {
                    type = typeof(HID),
                    stateFormat = new FourCC('H', 'I', 'D'),
                    deviceDescription = deviceDescription
                };

                ////TODO: for joysticks, set up stick from X and Y

                // Process HID descriptor.
                foreach (var element in hidDescriptor.elements)
                {
                    if (element.reportType != HIDReportType.Input)
                        continue;

                    var template = element.DetermineTemplate();
                    if (template != null)
                    {
                        var control =
                            builder.AddControl(element.DetermineName())
                            .WithTemplate(template)
                            .WithOffset((uint)element.reportBitOffset / 8)
                            .WithBit((uint)element.reportBitOffset % 8)
                            .WithFormat(element.DetermineFormat());

                        ////TODO: configure axis parameters from min/max limits

                        element.SetUsage(control);
                    }
                }

                return builder.Build();
            }
        }

        public enum HIDReportType
        {
            Unknown,
            Input,
            Output,
            Feature
        }

        public enum HIDCollectionType
        {
            Physical = 0x00,
            Application = 0x01,
            Logical = 0x02,
            Report = 0x03,
            NamedArray = 0x04,
            UsageSwitch = 0x05,
            UsageModifier = 0x06
        }

        [Flags]
        public enum HIDElementFlags
        {
            Constant = 1 << 0,
            Variable = 1 << 1,
            Relative = 1 << 2,
            Wrap = 1 << 3,
            NonLinear = 1 << 4,
            NoPreferred = 1 << 5,
            NullState = 1 << 6,
            Volatile = 1 << 7,
            BufferedBytes = 1 << 8
        }

        /// <summary>
        /// Descriptor for a single report element.
        /// </summary>
        [Serializable]
        public struct HIDElementDescriptor
        {
            public int usage;
            public UsagePage usagePage;
            public int unit;
            public int unitExponent;
            public int logicalMin;
            public int logicalMax;
            public int physicalMin;
            public int physicalMax;
            public HIDReportType reportType;
            public int collectionIndex;
            public int reportId;
            public int reportSizeInBits;
            public int reportBitOffset;
            public HIDElementFlags flags;

            // Fields only relevant to arrays.
            public int? usageMin;
            public int? usageMax;

            public bool hasNullState
            {
                get { return (flags & HIDElementFlags.NullState) == HIDElementFlags.NullState; }
            }

            public bool hasPreferredState
            {
                get { return (flags & HIDElementFlags.NoPreferred) != HIDElementFlags.NoPreferred; }
            }

            public bool isArray
            {
                get { return (flags & HIDElementFlags.Variable) != HIDElementFlags.Variable; }
            }

            public bool isNonLinear
            {
                get { return (flags & HIDElementFlags.NonLinear) == HIDElementFlags.NonLinear; }
            }

            public bool isRelative
            {
                get { return (flags & HIDElementFlags.Relative) == HIDElementFlags.Relative; }
            }

            public bool isConstant
            {
                get { return (flags & HIDElementFlags.Constant) == HIDElementFlags.Constant; }
            }

            public bool isWrapping
            {
                get { return (flags & HIDElementFlags.Wrap) == HIDElementFlags.Wrap; }
            }

            internal string DetermineName()
            {
                // It's rare for HIDs to declare string names for items and HID drivers may report weird strings
                // plus there's no guarantee that these names are unique per item. So, we don't bother here with
                // device/driver-supplied names at all but rather do our own naming.

                switch (usagePage)
                {
                    case UsagePage.Button:
                        return string.Format("button{0}", usage);
                    case UsagePage.GenericDesktop:
                        return ((GenericDesktop)usage).ToString();
                }

                return string.Format("UsagePage({0:X}) Usage({1:X})", usagePage, usage);
            }

            internal string DetermineTemplate()
            {
                ////TODO: support output elements
                if (reportType != HIDReportType.Input)
                    return null;

                ////TODO: deal with arrays

                switch (usagePage)
                {
                    case UsagePage.Button:
                        return "Button";
                    case UsagePage.GenericDesktop:
                        switch (usage)
                        {
                            case (int)GenericDesktop.X:
                            case (int)GenericDesktop.Y:
                            case (int)GenericDesktop.Z:
                            case (int)GenericDesktop.Rx:
                            case (int)GenericDesktop.Ry:
                            case (int)GenericDesktop.Rz:
                            case (int)GenericDesktop.Vx:
                            case (int)GenericDesktop.Vy:
                            case (int)GenericDesktop.Vz:
                            case (int)GenericDesktop.Vbrx:
                            case (int)GenericDesktop.Vbry:
                            case (int)GenericDesktop.Vbrz:
                            case (int)GenericDesktop.Slider:
                            case (int)GenericDesktop.Dial:
                            case (int)GenericDesktop.Wheel:
                                return "Axis";

                            case (int)GenericDesktop.Select:
                            case (int)GenericDesktop.Start:
                            case (int)GenericDesktop.DpadUp:
                            case (int)GenericDesktop.DpadDown:
                            case (int)GenericDesktop.DpadLeft:
                            case (int)GenericDesktop.DpadRight:
                                return "Button";
                        }
                        break;
                }

                return null;
            }

            internal FourCC DetermineFormat()
            {
                ////TODO: do this properly instead of this nonsense; we need to look at the format that the usage+usagePage actually stands for
                switch (reportSizeInBits)
                {
                    case 1: return InputStateBlock.kTypeBit;
                    case 8: return InputStateBlock.kTypeByte;
                    case 16: return InputStateBlock.kTypeShort;
                    case 32: return InputStateBlock.kTypeInt;
                }

                return new FourCC();
            }

            internal void SetUsage(InputTemplate.Builder.ControlBuilder control)
            {
                if (usagePage == UsagePage.Button && usage == 0)
                    control.WithUsages(new[] {CommonUsages.PrimaryTrigger, CommonUsages.PrimaryAction});
                if (usagePage == UsagePage.Button && usage == 1)
                    control.WithUsages(new[] {CommonUsages.SecondaryTrigger, CommonUsages.SecondaryAction});
            }
        }

        /// <summary>
        /// Descriptor for a collection of HID elements.
        /// </summary>
        [Serializable]
        public struct HIDCollectionDescriptor
        {
            public HIDCollectionType type;
            public int usage;
            public UsagePage usagePage;
            public int parent; // -1 if no parent.
            public int childCount;
            public int firstChild;
        }

        /// <summary>
        /// HID descriptor for a HID class device.
        /// </summary>
        /// <remarks>
        /// This is a processed view of the combined descriptors provided by a HID as defined
        /// in the HID specification, i.e. it's a combination of information from the USB device
        /// descriptor, HID class descriptor, and HID report descriptor.
        /// </remarks>
        [Serializable]
        public struct HIDDeviceDescriptor
        {
            /// <summary>
            /// USB vendor ID.
            /// </summary>
            /// <remarks>
            /// To get the string version of the vendor ID, see <see cref="InputDeviceDescription.manufacturer"/>
            /// on <see cref="InputDevice.description"/>.
            /// </remarks>
            public int vendorId;

            /// <summary>
            /// USB product ID.
            /// </summary>
            public int productId;
            public int usage;
            public UsagePage usagePage;

            /// <summary>
            /// Maximum size of individual input reports sent by the device.
            /// </summary>
            public int inputReportSize;

            /// <summary>
            /// Maximum size of individual output reports sent to the device.
            /// </summary>
            public int outputReportSize;

            /// <summary>
            /// Maximum size of individual feature reports exchanged with the device.
            /// </summary>
            public int featureReportSize;

            public HIDElementDescriptor[] elements;
            public HIDCollectionDescriptor[] collections;

            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }

            public static HIDDeviceDescriptor FromJson(string json)
            {
                return JsonUtility.FromJson<HIDDeviceDescriptor>(json);
            }
        }

        /// <summary>
        /// Enumeration of HID usage pages.
        /// </summary>00
        /// <remarks>
        /// Note that some of the values are actually ranges.
        /// </remarks>
        /// <seealso cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
        public enum UsagePage
        {
            Undefined = 0x00,
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
            Monitor = 0x80, // Starts here and goes up to 0x83.
            Power = 0x84, // Starts here and goes up to 0x87.
            BarCodeScanner = 0x8C,
            MagneticStripeReader = 0x8E,
            Camera = 0x90,
            Arcade = 0x91,
            VendorDefined = 0xFF00, // Starts here and goes up to 0xFFFF.
        }

        /// <summary>
        /// Usages in the GenericDesktop HID usage page.
        /// </summary>
        /// <seealso cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
        public enum GenericDesktop
        {
            Undefined = 0x00,
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
            Select = 0x3E,
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
            Undefined = 0x00,
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
            Undefined = 0,
            Primary,
            Secondary,
            Tertiary
        }
    }
}
