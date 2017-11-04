#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ISX.Editor
{
    // Multi-column TreeView that shows control tree of device.
    internal class InputControlTreeView : TreeView
    {
        // If this is set, the controls won't display their current value but we'll
        // show their state data from this buffer instead.
        public byte[] stateBuffer;

        public static InputControlTreeView Create(InputControl rootControl, ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new InputControlTreeView(rootControl, treeState, header);
        }

        private const float kRowHeight = 20f;

        private class Item : TreeViewItem
        {
            public InputControl control;
        }

        private enum ColumnId
        {
            Name,
            Template,
            Type,
            Format,
            Offset,
            Bit,
            Size,
            Value,

            COUNT
        }

        private InputControl m_RootControl;
        private List<InputControl> m_Controls = new List<InputControl>();

        private static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT];

            columns[(int)ColumnId.Name] =
                new MultiColumnHeaderState.Column
            {
                width = 180,
                minWidth = 60,
                headerContent = new GUIContent("Name")
            };
            columns[(int)ColumnId.Template] =
                new MultiColumnHeaderState.Column
            {
                width = 100,
                minWidth = 60,
                headerContent = new GUIContent("Template")
            };
            columns[(int)ColumnId.Type] =
                new MultiColumnHeaderState.Column
            {
                width = 100,
                minWidth = 60,
                headerContent = new GUIContent("Type")
            };
            columns[(int)ColumnId.Format] =
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Format")};
            columns[(int)ColumnId.Offset] =
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Offset")};
            columns[(int)ColumnId.Bit] =
                new MultiColumnHeaderState.Column {width = 40, headerContent = new GUIContent("Bit")};
            columns[(int)ColumnId.Size] =
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Size (Bits)")};
            columns[(int)ColumnId.Value] =
                new MultiColumnHeaderState.Column {width = 120, headerContent = new GUIContent("Value")};

            return new MultiColumnHeaderState(columns);
        }

        private InputControlTreeView(InputControl root, TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            m_RootControl = root;
            showBorder = false;
            rowHeight = kRowHeight;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            // Build tree from control down the control hierarchy.
            var rootItem = BuildControlTreeRecursive(m_RootControl, 0);

            // Wrap root control in invisible item required by TreeView.
            return new Item
            {
                displayName = "Root",
                id = 0,
                children = new List<TreeViewItem> {rootItem},
                depth = -1
            };
        }

        private TreeViewItem BuildControlTreeRecursive(InputControl control, int depth)
        {
            m_Controls.Add(control);
            var id = m_Controls.Count;

            ////TODO: come up with nice icons depicting different control types

            var item = new Item
            {
                id = id,
                displayName = control.name,
                control = control,
                depth = depth
            };

            // Build children.
            if (control.children.Count > 0)
            {
                var children = new List<TreeViewItem>();

                foreach (var child in control.children)
                {
                    var childItem = BuildControlTreeRecursive(child, depth + 1);
                    childItem.parent = item;
                    children.Add(childItem);
                }

                item.children = children;
            }

            return item;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (Item)args.item;

            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
            {
                ColumnGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        private void ColumnGUI(Rect cellRect, Item item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case (int)ColumnId.Name:
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
                case (int)ColumnId.Template:
                    GUI.Label(cellRect, item.control.template);
                    break;
                case (int)ColumnId.Format:
                    GUI.Label(cellRect, item.control.stateBlock.format.ToString());
                    break;
                case (int)ColumnId.Offset:
                    // Offsets on the controls are absolute. Make them relative to the
                    // root control.
                    var controlOffset = item.control.stateBlock.byteOffset;
                    var rootOffset = m_RootControl.stateBlock.byteOffset;
                    GUI.Label(cellRect, (controlOffset - rootOffset).ToString());
                    break;
                case (int)ColumnId.Bit:
                    GUI.Label(cellRect, item.control.stateBlock.bitOffset.ToString());
                    break;
                case (int)ColumnId.Size:
                    GUI.Label(cellRect, item.control.stateBlock.sizeInBits.ToString());
                    break;
                case (int)ColumnId.Type:
                    GUI.Label(cellRect, item.control.GetType().Name);
                    break;
                case (int)ColumnId.Value:
                    if (stateBuffer != null)
                    {
                        ////TODO: switch to ReadValueFrom
                        var text = ReadRawValueAsString(item.control);
                        if (text != null)
                            GUI.Label(cellRect, text);
                    }
                    else
                    {
                        var value = item.control.valueAsObject;
                        if (value != null)
                            GUI.Label(cellRect, value.ToString());
                    }
                    break;
            }
        }

        private unsafe string ReadRawValueAsString(InputControl control)
        {
            fixed(byte* state = stateBuffer)
            {
                var ptr = state + control.m_StateBlock.byteOffset - m_RootControl.m_StateBlock.byteOffset;
                var format = control.m_StateBlock.format;

                if (format == InputStateBlock.kTypeBit)
                {
                    if (BitfieldHelpers.ReadSingleBit(new IntPtr(ptr), control.m_StateBlock.bitOffset))
                        return "1";
                    return "0";
                }

                if (format == InputStateBlock.kTypeByte)
                {
                    return (*ptr).ToString();
                }

                if (format == InputStateBlock.kTypeShort)
                {
                    return (*((short*)ptr)).ToString();
                }

                if (format == InputStateBlock.kTypeInt)
                {
                    return (*((int*)ptr)).ToString();
                }

                if (format == InputStateBlock.kTypeFloat)
                {
                    return (*((float*)ptr)).ToString();
                }

                if (format == InputStateBlock.kTypeDouble)
                {
                    return (*((double*)ptr)).ToString();
                }

                return null;
            }
        }
    }
}
#endif // UNITY_EDITOR
