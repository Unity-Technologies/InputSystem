using System;
using System.Collections.Generic;

////TODO: array support
////TODO: delimiter support
////TODO: designator support

namespace UnityEngine.Experimental.Input.Plugins.HID
{
    /// <summary>
    /// Turns binary HID descriptors into <see cref="HID.HIDDeviceDescriptor"/> instances.
    /// </summary>
    /// <remarks>
    /// For information about the format, see the <a href="http://www.usb.org/developers/hidpage/HID1_11.pdf">
    /// Device Class Definition for Human Interface Devices</a> section 6.2.2.
    /// </remarks>
    public static class HIDParser
    {
        /// <summary>
        /// Parse a HID report descriptor as defined by section 6.2.2 of the
        /// <a href="http://www.usb.org/developers/hidpage/HID1_11.pdf">HID
        /// specification</a> and add the elements and collections from the
        /// descriptor to the given <paramref name="deviceDescriptor"/>.
        /// </summary>
        /// <param name="buffer">Buffer containing raw HID report descriptor.</param>
        /// <param name="deviceDescriptor">HID device descriptor to complete with the information
        /// from the report descriptor. Elements and collections will get added to this descriptor.</param>
        /// <returns>True if the report descriptor was successfully parsed.</returns>
        /// <remarks>
        /// Will also set <see cref="HID.HIDDeviceDescriptor.inputReportSize"/>,
        /// <see cref="HID.HIDDeviceDescriptor.outputReportSize"/>, and
        /// <see cref="HID.HIDDeviceDescriptor.featureReportSize"/>.
        /// </remarks>
        public static unsafe bool ParseReportDescriptor(byte[] buffer, ref HID.HIDDeviceDescriptor deviceDescriptor)
        {
            fixed(byte* bufferPtr = buffer)
            {
                return ParseReportDescriptor(bufferPtr, buffer.Length, ref deviceDescriptor);
            }
        }

        public unsafe static bool ParseReportDescriptor(byte* bufferPtr, int bufferLength, ref HID.HIDDeviceDescriptor deviceDescriptor)
        {
            // Item state.
            var localItemState = new HIDItemStateLocal();
            var globalItemState = new HIDItemStateGlobal();

            // Lists where we accumulate the data from the HID items.
            var reports = new List<HIDReportData>();
            var elements = new List<HID.HIDElementDescriptor>();
            var collections = new List<HID.HIDCollectionDescriptor>();
            var currentCollection = -1;

            // Parse the linear list of items.
            var endPtr = bufferPtr + bufferLength;
            var currentPtr = bufferPtr;
            while (currentPtr < endPtr)
            {
                var firstByte = *currentPtr;

                ////TODO
                if (firstByte == 0xFE)
                    throw new NotImplementedException("long item support");

                // Read item header.
                var itemSize = (byte)(firstByte & 0x3);
                var itemTypeAndTag = (byte)(firstByte & 0xFC);
                ++currentPtr;

                // Process item.
                switch (itemTypeAndTag)
                {
                    // ------------ Global Items --------------
                    // These set item state permanently until it is reset.

                    // Usage Page
                    case (int)HIDItemTypeAndTag.UsagePage:
                        globalItemState.usagePage = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Report Count
                    case (int)HIDItemTypeAndTag.ReportCount:
                        globalItemState.reportCount = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Report Size
                    case (int)HIDItemTypeAndTag.ReportSize:
                        globalItemState.reportSize = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Report ID
                    case (int)HIDItemTypeAndTag.ReportID:
                        globalItemState.reportId = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Logical Minimum
                    case (int)HIDItemTypeAndTag.LogicalMinimum:
                        globalItemState.logicalMinimum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Logical Maximum
                    case (int)HIDItemTypeAndTag.LogicalMaximum:
                        globalItemState.logicalMaximum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Physical Minimum
                    case (int)HIDItemTypeAndTag.PhysicalMinimum:
                        globalItemState.physicalMinimum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Physical Maximum
                    case (int)HIDItemTypeAndTag.PhysicalMaximum:
                        globalItemState.physicalMaximum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Unit Exponent
                    case (int)HIDItemTypeAndTag.UnitExponent:
                        globalItemState.unitExponent = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Unit
                    case (int)HIDItemTypeAndTag.Unit:
                        globalItemState.unit = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // ------------ Local Items --------------
                    // These set the state for the very next elements to be generated.

                    // Usage
                    case (int)HIDItemTypeAndTag.Usage:
                        localItemState.SetUsage(ReadData(itemSize, currentPtr, endPtr));
                        break;

                    // Usage Minimum
                    case (int)HIDItemTypeAndTag.UsageMinimum:
                        localItemState.usageMinimum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // Usage Maximum
                    case (int)HIDItemTypeAndTag.UsageMaximum:
                        localItemState.usageMaximum = ReadData(itemSize, currentPtr, endPtr);
                        break;

                    // ------------ Main Items --------------
                    // These emit things into the descriptor based on the local and global item state.

                    // Collection
                    case (int)HIDItemTypeAndTag.Collection:

                        // Start new collection.
                        var parentCollection = currentCollection;
                        currentCollection = collections.Count;
                        collections.Add(new HID.HIDCollectionDescriptor
                        {
                            type = (HID.HIDCollectionType)ReadData(itemSize, currentPtr, endPtr),
                            parent = parentCollection,
                            usagePage = globalItemState.GetUsagePage(0, ref localItemState),
                            usage = localItemState.GetUsage(0),
                            firstChild = elements.Count
                        });

                        HIDItemStateLocal.Reset(ref localItemState);
                        break;

                    // EndCollection
                    case (int)HIDItemTypeAndTag.EndCollection:
                        if (currentCollection == -1)
                            return false;

                        // Close collection.
                        var collection = collections[currentCollection];
                        collection.childCount = elements.Count - collection.firstChild;
                        collections[currentCollection] = collection;

                        // Switch back to parent collection (if any).
                        currentCollection = collection.parent;

                        HIDItemStateLocal.Reset(ref localItemState);
                        break;

                    // Input/Output/Feature
                    case (int)HIDItemTypeAndTag.Input:
                    case (int)HIDItemTypeAndTag.Output:
                    case (int)HIDItemTypeAndTag.Feature:

                        // Determine report type.
                        var reportType = itemTypeAndTag == (int)HIDItemTypeAndTag.Input
                            ? HID.HIDReportType.Input
                            : itemTypeAndTag == (int)HIDItemTypeAndTag.Output
                            ? HID.HIDReportType.Output
                            : HID.HIDReportType.Feature;

                        // Find report.
                        var reportIndex = HIDReportData.FindOrAddReport(globalItemState.reportId, reportType, reports);
                        var report = reports[reportIndex];

                        // If we have a report ID, then reports start with an 8 byte report ID.
                        // Shift our offsets accordingly.
                        if (report.currentBitOffset == 0 && globalItemState.reportId.HasValue)
                            report.currentBitOffset = 8;

                        // Add elements to report.
                        var reportCount = globalItemState.reportCount.GetValueOrDefault(1);
                        var flags = ReadData(itemSize, currentPtr, endPtr);
                        for (var i = 0; i < reportCount; ++i)
                        {
                            var element = new HID.HIDElementDescriptor
                            {
                                usage = localItemState.GetUsage(i) & 0xFFFF, // Mask off usage page, if set.
                                usagePage = globalItemState.GetUsagePage(i, ref localItemState),
                                reportType = reportType,
                                reportSizeInBits = globalItemState.reportSize.GetValueOrDefault(8),
                                reportOffsetInBits = report.currentBitOffset,
                                reportId = globalItemState.reportId.GetValueOrDefault(1),
                                flags = (HID.HIDElementFlags)flags,
                                logicalMin = globalItemState.logicalMinimum.GetValueOrDefault(0),
                                logicalMax = globalItemState.logicalMaximum.GetValueOrDefault(0),
                                physicalMin = globalItemState.GetPhysicalMin(),
                                physicalMax = globalItemState.GetPhysicalMax(),
                                unitExponent = globalItemState.unitExponent.GetValueOrDefault(0),
                                unit = globalItemState.unit.GetValueOrDefault(0),
                            };
                            report.currentBitOffset += element.reportSizeInBits;
                            elements.Add(element);
                        }
                        reports[reportIndex] = report;

                        HIDItemStateLocal.Reset(ref localItemState);
                        break;
                }

                if (itemSize == 3)
                    currentPtr += 4;
                else
                    currentPtr += itemSize;
            }

            deviceDescriptor.elements = elements.ToArray();
            deviceDescriptor.collections = collections.ToArray();

            // Set usage and usage page on device descriptor to what's
            // on the toplevel application collection.
            foreach (var collection in collections)
            {
                if (collection.parent == -1 && collection.type == HID.HIDCollectionType.Application)
                {
                    deviceDescriptor.usage = collection.usage;
                    deviceDescriptor.usagePage = collection.usagePage;
                    break;
                }
            }

            return true;
        }

        private unsafe static int ReadData(int itemSize, byte* currentPtr, byte* endPtr)
        {
            if (itemSize == 0)
                return 0;

            // Read byte.
            if (itemSize == 1)
            {
                if (currentPtr >= endPtr)
                    return 0;
                return *currentPtr;
            }

            // Read short.
            if (itemSize == 2)
            {
                if (currentPtr + 2 >= endPtr)
                    return 0;
                var data1 = *currentPtr;
                var data2 = *(currentPtr + 1);
                return (data2 << 8) | data1;
            }

            // Read int.
            if (itemSize == 3) // Item size 3 means 4 bytes!
            {
                if (currentPtr + 4 >= endPtr)
                    return 0;

                var data1 = *currentPtr;
                var data2 = *(currentPtr + 1);
                var data3 = *(currentPtr + 2);
                var data4 = *(currentPtr + 3);

                return (data4 << 24) | (data3 << 24) | (data2 << 8) | data1;
            }

            Debug.Assert(false, "Should not reach here");
            return 0;
        }

        private struct HIDReportData
        {
            public int reportId;
            public HID.HIDReportType reportType;
            public int currentBitOffset;

            public static int FindOrAddReport(int? reportId, HID.HIDReportType reportType, List<HIDReportData> reports)
            {
                var id = 1;
                if (reportId.HasValue)
                    id = reportId.Value;

                for (var i = 0; i < reports.Count; ++i)
                {
                    if (reports[i].reportId == id && reports[i].reportType == reportType)
                        return i;
                }

                reports.Add(new HIDReportData
                {
                    reportId = id,
                    reportType = reportType
                });

                return reports.Count - 1;
            }
        }

        // All types and tags with size bits (low order two bits) masked out (i.e. being 0).
        private enum HIDItemTypeAndTag
        {
            Input = 0x80,
            Output = 0x90,
            Feature = 0xB0,
            Collection = 0xA0,
            EndCollection = 0xC0,
            UsagePage = 0x04,
            LogicalMinimum = 0x14,
            LogicalMaximum = 0x24,
            PhysicalMinimum = 0x34,
            PhysicalMaximum = 0x44,
            UnitExponent = 0x54,
            Unit = 0x64,
            ReportSize = 0x74,
            ReportID = 0x84,
            ReportCount = 0x94,
            Push = 0xA4,
            Pop = 0xB4,
            Usage = 0x08,
            UsageMinimum = 0x18,
            UsageMaximum = 0x28,
            DesignatorIndex = 0x38,
            DesignatorMinimum = 0x48,
            DesignatorMaximum = 0x58,
            StringIndex = 0x78,
            StringMinimum = 0x88,
            StringMaximum = 0x98,
            Delimiter = 0xA8,
        }

        // State that needs to be defined for each main item separately.
        // See section 6.2.2.8
        private struct HIDItemStateLocal
        {
            public int? usage;
            public int? usageMinimum;
            public int? usageMaximum;
            public int? designatorIndex;
            public int? designatorMinimum;
            public int? designatorMaximum;
            public int? stringIndex;
            public int? stringMinimum;
            public int? stringMaximum;

            public List<int> usageList;

            // Wipe state but preserve usageList allocation.
            public static void Reset(ref HIDItemStateLocal state)
            {
                var usageList = state.usageList;
                state = new HIDItemStateLocal();
                if (usageList != null)
                {
                    usageList.Clear();
                    state.usageList = usageList;
                }
            }

            // Usage can be set repeatedly to provide an enumeration of usages.
            public void SetUsage(int value)
            {
                if (usage.HasValue)
                {
                    if (usageList == null)
                        usageList = new List<int>();
                    usageList.Add(usage.Value);
                }
                usage = value;
            }

            // Get usage for Nth element in [0-reportCount] list.
            public int GetUsage(int index)
            {
                // If we have minimum and maximum usage, interpolate between that.
                if (usageMinimum.HasValue && usageMaximum.HasValue)
                {
                    var min = usageMinimum.Value;
                    var max = usageMaximum.Value;

                    var range = max - min;
                    if (range < 0)
                        return 0;
                    if (index >= range)
                        return max;
                    return min + index;
                }

                // If we have a list of usages, index into that.
                if (usageList != null && usageList.Count > 0)
                {
                    var usageCount = usageList.Count;
                    if (index >= usageCount)
                        return usage.Value;

                    return usageList[index];
                }

                if (usage.HasValue)
                    return usage.Value;

                ////TODO: min/max

                return 0;
            }
        }

        // State that is carried over from main item to main item.
        // See section 6.2.2.7
        private struct HIDItemStateGlobal
        {
            public int? usagePage;
            public int? logicalMinimum;
            public int? logicalMaximum;
            public int? physicalMinimum;
            public int? physicalMaximum;
            public int? unitExponent;
            public int? unit;
            public int? reportSize;
            public int? reportCount;
            public int? reportId;

            public HID.UsagePage GetUsagePage(int index, ref HIDItemStateLocal localItemState)
            {
                if (!usagePage.HasValue)
                {
                    var usage = localItemState.GetUsage(index);
                    return (HID.UsagePage)(usage >> 16);
                }

                return (HID.UsagePage)usagePage.Value;
            }

            public int GetPhysicalMin()
            {
                if (physicalMinimum == null || physicalMaximum == null ||
                    (physicalMinimum.Value == 0 && physicalMaximum.Value == 0))
                    return logicalMinimum.GetValueOrDefault(0);
                return physicalMinimum.Value;
            }

            public int GetPhysicalMax()
            {
                if (physicalMinimum == null || physicalMaximum == null ||
                    (physicalMinimum.Value == 0 && physicalMaximum.Value == 0))
                    return logicalMaximum.GetValueOrDefault(0);
                return physicalMaximum.Value;
            }
        }
    }
}
