#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine.Profiling;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

// JsonParser implements Yet Another JSON Parser. So that's sad. The one redeeming quality and sole reason
// for its existence is that it can avoid a bunch of GC heap allocations for strings, arrays, and dictionaries
// by navigating the JSON string directly. Means we can compare individual values to JSON data we receive
// from the native backend without having to actually pipe the JSON data through JsonUtility.

internal class JsonParserTests
{
    [Test]
    [Category("Utilities")]
    public void Utilities_CanCompareJsonStringProperty()
    {
        const string json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : ""test"",
                ""fourth"" : ""ot\""her""
            }
        ";

        var parser = new JsonParser(json);
        Assert.That(parser.NavigateToProperty("third"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo("test"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo("foo"), Is.False);
        parser.Reset();
        Assert.That(parser.NavigateToProperty("fourth"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo("ot\"her"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo("other"), Is.False);
    }

    [Test]
    [Category("Utilities")]
    [Retry(2)] // Warm up JIT.
    public void Utilities_CanCompareJsonStringProperty_WithoutAllocatingGCMemory()
    {
        var json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : ""test"",
                ""fourth"" : ""other""
            }
        ";

        var kProfileRegion = "Utilities_CanCompareJsonStringProperty_WithoutAllocatingGCMemory";
        var kThird = "third";
        var kTest = "test";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfileRegion);
            var parser = new JsonParser(json);
            parser.NavigateToProperty(kThird);
            parser.CurrentPropertyHasValueEqualTo(kTest);
            Profiler.EndSample();
        }, ConstraintExtensions.AllocatingGCMemory(Is.Not));
    }

    [Test]
    [Category("Utilities")]
    [Retry(2)] // Warm up JIT
    public void Utilities_CanCompareJsonStringProperty_WithoutAllocatingGCMemory_EvenIfStringContainsEscapeSequences()
    {
        var json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : ""te\""st"",
                ""fourth"" : ""other""
            }
        ";

        var kProfileRegion = "Utilities_CanCompareJsonStringProperty_WithoutAllocatingGCMemory";
        var kThird = "third";
        var kTest = "te\"st";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfileRegion);
            var parser = new JsonParser(json);
            parser.NavigateToProperty(kThird);
            parser.CurrentPropertyHasValueEqualTo(kTest);
            Profiler.EndSample();
        }, ConstraintExtensions.AllocatingGCMemory(Is.Not));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanCompareJsonNumberProperty()
    {
        const string json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : 123,
                ""fourth"" : 234.567
            }
        ";

        var parser = new JsonParser(json);
        Assert.That(parser.NavigateToProperty("third"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(123), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(234), Is.False);
        parser.Reset();
        Assert.That(parser.NavigateToProperty("fourth"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(234.567), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(345.678), Is.False);
    }

    private enum TestEnum
    {
        AA,
        BB,
        CC,
    }

    // To JsonParser, enum string literals, integer values, and boxed Enum values are all interchangeable.
    [Test]
    [Category("Utilities")]
    public void Utilities_CanCompareJsonEnumProperty()
    {
        const string json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : 1,
                ""fourth"" : ""CC""
            }
        ";

        var parser = new JsonParser(json);
        Assert.That(parser.NavigateToProperty("third"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(TestEnum.BB), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(TestEnum.CC), Is.False);
        parser.Reset();
        Assert.That(parser.NavigateToProperty("fourth"), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(TestEnum.CC), Is.True);
        Assert.That(parser.CurrentPropertyHasValueEqualTo(TestEnum.BB), Is.False);
    }

    [Test]
    [Category("Utilities")]
    [Retry(2)] // Warm up JIT
    [Ignore(".NET Enum APIs allocate garbage")]
    public void TODO_Utilities_CanCompareJsonEnumProperty_WithoutAllocatingGCMemory()
    {
        var json = @"
            {
                ""first"" : true,
                ""second"" : [ 1, 2, 3.00 ],
                ""third"" : 1,
                ""fourth"" : ""BB""
            }
        ";

        var kProfileRegion = "Utilities_CanCompareJsonEnumProperty_WithoutAllocatingGCMemory";
        var kThird = "third";
        var kFourth = "fourth";
        var kTest = (Enum)TestEnum.BB;

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfileRegion);
            var parser = new JsonParser(json);
            parser.NavigateToProperty(kThird);
            parser.CurrentPropertyHasValueEqualTo(kTest);

            parser.Reset();
            parser.NavigateToProperty(kFourth);
            parser.CurrentPropertyHasValueEqualTo(kTest);

            Profiler.EndSample();
        }, ConstraintExtensions.AllocatingGCMemory(Is.Not));
    }
}
#endif // UNITY_EDITOR
