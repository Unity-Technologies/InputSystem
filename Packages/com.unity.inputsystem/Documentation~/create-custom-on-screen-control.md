## Create a custom on-screen Control

You can add support for new types of [Input Controls](Controls.md) by extending [`OnScreenControl`](../api/UnityEngine.InputSystem.OnScreen.OnScreenControl.html). An easy example to follow is [`OnScreenButton`](../api/UnityEngine.InputSystem.OnScreen.OnScreenButton.html).

```CSharp
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