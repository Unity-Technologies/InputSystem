using NUnit.Framework;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

public class ReadOnlyArrayTests
{
    [Test]
    public void WithForEach()
    {
        var array = new ReadOnlyArray<float>(new float[] { 1, 2, 3, 4 });
        // Warm up.
        var foo1 = 1.0f;
        foreach (var element in array)
            foo1 = element + 1;
        Assert.That(() =>
            {
                var foo = 1.0f;
                foreach (var element in array)
                    foo = element + 1;
            },
            Is.Not.AllocatingGCMemory());
    }
    [Test]
    public void WithoutForEach()
    {
        var array = new ReadOnlyArray<float>(new float[] { 1, 2, 3, 4 });
        // Warm up.
        var foo1 = 1.0f;
        for (var i = 0; i < array.Count; ++i)
            foo1 = array[i] + 1;
        Assert.That(() =>
            {
                var foo = 1.0f;
                for (var i = 0; i < array.Count; ++i)
                    foo = array[i] + 1;
            },
            Is.Not.AllocatingGCMemory());
    }
}