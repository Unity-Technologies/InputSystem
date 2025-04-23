using UnityEngine;
using UnityEngine.InputSystem;

public class ActionDebug : MonoBehaviour
{
    public InputActionReference trigger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trigger.action.performed += ActionOnPerformed;
        trigger.action.canceled += ActionOnCanceled;
        trigger.action.started += ActionOnStarted;
        trigger.action.Enable();
    }

    private void ActionOnStarted(InputAction.CallbackContext obj)
    {
        Debug.Log("Action Started");
    }

    private void ActionOnCanceled(InputAction.CallbackContext obj)
    {
        Debug.Log("Action Canceled");
    }

    private void ActionOnPerformed(InputAction.CallbackContext obj)
    {
        Debug.Log("Action Performed");
    }

    // Update is called once per frame
    void Update()
    {
        if (trigger.action.WasPerformedThisFrame())
            Debug.Log("Action Performed (Polled Event)");
    }
}
