#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;
using UnityEngineInternal.Input;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;


partial class CoreTests
{
    // resets MouseEvents script to default state
    public override void TearDown()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();
        var touchscreen = InputSystem.AddDevice<Touchscreen>();
        SetMouse(mouse, Vector2.zero, 0f);
        SetPen(pen, Vector2.zero, 0f);
        SetTouch(touchscreen, Vector2.zero, TouchPhase.Ended);
        base.TearDown();
    }

    internal GameObject SetUpScene()
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
        return gameObject;
    }

    #region Mouse
    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseDown()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(vec.x, vec.y), 1f);

        yield return null;

        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseDown event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseUp()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 2)), "No MouseUp event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseUpAsButton()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseUpAsButton event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseDrag()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x + 1f, vec.y), 1f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 3)), "No MouseDrag event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseEnterAndMouseExit()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(0, 0), 0f);
        yield return null;
        SetMouse(mouse, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.green), "No MouseEnter event received.");

        SetMouse(mouse, new Vector2(0, 0), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.red), "No MouseExit event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseOver()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetMouse(mouse, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.blue), "No MouseOver event received.");
    }

    // we need to use the NativeInputRuntime to queue the events in order to get the input events in native
    // code, where the mouse events are processed.
    unsafe void SetMouse(Mouse mouse, Vector2 pos, float pressed)
    {
        using (StateEvent.From(mouse, out var eventPtr))
        {
            eventPtr.time = InputState.currentTime;
            mouse.position.WriteValueIntoEvent(pos, eventPtr);
            mouse.leftButton.WriteValueIntoEvent(pressed, eventPtr);
            NativeInputRuntime.instance.QueueEvent(eventPtr);
        }
    }

    #endregion

    #region Pen

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseDown()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(vec.x, vec.y), 1f);

        yield return null;

        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseDown event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseUp()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetPen(pen, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 2)), "No MouseUp event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseUpAsButton()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetPen(pen, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseUpAsButton event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseDrag()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        SetPen(pen, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetPen(pen, new Vector2(vec.x, vec.y), 1f);
        yield return null;
        SetPen(pen, new Vector2(vec.x + 1f, vec.y), 1f);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 3)), "No MouseDrag event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseEnterAndMouseExit()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(0, 0), 0f);
        yield return null;
        SetPen(pen, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.green), "No MouseEnter event received.");

        SetPen(pen, new Vector2(0, 0), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.red), "No MouseExit event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator PenEvents_CanReceiveOnMouseOver()
    {
        var pen = InputSystem.AddDevice<Pen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetPen(pen, new Vector2(vec.x, vec.y), 0f);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.blue), "No MouseOver event received.");
    }

    // we need to use the NativeInputRuntime to queue the events in order to get the input events in native
    // code, where the pen events are processed.
    unsafe void SetPen(Pen pen, Vector2 pos, float pressed)
    {
        using (StateEvent.From(pen, out var eventPtr))
        {
            eventPtr.time = InputState.currentTime;
            pen.position.WriteValueIntoEvent(pos, eventPtr);
            pen.tip.WriteValueIntoEvent(pressed, eventPtr);
            NativeInputRuntime.instance.QueueEvent(eventPtr);
        }
    }

    #endregion

    #region Touch

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseDown()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Began);

        yield return null;

        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseDown event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseUp()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Began);
        yield return null;
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Ended);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 2)), "No MouseUp event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseUpAsButton()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Began);
        yield return null;
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Ended);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseUpAsButton event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseDrag()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Ended);
        yield return null;
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Began);
        yield return null;
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.Moved);
        yield return null;
        SetTouch(touch, new Vector2(vec.x + 1f, vec.y), TouchPhase.Moved);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 3)), "No MouseDrag event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseEnterAndMouseExit()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(0, 0), TouchPhase.Ended);
        yield return null;
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.None);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.green), "No MouseEnter event received.");

        SetTouch(touch, new Vector2(0, 0), TouchPhase.None);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.red), "No MouseExit event received.");
    }

    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator TouchEvents_CanReceiveOnMouseOver()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();

        var gameObject = SetUpScene();
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        SetTouch(touch, new Vector2(vec.x, vec.y), TouchPhase.None);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.blue), "No MouseOver event received.");
    }

    private long m_eventSize = UnsafeUtility.SizeOf<StateEvent>() + (uint)UnsafeUtility.SizeOf<TouchState>() - StateEvent.kStateDataSizeToSubtract;

    // we need to use the NativeInputRuntime to queue the events in order to get the input events in native
    // code, where the touch events are processed.
    unsafe void SetTouch(Touchscreen touch, Vector2 pos, TouchPhase phase)
    {
        var state = new TouchState
        {
            touchId = 1,
            phase = phase,
            position = pos,
            delta = default,
            pressure = 1f,
            displayIndex = 0,
        };

        var stateEvent =
            new StateEvent
        {
            baseEvent = new InputEvent(StateEvent.Type, (int)m_eventSize, touch.deviceId, InputState.currentTime),
            stateFormat = state.format
        };

        var ptr = stateEvent.stateData;
        UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref state), m_eventSize);

        NativeInputRuntime.instance.QueueEvent((InputEvent*)UnsafeUtility.AddressOf(ref stateEvent));
    }

    #endregion
}

internal class OnMouseEventsTest : MonoBehaviour
{
    private void OnMouseDown()
    {
        gameObject.transform.position = new Vector3(0, 0, 1);
    }

    private void OnMouseUp()
    {
        gameObject.transform.position = new Vector3(0, 0, 2);
    }

    private void OnMouseDrag()
    {
        gameObject.transform.position = new Vector3(0, 0, 3);
    }

    private void OnMouseEnter()
    {
        gameObject.GetComponent<Renderer>().material.color = Color.green;
    }

    private void OnMouseExit()
    {
        gameObject.GetComponent<Renderer>().material.color = Color.red;
    }
}

internal class OnMousEventTestTwo : MonoBehaviour
{
    private void OnMouseUpAsButton()
    {
        gameObject.transform.position = new Vector3(0, 0, 1);
    }

    private void OnMouseOver()
    {
        gameObject.GetComponent<Renderer>().material.color = Color.blue;
    }

    private void OnMouseDrag()
    {
        gameObject.transform.position = new Vector3(0, 0, 3);
    }
}
#endif
