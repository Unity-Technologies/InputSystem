#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////TODO: find better way to present controls when filtering to specific devices

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputControlPickerDropdown : AdvancedDropdown
    {
        private Action<string> m_OnPickCallback;
        private string[] m_DeviceFilter;
        private Type m_ExpectedControlLayoutFilterType;
        private Mode m_Mode;

        public InputControlPickerDropdown(AdvancedDropdownState state, Action<string> onPickCallback, Mode mode = Mode.PickControl)
            : base(state)
        {
            m_Gui = new InputControlPickerGUI();
            minimumSize = new Vector2(350, 250);
            maximumSize = new Vector2(0, 300);
            m_OnPickCallback = onPickCallback;
            m_Mode = mode;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("");

            if (m_Mode != Mode.PickDevice)
            {
                var usages = BuildTreeForUsages();
                if (usages.children.Any())
                    root.AddChild(usages);
            }

            var devices = BuildTreeForAbstractDevices();
            if (devices.children.Any())
                root.AddChild(devices);
            var products = BuildTreeForSpecificDevices();
            if (products.children.Any())
                root.AddChild(products);

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var path = ((InputControlTreeViewItem)item).controlPathWithDevice;
            m_OnPickCallback(path);
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
            // EXCEPTION: We're okay with empty devices if we're picking devices and not controls.
            if (layout.controls.Count == 0 && m_Mode != Mode.PickDevice)
                return;

            var deviceItem = new DeviceTreeViewItem(layout);

            // If we have a device filter, see if we should ignore the device.
            if (m_DeviceFilter != null)
            {
                var matchesAnyInDeviceFilter = false;
                foreach (var entry in m_DeviceFilter)
                {
                    if (entry == layout.name ||
                        InputControlLayout.s_Layouts.IsBasedOn(new InternedString(entry), layout.name))
                    {
                        matchesAnyInDeviceFilter = true;
                    }
                    else
                    {
                        ////FIXME: this also needs to work for full control paths and not just stuff like "<Gamepad>"
                        var expectedLayout = InputControlPath.TryGetDeviceLayout(entry);
                        if (!string.IsNullOrEmpty(expectedLayout) &&
                            (expectedLayout == layout.name ||
                             InputControlLayout.s_Layouts.IsBasedOn(new InternedString(expectedLayout), layout.name)))
                        {
                            matchesAnyInDeviceFilter = true;
                        }
                    }
                }

                if (!matchesAnyInDeviceFilter)
                    return;
            }

            if (m_Mode != Mode.PickDevice)
                AddControlTreeItemsRecursive(layout, deviceItem, "", layout.name, null);

            if (deviceItem.children.Any() || m_Mode == Mode.PickDevice)
                parent.AddChild(deviceItem);

            if (m_Mode != Mode.PickDevice)
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

        private bool LayoutMatchesExpectedControlLayoutFilter(string layout)
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

        public enum Mode
        {
            PickControl,
            PickDevice,
        }
    }
}
#endif // UNITY_EDITOR
