using NUnit.Framework;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

public class ReadOnlyArrayTests
{
    [Test]
    [Category("Utilities")]
    [Retry(2)] // Warm up JIT
    public void Utilities_ReadOnlyArray_ForEachDoesNotAllocateGCMemory()
    {
        var array = new ReadOnlyArray<float>(new float[] { 1, 2, 3, 4 });
        Assert.That(() =>
        {
            var foo = 1.0f;
            foreach (var element in array)
                foo = element + 1;
        }, Is.Not.AllocatingGCMemory());
    }
}
