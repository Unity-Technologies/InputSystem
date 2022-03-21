using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TestState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('R', 'N', 'D', 'V');

        [InputControl(layout = "Mouse")]
        public MouseState mouse;

        [InputControl(layout = "Keyboard")]
        public KeyboardState keyboard;

        public FourCC format => Format;
    }
}

namespace UnityEngine.InputSystem
{
    [InputControlLayout(stateType = typeof(TestState))]
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