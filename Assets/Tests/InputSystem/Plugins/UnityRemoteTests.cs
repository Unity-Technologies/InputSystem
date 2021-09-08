using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

internal class UnityRemoteTests : CoreTestsFixture
{
    public override void TearDown()
    {
        UnityRemoteSupport.ResetGlobalState();
        base.TearDown();
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveTouchInputFromUnityRemote()
    {
        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        Assert.That(Touchscreen.current, Is.Not.Null);
        Assert.That(Touchscreen.current.remote, Is.True);

        SendUnityRemoteMessage(new UnityRemoteSupport.TouchInputMessage
        {
            positionX = 123,
            positionY = 234,
            phase = (int)UnityEngine.TouchPhase.Began,
            id = 0 // Old input system allows zero IDs so test with it here.
        });
        InputSystem.Update();

        Assert.That(Touchscreen.current.primaryTouch.isInProgress, Is.True);
        Assert.That(Touchscreen.current.primaryTouch.touchId.ReadValue(), Is.EqualTo(1)); // Should +1 to every ID.
        Assert.That(Touchscreen.current.primaryTouch.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));

        SendUnityRemoteMessage(new UnityRemoteSupport.GoodbyeMessage());

        Assert.That(Touchscreen.current, Is.Null);
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveJoystickInputFromUnityRemote()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveGyroscopeInputFromUnityRemote()
    {
        Assert.Fail();
    }

    private unsafe void SendUnityRemoteMessage<TMessage>(TMessage message)
        where TMessage : unmanaged, UnityRemoteSupport.IUnityRemoteMessage
    {
        var ptr = UnsafeUtility.AddressOf(ref message);
        *(byte*)ptr = message.staticType;
        *(int*)((byte*)ptr + 1) = UnsafeUtility.SizeOf<TMessage>();

        runtime.onUnityRemoteMessage(new IntPtr(ptr));
    }
}
