using NUnit.Framework;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

public class SeparatedStringSetTests
{
    private static readonly char separator = ';';

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSet_ShouldBeEmptyIfDefaultConstructed()
    {
        Assert.That(new SeparatedStringSet(separator).IsEmpty, Is.True);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities__SeparatedStringSetValue_ShouldBeNullIfDefaultConstructed()
    {
        Assert.That(new SeparatedStringSet(separator).Value, Is.Null);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSetRemove_ShouldDoNothingIfEmpty()
    {
        var set = new SeparatedStringSet(separator);
        set.Remove("what");
        Assert.That(set.IsEmpty, Is.True);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSetRemove_ShouldDoNothing()
    {
        var set = new SeparatedStringSet(separator);
        set.Remove("what");
        Assert.That(set.IsEmpty, Is.True);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSetAdd_ShouldAccumulateWithSeparator()
    {
        var set = new SeparatedStringSet(separator);
        Assert.That(set.Value, Is.Null);
        set.Add("first");
        Assert.That(set.Value, Is.EqualTo("first"));
        set.Add("second");
        Assert.That(set.Value, Is.EqualTo("first;second"));
        set.Add("third");
        Assert.That(set.Value, Is.EqualTo("first;second;third"));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSetRemove_ShouldRemoveSubStringIfExists()
    {
        var set = new SeparatedStringSet(separator);
        set.Add("first");
        set.Add("second");
        set.Add("third");
        set.Add("fourth");
        set.Remove("fourth");
        Assert.That(set.Value, Is.EqualTo("first;second;third"));
        set.Remove("second");
        Assert.That(set.Value, Is.EqualTo("first;third"));
        set.Remove("first");
        Assert.That(set.Value, Is.EqualTo("third"));
        set.Remove("third");
        Assert.That(set.Value, Is.Null);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_SeparatedStringSetRemove_ShouldNotRemoveSubStringIfSubStringOfElement()
    {
        var set = new SeparatedStringSet("first;second;d;third", separator);
        set.Add("first");
        set.Add("second");
        set.Add("third");
        Assert.That(set.Value, Is.EqualTo("first;second;d;third"));
        set.Remove("fir");
        Assert.That(set.Value, Is.EqualTo("first;second;d;third"));
        set.Remove("rst");
        Assert.That(set.Value, Is.EqualTo("first;second;d;third"));
        set.Remove("thir");
        Assert.That(set.Value, Is.EqualTo("first;second;d;third"));
        set.Remove("d");
        Assert.That(set.Value, Is.EqualTo("first;second;third"));
    }
}
