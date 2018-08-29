using UnityEngine;

public class ControllerDiagramForInputManager : GamepadForInputManager
{
    // Update is called once per frame
    void Update()
    {
        UpdateAllButtons();

        // Only support the first 10 axles. Axles from 11th won't be able to show in this project
        for (int i = 1; i <= 10; i++)
        {
            string axisName = "Axis " + i;
            UpdateAxisValue(axisName);
        }
    }

    private void UpdateAxisValue(string axisName)
    {
        float value = Input.GetAxis(axisName);
        Transform axis = m_buttonContainer.Find(axisName);
        axis.GetComponent<TextMesh>().text = value.ToString("F2");
    }
}
