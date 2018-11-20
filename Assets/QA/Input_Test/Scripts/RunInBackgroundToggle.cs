using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;

public class RunInBackgroundToggle : MonoBehaviour
{
    [SerializeField]
    private bool m_ShouldRunInBackground;

    private void Start()
    {
        InputSystem.runInBackground = m_ShouldRunInBackground;
    }
}
