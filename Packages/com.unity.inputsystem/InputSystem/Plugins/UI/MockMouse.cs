using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Flags]
internal enum ButtonDeltaState
{
    NoChange = 0,
    Pressed = 1,
    Released = 2,
}

internal struct MockMouseButtonEventData
{
    public MockMouseButtonEventData(EventSystem eventSystem, bool initialState)
    {
        m_CurrentState = initialState;
        m_DeltaState = ButtonDeltaState.NoChange;
        m_EventData = new PointerEventData(eventSystem);
    }

    void FlipFrame()
    {
        m_DeltaState = ButtonDeltaState.NoChange;
    }

    private bool m_CurrentState;
    private ButtonDeltaState m_DeltaState;

    private PointerEventData m_EventData;
}

public struct MockMouseState
{
    public int pointerId
    {
        get { return m_PointerId; }
    }

    public bool dirty
    {
        get { return m_ChangedThisFrame; }
    }

    public Vector2 position
    {
        get { return buttonEventData[(int)PointerEventData.InputButton.Left].position; }
        set
        {
            for(int i = 0; i < buttonEventData.Length; i++)
            {
                buttonEventData[i].position = value;
                buttonEventData[i].delta += value;
            }
            m_ChangedThisFrame = true;
        }
    }

    public Vector2 deltaPosition
    {
        get { return buttonEventData[(int)PointerEventData.InputButton.Left].delta; }
    }

    public Vector2 scrollDelta
    {
        get
        {
            return buttonEventData[(int)PointerEventData.InputButton.Left].scrollDelta;
        }
        set
        {
            for (int i = 0; i < buttonEventData.Length; i++)
            {
                buttonEventData[i].scrollDelta = value;
                m_ChangedThisFrame = true;
            }
            m_ChangedThisFrame = true;
        }
    }

    public bool leftButton
    {
        get
        {
            return m_LeftButton;
        }
        set
        {
            //buttonEventData[(int)PointerEventData.InputButton.Left].pointerPress
            m_LeftButton = value;
            m_LeftButtonDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
            m_ChangedThisFrame = true;
        }
    }

    public bool rightButton
    {
        get
        {
            return m_RightButton;
        }
        set
        {
            m_RightButton = value;
            m_RightButtonDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
            m_ChangedThisFrame = true;
        }
    }

    public bool middleButton
    {
        get
        {
            return m_MiddleButton;
        }
        set
        {
            m_MiddleButton = value;
            m_MiddleButtonDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
            m_ChangedThisFrame = true;
        }
    }

    public MockMouseState(EventSystem eventSystem, int pointerId)
    {
        m_PointerId = pointerId;
        m_ChangedThisFrame = false;
        m_Position = m_DeltaPosition = m_ScrollDelta = Vector2.zero;
        m_LeftButton = m_RightButton = m_MiddleButton = false;
        m_LeftButtonDelta = m_RightButtonDelta = m_MiddleButtonDelta = ButtonDeltaState.NoChange;

        buttonEventData = new PointerEventData[] { new PointerEventData(eventSystem), new PointerEventData(eventSystem), new PointerEventData(eventSystem) };
        PointerEventData leftData = buttonEventData[(int)PointerEventData.InputButton.Left];
        leftData.button = PointerEventData.InputButton.Left;
        leftData.pointerId = pointerId;

        PointerEventData rightData = buttonEventData[(int)PointerEventData.InputButton.Right];
        rightData.button = PointerEventData.InputButton.Right;
        rightData.pointerId = pointerId;

        PointerEventData middleData = buttonEventData[(int)PointerEventData.InputButton.Middle];
        middleData.button = PointerEventData.InputButton.Middle;
        middleData.pointerId = pointerId;


    }

    public void ClearDirty()
    {
        m_DeltaPosition = m_ScrollDelta = Vector2.zero;
        m_ChangedThisFrame = false;
        m_LeftButtonDelta = m_RightButtonDelta = m_MiddleButtonDelta = ButtonDeltaState.NoChange;
    }

    private int m_PointerId;
    private bool m_ChangedThisFrame;

    private Vector2 m_Position;
    private Vector2 m_DeltaPosition;
    private Vector2 m_ScrollDelta;
    private bool m_LeftButton;
    private ButtonDeltaState m_LeftButtonDelta;
    private bool m_RightButton;
    private ButtonDeltaState m_RightButtonDelta;
    private bool m_MiddleButton;
    private ButtonDeltaState m_MiddleButtonDelta;

    private PointerEventData[] buttonEventData;
}
