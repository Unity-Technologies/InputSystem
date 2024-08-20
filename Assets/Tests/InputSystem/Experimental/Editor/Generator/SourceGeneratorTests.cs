using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Generator;

namespace Tests.InputSystem.Experimental.Editor.Generator
{
    [Category("Experimental")]
    public class SourceGeneratorTests
    {
        [Test]
        public void SyntaxClassTest()
        {
            var c = new SourceContext();
            var myType = c.root.DeclareClass("MyType");
            var field1 = myType.DeclareField(typeof(int), "x");
            var field2 = myType.DeclareField<float>( "y", "3.14f");
            var inner = myType.DeclareClass("Inner");
            field2.visibility = Syntax.Visibility.Private;
            Assert.That(c.ToSource(), Is.EqualTo($@"using System;

class MyType
{{
    int x;
    private float y = 3.14f;
    class Inner
    {{
    }}
}}"));
        }

        [Test]
        public void SyntaxStructTest()
        {
            var c = new SourceContext();
            var foo = c.root.DeclareStruct("Foo");
            foo.isPartial = true;
            foo.docSummary = "This is the Foo class";
            foo.StructLayout(LayoutKind.Sequential);
            var x = foo.DeclareField<int>("x");
            x.fieldOffset = 0;
            var y = foo.DeclareField<int>("y");
            y.fieldOffset = 4;
            Assert.That(c.ToSource(), Is.EqualTo(@"using System;

/// <summary>
/// This is the Foo class
/// </summary>
[StructLayout(layoutKind: LayoutKind.Sequential)]
partial struct Foo
{
    [FieldOffset(0)] int x;
    [FieldOffset(4)] int y;
}"));
        }

        [Test]
        public void SyntaxEnumTest()
        {
            var c = new SourceContext();
            var e = c.root.DeclareEnum("MyEnum");
            e.docSummary = "My summary.";
            e.AddItem(new Syntax.Enum.Item("First"));
            e.AddItem(new Syntax.Enum.Item("Second"));
            Assert.That(c.ToSource(), Is.EqualTo(@"/// <summary>
/// My summary.
/// </summary>
enum MyEnum
{
    First,
    Second
}"));
        }
        
        [Test]
        public void SyntaxEnumFlagsTest()
        {
            var c = new SourceContext();
            var e = c.root.DeclareEnumFlags(name: "MyEnum");
            e.docSummary = "My summary.";
            e.AddItem("None", "0");
            e.AddItem("First", "1");
            e.AddItem("Second", "2");
            e.AddItem("Third", "4");
            e.AddItem("Fourth", "8");
            Assert.That(c.ToSource(), Is.EqualTo(@"/// <summary>
/// My summary.
/// </summary>
[Flags]
enum MyEnum
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 4,
    Fourth = 8
}"));
        }

        [Test]
        public void SyntaxInterfaceTest()
        {
            var c = new SourceContext();
            var i = c.root.DeclareInterface("IFoo");
            i.DefineMethod("Bar");
            Assert.That(c.ToSource(), Is.EqualTo(@""));
        }
        
        [Test]
        public void SyntaxMethodTest()
        {
            var c = new SourceContext();
            var foo = c.root.DeclareClass("Foo");
            var bar = foo.DefineMethod("Bar", Syntax.Visibility.Public, Syntax.TypeReference.For<int>())
                .Statement("return 5");
            Assert.That(c.ToSource(), Is.EqualTo(@"class Foo
{
    public int Bar()
    {
        return 5;
    }
}"));
        }
        
        [Test]
        public void SyntaxMethodWithArgumentsTest()
        {
            var c = new SourceContext();
            var foo = c.root.DeclareClass("Foo");
            var bar = foo.DefineMethod("Bar", Syntax.Visibility.Private)
                .Parameter("x", typeof(int))
                .Parameter("y", typeof(float))
                .Statement("var z = (float)x * y")
                .Statement("Debug.Log(z)");
            Assert.That(c.ToSource(), Is.EqualTo(@"class Foo
{
    private void Bar(int x, float y)
    {
        var z = (float)x * y;
        Debug.Log(z);
    }
}"));
        }
    }
}