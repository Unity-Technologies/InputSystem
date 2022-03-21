using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.InputSystem;
using Unity.InputSystem.Runtime;
using static Unity.InputSystem.Runtime.Native;


// [assembly: InputInlineDeviceDatabase(@"
//
// deviceTraits:
// - guid: a005ccf0-40a4-4dd8-b959-89ab5c1e3151
//   name: MyCustomTrait
//   displayName: My Custom Trait
//   controls:
//   - {guid: 2830970c-cd86-4897-99e1-47c767cd858e, name: custom1, type: Button}
//   - {guid: 1228f2ef-8f72-4f4e-bff4-8493180809ef, name: custom2, type: Button}
//
// devices:
// - guid: 37413297-9ab9-428f-9cd0-f675bcc928b6
//   name: MyCustomDevice
//   displayName: My Custom Device
//   traits:
//   - Gamepad
//   - MyCustomTrait
//
// ")]

namespace UniversalInputBackend
{
    class Program
    {
        
        static unsafe void Main(string[] args)
        {
            Console.WriteLine("hello from managed 1");

            #if false

            var builder = new Unity.InputSystem.DeviceDatabase.IR.GeneratorBuilder().AddViaReflection().Build();

            File.WriteAllText(
                "/Users/dmytro/Documents/universal-input-backend/Runtime.CSharp/BuiltInDeviceDatabase.Generated.cs",
                builder.GenerateManagedSourceCode());
            File.WriteAllText(
                "/Users/dmytro/Documents/universal-input-backend/Runtime.Cpp/_BuiltInDeviceDatabase.h",
                builder.GenerateNativeSourceCode());

            #endif
            

            #if false

            var gamepad = new InputDualSense {};

            //gamepad.SetColor(1.0f, 2.0f, 3.0f, 4.0f);

            //var pressed = btn.GetLatestSample().sample.value;
            
            //btn.Ingress(new InputControlTimestamp(), InputButtonControlSamplePressed);

            //var btn2 = gamepad[InputGamepad.Sticks.Left][InputStickControl.Buttons.Left];

            #endif

            // var foo = new Unity.InputSystem.InputDebugTag {};


            // var foo = new DualShockTrait
            // {
            //
            // };

            //foo._SetPlayerLED(1);

            // TODO on Mono it needs
            #if ENABLE_MONO
            [MonoPInvokeCallback(typeof(...))]
            #endif
            var _container = new PALCallbacksContainer(
                (ptr => Console.WriteLine(new string(ptr))),
                () =>
                {
                    //throw new Exception("native assert");
                }
            );

            var _container2 = new DatabaseCallbacksContainer();
            
            InputRuntimeRunNativeTests();

            InputRuntimeInit(1);

            #if true
            
            var device = InputInstantiateDevice(Guid.Parse("b642521e-7c4b-45d0-b3b7-6084e786aa22"));

            var mouse = device.As<InputMouse>();
            
            mouse.leftButton.Ingress(true);
            Assert.That(mouse.leftButton.isPressed == false);
            Assert.That(mouse.leftButton.wasPressedThisIOFrame == false);
            Assert.That(mouse.leftButton.wasReleasedThisIOFrame == false);
            Assert.That(mouse.leftButton.asAxisOneWay.value, Is.EqualTo(0.0f).Within(0.0001f));

            //var ff = mouse.leftButton;
            
            InputSwapFramebuffer();
            Assert.That(mouse.leftButton.isPressed == true);
            Assert.That(mouse.leftButton.wasPressedThisIOFrame == true);
            Assert.That(mouse.leftButton.wasReleasedThisIOFrame == false);
            Assert.That(mouse.leftButton.asAxisOneWay.value, Is.EqualTo(1.0f).Within(0.0001f));
            
            mouse.leftButton.asAxisOneWay.Ingress(0.45f);
            InputSwapFramebuffer();
            Assert.That(mouse.leftButton.isPressed == false);
            Assert.That(mouse.leftButton.wasPressedThisIOFrame == false);
            Assert.That(mouse.leftButton.wasReleasedThisIOFrame == true);
            // derived control doesn't really have it's own value, but it's rather driven by parent control state 
            Assert.That(mouse.leftButton.asAxisOneWay.value, Is.EqualTo(0.0f).Within(0.0001f));
            
            InputRemoveDevice(device);

            var device2 = InputInstantiateDevice(Guid.Parse("37413297-9ab9-428f-9cd0-f675bcc928b6"));

            // var myTrait = device2.As<InputMyCustomTrait>();
            //
            // myTrait.custom1Button.Ingress(true);
            // InputSwapFramebuffer();
            // Assert.That(myTrait.custom1Button.isPressed == true);
            
            InputRemoveDevice(device2);

            var device3 = InputInstantiateDevice(Guid.Parse("ff0896da-9c98-4489-94c3-4b244162c372"));

            var gamepad = device3.As<InputGamepad>();
            
            //gamepad.leftStick.Ingress(new InputStickControlSample { x = 1, y = 1});
            
            gamepad.leftStick.leftButton.Ingress(true);
            
            InputSwapFramebuffer();

            Assert.That(gamepad.leftStick.verticalAxisTwoWay.value, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(gamepad.leftStick.horizontalAxisTwoWay.value, Is.EqualTo(-1.0f).Within(0.0001f));

            Assert.That(gamepad.leftStick.leftAxisOneWay.value, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(gamepad.leftStick.rightAxisOneWay.value, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(gamepad.leftStick.upAxisOneWay.value, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(gamepad.leftStick.downAxisOneWay.value, Is.EqualTo(0.0f).Within(0.0001f));

            Assert.That(gamepad.leftStick.leftButton.isPressed);
            Assert.That(gamepad.leftStick.rightButton.isNotPressed);
            Assert.That(gamepad.leftStick.upButton.isNotPressed);
            Assert.That(gamepad.leftStick.downButton.isNotPressed);
            
            

            InputRemoveDevice(device3);

            #endif

            InputRuntimeDeinit();

            Console.WriteLine("hello from managed 2");
            //
            // HelloFrom("abc");

        }
    }
}
