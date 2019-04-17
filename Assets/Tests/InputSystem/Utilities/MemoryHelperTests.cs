using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

internal class MemoryHelperTests
{
    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanSetBitsInBuffer()
    {
        using (var array = new NativeArray<byte>(6, Allocator.Temp))
        {
            var arrayPtr = (byte*)array.GetUnsafePtr();

            // Set bit #0.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 0, 1, true);

            Assert.That(arrayPtr[0], Is.EqualTo(1));
            Assert.That(arrayPtr[1], Is.Zero);

            // Reset bit #0.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 0, 1, false);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.Zero);

            // Set bit #4.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 4, 1, true);

            Assert.That(arrayPtr[0], Is.EqualTo(1 << 4));
            Assert.That(arrayPtr[1], Is.Zero);

            // Reset bit #4.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 4, 1, false);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.Zero);

            // Set bits #1-#5.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 1, 5, true);

            Assert.That(arrayPtr[0], Is.EqualTo(0x3E));
            Assert.That(arrayPtr[1], Is.Zero);

            // Unset bits #1-#5.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 1, 5, false);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.Zero);

            // Set bits #4-#10.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 4, 7, true);

            Assert.That(arrayPtr[0], Is.EqualTo(0xF0));
            Assert.That(arrayPtr[1], Is.EqualTo(0x07));
            Assert.That(arrayPtr[2], Is.Zero);

            // Unset bits #4-#10.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 4, 7, false);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.Zero);
            Assert.That(arrayPtr[2], Is.Zero);

            // Set bits #9-#28.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 9, 20, true);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.EqualTo(0xFE));
            Assert.That(arrayPtr[2], Is.EqualTo(0xFF));
            Assert.That(arrayPtr[3], Is.EqualTo(0x1F));
            Assert.That(arrayPtr[4], Is.Zero);

            // Unset bits #4-#10.
            MemoryHelpers.SetBitsInBuffer(arrayPtr, 0, 9, 20, false);

            Assert.That(arrayPtr[0], Is.Zero);
            Assert.That(arrayPtr[1], Is.Zero);
            Assert.That(arrayPtr[2], Is.Zero);
            Assert.That(arrayPtr[3], Is.Zero);
            Assert.That(arrayPtr[4], Is.Zero);
        }
    }

    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanCompareMemoryBitRegions()
    {
        using (var array1 = new NativeArray<byte>(6, Allocator.Temp))
        using (var array2 = new NativeArray<byte>(6, Allocator.Temp))
        {
            var array1Ptr = (byte*)array1.GetUnsafePtr();
            var array2Ptr = (byte*)array2.GetUnsafePtr();

            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1), Is.False);

            MemoryHelpers.SetBitsInBuffer(array2Ptr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1), Is.True);

            UnsafeUtility.MemClear(array1Ptr, 6);
            UnsafeUtility.MemClear(array2Ptr, 6);

            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24), Is.False);

            MemoryHelpers.SetBitsInBuffer(array2Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24), Is.True);
        }
    }

    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanCompareMemoryBitRegions_AndIgnoreBitsUsingMask()
    {
        using (var array1 = new NativeArray<byte>(8, Allocator.Temp))
        using (var array2 = new NativeArray<byte>(8, Allocator.Temp))
        using (var mask = new NativeArray<byte>(8, Allocator.Temp))
        {
            var array1Ptr = (byte*)array1.GetUnsafePtr();
            var array2Ptr = (byte*)array2.GetUnsafePtr();
            var maskPtr = (byte*)mask.GetUnsafePtr();

            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1, maskPtr), Is.True);

            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1, maskPtr), Is.False);

            UnsafeUtility.MemClear(array1Ptr, 8);
            UnsafeUtility.MemClear(array2Ptr, 8);
            UnsafeUtility.MemClear(maskPtr, 8);

            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);

            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 5, 20, true);
            MemoryHelpers.SetBitsInBuffer(array2Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);

            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 7, 21, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);
        }
    }
}
