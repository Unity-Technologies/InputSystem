using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.UI
{
    internal class TrackedPointerEventData : PointerEventData
    {
        public TrackedPointerEventData(EventSystem eventSystem)
            : base(eventSystem)
        {}

        public Ray ray { get; set; }
        public float maxDistance { get; set; }
    }

    internal struct TrackedDeviceModel
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

            public Vector2? lastFrameScreenPosition { get; set; }

            /// <summary>
            /// Resets this object to it's default, unused state.
            /// </summary>
            public void Reset()
            {
                isDragging = false;
                pressedTime = 0.0f;
                pressedPosition = Vector2.zero;
                lastFrameScreenPosition = null;
                pressedRaycast = new RaycastResult();
                pressedGameObject = pressedGameObjectRaw = draggedGameObject = null;
            }
        }

        public int pointerId { get; private set; }

        public InputDevice device { get; private set; }

        public bool select
        {
            get
            {
                return m_SelectDown;
            }
            set
            {
                if (m_SelectDown != value)
                {
                    m_SelectDown = value;
                    selectDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
                    changedThisFrame = true;
                }
            }
        }
        public ButtonDeltaState selectDelta { get; private set; }

        public bool changedThisFrame { get; private set; }

        public Vector3 position
        {
            get
            {
                return m_Position;
            }
            set
            {
                if (m_Position != value)
                {
                    m_Position = value;
                    changedThisFrame = true;
                }
            }
        }

        public Quaternion orientation
        {
            get
            {
                return m_Orientation;
            }
            set
            {
                if (m_Orientation != value)
                {
                    m_Orientation = value;
                    changedThisFrame = true;
                }
            }
        }

        public TrackedDeviceModel(int pointerId, InputDevice device)
        {
            this.pointerId = pointerId;
            this.device = device;

            m_Orientation = Quaternion.identity;
            m_Position = Vector3.zero;
            m_SelectDown = changedThisFrame = false;
            selectDelta = ButtonDeltaState.NoChange;

            m_InternalData = new InternalData();
            m_InternalData.Reset();
        }

        public void Reset()
        {
            m_Orientation = Quaternion.identity;
            m_Position = Vector3.zero;
            m_SelectDown = changedThisFrame = false;
            selectDelta = ButtonDeltaState.NoChange;
            m_InternalData.Reset();
        }

        public void OnFrameFinished()
        {
            selectDelta = ButtonDeltaState.NoChange;
            changedThisFrame = false;
        }

        public void CopyTo(TrackedPointerEventData eventData)
        {
            eventData.ray = new Ray(m_Position, m_Orientation * Vector3.forward);
            eventData.maxDistance = 1000;
            // Demolish the position so we don't trigger any checks from the Graphics Raycaster.
            eventData.position = new Vector2(float.MinValue, float.MinValue);

            eventData.pointerEnter = m_InternalData.pointerTarget;
            eventData.dragging = m_InternalData.isDragging;
            eventData.clickTime = m_InternalData.pressedTime;
            eventData.pressPosition = m_InternalData.pressedPosition;
            eventData.pointerPressRaycast = m_InternalData.pressedRaycast;
            eventData.pointerPress = m_InternalData.pressedGameObject;
            eventData.rawPointerPress = m_InternalData.pressedGameObjectRaw;
            eventData.pointerDrag = m_InternalData.draggedGameObject;
            eventData.pointerId = pointerId;

            eventData.hovered.Clear();
            eventData.hovered.AddRange(m_InternalData.hoverTargets);
        }

        public void CopyFrom(TrackedPointerEventData eventData)
        {
            m_InternalData.pointerTarget = eventData.pointerEnter;
            m_InternalData.isDragging = eventData.dragging;
            m_InternalData.pressedTime = eventData.clickTime;
            m_InternalData.pressedPosition = eventData.pressPosition;
            m_InternalData.pressedRaycast = eventData.pointerPressRaycast;
            m_InternalData.pressedGameObject = eventData.pointerPress;
            m_InternalData.pressedGameObjectRaw = eventData.rawPointerPress;
            m_InternalData.draggedGameObject = eventData.pointerDrag;
            pointerId = eventData.pointerId;

            var hoverTargets = m_InternalData.hoverTargets;
            hoverTargets.ClearWithCapacity();
            hoverTargets.Append(eventData.hovered);
            m_InternalData.hoverTargets = hoverTargets;
        }

        private bool m_SelectDown;
        private Vector3 m_Position;
        private Quaternion m_Orientation;

        private InternalData m_InternalData;
    }
}
