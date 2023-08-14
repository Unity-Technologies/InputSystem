using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    public class DropManipulator : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            Debug.Log("Registering callbacks for DropManipulator");
            target.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        void OnDragExitedEvent(DragExitedEvent evt)
        {
            Debug.Log("Drag exited");
            // ((Label)evt.target).style.backgroundColor = Color.clear;
            object draggedLabel = DragAndDrop.GetGenericData("string");
            if (DragManipulator.dragging)
            {
                DragManipulator.dragging = false;
            }
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            object data = DragAndDrop.GetGenericData("string");
            Debug.Log("Drag performed with data: " + data + "to target: " + evt.currentTarget);
            DragManipulator.dragging = false;
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            // Unclear why we need this, but saw it in another example.
            evt.StopPropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            Debug.Log("Drag leave");
            // ((Label)evt.target).style.backgroundColor = Color.clear;
        }

        void OnDragEnterEvent(DragEnterEvent evt)
        {
            Debug.Log("Drag enter event " + evt.currentTarget);

            // This makes sure that drop is only allowed on the correct type of element that started the drag.
            // The drag element will set a string in the DragAndDrop generic data, which we can use to check if the
            // drop is allowed.
            if (DragAndDrop.GetGenericData("string") != null)
            {
                // ((Label)evt.target).style.backgroundColor = Color.gray;
            }
        }
    }
}
