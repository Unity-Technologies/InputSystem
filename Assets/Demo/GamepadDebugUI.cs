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
        GUILayout.Label($"Left Trigger: {gamepad.leftTrigger.value}");
        GUILayout.Label($"Right Trigger: {gamepad.rightTrigger.value}");
        GUILayout.Label($"A Button: {gamepad.aButton.value}");
        GUILayout.Label($"B Button: {gamepad.bButton.value}");
        GUILayout.Label($"X Button: {gamepad.xButton.value}");
        GUILayout.Label($"Y Button: {gamepad.yButton.value}");
        GUILayout.Label($"Dpad Up: {gamepad.dpad.up.value}");
        GUILayout.Label($"Dpad Down: {gamepad.dpad.down.value}");
        GUILayout.Label($"Dpad Left: {gamepad.dpad.left.value}");
        GUILayout.Label($"Dpad Right: {gamepad.dpad.right.value}");
        GUILayout.Label($"Left Shoulder: {gamepad.leftShoulder.value}");
        GUILayout.Label($"Right Shoulder: {gamepad.rightShoulder.value}");
        GUILayout.Label($"Left Stick Press: {gamepad.leftStickButton.value}");
        GUILayout.Label($"Right Stick Press: {gamepad.rightStickButton.value}");
        GUILayout.Label($"Start Button: {gamepad.startButton.value}");
        GUILayout.Label($"Select Button: {gamepad.selectButton.value}");
    }
}
