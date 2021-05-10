// Case 1274997 - High Managed Stripping level makes these tests fail,  seems like test framework is not capable of
//                running multiple variations of UnityTest - and instead "No arguments were provided." error shows up.
//                On other hand there's no screen keyboard on standalone, so keep these tests only in Editor + Mobile for now.
#if UNITY_EDITOR || !UNITY_STANDALONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Android;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.iOS;
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
            ScreenKeyboard _keyboard;
            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.LinuxEditor)
            {
                _keyboard = runtime.screenKeyboard;
            }
            else
            {
                // When running on native platform, we always want to test real screen keyboard
                InputSystem.settings.screenKeyboardFactory = typeof(DefaultScreenKeyboardFactory);
                _keyboard = NativeInputRuntime.instance.screenKeyboard;
            }
            if (_keyboard == null)
                throw new Exception("No Screen Keyboard to test?");

            switch (Application.platform)
            {
#if !DISABLE_SCREEN_KEYBOARD
                case RuntimePlatform.IPhonePlayer:
                    Assert.AreEqual(_keyboard.GetType(), typeof(iOSScreenKeyboard)); break;
                case RuntimePlatform.Android:
                    Assert.AreEqual(_keyboard.GetType(), typeof(AndroidScreenKeyboard)); break;
#endif
                default:
                    Assert.AreEqual(_keyboard.GetType(), typeof(EmulatedScreenKeyboard)); break;
            }

            // Keep native logging enabled for more info
            _keyboard.logging = true;
            return _keyboard;
        }
    }

    IEnumerator Waiting()
    {
#if UNITY_EDITOR
        // WaitForEndOfFrame doesn't work in batch mode
        int startFrame = Time.frameCount;
        return new WaitUntil(() => Time.frameCount - startFrame >= 1);

#else
        yield return new WaitForEndOfFrame();
#endif
    }

    private IEnumerator ResetKeyboard()
    {
        return HideKeyboard();
    }

    private IEnumerator HideKeyboard()
    {
        if (keyboard.state != ScreenKeyboardState.Visible)
            yield break;

        keyboard.Hide();
        for (int i = 0; i < kFrameTimeout && keyboard.state == ScreenKeyboardState.Visible; i++)
            yield return Waiting();

        Assert.IsFalse(keyboard.state == ScreenKeyboardState.Visible, "Couldn't hide keyboard");
    }

    private IEnumerator ShowKeyboard(ScreenKeyboardCallbacks callbacks = default)
    {
        return ShowKeyboard(new ScreenKeyboardShowParams(), callbacks);
    }

    private IEnumerator ShowKeyboard(ScreenKeyboardShowParams showParams, ScreenKeyboardCallbacks callbacks = default)
    {
        Assert.IsTrue(keyboard.state != ScreenKeyboardState.Visible, "Expected keybard to be not visible");

        keyboard.Show(showParams, callbacks);
        for (int i = 0; i < kFrameTimeout && keyboard.state != ScreenKeyboardState.Visible; i++)
            yield return Waiting();
        Assert.AreEqual(ScreenKeyboardState.Visible, keyboard.state, "Couldn't show keyboard");
    }

    [UnityTest]
    public IEnumerator CheckShowHideOperations()
    {
        Console.WriteLine("Testable keyboard is " + keyboard.GetType().FullName);
        yield return ResetKeyboard();
        yield return ShowKeyboard();
        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator CheckStateCallback()
    {
        yield return ResetKeyboard();

        var stateCallbackInfo = new CallbackInfo<ScreenKeyboardState>(ScreenKeyboardState.Canceled);
        var callbacks = new ScreenKeyboardCallbacks
        {
            stateChanged = (state) =>
            {
                stateCallbackInfo.CallbackInvoked(state);
            }
        };

        yield return ShowKeyboard(callbacks);

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
        var callbacks = new ScreenKeyboardCallbacks
        {
            inputFieldTextChanged = (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);
            }
        };


        yield return ShowKeyboard(new ScreenKeyboardShowParams(){multiline = multiline, inputFieldHidden = inputFieldHidden}, callbacks);

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
        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);

        var callbacks = new ScreenKeyboardCallbacks
        {
            inputFieldSelectionChanged = (range) =>
            {
                selectionCallbackInfo.CallbackInvoked(range);
            },
            inputFieldTextChanged = (text) =>
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
            }
        };

        yield return ShowKeyboard(new ScreenKeyboardShowParams() { multiline = multiline, inputFieldHidden = inputFieldHidden}, callbacks);

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

        var callbacks = new ScreenKeyboardCallbacks
        {
            inputFieldSelectionChanged = (range) =>
            {
                selectionCallbackInfo.CallbackInvoked(range);
                keyboard.selection = new RangeInt(1, 0);
            }
        };

        yield return ShowKeyboard(new ScreenKeyboardShowParams { inputFieldHidden = inputFieldHidden }, callbacks);

        keyboard.inputFieldText = "Hello";

        // Note: Even if input field is hidden we want selection to behave normally
        //       Since the users of screen keyboard might be using the selection field for manually drawing cursor
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);

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
        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);

        var callbacks = new ScreenKeyboardCallbacks
        {
            inputFieldSelectionChanged = (range) =>
            {
                selectionCallbackInfo.CallbackInvoked(range);
            },
            inputFieldTextChanged = (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);;
            }
        };

        yield return ShowKeyboard(callbacks);

        Assert.AreEqual(new MyRangeInt(0, 0), (MyRangeInt)keyboard.selection);

        var targetText = "Hello";
        keyboard.inputFieldText = targetText;

        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(Time.frameCount, selectionCallbackInfo.Frame);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, selectionCallbackInfo.ThreadId);
        Assert.AreEqual(1, selectionCallbackInfo.CalledCount);

        // Assign inputFieldTextChanged, and see that setting selection doesn't trigger it
        keyboard.selection = new RangeInt(1, 0);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);

        // Calling selection shouldn't trigger inputFieldText callback
        Assert.AreEqual(1, inputFieldTextCallbackInfo.CalledCount);

        // Check what happens when selection start is out of bounds
        // Previous selection should be kept
        keyboard.selection = new RangeInt(targetText.Length + 1, 5);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);

        // Check what happens when selection length is out of bounds
        // Previous selection should be kept
        keyboard.selection = new RangeInt(targetText.Length - 1 , 2);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(2, selectionCallbackInfo.CalledCount);

        keyboard.selection = new RangeInt(targetText.Length, 0);
        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(3, selectionCallbackInfo.CalledCount);

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
            yield return Waiting();
        Assert.AreEqual(ScreenKeyboardState.Canceled, keyboard.state, "Couldn't hide keyboard using back button");
    }
}
#endif
