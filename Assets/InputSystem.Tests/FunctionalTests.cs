#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISX;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestTools;
using UnityEngineInternal.Input;
using ISX.LowLevel;

#if UNITY_EDITOR
using ISX.Editor;
using UnityEditor;
#endif

#if !NET_4_0
using ISX.Net35Compatibility;
#endif

// These tests rely on the default template setup present in the code
// of the system (e.g. they make assumptions about Gamepad is set up).
public class FunctionalTests : InputTestFixture
{
    // The test categories give the feature area associated with the test:
    //
    //     a) Controls
    //     b) Templates
    //     c) Devices
    //     d) State
    //     e) Events
    //     f) Actions
    //     g) Editor
    //     h) Remote
    //     i) Plugins

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
    public void Templates_CannotUseControlTemplateAsToplevelTemplate()
    {
        Assert.That(() => new InputControlSetup("Button"), Throws.InvalidOperationException);
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

        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo(new InternedString("square")));
        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo(new InternedString("x")));
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
    [Category("Template")]
    public void Templates_CanOverrideTemplateMatchesForDiscoveredDevices()
    {
        InputSystem.onFindTemplateForDevice +=
            (description, templateMatch) => "Keyboard";

        var device = InputSystem.AddDevice(new InputDeviceDescription {deviceClass = "Gamepad"});

        Assert.That(device, Is.TypeOf<Keyboard>());
    }

    // If a template only specifies an interface in its descriptor, it is considered
    // a fallback for when there is no more specific template that is able to match
    // by product.
    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanHaveTemplateFallbackForInterface()
    {
        const string fallbackJson = @"
            {
                ""name"" : ""FallbackTemplate"",
                ""device"" : {
                    ""interface"" : ""MyInterface""
                }
            }
        ";
        const string productJson = @"
            {
                ""name"" : ""ProductTemplate"",
                ""device"" : {
                    ""interface"" : ""MyInterface"",
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterTemplate(fallbackJson);
        InputSystem.RegisterTemplate(productJson);

        Assert.Fail();
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_WhenTwoTemplatesConflict_LastOneRegisteredWins()
    {
        const string firstTemplate = @"
            {
                ""name"" : ""FirstTemplate"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";
        const string secondTemplate = @"
            {
                ""name"" : ""SecondTemplate"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterTemplate(firstTemplate);
        InputSystem.RegisterTemplate(secondTemplate);

        var template = InputSystem.TryFindMatchingTemplate(new InputDeviceDescription {product = "MyProduct"});

        Assert.That(template, Is.EqualTo("SecondTemplate"));
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
    public void Templates_ReplacingDeviceTemplateWithTemplateUsingDifferentType_PreservesDeviceIdAndDescription()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : { ""product"" : ""Test"" }
            }
        ";

        InputSystem.RegisterTemplate(initialJson);
        InputSystem.ReportAvailableDevice(new InputDeviceDescription {product = "Test"});
        var oldDevice = InputSystem.devices.First(x => x.template == "MyDevice");

        var oldDeviceId = oldDevice.id;
        var oldDeviceDescription = oldDevice.description;

        const string newJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Keyboard""
            }
        ";

        InputSystem.RegisterTemplate(newJson);
        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("template").EqualTo("MyDevice"));

        var newDevice = InputSystem.devices.First(x => x.template == "MyDevice");

        Assert.That(newDevice.id, Is.EqualTo(oldDeviceId));
        Assert.That(newDevice.description, Is.EqualTo(oldDeviceDescription));
    }

    private class MyButtonControl : ButtonControl
    {
    }

    [Test]
    [Category("Templates")]
    public void Templates_ReplacingControlTemplateAffectsAllDevicesUsingTemplate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Replace "Button" template.
        InputSystem.RegisterTemplate<MyButtonControl>("Button");

        Assert.That(gamepad.leftTrigger, Is.TypeOf<MyButtonControl>());
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
    public void TODO_Templates_WhenModifyingChildControlsByPath_DependentControlsUsingStateFromAreUpdatedAsWell()
    {
        const string baseJson = @"
            {
                ""name"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick"", ""template"" : ""Stick"" }
                ]
            }
        ";
        // When modifying the state layout of the X and Y controls of a stick from outside,
        // we also expect the up/down/left/right controls to get updated with the new state
        // layout.
        const string derivedJson = @"
            {
                ""name"" : ""Derived"",
                ""extend"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""stick/y"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterTemplate(baseJson);
        InputSystem.RegisterTemplate(derivedJson);

        var setup = new InputControlSetup("Derived");
        var stick = setup.GetControl<StickControl>("stick");

        Assert.That(stick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanBuildTemplatesInCode()
    {
        var builder = new InputTemplate.Builder();

        builder.name = "MyTemplate";
        builder.type = typeof(Gamepad);

        var template = builder.Build();

        Assert.That(template.name, Is.EqualTo(new InternedString("MyTemplate")));
        Assert.That(template.type, Is.SameAs(typeof(Gamepad)));
    }

    [Serializable]
    private class MyTemplateConstructor
    {
        public InputTemplate template = new InputTemplate.Builder().WithName("MyTemplate").Build();
        public InputTemplate DoIt()
        {
            return template;
        }
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanAddCustomTemplateConstructor()
    {
        var constructor = new MyTemplateConstructor();

        InputSystem.RegisterTemplateConstructor(() => constructor.DoIt(), "MyTemplate");

        var result = InputSystem.TryLoadTemplate("MyTemplate");

        Assert.That(result, Is.SameAs(constructor.template));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanTurnTemplateIntoJson()
    {
        var template = InputSystem.TryLoadTemplate("Gamepad");
        var json = template.ToJson();
        var deserializedTemplate = InputTemplate.FromJson(json);

        Assert.That(deserializedTemplate.name, Is.EqualTo(template.name));
        Assert.That(deserializedTemplate.controls, Has.Count.EqualTo(template.controls.Count));
        Assert.That(deserializedTemplate.stateFormat, Is.EqualTo(template.stateFormat));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanGetControlTemplateFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        // Control template mentioned explicitly.
        Assert.That(InputControlPath.TryGetControlTemplate("*/<button>"), Is.EqualTo("button")); // Does not "correct" casing.
        // Control template can be looked up from device template.
        Assert.That(InputControlPath.TryGetControlTemplate("/<gamepad>/leftStick"), Is.EqualTo("Stick"));
        // With multiple controls, only returns result if all controls use the same template.
        Assert.That(InputControlPath.TryGetControlTemplate("/<gamepad>/*Stick"), Is.EqualTo("Stick"));
        // Except if we match all controls on the device in which case it's taken to mean "any template goes".
        Assert.That(InputControlPath.TryGetControlTemplate("/<gamepad>/*"), Is.EqualTo("*"));
        ////TODO
        // However, having a wildcard on the device path is taken to mean "all device templates" in this case.
        //Assert.That(InputControlPath.TryGetControlTemplate("/*/*Stick"), Is.EqualTo("Stick"));
        // Can determine template used by child control.
        Assert.That(InputControlPath.TryGetControlTemplate("<gamepad>/leftStick/x"), Is.EqualTo("Axis"));
        // Can determine template from control with usage.
        Assert.That(InputControlPath.TryGetControlTemplate("<gamepad>/{PrimaryAction}"), Is.EqualTo("Button"));
        // Will not look up from instanced devices at runtime so can't know device template from this path.
        Assert.That(InputControlPath.TryGetControlTemplate("/gamepad/leftStick"), Is.Null);
        // If only a device template is given, can't know control template.
        Assert.That(InputControlPath.TryGetControlTemplate("/<gamepad>"), Is.Null);

        ////TODO: make sure we can find templates from control template modifying child paths
        ////TODO: make sure that finding by usage can look arbitrarily deep into the hierarchy
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanGetDeviceTemplateFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        Assert.That(InputControlPath.TryGetDeviceTemplate("<gamepad>/leftStick"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceTemplate("/<gamepad>"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceTemplate("/*/*Stick"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceTemplate("/*"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceTemplate("/gamepad/leftStick"), Is.Null);
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanLoadTemplate()
    {
        var json = @"
            {
                ""name"" : ""MyTemplate"",
                ""controls"" : [ { ""name"" : ""MyControl"" } ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var jsonTemplate = InputSystem.TryLoadTemplate("MyTemplate");

        Assert.That(jsonTemplate, Is.Not.Null);
        Assert.That(jsonTemplate.name, Is.EqualTo(new InternedString("MyTemplate")));
        Assert.That(jsonTemplate.controls, Has.Count.EqualTo(1));
        Assert.That(jsonTemplate.controls[0].name, Is.EqualTo(new InternedString("MyControl")));

        var gamepadTemplate = InputSystem.TryLoadTemplate("Gamepad");

        Assert.That(gamepadTemplate, Is.Not.Null);
        Assert.That(gamepadTemplate.name, Is.EqualTo(new InternedString("Gamepad")));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanRemoveTemplate()
    {
        var json = @"
            {
                ""name"" : ""MyTemplate"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = InputSystem.AddDevice("MyTemplate");

        Assert.That(InputSystem.ListTemplates(), Has.Exactly(1).EqualTo("MyTemplate"));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(device));

        InputSystem.RemoveTemplate("MyTemplate");

        Assert.That(InputSystem.ListTemplates(), Has.None.EqualTo("MyTemplate"));
        Assert.That(InputSystem.devices, Has.None.SameAs(device));
        Assert.That(InputSystem.devices, Has.None.With.Property("template").EqualTo("MyTemplate"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_ChangingTemplates_SendsNotifications()
    {
        InputTemplateChange? receivedChange = null;
        string receivedTemplate = null;

        InputSystem.onTemplateChange +=
            (template, change) =>
            {
                receivedChange = change;
                receivedTemplate = template;
            };

        const string jsonV1 = @"
            {
                ""name"" : ""MyTemplate"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add template.
        InputSystem.RegisterTemplate(jsonV1);

        Assert.That(receivedChange, Is.EqualTo(InputTemplateChange.Added));
        Assert.That(receivedTemplate, Is.EqualTo("MyTemplate"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyTemplate"",
                ""extend"" : ""Keyboard""
            }
        ";

        receivedChange = null;
        receivedTemplate = null;

        // Change template.
        InputSystem.RegisterTemplate(jsonV2);

        Assert.That(receivedChange, Is.EqualTo(InputTemplateChange.Replaced));
        Assert.That(receivedTemplate, Is.EqualTo("MyTemplate"));

        receivedChange = null;
        receivedTemplate = null;

        // RemoveTemplate.
        InputSystem.RemoveTemplate("MyTemplate");

        Assert.That(receivedChange, Is.EqualTo(InputTemplateChange.Removed));
        Assert.That(receivedTemplate, Is.EqualTo("MyTemplate"));
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_RemovingTemplates_RemovesAllTemplatesBasedOnIt()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanQueryImageAndDisplayNameFromControl()
    {
        var json = @"
            {
                ""name"" : ""MyTemplate"",
                ""controls"" : [ { ""name"" : ""MyControl"",  } ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        Assert.Fail();
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanConstructTemplateFromHIDDescriptor()
    {
        var descriptor = @"
        ";

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

    ////TODO: make the same kind of functionality work for aliases
    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
        var controller = InputSystem.AddDevice("XRController");

        Assert.That(controller.usages, Has.Count.EqualTo(0));

        InputSystem.SetUsage(controller, CommonUsages.LeftHand);

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(XRController.leftHand, Is.SameAs(controller));

        InputSystem.SetUsage(controller, CommonUsages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(CommonUsages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        Assert.That(XRController.rightHand, Is.SameAs(controller));
        Assert.That(XRController.leftHand, Is.Not.SameAs(controller));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingUsageOfDevice_SendsDeviceChangeNotification()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                receivedDevice = d;
                receivedDeviceChange = c;
            };

        InputSystem.SetUsage(device, CommonUsages.LeftHand);

        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.UsageChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsage()
    {
        InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("XRController");

        var controller = InputSystem.AddDevice("XRController");
        InputSystem.SetUsage(controller, CommonUsages.LeftHand);

        var controls = InputSystem.GetControls("/{LeftHand}");

        Assert.That(controls, Has.Count.EqualTo(1));
        Assert.That(controls, Has.Exactly(1).SameAs(controller));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsageAndTemplate()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        InputSystem.SetUsage(gamepad, CommonUsages.LeftHand);

        InputSystem.AddDevice("XRController");

        var controller = InputSystem.AddDevice("XRController");
        InputSystem.SetUsage(controller, CommonUsages.LeftHand);

        var controls = InputSystem.GetControls("/<XRController>{LeftHand}");

        Assert.That(controls, Has.Count.EqualTo(1));
        Assert.That(controls, Has.Exactly(1).SameAs(controller));
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
    public void TODO_Controls_MotorsCanWriteToState()
    {
        /*
        var gamepad = (Gamepad) InputSystem.AddDevice("Gamepad");

        gamepad.leftMotor.value = 0.5f;

        InputSystem.QueueOutputEvent(...);

        InputSystem.Update();
        */

        Assert.Fail();
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
    public void Devices_DevicesGetNameFromBaseTemplate()
    {
        var json = @"
            { ""name"" : ""MyDevice"",
              ""extend"" : ""Gamepad"" }
        ";

        InputSystem.RegisterTemplate(json);

        var setup = new InputControlSetup("MyDevice");
        var device = setup.Finish();

        Assert.That(device.name, Is.EqualTo("Gamepad"));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.stateBlock.sizeInBits, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>() * 8));
        Assert.That(gamepad.leftStick.stateBlock.byteOffset, Is.EqualTo(UnsafeUtility.OffsetOf<GamepadState>("leftStick")));
        Assert.That(gamepad.dpad.stateBlock.byteOffset, Is.EqualTo(UnsafeUtility.OffsetOf<GamepadState>("buttons")));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        var setup = new InputControlSetup("Gamepad");
        var rightMotor = setup.GetControl("rightMotor");
        setup.Finish();

        var outputOffset = UnsafeUtility.OffsetOf<GamepadState>("motors");
        var rightMotorOffset = outputOffset + UnsafeUtility.OffsetOf<GamepadOutputState>("rightMotor");

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

        var leftStickOffset = UnsafeUtility.OffsetOf<GamepadState>("leftStick");
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

        var leftStickOffset = UnsafeUtility.OffsetOf<GamepadState>("leftStick");
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

        var sizeofGamepadState = UnsafeUtility.SizeOf<GamepadState>();

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
    // (which requires knowledge of the specific layout used by the HID). By having flexible state
    // layouts we can do this entirely through data using just templates.
    //
    // A template that customizes state layout can also "park" unused controls outside the block of
    // data that will actually be sent in via state events. Space for the unused controls will still
    // be allocated in the state buffers (since InputControls still refer to it) but InputManager
    // is okay with sending StateEvents that are shorter than the full state block of a device.
    ////REVIEW: we might want to equip InputControls with the ability to be disabled (in which case they return default values)
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
        // Turn left stick into a 2D vector of shorts.
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

    // Using "offset = N" with an InputControlAttribute that doesn't specific a child path (or even then?)
    // should add the base offset of the field itself.
    [Test]
    [Category("State")]
    public void TODO_State_SpecifyingOffsetOnControlProperty_AddsBaseOffset()
    {
        Assert.Fail();
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

        Assert.That(gamepad.bButton.wasJustPressed, Is.False);
        Assert.That(gamepad.bButton.wasJustReleased, Is.False);

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.Update();

        Assert.That(gamepad.bButton.wasJustPressed, Is.True);
        Assert.That(gamepad.bButton.wasJustReleased, Is.False);

        var secondState = new GamepadState {buttons = 0};
        InputSystem.QueueStateEvent(gamepad, secondState);
        InputSystem.Update();

        Assert.That(gamepad.bButton.wasJustPressed, Is.False);
        Assert.That(gamepad.bButton.wasJustReleased, Is.True);
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
        Assert.That(gamepad.bButton.wasJustPressed, Is.False);
        Assert.That(gamepad.bButton.wasJustReleased, Is.False);
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

    ////REVIEW: don't do this; instead have event handlers hooked into onEvent and onUpdate perform the work
    // Controls like mouse deltas need to reset to zero when there is no activity on them in a frame.
    // This could be done by requiring the state producing code to always send appropriate state events
    // when necessary. However, for state producers that are hooked to event sources (like eg. NSEvents
    // on OSX and MSGs on Windows), this can be very awkward to handle as it requires synchronizing with
    // input updates and can complicate state producer logic quite a bit.
    //
    // So, instead of putting the burden on state producers, controls come with an auto-reset feature
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

        //if there is a state event for pointer device X, update it to accumulate deltas
        //before an update, reset the ... how? actions need to see the reset

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
    public void Devices_AddingDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        InputSystem.AddDevice("Keyboard");

        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.5).Within(0.0000001));
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
        var device = InputSystem.AddDevice("Gamepad", "test"); // Give name to make sure we're not matching by name.
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
    public void Devices_CanMatchTemplateByDeviceClass()
    {
        InputSystem.ReportAvailableDevice(new InputDeviceDescription {deviceClass = "Touchscreen"});

        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Touchscreen>());

        // Should not try to use a control template.
        InputSystem.ReportAvailableDevice(new InputDeviceDescription {deviceClass = "Touch"});

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
    }

    // For some devices, we need to discover their setup at runtime and cannot create templates
    // in advance. HID joysticks are one such case. We want to be able to turn any HID joystick
    // into a Joystick device and accurately represent all the axes and buttons the device
    // actually has. If we couldn't make up templates on the fly, we would have to have a fallback
    // joystick template that simply has N buttons and M axes.
    //
    // So, instead we have a callback that tells us when a device has been discovered. We can use
    // this callback to generate a template on the fly.
    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanDetermineWhichTemplateIsChosenOnDeviceDiscovery()
    {
        Assert.Fail();
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
    public void Devices_NativeDevicesAreFlaggedAsSuch()
    {
        var description = new InputDeviceDescription {deviceClass = "Gamepad"};
        var deviceId = NativeInputSystem.ReportNewInputDevice(description.ToJson());

        InputSystem.Update();

        var device = InputSystem.TryGetDeviceById(deviceId);

        Assert.That(device, Is.Not.Null);
        Assert.That(device.native, Is.True);
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
    public void Devices_CanCreateGenericJoystick()
    {
        var json = @"
            {
                ""name"" : ""MyJoystick"",
                ""extend"" : ""Joystick"",
                ""controls"" : [
                    { ""name"" : ""button1"", ""template"" : ""Button"" },
                    { ""name"" : ""button2"", ""template"" : ""Button"" },
                    { ""name"" : ""axis1"", ""template"" : ""Axis"" },
                    { ""name"" : ""axis2"", ""template"" : ""Axis"" },
                    { ""name"" : ""discrete"", ""template"" : ""Digital"" }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var device = InputSystem.AddDevice("MyJoystick");

        Assert.That(device, Is.TypeOf<Joystick>());
        Assert.That(Joystick.current, Is.SameAs(device));

        var joystick = (Joystick)device;

        Assert.That(joystick.axes, Has.Count.EqualTo(4)); // Includes stick.
        Assert.That(joystick.buttons, Has.Count.EqualTo(3)); // Includes trigger.
        Assert.That(joystick.trigger.name, Is.EqualTo("trigger"));
        Assert.That(joystick.stick.name, Is.EqualTo("stick"));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanQueryKeyCodeInformationFromKeyboard()
    {
        //set up callback equivalent to what native does to query per-key control data
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
    [TestCase("Joystick")]
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

        Assert.That(InputSystem.GetControls(string.Format("/<{0}>", baseTemplate)), Has.Exactly(1).SameAs(device));
        Assert.That(device.name, Is.EqualTo(baseTemplate));
        Assert.That(device.description.manufacturer, Is.EqualTo(manufacturer));
        Assert.That(device.description.interfaceName, Is.EqualTo(interfaceName));
        Assert.That(device.description.product, Is.EqualTo(product));
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
    public void Devices_CanGetTextInputFromKeyboard()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        var textReceived = "";
        keyboard.onTextInput += ch => textReceived += ch;

        InputSystem.QueueTextEvent(keyboard, 'a');
        InputSystem.QueueTextEvent(keyboard, 'b');
        InputSystem.QueueTextEvent(keyboard, 'c');

        InputSystem.Update();

        Assert.That(textReceived, Is.EqualTo("abc"));
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
                Assert.That(inputEvent.deviceId, Is.EqualTo(device.id));
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
                    (ulong)UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.id));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState), StateEvent.From(events[1])->state,
                    (ulong)UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
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
    public void Devices_RemovingDeviceCleansUpUpdateCallback()
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
    public void Actions_CanTargetSingleControl()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetMultipleControls()
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

        Assert.That(action.set, Is.Null);
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

        Assert.That(action.set, Is.Null);
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
        var action = new InputAction(binding: "/<gamepad>/<button>", modifiers: "press");

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
    public void Actions_CanAddActionsToSet()
    {
        var set = new InputActionSet();

        set.AddAction("action1");
        set.AddAction("action2");

        Assert.That(set.actions, Has.Count.EqualTo(2));
        Assert.That(set.actions[0], Has.Property("name").EqualTo("action1"));
        Assert.That(set.actions[1], Has.Property("name").EqualTo("action2"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddBindingsToActionsInSet()
    {
        var set = new InputActionSet();

        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        action1.AddBinding("/gamepad/leftStick");
        action2.AddBinding("/gamepad/rightStick");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action2.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddUnnamedActionToSet()
    {
        var set = new InputActionSet();
        Assert.That(() => set.AddAction(""), Throws.ArgumentException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddTwoActionsWithTheSameNameToSet()
    {
        var set = new InputActionSet();
        set.AddAction("action");

        Assert.That(() => set.AddAction("action"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionInSet()
    {
        var set = new InputActionSet();

        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        Assert.That(set.TryGetAction("action1"), Is.SameAs(action1));
        Assert.That(set.TryGetAction("action2"), Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertActionSetToAndFromJson()
    {
        var set = new InputActionSet("test");

        set.AddAction(name: "action1", binding: "/gamepad/leftStick").AddBinding("/gamepad/rightStick", groups: "group");
        set.AddAction(name: "action2", binding: "/gamepad/buttonSouth", modifiers: "tap,slowTap(duration=0.1)");

        var json = set.ToJson();
        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(1));
        Assert.That(sets[0], Has.Property("name").EqualTo("test"));
        Assert.That(sets[0].actions, Has.Count.EqualTo(2));
        Assert.That(sets[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(sets[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(sets[0].actions[0].bindings, Has.Count.EqualTo(2));
        Assert.That(sets[0].actions[1].bindings, Has.Count.EqualTo(1));
        Assert.That(sets[0].actions[0].bindings[0].group, Is.Null);
        Assert.That(sets[0].actions[0].bindings[1].group, Is.EqualTo("group"));
        Assert.That(sets[0].actions[0].bindings[0].modifiers, Is.Null);
        Assert.That(sets[0].actions[0].bindings[1].modifiers, Is.Null);
        Assert.That(sets[0].actions[1].bindings[0].group, Is.Null);
        Assert.That(sets[0].actions[1].bindings[0].modifiers, Is.EqualTo("tap,slowTap(duration=0.1)"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionSetJsonCanBeEmpty()
    {
        var sets = InputActionSet.FromJson("{}");
        Assert.That(sets, Is.Not.Null);
        Assert.That(sets, Has.Length.EqualTo(0));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertMultipleActionSetsToAndFromJson()
    {
        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");

        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set2.AddAction(name: "action2", binding: "/gamepad/rightStick");

        var json = InputActionSet.ToJson(new[] {set1, set2});
        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(2));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("set1"));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("set2"));
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
    public void Actions_CanSerializeActionSet()
    {
        var set = new InputActionSet("set");

        set.AddAction("action1", binding: "/gamepad/leftStick");
        set.AddAction("action2", binding: "/gamepad/rightStick");

        var json = JsonUtility.ToJson(set);
        var deserializedSet = JsonUtility.FromJson<InputActionSet>(json);

        Assert.That(deserializedSet.name, Is.EqualTo("set"));
        Assert.That(deserializedSet.actions, Has.Count.EqualTo(2));
        Assert.That(deserializedSet.actions[0].name, Is.EqualTo("action1"));
        Assert.That(deserializedSet.actions[1].name, Is.EqualTo("action2"));
        Assert.That(deserializedSet.actions[0].bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(deserializedSet.actions[1].bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
        Assert.That(deserializedSet.actions[0].set, Is.SameAs(deserializedSet));
        Assert.That(deserializedSet.actions[1].set, Is.SameAs(deserializedSet));
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

        var state = new GamepadState { leftStick = new Vector2(0.5f, 0.5f)};
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.leftStick));

        performedReceivedCalls = 0;

        state.rightStick = new Vector2(0.5f, 0.5f);
        InputSystem.QueueStateEvent(gamepad, state);
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

    ////REVIEW: what's the bahavior we want here?
    /*
    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenDeviceIsDisconnectedAndReconnected()
    {
        var gamepad = (Gamepad) InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.Enable();

        InputSystem.QueueDisconnectEvent(gamepad);
        InputSystem.Update();

        Assert.That(action.controls, Has.Count.Zero);

        InputSystem.QueueConnectEvent(gamepad);
        InputSystem.Update();

        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));
    }

    [Test]
    [Category("Actions")]
    public void Actions_DoNotBindToDisconnectedDevices()
    {
        Assert.Fail();
    }
    */

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

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

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
    public void TODO_Actions_CanChainBindings()
    {
        // Set up an action that requires the left trigger to be held when pressing the A button.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
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
    public void TODO_Actions_ChainedBindingsTriggerIfControlsActivateAtSameTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_ChainedBindingsDoNotTriggerIfControlsActivateInWrongOrder()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
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
    [Category("Actions")]
    public void TODO_Actions_CanChainBindingsWithModifiers()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        // Tap or slow tap on A button when left trigger is held.
        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth", modifiers: "tap,slowTap");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 0}, InputConfiguration.SlowTapTime + 0.1);
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<SlowTapModifier>());
    }

    ////REVIEW: don't think this one makes sense to have
    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanPerformContinuousAction()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick", modifiers: "continuous");
        action.Enable();

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

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

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryStartAndPerformTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftTrigger", modifiers: "slowTap");
        action.Enable();

        var receivedStartTime = 0.0;
        var receivedTime = 0.0;

        action.performed +=
            ctx =>
            {
                receivedStartTime = ctx.startTime;
                receivedTime = ctx.time;
            };

        var startTime = 0.123;
        var endTime = 0.123 + InputConfiguration.SlowTapTime + 1.0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f}, startTime);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.0f}, endTime);
        InputSystem.Update();

        Assert.That(receivedStartTime, Is.EqualTo(startTime).Within(0.000001));
        Assert.That(receivedTime, Is.EqualTo(endTime).Within(0.000001));
    }

    // Make sure that if we target "*/{ActionAction}", for example, and the gamepad's A button
    // goes down and starts the action, then whatever happens with the mouse's left button
    // shouldn't matter until the gamepad's A button comes back up.
    [Test]
    [Category("Actions")]
    public void TODO_Actions_StartingOfActionCapturesControl()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddSetsToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");

        asset.AddActionSet(set1);
        asset.AddActionSet(set2);

        Assert.That(asset.actionSets, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets, Has.Exactly(1).SameAs(set1));
        Assert.That(asset.actionSets, Has.Exactly(1).SameAs(set2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_SetsInAssetMustHaveName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var set = new InputActionSet();

        Assert.That(() => asset.AddActionSet(set), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_SetsInAssetsMustHaveUniqueNames()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var set1 = new InputActionSet("same");
        var set2 = new InputActionSet("same");

        asset.AddActionSet(set1);
        Assert.That(() => asset.AddActionSet(set2), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpSetInAssetByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var set = new InputActionSet("test");
        asset.AddActionSet(set);

        Assert.That(asset.TryGetActionSet("test"), Is.SameAs(set));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveActionSetFromAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(new InputActionSet("test"));
        asset.RemoveActionSet("test");

        Assert.That(asset.actionSets, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryLastTrigger()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/rightTrigger", modifiers: "slowTap(duration=1)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1}, 2);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerModifier, Is.TypeOf<SlowTapModifier>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0}, 4);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(4).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerModifier, Is.TypeOf<SlowTapModifier>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindings()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.Enable();

        var wasPerformed = false;
        action.performed += ctx => wasPerformed = true;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1});
        InputSystem.Update();

        Assert.That(wasPerformed);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhileActionIsEnabled_CannotApplyOverrides()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.Enable();

        Assert.That(() => action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"}),
            Throws.InvalidOperationException);
    }

    // If there's multiple bindings on an action, we don't readily know which binding to apply
    // an override to. We use groups to disambiguate the case.
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_OverridingRequiresGroups()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "b");

        Assert.That(() => action.ApplyBindingOverride("/gamepad/buttonSouth"), Throws.InvalidOperationException);

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "b");

        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    // We don't do anything smart when groups are ambiguous. It's a perfectly valid case to have
    // multiple bindings in the same group but when you try to override and only give a group,
    // only the first binding that uses the group is affected.
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_IfGroupIsAmbiguous_OverridesOnlyFirst()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "a");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[1].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindingsWithSameGroup_CanTargetIndividualBindingsByIndex()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "a[1]");

        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultsAfterOverridingBinding()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.RemoveAllBindingOverrides();

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ApplyingNullOrEmptyOverride_IsSameAsRemovingOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");

        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.ApplyBindingOverride(new InputBindingOverride());
        Assert.That(action.bindings[0].overridePath, Is.Null);

        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.ApplyBindingOverride(new InputBindingOverride { binding = "" });
        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveOverrides()
    {
        var action = new InputAction(name: "foo");
        action.Enable();
        Assert.That(() => action.RemoveAllBindingOverrides(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultForSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBindingOverride {binding = "/gamepad/rightTrigger"};

        action.ApplyBindingOverride(bindingOverride);
        action.RemoveBindingOverride(bindingOverride);

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBindingOverride {binding = "/gamepad/rightTrigger"};
        action.ApplyBindingOverride(bindingOverride);
        action.Enable();
        Assert.That(() => action.RemoveBindingOverride(bindingOverride), Throws.InvalidOperationException);
    }

    // The following functionality is meant in a way where you have a base action set that
    // you then clone multiple times and put overrides on each of the clones to associate them
    // with specific devices.
    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices()
    {
        // Action that matches leftStick on *any* gamepad in the system.
        var action = new InputAction(binding: "/<gamepad>/leftStick");
        action.AddBinding("/keyboard/enter"); // Add unrelated binding which should not be touched.

        InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Add overrides to make bindings specific to #2 gamepad.
        var numOverrides = action.ApplyOverridesUsingMatchingControls(gamepad2);
        action.Enable();

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad2.leftStick));
        Assert.That(action.bindings[0].overridePath, Is.EqualTo(gamepad2.leftStick.path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices_OnActionsInSet()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        var action2 = set.AddAction("action2", "/<gamepad>/buttonSouth");
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var numOverrides = set.ApplyOverridesUsingMatchingControls(gamepad);

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo(gamepad.aButton.path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableEntireSet()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        set.Enable();

        Assert.That(set.enabled);
        Assert.That(action1.enabled);
        Assert.That(action2.enabled);

        set.Disable();

        Assert.That(set.enabled, Is.False);
        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneAction()
    {
        var action = new InputAction(name: "action");
        action.AddBinding("/gamepad/leftStick", modifiers: "tap", groups: "group");
        action.AddBinding("/gamepad/rightStick");

        var clone = action.Clone();

        Assert.That(clone, Is.Not.SameAs(action));
        Assert.That(clone.name, Is.EqualTo(action.name));
        Assert.That(clone.bindings, Has.Count.EqualTo(action.bindings.Count));
        Assert.That(clone.bindings[0].path, Is.EqualTo(action.bindings[0].path));
        Assert.That(clone.bindings[0].modifiers, Is.EqualTo(action.bindings[0].modifiers));
        Assert.That(clone.bindings[0].group, Is.EqualTo(action.bindings[0].group));
        Assert.That(clone.bindings[1].path, Is.EqualTo(action.bindings[1].path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningActionFromSet_ProducesSingletonAction()
    {
        var set = new InputActionSet("set");
        var action = set.AddAction("action1");

        var clone = action.Clone();

        Assert.That(clone.set, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningEnabledAction_ProducesDisabledAction()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var clone = action.Clone();

        Assert.That(clone.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionSets()
    {
        var set = new InputActionSet("set");
        var action1 = set.AddAction("action1", binding: "/gamepad/leftStick", modifiers: "tap");
        var action2 = set.AddAction("action2", binding: "/gamepad/rightStick", modifiers: "tap");

        var clone = set.Clone();

        Assert.That(clone, Is.Not.SameAs(set));
        Assert.That(clone.name, Is.EqualTo(set.name));
        Assert.That(clone.actions, Has.Count.EqualTo(set.actions.Count));
        Assert.That(clone.actions, Has.None.SameAs(action1));
        Assert.That(clone.actions, Has.None.SameAs(action2));
        Assert.That(clone.actions[0].name, Is.EqualTo(set.actions[0].name));
        Assert.That(clone.actions[1].name, Is.EqualTo(set.actions[1].name));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionAssets()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "Asset";
        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");
        asset.AddActionSet(set1);
        asset.AddActionSet(set2);

        var clone = asset.Clone();

        Assert.That(clone, Is.Not.SameAs(asset));
        Assert.That(clone.GetInstanceID(), Is.Not.EqualTo(asset.GetInstanceID()));
        Assert.That(clone.actionSets, Has.Count.EqualTo(2));
        Assert.That(clone.actionSets, Has.None.SameAs(set1));
        Assert.That(clone.actionSets, Has.None.SameAs(set2));
        Assert.That(clone.actionSets[0].name, Is.EqualTo("set1"));
        Assert.That(clone.actionSets[1].name, Is.EqualTo("set2"));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanRebindFromUserInput()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        var gamepad = InputSystem.AddDevice("Gamepad");

        using (var rebind = InputActionRebinding.PerformUserRebind(action))
        {
        }

        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanResolveActionReference()
    {
        var set = new InputActionSet("set");
        set.AddAction("action1");
        var action2 = set.AddAction("action2");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "set", "action2");

        var referencedAction = reference.action;

        Assert.That(referencedAction, Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAllEnabledActionsInOneGo()
    {
        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/rightStick");
        var set = new InputActionSet();
        var action3 = set.AddAction("action", "/gamepad/buttonSouth");

        action1.Enable();
        action2.Enable();
        set.Enable();

        InputSystem.DisableAllEnabledActions();

        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
        Assert.That(action3.enabled, Is.False);
        Assert.That(set.enabled, Is.False);
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanConnectTwoInputSystemsOverNetwork()
    {
        // Add some data to the local input system.
        InputSystem.AddDevice("Gamepad");
        InputSystem.RegisterTemplate(@"{ ""name"" : ""MyGamepad"", ""extend"" : ""Gamepad"" }");
        var localGamepad = (Gamepad)InputSystem.AddDevice("MyGamepad");

        // Now create another input system instance and connect it
        // to our "local" instance.
        // NOTE: This relies on internal APIs. We want remoting as such to be available
        //       entirely from user land but having multiple input systems in the same
        //       application isn't something that we necessarily want to expose (we do
        //       have global state so it can easily lead to surprising results).
        // NOTE: This second system is *NOT* connected to NativeInputSystem. Running
        //       updates on it, for example, won't do anything.
        var secondInputSystem = new InputManager();
        secondInputSystem.InitializeData();

        var local = InputSystem.remoting;
        var remote = new InputRemoting(secondInputSystem);

        // We wire the two directly into each other effectively making function calls
        // our "network transport layer". In a real networking situation, we'd effectively
        // have an RPC-like mechanism sitting in-between.
        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        var remoteGamepadTemplate =
            string.Format("{0}0::{1}", InputRemoting.kRemoteTemplateNamespacePrefix, localGamepad.template);

        // Make sure that our "remote" system now has the data we initially
        // set up on the local system.
        Assert.That(secondInputSystem.devices,
            Has.Exactly(1).With.Property("template").EqualTo(remoteGamepadTemplate));
        Assert.That(secondInputSystem.devices, Has.Exactly(2).TypeOf<Gamepad>());
        Assert.That(secondInputSystem.devices, Has.All.With.Property("remote").True);

        // Send state event to local gamepad.
        InputSystem.QueueStateEvent(localGamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        // Have second system install its state buffers. Remember that state isn't stored
        // on the controls so querying controls on remoteGamepad for their values would read
        // state that is actually owned by the "real" local input system.
        secondInputSystem.m_StateBuffers.SwitchTo(InputUpdateType.Dynamic);

        var remoteGamepad = (Gamepad)secondInputSystem.devices.First(x => x.template == remoteGamepadTemplate);

        Assert.That(remoteGamepad.leftTrigger.value, Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingDevicesWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputSystem = new InputManager();
        secondInputSystem.InitializeData();

        var local = InputSystem.remoting;
        var remote = new InputRemoting(secondInputSystem);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        // Add device.
        var localGamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(secondInputSystem.devices, Has.Count.EqualTo(1));
        var remoteGamepad = secondInputSystem.devices[0];
        Assert.That(remoteGamepad, Is.TypeOf<Gamepad>());
        Assert.That(remoteGamepad.remote, Is.True);
        Assert.That(remoteGamepad.template, Contains.Substring("Gamepad"));

        // Change usage.
        InputSystem.SetUsage(localGamepad, CommonUsages.LeftHand);
        Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));

        // Bind and disconnect are events so no need to test those.

        // Remove device.
        InputSystem.RemoveDevice(localGamepad);
        Assert.That(secondInputSystem.devices, Has.Count.Zero);
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingTemplatesWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputSystem = new InputManager();
        secondInputSystem.InitializeData();

        var local = InputSystem.remoting;
        var remote = new InputRemoting(secondInputSystem);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        const string jsonV1 = @"
            {
                ""name"" : ""MyTemplate"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add template.
        InputSystem.RegisterTemplate(jsonV1);

        var template = secondInputSystem.TryLoadTemplate(new InternedString("remote0::MyTemplate"));
        Assert.That(template, Is.Not.Null);
        Assert.That(template.extendsTemplate, Is.EqualTo("Gamepad"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyTemplate"",
                ""extend"" : ""Keyboard""
            }
        ";

        // Change template.
        InputSystem.RegisterTemplate(jsonV2);

        template = secondInputSystem.TryLoadTemplate(new InternedString("remote0::MyTemplate"));
        Assert.That(template.extendsTemplate, Is.EqualTo("Keyboard"));

        // Remove template.
        InputSystem.RemoveTemplate("MyTemplate");

        Assert.That(secondInputSystem.TryLoadTemplate(new InternedString("remote0::MyTemplate")), Is.Null);
    }

    // If we have more than two players connected, for example, and we add a template from player A
    // to the system, we don't want to send the template to player B in turn. I.e. all data mirrored
    // from remotes should stay local.
    [Test]
    [Category("Remote")]
    public void TODO_Remote_WithMultipleRemotesConnected_DoesNotDuplicateDataFromOneRemoteToOtherRemotes()
    {
        Assert.Fail();
    }

    // PlayerConnection isn't connected in the editor and EditorConnection isn't connected
    // in players so we can't really test actual transport in just the application itself.
    // This will act as an IEditorPlayerConnection that immediately makes the FakePlayerConnection
    // on the other end receive messages.
    class FakePlayerConnection : IEditorPlayerConnection
    {
        public int playerId;
        // The fake connection acting as the socket on the opposite end of us.
        public FakePlayerConnection otherEnd;

        public void Register(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            MessageEvent msgEvent;
            if (!m_MessageListeners.TryGetValue(messageId, out msgEvent))
            {
                msgEvent = new MessageEvent();
                m_MessageListeners[messageId] = msgEvent;
            }

            msgEvent.AddListener(callback);
        }

        public void Unregister(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            m_MessageListeners[messageId].RemoveListener(callback);
        }

        public void DisconnectAll()
        {
            m_MessageListeners.Clear();
            m_ConnectionListeners.RemoveAllListeners();
            m_DisconnectionListeners.RemoveAllListeners();
        }

        public void RegisterConnection(UnityAction<int> callback)
        {
            m_ConnectionListeners.AddListener(callback);
        }

        public void RegisterDisconnection(UnityAction<int> callback)
        {
            m_DisconnectionListeners.AddListener(callback);
        }

        public void Receive(Guid messageId, byte[] data)
        {
            MessageEvent msgEvent;
            if (m_MessageListeners.TryGetValue(messageId, out msgEvent))
                msgEvent.Invoke(new MessageEventArgs {playerId = playerId, data = data});
        }

        public void Send(Guid messageId, byte[] data)
        {
            otherEnd.Receive(messageId, data);
        }

        private Dictionary<Guid, MessageEvent> m_MessageListeners = new Dictionary<Guid, MessageEvent>();
        private ConnectEvent m_ConnectionListeners = new ConnectEvent();
        private ConnectEvent m_DisconnectionListeners = new ConnectEvent();
        private class MessageEvent : UnityEvent<MessageEventArgs>
        {
        }
        private class ConnectEvent : UnityEvent<int>
        {
        }
    }

    public class RemoteTestObserver : IObserver<InputRemoting.Message>
    {
        public List<InputRemoting.Message> messages = new List<InputRemoting.Message>();

        public void OnNext(InputRemoting.Message msg)
        {
            messages.Add(msg);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanConnectInputSystemsOverEditorPlayerConnection()
    {
        var connectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
        var connectionToPlayer = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();

        connectionToEditor.name = "ConnectionToEditor";
        connectionToPlayer.name = "ConnectionToPlayer";

        var fakeEditorConnection = new FakePlayerConnection {playerId = 0};
        var fakePlayerConnection = new FakePlayerConnection {playerId = 1};

        fakeEditorConnection.otherEnd = fakePlayerConnection;
        fakePlayerConnection.otherEnd = fakeEditorConnection;

        var observer = new RemoteTestObserver();

        // In the Unity API, "PlayerConnection" is the connection to the editor
        // and "EditorConnection" is the connection to the player. Seems counter-intuitive.
        connectionToEditor.Bind(fakePlayerConnection, true);
        connectionToPlayer.Bind(fakeEditorConnection, true);

        // Bind the local remote on the player side.
        InputSystem.remoting.Subscribe(connectionToEditor);
        InputSystem.remoting.StartSending();

        connectionToPlayer.Subscribe(observer);

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();
        InputSystem.RemoveDevice(device);

        ////TODO: make sure that we also get the connection sequence right and send our initial templates and devices
        Assert.That(observer.messages, Has.Count.EqualTo(4));
        Assert.That(observer.messages[0].type, Is.EqualTo(InputRemoting.MessageType.Connect));
        Assert.That(observer.messages[1].type, Is.EqualTo(InputRemoting.MessageType.NewDevice));
        Assert.That(observer.messages[2].type, Is.EqualTo(InputRemoting.MessageType.NewEvents));
        Assert.That(observer.messages[3].type, Is.EqualTo(InputRemoting.MessageType.RemoveDevice));

        ////TODO: test disconnection
    }

    // This is nested but should still be found by the type scanning.
    [InputPlugin]
    public static class TestPlugin
    {
        public static bool s_Initialized;
        public static void Initialize()
        {
            s_Initialized = true;
        }
    }

    // The plugin system is designed to provide a sensible (though not necessarily desirable) default -- initialize
    // whatever we can find in the assemblies present in the system -- but to allow completely suppressing default
    // behavior and have custom plugin management take control. It is targeted at a workflow where zero-setup provides
    // sensible behavior out of the box while allowing users to explicitly take control and determine what gets
    // shipped and enabled in a player.
    [Test]
    [Category("Plugins")]
    public void Plugins_WhenNoPluginManagerIsRegistered_AutomaticallyInitializesPluginsInAllLoadedAssemblies()
    {
        // InputTestFixture installs a dummy plugin manager so we need
        // to get rid of that.
        InputSystem.Reset();

        TestPlugin.s_Initialized = false;
        InputSystem.s_Manager.InitializePlugins();
        Assert.That(TestPlugin.s_Initialized, Is.True);
    }

    public class TestPluginManager : IInputPluginManager
    {
        public bool initialized;
        public void InitializePlugins()
        {
            initialized = true;
        }
    }

    [Test]
    [Category("Plugins")]
    public void Plugins_WhenAtLeastOnePluginManagerIsRegistered_LeavesPluginInitializationToManager()
    {
        var manager = new TestPluginManager();
        InputSystem.RegisterPluginManager(manager);

        InputSystem.s_Manager.InitializePlugins();

        Assert.That(manager.initialized, Is.True);
    }

    // We need to give opportunity for InputSystem.RegisterPluginManager() being called before we attempt
    // to initialize plugins. This means we cannot run plugin initialization directly as part of normal
    // input system initialization. What we do instead is defer plugin initialization until we get the
    // first callback from NativeInputSystem.
    [Test]
    [Category("Plugins")]
    public void Plugins_AreInitializedOnFirstUpdate()
    {
        TestPlugin.s_Initialized = false;

        // The way the Unity test runner executes [SetUp] it seems that will
        // go back to native code in-between SetUp and running the actual test code.
        // This means there will be native input updates happening in-between.
        // Reset the system into a clean state (which also gets rid of the
        // DummyInputPluginMananager installed by InputTestFixture).
        InputSystem.Reset();

        Assert.That(TestPlugin.s_Initialized, Is.False);

        InputSystem.Update();

        Assert.That(TestPlugin.s_Initialized, Is.True);
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

        Assert.That(InputSystem.devices, Has.Count.EqualTo(0));

        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("template").EqualTo("MyDevice").And.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringDeviceFromSave_RestoresRelevantDynamicConfiguration()
    {
        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.SetUsage(device, CommonUsages.LeftHand);
        ////TODO: set variant

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        var newDevice = InputSystem.devices.First(x => x is Gamepad);

        Assert.That(newDevice.template, Is.EqualTo("Gamepad"));
        Assert.That(newDevice.usages, Has.Count.EqualTo(1));
        Assert.That(newDevice.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(Gamepad.current, Is.SameAs(newDevice));
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

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionSetThroughSerialization()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        InputActionSerializationHelpers.AddActionSet(obj);
        InputActionSerializationHelpers.AddActionSet(obj);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets[0].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionSets[1].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionSets[0].name, Is.Not.EqualTo(asset.actionSets[1].name));

        var actionSet2Name = asset.actionSets[1].name;

        InputActionSerializationHelpers.DeleteActionSet(obj, 0);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets, Has.Count.EqualTo(1));
        Assert.That(asset.actionSets[0].name, Is.EqualTo(actionSet2Name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionThroughSerialization()
    {
        var set = new InputActionSet("set");
        set.AddAction(name: "action", binding: "/gamepad/leftStick");
        set.AddAction(name: "action1", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var obj = new SerializedObject(asset);
        var setProperty = obj.FindProperty("m_ActionSets").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddAction(setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets[0].actions, Has.Count.EqualTo(3));
        Assert.That(asset.actionSets[0].actions[2].name, Is.EqualTo("action2"));
        Assert.That(asset.actionSets[0].actions[2].bindings, Has.Count.Zero);

        InputActionSerializationHelpers.DeleteAction(setProperty, 2);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets[0].actions[0].name, Is.EqualTo("action"));
        Assert.That(asset.actionSets[0].actions[1].name, Is.EqualTo("action1"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveBindingThroughSerialization()
    {
        var set = new InputActionSet("set");
        set.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var obj = new SerializedObject(asset);
        var setProperty = obj.FindProperty("m_ActionSets").GetArrayElementAtIndex(0);
        var action1Property = setProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AppendBinding(action1Property, setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        // Sets and actions aren't UnityEngine.Objects so the modifications will not
        // be in-place. Look up the actions after each apply.
        var action1 = asset.actionSets[0].TryGetAction("action1");
        var action2 = asset.actionSets[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action1.bindings[1].path, Is.EqualTo(""));
        Assert.That(action1.bindings[1].modifiers, Is.EqualTo(""));
        Assert.That(action1.bindings[1].group, Is.EqualTo(""));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));

        InputActionSerializationHelpers.RemoveBinding(action1Property, 1, setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        action1 = asset.actionSets[0].TryGetAction("action1");
        action2 = asset.actionSets[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset()
    {
        var set1 = new InputActionSet("set1");
        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set1.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var set2 = new InputActionSet("set2");
        set2.AddAction(name: "action1", binding: "/gamepad/buttonSouth");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set1);
        asset.AddActionSet(set2);
        asset.name = "MyControls";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
                new InputActionCodeGenerator.Options {namespaceName = "MyNamespace"});

        // Our version of Mono doesn't implement the CodeDom stuff so all we can do here
        // is just perform some textual verification. Once we have the newest Mono, this should
        // use CSharpCodeProvider and at least parse if not compile and run the generated wrapper.

        Assert.That(code, Contains.Substring("namespace MyNamespace"));
        Assert.That(code, Contains.Substring("public class MyControls"));
        Assert.That(code, Contains.Substring("public ISX.InputActionSet Clone()"));
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

    ////TODO: tests for InputAssetImporter; for this we need C# mocks to be able to cut us off from the actual asset DB

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
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
