using System;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.TestTools;

namespace Tests.InputSystem.Experimental
{
    internal class SubscribeExtensionsTests : ContextTestFixture
    {
        private int m_CompletedCounter;
        private int m_ErrorCounter;
        private int m_NextCounter;

        [SetUp]
        public void SetUpCounters()
        {
            m_CompletedCounter = 0;
            m_ErrorCounter = 0;
            m_NextCounter = 0;
        }

        [Test]
        [Description("Verifies that Subscribe has an overload accepting a single action.")]
        public void BasicActionAdapter()
        {
            var button = Gamepad.ButtonEast.Stub(context);
            using var s = Gamepad.ButtonEast.Pressed().Subscribe(
                action: (InputEvent evt) => { ++m_NextCounter; }, context: context);
            
            button.Press();
            button.Release();
            context.Update();

            Assert.That(m_CompletedCounter, Is.EqualTo(0));
            Assert.That(m_ErrorCounter, Is.EqualTo(0));
            Assert.That(m_NextCounter, Is.EqualTo(1));
        }
        
        [Test]
        [Description("Verifies that Subscribe has an overload accepting actions mapping to observable interface.")]
        public void ActionAdapter()
        {
            var button = Gamepad.ButtonSouth.Stub(context);
            using var s = Gamepad.ButtonSouth.Pressed().Subscribe(
                onCompleted: () => ++m_NextCounter,
                onError: (Exception e) => ++m_NextCounter,
                onNext: (InputEvent evt) => ++m_NextCounter,
                context: context);
            
            button.Press();
            button.Release();
            context.Update();

            Assert.That(m_CompletedCounter, Is.EqualTo(0));
            Assert.That(m_ErrorCounter, Is.EqualTo(0));
            Assert.That(m_NextCounter, Is.EqualTo(1));
        }
    }
}