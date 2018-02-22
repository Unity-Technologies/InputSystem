using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageRedIfTouchEnabled : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        if (Input.touchSupported)
        {
            GetComponent<Image>().color = Color.red;
        }
    }
}
