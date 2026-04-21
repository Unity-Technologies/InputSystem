using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using UnityEngine.Pool;

////REVIEW: there will probably be lots of cases where the HID device creation process just needs a little tweaking; we should
////        have better mechanism to do that without requiring to replace the entire process wholesale

////TODO: expose the layout builder so that other layout builders can use it for their own purposes

////REVIEW: how are we dealing with multiple different input reports on the same device?

////REVIEW: move the enums and structs out of here and into UnityEngine.InputSystem.HID? Or remove the "HID" name prefixes from them?

////TODO: add blacklist for devices we really don't want to use (like apple's internal trackpad)

////TODO: add a way to mark certain layouts (such as HID layouts) as fallbacks; ideally, affect the layout matching score

////TODO: enable this to handle devices that split their input into multiple reports

#pragma warning disable CS0649, CS0219
namespace UnityEngine.InputSystem.HID
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public class HID : InputDevice
    {
        internal const string kHIDInterface = "HID";
        internal const string kHIDNamespace = "HID";

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

        /// <summary>
        /// The FourCC code identifying <see cref="QueryHIDParsedReportDescriptorDeviceCommand"/>.
        /// </summary>
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

        private static readonly ProfilerMarker k_HIDParseDescriptorFallback = new ProfilerMarker("HIDParseDescriptorFallback");

        // This is the workhorse for figuring out fallback options for HIDs attached to the system.
        // If the system cannot find a more specific layout for a given HID, this method will try
        // to produce a layout builder on the fly based on the HID descriptor received from
        // the device.
        internal static string OnFindLayoutForDevice(ref InputDeviceDescription description, string matchedLayout,
            InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            // If the system found a matching layout, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedLayout))
                return null;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != kHIDInterface)
                return null;

            // Read HID descriptor.
            var hidDeviceDescriptor = ReadHIDDeviceDescriptor(ref description, executeDeviceCommand);

            if (!HIDSupport.supportedHIDUsages.Contains(new HIDSupport.HIDPageUsage(hidDeviceDescriptor.usagePage, hidDeviceDescriptor.usage)))
                return null;

            // Determine if there's any usable elements on the device.
            var hasUsableElements = false;
            if (hidDeviceDescriptor.elements != null)
            {
                foreach (var element in hidDeviceDescriptor.elements)
                {
                    if (element.IsUsableElement())
                    {
                        hasUsableElements = true;
                        break;
                    }
                }
            }

            // If not, there's nothing we can do with the device.
            if (!hasUsableElements)
                return null;

            ////TODO: we should be able to differentiate a HID joystick from other joysticks in bindings alone
            // Determine base layout.
            var baseType = typeof(HID);
            var baseLayout = "HID";
            if (hidDeviceDescriptor.usagePage == UsagePage.GenericDesktop)
            {
                if (hidDeviceDescriptor.usage == (int)GenericDesktop.Joystick || hidDeviceDescriptor.usage == (int)GenericDesktop.Gamepad)
                {
                    baseLayout = "Joystick";
                    baseType = typeof(Joystick);
                }
            }

            // A HID may implement the HID interface arbitrary many times, each time with a different
            // usage page + usage combination. In a OS, this will typically come out as multiple separate
            // devices. Thus, to make layout names unique, we have to take usages into account. What we do
            // is we tag the usage name onto the layout name *except* if it's a joystick or gamepad. This
            // gives us nicer names for joysticks while still disambiguating other devices correctly.
            var usageName = "";
            if (baseLayout != "Joystick")
            {
                usageName = hidDeviceDescriptor.usagePage == UsagePage.GenericDesktop
                    ? $" {(GenericDesktop) hidDeviceDescriptor.usage}"
                    : $" {hidDeviceDescriptor.usagePage}-{hidDeviceDescriptor.usage}";
            }

            ////REVIEW: these layout names are impossible to bind to; come up with a better way
            ////TODO: match HID layouts by vendor and product ID
            ////REVIEW: this probably works fine for most products out there but I'm not sure it works reliably for all cases
            // Come up with a unique template name. HIDs are required to have product and vendor IDs.
            // We go with the string versions if we have them and with the numeric versions if we don't.
            string layoutName;
            var deviceMatcher = InputDeviceMatcher.FromDeviceDescription(description);
            if (!string.IsNullOrEmpty(description.product) && !string.IsNullOrEmpty(description.manufacturer))
            {
                layoutName = $"{kHIDNamespace}::{description.manufacturer} {description.product}{usageName}";
            }
            else if (!string.IsNullOrEmpty(description.product))
            {
                layoutName = $"{kHIDNamespace}::{description.product}{usageName}";
            }
            else
            {
                // Sanity check to make sure we really have the data we expect.
                if (hidDeviceDescriptor.vendorId == 0)
                    return null;
                layoutName =
                    $"{kHIDNamespace}::{hidDeviceDescriptor.vendorId:X}-{hidDeviceDescriptor.productId:X}{usageName}";

                deviceMatcher = deviceMatcher
                    .WithCapability("productId", hidDeviceDescriptor.productId)
                    .WithCapability("vendorId", hidDeviceDescriptor.vendorId);
            }

            // Also match by usage. See comment above about multiple HID interfaces on the same device.
            deviceMatcher = deviceMatcher
                .WithCapability("usage", hidDeviceDescriptor.usage)
                .WithCapability("usagePage", hidDeviceDescriptor.usagePage);

            // Register layout builder that will turn the HID descriptor into an
            // InputControlLayout instance.
            var layout = new HIDLayoutBuilder
            {
                displayName = description.product,
                hidDescriptor = hidDeviceDescriptor,
                parentLayout = baseLayout,
                deviceType = baseType ?? typeof(HID)
            };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(),
                layoutName, baseLayout, deviceMatcher);

            return layoutName;
        }

        internal static unsafe HIDDeviceDescriptor ReadHIDDeviceDescriptor(ref InputDeviceDescription deviceDescription,
            InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            if (deviceDescription.interfaceName != kHIDInterface)
                throw new ArgumentException(
                    $"Device '{deviceDescription}' is not a HID");

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
                    Debug.LogError($"Could not parse HID descriptor of device '{deviceDescription}'");
                    Debug.LogException(exception);
                }
            }

            ////REVIEW: we *could* switch to a single path here that supports *only* parsed descriptors but it'd
            ////        mean having to switch *every* platform supporting HID to the hack we currently have to do
            ////        on Windows

            // Request descriptor, if necessary.
            if (needToRequestDescriptor)
            {
                // Try to get the size of the HID descriptor from the device.
                var sizeOfDescriptorCommand = new InputDeviceCommand(QueryHIDReportDescriptorSizeDeviceCommandType);
                var sizeOfDescriptorInBytes = executeCommandDelegate(ref sizeOfDescriptorCommand);
                if (sizeOfDescriptorInBytes > 0)
                {
                    // Now try to fetch the HID descriptor.
                    using (var buffer =
                               InputDeviceCommand.AllocateNative(QueryHIDReportDescriptorDeviceCommandType, (int)sizeOfDescriptorInBytes))
                    {
                        var commandPtr = (InputDeviceCommand*)buffer.GetUnsafePtr();
                        if (executeCommandDelegate(ref *commandPtr) != sizeOfDescriptorInBytes)
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
                        var commandPtr = (InputDeviceCommand*)buffer.GetUnsafePtr();
                        var utf8Length = executeCommandDelegate(ref *commandPtr);
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
                            Debug.LogError($"Could not parse HID descriptor of device '{deviceDescription}'");
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

        /// <summary>
        /// Returns a human-readable name for the given HID usage page.
        /// </summary>
        public static string UsagePageToString(UsagePage usagePage)
        {
            return (int)usagePage >= 0xFF00 ? "Vendor-Defined" : usagePage.ToString();
        }

        /// <summary>
        /// Returns a human-readable name for the given HID usage within the specified usage page.
        /// </summary>
        public static string UsageToString(UsagePage usagePage, int usage)
        {
            switch (usagePage)
            {
                case UsagePage.GenericDesktop:
                    return ((GenericDesktop)usage).ToString();
                case UsagePage.Simulation:
                    return ((Simulation)usage).ToString();
                default:
                    return null;
            }
        }

        [Serializable]
        private class HIDLayoutBuilder
        {
            public string displayName;
            public HIDDeviceDescriptor hidDescriptor;
            public string parentLayout;
            public Type deviceType;

            public InputControlLayout Build()
            {
                var builder = new InputControlLayout.Builder
                {
                    displayName = displayName,
                    type = deviceType,
                    extendsLayout = parentLayout,
                    stateFormat = new FourCC('H', 'I', 'D')
                };

                var xElement = Array.Find(hidDescriptor.elements,
                    element => element.usagePage == UsagePage.GenericDesktop &&
                    element.usage == (int)GenericDesktop.X);
                var yElement = Array.Find(hidDescriptor.elements,
                    element => element.usagePage == UsagePage.GenericDesktop &&
                    element.usage == (int)GenericDesktop.Y);

                ////REVIEW: in case the X and Y control are non-contiguous, should we even turn them into a stick
                ////REVIEW: there *has* to be an X and a Y for us to be able to successfully create a joystick
                // If GenericDesktop.X and GenericDesktop.Y are both present, turn the controls
                // into a stick.
                var haveStick = xElement.usage == (int)GenericDesktop.X && yElement.usage == (int)GenericDesktop.Y;
                if (haveStick)
                {
                    int bitOffset, byteOffset, sizeInBits;
                    if (xElement.reportOffsetInBits <= yElement.reportOffsetInBits)
                    {
                        bitOffset = xElement.reportOffsetInBits % 8;
                        byteOffset = xElement.reportOffsetInBits / 8;
                        sizeInBits = (yElement.reportOffsetInBits + yElement.reportSizeInBits) -
                            xElement.reportOffsetInBits;
                    }
                    else
                    {
                        bitOffset = yElement.reportOffsetInBits % 8;
                        byteOffset = yElement.reportOffsetInBits / 8;
                        sizeInBits = (xElement.reportOffsetInBits + xElement.reportSizeInBits) -
                            yElement.reportSizeInBits;
                    }

                    const string stickName = "stick";
                    builder.AddControl(stickName)
                        .WithDisplayName("Stick")
                        .WithLayout("Stick")
                        .WithBitOffset((uint)bitOffset)
                        .WithByteOffset((uint)byteOffset)
                        .WithSizeInBits((uint)sizeInBits)
                        .WithUsages(CommonUsages.Primary2DMotion);

                    var xElementParameters = xElement.DetermineParameters();
                    var yElementParameters = yElement.DetermineParameters();

                    builder.AddControl(stickName + "/x")
                        .WithFormat(xElement.DetermineFormat())
                        .WithByteOffset((uint)(xElement.reportOffsetInBits / 8 - byteOffset))
                        .WithBitOffset((uint)(xElement.reportOffsetInBits % 8))
                        .WithSizeInBits((uint)xElement.reportSizeInBits)
                        .WithParameters(xElementParameters)
                        .WithDefaultState(xElement.DetermineDefaultState())
                        .WithProcessors(xElement.DetermineProcessors());

                    builder.AddControl(stickName + "/y")
                        .WithFormat(yElement.DetermineFormat())
                        .WithByteOffset((uint)(yElement.reportOffsetInBits / 8 - byteOffset))
                        .WithBitOffset((uint)(yElement.reportOffsetInBits % 8))
                        .WithSizeInBits((uint)yElement.reportSizeInBits)
                        .WithParameters(yElementParameters)
                        .WithDefaultState(yElement.DetermineDefaultState())
                        .WithProcessors(yElement.DetermineProcessors());

                    // Propagate parameters needed on x and y to the four button controls.
                    builder.AddControl(stickName + "/up")
                        .WithParameters(
                            StringHelpers.Join(",", yElementParameters, "clamp=2,clampMin=-1,clampMax=0,invert=true"));
                    builder.AddControl(stickName + "/down")
                        .WithParameters(
                            StringHelpers.Join(",", yElementParameters, "clamp=2,clampMin=0,clampMax=1,invert=false"));
                    builder.AddControl(stickName + "/left")
                        .WithParameters(
                            StringHelpers.Join(",", xElementParameters, "clamp=2,clampMin=-1,clampMax=0,invert"));
                    builder.AddControl(stickName + "/right")
                        .WithParameters(
                            StringHelpers.Join(",", xElementParameters, "clamp=2,clampMin=0,clampMax=1"));
                }

                // Process HID descriptor.
                var elements = hidDescriptor.elements;
                var elementCount = elements.Length;
                for (var i = 0; i < elementCount; ++i)
                {
                    ref var element = ref elements[i];
                    if (element.reportType != HIDReportType.Input)
                        continue;

                    // Skip X and Y if we already turned them into a stick.
                    if (haveStick && (element.Is(UsagePage.GenericDesktop, (int)GenericDesktop.X) ||
                                      element.Is(UsagePage.GenericDesktop, (int)GenericDesktop.Y)))
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
                                .WithDisplayName(element.DetermineDisplayName())
                                .WithLayout(layout)
                                .WithByteOffset((uint)element.reportOffsetInBits / 8)
                                .WithBitOffset((uint)element.reportOffsetInBits % 8)
                                .WithSizeInBits((uint)element.reportSizeInBits)
                                .WithFormat(element.DetermineFormat())
                                .WithDefaultState(element.DetermineDefaultState())
                                .WithProcessors(element.DetermineProcessors());

                        var parameters = element.DetermineParameters();
                        if (!string.IsNullOrEmpty(parameters))
                            control.WithParameters(parameters);

                        var usages = element.DetermineUsages();
                        if (usages != null)
                            control.WithUsages(usages);

                        element.AddChildControls(ref element, name, ref builder);
                    }
                }

                return builder.Build();
            }
        }

        /// <summary>
        /// Type of a HID report as defined in the HID specification.
        /// </summary>
        public enum HIDReportType
        {
            /// <summary>
            /// The report type is not known or not parsed.
            /// </summary>
            Unknown,
            /// <summary>
            /// An input report sent from the device to the host.
            /// </summary>
            Input,
            /// <summary>
            /// An output report sent from the host to the device.
            /// </summary>
            Output,
            /// <summary>
            /// A feature report for bidirectional configuration data.
            /// </summary>
            Feature
        }

        /// <summary>
        /// Type of a HID collection as defined in the HID specification.
        /// </summary>
        public enum HIDCollectionType
        {
            /// <summary>
            /// A group of axes that represents data from one geometric point.
            /// </summary>
            Physical = 0x00,
            /// <summary>
            /// A collection that encompasses all the controls that are part of a single application.
            /// </summary>
            Application = 0x01,
            /// <summary>
            /// A logical grouping of controls.
            /// </summary>
            Logical = 0x02,
            /// <summary>
            /// A collection of items that represent a single report.
            /// </summary>
            Report = 0x03,
            /// <summary>
            /// A named array collection.
            /// </summary>
            NamedArray = 0x04,
            /// <summary>
            /// A usage switch collection.
            /// </summary>
            UsageSwitch = 0x05,
            /// <summary>
            /// A usage modifier collection.
            /// </summary>
            UsageModifier = 0x06
        }

        /// <summary>
        /// Flags describing a HID element as defined in the HID specification Main item tags.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "No better term for underlying data.")]
        [Flags]
        public enum HIDElementFlags
        {
            /// <summary>
            /// The element reports a constant value (data bit 0 is set).
            /// </summary>
            Constant = 1 << 0,
            /// <summary>
            /// The element reports individual controls per usage (as opposed to array).
            /// </summary>
            Variable = 1 << 1,
            /// <summary>
            /// The element reports relative values (delta from last report).
            /// </summary>
            Relative = 1 << 2,
            /// <summary>
            /// The element wraps around when it reaches its logical bounds.
            /// </summary>
            Wrap = 1 << 3,
            /// <summary>
            /// The element represents a non-linear control.
            /// </summary>
            NonLinear = 1 << 4,
            /// <summary>
            /// The element has no preferred state (does not return to a neutral state).
            /// </summary>
            NoPreferred = 1 << 5,
            /// <summary>
            /// The element has a null (out-of-range) state.
            /// </summary>
            NullState = 1 << 6,
            /// <summary>
            /// The element value may change without a host interaction (output/feature only).
            /// </summary>
            Volatile = 1 << 7,
            /// <summary>
            /// The element contains a stream of bytes (buffered bytes).
            /// </summary>
            BufferedBytes = 1 << 8
        }

        /// <summary>
        /// Descriptor for a single report element.
        /// </summary>
        [Serializable]
        public struct HIDElementDescriptor
        {
            /// <summary>
            /// The usage ID of the element within its usage page.
            /// </summary>
            public int usage;
            /// <summary>
            /// The HID usage page this element belongs to.
            /// </summary>
            public UsagePage usagePage;
            /// <summary>
            /// The unit of measurement for the element's value.
            /// </summary>
            public int unit;
            /// <summary>
            /// The exponent applied to the unit value (as a power of 10).
            /// </summary>
            public int unitExponent;
            /// <summary>
            /// The minimum logical value reported by this element.
            /// </summary>
            public int logicalMin;
            /// <summary>
            /// The maximum logical value reported by this element.
            /// </summary>
            public int logicalMax;
            /// <summary>
            /// The minimum physical value this element can represent.
            /// </summary>
            public int physicalMin;
            /// <summary>
            /// The maximum physical value this element can represent.
            /// </summary>
            public int physicalMax;
            /// <summary>
            /// The type of report this element belongs to.
            /// </summary>
            public HIDReportType reportType;
            /// <summary>
            /// Index of the collection this element belongs to.
            /// </summary>
            public int collectionIndex;
            /// <summary>
            /// The ID of the HID report this element is part of.
            /// </summary>
            public int reportId;
            /// <summary>
            /// The size of this element's data in the report, in bits.
            /// </summary>
            public int reportSizeInBits;
            /// <summary>
            /// The bit offset of this element within its report.
            /// </summary>
            public int reportOffsetInBits;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flags", Justification = "No better term for underlying data.")]
            /// <summary>
            /// Flags describing the element's data attributes.
            /// </summary>
            public HIDElementFlags flags;

            // Fields only relevant to arrays.
            /// <summary>
            /// The minimum usage ID when this element represents a usage range.
            /// </summary>
            public int? usageMin;
            /// <summary>
            /// The maximum usage ID when this element represents a usage range.
            /// </summary>
            public int? usageMax;

            /// <summary>
            /// True if the element has a null state (reports out-of-range values).
            /// </summary>
            public bool hasNullState => (flags & HIDElementFlags.NullState) == HIDElementFlags.NullState;

            /// <summary>
            /// True if the element returns to a preferred/neutral state.
            /// </summary>
            public bool hasPreferredState => (flags & HIDElementFlags.NoPreferred) != HIDElementFlags.NoPreferred;

            /// <summary>
            /// True if the element is an array (reports index values rather than individual bits).
            /// </summary>
            public bool isArray => (flags & HIDElementFlags.Variable) != HIDElementFlags.Variable;

            /// <summary>
            /// True if the element represents a non-linear control.
            /// </summary>
            public bool isNonLinear => (flags & HIDElementFlags.NonLinear) == HIDElementFlags.NonLinear;

            /// <summary>
            /// True if the element reports relative values.
            /// </summary>
            public bool isRelative => (flags & HIDElementFlags.Relative) == HIDElementFlags.Relative;

            /// <summary>
            /// True if the element always reports a constant value.
            /// </summary>
            public bool isConstant => (flags & HIDElementFlags.Constant) == HIDElementFlags.Constant;

            /// <summary>
            /// True if the element value wraps around at its limits.
            /// </summary>
            public bool isWrapping => (flags & HIDElementFlags.Wrap) == HIDElementFlags.Wrap;

            internal bool isSigned => logicalMin < 0;

            internal float minFloatValue
            {
                get
                {
                    if (isSigned)
                    {
                        var minValue = (int)-(long)(1UL << (reportSizeInBits - 1));
                        var maxValue = (int)((1UL << (reportSizeInBits - 1)) - 1);
                        return NumberHelpers.IntToNormalizedFloat(logicalMin, minValue, maxValue) * 2.0f - 1.0f;
                    }
                    else
                    {
                        Debug.Assert(logicalMin >= 0, $"Expected logicalMin to be unsigned");
                        var maxValue = (uint)((1UL << reportSizeInBits) - 1);
                        return NumberHelpers.UIntToNormalizedFloat((uint)logicalMin, 0, maxValue);
                    }
                }
            }

            internal float maxFloatValue
            {
                get
                {
                    if (isSigned)
                    {
                        var minValue = (int)-(long)(1UL << (reportSizeInBits - 1));
                        var maxValue = (int)((1UL << (reportSizeInBits - 1)) - 1);
                        return NumberHelpers.IntToNormalizedFloat(logicalMax, minValue, maxValue) * 2.0f - 1.0f;
                    }
                    else
                    {
                        Debug.Assert(logicalMax >= 0, $"Expected logicalMax to be unsigned");
                        var maxValue = (uint)((1UL << reportSizeInBits) - 1);
                        return NumberHelpers.UIntToNormalizedFloat((uint)logicalMax, 0, maxValue);
                    }
                }
            }

            /// <summary>
            /// Returns true if this element matches the given usage page and usage ID.
            /// </summary>
            public bool Is(UsagePage usagePage, int usage)
            {
                return usagePage == this.usagePage && usage == this.usage;
            }

            internal string DetermineName()
            {
                // It's rare for HIDs to declare string names for items and HID drivers may report weird strings
                // plus there's no guarantee that these names are unique per item. So, we don't bother here with
                // device/driver-supplied names at all but rather do our own naming.

                switch (usagePage)
                {
                    case UsagePage.Button:
                        if (usage == 1)
                            return "trigger";
                        return $"button{usage}";
                    case UsagePage.GenericDesktop:
                        if (usage == (int)GenericDesktop.HatSwitch)
                            return "hat";
                        var text = ((GenericDesktop)usage).ToString();
                        // Lower-case first letter.
                        text = char.ToLowerInvariant(text[0]) + text.Substring(1);
                        return text;
                }

                // Fallback that generates a somewhat useless but at least very informative name.
                return $"UsagePage({usagePage:X}) Usage({usage:X})";
            }

            internal string DetermineDisplayName()
            {
                switch (usagePage)
                {
                    case UsagePage.Button:
                        if (usage == 1)
                            return "Trigger";
                        return $"Button {usage}";
                    case UsagePage.GenericDesktop:
                        return ((GenericDesktop)usage).ToString();
                }

                return null;
            }

            internal bool IsUsableElement()
            {
                switch (usage)
                {
                    case (int)GenericDesktop.X:
                    case (int)GenericDesktop.Y:
                        return usagePage == UsagePage.GenericDesktop;
                    default:
                        return DetermineLayout() != null;
                }
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
                        return isSigned ? InputStateBlock.FormatSByte : InputStateBlock.FormatByte;
                    case 16:
                        return isSigned ? InputStateBlock.FormatShort : InputStateBlock.FormatUShort;
                    case 32:
                        return isSigned ? InputStateBlock.FormatInt : InputStateBlock.FormatUInt;
                    default:
                        // Generic bitfield value.
                        return InputStateBlock.FormatBit;
                }
            }

            internal InternedString[] DetermineUsages()
            {
                if (usagePage == UsagePage.Button && usage == 1)
                    return new[] {CommonUsages.PrimaryTrigger, CommonUsages.PrimaryAction};
                if (usagePage == UsagePage.Button && usage == 2)
                    return new[] {CommonUsages.SecondaryTrigger, CommonUsages.SecondaryAction};
                if (usagePage == UsagePage.GenericDesktop && usage == (int)GenericDesktop.Rz)
                    return new[] { CommonUsages.Twist };
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
                        case (int)GenericDesktop.Z:
                        case (int)GenericDesktop.Rx:
                        case (int)GenericDesktop.Rz:
                        case (int)GenericDesktop.Vx:
                        case (int)GenericDesktop.Vz:
                        case (int)GenericDesktop.Vbrx:
                        case (int)GenericDesktop.Vbrz:
                        case (int)GenericDesktop.Slider:
                        case (int)GenericDesktop.Dial:
                        case (int)GenericDesktop.Wheel:
                            return DetermineAxisNormalizationParameters();

                        // Our Ys tend to be the opposite of what most HIDs do. We can't be sure and may well
                        // end up inverting a value here when we shouldn't but as always with the HID fallback,
                        // let's try to do what *seems* to work with the majority of devices.
                        case (int)GenericDesktop.Y:
                        case (int)GenericDesktop.Ry:
                        case (int)GenericDesktop.Vy:
                        case (int)GenericDesktop.Vbry:
                            return StringHelpers.Join(",", "invert", DetermineAxisNormalizationParameters());
                    }
                }

                return null;
            }

            private string DetermineAxisNormalizationParameters()
            {
                // If we have min/max bounds on the axis values, set up normalization on the axis.
                // NOTE: We put the center in the middle between min/max as we can't know where the
                //       resting point of the axis is (may be on min if it's a trigger, for example).
                if (logicalMin == 0 && logicalMax == 0)
                    return "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5";
                var min = minFloatValue;
                var max = maxFloatValue;
                // Do nothing if result of floating-point conversion is already normalized.
                if (Mathf.Approximately(0f, min) && Mathf.Approximately(0f, max))
                    return null;
                var zero = min + (max - min) / 2.0f;
                return string.Format(CultureInfo.InvariantCulture, "normalize,normalizeMin={0},normalizeMax={1},normalizeZero={2}", min, max, zero);
            }

            internal string DetermineProcessors()
            {
                switch (usagePage)
                {
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
                                return "axisDeadzone";
                        }
                        break;
                }

                return null;
            }

            internal PrimitiveValue DetermineDefaultState()
            {
                switch (usagePage)
                {
                    case UsagePage.GenericDesktop:
                        switch (usage)
                        {
                            case (int)GenericDesktop.HatSwitch:
                                // Figure out null state for hat switches.
                                if (hasNullState)
                                {
                                    // We're looking for a value that is out-of-range with respect to the
                                    // logical min and max but in range with respect to what we can store
                                    // in the bits we have.

                                    // Test lower bound, we can store >= 0.
                                    if (logicalMin >= 1)
                                        return new PrimitiveValue(logicalMin - 1);

                                    // Test upper bound, we can store <= maxValue.
                                    var maxValue = (1UL << reportSizeInBits) - 1;
                                    if ((ulong)logicalMax < maxValue)
                                        return new PrimitiveValue(logicalMax + 1);
                                }
                                break;

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
                                // For axes that are *NOT* stored as signed values (which we assume are
                                // centered on 0), put the default state in the middle between the min and max.
                                if (!isSigned)
                                {
                                    var defaultValue = logicalMin + (logicalMax - logicalMin) / 2;
                                    if (defaultValue != 0)
                                        return new PrimitiveValue(defaultValue);
                                }
                                break;
                        }
                        break;
                }

                return new PrimitiveValue();
            }

            internal void AddChildControls(ref HIDElementDescriptor element, string controlName, ref InputControlLayout.Builder builder)
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
                        .WithFormat(InputStateBlock.FormatBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1},nullValue={2},wrapAtValue={3}",
                            logicalMax, logicalMin + 1, nullValue.ToString(), logicalMax))
                        .WithBitOffset((uint)element.reportOffsetInBits % 8)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/right")
                        .WithFormat(InputStateBlock.FormatBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 1, logicalMin + 3))
                        .WithBitOffset((uint)element.reportOffsetInBits % 8)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/down")
                        .WithFormat(InputStateBlock.FormatBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 3, logicalMin + 5))
                        .WithBitOffset((uint)element.reportOffsetInBits % 8)
                        .WithSizeInBits((uint)reportSizeInBits);

                    builder.AddControl(controlName + "/left")
                        .WithFormat(InputStateBlock.FormatBit)
                        .WithLayout("DiscreteButton")
                        .WithParameters(string.Format(CultureInfo.InvariantCulture,
                            "minValue={0},maxValue={1}",
                            logicalMin + 5, logicalMin + 7))
                        .WithBitOffset((uint)element.reportOffsetInBits % 8)
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
            /// <summary>
            /// The type of this HID collection.
            /// </summary>
            public HIDCollectionType type;
            /// <summary>
            /// The usage ID of this collection.
            /// </summary>
            public int usage;
            /// <summary>
            /// The usage page of this collection.
            /// </summary>
            public UsagePage usagePage;
            /// <summary>
            /// Index of the parent collection, or -1 if this is a root collection.
            /// </summary>
            public int parent; // -1 if no parent.
            /// <summary>
            /// Number of child collections contained within this collection.
            /// </summary>
            public int childCount;
            /// <summary>
            /// Index of the first child collection.
            /// </summary>
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
            /// <summary>
            /// The HID usage ID of the device.
            /// </summary>
            public int usage;
            /// <summary>
            /// The HID usage page of the device.
            /// </summary>
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

            /// <summary>
            /// All elements (controls) described in the HID report descriptor.
            /// </summary>
            public HIDElementDescriptor[] elements;
            /// <summary>
            /// All collections described in the HID report descriptor.
            /// </summary>
            public HIDCollectionDescriptor[] collections;

            /// <summary>
            /// Serializes this descriptor to a JSON string.
            /// </summary>
            public string ToJson()
            {
                return JsonUtility.ToJson(this, true);
            }

            /// <summary>
            /// Deserializes a <see cref="HIDDeviceDescriptor"/> from a JSON string.
            /// </summary>
            public static HIDDeviceDescriptor FromJson(string json)
            {
                try
                {
                    // HID descriptors, when formatted correctly, are always json strings with no whitespace and a
                    // predictable order of elements, so we can try and use this simple predictive parser to extract
                    // the data. If for any reason the data is not formatted correctly, we'll automatically fall back
                    // to Unity's default json parser.
                    var descriptor = new HIDDeviceDescriptor();

                    var jsonSpan = json.AsSpan();
                    var parser = new PredictiveParser();
                    parser.ExpectSingleChar(jsonSpan, '{');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.vendorId = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.productId = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.usage = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.usagePage = (UsagePage)parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.inputReportSize = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.outputReportSize = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    parser.AcceptString(jsonSpan, out _);
                    parser.ExpectSingleChar(jsonSpan, ':');
                    descriptor.featureReportSize = parser.ExpectInt(jsonSpan);
                    parser.AcceptSingleChar(jsonSpan, ',');

                    // elements
                    parser.AcceptString(jsonSpan, out var key);
                    if (key.ToString() != "elements") return descriptor;

                    parser.ExpectSingleChar(jsonSpan, ':');
                    parser.ExpectSingleChar(jsonSpan, '[');

                    using var pool = ListPool<HIDElementDescriptor>.Get(out var elements);
                    while (!parser.AcceptSingleChar(jsonSpan, ']'))
                    {
                        parser.AcceptSingleChar(jsonSpan, ',');
                        parser.ExpectSingleChar(jsonSpan, '{');

                        HIDElementDescriptor elementDesc = default;


                        parser.AcceptSingleChar(jsonSpan, '}');
                        parser.AcceptSingleChar(jsonSpan, ',');

                        // usage
                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.usage = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.usagePage = (UsagePage)parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.unit = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.unitExponent = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.logicalMin = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.logicalMax = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.physicalMin = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.physicalMax = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.collectionIndex = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.reportType = (HIDReportType)parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.reportId = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        // reportCount. We don't store this one
                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        parser.AcceptInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.reportSizeInBits = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.reportOffsetInBits = parser.ExpectInt(jsonSpan);
                        parser.AcceptSingleChar(jsonSpan, ',');

                        parser.ExpectString(jsonSpan);
                        parser.ExpectSingleChar(jsonSpan, ':');
                        elementDesc.flags = (HIDElementFlags)parser.ExpectInt(jsonSpan);

                        parser.ExpectSingleChar(jsonSpan, '}');

                        elements.Add(elementDesc);
                    }
                    descriptor.elements = elements.ToArray();

                    return descriptor;
                }
                catch (Exception)
                {
                    k_HIDParseDescriptorFallback.Begin();
                    var descriptor = JsonUtility.FromJson<HIDDeviceDescriptor>(json);
                    k_HIDParseDescriptorFallback.End();
                    return descriptor;
                }
            }
        }

        /// <summary>
        /// Helper to quickly build descriptors for arbitrary HIDs.
        /// </summary>
        public struct HIDDeviceDescriptorBuilder
        {
            /// <summary>
            /// The HID usage page for the device being described.
            /// </summary>
            public UsagePage usagePage;
            /// <summary>
            /// The HID usage ID for the device being described.
            /// </summary>
            public int usage;

            /// <summary>
            /// Initializes the builder for a device with the given usage page and usage ID.
            /// </summary>
            public HIDDeviceDescriptorBuilder(UsagePage usagePage, int usage)
                : this()
            {
                this.usagePage = usagePage;
                this.usage = usage;
            }

            /// <summary>
            /// Initializes the builder for a Generic Desktop device with the given usage.
            /// </summary>
            public HIDDeviceDescriptorBuilder(GenericDesktop usage)
                : this(UsagePage.GenericDesktop, (int)usage)
            {
            }

            /// <summary>
            /// Starts a new HID report of the given type with the specified report ID.
            /// </summary>
            public HIDDeviceDescriptorBuilder StartReport(HIDReportType reportType, int reportId = 1)
            {
                m_CurrentReportId = reportId;
                m_CurrentReportType = reportType;
                m_CurrentReportOffsetInBits = 8; // Report ID.
                return this;
            }

            /// <summary>
            /// Adds a new element to the current report with the specified usage page, usage, and bit size.
            /// </summary>
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
                            throw new InvalidOperationException(
                                $"Cannot add two elements with the same usage page '{usagePage}' and usage '0x{usage:X} the to same device");
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

            /// <summary>
            /// Adds a new Generic Desktop element to the current report with the specified usage and bit size.
            /// </summary>
            public HIDDeviceDescriptorBuilder AddElement(GenericDesktop usage, int sizeInBits)
            {
                return AddElement(UsagePage.GenericDesktop, (int)usage, sizeInBits);
            }

            /// <summary>
            /// Sets the physical minimum and maximum for the last added element.
            /// </summary>
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

            /// <summary>
            /// Sets the logical minimum and maximum for the last added element.
            /// </summary>
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

            /// <summary>
            /// Completes the builder and returns the resulting <see cref="HIDDeviceDescriptor"/>.
            /// </summary>
            public HIDDeviceDescriptor Finish()
            {
                var descriptor = new HIDDeviceDescriptor
                {
                    usage = usage,
                    usagePage = usagePage,
                    elements = m_Elements?.ToArray(),
                    collections = m_Collections?.ToArray(),
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
        /// <seealso href="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
        public enum UsagePage
        {
            /// <summary>
            /// Undefined or unknown usage page.
            /// </summary>
            Undefined = 0x00,
            /// <summary>
            /// Generic Desktop Controls usage page (page 0x01).
            /// </summary>
            GenericDesktop = 0x01,
            /// <summary>
            /// Simulation Controls usage page (page 0x02).
            /// </summary>
            Simulation = 0x02,
            /// <summary>
            /// VR Controls usage page (page 0x03).
            /// </summary>
            VRControls = 0x03,
            /// <summary>
            /// Sport Controls usage page (page 0x04).
            /// </summary>
            SportControls = 0x04,
            /// <summary>
            /// Game Controls usage page (page 0x05).
            /// </summary>
            GameControls = 0x05,
            /// <summary>
            /// Generic Device Controls usage page (page 0x06).
            /// </summary>
            GenericDeviceControls = 0x06,
            /// <summary>
            /// Keyboard/Keypad usage page (page 0x07).
            /// </summary>
            Keyboard = 0x07,
            /// <summary>
            /// LED usage page (page 0x08).
            /// </summary>
            LEDs = 0x08,
            /// <summary>
            /// Button usage page (page 0x09).
            /// </summary>
            Button = 0x09,
            /// <summary>
            /// Ordinal usage page (page 0x0A).
            /// </summary>
            Ordinal = 0x0A,
            /// <summary>
            /// Telephony Device usage page (page 0x0B).
            /// </summary>
            Telephony = 0x0B,
            /// <summary>
            /// Consumer usage page (page 0x0C).
            /// </summary>
            Consumer = 0x0C,
            /// <summary>
            /// Digitizer usage page (page 0x0D).
            /// </summary>
            Digitizer = 0x0D,
            /// <summary>
            /// Physical Interface Device (PID) usage page (page 0x0F).
            /// </summary>
            PID = 0x0F,
            /// <summary>
            /// Unicode usage page (page 0x10).
            /// </summary>
            Unicode = 0x10,
            /// <summary>
            /// Alphanumeric Display usage page (page 0x14).
            /// </summary>
            AlphanumericDisplay = 0x14,
            /// <summary>
            /// Medical Instrument usage page (page 0x40).
            /// </summary>
            MedicalInstruments = 0x40,
            /// <summary>
            /// Monitor usage page (page 0x80).
            /// </summary>
            Monitor = 0x80, // Starts here and goes up to 0x83.
            /// <summary>
            /// Power Device usage page (page 0x84).
            /// </summary>
            Power = 0x84, // Starts here and goes up to 0x87.
            /// <summary>
            /// Bar Code Scanner usage page (page 0x8C).
            /// </summary>
            BarCodeScanner = 0x8C,
            /// <summary>
            /// Magnetic Stripe Reader usage page (page 0x8E).
            /// </summary>
            MagneticStripeReader = 0x8E,
            /// <summary>
            /// Camera Control usage page (page 0x90).
            /// </summary>
            Camera = 0x90,
            /// <summary>
            /// Arcade usage page (page 0x91).
            /// </summary>
            Arcade = 0x91,
            /// <summary>
            /// Vendor-defined usage page (page 0xFF00 and above).
            /// </summary>
            VendorDefined = 0xFF00, // Starts here and goes up to 0xFFFF.
        }

        /// <summary>
        /// Usages in the GenericDesktop HID usage page.
        /// </summary>
        /// <seealso href="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
        public enum GenericDesktop
        {
            /// <summary>
            /// Undefined Generic Desktop usage.
            /// </summary>
            Undefined = 0x00,
            /// <summary>
            /// Pointer device usage (0x01).
            /// </summary>
            Pointer = 0x01,
            /// <summary>
            /// Mouse device usage (0x02).
            /// </summary>
            Mouse = 0x02,
            /// <summary>
            /// Joystick device usage (0x04).
            /// </summary>
            Joystick = 0x04,
            /// <summary>
            /// Gamepad device usage (0x05).
            /// </summary>
            Gamepad = 0x05,
            /// <summary>
            /// Keyboard device usage (0x06).
            /// </summary>
            Keyboard = 0x06,
            /// <summary>
            /// Keypad device usage (0x07).
            /// </summary>
            Keypad = 0x07,
            /// <summary>
            /// Multi-axis controller device usage (0x08).
            /// </summary>
            MultiAxisController = 0x08,
            /// <summary>
            /// Tablet PC System Controls device usage (0x09).
            /// </summary>
            TabletPCControls = 0x09,
            /// <summary>
            /// Assistive Control device usage (0x0A).
            /// </summary>
            AssistiveControl = 0x0A,
            /// <summary>
            /// X axis control (0x30).
            /// </summary>
            X = 0x30,
            /// <summary>
            /// Y axis control (0x31).
            /// </summary>
            Y = 0x31,
            /// <summary>
            /// Z axis control (0x32).
            /// </summary>
            Z = 0x32,
            /// <summary>
            /// Rotation around the X axis (0x33).
            /// </summary>
            Rx = 0x33,
            /// <summary>
            /// Rotation around the Y axis (0x34).
            /// </summary>
            Ry = 0x34,
            /// <summary>
            /// Rotation around the Z axis (0x35).
            /// </summary>
            Rz = 0x35,
            /// <summary>
            /// Slider control (0x36).
            /// </summary>
            Slider = 0x36,
            /// <summary>
            /// Dial control (0x37).
            /// </summary>
            Dial = 0x37,
            /// <summary>
            /// Wheel control (0x38).
            /// </summary>
            Wheel = 0x38,
            /// <summary>
            /// Hat switch control (0x39).
            /// </summary>
            HatSwitch = 0x39,
            /// <summary>
            /// Counted buffer (0x3A).
            /// </summary>
            CountedBuffer = 0x3A,
            /// <summary>
            /// Byte count (0x3B).
            /// </summary>
            ByteCount = 0x3B,
            /// <summary>
            /// Motion wakeup control (0x3C).
            /// </summary>
            MotionWakeup = 0x3C,
            /// <summary>
            /// Start button (0x3D).
            /// </summary>
            Start = 0x3D,
            /// <summary>
            /// Select button (0x3E).
            /// </summary>
            Select = 0x3E,
            /// <summary>
            /// Vector in the X direction (0x40).
            /// </summary>
            Vx = 0x40,
            /// <summary>
            /// Vector in the Y direction (0x41).
            /// </summary>
            Vy = 0x41,
            /// <summary>
            /// Vector in the Z direction (0x42).
            /// </summary>
            Vz = 0x42,
            /// <summary>
            /// Vector of the body rotation around X (0x43).
            /// </summary>
            Vbrx = 0x43,
            /// <summary>
            /// Vector of the body rotation around Y (0x44).
            /// </summary>
            Vbry = 0x44,
            /// <summary>
            /// Vector of the body rotation around Z (0x45).
            /// </summary>
            Vbrz = 0x45,
            /// <summary>
            /// No vector (0x46).
            /// </summary>
            Vno = 0x46,
            /// <summary>
            /// Feature notification (0x47).
            /// </summary>
            FeatureNotification = 0x47,
            /// <summary>
            /// Resolution multiplier (0x48).
            /// </summary>
            ResolutionMultiplier = 0x48,
            /// <summary>
            /// System control collection (0x80).
            /// </summary>
            SystemControl = 0x80,
            /// <summary>
            /// System power down (0x81).
            /// </summary>
            SystemPowerDown = 0x81,
            /// <summary>
            /// System sleep (0x82).
            /// </summary>
            SystemSleep = 0x82,
            /// <summary>
            /// System wake up (0x83).
            /// </summary>
            SystemWakeUp = 0x83,
            /// <summary>
            /// System context menu (0x84).
            /// </summary>
            SystemContextMenu = 0x84,
            /// <summary>
            /// System main menu (0x85).
            /// </summary>
            SystemMainMenu = 0x85,
            /// <summary>
            /// System application menu (0x86).
            /// </summary>
            SystemAppMenu = 0x86,
            /// <summary>
            /// System menu help (0x87).
            /// </summary>
            SystemMenuHelp = 0x87,
            /// <summary>
            /// System menu exit (0x88).
            /// </summary>
            SystemMenuExit = 0x88,
            /// <summary>
            /// System menu select (0x89).
            /// </summary>
            SystemMenuSelect = 0x89,
            /// <summary>
            /// System menu right (0x8A).
            /// </summary>
            SystemMenuRight = 0x8A,
            /// <summary>
            /// System menu left (0x8B).
            /// </summary>
            SystemMenuLeft = 0x8B,
            /// <summary>
            /// System menu up (0x8C).
            /// </summary>
            SystemMenuUp = 0x8C,
            /// <summary>
            /// System menu down (0x8D).
            /// </summary>
            SystemMenuDown = 0x8D,
            /// <summary>
            /// System cold restart (0x8E).
            /// </summary>
            SystemColdRestart = 0x8E,
            /// <summary>
            /// System warm restart (0x8F).
            /// </summary>
            SystemWarmRestart = 0x8F,
            /// <summary>
            /// D-pad up direction (0x90).
            /// </summary>
            DpadUp = 0x90,
            /// <summary>
            /// D-pad down direction (0x91).
            /// </summary>
            DpadDown = 0x91,
            /// <summary>
            /// D-pad right direction (0x92).
            /// </summary>
            DpadRight = 0x92,
            /// <summary>
            /// D-pad left direction (0x93).
            /// </summary>
            DpadLeft = 0x93,
            /// <summary>
            /// System dock (0xA0).
            /// </summary>
            SystemDock = 0xA0,
            /// <summary>
            /// System undock (0xA1).
            /// </summary>
            SystemUndock = 0xA1,
            /// <summary>
            /// System setup (0xA2).
            /// </summary>
            SystemSetup = 0xA2,
            /// <summary>
            /// System break (0xA3).
            /// </summary>
            SystemBreak = 0xA3,
            /// <summary>
            /// System debugger break (0xA4).
            /// </summary>
            SystemDebuggerBreak = 0xA4,
            /// <summary>
            /// Application break (0xA5).
            /// </summary>
            ApplicationBreak = 0xA5,
            /// <summary>
            /// Application debugger break (0xA6).
            /// </summary>
            ApplicationDebuggerBreak = 0xA6,
            /// <summary>
            /// System speaker mute (0xA7).
            /// </summary>
            SystemSpeakerMute = 0xA7,
            /// <summary>
            /// System hibernate (0xA8).
            /// </summary>
            SystemHibernate = 0xA8,
            /// <summary>
            /// System display invert (0xB0).
            /// </summary>
            SystemDisplayInvert = 0xB0,
            /// <summary>
            /// System display internal only (0xB1).
            /// </summary>
            SystemDisplayInternal = 0xB1,
            /// <summary>
            /// System display external only (0xB2).
            /// </summary>
            SystemDisplayExternal = 0xB2,
            /// <summary>
            /// System display both internal and external (0xB3).
            /// </summary>
            SystemDisplayBoth = 0xB3,
            /// <summary>
            /// System display dual (0xB4).
            /// </summary>
            SystemDisplayDual = 0xB4,
            /// <summary>
            /// System display toggle internal/external (0xB5).
            /// </summary>
            SystemDisplayToggleIntExt = 0xB5,
            /// <summary>
            /// System display swap primary and secondary (0xB6).
            /// </summary>
            SystemDisplaySwapPrimarySecondary = 0xB6,
            /// <summary>
            /// System LCD display auto-scale (0xB7).
            /// </summary>
            SystemDisplayLCDAutoScale = 0xB7
        }

        /// <summary>
        /// HID Simulation Controls usage page (0x02) usages.
        /// </summary>
        public enum Simulation
        {
            /// <summary>
            /// Undefined simulation usage.
            /// </summary>
            Undefined = 0x00,
            /// <summary>
            /// Flight simulation device (0x01).
            /// </summary>
            FlightSimulationDevice = 0x01,
            /// <summary>
            /// Automobile simulation device (0x02).
            /// </summary>
            AutomobileSimulationDevice = 0x02,
            /// <summary>
            /// Tank simulation device (0x03).
            /// </summary>
            TankSimulationDevice = 0x03,
            /// <summary>
            /// Spaceship simulation device (0x04).
            /// </summary>
            SpaceshipSimulationDevice = 0x04,
            /// <summary>
            /// Submarine simulation device (0x05).
            /// </summary>
            SubmarineSimulationDevice = 0x05,
            /// <summary>
            /// Sailing simulation device (0x06).
            /// </summary>
            SailingSimulationDevice = 0x06,
            /// <summary>
            /// Motorcycle simulation device (0x07).
            /// </summary>
            MotorcycleSimulationDevice = 0x07,
            /// <summary>
            /// Sports simulation device (0x08).
            /// </summary>
            SportsSimulationDevice = 0x08,
            /// <summary>
            /// Airplane simulation device (0x09).
            /// </summary>
            AirplaneSimulationDevice = 0x09,
            /// <summary>
            /// Helicopter simulation device (0x0A).
            /// </summary>
            HelicopterSimulationDevice = 0x0A,
            /// <summary>
            /// Magic carpet simulation device (0x0B).
            /// </summary>
            MagicCarpetSimulationDevice = 0x0B,
            /// <summary>
            /// Bicycle simulation device (0x0C).
            /// </summary>
            BicylcleSimulationDevice = 0x0C,
            /// <summary>
            /// Flight control stick control (0x20).
            /// </summary>
            FlightControlStick = 0x20,
            /// <summary>
            /// Flight stick control (0x21).
            /// </summary>
            FlightStick = 0x21,
            /// <summary>
            /// Cyclic control (0x22).
            /// </summary>
            CyclicControl = 0x22,
            /// <summary>
            /// Cyclic trim control (0x23).
            /// </summary>
            CyclicTrim = 0x23,
            /// <summary>
            /// Flight yoke control (0x24).
            /// </summary>
            FlightYoke = 0x24,
            /// <summary>
            /// Track control (0x25).
            /// </summary>
            TrackControl = 0x25,
            /// <summary>
            /// Aileron control (0xB0).
            /// </summary>
            Aileron = 0xB0,
            /// <summary>
            /// Aileron trim control (0xB1).
            /// </summary>
            AileronTrim = 0xB1,
            /// <summary>
            /// Anti-torque control (0xB2).
            /// </summary>
            AntiTorqueControl = 0xB2,
            /// <summary>
            /// Autopilot enable control (0xB3).
            /// </summary>
            AutopilotEnable = 0xB3,
            /// <summary>
            /// Chaff release control (0xB4).
            /// </summary>
            ChaffRelease = 0xB4,
            /// <summary>
            /// Collective control (0xB5).
            /// </summary>
            CollectiveControl = 0xB5,
            /// <summary>
            /// Dive brake control (0xB6).
            /// </summary>
            DiveBreak = 0xB6,
            /// <summary>
            /// Electronic countermeasures control (0xB7).
            /// </summary>
            ElectronicCountermeasures = 0xB7,
            /// <summary>
            /// Elevator control (0xB8).
            /// </summary>
            Elevator = 0xB8,
            /// <summary>
            /// Elevator trim control (0xB9).
            /// </summary>
            ElevatorTrim = 0xB9,
            /// <summary>
            /// Rudder control (0xBA).
            /// </summary>
            Rudder = 0xBA,
            /// <summary>
            /// Throttle control (0xBB).
            /// </summary>
            Throttle = 0xBB,
            /// <summary>
            /// Flight communications control (0xBC).
            /// </summary>
            FlightCommunications = 0xBC,
            /// <summary>
            /// Flare release control (0xBD).
            /// </summary>
            FlareRelease = 0xBD,
            /// <summary>
            /// Landing gear control (0xBE).
            /// </summary>
            LandingGear = 0xBE,
            /// <summary>
            /// Toe brake control (0xBF).
            /// </summary>
            ToeBreak = 0xBF,
            /// <summary>
            /// Trigger control (0xC0).
            /// </summary>
            Trigger = 0xC0,
            /// <summary>
            /// Weapons arm control (0xC1).
            /// </summary>
            WeaponsArm = 0xC1,
            /// <summary>
            /// Weapons select control (0xC2).
            /// </summary>
            WeaponsSelect = 0xC2,
            /// <summary>
            /// Wing flaps control (0xC3).
            /// </summary>
            WingFlaps = 0xC3,
            /// <summary>
            /// Accelerator control (0xC4).
            /// </summary>
            Accelerator = 0xC4,
            /// <summary>
            /// Brake control (0xC5).
            /// </summary>
            Brake = 0xC5,
            /// <summary>
            /// Clutch control (0xC6).
            /// </summary>
            Clutch = 0xC6,
            /// <summary>
            /// Shifter control (0xC7).
            /// </summary>
            Shifter = 0xC7,
            /// <summary>
            /// Steering control (0xC8).
            /// </summary>
            Steering = 0xC8,
            /// <summary>
            /// Turret direction control (0xC9).
            /// </summary>
            TurretDirection = 0xC9,
            /// <summary>
            /// Barrel elevation control (0xCA).
            /// </summary>
            BarrelElevation = 0xCA,
            /// <summary>
            /// Dive plane control (0xCB).
            /// </summary>
            DivePlane = 0xCB,
            /// <summary>
            /// Ballast control (0xCC).
            /// </summary>
            Ballast = 0xCC,
            /// <summary>
            /// Bicycle crank control (0xCD).
            /// </summary>
            BicycleCrank = 0xCD,
            /// <summary>
            /// Handle bars control (0xCE).
            /// </summary>
            HandleBars = 0xCE,
            /// <summary>
            /// Front brake control (0xCF).
            /// </summary>
            FrontBrake = 0xCF,
            /// <summary>
            /// Rear brake control (0xD0).
            /// </summary>
            RearBrake = 0xD0
        }

        /// <summary>
        /// HID Button usage page (0x09) usages.
        /// </summary>
        public enum Button
        {
            /// <summary>
            /// Undefined button usage.
            /// </summary>
            Undefined = 0,
            /// <summary>
            /// Primary button (button 1).
            /// </summary>
            Primary,
            /// <summary>
            /// Secondary button (button 2).
            /// </summary>
            Secondary,
            /// <summary>
            /// Tertiary button (button 3).
            /// </summary>
            Tertiary
        }
    }
}
