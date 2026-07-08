#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

////TODO: use two columns for treeview and separate name and value

namespace UnityEngine.InputSystem.HID.Editor
{
    /// <summary>
    /// A window that dumps a raw HID descriptor in a tree view.
    /// </summary>
    /// <remarks>
    /// Not specific to InputDevices of type <see cref="HID"/> so that it can work with
    /// any <see cref="InputDevice"/> created for a device using the "HID" interface.
    /// </remarks>
    internal class HIDDescriptorWindow : EditorWindow, ISerializationCallbackReceiver
    {
        public static void CreateOrShowExisting(int deviceId, InputDeviceDescription deviceDescription)
        {
            // See if we have an existing window for the device and if so pop it
            // in front.
            if (s_OpenWindows != null)
            {
                for (var i = 0; i < s_OpenWindows.Count; ++i)
                {
                    var existingWindow = s_OpenWindows[i];
                    if (existingWindow.m_DeviceId == deviceId)
                    {
                        existingWindow.Show();
                        existingWindow.Focus();
                        return;
                    }
                }
            }

            // No, so create a new one.
            var window = CreateInstance<HIDDescriptorWindow>();
            window.InitializeWith(deviceId, deviceDescription);
            window.minSize = new Vector2(270, 200);
            window.Show();
            window.titleContent = new GUIContent("HID Descriptor");
        }

        public void Awake()
        {
            AddToList();
        }

        public void OnDestroy()
        {
            RemoveFromList();
        }

        public void OnGUI()
        {
            if (!m_Initialized)
                InitializeWith(m_DeviceId, m_DeviceDescription);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(m_Label, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(rect);
        }

        private void InitializeWith(int deviceId, InputDeviceDescription deviceDescription)
        {
            m_DeviceId = deviceId;
            m_DeviceDescription = deviceDescription;
            m_Initialized = true;

            // Set up tree view for HID descriptor.
            var hidDescriptor = HID.ReadHIDDeviceDescriptor(ref m_DeviceDescription,
                (ref InputDeviceCommand command) => InputRuntime.s_Instance.DeviceCommand(m_DeviceId, ref command));
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new HIDDescriptorTreeView(m_TreeViewState, hidDescriptor);
            m_TreeView.SetExpanded(1, true);

            m_Label = new GUIContent(
                $"HID Descriptor for '{deviceDescription.manufacturer} {deviceDescription.product}'");
        }

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private HIDDescriptorTreeView m_TreeView;
        [NonSerialized] private GUIContent m_Label;

        [SerializeField] private int m_DeviceId;
        [SerializeField] private InputDeviceDescription m_DeviceDescription;
        [SerializeField] private TreeViewState m_TreeViewState;

        private void AddToList()
        {
            if (s_OpenWindows == null)
                s_OpenWindows = new List<HIDDescriptorWindow>();
            if (!s_OpenWindows.Contains(this))
                s_OpenWindows.Add(this);
        }

        private void RemoveFromList()
        {
            if (s_OpenWindows != null)
                s_OpenWindows.Remove(this);
        }

        private static List<HIDDescriptorWindow> s_OpenWindows;

        private class HIDDescriptorTreeView : TreeView
        {
            private HID.HIDDeviceDescriptor m_Descriptor;

            public HIDDescriptorTreeView(TreeViewState state, HID.HIDDeviceDescriptor descriptor)
                : base(state)
            {
                m_Descriptor = descriptor;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;

                var root = new TreeViewItem
                {
                    id = id++,
                    depth = -1
                };

                var item = BuildDeviceItem(m_Descriptor, ref id);
                root.AddChild(item);

                return root;
            }

            private TreeViewItem BuildDeviceItem(HID.HIDDeviceDescriptor device, ref int id)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = "Device"
                };

                AddChild(item, string.Format("Vendor ID: 0x{0:X}", device.vendorId), ref id);
                AddChild(item, string.Format("Product ID: 0x{0:X}", device.productId), ref id);
                AddChild(item, string.Format("Usage Page: 0x{0:X} ({1})", (uint)device.usagePage, device.usagePage), ref id);
                AddChild(item, string.Format("Usage: 0x{0:X}", device.usage), ref id);
                AddChild(item, "Input Report Size: " + device.inputReportSize, ref id);
                AddChild(item, "Output Report Size: " + device.outputReportSize, ref id);
                AddChild(item, "Feature Report Size: " + device.featureReportSize, ref id);

                // Elements.
                if (device.elements != null)
                {
                    var elementCount = device.elements.Length;
                    var elements = AddChild(item, elementCount + " Elements", ref id);
                    for (var i = 0; i < elementCount; ++i)
                        BuildElementItem(i, elements, device.elements[i], ref id);
                }
                else
                    AddChild(item, "0 Elements", ref id);

                ////TODO: collections

                return item;
            }

            private TreeViewItem BuildElementItem(int index, TreeViewItem parent, HID.HIDElementDescriptor element, ref int id)
            {
                var item = AddChild(parent, string.Format("Element {0} ({1})", index, element.reportType), ref id);

                string usagePageString = HID.UsagePageToString(element.usagePage);
                string usageString = HID.UsageToString(element.usagePage, element.usage);

                AddChild(item, string.Format("Usage Page: 0x{0:X} ({1})", (uint)element.usagePage, usagePageString), ref id);
                if (usageString != null)
                    AddChild(item, string.Format("Usage: 0x{0:X} ({1})", element.usage, usageString), ref id);
                else
                    AddChild(item, string.Format("Usage: 0x{0:X}", element.usage), ref id);

                AddChild(item, "Report Type: " + element.reportType, ref id);
                AddChild(item, "Report ID: " + element.reportId, ref id);
                AddChild(item, "Report Size in Bits: " + element.reportSizeInBits, ref id);
                AddChild(item, "Report Bit Offset: " + element.reportOffsetInBits, ref id);
                AddChild(item, "Collection Index: " + element.collectionIndex, ref id);
                AddChild(item, string.Format("Unit: {0:X}", element.unit), ref id);
                AddChild(item, string.Format("Unit Exponent: {0:X}", element.unitExponent), ref id);
                AddChild(item, "Logical Min: " + element.logicalMin, ref id);
                AddChild(item, "Logical Max: " + element.logicalMax, ref id);
                AddChild(item, "Physical Min: " + element.physicalMin, ref id);
                AddChild(item, "Physical Max: " + element.physicalMax, ref id);
                AddChild(item, "Has Null State?: " + element.hasNullState, ref id);
                AddChild(item, "Has Preferred State?: " + element.hasPreferredState, ref id);
                AddChild(item, "Is Array?: " + element.isArray, ref id);
                AddChild(item, "Is Non-Linear?: " + element.isNonLinear, ref id);
                AddChild(item, "Is Relative?: " + element.isRelative, ref id);
                AddChild(item, "Is Constant?: " + element.isConstant, ref id);
                AddChild(item, "Is Wrapping?: " + element.isWrapping, ref id);

                return item;
            }

            private TreeViewItem AddChild(TreeViewItem parent, string displayName, ref int id)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = displayName
                };

                parent.AddChild(item);

                return item;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            AddToList();
        }
    }
}
#endif // UNITY_EDITOR
