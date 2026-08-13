using UnityEngine;
using UnityEngine.InputSystem;

class ControlActuation : MonoBehaviour
{
    void Example(){

    #region actuation
    // Check if leftStick is currently actuated.
    if (Gamepad.current.leftStick.IsActuated())
        Debug.Log("Left Stick is actuated");
    #endregion

    #region actuation2
    // Check if left stick is actuated more than a quarter of its motion range.
    if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
        Debug.Log("Left Stick actuated past 25%");
    #endregion

    }

}