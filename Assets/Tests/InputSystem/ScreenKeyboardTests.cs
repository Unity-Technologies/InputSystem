using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

public class ScreenKeyboardTests : InputTestFixture
{
    static ScreenKeyboard s_TargetKeyboard;
    const int kFrameTimeout = 30;

    // Workaround RangeInt not having ToString function
    private struct MyRangeInt
    {
        public int start;
        public int length;

        public static implicit operator MyRangeInt(RangeInt range)
        {
            return new MyRangeInt(range.start, range.length);
        }

        public MyRangeInt(int start, int length)
        {
            this.start = start;
            this.length = length;
        }

        public override string ToString()
        {
            return $"{start}, {length}";
        }
    }

    ScreenKeyboard keyboard
    {
        get
        {
            if (s_TargetKeyboard == null)
            {
#if UNITY_EDITOR
                s_TargetKeyboard = runtime.screenKeyboard;
                Assert.AreEqual(s_TargetKeyboard.GetType(), typeof(FakeScreenKeyboard));
#else
                s_TargetKeyboard = NativeInputRuntime.instance.screenKeyboard;
#if UNITY_ANDROID
                Assert.AreEqual(s_TargetKeyboard.GetType(), typeof(UnityEngine.InputSystem.Android.AndroidScreenKeyboard));
#elif UNITY_IOS
                Assert.AreEqual(s_TargetKeyboard.GetType(), typeof(UnityEngine.InputSystem.iOS.iOSScreenKeyboard));
#endif
#endif
                if (s_TargetKeyboard == null)
                    throw new Exception("No Screen Keyboard to test?");
                Console.WriteLine($"Testable Keyboards is: {s_TargetKeyboard.GetType().FullName}");
            }
            return s_TargetKeyboard;
        }
    }

    private IEnumerator ResetKeyboard()
    {
        if (keyboard.status != ScreenKeyboardStatus.Done)
        {
            keyboard.Hide();
            for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Done; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status, "Couldn't reset keyboard");
        }
    }

    // TODO:
    // See if callbacks are received at the same frame, callback count
    // Disable selection callbacks, when no input field is present
    // Return selection 0, 0, whhen inmput field is not present
    // See that callbacks are not called when keyboard is not shown
    [UnityTest]
    public IEnumerator CheckShowHideOperations()
    {
        yield return ResetKeyboard();

        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);

        keyboard.Show();

        // Show, Hide are not immediate operations, it might take some time until keyboard becomes visible
        for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Visible; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);

        keyboard.Hide();
        for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Done; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
    }

    [UnityTest]
    public IEnumerator CheckCallbacks()
    {
        yield return ResetKeyboard();

        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
        ScreenKeyboardStatus stateFromCallback = ScreenKeyboardStatus.Canceled;
        int stateCallbackFrame = -1;
        int statedCallbackThreadId = -1;
        var stateCallback = new Action<ScreenKeyboardStatus>(
            (state) =>
            {
                stateFromCallback = state;
                stateCallbackFrame = Time.frameCount;
                statedCallbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });
        keyboard.stateChanged += stateCallback;

        string textFromCallback = string.Empty;
        int inputFieldTextChangedCallbackFrame = -1;
        int inputFieldTextChangedCallbackThreadId = -1;

        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                textFromCallback = text;
                inputFieldTextChangedCallbackFrame = Time.frameCount;
                inputFieldTextChangedCallbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });


        keyboard.inputFieldTextChanged += inputFieldCallback;
        MyRangeInt selectionRange = new MyRangeInt(0, 0);
        int selectionChangedCallbackFrame = -1;
        int selectionChangedCallbackThreadId = -1;

        var selectionCallback = new Action<RangeInt>((range) =>
        {
            selectionRange = range;
            selectionChangedCallbackFrame = Time.frameCount;
            selectionChangedCallbackThreadId = Thread.CurrentThread.ManagedThreadId;
        });

        keyboard.selectionChanged += selectionCallback;

        keyboard.Show();
        for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Visible; i++)
            yield return new WaitForFixedUpdate();

        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status);
        Assert.AreEqual(string.Empty, keyboard.inputFieldText);
        Assert.AreEqual(new MyRangeInt(0, 0), (MyRangeInt)keyboard.selection);
        Assert.AreEqual(ScreenKeyboardStatus.Visible, stateFromCallback);
        Assert.AreEqual(statedCallbackThreadId, Thread.CurrentThread.ManagedThreadId);


        var targetText = "Hello";
        keyboard.inputFieldText = targetText;
        Assert.AreEqual(textFromCallback, targetText);
        Assert.AreEqual(inputFieldTextChangedCallbackFrame, Time.frameCount);
        Assert.AreEqual(inputFieldTextChangedCallbackThreadId, Thread.CurrentThread.ManagedThreadId);

        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionRange);
        Assert.AreEqual(selectionChangedCallbackFrame, Time.frameCount);
        Assert.AreEqual(selectionChangedCallbackThreadId, Thread.CurrentThread.ManagedThreadId);

        keyboard.stateChanged -= stateCallback;
        keyboard.inputFieldTextChanged -= inputFieldCallback;
        keyboard.selectionChanged -= selectionCallback;

        keyboard.Hide();
        for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Done; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status);
    }
}
