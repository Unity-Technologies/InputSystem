using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class MotorSettings : MonoBehaviour
{
    [Range(0, 1)] public float lowFrequencyMotorSpeed; 
    [Range(0, 1)] public float highFrequencyMotorSpeed; 

    public void SetMotorSpeeds()
    {
        var gamepad = DualShockGamepad.current;
        if (gamepad != null)
        {
            Debug.Log("Current gamepad: " + gamepad);
            gamepad.SetMotorSpeeds(lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
        }
    }

    private void OnDisable()
    {
        var gamepad = DualShockGamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0, 0);
        }
    }
}
