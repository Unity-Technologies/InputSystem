using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MustUseLegacyInputManager : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        Debug.Log("This Scene must be run with the Legacy InputManager set as the input path in PlayerSettings.");
    }
}
