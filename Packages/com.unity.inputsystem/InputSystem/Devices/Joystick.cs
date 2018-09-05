using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    public struct JoystickState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('J', 'O', 'Y'); }
        }

        [InputControl(name = "hat", layout = "Dpad", usage = "Hatswitch")]
        [InputControl(name = "trigger", layout = "Button", usages = new[] { "PrimaryTrigger", "PrimaryAction" }, bit = (int)Button.Trigger)]
        public int buttons;

        [InputControl(layout = "Stick", usage = "Primary2DMotion")]
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
}

namespace UnityEngine.Experimental.Input
{
    // A joystick with an arbitrary number of buttons and axes.
    // By default comes with just a trigger, a potentially twistable
    // stick and an optional single hatswitch.
    [InputControlLayout(stateType = typeof(JoystickState))]
    public class Joystick : InputDevice
    {
        public ButtonControl trigger { get; private set; }
        public StickControl stick { get; private set; }

        // Optional features. These may be null.
        public AxisControl twist { get; private set; }
        public DpadControl hat { get; private set; }

        ////REVIEW: are these really useful?
        // List of all buttons and axes on the joystick.
        public ReadOnlyArray<ButtonControl> buttons
        {
            get { return new ReadOnlyArray<ButtonControl>(m_Buttons); }
        }

        public ReadOnlyArray<AxisControl> axes
        {
            get { return new ReadOnlyArray<AxisControl>(m_Axes); }
        }

        public static Joystick current { get; internal set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
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
            trigger = builder.GetControl<ButtonControl>("{PrimaryTrigger}");
            stick = builder.GetControl<StickControl>("{Primary2DMotion}");

            // Optional controls.
            twist = builder.TryGetControl<AxisControl>("{Twist}");
            hat = builder.TryGetControl<DpadControl>("{Hatswitch}");

            base.FinishSetup(builder);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
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
