using UnityEngine;
using UnityEngine.InputSystem;

public class ActionDebug : MonoBehaviour
{
    public InputActionReference trigger;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trigger.action.performed += ActionOnperformed;
        trigger.action.Enable();
    }

    private void ActionOnperformed(InputAction.CallbackContext obj)
    {
        Debug.Log("Action Performed");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
