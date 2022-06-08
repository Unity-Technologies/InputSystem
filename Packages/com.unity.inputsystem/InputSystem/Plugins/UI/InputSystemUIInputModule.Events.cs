#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    public abstract partial class InputSystemUIInputModule
    {
        /// <summary>
        /// Calls the methods in its invocation list after the input module collects a list of type <see cref="RaycastResult"/>, but before the results are used.
        /// Note that not all fields of the event data are still valid or up to date at this point in the UI event processing.
        /// This event can be used to read, modify, or reorder results.
        /// After the event, the first result in the list with a non-null GameObject will be used.
        /// </summary>
        public event Action<PointerEventData, List<RaycastResult>> onFinalizeRaycastResults;

        /// <summary>
        /// This occurs when a UI pointer enters an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onPointerEnter;

        /// <summary>
        /// This occurs when a UI pointer exits an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onPointerExit;

        /// <summary>
        /// This occurs when a select button down occurs while a UI pointer is hovering an element.
        /// This event is executed using ExecuteEvents.ExecuteHierarchy when sent to the target element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onPointerDown;

        /// <summary>
        /// This occurs when a select button up occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onPointerUp;

        /// <summary>
        /// This occurs when a select button click occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onPointerClick;

        /// <summary>
        /// This occurs when a potential drag occurs on an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onInitializePotentialDrag;

        /// <summary>
        /// This occurs when a drag first occurs on an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onBeginDrag;

        /// <summary>
        /// This occurs every frame while dragging an element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onDrag;

        /// <summary>
        /// This occurs on the last frame an element is dragged.
        /// </summary>
        public event Action<GameObject, PointerEventData> onEndDrag;

        /// <summary>
        /// This occurs when a dragged element is dropped on a drop handler.
        /// </summary>
        public event Action<GameObject, PointerEventData> onDrop;

        /// <summary>
        /// This occurs when an element is scrolled
        /// This event is executed using ExecuteEvents.ExecuteHierarchy when sent to the target element.
        /// </summary>
        public event Action<GameObject, PointerEventData> onScroll;

        /// <summary>
        /// This occurs on update for the currently selected object.
        /// </summary>
        public event Action<GameObject, BaseEventData> onUpdateSelected;

        /// <summary>
        /// This occurs when the move axis is activated.
        /// </summary>
        public event Action<GameObject, AxisEventData> onMove;

        /// <summary>
        /// This occurs when the submit button is pressed.
        /// </summary>
        public event Action<GameObject, BaseEventData> onSubmit;

        /// <summary>
        /// This occurs when the cancel button is pressed.
        /// </summary>
        public event Action<GameObject, BaseEventData> onCancel;
    }
}
#endif
