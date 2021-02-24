using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools.Utils;

public class GamepadMouseCursorTests : CoreTestsFixture
{
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

    // This test requires some functionality which ATM is only available through InputTestRuntime (namely, being able to create
    // native devices and set up IOCTLs for them).
    [Test]
    [Category("UI")]
    [Ignore("TODO")]
    public void TODO_UI_CanDriveVirtualMouseCursorFromGamepad_AndWarpSystemMouseIfPresent()
    {
        Assert.Fail();
    }
}
