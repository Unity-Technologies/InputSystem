using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RedImageOnButtonHeld : MonoBehaviour
{
    public string buttonName = "Fire1";

    Image m_Image;

    void Start()
    {
        m_Image = GetComponent<Image>();
    }

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
