using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HingeAngleTest : MonoBehaviour
{
    public Text info;
    void Start()
    {
        if (HingeAngle.current != null)
            InputSystem.EnableDevice(HingeAngle.current);
    }

    // Update is called once per frame
    void Update()
    {
        if (HingeAngle.current != null)
            info.text = $"Angle: {HingeAngle.current.angle.ReadValue()}";
    }
}
