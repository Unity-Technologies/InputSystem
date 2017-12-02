using ISX;
using ISX.LowLevel;
using NUnit.Framework;

public class UnitTests_InputEventQueue
{
    [Test]
    [Category("Events")]
    public void CanQueueAndDequeueEvent()
    {
        var queue = new InputEventQueue(InputEvent.kBaseEventSize, 10);
        //queue.WriteEvent(new InputEvent());
        Assert.Fail();
    }
}
