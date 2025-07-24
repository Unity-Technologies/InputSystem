using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;
using UnityEngineInternal.Input;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;


partial class CoreTests
{
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
        SetMouse(mouse, new Vector2(vec.x, vec.y));

        yield return null;

        Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(0, 0, 1)), "No MouseDown event received.");
    }

    unsafe void SetMouse(Mouse mouse, Vector2 pos, float pressed = 1f)
    {
        using (StateEvent.From(mouse, out var eventPtr))
        {
            eventPtr.time = InputState.currentTime;
            mouse.position.WriteValueIntoEvent(pos, eventPtr);
            mouse.leftButton.WriteValueIntoEvent(pressed, eventPtr);
            NativeInputRuntime.instance.QueueEvent(eventPtr);
        }
    }
}

public class OnMouseEventsTest : MonoBehaviour
{
    void OnMouseDown()
    {
        gameObject.transform.position = new Vector3(0, 0, 1);
    }
}
