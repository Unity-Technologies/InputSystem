using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using Usages = UnityEngine.InputSystem.Experimental.Usages;

namespace Tests.InputSystem.Experimental
{
    internal class GamepadTests : ContextTestFixture
    {
        [Test]
        public void GamepadState()
        {
            GamepadState state = default;
            
            Assert.That(state.buttonEast, Is.False);
            state.buttonEast = true;
            Assert.That(state.buttonEast, Is.True);
            state.buttonEast = false;
            Assert.That(state.buttonEast, Is.False);
            
            Assert.That(state.buttonNorth, Is.False);
            state.buttonNorth = true;
            Assert.That(state.buttonNorth, Is.True);
            state.buttonNorth = false;
            Assert.That(state.buttonNorth, Is.False);

            state.leftStick = new Vector2(1, 2);
            Assert.That(state.leftStick, Is.EqualTo(new Vector2(1,2)));
            Assert.That(state.leftStick.x, Is.EqualTo(1));
            Assert.That(state.leftStick.y, Is.EqualTo(2));
            
            // TODO Test remaining
        }

        

        // A producer perspective of a gamepad
        private struct GamepadWriter : IDisposable
        {
            private readonly Endpoint m_Endpoint;
            private Stream<GamepadState> m_Stream;
            public GamepadState Value;
            private long m_Flags;

            private const long LockBit = 1;
            private const long LockMask = ~LockBit;
            
            public GamepadWriter(ushort deviceId, GamepadState initialValue = default)
            {
                m_Endpoint = Endpoint.FromDeviceAndUsage(deviceId, Usages.Devices.Gamepad);
                m_Stream = null;
                Value = initialValue;
                m_Flags = 0;
            }

            public void BeginTransaction()
            {
                for (;;)
                {
                    var previous = Interlocked.CompareExchange(ref m_Flags, m_Flags | LockBit, m_Flags & LockMask);
                    if ((previous & LockBit) != 0)
                        break; // successfully locked
                }
                
                Debug.Assert((m_Flags & LockBit) != 0);
            }

            public void EndTransaction()
            {
                Debug.Assert((m_Flags & LockBit) != 0);
                
                while ((Interlocked.CompareExchange(ref m_Flags, m_Flags | LockBit, m_Flags & LockMask) & LockBit) != 0)
                {
                    // Busy-wait since we are not expecting any concurrent writers
                }
            }

            public void SetStream(Stream<GamepadState> stream)
            {
                // TODO Should reduce ref count on existing stream, should increase ref count on new stream
                m_Stream = stream; // TODO Needs to be CAS
                stream.AddRef();
            }
            
            public void Dispose()
            {
                m_Stream.Release();
                m_Stream = null;
            }

            public bool hasStream => m_Stream != null;
            
            public void Publish()
            {
                Debug.Assert((m_Flags & LockBit) != 0, "Cannot publish without pending transaction");
                
                if (m_Stream == null)
                    return;
                
                m_Stream.OfferByRef(ref Value);
            }
        }
        
        [Test]
        public void GamepadProducer()
        {
            using var stream = context.CreateStream<GamepadState>(Usages.Devices.Gamepad, default);
            
            var writer = new GamepadWriter(100);
            writer.SetStream(stream);
            
            writer.Value.buttonEast = true;
            writer.Value.leftStick = Vector2.left;
            
            writer.Publish();

            Assert.That(stream.AsSpan().Length, Is.EqualTo(1));
            Assert.That(stream.AsSpan()[0].buttonEast, Is.EqualTo(true));
            Assert.That(stream.AsSpan()[0].leftStick, Is.EqualTo(Vector2.left));
        }
        
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