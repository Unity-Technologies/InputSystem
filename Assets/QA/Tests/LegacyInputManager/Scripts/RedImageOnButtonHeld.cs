using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RedImageOnButtonHeld : MonoBehaviour
{
    public string buttonName = "Fire1";

    Image m_Image;

    // Use this for initialization
    void Start()
    {
        m_Image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton(buttonName))
        {
            m_Image.color = Color.red;
        }
        else
        {
            m_Image.color = Color.white;
        }
    }
}
