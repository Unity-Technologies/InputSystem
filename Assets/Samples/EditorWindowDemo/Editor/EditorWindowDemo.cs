using UnityEditor;
using UnityEngine.InputSystem;

public class EditorWindowDemo : EditorWindow
{
    [MenuItem("Window/Input System Editor Window Demo")]
    public static void Open()
    {
        GetWindow<EditorWindowDemo>();
    }

    protected void OnGUI()
    {
        // Grab the current pointer device (mouse, pen, touchscreen).
        var pointer = Pointer.current;
        if (pointer == null)
            return;

        // Pointer positions should automatically be converted to EditorWindow space of
        // the current window. Unlike player window coordinates, this uses UI window space,
        // i.e. Y goes top down rather than bottom up.
        var position = pointer.position.ReadValue();
        var pressure = pointer.pressure.ReadValue();
        var contact = pointer.press.isPressed;

        EditorGUILayout.LabelField($"Device: {pointer}");
        EditorGUILayout.LabelField($"Position: {position}");
        EditorGUILayout.LabelField($"Pressure: {pressure}");
        EditorGUILayout.LabelField($"Contact?: {contact}");

        // Just for kicks, also read out some data from the currently used gamepad (if any).
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Gamepad Left Stick: {gamepad.leftStick.ReadValue()}");
            EditorGUILayout.LabelField($"Gamepad Right Stick: {gamepad.leftStick.ReadValue()}");
        }

        // We want to constantly refresh to show the current values so trigger
        // another refresh right away. Otherwise, the values we show will only
        // update periodically.
        Repaint();
    }
}
