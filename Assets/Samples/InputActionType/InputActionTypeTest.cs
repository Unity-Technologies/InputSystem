using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionTypeTest : MonoBehaviour
{
    [SerializeField] public GameObject cube;

    InputAction move;
    InputAction colorChange;
    InputAction moveWithEvents;
    Vector2 moveValueFromEvent;

    // Start is called before the first frame update
    void Start()
    {
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
