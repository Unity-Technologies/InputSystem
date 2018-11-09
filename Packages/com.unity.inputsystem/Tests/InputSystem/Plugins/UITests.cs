using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools;
using UnityEngine.UI;

////TODO: app focus handling
////TODO: send IUpdateSelectedHandler.OnUpdateSelected event

internal class UITests : InputTestFixture
{
    private struct TestObjects
    {
        internal UIActionInputModule uiModule;
        internal TestEventSystem eventSystem;
        internal GameObject leftGameObject;
        internal GameObject rightGameObject;
    }

    // Set up a UIActionInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    TestObjects CreateScene()
    {
        TestObjects objects = new TestObjects();

        // Set up GameObject with EventSystem.
        var systemObject = new GameObject("System");
        objects.eventSystem = systemObject.AddComponent<TestEventSystem>();
        var uiModule = systemObject.AddComponent<UIActionInputModule>();
        uiModule.sendEventsWhenInBackground = true;
        objects.uiModule = uiModule;
        objects.eventSystem.UpdateModules();
        objects.eventSystem.InvokeUpdate(); // Initial update only sets current module.

        // Set up camera and canvas on which we can perform raycasts.
        var cameraObject = new GameObject("Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        camera.pixelRect = new Rect(0, 0, 640, 480);

        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceRaycaster>();
        canvas.worldCamera = camera;

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject("Parent");
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        parentGameObject.AddComponent<UICallbackReceiver>();

        var leftChildGameObject = new GameObject("Left Child");
        var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
        leftChildGameObject.AddComponent<Image>();
        leftChildGameObject.AddComponent<UICallbackReceiver>();
        objects.leftGameObject = leftChildGameObject;

        var rightChildGameObject = new GameObject("Right Child");
        var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
        rightChildGameObject.AddComponent<Image>();
        rightChildGameObject.AddComponent<UICallbackReceiver>();
        objects.rightGameObject = rightChildGameObject;

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

        return objects;
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator MouseActions_CanDriveUI()
    {
        // Create devices.
        var mouse = InputSystem.AddDevice<Mouse>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create actions.
        var map = new InputActionMap();
        var pointAction = map.AddAction("point");
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

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.point = new InputActionProperty(pointAction);
        uiModule.leftClick = new InputActionProperty(leftClickAction);
        uiModule.middleClick = new InputActionProperty(middleClickAction);
        uiModule.rightClick = new InputActionProperty(rightClickAction);
        uiModule.scrollWheel = new InputActionProperty(scrollAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Move mouse over left child.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check basic down/up
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = (ushort)(1 << (int)MouseButton.Left) });
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
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = (ushort)(1 << (int)MouseButton.Right) });
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
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator JoystickActions_CanDriveUI()
    {
        // Create devices.
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create actions.
        var map = new InputActionMap();
        var moveAction = map.AddAction("move");
        var submitAction = map.AddAction("submit");
        var cancelAction = map.AddAction("cancel");

        // Create bindings.
        moveAction.AddBinding(gamepad.leftStick);
        submitAction.AddBinding(gamepad.buttonSouth);
        cancelAction.AddBinding(gamepad.buttonEast);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.move = new InputActionProperty(moveAction);
        uiModule.submit = new InputActionProperty(submitAction);
        uiModule.cancel = new InputActionProperty(cancelAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

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
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = (uint)(1 << (int)GamepadButton.South) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        // Check Cancel
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = (uint)(1 << (int)GamepadButton.East) });
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

    private struct TestTrackedDeviceLayout : IInputStateTypeInfo
    {
        public const int kSizeInBytes = 29;

        [InputControl(name = "position", layout = "Vector3")]
        public Vector3 position;
        [InputControl(name = "orientation", layout = "Quaternion", offset = 12)]
        public Quaternion orientation;
        [InputControl(name = "select", layout = "Button", offset = 28, sizeInBits = 8)]
        public byte select;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(TestTrackedDeviceLayout))]
    class TestTrackedDevice : InputDevice
    {
        public Vector3Control position { get; private set; }
        public QuaternionControl orientation { get; private set; }
        public ButtonControl select { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            position = builder.GetControl<Vector3Control>("position");
            orientation = builder.GetControl<QuaternionControl>("orientation");
            select = builder.GetControl<ButtonControl>("select");
        }
    }

    ////FIXME: this is suffering from InputTestFixture's problem with running normal frame updates
    [UnityTest]
    [Category("Actions")]
    [Ignore("TODO")]
    public IEnumerator TODO_TrackedDeviceActions_CanDriveUI()
    {
        // Create device.
        InputSystem.RegisterLayout<TestTrackedDevice>();
        var trackedDevice = InputSystem.AddDevice<TestTrackedDevice>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create actions.
        var map = new InputActionMap();
        var trackedPositionAction = map.AddAction("position");
        var trackedOrientationAction = map.AddAction("orientation");
        var trackedSelectAction = map.AddAction("selection");

        trackedPositionAction.AddBinding(trackedDevice.position);
        trackedOrientationAction.AddBinding(trackedDevice.orientation);
        trackedSelectAction.AddBinding(trackedDevice.select);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.AddTrackedDevice(new InputActionProperty(trackedPositionAction), new InputActionProperty(trackedOrientationAction), new InputActionProperty(trackedSelectAction));

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        InputEventPtr stateEvent;
        using (StateEvent.From(trackedDevice, out stateEvent))
        {
            // Reset to Defaults
            trackedDevice.position.WriteValueInto(stateEvent, Vector3.zero);
            trackedDevice.orientation.WriteValueInto(stateEvent, Quaternion.Euler(0.0f, -90.0f, 0.0f));
            trackedDevice.select.WriteValueInto(stateEvent, (byte)0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();

            leftChildReceiver.Reset();
            rightChildReceiver.Reset();

            // Move over left child.
            trackedDevice.orientation.WriteValueInto(stateEvent, Quaternion.Euler(0.0f, -30.0f, 0.0f));
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

            // Check basic down/up
            trackedDevice.select.WriteValueInto(stateEvent, byte.MaxValue);
            InputSystem.QueueEvent(stateEvent);
            trackedDevice.select.WriteValueInto(stateEvent, (byte)0);
            InputSystem.QueueEvent(stateEvent);
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
            trackedDevice.select.WriteValueInto(stateEvent, byte.MaxValue);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

            // Move to new location on left child
            trackedDevice.orientation.WriteValueInto(stateEvent, Quaternion.Euler(0.0f, -10.0f, 0.0f));
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

            // Move children
            trackedDevice.orientation.WriteValueInto(stateEvent, Quaternion.Euler(0.0f, 30.0f, 0.0f));
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            rightChildReceiver.Reset();

            trackedDevice.select.WriteValueInto(stateEvent, (byte)0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Up));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.EndDrag));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Drop));
            rightChildReceiver.Reset();
        }
    }

    private struct TestTouchLayout : IInputStateTypeInfo
    {
        public const int kSizeInBytes = TouchState.kSizeInBytes;

        [InputControl(name = "touch", layout = "Touch")]
        public TouchState touch;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'T', 'L', ' ');
        }
    }

    [InputControlLayout(stateType = typeof(TestTouchLayout))]
    class TestTouchDevice : InputDevice
    {
        public TouchControl touch { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            touch = builder.GetControl<TouchControl>("touch");
        }
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator TouchActions_CanDriveUI()
    {
        // Create devices.
        var touchDevice = InputSystem.AddDevice<TestTouchDevice>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create actions.
        var map = new InputActionMap();
        var positionAction = map.AddAction("position");
        var phaseAction = map.AddAction("phase");

        // Create bindings.
        positionAction.AddBinding(touchDevice.touch.position);
        phaseAction.AddBinding(touchDevice.touch.phase);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.AddTouch(new InputActionProperty(positionAction), new InputActionProperty(phaseAction));

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Move mouse over left child.
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(0, 0), phase = PointerPhase.Began });
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(100, 100), phase = PointerPhase.Moved });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(3));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        //Drag to new location on left child
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(100, 200), phase = PointerPhase.Moved });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(0));

        //Now move children
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(400, 200), phase = PointerPhase.Moved });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Exit));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        rightChildReceiver.Reset();


        //And now release
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(400, 200), phase = PointerPhase.Ended });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Up));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.EndDrag));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Drop));
        rightChildReceiver.Reset();

        //Now check for a quick click
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(400, 200), phase = PointerPhase.Began });
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(400, 200), phase = PointerPhase.Moved });
        InputSystem.QueueDeltaStateEvent(touchDevice.touch, new TouchState { position = new Vector2(400, 200), phase = PointerPhase.Ended });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(0));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(4));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(rightChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        Assert.That(rightChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
        Assert.That(rightChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
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
