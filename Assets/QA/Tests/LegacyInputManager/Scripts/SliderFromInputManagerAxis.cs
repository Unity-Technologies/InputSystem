using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderFromInputManagerAxis : MonoBehaviour {

    public string axisName = "Horizontal";

    Slider m_Slider;

	// Use this for initialization
	void Start () {
        m_Slider = GetComponent<Slider>();
	}
	
	// Update is called once per frame
	void Update () {
        m_Slider.value = Input.GetAxis(axisName);
	}
}
