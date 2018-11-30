#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputControlPickerDropdown : AdvancedDropdown
    {
        SerializedProperty m_PathProperty;
        Action<SerializedProperty> m_OnPickCallback;
        string[] m_DeviceFilter;
        Type m_ExpectedControlLayoutFilterType;

        public InputControlPickerDropdown(AdvancedDropdownState state, SerializedProperty pathProperty, Action<SerializedProperty> onPickCallback)
            : base(state)
        {
            m_Gui = new InputControlPickerGUI();
            minimumSize = new Vector2(350, 250);
            maximumSize = new Vector2(0, 300);
            m_PathProperty = pathProperty;
            m_OnPickCallback = onPickCallback;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("");

            var usages = BuildTreeForUsages();
            if (usages.children.Any())
                root.AddChild(usages);
            var devices = BuildTreeForAbstractDevices();
            if (devices.children.Any())
                root.AddChild(devices);
            var products = BuildTreeForSpecificDevices();
            if (products.children.Any())
                root.AddChild(products);

            if (m_DeviceFilter != null && m_DeviceFilter.Length > 0)
            {
                var newRoot = new AdvancedDropdownItem("");
                FindDevice(newRoot, root, m_DeviceFilter);
                if (newRoot.children.Count() == 1)
                {
                    return newRoot.children.ElementAt(0);
                }
                return newRoot;
            }

            return root;
        }

        void FindDevice(AdvancedDropdownItem newRoot, AdvancedDropdownItem root, string[] deviceFilter)
        {
            foreach (var child in root.children)
            {
                var deviceItem = child as DeviceTreeViewItem;
                if (child is DeviceTreeViewItem)
                {
                    if (deviceFilter.Contains(deviceItem.controlPathWithDevice))
                    {
                        newRoot.AddChild(deviceItem);
                    }
                }
                if (child.children.Any())
                {
                    FindDevice(newRoot, child, deviceFilter);
                }
            }
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            m_PathProperty.stringValue = ((InputControlTreeViewItem)item).controlPathWithDevice;
            m_OnPickCallback(m_PathProperty);
        }

        private AdvancedDropdownItem BuildTreeForUsages()
        {
            var usageRoot = new AdvancedDropdownItem("Usages");
            foreach (var usage in EditorInputControlLayoutCache.allUsages)
            {
                var child = new UsageTreeViewItem(usage);
                if (usage.Value.Any(LayoutMatchesExpectedControlLayoutFilter))
                {
                    usageRoot.AddChild(child);
                }
            }
            return usageRoot;
        }

        private AdvancedDropdownItem BuildTreeForAbstractDevices()
        {
            var mainGroup = new AdvancedDropdownItem("Abstract Devices");
            foreach (var deviceLayout in EditorInputControlLayoutCache.allDeviceLayouts.OrderBy(a => a.name))
                AddDeviceTreeItem(deviceLayout, mainGroup);
            return mainGroup;
        }

        private AdvancedDropdownItem BuildTreeForSpecificDevices()
        {
            var mainGroup = new AdvancedDropdownItem("Specific Devices");
            foreach (var layout in EditorInputControlLayoutCache.allProductLayouts.OrderBy(a => a.name))
            {
                var rootLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                if (string.IsNullOrEmpty(rootLayoutName))
                    rootLayoutName = "Other";
                else
                    rootLayoutName = rootLayoutName.GetPlural();

                var rootLayoutGroup = mainGroup.children.Any()
                    ? mainGroup.children.FirstOrDefault(x => x.name == rootLayoutName)
                    : null;
                if (rootLayoutGroup == null)
                {
                    rootLayoutGroup = new DeviceTreeViewItem(layout)
                    {
                        name = rootLayoutName,
                        id = rootLayoutName.GetHashCode(),
                    };
                }

                AddDeviceTreeItem(layout, rootLayoutGroup);

                if (rootLayoutGroup.children.Any() && !mainGroup.children.Contains(rootLayoutGroup))
                    mainGroup.AddChild(rootLayoutGroup);
            }
            return mainGroup;
        }

        private void AddDeviceTreeItem(InputControlLayout layout, AdvancedDropdownItem parent)
        {
            // Ignore devices that have no controls. We're looking at fully merged layouts here so
            // we're also taking inherited controls into account.
            if (layout.controls.Count == 0)
                return;

            var deviceItem = new DeviceTreeViewItem(layout);

            AddControlTreeItemsRecursive(layout, deviceItem, "", layout.name, null);

            if (deviceItem.children.Any())
                parent.AddChild(deviceItem);

            foreach (var commonUsage in layout.commonUsages)
            {
                var commonUsageGroup = new DeviceTreeViewItem(layout, commonUsage);
                AddControlTreeItemsRecursive(layout, commonUsageGroup, "", layout.name, commonUsage);
                if (commonUsageGroup.children.Any())
                    parent.AddChild(commonUsageGroup);
            }
        }

        private void AddControlTreeItemsRecursive(InputControlLayout layout, AdvancedDropdownItem parent, string prefix, string deviceControlId, string commonUsage)
        {
            foreach (var control in layout.controls.OrderBy(a => a.name))
            {
                if (control.isModifyingChildControlByPath)
                    continue;

                // Skip variants except the default variant and variants dictated by the layout itself.
                if (!control.variants.IsEmpty() && control.variants != InputControlLayout.DefaultVariant
                    && (layout.variants.IsEmpty() || !InputControlLayout.VariantsMatch(layout.variants, control.variants)))
                {
                    continue;
                }

                var child = new ControlTreeViewItem(control, prefix, deviceControlId, commonUsage);

                if (LayoutMatchesExpectedControlLayoutFilter(control.layout))
                {
                    parent.AddChild(child);
                }

                var childLayout = EditorInputControlLayoutCache.TryGetLayout(control.layout);
                if (childLayout != null)
                {
                    AddControlTreeItemsRecursive(childLayout, parent, child.controlPath, deviceControlId, commonUsage);
                }
            }

            // Add optional layouts for devices
            var optionalLayouts = EditorInputControlLayoutCache.GetOptionalControlsForLayout(layout.name);
            if (optionalLayouts.Any() && layout.isDeviceLayout)
            {
                var optionalGroup = new AdvancedDropdownItem("Optional");
                foreach (var optionalLayout in optionalLayouts)
                {
                    if (LayoutMatchesExpectedControlLayoutFilter(optionalLayout.layout))
                        optionalGroup.AddChild(new OptionalControlTreeViewItem(optionalLayout, deviceControlId, commonUsage));
                }
                if (optionalGroup.children.Any())
                    parent.AddChild(optionalGroup);
            }
        }

        bool LayoutMatchesExpectedControlLayoutFilter(string layout)
        {
            if (m_ExpectedControlLayoutFilterType == null)
            {
                return true;
            }
            var layoutType = InputSystem.s_Manager.m_Layouts.GetControlTypeForLayout(new InternedString(layout));
            return m_ExpectedControlLayoutFilterType.IsAssignableFrom(layoutType);
        }

        public void SetDeviceFilter(string[] deviceFilter)
        {
            m_DeviceFilter = deviceFilter;
        }

        public void SetExpectedControlLayoutFilter(string expectedControlLayout)
        {
            m_ExpectedControlLayoutFilterType = InputSystem.s_Manager.m_Layouts.GetControlTypeForLayout(new InternedString(expectedControlLayout));
        }
    }
}
#endif // UNITY_EDITOR
