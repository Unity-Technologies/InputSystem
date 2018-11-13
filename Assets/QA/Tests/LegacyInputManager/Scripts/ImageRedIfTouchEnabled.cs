using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageRedIfTouchEnabled : MonoBehaviour
{
    void Start()
    {
        if (Input.touchSupported)
        {
            GetComponent<Image>().color = Color.red;
        }
    }
}
