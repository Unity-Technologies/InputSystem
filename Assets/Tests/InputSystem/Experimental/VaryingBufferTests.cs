using NUnit.Framework;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class VaryingBufferTests
    {
        [Ignore("Implementation needs fixing")]
        [Test]
        public void Varying()
        {
            //using var buf = new VaryingBuffer(128, AllocatorManager.Persistent);
            //for (var i=0; i < 100; ++i)
            //    buf.Push(5);
        }
    }
}