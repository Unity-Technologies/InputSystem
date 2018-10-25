// We always send analytics in the editor (though the actual sending may be disabled in Pro) but we
// only send analytics in the player if enabled.
#if UNITY_ANALYTICS || UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Editor;
#endif

////TODO: restricting startup event to first run after installation (in player only)

partial class CoreTests
{
    [Test]
    [Category("Analytics")]
    public void Analytics_RegistersEventsWhenInitialized()
    {
        var receivedNames = new List<string>();
        var receivedMaxPerHours = new List<int>();
        var receivedMaxPropertiesPerEvents = new List<int>();

        testRuntime.onRegisterAnalyticsEvent =
            (name, maxPerHour, maxPropertiesPerEvent) =>
        {
            receivedNames.Add(name);
            receivedMaxPerHours.Add(maxPerHour);
            receivedMaxPropertiesPerEvents.Add(maxPropertiesPerEvent);
        };

        // The test fixture has already initialized the input system.
        // Create a new manager to test registration.
        var manager = new InputManager();
        manager.Initialize(testRuntime);

        Assert.That(receivedNames,
            Is.EquivalentTo(new[]
            {
                InputAnalytics.kEventStartup, InputAnalytics.kEventFirstUserInteraction, InputAnalytics.kEventShutdown
            }));
        Assert.That(receivedMaxPerHours.Count, Is.EqualTo(3));
        Assert.That(receivedMaxPropertiesPerEvents.Count, Is.EqualTo(3));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesStartupEventOnFirstUpdate()
    {
        string receivedName = null;
        object receivedData = null;

        testRuntime.onSendAnalyticsEvent =
            (name, data) =>
        {
            Assert.That(receivedName, Is.Null);
            receivedName = name;
            receivedData = data;
        };

        // Add some data to the system.
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "TestProductA",
            manufacturer = "TestManufacturerA",
            deviceClass = "Mouse",
            interfaceName = "TestA"
        }.ToJson());
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "TestProductB",
            manufacturer = "TestManufacturerB",
            deviceClass = "Keyboard",
            interfaceName = "TestB"
        }.ToJson());
        testRuntime.ReportNewInputDevice(new InputDeviceDescription // Unrecognized; won't result in device.
        {
            product = "TestProductC",
            manufacturer = "TestManufacturerC",
            deviceClass = "Unknown",
            interfaceName = "Other"
        }.ToJson());
        InputSystem.AddDevice<Gamepad>();

        InputSystem.Update();

        Assert.That(receivedName, Is.EqualTo(InputAnalytics.kEventStartup));
        Assert.That(receivedData, Is.TypeOf<InputAnalytics.StartupEventData>());

        var startupData = (InputAnalytics.StartupEventData)receivedData;

        Assert.That(startupData.version, Is.EqualTo(InputSystem.version.ToString()));
        Assert.That(startupData.devices, Is.Not.Null.And.Length.EqualTo(3));
        Assert.That(startupData.unrecognized_devices, Is.Not.Null.And.Length.EqualTo(1));

        Assert.That(startupData.devices[0].product, Is.Null);
        Assert.That(startupData.devices[0].@interface, Is.Null);
        Assert.That(startupData.devices[0].layout, Is.EqualTo("Gamepad"));
        Assert.That(startupData.devices[0].native, Is.False);

        Assert.That(startupData.devices[1].product, Is.EqualTo("TestManufacturerA TestProductA"));
        Assert.That(startupData.devices[1].@interface, Is.EqualTo("TestA"));
        Assert.That(startupData.devices[1].layout, Is.EqualTo("Mouse"));
        Assert.That(startupData.devices[1].native, Is.True);

        Assert.That(startupData.devices[2].product, Is.EqualTo("TestManufacturerB TestProductB"));
        Assert.That(startupData.devices[2].@interface, Is.EqualTo("TestB"));
        Assert.That(startupData.devices[2].layout, Is.EqualTo("Keyboard"));
        Assert.That(startupData.devices[2].native, Is.True);

        Assert.That(startupData.unrecognized_devices[0].product, Is.EqualTo("TestManufacturerC TestProductC"));
        Assert.That(startupData.unrecognized_devices[0].@interface, Is.EqualTo("Other"));
        Assert.That(startupData.unrecognized_devices[0].layout, Is.EqualTo("Unknown"));
        Assert.That(startupData.unrecognized_devices[0].native, Is.True);
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_SendsStartupEventOnlyOnFirstUpdate()
    {
        var numReceivedCalls = 0;
        testRuntime.onSendAnalyticsEvent =
            (name, data) => ++ numReceivedCalls;

        InputSystem.Update();
        InputSystem.Update();

        Assert.That(numReceivedCalls, Is.EqualTo(1));
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Analytics")]
    public void Analytics_InEditor_StartupEventTransmitsBackendEnabledStatus()
    {
        // Save current player settings so we can restore them.
        var oldEnabled = EditorPlayerSettings.oldSystemBackendsEnabled;
        var newEnabled = EditorPlayerSettings.newSystemBackendsEnabled;

        try
        {
            // Enable new and disable old.
            EditorPlayerSettings.newSystemBackendsEnabled = true;
            EditorPlayerSettings.oldSystemBackendsEnabled = false;

            object receivedData = null;
            testRuntime.onSendAnalyticsEvent =
                (name, data) =>
            {
                Assert.That(receivedData, Is.Null);
                receivedData = data;
            };

            InputSystem.Update();
            var startupData = (InputAnalytics.StartupEventData)receivedData;

            Assert.That(startupData.new_enabled, Is.True);
            Assert.That(startupData.old_enabled, Is.False);
        }
        finally
        {
            EditorPlayerSettings.oldSystemBackendsEnabled = oldEnabled;
            EditorPlayerSettings.newSystemBackendsEnabled = newEnabled;
        }
    }

    #endif

    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesEventOnShutdown()
    {
        // Add and pump some data so we're getting some meaningful metrics.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update(InputUpdateType.Dynamic);

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update(InputUpdateType.Fixed);

        string receivedName = null;
        object receivedData = null;
        testRuntime.onSendAnalyticsEvent =
            (name, data) =>
        {
            Assert.That(receivedData, Is.Null);
            receivedName = name;
            receivedData = data;
        };

        // Simulate shutdown.
        testRuntime.onShutdown();

        Assert.That(receivedName, Is.EqualTo(InputAnalytics.kEventShutdown));
        Assert.That(receivedData, Is.TypeOf<InputAnalytics.ShutdownEventData>());

        var shutdownData = (InputAnalytics.ShutdownEventData)receivedData;
        var metrics = InputSystem.GetMetrics();

        Assert.That(shutdownData.max_num_devices, Is.EqualTo(metrics.maxNumDevices));
        Assert.That(shutdownData.max_state_size_in_bytes, Is.EqualTo(metrics.maxStateSizeInBytes));
        Assert.That(shutdownData.total_event_bytes, Is.EqualTo(metrics.totalEventBytes));
        Assert.That(shutdownData.total_event_count, Is.EqualTo(metrics.totalEventCount));
        Assert.That(shutdownData.total_frame_count, Is.EqualTo(metrics.totalFrameCount));
        Assert.That(shutdownData.total_event_processing_time, Is.EqualTo(metrics.totalEventProcessingTime).Within(0.00001));
    }

    ////TODO: for this one to make sense, we first need noise filtering to be able to tell real user interaction from garbage data
    [Test]
    [Category("Analytics")]
    [Ignore("TODO")]
    public void TODO_Analytics_ReceivesEventOnFirstUserInteraction()
    {
        Assert.Fail();
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
