#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

internal class XRTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    [TestCase(DeviceRole.Generic, "XRHMD", typeof(XRHMD))]
    [TestCase(DeviceRole.LeftHanded, "XRController", typeof(XRController))]
    [TestCase(DeviceRole.RightHanded, "XRController", typeof(XRController))]
    [TestCase(DeviceRole.HardwareTracker, null, typeof(InputDevice))]
    [TestCase(DeviceRole.TrackingReference, null, typeof(InputDevice))]
    [TestCase(DeviceRole.GameController, null, typeof(InputDevice))]
    [TestCase(DeviceRole.Unknown, null, typeof(InputDevice))]
    public void Devices_XRDeviceRoleDeterminesTypeOfDevice(DeviceRole role, string baseLayoutName, Type expectedType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(role);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.That(createdDevice, Is.TypeOf(expectedType));

        var generatedLayout = InputSystem.TryLoadLayout(string.Format("{0}::{1}::{2}", XRUtilities.kXRInterfaceCurrent,
            deviceDescription.manufacturer, deviceDescription.product));
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.baseLayouts, Is.EquivalentTo(new[] { new InternedString(baseLayoutName) }));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.LeftHanded);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var controller = InputSystem.devices[0];

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.Null);
        Assert.That(XRController.leftHand, Is.EqualTo(controller));

        InputSystem.SetDeviceUsage(controller, CommonUsages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(controller));
        Assert.That(XRController.leftHand, Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutIsNamespacedAsInterfaceManufacturerDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = string.Format("{0}::{1}::{2}", XRUtilities.kXRInterfaceCurrent,
            deviceDescription.manufacturer, deviceDescription.product);
        Assert.AreEqual(createdDevice.layout, expectedLayoutName);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutWithoutManufacturer_IsNamespacedAsInterfaceDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        deviceDescription.manufacturer = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = string.Format("{0}::{1}", XRUtilities.kXRInterfaceCurrent, deviceDescription.product);
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

        Assert.AreEqual(createdDevice.layout, "XRInputV1::Manufacturer::XRThisLayoutShouldhave1ValidName");
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutFeatures_OnlyContainAllowedCharacters()
    {
        runtime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var generatedLayout = InputSystem.TryLoadLayout(createdDevice.layout);
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(1));

        var childControl = generatedLayout.controls[0];
        Assert.That(childControl.name, Is.EqualTo(new InternedString("SimpleFeature1")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRDevicesWithNoOrInvalidCapabilities_DoNotCreateLayouts()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        deviceDescription.capabilities = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Manufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);

        deviceDescription.capabilities = "Not a JSON String";
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    [TestCase("Windows Mixed Reality HMD", "Microsoft", typeof(WMRHMD))]
    [TestCase("Spatial Controller", "Microsoft", typeof(WMRSpatialController))]
    [TestCase("Oculus Rift", "Oculus", typeof(OculusHMD))]
    [TestCase("Oculus Touch Controller", "Oculus", typeof(OculusTouchController))]
    [TestCase("Tracking Reference", "Oculus", typeof(OculusTrackingReference))]
    [TestCase("Oculus HMD", "Samsung", typeof(GearVRHMD))]
    [TestCase("Oculus Tracked Remote", "Samsung", typeof(GearVRTrackedController))]
    [TestCase("Daydream HMD", null, typeof(DaydreamHMD))]
    [TestCase("Daydream Controller", null, typeof(DaydreamController))]
    [TestCase("Vive MV.", "HTC", typeof(ViveHMD))]
    [TestCase("OpenVR Controller(Vive Controller)", "HTC", typeof(ViveWand))]
    [TestCase("HTC V2-XD/XE", "HTC", typeof(ViveLighthouse))]
    [TestCase("OpenVR Controller(Knuckles)", "Valve", typeof(KnucklesController))]
    public void Devices_KnownDevice_UsesSpecializedDeviceType(string name, string manufacturer, Type expectedDeviceType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        deviceDescription.product = name;
        deviceDescription.manufacturer = manufacturer;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];
        Assert.That(createdDevice, Is.TypeOf(expectedDeviceType));
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
        Assert.That(device["Custom"], Is.Null);
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
        Assert.That(device["Custom"], Is.Null);
        Assert.That(((ButtonControl)device["Last"]).isPressed, Is.True);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AllFeatureTypes_AreRepresentedInTheGeneratedLayout()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(7));

        var binaryControl = generatedLayout.controls[0];
        Assert.That(binaryControl.name, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.offset, Is.EqualTo(0));
        Assert.That(binaryControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.usages.Count, Is.EqualTo(1));
        Assert.That(binaryControl.usages[0], Is.EqualTo(new InternedString("ButtonUsage")));

        var discreteControl = generatedLayout.controls[1];
        Assert.That(discreteControl.name, Is.EqualTo(new InternedString("DiscreteState")));
        Assert.That(discreteControl.offset, Is.EqualTo(4));
        Assert.That(discreteControl.layout, Is.EqualTo(new InternedString("Integer")));
        Assert.That(discreteControl.usages.Count, Is.EqualTo(1));
        Assert.That(discreteControl.usages[0], Is.EqualTo(new InternedString("DiscreteStateUsage")));

        var axisControl = generatedLayout.controls[2];
        Assert.That(axisControl.name, Is.EqualTo(new InternedString("Axis")));
        Assert.That(axisControl.offset, Is.EqualTo(8));
        Assert.That(axisControl.layout, Is.EqualTo(new InternedString("Analog")));
        Assert.That(axisControl.usages.Count, Is.EqualTo(1));
        Assert.That(axisControl.usages[0], Is.EqualTo(new InternedString("Axis1DUsage")));

        var vec2Control = generatedLayout.controls[3];
        Assert.That(vec2Control.name, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.offset, Is.EqualTo(12));
        Assert.That(vec2Control.layout, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec2Control.usages[0], Is.EqualTo(new InternedString("Axis2DUsage")));

        var vec3Control = generatedLayout.controls[4];
        Assert.That(vec3Control.name, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.offset, Is.EqualTo(20));
        Assert.That(vec3Control.layout, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec3Control.usages[0], Is.EqualTo(new InternedString("Axis3DUsage")));

        var rotationControl = generatedLayout.controls[5];
        Assert.That(rotationControl.name, Is.EqualTo(new InternedString("Rotation")));
        Assert.That(rotationControl.offset, Is.EqualTo(32));
        Assert.That(rotationControl.layout, Is.EqualTo(new InternedString("Quaternion")));
        Assert.That(rotationControl.usages.Count, Is.EqualTo(1));
        Assert.That(rotationControl.usages[0], Is.EqualTo(new InternedString("RotationUsage")));

        // Custom element is skipped, but occupies 256 bytes

        var lastControl = generatedLayout.controls[6];
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

        var generatedLayout = InputSystem.TryLoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(8));

        var currentControl = generatedLayout.controls[0];
        Assert.That(currentControl.offset, Is.EqualTo(0));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[1];
        Assert.That(currentControl.offset, Is.EqualTo(1));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[2];
        Assert.That(currentControl.offset, Is.EqualTo(2));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[3];
        Assert.That(currentControl.offset, Is.EqualTo(3));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[4];
        Assert.That(currentControl.offset, Is.EqualTo(4));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[5];
        Assert.That(currentControl.offset, Is.EqualTo(5));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout.controls[6];
        Assert.That(currentControl.offset, Is.EqualTo(8));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Analog")));

        currentControl = generatedLayout.controls[7];
        Assert.That(currentControl.offset, Is.EqualTo(12));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));
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

    [Test]
    [Category("Components")]
    public void Components_CanUpdateGameObjectTransformThroughTrackedPoseDriver()
    {
        var testpos = new Vector3(1.0f, 2.0f, 3.0f);
        var testrot = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd1 = go.AddComponent<TrackedPoseDriver>();
        var tpd = tpd1;
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
            var go1 = tpd.gameObject;
            go1.transform.position = Vector3.zero;
            go1.transform.rotation = new Quaternion(0, 0, 0, 0);
            tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            device.quaternion.WriteValueInto(stateEvent, testrot);
            device.vector3.WriteValueInto(stateEvent, testpos);

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

            var go2 = tpd.gameObject;
            go2.transform.position = Vector3.zero;
            go2.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

            // update only
            var go3 = tpd.gameObject;
            go3.transform.position = Vector3.zero;
            go3.transform.rotation = new Quaternion(0, 0, 0, 0);
            tpd.updateType = TrackedPoseDriver.UpdateType.Update;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));

            GameObject go4 = tpd.gameObject;
            go4.transform.position = Vector3.zero;
            go4.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));


            // check the rot/pos case also Update AND Render.
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            var go5 = tpd.gameObject;
            go5.transform.position = Vector3.zero;
            go5.transform.rotation = new Quaternion(0, 0, 0, 0);

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(testpos));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(testrot));

            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            var go6 = tpd.gameObject;
            go6.transform.position = Vector3.zero;
            go6.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(testpos));
            Assert.That(tpd.gameObject.transform.rotation.Equals(testrot));
        }
    }

    private static InputDeviceDescription CreateSimpleDeviceDescriptionByRole(DeviceRole role)
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterfaceCurrent,
            product = "Device",
            manufacturer = "Manufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = role,
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
            interfaceName = XRUtilities.kXRInterfaceCurrent,
            product = "XR_This.Layout/Should have 1 Valid::Name",
            manufacturer = "__Manufacturer::",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.Generic,
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
                interfaceName = XRUtilities.kXRInterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
                    deviceRole = DeviceRole.Generic,
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

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
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
                interfaceName = XRUtilities.kXRInterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
                    deviceRole = DeviceRole.Generic,
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

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
