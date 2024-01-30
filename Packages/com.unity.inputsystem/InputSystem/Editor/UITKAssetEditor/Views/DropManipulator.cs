using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Class to handle drop from the actions tree view to the action maps list view.
    /// </summary>
    public class DropManipulator : Manipulator
    {
        private EventCallback<DragPerformEvent> DroppedPerformedCallback;
        private VisualElement m_OtherVerticalList;
        private ListView listView => target as ListView;

        public DropManipulator(EventCallback<DragPerformEvent> droppedPerformedCallback, VisualElement otherVerticalList)
        {
            DroppedPerformedCallback = droppedPerformedCallback;
            m_OtherVerticalList = otherVerticalList;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            m_OtherVerticalList.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            m_OtherVerticalList.RegisterCallback<DragPerformEvent>(OnDragPerformEvent, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            m_OtherVerticalList.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            m_OtherVerticalList.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent, TrickleDown.TrickleDown);
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (target.panel.Pick(evt.mousePosition).FindAncestorUserData() is null) //TODO
                return;
            evt.StopImmediatePropagation();
            DragAndDrop.AcceptDrag();
            DroppedPerformedCallback.Invoke(evt);
            Reset();
        }

        private int m_InitialIndex = -1;
        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var treeViewItem = target.panel.Pick(evt.mousePosition)?.parent;
            if (treeViewItem is InputActionMapsTreeViewItem mapItem)
            {
                listView?.Focus();
                if (m_InitialIndex < 0 && listView != null)
                    m_InitialIndex = listView.selectedIndex;
                //select map item to visualize the drop
                listView?.SetSelectionWithoutNotify(new[] { (int)mapItem.userData }); //the user data contains the index of the map item
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopImmediatePropagation();
            }
            else
                Reset();
        }

        private void Reset()
        {
            var treeView = m_OtherVerticalList as TreeView;
            treeView?.Focus();
            if (m_InitialIndex >= 0)
                listView?.SetSelectionWithoutNotify(new[] {m_InitialIndex});
            m_InitialIndex = -1;
        }
    }
}
