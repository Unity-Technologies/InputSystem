using ISX;
using UnityEngine;

public class GamepadDebugUI : MonoBehaviour
{
    public void OnGUI()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null)
        {
            GUILayout.Label("No gamepad connected.");
            return;
        }

        GUI.contentColor = Color.black;
        GUILayout.Label($"Left Stick: {gamepad.leftStick.value}");
        GUILayout.Label($"Right Stick: {gamepad.rightStick.value}");
        GUILayout.Label($"A Button: {gamepad.aButton.value}");
        GUILayout.Label($"B Button: {gamepad.bButton.value}");
        GUILayout.Label($"X Button: {gamepad.xButton.value}");
        GUILayout.Label($"Y Button: {gamepad.yButton.value}");
        GUILayout.Label($"Left Shoulder Button: {gamepad.leftShoulder.value}");
        GUILayout.Label($"Right Shoulder Button: {gamepad.rightShoulder.value}");
    }
}
