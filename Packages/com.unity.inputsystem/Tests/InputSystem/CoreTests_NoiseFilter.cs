using NUnit.Framework;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

partial class CoreTests
{
    [InputControlLayout]
    public class NoisyInputDevice : InputDevice
    {
        public static NoisyInputDevice current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct NoisyInputEventState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }

        [FieldOffset(0)] public short button1;
        [FieldOffset(2)] public short button2;
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_NoisyControlInLayoutAppliesToDevices()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""noisy"" : ""true"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("MyDevice");
        Assert.IsTrue(device["first"].noisy);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_NoisyControlDoesNotUpdateCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 0, ""noisy"" : ""true"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");
        double lastUpdateTime = device1.lastUpdateTime;

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = 0 });
        InputSystem.Update();

        Assert.AreEqual(lastUpdateTime, device1.lastUpdateTime);
        Assert.AreEqual(NoisyInputDevice.current, device2);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_NonNoisyControlDoesUpdateCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 0, ""noisy"" : ""true"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.AreEqual(NoisyInputDevice.current, device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = short.MaxValue });
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_NoisyDeadzonesAffectCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.5,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.AreEqual(Gamepad.current, device2);

        InputSystem.QueueStateEvent(device1, new GamepadState { leftStick = new Vector2(0.1f, 0.1f) });
        InputSystem.Update();

        Assert.AreEqual(Gamepad.current, device2);

        InputSystem.QueueStateEvent(device1, new GamepadState { leftStick = new Vector2(1.0f, 1.0f) });
        InputSystem.Update();

        Assert.AreEqual(Gamepad.current, device1);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_NoisyControlsDetectedOnDeltaStateEvents()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""noisy"" : ""true"" }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent<short>(device1["first"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);

        InputSystem.QueueDeltaStateEvent<short>(device2["second"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_SettingNullFilterSkipsNoiseFiltering()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        device1.noiseFilter = null;

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent<short>(device1["first"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_CanOverrideDefaultNoiseFilter()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        // Tag the entire device as noisy
        device1.noiseFilter = new NoiseFilter
        {
            elements = new NoiseFilter.FilteredElement[]
            {
                new NoiseFilter.FilteredElement
                {
                    controlIndex = 0,
                    type = NoiseFilter.EElementType.TypeFull
                },
                new NoiseFilter.FilteredElement
                {
                    controlIndex = 1,
                    type = NoiseFilter.EElementType.TypeFull
                },
            }
        };

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = short.MaxValue });
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device2);
    }

    [Test]
    [Category("NoiseFilter")]
    public void NoiseFilter_CanHandleMultipleProcessors()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""FLT"", ""processors"" : ""Invert(),Clamp(min=0.0,max=0.9)""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""FLT"", ""processors"" : ""Clamp(min=0.0,max=0.9),Invert()""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent<float>(device1["first"], 1.0f);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device2);

        InputSystem.QueueDeltaStateEvent<float>(device1["second"], 1.0f);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }
}
