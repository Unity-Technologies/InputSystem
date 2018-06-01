using System;
using UnityEngine;

[Serializable]
public class AnalogStick
{
    //-------------------------------------------------------------------------------------------
    // For Input Manager:
    // Each Analog Stck has 2 different axis associated with it
    // One is control by the stick's movement in X direction; the other is controled by Y.
    // For Input System:
    // This is only needed for position adjustment
    //-------------------------------------------------------------------------------------------

    private Transform stick;            // The moving part of the analog stick. It is the child object named "Stick";
    private string name;                // The name for the Transform Stick. It should make sense for the controller used, such as "Left_Stick" for Xbox controller.
    private string x_axis_name;         // The Axis controlled through the stick's movement in X direction. The name is set in Input Manager.
    private string y_axis_name;         // The Axis controlled through the stick's movement in Y direction. The name is set in Input Manager.
    private bool is_y_reversed = false; // In case the Y axis is reversed, like for Input Manager.
    private bool is_x_reversed = false; // In case the X axis is reversed. Probably not useful.

    private float max_move_distance;    // The distance of the transform can move in each direction
    private Vector3 original_position;  // The stick's initial position in the scene

    public string Name { get { return name; } }
    public string X_Axis_Name { get { return x_axis_name; } }
    public string Y_Axis_Name { get { return y_axis_name; } }
    public float Max_Move_Distance { get { return max_move_distance; } }
    public Transform Stick
    {
        get { return stick; }
        set
        {
            name = value.name;
            stick = value.Find("Stick");
            original_position = stick.position;
        }
    }

    // For Input Manager Initialization
    public AnalogStick(Transform stck, string XName, string YName, float maxDistance = 0.5f, bool isYReversed = false)
    {
        x_axis_name = XName;
        y_axis_name = YName;
        max_move_distance = maxDistance;
        Stick = stck;
        is_y_reversed = isYReversed;
    }

    // For Input System Initialization
    public AnalogStick(Transform stck, float maxDistance = 0.5f, bool isYReversed = false)
    {
        Stick = stck;
        max_move_distance = maxDistance;
        is_y_reversed = isYReversed;
    }

    // Update the stick position according to the input value
    public void UpdatePosition(float xValue, float yValue)
    {
        if (is_x_reversed) xValue *= -1;
        if (is_y_reversed) yValue *= -1;
        Vector3 adjust = new Vector3(xValue * max_move_distance, yValue * max_move_distance, 0f);
        stick.position = original_position + adjust;
    }

    public void UpdatePosition(Vector2 pos)
    {
        UpdatePosition(pos.x, pos.y);
    }
}
