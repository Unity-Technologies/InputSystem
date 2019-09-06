using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventsystemPicker : MonoBehaviour
{
    public StandaloneInputModule m_oldSystem;
    public InputSystemUIInputModule m_newSystem;

    private IEnumerable<Toggle> m_toggles;

    // Start is called before the first frame update
    void Start()
    {
        m_toggles = GetComponent<ToggleGroup>().ActiveToggles();
        m_oldSystem.enabled = false;
        m_newSystem.enabled = true;

        foreach (Toggle toggle in m_toggles)
        {
            toggle.onValueChanged.AddListener(delegate {
                OnToggleChanged();
            });
            if (toggle.gameObject.name == "New")
                toggle.isOn = true;
        }
    }

    public void OnToggleChanged()
    {
        foreach (Toggle toggle in m_toggles)
        {
            if (toggle.isOn)
            {
                if (toggle.gameObject.name == "Old")
                {
                    m_newSystem.enabled = false;
                    m_oldSystem.enabled = true;
                }
                else
                {
                    m_oldSystem.enabled = false;
                    m_newSystem.enabled = true;
                }
            }
        }
    }
}
