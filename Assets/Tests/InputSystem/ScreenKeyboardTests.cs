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

    public class CallbackInfo<T>
    {
        public T Data { private set; get; }
        public int Frame { private set; get; }
        public int ThreadId { private set; get; }

        public CallbackInfo()
        {
            Frame = -1;
            ThreadId = -1;
        }

        public CallbackInfo(T initialData)
        {
            Data = initialData;
            Frame = -1;
            ThreadId = -1;
        }

        public void CallbackInvoked(T data)
        {
            Data = data;
            Frame = Time.frameCount;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
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

    private IEnumerator HideKeyboard()
    {
        if (keyboard.status != ScreenKeyboardStatus.Done)
        {
            keyboard.Hide();
            for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Done; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(ScreenKeyboardStatus.Done, keyboard.status, "Couldn't hide keyboard");
        }
    }

    private IEnumerator ShowKeyboard()
    {
        return ShowKeyboard(new ScreenKeyboardShowParams());
    }

    private IEnumerator ShowKeyboard(ScreenKeyboardShowParams showParams)
    {
        Assert.IsTrue(keyboard.status != ScreenKeyboardStatus.Visible, "Expected keybard to be not visible");

        keyboard.Show(showParams);
        for (int i = 0; i < kFrameTimeout && keyboard.status != ScreenKeyboardStatus.Visible; i++)
            yield return new WaitForFixedUpdate();
        Assert.AreEqual(ScreenKeyboardStatus.Visible, keyboard.status, "Couldn't show keyboard");
    }

    // TODO:
    // Disable selection callbacks, when no input field is present. Since there's nothing to select
    // See that callbacks are not called when keyboard is not shown. ??? Do we really need this
    [UnityTest]
    public IEnumerator CheckShowHideOperations()
    {
        yield return HideKeyboard();
        yield return ShowKeyboard();
        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator CheckStateCallback()
    {
        yield return HideKeyboard();

        var stateCallbackInfo = new CallbackInfo<ScreenKeyboardStatus>(ScreenKeyboardStatus.Canceled);
        var stateCallback = new Action<ScreenKeyboardStatus>(
            (state) =>
            {
                stateCallbackInfo.CallbackInvoked(state);
            });
        keyboard.stateChanged += stateCallback;

        yield return ShowKeyboard();

        Assert.AreEqual(ScreenKeyboardStatus.Visible, stateCallbackInfo.Data);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, stateCallbackInfo.ThreadId);
        // Don't check frame, since when you call Show the keyboard can appear only in next frame

        yield return HideKeyboard();

        Assert.AreEqual(ScreenKeyboardStatus.Done, stateCallbackInfo.Data);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, stateCallbackInfo.ThreadId);
        keyboard.stateChanged -= stateCallback;
    }

    [UnityTest]
    public IEnumerator CheckInputFieldTextCallback()
    {
        yield return HideKeyboard();

        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);
        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);;
            });
        keyboard.inputFieldTextChanged += inputFieldCallback;

        yield return ShowKeyboard();

        Assert.AreEqual(string.Empty, keyboard.inputFieldText);

        var targetText = "Hello";
        keyboard.inputFieldText = targetText;

        Assert.AreEqual(targetText, inputFieldTextCallbackInfo.Data);
        Assert.AreEqual(Time.frameCount, inputFieldTextCallbackInfo.Frame);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, inputFieldTextCallbackInfo.ThreadId);

        keyboard.inputFieldTextChanged -= inputFieldCallback;

        yield return HideKeyboard();
    }

    [UnityTest]
    public IEnumerator CheckInputFieldText()
    {
        yield return HideKeyboard();
        var initiaText = "Placeholder";
        var targetText = "Hello";
        yield return ShowKeyboard(new ScreenKeyboardShowParams {initialText = initiaText });

        Assert.AreEqual(initiaText, keyboard.inputFieldText);
        keyboard.inputFieldText = targetText;
        Assert.AreEqual(targetText, keyboard.inputFieldText);

        yield return HideKeyboard();

        Assert.AreEqual(targetText, keyboard.inputFieldText);

        // TODO: multiline
    }

    [UnityTest]
    public IEnumerator CheckSelectionCallbacks()
    {
        yield return HideKeyboard();

        var selectionCallbackInfo = new CallbackInfo<MyRangeInt>(new MyRangeInt(0, 0));
        var selectionCallback = new Action<RangeInt>((range) => { selectionCallbackInfo.CallbackInvoked(range); });

        keyboard.selectionChanged += selectionCallback;

        yield return ShowKeyboard();

        Assert.AreEqual(new MyRangeInt(0, 0), (MyRangeInt)keyboard.selection);

        var targetText = "Hello";
        keyboard.inputFieldText = targetText;

        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), selectionCallbackInfo.Data);
        Assert.AreEqual(Time.frameCount, selectionCallbackInfo.Frame);
        Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, selectionCallbackInfo.ThreadId);

        //TODO: call selection directly, see that text callback is not called
        var inputFieldTextCallbackInfo = new CallbackInfo<string>(string.Empty);
        var inputFieldCallback = new Action<string>(
            (text) =>
            {
                inputFieldTextCallbackInfo.CallbackInvoked(text);;
            });
        keyboard.inputFieldTextChanged += inputFieldCallback;

        keyboard.selection = new RangeInt(1, 0);
        Assert.AreEqual(new MyRangeInt(1, 0), selectionCallbackInfo.Data);

        // Calling selection shouldn't trigger inputFieldText callback
        Assert.AreEqual(-1, inputFieldTextCallbackInfo.Frame);
        keyboard.inputFieldTextChanged -= inputFieldCallback;
        keyboard.selectionChanged -= selectionCallback;

        // TODO: check selection out of bounds behavior

        keyboard.selection = new RangeInt(targetText.Length, 0);

        yield return HideKeyboard();

        Assert.AreEqual(new MyRangeInt(targetText.Length, 0), (MyRangeInt)keyboard.selection);
    }
}
