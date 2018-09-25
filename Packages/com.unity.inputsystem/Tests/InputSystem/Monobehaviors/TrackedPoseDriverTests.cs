using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SpatialTracking;
using System.Linq;
using UnityEngine.XR.PoseProvider;

namespace UnityEngine.Experimental.Input.XR
{
    [TestFixture]
    public class TrackedPoseDriverTests
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


        internal class TestPoseProvider : BasePoseProvider
        {
            public override bool TryGetPoseFromProvider(out Pose output)
            {
                Pose tmp = new Pose();                
                tmp.position = testpos;
                tmp.rotation = testrot;
                output = tmp;
                return true;
            }
        }

        internal static TestTrackedPoseDriverWrapper CreateGameObjectWithTPD()
        {
            GameObject go = new GameObject();
            TestTrackedPoseDriverWrapper tpd = go.AddComponent<TestTrackedPoseDriverWrapper>();
            return tpd;
        }

        internal static BasePoseProvider CreatePoseProviderOnTPD(TestTrackedPoseDriverWrapper tpd)
        {
            TestPoseProvider tpp = tpd.gameObject.AddComponent<TestPoseProvider>();
            tpd.poseProviderComponent = tpp;
            return tpp;
        }


        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void TPDBindingTest()
        {
            var baseMap = new InputActionMap("Base");
            var action1 = baseMap.AddAction("action1", binding: "<Gamepad>/buttonSouth");
            var action2 = baseMap.AddAction("action2", binding: "<Gamepad>/buttonNorth");

            TestTrackedPoseDriverWrapper tpd = CreateGameObjectWithTPD();
            tpd.positionAction = action1;
            Assert.That(tpd.positionAction.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            tpd.positionAction = action2;
            Assert.That(tpd.positionAction.bindings[0].path, Is.EqualTo("<Gamepad>/buttonNorth"));

            
            tpd.rotationAction = action1;
            Assert.That(tpd.rotationAction.bindings[0].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            tpd.rotationAction = action2;
            Assert.That(tpd.rotationAction.bindings[0].path, Is.EqualTo("<Gamepad>/buttonNorth"));        
        }


        [Test]
        public void TPDPoseProviderTest()
        {
            TestTrackedPoseDriverWrapper tpd = CreateGameObjectWithTPD();
            BasePoseProvider pp = CreatePoseProviderOnTPD(tpd);

            Assert.That(tpd.poseProviderComponent, Is.EqualTo(pp));

            tpd.FakeUpdate();
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

        }

        public void Reset(GameObject go)
        {
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        }

        [Test]
        public void TPDUpdateOptionTest()
        {
            TestTrackedPoseDriverWrapper tpd = CreateGameObjectWithTPD();
            BasePoseProvider pp = CreatePoseProviderOnTPD(tpd);

            Assert.That(tpd.poseProviderComponent, Is.EqualTo(pp));

            // check the update/before render case
            tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            Reset(tpd.gameObject);
            tpd.FakeUpdate();
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

            tpd.FakeOnBeforeRender();
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

            Reset(tpd.gameObject);

            tpd.updateType = TrackedPoseDriver.UpdateType.Update;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.FakeOnBeforeRender();
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

            tpd.FakeUpdate(); 
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

            // check the rot/pos case
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            Reset(tpd.gameObject);
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
    