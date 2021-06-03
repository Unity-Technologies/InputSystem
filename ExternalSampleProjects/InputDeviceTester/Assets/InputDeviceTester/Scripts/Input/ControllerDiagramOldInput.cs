using UnityEngine;

public class ControllerDiagramOldInput : GamepadOldInput
{
#if ENABLE_LEGACY_INPUT_MANAGER
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

    protected override void StartHighlightButton(string buttonName)
    {
        Transform button = m_buttonContainer.Find(buttonName);
        if (button == null)
            ShowMessage(buttonName);
        else
        {
            RemoveTransparency(button);
            ParticleSystem ps = button.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                Instantiate(m_buttonHighlight, button.position - new Vector3(0f, 0f, 0.1f), button.rotation, button);
            else
                ps.Play();
        }
    }

    private void UpdateAxisValue(string axisName)
    {
        float value = Input.GetAxis(axisName);
        Transform axis = m_buttonContainer.Find(axisName);
        axis.GetComponent<TextMesh>().text = value.ToString("F2");

        if (value != 0f)
            RemoveTransparency(axis);
    }

    private void RemoveTransparency(Transform controlTrans)
    {
        // Remove transparency from all the Sprite Renderers
        foreach (SpriteRenderer sr in controlTrans.GetComponentsInChildren<SpriteRenderer>())
            sr.color = RemoveColorTranparency(sr.color);

        // Remove transparency from the text mesh and change text to the transform's name
        foreach (TextMesh tm in controlTrans.GetComponentsInChildren<TextMesh>())
            tm.color = RemoveColorTranparency(tm.color);
    }

    private Color RemoveColorTranparency(Color color)
    {
        color.a = 1f;
        return color;
    }

#endif
}
