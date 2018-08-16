#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

////TODO: allow restricting to certain types of controls

////TODO: add means to pick specific device index

////TODO: add usages actually used by a layout also to the list of controls of the layout

////TODO: prime picker with currently selected control (also with usage on device)

namespace UnityEngine.Experimental.Input.Editor.InputControlPicker
{
    public class InputControlTree : TreeView
    {
        InputControlPickerPopup m_ParentWindow;
        Action<String> m_OnSelected;

        public InputControlTree(TreeViewState state, InputControlPickerPopup parentWindow, Action<string> onSelected)
            : base(state)
        {
            m_ParentWindow = parentWindow;
            m_OnSelected = onSelected;
            Reload();
        }

        protected override bool DoesItemMatchSearch(TreeViewItem treeViewItem, string search)
        {
            if (treeViewItem.hasChildren)
                return false;
            search = search.ToLower();
            if (treeViewItem.displayName.ToLower().Contains(search))
                return true;
            return false;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (hasSearch)
            {
                var rows = base.BuildRows(root);
                return rows.Cast<InputControlTreeViewItem>().Select(item => item.GetSearchableItem()).OrderBy(a => a.displayName).ToList();
            }
            return base.BuildRows(root);
        }

        protected override void KeyEvent()
        {
            var e = Event.current;

            if (e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.Return && HasSelection())
            {
                DoubleClickedItem(GetSelection().First());
                return;
            }

            if (e.keyCode == KeyCode.UpArrow
                || e.keyCode == KeyCode.DownArrow
                || e.keyCode == KeyCode.LeftArrow
                || e.keyCode == KeyCode.RightArrow)
            {
                return;
            }
            m_ParentWindow.m_SearchField.SetFocus();
            m_ParentWindow.editorWindow.Repaint();
        }
        
        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as InputControlTreeViewItem;
            if (item != null && item.selectable)
            {
                var path = item.controlPathWithDevice;

                if(m_OnSelected!=null)
                    m_OnSelected(path);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                displayName = "Root",
                id = 0,
                depth = -1
            };

            var usages = BuildTreeForUsages();
            root.AddChild(usages);
            var devices = BuildTreeForAbstractDevices();
            root.AddChild(devices);
            var products = BuildTreeForSpecificDevices();
            root.AddChild(products);
            return root;
        }
        
        TreeViewItem BuildTreeForUsages()
        {
            var usageRoot = new TreeViewItem
            {
                displayName = "Usages",
                id = "Usages".GetHashCode(),
                depth = 0
            };

            foreach (var usage in EditorInputControlLayoutCache.allUsages)
            {
                var child = new UsageTreeViewItem(usage);
                usageRoot.AddChild(child);
            }

            return usageRoot;
        }
        
        TreeViewItem BuildTreeForAbstractDevices()
        {
            var mainGroup = new TreeViewItem
            {
                depth = 0,
                displayName = "Abstract Devices",
                id = "Abstract Devices".GetHashCode()
            };
            foreach (var deviceLayout in EditorInputControlLayoutCache.allDeviceLayouts.OrderBy(a=>a.name))
            {
                // Skip layouts that don't have any controls (like the "HID" layout).
                if (deviceLayout.controls.Count == 0)
                    continue;

                var deviceGroup = new DeviceGroupTreeViewItem(deviceLayout);
                mainGroup.AddChild(deviceGroup);
                ParseDeviceLayout(deviceLayout, deviceGroup, "", deviceLayout.name, null);

                foreach (var commonUsage in deviceLayout.commonUsages)
                {
                    var commonUsageGroup = new DeviceGroupTreeViewItem(deviceLayout, commonUsage);
                    mainGroup.AddChild(commonUsageGroup);
                    ParseDeviceLayout(deviceLayout, commonUsageGroup, "", deviceLayout.name, commonUsage);
                }
            }
            return mainGroup;
        }

        TreeViewItem BuildTreeForSpecificDevices()
        {
            var mainGroup = new TreeViewItem
            {
                depth = 0,
                displayName = "Specific Devices",
                id = "Specific Devices".GetHashCode()
            };
            foreach (var layout in EditorInputControlLayoutCache.allProductLayouts.OrderBy(a=>a.name))
            {
                var rootBaseLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                if (string.IsNullOrEmpty(rootBaseLayoutName))
                    rootBaseLayoutName = "Other";
                else
                    rootBaseLayoutName += "s";

                var rootBaseGroup = mainGroup.hasChildren
                    ? mainGroup.children.FirstOrDefault(x => x.displayName == rootBaseLayoutName)
                    : null;
                if (rootBaseGroup == null)
                {
                    rootBaseGroup = new TreeViewItem
                    {
                        depth = mainGroup.depth + 1,
                        displayName = rootBaseLayoutName,
                        id = rootBaseLayoutName.GetHashCode()
                    };
                    mainGroup.AddChild(rootBaseGroup);
                }

                var deviceGroup = new DeviceGroupTreeViewItem(layout)
                {
                    depth = rootBaseGroup.depth + 1
                };
                rootBaseGroup.AddChild(deviceGroup);

                ParseDeviceLayout(layout, deviceGroup, "", layout.name, null);
                
                foreach (var commonUsage in layout.commonUsages)
                {
                    var commonUsageGroup = new DeviceGroupTreeViewItem(layout, commonUsage)
                    {
                        depth = rootBaseGroup.depth + 1
                    };
                    rootBaseGroup.AddChild(commonUsageGroup);
                    ParseDeviceLayout(layout, commonUsageGroup, "", layout.name, commonUsage);
                }

            }
            return mainGroup;
        }

        void ParseDeviceLayout(InputControlLayout layout, TreeViewItem parent, string prefix, string deviceControlId, string commonUsage)
        {
            foreach (var control in layout.controls.OrderBy(a=>a.name))
            {
                if (control.isModifyingChildControlByPath)
                    continue;

                // Skip variants.
                if (!string.IsNullOrEmpty(control.variants) && control.variants.ToLower() != "default")
                    continue;

                var child = new ControlTreeViewItem(control, prefix, deviceControlId, commonUsage)
                {
                    depth = parent.depth + 1,
                };
                parent.AddChild(child);
                
                var childLayout = EditorInputControlLayoutCache.TryGetLayout(control.layout);
                if (childLayout != null)
                {
                    ParseDeviceLayout(childLayout, parent, child.controlPath, deviceControlId, commonUsage);
                }
                                
            }
        }
    }
}
#endif // UNITY_EDITOR
