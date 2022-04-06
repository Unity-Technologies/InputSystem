using System.Globalization;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Utilities;

internal class ArrayHelperTests
{
    [Test]
    [Category("Utilities")]
    public void Utilities_CanMoveArraySlice()
    {
        var array1 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array2 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array3 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array4 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array5 = new[] {1, 2, 3, 4};
        var array6 = new[] {1, 2, 3, 4};
        var array7 = new[] {1, 2, 3, 4};
        var array8 = new[] {1, 2, 3, 4};
        var array9 = new[] {1, 2, 3, 4};

        ArrayHelpers.MoveSlice(array1, 1, 6, 2);
        ArrayHelpers.MoveSlice(array2, 6, 1, 2);
        ArrayHelpers.MoveSlice(array3, 0, 5, 3);
        ArrayHelpers.MoveSlice(array4, 4, 2, 2);
        ArrayHelpers.MoveSlice(array5, 0, 2, 2);
        ArrayHelpers.MoveSlice(array6, 2, 1, 2);
        ArrayHelpers.MoveSlice(array7, 3, 0, 1);
        ArrayHelpers.MoveSlice(array8, 1, 0, 3);
        ArrayHelpers.MoveSlice(array9, 0, 1, 3);

        Assert.That(array1, Is.EqualTo(new[] {1, 4, 5, 6, 7, 8, 2, 3}));
        Assert.That(array2, Is.EqualTo(new[] {1, 7, 8, 2, 3, 4, 5, 6}));
        Assert.That(array3, Is.EqualTo(new[] {4, 5, 6, 7, 8, 1, 2, 3}));
        Assert.That(array4, Is.EqualTo(new[] {1, 2, 5, 6, 3, 4, 7, 8}));
        Assert.That(array5, Is.EqualTo(new[] {3, 4, 1, 2}));
        Assert.That(array6, Is.EqualTo(new[] {1, 3, 4, 2}));
        Assert.That(array7, Is.EqualTo(new[] {4, 1, 2, 3}));
        Assert.That(array8, Is.EqualTo(new[] {2, 3, 4, 1}));
        Assert.That(array9, Is.EqualTo(new[] {4, 1, 2, 3}));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanEraseInArrayWithCapacity()
    {
        var array1 = new[] {1, 2, 3, 4, 5, 0, 0, 0};
        var array2 = new[] {1, 2, 3, 4, 5, 6, 7, 8};
        var array3 = new[] {1, 2, 3, 4, 0, 0, 0, 0};

        var array1Length = 5;
        var array2Length = 8;
        var array3Length = 4;

        ArrayHelpers.EraseAtWithCapacity(array1, ref array1Length, 2);
        ArrayHelpers.EraseAtWithCapacity(array2, ref array2Length, 7);
        ArrayHelpers.EraseAtWithCapacity(array3, ref array3Length, 0);

        Assert.That(array1, Is.EqualTo(new[] {1, 2, 4, 5, 0, 0, 0, 0}));
        Assert.That(array2, Is.EqualTo(new[] {1, 2, 3, 4, 5, 6, 7, 0}));
        Assert.That(array3, Is.EqualTo(new[] {2, 3, 4, 0, 0, 0, 0, 0}));

        Assert.That(array1Length, Is.EqualTo(4));
        Assert.That(array2Length, Is.EqualTo(7));
        Assert.That(array3Length, Is.EqualTo(3));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanEraseInNativeArrayWithCapacity()
    {
        var array1 = new NativeArray<int>(new[] {1, 2, 3, 4, 5, 0, 0, 0}, Allocator.Temp);
        var array2 = new NativeArray<int>(new[] {1, 2, 3, 4, 5, 6, 7, 8}, Allocator.Temp);
        var array3 = new NativeArray<int>(new[] {1, 2, 3, 4, 0, 0, 0, 0}, Allocator.Temp);

        var array1Length = 5;
        var array2Length = 8;
        var array3Length = 4;

        try
        {
            ArrayHelpers.EraseAtWithCapacity(array1, ref array1Length, 2);
            ArrayHelpers.EraseAtWithCapacity(array2, ref array2Length, 7);
            ArrayHelpers.EraseAtWithCapacity(array3, ref array3Length, 0);

            // For NativeArray, we don't clear memory.
            Assert.That(array1, Is.EqualTo(new[] {1, 2, 4, 5, 5, 0, 0, 0}));
            Assert.That(array2, Is.EqualTo(new[] {1, 2, 3, 4, 5, 6, 7, 8}));
            Assert.That(array3, Is.EqualTo(new[] {2, 3, 4, 4, 0, 0, 0, 0}));

            Assert.That(array1Length, Is.EqualTo(4));
            Assert.That(array2Length, Is.EqualTo(7));
            Assert.That(array3Length, Is.EqualTo(3));
        }
        finally
        {
            array1.Dispose();
            array2.Dispose();
            array3.Dispose();
        }
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_IndexOfReference__IsUsingReferenceEqualsAndConstrainedByStartIndexAndCount()
    {
        var arr = new object[] { new object(), new object(), new object(), new object(), new object() };

        Assert.AreEqual(-1, arr.IndexOfReference(new object(), 0, arr.Length));
        Assert.AreEqual(-1, arr.IndexOfReference(arr[4], 4, 0));

        Assert.AreEqual(0, arr.IndexOfReference(arr[0], 0, arr.Length));
        Assert.AreEqual(4, arr.IndexOfReference(arr[4], 0, arr.Length));

        Assert.AreEqual(-1, arr.IndexOfReference(arr[0], 1, 3));
        Assert.AreEqual(-1, arr.IndexOfReference(arr[4], 1, 3));
        Assert.AreEqual(1, arr.IndexOfReference(arr[1], 1, 3));
        Assert.AreEqual(2, arr.IndexOfReference(arr[2], 1, 3));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_IndexOfPredicate__IsUsingPredicateForEqualityAndConstraintedByStartIndexAndCount()
    {
        var arr = new int[] { 0, 1, 2, 3, 4, 5 };

        Assert.That(arr.IndexOf(x => x >= 3, 0, arr.Length), Is.EqualTo(3));
        Assert.That(arr.IndexOf(x => x >= 3, 0, 3), Is.EqualTo(-1));
        Assert.That(arr.IndexOf(x => x >= 3, 1, 3), Is.EqualTo(3));
        Assert.That(arr.IndexOf(x => x >= 3, 4, 0), Is.EqualTo(-1));
        Assert.That(arr.IndexOf(x => x < 0, 3, 3), Is.EqualTo(-1));
    }
}
