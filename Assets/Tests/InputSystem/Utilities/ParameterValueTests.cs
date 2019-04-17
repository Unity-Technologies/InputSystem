using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Experimental.Input.Utilities;

// The "parameter" utilities address the problem of storing structured data of unknown format in serialized data.
// For example, processors (InputProcessor) need to have their configuration data stored in layouts as well as in
// within action data. This amounts to the need for polymorphism in the serialized data.
//
// As a simple solution, the "parameter" form simply stores name/value pairs and puts them inside strings. This simple
// mechanism is used in a variety of places to make both configuration and serialization painless.
//
// Given how widely the mechanism is used, we have a separate suite of tests for the various facilities around it here.

internal class ParameterValueTests
{
    [Test]
    [Category("Utilities")]
    public void Utilities_CanParseNameAndParameterList()
    {
        Assert.That(NameAndParameters.Parse("name()").name, Is.EqualTo("name"));
        Assert.That(NameAndParameters.Parse("name()").parameters, Is.Empty);
        Assert.That(NameAndParameters.Parse("name").name, Is.EqualTo("name"));
        Assert.That(NameAndParameters.Parse("name").parameters, Is.Empty);
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").name, Is.EqualTo("Name"));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters, Has.Count.EqualTo(3));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[0].name, Is.EqualTo("foo"));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[1].name, Is.EqualTo("Bar"));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[2].name, Is.EqualTo("blub"));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[0].type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[1].type, Is.EqualTo(TypeCode.Int32));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[2].type, Is.EqualTo(TypeCode.Double));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[0].value.ToBoolean(), Is.EqualTo(true));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[1].value.ToInt32(), Is.EqualTo(123));
        Assert.That(NameAndParameters.Parse("Name(foo,Bar=123,blub=234.56)").parameters[2].value.ToDouble(), Is.EqualTo(234.56).Within(0.0001));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_ParsingNameAndParameterList_RequiresStringToNotBeEmpty()
    {
        Assert.That(() => NameAndParameters.Parse("").name,
            Throws.Exception.With.Message.Contains("Expecting name"));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanParseMultipleNameAndParameterLists()
    {
        Assert.That(NameAndParameters.ParseMultiple("a,b").Count(), Is.EqualTo(2));
        Assert.That(NameAndParameters.ParseMultiple("a,b").ToArray()[0].name, Is.EqualTo("a"));
        Assert.That(NameAndParameters.ParseMultiple("a,b").ToArray()[0].parameters, Is.Empty);
        Assert.That(NameAndParameters.ParseMultiple("a,b").ToArray()[1].name, Is.EqualTo("b"));
        Assert.That(NameAndParameters.ParseMultiple("a,b").ToArray()[1].parameters, Is.Empty);

        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").Count(), Is.EqualTo(3));
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[0].name, Is.EqualTo("a"));
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[0].parameters, Is.Empty);
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[1].name, Is.EqualTo("b"));
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[1].parameters, Has.Count.EqualTo(2));
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[2].name, Is.EqualTo("c"));
        Assert.That(NameAndParameters.ParseMultiple("a,b(r=1,t),c").ToArray()[2].parameters, Is.Empty);

        Assert.That(NameAndParameters.ParseMultiple("a(b,c=123)").Count(), Is.EqualTo(1));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanConvertStringToPrimitiveValue()
    {
        // Int.
        Assert.That(PrimitiveValue.FromString("123").type, Is.EqualTo(TypeCode.Int32));
        Assert.That(PrimitiveValue.FromString("123").ToInt32(), Is.EqualTo(123));
        Assert.That(PrimitiveValue.FromString("-123456").type, Is.EqualTo(TypeCode.Int32));
        Assert.That(PrimitiveValue.FromString("-123456").ToInt32(), Is.EqualTo(-123456));
        Assert.That(PrimitiveValue.FromString("0x1234ABC").type, Is.EqualTo(TypeCode.Int32));
        Assert.That(PrimitiveValue.FromString("0x1234ABC").ToInt32(), Is.EqualTo(0x1234ABC));

        // Double.
        Assert.That(PrimitiveValue.FromString("0.0").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("0.0").ToDouble(), Is.EqualTo(0).Within(0.00001));
        Assert.That(PrimitiveValue.FromString("1.23").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("1.23").ToDouble(), Is.EqualTo(1.23).Within(0.00001));
        Assert.That(PrimitiveValue.FromString("-1.23456").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("-1.23456").ToDouble(), Is.EqualTo(-1.23456).Within(0.00001));
        Assert.That(PrimitiveValue.FromString("1e10").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("1e10").ToDouble(), Is.EqualTo(1e10).Within(0.00001));
        Assert.That(PrimitiveValue.FromString("-2E-10").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("-2E-10").ToDouble(), Is.EqualTo(-2e-10f).Within(0.00001));
        Assert.That(PrimitiveValue.FromString("Infinity").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("Infinity").ToDouble(), Is.EqualTo(double.PositiveInfinity));
        Assert.That(PrimitiveValue.FromString("-Infinity").type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromString("-Infinity").ToDouble(), Is.EqualTo(double.NegativeInfinity));

        // Bool.
        Assert.That(PrimitiveValue.FromString("true").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("true").ToBoolean(), Is.True);
        Assert.That(PrimitiveValue.FromString("false").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("false").ToBoolean(), Is.False);
        Assert.That(PrimitiveValue.FromString("True").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("True").ToBoolean(), Is.True);
        Assert.That(PrimitiveValue.FromString("False").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("False").ToBoolean(), Is.False);
        Assert.That(PrimitiveValue.FromString("TRUE").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("TRUE").ToBoolean(), Is.True);
        Assert.That(PrimitiveValue.FromString("FALSE").type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromString("FALSE").ToBoolean(), Is.False);
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanConvertPrimitiveValueToString()
    {
        // Bool.
        Assert.That(new PrimitiveValue(true).ToString(), Is.EqualTo("true"));
        Assert.That(new PrimitiveValue(false).ToString(), Is.EqualTo("false"));

        // Char.
        Assert.That(new PrimitiveValue('c').ToString(), Is.EqualTo("'c'"));

        // Int.
        Assert.That(new PrimitiveValue((byte)123).ToString(), Is.EqualTo("123"));
        Assert.That(new PrimitiveValue((sbyte)-123).ToString(), Is.EqualTo("-123"));
        Assert.That(new PrimitiveValue((short)-123).ToString(), Is.EqualTo("-123"));
        Assert.That(new PrimitiveValue((ushort)123).ToString(), Is.EqualTo("123"));
        Assert.That(new PrimitiveValue(-123).ToString(), Is.EqualTo("-123"));
        Assert.That(new PrimitiveValue((uint)123).ToString(), Is.EqualTo("123"));
        Assert.That(new PrimitiveValue((long)-123).ToString(), Is.EqualTo("-123"));
        Assert.That(new PrimitiveValue((ulong)123).ToString(), Is.EqualTo("123"));

        // Float.
        Assert.That(new PrimitiveValue(1.234f).ToString(), Is.EqualTo("1.234"));
        Assert.That(new PrimitiveValue(1.234).ToString(), Is.EqualTo("1.234"));
        Assert.That(new PrimitiveValue(double.PositiveInfinity).ToString(), Is.EqualTo("Infinity"));
        Assert.That(new PrimitiveValue(double.NegativeInfinity).ToString(), Is.EqualTo("-Infinity"));
    }

    // It is important that we do NOT respect the current culture's floating-point number format but
    // instead always use culture-invariant notation. Otherwise we'll run into trouble as the ',' separator
    // used in many cultures as a decimal separator will clash with the use of comma as a separator
    // between parameters.
    [Test]
    [Category("Utilities")]
    public void Utilities_ConvertingPrimitiveValueToString_DoesNotTakeCultureIntoAccount()
    {
        var oldCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.That(new PrimitiveValue(0.1234f).ToString(), Is.EqualTo("0.1234"));
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanConvertObjectToPrimitiveValue()
    {
        Assert.That(PrimitiveValue.FromObject(true).type, Is.EqualTo(TypeCode.Boolean));
        Assert.That(PrimitiveValue.FromObject(true).ToBoolean(), Is.True);

        Assert.That(PrimitiveValue.FromObject('c').type, Is.EqualTo(TypeCode.Char));
        Assert.That(PrimitiveValue.FromObject('c').ToByte(), Is.EqualTo('c'));

        Assert.That(PrimitiveValue.FromObject((byte)123).type, Is.EqualTo(TypeCode.Byte));
        Assert.That(PrimitiveValue.FromObject(123).ToByte(), Is.EqualTo(123));

        Assert.That(PrimitiveValue.FromObject((sbyte)-123).type, Is.EqualTo(TypeCode.SByte));
        Assert.That(PrimitiveValue.FromObject(-123).ToSByte(), Is.EqualTo(-123));

        Assert.That(PrimitiveValue.FromObject((short)-1234).type, Is.EqualTo(TypeCode.Int16));
        Assert.That(PrimitiveValue.FromObject(-1234).ToInt16(), Is.EqualTo(-1234));

        Assert.That(PrimitiveValue.FromObject((ushort)1234).type, Is.EqualTo(TypeCode.UInt16));
        Assert.That(PrimitiveValue.FromObject(1234).ToUInt16(), Is.EqualTo(1234));

        Assert.That(PrimitiveValue.FromObject(-1234).type, Is.EqualTo(TypeCode.Int32));
        Assert.That(PrimitiveValue.FromObject(-1234).ToInt32(), Is.EqualTo(-1234));

        Assert.That(PrimitiveValue.FromObject((uint)1234).type, Is.EqualTo(TypeCode.UInt32));
        Assert.That(PrimitiveValue.FromObject(1234).ToUInt32(), Is.EqualTo(1234));

        Assert.That(PrimitiveValue.FromObject((long)-1234).type, Is.EqualTo(TypeCode.Int64));
        Assert.That(PrimitiveValue.FromObject(-1234).ToInt64(), Is.EqualTo(-1234));

        Assert.That(PrimitiveValue.FromObject((ulong)1234).type, Is.EqualTo(TypeCode.UInt64));
        Assert.That(PrimitiveValue.FromObject(1234).ToUInt64(), Is.EqualTo(1234));

        Assert.That(PrimitiveValue.FromObject(1.2345f).type, Is.EqualTo(TypeCode.Single));
        Assert.That(PrimitiveValue.FromObject(1.2345f).ToSingle(), Is.EqualTo(1.2345).Within(0.00001));

        Assert.That(PrimitiveValue.FromObject(1.2345).type, Is.EqualTo(TypeCode.Double));
        Assert.That(PrimitiveValue.FromObject(1.2345).ToSingle(), Is.EqualTo(1.2345).Within(0.00001));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanConvertPrimitiveValueBetweenTypes()
    {
        // bool <-> int.
        Assert.That(new PrimitiveValue(true).ConvertTo(TypeCode.Int32).ToInt32(), Is.EqualTo(1));
        Assert.That(new PrimitiveValue(false).ConvertTo(TypeCode.Int32).ToInt32(), Is.EqualTo(0));
        Assert.That(new PrimitiveValue(123).ConvertTo(TypeCode.Boolean).ToBoolean(), Is.True);
        Assert.That(new PrimitiveValue(0).ConvertTo(TypeCode.Boolean).ToBoolean(), Is.False);

        // short <-> int.
        Assert.That(new PrimitiveValue((short)123).ConvertTo(TypeCode.Int32).ToInt32(), Is.EqualTo(123));
        Assert.That(new PrimitiveValue(123).ConvertTo(TypeCode.Int16).ToInt16(), Is.EqualTo(123));

        // int <-> float.
        Assert.That(new PrimitiveValue(123).ConvertTo(TypeCode.Single).ToSingle(), Is.EqualTo(123).Within(0.00001));
        Assert.That(new PrimitiveValue(123f).ConvertTo(TypeCode.Int32).ToInt32(), Is.EqualTo(123));

        // float <-> double.
        Assert.That(new PrimitiveValue(123.0).ConvertTo(TypeCode.Single).ToSingle(), Is.EqualTo(123).Within(0.00001));
        Assert.That(new PrimitiveValue(123f).ConvertTo(TypeCode.Double).ToSingle(), Is.EqualTo(123).Within(0.00001));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanConvertEnumValueToPrimitiveValue()
    {
        Assert.That(PrimitiveValue.FromObject(TestEnum.One).type, Is.EqualTo(TypeCode.Int16));
        Assert.That(PrimitiveValue.FromObject(TestEnum.Two).ToInt32(), Is.EqualTo(2));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanSetEnumFieldOnObjectFromPrimitiveValue()
    {
        var obj = new TestObject();
        NamedValue.ApplyAllToObject(obj, new[] { NamedValue.From("enumField", 1)});
        Assert.That(obj.enumField, Is.EqualTo(TestEnum.One));
    }

    private enum TestEnum : short
    {
        Zero,
        One,
        Two,
    }

    private class TestObject
    {
#pragma warning disable 649
        public TestEnum enumField;
#pragma warning restore 649
    }
}
