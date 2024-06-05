using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using Vector2 = UnityEngine.Vector2;

// TODO Do we need a FixedInput type?

namespace Tests.InputSystem
{
    internal static class ContextExtensions
    {
        public static void GivenStreamWithData<T>(this Context context, Usage key, params T[] values) where T : struct
        {
            var s = context.CreateStream(key, values[0]);
            for (var i = 1; i < values.Length; ++i)
                s.OfferByRef(ref values[i]);
        }
    }

    internal class Experimental_Tests
    {
        private Context m_Context;
        private int m_MoveCount;
        private int m_JumpCount;

        [SetUp]
        public void SetUp()
        {
            m_Context = new Context();
        }

        [TearDown]
        public void TearDown()
        {
            m_Context?.Dispose();
        }

        private void Move(Vector2 value)
        {
            ++m_MoveCount;
        }

        private void Jump(InputEvent evt)
        {
            ++m_JumpCount;
        }

        [Test]
        public void Concept()
        {
            var data = new ListObserver<Vector2>();
            using var subscription = Gamepad.leftStick.Subscribe(m_Context, data);

            var s = m_Context.CreateStream(Usages.Gamepad.leftStick, Vector2.zero);
            s.OfferByValue(Vector2.zero);
            s.OfferByValue(Vector2.left);
            s.OfferByValue(Vector2.right);
            m_Context.Update();

            Assert.That(data.Next.Count, Is.EqualTo(4));
            Assert.That(data.Next[0], Is.EqualTo(Vector2.zero)); // initial state
            Assert.That(data.Next[1], Is.EqualTo(Vector2.zero));
            Assert.That(data.Next[2], Is.EqualTo(Vector2.left));
            Assert.That(data.Next[3], Is.EqualTo(Vector2.right));
        }

        // TODO Verify that initial state is properly recorded so we e.g. may start in actuated state on first sync

        [Test]
        public void UseCase_DirectAccess()
        {
            m_Context.CreateStream(Usages.Gamepad.buttonSouth, true);

            var data = new ListObserver<Button>();
            using var subscription = Gamepad.buttonSouth.Subscribe(data);

            m_Context.Update();

            Assert.That(data.Next.Count, Is.EqualTo(2));
        }

        [Test]
        public void UseCase_DirectBindingsShouldBeCompileTimeTypeSafe()
        {
            m_Context.GivenStreamWithData(Usages.Gamepad.buttonSouth, false, true);

            using var move = new BindableInput<Vector2>(callback : Move);
            move.Bind(Gamepad.leftStick);

            // Act
            using BindableInput<InputEvent> jump = new(callback : Jump);
            jump.Bind(Gamepad.buttonSouth.Pressed());
            jump.Bind(Keyboard.space.Pressed());

            jump.OnNext(new InputEvent());
            m_Context.Update();

            Assert.That(m_JumpCount, Is.EqualTo(1));
        }

        /*[Test]
        public void UseCase_ShouldBeAbleToStartFromPreset()
        {
            m_Context = new Context();
            m_Context.GivenStreamWithData(Usages.Gamepad.buttonSouth, false, true);

            // Act
            using BindableInput<InputEvent> jump = new(callback: Jump, source: Presets.Jump());

            m_Context.Update();
            Assert.That(m_JumpCount, Is.EqualTo(1));
        }*/

        public void Example()
        {
            // React to Gamepad button press event
            using var subscription = Gamepad.buttonSouth.Pressed()
                    .Subscribe(DebugObserver<InputEvent>.Create());


            // TODO filter Gamepad.leftStick.filter(x => x > 0.5f); and return new binding source?
            // TODO PoC a temporal filter in the same way, if passed type is not an interface/lambda
            //      there is an oppertunity to avoid the indirect call.
        }

        [Test]
        public void Press()
        {
            var s = m_Context.CreateStream(Usages.Gamepad.buttonEast, false);
            s.OfferByValue(true);
            s.OfferByValue(false);

            // React to Gamepad button press event
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.buttonEast.Pressed(m_Context)
                    .Subscribe(m_Context, observer);

            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
        }

        [Test]
        public void Output_Direct()
        {
            Gamepad.rumbleHaptic.Offer(1.0f);
        }

        [Test]
        public void Output_Indirect()
        {
            var rumble = new BindableOutput<float>(Gamepad.rumbleHaptic);
            rumble.Offer(1.0f);
        }

        [Test]
        public void Multiplex()
        {
            var east = m_Context.CreateDefaultInitializedStream(Gamepad.buttonEast);
            var north = m_Context.CreateDefaultInitializedStream(Gamepad.buttonNorth);

            var output = new ListObserver<bool>();
            var mux = new Multiplexer<bool>(Gamepad.buttonEast, Gamepad.buttonNorth);
            using var sub = mux.Subscribe(m_Context, output);

            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(2));
            Assert.That(output.Next[0], Is.EqualTo(false));
            Assert.That(output.Next[1], Is.EqualTo(false));

            east.OfferByValue(true);

            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(3));
            Assert.That(output.Next[2], Is.EqualTo(true));
        }

        // TODO Local multiplayer, basically a binding filter, but probably good to let sources get assigned to players since physical control makes sense to assign to players. Let devices have a flag.
        // TODO Cover scenarios similar to Value, PassThrough, Button, e.g.
        // TODO Trigger once vs trigger once a frame, vs trigger on received value
        // TODO Phases are related to interactions. Phases associated with data makes no sense, but how do we handle touch?
        // TODO Disambuigitation is better solved on binding level where one can either merge or select?
    }
}
