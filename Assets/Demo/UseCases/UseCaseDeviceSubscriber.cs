using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UseCases;

public class UseCaseDeviceSubscriber : UseCase
{
    private IDisposable subscription;
    
    private void OnEnable() => subscription = Mouse.any.Subscribe(m => Debug.Log(m.delta + " " + m.buttons[0]));

    private void OnDisable() => subscription?.Dispose();
}
