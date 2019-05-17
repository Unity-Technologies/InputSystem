using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Plugins.UI
{
    internal struct TouchModel
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

        public int pointerId { get; private set; }

        public PointerPhase selectPhase
        {
            get
            {
                return m_SelectPhase;
            }
            set
            {
                if (m_SelectPhase != value)
                {
                    if (value == PointerPhase.Began)
                        selectDelta |= ButtonDeltaState.Pressed;

                    if (value == PointerPhase.Ended || value == PointerPhase.Canceled)
                        selectDelta |= ButtonDeltaState.Released;

                    m_SelectPhase = value;

                    changedThisFrame = true;
                }
            }
        }

        public ButtonDeltaState selectDelta { get; private set; }

        public bool changedThisFrame { get; private set; }

        public Vector2 position
        {
            get
            {
                return m_Position;
            }
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

        public TouchModel(int pointerId)
        {
            this.pointerId = pointerId;

            m_Position = deltaPosition = Vector2.zero;

            m_SelectPhase = PointerPhase.Canceled;
            changedThisFrame = false;
            selectDelta = ButtonDeltaState.NoChange;

            m_InternalData = new InternalData();
            m_InternalData.Reset();
        }

        public void Reset()
        {
            m_Position = deltaPosition = Vector2.zero;
            changedThisFrame = false;
            selectDelta = ButtonDeltaState.NoChange;
            m_InternalData.Reset();
        }

        public void OnFrameFinished()
        {
            deltaPosition = Vector2.zero;
            selectDelta = ButtonDeltaState.NoChange;
            changedThisFrame = false;
        }

        public void CopyTo(PointerEventData eventData)
        {
            eventData.pointerId = pointerId;
            eventData.position = position;
            eventData.delta = ((selectDelta & ButtonDeltaState.Pressed) != 0) ? Vector2.zero : deltaPosition;

            eventData.pointerEnter = m_InternalData.pointerTarget;
            eventData.dragging = m_InternalData.isDragging;
            eventData.clickTime = m_InternalData.pressedTime;
            eventData.pressPosition = m_InternalData.pressedPosition;
            eventData.pointerPressRaycast = m_InternalData.pressedRaycast;
            eventData.pointerPress = m_InternalData.pressedGameObject;
            eventData.rawPointerPress = m_InternalData.pressedGameObjectRaw;
            eventData.pointerDrag = m_InternalData.draggedGameObject;

            eventData.hovered.Clear();
            eventData.hovered.AddRange(m_InternalData.hoverTargets);
        }

        public void CopyFrom(PointerEventData eventData)
        {
            m_InternalData.pointerTarget = eventData.pointerEnter;
            m_InternalData.isDragging = eventData.dragging;
            m_InternalData.pressedTime = eventData.clickTime;
            m_InternalData.pressedPosition = eventData.pressPosition;
            m_InternalData.pressedRaycast = eventData.pointerPressRaycast;
            m_InternalData.pressedGameObject = eventData.pointerPress;
            m_InternalData.pressedGameObjectRaw = eventData.rawPointerPress;
            m_InternalData.draggedGameObject = eventData.pointerDrag;

            var hoverTargets = m_InternalData.hoverTargets;
            hoverTargets.ClearWithCapacity();
            hoverTargets.Append(eventData.hovered);
            m_InternalData.hoverTargets = hoverTargets;
        }

        private PointerPhase m_SelectPhase;
        private Vector2 m_Position;

        private InternalData m_InternalData;
    }
}
