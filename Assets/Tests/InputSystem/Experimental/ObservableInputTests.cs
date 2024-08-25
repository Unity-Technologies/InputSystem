using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace Tests.InputSystem.Experimental
{
    // TODO Consider redesigning so that tests auto-scales with added types
    [Category("Experimental")]
    public class ObservableInputTests
    {
        public static IEnumerable<(IDependencyGraphNode, string)> DisplayNameCases()
        {
            yield return (Gamepad.leftStick, "Gamepad.LeftStick");
            yield return (Gamepad.ButtonEast, "Gamepad.ButtonEast");
            yield return (Gamepad.ButtonEast.Pressed(), "Pressed( Gamepad.ButtonEast )");
        }
        
        [Test]
        [Description("Verifies that dependency chain can be described (As in converted to string representation).")]
        [TestCaseSource(nameof(DisplayNameCases))]
        public void Describe((IDependencyGraphNode node, string expectedDisplayName) td)
        {
            Assert.That(td.node.Describe(), Is.EqualTo(td.expectedDisplayName));
        }

        [Test]
        public void Serialization()
        {
            var json = UnityEngine.JsonUtility.ToJson(Gamepad.leftStick);
            Assert.That(json, Is.EqualTo(""));
        }
    }
}