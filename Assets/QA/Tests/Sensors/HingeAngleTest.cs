using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HingeAngleTest : MonoBehaviour
{
    [Serializable]
    class SensorCapabilities
    {
        public int sensorType;
        public float resolution;
        public int minDelay;
    }

    public Text info;
    SensorCapabilities caps;
    void Start()
    {
        if (HingeAngle.current != null)
        {
            InputSystem.EnableDevice(HingeAngle.current);
            caps = JsonUtility.FromJson<SensorCapabilities>(HingeAngle.current.description.capabilities);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (HingeAngle.current != null)
        {
            info.text = $"Capabilities: resolution = {caps.resolution}, minDelay = {caps.minDelay}\n" +
                $"Angle: {HingeAngle.current.angle.ReadValue()}";
        }
    }
}
