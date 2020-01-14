using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputRecorderTests : InputTestFixture
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

        var keyboardAction = new InputAction(binding: "<Keyboard>/a");
        var mouseAction = new InputAction(binding: "<Mouse>/leftButton");

        keyboardAction.Enable();
        mouseAction.Enable();

        recorder.StartReplay();
        InputSystem.Update(); // Recorder only queues events.

        Assert.That(keyboardAction.triggered, Is.True);
        Assert.That(mouseAction.triggered, Is.False);
        Assert.That(keyboardAction.ReadValue<float>(), Is.EqualTo(0));
        Assert.That(recorder.replay, Is.Not.Null);
        Assert.That(recorder.replay.finished, Is.True);
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
