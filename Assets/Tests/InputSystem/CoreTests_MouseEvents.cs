#if UNITY_INPUTSYSTEM_SUPPORTS_MOUSE_SCRIPT_EVENTS
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;

partial class CoreTests
{
    private GameObject SetUpScene(string pointerType, out Pointer pointer)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.transform.position = Vector3.zero;
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2f);
        camera.tag = "MainCamera";
        pointer = (Pointer)InputSystem.s_Manager.AddDevice(pointerType);
        InputSystem.AddDevice(pointer);
        return gameObject;
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Touchscreen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseDown(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);

        Set(pointer, new Vector2(vec.x, vec.y), true, false);
        yield return null;

        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseDown event received.");
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Touchscreen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseUp(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Set(pointer, new Vector2(vec.x, vec.y), true, false);
        yield return null;
        Set(pointer, vec, false, true);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 2)), "No MouseUp event received.");
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Touchscreen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseUpAsButton(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Set(pointer, new Vector2(vec.x, vec.y), true, false);
        yield return null;
        Set(pointer, vec, false, true);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseUpAsButton event received.");
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Touchscreen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseDrag(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Set(pointer, new Vector2(vec.x, vec.y), false, false);
        yield return null;
        Set(pointer, new Vector2(vec.x, vec.y), true, false);
        yield return null;
        Set(pointer, new Vector2(vec.x + 1f, vec.y), false, false);
        yield return null;
        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 3)), "No MouseDrag event received.");
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseEnterAndMouseExit(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMouseEventsTest>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Set(pointer, new Vector2(0, 0), false, false);
        yield return null;
        Set(pointer, new Vector2(vec.x, vec.y), false, false);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.green), "No MouseEnter event received.");

        Set(pointer, new Vector2(0, 0), false, false);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.red), "No MouseExit event received.");
    }

    [UnityTest]
    [TestCase("Pen", ExpectedResult = null)]
    [TestCase("Mouse", ExpectedResult = null)]
    [Category("MouseEvents")]
    public IEnumerator MouseEvents_CanReceiveOnMouseOver(string pointerType)
    {
        var gameObject = SetUpScene(pointerType, out var pointer);
        gameObject.AddComponent<OnMousEventTestTwo>();
        var vec = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Set(pointer, new Vector2(vec.x, vec.y), false, false);
        yield return null;
        Assert.That(gameObject.GetComponent<Renderer>().material.color, Is.EqualTo(Color.blue), "No MouseOver event received.");
    }

    void Set(Pointer pointer, Vector2 point, bool pressed, bool released)
    {
        // Touch needs special handling in InputTestFixture
        if (pointer is Touchscreen touchscreen)
        {
            if (pressed)
                BeginTouch(0, point, false, touchscreen);
            else if (released)
                EndTouch(0, point, delta: default, false, touchscreen);
            else
                MoveTouch(0, point, delta: default, false, touchscreen);
            return;
        }
        // all other Pointer, e.g. Mouse, Pen...
        Move(pointer.position, point);
        if (pressed)
            Press(pointer.press);
        else if (released)
            Release(pointer.press);
    }
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
