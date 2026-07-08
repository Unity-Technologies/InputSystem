---
uid: input-system-screen-custom-control
---

# Create a custom on-screen control

To create custom [input controls](controls.md), you can extend [`OnScreenControl`](xref:UnityEngine.InputSystem.OnScreen.OnScreenControl).

The following sample demonstrates one way to do this:

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
