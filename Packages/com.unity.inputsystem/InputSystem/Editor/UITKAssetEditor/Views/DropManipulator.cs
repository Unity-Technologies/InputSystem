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

        public DropManipulator(EventCallback<DragPerformEvent> droppedPerformedCallback)
        {
            DroppedPerformedCallback = droppedPerformedCallback;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent, TrickleDown.TrickleDown);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        void OnDragExitedEvent(DragExitedEvent evt)
        {
            Debug.Log("Drag exited");
            if (DragManipulator.dragging)
            {
                DragManipulator.dragging = false;
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            DroppedPerformedCallback.Invoke(evt);
            DragManipulator.dragging = false;
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.StopImmediatePropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }

        void OnDragEnterEvent(DragEnterEvent evt)
        {
            Debug.Log("Drag enter event " + evt.currentTarget);

            //TODO
            // This event can be used to mae sure that drop is only allowed on the correct type of element that started
            // the drag.
        }
    }
}
