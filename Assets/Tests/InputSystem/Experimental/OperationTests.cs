using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class OperationTests
    {
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
        }
        
        [Test]
        public void Press()
        {
            var button = Gamepad.ButtonEast.Stub(m_Context);
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonEast.Pressed().Subscribe(m_Context, observer);
            
            // Press should trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            
            // Press should trigger event also when released afterwards
            button.Release();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));

            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
        }

        [Test]
        public void PressUnsafe()
        {
            var button = Gamepad.ButtonEast.Stub(m_Context);
            var observer = new UnsafeListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonEast.Pressed().Subscribe(m_Context, observer.ToDelegate());
            
            // Press should trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.next.Length, Is.EqualTo(1));
            
            // Press should trigger event also when released afterwards
            button.Release();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.next.Length, Is.EqualTo(2));

            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.next.Length, Is.EqualTo(2));
        }
        
        [Test]
        public void Release()
        {
            var button = Gamepad.ButtonNorth.Stub(m_Context);
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonNorth.Released().Subscribe(m_Context, observer);
            
            // Press should not trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            // Release (should trigger event)
            button.Release();
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));

            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
        }

        [Test]
        public void Held()
        {
            var button = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonNorth.Held(TimeSpan.FromMilliseconds(1)).Subscribe(m_Context, observer);

            // Press should not trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            // Release (should not trigger event)
            button.Release();
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));

            // TODO Press and hold should trigger event
            
            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
        }
        
        [Test]
        public void Chord()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<bool>();
            using var subscription = Combine.Chord(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
            
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(true));
            
            button0.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(false));
        }
        
        [Test]
        public void Shortcut_Test()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<bool>();
            using var subscription = Combine.Shortcut(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
                
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(true));
            
            button0.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(false));
            
            button1.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            
            button0.Press(); // Should not trigger if button0 (modifier) is pressed after button1
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
        }

        [Test]
        public void Shortcut_ShouldSuppressPressEvent_IfOverlappingAnotherDependencyGraphAndHavingPriority()
        {
            // TODO Solutions include marking certain observableinput as modifier or blindly relying on priority
            //      unless 0 in which case its unaffected
            
            // TODO Simple scenario with e.g. L1+X and X configured, also add Stick into mix for suppression
            throw new NotImplementedException();
        }

        [Test]
        public void Shortcut_ShouldSuppressComposite_IfButtonIsPartOfComposite()
        {
            throw new NotImplementedException();
        }
        
        [Test]
        public void Composite_Test()
        {
            var west = Gamepad.ButtonWest.Stub(m_Context);   
            var east = Gamepad.ButtonEast.Stub(m_Context);
            
            var observer = new ListObserver<float>();
            using var subscription = Combine.Composite(negative: Gamepad.ButtonWest, positive: Gamepad.ButtonEast).Subscribe(m_Context, observer);
            
            west.Press();
            m_Context.Update();
            east.Press();
            m_Context.Update();
            west.Release();
            m_Context.Update();
            east.Release();
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(4));
            Assert.That(observer.Next[0], Is.EqualTo(-1.0f));
            Assert.That(observer.Next[1], Is.EqualTo(0.0f));            
            Assert.That(observer.Next[2], Is.EqualTo(1.0f));
            Assert.That(observer.Next[3], Is.EqualTo(0.0f));            
        }
        
        [Test]
        public void Filter()
        {
            var stick = Gamepad.leftStick.Stub(m_Context);
            
            var observer = new ListObserver<Vector2>();
            using var opaqueSubscription = Gamepad.leftStick.Filter((v) => v.x >= 0.5f).Subscribe(m_Context, observer);
            //using var subscription = Gamepad.LeftStick.Filter<Vector2>(v => v.x >= 0.5f).Subscribe(m_Context, observer);
            
            stick.Change(new Vector2(0.4f, 0.0f));
            stick.Change(new Vector2(0.5f, 0.0f));
            stick.Change(new Vector2(0.6f, 0.1f));
            stick.Change(new Vector2(0.3f, 0.2f));
            
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[0], Is.EqualTo(new Vector2(0.5f, 0.0f)));
            Assert.That(observer.Next[1], Is.EqualTo(new Vector2(0.6f, 0.1f)));
        }

        [Test]
        public void Convert()
        {
            var stick = Gamepad.leftStick.Stub(m_Context);
            
            var observer = new ListObserver<float>();
            using var opaqueSubscription = Gamepad.leftStick.Convert((Vector2 value) => value.x).Subscribe(m_Context, observer);
            //using var subscription = Gamepad.LeftStick.Filter<Vector2>(v => v.x >= 0.5f).Subscribe(m_Context, observer);
            
            stick.Change(new Vector2(0.4f, 0.0f));
            stick.Change(new Vector2(0.5f, 0.0f));
            stick.Change(new Vector2(0.6f, 0.1f));
            stick.Change(new Vector2(0.3f, 0.2f));
            
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(4));
            Assert.That(observer.Next[0], Is.EqualTo(0.4f));
            Assert.That(observer.Next[1], Is.EqualTo(0.5f));
            Assert.That(observer.Next[2], Is.EqualTo(0.6f));
            Assert.That(observer.Next[3], Is.EqualTo(0.3f));
        }
        
        // TODO Make karting demo using this
        // InputAction<Vector2> steer;
        // steer.AddBinding(Composite.FromDirections(left: Keyboard.A, right: Keyboard.D));
        // steer.AddBinding(Gamepad.leftStick.x.Deadzone());
        // steer.AddBinding(AttitudeSensor.sensors[0].CompensateForOrientation().TiltAroundZ());
        // steerSubscription = steer.DistinctUntilChanged().Last().Subscribe((v) => input.Move = v);
        //
        // InputAction<bool> accelerate;
        // accelerate.AddBinding(Keyboard.Space);
        // accelerate.AddBinding(Gamepad.buttonSouth);
        // accelerateSubscription = accelerate.Last().Subscribe(x => Debug.Log("Accelerating"));
        //
        // InputAction<bool> brake;
        // brake.AddBinding(Keyboard.leftCtrl);
        // brake.AddBinding(Gamepad.buttonWest);
        // brakeSubscription = brake.Last().Subscribe(x => Debug.Log("Braking"));
        //
        // InputAction toggleMenu;
        // toggleMenu.AddBinding(Keyboard.Escape);
        // toggleMenu.AddBinding(Gamepad.start);
        // toggleMenuSubscription = toggleMenu.Last().Subscribe(() => ToggleMenu());
        //
        // TODO InputAction should likely also be a struct proxy there is little need to keep it around since its
        //      just a template for instantiating subscriptions.
        
        [Test]
        public void LowPassFilter()
        {
            var stick = Gamepad.LeftTrigger.Stub(m_Context);
            
            var observer = new ListObserver<float>();
            using var subscription = Gamepad.LeftTrigger.LowPassFilter().Subscribe(m_Context, observer);
            
            stick.Change(0.0f);
            stick.Change(0.1f);
            stick.Change(0.2f);
            stick.Change(0.9f);
            stick.Change(0.5f);
            
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(5));
            Assert.That(observer.Next[0], Is.EqualTo(0.0f));
        }
        
        [Test]
        public void Merge()
        {
            var east = Gamepad.ButtonEast.Stub(m_Context);
            var north = Gamepad.ButtonNorth.Stub(m_Context);
            
            var output = new ListObserver<bool>();
            using var mux = Combine.Merge(Gamepad.ButtonEast, Gamepad.ButtonNorth).Subscribe(m_Context, output);

            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(0));

            east.Press();
            m_Context.Update();
            Assert.That(output.Next.Count, Is.EqualTo(1));
            Assert.That(output.Next[0], Is.EqualTo(true));
            
            north.Press();
            m_Context.Update();
            Assert.That(output.Next.Count, Is.EqualTo(2));
            Assert.That(output.Next[1], Is.EqualTo(true));
        }
        
        [Test]
        public void CombineLatest()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<ValueTuple<bool, bool>>();
            using var subscription = Combine.Latest(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
            
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(new ValueTuple<bool, bool>(true, false)));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(new ValueTuple<bool, bool>(true, true)));
            
            button0.Release();
            button1.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(4));
            Assert.That(observer.Next[2], Is.EqualTo(new ValueTuple<bool, bool>(false, true)));
            Assert.That(observer.Next[3], Is.EqualTo(new ValueTuple<bool, bool>(false, false)));
        }
        
        [Test]
        public void Compare()
        {
            var observer = new ListObserver<bool>();
            using var subscription = Gamepad.LeftTrigger.GreaterThan(0.5f).Subscribe(m_Context, observer);
            
            // TODO Implement
        }
    }
}