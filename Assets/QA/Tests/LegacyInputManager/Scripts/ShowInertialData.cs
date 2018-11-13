using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowInertialData : MonoBehaviour
{
    public enum IMUDisplaySelect
    {
        Acceleration,
        Gyroscope,
        Compass
    }

    public IMUDisplaySelect displaySelect;

    Text m_Text;

    void Start()
    {
        m_Text = GetComponent<Text>();

        switch (displaySelect)
        {
            case IMUDisplaySelect.Acceleration:
                if (!SystemInfo.supportsAccelerometer) { m_Text.color = Color.red; }
                break;
            case IMUDisplaySelect.Gyroscope:
                if (!SystemInfo.supportsGyroscope) { m_Text.color = Color.red; }
                break;
            case IMUDisplaySelect.Compass:
                Input.compass.enabled = true;
                if (!Input.compass.enabled) { m_Text.color = Color.red; }
                break;
            default:
                Debug.Log("Error - not a valid IMUDisplaySelect");
                break;
        }
    }

    void Update()
    {
        Vector3 dataToDisplay;
        switch (displaySelect)
        {
            case IMUDisplaySelect.Acceleration:
                dataToDisplay = Input.acceleration;
                break;
            case IMUDisplaySelect.Gyroscope:
                dataToDisplay = Input.gyro.attitude.eulerAngles;
                break;
            case IMUDisplaySelect.Compass:
                dataToDisplay = Input.compass.rawVector;
                break;
            default:
                Debug.Log("Error - not a valid IMUDisplaySelect");
                dataToDisplay = Vector3.zero;
                break;
        }

        m_Text.text = dataToDisplay.x.ToString("+000.000; -000.000; +000.000") + ", " +
            dataToDisplay.y.ToString("+000.000; -000.000; +000.000") + ", " +
            dataToDisplay.z.ToString("+000.000; -000.000; +000.000");
    }
}
