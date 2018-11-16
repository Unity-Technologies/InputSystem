using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class NumberOfTouches : MonoBehaviour
{
    Text m_NumTouchesText;

    void Start()
    {
        m_NumTouchesText = GetComponent<Text>();
    }

    void Update()
    {
        m_NumTouchesText.text = Input.touchCount.ToString();
    }
}
