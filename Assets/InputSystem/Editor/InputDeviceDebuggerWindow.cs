#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ISX
{
    // Shows status and activity of a single input device in a separate window.
    // Can also be used to alter the state of a device by making up state events.
    internal class InputDeviceDebuggerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        // Size of the raw memory buffer used for event recording in number of state
        // events. E.g. if a device has a state size of 32 bytes, we allocate a buffer
        // of 64 * 32 bytes.
        public const int kEventBufferSize = 64;

        public static void CreateOrShowExisting(InputDevice device)
        {
            // See if we have an existing window for the device and if so pop it
            // in front.
            if (s_OpenDebuggerWindows != null)
            {
                for (var i = 0; i < s_OpenDebuggerWindows.Count; ++i)
                {
                    var existingWindow = s_OpenDebuggerWindows[i];
                    if (existingWindow.m_Device == device)
                    {
                        existingWindow.Show();
                        existingWindow.Focus();
                        return;
                    }
                }
            }

            // No, so create a new one.
            var window = CreateInstance<InputDeviceDebuggerWindow>();
            window.InitializeWith(device);
            window.minSize = new Vector2(270, 300);
            window.Show();
            window.titleContent = new GUIContent(device.name);
            window.AddToList();
        }

        public void OnDestroy()
        {
            RemoveFromList();
        }

        public void OnGUI()
        {
            // Find device again if we've gone through a domain reload.
            if (m_Device == null)
            {
                m_Device = InputSystem.TryGetDeviceById(m_DeviceId);

                if (m_Device == null)
                {
                    EditorGUILayout.HelpBox(Styles.notFoundHelpText, MessageType.Warning);
                    return;
                }
            }

            ////FIXME: editor still expands height for some reason....
            EditorGUILayout.BeginVertical("OL Box", GUILayout.ExpandHeight(false));
            EditorGUILayout.LabelField("Name", m_Device.name);
            EditorGUILayout.LabelField("Template", m_Device.template);
            EditorGUILayout.LabelField("Connected", m_Device.connected ? "True" : "False");
            EditorGUILayout.LabelField("Interface", m_Device.description.interfaceName);
            EditorGUILayout.LabelField("Product", m_Device.description.product);
            EditorGUILayout.LabelField("Manufacturer", m_Device.description.manufacturer);
            EditorGUILayout.LabelField("Serial Number", m_Device.description.serial);
            EditorGUILayout.LabelField("Device ID", m_DeviceIdString);
            EditorGUILayout.EndVertical();

            DrawControlTree();
            DrawEventList();
        }

        private void DrawControlTree()
        {
            m_ControlTreeScrollPosition = EditorGUILayout.BeginScrollView(m_ControlTreeScrollPosition);
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_ControlTree.OnGUI(rect);
            EditorGUILayout.EndScrollView();
        }

        private void DrawEventList()
        {
        }

        private void InitializeWith(InputDevice device)
        {
            m_Device = device;
            m_DeviceId = device.id;
            m_DeviceIdString = device.id.ToString();

            // Set up control tree.
            if (m_ControlTreeState == null)
                m_ControlTreeState = new TreeViewState();
            if (m_ControlTreeHeaderState == null)
            {
                var columns = new MultiColumnHeaderState.Column[(int)ControlTreeView.ColumnId.COUNT];

                columns[(int)ControlTreeView.ColumnId.Name] =
                    new MultiColumnHeaderState.Column
                {
                    width = 150,
                    minWidth = 60,
                    headerContent = new GUIContent("Name")
                };
                columns[(int)ControlTreeView.ColumnId.Template] =
                    new MultiColumnHeaderState.Column
                {
                    width = 100,
                    minWidth = 60,
                    headerContent = new GUIContent("Template")
                };
                columns[(int)ControlTreeView.ColumnId.Type] =
                    new MultiColumnHeaderState.Column
                {
                    width = 100,
                    minWidth = 60,
                    headerContent = new GUIContent("Type")
                };
                columns[(int)ControlTreeView.ColumnId.Format] =
                    new MultiColumnHeaderState.Column {headerContent = new GUIContent("Format")};
                columns[(int)ControlTreeView.ColumnId.Offset] =
                    new MultiColumnHeaderState.Column {headerContent = new GUIContent("Offset")};
                columns[(int)ControlTreeView.ColumnId.Bit] =
                    new MultiColumnHeaderState.Column {width = 40, headerContent = new GUIContent("Bit")};
                columns[(int)ControlTreeView.ColumnId.Size] =
                    new MultiColumnHeaderState.Column {headerContent = new GUIContent("Size (Bits)")};
                columns[(int)ControlTreeView.ColumnId.Value] =
                    new MultiColumnHeaderState.Column {width = 120, headerContent = new GUIContent("Value")};

                m_ControlTreeHeaderState = new MultiColumnHeaderState(columns);
            }

            var header = new MultiColumnHeader(m_ControlTreeHeaderState);
            m_ControlTree = new ControlTreeView(m_Device, m_ControlTreeState, header);
            m_ControlTree.ExpandAll();
        }

        [NonSerialized] private InputDevice m_Device;
        [NonSerialized] private string m_DeviceIdString;
        [NonSerialized] private ControlTreeView m_ControlTree;

        [SerializeField] private int m_DeviceId = InputDevice.kInvalidDeviceId;
        [SerializeField] private IntPtr m_EventBuffer;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private Vector2 m_ControlTreeScrollPosition;
        [SerializeField] private Vector2 m_EventListScrollPosition;

        private static List<InputDeviceDebuggerWindow> s_OpenDebuggerWindows;

        private void AddToList()
        {
            if (s_OpenDebuggerWindows == null)
                s_OpenDebuggerWindows = new List<InputDeviceDebuggerWindow>();
            if (!s_OpenDebuggerWindows.Contains(this))
                s_OpenDebuggerWindows.Add(this);
        }

        private void RemoveFromList()
        {
            s_OpenDebuggerWindows?.Remove(this);
        }

        private static class Styles
        {
            public static string notFoundHelpText = "Device could not be found.";
        }

        // We can look up devices only after we're guaranteed that the input system itself has come
        // back from the reload. During deserialization we don't know whether the input system data
        // comes before or after us. So, we reconnect devices to their debug windows in a separate
        // step here.
        internal static void ReviveAfterDomainReload()
        {
            if (s_OpenDebuggerWindows == null)
                return;

            foreach (var window in s_OpenDebuggerWindows)
            {
                var device = InputSystem.TryGetDeviceById(window.m_DeviceId);
                if (device != null)
                    window.InitializeWith(device);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            AddToList();
        }

        private class ControlTreeView : TreeView
        {
            private const float kRowHeight = 20f;

            public class Item : TreeViewItem
            {
                public InputControl control;
            }

            public enum ColumnId
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

            public InputControl root;
            private List<InputControl> m_Controls = new List<InputControl>();

            public ControlTreeView(InputControl root, TreeViewState state, MultiColumnHeader header)
                : base(state, header)
            {
                this.root = root;
                showBorder = false;
                rowHeight = kRowHeight;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                // Build tree from control down the control hierarchy.
                var rootItem = BuildControlTreeRecursive(root, 0);

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
                        cellRect.x += GetContentIndent(item);
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
                        GUI.Label(cellRect, item.control.stateBlock.byteOffset.ToString());
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
                        var value = item.control.valueAsObject;
                        if (value != null)
                            GUI.Label(cellRect, value.ToString());
                        break;
                }
            }
        }

        // Additional window that we can pop open to inspect or even edit raw state (either
        // on events or on controls/devices).
        private class StateWindow : EditorWindow
        {
            // If set, this is a struct that describes the memory layout.
            public Type structType;

            public InputControl control;
            public InputEvent eventInfo;
            public IntPtr state;
            public InputStateBlock block;
        }
    }
}

#endif // UNITY_EDITOR
