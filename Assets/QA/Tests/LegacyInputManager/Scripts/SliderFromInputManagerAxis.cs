using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderFromInputManagerAxis : MonoBehaviour
{
    public string axisName = "Horizontal";

    Slider m_Slider;

    void Start()
    {
        m_Slider = GetComponent<Slider>();
    }

    void Update()
    {
        m_Slider.value = Input.GetAxis(axisName);
    }
}
