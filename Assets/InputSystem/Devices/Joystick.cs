using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
    public struct JoystickState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('J', 'O', 'Y');

        [InputControl(name = "hat", template = "Dpad", usage = "Hatswitch")]
        [InputControl(name = "trigger", template = "Button", usages = new[] { "PrimaryTrigger", "PrimaryAction" }, bit = (int)Button.Trigger)]
        public int buttons;

        [InputControl(template = "Stick", usage = "Primary2DMotion")]
        public Vector2 stick;

        public enum Button
        {
            // IMPORTANT: Order has to match what is expected by DpadControl.
            HatSwitchUp,
            HatSwitchDown,
            HatSwitchLeft,
            HatSwitchRight,

            Trigger
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    // A joystick with an arbitrary number of buttons and axes.
    // By default comes with just a trigger, a potentially twistable
    // stick and an optional single hatswitch.
    [InputState(typeof(JoystickState))]
    public class Joystick : InputDevice
    {
        public ButtonControl trigger { get; private set; }
        public StickControl stick { get; private set; }

        // Optional features. These may be null.
        public AxisControl twist { get; private set; }
        public DpadControl hat { get; private set; }

        ////REVIEW: are these really useful?
        // List of all buttons and axes on the joystick.
        public ReadOnlyArray<ButtonControl> buttons => new ReadOnlyArray<ButtonControl>(m_Buttons);
        public ReadOnlyArray<AxisControl> axes => new ReadOnlyArray<AxisControl>(m_Axes);

        public static Joystick current { get; internal set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            var buttons = new List<ButtonControl>();
            var axes = new List<AxisControl>();

            FindControlsRecursive(this, buttons, x => !(x.parent is StickControl) && !(x.parent is DpadControl));
            FindControlsRecursive(this, axes, x => !(x is ButtonControl));

            if (buttons.Count > 0)
                m_Buttons = buttons.ToArray();
            if (axes.Count > 0)
                m_Axes = axes.ToArray();

            // Mandatory controls.
            trigger = setup.GetControl<ButtonControl>("{PrimaryTrigger}");
            stick = setup.GetControl<StickControl>("{Primary2DMotion}");

            // Optional controls.
            twist = setup.TryGetControl<AxisControl>("{Twist}");
            hat = setup.TryGetControl<DpadControl>("{Hatswitch}");

            base.FinishSetup(setup);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        ////TODO: move this into InputControl
        private void FindControlsRecursive<TControl>(InputControl parent, List<TControl> controls, Func<TControl, bool> filter)
            where TControl : InputControl
        {
            var parentAsTControl = parent as TControl;
            if (parentAsTControl != null && filter(parentAsTControl))
            {
                controls.Add(parentAsTControl);
            }

            var children = parent.children;
            var childCount = children.Count;
            for (var i = 0; i < childCount; ++i)
            {
                var child = parent.children[i];
                FindControlsRecursive<TControl>(child, controls, filter);
            }
        }

        private ButtonControl[] m_Buttons;
        private AxisControl[] m_Axes;
    }
}
