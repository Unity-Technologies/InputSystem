using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    internal struct JoystickState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('J', 'O', 'Y');

        [InputControl(name = "trigger", displayName = "Trigger", layout = "Button", usages = new[] { "PrimaryTrigger", "PrimaryAction", "Submit" }, bit = (int)Button.Trigger)]
        public int buttons;

        [InputControl(displayName = "Stick", layout = "Stick", usage = "Primary2DMotion", processors = "stickDeadzone")]
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

        public FourCC format => kFormat;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A joystick with an arbitrary number of buttons and axes.
    /// </summary>
    /// <remarks>
    /// Joysticks are somewhat hard to classify as there is little commonality other
    /// than that there is one main stick 2D control and at least one button. From the
    /// input system perspective, everything that is not a <see cref="Gamepad"/> and
    /// that has at least one <see cref="stick"/> and one <see cref="trigger"/> control
    /// is considered a candidate for being a joystick.
    ///
    /// Optionally, a joystick may also have the ability to <see cref="twist"/>, i.e.
    /// for the stick to rotate around its own axis, and at least one <see cref="hatswitch"/>.
    ///
    /// Note that devices based on Joystick may have many more controls. Joystick
    /// itself only defines a minimum required to separate joysticks as a concept
    /// from other types of devices.
    /// </remarks>
    [InputControlLayout(stateType = typeof(JoystickState), isGenericTypeOfDevice = true)]
    [Scripting.Preserve]
    public class Joystick : InputDevice
    {
        /// <summary>
        /// The primary trigger button of the joystick.
        /// </summary>
        /// <value>Control representing the primary trigger button.</value>
        /// <remarks>
        /// This is the <see cref="ButtonControl"/> type control on the joystick
        /// that has the <see cref="CommonUsages.PrimaryTrigger"/> usage.
        /// </remarks>
        public ButtonControl trigger { get; private set; }

        /// <summary>
        /// The 2D axis of the stick itself.
        /// </summary>
        /// <value>Control representing the main joystick axis.</value>
        /// <remarks>
        /// This is the <see cref="StickControl"/> type control on the joystick
        /// that has the <see cref="CommonUsages.Primary2DMotion"/> usage.
        /// </remarks>
        public StickControl stick { get; private set; }

        /// <summary>
        /// An optional control representing the rotation of the stick around its
        /// own axis (i.e. side-to-side circular motion). If not supported, will be
        /// <c>null</c>.
        /// </summary>
        /// <value>Control representing the twist motion of the joystick.</value>
        /// <remarks>
        /// This is the <see cref="AxisControl"/> type control on the joystick
        /// that has the <see cref="CommonUsages.Twist"/> usage.
        /// </remarks>
        public AxisControl twist { get; private set; }

        /// <summary>
        /// An optional control representing a four-way "hat switch" on the
        /// joystick. If not supported, will be <c>null</c>.
        /// </summary>
        /// <value>Control representing a hatswitch on the joystick.</value>
        /// <remarks>
        /// Hat switches are usually thumb-operated four-way switches that operate
        /// much like the "d-pad" on a gamepad (see <see cref="Gamepad.dpad"/>).
        /// If present, this is the <see cref="Vector2Control"/> type control on the
        /// joystick that has the <see cref="CommonUsages.Hatswitch"/> usage.
        /// </remarks>
        public Vector2Control hatswitch { get; private set; }

        /// <summary>
        /// The joystick that was added or used last. Null if there is none.
        /// </summary>
        /// <value>Joystick that was added or used last.</value>
        /// <remarks>
        /// See <see cref="InputDevice.MakeCurrent"/> for details about when a device
        /// is made current.
        /// </remarks>
        /// <seealso cref="all"/>
        public static Joystick current { get; private set; }

        /// <summary>
        /// A list of joysticks currently connected to the system.
        /// </summary>
        /// <value>All currently connected joystick.</value>
        /// <remarks>
        /// Does not cause GC allocation.
        ///
        /// Do <em>not</em> hold on to the value returned by this getter but rather query it whenever
        /// you need it. Whenever the joystick setup changes, the value returned by this getter
        /// is invalidated.
        /// </remarks>
        /// <seealso cref="current"/>
        public new static ReadOnlyArray<Joystick> all => new ReadOnlyArray<Joystick>(s_Joysticks, 0, s_JoystickCount);

        /// <summary>
        /// Called when the joystick has been created but before it is added
        /// to the system.
        /// </summary>
        protected override void FinishSetup()
        {
            // Mandatory controls.
            trigger = GetChildControl<ButtonControl>("{PrimaryTrigger}");
            stick = GetChildControl<StickControl>("{Primary2DMotion}");

            // Optional controls.
            twist = TryGetChildControl<AxisControl>("{Twist}");
            hatswitch = TryGetChildControl<Vector2Control>("{Hatswitch}");

            base.FinishSetup();
        }

        /// <summary>
        /// Make the joystick the <see cref="current"/> one.
        /// </summary>
        /// <remarks>
        /// This is called automatically by the input system when a device
        /// receives input or is added to the system. See <see cref="InputDevice.MakeCurrent"/>
        /// for details.
        /// </remarks>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <summary>
        /// Called when the joystick is added to the system.
        /// </summary>
        protected override void OnAdded()
        {
            ArrayHelpers.AppendWithCapacity(ref s_Joysticks, ref s_JoystickCount, this);
        }

        /// <summary>
        /// Called when the joystick is removed from the system.
        /// </summary>
        protected override void OnRemoved()
        {
            base.OnRemoved();

            if (current == this)
                current = null;

            // Remove from `all`.
            var index = ArrayHelpers.IndexOfReference(s_Joysticks, this, s_JoystickCount);
            if (index != -1)
                ArrayHelpers.EraseAtWithCapacity(s_Joysticks, ref s_JoystickCount, index);
            else
            {
                Debug.Assert(false,
                    $"Joystick {this} seems to not have been added but is being removed (joystick list: {string.Join(", ", all)})"); // Put in else to not allocate on normal path.
            }
        }

        private static int s_JoystickCount;
        private static Joystick[] s_Joysticks;
    }
}
