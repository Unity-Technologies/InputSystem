using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

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

            // Set bit #2 in array1.
            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1, maskPtr), Is.True);

            // Set bit #2 in mask.
            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 2, 1, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 2, 1, maskPtr), Is.False);

            UnsafeUtility.MemClear(array1Ptr, 8);
            UnsafeUtility.MemClear(array2Ptr, 8);
            UnsafeUtility.MemClear(maskPtr, 8);

            // Set 24 bits in array1 starting at bit #5.
            MemoryHelpers.SetBitsInBuffer(array1Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);

            // Set 20 bits in mask starting at bit #5.
            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 5, 20, true);
            // Set 24 bits in array2 starting at bit #5.
            MemoryHelpers.SetBitsInBuffer(array2Ptr, 0, 5, 24, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);

            // Set 21 bits in mask starting at bit #7.
            MemoryHelpers.SetBitsInBuffer(maskPtr, 0, 7, 21, true);

            Assert.That(MemoryHelpers.MemCmpBitRegion(array1Ptr, array2Ptr, 5, 24, maskPtr), Is.True);
        }
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanComputeOverlapBetweenBitRegions()
    {
        Assert.That(new MemoryHelpers.BitRegion(1, 0, 8).Overlap(new MemoryHelpers.BitRegion(1, 0, 8)),
            Is.EqualTo(new MemoryHelpers.BitRegion(1, 0, 8)));
        Assert.That(new MemoryHelpers.BitRegion(1, 0, 8).Overlap(new MemoryHelpers.BitRegion(2, 0, 8)),
            Is.EqualTo(default(MemoryHelpers.BitRegion)));
        Assert.That(new MemoryHelpers.BitRegion(2, 0, 8).Overlap(new MemoryHelpers.BitRegion(1, 0, 8)),
            Is.EqualTo(default(MemoryHelpers.BitRegion)));

        Assert.That(new MemoryHelpers.BitRegion(12, 3).Overlap(new MemoryHelpers.BitRegion(13, 2)),
            Is.EqualTo(new MemoryHelpers.BitRegion(13, 2)));
        Assert.That(new MemoryHelpers.BitRegion(12, 2).Overlap(new MemoryHelpers.BitRegion(13, 2)),
            Is.EqualTo(new MemoryHelpers.BitRegion(13, 1)));
    }

    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanInitializeMemory()
    {
        using (var mem = new NativeArray<byte>(6, Allocator.Temp))
        {
            var memPtr = (byte*)mem.GetUnsafePtr();

            MemoryHelpers.MemSet(memPtr, 6, 123);

            Assert.That(memPtr[0], Is.EqualTo(123));
            Assert.That(memPtr[1], Is.EqualTo(123));
            Assert.That(memPtr[2], Is.EqualTo(123));
            Assert.That(memPtr[3], Is.EqualTo(123));
            Assert.That(memPtr[4], Is.EqualTo(123));
            Assert.That(memPtr[5], Is.EqualTo(123));
        }
    }

    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanCopyMemoryWithMask()
    {
        using (var from = new NativeArray<byte>(6, Allocator.Temp))
        using (var to = new NativeArray<byte>(6, Allocator.Temp))
        using (var mask = new NativeArray<byte>(6, Allocator.Temp))
        {
            var fromPtr = (byte*)from.GetUnsafePtr();
            var toPtr = (byte*)to.GetUnsafePtr();
            var maskPtr = (byte*)mask.GetUnsafePtr();

            toPtr[0] = 0xff;
            toPtr[1] = 0xf0;
            toPtr[2] = 0x0f;
            toPtr[3] = 0x01;
            toPtr[4] = 0x40;
            toPtr[5] = 0x00;

            fromPtr[0] = 0x00;
            fromPtr[1] = 0x01;
            fromPtr[2] = 0x12;
            fromPtr[3] = 0x10;
            fromPtr[4] = 0x88;
            fromPtr[5] = 0xC1;

            maskPtr[0] = 0xF0;
            maskPtr[1] = 0xF0;
            maskPtr[2] = 0x0F;
            maskPtr[3] = 0x00;
            maskPtr[4] = 0xC0;
            maskPtr[5] = 0x11;

            MemoryHelpers.MemCpyMasked(toPtr, fromPtr, 6, maskPtr);

            Assert.That(toPtr[0], Is.EqualTo(0x0F));
            Assert.That(toPtr[1], Is.EqualTo(0x00));
            Assert.That(toPtr[2], Is.EqualTo(0x02));
            Assert.That(toPtr[3], Is.EqualTo(0x01));
            Assert.That(toPtr[4], Is.EqualTo(0x80));
            Assert.That(toPtr[5], Is.EqualTo(0x01));
        }
    }

    [Test]
    [Category("Utilities")]
    public unsafe void Utilities_CanCopyBitRegion()
    {
        using (var from = new NativeArray<byte>(6, Allocator.Temp))
        using (var to = new NativeArray<byte>(6, Allocator.Temp))
        {
            var fromPtr = (byte*)from.GetUnsafePtr();
            var toPtr = (byte*)to.GetUnsafePtr();

            fromPtr[0] = 0x00;
            fromPtr[1] = 0x01;
            fromPtr[2] = 0x12;
            fromPtr[3] = 0x10;
            fromPtr[4] = 0x88;
            fromPtr[5] = 0xC1;

            MemoryHelpers.MemCpyBitRegion(toPtr, fromPtr, 18, 4);

            Assert.That(toPtr[0], Is.Zero);
            Assert.That(toPtr[1], Is.Zero);
            Assert.That(toPtr[2], Is.EqualTo(0x10));
            Assert.That(toPtr[3], Is.Zero);
            Assert.That(toPtr[4], Is.Zero);
            Assert.That(toPtr[5], Is.Zero);

            UnsafeUtility.MemClear(toPtr, 6);

            MemoryHelpers.MemCpyBitRegion(toPtr, fromPtr, 28, 8);

            Assert.That(toPtr[0], Is.Zero);
            Assert.That(toPtr[1], Is.Zero);
            Assert.That(toPtr[2], Is.Zero);
            Assert.That(toPtr[3], Is.EqualTo(0x10));
            Assert.That(toPtr[4], Is.EqualTo(0x08));
            Assert.That(toPtr[5], Is.Zero);

            UnsafeUtility.MemClear(toPtr, 6);

            MemoryHelpers.MemCpyBitRegion(toPtr, fromPtr, 0, 6 * 8);

            Assert.That(toPtr[0], Is.EqualTo(0x00));
            Assert.That(toPtr[1], Is.EqualTo(0x01));
            Assert.That(toPtr[2], Is.EqualTo(0x12));
            Assert.That(toPtr[3], Is.EqualTo(0x10));
            Assert.That(toPtr[4], Is.EqualTo(0x88));
            Assert.That(toPtr[5], Is.EqualTo(0xC1));
        }
    }
}
