using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPositionFromMouse : MonoBehaviour {

    RectTransform m_RectTransform;

    void Start()
    {
        m_RectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update () {
        m_RectTransform.position = Input.mousePosition; //new Vector3(Input.mousePosition.x / (float)Screen.width,
                                     //          Input.mousePosition.y / (float)Screen.height,
                                       //        0);
	}
}
