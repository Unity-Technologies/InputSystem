# On-Screen Controls

On-Screen controls can be used to simulate input devices using UI controls that are displayed and interacted with on the screen. The most prominent example is the use of stick and button controls on touchscreens to emulate a joystick or gamepad.

There are currently two control types implemented out of the box, [buttons](#on-screen-buttons) and [sticks](#on-screen-sticks). Custom controls can be implemented by extending the the base `OnScreenControl` MonoBehaviour (see [details](#writing-custom-on-screen-controls)).

>NOTE: On-Screen controls do not have a predefined visual representation. Instead, setting up the visual aspect of a control is up to you (e.g. by adding a sprite or UI component to the `GameObject`). The On-Screen controls take care of the interaction logic and of setting up and generating input from interactions.

Each on-screen control references the control that it should feed input to using a [control path](Controls.md#control-paths). For example, the following on-screen button will feed input to the right shoulder button of a gamepad:

![OnScreenButton](Images/OnScreenButton.png)

The collection of on-screen controls present in a scene together forms one or more [input devices](Devices.md). One input device is created for each distinct type of device referenced by the controls. For example, if one on-screen button references `<Gamepad>/buttonSouth` and another on-screen button references `<Keyboard>/a`, then both a `Gamepad` and a `Keyboard` will be created. This happens automatically when the components are enabled. When disabled, the devices will automatically be removed again.

The control -- and thus implicitly the device -- that an on-screen control feeds into can be queried using the `OnScreenControl.control` property.

Note that this design allows you to use on-screen controls to create input for arbitrary input devices, not just joysticks or gamepads.

## On-Screen Buttons

The `OnScreenButton` component requires the target control to be a `Button` control. The target control will be set to 1 when a pointer-down (`IPointerDownHandler.OnPointerDown`) is received and will be set to 0 when a pointer-up (`IPointerUpHandler.OnPointerUp`) is received.

![OnScreenButton](Images/OnScreenButton.png)

## On-Screen Sticks

The `OnScreenStick` component requires the target control to be a `Vector2`
control. Movement of the stick control is started on receiving a pointer-down (`IPointerDownHandler.OnPointerDown`) event and stopped on received a pointer-up (`IPointerUpHandler.OnPointerUp`) event.

In-between, the stick is moved according to the pointer being dragged (`IDragHandler.OnDrag`) within a box centered on the pointer down screen point and with an edge length of "Movement Range" as defined in the component's properties. A movement range of 50, for example, means that the stick's on-screen area is 25 pixels up, down, left, and right of the pointer-down point on screen.

![OnScreenStick](Images/OnScreenStick.png)

## Writing Custom On-Screen Controls

Support for new types of [input controls](Controls.md) can be added by extending `OnScreenControl`. An easy example to follow is `OnScreenButton` itself.

```
    [AddComponentMenu("Input/On-Screen Button")]
    public class OnScreenButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData data)
        {
            SendValueToControl(0.0f);
        }

        public void OnPointerDown(PointerEventData data)
        {
            SendValueToControl(1.0f);
        }

        [InputControl(layout = "Button")]
        [SerializeField]
        private string m_ControlPath;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }
    }
```
