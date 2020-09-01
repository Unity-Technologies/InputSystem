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

/// <summary>
/// Behavioral tests for Screen Keyboard
/// These tests ensure that screen keyboard is behaving in the same manner on all platforms
/// Most likely, some OS' might have a different behavior, in that case if possible, the backend must simulate the behavior described in these tests
/// </summary>
public class ScreenKeyboardTests : InputTestFixture
{
    static ScreenKeyboard s_TargetKeyboard;
    const int kFrameTimeout = 30;

    public class CallbackInfo<T>
    {
        public T Data { private set; get; }
        public int Frame { private set; get; }
        public int ThreadId { private set; get; }
        public int CalledCount { private set; get; }

        public CallbackInfo()
        {
            Frame = -1;
            ThreadId = -1;
            CalledCount = 0;
        }

        public CallbackInfo(T initialData)
        {
            Data = initialData;
            Frame = -1;
            ThreadId = -1;
            CalledCount = 0;
        }

        public void CallbackInvoked(T data)
        {
            Data = data;
            Frame = Time.frameCount;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            CalledCount++;
        }
    }

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
        // If there's a failure in test, the callbacks might not be properly cleaned up
        // So it's easier to clean them up before starting test
        keyboard.ClearListeners();
        return HideKeyboard();
    }

    private IEnumerator HideKeyboard()
    {
        if (keyboard.state != ScreenKeyboardState.Visible)
            yield break;

        keyboard.Hide();
        for (int i = 0; i < kFrameTimeout && keyboard.state == ScreenKeyboardState.Visible; i++)
            yield return new WaitForFixedUpdate();
        Assert.IsFalse(keyboard.state == ScreenKeyboardState.Visible, "Couldn't hide keyboard");
    }

    private IEnumerator ShowKeyboard()
    {
        return ShowKeyboard(new ScreenKeyboardShowParams());
    }

    private IEnumerator ShowKeyboard(ScreenKeyboardShowParams showParams)
    {
        Assert.IsTrue(keyboard.state != ScreenKeyboardState.Visible, "Expected keybard to be not visible");

        keyboard.Show(showParams);
        for (int i = 0; i < kFrameTimeout && keyboard.state != ScreenKeyboardState.Visible; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardState.Visible, keyboard.state, "Couldn't show keyboard");
    }

    [UnityTest]
    public IEnumerator CheckShowHideOperations()
    {
        yield return ResetKeyboard();
        yield return ShowKeyboard();
        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator CheckStateCallback()
    {
        yield return ResetKeyboard();

        var stateCallbackInfo = new CallbackInfo<ScreenKeyboardState>(ScreenKeyboardState.Canceled);
        var stateCallback = new Action<ScreenKeyboardState>(
            (state) =>
            {
                stateCallbackInfo.CallbackInvoked(state);
            });
        keyboard.stateChanged += stateCallback;

        yield return ShowKeyboard();

        Assert.AreEqual(ScreenKeyboardState.Visible, stateCallbackInfo.Data);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, stateCallbackInfo.ThreadId);
        Assert.AreEqual(1, stateCallbackInfo.CalledCount);
        // Don't check frame, since when you call Show the keyboard can appear only in next frame

        yield return HideKeyboard();

        Assert.AreEqual(ScreenKeyboardState.Done, stateCallbackInfo.Data);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, stateCallbackInfo.ThreadId);
        Assert.AreEqual(2, stateCallbackInfo.CalledCount);
    }

    [UnityTest]
    public IEnumerator CheckInputFieldTextCallback([Values(true, false)] bool multiline, [Values(true, false)] bool inputFieldHidden)
    {
        yield return ResetKeyboard();

        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);
        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);
            });
        keyboard.inputFieldTextChanged += inputFieldCallback;
        yield return ShowKeyboard(new ScreenKeyboardShowParams(){multiline = multiline, inputFieldHidden = inputFieldHidden});

        Assert.AreEqual(string.Empty, keyboard.inputFieldText);

        var targetText = "Hello";
        keyboard.inputFieldText = targetText;

        Assert.AreEqual(targetText, inputFieldTextCallbackInfo.Data);
        Assert.AreEqual(Time.frameCount, inputFieldTextCallbackInfo.Frame);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, inputFieldTextCallbackInfo.ThreadId);
        Assert.AreEqual(1, inputFieldTextCallbackInfo.CalledCount);

        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator ChangeTextInsideInputFieldCallback([Values(true, false)] bool multiline, [Values(true, false)] bool inputFieldHidden)
    {
        yield return ResetKeyboard();

        var selectionCallbackInfo = new CallbackInfo<MyRangeInt>(new MyRangeInt(0, 0));
        var selectionCallback = new Action<RangeInt>((range) => { selectionCallbackInfo.CallbackInvoked(range); });

        keyboard.selectionChanged += selectionCallback;
        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);
        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);
                if (text.Equals("12345"))
                {
                    // Change to text with same length
                    keyboard.inputFieldText = "11111";
                }
                else if (text.Equals("11111"))
                {
                    // Change to text with different length
                    keyboard.inputFieldText = "123456";
                }
                else
                {
                    // Change to same text, this shouldn't trigger a callback, since text didn't change
                    keyboard.inputFieldText = text;
                }
            });
        keyboard.inputFieldTextChanged += inputFieldCallback;

        yield return ShowKeyboard(new ScreenKeyboardShowParams() { multiline = multiline, inputFieldHidden = inputFieldHidden});

        var targetText = "12345";
        keyboard.inputFieldText = targetText;
        targetText = "123456";

        Assert.AreEqual(targetText, inputFieldTextCallbackInfo.Data);
        Assert.AreEqual(targetText, keyboard.inputFieldText);
        Assert.AreEqual(3, inputFieldTextCallbackInfo.CalledCount);
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);

        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator ChangeSelectionInsideSelectionCallback([Values(true, false)] bool inputFieldHidden)
    {
        yield return ResetKeyboard();

        var selectionCallbackInfo = new CallbackInfo<MyRangeInt>(new MyRangeInt(0, 0));
        var selectionCallback = new Action<RangeInt>((range) =>
        {
            selectionCallbackInfo.CallbackInvoked(range);
            keyboard.selection = new RangeInt(1, 0);
        });

        keyboard.selectionChanged += selectionCallback;
        yield return ShowKeyboard(new ScreenKeyboardShowParams { inputFieldHidden = inputFieldHidden });

        var targetText = "Hello";
        keyboard.inputFieldText = "Hello";
        // For hidden input fields, yuo cannot select a text
        if (inputFieldHidden)
        {
            Assert.AreEqual(1, selectionCallbackInfo.CalledCount);
            Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        }
        else
        {
            Assert.AreEqual(2, selectionCallbackInfo.CalledCount);
            Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);
        }
        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator CheckInputFieldText([Values(true, false)] bool multiline, [Values(true, false)] bool inputFieldHidden)
    {
        yield return ResetKeyboard();
        var initiaText = "Placeholder";
        var targetText = "Hello";
        yield return ShowKeyboard(new ScreenKeyboardShowParams {initialText = initiaText, multiline =  multiline, inputFieldHidden = inputFieldHidden});

        Assert.AreEqual(initiaText, keyboard.inputFieldText);
        keyboard.inputFieldText = targetText;
        Assert.AreEqual(targetText, keyboard.inputFieldText);

        yield return HideKeyboard();

        Assert.AreEqual(targetText, keyboard.inputFieldText);
    }

    [UnityTest]
    public IEnumerator CheckInputFieldTextWithReallyLongText([Values(true, false)] bool multiline, [Values(true, false)] bool inputFieldHidden)
    {
        yield return ResetKeyboard();
        yield return ShowKeyboard(new ScreenKeyboardShowParams { multiline = multiline, inputFieldHidden = inputFieldHidden });

        string targetText = "";

        for (int i = 0; i < 5; i++)
        {
            for (int x = 33; x < 127; x++)
            {
                targetText += (char)(x);
            }
            targetText += " ";

            keyboard.inputFieldText = targetText;
            Assert.AreEqual(targetText, keyboard.inputFieldText);
        }

        yield return HideKeyboard();

        Assert.AreEqual(targetText, keyboard.inputFieldText);
    }

    [UnityTest]
    public IEnumerator CheckSelectionCallbacks()
    {
        yield return ResetKeyboard();
        var selectionCallbackInfo = new CallbackInfo<MyRangeInt>(new MyRangeInt(0, 0));
        var selectionCallback = new Action<RangeInt>((range) =>
        {
            selectionCallbackInfo.CallbackInvoked(range);
        });

        keyboard.selectionChanged += selectionCallback;

        yield return ShowKeyboard();

        Assert.AreEqual(new MyRangeInt(0, 0), (MyRangeInt)keyboard.selection);

        var targetText = "Hello";
        keyboard.inputFieldText = targetText;

        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(Time.frameCount, selectionCallbackInfo.Frame);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, selectionCallbackInfo.ThreadId);
        Assert.AreEqual(1, selectionCallbackInfo.CalledCount);

        // Assign inputFieldTextChanged, and see that setting selection doesn't trigger it
        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);
        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);;
            });
        keyboard.inputFieldTextChanged += inputFieldCallback;

        keyboard.selection = new RangeInt(1, 0);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);

        // Calling selection shouldn't trigger inputFieldText callback
        Assert.AreEqual(0, inputFieldTextCallbackInfo.CalledCount);

        // TODO: ANDROID, ignores the setting and keeps the old one
        // Check what happens when selection start is out of bounds
        keyboard.selection = new RangeInt(targetText.Length + 1, 5);
        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(3, selectionCallbackInfo.CalledCount);

        // Check what happens when selection length is out of bounds
        keyboard.selection = new RangeInt(targetText.Length - 1 , 2);
        Assert.AreEqual(new MyRangeInt(targetText.Length - 1, 1), selectionCallbackInfo.Data);
        Assert.AreEqual(4, selectionCallbackInfo.CalledCount);

        keyboard.selection = new RangeInt(targetText.Length, 0);
        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(5, selectionCallbackInfo.CalledCount);

        yield return HideKeyboard();

        // Check that selection persists even after hiding the keyboard
        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), (MyRangeInt)keyboard.selection);
    }

    [UnityTest]
    public IEnumerator CheckBackButtonFunctionality([Values(true, false)] bool multiline, [Values(true, false)] bool inputFieldHidden)
    {
        int keyCode = -1;
        bool supported = true;
#if UNITY_EDITOR
        keyCode = (int)KeyCode.Escape;
#elif UNITY_ANDROID
        const int kBackButton = 4;
        keyCode = kBackButton;
#else
        supported = false;
#endif
        if (!supported)
        {
            Console.WriteLine("CheckBackButtonFunctionality is not supported on this platform");
            yield break;
        }
        yield return ResetKeyboard();

        yield return ShowKeyboard(new ScreenKeyboardShowParams { multiline = multiline, inputFieldHidden = inputFieldHidden });
        keyboard.SimulateKeyEvent(keyCode);
        for (int i = 0; i < kFrameTimeout && keyboard.state != ScreenKeyboardState.Canceled; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardState.Canceled, keyboard.state, "Couldn't hide keyboard using back button");
    }
}
