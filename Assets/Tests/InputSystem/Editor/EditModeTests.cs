#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA || UNITY_SWITCH || UNITY_LUMIN || UNITY_INPUT_FORCE_XR_PLUGIN) && UNITY_INPUT_SYSTEM_ENABLE_XR && ENABLE_VR
#define ENABLE_XR_COMBINED_DEFINE
#endif

//[Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public class EditModeTests : InputTestFixture
{
    class InputUpdateCounter
    {
        List<InputUpdateType> m_Updates = new List<InputUpdateType>();

        public List<InputUpdateType> updates => m_Updates;

        public bool started { get; private set; }

        public void Start()
        {
            if (started)
                return;

            InputSystem.onAfterUpdate += OnAfterUpdate;
            started = true;
        }

        public void Stop()
        {
            if (!started)
                return;

            InputSystem.onAfterUpdate -= OnAfterUpdate;
            started = false;
        }

        void OnAfterUpdate()
        {
            m_Updates.Add(InputState.currentUpdateType);
        }

        public void Reset()
        {
            m_Updates.Clear();
        }
    }

    [Test]
    [Category("EditMode")]
    [TestCase(InputSettings.UpdateMode.ProcessEventsManually, InputUpdateType.Manual)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate, InputUpdateType.Dynamic)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInFixedUpdate, InputUpdateType.Fixed)]
    #if !ENABLE_XR_COMBINED_DEFINE
    [Ignore("Must be on an XR-supported platform to run this test.")]
    #endif
    public void EditMode_RunUpdatesInEditMode_AllowsNonEditorUpdates(InputSettings.UpdateMode updateMode, InputUpdateType updateType)
    {
#if ENABLE_XR_COMBINED_DEFINE
        runtime.isInPlayMode = false;
        InputSystem.settings.updateMode = updateMode;
        var counter = new InputUpdateCounter();
        counter.Start();

        InputSystem.Update(InputUpdateType.Editor);
        InputSystem.Update(updateType);

        Assert.That(counter.updates.Count, Is.EqualTo(1));
        Assert.That(counter.updates[0], Is.EqualTo(InputUpdateType.Editor));
        counter.Reset();
        InputSystem.runUpdatesInEditMode = true;

        InputSystem.Update(InputUpdateType.Editor);
        InputSystem.Update(updateType);

        Assert.That(counter.updates.Count, Is.EqualTo(2));
        Assert.That(counter.updates[0], Is.EqualTo(InputUpdateType.Editor));
        Assert.That(counter.updates[1], Is.EqualTo(updateType));
        counter.Reset();

        InputSystem.runUpdatesInEditMode = false;

        runtime.isInPlayMode = true;
        counter.Stop();
#endif
    }

    [Test]
#if !ENABLE_XR_COMBINED_DEFINE
    [Ignore("Must be on an XR-supported platform to run this test.")]
#endif
    public void EditMode_InputActions_TriggerInEditMode()
    {
#if ENABLE_XR_COMBINED_DEFINE
        runtime.isInPlayMode = false;
        InputSystem.runUpdatesInEditMode = true;
        var counter = new InputUpdateCounter();
        counter.Start();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");

        int performedCallCount = 0;
        action.performed += context => performedCallCount++;
        action.Enable();

        Set(gamepad.leftTrigger, 0f);
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(performedCallCount, Is.EqualTo(0));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0));

        Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(performedCallCount, Is.EqualTo(1));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0.75f).Within(0.00001f));

        InputSystem.RemoveDevice(gamepad);
        InputSystem.runUpdatesInEditMode = true;
        runtime.isInPlayMode = true;
        counter.Stop();
#endif
    }
}
