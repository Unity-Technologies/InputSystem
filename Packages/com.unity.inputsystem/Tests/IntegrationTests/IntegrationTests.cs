using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

internal class IntegrationTests
{
    [Test]
    public void CanSendAndReceiveEvents()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();

            Assert.That(keyboard.aKey.isPressed, Is.True);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
    }
}
