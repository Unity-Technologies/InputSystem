using System;
using UnityEditor;
using UnityEngine;
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
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (target.panel.Pick(evt.mousePosition).FindAncestorUserData() is not null) //TODO
            {
                (target as ListView)?.Focus(); //TODO focus element on the listview
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopImmediatePropagation();
            }
            else
            {
                otherVerticalList.Focus();
            }
        }
    }
}
