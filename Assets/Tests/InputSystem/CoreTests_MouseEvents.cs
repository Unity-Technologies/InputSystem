using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;
using UnityEngineInternal.Input;


partial class CoreTests
{
    // resets MouseEvents script to default state
    public override void TearDown()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();
        SetMouse(mouse, Vector2.zero, 0f);
        SetPen(pen, Vector2.zero, 0f);
        base.TearDown();
    }

    #region Mouse
    [UnityTest]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseDown()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMousEventTestTwo>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMousEventTestTwo>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMousEventTestTwo>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMousEventTestTwo>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMouseEventsTest>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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

        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.AddComponent<OnMousEventTestTwo>();
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
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
