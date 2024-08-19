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
        public void Empty()
        {
            Assert.That(new OldSourceFormatter().ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void StructTest()
        {
            Assert.That(new OldSourceFormatter().BeginStruct("Gamepad").EndStruct().ToString(), 
                Is.EqualTo(@"internal struct Gamepad
{
}"));       
        }

        [StructLayout(layoutKind: LayoutKind.Sequential)]
        struct MyStruct
        {
            
        }
        
        [Test]
        public void StructSequentialTest()
        {
            var g = new OldSourceFormatter();
            g.BeginStruct("Gamepad", OldSourceFormatter.Visiblity.Internal, new StructLayoutAttribute(LayoutKind.Sequential));
            g.EndStruct();
            
            Assert.That(g.ToString(), Is.EqualTo(@"[StructLayout(layoutKind: LayoutKind.Sequential)]
internal struct Gamepad
{
}"));       
        }

        [Test]
        public void StructExplicitTest()
        {
            var g = new OldSourceFormatter();
            g.BeginStruct("Gamepad", OldSourceFormatter.Visiblity.Default, new StructLayoutAttribute(LayoutKind.Explicit));
            g.EndStruct();
            
            Assert.That(g.ToString(), Is.EqualTo(@"[StructLayout(layoutKind: LayoutKind.Explicit)]
struct Gamepad
{
}"));       
        }

        [Test]
        public void DeclareField()
        {
            var g = new OldSourceFormatter();
            g.DeclareField("int", "x", OldSourceFormatter.Visiblity.Public);
            g.DeclareField("int", "y", OldSourceFormatter.Visiblity.Private, 5.ToString());
            g.DeclareField("int", "z", OldSourceFormatter.Visiblity.Internal, fieldOffset: 2);
            g.DeclareField("int", "w", OldSourceFormatter.Visiblity.Default);
            Assert.That(g.ToString(), Is.EqualTo(@"public int x;
private int y = 5;
[FieldOffset(2)] internal int z;
int w;"));
        }

        [Test]
        public void Summary()
        {
            var g = new OldSourceFormatter();
            g.WriteSummary("Lorum Ipsum");
            Assert.That(g.ToString(), Is.EqualTo(@"/// <summary>
/// Lorum Ipsum
/// </summary>"));
        }

        [Test]
        public void Header()
        {
            var g = new OldSourceFormatter();
            g.header = "// My header prefix";
            Assert.That(g.ToString(), Is.EqualTo(g.header));
        }

        [Test]
        public void SyntaxClassTest()
        {
            var c = new SourceContext();
            var myType = c.DeclareClass("MyType");
            var field1 = myType.DeclareField(typeof(int), "x");
            var field2 = myType.DeclareField<float>( "y", "3.14f");
            var inner = myType.DeclareClass("Inner");
            field2.visibility = Syntax.Visibility.Private;
            Assert.That(new SourceGenerator(c).ToString(), Is.EqualTo($@"using System;

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
            var foo = c.DeclareStruct("Foo");
            foo.isPartial = true;
            foo.docSummary = "This is the Foo class";
            foo.annotations.Add(Syntax.Attribute.StructLayout(LayoutKind.Sequential));
            var x = foo.DeclareField<int>("x");
            x.fieldOffset = 0;
            var y = foo.DeclareField<int>("y");
            y.fieldOffset = 4;
            var src = new SourceGenerator(c).ToString();
            Assert.That(src, Is.EqualTo(@"using System;

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
            var e = c.DeclareEnum("MyEnum");
            e.docSummary = "My summary.";
            e.AddItem(new Syntax.Enum.Item("First"));
            e.AddItem(new Syntax.Enum.Item("Second"));
            var src = new SourceGenerator(c).ToString();
            Assert.That(src, Is.EqualTo(@"/// <summary>
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
            var e = c.DeclareEnumFlags(name: "MyEnum");
            e.docSummary = "My summary.";
            e.AddItem("None", "0");
            e.AddItem("First", "1");
            e.AddItem("Second", "2");
            e.AddItem("Third", "4");
            e.AddItem("Fourth", "8");
            Assert.That(new SourceGenerator(c).ToString(), Is.EqualTo(@"/// <summary>
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
            // var i = c.DeclareInterface("IFoo");
            Assert.That(new SourceGenerator(c).ToString(), Is.EqualTo(@""));
        }
        
        [Test]
        public void SyntaxMethodTest()
        {
            var c = new SourceContext();
            var foo = c.DeclareClass("Foo");
            foo.DeclareMethod("Bar");
            Assert.That(new SourceGenerator(c).ToString(), Is.EqualTo(@""));
        }
    }
}