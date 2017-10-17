using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

////WIP: this is still very rough and shoddy

//probably something like...
//  one dimension is usage (may want to bring back InputUsage)
//  the other dimension is template
//  the third one is custom where the user can just enter a string

////FIXME: lower-casing done internally is visible

namespace ISX
{
    // Popup window that allows selecting controls to target in a binding. Will generate
    // a path string as a result and store it in the given "path" property.
    //
    // At the moment, the interface is pretty simplistic. You can either select a usage
    // or select a specific control on a specific base device template.
    //
    // Usages are discovered from all templates that are registered with the system.
    public class InputBindingPathSelector : PopupWindowContent
    {
        private string m_SearchString;
        private SerializedProperty m_PathProperty;

        public InputBindingPathSelector(SerializedProperty pathProperty)
        {
            if (pathProperty == null)
                throw new ArgumentNullException(nameof(pathProperty));
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
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

            var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.toolbarSearchField, GUILayout.MinWidth(80));
            m_SearchString = EditorGUI.TextField(searchRect, m_SearchString, Styles.toolbarSearchField);
            if (GUILayout.Button(
                    GUIContent.none,
                    m_SearchString == string.Empty ? Styles.toolbarSearchFieldCancelEmpty : Styles.toolbarSearchFieldCancel))
            {
                m_SearchString = string.Empty;
                EditorGUIUtility.keyboardControl = 0;
            }

            GUILayout.EndHorizontal();
        }

        private PathTreeView m_PathTree;
        private TreeViewState m_PathTreeState;

        private static class Styles
        {
            public static GUIStyle toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
            public static GUIStyle toolbarSearchFieldCancel = new GUIStyle("ToolbarSeachCancelButton");
            public static GUIStyle toolbarSearchFieldCancelEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty");
        }

        private class PathTreeView : TreeView
        {
            private InputBindingPathSelector m_Parent;

            private struct Item
            {
                public string usage;
                public string device;
                public string control;
            };

            private List<Item> m_Items;

            public PathTreeView(TreeViewState state, InputBindingPathSelector parent)
                : base(state)
            {
                m_Parent = parent;
                Reload();
            }

            // When an item is double-clicked, form a path from the item and store it
            // in the path property. Then close the popup window.
            protected override void DoubleClickedItem(int id)
            {
                if (id > 0 && id <= m_Items.Count)
                {
                    var item = m_Items[id - 1];

                    String path = null;
                    if (item.usage != null)
                        path = $"*/{{{item.usage}}}";
                    else if (item.device != null && item.control != null)
                        path = $"<{item.device}>/{item.control}";
                    else if (item.device != null)
                        path = $"<{item.device}>";

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
                m_Items = new List<Item>();

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
                root.AddChild(usageRoot);

                foreach (var template in EditorInputTemplateCache.allBaseDeviceTemplates)
                {
                    var tree = BuildTreeForDevice(template, ref id);
                    root.AddChild(tree);
                }

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
                m_Items.Add(new Item());

                foreach (var usage in EditorInputTemplateCache.allUsages)
                {
                    var child = new TreeViewItem
                    {
                        id = id++,
                        depth = 1,
                        displayName = usage.Key
                    };
                    m_Items.Add(new Item { usage = usage.Key });

                    usageRoot.AddChild(child);
                }

                return usageRoot;
            }

            private TreeViewItem BuildTreeForDevice(InputTemplate template, ref int id)
            {
                var deviceRoot = new TreeViewItem
                {
                    displayName = template.name,
                    id = id++,
                    depth = 0
                };
                m_Items.Add(new Item { device = template.name });

                BuildControlsRecursive(deviceRoot, template, string.Empty, ref id);

                return deviceRoot;
            }

            private void BuildControlsRecursive(TreeViewItem parent, InputTemplate template, string prefix, ref int id)
            {
                foreach (var control in template.controls)
                {
                    if (control.isModifyingChildControlByPath)
                        continue;

                    var controlPath = prefix + control.name;
                    var child = new TreeViewItem
                    {
                        id = id++,
                        depth = 1,
                        displayName = controlPath
                    };
                    m_Items.Add(new Item { device = parent.displayName, control = controlPath });

                    var childTemplate = EditorInputTemplateCache.TryGetTemplate(control.template);
                    if (childTemplate != null)
                    {
                        BuildControlsRecursive(parent, childTemplate, controlPath + "/", ref id);
                    }

                    parent.AddChild(child);
                }

                parent.children?.Sort((a, b) =>
                    string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
            }
        }
    }
}
