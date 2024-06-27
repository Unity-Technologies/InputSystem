// We always send analytics in the editor (though the actual sending may be disabled in Pro) but we
// only send analytics in the player if enabled.
#if UNITY_ANALYTICS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using InputAnalytics = UnityEngine.InputSystem.InputAnalytics;
using Object = UnityEngine.Object;

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
    public void Analytics_ShouldReportEditorSessionAnalytics_IfAccordingToEditorSessionAnalyticsFiniteStateMachine()
    {
        CollectAnalytics(InputActionsEditorSessionAnalytic.kEventName);

        // Editor session analytics is stateful and instantiated
        var session = new InputActionsEditorSessionAnalytic(
            InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings);

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
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxEventsPerHour));
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxNumberOfElements));
#endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputActionsEditorSessionAnalytic.Data>());

        // Assert: Data content
        var data = (InputActionsEditorSessionAnalytic.Data)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings));
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

    private void TestMultipleEditorFocusSessions(InputActionsEditorSessionAnalytic session = null)
    {
        CollectAnalytics(InputActionsEditorSessionAnalytic.kEventName);

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
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxEventsPerHour));
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxNumberOfElements));
#endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputActionsEditorSessionAnalytic.Data>());

        // Assert: Data content
        var data = (InputActionsEditorSessionAnalytic.Data)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings));
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
    public void Analytics_ShouldReportEditorSessionAnalyticsWithFocusTime_IfHavingMultipleFocusSessionsWithinSession()
    {
        TestMultipleEditorFocusSessions(
            new InputActionsEditorSessionAnalytic(InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportEditorSessionAnalyticsWithFocusTime_WhenActionsDriveImplicitConditions()
    {
        CollectAnalytics(InputActionsEditorSessionAnalytic.kEventName);

        // Editor session analytics is stateful and instantiated
        var session = new InputActionsEditorSessionAnalytic(
            InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings);

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
        Assert.That(registeredAnalytics[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(registeredAnalytics[0].maxPerHour, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxEventsPerHour));
        Assert.That(registeredAnalytics[0].maxPropertiesPerEvent, Is.EqualTo(InputActionsEditorSessionAnalytic.kMaxNumberOfElements));
        #endif // (UNITY_2023_2_OR_NEWER && UNITY_EDITOR)

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputActionsEditorSessionAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputActionsEditorSessionAnalytic.Data>());

        // Assert: Data content
        var data = (InputActionsEditorSessionAnalytic.Data)sentAnalyticsEvents[0].data;
        Assert.That(data.kind, Is.EqualTo(InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings));
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
    public void Analytics_ShouldReportEditorSessionAnalytics_IfMultipleSessionsAreReportedUsingTheSameInstance()
    {
        // We reuse an existing test case to prove that the object is reset properly and can be reused after
        // ending the session. We currently let CollectAnalytics reset test harness state which is fine for
        // the targeted verification aspect since only affecting test harness data.
        var session = new InputActionsEditorSessionAnalytic(
            InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings);

        TestMultipleEditorFocusSessions(session);
        TestMultipleEditorFocusSessions(session);
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportBuildAnalytics_WhenNotHavingSettingsAsset()
    {
        CollectAnalytics(InputBuildAnalytic.kEventName);

        var storedSettings = InputSystem.s_Manager.settings;
        InputSettings defaultSettings = null;

        try
        {
            defaultSettings = ScriptableObject.CreateInstance<InputSettings>();
            InputSystem.settings = defaultSettings;

            // Simulate a build (note that we cannot create a proper build report)
            var processor = new InputBuildAnalytic.ReportProcessor();
            processor.OnPostprocessBuild(null); // Note that we cannot create a report

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputBuildAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputBuildAnalytic.InputBuildAnalyticData>());

            // Assert: Data content
            var data = (InputBuildAnalytic.InputBuildAnalyticData)sentAnalyticsEvents[0].data;
            Assert.That(data.buildGuid, Is.EqualTo(string.Empty));
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            Assert.That(data.hasProjectWideInputActionAsset, Is.EqualTo(InputSystem.actions != null));
#else
            Assert.That(data.hasProjectWideInputActionAsset, Is.False);
#endif
            Assert.That(data.hasSettingsAsset, Is.False);
            Assert.That(data.hasDefaultSettings, Is.True);

            Assert.That(data.updateMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.UpdateMode.ProcessEventsInDynamicUpdate));
            Assert.That(data.compensateForScreenOrientation, Is.EqualTo(defaultSettings.compensateForScreenOrientation));
            Assert.That(data.defaultDeadzoneMin, Is.EqualTo(defaultSettings.defaultDeadzoneMin));
            Assert.That(data.defaultDeadzoneMax, Is.EqualTo(defaultSettings.defaultDeadzoneMax));
            Assert.That(data.defaultButtonPressPoint, Is.EqualTo(defaultSettings.defaultButtonPressPoint));
            Assert.That(data.buttonReleaseThreshold, Is.EqualTo(defaultSettings.buttonReleaseThreshold));
            Assert.That(data.defaultTapTime, Is.EqualTo(defaultSettings.defaultTapTime));
            Assert.That(data.defaultSlowTapTime, Is.EqualTo(defaultSettings.defaultSlowTapTime));
            Assert.That(data.defaultHoldTime, Is.EqualTo(defaultSettings.defaultHoldTime));
            Assert.That(data.tapRadius, Is.EqualTo(defaultSettings.tapRadius));
            Assert.That(data.multiTapDelayTime, Is.EqualTo(defaultSettings.multiTapDelayTime));
            Assert.That(data.backgroundBehavior, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.BackgroundBehavior.ResetAndDisableNonBackgroundDevices));
            Assert.That(data.editorInputBehaviorInPlayMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus));
            Assert.That(data.inputActionPropertyDrawerMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.InputActionPropertyDrawerMode.Compact));
            Assert.That(data.maxEventBytesPerUpdate, Is.EqualTo(defaultSettings.maxEventBytesPerUpdate));
            Assert.That(data.maxQueuedEventsPerUpdate, Is.EqualTo(defaultSettings.maxQueuedEventsPerUpdate));
            Assert.That(data.supportedDevices, Is.EqualTo(defaultSettings.supportedDevices));
            Assert.That(data.disableRedundantEventsMerging, Is.EqualTo(defaultSettings.disableRedundantEventsMerging));
            Assert.That(data.shortcutKeysConsumeInput, Is.EqualTo(defaultSettings.shortcutKeysConsumeInput));

            Assert.That(data.featureOptimizedControlsEnabled, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kUseOptimizedControls)));
            Assert.That(data.featureReadValueCachingEnabled, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kUseReadValueCaching)));
            Assert.That(data.featureParanoidReadValueCachingChecksEnabled, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kParanoidReadValueCachingChecks)));
            Assert.That(data.featureDisableUnityRemoteSupport, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kDisableUnityRemoteSupport)));
            Assert.That(data.featureRunPlayerUpdatesInEditMode, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kRunPlayerUpdatesInEditMode)));
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            Assert.That(data.featureUseIMGUIEditorForAssets, Is.EqualTo(defaultSettings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets)));
#else
            Assert.That(data.featureUseIMGUIEditorForAssets, Is.False);
#endif
        }
        finally
        {
            InputSystem.s_Manager.settings = storedSettings;
            if (defaultSettings != null)
                Object.DestroyImmediate(defaultSettings);
        }
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportBuildAnalytics_WhenHavingSettingsAssetWithCustomSettings()
    {
        CollectAnalytics(InputBuildAnalytic.kEventName);

        var storedSettings = InputSystem.s_Manager.settings;
        InputSettings customSettings = null;

        try
        {
            customSettings = ScriptableObject.CreateInstance<InputSettings>();
            customSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
            customSettings.compensateForScreenOrientation = true;
            customSettings.defaultDeadzoneMin = 0.4f;
            customSettings.defaultDeadzoneMax = 0.6f;
            customSettings.defaultButtonPressPoint = 0.1f;
            customSettings.buttonReleaseThreshold = 0.7f;
            customSettings.defaultTapTime = 1.3f;
            customSettings.defaultSlowTapTime = 2.3f;
            customSettings.defaultHoldTime = 3.3f;
            customSettings.tapRadius = 0.1f;
            customSettings.multiTapDelayTime = 1.2f;
            customSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            customSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            customSettings.inputActionPropertyDrawerMode =
                InputSettings.InputActionPropertyDrawerMode.MultilineEffective;
            customSettings.maxEventBytesPerUpdate = 11;
            customSettings.maxQueuedEventsPerUpdate = 12;
            customSettings.supportedDevices = Array.Empty<string>();
            customSettings.disableRedundantEventsMerging = true;
            customSettings.shortcutKeysConsumeInput = true;

            customSettings.SetInternalFeatureFlag(InputFeatureNames.kUseOptimizedControls, true);
            customSettings.SetInternalFeatureFlag(InputFeatureNames.kParanoidReadValueCachingChecks, true);
            customSettings.SetInternalFeatureFlag(InputFeatureNames.kDisableUnityRemoteSupport, true);
            customSettings.SetInternalFeatureFlag(InputFeatureNames.kRunPlayerUpdatesInEditMode, true);
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            customSettings.SetInternalFeatureFlag(InputFeatureNames.kUseIMGUIEditorForAssets, true);
#endif
            customSettings.SetInternalFeatureFlag(InputFeatureNames.kUseReadValueCaching, true);

            InputSystem.settings = customSettings;

            // Simulate a build (note that we cannot create a proper build report)
            var processor = new InputBuildAnalytic.ReportProcessor();
            processor.OnPostprocessBuild(null); // Note that we cannot create a report

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputBuildAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputBuildAnalytic.InputBuildAnalyticData>());

            // Assert: Data content
            var data = (InputBuildAnalytic.InputBuildAnalyticData)sentAnalyticsEvents[0].data;

            Assert.That(data.buildGuid, Is.EqualTo(string.Empty));
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            Assert.That(data.hasProjectWideInputActionAsset, Is.EqualTo(InputSystem.actions != null));
#else
            Assert.That(data.hasProjectWideInputActionAsset, Is.False);
#endif
            Assert.That(data.hasSettingsAsset, Is.False); // Note: We just don't write any file in this test, hence false
            Assert.That(data.hasDefaultSettings, Is.False);

            Assert.That(data.updateMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.UpdateMode.ProcessEventsInFixedUpdate));
            Assert.That(data.compensateForScreenOrientation, Is.EqualTo(true));
            Assert.That(data.defaultDeadzoneMin, Is.EqualTo(0.4f));
            Assert.That(data.defaultDeadzoneMax, Is.EqualTo(0.6f));
            Assert.That(data.defaultButtonPressPoint, Is.EqualTo(0.1f));
            Assert.That(data.buttonReleaseThreshold, Is.EqualTo(0.7f));
            Assert.That(data.defaultTapTime, Is.EqualTo(1.3f));
            Assert.That(data.defaultSlowTapTime, Is.EqualTo(2.3f));
            Assert.That(data.defaultHoldTime, Is.EqualTo(3.3f));
            Assert.That(data.tapRadius, Is.EqualTo(customSettings.tapRadius));
            Assert.That(data.multiTapDelayTime, Is.EqualTo(customSettings.multiTapDelayTime));
            Assert.That(data.backgroundBehavior, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.BackgroundBehavior.IgnoreFocus));
            Assert.That(data.editorInputBehaviorInPlayMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView));
            Assert.That(data.inputActionPropertyDrawerMode, Is.EqualTo(InputBuildAnalytic.InputBuildAnalyticData.InputActionPropertyDrawerMode.MultilineEffective));
            Assert.That(data.maxEventBytesPerUpdate, Is.EqualTo(customSettings.maxEventBytesPerUpdate));
            Assert.That(data.maxQueuedEventsPerUpdate, Is.EqualTo(customSettings.maxQueuedEventsPerUpdate));
            Assert.That(data.supportedDevices, Is.EqualTo(customSettings.supportedDevices));
            Assert.That(data.disableRedundantEventsMerging, Is.EqualTo(customSettings.disableRedundantEventsMerging));
            Assert.That(data.shortcutKeysConsumeInput, Is.EqualTo(customSettings.shortcutKeysConsumeInput));

            Assert.That(data.featureOptimizedControlsEnabled, Is.True);
            Assert.That(data.featureReadValueCachingEnabled, Is.True);
            Assert.That(data.featureParanoidReadValueCachingChecksEnabled, Is.True);
            Assert.That(data.featureDisableUnityRemoteSupport, Is.True);
            Assert.That(data.featureRunPlayerUpdatesInEditMode, Is.True);
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            Assert.That(data.featureUseIMGUIEditorForAssets, Is.True);
#else
            Assert.That(data.featureUseIMGUIEditorForAssets, Is.False); // No impact
#endif
        }
        finally
        {
            InputSystem.s_Manager.settings = storedSettings;
            if (customSettings != null)
                Object.DestroyImmediate(customSettings);
        }
    }

    [TestCase(InputSystemComponent.PlayerInput)]
    [TestCase(InputSystemComponent.PlayerInputManager)]
    [TestCase(InputSystemComponent.InputSystemUIInputModule)]
    [TestCase(InputSystemComponent.StandaloneInputModule)]
    [TestCase(InputSystemComponent.VirtualMouseInput)]
    [TestCase(InputSystemComponent.TouchSimulation)]
    [TestCase(InputSystemComponent.OnScreenButton)]
    [TestCase(InputSystemComponent.OnScreenStick)]
    [Category("Analytics")]
    public void Analytics_ShouldReportComponentAnalytics_WhenEditorIsCreatedAndDestroyed(InputSystemComponent component)
    {
        CollectAnalytics(InputComponentEditorAnalytic.kEventName);

        new InputComponentEditorAnalytic(component).Send();

        // Assert: Data received
        Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
        Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(InputComponentEditorAnalytic.kEventName));
        Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<InputComponentEditorAnalytic.Data>());

        // Assert: Data content
        var data = (InputComponentEditorAnalytic.Data)sentAnalyticsEvents[0].data;
        Assert.That(data.component, Is.EqualTo(component));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportPlayerInputData()
    {
        CollectAnalytics(PlayerInputEditorAnalytic.kEventName);

        using (var gameObject = Scoped.Object(new GameObject()))
        {
            var playerInput = gameObject.value.AddComponent<PlayerInput>();

            var analyticData = new PlayerInputEditorAnalytic.Data(playerInput);
            new PlayerInputEditorAnalytic(ref analyticData).Send();

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(PlayerInputEditorAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<PlayerInputEditorAnalytic.Data>());

            // Assert: Data content
            var data = (PlayerInputEditorAnalytic.Data)sentAnalyticsEvents[0].data;
            Assert.That(data.behavior, Is.EqualTo(InputEditorAnalytics.PlayerNotificationBehavior.SendMessages));
            Assert.That(data.hasActions, Is.False);
            Assert.That(data.hasDefaultMap, Is.False);
            Assert.That(data.hasUIInputModule, Is.False);
            Assert.That(data.hasCamera, Is.False);
        }
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportPlayerInputManagerData()
    {
        CollectAnalytics(PlayerInputManagerEditorAnalytic.kEventName);

        using (var gameObject = Scoped.Object(new GameObject()))
        {
            var playerInputManager = gameObject.value.AddComponent<PlayerInputManager>();

            var analyticData = new PlayerInputManagerEditorAnalytic.Data(playerInputManager);
            new PlayerInputManagerEditorAnalytic(ref analyticData).Send();

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(PlayerInputManagerEditorAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<PlayerInputManagerEditorAnalytic.Data>());

            // Assert: Data content
            var data = (PlayerInputManagerEditorAnalytic.Data)sentAnalyticsEvents[0].data;
            Assert.That(data.behavior, Is.EqualTo(InputEditorAnalytics.PlayerNotificationBehavior.SendMessages));
            Assert.That(data.joinBehavior, Is.EqualTo(PlayerInputManagerEditorAnalytic.Data.PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed));
            Assert.That(data.joiningEnabledByDefault, Is.True);
            Assert.That(data.maxPlayerCount, Is.EqualTo(-1));
        }
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportOnScreenStickData()
    {
        CollectAnalytics(OnScreenStickEditorAnalytic.kEventName);

        using (var gameObject = Scoped.Object(new GameObject()))
        {
            var onScreenStick = gameObject.value.AddComponent<OnScreenStick>();

            var analyticData = new OnScreenStickEditorAnalytic.Data(onScreenStick);
            new OnScreenStickEditorAnalytic(ref analyticData).Send();

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(OnScreenStickEditorAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<OnScreenStickEditorAnalytic.Data>());

            // Assert: Data content
            var data = (OnScreenStickEditorAnalytic.Data)sentAnalyticsEvents[0].data;
            Assert.That(data.behavior, Is.EqualTo(OnScreenStickEditorAnalytic.Data.OnScreenStickBehaviour.RelativePositionWithStaticOrigin));
            Assert.That(data.movementRange, Is.EqualTo(50.0f));
            Assert.That(data.dynamicOriginRange, Is.EqualTo(100.0f));
            Assert.That(data.useIsolatedInputActions, Is.False);
        }
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ShouldReportVirtualMouseInputData()
    {
        CollectAnalytics(VirtualMouseInputEditorAnalytic.kEventName);

        using (var gameObject = Scoped.Object(new GameObject()))
        {
            var virtualMouseInput = gameObject.value.AddComponent<VirtualMouseInput>();

            var analyticData = new VirtualMouseInputEditorAnalytic.Data(virtualMouseInput);
            new VirtualMouseInputEditorAnalytic(ref analyticData).Send();

            // Assert: Data received
            Assert.That(sentAnalyticsEvents.Count, Is.EqualTo(1));
            Assert.That(sentAnalyticsEvents[0].name, Is.EqualTo(VirtualMouseInputEditorAnalytic.kEventName));
            Assert.That(sentAnalyticsEvents[0].data, Is.TypeOf<VirtualMouseInputEditorAnalytic.Data>());

            // Assert: Data content
            var data = (VirtualMouseInputEditorAnalytic.Data)sentAnalyticsEvents[0].data;
            Assert.That(data.cursorMode, Is.EqualTo(VirtualMouseInputEditorAnalytic.Data.CursorMode.SoftwareCursor));
            Assert.That(data.cursorSpeed, Is.EqualTo(400.0f));
            Assert.That(data.scrollSpeed, Is.EqualTo(45.0f));
        }
    }

    // Note: Currently not testing proper analytics reporting when editor is enabled/disabled since unclear how
    //       to achieve this with test framework. This would be a good future improvement.
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
