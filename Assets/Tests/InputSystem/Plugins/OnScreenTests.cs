using System;
using System.Collections;
using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal class OnScreenTests : CoreTestsFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenStick()
    {
        var gameObject = new GameObject();
        var stickObject = new GameObject();
        gameObject.AddComponent<Camera>();
        var canvas = gameObject.AddComponent<Canvas>();
        var eventSystem = gameObject.AddComponent<EventSystem>();

        stickObject.AddComponent<RectTransform>();
        var stick = stickObject.AddComponent<OnScreenStick>();
        stick.transform.SetParent(canvas.transform);
        stick.controlPath = "/<Gamepad>/leftStick";

        Assert.That(stick.control.device, Is.TypeOf<Gamepad>());
        Assert.That(stick.control, Is.SameAs(stick.control.device["leftStick"]));
        Assert.That(stick.control, Is.TypeOf<StickControl>());
        var stickControl = (StickControl)stick.control;

        stick.OnDrag(new PointerEventData(eventSystem)
        {
            position = new Vector2(stick.movementRange, stick.movementRange)
        });

        InputSystem.Update();

        Assert.That(stick.control.ReadValueAsObject(),
            Is.EqualTo(stickControl.ProcessValue(new Vector2(stick.movementRange / 2f, stick.movementRange / 2f)))
                .Using(Vector2EqualityComparer.Instance));

        Assert.That(stickObject.transform.position.x, Is.GreaterThan(0.0f));
        Assert.That(stickObject.transform.position.y, Is.GreaterThan(0.0f));
    }

    [UnityTest]
    [Category("Devices")]
    [TestCase(OnScreenStick.Behaviour.RelativePositionWithStaticOrigin, ExpectedResult = -1)]
    [TestCase(OnScreenStick.Behaviour.ExactPositionWithStaticOrigin, ExpectedResult = -1)]
    [TestCase(OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin, ExpectedResult = -1)]
    public IEnumerator Devices_OnScreenStickBehavesAccordingToBehaviourProperty(OnScreenStick.Behaviour expectedBehaviour)
    {
        var systemGO = new GameObject();
        systemGO.SetActive(false);
        TestEventSystem eventSystem = systemGO.AddComponent<TestEventSystem>();
        var uiModule = systemGO.AddComponent<InputSystemUIInputModule>();
        uiModule.AssignDefaultActions();
        eventSystem.UpdateModules();

        var cameraGO = new GameObject();
        var orthoCamera = cameraGO.AddComponent<Camera>();
        orthoCamera.orthographic = true;
        orthoCamera.enabled = true;

        var canvasGO = new GameObject();
        canvasGO.SetActive(false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.planeDistance = 100.0f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvasGO.AddComponent<GraphicRaycaster>();
        canvas.worldCamera = orthoCamera;

        var stickObject = new GameObject();
        stickObject.SetActive(false);
        var stick = stickObject.AddComponent<OnScreenStick>();
        stickObject.AddComponent<Image>();
        stick.controlPath = "/<Gamepad>/leftStick";
        stick.behaviour = expectedBehaviour;

        var initialStickPosition = new Vector2(150, 150);
        var rec = stickObject.GetComponent<RectTransform>();
        rec.anchorMin = new Vector2(0.5f, 0.5f);
        rec.anchorMax = new Vector2(0.5f, 0.5f);
        rec.sizeDelta = new Vector2(100, 100);
        rec.anchoredPosition = initialStickPosition;
        rec.transform.SetParent(canvas.transform, worldPositionStays: false);

        systemGO.SetActive(true);
        canvasGO.SetActive(true);
        stickObject.SetActive(true);
        InputSystem.Update(); // Will invoke stick.Start() on next yield
        yield return null;
        eventSystem.Update();
        yield return null;

        Assert.That(stick.control, Is.TypeOf<StickControl>());
        var stickControl = (StickControl)stick.control;

        // Check that properties can be modified after stick.Start() was called
        stick.movementRange = stick.movementRange + 10;
        stick.dynamicOriginRange = stick.dynamicOriginRange + 50;

        // Check initial state and position of the Stick
        Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        Func<Vector2, Vector2> canvasToScreenSpace = (Vector2 v) =>
            orthoCamera.WorldToScreenPoint(canvas.transform.TransformPoint(v));

        // Press that doesn't touch any (neither the stick nor within it's dynamic placement range)
        var outsidePress = initialStickPosition + new Vector2(-stick.dynamicOriginRange - 1, 0);
        BeginTouch(1, canvasToScreenSpace(outsidePress));
        yield return null;
        eventSystem.Update();
        yield return null;
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.False);
        Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        EndTouch(1, canvasToScreenSpace(outsidePress));
        yield return null;
        eventSystem.Update();
        yield return null;

        // Initial press on the visual stick
        var beginOffset = new Vector2(-stick.movementRange / 2f, 0);
        var beginPressPos = initialStickPosition + beginOffset;
        var touchOrigin = initialStickPosition;
        if (expectedBehaviour == OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin)
        {
            // Touch inside dynamic origin zone (but not touching the visual element)
            beginOffset = new Vector2(-stick.dynamicOriginRange + 1, 0);
            beginPressPos = initialStickPosition + beginOffset;
            touchOrigin = beginPressPos;
            Assert.That(beginOffset.x, Is.LessThan(rec.sizeDelta.x / 2f)); // Touching outside of visual element
        }
        BeginTouch(1, canvasToScreenSpace(beginPressPos));
        yield return null;
        eventSystem.Update();
        yield return null;
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.True);

        switch (expectedBehaviour)
        {
            case OnScreenStick.Behaviour.RelativePositionWithStaticOrigin:
                // Check that the first press didn't cause the stick to move
                Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
                touchOrigin = beginPressPos;
                break;

            case OnScreenStick.Behaviour.ExactPositionWithStaticOrigin:
                // Check that stick moved to the initial pointer position
                Assert.That(rec.anchoredPosition, Is.EqualTo(beginPressPos).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(beginOffset / stick.movementRange)).Using(Vector2EqualityComparer.Instance));
                break;

            case OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin:
                // Check that stick origin moved to the initial pointer position - but remains unactuated
                Assert.That(rec.anchoredPosition, Is.EqualTo(beginPressPos).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(Vector2.zero)).Using(Vector2EqualityComparer.Instance));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(expectedBehaviour), expectedBehaviour, null);
        }

        // Check stick now moves with the pointer drag
        var firstMoveDelta = new Vector2(-stick.movementRange / 2f, 0);
        var firstMovePos = beginPressPos + firstMoveDelta;
        MoveTouch(1, canvasToScreenSpace(firstMovePos));
        yield return null;
        eventSystem.Update();
        yield return null;
        switch (expectedBehaviour)
        {
            case OnScreenStick.Behaviour.RelativePositionWithStaticOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition + firstMoveDelta).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(new Vector2(-0.5f, 0))).Using(Vector2EqualityComparer.Instance));
                break;

            case OnScreenStick.Behaviour.ExactPositionWithStaticOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(firstMovePos).Using(Vector2EqualityComparer.Instance));
                var controlDelta = firstMovePos - initialStickPosition;
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(controlDelta / stick.movementRange)).Using(Vector2EqualityComparer.Instance));
                break;

            case OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(firstMovePos).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(firstMoveDelta / stick.movementRange)).Using(Vector2EqualityComparer.Instance));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(expectedBehaviour), expectedBehaviour, null);
        }

        // Check stick is clamped to stay within it's range of motion
        var secondMoveDelta = new Vector2(100, 100);
        var secondMovePos = touchOrigin + secondMoveDelta;
        MoveTouch(1, canvasToScreenSpace(secondMovePos));
        yield return null;
        eventSystem.Update();
        yield return null;
        var clampedRange = secondMoveDelta;
        clampedRange.Normalize();
        clampedRange.Scale(new Vector2(stick.movementRange, stick.movementRange));
        switch (expectedBehaviour)
        {
            case OnScreenStick.Behaviour.RelativePositionWithStaticOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition + clampedRange).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(clampedRange)).Using(new Vector2EqualityComparer(0.01f)));
                break;

            case OnScreenStick.Behaviour.ExactPositionWithStaticOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition + clampedRange).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(clampedRange)).Using(new Vector2EqualityComparer(0.01f)));
                break;

            case OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin:
                Assert.That(rec.anchoredPosition, Is.EqualTo(beginPressPos + clampedRange).Using(Vector2EqualityComparer.Instance));
                Assert.That(Gamepad.all[0].leftStick.ReadValue(),
                    Is.EqualTo(stickControl.ProcessValue(clampedRange)).Using(new Vector2EqualityComparer(0.01f)));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(expectedBehaviour), expectedBehaviour, null);
        }

        // Releasing restores the stick position
        EndTouch(1, beginPressPos);
        yield return null;
        eventSystem.Update();
        yield return null;
        Assert.That(rec.anchoredPosition, Is.EqualTo(initialStickPosition).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].leftStick.ReadValue(),
            Is.EqualTo(stickControl.ProcessValue(Vector2.zero)).Using(Vector2EqualityComparer.Instance));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenButton()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "/<Keyboard>/a";

        Assert.That(InputSystem.devices, Has.Exactly(1).InstanceOf<Keyboard>());
        var keyboard = (Keyboard)InputSystem.devices.FirstOrDefault(x => x is Keyboard);

        Assert.That(keyboard.aKey.isPressed, Is.False);

        button.OnPointerDown(null);
        InputSystem.Update();
        Assert.That(keyboard.aKey.isPressed, Is.True);

        button.OnPointerUp(null);
        InputSystem.Update();
        Assert.That(keyboard.aKey.isPressed, Is.False);
    }

    // When we receive the OnPointerDown event in OnScreenButton, someone may disable the button as a response.
    // In that case, we don't get an OnPointerUp. Ensure that the OnScreenButton correctly resets the state of
    // its InputControl when the button is enabled.
    [Test]
    [Category("Devices")]
    public void Devices_CanDisableOnScreenButtonFromPressEvent()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "<Keyboard>/a";

        // Add a second button so that the device doesn't go away when we disable
        // the first one.
        new GameObject().AddComponent<OnScreenButton>().controlPath = "<Keyboard>/b";

        // When we disable the OnScreenComponent, the keyboard goes away, so use a state monitor
        // to observe the change.
        bool? isPressed = null;
        InputState.AddChangeMonitor(Keyboard.current.aKey,
            (control, time, eventPtr, index) =>
            {
                isPressed = ((ButtonControl)control).isPressed;
            });

        button.OnPointerDown(null);
        InputSystem.Update();

        Assert.That(isPressed, Is.True);

        isPressed = null;
        gameObject.SetActive(false);
        InputSystem.Update();

        Assert.That(isPressed, Is.False);
    }

    ////TODO: we should allow this as an optional feature
    [Test]
    [Category("Devices")]
    public void Devices_OnScreenControlsDoNotUseExistingDevices()
    {
        var existingKeyboard = InputSystem.AddDevice<Keyboard>();
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "/<Keyboard>/a";

        Assert.That(existingKeyboard.aKey.isPressed, Is.False);
        button.OnPointerDown(null);
        InputSystem.Update();
        Assert.That(existingKeyboard.aKey.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenControlsShareDevicesOfTheSameType()
    {
        var gameObject = new GameObject();
        var aKey = gameObject.AddComponent<OnScreenButton>();
        var bKey = gameObject.AddComponent<OnScreenButton>();
        var leftTrigger = gameObject.AddComponent<OnScreenButton>();

        aKey.controlPath = "/<Keyboard>/a";
        bKey.controlPath = "/<Keyboard>/b";
        leftTrigger.controlPath = "/<Gamepad>/leftTrigger";

        Assert.That(aKey.control.device, Is.SameAs(bKey.control.device));
        Assert.That(aKey.control.device, Is.Not.SameAs(leftTrigger.control.device));
        Assert.That(bKey.control.device, Is.Not.SameAs(leftTrigger.control.device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_DisablingLastOnScreenControlRemovesCreatedDevice()
    {
        var gameObject = new GameObject();
        var buttonA = gameObject.AddComponent<OnScreenButton>();
        var buttonB = gameObject.AddComponent<OnScreenButton>();
        buttonA.controlPath = "/<Keyboard>/a";
        buttonB.controlPath = "/<Keyboard>/b";

        Assert.That(InputSystem.devices, Has.Exactly(1).InstanceOf<Keyboard>());

        buttonA.enabled = false;

        Assert.That(InputSystem.devices, Has.Exactly(1).InstanceOf<Keyboard>());

        buttonB.enabled = false;

        Assert.That(InputSystem.devices, Has.None.InstanceOf<Keyboard>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_DisablingLastOnScreenControlDoesReportActiveControl()
    {
        var gameObject = new GameObject();

        Assert.That(OnScreenControl.HasAnyActive, Is.False);

        var buttonA = gameObject.AddComponent<OnScreenButton>();

        Assert.That(OnScreenControl.HasAnyActive, Is.True);

        var buttonB = gameObject.AddComponent<OnScreenButton>();
        buttonA.controlPath = "/<Keyboard>/a";
        buttonB.controlPath = "/<Keyboard>/b";

        Assert.That(OnScreenControl.HasAnyActive, Is.True);

        buttonA.enabled = false;

        Assert.That(OnScreenControl.HasAnyActive, Is.True);

        buttonB.enabled = false;

        Assert.That(OnScreenControl.HasAnyActive, Is.False);
    }

    // https://fogbugz.unity3d.com/f/cases/1271942
    [UnityTest]
    [Category("Devices")]
    public IEnumerator Devices_CanHaveOnScreenJoystickControls([Values(false, true)] bool useInIsolation)
    {
        foreach (var c in Camera.allCameras)
            Object.Destroy(c.gameObject);

        yield return null;

        InputSystem.AddDevice<Touchscreen>();

        // Set up a full UI scene with an on-screen stick and button.

        var eventSystemGO = new GameObject("EventSystem");
        var eventSystem = eventSystemGO.AddComponent<TestEventSystem>();
        var uiModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
        eventSystem.OnApplicationFocus(true);

        var uiActions = new DefaultInputActions();
        uiModule.actionsAsset = uiActions.asset;
        uiModule.leftClick = InputActionReference.Create(uiActions.UI.Click);
        uiModule.point = InputActionReference.Create(uiActions.UI.Point);

        var canvasGO = new GameObject("Canvas");
        var canvasTransform = canvasGO.AddComponent<RectTransform>();
        var canvas = canvasGO.AddComponent<Canvas>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var stickLeftGO = new GameObject("StickLeft");
        stickLeftGO.SetActive(false);
        var stickLeftTransform = stickLeftGO.AddComponent<RectTransform>();
        var stickLeft = stickLeftGO.AddComponent<OnScreenStick>();
        stickLeft.useIsolatedInputActions = useInIsolation;
        stickLeftGO.AddComponent<Image>();
        stickLeftTransform.SetParent(canvasTransform);
        stickLeftTransform.anchorMin = new Vector2(0, 0);
        stickLeftTransform.anchorMax = new Vector2(0, 0);
        stickLeftTransform.anchoredPosition = new Vector2(100, 100);
        stickLeftTransform.sizeDelta = new Vector2(100, 100);
        stickLeft.controlPath = "<Gamepad>/leftStick";
        stickLeftGO.SetActive(true);

        var stickRightGO = new GameObject("StickRight");
        stickRightGO.SetActive(false);
        var stickRightTransform = stickRightGO.AddComponent<RectTransform>();
        var stickRight = stickRightGO.AddComponent<OnScreenStick>();
        stickRight.useIsolatedInputActions = useInIsolation;
        stickRightGO.AddComponent<Image>();
        stickRightTransform.SetParent(canvasTransform);
        stickRightTransform.anchorMin = new Vector2(0, 0);
        stickRightTransform.anchorMax = new Vector2(0, 0);
        stickRightTransform.anchoredPosition = new Vector2(500, 100);
        stickRightTransform.sizeDelta = new Vector2(100, 100);
        stickRight.controlPath = "<Gamepad>/rightStick";
        stickRightGO.SetActive(true);

        var buttonGO = new GameObject("Button");
        buttonGO.SetActive(false);
        var buttonTransform = buttonGO.AddComponent<RectTransform>();
        var button = buttonGO.AddComponent<OnScreenButton>();
        buttonGO.AddComponent<Button>();
        buttonGO.AddComponent<Image>(); // Give it a Graphic so the raycaster sees it.
        buttonTransform.SetParent(canvasTransform);
        buttonTransform.anchorMin = new Vector2(0, 0);
        buttonTransform.anchorMax = new Vector2(0, 0);
        buttonTransform.anchoredPosition = new Vector2(300, 100);
        buttonTransform.sizeDelta = new Vector2(100, 100);
        button.controlPath = "<Gamepad>/buttonSouth";
        buttonGO.SetActive(true);

        // Add player and hook it up to the gamepad.
        var playerActions = new DefaultInputActions();
        var playerGO = new GameObject("Player");
        playerGO.SetActive(false);
        var player = playerGO.AddComponent<PlayerInput>();
        player.actions = playerActions.asset;
        player.defaultControlScheme = "Gamepad";
        player.neverAutoSwitchControlSchemes = true;
        playerGO.SetActive(true);

        yield return null;
        eventSystem.Update();

        Assert.That(player.devices, Is.EquivalentTo(new[] { Gamepad.all[0] }));

        // Touch the Left stick and drag it upwards.
        BeginTouch(1, new Vector2(150, 150));
        yield return null;
        eventSystem.Update();
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.True);
        MoveTouch(1, new Vector2(150, 200));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Stick is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(new Vector2(0, 1)).Using(Vector2EqualityComparer.Instance));

        // Press the button.
        BeginTouch(2, new Vector2(350, 150));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Button is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].buttonSouth.isPressed, Is.True);

        // Release the button.
        EndTouch(2, new Vector2(351, 151));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Button is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].buttonSouth.isPressed, Is.False);

        // Touch the right stick and drag it downwards
        BeginTouch(2, new Vector2(550, 150));
        yield return null;
        eventSystem.Update();
        Assert.That(eventSystem.IsPointerOverGameObject(), Is.True);
        MoveTouch(2, new Vector2(550, 50));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Stick is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(new Vector2(0, 1)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].rightStick.ReadValue(), Is.EqualTo(new Vector2(0, -1)).Using(Vector2EqualityComparer.Instance));

        // Release finger one and move second and ensure that it still works
        EndTouch(1, new Vector2(550, 200));
        MoveTouch(2, new Vector2(600, 150));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Stick is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(new Vector2(0, 0)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].rightStick.ReadValue(), Is.EqualTo(new Vector2(1, 0)).Using(Vector2EqualityComparer.Instance));

        // Release finger two
        EndTouch(2, new Vector2(600, 150));
        yield return null;
        eventSystem.Update();
        InputSystem.Update(); // Stick is feeding events when responding to UI events.

        Assert.That(Gamepad.all[0].leftStick.ReadValue(), Is.EqualTo(new Vector2(0, 0)).Using(Vector2EqualityComparer.Instance));
        Assert.That(Gamepad.all[0].rightStick.ReadValue(), Is.EqualTo(new Vector2(0, 0)).Using(Vector2EqualityComparer.Instance));
    }

    [UnityTest]
    [Category("Devices")]
    public IEnumerator Devices_OnScreenStickDoesNotReceivePointerUpEventsInIsolatedMode()
    {
        InputSystem.AddDevice<Touchscreen>();

        var uiTestScene = new UITestScene(this);
        var image = uiTestScene.AddImage();

        var stick = image.gameObject.AddComponent<OnScreenStick>();
        stick.transform.SetParent(uiTestScene.canvas.transform);
        stick.controlPath = "<Gamepad>/leftStick";
        stick.useIsolatedInputActions = true;

        var stickOriginPosition = ((RectTransform)stick.transform).anchoredPosition;


        // PlayerInput listens for unpaired device activity and then switches to that device which has the effect
        // of re-resolving bindings, which causes any active actions to cancel. This code replicates that.
        InputUser.listenForUnpairedDeviceActivity++;
        InputUser.PerformPairingWithDevice(InputSystem.GetDevice<Touchscreen>());
        InputUser.onUnpairedDeviceUsed += (control, eventPtr) =>
        {
            uiTestScene.uiInputModule.actionsAsset.actionMaps[0].LazyResolveBindings(true);
        };

        // Ensure that the OnScreenStick component has been started
        yield return null;

        yield return uiTestScene.PressAndDrag(image, new Vector2(50, 50));

        // The OnScreenStick when being driven from the UI (non-isolated mode) queues the events into the next
        // frame, because the UI events are processed after the input system update has completed (as opposed to running
        // inside input action callbacks. When events are queued from there, they are processed in the same frame), so
        // we need an extra frame here to flush those events.
        yield return null;

        // The effect on the stick of cancelling the pointer action is that it jumps back to the center position,
        // so assert that it hasn't done that
        Assert.That(stick.gameObject.GetComponent<RectTransform>().anchoredPosition, Is.Not.EqualTo(stickOriginPosition));
    }

    // https://fogbugz.unity3d.com/f/cases/1305016/
    [Test]
    [Category("Devices")]
    public void Devices_CanUseKeyboardCurrentAfterDisablingOnScreenButton()
    {
        var systemKeyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(Keyboard.current, Is.EqualTo(systemKeyboard));

        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "<Keyboard>/a";

        Assert.That(Keyboard.current, Is.Not.EqualTo(systemKeyboard));
        Assert.That(Keyboard.current, Is.Not.Null);

        gameObject.SetActive(false);

        Assert.That(Keyboard.current, Is.EqualTo(systemKeyboard));
    }

    // https://fogbugz.unity3d.com/f/cases/1380790/
    // This test only indirectly triggers the problem in that report (the specific cause is
    // covered by Devices_CanSetUsagesOnDevice_WithoutAnyControlWithUsages) but it is useful
    // in that it covers quite a bit of ground and thus provides a general sanity check.
    [Test]
    [Category("Devices")]
    public void Devices_CanUseOnScreenButtonWithCustomDevice()
    {
        InputSystem.RegisterLayout<CustomDevice>();

        var gameObject = new GameObject();
        var buttonObject = new GameObject();

        gameObject.SetActive(false);
        buttonObject.SetActive(false);

        gameObject.AddComponent<Camera>();
        var canvas = gameObject.AddComponent<Canvas>();
        gameObject.AddComponent<EventSystem>();
        gameObject.AddComponent<InputSystemUIInputModule>();
        buttonObject.transform.SetParent(canvas.transform);

        var button = buttonObject.AddComponent<OnScreenButton>();
        button.controlPath = "<CustomDevice>/button";

        gameObject.SetActive(true);
        buttonObject.SetActive(true);

        LogAssert.NoUnexpectedReceived();
    }

    public class CustomDevice : InputDevice
    {
        [InputControl]
        public ButtonControl button { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            button = GetChildControl<ButtonControl>("button");
        }
    }

    private class TestEventSystem : EventSystem
    {
        public bool hasFocus;
        public new void OnApplicationFocus(bool hasFocus)
        {
            this.hasFocus = hasFocus;
            base.OnApplicationFocus(hasFocus);
        }

        public new void Update()
        {
            base.Update();
        }
    }
}
