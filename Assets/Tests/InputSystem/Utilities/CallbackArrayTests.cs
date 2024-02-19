using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine.InputSystem.Utilities;

// Unit tests of CallbackArray and DelegateHelpers.
internal class CallbackArrayTests
{
    private int callbackCount = 0;
    private List<int> callbacks = new List<int>();

    private void Callback()
    {
        ++callbackCount;
    }

    private void OtherCallback()
    {
        ++callbackCount;
    }

    [Test]
    [Category("Utilities")]
    public void InvokeCallbacksSafe_ShouldInvokeRegisteredCallbacks()
    {
        CallbackArray<Action> sut = new CallbackArray<Action>();
        sut.AddCallback(Callback);

        DelegateHelpers.InvokeCallbacksSafe(ref sut, "x");
        Assert.That(callbackCount, Is.EqualTo(1));

        DelegateHelpers.InvokeCallbacksSafe(ref sut, "x");
        Assert.That(callbackCount, Is.EqualTo(2));
    }

    // TODO Add test case verifying callbacks with multiple callbacks registered
    // TODO Add test case verifying behavior when adding a callback from within callback
    // TODO Add test case verifying behavior when removing a callback from with callback
    // TODO Add test case verifying behavior when doing multiple add/remove/add etc. from within a callback

    [Test]
    [Category("Utilities")]
    public void Length_ShouldReflectNumberOfCurrentlyRegisteredCallbacks()
    {
        CallbackArray<Action> sut = new CallbackArray<Action>();
        Assert.That(sut.length, Is.EqualTo(0));

        sut.AddCallback(Callback);
        Assert.That(sut.length, Is.EqualTo(1));

        sut.AddCallback(Callback); // Note: Shouldn't count, already registered
        Assert.That(sut.length, Is.EqualTo(1));

        sut.AddCallback(OtherCallback);
        Assert.That(sut.length, Is.EqualTo(2));

        sut.RemoveCallback(Callback);
        Assert.That(sut.length, Is.EqualTo(1));

        sut.RemoveCallback(OtherCallback);
        Assert.That(sut.length, Is.EqualTo(0));
    }
}
