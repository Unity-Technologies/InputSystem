using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[ExecuteInEditMode]
public class TestMouseDelta : MonoBehaviour
{
    [Serializable]
    class TestState
    {
        public Vector2 m_PreviousMousePosition;
        public Vector2 m_PreviousDelta;
    }

    [SerializeField]
    TestState m_DynamicTestState;
    
    [SerializeField]
    TestState m_EditorTestState;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        InputSystem.onAfterUpdate += AfterInputSystemUpdate;
    }

    // Update is called once per frame
    void OnDisable()
    {
        InputSystem.onAfterUpdate -= AfterInputSystemUpdate;
    }
    
    void AfterInputSystemUpdate()
    {
        var updateType = InputState.currentUpdateType;
        var mouse = Pointer.current;
        switch (updateType)
        {
            case InputUpdateType.Editor:
            {
                CheckState(mouse, ref m_EditorTestState);
                break;
            }
            case InputUpdateType.Dynamic:
            {
                CheckState(mouse, ref m_DynamicTestState);
                break;
            }
        }
    }

    static void CheckState(Pointer mouse, ref TestState state)
    {
        state.m_PreviousMousePosition = mouse.position.ReadValue();
        state.m_PreviousDelta = mouse.delta.ReadValue();
    }
}
