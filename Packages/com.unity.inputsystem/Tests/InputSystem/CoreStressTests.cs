#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;

////TODO: write a test that generates a pseudo-random event sequence and runs it through a pseudo-random
////      update pattern and verifies the state and action outcome is as expected

internal class CoreStressTests
{
    [Test]
    [Category("Stress")]
    [Ignore("TODO")]
    public void TODO_Create512GamepadsAndSend1024Events()
    {
        Assert.Fail();
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
