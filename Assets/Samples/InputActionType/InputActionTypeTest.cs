using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class InputActionTypeTest : MonoBehaviour
{
    [SerializeField] public GameObject cube;
    public int forceJump = 5;

    Rigidbody m_CubeRigidbody;
    float m_previousPressedValue = 0.0f;
    bool  m_ControlReleased = false;

    InputAction move;
    InputAction colorChange;
    InputAction moveWithEvents;
    InputAction jump;
    Vector2 moveValueFromEvent;

    // Start is called before the first frame update
    void Start()
    {
        m_CubeRigidbody = cube.GetComponent<Rigidbody>();
        // Project-Wide Actions
        if (InputSystem.actions)
        {
            move = InputSystem.actions.FindAction("Player/Move");
            if (move == null)
            {
                Debug.Log("Move action not found. Looking for MoveWithEvents");
                moveWithEvents = InputSystem.actions.FindAction("Player/MoveWithEvents");
            }

            colorChange = InputSystem.actions.FindAction("Player/ColorChange");
            jump = InputSystem.actions.FindAction("Player/Jump");
        }
        else
        {
            Debug.Log("Setup Project Wide Input Actions in the Player Settings, Input System section");
        }

        // Handle input by responding to callbacks
        if (colorChange != null)
        {
            colorChange.started += OnColorChangeStarted;
            colorChange.performed += OnColorChangePerformed;
            colorChange.canceled += OnColorChangeCanceled;
        }

        if (moveWithEvents != null)
        {
            moveWithEvents.performed += OnMoveWithEventsPerformed;
            moveWithEvents.canceled += OnMoveWithEventsCanceled;
        }

        if (jump != null)
        {
            jump.started += JumpReset;
            jump.canceled += JumpReset;
            jump.performed += JumpProcess;
        }
    }

    void JumpReset(InputAction.CallbackContext obj)
    {
        Debug.Log("Reset Jump");
        m_previousPressedValue = 0.0f;

        m_ControlReleased = false;
    }

    void JumpProcess(InputAction.CallbackContext ctx)
    {
        switch (ctx.interaction)
        {
            case TapInteraction:
                Debug.Log("Tap Interaction performed. Jumping...");
                m_CubeRigidbody.AddForce(Vector3.up * forceJump, ForceMode.Impulse);
                break;
            case SlowTapInteraction:
                Debug.Log("Slow Tap Interaction performed. Jumping HIGHER...");
                m_CubeRigidbody.AddForce(Vector3.up * forceJump * 2, ForceMode.Impulse);
                break;
            case PressInteraction pressInteraction:
                Debug.Log("Press Interaction performed. Press point: " + pressInteraction.pressPoint);
                m_CubeRigidbody.AddForce(Vector3.up * forceJump, ForceMode.Impulse);
                break;
            case ChangeInteraction:
                // Demo of how ChangeInteraction can be used to process specific logic
                // I didn't find a way to have this logic just with an existing interaction so decided
                // to build one
                var pressedValue = ctx.ReadValue<float>();
                Debug.Log("Pressed Value: " + pressedValue + " Previous Pressed Value: " + m_previousPressedValue);

                // Only perform the jump on a release of the trigger
                // This means the previous value was higher than the current value
                if (m_previousPressedValue >= pressedValue && m_ControlReleased == false)
                {
                    m_ControlReleased = true;
                    Debug.Log("Previous is bigger: " + m_previousPressedValue + " Current: " + pressedValue);
                    if (m_previousPressedValue >= 0.9f)
                    {
                        m_CubeRigidbody.AddForce(Vector3.up * forceJump * 2, ForceMode.Impulse);
                        Debug.Log("Stronger Press Interaction performed . Jumping HIGHER...");
                    }
                    else if (m_previousPressedValue >= 0.2f)
                    {
                        m_CubeRigidbody.AddForce(Vector3.up * forceJump, ForceMode.Impulse);
                        Debug.Log("Medium Press Interaction performed. Jumping...");
                    }
                }
                m_previousPressedValue = pressedValue;

                break;
        }
    }

    private void OnMoveWithEventsPerformed(InputAction.CallbackContext ctx)
    {
        moveValueFromEvent = ctx.ReadValue<Vector2>();
        Debug.Log("Device: " + ctx.action.activeControl.device.name + " Move action type performed: " + ctx.action.type + " ctrl type: " + ctx.action.expectedControlType + " value: " + moveValueFromEvent);
    }

    private void OnMoveWithEventsCanceled(InputAction.CallbackContext ctx)
    {
        moveValueFromEvent = Vector2.zero;
        Debug.Log("Device: " + ctx.action.activeControl.device.name + " Move action type canceled: " + ctx.action.type + " ctrl type: " + ctx.action.expectedControlType + " value: " + moveValueFromEvent);
    }

    private void OnColorChangeStarted(InputAction.CallbackContext ctx)
    {
        Debug.Log("Action type started: " + ctx.action.type + " ctrl type: " + ctx.action.expectedControlType);
        cube.GetComponent<Renderer>().material.color = Color.blue;
    }

    private void OnColorChangePerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Action type performed: " + ctx.action.type + " ctrl type: " + ctx.action.expectedControlType);
        cube.GetComponent<Renderer>().material.color = Color.red;
    }

    private void OnColorChangeCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log("Action type canceled: " + ctx.action.type + " ctrl type: " + ctx.action.expectedControlType);
        cube.GetComponent<Renderer>().material.color = Color.green;
    }

    void OnDestroy()
    {
        if (colorChange != null)
        {
            colorChange.started -= OnColorChangeStarted;
            colorChange.performed -= OnColorChangePerformed;
            colorChange.canceled -= OnColorChangeCanceled;
        }

        if (moveWithEvents != null)
        {
            moveWithEvents.performed -= OnMoveWithEventsPerformed;
            moveWithEvents.canceled -= OnMoveWithEventsCanceled;
        }

        if (jump != null)
        {
            jump.performed -= JumpProcess;
            jump.started -= JumpReset;
            jump.canceled -= JumpReset;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var moveVal = Vector2.zero;
        if (move != null)
        {
            // Handle input by polling each frame
            moveVal = move.ReadValue<Vector2>();
        }
        else if (moveWithEvents != null)
        {
            // Handle input by responding to events
            moveVal = moveValueFromEvent;
        }

        moveVal *= 10.0f * Time.deltaTime;
        cube.transform.Translate(new Vector3(moveVal.x, moveVal.y, 0));
    }
}
