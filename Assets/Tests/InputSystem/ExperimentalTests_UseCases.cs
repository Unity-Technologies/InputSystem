using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Interfaces;
using NUnit.Framework;

namespace Tests.InputSystem
{
    public class ExperimentalTests_UseCases
    {
        private const string kTestCategory = "Experimental";
        private Context m_Context;
        
        [SetUp]
        public void SetUp()
        {
            m_Context = new Context();
        }

        [TearDown]
        public void TearDown()
        {
            m_Context.Dispose();
            m_Context = null;
        }

        class CustomService
        {
            
        }
        
        /*[Test]
        [Category(kTestCategory)]
        public void Add_Service()
        {
            m_Context.AddService(new CustomService());
        }*/
        
        [Test]
        [Category(kTestCategory)]
        public void Enumerating_StandardGamepadDevice()
        {
            for (var i = 0; i < m_Context.GetDeviceCount(); ++i)
            {
                var device = m_Context.GetDevice(i);
                
            }
            
            //var gamepadInterface = device.GetDeviceInterface<StandardGamepad>(); // TODO We also want to be able to enumerate interfaces
            //gamepadInterface.OnChange += () => { }; // TODO Consider if we should use one more level of indirection
        }
       
        /*[Test]
        public void SubscribeToData_StandardGamepadDevice()
        {
            var gamepad = StandardGamepad.GetDevice(0);
        }

        [Test]
        public void Interaction_DetermineIfSpecificGamepadInstanceButtonWasPressedLastFrame()
        {
            var gamepad = StandardGamepad.GetDevice(0);
        }*/
    }
}