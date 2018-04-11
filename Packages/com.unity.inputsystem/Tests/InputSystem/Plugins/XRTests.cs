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
    [Test]
    [Category("Layouts")]
    public void Layouts_GenericDeviceCreatesLayoutThatExtendsXRHMD()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRGenericDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.Generic,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRGenericDevice");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
        Assert.That(generatedLayout.extendsLayout, Is.EqualTo("XRHMD"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_GenericDeviceRoleCreatesHMDDevice()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRGenericDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.Generic,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var hmd = InputSystem.devices[0];
        Assert.That(hmd, Is.TypeOf<XRHMD>());
        Assert.That(hmd.description.interfaceName, Is.EqualTo(XRUtilities.kXRInterface));
        Assert.That(hmd.usages.Count, Is.EqualTo(0));
        Assert.That(XRHMD.current, Is.EqualTo(hmd));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_LeftAndRightHandedDevicesCreateLayoutThatExtendsXRController()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRLeftHandedDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.LeftHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRRightHandedDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.RightHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRLeftHandedDevice");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
        Assert.That(generatedLayout.extendsLayout, Is.EqualTo("XRController"));

        generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRRightHandedDevice");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
        Assert.That(generatedLayout.extendsLayout, Is.EqualTo("XRController"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_LeftAndRightRoleCreatesXRControllerDevice()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRLeftHandedDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.LeftHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRRightHandedDevice",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.RightHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));

        var leftHandedDevice = InputSystem.devices[0];
        Assert.That(leftHandedDevice, Is.TypeOf<XRController>());
        Assert.That(leftHandedDevice.description.interfaceName, Is.EqualTo(XRUtilities.kXRInterface));
        Assert.That(leftHandedDevice.usages.Count, Is.EqualTo(1));
        Assert.That(leftHandedDevice.usages[0], Is.EqualTo(CommonUsages.LeftHand));
        Assert.That(XRController.leftHand, Is.EqualTo(leftHandedDevice));

        var rightHandedDevice = InputSystem.devices[1];
        Assert.That(rightHandedDevice, Is.TypeOf<XRController>());
        Assert.That(rightHandedDevice.description.interfaceName, Is.EqualTo(XRUtilities.kXRInterface));
        Assert.That(rightHandedDevice.usages.Count, Is.EqualTo(1));
        Assert.That(rightHandedDevice.usages[0], Is.EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(rightHandedDevice));
    }

    [Test]
    [Category("Devices")]
    public void Devices_HardwareTrackerCreatesDefaultInputDevice()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "HardwareTracker",
            manufacturer = "XRManufacturer",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.HardwareTracker,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());


        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var hardwareTracker = InputSystem.devices[0];
        Assert.That(hardwareTracker, Is.TypeOf<InputDevice>());
        Assert.That(hardwareTracker.description.interfaceName, Is.EqualTo(XRUtilities.kXRInterface));
        Assert.That(hardwareTracker.usages.Count, Is.EqualTo(0));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRController",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.LeftHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        var controller = InputSystem.devices[0];

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(null));
        Assert.That(XRController.leftHand, Is.EqualTo(controller));

        InputSystem.SetUsage(controller, CommonUsages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(controller));
        Assert.That(XRController.leftHand, Is.EqualTo(null));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRGeneratedLayoutNamesOnlyContainAllowedCharacters()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
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
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Manufacturer::XRThisLayoutShouldhave1ValidName");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutFeaturesOnlyContainAllowedCharacters()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRDevice",
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
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Manufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(1));

        var childControl = generatedLayout.controls[0];
        Assert.That(childControl.name, Is.EqualTo(new InternedString("SimpleFeature1")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRDevicesWithNoOrInvalidCapabilitiesDoNotCreateLayouts()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRDevice1",
            manufacturer = "XRManufacturer",
            capabilities = null
        }.ToJson());

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "XRDevice2",
            manufacturer = "XRManufacturer",
            capabilities = "Not A JSON String"
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRDevice1");
        Assert.That(generatedLayout, Is.EqualTo(null));

        generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRDevice2");
        Assert.That(generatedLayout, Is.EqualTo(null));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_WhenMatchingAKnownDeviceTheGeneratedLayoutInheritsProperly()
    {
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
        {
            interfaceName = XRUtilities.kXRInterface,
            product = "Oculus Rift",
            manufacturer = "Oculus",
            capabilities = new XRDeviceDescriptor
            {
                deviceRole = DeviceRole.LeftHanded,
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        }.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::Oculus::OculusRift");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
        Assert.That(generatedLayout.extendsLayout, Is.EqualTo("OculusHMD"));
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

        public static InputDeviceDescription deviceDescription = new InputDeviceDescription()
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

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [Test]
    [Category("State")]
    public void State_AllFeaturesAlignToCorrectStateEntries()
    {
        testRuntime.ReportNewInputDevice(TestXRDeviceState.deviceDescription.ToJson());

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

        Assert.That((device["Button"] as ButtonControl).isPressed, Is.EqualTo(false));
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(0));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(Vector2.zero));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(Vector3.zero));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(Quaternion.identity));
        Assert.That(device["Custom"], Is.EqualTo(null));
        Assert.That((device["Last"] as ButtonControl).isPressed, Is.EqualTo(false));

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

        Assert.That((device["Button"] as ButtonControl).isPressed, Is.EqualTo(true));
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(17));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(1.24f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(new Vector2(0.1f, 0.2f)));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(new Vector3(0.3f, 0.4f, 0.5f)));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(new Quaternion(0.6f, 0.7f, 0.8f, 0.9f)));
        Assert.That(device["Custom"], Is.EqualTo(null));
        Assert.That((device["Last"] as ButtonControl).isPressed, Is.EqualTo(true));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AllFeatureTypesOccupyTheAppropriateSizeAndType()
    {
        testRuntime.ReportNewInputDevice(TestXRDeviceState.deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("XRInput::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.EqualTo(null));
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
