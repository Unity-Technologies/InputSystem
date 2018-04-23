using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR;
using UnityEngine.Experimental.Input.Controls;

public class XRTests : InputTestFixture
{
    InputDeviceDescription CreateSimpleDeviceDescriptionByRole(DeviceRole role)
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
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

    InputDeviceDescription CreateMangledNameDeviceDescription()
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
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
    unsafe struct TestXRDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte button;
        [FieldOffset(1)] public uint discreteState;
        [FieldOffset(5)] public float axis;
        [FieldOffset(9)] public Vector2 axis2D;
        [FieldOffset(17)] public Vector3 axis3D;
        [FieldOffset(29)] public Quaternion rotation;
        [FieldOffset(45)] public fixed byte buffer[256];
        [FieldOffset(301)] public byte lastElement;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription()
            {
                interfaceName = XRUtilities.kXRInterface,
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


    [Test]
    [Category("Layouts")]
    [TestCase(DeviceRole.Generic, "XRHMD")]
    [TestCase(DeviceRole.LeftHanded, "XRController")]
    [TestCase(DeviceRole.RightHanded, "XRController")]
    [TestCase(DeviceRole.HardwareTracker, null)]
    [TestCase(DeviceRole.TrackingReference, null)]
    [TestCase(DeviceRole.GameController, null)]
    [TestCase(DeviceRole.Unknown, null)]
    public void Layouts_DeviceRole_ExtendsSpecificDevice(DeviceRole role, string extends)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(role);
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Manufacturer::Device");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.extendsLayout, Is.EqualTo(extends));
    }

    [Test]
    [Category("Devices")]
    [TestCase(DeviceRole.Generic, typeof(XRHMD))]
    [TestCase(DeviceRole.LeftHanded, typeof(XRController))]
    [TestCase(DeviceRole.RightHanded, typeof(XRController))]
    [TestCase(DeviceRole.HardwareTracker, typeof(InputDevice))]
    [TestCase(DeviceRole.TrackingReference, typeof(InputDevice))]
    [TestCase(DeviceRole.GameController, typeof(InputDevice))]
    [TestCase(DeviceRole.Unknown, typeof(InputDevice))]
    public void Devices_DeviceRole_CreatesSpecificDeviceType(DeviceRole role, Type expectedType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(role);
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];
        Assert.That(createdDevice, Is.TypeOf(expectedType));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_GenericDevice_IsAvailableViaHMDCurrent()
    {
        Assert.That(XRHMD.current, Is.Null);

        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var device = InputSystem.devices[0];
        Assert.That(XRHMD.current, Is.EqualTo(device));
    }

    ////FIXME: this test is causing stack overflows
    /*
    [Test]
    [Category("Devices")]
    public void TODO_Devices_LeftAndRightDevices_AreAvailableViaXRControllerLeftAndRigthHandProperties()
    {
        Assert.That(XRController.leftHand, Is.Null);
        Assert.That(XRController.rightHand, Is.Null);

        testRuntime.ReportNewInputDevice(CreateSimpleDeviceDescriptionByRole(DeviceRole.LeftHanded).ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var leftHandedDevice = InputSystem.devices[0];

        Assert.That(XRController.leftHand, Is.EqualTo(leftHandedDevice));
        Assert.That(XRController.rightHand, Is.Null);

        testRuntime.ReportNewInputDevice(CreateSimpleDeviceDescriptionByRole(DeviceRole.RightHanded).ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        var rightHandedDevice = InputSystem.devices[1];

        Assert.That(XRController.leftHand, Is.EqualTo(leftHandedDevice));
        Assert.That(XRController.rightHand, Is.EqualTo(rightHandedDevice));
    }
    */

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.LeftHanded);
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var controller = InputSystem.devices[0];

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.Null);
        Assert.That(XRController.leftHand, Is.EqualTo(controller));

        InputSystem.SetUsage(controller, CommonUsages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(controller));
        Assert.That(XRController.leftHand, Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layout_XRLayoutIsNamespacedAsInterfaceManufacturerDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = String.Format("{0}::{1}::{2}", XRUtilities.kXRInterface,
                deviceDescription.manufacturer, deviceDescription.product);
        Assert.AreEqual(createdDevice.layout, expectedLayoutName);
    }

    [Test]
    [Category("Layouts")]
    public void Layout_XRLayoutWithoutManufacturer_IsNamespacedAsInterfaceDevice()
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        deviceDescription.manufacturer = null;
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = string.Format("{0}::{1}", XRUtilities.kXRInterface, deviceDescription.product);
        Assert.AreEqual(expectedLayoutName, createdDevice.layout);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRGeneratedLayoutNames_OnlyContainAllowedCharacters()
    {
        testRuntime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.AreEqual(createdDevice.layout, "XRInput::Manufacturer::XRThisLayoutShouldhave1ValidName");
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutFeatures_OnlyContainAllowedCharacters()
    {
        testRuntime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

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
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Manufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);

        deviceDescription.capabilities = "Not a JSON String";
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    [TestCase("Windows Mixed Reality HMD", "Microsoft", typeof(WMRHMD))]
    [TestCase("Spatial Controller", "Microsoft", typeof(WMRSpatialController))]
    [TestCase("Spatial Controller", "Microsoft", typeof(WMRSpatialController))]
    [TestCase("Oculus Rift", "Oculus", typeof(OculusHMD))]
    [TestCase("Oculus Touch Controller", "Oculus", typeof(OculusTouchController))]
    [TestCase("Oculus Touch Controller", "Oculus", typeof(OculusTouchController))]
    [TestCase("Tracking Reference", "Oculus", typeof(OculusTrackingReference))]
    [TestCase("Oculus HMD", "Samsung", typeof(GearVRHMD))]
    [TestCase("Oculus Tracked Remote", "Samsung", typeof(GearVRTrackedController))]
    [TestCase("Oculus Tracked Remote", "Samsung", typeof(GearVRTrackedController))]
    [TestCase("Daydream HMD", null, typeof(DaydreamHMD))]
    [TestCase("Daydream Controller", null, typeof(DaydreamController))]
    [TestCase("Daydream Controller", null, typeof(DaydreamController))]
    [TestCase("Vive MV.", "HTC", typeof(ViveHMD))]
    [TestCase("OpenVR Controller(Vive Controller)", "HTC", typeof(ViveWand))]
    [TestCase("OpenVR Controller(Vive Controller)", "HTC", typeof(ViveWand))]
    [TestCase("HTC V2-XD/XE", "HTC", typeof(ViveLighthouse))]
    [TestCase("OpenVR Controller(Knuckles)", "Valve", typeof(KnucklesController))]
    [TestCase("OpenVR Controller(Knuckles)", "Valve", typeof(KnucklesController))]
    public void Devices_KnownDevice_UsesSpecializedDeviceType(string name, string manufacturer, Type expectedDeviceType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByRole(DeviceRole.Generic);
        deviceDescription.product = name;
        deviceDescription.manufacturer = manufacturer;
        testRuntime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];
        Assert.That(createdDevice, Is.TypeOf(expectedDeviceType));
    }

    [Test]
    [Category("State")]
    public void State_AllFeatureTypes_ReadTheSameAsTheirStateValue()
    {
        testRuntime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

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
        testRuntime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRDevice");
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
        Assert.That(discreteControl.offset, Is.EqualTo(1));
        Assert.That(discreteControl.layout, Is.EqualTo(new InternedString("Integer")));
        Assert.That(discreteControl.usages.Count, Is.EqualTo(1));
        Assert.That(discreteControl.usages[0], Is.EqualTo(new InternedString("DiscreteStateUsage")));

        var axisControl = generatedLayout.controls[2];
        Assert.That(axisControl.name, Is.EqualTo(new InternedString("Axis")));
        Assert.That(axisControl.offset, Is.EqualTo(5));
        Assert.That(axisControl.layout, Is.EqualTo(new InternedString("Analog")));
        Assert.That(axisControl.usages.Count, Is.EqualTo(1));
        Assert.That(axisControl.usages[0], Is.EqualTo(new InternedString("Axis1DUsage")));

        var vec2Control = generatedLayout.controls[3];
        Assert.That(vec2Control.name, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.offset, Is.EqualTo(9));
        Assert.That(vec2Control.layout, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec2Control.usages[0], Is.EqualTo(new InternedString("Axis2DUsage")));

        var vec3Control = generatedLayout.controls[4];
        Assert.That(vec3Control.name, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.offset, Is.EqualTo(17));
        Assert.That(vec3Control.layout, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec3Control.usages[0], Is.EqualTo(new InternedString("Axis3DUsage")));

        var rotationControl = generatedLayout.controls[5];
        Assert.That(rotationControl.name, Is.EqualTo(new InternedString("Rotation")));
        Assert.That(rotationControl.offset, Is.EqualTo(29));
        Assert.That(rotationControl.layout, Is.EqualTo(new InternedString("Quaternion")));
        Assert.That(rotationControl.usages.Count, Is.EqualTo(1));
        Assert.That(rotationControl.usages[0], Is.EqualTo(new InternedString("RotationUsage")));

        // Custom element is skipped, but occupies 256 bytes

        var lastControl = generatedLayout.controls[6];
        Assert.That(lastControl.name, Is.EqualTo(new InternedString("Last")));
        Assert.That(lastControl.offset, Is.EqualTo(301));
        Assert.That(lastControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(lastControl.usages.Count, Is.EqualTo(2));
        Assert.That(lastControl.usages[0], Is.EqualTo(new InternedString("LastElementUsage")));
        Assert.That(lastControl.usages[1], Is.EqualTo(new InternedString("SecondUsage")));
    }
}
