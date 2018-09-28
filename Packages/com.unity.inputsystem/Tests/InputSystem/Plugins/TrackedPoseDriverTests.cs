using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.Layouts;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [TestFixture]
    public class TrackedPoseDriverTests : InputTestFixture
    {
        public class TestTrackedPoseDriverWrapper : TrackedPoseDriver
        {
            public void FakeUpdate()
            {
                Update();
            }

            public void FakeOnBeforeRender()
            {
                OnBeforeRender();
            }
        }

        static Vector3 testpos = new Vector3(1.0f, 2.0f, 3.0f);
        static Quaternion testrot = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        internal static TestTrackedPoseDriverWrapper CreateGameObjectWithTPD()
        {
            var go = new GameObject();
            var tpd = go.AddComponent<TestTrackedPoseDriverWrapper>();
            return tpd;
        }

        [InputControlLayout]
        public class TestHMD : InputDevice
        {
            public QuaternionControl quaternion { get; set; }
            public Vector3Control vector3 { get; set; }
            protected override void FinishSetup(InputDeviceBuilder builder)
            {
                base.FinishSetup(builder);
                quaternion = builder.GetControl<QuaternionControl>("quaternion");
                vector3 = builder.GetControl<Vector3Control>("vector3");
            }
        }
  
       
        public void Reset(GameObject go)
        {
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        }

        [Test]
        [Category("TrackedPoseDriver")]
        public void TPD_UpdateOptions_EffectProcessedData()
        {
            var tpd = CreateGameObjectWithTPD();
            var device = InputSystem.AddDevice<TestHMD>();
            
            InputEventPtr stateEvent;
            using (StateEvent.From(device, out stateEvent))
            {                                
                var positionAction = new InputAction();
                positionAction.AddBinding("<TestHMD>/vector3");

                var rotationAction = new InputAction();
                rotationAction.AddBinding("<TestHMD>/quaternion");

                tpd.positionAction = positionAction;
                tpd.rotationAction = rotationAction;

                // before render only
                Reset(tpd.gameObject);
                tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

                device.quaternion.WriteValueInto(stateEvent, testrot);
                device.vector3.WriteValueInto(stateEvent, testpos);
                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update();

                tpd.FakeUpdate();
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

                tpd.FakeOnBeforeRender();
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

                // update only
                tpd.updateType = TrackedPoseDriver.UpdateType.Update;
                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                Reset(tpd.gameObject);

                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update();

                tpd.FakeOnBeforeRender();
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

                tpd.FakeUpdate();
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

                // check the rot/pos case also Update AND Render.
                tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
                Reset(tpd.gameObject);

                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update();

                tpd.FakeUpdate();
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
                Reset(tpd.gameObject);
                tpd.FakeUpdate();
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot)); 
            }
        }
    }
}
    