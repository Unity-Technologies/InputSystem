using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Flags]
public enum ButtonDeltaState
{
    NoChange = 0,
    Pressed = 1,
    Released = 2,
}

public struct MockMouseButton
{
    public bool isDown
    {
        get
        {
            return m_IsDown;
        }
        set
        {
            m_IsDown = value;
            m_LastFrameDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
        }
    }

    public ButtonDeltaState lastFrameDelta
    {
        get
        {
            return m_LastFrameDelta;
        }
    }

    private ButtonDeltaState m_LastFrameDelta;

    public bool isDragging { get; set; }
    public float pressedTime { get; set; }

    public Vector2 pressedPosition { get; set; }
    public RaycastResult pressedRaycast { get; set; }
    public GameObject pressedGameObject { get; set; }
    public GameObject pressedGameObjectRaw { get; set; }
    public GameObject draggedGameObject { get; set; }

    public void Reset()
    {
        m_LastFrameDelta = ButtonDeltaState.NoChange;
        m_IsDown = isDragging = false;
        pressedTime = 0.0f;
        pressedPosition = Vector2.zero;
        pressedRaycast = new RaycastResult();
        pressedGameObject = pressedGameObjectRaw = draggedGameObject = null;
    }

    public void OnFrameFinished()
    {
        m_LastFrameDelta = ButtonDeltaState.NoChange;
    }

    public void CopyTo(PointerEventData eventData)
    {
        eventData.dragging = isDragging;
        eventData.clickTime = pressedTime;
        eventData.pressPosition = pressedPosition;
        eventData.pointerPressRaycast = pressedRaycast;
        eventData.pointerPress = pressedGameObject;
        eventData.rawPointerPress = pressedGameObjectRaw;
        eventData.pointerDrag = draggedGameObject;
    }

    public void CopyFrom(PointerEventData eventData)
    {
        isDragging = eventData.dragging;
        pressedTime = eventData.clickTime;
        pressedPosition = eventData.pressPosition;
        pressedRaycast = eventData.pointerPressRaycast;
        pressedGameObject = eventData.pointerPress;
        pressedGameObjectRaw = eventData.rawPointerPress;
        draggedGameObject = eventData.pointerDrag;
    }

    private bool m_IsDown;

}

public struct MockMouseState
{
    /// ///////////////////////////////////////
 
    public List<GameObject> hoverTargets { get; set; }
    public GameObject pointerTarget { get; set; }

    /// ///////////////////////////////////////

    public int pointerId { get; private set; }

    public bool changedThisFrame { get; private set; }

    public Vector2 position
    {
        get { return m_Position; }
        set
        {
            m_Position = value;
            deltaPosition += value;
            changedThisFrame = true;
        }
    }

    public Vector2 deltaPosition { get; private set; }

    public Vector2 scrollDelta
    {
        get
        {
            return m_ScrollDelta;
        }
        set
        {
            m_ScrollDelta = value;
            changedThisFrame = true;
        }
    }

    public MockMouseButton leftButton
    {
        get
        {
            return m_LeftButton;
        }
        set
        {
            m_LeftButton = value;
            changedThisFrame = true;
        }
    }

    public MockMouseButton rightButton
    {
        get
        {
            return m_RightButton;
        }
        set
        {
            m_RightButton = value;
            changedThisFrame = true;
        }
    }

    public MockMouseButton middleButton
    {
        get
        {
            return m_MiddleButton;
        }
        set
        {
            m_MiddleButton = value;
            changedThisFrame = true;
        }
    }

    public MockMouseState(EventSystem eventSystem, int pointerId)
    {
        this.pointerId = pointerId;
        changedThisFrame = false;
        m_Position = deltaPosition = m_ScrollDelta = Vector2.zero;

        m_LeftButton = new MockMouseButton();
        m_RightButton = new MockMouseButton();
        m_MiddleButton = new MockMouseButton();
        m_LeftButton.Reset();
        m_RightButton.Reset();
        m_MiddleButton.Reset();

        pointerTarget = null;
        hoverTargets = new List<GameObject>();
    }

    public void OnFrameFinished()
    {
        changedThisFrame = false;
        deltaPosition = m_ScrollDelta = Vector2.zero;
        m_LeftButton.OnFrameFinished();
        m_RightButton.OnFrameFinished();
        m_MiddleButton.OnFrameFinished();
    }

    private Vector2 m_Position;
    private Vector2 m_ScrollDelta;
    private MockMouseButton m_LeftButton;
    private MockMouseButton m_RightButton;
    private MockMouseButton m_MiddleButton;
}
