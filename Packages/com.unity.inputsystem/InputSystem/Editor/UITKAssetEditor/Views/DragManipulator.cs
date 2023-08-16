using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    public class DragManipulator : Manipulator
    {
        String initialLabelText;

        public static bool dragging;
        private bool m_IsPointerDown;
        protected override void RegisterCallbacksOnTarget()
        {
            Debug.Log("Registering callbacks");
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging && m_IsPointerDown)
            {
                m_IsPointerDown = false;
                Debug.Log("Starting drag!");
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("string", "dropped");
                DragAndDrop.StartDrag(string.Empty);
                dragging = true;
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            Debug.Log("Pointer up");
            m_IsPointerDown = false;
            if (dragging)
            {
                dragging = false;
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.isPrimary && evt.button == 0)
            {
                Debug.Log("Pointer down");
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                m_IsPointerDown = true;
                dragging = false;
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }
}
