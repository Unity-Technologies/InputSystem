using System;
using NUnit.Framework;

using Unity.InputSystem;
using Unity.InputSystem.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.InputSystem.Runtime.Native;

public class DevelopmentTests : CoreTestsFixture
{
    [Test]
    [Category("RuntimeNext")]
    public void RuntimeNext_Test()
    {
        var _cb1 = InputRuntimeNextPAL.Create();
        var _cb2 = new DatabaseCallbacksContainer();
        
        InputRuntimeInit(1);

        var device = InputInstantiateDevice(Guid.Parse("8d37e884-458e-4b1d-805f-95425987e9d1"));
        var keyboard = device.As<InputKeyboard>();
        
        keyboard.spaceButton.Ingress(true);
        
        Assert.That(keyboard.spaceButton.value, Is.False);

        InputSwapFramebuffer();

        Assert.That(keyboard.spaceButton.value, Is.True);
        
        
        InputRuntimeDeinit();
    }

    [Test]
    [Category("RuntimeNext")]
    public void RuntimeNext_PokingAtDeviceComposition()
    {
        var device = InputSystem.AddDevice<RuntimeNextDevice>();
        device.mouse.leftButton.QueueValueChange(1.0f);
        InputSystem.Update();
        Assert.That(device.mouse.leftButton.isPressed, Is.True);
    }

}