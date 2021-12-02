using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Property = NUnit.Framework.PropertyAttribute;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

internal class EnhancedTouchTests : CoreTestsFixture
{
    private TouchSimulation m_OldTouchSimulationInstance;

    public override void Setup()
    {
        base.Setup();

        // Disable() will not reset this so default initialize it here.
        Touch.s_GlobalState.historyLengthPerFinger = 64;

        if (!TestContext.CurrentContext.Test.Properties.ContainsKey("EnhancedTouchDisabled"))
        {
            InputSystem.AddDevice<Touchscreen>();
            EnhancedTouchSupport.Enable();
        }

        // Make sure we don't run into interference with a TouchSimulation instance that may
        // already be in place.
        m_OldTouchSimulationInstance = TouchSimulation.s_Instance;
        TouchSimulation.s_Instance = null;
    }

    public override void TearDown()
    {
        EnhancedTouchSupport.Disable();

        // Make sure cleanup really did clean up.
        Assert.That(Touch.s_GlobalState.touchscreens.length, Is.EqualTo(0));
        Assert.That(Touch.s_GlobalState.playerState, Is.EqualTo(default(Touch.FingerAndTouchState)));
        #if UNITY_EDITOR
        Assert.That(Touch.s_GlobalState.editorState, Is.EqualTo(default(Touch.FingerAndTouchState)));
        #endif

        // Some state is kept alive in-between Disable/Enable. Manually clean it out.
        Touch.s_GlobalState.onFingerDown = default;
        Touch.s_GlobalState.onFingerUp = default;
        Touch.s_GlobalState.onFingerMove = default;

        TouchSimulation.Destroy();
        TouchSimulation.s_Instance = m_OldTouchSimulationInstance;
        m_OldTouchSimulationInstance = null;

        base.TearDown();
    }

    [Test]
    [Category("EnhancedTouch")]
    [Property("EnhancedTouchDisabled", 1)]
    public void EnhancedTouch_IsDisabledByDefault()
    {
        Assert.That(EnhancedTouchSupport.enabled, Is.False);
    }

    [Test]
    [Category("EnhancedTouch")]
    [Property("EnhancedTouchDisabled", 1)]
    public void EnhancedTouch_ThrowsExceptionWhenNotEnabled()
    {
        Assert.That(() => Touch.activeFingers, Throws.InvalidOperationException);
        Assert.That(() => Touch.activeTouches, Throws.InvalidOperationException);
        Assert.That(() => Touch.fingers, Throws.InvalidOperationException);
        Assert.That(() => Touch.screens, Throws.InvalidOperationException);
    }

    [Test]
    [Category("EnhancedTouch")]
    [Property("EnhancedTouchDisabled", 1)]
    public void EnhancedTouch_CanBeDisabledAndEnabled()
    {
        InputSystem.AddDevice<Touchscreen>();

        EnhancedTouchSupport.Enable();
        Assert.That(EnhancedTouchSupport.enabled, Is.True);

        EnhancedTouchSupport.Disable();
        Assert.That(EnhancedTouchSupport.enabled, Is.False);
    }

    [Test]
    [Category("EnhancedTouch")]
    [Property("EnhancedTouchDisabled", 1)]
    public void EnhancedTouch_CanBeDisabledAndEnabled_WithoutTouchscreenPresent()
    {
        EnhancedTouchSupport.Enable();
        Assert.That(EnhancedTouchSupport.enabled, Is.True);

        EnhancedTouchSupport.Disable();
        Assert.That(EnhancedTouchSupport.enabled, Is.False);
    }

    // The following tests deal with the fact that the input system potentially maintains more than one view on input
    // because it concurrently supports input state queries in fixed update, dynamic/manual update, and editor updates.
    // For the touch system, this means that we have to potentially track several different versions of state, too.
    // The tests here go through the various combinations and make sure that Touch captures information correctly.
    #region Update Types / Modes

    [Test]
    [Category("EnhancedTouch")]
    [TestCase(InputSettings.UpdateMode.ProcessEventsManually, InputUpdateType.Manual)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate, InputUpdateType.Dynamic)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInFixedUpdate, InputUpdateType.Fixed)]
    public void EnhancedTouch_SupportsInputUpdateIn(InputSettings.UpdateMode updateMode, InputUpdateType updateType)
    {
        InputSystem.settings.updateMode = updateMode;
        runtime.currentTimeForFixedUpdate += Time.fixedDeltaTime;
        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        InputSystem.Update(updateType);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].screenPosition,
            Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
    }

    #if UNITY_EDITOR
    [Test]
    [Category("EnhancedTouch")]
    [TestCase(InputSettings.UpdateMode.ProcessEventsManually)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInFixedUpdate)]
    public void EnhancedTouch_SupportsEditorUpdates(InputSettings.UpdateMode updateMode)
    {
        InputSystem.settings.editorInputBehaviorInPlayMode = default;

        // To better observe that play mode and edit mode state is indeed independent and handled
        // correctly, suppress resetting of the touch device when focus is lost to the player.
        runtime.runInBackground = true;
        SetCanRunInBackground(Touchscreen.current);

        InputSystem.settings.updateMode = updateMode;
        runtime.currentTimeForFixedUpdate += Time.fixedDeltaTime;
        // Run one player update with data.
        BeginTouch(1, new Vector2(0.123f, 0.234f));

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));

        // And make sure we're not seeing the data in the editor.
        runtime.PlayerFocusLost();
        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(Touch.activeTouches, Is.Empty);

        // Feed some data into editor state.
        BeginTouch(2, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(2));

        // Switch back to player.
        runtime.PlayerFocusGained();
        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
    }

    #endif

    #endregion

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanGetActiveTouches()
    {
        // Begin and move in same frame.
        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        // Begin only.
        BeginTouch(2, new Vector2(0.345f, 0.456f), queueEventOnly: true);
        // Begin, move, and end in same frame.
        BeginTouch(3, new Vector2(0.456f, 0.567f), queueEventOnly: true);
        MoveTouch(3, new Vector2(0.111f, 0.222f), queueEventOnly: true); // This one should get ignored.
        EndTouch(3, new Vector2(0.567f, 0.678f), queueEventOnly: true);
        // Begin only but reusing previous touch ID.
        BeginTouch(3, new Vector2(0.678f, 0.789f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(4));

        // When we begin and move a touch in the same frame, the phase should be Began, *NOT* Moved.
        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        // A touch that begins and ends in the same frame, will see a Began in the current frame and a separate Ended in the next
        // (even though there was no actual activity on the touch that frame).
        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.456f, 0.567f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.678f, 0.789f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(4));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Moved)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        // Ended record for touch touch #3 that began and ended in previous frame.
        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.567f, 0.678f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.678f, 0.789f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.678f, 0.789f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.678f, 0.789f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(default(Vector2)));

        EndTouch(3, new Vector2(0.111f, 0.222f), queueEventOnly: true);
        EndTouch(2, new Vector2(0.222f, 0.333f), queueEventOnly: true);
        EndTouch(1, new Vector2(0.333f, 0.444f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.333f, 0.444f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(new Vector2(0.099f, 0.099f)).Using(Vector2EqualityComparer.Instance));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.222f, 0.333f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(new Vector2(-0.123f, -0.123f)).Using(Vector2EqualityComparer.Instance));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.111f, 0.222f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("delta").EqualTo(new Vector2(-0.567f, -0.567f)).Using(Vector2EqualityComparer.Instance));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Is.Empty);
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_DeltasInActiveTouchesAccumulateAndReset()
    {
        // Only Began in frame.
        BeginTouch(1, new Vector2(0.111f, 0.222f), queueEventOnly: true);
        // Began and Moved in same frame.
        BeginTouch(2, new Vector2(0.222f, 0.333f), queueEventOnly: true);
        MoveTouch(2, new Vector2(0.333f, 0.444f), queueEventOnly: true);
        // Began and Ended in same frame.
        BeginTouch(3, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        EndTouch(3, new Vector2(0.234f, 0.345f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.111f, 0.222f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.222f, 0.333f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Began)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.123f, 0.234f)));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3)); // Touch #3 ends this frame.

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.111f, 0.222f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Moved)
            .And.With.Property("delta").EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.333f, 0.444f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(3)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("delta").EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.234f, 0.345f)));

        MoveTouch(1, new Vector2(0.444f, 0.555f), queueEventOnly: true); // Generates delta to (0.111,0.111)!
        MoveTouch(1, new Vector2(0.555f, 0.666f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.666f, 0.777f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(2));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Moved)
            .And.With.Property("delta").EqualTo(new Vector2(0.555f, 0.555f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.666f, 0.777f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.333f, 0.444f)));

        MoveTouch(1, new Vector2(0.777f, 0.888f), queueEventOnly: true);
        EndTouch(1, new Vector2(0.888f, 0.999f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(2));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(1)
            .And.With.Property("phase").EqualTo(TouchPhase.Ended)
            .And.With.Property("delta").EqualTo(new Vector2(0.222f, 0.222f)).Using(Vector2EqualityComparer.Instance)
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.888f, 0.999f)));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.333f, 0.444f)));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));

        Assert.That(Touch.activeTouches, Has.Exactly(1)
            .With.Property("touchId").EqualTo(2)
            .And.With.Property("phase").EqualTo(TouchPhase.Stationary)
            .And.With.Property("delta").EqualTo(default(Vector2))
            .And.With.Property("screenPosition").EqualTo(new Vector2(0.333f, 0.444f)));
    }

    // Unlike when looking at activeTouches (given that "active" is a frame-to-frame concept here)
    // when looking at touch history, we're looking at values the touches had when they were reported.
    // Thus we don't want accumulation and resetting (which again are frame-to-frame kind of mechanics).
    [Test]
    [Category("EnhancedTouch")]
    [TestCase(false)]
    [TestCase(true)]
    public void EnhancedTouch_DeltasInTouchHistoryDoNotAccumulateAndReset_WithEventMergingSetTo(bool mergeRedundantEvents)
    {
        InputSystem.settings.disableRedundantEventsMerging = !mergeRedundantEvents;

        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.345f, 0.456f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.456f, 0.567f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeFingers[0].touchHistory.Count, Is.EqualTo(mergeRedundantEvents ? 3 : 4));

        if (mergeRedundantEvents)
        {
            // Event merging adds deltas inside
            Assert.That(Touch.activeFingers[0].touchHistory[0].delta,
                Is.EqualTo(new Vector2(0.222f, 0.222f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(Touch.activeFingers[0].touchHistory[1].delta,
                Is.EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            Assert.That(Touch.activeFingers[0].touchHistory[0].delta,
                Is.EqualTo(new Vector2(0.222f, 0.222f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(Touch.activeFingers[0].touchHistory[1].delta,
                Is.EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(Touch.activeFingers[0].touchHistory[2].delta,
                Is.EqualTo(new Vector2(0.111f, 0.111f)).Using(Vector2EqualityComparer.Instance));
        }

        Assert.That(Touch.activeFingers[0].touchHistory.Last().delta,
            Is.EqualTo(new Vector2()).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanCheckForTaps()
    {
        BeginTouch(1, new Vector2(123, 234));

        Assert.That(Touch.activeTouches[0].isTap, Is.False);
        Assert.That(Touch.activeTouches[0].tapCount, Is.EqualTo(0));

        EndTouch(1, new Vector2(123, 234));

        Assert.That(Touch.activeTouches[0].isTap, Is.True);
        Assert.That(Touch.activeTouches[0].tapCount, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory, Has.Count.EqualTo(2));
        Assert.That(Touch.fingers[0].touchHistory[0].isTap, Is.True);
        Assert.That(Touch.fingers[0].touchHistory[1].isTap, Is.False);
        Assert.That(Touch.fingers[0].touchHistory[0].tapCount, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory[1].tapCount, Is.EqualTo(0));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanGetStartPositionAndTimeOfTouch()
    {
        runtime.currentTime = 0.111;
        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        runtime.currentTime = 0.222;
        MoveTouch(1, new Vector2(0.345f, 0.456f), queueEventOnly: true);
        BeginTouch(2, new Vector2(0.456f, 0.567f), queueEventOnly: true);
        runtime.currentTime = 0.333;
        EndTouch(2, new Vector2(0.567f, 0.678f), queueEventOnly: true);
        InputSystem.Update();

        Assert.That(Touch.activeTouches[0].startScreenPosition,
            Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[1].startScreenPosition,
            Is.EqualTo(new Vector2(0.456f, 0.567f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].startTime, Is.EqualTo(0.111));
        Assert.That(Touch.activeTouches[1].startTime, Is.EqualTo(0.222));
    }

    [Test]
    [Category("EnhancedTouch")]
    [TestCase(false)]
    [TestCase(true)]
    public void EnhancedTouch_CanAccessHistoryOfTouch_WithEventMergingSetTo(bool mergeRedundantEvents)
    {
        InputSystem.settings.disableRedundantEventsMerging = !mergeRedundantEvents;

        // Noise. This one shouldn't show up in the history.
        BeginTouch(2, new Vector2(0.111f, 0.222f), queueEventOnly: true);
        EndTouch(2, new Vector2(0.111f, 0.222f), queueEventOnly: true);
        InputSystem.Update();
        InputSystem.Update(); // The end touch lingers for one frame.

        runtime.currentTime = 0.876;
        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        runtime.currentTime = 0.987;
        MoveTouch(1, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.345f, 0.456f), queueEventOnly: true);
        MoveTouch(1, new Vector2(0.456f, 0.567f), queueEventOnly: true);
        BeginTouch(3, new Vector2(0.666f, 0.666f), queueEventOnly: true);
        BeginTouch(4, new Vector2(0.777f, 0.777f), queueEventOnly: true);
        EndTouch(4, new Vector2(0.888f, 0.888f), queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(3));

        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].history, Has.Count.EqualTo(mergeRedundantEvents ? 2 : 3));
        Assert.That(Touch.activeTouches[0].history, Has.All.Property("finger").SameAs(Touch.activeTouches[0].finger));
        var beganIndex = mergeRedundantEvents ? 1 : 2;
        Assert.That(Touch.activeTouches[0].history[beganIndex].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.activeTouches[0].history[beganIndex].time, Is.EqualTo(0.876));
        Assert.That(Touch.activeTouches[0].history[beganIndex].startTime, Is.EqualTo(0.876));
        Assert.That(Touch.activeTouches[0].history[beganIndex].startScreenPosition,
            Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        for (int index = 0; index < (mergeRedundantEvents ? 1 : 2); ++index)
        {
            Assert.That(Touch.activeTouches[0].history[index].phase, Is.EqualTo(TouchPhase.Moved));
            Assert.That(Touch.activeTouches[0].history[index].time, Is.EqualTo(0.987));
            Assert.That(Touch.activeTouches[0].history[index].startTime, Is.EqualTo(0.876));
            Assert.That(Touch.activeTouches[0].history[index].startScreenPosition,
                Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        }

        Assert.That(Touch.activeTouches[1].touchId, Is.EqualTo(3));
        Assert.That(Touch.activeTouches[1].history, Is.Empty);

        Assert.That(Touch.activeTouches[2].touchId, Is.EqualTo(4));
        Assert.That(Touch.activeTouches[2].history, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[2].history[0].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.activeTouches[2].history[0].screenPosition,
            Is.EqualTo(new Vector2(0.777f, 0.777f)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_HasOneFingerForEveryPossibleContactOnScreen()
    {
        var touchscreen1 = Touchscreen.current;

        // To mix it up, have a touchscreen with 15 possible contacts instead of
        // the default ten.
        const string json = @"
            {
                ""name"" : ""CustomTouchscreen"",
                ""extend"" : ""Touchscreen"",
                ""controls"" : [
                    { ""name"" : ""touch"", ""arraySize"" : 15 }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var touchscreen2 = (Touchscreen)InputSystem.AddDevice("CustomTouchscreen");

        // Make sure that the system has noticed both screens. One got added before it initialized,
        // one got added after.
        Assert.That(Touch.screens.Count(), Is.EqualTo(2));
        Assert.That(Touch.screens, Has.Exactly(1).SameAs(touchscreen1));
        Assert.That(Touch.screens, Has.Exactly(1).SameAs(touchscreen2));

        // Make we get a combined 70 fingers.
        Assert.That(Touch.fingers, Has.Count.EqualTo(touchscreen1.touches.Count + touchscreen2.touches.Count));
        Assert.That(Touch.fingers, Has.Exactly(touchscreen1.touches.Count).With.Property("screen").SameAs(touchscreen1));
        Assert.That(Touch.fingers, Has.Exactly(touchscreen2.touches.Count).With.Property("screen").SameAs(touchscreen2));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanTrackActiveFingers()
    {
        Assert.That(Touch.activeFingers, Is.Empty);

        BeginTouch(1, new Vector2(0.123f, 0.234f));

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(1));
        Assert.That(Touch.activeFingers[0].isActive, Is.True);
        Assert.That(Touch.activeFingers[0].index, Is.EqualTo(0));
        Assert.That(Touch.activeFingers[0].screen, Is.SameAs(Touchscreen.current));
        Assert.That(Touch.activeFingers[0].screenPosition,
            Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));

        MoveTouch(1, new Vector2(0.234f, 0.345f));

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(1));
        Assert.That(Touch.activeFingers[0].isActive, Is.True);
        Assert.That(Touch.activeFingers[0].index, Is.EqualTo(0));
        Assert.That(Touch.activeFingers[0].screen, Is.SameAs(Touchscreen.current));
        Assert.That(Touch.activeFingers[0].screenPosition,
            Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));

        BeginTouch(2, new Vector2(0.987f, 0.789f));

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(2));
        Assert.That(Touch.activeFingers[0].isActive, Is.True);
        Assert.That(Touch.activeFingers[0].index, Is.EqualTo(0));
        Assert.That(Touch.activeFingers[0].screen, Is.SameAs(Touchscreen.current));
        Assert.That(Touch.activeFingers[0].screenPosition,
            Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeFingers[1].isActive, Is.True);
        Assert.That(Touch.activeFingers[1].index, Is.EqualTo(1));
        Assert.That(Touch.activeFingers[1].screen, Is.SameAs(Touchscreen.current));
        Assert.That(Touch.activeFingers[1].screenPosition,
            Is.EqualTo(new Vector2(0.987f, 0.789f)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanTrackActiveFingers_FromMultipleTouchscreens()
    {
        var screen1 = Touchscreen.current;
        var screen2 = InputSystem.AddDevice<Touchscreen>();

        Assert.That(Touch.fingers, Has.Count.EqualTo(screen1.touches.Count + screen2.touches.Count));
        Assert.That(Touch.fingers, Has.Exactly(screen1.touches.Count).With.Property("screen").SameAs(screen1));
        Assert.That(Touch.fingers, Has.Exactly(screen2.touches.Count).With.Property("screen").SameAs(screen2));

        BeginTouch(1, new Vector2(0.123f, 0.234f), screen: screen1);
        BeginTouch(1, new Vector2(0.234f, 0.345f), screen: screen2);

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(2));
        Assert.That(Touch.activeFingers,
            Has.Exactly(1).With.Property("screen").SameAs(screen1).And.With.Property("screenPosition")
                .EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeFingers,
            Has.Exactly(1).With.Property("screen").SameAs(screen2).And.With.Property("screenPosition")
                .EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_RemovingTouchscreenRemovesItsActiveTouches()
    {
        BeginTouch(1, new Vector2(123, 234));

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));

        InputSystem.RemoveDevice(Touchscreen.current);

        Assert.That(Touch.activeTouches, Is.Empty);
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanGetCurrentTouchFromFinger()
    {
        BeginTouch(1, new Vector2(0.123f, 0.234f));
        BeginTouch(2, new Vector2(0.456f, 0.567f));

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(2));
        Assert.That(Touch.activeFingers[0].currentTouch, Is.Not.EqualTo(default(Touch)));
        Assert.That(Touch.activeFingers[0].lastTouch, Is.EqualTo(Touch.activeFingers[0].currentTouch));
        Assert.That(Touch.activeFingers[0].currentTouch.screenPosition, Is.EqualTo(new Vector2(0.123f, 0.234f)));
        Assert.That(Touch.activeFingers[0].currentTouch.touchId, Is.EqualTo(1));

        EndTouch(1, new Vector2(0.234f, 0.345f));

        // The ended touch should linger for one frame.
        Assert.That(Touch.activeFingers, Has.Count.EqualTo(2));
        Assert.That(Touch.activeFingers[0].currentTouch.phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.activeFingers[0].currentTouch.screenPosition, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Touch.activeFingers[0].currentTouch.touchId, Is.EqualTo(1));
        Assert.That(Touch.activeFingers[0].lastTouch, Is.EqualTo(Touch.activeFingers[0].currentTouch));

        InputSystem.Update();

        Assert.That(Touch.activeFingers, Has.Count.EqualTo(1));
        Assert.That(Touch.activeFingers[0].currentTouch.touchId, Is.EqualTo(2));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanGetLastTouchFromFinger()
    {
        BeginTouch(1, new Vector2(0.123f, 0.234f));
        EndTouch(1, new Vector2(0.234f, 0.345f));

        InputSystem.Update();

        Assert.That(Touch.fingers[0].currentTouch.valid, Is.False);
        Assert.That(Touch.fingers[0].lastTouch.valid, Is.True);
        Assert.That(Touch.fingers[0].lastTouch.screenPosition, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Touch.fingers[0].lastTouch.touchId, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].lastTouch.phase, Is.EqualTo(TouchPhase.Ended));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanAccessTouchHistoryOnFinger()
    {
        BeginTouch(1, new Vector2(0.123f, 0.234f));    // Finger #0, touch #3
        MoveTouch(1, new Vector2(0.234f, 0.345f));     // Finger #0, touch #2
        MoveTouch(1, new Vector2(0.345f, 0.456f));     // Finger #0, touch #1
        BeginTouch(2, new Vector2(0.456f, 0.567f));    // Finger #1, touch #4
        MoveTouch(2, new Vector2(0.567f, 0.678f));     // Finger #1, touch #3
        InputSystem.Update(); // Noise.
        MoveTouch(1, new Vector2(0.789f, 0.890f));     // Finger #0, touch #0
        EndTouch(2, new Vector2(0.111f, 0.222f));      // Finger #1, touch #2
        BeginTouch(3, new Vector2(0.222f, 0.333f));    // Finger #1, touch #1
        EndTouch(3, new Vector2(0.333f, 0.444f));      // Finger #1, touch #0

        Assert.That(Touch.fingers[0].touchHistory, Has.Count.EqualTo(4));
        Assert.That(Touch.fingers[1].touchHistory, Has.Count.EqualTo(5));
        Assert.That(Touch.fingers[0].touchHistory, Has.All.Property("finger").SameAs(Touch.fingers[0]));
        Assert.That(Touch.fingers[1].touchHistory, Has.All.Property("finger").SameAs(Touch.fingers[1]));

        Assert.That(Touch.fingers[0].touchHistory[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory[0].phase, Is.EqualTo(TouchPhase.Moved));
        Assert.That(Touch.fingers[0].touchHistory[0].screenPosition,
            Is.EqualTo(new Vector2(0.789f, 0.890f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[0].touchHistory[1].touchId, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory[1].phase, Is.EqualTo(TouchPhase.Moved));
        Assert.That(Touch.fingers[0].touchHistory[1].screenPosition,
            Is.EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[0].touchHistory[2].touchId, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory[2].phase, Is.EqualTo(TouchPhase.Moved));
        Assert.That(Touch.fingers[0].touchHistory[2].screenPosition,
            Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[0].touchHistory[3].touchId, Is.EqualTo(1));
        Assert.That(Touch.fingers[0].touchHistory[3].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.fingers[0].touchHistory[3].screenPosition,
            Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));

        Assert.That(Touch.fingers[1].touchHistory[0].touchId, Is.EqualTo(3));
        Assert.That(Touch.fingers[1].touchHistory[0].phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.fingers[1].touchHistory[0].screenPosition,
            Is.EqualTo(new Vector2(0.333f, 0.444f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[1].touchHistory[1].touchId, Is.EqualTo(3));
        Assert.That(Touch.fingers[1].touchHistory[1].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.fingers[1].touchHistory[1].screenPosition,
            Is.EqualTo(new Vector2(0.222f, 0.333f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[1].touchHistory[2].touchId, Is.EqualTo(2));
        Assert.That(Touch.fingers[1].touchHistory[2].phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.fingers[1].touchHistory[2].screenPosition,
            Is.EqualTo(new Vector2(0.111f, 0.222f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[1].touchHistory[3].touchId, Is.EqualTo(2));
        Assert.That(Touch.fingers[1].touchHistory[3].phase, Is.EqualTo(TouchPhase.Moved));
        Assert.That(Touch.fingers[1].touchHistory[3].screenPosition,
            Is.EqualTo(new Vector2(0.567f, 0.678f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.fingers[1].touchHistory[4].touchId, Is.EqualTo(2));
        Assert.That(Touch.fingers[1].touchHistory[4].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.fingers[1].touchHistory[4].screenPosition,
            Is.EqualTo(new Vector2(0.456f, 0.567f)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanReceiveCallbacksOnFingerActivity()
    {
        var receivedFingers = new List<Tuple<string, Finger>>();

        Touch.onFingerDown +=
            finger => receivedFingers.Add(new Tuple<string, Finger>("Down", finger));
        Touch.onFingerUp +=
            finger => receivedFingers.Add(new Tuple<string, Finger>("Up", finger));
        Touch.onFingerMove +=
            finger => receivedFingers.Add(new Tuple<string, Finger>("Move", finger));

        BeginTouch(1, new Vector2(0.123f, 0.234f));
        BeginTouch(2, new Vector2(0.234f, 0.345f));

        Assert.That(receivedFingers, Is.EquivalentTo(new[]
        {
            new Tuple<string, Finger>("Down", Touch.fingers[0]),
            new Tuple<string, Finger>("Down", Touch.fingers[1]),
        }));

        receivedFingers.Clear();

        MoveTouch(1, new Vector2(0.345f, 0.456f));
        MoveTouch(1, new Vector2(0.456f, 0.567f));

        Assert.That(receivedFingers, Is.EquivalentTo(new[]
        {
            new Tuple<string, Finger>("Move", Touch.fingers[0]),
            new Tuple<string, Finger>("Move", Touch.fingers[0]),
        }));

        receivedFingers.Clear();

        EndTouch(2, new Vector2(0.567f, 0.678f));

        Assert.That(receivedFingers, Is.EquivalentTo(new[]
        {
            new Tuple<string, Finger>("Up", Touch.fingers[1])
        }));
    }

    // https://fogbugz.unity3d.com/f/cases/1286865/
    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanBeDisabledAndReenabled()
    {
        BeginTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Began));

        MoveTouch(1, new Vector2(0.234f, 0.345f), queueEventOnly: true);
        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Moved));

        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Stationary));

        EnhancedTouchSupport.Disable();
        EnhancedTouchSupport.Enable();

        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Stationary));

        MoveTouch(1, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Moved));

        InputSystem.Update();
        Assert.That(Touch.activeTouches.Count, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Stationary));
    }

    [Test]
    [Category("EnhancedTouch")]
    [Property("EnhancedTouchDisabled", 1)]
    public void EnhancedTouch_CanEnableAndDisableTouchSimulation()
    {
        Assert.That(InputSystem.devices, Has.None.TypeOf<Touchscreen>());

        TouchSimulation.Enable();

        Assert.That(InputSystem.devices, Has.Exactly(1).AssignableTo<Touchscreen>());
        Assert.That(TouchSimulation.instance, Is.Not.Null);
        Assert.That(TouchSimulation.instance.simulatedTouchscreen, Is.Not.Null);
        Assert.That(TouchSimulation.instance.simulatedTouchscreen, Is.SameAs(Touchscreen.current));

        TouchSimulation.Disable();

        Assert.That(InputSystem.devices, Has.None.TypeOf<Touchscreen>());

        // Make sure we can re-enable it.
        TouchSimulation.Enable();

        Assert.That(InputSystem.devices, Has.Exactly(1).AssignableTo<Touchscreen>());

        TouchSimulation.Destroy();

        Assert.That(TouchSimulation.instance, Is.Null);
    }

    [Test]
    [Category("EnhancedTouch")]
    [TestCase("Mouse")]
    [TestCase("Pen")]
    [TestCase("Pointer")]
    public void EnhancedTouch_CanSimulateTouchInputFrom(string layoutName)
    {
        var pointer = (Pointer)InputSystem.AddDevice(layoutName);

        TouchSimulation.Enable();

        Set(pointer.position, new Vector2(123, 234), queueEventOnly: true);
        Press(pointer.press);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.activeTouches[0].tapCount, Is.Zero);
        Assert.That(Touch.activeTouches[0].isTap, Is.False);

        Move(pointer.position, new Vector2(234, 345));

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(new Vector2(111, 111)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Moved));
        Assert.That(Touch.activeTouches[0].tapCount, Is.Zero);
        Assert.That(Touch.activeTouches[0].isTap, Is.False);

        Release(pointer.press);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.activeTouches[0].tapCount, Is.Zero);
        Assert.That(Touch.activeTouches[0].isTap, Is.False);

        PressAndRelease(pointer.press);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(2));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Began)); // Ended comes in next frame.
        Assert.That(Touch.activeTouches[0].tapCount, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].isTap, Is.True);

        PressAndRelease(pointer.press);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(2));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(2));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.activeTouches[0].tapCount, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].isTap, Is.True);
        Assert.That(Touch.activeTouches[1].touchId, Is.EqualTo(3));
        Assert.That(Touch.activeTouches[1].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[1].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[1].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[1].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.activeTouches[1].tapCount, Is.EqualTo(2));
        Assert.That(Touch.activeTouches[1].isTap, Is.True);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(3));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Ended));
        Assert.That(Touch.activeTouches[0].tapCount, Is.EqualTo(2));
        Assert.That(Touch.activeTouches[0].isTap, Is.True);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Is.Empty);
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_CanSimulateTouchInputFromMultiplePointers()
    {
        var pointer1 = InputSystem.AddDevice<Pointer>();
        var pointer2 = InputSystem.AddDevice<Pointer>();

        TouchSimulation.Enable();

        Set(pointer1.position, new Vector2(123, 234), queueEventOnly: true);
        Set(pointer2.position, new Vector2(234, 345), queueEventOnly: true);
        Press(pointer1.press, queueEventOnly: true);
        Press(pointer2.press, queueEventOnly: true);

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(2));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
        Assert.That(Touch.activeTouches[0].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[0].screenPosition, Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(Touch.activeTouches[1].touchId, Is.EqualTo(2));
        Assert.That(Touch.activeTouches[1].screen, Is.SameAs(TouchSimulation.instance.simulatedTouchscreen));
        Assert.That(Touch.activeTouches[1].screenPosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[1].delta, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(Touch.activeTouches[1].phase, Is.EqualTo(TouchPhase.Began));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_TouchSimulation_CanAddAndRemovePointerDevices()
    {
        TouchSimulation.Enable();

        var pointer = InputSystem.AddDevice<Pointer>();
        Press(pointer.press);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));

        InputSystem.RemoveDevice(pointer);

        ////FIXME: This doesn't work yet as TouchSimulation isn't using events and Touch ignores input that isn't from events
        //Assert.That(Touch.activeTouches, Is.Empty);
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_TouchSimulation_ProducesOneTouchFromEveryNonSyntheticButton()
    {
        const string json = @"
            {
                ""name"" : ""CustomPointer"",
                ""extend"" : ""Pointer"",
                ""controls"" : [
                    { ""name"" : ""syntheticButton"", ""layout"" : ""Button"", ""synthetic"" : true },
                    { ""name"" : ""nonSyntheticButton"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Pointer)InputSystem.AddDevice("CustomPointer");

        TouchSimulation.Enable();

        Press((ButtonControl)device["nonSyntheticButton"]);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));

        Press((ButtonControl)device["syntheticButton"]);

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].touchId, Is.EqualTo(1));
    }

    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_TouchSimulation_ProducesPrimaryTouches()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        TouchSimulation.Enable();

        Set(mouse.position, new Vector2(123, 234));
        Press(mouse.leftButton);

        Assert.That(TouchSimulation.instance.simulatedTouchscreen.press.ReadValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.position.ReadValue(),
            Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.delta.ReadValue(),
            Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        Set(mouse.position, new Vector2(234, 345));

        Assert.That(TouchSimulation.instance.simulatedTouchscreen.press.ReadValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.position.ReadValue(),
            Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.delta.ReadValue(),
            Is.EqualTo(new Vector2(111, 111)).Using(Vector2EqualityComparer.Instance));

        InputSystem.Update();

        Assert.That(TouchSimulation.instance.simulatedTouchscreen.press.ReadValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.primaryTouch.phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.position.ReadValue(),
            Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));
        Assert.That(TouchSimulation.instance.simulatedTouchscreen.delta.ReadValue(),
            Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
    }

    // This is mostly for domain reloads.
    [Test]
    [Category("EnhancedTouch")]
    public void EnhancedTouch_TouchSimulation_ReusesSimulatedTouchscreenInstanceIfPresent()
    {
        var device = InputSystem.AddDevice<Touchscreen>("Simulated Touchscreen");

        TouchSimulation.Enable();

        Assert.That(TouchSimulation.instance.simulatedTouchscreen, Is.SameAs(device));
    }

    [Test]
    [Category("EnhancedTouch")]
    public unsafe void EnhancedTouch_TouchSimulation_DisablesPointerDevicesWithoutDisablingEvents()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();

        runtime.SetDeviceCommandCallback(mouse, (id, command) =>
        {
            Assert.That(command->type, Is.Not.EqualTo(DisableDeviceCommand.Type));
            return InputDeviceCommand.GenericFailure;
        });

        TouchSimulation.Enable();

        Assert.That(mouse.enabled, Is.False);
        Assert.That(pen.enabled, Is.False);

        InputSystem.QueueStateEvent(mouse, new MouseState
        {
            position = new Vector2(123, 234),
        }.WithButton(MouseButton.Left));
        InputSystem.Update();

        Assert.That(Touchscreen.current.touches[0].isInProgress, Is.True);
        Assert.That(Touchscreen.current.touches[0].position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
    }

    [Test]
    [Category("EnhancedTouch")]
    [TestCase(true)]
    [TestCase(false)]
    public void EnhancedTouch_ActiveTouchesGetCanceledOnFocusLoss_WithRunInBackgroundBeing(bool runInBackground)
    {
        runtime.runInBackground = runInBackground;

        BeginTouch(1, new Vector2(123, 456));

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Began));

        runtime.PlayerFocusLost();

        if (runInBackground)
        {
            // When running in the background, next update after focus loss sees touches cancelled
            // and update after that sees them gone.
            InputSystem.Update(InputUpdateType.Dynamic);
        }
        else
        {
            // When not running in the background, the same thing happens but only on focus gain.
            runtime.PlayerFocusGained();
            InputSystem.Update();
        }

        Assert.That(Touch.activeTouches, Has.Count.EqualTo(1));
        Assert.That(Touch.activeTouches[0].phase, Is.EqualTo(TouchPhase.Canceled));

        InputSystem.Update();

        Assert.That(Touch.activeTouches, Is.Empty);
    }
}
