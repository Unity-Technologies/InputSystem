using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// A series of flags to determine if a button has been pressed or released since the last time checked.
    /// Useful for identifying press/release events that occur in a single frame or sample.
    /// </summary>
    [Flags]
    internal enum ButtonDeltaState
    {
        NoChange = 0,
        Pressed = 1,
        Released = 2,
    }

    /// <summary>
    /// Represents the state of a single mouse button within the uGUI system.  Keeps track of various book-keeping regarding clicks, drags, and presses.
    /// Can be converted to and from PointerEventData for sending into uGUI.
    /// </summary>
    internal struct MouseButtonModel
    {
        public struct InternalData
        {
            /// <summary>
            /// Used to cache whether or not the current mouse button is being dragged.  See <see cref="PointerEventData.dragging"/> for more details.
            /// </summary>
            public bool isDragging { get; set; }

            /// <summary>
            /// Used to cache the last time this button was pressed.  See <see cref="PointerEventData.clickTime"/> for more details.
            /// </summary>
            public float pressedTime { get; set; }

            /// <summary>
            /// The position on the screen that this button was last pressed.  In the same scale as <see cref="MouseModel.position"/>, and caches the same value as <see cref="PointerEventData.pressPosition"/>.
            /// </summary>
            public Vector2 pressedPosition { get; set; }

            /// <summary>
            /// The Raycast data from the time it was pressed.  See <see cref="PointerEventData.pointerPressRaycast"/> for more details.
            /// </summary>
            public RaycastResult pressedRaycast { get; set; }

            /// <summary>
            /// The last gameobject pressed on that can handle press or click events.  See <see cref="PointerEventData.pointerPress"/> for more details.
            /// </summary>
            public GameObject pressedGameObject { get; set; }

            /// <summary>
            /// The last gameobject pressed on regardless of whether it can handle events or not.  See <see cref="PointerEventData.rawPointerPress"/> for more details.
            /// </summary>
            public GameObject pressedGameObjectRaw { get; set; }

            /// <summary>
            /// The gameobject currently being dragged if any.  See <see cref="PointerEventData.pointerDrag"/> for more details.
            /// </summary>
            public GameObject draggedGameObject { get; set; }

            /// <summary>
            /// Resets this object to it's default, unused state.
            /// </summary>
            public void Reset()
            {
                isDragging = false;
                pressedTime = 0.0f;
                pressedPosition = Vector2.zero;
                pressedRaycast = new RaycastResult();
                pressedGameObject = pressedGameObjectRaw = draggedGameObject = null;
            }
        }

        /// <summary>
        /// Used to store the current binary state of the button.  When set, will also track the changes between calls of <see cref="OnFrameFinished"/> in <see cref="lastFrameDelta"/>.
        /// </summary>
        public bool isDown
        {
            get
            {
                return m_IsDown;
            }
            set
            {
                if (m_IsDown != value)
                {
                    m_IsDown = value;
                    lastFrameDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
                }
            }
        }

        public int clickCount { get; set; }

        public bool hasNativeClickCount => clickCount != 0;

        /// <summary>
        /// A set of flags to identify the changes that have occured between calls of <see cref="OnFrameFinished"/>.
        /// </summary>
        internal ButtonDeltaState lastFrameDelta { get; private set; }

        /// <summary>
        /// Set's this object to it's default, unused state.
        /// </summary>
        public void Reset()
        {
            lastFrameDelta = ButtonDeltaState.NoChange;
            m_IsDown = false;

            m_InternalData.Reset();
        }

        /// <summary>
        /// Call this on each frame in order to reset properties that detect whether or not a certain condition was met this frame.
        /// </summary>
        public void OnFrameFinished()
        {
            lastFrameDelta = ButtonDeltaState.NoChange;
        }

        /// <summary>
        /// Fills a <see cref="PointerEventData"/> with this mouse button's internally cached values.
        /// </summary>
        /// <param name="eventData">These objects are used to send data through the uGUI system.</param>
        public void CopyTo(PointerEventData eventData)
        {
            eventData.dragging = m_InternalData.isDragging;
            eventData.clickTime = m_InternalData.pressedTime;
            eventData.pressPosition = m_InternalData.pressedPosition;
            eventData.pointerPressRaycast = m_InternalData.pressedRaycast;
            eventData.pointerPress = m_InternalData.pressedGameObject;
            eventData.rawPointerPress = m_InternalData.pressedGameObjectRaw;
            eventData.pointerDrag = m_InternalData.draggedGameObject;
            if (hasNativeClickCount)
                eventData.clickCount = clickCount;
        }

        /// <summary>
        /// Fills this object with the values from a <see cref="PointerEventData"/>.
        /// </summary>
        /// <param name="eventData">These objects are used to send data through the uGUI system.</param>
        public void CopyFrom(PointerEventData eventData)
        {
            m_InternalData.isDragging = eventData.dragging;
            m_InternalData.pressedTime = eventData.clickTime;
            m_InternalData.pressedPosition = eventData.pressPosition;
            m_InternalData.pressedRaycast = eventData.pointerPressRaycast;
            m_InternalData.pressedGameObject = eventData.pointerPress;
            m_InternalData.pressedGameObjectRaw = eventData.rawPointerPress;
            m_InternalData.draggedGameObject = eventData.pointerDrag;
        }

        private bool m_IsDown;
        private InternalData m_InternalData;
    }

    internal struct MouseModel
    {
        public struct InternalData
        {
            /// <summary>
            /// This tracks the current GUI targets being hovered over.  Syncs up to <see cref="PointerEventData.hovered"/>.
            /// </summary>
            public InlinedArray<GameObject> hoverTargets { get; set; }

            /// <summary>
            ///  Tracks the current enter/exit target being hovered over at any given moment. Syncs up to <see cref="PointerEventData.pointerEnter"/>.
            /// </summary>
            public GameObject pointerTarget { get; set; }

            public void Reset()
            {
                pointerTarget = null;
                hoverTargets = new InlinedArray<GameObject>();
            }
        }

        /// <summary>
        /// An Id representing a unique pointer.  See <see cref="UnityEngine.InputSystem.Pointer.pointerId"/> for more details.
        /// </summary>
        public int pointerId { get; private set; }

        /// <summary>
        /// Used by InputSystemUIInputModel to map this MouseModel to a device, to map incoming action callbacks to pointer models
        /// (so we can handle multiple independent pointers/touches).
        /// </summary>
        public InputDevice device { get; private set; }

        /// <summary>
        /// Used by InputSystemUIInputModel to map this MouseModel to a touch, to map incoming action callbacks to pointer models
        /// (so we can handle multiple independent pointers/touches).
        /// </summary>
        public int touchId { get; private set; }

        /// <summary>
        /// A flag representing whether any mouse data has changed this frame, meaning that events should be processed.
        /// </summary>
        /// <remarks>
        /// This only checks for changes in mouse state (<see cref="position"/>, <see cref="leftButton"/>, <see cref="rightButton"/>, <see cref="middleButton"/>, or <see cref="scrollDelta"/>).
        /// </remarks>
        public bool changedThisFrame { get; private set; }

        public Vector2 position
        {
            get { return m_Position; }
            set
            {
                if (m_Position != value)
                {
                    deltaPosition = value - m_Position;
                    m_Position = value;
                    changedThisFrame = true;
                }
            }
        }

        /// <summary>
        /// The pixel-space change in <see cref="position"/> since the last call to <see cref="OnFrameFinished"/>.
        /// </summary>
        public Vector2 deltaPosition { get; private set; }

        /// <summary>
        /// The scroll delta input value from a mouse wheel or scroll gesture.
        /// </summary>
        /// <remarks>
        /// For backwards compatibility with the UI event system, this value is represented in "lines of text", where
        /// the height of a line is defined as 20 pixels.
        /// </remarks>
        public Vector2 scrollDelta
        {
            get { return m_ScrollDelta; }
            set
            {
                if (m_ScrollDelta != value)
                {
                    changedThisFrame = true;
                    m_ScrollDelta = value;
                }
            }
        }


        /// <summary>
        /// Cached data and button state representing a left mouse button on a mouse.  Used by uGUI to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel leftButton
        {
            get
            {
                return m_LeftButton;
            }
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_LeftButton = value;
            }
        }

        /// <summary>
        /// Cached data and button state representing a right mouse button on a mouse.  Used by uGUI to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel rightButton
        {
            get
            {
                return m_RightButton;
            }
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_RightButton = value;
            }
        }

        /// <summary>
        /// Cached data and button state representing a middle mouse button on a mouse.  Used by uGUI to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel middleButton
        {
            get
            {
                return m_MiddleButton;
            }
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_MiddleButton = value;
            }
        }

        public MouseModel(int pointerId, InputDevice device, int touchId)
        {
            this.pointerId = pointerId;
            this.device = device;
            this.touchId = touchId;
            changedThisFrame = false;
            m_Position = deltaPosition = m_ScrollDelta = Vector2.zero;

            m_LeftButton = new MouseButtonModel();
            m_RightButton = new MouseButtonModel();
            m_MiddleButton = new MouseButtonModel();
            m_LeftButton.Reset();
            m_RightButton.Reset();
            m_MiddleButton.Reset();

            m_InternalData = new InternalData();
            m_InternalData.Reset();
            m_InternalData.pointerTarget = null;
            m_InternalData.hoverTargets = new InlinedArray<GameObject>();
        }

        /// <summary>
        /// Call this at the end of polling for per-frame changes.  This resets delta values, such as <see cref="deltaPosition"/>, <see cref="scrollDelta"/>, and <see cref="MouseButtonModel.lastFrameDelta"/>.
        /// </summary>
        public void OnFrameFinished()
        {
            changedThisFrame = false;
            deltaPosition = scrollDelta = Vector2.zero;
            m_LeftButton.OnFrameFinished();
            m_RightButton.OnFrameFinished();
            m_MiddleButton.OnFrameFinished();
        }

        public void CopyTo(PointerEventData eventData)
        {
            eventData.pointerId = pointerId;
            eventData.position = position;
            eventData.delta = deltaPosition;
            eventData.scrollDelta = scrollDelta;

            eventData.pointerEnter = m_InternalData.pointerTarget;
            eventData.hovered.Clear();
            eventData.hovered.AddRange(m_InternalData.hoverTargets);

            // This is unset in legacy systems and can safely assumed to stay true.
            eventData.useDragThreshold = true;
        }

        public void CopyFrom(PointerEventData eventData)
        {
            var hoverTargets = m_InternalData.hoverTargets;
            hoverTargets.ClearWithCapacity();
            hoverTargets.Append(eventData.hovered);
            m_InternalData.hoverTargets = hoverTargets;
            m_InternalData.pointerTarget = eventData.pointerEnter;
        }

        private Vector2 m_Position;
        private Vector2 m_ScrollDelta;
        private MouseButtonModel m_LeftButton;
        private MouseButtonModel m_RightButton;
        private MouseButtonModel m_MiddleButton;

        internal GameObject pointerTarget => m_InternalData.pointerTarget;

        private InternalData m_InternalData;
    }
}
