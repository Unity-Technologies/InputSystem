using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Is = UnityEngine.TestTools.Constraints.Is;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;
#endif

#pragma warning disable CS0649
////TODO: app focus handling

internal class UITests : CoreTestsFixture
{
    private struct TestObjects
    {
        public Camera camera;
        public Canvas canvas;
        public InputSystemUIInputModule uiModule;
        public TestEventSystem eventSystem;
        public GameObject parentGameObject;
        public GameObject leftGameObject;
        public GameObject rightGameObject;
        public UICallbackReceiver parentReceiver;
        public UICallbackReceiver leftChildReceiver;
        public UICallbackReceiver rightChildReceiver;
        public DefaultInputActions actions;

        // Assume a 640x480 resolution and translate the given coordinates from a resolution
        // in that space to coordinates in the current camera screen space.
        public Vector2 From640x480ToScreen(float x, float y)
        {
            var cameraRect = camera.rect;
            var cameraPixelRect = camera.pixelRect;

            var result = new Vector2(cameraPixelRect.x + x / 640f * cameraRect.width * cameraPixelRect.width,
                cameraPixelRect.y + y / 480f * cameraRect.height * cameraPixelRect.height);

            // Pixel-snap. Not sure where this is coming from but Mac tests are failing without this.
            return new Vector2(Mathf.Floor(result.x), Mathf.Floor(result.y));
        }

        public bool IsWithinRect(Vector2 screenPoint, GameObject gameObject)
        {
            var transform = gameObject.GetComponent<RectTransform>();
            return RectTransformUtility.RectangleContainsScreenPoint(transform, screenPoint, camera, default);
        }

        public void ClearEvents()
        {
            parentReceiver.events.Clear();
            leftChildReceiver.events.Clear();
            rightChildReceiver.events.Clear();
        }
    }

    [SetUp]
    public override void Setup()
    {
        base.Setup();
    }

    private static TestObjects CreateUIScene()
    {
        var scene = CreateTestUI();
        scene.actions = new DefaultInputActions();

        scene.uiModule.point = InputActionReference.Create(scene.actions.UI.Point);
        scene.uiModule.leftClick = InputActionReference.Create(scene.actions.UI.Click);

        scene.actions.UI.Enable();

        return scene;
    }

    // Set up a InputSystemUIInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    private static TestObjects CreateTestUI(Rect viewport = default, bool noFirstSelected = false, string namePrefix = "", bool makeSelectable = false)
    {
        var objects = new TestObjects();

        // Set up GameObject with EventSystem.
        var systemObject = new GameObject(namePrefix + "System");
        objects.eventSystem = systemObject.AddComponent<TestEventSystem>();
        var uiModule = systemObject.AddComponent<InputSystemUIInputModule>();
        uiModule.UnassignActions();
        objects.uiModule = uiModule;
        objects.eventSystem.UpdateModules();

        var cameraObject = new GameObject(namePrefix + "Camera");
        objects.camera = cameraObject.AddComponent<Camera>();
        objects.camera.stereoTargetEye = StereoTargetEyeMask.None;
        objects.camera.rect = viewport == default ? new Rect(0, 0, 1, 1) : viewport;

        var canvasObject = new GameObject(namePrefix + "Canvas");
        canvasObject.SetActive(false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceRaycaster>();
        canvas.worldCamera = objects.camera;
        objects.canvas = canvas;

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject(namePrefix + "Parent");
        parentGameObject.SetActive(false);
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        objects.parentGameObject = parentGameObject;
        objects.parentReceiver = parentGameObject.AddComponent<UICallbackReceiver>();

        var leftChildGameObject = new GameObject(namePrefix + "Left Child");
        leftChildGameObject.SetActive(false);
        var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
        leftChildGameObject.AddComponent<Image>();
        objects.leftChildReceiver = leftChildGameObject.AddComponent<UICallbackReceiver>();
        objects.leftGameObject = leftChildGameObject;
        if (makeSelectable)
            leftChildGameObject.AddComponent<Selectable>();

        var rightChildGameObject = new GameObject(namePrefix + "Right Child");
        rightChildGameObject.SetActive(false);
        var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
        rightChildGameObject.AddComponent<Image>();
        objects.rightChildReceiver = rightChildGameObject.AddComponent<UICallbackReceiver>();
        objects.rightGameObject = rightChildGameObject;
        if (makeSelectable)
            rightChildGameObject.AddComponent<Selectable>();

        parentTransform.SetParent(canvasObject.transform, worldPositionStays: false);
        leftChildTransform.SetParent(parentTransform, worldPositionStays: false);
        rightChildTransform.SetParent(parentTransform, worldPositionStays: false);

        // Parent occupies full space of canvas.
        parentTransform.anchoredPosition = default;
        parentTransform.anchorMin = Vector2.zero;
        parentTransform.anchorMax = Vector2.one;
        parentTransform.sizeDelta = default;

        // Left child occupies left half of parent.
        leftChildTransform.anchoredPosition = default;
        leftChildTransform.anchorMin = default;
        leftChildTransform.anchorMax = new Vector2(0.5f, 1);
        leftChildTransform.sizeDelta = default;

        // Right child occupies right half of parent.
        rightChildTransform.anchoredPosition = default;
        rightChildTransform.anchorMin = new Vector2(0.5f, 0);
        rightChildTransform.anchorMax = new Vector2(1, 1);
        rightChildTransform.sizeDelta = default;

        canvasObject.SetActive(true);
        parentGameObject.SetActive(true);
        leftChildGameObject.SetActive(true);
        rightChildGameObject.SetActive(true);

        objects.eventSystem.playerRoot = parentGameObject;
        if (!noFirstSelected)
            objects.eventSystem.firstSelectedGameObject = leftChildGameObject;
        objects.eventSystem.InvokeUpdate(); // Initial update only sets current module.

        return objects;
    }

    [Test]
    [Category("UI")]
    public void UI_InputModuleHasDefaultActions()
    {
        var go = new GameObject();
        var uiModule = go.AddComponent<InputSystemUIInputModule>();

        Assert.That(uiModule.actionsAsset, Is.Not.Null);
        Assert.That(uiModule.point?.action, Is.SameAs(uiModule.actionsAsset["UI/Point"]));
        Assert.That(uiModule.leftClick?.action, Is.SameAs(uiModule.actionsAsset["UI/Click"]));
        Assert.That(uiModule.rightClick?.action, Is.SameAs(uiModule.actionsAsset["UI/RightClick"]));
        Assert.That(uiModule.middleClick?.action, Is.SameAs(uiModule.actionsAsset["UI/MiddleClick"]));
        Assert.That(uiModule.scrollWheel?.action, Is.SameAs(uiModule.actionsAsset["UI/ScrollWheel"]));
        Assert.That(uiModule.submit?.action, Is.SameAs(uiModule.actionsAsset["UI/Submit"]));
        Assert.That(uiModule.cancel?.action, Is.SameAs(uiModule.actionsAsset["UI/Cancel"]));
        Assert.That(uiModule.move?.action, Is.SameAs(uiModule.actionsAsset["UI/Navigate"]));
        Assert.That(uiModule.trackedDeviceOrientation?.action, Is.SameAs(uiModule.actionsAsset["UI/TrackedDeviceOrientation"]));
        Assert.That(uiModule.trackedDevicePosition?.action, Is.SameAs(uiModule.actionsAsset["UI/TrackedDevicePosition"]));

        uiModule.UnassignActions();

        Assert.That(uiModule.actionsAsset, Is.Null);
        Assert.That(uiModule.point, Is.Null);
        Assert.That(uiModule.leftClick, Is.Null);
        Assert.That(uiModule.rightClick, Is.Null);
        Assert.That(uiModule.middleClick, Is.Null);
        Assert.That(uiModule.scrollWheel, Is.Null);
        Assert.That(uiModule.submit, Is.Null);
        Assert.That(uiModule.cancel, Is.Null);
        Assert.That(uiModule.move, Is.Null);
        Assert.That(uiModule.trackedDeviceOrientation, Is.Null);
        Assert.That(uiModule.trackedDevicePosition, Is.Null);
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
#if UNITY_IOS || UNITY_TVOS
    [Ignore("Failing on iOS https://jira.unity3d.com/browse/ISX-448")]
#endif
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

        var scene = CreateTestUI();

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
            Set(device, "deviceRotation", trackedOrientation, queueEventOnly: true);
            Set(device, "devicePosition", trackedPosition, queueEventOnly: true);
        }

        // We need to wait a frame to let the underlying canvas update and properly order the graphics images for raycasting.
        yield return null;

        // Reset initial selection.
        scene.leftChildReceiver.events.Clear();

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.False);

        var firstScreenPosition = scene.From640x480ToScreen(100, 100);
        var secondScreenPosition = scene.From640x480ToScreen(100, 200);
        var thirdScreenPosition = scene.From640x480ToScreen(350, 200);

        var clickTime = 0f;
        var clickCount = 0;

        // Move pointer over left child.
        currentTime = 1;
        unscaledGameTime = 1;
        if (isTouch)
        {
            BeginTouch(1, firstScreenPosition, queueEventOnly: true);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, -35, 0);
            Set(device, "deviceRotation", trackedOrientation, queueEventOnly: true);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], firstScreenPosition, queueEventOnly: true);
        }
        yield return null;

        const int kHaveMovementEvents =
#if UNITY_2021_2_OR_NEWER
            1
#else
            0
#endif
        ;

        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo((isTouch ? 3 : 1) + kHaveMovementEvents));
        Assert.That(scene.parentReceiver.events, Has.Count.EqualTo(1 + kHaveMovementEvents));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);

        if (isTracked)
        {
            // Different screen geometries will lead to different ray intersection points from tracked devices.
            // Only check whether we reported a position inside of leftGameObject.
            Assert.That(scene.IsWithinRect(scene.leftChildReceiver.events[0].pointerData.position, scene.leftGameObject), Is.True);

            firstScreenPosition = scene.leftChildReceiver.events[0].pointerData.position;
        }

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
        ////REVIEW: For touch, should the initial pointer event really have a delta or should it be (0,0) when we first touch the screen?
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

#if UNITY_2021_2_OR_NEWER
            // PointerMove.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].type, Is.EqualTo(EventType.PointerMove));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.delta, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance)); // Same as PointerEnter.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pressPosition, Is.EqualTo(default(Vector2)));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.clickTime, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.rawPointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.lastPress, Is.Null); // This actually means lastPointerPress, i.e. last value of pointerPress before current.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.eligibleForClick, Is.False);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.hovered, Is.Empty); // Same as PointerEnter.
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerPressRaycast.gameObject, Is.Null);
            Assert.That(scene.leftChildReceiver.events[0 + kHaveMovementEvents].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(default(Vector2)).Using(Vector2EqualityComparer.Instance));
#endif

            // PointerDown.
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].type, Is.EqualTo(EventType.PointerDown));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.device, Is.SameAs(device));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.clickTime, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerDrag, Is.Null);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.rawPointerPress, Is.Null); // This is set only after the event has been processed.
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.lastPress, Is.Null); // This actually means lastPointerPress, i.e. last value of pointerPress before current.
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.useDragThreshold, Is.True);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[1 + kHaveMovementEvents].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

            // InitializePotentialDrag.
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].type, Is.EqualTo(EventType.InitializePotentialDrag));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.button, Is.EqualTo(PointerEventData.InputButton.Left));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerId, Is.EqualTo(pointerId));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.touchId, Is.EqualTo(touchId));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerType, Is.EqualTo(pointerType));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.position, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.delta, Is.EqualTo(Vector2.zero));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pressPosition, Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.clickTime, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.clickCount, Is.Zero);
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerEnter, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.lastPress, Is.Null);
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.dragging, Is.False);
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.useDragThreshold, Is.False); // We set it in OnInitializePotentialDrag.
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.eligibleForClick, Is.True);
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.hovered, Is.EquivalentTo(new[] { scene.leftGameObject, scene.parentGameObject }));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerCurrentRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
            Assert.That(scene.leftChildReceiver.events[2 + kHaveMovementEvents].pointerData.pointerPressRaycast.screenPosition,
                Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));
        }
        else
        {
            // For mouse, pen, and tracked, next we click.

            scene.leftChildReceiver.events.Clear();

            PressAndRelease(clickControl, queueEventOnly: true);
            yield return null;

            clickTime = unscaledGameTime;
            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("button", clickButton),
                    AllEvents("pointerId", pointerId),
                    AllEvents("touchId", touchId),
                    AllEvents("device", device),
                    AllEvents("pointerType", pointerType),
                    AllEvents("position", firstScreenPosition),
                    AllEvents("delta", Vector2.zero),
                    AllEvents("pressPosition", firstScreenPosition),
                    AllEvents("pointerEnter", scene.leftGameObject),
                    AllEvents("pointerCurrentRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerCurrentRaycast.screenPosition", firstScreenPosition),
                    AllEvents("pointerPressRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerPressRaycast.screenPosition", firstScreenPosition),
                    AllEvents("hovered", new[] { scene.leftGameObject, scene.parentGameObject }),
                    AllEvents("lastPress", null),
                    AllEvents("dragging", false),
                    AllEvents("eligibleForClick", true),

                    // PointerDown.
                    OneEvent("type", EventType.PointerDown),
                    OneEvent("clickTime", 0f),
                    OneEvent("clickCount", 0),
                    OneEvent("pointerDrag", null),
                    OneEvent("pointerPress", null),
                    OneEvent("rawPointerPress", null),
                    OneEvent("useDragThreshold", true),

                    // InitializePotentialDrag.
                    OneEvent("type", EventType.InitializePotentialDrag),
                    OneEvent("clickTime", 0f),
                    OneEvent("clickCount", 0),
                    OneEvent("pointerDrag", scene.leftGameObject),
                    OneEvent("pointerPress", scene.leftGameObject),
                    OneEvent("rawPointerPress", scene.leftGameObject),
                    OneEvent("useDragThreshold", false),

                    // PointerUp.
                    OneEvent("type", EventType.PointerUp),
                    OneEvent("clickTime", unscaledGameTime),
                    OneEvent("clickCount", 1),
                    OneEvent("pointerDrag", scene.leftGameObject),
                    OneEvent("pointerPress", scene.leftGameObject),
                    OneEvent("rawPointerPress", scene.leftGameObject),
                    OneEvent("useDragThreshold", false),

                    // PointerClick.
                    OneEvent("type", EventType.PointerClick),
                    OneEvent("clickTime", clickTime),
                    OneEvent("clickCount", 1),
                    OneEvent("pointerDrag", scene.leftGameObject),
                    OneEvent("pointerPress", scene.leftGameObject),
                    OneEvent("rawPointerPress", scene.leftGameObject),
                    OneEvent("useDragThreshold", false)
                )
            );

            Assert.That(scene.rightChildReceiver.events, Is.Empty);

            scene.leftChildReceiver.events.Clear();

            // Press again to start drag.
            unscaledGameTime = 1.2f; // Advance so we can tell whether this got picked up by click detection (but stay below clickSpeed).
            Press(clickControl, queueEventOnly: true);
            yield return null;

            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("button", clickButton),
                    AllEvents("pointerId", pointerId),
                    AllEvents("touchId", touchId),
                    AllEvents("device", device),
                    AllEvents("pointerType", pointerType),
                    AllEvents("position", firstScreenPosition),
                    AllEvents("pressPosition", firstScreenPosition),
                    AllEvents("delta", Vector2.zero),
                    AllEvents("pointerEnter", scene.leftGameObject),
                    AllEvents("dragging", false),
                    AllEvents("eligibleForClick", true),
                    AllEvents("hovered", new[] { scene.leftGameObject, scene.parentGameObject }),
                    AllEvents("pointerCurrentRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerCurrentRaycast.screenPosition", firstScreenPosition),
                    AllEvents("pointerPressRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerPressRaycast.screenPosition", firstScreenPosition),

                    // PointerDown.
                    OneEvent("type", EventType.PointerDown),
                    OneEvent("clickTime", clickTime),
                    OneEvent("clickCount", 1),
                    OneEvent("pointerDrag", null),
                    OneEvent("pointerPress", null), // This is set only after the event has been processed.
                    OneEvent("rawPointerPress", null), // This is set only after the event has been processed.
                    OneEvent("lastPress", scene.leftGameObject),
                    OneEvent("useDragThreshold", true),

                    // InitializePotentialDrag.
                    OneEvent("type", EventType.InitializePotentialDrag),
                    OneEvent("clickTime", clickTime),
                    OneEvent("clickCount", 1),
                    OneEvent("pointerDrag", scene.leftGameObject),
                    OneEvent("pointerPress", scene.leftGameObject),
                    OneEvent("rawPointerPress", scene.leftGameObject),
                    OneEvent("lastPress", null), // See PointerModel.ButtonState.CopyPressStateTo.
                    OneEvent("useDragThreshold", false) // We set it in OnInitializePotentialDrag.
                )
            );

            Assert.That(scene.rightChildReceiver.events, Is.Empty);
        }

        scene.leftChildReceiver.events.Clear();
        clickCount = isTouch ? 0 : 1;

        // Move. Still over left object.
        runtime.unscaledGameTime = 2;
        if (isTouch)
        {
            MoveTouch(1, secondScreenPosition, queueEventOnly: true);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, -30, 0);
            Set(device, "deviceRotation", trackedOrientation, queueEventOnly: true);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], secondScreenPosition, queueEventOnly: true);
        }
        yield return null;

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);

        if (isTracked)
        {
            // Different screen geometries will lead to different ray intersection points from tracked devices.
            // Only check whether we reported a position inside of leftGameObject.
            Assert.That(scene.IsWithinRect(scene.leftChildReceiver.events[0].pointerData.position, scene.leftGameObject), Is.True);

            secondScreenPosition = scene.leftChildReceiver.events[0].pointerData.position;
        }

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", pointerType),
                AllEvents("pointerId", pointerId),
                AllEvents("touchId", touchId),
                AllEvents("device", device),
                AllEvents("position", secondScreenPosition),
                AllEvents("delta", secondScreenPosition - firstScreenPosition),
                AllEvents("trackedDeviceOrientation", trackedOrientation),
                AllEvents("trackedDevicePosition", trackedPosition),
                AllEvents("pointerEnter", scene.leftGameObject),
                AllEvents("useDragThreshold", false), // We set it in OnInitializePotentialDrag.
                AllEvents("eligibleForClick", true),
                AllEvents("hovered", new[] { scene.leftGameObject, scene.parentGameObject }),
                AllEvents("pointerCurrentRaycast.gameObject", scene.leftGameObject),
                AllEvents("pointerCurrentRaycast.screenPosition", secondScreenPosition),

                // PointerMove.
#if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove),
                OneEvent("dragging", false),
                // Again, pointer movement is processed exclusively "from" the left button.
                OneEvent("button", PointerEventData.InputButton.Left),
                OneEvent("pressPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
                OneEvent("clickTime", clickButton == PointerEventData.InputButton.Left ? clickTime : 0f),
                OneEvent("clickCount", clickButton == PointerEventData.InputButton.Left ? clickCount : 0),
                OneEvent("pointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("rawPointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerDrag", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("lastPress", clickButton == PointerEventData.InputButton.Left ? null : scene.leftGameObject),
                OneEvent("pointerPressRaycast.gameObject", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerPressRaycast.screenPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
#endif

                // BeginDrag.
                OneEvent("type", EventType.BeginDrag),
                OneEvent("button", clickButton),
                OneEvent("dragging", false),
                OneEvent("pressPosition", firstScreenPosition),
                OneEvent("clickTime", clickTime),
                OneEvent("clickCount", clickCount),
                OneEvent("pointerPress", scene.leftGameObject),
                OneEvent("rawPointerPress", scene.leftGameObject),
                OneEvent("pointerDrag", scene.leftGameObject),
                OneEvent("lastPress", null),
                OneEvent("pointerPressRaycast.gameObject", scene.leftGameObject),
                OneEvent("pointerPressRaycast.screenPosition", firstScreenPosition),

                // Dragging.
                OneEvent("type", EventType.Dragging),
                OneEvent("button", clickButton),
                OneEvent("dragging", true),
                OneEvent("pressPosition", firstScreenPosition),
                OneEvent("clickTime", clickTime),
                OneEvent("clickCount", clickCount),
                OneEvent("pointerPress", scene.leftGameObject),
                OneEvent("rawPointerPress", scene.leftGameObject),
                OneEvent("pointerDrag", scene.leftGameObject),
                OneEvent("lastPress", null),
                OneEvent("pointerPressRaycast.gameObject", scene.leftGameObject),
                OneEvent("pointerPressRaycast.screenPosition", firstScreenPosition)
            )
        );

        Assert.That(scene.rightChildReceiver.events, Is.Empty);
        Assert.That(scene.parentReceiver.events,
            EventSequence(
#if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove)
#endif
            )
        );

        scene.leftChildReceiver.events.Clear();
        scene.parentReceiver.events.Clear();

        // Move over to right object.
        runtime.unscaledGameTime = 2.5f;
        if (isTouch)
        {
            MoveTouch(1, thirdScreenPosition, queueEventOnly: true);
        }
        else if (isTracked)
        {
            trackedOrientation = Quaternion.Euler(0, 30, 0);
            Set(device, "deviceRotation", trackedOrientation, queueEventOnly: true);
        }
        else
        {
            Set((Vector2Control)pointAction.controls[0], thirdScreenPosition, queueEventOnly: true);
        }
        yield return null;

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);
        // Should not have seen pointer enter/exit on parent (we only moved from one of its
        // children to another) but *should* have seen a move event.
        Assert.That(scene.parentReceiver.events,
            EventSequence(
#if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove)
#endif
            )
        );

        if (isTracked)
        {
            // Different screen geometries will lead to different ray intersection points from tracked devices.
            // Only check whether we reported a position inside of rightGameObject.
            Assert.That(scene.IsWithinRect(scene.leftChildReceiver.events[0].pointerData.position, scene.rightGameObject), Is.True);

            thirdScreenPosition = scene.leftChildReceiver.events[0].pointerData.position;
        }

        // Input module (like StandaloneInputModule on mouse path) processes move first which is why
        // we get an exit *before* a drag even though it would make more sense the other way round.

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerId", pointerId),
                AllEvents("position", thirdScreenPosition),
                AllEvents("delta", thirdScreenPosition - secondScreenPosition),
                AllEvents("useDragThreshold", false), // We set it in OnInitializePotentialDrag.
                AllEvents("pointerCurrentRaycast.gameObject", scene.rightGameObject),
                AllEvents("pointerCurrentRaycast.screenPosition", thirdScreenPosition),

                // Moves (pointer enter/exit) are always keyed to the left button. So when clicking with another one,
                // press positions on the moves will be zero.

                // PointerMove.
#if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove),
                OneEvent("button", PointerEventData.InputButton.Left),
                OneEvent("pointerEnter", scene.leftGameObject),
                OneEvent("hovered", new[] { scene.leftGameObject, scene.parentGameObject }),
                OneEvent("pressPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
                OneEvent("clickTime", clickButton == PointerEventData.InputButton.Left ? clickTime : 0f),
                OneEvent("clickCount", clickButton == PointerEventData.InputButton.Left ? clickCount : 0),
                OneEvent("pointerDrag", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("rawPointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("lastPress", clickButton == PointerEventData.InputButton.Left ? null : scene.leftGameObject),
                OneEvent("dragging", clickButton == PointerEventData.InputButton.Left ? true : false),
                OneEvent("pointerPressRaycast.gameObject", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerPressRaycast.screenPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
#endif

                // PointerExit.
                OneEvent("type", EventType.PointerExit),
                OneEvent("button", PointerEventData.InputButton.Left),
                OneEvent("pointerEnter", scene.leftGameObject),
                OneEvent("hovered", new[] { scene.leftGameObject, scene.parentGameObject }),
                OneEvent("pressPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
                OneEvent("clickTime", clickButton == PointerEventData.InputButton.Left ? clickTime : 0f),
                OneEvent("clickCount", clickButton == PointerEventData.InputButton.Left ? clickCount : 0),
                OneEvent("pointerDrag", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("rawPointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("lastPress", clickButton == PointerEventData.InputButton.Left ? null : scene.leftGameObject),
                OneEvent("dragging", clickButton == PointerEventData.InputButton.Left ? true : false),
                OneEvent("pointerPressRaycast.gameObject", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                OneEvent("pointerPressRaycast.screenPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),

                // Dragging.
                // The drag is sent after the PointerEnter is sent to the right child so it already has the information
                // from the pointer enter.
                OneEvent("type", EventType.Dragging),
                OneEvent("button", clickButton),
                OneEvent("pointerEnter", scene.rightGameObject),
                OneEvent("hovered", new[] { scene.rightGameObject, scene.parentGameObject }),
                OneEvent("pressPosition", firstScreenPosition),
                OneEvent("clickTime", clickTime),
                OneEvent("clickCount", clickCount),
                OneEvent("pointerDrag", scene.leftGameObject),
                OneEvent("pointerPress", scene.leftGameObject),
                OneEvent("rawPointerPress", scene.leftGameObject),
                OneEvent("lastPress", null),
                OneEvent("dragging", true),
                OneEvent("pointerPressRaycast.gameObject", scene.leftGameObject),
                OneEvent("pointerPressRaycast.screenPosition", firstScreenPosition)
            )
        );

        Assert.That(scene.rightChildReceiver.events,
            EventSequence(
                AllEvents("button", PointerEventData.InputButton.Left),
                AllEvents("pointerId", pointerId),
                AllEvents("position", thirdScreenPosition),
                AllEvents("delta", thirdScreenPosition - secondScreenPosition),
                AllEvents("pointerEnter", scene.rightGameObject),
                AllEvents("useDragThreshold", false), // We set it in OnInitializePotentialDrag.
                AllEvents("hovered", new[] { scene.parentGameObject }), // Right GO gets added after.
                AllEvents("pointerCurrentRaycast.gameObject", scene.rightGameObject),
                AllEvents("pointerCurrentRaycast.screenPosition", thirdScreenPosition),
                AllEvents("pressPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),
                AllEvents("clickTime", clickButton == PointerEventData.InputButton.Left ? clickTime : 0f),
                AllEvents("clickCount", clickButton == PointerEventData.InputButton.Left ? clickCount : 0),
                AllEvents("pointerDrag", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                AllEvents("dragging", clickButton == PointerEventData.InputButton.Left ? true : false),
                AllEvents("pointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                AllEvents("rawPointerPress", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                AllEvents("lastPress", clickButton == PointerEventData.InputButton.Left ? null : scene.leftGameObject),
                AllEvents("pointerPressRaycast.gameObject", clickButton == PointerEventData.InputButton.Left ? scene.leftGameObject : null),
                AllEvents("pointerPressRaycast.screenPosition", clickButton == PointerEventData.InputButton.Left ? firstScreenPosition : Vector2.zero),

                OneEvent("type", EventType.PointerEnter)
#if UNITY_2021_2_OR_NEWER
                , OneEvent("type", EventType.PointerMove)
#endif
            )
        );

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();
        scene.parentReceiver.events.Clear();

        // Release.
        if (isTouch)
            EndTouch(1, thirdScreenPosition, queueEventOnly: true);
        else
            Release(clickControl, queueEventOnly: true);
        yield return null;

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
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject)); // Pointer-exit comes before pointer-up.
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.pointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.rawPointerPress, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[0].pointerData.lastPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[0].pointerData.useDragThreshold, Is.False); // We set it in OnInitializePotentialDrag.
        Assert.That(scene.leftChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
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
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerEnter, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerDrag, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.rawPointerPress, Is.Null);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.lastPress, Is.SameAs(scene.leftGameObject)); // Remembers last pointerPress.
        Assert.That(scene.leftChildReceiver.events[1].pointerData.dragging, Is.True);
        Assert.That(scene.leftChildReceiver.events[1].pointerData.useDragThreshold, Is.False); // We set it in OnInitializePotentialDrag.
        Assert.That(scene.leftChildReceiver.events[1].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.leftChildReceiver.events[1].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.True);
        Assert.That(scene.parentReceiver.events, Is.Empty);

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
        Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.False); // We set it in OnInitializePotentialDrag.
        Assert.That(scene.rightChildReceiver.events[0].pointerData.hovered, Is.EquivalentTo(new[] { scene.rightGameObject, scene.parentGameObject }));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.gameObject, Is.SameAs(scene.rightGameObject));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerCurrentRaycast.screenPosition,
            Is.EqualTo(thirdScreenPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.gameObject, Is.SameAs(scene.leftGameObject));
        Assert.That(scene.rightChildReceiver.events[0].pointerData.pointerPressRaycast.screenPosition,
            Is.EqualTo(firstScreenPosition).Using(Vector2EqualityComparer.Instance));

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Scroll.
        if (scrollAction.controls.Count > 0)
        {
            Set((Vector2Control)scrollAction.controls[0], Vector2.one, queueEventOnly: true);
            yield return null;

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
            Assert.That(scene.rightChildReceiver.events[0].pointerData.useDragThreshold, Is.False); // We set it in OnInitializePotentialDrag.
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

        // For touch, the pointer should cease to exist one frame after and we should get exit events.
        if (isTouch)
        {
            scene.leftChildReceiver.events.Clear();
            scene.rightChildReceiver.events.Clear();
            scene.parentReceiver.events.Clear();

            yield return null;

            Assert.That(scene.eventSystem.IsPointerOverGameObject(pointerId), Is.False);

            // Right child should have seen exit.
            Assert.That(scene.rightChildReceiver.events,
                EventSequence(
                    AllEvents("pointerId", pointerId),
                    AllEvents("position", thirdScreenPosition),
                    AllEvents("delta", Vector2.zero),
                    AllEvents("pressPosition", firstScreenPosition),
                    AllEvents("clickTime", clickTime),
                    AllEvents("clickCount", clickCount),
                    AllEvents("pointerDrag", null),
                    AllEvents("pointerPress", null),
                    AllEvents("rawPointerPress", null),
                    AllEvents("lastPress", scene.leftGameObject),
                    AllEvents("dragging", false),
                    AllEvents("useDragThreshold", false),
                    AllEvents("pointerCurrentRaycast.gameObject", scene.rightGameObject),
                    AllEvents("pointerCurrentRaycast.screenPosition", thirdScreenPosition),
                    AllEvents("pointerPressRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerPressRaycast.screenPosition", firstScreenPosition),

                    // PointerExit.
                    OneEvent("type", EventType.PointerExit),
                    OneEvent("button", PointerEventData.InputButton.Left), // PointerExit always coming from left buffer.
                    OneEvent("pointerEnter", scene.rightGameObject), // Reset after event.
                    OneEvent("hovered", new[] { scene.rightGameObject, scene.parentGameObject })
                )
            );

            // Parent should have seen an exit, too. However, no PointerMove as we
            // released the touch in a position we had already moved to.
            Assert.That(scene.parentReceiver.events,
                EventSequence(
                    AllEvents("button", PointerEventData.InputButton.Left),
                    AllEvents("pointerId", pointerId),
                    AllEvents("position", thirdScreenPosition),
                    AllEvents("delta", Vector2.zero),
                    AllEvents("pressPosition", firstScreenPosition),
                    AllEvents("clickTime", clickTime),
                    AllEvents("clickCount", clickCount),
                    AllEvents("pointerEnter", scene.rightGameObject),
                    AllEvents("pointerDrag", null),
                    AllEvents("pointerPress", null),
                    AllEvents("rawPointerPress", null),
                    AllEvents("lastPress", scene.leftGameObject),
                    AllEvents("dragging", false),
                    AllEvents("useDragThreshold", false), // We set it in OnInitializePotentialDrag.
                    ////REVIEW: This behavior is inconsistent between "normal" pointer-enter/exit sequences but is consistent with what StandaloneInputModule does.
                    ////        However, it seems wrong that on one path, GOs are removed one-by-one from `hovered` as the callbacks step through the hierarchy, whereas
                    ////        on the other path, the list stays unmodified until the end and is then cleared en-bloc.
                    AllEvents("hovered", new[] { scene.rightGameObject, scene.parentGameObject }),
                    AllEvents("pointerCurrentRaycast.gameObject", scene.rightGameObject),
                    AllEvents("pointerCurrentRaycast.screenPosition", thirdScreenPosition),
                    AllEvents("pointerPressRaycast.gameObject", scene.leftGameObject),
                    AllEvents("pointerPressRaycast.screenPosition", firstScreenPosition),

                    OneEvent("type", EventType.PointerExit)
                )
            );
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1232705/
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanReceivePointerExitsWhenChangingUIStateWithoutMovingPointer()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var scene = CreateTestUI();
        var actions = new DefaultInputActions();
        scene.uiModule.point = InputActionReference.Create(actions.UI.Point);

        Set(mouse.position, scene.From640x480ToScreen(100, 100));

        yield return null;

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);

        scene.parentReceiver.events.Clear();

        // Hide the left GO. Should send a pointer exit.
        scene.leftGameObject.SetActive(false);
        yield return null;

        // We already disabled the left GO so ExecuteEvents will refuse to send OnPointerExit
        // to it. However, we should see the exit on the still-active parent.
        Assert.That(scene.parentReceiver.events,
            EventSequence(
                OneEvent("type", EventType.PointerExit),
                AllEvents("position", scene.From640x480ToScreen(100, 100)),
                AllEvents("pointerEnter", scene.leftGameObject)
            )
        );
    }

    [UnityTest]
    [Category("UI")]
    [TestCase(UIPointerBehavior.SingleUnifiedPointer, ExpectedResult = -1)]
    [TestCase(UIPointerBehavior.AllPointersAsIs, ExpectedResult = -1)]
    [TestCase(UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack, ExpectedResult = -1)]
#if UNITY_IOS || UNITY_TVOS
    [Ignore("Failing on iOS https://jira.unity3d.com/browse/ISX-448")]
#endif
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

        var scene = CreateTestUI();
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
        var firstPosition = scene.From640x480ToScreen(100, 100);
        Set(mouse1.position, firstPosition);
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Put mouse2 over right object.
        var secondPosition = scene.From640x480ToScreen(350, 200);
        Set(mouse2.position, secondPosition);
        yield return null;

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
                // Pointer-exit on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                break;

            case UIPointerBehavior.AllPointersAsIs:
                // No change on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events, Is.Empty);
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                break;
        }

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Put tracked device #1 over left object and tracked device #2 over right object.
        // Need two updates as otherwise we'd end up with just another pointer of the right object
        // which would not result in an event.
        Set(trackedDevice1, "deviceRotation", Quaternion.Euler(0, -30, 0));
        yield return null;
        Set(trackedDevice2, "deviceRotation", Quaternion.Euler(0, 30, 0));
        yield return null;

        var leftPosition = scene.From640x480ToScreen(80, 240);
        var rightPosition = scene.From640x480ToScreen(560, 240);

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
                // Pointer-exit on right, pointer-enter on left, pointer-exit on left, pointer-enter on right.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And // System transparently switched from mouse2 to trackedDevice1.
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.leftGameObject))); // Exits at position of trackedDevice1.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.leftGameObject)));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.rightGameObject)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.rightGameObject)));
                break;

            case UIPointerBehavior.AllPointersAsIs:
            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
                if (pointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack)
                {
                    // Tracked device activity should have removed the MouseOrPen pointer and thus generated exit events on it.
                    Assert.That(scene.rightChildReceiver.events,
                        Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                            .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse2).And
                            .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                }

                // Pointer-enter on left, pointer-enter on right.
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice1).And
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.leftGameObject)));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == trackedDevice2).And
                        .Matches((UICallbackReceiver.Event e) =>
                            scene.IsWithinRect(e.pointerData.position, scene.rightGameObject)));
                break;
        }

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch right object on first touchscreen and left object on second touchscreen.
        BeginTouch(1, secondPosition, screen: touch1);
        yield return null;
        BeginTouch(1, firstPosition, screen: touch2);
        yield return null;

        switch (pointerBehavior)
        {
            case UIPointerBehavior.SingleUnifiedPointer:
                ////REVIEW: this likely needs refinement; ATM the second touch leads to a drag as the position changes after a press has occurred
                // Pointer-down on right, pointer-exit on right, begin-drag on right, pointer-enter on left.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And // Transparently switched.
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And // Transparently switched.
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.BeginDrag).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
                break;

            case UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack:
            case UIPointerBehavior.AllPointersAsIs:
                // Pointer-enter on right, pointer-enter on left.
                Assert.That(scene.rightChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch1).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
                Assert.That(scene.leftChildReceiver.events,
                    Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touch2).And
                        .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
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
        var scene = CreateTestUI(noFirstSelected: true);

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
        var firstPosition = scene.From640x480ToScreen(100, 100);
        BeginTouch(1, firstPosition);
        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == firstPosition));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch right object.
        var secondPosition = scene.From640x480ToScreen(350, 200);
        BeginTouch(2, secondPosition);
        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == secondPosition));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Drag left object over right object.
        var thirdPosition = scene.From640x480ToScreen(355, 210);
        MoveTouch(1, thirdPosition);
        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == thirdPosition));
        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == thirdPosition));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 1).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == thirdPosition));

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Touch left object again.
        var fourthPosition = scene.From640x480ToScreen(123, 123);
        BeginTouch(3, fourthPosition);
        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == fourthPosition));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // End second touch.
        var fifthPosition = scene.From640x480ToScreen(355, 205);
        EndTouch(2, fifthPosition);
        yield return null;

        Assert.That(scene.eventSystem.IsPointerOverGameObject(), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(touchScreen.deviceId), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(1), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(2), Is.True);
        Assert.That(scene.eventSystem.IsPointerOverGameObject(3), Is.True);

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerUp).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == fifthPosition));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == fifthPosition));

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Begin second touch again.
        var sixthPosition = scene.From640x480ToScreen(345, 195);
        BeginTouch(2, sixthPosition);
        yield return null;

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
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == sixthPosition));
        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == touchScreen).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.touchId == 2).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.pointerType == UIPointerType.Touch).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.position == sixthPosition));
        Assert.That(scene.leftChildReceiver.events, Is.Empty);
    }

    // https://fogbugz.unity3d.com/f/cases/1190150/
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanUseTouchSimulationWithUI()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var scene = CreateTestUI();
        scene.uiModule.AssignDefaultActions();
        TouchSimulation.Enable();

        // https://fogbugz.unity3d.com/f/cases/1330014/
        // Scale the left button down on X just a little bit so as to give us some space where we can hit nothing.
        // This makes sure that if the code ends up putting something at (0,0), it'll hit nothing.
        ((RectTransform)scene.leftGameObject.transform).localScale = new Vector3(0.95f, 1, 1);

        try
        {
            yield return null;
            scene.leftChildReceiver.events.Clear();

            InputSystem.QueueStateEvent(mouse, new MouseState
            {
                position = scene.From640x480ToScreen(180, 180)
            }.WithButton(MouseButton.Left));
            InputSystem.Update();

            yield return null;

            Assert.That(scene.uiModule.m_CurrentPointerType, Is.EqualTo(UIPointerType.Touch));
            Assert.That(scene.uiModule.m_PointerIds.length, Is.EqualTo(1));
            Assert.That(scene.uiModule.m_PointerTouchControls.length, Is.EqualTo(1));
            Assert.That(scene.uiModule.m_PointerTouchControls[0], Is.SameAs(Touchscreen.current.touches[0]));
            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("pointerType", UIPointerType.Touch),
                    AllEvents("touchId", 1),
                    AllEvents("position", scene.From640x480ToScreen(180, 180)),
                    OneEvent("type", EventType.PointerEnter)
                    #if UNITY_2021_2_OR_NEWER
                    , OneEvent("type", EventType.PointerMove)
                    #endif
                    , OneEvent("type", EventType.PointerDown)
                    , OneEvent("type", EventType.InitializePotentialDrag)
                )
            );

            scene.leftChildReceiver.events.Clear();

            Release(mouse.leftButton);
            yield return null;

            // Touch pointer record lingers for one frame.

            Assert.That(scene.uiModule.m_CurrentPointerType, Is.EqualTo(UIPointerType.Touch));
            Assert.That(scene.uiModule.m_PointerIds.length, Is.EqualTo(1));
            Assert.That(scene.uiModule.m_PointerTouchControls.length, Is.EqualTo(1));
            Assert.That(scene.uiModule.m_PointerTouchControls[0], Is.SameAs(Touchscreen.current.touches[0]));
            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("pointerType", UIPointerType.Touch),
                    AllEvents("touchId", 1),
                    AllEvents("position", scene.From640x480ToScreen(180, 180)),
                    OneEvent("type", EventType.PointerUp),
                    OneEvent("type", EventType.PointerClick)
                )
            );

            scene.leftChildReceiver.events.Clear();

            yield return null;

            Assert.That(scene.uiModule.m_CurrentPointerType, Is.EqualTo(UIPointerType.None));
            Assert.That(scene.uiModule.m_PointerIds.length, Is.Zero);
            Assert.That(scene.uiModule.m_PointerTouchControls.length, Is.Zero);
            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("pointerType", UIPointerType.Touch),
                    AllEvents("touchId", 1),
                    AllEvents("position", scene.From640x480ToScreen(180, 180)),
                    OneEvent("type", EventType.PointerExit)
                )
            );

            scene.leftChildReceiver.events.Clear();

            yield return null;
            Press(mouse.leftButton);
            yield return null;

            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("pointerType", UIPointerType.Touch),
                    AllEvents("touchId", 2),
                    AllEvents("position", scene.From640x480ToScreen(180, 180)),
                    OneEvent("type", EventType.PointerEnter),
                    OneEvent("type", EventType.PointerDown),
                    OneEvent("type", EventType.InitializePotentialDrag)
                )
            );

            scene.leftChildReceiver.events.Clear();

            Release(mouse.leftButton);
            yield return null;

            Assert.That(scene.leftChildReceiver.events,
                EventSequence(
                    AllEvents("pointerType", UIPointerType.Touch),
                    AllEvents("touchId", 2),
                    AllEvents("position", scene.From640x480ToScreen(180, 180)),
                    OneEvent("type", EventType.PointerUp),
                    OneEvent("type", EventType.PointerClick)
                )
            );
        }
        finally
        {
            TouchSimulation.Disable();
        }
    }

    #if UNITY_IOS || UNITY_TVOS
    [Ignore("Failing on iOS https://jira.unity3d.com/browse/ISX-448")]
    #endif
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromMultipleTrackedDevices()
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);

        var trackedDevice1 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");
        var trackedDevice2 = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");

        var scene = CreateTestUI();

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
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice1.deviceId),
                AllEvents("device", trackedDevice1),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, -30, 0)),
                OneEvent("type", EventType.PointerEnter)
                #if UNITY_2021_2_OR_NEWER
                , OneEvent("type", EventType.PointerMove)
                #endif
            )
        );
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Point second device at left child.
        Set(trackedDevice2.deviceRotation, Quaternion.Euler(0, -31, 0));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice2.deviceId),
                AllEvents("device", trackedDevice2),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, -31, 0)),
                OneEvent("type", EventType.PointerEnter)
                #if UNITY_2021_2_OR_NEWER
                , OneEvent("type", EventType.PointerMove)
                #endif
            )
        );
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Click button on first device.
        PressAndRelease((ButtonControl)trackedDevice1["button"]);
        yield return null;


        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice1.deviceId),
                AllEvents("device", trackedDevice1),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, -30, 0)),
                OneEvent("type", EventType.PointerDown),
                OneEvent("type", EventType.InitializePotentialDrag),
                OneEvent("type", EventType.PointerUp),
                OneEvent("type", EventType.PointerClick)
            )
        );
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Click button on second device.
        PressAndRelease((ButtonControl)trackedDevice2["button"]);
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice2.deviceId),
                AllEvents("device", trackedDevice2),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, -31, 0)),
                OneEvent("type", EventType.PointerDown),
                OneEvent("type", EventType.InitializePotentialDrag),
                OneEvent("type", EventType.PointerUp),
                OneEvent("type", EventType.PointerClick)
            )
        );
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Point first device at right child.
        Set(trackedDevice1.deviceRotation, Quaternion.Euler(0, 30, 0));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice1.deviceId),
                AllEvents("device", trackedDevice1),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, 30, 0)),
                #if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove),
                #endif
                OneEvent("type", EventType.PointerExit)
            )
        );
        Assert.That(scene.rightChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice1.deviceId),
                AllEvents("device", trackedDevice1),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, 30, 0)),
                OneEvent("type", EventType.PointerEnter)
                #if UNITY_2021_2_OR_NEWER
                , OneEvent("type", EventType.PointerMove)
                #endif
            )
        );

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Point second device at right child.
        Set(trackedDevice2.deviceRotation, Quaternion.Euler(0, 31, 0));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice2.deviceId),
                AllEvents("device", trackedDevice2),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, 31, 0)),
                #if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove),
                #endif
                OneEvent("type", EventType.PointerExit)
            )
        );
        Assert.That(scene.rightChildReceiver.events,
            EventSequence(
                AllEvents("pointerType", UIPointerType.Tracked),
                AllEvents("pointerId", trackedDevice2.deviceId),
                AllEvents("device", trackedDevice2),
                AllEvents("trackedDeviceOrientation", Quaternion.Euler(0, 31, 0)),
                OneEvent("type", EventType.PointerEnter)
                #if UNITY_2021_2_OR_NEWER
                , OneEvent("type", EventType.PointerMove)
                #endif
            )
        );
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

        var scene = CreateTestUI();

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
        Set(mouse.position, scene.From640x480ToScreen(350, 200));
        yield return null;

        scene.rightChildReceiver.events.Clear();

        Press(keyboard.spaceKey);
        yield return null;

        Assert.That(scene.rightChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerDown).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.pointerId == mouse.deviceId).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.pointerType == UIPointerType.MouseOrPen).And
                .Matches((UICallbackReceiver.Event eventRecord) => eventRecord.pointerData.clickCount == 0));
    }

    // https://fogbugz.unity3d.com/f/cases/1317239/
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanDetectClicks_WithSuccessiveClicksReflectedInClickCount()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var scene = CreateTestUI();
        var actions = new DefaultInputActions();
        scene.uiModule.point = InputActionReference.Create(actions.UI.Point);
        scene.uiModule.leftClick = InputActionReference.Create(actions.UI.Click);

        Set(mouse.position, scene.From640x480ToScreen(100, 100), queueEventOnly: true);

        yield return null;

        scene.ClearEvents();
        Press(mouse.leftButton, queueEventOnly: true);

        yield return null;

        // No click yet.
        // NOTE: This is different from StandaloneInputModule which immediately ups
        //       clickCount on press. However, until we release we don't know yet whether
        //       we actually have a click or a drag (or neither).
        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("clickCount", 0),
                AllEvents("clickTime", 0f),
                AllEvents("lastPress", null)
            )
        );

        scene.ClearEvents();
        Release(mouse.leftButton, queueEventOnly: true);

        yield return null;

        var clickTime = runtime.unscaledGameTime;
        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("clickCount", 1),
                AllEvents("clickTime", clickTime),
                AllEvents("lastPress", null)
            )
        );

        scene.ClearEvents();
        PressAndRelease(mouse.leftButton, queueEventOnly: true);

        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                // PointerDown.
                OneEvent("type", EventType.PointerDown),
                OneEvent("clickCount", 1),
                OneEvent("clickTime", clickTime),

                // InitializePotentialDrag.
                OneEvent("type", EventType.InitializePotentialDrag),
                OneEvent("clickCount", 1),
                OneEvent("clickTime", clickTime),

                // PointerUp.
                OneEvent("type", EventType.PointerUp),
                OneEvent("clickCount", 2),
                OneEvent("clickTime", runtime.unscaledGameTime),

                // PointerClick.
                OneEvent("type", EventType.PointerClick),
                OneEvent("clickCount", 2),
                OneEvent("clickTime", runtime.unscaledGameTime)
            )
        );

        clickTime = runtime.unscaledGameTime;
        scene.ClearEvents();
        PressAndRelease(mouse.leftButton, queueEventOnly: true);

        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                // PointerDown.
                OneEvent("type", EventType.PointerDown),
                OneEvent("clickCount", 2),
                OneEvent("clickTime", clickTime),

                // InitializePotentialDrag.
                OneEvent("type", EventType.InitializePotentialDrag),
                OneEvent("clickCount", 2),
                OneEvent("clickTime", clickTime),

                // PointerUp.
                OneEvent("type", EventType.PointerUp),
                OneEvent("clickCount", 3),
                OneEvent("clickTime", runtime.unscaledGameTime),

                // PointerClick.
                OneEvent("type", EventType.PointerClick),
                OneEvent("clickCount", 3),
                OneEvent("clickTime", runtime.unscaledGameTime)
            )
        );

        clickTime = runtime.unscaledGameTime;
        scene.ClearEvents();
        Press(mouse.leftButton, queueEventOnly: true);

        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("clickCount", 3),
                AllEvents("clickTime", clickTime)
            )
        );

        scene.ClearEvents();
        Release(mouse.leftButton, queueEventOnly: true);

        // Doesn't matter how long we hold the button. If we don't move far enough by the time we release and we still
        // have the same UI element underneath us, it's a click.
        runtime.unscaledGameTime += 1f;
        yield return null;

        clickTime = runtime.unscaledGameTime;
        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("clickCount", 4),
                AllEvents("clickTime", clickTime)
            )
        );

        scene.ClearEvents();

        // However, when more than 0.3s elapses between release and next press, it resets
        // the click sequence.
        Press(mouse.leftButton, queueEventOnly: true);
        runtime.unscaledGameTime += 1f;
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                // On button down, we still see the state from the previous click (we don't
                // know yet whether we clicked on the same object).
                OneEvent("type", EventType.PointerDown),
                OneEvent("clickCount", 4),
                OneEvent("clickTime", clickTime),

                // Before InitializePotentialDrag, we reset.
                OneEvent("type", EventType.InitializePotentialDrag),
                OneEvent("clickCount", 0),
                OneEvent("clickTime", 0f)
            )
        );
        clickTime = runtime.unscaledGameTime;

        // Clone left button. If we release the button now, there should not be
        // a click because the object pointed to has changed.
        UnityEngine.Object.Instantiate(scene.leftGameObject, scene.parentReceiver.transform);
        Release(mouse.leftButton, queueEventOnly: true);
        scene.ClearEvents();

        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                AllEvents("clickCount", 0),
                AllEvents("clickTime", 0f)
            )
        );
    }

    // The UI input module needs to return true from IsPointerOverGameObject() for touches
    // that have ended in the current frame. I.e. even though the touch is already concluded
    // at the InputDevice level, the UI module needs to maintain state for one more frame.
    //
    // https://fogbugz.unity3d.com/f/cases/1347048/
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_TouchPointersAreKeptForOneFrameAfterRelease()
    {
        InputSystem.AddDevice<Touchscreen>();

        var scene = CreateTestUI();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough, binding: "<Touchscreen>/position");
        var clickAction = uiActions.AddAction("press", type: InputActionType.PassThrough, binding: "<Touchscreen>/press");

        actions.Enable();

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        yield return null;

        BeginTouch(1, new Vector2(100, 100), queueEventOnly: true);
        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);

        EndTouch(1, new Vector2(100, 100), queueEventOnly: true);
        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);

        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.False);
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CallingIsPointerOverGameObject_FromActionCallback_ResultsInWarning()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var scene = CreateTestUI();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        scene.uiModule.point = InputActionReference.Create(pointAction);

        pointAction.performed += ctx => { EventSystem.current.IsPointerOverGameObject(); };

        actions.Enable();

        yield return null;

        LogAssert.Expect(LogType.Warning, new Regex("Calling IsPointerOverGameObject\\(\\) from within event processing .* will not work as expected"));

        Set(mouse.position, new Vector2(123, 234), queueEventOnly: true);

        yield return null;
    }

#if UNITY_IOS || UNITY_TVOS
    [Ignore("Failing on iOS https://jira.unity3d.com/browse/ISX-448")]
#endif
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanGetRaycastResultMatchingEvent()
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);

        var trackedDevice = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");
        var scene = CreateTestUI();

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

        // Point device away from objects.
        Set(trackedDevice.deviceRotation, Quaternion.Euler(0, -90, 0));
        yield return null;

        scene.leftChildReceiver.events.Clear();
        scene.rightChildReceiver.events.Clear();

        // Point device at left child.
        Set(trackedDevice.deviceRotation, Quaternion.Euler(0, -30, 0));
        yield return null;

        var raycastResult = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(raycastResult.isValid, Is.True);

        //2021.2 added an additional move event.
#if UNITY_2021_2_OR_NEWER
        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(2));
#else
        Assert.That(scene.leftChildReceiver.events, Has.Count.EqualTo(1));
#endif
        Assert.That(scene.leftChildReceiver.events[0].pointerData, Is.Not.Null);

        var eventRaycastResult = scene.leftChildReceiver.events[0].pointerData.pointerCurrentRaycast;
        Assert.That(raycastResult.worldPosition, Is.EqualTo(eventRaycastResult.worldPosition));
        Assert.That(raycastResult.worldNormal, Is.EqualTo(eventRaycastResult.worldNormal));
        Assert.That(raycastResult.distance, Is.EqualTo(eventRaycastResult.distance));
        Assert.That(raycastResult.module, Is.EqualTo(eventRaycastResult.module));
        Assert.That(raycastResult.screenPosition, Is.EqualTo(eventRaycastResult.screenPosition));
        Assert.That(raycastResult.gameObject, Is.EqualTo(eventRaycastResult.gameObject));

        // Move back off the object
        Set(trackedDevice.deviceRotation, Quaternion.Euler(0, -90, 0));
        yield return null;

        raycastResult = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(raycastResult.isValid, Is.False);
    }

    #if UNITY_IOS || UNITY_TVOS
    [Ignore("Failing on iOS https://jira.unity3d.com/browse/ISX-448")]
    #endif
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_XRTrackingOriginTransformModifiesTrackedPointers()
    {
        InputSystem.RegisterLayout(kTrackedDeviceWithButton);

        var trackedDevice = (TrackedDevice)InputSystem.AddDevice("TrackedDeviceWithButton");

        var scene = CreateTestUI();

        var xrTrackingOrigin = new GameObject("XRStage").transform;
        scene.uiModule.xrTrackingOrigin = xrTrackingOrigin;

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
        Set(trackedDevice.deviceRotation, Quaternion.Euler(0, -90, 0));

        yield return null;

        var trackedDeviceRaycast = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(trackedDeviceRaycast.isValid, Is.False);

        // Point device at left child.
        Set(trackedDevice.deviceRotation, Quaternion.Euler(0, -30, 0));
        yield return null;

        trackedDeviceRaycast = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(trackedDeviceRaycast.isValid, Is.True);
        Assert.That(trackedDeviceRaycast.gameObject, Is.EqualTo(scene.leftGameObject));

        // Rotate so right object is targetted
        xrTrackingOrigin.rotation = Quaternion.Euler(0f, 60, 0f);
        yield return null;

        trackedDeviceRaycast = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(trackedDeviceRaycast.isValid, Is.True);
        Assert.That(trackedDeviceRaycast.gameObject, Is.EqualTo(scene.rightGameObject));

        xrTrackingOrigin.position = Vector3.up * 1000f;
        yield return null;

        trackedDeviceRaycast = scene.uiModule.GetLastRaycastResult(trackedDevice.deviceId);
        Assert.That(trackedDeviceRaycast.isValid, Is.False);
    }

    [UnityTest]
    [Category("UI")]
    [Ignore("TODO")]
    public IEnumerator TODO_UI_CanGetLastUsedDevice()
    {
        yield return null;
        Assert.Fail();
    }

    [Test]
    [Category("UI")]
    public void UI_ClickDraggingMouseDoesNotAllocateGCMemory()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var scene = CreateTestUI();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = uiActions.AddAction("click", type: InputActionType.PassThrough, binding: "<Mouse>/leftClick");

        pointAction.Enable();
        clickAction.Enable();

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        // Remove the test listeners as they will allocate GC memory.
        Component.DestroyImmediate(scene.parentReceiver);
        Component.DestroyImmediate(scene.leftChildReceiver);
        Component.DestroyImmediate(scene.rightChildReceiver);

        scene.eventSystem.InvokeUpdate();

        // The first time we go through this, we allocate some memory (not garbage, just memory that is going to be reused over
        // and over). The events, the hovered list, stuff like that. So do one click drag as a dry run and then do it for real.
        //
        // NOTE: We also need to do this because we can't use Retry(2) -- as we normally do to warm up the JIT -- in combination
        //       with UnityTest. Doing so will flat out lead to InvalidCastExceptions in the test runner :(
        Set(mouse.position, scene.From640x480ToScreen(100, 100));
        scene.eventSystem.InvokeUpdate();
        Press(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();
        Set(mouse.position, scene.From640x480ToScreen(200, 200));
        scene.eventSystem.InvokeUpdate();
        Release(mouse.leftButton);
        scene.eventSystem.InvokeUpdate();

        var kProfilerRegion = "UI_ClickDraggingDoesNotAllocateGCMemory";

        // Now for real.
        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            Set(mouse.position, scene.From640x480ToScreen(100, 100));
            scene.eventSystem.InvokeUpdate();
            Press(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();
            Set(mouse.position, scene.From640x480ToScreen(200, 200));
            scene.eventSystem.InvokeUpdate();
            Release(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();

            // And just for kicks, do it the opposite way, too.
            Set(mouse.position, scene.From640x480ToScreen(200, 200));
            scene.eventSystem.InvokeUpdate();
            Press(mouse.leftButton);
            scene.eventSystem.InvokeUpdate();
            Set(mouse.position, scene.From640x480ToScreen(100, 100));
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

        // Create two players each with their own UI scene and split
        // it vertically across the screen.
        var players = new[]
        {
            CreateTestUI(new Rect(0, 0, 1, 0.5f), namePrefix: "Player1 "),
            CreateTestUI(new Rect(0, 0.5f, 1, 0.5f), namePrefix: "Player2 ")
        };

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

        // Click left gameObject of player 0.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[0].From640x480ToScreen(100, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[0].From640x480ToScreen(100, 100), buttons = 0 });

        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.Null);

        // Click right gameObject of player 1.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[1].From640x480ToScreen(400, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[1].From640x480ToScreen(400, 100), buttons = 0 });

        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].rightGameObject));

        // Click right gameObject of player 0.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[0].From640x480ToScreen(400, 100), buttons = 1 << (int)MouseButton.Left });
        InputSystem.QueueStateEvent(mouse, new MouseState { position = players[0].From640x480ToScreen(400, 100), buttons = 0 });

        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].rightGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].rightGameObject));
    }

    // Check that two players can have separate UI and control it using separate gamepads, using
    // MultiplayerEventSystem.
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanOperateMultiplayerUILocallyUsingGamepads()
    {
        // Create devices.
        var gamepads = new[] { InputSystem.AddDevice<Gamepad>(), InputSystem.AddDevice<Gamepad>() };

        // Create scene with side-by-side split-screen.
        var players = new[]
        {
            CreateTestUI(new Rect(0, 0, 0.5f, 1), namePrefix: "Player1", makeSelectable: true), // Left
            CreateTestUI(new Rect(0.5f, 0, 0.5f, 1), namePrefix: "Player2", makeSelectable: true) // Right
        };

        // Offset player #2's canvas by moving its camera such that the resulting UI will be to the *right*
        // of that of player #1. This is important as it will, by default, make player #2's UI navigatable
        // from player #1 using the navigation logic in Selectable.
        var screenWidthInWorldSpace =
            Mathf.Abs(players[1].camera.ScreenToWorldPoint(new Vector3(0, 0, players[1].canvas.planeDistance)).x -
                players[1].camera.ScreenToWorldPoint(new Vector3(Screen.width, 0, players[1].canvas.planeDistance)).x);
        players[1].camera.transform.Translate(new Vector3(screenWidthInWorldSpace, 0, 0));

        for (var i = 0; i < 2; i++)
        {
            // Create asset
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Create actions.
            var map = new InputActionMap("map");
            asset.AddActionMap(map);
            var moveAction = map.AddAction("move", type: InputActionType.Value);
            var submitAction = map.AddAction("submit", type: InputActionType.Button);
            var cancelAction = map.AddAction("cancel", type: InputActionType.Button);

            // Create bindings.
            moveAction.AddBinding(gamepads[i].leftStick);
            submitAction.AddBinding(gamepads[i].buttonSouth);
            cancelAction.AddBinding(gamepads[i].buttonEast);

            // Wire up actions.
            players[i].uiModule.move = InputActionReference.Create(moveAction);
            players[i].uiModule.submit = InputActionReference.Create(submitAction);
            players[i].uiModule.cancel = InputActionReference.Create(cancelAction);

            // Enable the whole thing.
            map.Enable();
        }

        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].leftGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].leftGameObject));

        players[0].leftChildReceiver.events.Clear();
        players[1].leftChildReceiver.events.Clear();

        // Move right on player #1's gamepad.
        Set(gamepads[0].leftStick, Vector2.right);
        yield return null;

        // Player #1 should have moved from left to right object.
        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].rightGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].leftGameObject));

        Assert.That(players[0].leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("type", EventType.Deselect)));
        Assert.That(players[0].rightChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Select)));

        players[0].leftChildReceiver.events.Clear();
        players[0].rightChildReceiver.events.Clear();

        // No change for player #2.
        Assert.That(players[1].leftChildReceiver.events, Is.Empty);
        Assert.That(players[1].rightChildReceiver.events, Is.Empty);

        // https://fogbugz.unity3d.com/f/cases/1306361/
        // Move right on player #1's gamepad AGAIN. This should *not* cross
        // over to player #2's UI but should instead not result in any selection change.
        Set(gamepads[0].leftStick, Vector2.zero);
        yield return null;
        Set(gamepads[0].leftStick, Vector2.right);
        yield return null;

        Assert.That(players[0].eventSystem.currentSelectedGameObject, Is.SameAs(players[0].rightGameObject));
        Assert.That(players[1].eventSystem.currentSelectedGameObject, Is.SameAs(players[1].leftGameObject));

        Assert.That(players[0].leftChildReceiver.events, Is.Empty);
        Assert.That(players[0].rightChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move))); // OnMove will still get called to *attempt* a move.

        players[0].leftChildReceiver.events.Clear();
        players[0].rightChildReceiver.events.Clear();

        // No change for player #2.
        Assert.That(players[1].leftChildReceiver.events, Is.Empty);
        Assert.That(players[1].rightChildReceiver.events, Is.Empty);

        Set(gamepads[0].leftStick, Vector2.zero);

        // Check Player 0 Submit
        PressAndRelease(gamepads[0].buttonSouth);
        yield return null;

        Assert.That(players[0].rightChildReceiver.events,
            EventSequence(OneEvent("type", EventType.Submit)));
        Assert.That(players[1].leftChildReceiver.events, Is.Empty);

        players[0].rightChildReceiver.events.Clear();

        // Check Player 1 Submit
        PressAndRelease(gamepads[1].buttonSouth);
        yield return null;

        Assert.That(players[1].leftChildReceiver.events,
            EventSequence(OneEvent("type", EventType.Submit)));
        Assert.That(players[0].rightChildReceiver.events, Is.Empty);
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_CanDriveUIFromGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var scene = CreateTestUI();

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
        Assert.That(scene.leftChildReceiver.events, EventSequence(OneEvent("type", EventType.Select)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move right.
        Set(gamepad.leftStick, new Vector2(1, 0.5f));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Right),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move left.
        Set(gamepad.leftStick, new Vector2(-1, 0.5f));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Left),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move up.
        Set(gamepad.leftStick, new Vector2(-0.5f, 1));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Up),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Move down.
        Set(gamepad.leftStick, new Vector2(0.5f, -1));
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Down),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Check repeat delay. Wait enough to cross both delay and subsequent repeat. We should
        // still only get one repeat event.
        unscaledGameTime += 1.21f;
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Down),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));

        scene.leftChildReceiver.events.Clear();

        // Check repeat rate. Same here; doesn't matter how much time we pass as long as we cross
        // the repeat rate threshold.
        unscaledGameTime += 2;
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.Move),
                OneEvent("moveDir", MoveDirection.Down),
                OneEvent("moveVector", gamepad.leftStick.ReadValue())));

        scene.leftChildReceiver.events.Clear();

        // Submit.
        PressAndRelease(gamepad.buttonSouth);
        yield return null;

        Assert.That(scene.leftChildReceiver.events, EventSequence(OneEvent("type", EventType.Submit)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Cancel.
        PressAndRelease(gamepad.buttonEast);
        yield return null;

        Assert.That(scene.leftChildReceiver.events, EventSequence(OneEvent("type", EventType.Cancel)));
        Assert.That(scene.rightChildReceiver.events, Is.Empty);

        scene.leftChildReceiver.events.Clear();

        // Make sure that no navigation events are sent if turned off.
        scene.eventSystem.sendNavigationEvents = false;

        Set(gamepad.leftStick, default);
        yield return null;
        Set(gamepad.leftStick, new Vector2(1, 1));
        yield return null;

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

        var scene = CreateTestUI();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var clickAction = map.AddAction("click", type: InputActionType.PassThrough, binding: "<Mouse>/leftButton");

        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        map.Enable();

        yield return null;

        // Put mouse over left object.
        Set(mouse.position, scene.From640x480ToScreen(100, 100));
        yield return null;

        Assert.That(scene.leftChildReceiver.events, Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerEnter));

        scene.leftChildReceiver.events.Clear();

        // Unbind click. This should not yet result in the pointer getting removed as we still
        // have the position binding.
        clickAction.ApplyBindingOverride(string.Empty);
        yield return null;

        Assert.That(scene.leftChildReceiver.events, Is.Empty);

        // Remove mouse. Should result in pointer-exit event.
        InputSystem.RemoveDevice(mouse);
        yield return null;

        Assert.That(scene.leftChildReceiver.events,
            Has.Exactly(1).With.Property("type").EqualTo(EventType.PointerExit).And
                .Matches((UICallbackReceiver.Event e) => e.pointerData.device == mouse));
    }

    private class InputSystemUIInputModuleTestScene_Setup : IPrebuildSetup, IPostBuildCleanup
    {
        public void Setup()
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene
                { path = UITestScene.TestScenePath, enabled = true }).ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            Debug.Log("Running post build cleanup");
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == UITestScene.TestScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
#endif
        }
    }

    [UnityTest]
    [Category("UI")]
    [PrebuildSetup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    [PostBuildCleanup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    public IEnumerator UI_WhenMultipleInputModulesExist_ActionsAreNotDisabledUntilTheLastInputModuleIsDisabled()
    {
        var firstScene = UITestScene.LoadScene();
        yield return null;

        var secondScene = UITestScene.LoadScene();
        yield return null;

        var unloadOperation = SceneManager.UnloadSceneAsync(firstScene.Scene);
        yield return new WaitUntil(() => unloadOperation.isDone);

        var pointAction = secondScene.InputModule.point.action;
        Assert.That(pointAction.enabled, Is.True);

        unloadOperation = SceneManager.UnloadSceneAsync(secondScene.Scene);
        yield return new WaitUntil(() => unloadOperation.isDone);

        Assert.That(pointAction.enabled, Is.False);
    }

    [Test]
    [Category("UI")]
    public void UI_WhenDisablingInputModule_ActionsAreNotDisabledIfTheyWereNotEnabledByTheInputModule()
    {
        var eventSystemGO = new GameObject();
        eventSystemGO.AddComponent<EventSystem>();
        var inputModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");

        map.Enable();

        inputModule.point = InputActionReference.Create(pointAction);

        GameObject.DestroyImmediate(eventSystemGO);

        Assert.That(pointAction.enabled, Is.True);
    }

    // https://fogbugz.unity3d.com/f/cases/1371332/
    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_WhenAssigningInputModuleActionAsset_OldInputsAreDisconnected_AndNewInputsAreConnected()
    {
        var mouse1 = InputSystem.AddDevice<Mouse>();
        var mouse2 = InputSystem.AddDevice<Mouse>();

        var eventSystemGO = new GameObject();
        eventSystemGO.AddComponent<EventSystem>();
        var inputModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();

        yield return null;

        var actions1 = new DefaultInputActions();
        actions1.devices = new[] { mouse1 };

        inputModule.actionsAsset = actions1.asset;

        Assert.That(actions1.UI.enabled, Is.True);

        Set(mouse1.position, new Vector2(123, 234));
        Set(mouse2.position, new Vector2(234, 345));

        yield return null;

        Assert.That(inputModule.m_CurrentPointerType, Is.EqualTo(UIPointerType.MouseOrPen));
        Assert.That(inputModule.m_PointerStates[0].screenPosition, Is.EqualTo(new Vector2(123, 234)));
        Assert.That(inputModule.m_PointerStates[0].eventData.device, Is.SameAs(mouse1));

        var actions2 = new DefaultInputActions();
        actions2.devices = new[] { mouse2 };

        actions1.Disable();

        inputModule.actionsAsset = actions2.asset;

        Assert.That(actions1.UI.enabled, Is.False);
        Assert.That(actions2.UI.enabled, Is.False);

        actions1.Enable();
        actions2.Enable();

        Set(mouse1.position, new Vector2(234, 345));
        Set(mouse2.position, new Vector2(345, 456));

        yield return null;

        Assert.That(inputModule.m_CurrentPointerType, Is.EqualTo(UIPointerType.MouseOrPen));
        Assert.That(inputModule.m_PointerStates[0].screenPosition, Is.EqualTo(new Vector2(345, 456)));
        Assert.That(inputModule.m_PointerStates[0].eventData.device, Is.SameAs(mouse2));
    }

    [UnityTest]
    [Category("UI")]
    [PrebuildSetup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    [PostBuildCleanup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    public IEnumerator UI_WhenAssigningInputModuleAction_PreviousOwnedActionsAreDisabled()
    {
        var scene = UITestScene.LoadScene();
        yield return null;

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");

        map.Enable();

        var inputModule = scene.InputModule;
        var previousAction = inputModule.point.action;
        inputModule.point = InputActionReference.Create(pointAction);

        Assert.That(previousAction.enabled, Is.False);
    }

    [UnityTest]
    [Category("UI")]
    [PrebuildSetup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    [PostBuildCleanup(typeof(InputSystemUIInputModuleTestScene_Setup))]
    public IEnumerator UI_WhenAssigningInputModuleAction_ExternalActionsAreNotDisabled()
    {
        var scene = UITestScene.LoadScene();
        yield return null;

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        var scrollAction = map.AddAction("scroll", type: InputActionType.PassThrough, binding: "<Mouse>/scroll/x");

        map.Enable();

        var inputModule = scene.InputModule;
        inputModule.point = InputActionReference.Create(pointAction);
        inputModule.point = InputActionReference.Create(scrollAction);

        Assert.That(pointAction.enabled, Is.True);
    }

    [UnityTest]
    [Category("UI")]
    [Ignore("TODO")]
    public IEnumerator TODO_UI_WhenEnabled_InitialPointerPositionIsPickedUp()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var scene = CreateTestUI();

        // Put mouse over left object.
        Set(mouse.position, scene.From640x480ToScreen(100, 100));

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var pointAction = map.AddAction("point", type: InputActionType.PassThrough, binding: "<Mouse>/position");
        scene.uiModule.point = InputActionReference.Create(pointAction);
        asset.Enable();

        yield return null;

        scene.leftChildReceiver.events.Clear();

        yield return null;

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

        // Get rid of IsUnityTest() GC hit.
        PressAndRelease(mouse.leftButton);

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

        var scene = CreateTestUI();

        // Resize parent to span only half the screen.
        ((RectTransform)scene.parentGameObject.transform).anchorMax = new Vector2(0.5f, 1);

        scene.uiModule.actionsAsset = actions;
        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        // Deselect behavior should be on by default as this corresponds to the behavior before
        // we introduced the switch that allows toggling the behavior off.
        Assert.That(scene.uiModule.deselectOnBackgroundClick, Is.True);

        // Give canvas a chance to set itself up.
        yield return null;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, scene.From640x480ToScreen(10, 10));
        PressAndRelease(mouse.leftButton);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));

        // Click on empty right side and make sure the selection gets cleared.
        Set(mouse.position, scene.From640x480ToScreen(400, 10));
        PressAndRelease(mouse.leftButton);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.Null);

        scene.uiModule.deselectOnBackgroundClick = false;

        // Click on left GO and make sure it gets selected.
        Set(mouse.position, scene.From640x480ToScreen(10, 10));
        PressAndRelease(mouse.leftButton);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));

        // Click on empty right side and make sure our selection does NOT get cleared.
        Set(mouse.position, scene.From640x480ToScreen(400, 10));
        PressAndRelease(mouse.leftButton);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));
    }

    [UnityTest]
    [Category("UI")]
    public IEnumerator UI_WhenBindingsAreReResolved_PointerStatesAreKeptInSync()
    {
        InputSystem.AddDevice<Touchscreen>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("Point", type: InputActionType.PassThrough, binding: "<Touchscreen>/position");
        var clickAction = uiActions.AddAction("Click", type: InputActionType.PassThrough, binding: "<Touchscreen>/press");

        pointAction.wantsInitialStateCheck = true;
        clickAction.wantsInitialStateCheck = true;

        actions.Enable();

        var scene = CreateTestUI();

        scene.uiModule.actionsAsset = actions;
        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);

        yield return null;

        BeginTouch(1, scene.From640x480ToScreen(100, 100), queueEventOnly: true);
        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);

        actions.Disable();
        yield return null;

        // UI module keeps pointer over GO in frame of release.
        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);

        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.False);

        actions.Enable();
        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);

        pointAction.ApplyBindingOverride("<Touchscreen>/primaryTouch/position");
        yield return null;

        Assert.That(EventSystem.current.IsPointerOverGameObject(), Is.True);
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

        var scene = CreateTestUI();

        scene.uiModule.actionsAsset = actions;
        scene.uiModule.point = InputActionReference.Create(pointAction);
        scene.uiModule.leftClick = InputActionReference.Create(clickAction);
        scene.uiModule.move = InputActionReference.Create(navigateAction);

        // Give canvas a chance to set itself up.
        yield return null;

        // Select left GO.
        Set(mouse.position, scene.From640x480ToScreen(10, 10));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));

        // Click on background and make sure we deselect.
        Set(mouse.position, scene.From640x480ToScreen(50, 250));
        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.Null);

        // Now perform a navigate-right action. Given we have no current selection, this should
        // cause the right GO to be selected based on the fact that the left GO was selected last.
        Press(gamepad.dpad.right);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.rightGameObject));

        // Just to make extra sure, navigate left and make sure that results in the expected selection
        // change over to the left GO.
        Release(gamepad.dpad.right);
        Press(gamepad.dpad.left);
        yield return null;

        Assert.That(scene.eventSystem.currentSelectedGameObject, Is.SameAs(scene.leftGameObject));
    }

    [Test]
    [Category("UI")]
    public void UI_CanDriveVirtualMouseCursorFromGamepad()
    {
        const float kCursorSpeed = 100;
        const float kScrollSpeed = 25;

        var eventSystemGO = new GameObject();
        eventSystemGO.SetActive(false);
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<InputSystemUIInputModule>();

        var canvasGO = new GameObject();
        canvasGO.SetActive(false);
        canvasGO.AddComponent<Canvas>();

        var cursorGO = new GameObject();
        cursorGO.SetActive(false);
        var cursorTransform = cursorGO.AddComponent<RectTransform>();
        var cursorInput = cursorGO.AddComponent<VirtualMouseInput>();
        cursorInput.cursorSpeed = kCursorSpeed;
        cursorInput.scrollSpeed = kScrollSpeed;
        cursorInput.cursorTransform = cursorTransform;
        cursorTransform.SetParent(canvasGO.transform, worldPositionStays: false);
        cursorTransform.pivot = new Vector2(0.5f, 0.5f);
        cursorTransform.anchorMin = Vector2.zero;
        cursorTransform.anchorMax = Vector2.zero;
        cursorTransform.anchoredPosition = new Vector2(123, 234);

        var positionAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/*stick");
        var leftButtonAction = new InputAction(binding: "<Gamepad>/buttonSouth");
        var rightButtonAction = new InputAction(binding: "<Gamepad>/rightShoulder");
        var middleButtonAction = new InputAction(binding: "<Gamepad>/leftShoulder");
        var forwardButtonAction = new InputAction(binding: "<Gamepad>/buttonWest");
        var backButtonAction = new InputAction(binding: "<Gamepad>/buttonEast");
        var scrollWheelAction = new InputAction();
        scrollWheelAction.AddCompositeBinding("2DVector(mode=2)")
            .With("Up", "<Gamepad>/leftTrigger")
            .With("Down", "<Gamepad>/rightTrigger")
            .With("Left", "<Gamepad>/dpad/left")
            .With("Right", "<Gamepad>/dpad/right");

        cursorInput.stickAction = new InputActionProperty(positionAction);
        cursorInput.leftButtonAction = new InputActionProperty(leftButtonAction);
        cursorInput.rightButtonAction = new InputActionProperty(rightButtonAction);
        cursorInput.middleButtonAction = new InputActionProperty(middleButtonAction);
        cursorInput.scrollWheelAction = new InputActionProperty(scrollWheelAction);
        cursorInput.forwardButtonAction = new InputActionProperty(forwardButtonAction);
        cursorInput.backButtonAction = new InputActionProperty(backButtonAction);

        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Get rid of deadzones to simplify computations.
        InputSystem.settings.defaultDeadzoneMin = 0;
        InputSystem.settings.defaultDeadzoneMax = 1;

        eventSystemGO.SetActive(true);
        canvasGO.SetActive(true);
        cursorGO.SetActive(true);

        // Make sure the component added a virtual mouse.
        var virtualMouse = Mouse.current;
        Assert.That(virtualMouse, Is.Not.Null);
        Assert.That(virtualMouse.layout, Is.EqualTo("VirtualMouse"));
        Assert.That(cursorInput.virtualMouse, Is.SameAs(virtualMouse));

        // Make sure we can disable and re-enable the component.
        cursorGO.SetActive(false);

        Assert.That(Mouse.current, Is.Null);

        cursorGO.SetActive(true);

        Assert.That(Mouse.current, Is.Not.Null);
        Assert.That(Mouse.current, Is.SameAs(virtualMouse));

        // Ensure everything is at default values.
        // Starting position should be that of the cursor's initial transform.
        Assert.That(virtualMouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(virtualMouse.delta.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(virtualMouse.scroll.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(virtualMouse.leftButton.isPressed, Is.False);
        Assert.That(virtualMouse.rightButton.isPressed, Is.False);
        Assert.That(virtualMouse.middleButton.isPressed, Is.False);
        Assert.That(cursorTransform.anchoredPosition, Is.EqualTo(new Vector2(123, 234)));

        // Now move the mouse cursor with the left stick and ensure we get a response.
        currentTime = 1;
        Set(gamepad.leftStick, new Vector2(0.25f, 0.75f));

        // No time has passed yet so first frame shouldn't move at all.
        Assert.That(virtualMouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(virtualMouse.delta.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(cursorTransform.anchoredPosition, Is.EqualTo(new Vector2(123, 234)));

        currentTime = 1.4;
        InputSystem.Update();

        const float kFirstDeltaX = kCursorSpeed * 0.25f * 0.4f;
        const float kFirstDeltaY = kCursorSpeed * 0.75f * 0.4f;

        Assert.That(virtualMouse.position.ReadValue(), Is.EqualTo(new Vector2(123 + kFirstDeltaX, 234 + kFirstDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(virtualMouse.delta.ReadValue(), Is.EqualTo(new Vector2(kFirstDeltaX, kFirstDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(cursorTransform.anchoredPosition, Is.EqualTo(new Vector2(123 + kFirstDeltaX, 234 + kFirstDeltaY)).Using(Vector2EqualityComparer.Instance));

        // Each update should move the cursor along while the stick is actuated.
        currentTime = 2;
        InputSystem.Update();

        const float kSecondDeltaX = kCursorSpeed * 0.25f * 0.6f;
        const float kSecondDeltaY = kCursorSpeed * 0.75f * 0.6f;

        Assert.That(virtualMouse.position.ReadValue(), Is.EqualTo(new Vector2(123 + kFirstDeltaX + kSecondDeltaX, 234 + kFirstDeltaY + kSecondDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(virtualMouse.delta.ReadValue(), Is.EqualTo(new Vector2(kSecondDeltaX, kSecondDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(cursorTransform.anchoredPosition, Is.EqualTo(new Vector2(123 + kFirstDeltaX + kSecondDeltaX, 234 + kFirstDeltaY + kSecondDeltaY)).Using(Vector2EqualityComparer.Instance));

        // Only the final state of the stick in an update should matter.
        currentTime = 3;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.34f, 0.45f)});
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.45f, 0.56f)});
        InputSystem.Update();

        const float kThirdDeltaX = kCursorSpeed * 0.45f;
        const float kThirdDeltaY = kCursorSpeed * 0.56f;

        Assert.That(virtualMouse.position.ReadValue(), Is.EqualTo(new Vector2(123 + kFirstDeltaX + kSecondDeltaX + kThirdDeltaX, 234 + kFirstDeltaY + kSecondDeltaY + kThirdDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(virtualMouse.delta.ReadValue(), Is.EqualTo(new Vector2(kThirdDeltaX, kThirdDeltaY)).Using(Vector2EqualityComparer.Instance));
        Assert.That(cursorTransform.anchoredPosition, Is.EqualTo(new Vector2(123 + kFirstDeltaX + kSecondDeltaX + kThirdDeltaX, 234 + kFirstDeltaY + kSecondDeltaY + kThirdDeltaY)).Using(Vector2EqualityComparer.Instance));

        var leftClickAction = new InputAction(binding: "<Mouse>/leftButton");
        var middleClickAction = new InputAction(binding: "<Mouse>/middleButton");
        var rightClickAction = new InputAction(binding: "<Mouse>/rightButton");
        var forwardClickAction = new InputAction(binding: "<Mouse>/forwardButton");
        var backClickAction = new InputAction(binding: "<Mouse>/backButton");
        var scrollAction = new InputAction(binding: "<Mouse>/scroll");

        leftClickAction.Enable();
        middleClickAction.Enable();
        rightClickAction.Enable();
        forwardClickAction.Enable();
        backClickAction.Enable();
        scrollAction.Enable();

        // Press buttons.
        PressAndRelease(gamepad.buttonSouth);
        Assert.That(leftClickAction.triggered);
        PressAndRelease(gamepad.rightShoulder);
        Assert.That(rightClickAction.triggered);
        PressAndRelease(gamepad.leftShoulder);
        Assert.That(middleClickAction.triggered);
        PressAndRelease(gamepad.buttonWest);
        Assert.That(forwardClickAction.triggered);
        PressAndRelease(gamepad.buttonEast);
        Assert.That(backClickAction.triggered);

        // Scroll wheel.
        Set(gamepad.leftTrigger, 0.5f);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, kScrollSpeed * 0.5f)).Using(Vector2EqualityComparer.Instance));
        Set(gamepad.rightTrigger, 0.3f);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, kScrollSpeed * (0.5f - 0.3f))).Using(Vector2EqualityComparer.Instance));
        Set(gamepad.leftTrigger, 0);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, -kScrollSpeed * 0.3f)).Using(Vector2EqualityComparer.Instance));
        Press(gamepad.dpad.left);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(-kScrollSpeed, -kScrollSpeed * 0.3f)).Using(Vector2EqualityComparer.Instance));
        Press(gamepad.dpad.right);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0, -kScrollSpeed * 0.3f)).Using(Vector2EqualityComparer.Instance));
        Release(gamepad.dpad.left);
        Assert.That(scrollAction.ReadValue<Vector2>(), Is.EqualTo(new Vector2(kScrollSpeed, -kScrollSpeed * 0.3f)).Using(Vector2EqualityComparer.Instance));
    }

    // Strictly speaking, this functionality is available as of 2021.1 but we can't add a reference to the "com.unity.ui" package
    // to our manifest without breaking test runs with previous versions of Unity. However, in 2021.2, all the UITK functionality
    // has moved into the com.unity.modules.uielements module which is also available in previous versions of Unity. This way we
    // can have a reference to UITK that doesn't break things in previous versions of Unity.
#if UNITY_2021_2_OR_NEWER
    [UnityTest]
    [Category("UI")]
    [TestCase(UIPointerBehavior.AllPointersAsIs, ExpectedResult = 1)]
    [TestCase(UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack, ExpectedResult = 1
    #if UNITY_STANDALONE_OSX && TEMP_DISABLE_UITOOLKIT_TEST
            // temporarily disable this test case on OSX player for 2021.2. It only intermittently works and I don't know why!
        , Ignore = "Currently fails on OSX IL2CPP player on Unity version 2021.2"
    #endif
     )]
    [TestCase(UIPointerBehavior.SingleUnifiedPointer, ExpectedResult = 1)]
#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
    [Ignore("Currently fails on the farm but succeeds locally on Note 10+; needs looking into.")]
#endif
    [PrebuildSetup(typeof(UI_CanOperateUIToolkitInterface_UsingInputSystemUIInputModule_Setup))]
    public IEnumerator UI_CanOperateUIToolkitInterface_UsingInputSystemUIInputModule(UIPointerBehavior pointerBehavior)
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        var scene = SceneManager.LoadScene("UITKTestScene", new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        Assert.That(scene.isLoaded, Is.True, "UITKTestScene did not load as expected");

        try
        {
            var objects = scene.GetRootGameObjects();
            var uiModule = objects.First(x => x.name == "EventSystem").GetComponent<InputSystemUIInputModule>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var uiDocument = objects.First(x => x.name == "UIDocument").GetComponent<UIDocument>();
            var uiRoot = uiDocument.rootVisualElement;
            var uiButton = uiRoot.Query<UnityEngine.UIElements.Button>("Button").First();
            var scrollView = uiRoot.Query<ScrollView>("ScrollView").First();

            uiModule.pointerBehavior = pointerBehavior;

            var clickReceived = false;
            uiButton.clicked += () => clickReceived = true;
            // NOTE: We do *NOT* do the following as the gamepad submit action will *not* trigger a ClickEvent.
            //uiButton.RegisterCallback<ClickEvent>(_ => clickReceived = true);

            yield return null;

            var buttonCenter = new Vector2(uiButton.worldBound.center.x, Screen.height - uiButton.worldBound.center.y);
            var buttonOutside = new Vector2(uiButton.worldBound.max.x + 10, Screen.height - uiButton.worldBound.center.y);
            var scrollViewCenter = new Vector2(scrollView.worldBound.center.x, Screen.height - scrollView.worldBound.center.y);

            Set(mouse.position, buttonCenter, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);

            ////TODO: look at BaseInput and whether we need to override it in order for IME to go through our codepaths
            ////TODO: look into or document raycasting aspect (GraphicRaycaster) when using UITK (disable raycaster?)
            ////TODO: fix scroll wheel bindings on virtual cursor sample

            yield return null;

            Assert.That(uiButton.HasMouseCapture(), Is.True, "Expected uiButton to have mouse capture");

            Release(mouse.leftButton, queueEventOnly: true);

            yield return null;

            Assert.That(uiButton.HasMouseCapture(), Is.False, "Expected uiButton to no longer have mouse capture");
            Assert.That(clickReceived, Is.True);

            // Put mouse in upper right corner and scroll down.
            Assert.That(scrollView.verticalScroller.value, Is.Zero, "Expected verticalScroller to be all the way up");
            Set(mouse.position, scrollViewCenter, queueEventOnly: true);
            yield return null;
            Set(mouse.scroll, new Vector2(0, -100), queueEventOnly: true);
            yield return null;

            ////FIXME: as of a time of writing, this line is broken on trunk due to the bug in UITK
            // The bug is https://fogbugz.unity3d.com/f/cases/1323488/
            // just adding a define as a safeguard measure to reenable it when trunk goes to next version cycle
#if UNITY_2021_3_OR_NEWER
            Assert.That(scrollView.verticalScroller.value, Is.GreaterThan(0));
#endif

            // Try a button press with the gamepad.
            // NOTE: The current version of UITK does not focus the button automatically. Fix for that is in the pipe.
            //       For now focus the button manually.
            uiButton.Focus();
            clickReceived = false;
            PressAndRelease(gamepad.buttonSouth, queueEventOnly: true);
            yield return null;

            Assert.That(clickReceived, Is.True, "Expected to have received click");

            ////TODO: tracked device support (not yet supported by UITK)

            static bool IsActive(VisualElement ve)
            {
                return ve.Query<VisualElement>().Active().ToList().Contains(ve);
            }

            // Move the mouse away from the button to check that touch inputs are also able to activate it.
            Set(mouse.position, buttonOutside, queueEventOnly: true);
            yield return null;
            InputSystem.RemoveDevice(mouse);

            var uiButtonDownCount = 0;
            var uiButtonUpCount = 0;
            uiButton.RegisterCallback<PointerDownEvent>(e => uiButtonDownCount++, TrickleDown.TrickleDown);
            uiButton.RegisterCallback<PointerUpEvent>(e => uiButtonUpCount++, TrickleDown.TrickleDown);

            // Case 1369081: Make sure button doesn't get "stuck" in an active state when multiple fingers are used.
            BeginTouch(1, buttonCenter, screen: touchscreen);
            yield return null;
            Assert.That(uiButtonDownCount, Is.EqualTo(1), "Expected uiButtonDownCount to be 0");
            Assert.That(uiButtonUpCount, Is.EqualTo(0), "Expected uiButtonUpCount to be 0");
            Assert.That(IsActive(uiButton), Is.True, "Expected uiButton to be active");

            BeginTouch(2, buttonOutside, screen: touchscreen);
            yield return null;
            EndTouch(2, buttonOutside, screen: touchscreen);
            yield return null;
            Assert.That(uiButtonDownCount, Is.EqualTo(1), "Expected uiButtonDownCount to be 1");

            if (pointerBehavior == UIPointerBehavior.SingleUnifiedPointer)
            {
                Assert.That(uiButtonUpCount, Is.EqualTo(1), "Expected uiButtonUpCount to be 1");
                Assert.That(IsActive(uiButton), Is.False, "Expected uiButton to no longer be active");
            }
            else
            {
                Assert.That(uiButtonUpCount, Is.EqualTo(0), "Expected uiButtonUpCount to be 0");
                Assert.That(IsActive(uiButton), Is.True, "Expected uiButton to be active");
            }

            EndTouch(1, buttonCenter, screen: touchscreen);
            yield return null;
            Assert.That(uiButtonDownCount, Is.EqualTo(1), "Expected uiButtonDownCount to be 1");
            Assert.That(uiButtonUpCount, Is.EqualTo(1), "Expected uiButtonUpCount to be 1");
            Assert.That(IsActive(uiButton), Is.False, "Expected uiButton to no longer be active");

            InputSystem.RemoveDevice(touchscreen);
        }
        finally
        {
            SceneManager.UnloadSceneAsync(scene);
        }

        // Wait for unload to complete.
        yield return null;
    }

    private class UI_CanOperateUIToolkitInterface_UsingInputSystemUIInputModule_Setup : IPrebuildSetup
    {
        public void Setup()
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene
                { path = "Assets/Tests/InputSystem/Assets/UITKTestScene.unity", enabled = true }).ToArray();
#endif
        }
    }
#endif

    static bool[] canRunInBackgroundValueSource = new[] { false, true };

    [UnityTest]
    public IEnumerator UI_WhenAppLosesAndRegainsFocus_WhileUIButtonIsPressed_UIButtonClickBehaviorShouldDependOnIfDeviceCanRunInBackground(
        [ValueSource(nameof(canRunInBackgroundValueSource))] bool canRunInBackground)
    {
        // Whether we run in the background or not should only move the reset of the mouse button
        // around. Without running in the background, the reset should happen when we come back into focus.
        // With running in the background, the reset should happen when we lose focus.
        runtime.runInBackground = true;

        var scene = CreateUIScene();
        var mousePosition = scene.From640x480ToScreen(100, 100);

        var mouse = InputSystem.AddDevice<Mouse>();
        if (canRunInBackground)
            runtime.SetCanRunInBackground(mouse.device.deviceId);
        Assert.That(mouse.device.canRunInBackground, Is.EqualTo(canRunInBackground)); // sanity check precondition

        // On sync, send current position but with all buttons up.
        SyncMouse(mouse, mousePosition);

        // Turn left object into a button.
        var button = scene.leftGameObject.AddComponent<MyButton>();
        var clicked = false;
        button.onClick.AddListener(() => clicked = true);

        yield return null;
        scene.leftChildReceiver.events.Clear();

        // Put mouse over button and press it.
        Set(mouse.position, mousePosition);
        Press(mouse.leftButton);
        yield return null;

        Assert.That(scene.actions.UI.Click.phase.IsInProgress(), Is.True);

        var clickCanceled = 0;
        scene.actions.UI.Click.canceled += _ => ++ clickCanceled;

        yield return null;

        Assert.That(button.receivedPointerDown, Is.True);
        Assert.That(scene.leftChildReceiver.events,
            EventSequence(
                OneEvent("type", EventType.PointerEnter),
#if UNITY_2021_2_OR_NEWER
                OneEvent("type", EventType.PointerMove),
#endif
                OneEvent("type", EventType.PointerDown),
                OneEvent("type", EventType.InitializePotentialDrag)
            )
        );

        scene.leftChildReceiver.events.Clear();

        runtime.PlayerFocusLost();
        if (canRunInBackground)
            Assert.That(clickCanceled, Is.EqualTo(0));
        else
            Assert.That(clickCanceled, Is.EqualTo(1));
        scene.eventSystem.SendMessage("OnApplicationFocus", false);

        Assert.That(scene.leftChildReceiver.events, Is.Empty);
        Assert.That(scene.eventSystem.hasFocus, Is.False);
        Assert.That(clicked, Is.False);

        runtime.PlayerFocusGained();
        scene.eventSystem.SendMessage("OnApplicationFocus", true);

        yield return null;

        // NOTE: We *do* need the pointer up to keep UI state consistent.

        if (canRunInBackground)
        {
            Assert.That(scene.eventSystem.hasFocus, Is.True);
            Assert.That(button.receivedPointerUp, Is.False);
            Assert.That(mouse.position.ReadValue(), Is.EqualTo(mousePosition));
            Assert.That(mouse.leftButton.isPressed, Is.True);
            Assert.That(clicked, Is.False);

            scene.leftChildReceiver.events.Clear();
            Release(mouse.leftButton);
            yield return null;
        }

        Assert.That(scene.eventSystem.hasFocus, Is.True);
        Assert.That(button.receivedPointerUp, Is.True);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(mousePosition));
        Assert.That(mouse.leftButton.isPressed, Is.False);
        Assert.That(clicked, Is.EqualTo(canRunInBackground));
    }

    public class MyButton : UnityEngine.UI.Button
    {
        public bool receivedPointerDown;
        public bool receivedPointerUp;
        public override void OnPointerDown(PointerEventData eventData)
        {
            receivedPointerDown = true;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            receivedPointerUp = true;
            base.OnPointerDown(eventData);
        }
    }

    private unsafe void SyncMouse(Mouse mouse, Vector2 mousePosition)////FIXME: mousePosition should be by reference.
    {
        runtime.SetDeviceCommandCallback(mouse,
            (id, command) =>
            {
                if (command->type == RequestSyncCommand.Type)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition });
                    return InputDeviceCommand.GenericSuccess;
                }

                return InputDeviceCommand.GenericFailure;
            });
    }

    // This test requires some functionality which ATM is only available through InputTestRuntime (namely, being able to create
    // native devices and set up IOCTLs for them).
    [Test]
    [Category("UI")]
    [Ignore("TODO")]
    public void TODO_UI_CanDriveVirtualMouseCursorFromGamepad_AndWarpSystemMouseIfPresent()
    {
        Assert.Fail();
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
#if UNITY_2021_2_OR_NEWER
        PointerMove,
#endif
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
#if UNITY_2021_2_OR_NEWER
        IPointerMoveHandler,
#endif
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

#if UNITY_2021_2_OR_NEWER
        public void OnPointerMove(PointerEventData eventData)
        {
            events.Add(new Event(EventType.PointerMove, ClonePointerEventData(eventData)));
        }

#endif

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
            // Slider sets useDragThreshold to false. Simulate this happening in response to InitializePotentialDrag
            // to ensure InputSystemUIInputModule handles that correctly.
            // https://fogbugz.unity3d.com/f/cases/1275834/
            Assert.That(eventData.useDragThreshold, Is.True); // Module should have initialized to true.
            eventData.useDragThreshold = false;

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
            return new ExtendedAxisEventData(EventSystem.current)
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
                trackedDevicePosition = extendedEventData.trackedDevicePosition,
#if UNITY_2021_1_OR_NEWER
                pressure = eventData.pressure,
                tangentialPressure = eventData.tangentialPressure,
                altitudeAngle = eventData.altitudeAngle,
                azimuthAngle = eventData.azimuthAngle,
                twist = eventData.twist,
                radius = eventData.radius,
                radiusVariance = eventData.radiusVariance,
#endif
            };

            // Can't set lastPress directly.
            clone.pointerPress = eventData.lastPress;
            clone.pointerPress = eventData.pointerPress;

            return clone;
        }
    }

    private class TestEventSystem : MultiplayerEventSystem
    {
        public bool hasFocus;

        public void InvokeUpdate()
        {
            Update();
        }

        protected override void OnApplicationFocus(bool hasFocus)
        {
            // Sync our focus state to that of the test runtime rather than to the Unity test runner (where
            // debugging may still focus and thus alter the test run).
            hasFocus = ((InputTestRuntime)InputRuntime.s_Instance).isPlayerFocused;
            this.hasFocus = hasFocus;
            base.OnApplicationFocus(hasFocus);
        }
    }

    private static KeyValuePair<string, object> OneEvent(string property, object value)
    {
        return new KeyValuePair<string, object>("OneEvent_" + property, value);
    }

    private static KeyValuePair<string, object> AllEvents(string property, object value)
    {
        return new KeyValuePair<string, object>("AllEvents_" + property, value);
    }

    private static EventConstraint EventSequence(params KeyValuePair<string, object>[] values)
    {
        return new EventConstraint(values);
    }

    private class EventConstraint : Constraint
    {
        public KeyValuePair<string, object>[] values { get; }

        public EventConstraint(KeyValuePair<string, object>[] values)
        {
            this.values = values;
            Description = string.Join("\n", values.Select(p => $"{p.Key}={p.Value}"));
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (!(actual is List<UICallbackReceiver.Event>))
                throw new ArgumentException($"Expecting List<UICallbackReceiver> but got {actual}",
                    nameof(actual));

            var events = (List<UICallbackReceiver.Event>)actual;

            bool Compare(string propertyPath, UICallbackReceiver.Event evt, object value)
            {
                var eventObject = propertyPath == "type" ? (object)evt : evt.data;
                object eventPropertyValue = null;
                Type eventPropertyType = null;
                foreach (var propertyName in propertyPath.Split('.'))
                {
                    if (eventPropertyValue != null)
                        eventObject = eventPropertyValue;
                    if (eventObject == null)
                        return false;
                    var eventProperty = eventObject.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (eventProperty == null)
                    {
                        // Sadly, "hovered" is a field.
                        var field = eventObject.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.Public);
                        if (field == null)
                            throw new Exception($"Could not find '{propertyName}' field or property on {eventObject}");
                        eventPropertyValue = field.GetValue(eventObject);
                        eventPropertyType = field.FieldType;
                    }
                    else
                    {
                        eventPropertyValue = eventProperty.GetValue(eventObject);
                        eventPropertyType = eventProperty.PropertyType;
                    }
                }

                bool result;
                if (eventPropertyType == typeof(float))
                    result = Mathf.Approximately((float)eventPropertyValue, (float)value);
                else if (eventPropertyType == typeof(double))
                    result = NumberHelpers.Approximately((double)eventPropertyValue, (double)value);
                else if (eventPropertyType == typeof(Vector2))
                    result = Vector2EqualityComparer.Instance.Equals((Vector2)eventPropertyValue, (Vector2)value);
                else if (eventPropertyType == typeof(Vector3))
                    result = Vector3EqualityComparer.Instance.Equals((Vector3)eventPropertyValue, (Vector3)value);
                else if (eventPropertyType == typeof(Quaternion))
                {
                    var q1 = (Quaternion)eventPropertyValue;
                    var q2 = (Quaternion)value;
                    result = Mathf.Approximately(q1.x, q2.x)
                        && Mathf.Approximately(q1.y, q2.y)
                        && Mathf.Approximately(q1.z, q2.z)
                        && Mathf.Approximately(q1.w, q2.w);
                }
                else if (typeof(IEnumerable<GameObject>).IsAssignableFrom(eventPropertyType))
                {
                    // This check corresponds to Is.EquivalentTo(), i.e. it only checks for the presence of the same
                    // elements but not for the order of them. The `hovered` property does not maintain a specific
                    // order but rather adds new elements to the end (and thus child elements may appear before *or*
                    // after their parents).
                    var eventPropertyList = (IEnumerable<GameObject>)eventPropertyValue;
                    var valueList = (IEnumerable<GameObject>)value;
                    result = eventPropertyList.All(x => valueList.Contains(x))
                        && eventPropertyList.Count() == valueList.Count();
                }
                else
                    result = value == null || eventPropertyValue == null ? ReferenceEquals(eventPropertyValue, value) : eventPropertyValue.Equals(value);

                if (!result)
                    Debug.Log(
                        $"Expected '{propertyPath}' to be '{value}' ({value?.GetType().GetNiceTypeName()}) but got '{eventPropertyValue}' ({eventPropertyValue?.GetType().GetNiceTypeName()}) instead!");
                return result;
            }

            if (values.Where(x => x.Key.StartsWith("AllEvents_"))
                .Any(p => events.Any(e => !Compare(p.Key.Substring("AllEvents_".Length), e, p.Value))))
                return new ConstraintResult(this, actual, false);

            // Take all OneEvent_XXX entries.
            if (values.Where(x => x.Key.StartsWith("OneEvent_"))
                // Group them.
                .GroupBy(x => x.Key)
                // Compare each entry in the group to the respective entry in `values`.
                .Any(g =>
                    g.Zip(events,
                        (a, b) => Compare(g.Key.Substring("OneEvent_".Length), b, a.Value))
                        .Any(b => !b) ||
                    g.Count() != events.Count))
                return new ConstraintResult(this, actual, false);

            return new ConstraintResult(this, actual, true);
        }
    }

    private class UITestScene
    {
        private UITestScene(Scene scene)
        {
            Scene = scene;
        }

        public static UITestScene LoadScene(LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
#if UNITY_EDITOR
            var scene = EditorSceneManager.LoadSceneInPlayMode(TestScenePath, new LoadSceneParameters(loadSceneMode));
#else
            var scene = SceneManager.LoadScene(TestScenePath, new LoadSceneParameters(loadSceneMode));
#endif
            return new UITestScene(scene);
        }

        public Scene Scene { get; }
        public InputSystemUIInputModule InputModule => Scene.GetRootGameObjects()[0].GetComponent<InputSystemUIInputModule>();

        public const string TestScenePath = "Assets/Tests/InputSystem/Assets/UIInputModuleTestScene.unity";
    }
}
