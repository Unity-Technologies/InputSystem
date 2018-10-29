using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.UI
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

        /// <summary>
        /// A set of flags to identify the changes that have occured between calls of <see cref="OnFrameFinished"/>.
        /// </summary>
        internal ButtonDeltaState lastFrameDelta { get; private set; }

        /// <summary>
        /// Internal bookkeeping data used by the uGUI system.
        /// </summary>
        public InternalData internalData { get; set; }

        /// <summary>
        /// Set's this object to it's default, unused state.
        /// </summary>
        public void Reset()
        {
            lastFrameDelta = ButtonDeltaState.NoChange;
            m_IsDown = false;

            internalData.Reset();
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
            InternalData bookkeeping = internalData;
            eventData.dragging = bookkeeping.isDragging;
            eventData.clickTime = bookkeeping.pressedTime;
            eventData.pressPosition = bookkeeping.pressedPosition;
            eventData.pointerPressRaycast = bookkeeping.pressedRaycast;
            eventData.pointerPress = bookkeeping.pressedGameObject;
            eventData.rawPointerPress = bookkeeping.pressedGameObjectRaw;
            eventData.pointerDrag = bookkeeping.draggedGameObject;
        }

        /// <summary>
        /// Fills this object with the values from a <see cref="PointerEventData"/>.
        /// </summary>
        /// <param name="eventData">These objects are used to send data through the uGUI system.</param>
        public void CopyFrom(PointerEventData eventData)
        {
            InternalData bookkeeping = internalData;
            bookkeeping.isDragging = eventData.dragging;
            bookkeeping.pressedTime = eventData.clickTime;
            bookkeeping.pressedPosition = eventData.pressPosition;
            bookkeeping.pressedRaycast = eventData.pointerPressRaycast;
            bookkeeping.pressedGameObject = eventData.pointerPress;
            bookkeeping.pressedGameObjectRaw = eventData.rawPointerPress;
            bookkeeping.draggedGameObject = eventData.pointerDrag;
            internalData = bookkeeping;
        }

        private bool m_IsDown;
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
        }

        /// <summary>
        /// An Id representing a unique pointer.  See <see cref="UnityEngine.Experimental.Input.Pointer.pointerId"/> for more details.
        /// </summary>
        public int pointerId { get; private set; }

        /// <summary>
        /// A flag representing whether any mouse data has changed this frame, meaning that events should be processed.
        /// </summary>
        /// <remarks>
        /// This only checks for changes in mouse state (<see cref="position"/>, <see cref="leftButton"/>, <see cref="rightButton"/>, <see cref="middleButton"/>, or <see cref="scrollPosition"/>).
        /// </remarks>
        public bool changedThisFrame { get; private set; }

        public Vector2 position
        {
            get { return m_Position; }
            set
            {
                if (m_Position != value)
                {
                    changedThisFrame = true;
                    deltaPosition = value - m_Position;
                    m_Position = value;
                }
            }
        }

        /// <summary>
        /// The pixel-space change in <see cref="position"/> since the last call to <see cref="OnFrameFinished"/>.
        /// </summary>
        public Vector2 deltaPosition { get; private set; }

        public Vector2 scrollPosition
        {
            get { return m_ScrollPosition; }
            set
            {
                if (m_ScrollPosition != value)
                {
                    changedThisFrame = true;
                    scrollDelta = value - m_ScrollPosition;
                    m_ScrollPosition = value;
                }
            }
        }

        /// <summary>
        /// The change in <see cref="scrollPosition"/> since the last call to <see cref="OnFrameFinished"/>.
        /// </summary>
        public Vector2 scrollDelta { get; private set; }


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

        /// <summary>
        /// Internal bookkeeping data used by the uGUI system.
        /// </summary>
        public InternalData internalData { get; set; }

        public MouseModel(EventSystem eventSystem, int pointerId)
        {
            this.pointerId = pointerId;
            changedThisFrame = false;
            m_Position = deltaPosition = m_ScrollPosition = scrollDelta = Vector2.zero;

            m_LeftButton = new MouseButtonModel();
            m_RightButton = new MouseButtonModel();
            m_MiddleButton = new MouseButtonModel();
            m_LeftButton.Reset();
            m_RightButton.Reset();
            m_MiddleButton.Reset();

            InternalData bookkeeping = new InternalData();
            bookkeeping.pointerTarget = null;
            bookkeeping.hoverTargets = new InlinedArray<GameObject>();
            internalData = bookkeeping;
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

        private Vector2 m_Position;
        private Vector2 m_ScrollPosition;
        private MouseButtonModel m_LeftButton;
        private MouseButtonModel m_RightButton;
        private MouseButtonModel m_MiddleButton;
    }
}
