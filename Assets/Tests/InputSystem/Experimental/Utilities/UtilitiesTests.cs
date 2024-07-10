using System.Text;
using NUnit.Framework;
using Tests.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.TestTools;

namespace Tests.InputSystem.Experimental
{
    internal class UtilitiesTests : ContextTestFixture
    {
        [Test]
        [Description("Verifies that DebugLog() constructs a subscription writing to Debug.Log")]
        public void DebugObserver()
        {
            var button = Gamepad.ButtonEast.Stub(context);
            using var s1 = Gamepad.ButtonEast.Pressed().DebugLog(context);
            //using var s2 = Gamepad.ButtonEast.Released().DebugLog(context);
            
            button.Press();
            button.Release();
            context.Update();
            
            LogAssert.Expect($"Pressed.OnNext: {new InputEvent().ToString()}");
            // TODO WHY? LogAssert.Expect($"Released.OnNext: {new InputEvent().ToString()}");
        }

        [Test]
        [Description("Verifies that Digraph can be parameterized for GraphViz Dot format generation")]
        public void DigraphSupportsCustomization()
        {
            var g = new Digraph(Gamepad.ButtonSouth)
            {
                name = "G",
                title = "Title",
                fontSize = 9,
                font = "Arial"
            };
            Assert.That(g.Build(), Is.EqualTo(@"digraph G {
   label=""Title""
   rankdir=""LR""
   node [shape=rect]
   graph [fontname=""Arial"" fontsize=9]
   node [fontname=""Arial"" fontsize=9]
   edge [fontname=""Arial"" fontsize=9]
   node0 [label=""Gamepad.ButtonSouth""]
}"));            
        }
        
        [Test]
        [Description("Verifies that Dot() construct a GraphViz Dot file representation of the associated dependency chain.")]
        public void Dot()
        {
            const string commonPrefix = @"digraph {
   rankdir=""LR""
   node [shape=rect]
   graph [fontname=""Source Code Pro"" fontsize=12]
   node [fontname=""Source Code Pro"" fontsize=12]
   edge [fontname=""Source Code Pro"" fontsize=12]";
            
            Assert.That(Gamepad.ButtonSouth.ToDot(), Is.EqualTo(commonPrefix + @"
   node0 [label=""Gamepad.ButtonSouth""]
}"));
            
            Assert.That(Gamepad.ButtonSouth.Pressed().ToDot(), Is.EqualTo(commonPrefix + @"
   node0 [label=""Pressed""]
   node1 [label=""Gamepad.ButtonSouth""]
   node0 -> node1
}"));
        }
    }
}