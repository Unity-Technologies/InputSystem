using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.UI;

////TODO: app focus handling
////TODO: send IUpdateSelectedHandler.OnUpdateSelected event

public class UITests : InputTestFixture
{
    // Set up a UIActionInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TDO_Actions_CanDriveUI()
    {
        // Create devices.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        // Set up GameObject with EventSystem.
        var systemObject = new GameObject();
        var eventSystem = systemObject.AddComponent<TestEventSystem>();
        var uiModule = systemObject.AddComponent<UIActionInputModule>();
        eventSystem.UpdateModules();
        eventSystem.InvokeUpdate(); // Initial update only sets current module.

        // Set up canvas on which we can perform raycasts.
        var canvasObject = new GameObject();
        var canvas = canvasObject.AddComponent<Canvas>();
        var cameraObject = new GameObject();
        var camera = cameraObject.AddComponent<Camera>();
        canvas.worldCamera = camera;
        camera.pixelRect = new Rect(0, 0, 640, 480);

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject();
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        var parentReceiver = parentGameObject.AddComponent<UICallbackReceiver>();
        var leftChildGameObject = new GameObject();
        var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
        var leftChildReceiver = leftChildGameObject.AddComponent<UICallbackReceiver>();
        var rightChildGameObject = new GameObject();
        var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
        var rightChildReceiver = rightChildGameObject.AddComponent<UICallbackReceiver>();

        parentTransform.SetParent(canvas.transform, worldPositionStays: false);
        leftChildTransform.SetParent(parentTransform, worldPositionStays: false);
        rightChildTransform.SetParent(parentTransform, worldPositionStays: false);

        // Parent occupies full space of canvas.
        parentTransform.sizeDelta = new Vector2(640, 480);

        // Left child occupies left half of parent.
        leftChildTransform.anchoredPosition = new Vector2(-(640 / 4), 0);
        leftChildTransform.sizeDelta = new Vector2(320, 480);

        // Right child occupies right half of parent.
        rightChildTransform.anchoredPosition = new Vector2(640 / 4, 0);
        rightChildTransform.sizeDelta = new Vector2(320, 480);

        // Create actions.
        var map = new InputActionMap();
        var pointAction = map.AddAction("point");
        var moveAction = map.AddAction("move");
        var submitAction = map.AddAction("submit");
        var cancelAction = map.AddAction("cancel");
        var leftClickAction = map.AddAction("leftClick");
        var rightClickAction = map.AddAction("rightClick");
        var middleClickAction = map.AddAction("middleClick");

        // Create bindings.
        pointAction.AddBinding(mouse.position);
        moveAction.AddBinding(gamepad.leftStick);
        submitAction.AddBinding(gamepad.buttonSouth);
        cancelAction.AddBinding(gamepad.buttonEast);
        cancelAction.AddBinding(keyboard.escapeKey);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.point = new InputActionProperty(pointAction);
        uiModule.move = new InputActionProperty(moveAction);
        uiModule.submit = new InputActionProperty(submitAction);
        uiModule.cancel = new InputActionProperty(cancelAction);
        uiModule.leftClick = new InputActionProperty(leftClickAction);
        uiModule.middleClick = new InputActionProperty(middleClickAction);
        uiModule.rightClick = new InputActionProperty(rightClickAction);

        // Enable the whole thing.
        map.Enable();

        // Move mouse over left child.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100)});
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanDriveUIFromGamepads()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanDriveUIFromJoysticks()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanDriveUIFromTouches()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanDriveUIFromMouseAndKeyboard()
    {
        Assert.Fail();
    }

    private enum EventType
    {
        Click,
        Down,
        Up,
        Enter,
        Exit,
        Move,
        Select,
        Deselect,
    }

    private class UICallbackReceiver : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler,
        IPointerExitHandler, IPointerUpHandler, IMoveHandler, ISelectHandler, IDeselectHandler
    {
        public struct Event
        {
            public EventType type;
            public BaseEventData data;

            public Event(EventType type, BaseEventData data)
            {
                this.type = type;
                this.data = data;
            }
        }

        public List<Event> events = new List<Event>();

        public void OnPointerClick(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Click,
                ClonePointerEventData(eventData)));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Down,
                ClonePointerEventData(eventData)));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Enter,
                ClonePointerEventData(eventData)));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Exit,
                ClonePointerEventData(eventData)));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Up,
                ClonePointerEventData(eventData)));
        }

        public void OnMove(AxisEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnSelect(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Select, null));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Deselect, null));
        }

        private PointerEventData ClonePointerEventData(PointerEventData eventData)
        {
            return new PointerEventData(EventSystem.current)
            {
                pointerId = eventData.pointerId,
                position = eventData.position,
                button = eventData.button,
                clickCount = eventData.clickCount,
                clickTime = eventData.clickTime,
                eligibleForClick = eventData.eligibleForClick,
                delta = eventData.delta,
                scrollDelta = eventData.scrollDelta,
                dragging = eventData.dragging,
                hovered = eventData.hovered.Select(x => x).ToList(),
                pointerDrag = eventData.pointerDrag,
                pointerEnter = eventData.pointerEnter,
                pointerPress = eventData.pointerPress,
                pressPosition = eventData.pressPosition,
                pointerCurrentRaycast = eventData.pointerCurrentRaycast,
                pointerPressRaycast = eventData.pointerPressRaycast,
                rawPointerPress = eventData.rawPointerPress,
                useDragThreshold = eventData.useDragThreshold,
            };
        }
    }

    private class TestEventSystem : EventSystem
    {
        public void InvokeUpdate()
        {
            current = this; // Needs to be current to be allowed to update.
            Update();
        }
    }
}
