#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

////TODO: add means to pick specific device index or device role
////      (toolbar button with a dropdown menu with "By Index" and "By Role" entries?)

namespace ISX.Editor
{
    // Popup window that allows selecting controls to target in a binding. Will generate
    // a path string as a result and store it in the given "path" property.
    //
    // At the moment, the interface is pretty simplistic. You can either select a usage
    // or select a specific control on a specific base device template.
    //
    // Usages are discovered from all templates that are registered with the system.
    public class InputControlPicker : PopupWindowContent
    {
        public InputControlPicker(SerializedProperty pathProperty)
        {
            if (pathProperty == null)
                throw new ArgumentNullException("pathProperty");
            m_PathProperty = pathProperty;

            m_PathTreeState = new TreeViewState();
            m_PathTree = new PathTreeView(m_PathTreeState, this);
        }

        public override void OnGUI(Rect rect)
        {
            DrawToolbar();

            var toolbarRect = GUILayoutUtility.GetLastRect();
            var listRect = new Rect(rect.x, rect.y + toolbarRect.height, rect.width, rect.height - toolbarRect.height);

            m_PathTree.OnGUI(listRect);
            m_FirstRenderCompleted = true;
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

            var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.toolbarSearchField, GUILayout.MinWidth(80));
            GUI.SetNextControlName("SearchField");
            m_PathTree.searchString = EditorGUI.TextField(searchRect, m_PathTree.searchString, Styles.toolbarSearchField);
            if (!m_FirstRenderCompleted)
                EditorGUI.FocusTextInControl("SearchField");
            if (GUILayout.Button(
                    GUIContent.none,
                    m_PathTree.searchString == string.Empty ? Styles.toolbarSearchFieldCancelEmpty : Styles.toolbarSearchFieldCancel))
            {
                m_PathTree.searchString = string.Empty;
                EditorGUIUtility.keyboardControl = 0;
            }

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

            private class Item : TreeViewItem
            {
                public string usage;
                public string device;
                public string control;
            }

            public PathTreeView(TreeViewState state, InputControlPicker parent)
                : base(state)
            {
                m_Parent = parent;
                Reload();
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                // If we're searching ATM, display the full path of controls. It's confusing to see
                // two "leftButton" controls show up in the list and now know where they are coming from.
                if (hasSearch)
                {
                    var item = args.item as Item;
                    if (item != null && item.control != null)
                    {
                        var indent = GetContentIndent(item);
                        var rect = args.rowRect;
                        rect.x += indent;
                        rect.width -= indent;
                        EditorGUI.LabelField(rect, string.Format("{0}/{1}", item.device, item.control));
                        return;
                    }
                }

                base.RowGUI(args);
            }

            // When an item is double-clicked, form a path from the item and store it
            // in the path property. Then close the popup window.
            protected override void DoubleClickedItem(int id)
            {
                var item = FindItem(id, rootItem) as Item;
                if (item != null)
                {
                    String path = null;
                    if (item.usage != null)
                        path = string.Format("*/{{{0}}}", item.usage);
                    else if (item.device != null && item.control != null)
                        path = string.Format("<{0}>/{1}", item.device, item.control);
                    else if (item.device != null)
                        path = string.Format("<{0}>", item.device);

                    if (path != null)
                    {
                        m_Parent.m_PathProperty.stringValue = path;
                        m_Parent.m_PathProperty.serializedObject.ApplyModifiedProperties();
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

                // This can use PLENTY of improvement. ATM all it does is add one branch
                // containing all unique usages in the system and then one branch for each
                // base device template.

                var id = 1;
                var usageRoot = BuildTreeForUsages(ref id);

                foreach (var template in EditorInputTemplateCache.allBaseDeviceTemplates)
                {
                    // Skip templates that don't have any controls (like the "HID" template).
                    if (template.controls.Count == 0)
                        continue;

                    var tree = BuildTreeForDevice(template, ref id);
                    root.AddChild(tree);
                }

                root.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                root.children.Insert(0, usageRoot);

                return root;
            }

            private TreeViewItem BuildTreeForUsages(ref int id)
            {
                var usageRoot = new TreeViewItem
                {
                    displayName = "By Usage",
                    id = id++,
                    depth = 0
                };

                ////TODO: filter out usages for output controls

                foreach (var usage in EditorInputTemplateCache.allUsages)
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

            private TreeViewItem BuildTreeForDevice(InputTemplate template, ref int id)
            {
                var deviceRoot = new Item
                {
                    displayName = template.name,
                    id = id++,
                    depth = 0,
                    device = template.name
                };

                BuildControlsRecursive(deviceRoot, template, string.Empty, ref id);

                return deviceRoot;
            }

            private void BuildControlsRecursive(TreeViewItem parent, InputTemplate template, string prefix, ref int id)
            {
                ////TODO: filter out output controls
                foreach (var control in template.controls)
                {
                    if (control.isModifyingChildControlByPath)
                        continue;

                    // Skip variants.
                    if (!string.IsNullOrEmpty(control.variant) && control.variant.ToLower() != "default")
                        continue;

                    var controlPath = prefix + control.name;
                    var child = new Item
                    {
                        id = id++,
                        depth = 1,
                        displayName = controlPath,
                        device = parent.displayName, ////REVIEW: this seems pointless
                        control = controlPath
                    };

                    var childTemplate = EditorInputTemplateCache.TryGetTemplate(control.template);
                    if (childTemplate != null)
                    {
                        BuildControlsRecursive(parent, childTemplate, controlPath + "/", ref id);
                    }

                    parent.AddChild(child);
                }

                if (parent.children != null)
                    parent.children.Sort((a, b) =>
                        string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
            }
        }
    }
}
#endif // UNITY_EDITOR
