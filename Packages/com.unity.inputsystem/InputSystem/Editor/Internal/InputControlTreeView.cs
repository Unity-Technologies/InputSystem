#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Profiling;

////TODO: make control values editable (create state events from UI and pump them into the system)

////TODO: show processors attached to controls

////TODO: make controls that have different `value` and `previous` in bold

namespace UnityEngine.Experimental.Input.Editor
{
    // Multi-column TreeView that shows control tree of device.
    internal class InputControlTreeView : TreeView
    {
        // If this is set, the controls won't display their current value but we'll
        // show their state data from this buffer instead.
        public byte[] stateBuffer;
        public byte[][] multipleStateBuffers;
        public bool showDifferentOnly;

        public static InputControlTreeView Create(InputControl rootControl, int numValueColumns, ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState(numValueColumns);
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new InputControlTreeView(rootControl, treeState, header);
        }

        public void RefreshControlValues()
        {
            if (rootItem != null)
                RefreshControlValuesRecursive(rootItem);
        }

        private void RefreshControlValuesRecursive(TreeViewItem item)
        {
            var controlItem = item as ControlItem;
            if (controlItem != null)
                ReadState(controlItem.control, out controlItem.value, out controlItem.values);

            if (item.children != null)
                foreach (var child in item.children)
                    RefreshControlValuesRecursive(child);
        }

        private const float kRowHeight = 20f;

        private enum ColumnId
        {
            Name,
            DisplayName,
            Layout,
            Type,
            Format,
            Offset,
            Bit,
            Size,
            Value,

            COUNT
        }

        private InputControl m_RootControl;

        private static MultiColumnHeaderState CreateHeaderState(int numValueColumns)
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT + numValueColumns - 1];

            columns[(int)ColumnId.Name] =
                new MultiColumnHeaderState.Column
            {
                width = 180,
                minWidth = 60,
                headerContent = new GUIContent("Name")
            };
            columns[(int)ColumnId.DisplayName] =
                new MultiColumnHeaderState.Column
            {
                width = 160,
                minWidth = 60,
                headerContent = new GUIContent("Display Name")
            };
            columns[(int)ColumnId.Layout] =
                new MultiColumnHeaderState.Column
            {
                width = 100,
                minWidth = 60,
                headerContent = new GUIContent("Layout")
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

            if (numValueColumns == 1)
            {
                columns[(int)ColumnId.Value] =
                    new MultiColumnHeaderState.Column {width = 120, headerContent = new GUIContent("Value")};
            }
            else
            {
                for (var i = 0; i < numValueColumns; ++i)
                    columns[(int)ColumnId.Value + i] =
                        new MultiColumnHeaderState.Column
                    {
                        width = 100,
                        headerContent = new GUIContent("Value " + (char)('A' + i))
                    };
            }

            return new MultiColumnHeaderState(columns);
        }

        private InputControlTreeView(InputControl root, TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            m_RootControl = root;
            showBorder = false;
            rowHeight = kRowHeight;
        }

        protected override TreeViewItem BuildRoot()
        {
            Profiler.BeginSample("BuildControlTree");

            var id = 1;

            // Build tree from control down the control hierarchy.
            var rootItem = BuildControlTreeRecursive(m_RootControl, 0, ref id);

            Profiler.EndSample();

            // Wrap root control in invisible item required by TreeView.
            return new TreeViewItem
            {
                id = 0,
                children = new List<TreeViewItem> {rootItem},
                depth = -1
            };
        }

        private ControlItem BuildControlTreeRecursive(InputControl control, int depth, ref int id)
        {
            // Build children.
            List<TreeViewItem> children = null;
            var isLeaf = control.children.Count == 0;
            if (!isLeaf)
            {
                children = new List<TreeViewItem>();

                foreach (var child in control.children)
                {
                    var childItem = BuildControlTreeRecursive(child, depth + 1, ref id);
                    if (childItem != null)
                        children.Add(childItem);
                }

                // If none of our children returned an item, none of their data is different,
                // so if we are supposed to show only controls that differ in value, we're sitting
                // on a branch that has no changes. Cull the branch except if we're all the way
                // at the root (we want to have at least one item).
                if (children.Count == 0 && showDifferentOnly && depth != 0)
                    return null;

                // Sort children by name.
                children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
            }

            // Compute offset. Offsets on the controls are absolute. Make them relative to the
            // root control.
            var controlOffset = control.stateBlock.byteOffset;
            var rootOffset = m_RootControl.stateBlock.byteOffset;
            var offset = controlOffset - rootOffset;

            // Read state.
            GUIContent value;
            GUIContent[] values;
            if (!ReadState(control, out value, out values))
                return null;

            ////TODO: come up with nice icons depicting different control types

            var item = new ControlItem
            {
                id = id++,
                displayName = control.name,
                control = control,
                depth = depth,
                layout = new GUIContent(control.layout),
                format = new GUIContent(control.stateBlock.format.ToString()),
                offset = new GUIContent(offset.ToString()),
                bit = new GUIContent(control.stateBlock.bitOffset.ToString()),
                sizeInBits = new GUIContent(control.stateBlock.sizeInBits.ToString()),
                type = new GUIContent(control.GetType().Name),
                value = value,
                values = values,
                children = children
            };

            if (children != null)
            {
                foreach (var child in children)
                    child.parent = item;
            }

            return item;
        }

        private bool ReadState(InputControl control, out GUIContent value, out GUIContent[] values)
        {
            value = null;
            values = null;

            if (stateBuffer != null)
            {
                ////TODO: switch to ReadValueFrom
                var text = ReadRawValueAsString(control, stateBuffer);
                if (text != null)
                    value = new GUIContent(text);
            }
            else if (multipleStateBuffers != null)
            {
                var valueStrings = multipleStateBuffers.Select(x => ReadRawValueAsString(control, x));
                if (showDifferentOnly && control.children.Count == 0 && valueStrings.Distinct().Count() == 1)
                    return false;
                values = valueStrings.Select(x => x != null ? new GUIContent(x) : null).ToArray();
            }
            else
            {
                var valueObject = control.ReadValueAsObject();
                if (valueObject != null)
                    value = new GUIContent(valueObject.ToString());
            }

            return true;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ControlItem)args.item;

            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
            {
                ColumnGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        private void ColumnGUI(Rect cellRect, ControlItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case (int)ColumnId.Name:
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
                case (int)ColumnId.DisplayName:
                    GUI.Label(cellRect, item.control.displayName);
                    break;
                case (int)ColumnId.Layout:
                    GUI.Label(cellRect, item.layout);
                    break;
                case (int)ColumnId.Format:
                    GUI.Label(cellRect, item.format);
                    break;
                case (int)ColumnId.Offset:
                    GUI.Label(cellRect, item.offset);
                    break;
                case (int)ColumnId.Bit:
                    GUI.Label(cellRect, item.bit);
                    break;
                case (int)ColumnId.Size:
                    GUI.Label(cellRect, item.sizeInBits);
                    break;
                case (int)ColumnId.Type:
                    GUI.Label(cellRect, item.type);
                    break;
                case (int)ColumnId.Value:
                    if (item.value != null)
                        GUI.Label(cellRect, item.value);
                    else if (item.values != null && item.values[0] != null)
                        GUI.Label(cellRect, item.values[0]);
                    break;
                default:
                    var valueIndex = column - (int)ColumnId.Value;
                    if (item.values != null && item.values[valueIndex] != null)
                        GUI.Label(cellRect, item.values[valueIndex]);
                    break;
            }
        }

        private unsafe string ReadRawValueAsString(InputControl control, byte[] state)
        {
            fixed(byte* statePtr = state)
            {
                var ptr = statePtr + control.m_StateBlock.byteOffset - m_RootControl.m_StateBlock.byteOffset;
                var format = control.m_StateBlock.format;

                object value = null;
                if (format == InputStateBlock.kTypeBit)
                {
                    if (MemoryHelpers.ReadSingleBit(new IntPtr(ptr), control.m_StateBlock.bitOffset))
                        value = "1";
                    value = "0";
                }
                else if (format == InputStateBlock.kTypeByte || format == InputStateBlock.kTypeSByte)
                {
                    value = (*ptr);
                }
                else if (format == InputStateBlock.kTypeShort)
                {
                    value = (*((short*)ptr));
                }
                else if (format == InputStateBlock.kTypeUShort)
                {
                    value = (*((ushort*)ptr));
                }
                else if (format == InputStateBlock.kTypeInt)
                {
                    value = (*((int*)ptr));
                }
                else if (format == InputStateBlock.kTypeUInt)
                {
                    value = (*((uint*)ptr));
                }
                else if (format == InputStateBlock.kTypeFloat)
                {
                    value = (*((float*)ptr));
                }
                else if (format == InputStateBlock.kTypeDouble)
                {
                    value = (*((double*)ptr));
                }

                // Stringify enum values, for. ex., PointerPhase
                if (value != null && control.valueType.IsEnum)
                {
                    var intValue = Convert.ToInt32(value);
                    value = Enum.ToObject(control.valueType, intValue);
                }

                return value != null ? value.ToString() : null;
            }
        }

        private class ControlItem : TreeViewItem
        {
            public InputControl control;
            public GUIContent layout;
            public GUIContent format;
            public GUIContent offset;
            public GUIContent bit;
            public GUIContent sizeInBits;
            public GUIContent type;
            public GUIContent value;
            public GUIContent[] values;
        }
    }
}
#endif // UNITY_EDITOR
