using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace Tests.InputSystem.Experimental
{
    internal class GamepadTests : ContextTestFixture
    {
        [Test]
        public void Push_Read_ButtonSouth()
        {
            var buttonSouthStub = Gamepad.ButtonSouth.Stub(context, initialValue: true);
            
            var buttonSouthValues = new ListObserver<bool>();
            using var buttonSouthSubscription = Gamepad.ButtonSouth.Subscribe(context, buttonSouthValues);

            context.Update();
            Assert.That(buttonSouthValues.Next.Count, Is.EqualTo(0));
            
            buttonSouthStub.Press(); // Note: Already initialized in pressed state so duplicate reading
            buttonSouthStub.Release();
            context.Update();
            
            Assert.That(buttonSouthValues.Next.Count, Is.EqualTo(2));
            Assert.That(buttonSouthValues.Next[0], Is.EqualTo(true));
            Assert.That(buttonSouthValues.Next[1], Is.EqualTo(false));
        }
        
        [Test]
        public void Push_Read_RightStick()
        {
            var rightStickValues = new ListObserver<Vector2>();
            using var rightStickSubscription = Gamepad.RightStick.Subscribe(context, rightStickValues);

            var rightStickStub = Gamepad.RightStick.Stub(context);
            rightStickStub.Change(Vector2.zero);
            rightStickStub.Change(Vector2.left);
            rightStickStub.Change(Vector2.right);
            context.Update();

            // Note: Initial state not reported via regular stream inspection
            Assert.That(rightStickValues.Next.Count, Is.EqualTo(3));
            Assert.That(rightStickValues.Next[0], Is.EqualTo(Vector2.zero));
            Assert.That(rightStickValues.Next[1], Is.EqualTo(Vector2.left));
            Assert.That(rightStickValues.Next[2], Is.EqualTo(Vector2.right));
        }
        
        // TODO Consider EnumerableInput, separated from ObservableInput
        [Test]
        public void Pull_Read_ButtonEast()
        {
            var data = new ListObserver<Vector2>();
            
            using var reader = Gamepad.ButtonEast.Subscribe(context);
            Assert.That(reader.ToArray().Length, Is.EqualTo(0));

            var button = Gamepad.ButtonEast.Stub(context);
            button.Press();
            button.Release();
            //m_Context.Update(); // TODO Incorrect with this pattern

            var values = reader.ToArray();
            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(values[0], Is.EqualTo(true));
            Assert.That(values[1], Is.EqualTo(false));
        }

        [Test]
        public void Output_Write_Direct()
        {
            Gamepad.RumbleHaptic.Offer(1.0f);
            throw new NotImplementedException();
        }
        
        [Test]
        public void Output_Write_Indirect() // TODO Generalize instead?
        {
            var rumble = new BindableOutput<float>(Gamepad.RumbleHaptic);
            rumble.Offer(1.0f);
            
            throw new NotImplementedException();
        }
    }
}