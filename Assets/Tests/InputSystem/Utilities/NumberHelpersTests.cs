using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

internal class NumberHelpersTests
{
    [Test]
    [Category("Utilities")]
    // out of boundary tests
    [TestCase(-1, 0, 1, 0.0f)]
    [TestCase(2, 0, 1, 1.0f)]
    // [0, 1]
    [TestCase(0, 0, 1, 0.0f)]
    [TestCase(1, 0, 1, 1.0f)]
    // [-128, 127]
    [TestCase(-128, sbyte.MinValue, sbyte.MaxValue, 0.0f)]
    [TestCase(0, sbyte.MinValue, sbyte.MaxValue, 0.501960813999176025391f)]
    [TestCase(127, sbyte.MinValue, sbyte.MaxValue, 1.0f)]
    // [0, 255]
    [TestCase(0, byte.MinValue, byte.MaxValue, 0.0f)]
    [TestCase(128, byte.MinValue, byte.MaxValue, 0.501960813999176025391f)]
    [TestCase(255, byte.MinValue, byte.MaxValue, 1.0f)]
    // [-32768, 32767]
    [TestCase(-32768, short.MinValue, short.MaxValue, 0.0f)]
    [TestCase(0, short.MinValue, short.MaxValue, 0.50000762939453125f)]
    [TestCase(32767, short.MinValue, short.MaxValue, 1.0f)]
    // [0, 65535]
    [TestCase(0, ushort.MinValue, ushort.MaxValue, 0.0f)]
    [TestCase(32767, ushort.MinValue, ushort.MaxValue, 0.49999237060546875f)]
    [TestCase(65535, ushort.MinValue, ushort.MaxValue, 1.0f)]
    // [-2147483648, 2147483647]
    [TestCase(-2147483648, int.MinValue, int.MaxValue, 0.0f)]
    [TestCase(0, int.MinValue, int.MaxValue, 0.5f)]
    [TestCase(2147483647, int.MinValue, int.MaxValue, 1.0f)]
    public void Utilities_NumberHelpers_CanConvertIntToNormalizedFloatAndBack(int value, int minValue, int maxValue, float expected)
    {
        var result = NumberHelpers.IntToNormalizedFloat(value, minValue, maxValue);
        Assert.That(result, Is.EqualTo(expected).Within(float.Epsilon));

        var integer = NumberHelpers.NormalizedFloatToInt(result, minValue, maxValue);
        Assert.That(integer, Is.EqualTo(Mathf.Clamp(value, minValue, maxValue)));
    }
    
    [Test]
    [Category("Utilities")]
    // out of boundary tests
    [TestCase(0U, 1U, 2U, 0.0f)]
    [TestCase(3U, 1U, 2U, 1.0f)]
    // [10, 30]
    [TestCase(10U, 10U, 30U, 0.0f)]
    [TestCase(25U, 10U, 30U, 0.75f)]
    [TestCase(30U, 10U, 30U, 1.0f)]
    // [0, 255]
    [TestCase(0U, byte.MinValue, byte.MaxValue, 0.0f)]
    [TestCase(128U, byte.MinValue, byte.MaxValue, 0.501960813999176025391f)]
    [TestCase(255U, byte.MinValue, byte.MaxValue, 1.0f)]
    // [0, 65535]
    [TestCase(0U, ushort.MinValue, ushort.MaxValue, 0.0f)]
    [TestCase(32767U, ushort.MinValue, ushort.MaxValue, 0.49999237060546875f)]
    [TestCase(65535U, ushort.MinValue, ushort.MaxValue, 1.0f)]
    // [0, 4294967295]
    [TestCase(0U, uint.MinValue, uint.MaxValue, 0.0f)]
    [TestCase(2147483647U, uint.MinValue, uint.MaxValue, 0.5f)]
    [TestCase(4294967295U, uint.MinValue, uint.MaxValue, 1.0f)]
    public void Utilities_NumberHelpers_CanConvertUIntToNormalizedFloatAndBack(uint value, uint minValue, uint maxValue, float expected)
    {
        var result = NumberHelpers.UIntToNormalizedFloat(value, minValue, maxValue);
        Assert.That(result, Is.EqualTo(expected).Within(float.Epsilon));
        
        var integer = NumberHelpers.NormalizedFloatToUInt(result, minValue, maxValue);
        Assert.That(integer, Is.EqualTo(Clamp(value, minValue, maxValue)));
    }

    // Mathf.Clamp is not overloaded for uint's, Math.Clamp is only available in .NET core 2.0+ / .NET 5
    private static uint Clamp(uint value, uint min, uint max)
    {
        if (value < min)
            value = min;
        else if (value > max)
            value = max;
        return value;
    }
}