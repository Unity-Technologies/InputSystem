using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

public class Usecase : MonoBehaviour
{
    private GameBehavior behaviour;
    
    void OnAwake()
    {
        behaviour = GetComponent<GameBehavior>();
    }
    
    void OnEnable()
    {
        Gamepad.ButtonSouth.Subscribe((bool x) => Debug.Log("Jump"));
        
    }

    private void OnDisable()
    {
        
    }
}
