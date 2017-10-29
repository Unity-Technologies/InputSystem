using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using ISX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngineInternal.Input;

////TODO: make work in player (ATM we rely on the domain reload logic; probably want to include that in debug players, too)

// These tests rely on the default template setup present in the code
// of the system (e.g. they make assumptions about Gamepad is set up).
public class FunctionalTests
{
    [SetUp]
    public void Setup()
    {
        InputSystem.Save();

        ////FIXME: ATM events fired by platform layers for mice and keyboard etc.
        ////       interfere with tests; we need to isolate the system from them
        ////       during testing (probably also from native device discoveries)
        ////       Put a switch in native that blocks events except those coming
        ////       in from C# through SendEvent and which supresses flushing device
        ////       discoveries to managed

        // Put system in a blank state where it has all the templates but has
        // none of the native devices.
        InputSystem.Reset();

        // Make sure we're not affected by the user giving focus away from the
        // game view.
        InputConfiguration.LockInputToGame = true;

        if (InputSystem.devices.Count > 0)
            Assert.Fail("Input system should not have devices after reset");
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.Restore();
    }

    // The test categories give the feature area associated with the test:
    //
    //     a) Controls
    //     b) Templates
    //     c) Devices
    //     d) State
    //     e) Events
    //     f) Actions
    //     g) Bindings
    //     h) Other

    [Test]
    [Category("Templates")]
    public void Templates_CanCreatePrimitiveControlsFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");

        // The default ButtonControl template has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetControl("start").children, Is.Empty);
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanCreateCompoundControlsFromTemplate()
    {
        const int kNumControlsInAStick = 6;

        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.GetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.GetControl("leftStick").children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetControl("leftStick").children, Has.Exactly(1).With.Property("name").EqualTo("x"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUpDeviceFromJsonTemplate()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2""
            }
        ";
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""myThing"",
                        ""template"" : ""MyControl"",
                        ""usage"" : ""LeftStick""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);
        InputSystem.RegisterTemplate(controlJson);

        var setup = new InputControlSetup("MyDevice");

        Assert.That(setup.GetControl("myThing/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.GetControl("myThing"), Has.Property("template").EqualTo("MyControl"));

        var device = setup.Finish();
        Assert.That(device, Is.TypeOf<InputDevice>());
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanExtendControlInBaseTemplateUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""format"" : ""BYTE""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var setup = new InputControlSetup("MyDevice");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.leftStick.x.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetControlParametersThroughControlAttribute()
    {
        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.up.clamp, Is.True);
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUsagesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.Primary2DMotion));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetAliasesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("square"));
        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("x"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetParametersOnControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""clamp=true,clampMin=0.123,clampMax=0.456""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        Assert.That(device.rightTrigger.clamp, Is.True);
        Assert.That(device.rightTrigger.clampMin, Is.EqualTo(0.123).Within(0.00001f));
        Assert.That(device.rightTrigger.clampMax, Is.EqualTo(0.456).Within(0.00001f));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanAddProcessorsToControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        // NOTE: Unfortunately, this currently relies on an internal method (TryGetProcessor).

        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>(), Is.Not.Null);
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().min, Is.EqualTo(0.1).Within(0.00001f));
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().max, Is.EqualTo(0.9).Within(0.00001f));
    }

    [Test]
    [Category("Templates")]
    public void Templates_BooleanParameterDefaultsToTrueIfValueOmitted()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""parameters"" : ""clamp""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad) new InputControlSetup("MyDevice").Finish();

        Assert.That(device.leftStick.x.clamp, Is.True);
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanFindTemplateFromDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""mything.*""
                }
            }
        ";

        InputSystem.RegisterTemplate(json);

        var template = InputSystem.TryFindMatchingTemplate(new InputDeviceDescription
        {
            product = "MyThingy"
        });

        Assert.That(template, Is.EqualTo("MyDevice"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_AddingTwoControlsWithSameName_WillCauseException()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""MyControl"",
                        ""template"" : ""Button""
                    },
                    {
                        ""name"" : ""MyControl"",
                        ""template"" : ""Button""
                    }
                ]
            }
        ";

        // We do minimal processing when adding a template so verification
        // only happens when we actually try to instantiate the template.
        InputSystem.RegisterTemplate(json);

        Assert.That(() => InputSystem.AddDevice("MyDevice"),
            Throws.TypeOf<Exception>().With.Property("Message").Contain("Duplicate control"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_ReplacingDeviceTemplateAffectsAllDevicesUsingTemplate()
    {
        // Create a device hiearchy and then replace the base template. We can't easily use
        // the gamepad (or something similar) as a base template as it will use the Gamepad
        // class which will expect a number of controls to be present on the device.
        const string baseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""first"", ""template"" : ""Button"" },
                    { ""name"" : ""second"", ""template"" : ""Button"" }
                ]
            }
        ";
        const string derivedDeviceJson = @"
            {
                ""name"" : ""MyDerived"",
                ""extend"" : ""MyBase""
            }
        ";
        const string newBaseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""yeah"", ""template"" : ""Stick"" }
                ]
            }
        ";

        InputSystem.RegisterTemplate(derivedDeviceJson);
        InputSystem.RegisterTemplate(baseDeviceJson);

        var device = InputSystem.AddDevice("MyDerived");

        InputSystem.RegisterTemplate(newBaseDeviceJson);

        Assert.That(device.children, Has.Count.EqualTo(1));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("yeah").And.Property("template").EqualTo("Stick"));
    }

    [Test]
    [Category("Templates")]
    public void TODO_Template_ReplacingDeviceTemplateWithTemplateUsingDifferentType_PreservesDeviceIdAndDescription()
    {
        Assert.Fail();
    }

    // Want to ensure that if a state struct declares an "int" field, for example, and then
    // assigns it then Axis template (which has a default format of float), the AxisControl
    // comes out with an "INT" format and not a "FLT" format.
    struct StateStructWithPrimitiveFields : IInputStateTypeInfo
    {
        [InputControl(template = "Axis")] public byte byteAxis;
        [InputControl(template = "Axis")] public short shortAxis;
        [InputControl(template = "Axis")] public int intAxis;
        // No float as that is the default format for Axis anyway.
        [InputControl(template = "Axis")] public double doubleAxis;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }
    [InputState(typeof(StateStructWithPrimitiveFields))]
    class DeviceWithStateStructWithPrimitiveFields : InputDevice
    {
    }

    [Test]
    [Category("Templates")]
    public void Templates_FormatOfControlWithPrimitiveTypeInStateStructInferredFromType()
    {
        InputSystem.RegisterTemplate<DeviceWithStateStructWithPrimitiveFields>("Test");
        var setup = new InputControlSetup("Test");

        Assert.That(setup.GetControl("byteAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
        Assert.That(setup.GetControl("shortAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
        Assert.That(setup.GetControl("intAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeInt));
        Assert.That(setup.GetControl("doubleAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeDouble));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanHaveOneControlUseStateOfAnotherControl()
    {
        // It's useful to be able to say that control X should simply use the same state as control
        // Y. An example of this is the up/down/left/right controls of sticks that simply want to reuse
        // state from the x and y controls already on the stick. "useStateFrom" not only ensures that
        // if the state is moved around we move with it, it allows to redirect entire controls from
        // one part of the hierarchy to another part of the hierarchy (Touchscreen does that to point
        // the controls expected by the base Pointer class to the controls inside of "touch0").
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""test"", ""template"" : ""Axis"", ""useStateFrom"" : ""leftStick/x"" }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var setup = new InputControlSetup("MyDevice");
        var testControl = setup.GetControl<AxisControl>("test");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.stateBlock.alignedSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>()));
        Assert.That(testControl.stateBlock.byteOffset, Is.EqualTo(device.leftStick.x.stateBlock.byteOffset));
        Assert.That(testControl.stateBlock.sizeInBits, Is.EqualTo(device.leftStick.x.stateBlock.sizeInBits));
        Assert.That(testControl.stateBlock.format, Is.EqualTo(device.leftStick.x.stateBlock.format));
        Assert.That(testControl.stateBlock.bitOffset, Is.EqualTo(device.leftStick.x.stateBlock.bitOffset));
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_ReplacingControlTemplateAffectsAllDevicesUsingTemplate()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanAddCustomTemplateFormat()
    {
        //Make it possible to add support for Steam VDF format externally
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceWithNestedState()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        // The gamepad's output state is nested inside GamepadState and requires the template
        // code to crawl inside the field to find the motor controls.
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftMotor"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplateMatchedByDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""interface"" : ""AA|BB"",
                    ""product"" : ""Shtabble""
                }
            }
        ";

        InputSystem.RegisterTemplate(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "BB",
            product = "Shtabble"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.template, Is.EqualTo("MyDevice"));
        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.name, Is.EqualTo("Shtabble")); // Product name becomes device name.
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceCreatedFromDeviceDescriptionStoresDescriptionOnDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""Product""
                }
            }
        ";

        InputSystem.RegisterTemplate(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "Interface",
            product = "Product",
            manufacturer = "Manufacturer",
            deviceClass = "DeviceClass",
            version = "Version",
            serial = "Serial"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.description, Is.EqualTo(description));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplateVariant()
    {
        var leftyGamepadSetup = new InputControlSetup("Gamepad", variant: "Lefty");
        var leftyGamepadPrimary2DMotion = leftyGamepadSetup.GetControl("{Primary2DMotion}");
        var leftyGamepadSecondary2DMotion = leftyGamepadSetup.GetControl("{Secondary2DMotion}");
        var leftyGamepadPrimaryTrigger = leftyGamepadSetup.GetControl("{PrimaryTrigger}");
        var leftyGamepadSecondaryTrigger = leftyGamepadSetup.GetControl("{SecondaryTrigger}");
        //shoulder?

        var defaultGamepadSetup = new InputControlSetup("Gamepad");
        var defaultGamepadPrimary2DMotion = defaultGamepadSetup.GetControl("{Primary2DMotion}");
        var defaultGamepadSecondary2DMotion = defaultGamepadSetup.GetControl("{Secondary2DMotion}");
        var defaultGamepadPrimaryTrigger = defaultGamepadSetup.GetControl("{PrimaryTrigger}");
        var defaultGamepadSecondaryTrigger = defaultGamepadSetup.GetControl("{SecondaryTrigger}");

        var leftyGamepad = (Gamepad)leftyGamepadSetup.Finish();
        var defaultGamepad = (Gamepad)defaultGamepadSetup.Finish();

        Assert.That(leftyGamepad.variant, Is.EqualTo("Lefty"));
        Assert.That(leftyGamepadPrimary2DMotion, Is.SameAs(leftyGamepad.rightStick));
        Assert.That(leftyGamepadSecondary2DMotion, Is.SameAs(leftyGamepad.leftStick));

        Assert.That(defaultGamepadPrimary2DMotion, Is.SameAs(defaultGamepad.leftStick));
        Assert.That(defaultGamepadSecondary2DMotion, Is.SameAs(defaultGamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CannotChangeSetupOfDeviceWhileAddedToSystem()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(() => new InputControlSetup("Keyboard", device), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeControlSetupAfterCreation()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""template"" : ""Button"" },
                    { ""name"" : ""second"", ""template"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterTemplate(initialJson);

        // Create initial version of device.
        var initialSetup = new InputControlSetup("MyDevice");
        var initialFirstControl = initialSetup.GetControl("first");
        var initialSecondControl = initialSetup.GetControl("second");
        var initialDevice = initialSetup.Finish();

        // Change template.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""template"" : ""Button"" },
                    { ""name"" : ""second"", ""template"" : ""Axis"" },
                    { ""name"" : ""third"", ""template"" : ""Button"" }
                ]
            }
        ";
        InputSystem.RegisterTemplate(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputControlSetup("MyDevice", existingDevice: initialDevice);
        var modifiedFirstControl = modifiedSetup.GetControl("first");
        var modifiedSecondControl = modifiedSetup.GetControl("second");
        var modifiedThirdControl = modifiedSetup.GetControl("third");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.SameAs(initialDevice));
        Assert.That(modifiedFirstControl, Is.SameAs(initialFirstControl));
        Assert.That(initialFirstControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.Not.SameAs(initialSecondControl));
        Assert.That(initialSecondControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.TypeOf<AxisControl>());
        Assert.That(modifiedThirdControl, Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeDeviceTypeAfterCreation()
    {
        // Device template for a generic InputDevice.
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""buttonSouth"", ""template"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterTemplate(initialJson);

        // Create initial version of device.
        var initialSetup = new InputControlSetup("MyDevice");
        var initialButton = initialSetup.GetControl<ButtonControl>("buttonSouth");
        var initialDevice = initialSetup.Finish();

        // Change template to now be a gamepad.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";
        InputSystem.RegisterTemplate(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputControlSetup("MyDevice", existingDevice: initialDevice);
        var modifiedButton = modifiedSetup.GetControl<ButtonControl>("buttonSouth");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.Not.SameAs(initialDevice));
        Assert.That(modifiedDevice, Is.TypeOf<Gamepad>());
        Assert.That(initialDevice, Is.TypeOf<InputDevice>());
        Assert.That(modifiedButton, Is.SameAs(initialButton)); // Button survives.
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanChangeHandednessOfXRController()
    {
        var controller = InputSystem.AddDevice("XRController");

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(XRController.leftHand, Is.SameAs(controller));

        InputSystem.SetUsage(controller, CommonUsages.LeftHand);

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(XRController.rightHand, Is.SameAs(controller));
        Assert.That(XRController.leftHand, Is.Not.SameAs(controller));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsInSetupByPath()
    {
        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.TryGetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.TryGetControl("leftStick/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/y"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/up"), Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Controls")]
    public void Controls_DeviceAndControlsRememberTheirTemplates()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.template, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.template, Is.EqualTo("Stick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirParent()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirDevices()
    {
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(leftStick.device, Is.SameAs(device));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AskingValueOfControlBeforeDeviceAddedToSystemIsInvalidOperation()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.Throws<InvalidOperationException>(() =>
            {
                var value = device.leftStick.value;
            });
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanProcessDeadzones()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        ////NOTE: Unfortunately, this relies on an internal method ATM.
        var processor = device.leftStick.TryGetProcessor<DeadzoneProcessor>();

        var firstState = new GamepadState {leftStick = new Vector2(0.05f, 0.05f)};
        var secondState = new GamepadState {leftStick = new Vector2(0.5f, 0.5f)};

        InputSystem.QueueStateEvent(device, firstState);
        InputSystem.Update();

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));

        InputSystem.QueueStateEvent(device, secondState);
        InputSystem.Update();

        Assert.That(device.leftStick.value, Is.EqualTo(processor.Process(new Vector2(0.5f, 0.5f))));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanChangeDefaultDeadzoneValuesOnTheFly()
    {
        // Deadzone processor with no specified min/max should take default values
        // from InputConfiguration.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        var processor = device.leftStick.TryGetProcessor<DeadzoneProcessor>();

        Assert.That(processor.minOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMin));
        Assert.That(processor.maxOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMax));

        InputConfiguration.DeadzoneMin = InputConfiguration.DeadzoneMin + 0.1f;
        InputConfiguration.DeadzoneMax = InputConfiguration.DeadzoneMin - 0.1f;

        Assert.That(processor.minOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMin));
        Assert.That(processor.maxOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMax));
    }

    [Test]
    [Category("Controls")]
    public void Controls_SticksProvideAccessToHalfAxes()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.down.value, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.right.value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.left.value, Is.EqualTo(0.0).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(-0.5f, -0.5f) });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.value, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.down.value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.right.value, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.left.value, Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanQueryValueFromStateEvents()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                Assert.That(gamepad.leftTrigger.ReadValueFrom(eventPtr), Is.EqualTo(0.234f).Within(0.00001));
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.234f });
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DpadVectorsAreCircular()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Up.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));
        Assert.That(gamepad.dpad.value, Is.EqualTo(Vector2.up));

        // Up left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));

        // Left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));
        Assert.That(gamepad.dpad.value, Is.EqualTo(Vector2.left));

        // Down left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));

        // Down.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));
        Assert.That(gamepad.dpad.value, Is.EqualTo(Vector2.down));

        // Down right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));

        // Down.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));
        Assert.That(gamepad.dpad.value, Is.EqualTo(Vector2.right));

        // Up right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.value.magnitude, Is.EqualTo(1).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_DevicesGetNameFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device.name, Contains.Substring("Gamepad"));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.stateBlock.sizeInBits, Is.EqualTo(Marshal.SizeOf<GamepadState>() * 8));
        Assert.That(gamepad.leftStick.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf<GamepadState>("leftStick").ToInt32()));
        Assert.That(gamepad.dpad.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf<GamepadState>("buttons").ToInt32()));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        var setup = new InputControlSetup("Gamepad");
        var rightMotor = setup.GetControl("rightMotor");
        setup.Finish();

        var outputOffset = Marshal.OffsetOf<GamepadState>("motors").ToInt32();
        var rightMotorOffset = outputOffset + Marshal.OffsetOf<GamepadOutputState>("rightMotor").ToInt32();

        Assert.That(rightMotor.stateBlock.byteOffset, Is.EqualTo(rightMotorOffset));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForMultiByteBitfieldWithFixedOffset()
    {
        var setup = new InputControlSetup("Keyboard");
        var downArrow = setup.GetControl("DownArrow");
        var keyboard = setup.Finish();

        Assert.That(downArrow.stateBlock.bitOffset, Is.EqualTo((int)Key.DownArrow));
        Assert.That(downArrow.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(keyboard.stateBlock.alignedSizeInBytes, Is.EqualTo(KeyboardState.kSizeInBytes));
    }

    [Test]
    [Category("State")]
    public void State_BeforeAddingDevice_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(leftStickXOffset));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_AfterAddingDevice_AllControlOffsetsAreRelativeToGlobalStateBuffer()
    {
        InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        var gamepad2StartOffset = gamepad2.stateBlock.byteOffset;

        Assert.That(gamepad2.leftStick.x.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickXOffset));
        Assert.That(gamepad2.leftStick.y.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_StateOfMultipleDevicesIsLaidOutSequentially()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        var sizeofGamepadState = Marshal.SizeOf<GamepadState>();

        Assert.That(gamepad1.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(gamepad1.stateBlock.byteOffset + sizeofGamepadState));
    }

    [Test]
    [Category("State")]
    public void State_RunningUpdateSwapsCurrentAndPrevious()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var oldState = new GamepadState
        {
            leftTrigger = 0.25f
        };
        var newState = new GamepadState
        {
            leftTrigger = 0.75f
        };

        InputSystem.QueueStateEvent(gamepad, oldState);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.25f).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.75f).Within(0.00001));
        Assert.That(gamepad.leftTrigger.previous, Is.EqualTo(0.25f).Within(0.00001));
    }

    [Test]
    [Category("State")]
    public void State_RunningMultipleFixedUpdates_FlipsDynamicUpdateBuffersOnlyOnFirstUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.25, previous=0.0
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.75, previous=0.0

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.previous, Is.Zero);
    }

    [Test]
    [Category("State")]
    public void State_RunningNoFixedUpdateInFrame_StillCapturesStateForNextFixedUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Fixed); // Fixed: current=0.75, previous=0.0

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
        InputSystem.Update(InputUpdateType.Dynamic); // Fixed: current=0.25, previous=0.75
        InputSystem.Update(InputUpdateType.Fixed); // Unchanged.

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.25).Within(0.000001));
        Assert.That(gamepad.leftTrigger.previous, Is.EqualTo(0.75).Within(0.000001));
    }

    // This test makes sure that a double-buffered state scheme does not lose state. In double buffering,
    // this only works if either the entire state is refreshed each step -- which for us is not guaranteed
    // as we don't know if a state event for a device will happen on a frame -- or if state is copied forward
    // between the buffers.
    [Test]
    [Category("State")]
    public void State_UpdateWithoutStateEventDoesNotAlterStateOfDevice()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var state = new GamepadState
        {
            leftTrigger = 0.25f
        };

        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.25f).Within(0.000001));
    }

    // The state layout for a given device is not fixed. Even though Gamepad, for example, specifies
    // GamepadState as its state struct, this does not necessarily mean that an actual Gamepad instance
    // will actually end up with that specific state layout. This is why Gamepad should not assume
    // that 'currentValuePtr' is a pointer to a GamepadState.
    //
    // Templates can be used to re-arrange the state layout of their base template. One case where
    // this is useful are HIDs. On OSX, for example, gamepad state data does not arrive in its own
    // distinct format but rather comes in as the same generic state data as any other HID device.
    // Yet we still want a gamepad to come out as a Gamepad and not as a generic InputDevice. If we
    // weren't able to customize the state layout of a gamepad, we'd have to have code somewhere
    // along the way that takes the incoming HID data, interprets it to determine that it is in
    // fact coming from a gamepad HID, and re-arranges it into a GamepadState-compatible format
    // (which requires knowledge of the specific layout used by the HID). By having flexibly state
    // layouts we can do this entirely through data using just templates.
    //
    // A template that customizes state layout can also "park" unused controls outside the block of
    // data that will actually be sent in via state events. Space for the unused controls will still
    // be allocated in the state buffers (since InputControls still refer to it) but InputManager
    // is okay with sending StateEvents that are shorter than the full state block of a device.
    ////REVIEW: we might want to equip InputControls with the ability to be disabled (in which case the return default values)
    [Test]
    [Category("State")]
    public void State_CanCustomizeStateLayoutOfDevice()
    {
        // Create a custom template that moves the offsets of some controls around.
        var jsonTemplate = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""format"" : ""CUST"",
                ""controls"" : [
                    {
                        ""name"" : ""buttonSouth"",
                        ""offset"" : 800
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(jsonTemplate);

        var setup = new InputControlSetup("CustomGamepad");
        Assert.That(setup.GetControl("buttonSouth").stateBlock.byteOffset, Is.EqualTo(800));

        var device = (Gamepad)setup.Finish();
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(801 * 8)); // Button bitfield adds one byte.
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanReformatAndResizeControlHierarchy()
    {
        // Turn left stick into a 2D vector of shorts. Need to reformat the up/down/left/right
        // axes along with X and Y.
        // NOTE: Child offsets are not absolute! They are relative to their parent.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick"", ""format"" : ""VC2S"", ""offset"" : 6 },
                    { ""name"" : ""leftStick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/y"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/up"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/down"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/left"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/right"", ""format"" : ""SHRT"", ""offset"" : 0 }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad) new InputControlSetup("MyDevice").Finish();

        Assert.That(device.leftStick.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.up.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.up.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.down.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.down.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.left.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.left.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.right.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.right.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
    }

    [Test]
    [Category("State")]
    public void State_CanStoreAxisAsShort()
    {
        // Make right trigger be represented as just a short and force it to different offset.
        var jsonTemplate = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""format"" : ""SHRT"",
                        ""offset"" : 0
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(jsonTemplate);

        var setup = new InputControlSetup("CustomGamepad");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.rightTrigger.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
    }

    [Test]
    [Category("State")]
    public void State_AppendsControlsWithoutForcedOffsetToEndOfState()
    {
        var json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""controlWithFixedOffset"",
                        ""template"" : ""Analog"",
                        ""offset"" : ""10"",
                        ""format"" : ""FLT""
                    },
                    {
                        ""name"" : ""controlWithAutomaticOffset"",
                        ""template"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var setup = new InputControlSetup("MyDevice");

        Assert.That(setup.GetControl("controlWithAutomaticOffset").stateBlock.byteOffset, Is.EqualTo(14));

        var device = setup.Finish();
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(15 * 8));
    }

    [Test]
    [Category("State")]
    public void State_CanSpecifyBitOffsetsOnControlProperties()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.dpad.right.stateBlock.bitOffset, Is.EqualTo((int)DpadControl.ButtonBits.Right));
        Assert.That(gamepad.dpad.right.stateBlock.byteOffset, Is.EqualTo(gamepad.dpad.stateBlock.byteOffset));
    }

    [Test]
    [Category("State")]
    public void State_CanUpdateButtonState()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.bButton.isPressed, Is.False);

        var newState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.bButton.isPressed, Is.True);
    }

    [Test]
    [Category("State")]
    public void State_CanDetectWhetherButtonStateHasChangedThisFrame()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.bButton.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.bButton.wasReleasedThisFrame, Is.False);

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.Update();

        Assert.That(gamepad.bButton.wasPressedThisFrame, Is.True);
        Assert.That(gamepad.bButton.wasReleasedThisFrame, Is.False);

        var secondState = new GamepadState {buttons = 0};
        InputSystem.QueueStateEvent(gamepad, secondState);
        InputSystem.Update();

        Assert.That(gamepad.bButton.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.bButton.wasReleasedThisFrame, Is.True);
    }

    // The way we keep state does not allow observing the state change on the final
    // state of the button. However, actions will still see the change.
    [Test]
    [Category("State")]
    public void State_PressingAndReleasingButtonInSameFrame_DoesNotShowStateChange()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(gamepad.bButton.isPressed, Is.False);
        Assert.That(gamepad.bButton.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.bButton.wasReleasedThisFrame, Is.False);
    }

    [Test]
    [Category("State")]
    public void State_CanStoreButtonAsFloat()
    {
        // Turn buttonSouth into float and move to left/x offset (so we can use
        // GamepadState to set it).
        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""buttonSouth"",
                        ""format"" : ""FLT"",
                        ""offset"" : 4
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var state = new GamepadState {leftStick = new Vector2(0.5f, 0.0f)};

        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(gamepad.aButton.value, Is.EqualTo(0.5f));
    }

    // Controls like mouse deltas need to reset to zero when there is no activity on them in a frame.
    // This could be done by requiring the state producing code to always send appropriate state events
    // when necessary. However, for state producers that are hooked to event sources (like eg. NSEvents
    // on OSX and MSGs on Windows), this can be very awkward to handle as it requires synchronizing with
    // input updates and can complicate state producer logic quite a bit.
    //
    // So, instead of putting the burden on state producers, controls come with an auto-reset features
    // that will automatically cause the system to clear memory of controls when needed.
    [Test]
    [Category("State")]
    public void TODO_State_CanAutomaticallyResetIndividualControlsBetweenFrames()
    {
        // Make leftStick/x automatically reset on gamepad.
        var json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""autoReset"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        InputSystem.QueueStateEvent(device, new GamepadState {leftStick = new Vector2(0.123f, 0.456f)});
        InputSystem.Update();

        Assert.That(device.leftStick.x.value, Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.leftStick.y.value, Is.EqualTo(0.456).Within(0.000001));

        InputSystem.Update();

        Assert.That(device.leftStick.x.value, Is.Zero);
        Assert.That(device.leftStick.y.value, Is.EqualTo(0.456).Within(0.000001));

        ////TODO: this test will require a corresponding test that actions see resets properly
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromTemplate()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTwiceIsIgnored()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.onDeviceChange +=
            (d, c) => Assert.Fail("Shouldn't send notification for duplicate adding of device.");

        InputSystem.AddDevice(device);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTriggersNotification()
    {
        var receivedCallCount = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                ++receivedCallCount;
                receivedDevice = device;
                receiveDeviceChange = change;
            };

        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(receivedCallCount, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Added));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItConnected()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device.connected, Is.False);

        InputSystem.AddDevice(device);

        Assert.That(device.connected, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.TryGetDeviceById(device.id), Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByTemplate()
    {
        var device = InputSystem.AddDevice("Gamepad");
        var result = InputSystem.GetDevice("Gamepad");

        Assert.That(result, Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EnsuresDeviceNamesAreUnique()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AssignsUniqueNumericIdToDevices()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.id, Is.Not.EqualTo(gamepad2.id));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeDisconnectedAndReconnected()
    {
        var device = InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                ++receivedCalls;
                receivedDevice = d;
                receiveDeviceChange = c;
            };

        InputSystem.QueueDisconnectEvent(device);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Disconnected));
        Assert.That(device.connected, Is.False);

        receivedCalls = 0;
        receivedDevice = null;
        receiveDeviceChange = null;

        InputSystem.QueueConnectEvent(device);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Connected));
        Assert.That(device.connected, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ResetToDefaultStateWhenDisconnected()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.234f });
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.234).Within(0.000001));

        InputSystem.QueueDisconnectEvent(gamepad);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.0f));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddTemplateForDeviceThatsAlreadyBeenReported()
    {
        InputSystem.ReportAvailableDevice(new InputDeviceDescription {product = "MyController"});

        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""MyController""
                }
            }
        ";

        InputSystem.RegisterTemplate(json);

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("template").EqualTo("CustomGamepad").And.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeRemoved()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad3 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var gamepad2Offset = gamepad2.stateBlock.byteOffset;

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                ++receivedCalls;
                receivedDevice = device;
                receivedChange = change;
            };

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad3));
        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad2));
        Assert.That(receivedChange, Is.EqualTo(InputDeviceChange.Removed));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(0)); // Should have lost its offset into state buffers.
        Assert.That(gamepad3.stateBlock.byteOffset, Is.EqualTo(gamepad2Offset)); // 3 should have moved into 2's position.
        Assert.That(gamepad2.leftStick.stateBlock.byteOffset,
            Is.EqualTo(UnsafeUtility.OffsetOf<GamepadState>("leftStick"))); // Should have unbaked offsets in control hierarchy.
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeReadded()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        InputSystem.RemoveDevice(gamepad);
        InputSystem.AddDevice(gamepad);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.5f).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanQueryAllGamepadsWithSimpleGetter()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        Assert.That(Gamepad.all, Has.Count.EqualTo(2));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad2));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_AllGamepadListRefreshesWhenGamepadIsAdded()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gamepad")]
    [TestCase("Keyboard")]
    [TestCase("Pointer")]
    [TestCase("Mouse")]
    [TestCase("Pen")]
    [TestCase("Touchscreen")]
    [TestCase("HMD")]
    [TestCase("XRController")]
    public void Devices_CanCreateDevice(string template)
    {
        var device = InputSystem.AddDevice(template);

        Assert.That(device, Is.InstanceOf<InputDevice>());
        Assert.That(device.template, Is.EqualTo(template));
    }

    [Test]
    [Category("Devices")]
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [TestCase("Xbox One Wired Controller", "Microsoft", "HID", "Gamepad")]
#endif
    public void Devices_SupportsPlatformsNativeDevice(string product, string manufacturer, string interfaceName, string baseTemplate)
    {
        var description = new InputDeviceDescription
        {
            interfaceName = interfaceName,
            product = product,
            manufacturer = manufacturer
        };

        InputDevice device = null;
        Assert.That(() => device = InputSystem.AddDevice(description), Throws.Nothing);
        Assert.That(InputSystem.GetControls($"/<{baseTemplate}>"), Has.Exactly(1).SameAs(device));
        Assert.That(device.name, Is.EqualTo(product));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCheckAnyKeyOnKeyboard()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();

        Assert.That(keyboard.any.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanPerformHorizontalAndVerticalScrollWithMouse()
    {
        var mouse = (Mouse)InputSystem.AddDevice("Mouse");

        InputSystem.QueueStateEvent(mouse.scrollWheel, new Vector2(10, 12));
        InputSystem.Update();

        Assert.That(mouse.scrollWheel.x.value, Is.EqualTo(10).Within(0.0000001));
        Assert.That(mouse.scrollWheel.y.value, Is.EqualTo(12).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_TouchscreenCanFunctionAsPointer()
    {
        Assert.Fail();
    }

    struct CustomDeviceState : IInputStateTypeInfo
    {
        public static FourCC Type = new FourCC('C', 'U', 'S', 'T');

        [InputControl(template = "Axis")]
        public float axis;

        public FourCC GetFormat()
        {
            return Type;
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControls()
    {
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));

        var device = setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AfterAddingDeviceCanQueryValueOfControls()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPath()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/Gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPathCaseInsensitive()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamePAD/LeftSTICK");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{Primary2DMotion}");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOfControlsFoundByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{Primary2DMotion}/x");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick.x));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTemplate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/<stick>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDevicesByTemplates()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByBaseTemplate()
    {
        const string json = @"
            {
                ""name"" : ""MyGamepad"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = InputSystem.AddDevice("MyGamepad");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(device));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsFromMultipleDevices()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var matches = InputSystem.GetControls("/*/*Stick");

        Assert.That(matches, Has.Count.EqualTo(4));

        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.rightStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.rightStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanOmitLeadingSlashWhenFindingControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTheirAliases()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matchByName = InputSystem.GetControls("/gamepad/buttonSouth");
        var matchByAlias1 = InputSystem.GetControls("/gamepad/a");
        var matchByAlias2 = InputSystem.GetControls("/gamepad/cross");

        Assert.That(matchByName, Has.Count.EqualTo(1));
        Assert.That(matchByName, Has.Exactly(1).SameAs(gamepad.aButton));
        Assert.That(matchByAlias1, Is.EqualTo(matchByName));
        Assert.That(matchByAlias2, Is.EqualTo(matchByName));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcardsInMiddleOfNames()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/g*pad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCustomizePressPointOfGamepadTriggers()
    {
        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""pressPoint=0.2""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var gamepad = (Gamepad) new InputControlSetup("CustomGamepad").Finish();

        Assert.That(gamepad.rightTrigger.pressPoint, Is.EqualTo(0.2f).Within(0.0001f));
    }

    // This is one of the most central tests. If this one breaks, it most often
    // hints at the state layouting or state updating machinery being borked.
    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.value, Is.EqualTo(0.123f));
        Assert.That(gamepad.leftStick.y.value, Is.EqualTo(0.456f));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdatePartialStateOfDeviceWithEvent()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Full state update to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 0xffffffff, rightStick = Vector2.one, leftTrigger = 0.123f, rightTrigger = 0.456f });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueStateEvent(gamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.y.value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.123).Within(0.000001));
        Assert.That(gamepad.rightStick.x.value, Is.EqualTo(1).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateEventToDeviceMakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState();

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithoutBeforeRenderEnabled_DoesNothingInBeforeRenderUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.value, Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithBeforeRenderEnabled_UpdatesDeviceInBeforeRender()
    {
        // Could use one of the tracking templates but let's do it with a
        // custom template that enables before render updates on a gamepad.
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var newState = new GamepadState { leftTrigger = 0.123f };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.123f).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenToEventStream()
    {
        var device = InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputSystem.onEvent += inputEvent =>
            {
                ++receivedCalls;
                Assert.That(inputEvent.IsA<StateEvent>(), Is.True);
                Assert.That(inputEvent.device, Is.SameAs(device));
            };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Events")]
    public void Events_AreProcessedInOrderTheyAreQueuedIn()
    {
        const double kFirstTime = 0.5;
        const double kSecondTime = 1.5;
        const double kThirdTime = 2.5;

        var receivedCalls = 0;
        var receivedFirstTime = 0.0;
        var receivedSecondTime = 0.0;
        var receivedThirdTime = 0.0;

        InputSystem.onEvent +=
            inputEvent =>
            {
                ++receivedCalls;
                if (receivedCalls == 1)
                    receivedFirstTime = inputEvent.time;
                else if (receivedCalls == 2)
                    receivedSecondTime = inputEvent.time;
                else
                    receivedThirdTime = inputEvent.time;
            };

        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState(), kSecondTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kFirstTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kThirdTime);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(3));
        Assert.That(receivedFirstTime, Is.EqualTo(kSecondTime).Within(0.00001));
        Assert.That(receivedSecondTime, Is.EqualTo(kFirstTime).Within(0.00001));
        Assert.That(receivedThirdTime, Is.EqualTo(kThirdTime).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanQueueAndReceiveEventsAgainstNonExistingDevices()
    {
        // Device IDs are looked up only *after* the system shows the event to us.

        var receivedCalls = 0;
        var receivedDeviceId = InputDevice.kInvalidDeviceId;
        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                receivedDeviceId = eventPtr.deviceId;
            };

        var inputEvent = ConnectEvent.Create(4, 1.0);
        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDeviceId, Is.EqualTo(4));
    }

    [Test]
    [Category("Events")]
    public void Events_HandledFlagIsResetWhenEventIsQueued()
    {
        var receivedCalls = 0;
        var wasHandled = true;

        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                wasHandled = eventPtr.handled;
            };

        var inputEvent = ConnectEvent.Create(4, 1.0);

        // This should go back to false when we inputEvent goes on the queue.
        // The way the behavior is implemented is a side-effect of how we store
        // the handled flag as a bit on the event ID -- which will get set by
        // native on an event when it is queued.
        inputEvent.baseEvent.handled = true;

        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(wasHandled, Is.False);
    }

    [Test]
    [Category("Events")]
    public void Events_AlreadyHandledEventsAreIgnoredWhenProcessingEvents()
    {
        // Need a device with before render enabled so we can produce
        // the effect of having already handled events in the event queue.
        // If we use an invalid device, before render updates will simply
        // ignore the event.
        const string json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = InputSystem.AddDevice("CustomGamepad");

        InputSystem.onEvent +=
            inputEvent =>
            {
                inputEvent.handled = true;
            };

        var event1 = ConnectEvent.Create(device.id, 1.0);
        var event2 = ConnectEvent.Create(device.id, 2.0);

        InputSystem.QueueEvent(ref event1);

        // Before render update won't clear queue so after the update
        // event1 is still in there.
        InputSystem.Update(InputUpdateType.BeforeRender);

        // Add new unhandled event.
        InputSystem.QueueEvent(ref event2);

        var receivedCalls = 0;
        var receivedTime = 0.0;

        InputSystem.onEvent +=
            inputEvent =>
            {
                ++receivedCalls;
                receivedTime = inputEvent.time;
            };
        InputSystem.Update();

        // On the second update, we should have seen only event2.
        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedTime, Is.EqualTo(2.0).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanPreventEventsFromBeingProcessed()
    {
        InputSystem.onEvent +=
            inputEvent =>
            {
                // If we mark the event handled, the system should skip it and not
                // let it go to the device.
                inputEvent.handled = true;
            };

        var device = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState { rightTrigger = 0.45f });
        InputSystem.Update();

        Assert.That(device.rightTrigger.value, Is.EqualTo(0.0).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanTraceEventsOfDevice()
    {
        var device = InputSystem.AddDevice("Gamepad");
        var noise = InputSystem.AddDevice("Gamepad");

        using (var trace = new InputEventTrace {deviceId = device.id})
        {
            trace.Enable();
            Assert.That(trace.enabled, Is.True);

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(noise, new GamepadOutputState()); // This one just to make sure we don't get it.

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));

            Assert.That(events[0].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[0].deviceId, Is.EqualTo(device.id));
            Assert.That(events[0].time, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(events[0].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref firstState), StateEvent.From(events[0])->state,
                    UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.id));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState), StateEvent.From(events[1])->state,
                    UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_WhenTraceIsFull_WillStartOverwritingOldEvents()
    {
        var device = InputSystem.AddDevice("Gamepad");
        using (var trace =
                   new InputEventTrace(StateEvent.GetEventSizeWithPayload<GamepadState>() * 2) {deviceId = device.id})
        {
            trace.Enable();

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};
            var thirdState = new GamepadState {leftTrigger = 0.95f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(device, thirdState, 2.5);

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(1.5).Within(0.000001));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(2.5).Within(0.000001));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanClearEventTrace()
    {
        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            var device = InputSystem.AddDevice("Gamepad");
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.Update();

            Assert.That(trace.ToList(), Has.Count.EqualTo(2));

            trace.Clear();

            Assert.That(trace.ToList(), Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_GetUniqueIds()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.QueueStateEvent(device, new GamepadState());

        var receivedCalls = 0;
        var firstId = InputEvent.kInvalidId;
        var secondId = InputEvent.kInvalidId;

        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                if (receivedCalls == 1)
                    firstId = eventPtr.id;
                else if (receivedCalls == 2)
                    secondId = eventPtr.id;
            };

        InputSystem.Update();

        Assert.That(firstId, Is.Not.EqualTo(secondId));
    }

    [Test]
    [Category("Events")]
    public void Events_DoNotLeakIntoNextUpdate()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState(), 1.0);
        InputSystem.QueueStateEvent(device, new GamepadState(), 2.0);

        var receivedUpdateCalls = 0;
        var receivedEventCount = 0;

        NativeUpdateCallback onUpdate =
            (updateType, eventCount, eventData) =>
            {
                ++receivedUpdateCalls;
                receivedEventCount += eventCount;
            };
        NativeInputSystem.onUpdate += onUpdate;

        InputSystem.Update();

        Assert.That(receivedUpdateCalls, Is.EqualTo(1));
        Assert.That(receivedEventCount, Is.EqualTo(2));

        receivedEventCount = 0;
        receivedUpdateCalls = 0;

        InputSystem.Update();

        Assert.That(receivedEventCount, Is.Zero);
        Assert.That(receivedUpdateCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Events")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightTrigger = 0.5f }, 2.0);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightTrigger = 0.75f }, 1.0);
        InputSystem.Update();

        Assert.That(gamepad.rightTrigger.value, Is.EqualTo(0.5f).Within(0.000001));
    }

    [InputState(typeof(CustomDeviceState))]
    class CustomDevice : InputDevice, IInputUpdateCallbackReceiver
    {
        public AxisControl axis { get; private set; }

        public int onUpdateCallCount;
        public InputUpdateType onUpdateType;

        public void OnUpdate(InputUpdateType updateType)
        {
            ++onUpdateCallCount;
            onUpdateType = updateType;
            InputSystem.QueueStateEvent(this, new CustomDeviceState {axis = 0.234f});
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            axis = setup.GetControl<AxisControl>(this, "axis");
            base.FinishSetup(setup);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdateDeviceWithEventsFromUpdateCallback()
    {
        InputSystem.RegisterTemplate<CustomDevice>();
        var device = (CustomDevice)InputSystem.AddDevice("CustomDevice");

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.EqualTo(1));
        Assert.That(device.onUpdateType, Is.EqualTo(InputUpdateType.Dynamic));
        Assert.That(device.axis.value, Is.EqualTo(0.234).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingDeviceCleansUpUpdat3Callback()
    {
        InputSystem.RegisterTemplate<CustomDevice>();
        var device = (CustomDevice)InputSystem.AddDevice("CustomDevice");
        InputSystem.RemoveDevice(device);

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotQueryControlsOnActionThatIsNotEnabled()
    {
        var action = new InputAction();

        Assert.That(() => action.controls, Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsSingleControl()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsMultipleControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/*stick");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_GoesIntoWaitingPhase()
    {
        InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionSet()
    {
        var action = new InputAction();

        Assert.That(action.actionSet, Is.Null);
    }

    ////REVIEW: not sure whether this is the best behavior
    [Test]
    [Category("Actions")]
    public void Actions_PathLeadingNowhereIsIgnored()
    {
        var action = new InputAction(binding: "nothing");

        Assert.DoesNotThrow(() => action.Enable());
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartOutInDisabledPhase()
    {
        var action = new InputAction();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));
    }

    [Test]
    [Category("Actions")]
    public void Actions_LoseActionHasNoSet()
    {
        var action = new InputAction();
        action.Enable(); // Force to create private action set.

        Assert.That(action.actionSet, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionIsPerformedWhenSourceControlChangesValue()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputAction receivedAction = null;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
                receivedAction = ctx.action;
                receivedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.Enable();

        var state = new GamepadState
        {
            leftStick = new Vector2(0.5f, 0.5f)
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanListenForStateChangeOnEntireDevice()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad");
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
                receivedControl = ctx.control;
            };
        action.Enable();

        var state = new GamepadState
        {
            rightTrigger = 0.5f
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad)); // We do not drill down to find the actual control that changed.
    }

    // Actions are able to observe every state change, even if the changes occur within
    // the same frame.
    [Test]
    [Category("Actions")]
    public void Actions_PressingAndReleasingButtonInSameFrame_StillTriggersAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/<gamepad>/<button>");

        var receivedCalls = 0;
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
            };
        action.Enable();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", modifiers: "hold(duration=0.4)");
        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedAction = ctx.action;
                performedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.started +=
            ctx =>
            {
                ++startedReceivedCalls;
                startedAction = ctx.action;
                startedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Started));
            };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.aButton));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 0.5);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.aButton));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformTapAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", modifiers: "tap");
        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedAction = ctx.action;
                performedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.started +=
            ctx =>
            {
                ++startedReceivedCalls;
                startedAction = ctx.action;
                startedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Started));
            };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.aButton));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), InputConfiguration.TapTime);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.aButton));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionSetFromJson()
    {
        const string json = @"
            {
                ""sets"" : [
                    {
                        ""name"" : ""default"",
                        ""actions"" : [ { ""name"" : ""jump"" } ]
                    }
                ]
            }
        ";

        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(1));
        Assert.That(sets[0], Has.Property("name").EqualTo("default"));
        Assert.That(sets[0].actions, Has.Count.EqualTo(1));
        Assert.That(sets[0].actions, Has.Exactly(1).With.Property("name").EqualTo("jump"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryAllEnabledActions()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var enabledActions = InputSystem.FindAllEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAction()
    {
        var action = new InputAction(name: "MyAction", binding: "/gamepad/leftStick");

        // Unity's JSON serializer goes through Unity's normal serialization machinery so if
        // this works, we should have a pretty good shot that binary and YAML serialization
        // are also working.
        var json = JsonUtility.ToJson(action);
        var deserializedAction = JsonUtility.FromJson<InputAction>(json);

        Assert.That(deserializedAction.name, Is.EqualTo(action.name));
        Assert.That(deserializedAction.bindings, Has.Count.EqualTo(1));
        Assert.That(deserializedAction.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddMultipleBindings()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftStick");
        action.AddBinding("/gamepad/rightStick");

        action.Enable();

        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action.bindings[1].path, Is.EqualTo("/gamepad/rightStick"));

        var performedReceivedCalls = 0;
        InputControl performedControl = null;

        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedControl = ctx.control;
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.leftStick));

        performedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenNewDeviceIsAdded()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/<gamepad>/buttonSouth");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad1.aButton));

        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.aButton));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.aButton));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanFindEnabledActions()
    {
        var action1 = new InputAction(name: "a");
        var action2 = new InputAction(name: "b");

        action1.Enable();
        action2.Enable();

        var enabledActions = InputSystem.FindAllEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(2));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action2));
    }

    private class TestModifier : IInputActionModifier
    {
        public float parm1;

        public static bool s_GotInvoked;

        public void Process(ref InputAction.ModifierContext context)
        {
            Assert.That(parm1, Is.EqualTo(5.0).Within(0.000001));
            s_GotInvoked = true;
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRegisterNewModifier()
    {
        InputSystem.RegisterModifier<TestModifier>();
        TestModifier.s_GotInvoked = false;

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick/x", modifiers: "test(parm1=5.0)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(TestModifier.s_GotInvoked, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTriggerActionFromPartialStateUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var receivedCalls = 0;
        InputControl receivedControl = null;
        action.performed += ctx =>
            {
                ++receivedCalls;
                receivedControl = ctx.control;
            };

        InputSystem.QueueStateEvent(gamepad.leftStick, Vector2.one);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDistinguishTapAndSlowTapOnSameAction()
    {
        // Bindings can have more than one modifier. Depending on the interaction happening on the bound
        // controls one of the modifiers may initiate a phase shift and which modifier initiated the
        // shift is visible on the callback.
        //
        // This is most useful for allowing variations of the same action. For example, you can have a
        // "Fire" action, bind it to the "PrimaryAction" button, and then put both a TapModifier and a
        // SlowTapModifier on the same binding. In the 'performed' callback you can then detect whether
        // the button was slow-pressed or fast-pressed. Depending on that, you can perform a normal
        // fire action or a charged fire action.

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/buttonSouth",
                modifiers: "tap(duration=0.1),slowTap(duration=0.5)");
        action.Enable();

        var started = new System.Collections.Generic.List<InputAction.CallbackContext>();
        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        var cancelled = new System.Collections.Generic.List<InputAction.CallbackContext>();

        action.started += ctx => started.Add(ctx);
        action.performed += ctx => performed.Add(ctx);
        action.cancelled += ctx => cancelled.Add(ctx);

        // Perform tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0}, 0.05);
        InputSystem.Update();

        // Only tap was started.
        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(started[0].modifier, Is.TypeOf<TapModifier>());

        // Only tap was performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<TapModifier>());

        // Nothing was cancelled.
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();
        cancelled.Clear();

        // Perform slow tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 2.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0}, 2.0 + InputConfiguration.SlowTapTime + 0.0001);
        InputSystem.Update();

        // First tap was started, then slow tap was started.
        Assert.That(started, Has.Count.EqualTo(2));
        Assert.That(started[0].modifier, Is.TypeOf<TapModifier>());
        Assert.That(started[1].modifier, Is.TypeOf<SlowTapModifier>());

        // Tap got cancelled.
        Assert.That(cancelled, Has.Count.EqualTo(1));
        Assert.That(cancelled[0].modifier, Is.TypeOf<TapModifier>());

        // Slow tap got performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<SlowTapModifier>());
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanCombineBindings()
    {
        // Set up an action that requires the left trigger to be held when pressing the A button.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        // Last control in combination is considered the trigger control.
        Assert.That(performed[0].control, Is.SameAs(gamepad.aButton));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CombinedBindingsTriggerIfControlsActivateAtSameTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CombinedBindingsDoNotTriggerIfControlsActivateInWrongOrder()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);
    }

    // The ability to combine bindings and have modifiers on them is crucial to be able to perform
    // most gestures as they usually require a button-like control that indicates whether a possible
    // gesture has started and then a positional control of some kind that gives the motion data for
    // the gesture.
    [Test]
    [Category("Action")]
    public void TODO_Actions_CanCombineBindingsWithModifiers()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        // Tap or slow tap on A button when left trigger is held.
        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth", modifiers: "tap,slowTap");
        action.Enable();

        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 0}, InputConfiguration.SlowTapTime + 0.1);
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<SlowTapModifier>());
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanPerformContinuousAction()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick", modifiers: "continuous");
        action.Enable();

        var started = new System.Collections.Generic.List<InputAction.CallbackContext>();
        var performed = new System.Collections.Generic.List<InputAction.CallbackContext>();
        var cancelled = new System.Collections.Generic.List<InputAction.CallbackContext>();

        action.started += ctx => performed.Add(ctx);
        action.cancelled += ctx => performed.Add(ctx);
        action.performed +=
            ctx =>
            {
                performed.Add(ctx);
                Assert.That(ctx.GetValue<Vector2>(), Is.EqualTo(new Vector2(0.123f, 0.456f)));
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.456f)});
        InputSystem.Update();
        InputSystem.Update();

        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(performed, Has.Count.EqualTo(2));
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(started, Has.Count.Zero);
        Assert.That(performed, Has.Count.Zero);
        Assert.That(cancelled, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_RemovingDeviceWillUpdateControlsOnAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Contains.Item(gamepad.leftStick));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAction()
    {
        InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");

        action.Enable();
        action.Disable();

        Assert.That(InputSystem.FindAllEnabledActions(), Has.Exactly(0).SameAs(action));
        Assert.That(() => action.controls, Throws.InvalidOperationException);
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));
        Assert.That(action.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSingleDeviceWithMultipleActions()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/leftStick");
        var action3 = new InputAction(binding: "/gamepad/rightStick");

        var action1Performed = 0;
        var action2Performed = 0;
        var action3Performed = 0;

        action1.performed += _ => ++ action1Performed;
        action2.performed += _ => ++ action2Performed;
        action3.performed += _ => ++ action3Performed;

        action1.Enable();
        action2.Enable();
        action3.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one, rightStick = Vector2.one});
        InputSystem.Update();

        Assert.That(action1Performed, Is.EqualTo(1));
        Assert.That(action2Performed, Is.EqualTo(1));
        Assert.That(action3Performed, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_ButtonTriggersActionOnlyAfterCrossingPressThreshold()
    {
        // Axis controls trigger for every value change whereas buttons only trigger
        // when crossing the press threshold.

        //should this depend on the modifiers being used?
        Assert.Fail();
    }

#if UNITY_EDITOR
    [Test]
    [Category("Editor")]
    public void Editor_CanSaveAndRestoreState()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterTemplate(json);
        InputSystem.AddDevice("MyDevice");

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("template").EqualTo("MyDevice").And.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillCleanUpEventHooks()
    {
        InputSystem.Save();

        var receivedOnEvent = 0;
        var receivedOnDeviceChange = 0;

        InputSystem.onEvent += _ => ++ receivedOnEvent;
        InputSystem.onDeviceChange += (c, d) => ++ receivedOnDeviceChange;

        InputSystem.Restore();

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedOnEvent, Is.Zero);
        Assert.That(receivedOnDeviceChange, Is.Zero);
    }

    // Editor updates are confusing in that they denote just another point in the
    // application loop where we push out events. They do not mean that the events
    // we send necessarily go to the editor state buffers.
    [Test]
    [Category("Editor")]
    public void Editor_WhenPlaying_EditorUpdatesWriteEventIntoPlayerState()
    {
        InputConfiguration.LockInputToGame = true;

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
        InputSystem.Update(InputUpdateType.Dynamic);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Editor);

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.previous, Is.EqualTo(0.25).Within(0.000001));
    }

    ////TODO: the following tests have to be edit mode tests but it looks like putting them into
    ////      Assembly-CSharp-Editor is the only way to mark them as such

    ////REVIEW: support actions in the editor at all?
    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_ActionSetUpInEditor_DoesNotTriggerInPlayMode()
    {
        throw new NotImplementedException();
    }

    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_PlayerActionDoesNotTriggerWhenGameViewIsNotFocused()
    {
        throw new NotImplementedException();
    }

#endif

    ////TODO:-----------------------------------------------------------------

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_AddingANewDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_CanDisableFixedUpdates()
    {
        //make sure it reduces memory usage
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanSwitchTemplateOfExistingDevice()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanLoadTemplateFromCSV()
    {
        //take device info from name and the csv data is just a flat list of controls
        Assert.Fail();
    }

    ////TODO: This test doesn't yet make sense. The thought of how the feature should work is
    ////      correct, but the setup makes no sense and doesn't work. Gamepad adds deadzones
    ////      on the *sticks* so modifying that requires a Vector2 type processor which invert
    ////      isn't.
    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanMoveProcessorFromBaseTemplateInProcessorStack()
    {
        // The base gamepad template is adding deadzone processors to sticks. However, a
        // template based on that one may want to add processors *before* deadzoning is
        // applied. It can do so very simply by listing where the deadzone processor should
        // occur.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""processors"" : ""invert,deadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var setup = new InputControlSetup("MyDevice");
        var leftStickX = setup.GetControl<AxisControl>("leftStick/x");

        Assert.That(leftStickX.processors, Has.Length.EqualTo(2));
        Assert.That(leftStickX.processors[0], Is.TypeOf<InvertProcessor>());
        Assert.That(leftStickX.processors[1], Is.TypeOf<DeadzoneProcessor>());
    }

    [Test]
    [Category("Templates")]
    public void TODO_Template_CustomizedStateLayoutWillNotUseFormatCodeFromBaseTemplate()
    {
        //make sure that if you customize a gamepad layout, you don't end up with the "GPAD" format on the device
        //in fact, the system should require a format code to be specified in that case
        Assert.Fail();
    }

    ////REVIEW: This one seems like it adds quite a bit of complexity for somewhat minor gain.
    ////        May even be safer to *not* support this as it may inject controls at offsets where you don't expect them.
    struct BaseInputState : IInputStateTypeInfo
    {
        [InputControl(template = "Axis")] public float axis;
        public int padding;
        public FourCC GetFormat()
        {
            return new FourCC("BASE");
        }
    }
    [InputState(typeof(BaseInputState))]
    class BaseInputDevice : InputDevice
    {
    }
    //[InputControl(name = "axis", offset = InputStateBlock.kInvalidOffset)]
    struct DerivedInputState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC("DERI");
        }
    }
    [InputState(typeof(DerivedInputState))]
    class DerivedInputDevice : InputDevice
    {
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_InputStateInDerivedClassMergesWithControlsOfInputStateFromBaseClass()
    {
        //axis should appear in DerivedInputDevice and should have been moved to offset 8 (from automatic assignment)
        Assert.Fail();
    }
}
