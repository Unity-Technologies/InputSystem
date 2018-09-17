using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.UI;

public class UITests : InputTestFixture
{
    // Set up a UIActionInputModule with a full roster of actions and inputs
    // and then see if we can generate all the various events expected by the UI
    // from activity on input devices.
    [Test]
    [Category("Actions")]
    [Ignore("TODO")]
    public void TODO_Actions_CanDriveUI()
    {
        // Create devices.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        // Set up GameObject with EventSystem.
        var gameObject = new GameObject();
        var eventSystem = gameObject.AddComponent<EventSystem>();
        var uiModule = gameObject.AddComponent<UIActionInputModule>();

        // Set up a GameObject hierarchy that we send events to. In a real setup,
        // this would be a hierarchy involving UI components.
        var parentGameObject = new GameObject();
        var parentTransform = parentGameObject.AddComponent<RectTransform>();
        var parentReceiver = parentGameObject.AddComponent<UICallbackReceiver>();
        var childGameObject = new GameObject();
        var childTransform = childGameObject.AddComponent<RectTransform>();
        var childReceiver = childGameObject.AddComponent<UICallbackReceiver>();

        // Create actions.
        var map = new InputActionMap();
        var pointAction = map.AddAction("point");
        var moveAction = map.AddAction("move");
        var leftClickAction = map.AddAction("leftClick");
        var rightClickAction = map.AddAction("rightClick");
        var middleClickAction = map.AddAction("middleClick");

        // Create bindings.
        pointAction.AddBinding(mouse.position);
        moveAction.AddBinding(gamepad.leftStick);
        leftClickAction.AddBinding(gamepad.buttonSouth);

        // Wire up actions.
        uiModule.point = new InputActionProperty(pointAction);
        uiModule.move = new InputActionProperty(moveAction);
        uiModule.leftClick = new InputActionProperty(leftClickAction);
        uiModule.middleClick = new InputActionProperty(middleClickAction);
        uiModule.rightClick = new InputActionProperty(rightClickAction);

        // Move mouse over object.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(123, 456)});
        InputSystem.Update();

        Assert.Fail();
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

    private class UICallbackReceiver : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler,
        IPointerExitHandler, IPointerUpHandler, IMoveHandler, ISelectHandler, IDeselectHandler
    {
        public enum PointerEventType
        {
            Click,
            Down,
            Up,
            Enter,
            Exit,
        }

        public List<KeyValuePair<PointerEventType, PointerEventData>> pointerEvents =
            new List<KeyValuePair<PointerEventType, PointerEventData>>();
        public List<AxisEventData> axisEvents = new List<AxisEventData>();
        public List<BaseEventData> selectEvents = new List<BaseEventData>();
        public List<BaseEventData> deselectEvents = new List<BaseEventData>();

        public void OnPointerClick(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnMove(AxisEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        private PointerEventData ClonePointerEventData(PointerEventData eventData)
        {
            throw new NotImplementedException();
        }

        public void OnSelect(BaseEventData eventData)
        {
            throw new NotImplementedException();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            throw new NotImplementedException();
        }
    }
}
