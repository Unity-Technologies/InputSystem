using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class NumberOfTouches : MonoBehaviour
{
    Text m_NumTouchesText;

    // Use this for initialization
    void Start()
    {
        m_NumTouchesText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        m_NumTouchesText.text = Input.touchCount.ToString();
    }
}
