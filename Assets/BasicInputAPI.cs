using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.InputSystem.Input;

public class BasicInputAPI : MonoBehaviour
{
    void Update()
    {
        // Scenario 1:
        // Do something when the left mouse button is clicked while the left control key is pressed
        if (Input.IsPressed(Inputs.Key_LeftCtrl) && Input.IsPressed(Inputs.Mouse_Left))
        {

        }
    }
}