using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageRedIfMultiTouchEnabled : MonoBehaviour
{
    void Start()
    {
        if (Input.multiTouchEnabled)
        {
            GetComponent<Image>().color = Color.red;
        }
    }
}
