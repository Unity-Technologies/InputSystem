using NUnit.Framework;
using UnityEngine.Experimental.Input.Utilities;

public class ArrayHelperTests
{
    [Test]
    [Category("Utilities")]
    public void Utilities_CanMoveArraySlice()
    {
        var array1 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array2 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array3 = new[] {1, 2, 3, 4, 5, 6, 7, 8};

        ArrayHelpers.MoveSlice(array1, 1, 6, 2);
        ArrayHelpers.MoveSlice(array2, 6, 1, 2);
        ArrayHelpers.MoveSlice(array3, 0, 5, 3);

        Assert.That(array1, Is.EquivalentTo(new[] {1, 4, 5, 6, 7, 8, 2, 3}));
        Assert.That(array2, Is.EquivalentTo(new[] {1, 7, 8, 2, 3, 4, 5, 6}));
        Assert.That(array3, Is.EquivalentTo(new[] {4, 5, 6, 7, 8, 1, 2, 3}));
    }
}
