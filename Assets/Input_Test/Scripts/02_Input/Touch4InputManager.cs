using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Touch4InputManager : MonoBehaviour {

	// Use this for initialization
	void Start () {

        if (!Input.touchSupported)
            Debug.LogError("Touch input is not supported. You may experience technical difficulty.");
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
