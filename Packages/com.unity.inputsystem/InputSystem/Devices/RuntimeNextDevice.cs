using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeNextDeviceState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('R', 'N', 'D', 'V');

        [InputControl(layout = "Mouse")]
        // [InputControl(name = "mouse/position")]
        // [InputControl(name = "mouse/delta")]
        // [InputControl(name = "mouse/scroll")]
        // [InputControl(name = "mouse/scroll/x")]
        // [InputControl(name = "mouse/scroll/y")]
        // [InputControl(name = "mouse/press", layout = "Button")]
        // [InputControl(name = "mouse/leftButton")]
        // [InputControl(name = "mouse/rightButton")]
        // [InputControl(name = "mouse/middleButton")]
        // [InputControl(name = "mouse/forwardButton")]
        // [InputControl(name = "mouse/backButton")]
        // [InputControl(name = "mouse/pressure")]
        // [InputControl(name = "mouse/radius")]
        // [InputControl(name = "mouse/pointerId")]
        // [InputControl(name = "mouse/clickCount")]
        public MouseState mouse;

        [InputControl(layout = "Keyboard")]
        public KeyboardState keyboard;

        public FourCC format => Format;
    }
}

namespace UnityEngine.InputSystem
{
    [InputControlLayout(stateType = typeof(RuntimeNextDeviceState))]
    public class RuntimeNextDevice : InputDevice
    {
        public Mouse mouse { get; protected set; }

        public Keyboard keyboard { get; protected set;  }

        protected override void FinishSetup()
        {
            mouse = GetChildControl<Mouse>("mouse");
            keyboard = GetChildControl<Keyboard>("keyboard");
            base.FinishSetup();
        }
    }
}