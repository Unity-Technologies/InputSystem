using System.Collections;
using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;

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

    // https://fogbugz.unity3d.com/f/cases/1271942
    [UnityTest]
    [Category("Devices")]
    public IEnumerator Devices_CanHaveOnScreenJoystickControls()
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

        var stickGO = new GameObject("Stick");
        stickGO.SetActive(false);
        var stickTransform = stickGO.AddComponent<RectTransform>();
        var stick = stickGO.AddComponent<OnScreenStick>();
        stickGO.AddComponent<Image>();
        stickTransform.SetParent(canvasTransform);
        stickTransform.anchorMin = new Vector2(0, 0);
        stickTransform.anchorMax = new Vector2(0, 0);
        stickTransform.anchoredPosition = new Vector2(100, 100);
        stickTransform.sizeDelta = new Vector2(100, 100);
        stick.controlPath = "<Gamepad>/leftStick";
        stickGO.SetActive(true);

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

        // Touch the stick and drag it upwards.
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

    private class TestEventSystem : EventSystem
    {
        public new void OnApplicationFocus(bool hasFocus)
        {
            base.OnApplicationFocus(hasFocus);
        }

        public new void Update()
        {
            base.Update();
        }
    }
}
