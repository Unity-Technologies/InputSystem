using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySliderValue : MonoBehaviour
{
    public Slider slider;
    public Text text;

    void Update()
    {
        if (slider == null || text == null)
            return;

        text.text = slider.value.ToString();
    }
}
