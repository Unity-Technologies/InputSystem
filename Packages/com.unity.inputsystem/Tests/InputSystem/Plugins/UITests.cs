using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

////TODO: app focus handling
////TODO: send IUpdateSelectedHandler.OnUpdateSelected event

public class UITests : InputTestFixture
{
    // Set up a UIActionInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    [UnityTest]
    [Category("Actions")]
    public IEnumerator Actions_CanDriveUI()
    {
        // Create devices.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        // Set up GameObject with EventSystem.
        var systemObject = new GameObject("System");
        var eventSystem = systemObject.AddComponent<TestEventSystem>();
        var uiModule = systemObject.AddComponent<UIActionInputModule>();
        eventSystem.UpdateModules();
        eventSystem.InvokeUpdate(); // Initial update only sets current module.

        // Set up canvas on which we can perform raycasts.
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvasObject.AddComponent<GraphicRaycaster>();
        var cameraObject = new GameObject("Camera");
        var camera = cameraObject.AddComponent<Camera>();
        canvas.worldCamera = camera;
        camera.pixelRect = new Rect(0, 0, 640, 480);

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject("Parent");
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        parentGameObject.AddComponent<UICallbackReceiver>();
        var leftChildGameObject = new GameObject("Left Child");
        var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
        leftChildGameObject.AddComponent<Image>();
        var leftChildReceiver = leftChildGameObject.AddComponent<UICallbackReceiver>();
        var rightChildGameObject = new GameObject("Right Child");
        var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
        rightChildGameObject.AddComponent<Image>();
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
        var scrollAction = map.AddAction("scroll");

        // Create bindings.
        pointAction.AddBinding(mouse.position);
        leftClickAction.AddBinding(mouse.leftButton);
        rightClickAction.AddBinding(mouse.rightButton);
        middleClickAction.AddBinding(mouse.middleButton);
        scrollAction.AddBinding(mouse.scroll);
        moveAction.AddBinding(gamepad.leftStick);
        submitAction.AddBinding(gamepad.buttonSouth);
        cancelAction.AddBinding(gamepad.buttonEast);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.point = new InputActionProperty(pointAction);
        uiModule.move = new InputActionProperty(moveAction);
        uiModule.submit = new InputActionProperty(submitAction);
        uiModule.cancel = new InputActionProperty(cancelAction);
        uiModule.leftClick = new InputActionProperty(leftClickAction);
        uiModule.middleClick = new InputActionProperty(middleClickAction);
        uiModule.rightClick = new InputActionProperty(rightClickAction);
        uiModule.scrollWheel = new InputActionProperty(scrollAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Move mouse over left child.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100)});
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check basic down/up
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = (ushort)(1 << (int)MouseState.Button.Left) });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = (ushort)0 });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(4));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
        Assert.That(leftChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check down and drag
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = (ushort)(1 << (int)MouseState.Button.Right) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Move to new location on left child
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(100, 200));
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Move children
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(400, 200));
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Exit));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        rightChildReceiver.Reset();

        // Release button
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 200), buttons = (ushort)0 });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Up));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.EndDrag));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Drop));
        rightChildReceiver.Reset();

        // Check Scroll
        InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.one);
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(0));
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Scroll));
        rightChildReceiver.Reset();

        // Reset Scroll
        InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.zero);
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(0));
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Scroll));
        rightChildReceiver.Reset();

        // Test Selection
        eventSystem.SetSelectedGameObject(leftChildGameObject);
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Select));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check Move Axes
        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(1.0f, 0.0f));
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check Submit
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = (uint)(1 << (int)GamepadState.Button.South) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check Cancel
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = (uint)(1 << (int)GamepadState.Button.East) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Cancel));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check Selection Swap
        eventSystem.SetSelectedGameObject(rightChildGameObject);
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Deselect));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Select));
        rightChildReceiver.Reset();

        // Check Deselect
        eventSystem.SetSelectedGameObject(null);
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(0));
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Deselect));
        rightChildReceiver.Reset();
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
        Select,
        Deselect,
        PotentialDrag,
        BeginDrag,
        Dragging,
        Drop,
        EndDrag,
        Move,
        Submit,
        Cancel,
        Scroll
    }

    private class UICallbackReceiver : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler,
        IPointerExitHandler, IPointerUpHandler, IMoveHandler, ISelectHandler, IDeselectHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISubmitHandler, ICancelHandler, IScrollHandler
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

        public void Reset()
        {
            events.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Click, ClonePointerEventData(eventData)));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Down, ClonePointerEventData(eventData)));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Enter, ClonePointerEventData(eventData)));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Exit, ClonePointerEventData(eventData)));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Up, ClonePointerEventData(eventData)));
        }

        public void OnMove(AxisEventData eventData)
        {
            events.Add(new Event(EventType.Move, CloneAxisEventData(eventData)));
        }

        public void OnSubmit(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Submit, null));
        }

        public void OnCancel(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Cancel, null));
        }

        public void OnSelect(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Select, null));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            events.Add(new Event(EventType.Deselect, null));
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PotentialDrag, ClonePointerEventData(eventData)));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            events.Add(new Event(EventType.BeginDrag, ClonePointerEventData(eventData)));
        }

        public void OnDrag(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Dragging, ClonePointerEventData(eventData)));
        }

        public void OnDrop(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Drop, ClonePointerEventData(eventData)));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            events.Add(new Event(EventType.EndDrag, ClonePointerEventData(eventData)));
        }

        public void OnScroll(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Scroll, ClonePointerEventData(eventData)));
        }

        private AxisEventData CloneAxisEventData(AxisEventData eventData)
        {
            return new AxisEventData(EventSystem.current)
            {
                moveVector = eventData.moveVector,
                moveDir = eventData.moveDir
            };
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
