using UnityEngine;

public class SetPositionFromMouse : MonoBehaviour
{
    RectTransform m_RectTransform;

    void Start()
    {
        m_RectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        m_RectTransform.position = Input.mousePosition; //new Vector3(Input.mousePosition.x / (float)Screen.width,
        //          Input.mousePosition.y / (float)Screen.height,
        //        0);
    }
}
