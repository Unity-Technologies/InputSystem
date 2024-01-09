// We always send analytics in the editor (though the actual sending may be disabled in Pro) but we
// only send analytics in the player if enabled.
#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////TODO: restricting startup event to first run after installation (in player only)

partial class CoreTests
{
    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesStartupEventOnFirstUpdate()
    {
        var registeredNames = new List<string>();
        string receivedName = null;
        object receivedData = null;

        runtime.onRegisterAnalyticsEvent = (name, maxPerHour, maxPropertiesPerEvent) =>
        {
            registeredNames.Add(name);
        };

        runtime.onSendAnalyticsEvent =
            (name, data) =>
        {
            #if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            // Registration handled by framework
            Assert.That(registeredNames.Count, Is.EqualTo(0));
            #else
            Assert.That(registeredNames.Contains(name));
            #endif
            Assert.That(receivedName, Is.Null);
            receivedName = name;
            receivedData = data;
        };

        // Add some data to the system.
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "TestProductA",
            manufacturer = "TestManufacturerA",
            deviceClass = "Mouse",
            interfaceName = "TestA"
        }.ToJson());
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "TestProductB",
            manufacturer = "TestManufacturerB",
            deviceClass = "Keyboard",
            interfaceName = "TestB"
        }.ToJson());
        runtime.ReportNewInputDevice(new InputDeviceDescription // Unrecognized; won't result in device.
        {
            product = "TestProductC",
            manufacturer = "TestManufacturerC",
            deviceClass = "Unknown",
            interfaceName = "Other"
        }.ToJson());
        InputSystem.AddDevice<Gamepad>();

        InputSystem.Update();

        Assert.That(receivedName, Is.EqualTo(InputAnalytics.StartupEventAnalytic.kEventName));
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
        runtime.onSendAnalyticsEvent =
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
        var oldEnabled = EditorPlayerSettingHelpers.oldSystemBackendsEnabled;
        var newEnabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled;

        try
        {
            // Enable new and disable old.
            EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
            EditorPlayerSettingHelpers.oldSystemBackendsEnabled = false;

            object receivedData = null;
            runtime.onSendAnalyticsEvent =
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
            EditorPlayerSettingHelpers.oldSystemBackendsEnabled = oldEnabled;
            EditorPlayerSettingHelpers.newSystemBackendsEnabled = newEnabled;
        }
    }

    #endif

    ////FIXME: these don't seem to actually make it out and to the analytics server
    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesEventOnShutdown()
    {
        // Add and pump some data so we're getting some meaningful metrics.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        var registeredNames = new List<string>();
        string receivedName = null;
        object receivedData = null;

        runtime.onRegisterAnalyticsEvent = (name, maxPerHour, maxPropertiesPerEvent) =>
        {
            registeredNames.Add(name);
        };

        runtime.onSendAnalyticsEvent =
            (name, data) =>
        {
#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            // Registration handled by framework
            Assert.That(registeredNames.Count, Is.EqualTo(0));
#else
            Assert.That(registeredNames.Contains(name));
#endif
            Assert.That(receivedData, Is.Null);
            receivedName = name;
            receivedData = data;
        };

        // Simulate shutdown.
        runtime.onShutdown();

        Assert.That(receivedName, Is.EqualTo(InputAnalytics.ShutdownEventDataAnalytic.kEventName));
        Assert.That(receivedData, Is.TypeOf<InputAnalytics.ShutdownEventData>());

        var shutdownData = (InputAnalytics.ShutdownEventData)receivedData;
        var metrics = InputSystem.metrics;

        Assert.That(shutdownData.max_num_devices, Is.EqualTo(metrics.maxNumDevices));
        Assert.That(shutdownData.max_state_size_in_bytes, Is.EqualTo(metrics.maxStateSizeInBytes));
        Assert.That(shutdownData.total_event_bytes, Is.EqualTo(metrics.totalEventBytes));
        Assert.That(shutdownData.total_event_count, Is.EqualTo(metrics.totalEventCount));
        Assert.That(shutdownData.total_frame_count, Is.EqualTo(metrics.totalUpdateCount));
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

    [Test]
    [Category("Analytics")]
    public void Analytics__ShouldReportEditorSessionAnalytics__IfAccordingToEditorSessionAnalyticsFiniteStateMachine()
    {
        CollectAnalytics(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName);

        // Editor session analytics is stateful and instantiated
        var session = new InputAnalytics.InputActionsEditorSessionAnalytic(
            InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings);

        session.Begin();                    // the user opens project settings and navigates to Input Actions
        session.RegisterEditorFocusIn();    // when window opens, it receives edit focus directly
        runtime.currentTime += 5;           // the user is just grasping what is on the screen for 5 seconds
        session.RegisterActionMapEdit();    // the user adds an action map or renames and action map or deletes one
        session.RegisterActionEdit();       // the user adds an action, or renames it, or deletes one or add binding
        session.RegisterBindingEdit();      // the user modifies a binding configuration
        session.RegisterEditorFocusOut();   // the window looses focus due to user closing e.g. project settings
        session.End();                      // the window is destroyed and the session ends.

        // Assert: Registration
#if (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)
        // Registration is a responsibility of the framework
        Assert.That(registeredAnalytics.Count, Is.EqualTo(0));
#else
        Assert.That(registeredAnalytics.Count, Is.EqualTo(1));
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(100));            // REVIEW: what to use?
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(100)); // REVIEW: what to use?
#endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputAnalytics.InputActionsEditorSessionData>());

        // Assert: Data content
        var data = (InputAnalytics.InputActionsEditorSessionData)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings));
        Assert.That(data.explicitSaveCount, Is.EqualTo(0));
        Assert.That(data.autoSaveCount, Is.EqualTo(0));
        Assert.That(data.sessionDurationSeconds, Is.EqualTo(5.0));
        Assert.That(data.sessionFocusDurationSeconds, Is.EqualTo(5.0));
        Assert.That(data.sessionFocusSwitchCount, Is.EqualTo(1)); // TODO Unclear name
        Assert.That(data.actionMapModificationCount, Is.EqualTo(1));
        Assert.That(data.actionModificationCount, Is.EqualTo(1));
        Assert.That(data.bindingModificationCount, Is.EqualTo(1));
        Assert.That(data.controlSchemeModificationCount, Is.EqualTo(0));
        Assert.That(data.resetCount, Is.EqualTo(0));
    }

    private void TestMultipleEditorFocusSessions(InputAnalytics.InputActionsEditorSessionAnalytic session = null)
    {
        CollectAnalytics(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName);

        session.Begin();                    // the user opens project settings and navigates to Input Actions
        session.RegisterEditorFocusIn();    // when window opens, it receives edit focus directly
        runtime.currentTime += 5;           // the user is just grasping what is on the screen for 5 seconds
        session.RegisterActionMapEdit();    // the user adds an action map or renames and action map or deletes one
        session.RegisterActionEdit();       // the user adds an action, or renames it, or deletes one or add binding
        session.RegisterBindingEdit();      // the user modifies a binding configuration
        session.RegisterControlSchemeEdit();// the user modifies control schemes
        session.RegisterEditorFocusOut();   // the window looses focus due to user closing e.g. project settings
        session.RegisterAutoSave();         // the asset is saved by automatic trigger
        runtime.currentTime += 30;          // the user has switched to something else but still has the window open.
        session.RegisterEditorFocusIn();    // the user switches back to the window
        runtime.currentTime += 2;           // the user spends some time in edit focus
        session.RegisterBindingEdit();      // the user is editing a binding.
        session.RegisterEditorFocusOut();   // the user is dismissing the window and loosing focus
        session.RegisterAutoSave();         // the asset is saved by automatic trigger
        session.End();                      // the window is destroyed and the session ends.

        // Assert: Registration
#if (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)
        // Registration is a responsibility of the framework
        Assert.That(registeredAnalytics.Count, Is.EqualTo(0));
#else
        Assert.That(registeredAnalytics.Count, Is.EqualTo(1));
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(100));            // REVIEW: what to use?
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(100)); // REVIEW: what to use?
#endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputAnalytics.InputActionsEditorSessionData>());

        // Assert: Data content
        var data = (InputAnalytics.InputActionsEditorSessionData)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings));
        Assert.That(data.explicitSaveCount, Is.EqualTo(0));
        Assert.That(data.autoSaveCount, Is.EqualTo(2));
        Assert.That(data.sessionDurationSeconds, Is.EqualTo(37.0));
        Assert.That(data.sessionFocusDurationSeconds, Is.EqualTo(7.0));
        Assert.That(data.sessionFocusSwitchCount, Is.EqualTo(2)); // TODO Unclear name
        Assert.That(data.actionMapModificationCount, Is.EqualTo(1));
        Assert.That(data.actionModificationCount, Is.EqualTo(1));
        Assert.That(data.bindingModificationCount, Is.EqualTo(2));
        Assert.That(data.controlSchemeModificationCount, Is.EqualTo(1));
        Assert.That(data.resetCount, Is.EqualTo(0));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics__ShouldReportEditorSessionAnalyticsWithFocusTime__IfHavingMultipleFocusSessionsWithinSession()
    {
        TestMultipleEditorFocusSessions(
            new InputAnalytics.InputActionsEditorSessionAnalytic(InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics__ShouldReportEditorSessionAnalyticsWithFocusTime__WhenActionsDriveImplicitConditions()
    {
        CollectAnalytics(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName);

        // Editor session analytics is stateful and instantiated
        var session = new InputAnalytics.InputActionsEditorSessionAnalytic(
            InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings);

        session.Begin();                    // the user opens project settings and navigates to Input Actions
        // session.RegisterEditorFocusIn(); // assumes we fail to capture focus-in event due to UI framework malfunction
        runtime.currentTime += 5;           // the user is just grasping what is on the screen for 5 seconds
        session.RegisterActionMapEdit();    // the user adds an action map or renames and action map or deletes one
        session.RegisterActionMapEdit();    // the user adds an action map or renames and action map or deletes one
        session.RegisterBindingEdit();      // the user modifies a binding configuration
        runtime.currentTime += 25;          // the user spends some time in edit focus
        // session.RegisterEditorFocusOut();// assumes we fail to detect focus out event due to UI framework malfunction
        session.RegisterExplicitSave();     // the user presses a save button
        session.End();                      // the window is destroyed and the session ends.

        // Assert: Registration
        #if (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)
        // Registration is a responsibility of the framework
        Assert.That(registeredAnalytics.Count, Is.EqualTo(0));
        #else
        Assert.That(registeredAnalytics.Count, Is.EqualTo(1));
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(100));            // REVIEW: what to use?
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(100)); // REVIEW: what to use?
        #endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputAnalytics.InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputAnalytics.InputActionsEditorSessionData>());

        // Assert: Data content
        var data = (InputAnalytics.InputActionsEditorSessionData)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings));
        Assert.That(data.explicitSaveCount, Is.EqualTo(1));
        Assert.That(data.autoSaveCount, Is.EqualTo(0));
        Assert.That(data.sessionDurationSeconds, Is.EqualTo(30.0));
        Assert.That(data.sessionFocusDurationSeconds, Is.EqualTo(25.0));
        Assert.That(data.sessionFocusSwitchCount, Is.EqualTo(1)); // TODO Unclear name
        Assert.That(data.actionMapModificationCount, Is.EqualTo(2));
        Assert.That(data.actionModificationCount, Is.EqualTo(0));
        Assert.That(data.bindingModificationCount, Is.EqualTo(1));
        Assert.That(data.controlSchemeModificationCount, Is.EqualTo(0));
        Assert.That(data.resetCount, Is.EqualTo(0));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics__ShouldReportEditorSessionAnalytics__IfMultipleSessionsAreReportedUsingTheSameInstance()
    {
        // We reuse an existing test case to prove that the object is reset properly and can be reused after
        // ending the session. We currently let CollectAnalytics reset test harness state which is fine for
        // the targeted verification aspect since only affecting test harness data.
        var session = new InputAnalytics.InputActionsEditorSessionAnalytic(
            InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings);

        TestMultipleEditorFocusSessions(session);
        TestMultipleEditorFocusSessions(session);
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
