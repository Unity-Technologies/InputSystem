using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageRedIfMultiTouchEnabled : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (Input.multiTouchEnabled)
        {
            GetComponent<Image>().color = Color.red;
        }
	}
}
