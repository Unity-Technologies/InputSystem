#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

////FIXME: preserve tree view state properly; currently resets whenever the picker is opened

////TODO: allow restricting to certain types of controls

////TODO: add means to pick specific device index

////TODO: add usages actually used by a layout also to the list of controls of the layout

////TODO: prime picker with currently selected control (also with usage on device)

////TODO: sort properly when search is active
////      (the logic we inherit from TreeView sorts by displayName of items but our rendering logic
////      tags additional text onto the items so the end result appears sorted incorrectly)

namespace UnityEngine.Experimental.Input.Editor
{
    // Popup window that allows selecting controls to target in a binding. Will generate
    // a path string as a result and store it in the given "path" property.
    //
    // At the moment, the interface is pretty simplistic. You can either select a usage
    // or select a specific control on a specific base device layout.
    //
    // Usages are discovered from all layouts that are registered with the system.
    public class InputControlPicker : PopupWindowContent
    {
        public Action<SerializedProperty> onPickCallback;
        public float width;
        SearchField m_SearchField;

        public InputControlPicker(SerializedProperty pathProperty, TreeViewState treeViewState = null)
        {
            if (pathProperty == null)
                throw new ArgumentNullException("pathProperty");
            m_PathProperty = pathProperty;
            m_PathTreeState = treeViewState ?? new TreeViewState();

            m_SearchField = new SearchField();
            m_SearchField.SetFocus();
            m_SearchField.downOrUpArrowKeyPressed += OnDownOrUpArrowKeyPressed;
        }

        void OnDownOrUpArrowKeyPressed()
        {
            m_PathTree.SetFocusAndEnsureSelectedItem();
        }

        public override Vector2 GetWindowSize()
        {
            var s = base.GetWindowSize();
            if (width > s.x)
                s.x = width;
            return s;
        }

        public override void OnGUI(Rect rect)
        {
            if (m_PathTree == null)
            {
                m_PathTree = new PathTreeView(m_PathTreeState, this);
            }

            DrawToolbar();

            var toolbarRect = GUILayoutUtility.GetLastRect();
            var listRect = new Rect(rect.x, rect.y + toolbarRect.height, rect.width, rect.height - toolbarRect.height);

            m_PathTree.OnGUI(listRect);
            m_FirstRenderCompleted = true;
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(75), GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.toolbarSearchField, GUILayout.MinWidth(70));
            m_PathTree.searchString = m_SearchField.OnToolbarGUI(searchRect, m_PathTree.searchString);
            GUILayout.EndHorizontal();
        }

        private SerializedProperty m_PathProperty;
        private PathTreeView m_PathTree;
        private TreeViewState m_PathTreeState;
        private bool m_FirstRenderCompleted;

        private static class Styles
        {
            public static GUIStyle toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
            public static GUIStyle toolbarSearchFieldCancel = new GUIStyle("ToolbarSeachCancelButton");
            public static GUIStyle toolbarSearchFieldCancelEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty");
        }

        private class PathTreeView : TreeView
        {
            private InputControlPicker m_Parent;

            private const int kUsagePopupWidth = 75;
            private static GUIContent s_NoUsage = new GUIContent("<Any>");

            private class Item : TreeViewItem
            {
                public string usage;
                public string device;
                public string controlPath;
                public InputControlLayout layout;
                public GUIContent[] popupOptions;
                public int[] popupValues;
                public int selectedPopupOption;
            }

            public PathTreeView(TreeViewState state, InputControlPicker parent)
                : base(state)
            {
                m_Parent = parent;
                Reload();
            }

            protected override bool DoesItemMatchSearch(TreeViewItem treeViewItem, string search)
            {
                if (treeViewItem.hasChildren)
                    return false;
                var item = (Item)treeViewItem;
                search = search.ToLower();
                if (item.device != null && item.device.ToLower().Contains(search))
                    return true;
                if (item.controlPath != null && item.controlPath.ToLower().Contains(search))
                    return true;
                return false;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = args.item as Item;

                // If we're searching ATM, display the full path of controls. Otherwise it's confusing to see
                // two "leftButton" controls show up in the list and now know where they are coming from.
                if (hasSearch && item != null && item.controlPath != null)
                {
                    var indent = GetContentIndent(item);
                    var rect = args.rowRect;
                    rect.x += indent;
                    rect.width -= indent;
                    EditorGUI.LabelField(rect, string.Format("{0}/{1}", item.device, item.controlPath));
                    return;
                }

                // If the item is a device and it has usages associated with it, show a popup.
                if (item != null
                    && item.device != null
                    && item.controlPath == null
                    && item.layout.commonUsages.Count > 0)
                {
                    // On first render, create popup options.
                    if (item.popupOptions == null)
                    {
                        var usageCount = item.layout.commonUsages.Count;
                        var options = new GUIContent[usageCount + 1];
                        var values = new int[usageCount + 1];

                        options[0] = s_NoUsage;
                        values[0] = 0;

                        for (var i = 0; i < usageCount; ++i)
                        {
                            options[i + 1] = new GUIContent(item.layout.commonUsages[i].ToString());
                            values[i + 1] = i + 1;
                        }

                        item.popupOptions = options;
                        item.popupValues = values;
                    }

                    // Show popup.
                    var rect = args.rowRect;
                    rect.x = rect.x + rect.width - kUsagePopupWidth - 2;
                    rect.width = kUsagePopupWidth;
                    item.selectedPopupOption = EditorGUI.IntPopup(rect, item.selectedPopupOption, item.popupOptions,
                        item.popupValues, EditorStyles.miniButton);
                }

                base.RowGUI(args);
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
                m_Parent.m_SearchField.SetFocus();
                m_Parent.editorWindow.Repaint();
            }

            // When an item is double-clicked, form a path from the item and store it
            // in the path property. Then close the popup window.
            protected override void DoubleClickedItem(int id)
            {
                var item = FindItem(id, rootItem) as Item;
                if (item != null)
                {
                    string path = null;
                    if (item.usage != null)
                        path = string.Format("*/{{{0}}}", item.usage);
                    else
                    {
                        // See if usage is set on device.
                        var deviceUsage = "";
                        var deviceItem = item;
                        if (item.controlPath != null)
                            deviceItem = item.parent as Item;
                        if (deviceItem != null && deviceItem.selectedPopupOption != 0)
                        {
                            deviceUsage = string.Format("{{{0}}}",
                                deviceItem.layout.commonUsages[deviceItem.selectedPopupOption - 1]);
                        }

                        if (item.controlPath != null)
                            path = string.Format("<{0}>{1}/{2}", item.device, deviceUsage, item.controlPath);
                        else if (item.device != null)
                            path = string.Format("<{0}>{1}", item.device, deviceUsage);
                    }

                    if (path != null)
                    {
                        m_Parent.m_PathProperty.stringValue = path;
                        m_Parent.m_PathProperty.serializedObject.ApplyModifiedProperties();

                        if (m_Parent.onPickCallback != null)
                            m_Parent.onPickCallback(m_Parent.m_PathProperty);
                    }
                }

                m_Parent.editorWindow.Close();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem
                {
                    displayName = "Root",
                    id = 0,
                    depth = -1
                };

                var id = 1;
                var usages = BuildTreeForUsages(ref id);
                var devices = AddChild(root, "Abstract Devices", ref id);
                var products = AddChild(root, "Specific Devices", ref id);

                foreach (var layout in EditorInputControlLayoutCache.allDeviceLayouts)
                {
                    // Skip layouts that don't have any controls (like the "HID" layout).
                    if (layout.controls.Count == 0)
                        continue;

                    BuildTreeForDevice(layout, devices, ref id);
                }
                foreach (var layout in EditorInputControlLayoutCache.allProductLayouts)
                {
                    var rootBaseLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                    if (string.IsNullOrEmpty(rootBaseLayoutName))
                        rootBaseLayoutName = "Other";
                    else
                        rootBaseLayoutName += "s";

                    var group = products.children != null
                        ? products.children.FirstOrDefault(x => x.displayName == rootBaseLayoutName)
                        : null;
                    if (group == null)
                        group = AddChild(products, rootBaseLayoutName, ref id);

                    BuildTreeForDevice(layout, group, ref id);
                }

                if (devices.children != null)
                    devices.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                if (products.children != null)
                {
                    products.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                    foreach (var productGroup in products.children)
                        productGroup.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }

                root.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                root.children.Insert(0, usages);

                return root;
            }

            private TreeViewItem BuildTreeForUsages(ref int id)
            {
                var usageRoot = new TreeViewItem
                {
                    displayName = "Usages",
                    id = id++,
                    depth = 0
                };

                foreach (var usage in EditorInputControlLayoutCache.allUsages)
                {
                    var child = new Item
                    {
                        id = id++,
                        depth = 1,
                        displayName = usage.Key,
                        usage = usage.Key
                    };

                    usageRoot.AddChild(child);
                }

                return usageRoot;
            }

            private TreeViewItem BuildTreeForDevice(InputControlLayout layout, TreeViewItem parent, ref int id)
            {
                var deviceRoot = new Item
                {
                    displayName = layout.name,
                    id = id++,
                    depth = parent.depth + 1,
                    device = layout.name,
                    layout = layout
                };
                parent.AddChild(deviceRoot);

                BuildControlsRecursive(deviceRoot, layout, string.Empty, ref id);

                return deviceRoot;
            }

            private void BuildControlsRecursive(Item parent, InputControlLayout layout, string prefix, ref int id)
            {
                foreach (var control in layout.controls)
                {
                    if (control.isModifyingChildControlByPath)
                        continue;

                    // Skip variants.
                    if (!string.IsNullOrEmpty(control.variants) && control.variants.ToLower() != "default")
                        continue;

                    var controlPath = prefix + control.name;
                    var child = new Item
                    {
                        id = id++,
                        depth = parent.depth + 1,
                        displayName = controlPath,
                        device = parent.layout.name,
                        controlPath = controlPath,
                        layout = layout
                    };

                    var childLayout = EditorInputControlLayoutCache.TryGetLayout(control.layout);
                    if (childLayout != null)
                    {
                        BuildControlsRecursive(parent, childLayout, controlPath + "/", ref id);
                    }

                    parent.AddChild(child);
                }

                if (parent.children != null)
                    parent.children.Sort((a, b) =>
                        string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
            }
        }

        private static TreeViewItem AddChild(TreeViewItem parent, string displayName, ref int id)
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
}
#endif // UNITY_EDITOR
