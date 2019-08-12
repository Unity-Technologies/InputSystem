using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

#pragma warning disable CS0649
////TODO: app focus handling
////TODO: send IUpdateSelectedHandler.OnUpdateSelected event

internal class UITests : InputTestFixture
{
    private struct TestObjects
    {
        internal InputSystemUIInputModule uiModule;
        internal TestEventSystem eventSystem;
        internal RectTransform parentTransform;
        internal GameObject leftGameObject;
        internal GameObject rightGameObject;
        internal UICallbackReceiver leftChildReceiver;
        internal UICallbackReceiver rightChildReceiver;
    }

    // Set up a InputSystemUIInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    private static TestObjects CreateScene(int minY = 0 , int maxY = 480)
    {
        var objects = new TestObjects();

        // Set up GameObject with EventSystem.
        var systemObject = new GameObject("System");
        objects.eventSystem = systemObject.AddComponent<TestEventSystem>();
        var uiModule = systemObject.AddComponent<InputSystemUIInputModule>();
        objects.uiModule = uiModule;
        objects.eventSystem.UpdateModules();

        var cameraObject = GameObject.Find("Camera");
        Camera camera;
        if (cameraObject == null)
        {
            // Set up camera and canvas on which we can perform raycasts.
            cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.pixelRect = new Rect(0, 0, 640, 480);
        }
        else
            camera = cameraObject.GetComponent<Camera>();

        var canvasObject = GameObject.Find("Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceRaycaster>();
            canvas.worldCamera = camera;
        }

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject("Parent");
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        objects.parentTransform = parentTransform;
        parentGameObject.AddComponent<UICallbackReceiver>();

        var leftChildGameObject = new GameObject("Left Child");
        var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
        leftChildGameObject.AddComponent<Image>();
        objects.leftChildReceiver = leftChildGameObject.AddComponent<UICallbackReceiver>();
        objects.leftGameObject = leftChildGameObject;

        var rightChildGameObject = new GameObject("Right Child");
        var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
        rightChildGameObject.AddComponent<Image>();
        objects.rightChildReceiver = rightChildGameObject.AddComponent<UICallbackReceiver>();
        objects.rightGameObject = rightChildGameObject;

        parentTransform.SetParent(canvasObject.transform, worldPositionStays: false);
        leftChildTransform.SetParent(parentTransform, worldPositionStays: false);
        rightChildTransform.SetParent(parentTransform, worldPositionStays: false);

        // Parent occupies full space of canvas.
        parentTransform.sizeDelta = new Vector2(640, maxY - minY);
        parentTransform.anchoredPosition = new Vector2(0, (maxY + minY) / 2 - 240);

        // Left child occupies left half of parent.
        leftChildTransform.anchoredPosition = new Vector2(-(640 / 4), 0); //(maxY + minY)/2 - 240);
        leftChildTransform.sizeDelta = new Vector2(320, maxY - minY);

        // Right child occupies right half of parent.
        rightChildTransform.anchoredPosition = new Vector2(640 / 4, 0); //(maxY + minY) / 2 - 240);
        rightChildTransform.sizeDelta = new Vector2(320, maxY - minY);

        objects.eventSystem.playerRoot = parentGameObject;
        objects.eventSystem.firstSelectedGameObject = leftChildGameObject;
        objects.eventSystem.InvokeUpdate(); // Initial update only sets current module.

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

        // Create asset
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough);
        var leftClickAction = map.AddAction("leftClick", type: InputActionType.PassThrough);
        var rightClickAction = map.AddAction("rightClick", type: InputActionType.PassThrough);
        var middleClickAction = map.AddAction("middleClick", type: InputActionType.PassThrough);
        var scrollAction = map.AddAction("scroll", type: InputActionType.PassThrough);

        // Create bindings.
        pointAction.AddBinding(mouse.position);
        leftClickAction.AddBinding(mouse.leftButton);
        rightClickAction.AddBinding(mouse.rightButton);
        middleClickAction.AddBinding(mouse.middleButton);
        scrollAction.AddBinding(mouse.scroll);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.point = InputActionReference.Create(pointAction);
        uiModule.leftClick = InputActionReference.Create(leftClickAction);
        uiModule.middleClick = InputActionReference.Create(middleClickAction);
        uiModule.rightClick = InputActionReference.Create(rightClickAction);
        uiModule.scrollWheel = InputActionReference.Create(scrollAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Reset initial selection
        leftChildReceiver.Reset();

        // Move mouse over left child.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Check basic down/up
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = 0 });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(4));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
        Assert.That(leftChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Check down and drag
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = 1 << (int)MouseButton.Right });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Right));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Move to new location on left child
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(100, 200));
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Move children
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(350, 200));
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
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(350, 200), buttons = 0 });
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

        Assert.That(leftChildReceiver.events, Is.Empty);
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Scroll));
        rightChildReceiver.Reset();
    }

    unsafe void SetTouchState(TouchscreenState state, int index, TouchState touch)
    {
        state.touches[index] = touch;
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator TouchActions_CanDriveUIAndDistinguishMultipleTouches()
    {
        // Create devices.
        var touchScreen = InputSystem.AddDevice<Touchscreen>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create asset
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough);
        var leftClickAction = map.AddAction("leftClick", type: InputActionType.PassThrough);

        // Create bindings.
        pointAction.AddBinding("<Touchscreen>/touch*/position");
        leftClickAction.AddBinding("<Touchscreen>/touch*/press");

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.point = InputActionReference.Create(pointAction);
        uiModule.leftClick = InputActionReference.Create(leftClickAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Reset initial selection
        leftChildReceiver.Reset();

        // Touch left button
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[0], new TouchState()
        {
            touchId = 1,
            position = new Vector2(100, 100)
        });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        InputSystem.QueueDeltaStateEvent(touchScreen.touches[0], new TouchState()
        {
            touchId = 1,
            position = new Vector2(100, 100),
            phase = TouchPhase.Began
        });

        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(leftChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Touch right button
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[1], new TouchState()
        {
            touchId = 2,
            position = new Vector2(400, 100)
        });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        rightChildReceiver.Reset();
        Assert.That(leftChildReceiver.events, Is.Empty);

        InputSystem.QueueDeltaStateEvent(touchScreen.touches[1], new TouchState()
        {
            touchId = 2,
            position = new Vector2(400, 100),
            phase = TouchPhase.Began
        });

        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
        Assert.That(rightChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((rightChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
        Assert.That((rightChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(rightChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
        rightChildReceiver.Reset();
        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Deselect));
        leftChildReceiver.Reset();

        // release left button
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[0], new TouchState()
        {
            touchId = 1,
            position = new Vector2(100, 100),
            phase = TouchPhase.Stationary
        });
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[0], new TouchState()
        {
            touchId = 1,
            position = new Vector2(100, 100),
            phase = TouchPhase.Ended
        });

        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(3));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Up));
        Assert.That(leftChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
        Assert.That((leftChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Click));
        Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Select));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // release right button
        // NOTE: The UI behavior is a bit funky here, as it cannot properly handle selection state for multiple
        // objects. As a result, only releasing the left button will receive a click in this setup.
        // This is not great, but multi-touch UI is a corner case, not generally supported by UI systems on
        // Touch OSes. This does however correctly handle multiple on screen controls, which is the use case we care about.
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[1], new TouchState()
        {
            touchId = 2,
            position = new Vector2(400, 100),
            phase = TouchPhase.Stationary
        });
        InputSystem.QueueDeltaStateEvent(touchScreen.touches[1], new TouchState()
        {
            touchId = 2,
            position = new Vector2(400, 100),
            phase = TouchPhase.Ended
        });

        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Up));
        Assert.That(rightChildReceiver.events[0].data, Is.TypeOf<PointerEventData>());
        Assert.That((rightChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
        Assert.That((rightChildReceiver.events[0].data as PointerEventData).button, Is.EqualTo(PointerEventData.InputButton.Left));
        rightChildReceiver.Reset();
        Assert.That(leftChildReceiver.events, Is.Empty);
    }

    [UnityTest]
    [Category("Actions")]
    // Check that two players can have separate UI, and that both selections will stay active when clicking on UI with the mouse,
    // using MultiPlayerEventSystem.playerRoot to match UI to the players.
    public IEnumerator MouseActions_MultiplayerEventSystemKeepsPerPlayerSelection()
    {
        // Create devices.
        var mouse = InputSystem.AddDevice<Mouse>();

        var players = new[] { CreateScene(0, 240), CreateScene(240, 480) };

        // Create asset
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough);
        var leftClickAction = map.AddAction("leftClick", type: InputActionType.PassThrough);
        var rightClickAction = map.AddAction("rightClick", type: InputActionType.PassThrough);
        var middleClickAction = map.AddAction("middleClick", type: InputActionType.PassThrough);
        var scrollAction = map.AddAction("scroll", type: InputActionType.PassThrough);

        // Create bindings.
        pointAction.AddBinding(mouse.position);
        leftClickAction.AddBinding(mouse.leftButton);
        rightClickAction.AddBinding(mouse.rightButton);
        middleClickAction.AddBinding(mouse.middleButton);
        scrollAction.AddBinding(mouse.scroll);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        foreach (var player in players)
        {
            player.uiModule.point = InputActionReference.Create(pointAction);
            player.uiModule.leftClick = InputActionReference.Create(leftClickAction);
            player.uiModule.middleClick = InputActionReference.Create(middleClickAction);
            player.uiModule.rightClick = InputActionReference.Create(rightClickAction);
            player.uiModule.scrollWheel = InputActionReference.Create(scrollAction);
            player.eventSystem.SetSelectedGameObject(null);
        }

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Click left gameObject of player 0
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100), buttons = 0 });
        InputSystem.Update();

        foreach (var player in players)
            player.eventSystem.InvokeUpdate();

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.Null);

        // Click right gameObject of player 1
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 300), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 300), buttons = 0 });

        InputSystem.Update();

        foreach (var player in players)
            player.eventSystem.InvokeUpdate();

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].rightGameObject));

        // Click right gameObject of player 0
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 100), buttons = 0 });
        InputSystem.Update();

        foreach (var player in players)
            player.eventSystem.InvokeUpdate();

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].rightGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].rightGameObject));
    }

    [UnityTest]
    [Category("Actions")]
    // Check that two players can have separate UI and control it using separate gamepads, using MultiplayerEventSystem.
    public IEnumerator JoystickActions_MultiplayerEventSystemKeepsPerPlayerSelection()
    {
        // Create devices.
        var gamepads = new[] { InputSystem.AddDevice<Gamepad>(), InputSystem.AddDevice<Gamepad>() };
        var players = new[] { CreateScene(0, 240), CreateScene(240, 480) };

        for (var i = 0; i < 2; i++)
        {
            // Create asset
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Create actions.
            var map = new InputActionMap("map");
            asset.AddActionMap(map);
            var moveAction = map.AddAction("move");
            var submitAction = map.AddAction("submit");
            var cancelAction = map.AddAction("cancel");

            // Create bindings.
            moveAction.AddBinding(gamepads[i].leftStick);
            submitAction.AddBinding(gamepads[i].buttonSouth);
            cancelAction.AddBinding(gamepads[i].buttonEast);

            // Wire up actions.
            players[i].uiModule.move = InputActionReference.Create(moveAction);
            players[i].uiModule.submit = InputActionReference.Create(submitAction);
            players[i].uiModule.cancel = InputActionReference.Create(cancelAction);

            players[i].leftChildReceiver.moveTo = players[i].rightGameObject;
            players[i].rightChildReceiver.moveTo = players[i].leftGameObject;

            // Enable the whole thing.
            map.Enable();
        }

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].leftGameObject));

        // Reset initial selection
        players[0].leftChildReceiver.Reset();
        players[1].leftChildReceiver.Reset();

        // Check Player 0 Move Axes
        InputSystem.QueueDeltaStateEvent(gamepads[0].leftStick, new Vector2(1.0f, 0.0f));
        InputSystem.Update();

        foreach (var player in players)
        {
            Assert.That(player.leftChildReceiver.events, Is.Empty);
            Assert.That(player.rightChildReceiver.events, Is.Empty);
            player.eventSystem.InvokeUpdate();
        }

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].rightGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].leftGameObject));

        Assert.That(players[0].leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(players[0].leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(players[0].leftChildReceiver.events[1].type, Is.EqualTo(EventType.Deselect));
        players[0].leftChildReceiver.Reset();

        Assert.That(players[0].rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(players[0].rightChildReceiver.events[0].type, Is.EqualTo(EventType.Select));
        players[0].rightChildReceiver.Reset();

        foreach (var player in players)
        {
            Assert.That(player.leftChildReceiver.events, Is.Empty);
            Assert.That(player.rightChildReceiver.events, Is.Empty);
            player.eventSystem.InvokeUpdate();
        }

        // Check Player 0 Submit
        InputSystem.QueueStateEvent(gamepads[0], new GamepadState { buttons = 1 << (int)GamepadButton.South });
        InputSystem.Update();

        foreach (var player in players)
        {
            Assert.That(player.leftChildReceiver.events, Is.Empty);
            Assert.That(player.rightChildReceiver.events, Is.Empty);
            player.eventSystem.InvokeUpdate();
        }

        Assert.That(players[0].rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(players[0].rightChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        players[0].rightChildReceiver.Reset();

        // Check Player 1 Submit
        InputSystem.QueueStateEvent(gamepads[1], new GamepadState { buttons = 1 << (int)GamepadButton.South });
        InputSystem.Update();

        foreach (var player in players)
        {
            Assert.That(player.leftChildReceiver.events, Is.Empty);
            Assert.That(player.rightChildReceiver.events, Is.Empty);
            player.eventSystem.InvokeUpdate();
        }
        Assert.That(players[1].leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(players[1].leftChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        players[1].leftChildReceiver.Reset();

        foreach (var player in players)
        {
            Assert.That(player.leftChildReceiver.events, Is.Empty);
            Assert.That(player.rightChildReceiver.events, Is.Empty);
        }
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

        // Create asset
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var moveAction = map.AddAction("move");
        var submitAction = map.AddAction("submit");
        var cancelAction = map.AddAction("cancel");

        // Create bindings.
        moveAction.AddBinding(gamepad.leftStick);
        submitAction.AddBinding(gamepad.buttonSouth);
        cancelAction.AddBinding(gamepad.buttonEast);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.move = InputActionReference.Create(moveAction);
        uiModule.submit = InputActionReference.Create(submitAction);
        uiModule.cancel = InputActionReference.Create(cancelAction);

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
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Check Move Axes
        // Fixme: replacing this with Set(gamepads[0].leftStick, new Vector2(1, 0)); throws a NRE.
        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(1.0f, 0.0f));
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Check Submit
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadButton.South });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

        // Check Cancel
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadButton.East });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Cancel));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);

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

        Assert.That(leftChildReceiver.events, Is.Empty);
        Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Deselect));
        rightChildReceiver.Reset();
    }

// The tracked device tests fail with NullReferenceException in the windows editor on yamato. I cannot reproduce this locally, so will disable them on windows for now.
#if !UNITY_EDITOR_WIN

    private struct TestTrackedDeviceLayout : IInputStateTypeInfo
    {
        public const int kSizeInBytes = 29;

        [InputControl(name = "position", layout = "Vector3")]
        public Vector3 position;
        [InputControl(name = "orientation", layout = "Quaternion", offset = 12)]
        public Quaternion orientation;
        [InputControl(name = "select", layout = "Button", offset = 28, sizeInBits = 8)]
        public byte select;

        public FourCC format => new FourCC('T', 'E', 'S', 'T');
    }

    [InputControlLayout(stateType = typeof(TestTrackedDeviceLayout))]
    class TestTrackedDevice : InputDevice
    {
        public Vector3Control position { get; private set; }
        public QuaternionControl orientation { get; private set; }
        public ButtonControl select { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            position = GetChildControl<Vector3Control>("position");
            orientation = GetChildControl<QuaternionControl>("orientation");
            select = GetChildControl<ButtonControl>("select");
        }
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator TrackedDeviceActions_CanDriveUI()
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
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var trackedPositionAction = map.AddAction("position", type: InputActionType.PassThrough);
        var trackedOrientationAction = map.AddAction("orientation", type: InputActionType.PassThrough);
        var trackedSelectAction = map.AddAction("selection", type: InputActionType.PassThrough);

        trackedPositionAction.AddBinding(trackedDevice.position);
        trackedOrientationAction.AddBinding(trackedDevice.orientation);
        trackedSelectAction.AddBinding(trackedDevice.select);

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.trackedDevicePosition = InputActionReference.Create(trackedPositionAction);
        uiModule.trackedDeviceOrientation = InputActionReference.Create(trackedOrientationAction);
        uiModule.trackedDeviceSelect = InputActionReference.Create(trackedSelectAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        using (StateEvent.From(trackedDevice, out var stateEvent))
        {
            // Reset to Defaults
            trackedDevice.position.WriteValueIntoEvent(Vector3.zero, stateEvent);
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -90.0f, 0.0f), stateEvent);
            trackedDevice.select.WriteValueIntoEvent(0f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();

            leftChildReceiver.Reset();
            rightChildReceiver.Reset();

            // Move over left child.
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -30.0f, 0.0f), stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Check basic down/up
            trackedDevice.select.WriteValueIntoEvent(1f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            trackedDevice.select.WriteValueIntoEvent(0f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
            Assert.That(leftChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Check down and drag
            trackedDevice.select.WriteValueIntoEvent(1f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Move to new location on left child
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -10.0f, 0.0f), stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Move children
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, 30.0f, 0.0f), stateEvent);
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

            trackedDevice.select.WriteValueIntoEvent(0f, stateEvent);
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

    [UnityTest]
    [Category("Actions")]
    // Check that we can track multiple tracked devices separately, with different pointer ids.
    public IEnumerator TrackedDeviceActions_CanDriveUIMultipleTrackers()
    {
        // Create device.
        InputSystem.RegisterLayout<TestTrackedDevice>();
        var trackedDevice = InputSystem.AddDevice<TestTrackedDevice>();
        var trackedDevice2 = InputSystem.AddDevice<TestTrackedDevice>();

        var objects = CreateScene();
        var uiModule = objects.uiModule;
        var eventSystem = objects.eventSystem;
        var leftChildGameObject = objects.leftGameObject;
        var leftChildReceiver = leftChildGameObject != null ? leftChildGameObject.GetComponent<UICallbackReceiver>() : null;
        var rightChildGameObject = objects.rightGameObject;
        var rightChildReceiver = rightChildGameObject != null ? rightChildGameObject.GetComponent<UICallbackReceiver>() : null;

        // Create actions.
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        // Create actions.
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var trackedPositionAction = map.AddAction("position", type: InputActionType.PassThrough);
        var trackedOrientationAction = map.AddAction("orientation", type: InputActionType.PassThrough);
        var trackedSelectAction = map.AddAction("selection", type: InputActionType.PassThrough);

        trackedPositionAction.AddBinding("*/position");
        trackedOrientationAction.AddBinding("*/orientation");
        trackedSelectAction.AddBinding("*/select");

        // Wire up actions.
        // NOTE: In a normal usage scenario, the user would wire these up in the inspector.
        uiModule.trackedDevicePosition = InputActionReference.Create(trackedPositionAction);
        uiModule.trackedDeviceOrientation = InputActionReference.Create(trackedOrientationAction);
        uiModule.trackedDeviceSelect = InputActionReference.Create(trackedSelectAction);

        // Enable the whole thing.
        map.Enable();

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        using (StateEvent.From(trackedDevice, out var stateEvent))
        using (StateEvent.From(trackedDevice2, out var stateEvent2))
        {
            // Reset to Defaults
            trackedDevice.position.WriteValueIntoEvent(Vector3.zero, stateEvent);
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -90.0f, 0.0f), stateEvent);
            trackedDevice.select.WriteValueIntoEvent(0f, stateEvent);
            InputSystem.QueueEvent(stateEvent);

            trackedDevice2.position.WriteValueIntoEvent(Vector3.zero, stateEvent2);
            trackedDevice2.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -90.0f, 0.0f), stateEvent2);
            trackedDevice2.select.WriteValueIntoEvent(0f, stateEvent2);
            InputSystem.QueueEvent(stateEvent2);
            InputSystem.Update();

            leftChildReceiver.Reset();
            rightChildReceiver.Reset();

            // Move over left child.
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -30.0f, 0.0f), stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            trackedDevice2.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, -30.0f, 0.0f), stateEvent2);
            InputSystem.QueueEvent(stateEvent2);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Check basic down/up
            trackedDevice.select.WriteValueIntoEvent(1f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            trackedDevice.select.WriteValueIntoEvent(0f, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That((leftChildReceiver.events[1].data as PointerEventData).pointerId, Is.EqualTo(0));
            Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
            Assert.That((leftChildReceiver.events[2].data as PointerEventData).pointerId, Is.EqualTo(0));
            Assert.That(leftChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
            Assert.That((leftChildReceiver.events[3].data as PointerEventData).pointerId, Is.EqualTo(0));

            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            trackedDevice2.select.WriteValueIntoEvent(1f, stateEvent2);
            InputSystem.QueueEvent(stateEvent2);
            trackedDevice2.select.WriteValueIntoEvent(0f, stateEvent2);
            InputSystem.QueueEvent(stateEvent2);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            Assert.That(leftChildReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            Assert.That(leftChildReceiver.events[2].type, Is.EqualTo(EventType.Up));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            Assert.That(leftChildReceiver.events[3].type, Is.EqualTo(EventType.Click));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Is.Empty);

            // Move to new location on right child
            trackedDevice.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, 30.0f, 0.0f), stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That((rightChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(0));
            rightChildReceiver.Reset();

            trackedDevice2.orientation.WriteValueIntoEvent(Quaternion.Euler(0.0f, 30.0f, 0.0f), stateEvent2);
            InputSystem.QueueEvent(stateEvent2);
            InputSystem.Update();
            eventSystem.InvokeUpdate();

            Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That((leftChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            leftChildReceiver.Reset();
            Assert.That(rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That((rightChildReceiver.events[0].data as PointerEventData).pointerId, Is.EqualTo(1));
            rightChildReceiver.Reset();
        }
    }

#endif

    private struct TestTouchLayout : IInputStateTypeInfo
    {
        [InputControl(name = "touch", layout = "Touch")]
        public TouchState touch;

        public FourCC format => new FourCC('T', 'T', 'L', ' ');
    }

    [InputControlLayout(stateType = typeof(TestTouchLayout))]
    class TestTouchDevice : InputDevice
    {
        public TouchControl touch { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            touch = GetChildControl<TouchControl>("touch");
        }
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

            public override string ToString()
            {
                var dataString = data?.ToString();
                dataString = dataString?.Replace("\n", "\n\t");
                return $"{type}[\n\t{dataString}]";
            }
        }

        public List<Event> events = new List<Event>();
        public GameObject moveTo;

        public void Reset()
        {
            events.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            events.Add(new Event(EventType.Click, ClonePointerEventData(eventData)));
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
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
            if (moveTo != null)
                EventSystem.current.SetSelectedGameObject(moveTo, eventData);
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

    private class TestEventSystem : MultiplayerEventSystem
    {
        public void InvokeUpdate()
        {
            Update();
        }
    }
}
