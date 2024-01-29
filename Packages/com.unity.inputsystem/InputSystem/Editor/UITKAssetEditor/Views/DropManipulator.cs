using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Class to handle drop events on a UI element.
    /// </summary>
    /// Created by setting a callback to the constructor that will run only when the drop is performed.
    public class DropManipulator : Manipulator
    {
        EventCallback<DragPerformEvent> DroppedPerformedCallback;
        VisualElement otherVerticalList;

        public DropManipulator(EventCallback<DragPerformEvent> droppedPerformedCallback, VisualElement otherVerticalList)
        {
            DroppedPerformedCallback = droppedPerformedCallback;
            this.otherVerticalList = otherVerticalList;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            otherVerticalList.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            otherVerticalList.RegisterCallback<DragPerformEvent>(OnDragPerformEvent, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            otherVerticalList.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            otherVerticalList.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent, TrickleDown.TrickleDown);
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (target.panel.Pick(evt.mousePosition).FindAncestorUserData() is null) //TODO
                return;
            evt.StopImmediatePropagation();
            DragAndDrop.AcceptDrag();
            DroppedPerformedCallback.Invoke(evt);
            Reset();
        }

        private int initialIndex = -1;
        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (target.panel.Pick(evt.mousePosition).FindAncestorUserData() is not null) //TODO
            {
                if (target.panel.Pick(evt.mousePosition).FindAncestorUserData() is not int) //TODO
                    return;
                (target as ListView)?.Focus();
                if (initialIndex < 0)
                    initialIndex = ((ListView)target).selectedIndex;
                (target as ListView)?.SetSelectionWithoutNotify(new[] {(int)target.panel.Pick(evt.mousePosition).FindAncestorUserData()});
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopImmediatePropagation();
            }
            else
                Reset();
        }

        void Reset()
        {
            (otherVerticalList as TreeView)?.Focus();
            if (initialIndex >= 0)
                (target as ListView)?.SetSelectionWithoutNotify(new[] {initialIndex});
            initialIndex = -1;
        }
    }
}
