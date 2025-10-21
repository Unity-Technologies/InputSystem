using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class InputRecorderTests : CoreTestsFixture
{
    [Test]
    [Category("Samples")]
    public void Samples_CanRecordAndReplayInputWithInputRecorder()
    {
        var go = new GameObject();
        var recorder = go.AddComponent<InputRecorder>();

        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        recorder.devicePath = "<Keyboard>";
        recorder.recordFrames = false;
        recorder.startRecordingWhenEnabled = true;

        Assert.That(recorder.captureIsRunning, Is.True);

        Press(keyboard.aKey);
        Press(mouse.leftButton);
        Release(keyboard.aKey);

        Assert.That(recorder.capture.eventCount, Is.EqualTo(2));

        var events = recorder.capture.ToArray();
        Assert.That(events[0].deviceId, Is.EqualTo(keyboard.deviceId));
        Assert.That(events[1].deviceId, Is.EqualTo(keyboard.deviceId));
        Assert.That(keyboard.aKey.ReadValueFromEvent(events[0]), Is.EqualTo(1).Within(0.0001));
        Assert.That(keyboard.aKey.ReadValueFromEvent(events[1]), Is.EqualTo(0).Within(0.0001));

        recorder.StopCapture();

        InputSystem.QueueStateEvent(keyboard, default(KeyboardState));
        InputSystem.QueueStateEvent(mouse, default(MouseState));
        InputSystem.Update();

        var keyboardAction = new InputAction(binding: "<Keyboard>/a");
        var mouseAction = new InputAction(binding: "<Mouse>/leftButton");

        keyboardAction.Enable();
        mouseAction.Enable();

        recorder.StartReplay();

        Assert.That(recorder.replayIsRunning, Is.True);
        Assert.That(recorder.replay, Is.Not.Null);
        Assert.That(recorder.replay.finished, Is.False);
        Assert.That(recorder.replay.position, Is.Zero);

        InputSystem.Update(); // Recorder only queues events.

        Assert.That(recorder.replayIsRunning, Is.False);
        Assert.That(recorder.replay, Is.Null);
        Assert.That(keyboardAction.triggered, Is.True);
        Assert.That(mouseAction.triggered, Is.False);
        Assert.That(keyboardAction.ReadValue<float>(), Is.EqualTo(0));
    }

    [Test]
    [Category("Samples")]
    public void Samples_CanRecordAndReplayInputWithInputRecorder_AndControlCaptureAndPlaybackViaButtons()
    {
        var go = new GameObject();
        var recorder = go.AddComponent<InputRecorder>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        recorder.recordButtonPath = "<Keyboard>/r";
        recorder.playButtonPath = "<Keyboard>/p";
        recorder.recordFrames = false; // Having to account for frame marker events here only complicates things.

        Assert.That(recorder.captureIsRunning, Is.False);
        Assert.That(recorder.replayIsRunning, Is.False);

        Press(keyboard.rKey);

        Assert.That(recorder.captureIsRunning, Is.True);
        Assert.That(recorder.replayIsRunning, Is.False);
        Assert.That(recorder.eventCount, Is.Zero);

        Release(keyboard.rKey);
        Press(keyboard.aKey);

        Assert.That(recorder.eventCount, Is.EqualTo(2));

        Press(keyboard.rKey);

        Assert.That(recorder.captureIsRunning, Is.False);
        Assert.That(recorder.replayIsRunning, Is.False);
        Assert.That(recorder.eventCount, Is.EqualTo(2));

        Release(keyboard.rKey);
        Press(keyboard.pKey);

        Assert.That(recorder.captureIsRunning, Is.False);
        Assert.That(recorder.replayIsRunning, Is.True);
        Assert.That(recorder.eventCount, Is.EqualTo(2));
    }
}
