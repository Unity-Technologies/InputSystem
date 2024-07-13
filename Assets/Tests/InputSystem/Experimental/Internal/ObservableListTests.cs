using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    internal class ObservableListTests : ContextTestFixture
    {
        [Test]
        public void Test()
        {
            ObserverList2<int> x;
            var observer = new ObserverList<Vector2>();
            
            Assert.That(() =>
            {
                using var s = Gamepad.LeftStick.Subscribe(context, observer);
            }, Is.Not.AllocatingGCMemory());            
        }
    }
}