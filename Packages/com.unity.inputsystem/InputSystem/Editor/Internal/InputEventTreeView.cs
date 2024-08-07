#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using Unity.Profiling;

////FIXME: this performs horribly; the constant rebuilding on every single event makes the debug view super slow when device is noisy

////TODO: add information about which update type + update count an event came through in

////TODO: add more information for each event (ideally, dump deltas that highlight control values that have changed)

////TODO: add diagnostics to immediately highlight problems with events (e.g. events getting ignored because of incorrect type codes)

////TODO: implement support for sorting data by different property columns (we currently always sort events by ID)

namespace UnityEngine.InputSystem.Editor
{
    // Multi-column TreeView that shows the events in a trace.
    internal class InputEventTreeView : TreeView
    {
        private readonly InputEventTrace m_EventTrace;
        private readonly InputControl m_RootControl;
        private static readonly ProfilerMarker k_InputEventTreeBuildRootMarker = new ProfilerMarker("InputEventTreeView.BuildRoot");

        private enum ColumnId
        {
            Id,
            Type,
            Device,
            Size,
            Time,
            Details,
            COUNT
        }

        public static InputEventTreeView Create(InputDevice device, InputEventTrace eventTrace, ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new InputEventTreeView(treeState, header, eventTrace, device);
        }

        private static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT];

            columns[(int)ColumnId.Id] =
                new MultiColumnHeaderState.Column
            {
                width = 80,
                minWidth = 60,
                headerContent = new GUIContent("Id"),
                canSort = false
            };
            columns[(int)ColumnId.Type] =
                new MultiColumnHeaderState.Column
            {
                width = 60,
                minWidth = 60,
                headerContent = new GUIContent("Type"),
                canSort = false
            };
            columns[(int)ColumnId.Device] =
                new MultiColumnHeaderState.Column
            {
                width = 80,
                minWidth = 60,
                headerContent = new GUIContent("Device"),
                canSort = false
            };
            columns[(int)ColumnId.Size] =
                new MultiColumnHeaderState.Column
            {
                width = 50,
                minWidth = 50,
                headerContent = new GUIContent("Size"),
                canSort = false
            };
            columns[(int)ColumnId.Time] =
                new MultiColumnHeaderState.Column
            {
                width = 100,
                minWidth = 80,
                headerContent = new GUIContent("Time"),
                canSort = false
            };

            columns[(int)ColumnId.Details] =
                new MultiColumnHeaderState.Column
            {
                width = 250,
                minWidth = 100,
                headerContent = new GUIContent("Details"),
                canSort = false
            };

            return new MultiColumnHeaderState(columns);
        }

        private InputEventTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, InputEventTrace eventTrace, InputControl rootControl)
            : base(state, multiColumnHeader)
        {
            m_EventTrace = eventTrace;
            m_RootControl = rootControl;
            Reload();
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as EventItem;
            if (item == null)
                return;

            // We can only inspect state events so ignore double-clicks on other
            // types of events.
            var eventPtr = item.eventPtr;
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            PopUpStateWindow(eventPtr);
        }

        ////TODO: move inspect and compare from a context menu to the toolbar of the event view
        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as EventItem;
            if (item == null)
                return;

            var menu = new GenericMenu();

            var selection = GetSelection();
            if (selection.Count == 1)
            {
                menu.AddItem(new GUIContent("Inspect"), false, OnInspectMenuItem, id);
            }
            else if (selection.Count > 1)
            {
                menu.AddItem(new GUIContent("Compare"), false, OnCompareMenuItem, selection);
            }

            menu.ShowAsContext();
        }

        private void OnCompareMenuItem(object userData)
        {
            var selection = (IList<int>)userData;
            var window = ScriptableObject.CreateInstance<InputStateWindow>();
            window.InitializeWithEvents(selection.Select(id => ((EventItem)FindItem(id, rootItem)).eventPtr).ToArray(), m_RootControl);
            window.Show();
        }

        private void OnInspectMenuItem(object userData)
        {
            var itemId = (int)userData;
            var item = FindItem(itemId, rootItem) as EventItem;
            if (item == null)
                return;
            PopUpStateWindow(item.eventPtr);
        }

        private void PopUpStateWindow(InputEventPtr eventPtr)
        {
            var window = ScriptableObject.CreateInstance<InputStateWindow>();
            window.InitializeWithEvent(eventPtr, m_RootControl);
            window.Show();
        }

        protected override TreeViewItem BuildRoot()
        {
            k_InputEventTreeBuildRootMarker.Begin();

            var root = new TreeViewItem
            {
                id = 0,
                depth = -1,
                displayName = "Root"
            };

            var eventCount = m_EventTrace.eventCount;
            if (eventCount == 0)
            {
                // TreeView doesn't allow having empty trees. Put a dummy item in here that we
                // render without contents.
                root.AddChild(new TreeViewItem(1));
            }
            else
            {
                var current = new InputEventPtr();
                // Can't set List to a fixed size and then fill it from the back. So we do it
                // the worse way... fill it in inverse order first, then reverse it :(
                root.children = new List<TreeViewItem>((int)eventCount);
                for (var i = 0; i < eventCount; ++i)
                {
                    if (!m_EventTrace.GetNextEvent(ref current))
                        break;

                    var item = new EventItem
                    {
                        id = i + 1,
                        depth = 1,
                        displayName = current.id.ToString(),
                        eventPtr = current
                    };

                    root.AddChild(item);
                }
                root.children.Reverse();
            }

            k_InputEventTreeBuildRootMarker.End();
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // Render nothing if event list is empty.
            if (m_EventTrace.eventCount == 0)
                return;

            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
            {
                var item = (EventItem)args.item;
                ColumnGUI(args.GetCellRect(i), item.eventPtr, args.GetColumn(i));
            }
        }

        private unsafe void ColumnGUI(Rect cellRect, InputEventPtr eventPtr, int column)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case (int)ColumnId.Id:
                    GUI.Label(cellRect, eventPtr.id.ToString());
                    break;
                case (int)ColumnId.Type:
                    GUI.Label(cellRect, eventPtr.type.ToString());
                    break;
                case (int)ColumnId.Device:
                    GUI.Label(cellRect, eventPtr.deviceId.ToString());
                    break;
                case (int)ColumnId.Size:
                    GUI.Label(cellRect, eventPtr.sizeInBytes.ToString());
                    break;
                case (int)ColumnId.Time:
                    GUI.Label(cellRect, eventPtr.time.ToString("0.0000s"));
                    break;
                case (int)ColumnId.Details:
                    if (eventPtr.IsA<DeltaStateEvent>())
                    {
                        var deltaEventPtr = DeltaStateEvent.From(eventPtr);
                        GUI.Label(cellRect, $"Format={deltaEventPtr->stateFormat}, Offset={deltaEventPtr->stateOffset}");
                    }
                    else if (eventPtr.IsA<StateEvent>())
                    {
                        var stateEventPtr = StateEvent.From(eventPtr);
                        GUI.Label(cellRect, $"Format={stateEventPtr->stateFormat}");
                    }
                    else if (eventPtr.IsA<TextEvent>())
                    {
                        var textEventPtr = TextEvent.From(eventPtr);
                        GUI.Label(cellRect, $"Character='{(char) textEventPtr->character}'");
                    }
                    break;
            }
        }

        private class EventItem : TreeViewItem
        {
            public InputEventPtr eventPtr;
        }
    }
}
#endif // UNITY_EDITOR
