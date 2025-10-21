using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class LightbarSettings : MonoBehaviour
{
    public Color SetColor;

    public void ChangeColor()
    {
        var gamepad = DualShockGamepad.current;
        if (gamepad != null)
        {
            Debug.Log("Current gamepad: " + gamepad);
            gamepad.SetLightBarColor(SetColor);
        }
    }
}
