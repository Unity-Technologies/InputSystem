using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////FIXME: pointer deltas in EditorWindows need to be Y *down*

////REVIEW: kill EditorWindowSpace processor and add GetPositionInEditorWindowSpace() and GetDeltaInEditorWindowSpace()?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state structure for pointer devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PointerState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('P', 'T', 'R'); }
        }

        [InputControl(layout = "Digital")]
        public uint pointerId;

        /// <summary>
        /// Position of the pointer in screen space.
        /// </summary>
#if UNITY_EDITOR
        [InputControl(layout = "Vector2", usage = "Point", processors = "AutoWindowSpace")]
#else
        [InputControl(layout = "Vector2", usage = "Point")]
#endif
        public Vector2 position;

        [InputControl(layout = "Vector2", usage = "Secondary2DMotion", processors = "Sensitivity")]
        public Vector2 delta;

        [InputControl(layout = "Analog", usage = "Pressure")]
        public float pressure;

        [InputControl(layout = "Axis", usage = "Twist")]
        public float twist;

        [InputControl(layout = "Vector2", usage = "Tilt")]
        public Vector2 tilt;

        [InputControl(layout = "Vector2", usage = "Radius")]
        public Vector2 radius;

        [InputControl(name = "phase", layout = "PointerPhase", sizeInBits = 4)]
        [InputControl(name = "button", layout = "Button", bit = 4, usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        public ushort flags;

        [InputControl(layout = "Digital")]
        public ushort displayIndex;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    ////REVIEW: does it really make sense to have this at the pointer level
    public enum PointerPhase
    {
        /// <summary>
        /// No activity has been registered on the pointer yet.
        /// </summary>
        None,

        Began,
        Moved,
        Ended,
        Cancelled
    }

    /// <summary>
    /// Base class for pointer-style devices moving on a 2D screen.
    /// </summary>
    /// <remarks>
    /// Note that a pointer may have "multi-point" ability as is the case with multi-touch where
    /// multiple touches represent multiple concurrent "pointers". However, for any pointer device
    /// with multiple pointers, only one pointer is considered "primary" and drives the pointer
    /// controls present on the base class.
    /// </remarks>
    [InputLayout(stateType = typeof(PointerState))]
    public class Pointer : InputDevice, IInputStateCallbackReceiver
    {
        ////REVIEW: shouldn't this be done for every touch position, too?
        /// <summary>
        /// The current pointer coordinates in window space.
        /// </summary>
        /// <remarks>
        /// Within player code, the coordinates are in the coordinate space of the <see cref="UnityEngine.Display">
        /// Display</see> space that is current according to <see cref="displayIndex"/>. When running with a
        /// single display, that means the coordinates will always be in window space of the first display.
        ///
        /// Within editor code, the coordinates are in the coordinate space of the current <see cref="UnityEditor.EditorWindow"/>.
        /// This means that if you query <c>Mouse.current.position</c> in <see cref="UnityEditor.EditorWindow.OnGUI"/>, for example,
        /// the returned 2D vector will be in the coordinate space of your local GUI (same as
        /// <see cref="UnityEngine.Event.mousePosition"/>).
        /// </remarks>
        public Vector2Control position { get; private set; }

        public Vector2Control delta { get; private set; }
        public Vector2Control tilt { get; private set; }
        public Vector2Control radius { get; private set; }
        public AxisControl pressure { get; private set; }
        public AxisControl twist { get; private set; }
        public IntegerControl pointerId { get; private set; }
        ////TODO: find a way which gives values as PointerPhase instead of as int
        public PointerPhaseControl phase { get; private set; }
        public IntegerControl displayIndex { get; private set; }////REVIEW: kill this and move to configuration?
        public ButtonControl button { get; private set; }

        /// <summary>
        /// The pointer that was added or updated last or null if there is no pointer
        /// connected to the system.
        /// </summary>
        public static Pointer current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            position = setup.GetControl<Vector2Control>(this, "position");
            delta = setup.GetControl<Vector2Control>(this, "delta");
            tilt = setup.GetControl<Vector2Control>(this, "tilt");
            radius = setup.GetControl<Vector2Control>(this, "radius");
            pressure = setup.GetControl<AxisControl>(this, "pressure");
            twist = setup.GetControl<AxisControl>(this, "twist");
            pointerId = setup.GetControl<IntegerControl>(this, "pointerId");
            phase = setup.GetControl<PointerPhaseControl>(this, "phase");
            displayIndex = setup.GetControl<IntegerControl>(this, "displayIndex");
            button = setup.GetControl<ButtonControl>(this, "button");

            base.FinishSetup(setup);
        }

        bool IInputStateCallbackReceiver.OnCarryStateForward(IntPtr statePtr)
        {
            var x = delta.x;
            var y = delta.y;

            var xValue = x.ReadValueFrom(statePtr);
            var yValue = y.ReadValueFrom(statePtr);

            if (Mathf.Approximately(0f, xValue) && Mathf.Approximately(0f, yValue))
                return false;

            // Reset delta.
            x.WriteValueInto(statePtr, 0f);
            y.WriteValueInto(statePtr, 0f);

            return true;
        }

        void IInputStateCallbackReceiver.OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
            var x = delta.x;
            var y = delta.y;

            // Accumulate delta.
            var oldDeltaX = x.ReadValueFrom(oldStatePtr);
            var oldDeltaY = y.ReadValueFrom(oldStatePtr);
            var newDeltaX = x.ReadValueFrom(newStatePtr);
            var newDeltaY = y.ReadValueFrom(newStatePtr);

            x.WriteValueInto(newStatePtr, oldDeltaX + newDeltaX);
            y.WriteValueInto(newStatePtr, oldDeltaY + newDeltaY);
        }
    }
}
