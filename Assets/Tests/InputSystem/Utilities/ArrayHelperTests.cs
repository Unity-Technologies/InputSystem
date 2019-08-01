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

        ArrayHelpers.MoveSlice(array1, 1, 6, 2);
        ArrayHelpers.MoveSlice(array2, 6, 1, 2);
        ArrayHelpers.MoveSlice(array3, 0, 5, 3);

        Assert.That(array1, Is.EquivalentTo(new[] {1, 4, 5, 6, 7, 8, 2, 3}));
        Assert.That(array2, Is.EquivalentTo(new[] {1, 7, 8, 2, 3, 4, 5, 6}));
        Assert.That(array3, Is.EquivalentTo(new[] {4, 5, 6, 7, 8, 1, 2, 3}));
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

        Assert.That(array1, Is.EquivalentTo(new[] {1, 2, 4, 5, 0, 0, 0, 0}));
        Assert.That(array2, Is.EquivalentTo(new[] {1, 2, 3, 4, 5, 6, 7, 0}));
        Assert.That(array3, Is.EquivalentTo(new[] {2, 3, 4, 0, 0, 0, 0, 0}));

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
            Assert.That(array1, Is.EquivalentTo(new[] {1, 2, 4, 5, 5, 0, 0, 0}));
            Assert.That(array2, Is.EquivalentTo(new[] {1, 2, 3, 4, 5, 6, 7, 8}));
            Assert.That(array3, Is.EquivalentTo(new[] {2, 3, 4, 4, 0, 0, 0, 0}));

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
    public void Utilities_CanSetUpRingBuffer()
    {
        var buffer = new ArrayHelpers.RingBuffer<int>(4);

        Assert.That(buffer.count, Is.Zero);

        buffer.Append(123);

        Assert.That(buffer.count, Is.EqualTo(1));
        Assert.That(buffer[0], Is.EqualTo(123));

        buffer.Append(234);
        buffer.Append(345);
        buffer.Append(456);

        Assert.That(buffer.count, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(123));
        Assert.That(buffer[1], Is.EqualTo(234));
        Assert.That(buffer[2], Is.EqualTo(345));
        Assert.That(buffer[3], Is.EqualTo(456));

        buffer.Append(567);

        Assert.That(buffer.count, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(234));
        Assert.That(buffer[1], Is.EqualTo(345));
        Assert.That(buffer[2], Is.EqualTo(456));
        Assert.That(buffer[3], Is.EqualTo(567));

        buffer.Append(678);
        buffer.Append(789);
        buffer.Append(890);

        Assert.That(buffer.count, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(567));
        Assert.That(buffer[1], Is.EqualTo(678));
        Assert.That(buffer[2], Is.EqualTo(789));
        Assert.That(buffer[3], Is.EqualTo(890));
    }
}
