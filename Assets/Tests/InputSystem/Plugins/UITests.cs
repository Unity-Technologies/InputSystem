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
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Is = UnityEngine.TestTools.Constraints.Is;

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
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromMouse()
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

        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        // Move mouse over left child.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100) });
        InputSystem.Update();
        eventSystem.InvokeUpdate();

        Assert.That(leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(leftChildReceiver.events[0].type, Is.EqualTo(EventType.Enter));
        leftChildReceiver.Reset();
        Assert.That(rightChildReceiver.events, Is.Empty);
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.True);

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
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.True);
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_TouchActionsCanDriveUIAndDistinguishMultipleTouches()
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

        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(2), Is.False);
        Assert.That(eventSystem.IsPointerOverGameObject(3), Is.False);

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

        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(3), Is.False);

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

        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(3), Is.False);

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

        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(eventSystem.IsPointerOverGameObject(3), Is.False);
    }

    [UnityTest]
    [Category("UI")]
    // Check that two players can have separate UI, and that both selections will stay active when
    // clicking on UI with the mouse, using MultiPlayerEventSystem.playerRoot to match UI to the players.
    public IEnumerator UI_CanOperateMultiplayerUIGloballyUsingMouse()
    {
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
    [Category("UI")]
    // Check that two players can have separate UI and control it using separate gamepads, using
    // MultiplayerEventSystem.
    public IEnumerator UI_CanOperateMultiplayerUILocallyUsingGamepads()
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
            var moveAction = map.AddAction("move", type: InputActionType.PassThrough);
            var submitAction = map.AddAction("submit", type: InputActionType.PassThrough);
            var cancelAction = map.AddAction("cancel", type: InputActionType.PassThrough);

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
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromGamepad()
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
        var moveAction = map.AddAction("move", type: InputActionType.PassThrough);
        var submitAction = map.AddAction("submit", type: InputActionType.PassThrough);
        var cancelAction = map.AddAction("cancel", type: InputActionType.PassThrough);

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

    [Test]
    [Category("UI")]
    public void UI_CanReassignUIActions()
    {
        var go = new GameObject();
        go.AddComponent<EventSystem>();
        var uiModule = go.AddComponent<InputSystemUIInputModule>();

        const string kActions = @"
            {
                ""maps"" : [
                    {
                        ""name"" : ""Gameplay"",
                        ""actions"" : [
                            { ""name"" : ""Point"" },
                            { ""name"" : ""Navigate"" }
                        ]
                    },
                    {
                        ""name"" : ""UI"",
                        ""actions"" : [
                            { ""name"" : ""Navigate"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""Point"", ""type"" : ""PassThrough"" }
                        ]
                    }
                ]
            }
        ";

        var actions1 = InputActionAsset.FromJson(kActions);

        uiModule.actionsAsset = actions1;
        uiModule.move = InputActionReference.Create(actions1["ui/navigate"]);
        uiModule.point = InputActionReference.Create(actions1["ui/point"]);

        Assert.That(uiModule.actionsAsset, Is.SameAs(actions1));
        Assert.That(uiModule.move.action, Is.SameAs(actions1["ui/navigate"]));
        Assert.That(uiModule.point.action, Is.SameAs(actions1["ui/point"]));

        var actions2 = ScriptableObject.Instantiate(actions1);
        actions2["ui/point"].RemoveAction();

        uiModule.actionsAsset = actions2;

        Assert.That(uiModule.actionsAsset, Is.SameAs(actions2));
        Assert.That(uiModule.move.action, Is.SameAs(actions2["ui/navigate"]));
        Assert.That(uiModule.point?.action, Is.Null);
    }

    // Right now, text input in uGUI is picked up from IMGUI events. ATM they're still out of reach for us.
    // Hopefully something we can solve as part of getting rid of the old input system.
    [Test]
    [Category("UI")]
    [Ignore("TODO")]
    public void TODO_UI_CanDriveTextInput()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var eventSystemGO = new GameObject();
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<InputSystemUIInputModule>();

        var canvasGO = new GameObject();
        canvasGO.AddComponent<Canvas>();

        var inputFieldGO = new GameObject();
        inputFieldGO.transform.SetParent(canvasGO.transform);
        var inputField = inputFieldGO.AddComponent<InputField>();
        inputField.text = string.Empty;

        InputSystem.QueueTextEvent(keyboard, 'a');
        InputSystem.QueueTextEvent(keyboard, 'b');
        InputSystem.QueueTextEvent(keyboard, 'c');
        InputSystem.Update();

        Assert.That(inputField.text, Is.EqualTo("abc"));
    }

    ////TODO: We need to override BaseInput which currently is still hooked to the old input system APIs.
    [Test]
    [Category("UI")]
    [Ignore("TODO")]
    public void TODO_UI_CanDriveIME()
    {
        Assert.Fail();
    }

    [Test]
    [Category("UI")]
    [Retry(2)] // Warm up JIT
    public void UI_MovingAndClickingMouseDoesNotAllocateGCMemory()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("Point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("Click", type: InputActionType.PassThrough, binding: "<Mouse>/leftButton");

        actions.Enable();

        var eventSystemGO = new GameObject();
        eventSystemGO.AddComponent<EventSystem>();
        var uiModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
        uiModule.actionsAsset = actions;
        uiModule.point = InputActionReference.Create(pointAction);
        uiModule.leftClick = InputActionReference.Create(clickAction);

        // We allow the first hit on the UI module to set up internal data structures
        // and thus allocate something. So go and run one event with data on the mouse.
        // Also gets rid of GC noise from the initial input system update.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 2) });
        InputSystem.Update();

        // Make sure we don't get an allocation from the string literal.
        var kProfilerRegion = "UI_MovingAndClickingMouseDoesNotAllocateMemory";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            Set(mouse.position, new Vector2(123, 234));
            Set(mouse.position, new Vector2(234, 345));
            Press(mouse.leftButton);
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    // https://forum.unity.com/threads/feature-request-option-to-disable-deselect-in-ui-input-module.761531
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanPreventAutomaticDeselectionOfGameObjects()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("Point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("Click", type: InputActionType.PassThrough, binding: "<Mouse>/leftButton");

        actions.Enable();

        var testObjects = CreateScene(0, 200);

        testObjects.uiModule.actionsAsset = actions;
        testObjects.uiModule.point = InputActionReference.Create(pointAction);
        testObjects.uiModule.leftClick = InputActionReference.Create(clickAction);

        // Deselect behavior should be on by default as this corresponds to the behavior before
        // we introduced the switch that allows toggling the behavior off.
        Assert.That(testObjects.uiModule.deselectOnBackgroundClick, Is.True);

        // Give canvas a chance to set itself up.
        yield return null;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, new Vector2(10, 10));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.leftGameObject));

        // Click outside of GOs and make sure the selection gets cleared.
        Set(mouse.position, new Vector2(50, 250));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.Null);

        testObjects.uiModule.deselectOnBackgroundClick = false;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, new Vector2(10, 10));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.leftGameObject));

        // Click outside of GOs and make sure our selection does NOT get cleared.
        Set(mouse.position, new Vector2(50, 250));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.leftGameObject));
    }

    ////REVIEW: While `deselectOnBackgroundClick` does solve the problem of breaking keyboard and gamepad navigation, the question
    ////        IMO is whether navigation should even be affected that way by not having a current selection. Seems to me that the
    ////        the system should remember the last selected object and start up navigation from there when nothing is selected.
    ////        However, given EventSystem.lastSelectedGameObject is no longer supported (why???), it seems like this would require
    ////        some larger changes.
    [UnityTest]
    [Category("UI")]
    [Ignore("TODO")]
    public IEnumerator TODO_UI_CanStartNavigationWhenNothingIsSelected()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("Point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("Click", type: InputActionType.PassThrough, binding: "<Mouse>/leftButton");
        var navigateAction = uiActions.AddAction("Navigate", type: InputActionType.PassThrough, binding: "<Gamepad>/dpad");

        actions.Enable();

        var testObjects = CreateScene(0, 200);

        testObjects.uiModule.actionsAsset = actions;
        testObjects.uiModule.point = InputActionReference.Create(pointAction);
        testObjects.uiModule.leftClick = InputActionReference.Create(clickAction);
        testObjects.uiModule.move = InputActionReference.Create(navigateAction);

        // Give canvas a chance to set itself up.
        yield return null;

        // Select left GO.
        Set(mouse.position, new Vector2(10, 10));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.leftGameObject));

        // Click on background and make sure we deselect.
        Set(mouse.position, new Vector2(50, 250));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.Null);

        // Now perform a navigate-right action. Given we have no current selection, this should
        // cause the right GO to be selected based on the fact that the left GO was selected last.
        Press(gamepad.dpad.right);
        yield return null;

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.rightGameObject));

        // Just to make extra sure, navigate left and make sure that results in the expected selection
        // change over to the left GO.
        Release(gamepad.dpad.right);
        Press(gamepad.dpad.left);
        yield return null;

        Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(testObjects.leftGameObject));
    }

// The tracked device tests fail with NullReferenceException in the windows editor on yamato. I cannot reproduce this locally, so will disable them on windows for now.
#if !UNITY_EDITOR_WIN

    private struct TestTrackedDeviceLayout : IInputStateTypeInfo
    {
        [InputControl(name = "position", layout = "Vector3")]
        public Vector3 position;
        [InputControl(name = "orientation", layout = "Quaternion", offset = 12)]
        public Quaternion orientation;
        [InputControl(name = "select", layout = "Button", offset = 28, sizeInBits = 8)]
        public byte select;

        public FourCC format => new FourCC('T', 'E', 'S', 'T');
    }

    ////TODO: switch this to an actual TrackedDevice
    [InputControlLayout(stateType = typeof(TestTrackedDeviceLayout))]
    [Preserve]
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
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromTrackedDevice()
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
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromMultipleTrackedDevices()
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

        protected override void OnApplicationFocus(bool hasFocus)
        {
            // Sync our focus state to that of the test runtime rather than to the Unity test runner (where
            // debugging may still focus and thus alter the test run).
            hasFocus = ((InputTestRuntime)InputRuntime.s_Instance).hasFocus;
            base.OnApplicationFocus(hasFocus);
        }
    }
}
