using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.Plugins.HID.Editor;
#endif

////REVIEW: there will probably be lots of cases where the HID device creation process just needs a little tweaking; we should
////        have better mechanism to do that without requiring to replace the entire process wholesale

////TODO: expose the layout builder so that other layout builders can use it for their own purposes

////REVIEW: how are we dealing with multiple different input reports on the same device?

////REVIEW: move the enums and structs out of here and into UnityEngine.Experimental.Input.HID? Or remove the "HID" name prefixes from them?

////TODO: add blacklist for devices we really don't want to use (like apple's internal trackpad)

////TODO: add a way to mark certain layouts (such as HID layouts) as fallbacks; ideally, affect the layout matching score

namespace UnityEngine.Experimental.Input.Plugins.HID
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
    {
        public const string kHIDInterface = "HID";
        public const string kHIDNamespace = "HID";

        /// <summary>
        /// Command code for querying the HID report descriptor from a device.
        /// </summary>
        /// <seealso cref="InputDevice.ExecuteCommand{TCommand}"/>
        public static FourCC QueryHIDReportDescriptorDeviceCommandType { get { return new FourCC('H', 'I', 'D', 'D'); } }

        /// <summary>
        /// Command code for querying the HID report descriptor size in bytes from a device.
        /// </summary>
        /// <seealso cref="InputDevice.ExecuteCommand{TCommand}"/>
        public static FourCC QueryHIDReportDescriptorSizeDeviceCommandType { get { return new FourCC('H', 'I', 'D', 'S'); } }

        public static FourCC QueryHIDParsedReportDescriptorDeviceCommandType { get { return new FourCC('H', 'I', 'D', 'P'); } }

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

        private bool m_HaveParsedHIDDescriptor;
        private HIDDeviceDescriptor m_HIDDescriptor;

        // This is the workhorse for figuring out fallback options for HIDs attached to the system.
        // If the system cannot find a more specific layout for a given HID, this method will try
        // to produce a layout builder on the fly based on the HID descriptor received from
        // the device.
        internal static string OnFindLayoutForDevice(int deviceId, ref InputDeviceDescription description, string matchedLayout, IInputRuntime runtime)
        {
            // If the system found a matching layout, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedLayout))
                return null;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != kHIDInterface)
                return null;

            // Read HID descriptor.
            var hidDeviceDescriptor = ReadHIDDeviceDescriptor(deviceId, ref description, runtime);

            // Determine if there's any usable elements on the device.
            var hasUsableElements = false;
            if (hidDeviceDescriptor.elements != null)
            {
                foreach (var element in hidDeviceDescriptor.elements)
                {
                    if (element.DetermineLayout() != null)
                    {
                        hasUsableElements = true;
                        break;
                    }
                }
            }

            // If not, there's nothing we can do with the device.
            if (!hasUsableElements)
                return null;

            // Determine base layout.
            var baseLayout = "HID";
            if (hidDeviceDescriptor.usagePage == UsagePage.GenericDesktop)
            {
                /*
                ////TODO: there's some work to be done to make the HID *actually* compatible with these devices
                if (hidDeviceDescriptor.usage == (int)GenericDesktop.Joystick)
                    baseLayout = "Joystick";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Gamepad)
                    baseLayout = "Gamepad";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Mouse)
                    baseLayout = "Mouse";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Pointer)
                    baseLayout = "Pointer";
                else if (hidDeviceDescriptor.usage == (int)GenericDesktop.Keyboard)
                    baseLayout = "Keyboard";
                */
            }

            ////TODO: match HID layouts by vendor and product ID
            ////REVIEW: this probably works fine for most products out there but I'm not sure it works reliably for all cases
            // Come up with a unique template name. HIDs are required to have product and vendor IDs.
            // We go with the string versions if we have them and with the numeric versions if we don't.
            string layoutName;
            var deviceMatcher = InputDeviceMatcher.FromDeviceDescription(description);
            if (!string.IsNullOrEmpty(description.product) && !string.IsNullOrEmpty(description.manufacturer))
            {
                layoutName = string.Format("{0}::{1} {2}", kHIDNamespace, description.manufacturer, description.product);
            }
            else
            {
                // Sanity check to make sure we really have the data we expect.
                if (hidDeviceDescriptor.vendorId == 0)
                    return null;
                layoutName = string.Format("{0}::{1:X}-{2:X}", kHIDNamespace, hidDeviceDescriptor.vendorId,
                    hidDeviceDescriptor.productId);

                deviceMatcher.WithCapability("productId", hidDeviceDescriptor.productId);
                deviceMatcher.WithCapability("vendorId", hidDeviceDescriptor.vendorId);
            }

            // Register layout builder that will turn the HID descriptor into an
            // InputControlLayout instance.
            var layout = new HIDLayoutBuilder {hidDescriptor = hidDeviceDescriptor};
            InputSystem.RegisterLayoutBuilder(() => layout.Build(),
                layoutName, baseLayout, deviceMatcher);

            return layoutName;
        }

        public static HIDDeviceDescriptor ReadHIDDeviceDescriptor(InputDevice device, IInputRuntime runtime)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            InputDeviceDescription deviceDescription = device.description;
            if (deviceDescription.interfaceName != kHIDInterface)
                throw new ArgumentException(
                    string.Format("Device '{0}' is not a HID (interface is '{1}')", device,
                        deviceDescription.interfaceName), "device");

            return ReadHIDDeviceDescriptor(device.id, ref deviceDescription, runtime);
        }

        public static unsafe HIDDeviceDescriptor ReadHIDDeviceDescriptor(int deviceId, ref InputDeviceDescription deviceDescription, IInputRuntime runtime)
        {
            if (deviceDescription.interfaceName != kHIDInterface)
                throw new ArgumentException(
                    string.Format("Device '{0}' is not a HID", deviceDescription));

            // See if we have to request a HID descriptor from the device.
            // We support having the descriptor directly as a JSON string in the `capabilities`
            // field of the device description.
            var needToRequestDescriptor = true;
            var hidDeviceDescriptor = new HIDDeviceDescriptor();
            if (!string.IsNullOrEmpty(deviceDescription.capabilities))
            {
                try
                {
                    hidDeviceDescriptor = HIDDeviceDescriptor.FromJson(deviceDescription.capabilities);

                    // If there's elements in the descriptor, we're good with the descriptor. If there aren't,
                    // we go and ask the device for a full descriptor.
                    if (hidDeviceDescriptor.elements != null && hidDeviceDescriptor.elements.Length > 0)
                        needToRequestDescriptor = false;
                }
                catch (Exception exception)
                {
                    Debug.LogError(string.Format("Could not parse HID descriptor of device '{0}'", deviceDescription));
                    Debug.LogException(exception);
                }
            }

            ////REVIEW: we *could* switch to a single path here that supports *only* parsed descriptors but it'd
            ////        mean having to switch *every* platform supporting HID to the hack we currently have to do
            ////        on Windows

            // Request descriptor, if necessary.
            if (needToRequestDescriptor)
            {
                // If the device has no assigned ID yet, we can't perform IOCTLs on the
                // device so no way to get a report descriptor.
                if (deviceId == kInvalidDeviceId)
                    return new HIDDeviceDescriptor();

                // Try to get the size of the HID descriptor from the device.
                var sizeOfDescriptorCommand = new InputDeviceCommand(QueryHIDReportDescriptorSizeDeviceCommandType);
                var sizeOfDescriptorInBytes = runtime.DeviceCommand(deviceId, ref sizeOfDescriptorCommand);
                if (sizeOfDescriptorInBytes > 0)
                {
                    // Now try to fetch the HID descriptor.
                    using (var buffer =
                               InputDeviceCommand.AllocateNative(QueryHIDReportDescriptorDeviceCommandType, (int)sizeOfDescriptorInBytes))
                    {
                        var commandPtr = (InputDeviceCommand*)NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
                        if (runtime.DeviceCommand(deviceId, ref *commandPtr) != sizeOfDescriptorInBytes)
                            return new HIDDeviceDescriptor();

                        // Try to parse the HID report descriptor.
                        if (!HIDParser.ParseReportDescriptor((byte*)commandPtr->payloadPtr, (int)sizeOfDescriptorInBytes, ref hidDeviceDescriptor))
                            return new HIDDeviceDescriptor();
                    }

                    // Update the descriptor on the device with the information we got.
                    deviceDescription.capabilities = hidDeviceDescriptor.ToJson();
                }
                else
                {
                    // The device may not support binary descriptors but may support parsed descriptors so
                    // try the IOCTL for parsed descriptors next.
                    //
                    // This path exists pretty much only for the sake of Windows where it is not possible to get
                    // unparsed/binary descriptors from the device (and where getting element offsets is only possible
                    // with some dirty hacks we're performing in the native runtime).

                    const int kMaxDescriptorBufferSize = 2 * 1024 * 1024; ////TODO: switch to larger buffer based on return code if request fails
                    using (var buffer =
                               InputDeviceCommand.AllocateNative(QueryHIDParsedReportDescriptorDeviceCommandType, kMaxDescriptorBufferSize))
                    {
                        var commandPtr = (InputDeviceCommand*)NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
                        var utf8Length = runtime.DeviceCommand(deviceId, ref *commandPtr);
                        if (utf8Length < 0)
                            return new HIDDeviceDescriptor();

                        // Turn UTF-8 buffer into string.
                        ////TODO: is there a way to not have to copy here?
                        var utf8 = new byte[utf8Length];
                        fixed(byte* utf8Ptr = utf8)
                        {
                            UnsafeUtility.MemCpy(utf8Ptr, commandPtr->payloadPtr, utf8Length);
                        }
                        var descriptorJson = Encoding.UTF8.GetString(utf8, 0, (int)utf8Length);

                        // Try to parse the HID report descriptor.
                        try
                        {
                            hidDeviceDescriptor = HIDDeviceDescriptor.FromJson(descriptorJson);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError(string.Format("Could not parse HID descriptor of device '{0}'", deviceDescription));
                            Debug.LogException(exception);
                            return new HIDDeviceDescriptor();
                        }

                        // Update the descriptor on the device with the information we got.
                        deviceDescription.capabilities = descriptorJson;
                    }
                }
            }

            return hidDeviceDescriptor;
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
        private class HIDLayoutBuilder
        {
            public HIDDeviceDescriptor hidDescriptor;

            public InputControlLayout Build()
            {
                var builder = new InputControlLayout.Builder
                {
                    type = typeof(HID),
                    stateFormat = new FourCC('H', 'I', 'D'),
                };

                ////TODO: for joysticks, set up stick from X and Y

                // Process HID descriptor.
                foreach (var element in hidDescriptor.elements)
                {
                    if (element.reportType != HIDReportType.Input)
                        continue;

                    var layout = element.DetermineLayout();
                    if (layout != null)
                    {
                        // Assign unique name.
                        var name = element.DetermineName();
                        Debug.Assert(!string.IsNullOrEmpty(name));
                        name = StringHelpers.MakeUniqueName(name, builder.controls, x => x.name);

                        // Add control.
                        var control =
                            builder.AddControl(name)
                                .WithLayout(layout)
                                .WithByteOffset((uint)element.reportOffsetInBits / 8)
                                .WithBitOffset((uint)element.reportOffsetInBits % 8)
                                .WithSizeInBits((uint)element.reportSizeInBits)
                                .WithFormat(element.DetermineFormat())
                                .WithDefaultState(element.DetermineDefaultState());

                        var parameters = element.DetermineParameters();
                        if (!string.IsNullOrEmpty(parameters))
                            control.WithParameters(parameters);

                        var usages = element.DetermineUsages();
                        if (usages != null)
                            control.WithUsages(usages);

                        element.AddChildControls(name, ref builder);
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
            public int reportOffsetInBits;
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

            public float resolution
            {
                get
                {
                    var min = physicalMin;
                    var max = physicalMax;

                    if (min == 0.0f && max == 0.0f)
                    {
                        min = logicalMin;
                        max = logicalMax;
                    }

                    return (logicalMax - logicalMin) / ((max - min) * Mathf.Pow(10, unitExponent));
                }
            }

            internal bool isSigned
            {
                get { return logicalMin < 0; }
            }

            internal float minFloat
            {
                get
                {
                    switch (reportSizeInBits)
                    {
                        case 8:
                            if (isSigned)
                                return (sbyte)logicalMin / 128.0f;
                            return (byte)logicalMin / 255.0f;

                        case 16:
                            if (isSigned)
                                return (short)logicalMin / 32768.0f;
                            return (ushort)logicalMin / 65536.0f;
                    }

                    return 0.0f;
                }
            }

            internal float maxFloat
            {
                get
                {
                    switch (reportSizeInBits)
                    {
                        case 8:
                            if (isSigned)
                                return (sbyte)logicalMax / 128.0f;
                            return (byte)logicalMax / 255.0f;

                        case 16:
                            if (isSigned)
                                return (short)logicalMax / 32768.0f;
                            return (ushort)logicalMax / 65536.0f;
                    }

                    return 1.0f;
                }
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
                        if (usage == (int)GenericDesktop.HatSwitch)
                            return "dpad";
                        var text = ((GenericDesktop)usage).ToString();
                        // Lower-case first letter.
                        text = char.ToLowerInvariant(text[0]) + text.Substring(1);
                        return text;
                }

                // Fallback that generates a somewhat useless but at least very informative name.
                return string.Format("UsagePage({0:X}) Usage({1:X})", usagePage, usage);
            }

            internal string DetermineLayout()
            {
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

                            case (int)GenericDesktop.HatSwitch:
                                // Only support hat switches with 8 directions.
                                if (logicalMax - logicalMin + 1 == 8)
                                    return "Dpad";
                                break;
                        }
                        break;
                }

                return null;
            }

            internal FourCC DetermineFormat()
            {
                switch (reportSizeInBits)
                {
                    case 8:
                        if (isSigned)
                            return InputStateBlock.kTypeSByte;
                        return InputStateBlock.kTypeByte;
                    case 16:
                        if (isSigned)
                            return InputStateBlock.kTypeShort;
                        return InputStateBlock.kTypeUShort;
                    case 32:
                        if (isSigned)
                            return InputStateBlock.kTypeInt;
                        return InputStateBlock.kTypeUInt;
                    default:
                        // Generic bitfield value.
                        return InputStateBlock.kTypeBit;
                }
            }

            internal InternedString[] DetermineUsages()
            {
                if (usagePage == UsagePage.Button && usage == 0)
                    return new[] {CommonUsages.PrimaryTrigger, CommonUsages.PrimaryAction};
                if (usagePage == UsagePage.Button && usage == 1)
                    return new[] {CommonUsages.SecondaryTrigger, CommonUsages.SecondaryAction};
                ////TODO: assign hatswitch usage to first and only to first hatswitch element
                return null;
            }

            internal string DetermineParameters()
            {
                if (usagePage == UsagePage.GenericDesktop)
                {
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
                            // If we have min/max bounds on the axis values, set up normalization on the axis.
                            // NOTE: We put the center in the middle between min/max as we can't know where the
                            //       resting point of the axis is (may be on min if it's a trigger, for example).
                            if (logicalMin == 0 && logicalMax == 0)
                                return null;
                            var min = minFloat;
                            var max = maxFloat;
                            // Do nothing if result of floating-point conversion is already normalized.
                            if (Mathf.Approximately(0f, minFloat) && Mathf.Approximately(0f, maxFloat))
                                return null;
                            var zero = min + (max - min) / 2.0f;
                            return string.Format(CultureInfo.InvariantCulture, "normalize,normalizeMin={0},normalizeMax={1},normalizeZero={2}", min, max, zero);
                    }
                }

                return null;
            }

            internal PrimitiveValue DetermineDefaultState()
            {
                if (usagePage == UsagePage.GenericDesktop && usage == (int)GenericDesktop.HatSwitch)
                {
                    // Figure out null state for hat switches.
                    if (hasNullState)
                    {
                        // We're looking for a value that is out-of-range with respect to the
                        // logical min and max but in range with respect to what we can store
                        // in the bits we have.

                        // Test lower bound.
                        var minMinusOne = logicalMin - 1;
                        if (minMinusOne >= 0)
                            return new PrimitiveValue(minMinusOne);

                        // Test upper bound.
                        var maxPlusOne = logicalMax + 1;
                        if (maxPlusOne <= ((1 << reportSizeInBits) - 1))
                            return new PrimitiveValue(maxPlusOne);
                    }
                }

                return new PrimitiveValue();
            }

            internal void AddChildControls(string controlName, ref InputControlLayout.Builder builder)
            {
                if (usagePage == UsagePage.GenericDesktop && usage == (int)GenericDesktop.HatSwitch)
                {
                    // There doesn't seem to be enough specificity in the HID spec to reliably figure this case out.
                    // Albeit detail is scarce, we could probably make some inferences based on the unit setting
                    // of the hat switch but even then it seems there's much left to the whims of a hardware manufacturer.
                    // Even if we know values go clockwise (HID spec doesn't really say; probably can be inferred from unit),
                    // which direction do we start with? Is 0 degrees up or right?
                    //
                    // What we do here is simply make the assumption that we're dealing with degrees here, that we go clockwise,
                    // and that 0 degrees is up (which is actually the opposite of the coordinate system suggested in 5.9 of
                    // of the HID spec but seems to be what manufacturers are actually using in practice). Of course, if the
                    // device we're looking at actually sets things up differently, then we end up with either an incorrectly
                    // oriented or (worse) a non-functional hat switch.

                    var nullValue = DetermineDefaultState();
                    if (nullValue.isEmpty)
                        return;

                    ////REVIEW: this probably only works with hatswitches that have their null value at logicalMax+1

                    builder.AddControl(controlName + "/up")
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1},nullValue={2},wrapAtValue={3}",
                            logicalMax, logicalMin + 1, nullValue.ToString(), logicalMax))
                        .WithBitOffset(0)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/right")
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 1, logicalMin + 3))
                        .WithBitOffset(0)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/down")
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 3, logicalMin + 5))
                        .WithBitOffset(0)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/left")
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 5, logicalMin + 7))
                        .WithBitOffset(0)
                        .WithSizeInBits((uint)reportSizeInBits);
                }
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
                return JsonUtility.ToJson(this, true);
            }

            public static HIDDeviceDescriptor FromJson(string json)
            {
                return JsonUtility.FromJson<HIDDeviceDescriptor>(json);
            }
        }

        /// <summary>
        /// Helper to quickly build descriptors for arbitrary HIDs.
        /// </summary>
        public struct HIDDeviceDescriptorBuilder
        {
            public UsagePage usagePage;
            public int usage;

            public HIDDeviceDescriptorBuilder(UsagePage usagePage, int usage)
                : this()
            {
                this.usagePage = usagePage;
                this.usage = usage;
            }

            public HIDDeviceDescriptorBuilder(GenericDesktop usage)
                : this(UsagePage.GenericDesktop, (int)usage)
            {
            }

            public HIDDeviceDescriptorBuilder StartReport(HIDReportType reportType, int reportId = 1)
            {
                m_CurrentReportId = reportId;
                m_CurrentReportType = reportType;
                m_CurrentReportOffsetInBits = 8; // Report ID.
                return this;
            }

            public HIDDeviceDescriptorBuilder AddElement(UsagePage usagePage, int usage, int sizeInBits)
            {
                if (m_Elements == null)
                {
                    m_Elements = new List<HIDElementDescriptor>();
                }
                else
                {
                    // Make sure the usage and usagePage combination is unique.
                    foreach (var element in m_Elements)
                    {
                        // Skip elements that aren't in the same report.
                        if (element.reportId != m_CurrentReportId || element.reportType != m_CurrentReportType)
                            continue;

                        if (element.usagePage == usagePage && element.usage == usage)
                            throw new InvalidOperationException(string.Format(
                                "Cannot add two elements with the same usage page '{0}' and usage '0x{1:X} the to same device",
                                usagePage, usage));
                    }
                }

                m_Elements.Add(new HIDElementDescriptor
                {
                    usage = usage,
                    usagePage = usagePage,
                    reportOffsetInBits = m_CurrentReportOffsetInBits,
                    reportSizeInBits = sizeInBits,
                    reportType = m_CurrentReportType,
                    reportId = m_CurrentReportId
                });
                m_CurrentReportOffsetInBits += sizeInBits;

                return this;
            }

            public HIDDeviceDescriptorBuilder AddElement(GenericDesktop usage, int sizeInBits)
            {
                return AddElement(UsagePage.GenericDesktop, (int)usage, sizeInBits);
            }

            public HIDDeviceDescriptorBuilder WithPhysicalMinMax(int min, int max)
            {
                var index = m_Elements.Count - 1;
                if (index < 0)
                    throw new InvalidOperationException("No element has been added to the descriptor yet");

                var element = m_Elements[index];
                element.physicalMin = min;
                element.physicalMax = max;
                m_Elements[index] = element;

                return this;
            }

            public HIDDeviceDescriptorBuilder WithLogicalMinMax(int min, int max)
            {
                var index = m_Elements.Count - 1;
                if (index < 0)
                    throw new InvalidOperationException("No element has been added to the descriptor yet");

                var element = m_Elements[index];
                element.logicalMin = min;
                element.logicalMax = max;
                m_Elements[index] = element;

                return this;
            }

            public HIDDeviceDescriptor Finish()
            {
                var descriptor = new HIDDeviceDescriptor
                {
                    usage = usage,
                    usagePage = usagePage,
                    elements = m_Elements != null ? m_Elements.ToArray() : null,
                    collections = m_Collections != null ? m_Collections.ToArray() : null,
                };

                return descriptor;
            }

            private int m_CurrentReportId;
            private HIDReportType m_CurrentReportType;
            private int m_CurrentReportOffsetInBits;

            private List<HIDElementDescriptor> m_Elements;
            private List<HIDCollectionDescriptor> m_Collections;

            private int m_InputReportSize;
            private int m_OutputReportSize;
            private int m_FeatureReportSize;
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
