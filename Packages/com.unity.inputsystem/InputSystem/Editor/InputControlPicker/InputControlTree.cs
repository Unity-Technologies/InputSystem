#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////TODO: allow restricting to certain types of controls

////TODO: add means to pick specific device index

////TODO: add usages actually used by a layout also to the list of controls of the layout

////TODO: prime picker with currently selected control (also with usage on device)

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputControlTree : TreeView
    {
        private InputControlPickerPopup m_ParentWindow;
        private Action<string> m_OnSelected;
        private string[] m_DeviceFilter;

        public InputControlTree(TreeViewState state, InputControlPickerPopup parentWindow, Action<string> onSelected, string[] deviceFilter)
            : base(state)
        {
            m_ParentWindow = parentWindow;
            m_OnSelected = onSelected;
            m_DeviceFilter = deviceFilter;
            Reload();
        }

        protected override bool DoesItemMatchSearch(TreeViewItem treeViewItem, string search)
        {
            ////REVIEW: why are we not ending up with the searchable tree view items when TreeView calls us here?
            var item = treeViewItem as InputControlTreeViewItem;
            if (item == null || !item.selectable)
                return false;

            var searchableItem = item.GetSearchableItem();

            // Break up search into multiple tokens if there's whitespace.
            var hasWhitespace = search.Any(char.IsWhiteSpace);
            if (hasWhitespace)
            {
                var searchElements = search.Split(char.IsWhiteSpace);
                return searchElements.All(element => searchableItem.displayName.ToLower().Contains(element.ToLower()));
            }

            if (searchableItem.displayName.ToLower().Contains(search.ToLower()))
                return true;
            return false;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (hasSearch)
            {
                var rows = base.BuildRows(root);
                ////TODO: order such that each device appears as a single block with all matches controls
                var result = rows.Cast<InputControlTreeViewItem>().Where(x => x.selectable)
                    .Select(x => x.GetSearchableItem())
                    .OrderBy(a => a.displayName).ToList();

                return result;
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

                if (m_OnSelected != null)
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

            if (m_DeviceFilter != null && m_DeviceFilter.Length > 0)
            {
                var newRoot = new TreeViewItem
                {
                    displayName = "Root",
                    id = 0,
                    depth = -1
                };
                FindDevice(newRoot, root, m_DeviceFilter);
                newRoot.children.ForEach(a =>
                {
                    a.depth = 0;
                    a.children.ForEach(b => b.depth = 1);
                });
                if (newRoot.children.Count == 1)
                {
                    SetExpanded(newRoot.children[0].id, true);
                }
                return newRoot;
            }

            return root;
        }

        void FindDevice(TreeViewItem newRoot, TreeViewItem root, string[] deviceFilter)
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
                if (child.hasChildren)
                {
                    FindDevice(newRoot, child, deviceFilter);
                }
            }
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
            foreach (var deviceLayout in EditorInputControlLayoutCache.allDeviceLayouts.OrderBy(a => a.name))
                AddDeviceTreeItem(deviceLayout, mainGroup);
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
            foreach (var layout in EditorInputControlLayoutCache.allProductLayouts.OrderBy(a => a.name))
            {
                var rootLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                if (string.IsNullOrEmpty(rootLayoutName))
                    rootLayoutName = "Other";
                else
                    rootLayoutName = rootLayoutName.GetPlural();

                var rootLayoutGroup = mainGroup.hasChildren
                    ? mainGroup.children.FirstOrDefault(x => x.displayName == rootLayoutName)
                    : null;
                if (rootLayoutGroup == null)
                {
                    rootLayoutGroup = new TreeViewItem
                    {
                        depth = mainGroup.depth + 1,
                        displayName = rootLayoutName,
                        id = rootLayoutName.GetHashCode(),
                    };
                    mainGroup.AddChild(rootLayoutGroup);
                }

                AddDeviceTreeItem(layout, rootLayoutGroup);
            }
            return mainGroup;
        }

        private static void AddDeviceTreeItem(InputControlLayout layout, TreeViewItem parent)
        {
            // Ignore devices that have no controls. We're looking at fully merged layouts here so
            // we're also taking inherited controls into account.
            if (layout.controls.Count == 0)
                return;

            var deviceItem = new DeviceTreeViewItem(layout)
            {
                depth = parent.depth + 1
            };

            AddControlTreeItemsRecursive(layout, deviceItem, "", layout.name, null);

            parent.AddChild(deviceItem);

            foreach (var commonUsage in layout.commonUsages)
            {
                var commonUsageGroup = new DeviceTreeViewItem(layout, commonUsage)
                {
                    depth = parent.depth + 1
                };
                parent.AddChild(commonUsageGroup);
                AddControlTreeItemsRecursive(layout, commonUsageGroup, "", layout.name, commonUsage);
            }
        }

        private static void AddControlTreeItemsRecursive(InputControlLayout layout, TreeViewItem parent, string prefix, string deviceControlId, string commonUsage)
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

                var child = new ControlTreeViewItem(control, prefix, deviceControlId, commonUsage)
                {
                    depth = parent.depth + 1,
                };
                parent.AddChild(child);

                var childLayout = EditorInputControlLayoutCache.TryGetLayout(control.layout);
                if (childLayout != null)
                {
                    AddControlTreeItemsRecursive(childLayout, parent, child.controlPath, deviceControlId, commonUsage);
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
