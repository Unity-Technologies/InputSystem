using UnityEngine;
using UnityEngine.InputSystem.Android;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AndroidHingeAngleTest : MonoBehaviour
{
    public Text info;
    void Start()
    {
        if (AndroidHingeAngle.current != null)
            InputSystem.EnableDevice(AndroidHingeAngle.current);
    }

    // Update is called once per frame
    void Update()
    {
        if (AndroidHingeAngle.current != null)
            info.text = $"Angle: {AndroidHingeAngle.current.angle.ReadValue()}";
    }
}
