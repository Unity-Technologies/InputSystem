using NUnit.Framework;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [TestFixture]
    public class TrackedPoseDriverTests : InputTestFixture
    {
        static Vector3 testpos = new Vector3(1.0f, 2.0f, 3.0f);
        static Quaternion testrot = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        internal static TrackedPoseDriver CreateGameObjectWithTPD()
        {
            var go = new GameObject();
            var tpd = go.AddComponent<TrackedPoseDriver>();
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
        public void TPD_UpdateOptions_AffectProcessedData()
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
                InputSystem.Update(InputUpdateType.Dynamic);
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

                Reset(tpd.gameObject);
                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update(InputUpdateType.BeforeRender);
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

                // update only
                Reset(tpd.gameObject);
                tpd.updateType = TrackedPoseDriver.UpdateType.Update;
                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update(InputUpdateType.Dynamic);
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

                Reset(tpd.gameObject);
                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update(InputUpdateType.BeforeRender);
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));


                // check the rot/pos case also Update AND Render.
                tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
                Reset(tpd.gameObject);

                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update(InputUpdateType.Dynamic);
                Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
                Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
                Reset(tpd.gameObject);
                InputSystem.QueueEvent(stateEvent);
                InputSystem.Update(InputUpdateType.BeforeRender);
                Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
                Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));
            }
        }
    }
}
