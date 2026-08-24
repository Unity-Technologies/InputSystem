using UnityEditor;
using UnityEngine.InputSystem.Editor;
#region boat
using UnityEngine;
using UnityEngine.InputSystem;

public class Boat : MonoBehaviour
{
    void OnMove(InputValue value)
    {
        // The X value will be used to rotate the boat
        var stick = value.Get<Vector2>();
        var direction = stick.x;
        transform.Rotate(Vector3.up, direction);
        // To move the boat forwards, this code block uses the Y value of the stick
        var speed = stick.y;
        transform.Translate(new Vector3(0, 0, speed), Space.Self);
    }
}
#endregion

class ProcessorsExamples : MonoBehaviour
{
    void Start()
    {
        #region processors
        // This references the processor registered as "scale" and sets its "factor"
        // parameter (a floating-point value) to a value of 2.5.
        var singleProcessor = "scale(factor=2.5)";

        // Multiple processors can be chained together. They are processed
        // from left to right.
        // Example: First invert the value, then normalize [0..10] values to [0..1].
        var chainedProcessors = "invert,normalize(min=0,max=10)";
        #endregion
    }
}

#region myvalueprocessor
public class MyValueShiftProcessor : InputProcessor<float>
{
    [Tooltip("Number to add to incoming values.")]
    public float valueShift = 0;

    public override float Process(float value, InputControl control)
    {
        return value + valueShift;
    }
}
#endregion

#region customizeUI
// No registration is necessary for an InputParameterEditor.
// The system automatically finds subclasses based on the
// <..> type parameter.
#if UNITY_EDITOR
public class MyValueShiftProcessorEditor : InputParameterEditor<MyValueShiftProcessor>
{
    private GUIContent m_SliderLabel = new GUIContent("Shift By");

    protected override void OnEnable()
    {
        // Put initialization code here. Use 'target' to refer
        // to the instance of MyValueShiftProcessor that is being
        // edited.
    }

    public override void OnGUI()
    {
        // Define your custom UI here using EditorGUILayout.
        target.valueShift = EditorGUILayout.Slider(m_SliderLabel,
            target.valueShift, 0, 10);
    }
}
#endif
#endregion
