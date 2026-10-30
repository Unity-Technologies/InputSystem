using UnityEditor;
using UnityEditor.IMGUI.Controls;

#if UNITY_EDITOR

namespace UnityEngine.InputSystem.Editor
{
    public class DeviceFlagsTreeView : TreeView
    {
        private InputDevice m_Device;
        
        private enum ColumnId
        {
            Name,
            Value,
            COUNT
        }

        public static DeviceFlagsTreeView Create(InputDevice device, ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new DeviceFlagsTreeView(treeState, header, device);
        }

        private static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT];

            columns[(int)ColumnId.Name] = new MultiColumnHeaderState.Column()
            {
                width = 320,
                minWidth = 60,
                headerContent = new GUIContent("Name"),
                canSort = false
            };
            columns[(int)ColumnId.Value] = new MultiColumnHeaderState.Column()
            {
                width = 80,
                minWidth = 60,
                headerContent = new GUIContent("Value"),
                canSort = false
            };
            
            return new MultiColumnHeaderState(columns);
        }

        private DeviceFlagsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, InputDevice device)
            : base(state, multiColumnHeader)
        {
            m_Device = device;
            Reload();
        }

        private void AddFlag(TreeViewItem root, InputDevice.DeviceFlags flag)
        {
            root.AddChild(new FlagItem()
            {
                id = 1,
                depth = 1,
                displayName = "",
                Flag = flag
            });
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            AddFlag(root, InputDevice.DeviceFlags.Native);
            AddFlag(root, InputDevice.DeviceFlags.Remote);

            AddFlag(root, InputDevice.DeviceFlags.CanRunInBackground);
            AddFlag(root, InputDevice.DeviceFlags.CanRunInBackgroundHasBeenQueried);
            
            AddFlag(root, InputDevice.DeviceFlags.UpdateBeforeRender);
            AddFlag(root, InputDevice.DeviceFlags.HasStateCallbacks);
            AddFlag(root, InputDevice.DeviceFlags.HasControlsWithDefaultState);
            AddFlag(root, InputDevice.DeviceFlags.HasDontResetControls);
            AddFlag(root, InputDevice.DeviceFlags.HasEventMerger);
            AddFlag(root, InputDevice.DeviceFlags.HasEventPreProcessor);
            
            AddFlag(root, InputDevice.DeviceFlags.DisabledInFrontend);
            AddFlag(root, InputDevice.DeviceFlags.DisabledInRuntime);
            AddFlag(root, InputDevice.DeviceFlags.DisabledWhileInBackground);
            AddFlag(root, InputDevice.DeviceFlags.DisabledStateHasBeenQueriedFromRuntime);
            
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
            {
                var item = (FlagItem)args.item;
                ColumnGUI(args.GetCellRect(i), item.Flag, args.GetColumn(i));
            }
        }
        
        private unsafe void ColumnGUI(Rect cellRect, InputDevice.DeviceFlags flag, int column)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            
            switch (column)
            {
                case (int)ColumnId.Name:
                    GUI.Label(cellRect, flag.ToString());
                    break;
                case (int)ColumnId.Value:
                {
                    var isSet = ((m_Device.m_DeviceFlags & flag) != 0);
                    if (isSet)
                        GUI.Label(cellRect, "true", EditorStyles.boldLabel);
                    else
                        GUI.Label(cellRect, "false");
                }
                    break;
            }
        }
        
        private class FlagItem : TreeViewItem
        {
            public InputDevice.DeviceFlags Flag;
        }
    }
}

#endif // UNITY_EDITOR