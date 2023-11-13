// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if ENABLE_VR || UNITY_GAMECORE
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

using Usages = UnityEngine.InputSystem.CommonUsages;

using InputDeviceRole = UnityEngine.XR.InputDeviceRole;

using DeviceRole = UnityEngine.XR.InputDeviceRole;

internal class XRTests : CoreTestsFixture
{
    [Test]
    [Category("Devices")]
    [TestCase(InputDeviceCharacteristics.HeadMounted, "XRHMD", typeof(XRHMD))]
    [TestCase((InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller), "XRController", typeof(XRController))]
    [TestCase((InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left), "XRController", typeof(XRController))]
    [TestCase(InputDeviceCharacteristics.TrackedDevice, null, typeof(UnityEngine.InputSystem.InputDevice))]
    [TestCase(InputDeviceCharacteristics.None, null, typeof(UnityEngine.InputSystem.InputDevice))]
    public void Devices_XRDeviceCharacteristicsDeterminesTypeOfDevice(InputDeviceCharacteristics characteristics, string baseLayoutName, Type expectedType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(characteristics);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.That(createdDevice, Is.TypeOf(expectedType));

        var generatedLayout = InputSystem.LoadLayout(
            $"{XRUtilities.InterfaceCurrent}::{deviceDescription.manufacturer}::{deviceDescription.product}");

        Assert.That(generatedLayout, Is.Not.Null);
        if (baseLayoutName == null)
            Assert.That(generatedLayout.baseLayouts, Is.Empty);
        else
            Assert.That(generatedLayout.baseLayouts, Is.EquivalentTo(new[] { new InternedString(baseLayoutName) }));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left);

        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var controller = InputSystem.devices[0];

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(Usages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(0).EqualTo(Usages.RightHand));
        Assert.That(XRController.rightHand, Is.Null);
        Assert.That(XRController.leftHand, Is.EqualTo(controller));

        InputSystem.SetDeviceUsage(controller, Usages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(Usages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(Usages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(controller));
        Assert.That(XRController.leftHand, Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutIsNamespacedAsInterfaceManufacturerDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName =
            $"{XRUtilities.InterfaceCurrent}::{deviceDescription.manufacturer}::{deviceDescription.product}";
        Assert.AreEqual(createdDevice.layout, expectedLayoutName);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutWithoutManufacturer_IsNamespacedAsInterfaceDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
        deviceDescription.manufacturer = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = $"{XRUtilities.InterfaceCurrent}::{deviceDescription.product}";
        Assert.AreEqual(expectedLayoutName, createdDevice.layout);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRGeneratedLayoutNames_OnlyContainAllowedCharacters()
    {
        runtime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.AreEqual(createdDevice.layout, "XRInputV1::__Manufacturer::XR_ThisLayoutShouldhave1ValidName");
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutFeatures_OnlyContainAllowedCharacters()
    {
        runtime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var generatedLayout = InputSystem.LoadLayout(createdDevice.layout);
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 1));

        var childControl = generatedLayout["SimpleFeature1"];
        Assert.That(childControl.name, Is.EqualTo(new InternedString("SimpleFeature1")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRDevicesWithNoOrInvalidCapabilities_DoNotCreateLayouts()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
        deviceDescription.capabilities = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInput::Manufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);

        deviceDescription.capabilities = "Not a JSON String";
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        generatedLayout = InputSystem.LoadLayout("XRInput::XRManufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);
    }

    [Test]
    [Category("State")]
    public void State_AllFeatureTypes_ReadTheSameAsTheirStateValue()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var device = InputSystem.devices[0];

        InputSystem.QueueStateEvent(device, new TestXRDeviceState
        {
            button = 0,
            discreteState = 0,
            axis = 0f,
            axis2D = Vector2.zero,
            axis3D = Vector3.zero,
            rotation = Quaternion.identity,
            lastElement = 0,
        });
        InputSystem.Update();

        Assert.That(((ButtonControl)device["Button"]).isPressed, Is.False);
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(0));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(Vector2.zero));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(Vector3.zero));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(Quaternion.identity));
        Assert.That(device.TryGetChildControl("Custom"), Is.Null);
        Assert.That(((ButtonControl)device["Last"]).isPressed, Is.False);

        InputSystem.QueueStateEvent(device, new TestXRDeviceState
        {
            button = 1,
            discreteState = 17,
            axis = 1.24f,
            axis2D = new Vector2(0.1f, 0.2f),
            axis3D = new Vector3(0.3f, 0.4f, 0.5f),
            rotation = new Quaternion(0.6f, 0.7f, 0.8f, 0.9f),
            lastElement = byte.MaxValue,
        });
        InputSystem.Update();

        Assert.That(((ButtonControl)device["Button"]).isPressed, Is.True);
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(17));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(1.24f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(new Vector2(0.1f, 0.2f)));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(new Vector3(0.3f, 0.4f, 0.5f)));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(new Quaternion(0.6f, 0.7f, 0.8f, 0.9f)));
        Assert.That(device.TryGetChildControl("Custom"), Is.Null);
        Assert.That(((ButtonControl)device["Last"]).isPressed, Is.True);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AllFeatureTypes_AreRepresentedInTheGeneratedLayout()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 9));

        var binaryControl = generatedLayout["Button"];
        Assert.That(binaryControl.name, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.offset, Is.EqualTo(0));
        Assert.That(binaryControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.usages.Count, Is.EqualTo(1));
        Assert.That(binaryControl.usages[0], Is.EqualTo(new InternedString("ButtonUsage")));

        var discreteControl = generatedLayout["DiscreteState"];
        Assert.That(discreteControl.name, Is.EqualTo(new InternedString("DiscreteState")));
        Assert.That(discreteControl.offset, Is.EqualTo(4));
        Assert.That(discreteControl.layout, Is.EqualTo(new InternedString("Integer")));
        Assert.That(discreteControl.usages.Count, Is.EqualTo(1));
        Assert.That(discreteControl.usages[0], Is.EqualTo(new InternedString("DiscreteStateUsage")));

        var axisControl = generatedLayout["Axis"];
        Assert.That(axisControl.name, Is.EqualTo(new InternedString("Axis")));
        Assert.That(axisControl.offset, Is.EqualTo(8));
        Assert.That(axisControl.layout, Is.EqualTo(new InternedString("Analog")));
        Assert.That(axisControl.usages.Count, Is.EqualTo(1));
        Assert.That(axisControl.usages[0], Is.EqualTo(new InternedString("Axis1DUsage")));

        var vec2Control = generatedLayout["Vector2"];
        Assert.That(vec2Control.name, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.offset, Is.EqualTo(12));
        Assert.That(vec2Control.layout, Is.EqualTo(new InternedString("Stick")));
        Assert.That(vec2Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec2Control.usages[0], Is.EqualTo(new InternedString("Axis2DUsage")));

        var vec3Control = generatedLayout["Vector3"];
        Assert.That(vec3Control.name, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.offset, Is.EqualTo(20));
        Assert.That(vec3Control.layout, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec3Control.usages[0], Is.EqualTo(new InternedString("Axis3DUsage")));

        var rotationControl = generatedLayout["Rotation"];
        Assert.That(rotationControl.name, Is.EqualTo(new InternedString("Rotation")));
        Assert.That(rotationControl.offset, Is.EqualTo(32));
        Assert.That(rotationControl.layout, Is.EqualTo(new InternedString("Quaternion")));
        Assert.That(rotationControl.usages.Count, Is.EqualTo(1));
        Assert.That(rotationControl.usages[0], Is.EqualTo(new InternedString("RotationUsage")));

        // Custom element is skipped, but occupies 256 bytes

        var lastControl = generatedLayout["Last"];
        Assert.That(lastControl.name, Is.EqualTo(new InternedString("Last")));
        Assert.That(lastControl.offset, Is.EqualTo(304));
        Assert.That(lastControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(lastControl.usages.Count, Is.EqualTo(2));
        Assert.That(lastControl.usages[0], Is.EqualTo(new InternedString("LastElementUsage")));
        Assert.That(lastControl.usages[1], Is.EqualTo(new InternedString("SecondUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ButtonsArePackedByTheByte_WhileLargerStructuresAreFourByteAligned()
    {
        runtime.ReportNewInputDevice(ButtonPackedXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 8));

        var currentControl = generatedLayout["Button1"];
        Assert.That(currentControl.offset, Is.EqualTo(0));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button2"];
        Assert.That(currentControl.offset, Is.EqualTo(1));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button3"];
        Assert.That(currentControl.offset, Is.EqualTo(2));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button4"];
        Assert.That(currentControl.offset, Is.EqualTo(3));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button5"];
        Assert.That(currentControl.offset, Is.EqualTo(4));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button6"];
        Assert.That(currentControl.offset, Is.EqualTo(5));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Axis1"];
        Assert.That(currentControl.offset, Is.EqualTo(8));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Analog")));

        currentControl = generatedLayout["Button7"];
        Assert.That(currentControl.offset, Is.EqualTo(12));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));
    }

    [InputControlLayout(updateBeforeRender = true)]
    class TestHMD : UnityEngine.InputSystem.InputDevice
    {
        [InputControl]
        public QuaternionControl rotation { get; protected set; }
        [InputControl]
        public Vector3Control position { get; protected set; }
        [InputControl]
        public IntegerControl trackingState { get; protected set; }
        protected override void FinishSetup()
        {
            base.FinishSetup();
            rotation = GetChildControl<QuaternionControl>("rotation");
            position = GetChildControl<Vector3Control>("position");
            trackingState = GetChildControl<IntegerControl>("trackingState");
        }
    }

    [InputControlLayout(updateBeforeRender = true)]
    class TestHMDWithoutTrackingState : UnityEngine.InputSystem.InputDevice
    {
        [InputControl]
        public QuaternionControl rotation { get; protected set; }
        [InputControl]
        public Vector3Control position { get; protected set; }
        protected override void FinishSetup()
        {
            base.FinishSetup();
            rotation = GetChildControl<QuaternionControl>("rotation");
            position = GetChildControl<Vector3Control>("position");
        }
    }

    [TestCase(InputTrackingState.None, true)]
    [TestCase(InputTrackingState.None, false)]
    [TestCase(InputTrackingState.Position, false)]
    [TestCase(InputTrackingState.Rotation, false)]
    [TestCase(InputTrackingState.Position | InputTrackingState.Rotation, false)]
    [Category("Components")]
    public void Components_TrackedPoseDriver_CanConstrainWithTrackingState(InputTrackingState trackingState, bool ignoreTrackingState)
    {
        var position = new Vector3(1f, 2f, 3f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);
        var positionValid = ignoreTrackingState || (trackingState & InputTrackingState.Position) != 0;
        var rotationValid = ignoreTrackingState || (trackingState & InputTrackingState.Rotation) != 0;

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.ignoreTrackingState = ignoreTrackingState;
        var transform = tpd.transform;
        var device = InputSystem.AddDevice<TestHMD>();

        using (StateEvent.From(device, out var stateEvent))
        {
            tpd.positionInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/position"));
            tpd.rotationInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/rotation"));
            tpd.trackingStateInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/trackingState"));

            device.rotation.WriteValueIntoEvent(rotation, stateEvent);
            device.position.WriteValueIntoEvent(position, stateEvent);
            device.trackingState.WriteValueIntoEvent((int)trackingState, stateEvent);

            // Constrained by Tracking State only
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(positionValid ? position : Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(rotationValid ? rotation : Quaternion.identity));

            // Constrained by both Tracking State and PositionOnly Tracking Type
            tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(positionValid ? position : Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(Quaternion.identity));

            // Constrained by both Tracking State and RotationOnly Tracking Type
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(rotationValid ? rotation : Quaternion.identity));
        }
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_CanConstrainWithUpdateType()
    {
        var position = new Vector3(1f, 2f, 3f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.ignoreTrackingState = true;
        var transform = tpd.transform;
        var device = InputSystem.AddDevice<TestHMD>();

        using (StateEvent.From(device, out var stateEvent))
        {
            tpd.positionInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/position"));
            tpd.rotationInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/rotation"));

            device.rotation.WriteValueIntoEvent(rotation, stateEvent);
            device.position.WriteValueIntoEvent(position, stateEvent);

            // BeforeRender only
            tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(Quaternion.identity));

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            // Update only
            tpd.updateType = TrackedPoseDriver.UpdateType.Update;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(Quaternion.identity));

            // Update and BeforeRender
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));
        }
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_CanConstrainWithTrackingType()
    {
        var position = new Vector3(1f, 2f, 3f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.updateType = TrackedPoseDriver.UpdateType.Update;
        tpd.ignoreTrackingState = true;
        var transform = tpd.transform;
        var device = InputSystem.AddDevice<TestHMD>();

        using (StateEvent.From(device, out var stateEvent))
        {
            tpd.positionInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/position"));
            tpd.rotationInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/rotation"));

            device.rotation.WriteValueIntoEvent(rotation, stateEvent);
            device.position.WriteValueIntoEvent(position, stateEvent);

            // Position only
            tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(Quaternion.identity));

            // Rotation only
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            // Rotation and Position
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));
        }
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_EnablesAndDisablesDirectActions()
    {
        var positionInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/position"));
        var rotationInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/rotation"));
        var trackingStateInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/trackingState"));

        var go = new GameObject();
        var component = go.AddComponent<TrackedPoseDriver>();
        component.enabled = false;
        component.positionInput = positionInput;
        component.rotationInput = rotationInput;
        component.trackingStateInput = trackingStateInput;

        Assert.That(positionInput.action.enabled, Is.False);
        Assert.That(rotationInput.action.enabled, Is.False);
        Assert.That(trackingStateInput.action.enabled, Is.False);

        component.enabled = true;

        Assert.That(positionInput.action.enabled, Is.True);
        Assert.That(rotationInput.action.enabled, Is.True);
        Assert.That(trackingStateInput.action.enabled, Is.True);

        component.enabled = false;

        Assert.That(positionInput.action.enabled, Is.False);
        Assert.That(rotationInput.action.enabled, Is.False);
        Assert.That(trackingStateInput.action.enabled, Is.False);
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_DoesNotEnableOrDisableReferenceActions()
    {
        var map = new InputActionMap("map");
        map.AddAction("Position", binding: "<TestHMD>/position");
        map.AddAction("Rotation", binding: "<TestHMD>/rotation");
        map.AddAction("Tracking State", binding: "<TestHMD>/trackingState");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var positionReference = ScriptableObject.CreateInstance<InputActionReference>();
        var rotationReference = ScriptableObject.CreateInstance<InputActionReference>();
        var trackingStateReference = ScriptableObject.CreateInstance<InputActionReference>();
        positionReference.Set(asset, "map", "Position");
        rotationReference.Set(asset, "map", "Rotation");
        trackingStateReference.Set(asset, "map", "Tracking State");

        var positionInput = new InputActionProperty(positionReference);
        var rotationInput = new InputActionProperty(rotationReference);
        var trackingStateInput = new InputActionProperty(trackingStateReference);

        var go = new GameObject();
        var component = go.AddComponent<TrackedPoseDriver>();
        component.enabled = false;
        component.positionInput = positionInput;
        component.rotationInput = rotationInput;
        component.trackingStateInput = trackingStateInput;

        Assert.That(positionInput.action.enabled, Is.False);
        Assert.That(rotationInput.action.enabled, Is.False);
        Assert.That(trackingStateInput.action.enabled, Is.False);

        component.enabled = true;

        Assert.That(positionInput.action.enabled, Is.False);
        Assert.That(rotationInput.action.enabled, Is.False);
        Assert.That(trackingStateInput.action.enabled, Is.False);

        component.enabled = false;

        Assert.That(positionInput.action.enabled, Is.False);
        Assert.That(rotationInput.action.enabled, Is.False);
        Assert.That(trackingStateInput.action.enabled, Is.False);
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_RequiresResolvedTrackingStateBindings()
    {
        // Tests the scenario that a single TrackedPoseDriver component has multiple bindings,
        // some to a device with tracking state and some to a device without tracking state.
        // The use case is having the Main Camera track an XRHMD (that has tracking state)
        // or a HandheldARInputDevice (which does not have tracking state), so the tracking
        // state should have an effective value of Position | Rotation.

        var position = new Vector3(1f, 2f, 3f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.updateType = TrackedPoseDriver.UpdateType.Update;
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.ignoreTrackingState = false;
        var transform = tpd.transform;
        var device = InputSystem.AddDevice<TestHMDWithoutTrackingState>();

        using (StateEvent.From(device, out var stateEvent))
        {
            var positionAction = new InputAction(binding: "<TestHMD>/position");
            positionAction.AddBinding("<TestHMDWithoutTrackingState>/position");
            var rotationAction = new InputAction(binding: "<TestHMD>/rotation");
            rotationAction.AddBinding("<TestHMDWithoutTrackingState>/rotation");
            var trackingStateAction = new InputAction(binding: "<TestHMD>/trackingState");

            tpd.positionInput = new InputActionProperty(positionAction);
            tpd.rotationInput = new InputActionProperty(rotationAction);
            tpd.trackingStateInput = new InputActionProperty(trackingStateAction);

            device.rotation.WriteValueIntoEvent(rotation, stateEvent);
            device.position.WriteValueIntoEvent(position, stateEvent);

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);

            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));
        }
    }

    [Test]
    [Category("Components")]
    public void Components_TrackedPoseDriver_RetainsPoseWhenTrackedDeviceRemoved()
    {
        // Tests the scenario that XR controller devices (which have tracking state) are removed
        // (e.g. due to being set down on a table) that the Transform pose will be retained
        // when the tracking state is not ignored.

        var position = new Vector3(1f, 2f, 3f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.updateType = TrackedPoseDriver.UpdateType.Update;
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.ignoreTrackingState = false;
        var transform = tpd.transform;
        var device = InputSystem.AddDevice<TestHMD>();

        using (StateEvent.From(device, out var stateEvent))
        {
            tpd.positionInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/position"));
            tpd.rotationInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/rotation"));
            tpd.trackingStateInput = new InputActionProperty(new InputAction(binding: "<TestHMD>/trackingState"));

            device.rotation.WriteValueIntoEvent(rotation, stateEvent);
            device.position.WriteValueIntoEvent(position, stateEvent);
            device.trackingState.WriteValueIntoEvent((int)(InputTrackingState.Position | InputTrackingState.Rotation), stateEvent);

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);

            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            InputSystem.RemoveDevice(device);
            InputSystem.Update(InputUpdateType.Dynamic);

            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));

            // Ensure the pose is retained even after OnEnable makes the behavior poll the input again
            tpd.enabled = false;
            tpd.enabled = true;
            InputSystem.Update(InputUpdateType.Dynamic);

            Assert.That(transform.position, Is.EqualTo(position));
            Assert.That(transform.rotation, Is.EqualTo(rotation));
        }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_PoseControlsCanBeCreatedBySubcontrols()
    {
        runtime.ReportNewInputDevice(PoseDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);

        // A Pose control parent was created based off subcontrols
        var pose = generatedLayout["PoseControl"];
        Assert.That(pose.layout, Is.EqualTo(new InternedString("Pose")));
    }

    private const int kNumBaseHMDControls = 10;

    InputDeviceRole RoleFromCharacteristics(InputDeviceCharacteristics characteristics)
    {
        if ((characteristics & InputDeviceCharacteristics.Left) != 0)
            return InputDeviceRole.LeftHanded;
        if ((characteristics & InputDeviceCharacteristics.Right) != 0)
            return InputDeviceRole.RightHanded;
        if ((characteristics & InputDeviceCharacteristics.TrackingReference) != 0)
            return InputDeviceRole.TrackingReference;
        if ((characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.HeldInHand) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.EyeTracking) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.Camera) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.Controller) != 0)
            return InputDeviceRole.GameController;
        if ((characteristics & InputDeviceCharacteristics.TrackedDevice) != 0)
            return InputDeviceRole.HardwareTracker;

        return InputDeviceRole.LegacyController;
    }

    private static InputDeviceDescription CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics deviceCharacteristics)
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.InterfaceCurrent,
            product = "Device",
            manufacturer = "Manufacturer",
            capabilities = new XRDeviceDescriptor
            {
                characteristics = deviceCharacteristics,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "Filler",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        };
    }

    private static InputDeviceDescription CreateMangledNameDeviceDescription()
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.InterfaceCurrent,
            product = "XR_This.Layout/Should have 1 Valid::Name",
            manufacturer = "__Manufacturer::",
            capabilities = new XRDeviceDescriptor
            {
                characteristics = InputDeviceCharacteristics.HeadMounted,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature[|.:+-?<1",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ButtonPackedXRDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte button1;
        [FieldOffset(1)] public byte button2;
        [FieldOffset(2)] public byte button3;
        [FieldOffset(3)] public byte button4;
        [FieldOffset(4)] public byte button5;
        [FieldOffset(5)] public byte button6;
        [FieldOffset(8)] public float axis1;
        [FieldOffset(12)] public byte button7;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription
            {
                interfaceName = XRUtilities.InterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
                    characteristics = InputDeviceCharacteristics.HeadMounted,
                    inputFeatures = new List<XRFeatureDescriptor>()
                    {
                        new XRFeatureDescriptor()
                        {
                            name = "Button1",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button2",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button3",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button4",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button5",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button6",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Axis1",
                            featureType = FeatureType.Axis1D
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button7",
                            featureType = FeatureType.Binary
                        },
                    }
                }.ToJson()
            };
        }

        public FourCC format => new FourCC('X', 'R', 'S', '0');
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct TestXRDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte button;
        [FieldOffset(4)] public uint discreteState;
        [FieldOffset(8)] public float axis;
        [FieldOffset(12)] public Vector2 axis2D;
        [FieldOffset(20)] public Vector3 axis3D;
        [FieldOffset(32)] public Quaternion rotation;
        [FieldOffset(48)] public fixed byte buffer[256];
        [FieldOffset(304)] public byte lastElement;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription()
            {
                interfaceName = XRUtilities.InterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
                    characteristics = InputDeviceCharacteristics.HeadMounted,
                    inputFeatures = new List<XRFeatureDescriptor>()
                    {
                        new XRFeatureDescriptor()
                        {
                            name = "Button",
                            featureType = FeatureType.Binary,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "ButtonUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "DiscreteState",
                            featureType = FeatureType.DiscreteStates,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "DiscreteStateUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Axis",
                            featureType = FeatureType.Axis1D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis1DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Vector2",
                            featureType = FeatureType.Axis2D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis2DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Vector3",
                            featureType = FeatureType.Axis3D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis3DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Rotation",
                            featureType = FeatureType.Rotation,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "RotationUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Custom",
                            featureType = FeatureType.Custom,
                            customSize = 256,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "CustomTypeUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Last",
                            featureType = FeatureType.Binary,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "LastElementUsage"
                                },
                                new UsageHint()
                                {
                                    content = "SecondUsage"
                                }
                            }
                        }
                    }
                }.ToJson()
            };
        }

        public FourCC format
        {
            get { return new FourCC('X', 'R', 'S', '0'); }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct PoseDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte isTracked;
        [FieldOffset(4)] public uint trackingState;
        [FieldOffset(8)] public Vector3 position;
        [FieldOffset(20)] public Quaternion rotation;
        [FieldOffset(36)] public Vector3 velocity;
        [FieldOffset(48)] public Vector3 angularVelocity;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription()
            {
                interfaceName = XRUtilities.InterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
                    inputFeatures = new List<XRFeatureDescriptor>()
                    {
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/isTracked",
                            featureType = FeatureType.Binary,
                            usageHints = new List<UsageHint>()
                            {}
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/trackingState",
                            featureType = FeatureType.DiscreteStates,
                            usageHints = new List<UsageHint>()
                            {}
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/position",
                            featureType = FeatureType.Axis3D,
                            usageHints = new List<UsageHint>()
                            {}
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/rotation",
                            featureType = FeatureType.Rotation,
                            usageHints = new List<UsageHint>()
                            {}
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/velocity",
                            featureType = FeatureType.Axis3D,
                            usageHints = new List<UsageHint>()
                            {}
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "PoseControl/angularVelocity",
                            featureType = FeatureType.Axis3D,
                            usageHints = new List<UsageHint>()
                            {}
                        }
                    }
                }.ToJson()
            };
        }

        public FourCC format
        {
            get { return new FourCC('X', 'R', 'S', '0'); }
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_XRAxisControls_AreClampedToOneMagnitude()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var device = InputSystem.devices[0];

        InputSystem.QueueStateEvent(device, new TestXRDeviceState
        {
            button = 0,
            discreteState = 0,
            axis = -2f,
            axis2D = -Vector2.one,
            axis3D = Vector3.zero,
            rotation = Quaternion.identity,
            lastElement = 0,
        });
        InputSystem.Update();

        Assert.That((device["Axis"] as AxisControl).EvaluateMagnitude(), Is.EqualTo(1f).Within(0.0001f));
        Assert.That((device["Vector2/x"] as AxisControl).EvaluateMagnitude(), Is.EqualTo(1f).Within(0.0001f));
        Assert.That((device["Vector2/y"] as AxisControl).EvaluateMagnitude(), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    [Category("Controls")]
    public void Controls_OptimizedControls_PoseControl_IsOptimized()
    {
        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseOptimizedControls, true);

        runtime.ReportNewInputDevice(PoseDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var device = InputSystem.devices[0];

        Assert.That((device["posecontrol"] as PoseControl).optimizedControlDataType, Is.EqualTo(InputStateBlock.FormatPose));
    }

    // ISXB-405
    [Test]
    [Category("Devices")]
    public void Devices_AddingUnusualDevice_ShouldntCrashTheSystem()
    {
        var deviceDescr =
            "{\"interface\":\"XRInputV1\",\"type\":\"\",\"product\":\"OpenXR Right Hand\",\"manufacturer\":\"\",\"serial\":\"\",\"version\":\"\",\"capabilities\":\"{\\\"deviceName\\\":\\\"OpenXR Right Hand\\\",\\\"manufacturer\\\":\\\"\\\",\\\"serialNumber\\\":\\\"\\\",\\\"characteristics\\\":620,\\\"deviceId\\\":4294967297,\\\"inputFeatures\\\":[{\\\"name\\\":\\\"Is Tracked\\\",\\\"usageHints\\\":[{\\\"content\\\":\\\"IsTracked\\\",\\\"id\\\":1429429695}],\\\"featureType\\\":1,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Tracking State\\\",\\\"usageHints\\\":[{\\\"content\\\":\\\"TrackingState\\\",\\\"id\\\":1636970542}],\\\"featureType\\\":2,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Hand Palm\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Hand Wrist\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Thumb Metacarpal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Thumb Proximal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Thumb Distal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Thumb Tip\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Index Metacarpal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Index Proximal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Index Intermediate\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Index Distal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Index Tip\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Middle Metacarpal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Middle Proximal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Middle Intermediate\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Middle Distal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Middle Tip\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Ring Metacarpal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Ring Proximal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Ring Intermediate\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Ring Distal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Ring Tip\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Little Metacarpal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Little Proximal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Little Intermediate\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Little Distal\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Little Tip\\\",\\\"usageHints\\\":[],\\\"featureType\\\":8,\\\"customSize\\\":4294967295},{\\\"name\\\":\\\"Hand Data\\\",\\\"usageHints\\\":[{\\\"content\\\":\\\"HandData\\\",\\\"id\\\":2609730070}],\\\"featureType\\\":7,\\\"customSize\\\":4294967295}],\\\"CanQueryForDeviceStateAtTime\\\":false}\"}";

        runtime.ReportNewInputDevice(deviceDescr);

        InputSystem.Update();

        var device = InputSystem.devices[0];

        Assert.That(device, Is.Not.Null);
    }

    [Test]
    [Category("Commands")]
    public void Commands_GetHapticCapabilitiesCommand_UsesCorrectPayloadSize()
    {
        unsafe
        {
            // Check that the payload of the command matches the low-level struct defined in IUnityXRInput.h (UnityXRHapticCapabilities)
            // and used in XRInputSubsystem by checking the size. The sizes are required to match for the event to be
            // sent to the device.
            Assert.That(sizeof(UnityEngine.InputSystem.XR.Haptics.HapticCapabilities), Is.EqualTo(sizeof(UnityEngine.XR.HapticCapabilities)));
            Assert.That(sizeof(UnityEngine.InputSystem.XR.Haptics.GetHapticCapabilitiesCommand) - InputDeviceCommand.BaseCommandSize, Is.EqualTo(sizeof(UnityEngine.XR.HapticCapabilities)));
        }
    }
}
#endif
