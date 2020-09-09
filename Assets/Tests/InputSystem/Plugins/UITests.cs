using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

#pragma warning disable CS0649
////TODO: app focus handling

internal class UITests : InputTestFixture
{
    private struct TestObjects
    {
        public InputSystemUIInputModule uiModule;
        public TestEventSystem eventSystem;
        public GameObject parentGameObject;
        public GameObject leftGameObject;
        public GameObject rightGameObject;
        public UICallbackReceiver parentReceiver;
        public UICallbackReceiver leftChildReceiver;
        public UICallbackReceiver rightChildReceiver;
    }

    // Set up a InputSystemUIInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    private static TestObjects CreateScene(int minY = 0 , int maxY = 480, bool noFirstSelected = false)
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
        objects.parentGameObject = parentGameObject;
        objects.parentReceiver = parentGameObject.AddComponent<UICallbackReceiver>();

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
        if (!noFirstSelected)
            objects.eventSystem.firstSelectedGameObject = leftChildGameObject;
        objects.eventSystem.InvokeUpdate(); // Initial update only sets current module.

        return objects;
    }

    // Comprehensive test for general pointer input behaviors.
    // NOTE: The behavior we test for here is slightly *DIFFERENT* than what you get with StandaloneInputModule. The reason is that
    //       StandaloneInputModule has both lots of inconsistencies between touch and mouse input (example: touch press handling goes
    //       selection -> enter/exit -> down -> click -> potential drag whereas mouse press handling goes selection -> down -> click
    //       -> potential drag; also, touch will set pressPosition always to the current position whereas mouse will set it only on
    //       click like the docs say) and also has some questionable behaviors that we opt to do different (for example, we perform
    //       click detection *before* invoking click handlers so that clickCount and clickTime correspond to the current click instead
    //       of to the previous click).
    [UnityTest]
    [Category("UI")]
    // All pointer input goes through a single code path. Goes for Pointer-derived devices as well as for TrackedDevice input but
    // also any other input that can deliver point and click functionality.
    //
    // NOTE: ExpectedResult is required for the test to pass; the value will not actually be validated for a UnityTest.
    [TestCase("Mouse", UIPointerType.MouseOrPen, PointerEventData.InputButton.Left, ExpectedResult = 1)]
    [TestCase("Mouse", UIPointerType.MouseOrPen, PointerEventData.InputButton.Middle, ExpectedResult = 1)]
    [TestCase("Mouse", UIPointerType.MouseOrPen, PointerEventData.InputButton.Right, ExpectedResult = 1)]
    [TestCase("Pen", UIPointerType.MouseOrPen, PointerEventData.InputButton.Left, ExpectedResult = 1)]
    [TestCase("Touchscreen", UIPointerType.Touch, PointerEventData.InputButton.Left, ExpectedResult = 1)]
    [TestCase("TrackedDeviceWithButton", UIPointerType.Tracked, PointerEventData.InputButton.Left, ExpectedResult = 1)]
    [TestCase("GenericDeviceWithPointingAbility", UIPointerType.MouseOrPen, PointerEventData.InputButton.Left, ExpectedResult = 1)]
    public IEnumerator UI_CanDriveUIFromPointer(string deviceLayout, UIPointerType pointerType, PointerEventData.InputButton clickButton)
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);
        InputSystem.RegisterLayout(kGenericDeviceWithPointingAbility);

        var device = InputSystem.AddDevice(deviceLayout);

        var isTouch = pointerType == UIPointerType.Touch;
        var isTracked = pointerType == UIPointerType.Tracked;
        var touchId = isTouch ? 1 : 0;
        var pointerId = isTouch ? ExtendedPointerEventData.MakePointerIdForTouch(device.deviceId, touchId) : device.deviceId;
        var trackedOrientation = isTracked ? Quaternion.Euler(0, -90, 0) : default;
        var trackedPosition = isTracked ? new Vector3(0.001f, 0.001f, 0.001f) : default;

        var scene = CreateScene();

        const string kActions = @"
            {
                ""maps"" : [
                    {
                        ""name"" : ""UIActions"",
                        ""actions"" : [
                            { ""name"" : ""point"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""leftClick"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""rightClick"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""middleClick"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""scroll"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""position"", ""type"" : ""PassThrough"" },
                            { ""name"" : ""orientation"", ""type"" : ""PassThrough"" }
                        ],
                        ""bindings"" : [
                            { ""path"" : ""<Mouse>/position"", ""action"" : ""point"" },
                            { ""path"" : ""<Pen>/position"", ""action"" : ""point"" },
                            { ""path"" : ""<Touchscreen>/touch*/position"", ""action"" : ""point"" },
                            { ""path"" : ""<GenericDeviceWithPointingAbility>/position"", ""action"" : ""point"" },
                            { ""path"" : ""<Mouse>/leftButton"", ""action"" : ""leftClick"" },
                            { ""path"" : ""<Pen>/tip"", ""action"" : ""leftClick"" },
                            { ""path"" : ""<Touchscreen>/touch*/press"", ""action"" : ""leftClick"" },
                            { ""path"" : ""<TrackedDevice>/button"", ""action"" : ""leftClick"" },
                            { ""path"" : ""<GenericDeviceWithPointingAbility>/click"", ""action"" : ""leftClick"" },
                            { ""path"" : ""<Mouse>/rightButton"", ""action"" : ""rightClick"" },
                            { ""path"" : ""<Pen>/barrel0"", ""action"" : ""rightClick"" },
                            { ""path"" : ""<Mouse>/middleButton"", ""action"" : ""middleClick"" },
                            { ""path"" : ""<Pen>/barrel1"", ""action"" : ""middleClick"" },
                            { ""path"" : ""<Mouse>/scroll"", ""action"" : ""scroll"" },
                            { ""path"" : ""<GenericDeviceWithPointingAbility>/scroll"", ""action"" : ""scroll"" },
                            { ""path"" : ""<TrackedDevice>/devicePosition"", ""action"" : ""position"" },
                            { ""path"" : ""<TrackedDevice>/deviceRotation"", ""action"" : ""orientation"" }
                        ]
                    }
                ]
            }
        ";

        var actions = InputActionAsset.FromJson(kActions);

        var pointAction = actions["point"];
        var leftClickAction = actions["leftClick"];
        var rightClickAction = actions["rightClick"];
        var middleClickAction = actions["middleClick"];
        var scrollAction = actions["scroll"];
        var positionAction = actions["position"];
        var orientationAction = actions["orientation"];

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(leftClickAction);
        scene.uiModule.middleClick = InputActionReference.Create(middleClickAction);
        scene.uiModule.rightClick = InputActionReference.Create(rightClickAction);
        scene.uiModule.scrollWheel = InputActionReference.Create(scrollAction);
        scene.uiModule.trackedDevicePosition = InputActionReference.Create(positionAction);
        scene.uiModule.trackedDeviceOrientation = InputActionReference.Create(orientationAction);

        actions.Enable();

        var clickControl = (ButtonControl)leftClickAction.controls[0];
        if (clickButton == PointerEventData.InputButton.Right)
            clickControl = (ButtonControl)rightClickAction.controls[0];
        else if (clickButton == PointerEventData.InputButton.Middle)
            clickControl = (ButtonControl)middleClickAction.controls[0];

        if (isTracked)
        {
            Set(device, "deviceRotation", trackedOrientation);
            Set(device, "devicePosition", trackedPosition);
        }

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Reset initial selection.
        scene.leftChildReceiver.events.Clear();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.False);

        var firstScreenPosition = new Vector2(100, 100);
        var secondScreenPosition = new Vector2(100, 200);
        var thirdScreenPosition = new Vector2(350, 200);

        if (isTracked)
        {
            firstScreenPosition = new Vector2(28.936f, 240);
            secondScreenPosition = new Vector2(80.006f, 240);
            thirdScreenPosition = new Vector2(560, 240);
        }

        // Move pointer over left child.
        currentTime = 1;
        runtime.unscaledGameTime = 1;
        if (isTouch)
        {
            BeginTouch(1, firstScreenPosition);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, -35, 0);
            Set(device, "deviceRotation", trackedOrientation);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], firstScreenPosition);
        }
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(isTouch ? 3 : 1));
        Assert.That(scene.parentReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);

        // For both regular pointers and touch, pointer enter is the first event.
        // NOTE: This is different to StandaloneInputModule where for mouse, click comes before pointer enter.
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(device));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(pointerType));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.touchId, Is.EqualTo(touchId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(trackedOrientation));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDevicePosition, Is.EqualTo(trackedPosition));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(Vector2.zero));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.Zero);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.Zero);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.False);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.eligibleForClick, Is.False);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.Empty); // The object is added *after* the event has been processed.
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        // Pointer enter event should also have been sent to parent.
        Assert.That(scene.parentReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.parentReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.parentReceiver.events[0].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.parentReceiver.events[0].pointerData.delta, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.parentReceiver.events[0].pointerData.pressPosition, Is.EqualTo(Vector2.zero));
        Assert.That(scene.parentReceiver.events[0].pointerData.clickTime, Is.Zero);
        Assert.That(scene.parentReceiver.events[0].pointerData.clickCount, Is.Zero);
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerDrag, Is.Null);
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerPress, Is.Null);
        Assert.That(scene.parentReceiver.events[0].pointerData.rawPointerPress, Is.Null);
        Assert.That(scene.parentReceiver.events[0].pointerData.lastPress, Is.Null);
        Assert.That(scene.parentReceiver.events[0].pointerData.dragging, Is.False);
        Assert.That(scene.parentReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.parentReceiver.events[0].pointerData.eligibleForClick, Is.False);
        Assert.That(scene.parentReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject })); // Added as we walk up the hierarchy.
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.Null);
        Assert.That(scene.parentReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        scene.parentReceiver.events.Clear();

        if (isTouch)
        {
            // Touch has no ability to point without pressing so pointer enter event is followed
            // right by pointer down event.

            // Pointer Down.
            Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.PointerDown));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.Null); // This actually means lastPointerPress, i.e. last value of pointerPress before current.
            Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            // Initialize Potential Drag.
            Assert.That(scene.leftChildReceiver.events[2].type, Is.EqualTo(EventType.InitializePotentialDrag));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.clickTime, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.clickCount, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            // For mouse, pen, and tracked, next we click.

            scene.leftChildReceiver.events.Clear();

            PressAndRelease(clickControl);
            scene.eventSystem.InvokeUpdate();

            Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(4));

            Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerDown));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.Zero); // Click detection comes after pointer down event.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null); // This actually means lastPointerPress, i.e. last value of pointerPress before current.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.InitializePotentialDrag));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.leftChildReceiver.events[2].type, Is.EqualTo(EventType.PointerUp));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.clickTime, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.clickCount, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[2].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.leftChildReceiver.events[3].type, Is.EqualTo(EventType.PointerClick));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.clickTime, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.clickCount, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[3].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[3].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[3].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[3].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.rightChildReceiver.events, Is.Empty);

            scene.leftChildReceiver.events.Clear();

            // Press again to start drag.
            runtime.unscaledGameTime = 1.2f; // Advance so we can tell whether this got picked up by click detection (but stay below clickSpeed).
            Press(clickControl);
            scene.eventSystem.InvokeUpdate();

            Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerDown));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(1));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.InitializePotentialDrag));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(1.2f)); // Click detection ran in-between PointerDown and InitializePotentialDrag.
            Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(2));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.Null); // See PointerModel.ButtonState.CopyPressStateTo.
            Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            Assert.That(scene.rightChildReceiver.events, Is.Empty);
        }

        scene.leftChildReceiver.events.Clear();
        var clickCount = isTouch ? 1 : 2;
        var clickTime = isTouch ? 1f : 1.2f;

        // Move. Still over left object.
        runtime.unscaledGameTime = 2;
        if (isTouch)
        {
            MoveTouch(1, secondScreenPosition);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, -30, 0);
            Set(device, "deviceRotation", trackedOrientation);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], secondScreenPosition);
        }
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(device));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(clickButton));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(pointerType));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.touchId, Is.EqualTo(touchId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(trackedOrientation));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDevicePosition, Is.EqualTo(trackedPosition));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(secondScreenPosition - firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.False);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.eligibleForClick, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(device));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(pointerType));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.touchId, Is.EqualTo(touchId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(secondScreenPosition - firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(clickTime));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(clickCount));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.eligibleForClick, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        Assert.That(scene.rightChildReceiver.events, Is.Empty);
        Assert.That(scene.parentReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move over to right object.
        runtime.unscaledGameTime = 2.5f;
        if (isTouch)
        {
            MoveTouch(1, thirdScreenPosition);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, 30, 0);
            Set(device, "deviceRotation", trackedOrientation);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], thirdScreenPosition);
        }
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);
        Assert.That(scene.parentReceiver.events, Is.Empty); // Should not have seen pointer enter/exit on parent.

        // Input module (like StandaloneInputModule on mouse path) processes move first which is why
        // we get an exit *before* a drag even though it would make more sense the other way round.

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerExit));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(thirdScreenPosition - secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject })); // Updated from pointer enter.
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        // Moves (pointer enter/exit) are always keyed to the left button. So when clicking with another one,
        // press positions on the moves will be zero.
        if (clickButton == PointerEventData.InputButton.Left)
        {
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.True);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(default(Vector2)));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition, Is.EqualTo(default(Vector2)));
        }

        // The drag is sent after the PointerEnter below so it already has the information from the pointer enter.
        Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(thirdScreenPosition - secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(clickTime));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(clickCount));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.delta, Is.EqualTo(thirdScreenPosition - secondScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.rightChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.parentGameObject })); // Right GO gets added after.
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        if (clickButton == PointerEventData.InputButton.Left)
        {
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.dragging, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.lastPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(default(Vector2)));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.Zero);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.Zero);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.dragging, Is.False);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.rawPointerPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.lastPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition, Is.EqualTo(default(Vector2)));
        }

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Release.
        if (isTouch)
            EndTouch(1, thirdScreenPosition);
        else
            Release(clickControl);
        scene.eventSystem.InvokeUpdate();

        // Left child should have seen pointer up and end drag.
        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(2));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerUp));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.button, Is.EqualTo(clickButton));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(isTouch ? null : scene.rightGameObject)); // Pointer-exit comes before pointer-up.
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(isTouch ? Enumerable.Empty<GameObject>() : new[] { scene.rightGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.EndDrag));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.delta, Is.EqualTo(Vector2.zero));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(clickTime));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(clickCount));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(isTouch ? null : scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.SameAs(scene.leftGameObject)); // Remembers last pointerPress.
        Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(isTouch ? Enumerable.Empty<GameObject>() : new[] { scene.rightGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        // For touch, the pointer ceases to exist so we get exit events.
        if (isTouch)
        {
            Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.False);

            Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(2));
            Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.PointerExit));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject)); // Reset after event.
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject)); // Drop not yet processed.
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.lastPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.dragging, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            // Parent should have seen an exit, too.
            Assert.That(scene.parentReceiver.events, Has.Count.EqualTo(1));
            Assert.That(scene.parentReceiver.events[0].type, Is.EqualTo(EventType.PointerExit));
            Assert.That(scene.parentReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.parentReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.parentReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.parentReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.parentReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.parentReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject)); // Reset after event.
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject)); // Drop not yet processed.
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.parentReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.parentReceiver.events[0].pointerData.lastPress, Is.Null);
            Assert.That(scene.parentReceiver.events[0].pointerData.dragging, Is.True);
            Assert.That(scene.parentReceiver.events[0].pointerData.useDragThreshold, Is.True);
            ////REVIEW: This behavior is inconsistent between "normal" pointer-enter/exit sequences but is consistent with what StandaloneInputModule does.
            ////        However, it seems wrong that on one path, GOs are removed one-by-one from `hovered` as the callbacks step through the hierarchy, whereas
            ////        on the other path, the list stays unmodified until the end and is then cleared en-bloc.
            Assert.That(scene.parentReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.parentReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            // Right child should have seen drop.
            Assert.That(scene.rightChildReceiver.events[1].type, Is.EqualTo(EventType.Drop));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(null));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerPress, Is.SameAs(scene.leftGameObject)); // For the drop, this is still set.
            Assert.That(scene.rightChildReceiver.events[1].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.lastPress, Is.Null); // See PointerModel.ButtonState.CopyPressStateTo.
            Assert.That(scene.rightChildReceiver.events[1].pointerData.dragging, Is.True);
            Assert.That(scene.rightChildReceiver.events[1].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.rightChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(Enumerable.Empty<GameObject>()));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            // For mouse and pen, pointer stays put.
            Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);
            Assert.That(scene.parentReceiver.events, Is.Empty); // No change on parent.

            // Right child should have seen drop.
            Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.Drop));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.button, Is.EqualTo(clickButton));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject)); // For the drop, this is still set.
            Assert.That(scene.rightChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.lastPress, Is.Null); // See PointerModel.ButtonState.CopyPressStateTo.
            Assert.That(scene.rightChildReceiver.events[0].pointerData.dragging, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }

        scene.parentReceiver.events.Clear();
        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Scroll.
        if (scrollAction.controls.Count > 0)
        {
            Set((Vector2Control)scrollAction.controls[0], Vector2.one);
            scene.eventSystem.InvokeUpdate();

            Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
            Assert.That(scene.leftChildReceiver.events, Is.Empty);
            Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(1));
            Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.Scroll));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left)); // Scrolls should always "come from" left button.
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.position, Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.scrollDelta, Is.EqualTo(Vector2.one * (1 / InputSystemUIInputModule.kPixelPerLine)).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.rawPointerPress, Is.Null);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.lastPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.dragging, Is.False);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.rightChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
            Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
            // Same logic as for moves, scrolls are always keyed to left button.
            if (clickButton == PointerEventData.InputButton.Left)
            {
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
                Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.EqualTo(clickTime));
                Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.EqualTo(clickCount));
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
                    Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            }
            else
            {
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pressPosition, Is.EqualTo(default(Vector2)));
                Assert.That(scene.rightChildReceiver.events[0].pointerData.clickTime, Is.Zero);
                Assert.That(scene.rightChildReceiver.events[0].pointerData.clickCount, Is.Zero);
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.Null);
                Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition, Is.EqualTo(default(Vector2)));
            }
        }
    }

    [UnityTest]
    [Category("UI")]
    [TestCase(UIPointerBehavior.SingleUnifiedPointer, ExpectedResult = -1)]
    [TestCase(UIPointerBehavior.AllPointersAsIs, ExpectedResult = -1)]
    [TestCase(UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack, ExpectedResult = -1)]
    public IEnumerator UI_CanDriveUIFromMultiplePointers(UIPointerBehavior pointerBehavior)
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);

        // Go crazy and hook the UI up with two mice, two touchscreens, and two tracked devices.
        // NOTE: Ignores pens as to the UI they are not really any different to mice.
        var mouse1 = InputSystem.AddDevice<Mouse>();
        var mouse2 = InputSystem.AddDevice<Mouse>();
        var touch1 = InputSystem.AddDevice<Touchscreen>();
        var touch2 = InputSystem.AddDevice<Touchscreen>();
        var trackedDevice1 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");
        var trackedDevice2 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");

        var scene = CreateScene();
        scene.uiModule.pointerBehavior = pointerBehavior;

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough);
        var clickAction = uiActions.AddAction("click", type: InputActionType.PassThrough);
        var positionAction =
            uiActions.AddAction("position", type: InputActionType.PassThrough, binding: "<TrackedDevice>/devicePosition");
        var orientationAction =
            uiActions.AddAction("orientation", type: InputActionType.PassThrough, binding: "<TrackedDevice>/deviceRotation");

        pointAction.AddBinding("<Mouse>/position");
        pointAction.AddBinding("<Pen>/position");
        pointAction.AddBinding("<Touchscreen>/touch*/position");

        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Pen>/tip");
        clickAction.AddBinding("<Touchscreen>/touch*/press");
        clickAction.AddBinding("<TrackedDevice>/button");

        pointAction.Enable();
        clickAction.Enable();
        positionAction.Enable();
        orientationAction.Enable();

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);
        scene.uiModule.trackedDevicePosition = InputActionReference.Create(positionAction);
        scene.uiModule.trackedDeviceOrientation = InputActionReference.Create(orientationAction);

        yield return null;

        scene.leftChildReceiver.events.Clear();

        // Put mouse1 over left object.
        Set(mouse1.position, new Vector2(100, 100));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Put mouse2 over right object.
        Set(mouse2.position, new Vector2(350, 200));
        scene.eventSystem.InvokeUpdate();

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
                // Pointer-exit on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                break;

            case UIPointerBehavior.AllPointersAsIs:
                // No change on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events, Is.Empty);
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                break;
        }

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Put tracked device #1 over left object and tracked device #2 over right object.
        // Need two updates as otherwise we'd end up with just another pointer of the right object
        // which would not result in an event.
        Set(trackedDevice1, "deviceRotation", Quaternion.Euler(0, -30, 0));
        scene.eventSystem.InvokeUpdate();
        Set(trackedDevice2, "deviceRotation", Quaternion.Euler(0, 30, 0));
        scene.eventSystem.InvokeUpdate();

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
                // Pointer-exit on right, pointer-enter on left, pointer-exit on left, pointer-enter on right.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And // System transparently switched from mouse2 to trackedDevice1.
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 80f) && Mathf.Approximately(e.pointerData.position.y, 240))); // Exits at position of trackedDevice1.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 80f) && Mathf.Approximately(e.pointerData.position.y, 240)));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 560f) && Mathf.Approximately(e.pointerData.position.y, 240)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 560) && Mathf.Approximately(e.pointerData.position.y, 240)));
                break;

            case UIPointerBehavior.AllPointersAsIs:
            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
                if (pointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack)
                {
                    // Tracked device activity should have removed the MouseOrPen pointer and thus generated exit events on it.
                    Assert.That(scene.rightChildReceiver.events,
                        Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                            .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                            .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                }

                // Pointer-enter on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 80f) && Mathf.Approximately(e.pointerData.position.y, 240)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            Mathf.Approximately(e.pointerData.position.x, 560) && Mathf.Approximately(e.pointerData.position.y, 240)));
                break;
        }

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch right object on first touchscreen and left object on second touchscreen.
        BeginTouch(1, new Vector2(350, 200), screen: touch1);
        scene.eventSystem.InvokeUpdate();
        BeginTouch(1, new Vector2(100, 100), screen: touch2);
        scene.eventSystem.InvokeUpdate();

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
                ////REVIEW: this likely needs refinement; ATM the second touch leads to a drag as the position changes after a press has occurred
                // Pointer-down on right, pointer-exit on right, begin-drag on right, pointer-enter on left.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And // Transparently switched.
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And // Transparently switched.
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.BeginDrag).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
                break;

            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
            case UIPointerBehavior.AllPointersAsIs:
                // Pointer-enter on right, pointer-enter on left.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
                Assert.That(scene.leftChildReceiver.events, Has.None.With.Property("type").EqualTo(EventType.Dragging));
                Assert.That(scene.rightChildReceiver.events, Has.None.With.Property("type").EqualTo(EventType.Dragging));
                break;
        }
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromMultipleTouches()
    {
        var touchScreen = InputSystem.AddDevice<Touchscreen>();

        // Prevent default selection of left object. This means that we will not have to contend with selections at all
        // in this test as they are driven from UI objects and not by the input module itself.
        var scene = CreateScene(noFirstSelected: true);

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Touchscreen>/touch*/position");
        var leftClickAction = map.AddAction("leftClick", type: InputActionType.PassThrough, binding: "<Touchscreen>/touch*/press");

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(leftClickAction);

        map.Enable();

        yield return null;

        scene.leftChildReceiver.events.Clear();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.False);

        // Touch left object.
        BeginTouch(1, new Vector2(100, 100));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.False);

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(100, 100)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch right object.
        BeginTouch(2, new Vector2(350, 200));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.False);

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(350, 200)));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Drag left object over right object.
        MoveTouch(1, new Vector2(355, 210));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.False);

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.Dragging).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(355, 210)));
        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(355, 210)));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(355, 210)));

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch left object again.
        BeginTouch(3, new Vector2(123, 123));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.True);

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 3).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(123, 123)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // End second touch.
        EndTouch(2, new Vector2(355, 205));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.False);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.True);

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(355, 205)));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerUp).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(355, 205)));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Begin second touch again.
        BeginTouch(2, new Vector2(345, 195));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.True);

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(345, 195)));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == new Vector2(345, 195)));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromMultipleTrackedDevices()
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);

        var trackedDevice1 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");
        var trackedDevice2 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");

        var scene = CreateScene();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("map");
        asset.AddActionMap(map);
        var trackedPositionAction = map.AddAction("position", type: InputActionType.PassThrough, binding: "*/devicePosition");
        var trackedOrientationAction = map.AddAction("orientation", type: InputActionType.PassThrough, binding: "*/deviceRotation");
        var clickAction = map.AddAction("click", type: InputActionType.PassThrough, binding: "*/button");

        scene.uiModule.trackedDevicePosition = InputActionReference.Create(trackedPositionAction);
        scene.uiModule.trackedDeviceOrientation = InputActionReference.Create(trackedOrientationAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        map.Enable();

        // Point both devices away from objects.
        Set(trackedDevice1.deviceRotation, Quaternion.Euler(0, -90, 0));
        Set(trackedDevice2.deviceRotation, Quaternion.Euler(0, -90, 0));

        yield return null;

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Point first device at left child.
        Set(trackedDevice1.deviceRotation, Quaternion.Euler(0, -30, 0));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -30, 0)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Point second device at left child.
        Set(trackedDevice2.deviceRotation, Quaternion.Euler(0, -31, 0));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -31, 0)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Click button on first device.
        PressAndRelease((ButtonControl)trackedDevice1["button"]);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(4));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerDown));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -30, 0)));
        Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.InitializePotentialDrag));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -30, 0)));
        Assert.That(scene.leftChildReceiver.events[2].type, Is.EqualTo(EventType.PointerUp));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -30, 0)));
        Assert.That(scene.leftChildReceiver.events[3].type, Is.EqualTo(EventType.PointerClick));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -30, 0)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Click button on second device.
        PressAndRelease((ButtonControl)trackedDevice2["button"]);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(4));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerDown));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -31, 0)));
        Assert.That(scene.leftChildReceiver.events[1].type, Is.EqualTo(EventType.InitializePotentialDrag));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -31, 0)));
        Assert.That(scene.leftChildReceiver.events[2].type, Is.EqualTo(EventType.PointerUp));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[2].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -31, 0)));
        Assert.That(scene.leftChildReceiver.events[3].type, Is.EqualTo(EventType.PointerClick));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[3].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, -31, 0)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Point first device at right child.
        Set(trackedDevice1.deviceRotation, Quaternion.Euler(0, 30, 0));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerExit));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, 30, 0)));
        Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice1.deviceId));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice1));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, 30, 0)));

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Point second device at right child.
        Set(trackedDevice2.deviceRotation, Quaternion.Euler(0, 31, 0));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.PointerExit));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, 31, 0)));
        Assert.That(scene.rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.rightChildReceiver.events[0].type, Is.EqualTo(EventType.PointerEnter));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerId, Is.EqualTo(trackedDevice2.deviceId));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.device, Is.SameAs(trackedDevice2));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerType, Is.EqualTo(UIPointerType.Tracked));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.trackedDeviceOrientation, Is.EqualTo(Quaternion.Euler(0, 31, 0)));
    }

    // There's nothing preventing the user from binding actions for pointer-type input to devices that aren't pointers.
    // For example, the setup allows binding the leftClick action to the space bar. When pointer-type actions get triggered
    // from non-pointer devices, we need to decide what to do. What the UI input module does is try to find a pointer (classic
    // or tracked) into which to route the input. Only if it can't find an existing pointer to route the input into will it
    // resort to turning the non-pointer device into a (likely non-functional) pointer.
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanTriggerPointerClicksFromNonPointerDevices()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var scene = CreateScene();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("click", type: InputActionType.PassThrough, binding: "<Keyboard>/space");

        pointAction.Enable();
        clickAction.Enable();

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        yield return null;

        // Move mouse over right object.
        Set(mouse.position, new Vector2(350, 200));
        scene.eventSystem.InvokeUpdate();

        scene.rightChildReceiver.events.Clear();

        Press(keyboard.spaceKey);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.pointerId == mouse.deviceId).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.pointerType == UIPointerType.MouseOrPen).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.clickCount == 0));
    }

    [UnityTest]
    [Category("UI")]
    [Ignore("TODO")]
    public IEnumerator TODO_UI_CanGetLastUsedDevice()
    {
        yield return null;
        Assert.Fail();
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_ClickDraggingMouseDoesNotAllocateGCMemory()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var scene = CreateScene();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("click", type: InputActionType.PassThrough, binding: "<Mouse>/leftClick");

        pointAction.Enable();
        clickAction.Enable();

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        // Remove the test listeners as they will allocate GC memory.
        Component.DestroyImmediate(scene.leftChildReceiver);
        Component.DestroyImmediate(scene.rightChildReceiver);

        yield return null;

        // The first time we go through this, we allocate some memory (not garbage, just memory that is going to be reused over
        // and over). The events, the hovered list, stuff like that. So do one click drag as a dry run and then do it for real.
        //
        // NOTE: We also need to do this because we can't use Retry(2) -- as we normally do to warm up the JIT -- in combination
        //       with UnityTest. Doing so will flat out lead to InvalidCastExceptions in the test runner :(
        Set(mouse.position, new Vector2(100, 100));
        scene.eventSystem.InvokeUpdate();
        Press(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();
        Set(mouse.position, new Vector2(200, 200));
        scene.eventSystem.InvokeUpdate();
        Release(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        var kProfilerRegion = "UI_ClickDraggingDoesNotAllocateGCMemory";

        // Now for real.
        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            Set(mouse.position, new Vector2(100, 100));
            scene.eventSystem.InvokeUpdate();
            Press(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();
            Set(mouse.position, new Vector2(200, 200));
            scene.eventSystem.InvokeUpdate();
            Release(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();

            // And just for kicks, do it the opposite way, too.
            Set(mouse.position, new Vector2(200, 200));
            scene.eventSystem.InvokeUpdate();
            Press(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();
            Set(mouse.position, new Vector2(100, 100));
            scene.eventSystem.InvokeUpdate();
            Release(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
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
        players[0].leftChildReceiver.events.Clear();
        players[1].leftChildReceiver.events.Clear();

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
        players[0].leftChildReceiver.events.Clear();

        Assert.That(players[0].rightChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(players[0].rightChildReceiver.events[0].type, Is.EqualTo(EventType.Select));
        players[0].rightChildReceiver.events.Clear();

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
        players[0].rightChildReceiver.events.Clear();

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
        players[1].leftChildReceiver.events.Clear();

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
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Get rid of deadzones to simplify test.
        InputSystem.settings.defaultDeadzoneMax = 1;
        InputSystem.settings.defaultDeadzoneMin = 0;

        var scene = CreateScene();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var moveAction = map.AddAction("move", type: InputActionType.PassThrough, binding: "<Gamepad>/*stick");
        var submitAction = map.AddAction("submit", type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");
        var cancelAction = map.AddAction("cancel", type: InputActionType.PassThrough, binding: "<Gamepad>/buttonEast");

        scene.uiModule.move = InputActionReference.Create(moveAction);
        scene.uiModule.submit = InputActionReference.Create(submitAction);
        scene.uiModule.cancel = InputActionReference.Create(cancelAction);

        scene.uiModule.moveRepeatDelay = 0.1f;
        scene.uiModule.moveRepeatRate = 0.1f;

        map.Enable();

        yield return null;

        ////TODO: test for IUpdateSelectedHandler

        // Left object should have been selected as part of enabling the event system and module.
        // This is key to having navigation work.
        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Select));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move right.
        Set(gamepad.leftStick, new Vector2(1, 0.5f));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Right));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(1, 0.5f).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move left.
        Set(gamepad.leftStick, new Vector2(-1, 0.5f));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Left));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(-1, 0.5f).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move up.
        Set(gamepad.leftStick, new Vector2(-0.5f, 1));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Up));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(-0.5f, 1).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move down.
        Set(gamepad.leftStick, new Vector2(0.5f, -1));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Down));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(0.5f, -1).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Check repeat delay. Wait enough to cross both delay and subsequent repeat. We should
        // still only get one repeat event.
        runtime.unscaledGameTime = 1.21f;
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Down));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(0.5f, -1).normalized).Using(Vector2EqualityComparer.Instance));

        scene.leftChildReceiver.events.Clear();

        // Check repeat rate. Same here; doesn't matter how much time we pass as long as we cross
        // the repeat rate threshold.
        runtime.unscaledGameTime = 2;
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Move));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveDir, Is.EqualTo(MoveDirection.Down));
        Assert.That(scene.leftChildReceiver.events[0].axisData.moveVector, Is.EqualTo(new Vector2(0.5f, -1).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Submit.
        PressAndRelease(gamepad.buttonSouth);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Submit));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Cancel.
        PressAndRelease(gamepad.buttonEast);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
        Assert.That(scene.leftChildReceiver.events[0].type, Is.EqualTo(EventType.Cancel));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Make sure that no navigation events are sent if turned off.
        scene.eventSystem.sendNavigationEvents = false;

        Set(gamepad.leftStick, default);
        scene.eventSystem.InvokeUpdate();
        Set(gamepad.leftStick, new Vector2(1, 1));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Is.Empty);
        Assert.That(scene.rightChildReceiver.events, Is.Empty);
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

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanChangeControlsOnActions()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var scene = CreateScene();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = map.AddAction("click", type: InputActionType.PassThrough, binding: "<Mouse>/leftButton");

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        map.Enable();

        yield return null;

        // Put mouse over left object.
        Set(mouse.position, new Vector2(100, 100));
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter));

        scene.leftChildReceiver.events.Clear();

        // Unbind click. This should not yet result in the pointer getting removed as we still
        // have the position binding.
        clickAction.ApplyBindingOverride(string.Empty);

        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        // Remove mouse. Should result in pointer-exit event.
        InputSystem.RemoveDevice(mouse);

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse));
    }

    [UnityTest]
    [Category("UI")]
    [Ignore("TODO")]
    public IEnumerator TODO_UI_WhenEnabled_InitialPointerPositionIsPickedUp()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        // Put mouse over left object.
        Set(mouse.position, new Vector2(100, 100));

        var scene = CreateScene();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        scene.uiModule.point = InputActionReference.Create(pointAction);
        asset.Enable();

        yield return null;

        scene.leftChildReceiver.events.Clear();

        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.leftChildReceiver.events, Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);
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

        var scene = CreateScene(0, 200);

        scene.uiModule.actionsAsset = actions;
        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        // Deselect behavior should be on by default as this corresponds to the behavior before
        // we introduced the switch that allows toggling the behavior off.
        Assert.That(scene.uiModule.deselectOnBackgroundClick, Is.True);

        // Give canvas a chance to set itself up.
        yield return null;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, new Vector2(10, 10));
        PressAndRelease(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));

        // Click outside of GOs and make sure the selection gets cleared.
        Set(mouse.position, new Vector2(50, 250));
        PressAndRelease(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.Null);

        scene.uiModule.deselectOnBackgroundClick = false;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, new Vector2(10, 10));
        PressAndRelease(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));

        // Click outside of GOs and make sure our selection does NOT get cleared.
        Set(mouse.position, new Vector2(50, 250));
        PressAndRelease(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));
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

    private const string kTrackedDeviceWithButton = @"
        {
            ""name"" : ""TrackedDeviceWithButton"",
            ""extend"" : ""TrackedDevice"",
            ""controls"" : [
                { ""name"" : ""button"", ""layout"" : ""Button"" }
            ]
        }
    ";

    // Random device that can point and click. Just to make sure the code won't get confused when dealing with
    // something other than Pointers and TrackedDevices.
    private const string kGenericDeviceWithPointingAbility = @"
        {
            ""name"" : ""GenericDeviceWithPointingAbility"",
            ""controls"" : [
                { ""name"" : ""position"", ""layout"" : ""Vector2"" },
                { ""name"" : ""scroll"", ""layout"" : ""Vector2"" },
                { ""name"" : ""click"", ""layout"" : ""Button"" }
            ]
        }
    ";

    private enum EventType
    {
        PointerClick,
        PointerDown,
        PointerUp,
        PointerEnter,
        PointerExit,
        Select,
        Deselect,
        InitializePotentialDrag,
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
            public EventType type { get; }
            public BaseEventData data { get; }
            public AxisEventData axisData => (AxisEventData)data;
            public ExtendedPointerEventData pointerData => (ExtendedPointerEventData)data;

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

        public void OnPointerClick(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerClick, ClonePointerEventData(eventData)));
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerDown, ClonePointerEventData(eventData)));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerEnter, ClonePointerEventData(eventData)));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerExit, ClonePointerEventData(eventData)));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerUp, ClonePointerEventData(eventData)));
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
            events.Add(new Event(EventType.InitializePotentialDrag, ClonePointerEventData(eventData)));
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

        private static AxisEventData CloneAxisEventData(AxisEventData eventData)
        {
            return new AxisEventData(EventSystem.current)
            {
                moveVector = eventData.moveVector,
                moveDir = eventData.moveDir
            };
        }

        private static ExtendedPointerEventData ClonePointerEventData(PointerEventData eventData)
        {
            // InputSystemUIInputModule should only be sending ExtendedPointEventData.
            var extendedEventData = (ExtendedPointerEventData)eventData;
            var clone = new ExtendedPointerEventData(EventSystem.current)
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
                hovered = eventData.hovered.ToList(),
                pointerDrag = eventData.pointerDrag,
                pointerEnter = eventData.pointerEnter,
                pressPosition = eventData.pressPosition,
                pointerCurrentRaycast = eventData.pointerCurrentRaycast,
                pointerPressRaycast = eventData.pointerPressRaycast,
                rawPointerPress = eventData.rawPointerPress,
                useDragThreshold = eventData.useDragThreshold,
                device = extendedEventData.device,
                touchId = extendedEventData.touchId,
                pointerType = extendedEventData.pointerType,
                trackedDeviceOrientation = extendedEventData.trackedDeviceOrientation,
                trackedDevicePosition = extendedEventData.trackedDevicePosition
            };

            // Can't set lastPress directly.
            clone.pointerPress = eventData.lastPress;
            clone.pointerPress = eventData.pointerPress;

            return clone;
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
