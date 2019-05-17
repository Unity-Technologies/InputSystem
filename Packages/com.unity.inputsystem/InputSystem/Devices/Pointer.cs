using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: add capabilities indicating whether pressure and tilt is supported

////REVIEW: should we put lock state directly on Pointer?

////REVIEW: should pointer IDs be required to be globally unique across pointing devices?

////FIXME: pointer deltas in EditorWindows need to be Y *down*

////REVIEW: kill EditorWindowSpace processor and add GetPositionInEditorWindowSpace() and GetDeltaInEditorWindowSpace()?
////        (if we do this, every touch control has to get this, too)

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state structure for pointer devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PointerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('P', 'T', 'R');

        [InputControl(layout = "Digital")]
        public uint pointerId;

        /// <summary>
        /// Position of the pointer in screen space.
        /// </summary>
#if UNITY_EDITOR
        [InputControl(layout = "Vector2", usage = "Point", processors = "AutoWindowSpace", displayName = "Position")]
#else
        [InputControl(layout = "Vector2", usage = "Point", displayName = "Position")]
#endif
        public Vector2 position;

        ////REVIEW: if we have Secondary2DMotion on this, seems like this should be normalized
        [InputControl(layout = "Vector2", usage = "Secondary2DMotion")]
        public Vector2 delta;

        [InputControl(layout = "Analog", usage = "Pressure", defaultState = "1.0")]
        public float pressure;

        [InputControl(layout = "Axis", usage = "Twist")]
        public float twist;

        [InputControl(layout = "Vector2", usage = "Tilt")]
        public Vector2 tilt;

        [InputControl(layout = "Vector2", usage = "Radius")]
        public Vector2 radius;

        [InputControl(name = "phase", layout = "PointerPhase", format = "BIT", sizeInBits = 4)]
        ////TODO: give this control a better name
        [InputControl(name = "button", layout = "Button", format = "BIT", bit = 4, usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        public ushort flags;

        [InputControl(layout = "Digital")]
        public ushort displayIndex;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.InputSystem
{
    ////REVIEW: does it really make sense to have this at the pointer level?
    public enum PointerPhase
    {
        /// <summary>
        /// No activity has been registered on the pointer yet.
        /// </summary>
        None,

        Began,
        Moved,
        Ended,
        Canceled,
        Stationary,
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
    [InputControlLayout(stateType = typeof(PointerState), isGenericTypeOfDevice = true)]
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
        /// This means that if you query <see cref="Mouse.position"/> in <see cref="UnityEditor.EditorWindow.OnGUI"/>, for example,
        /// the returned 2D vector will be in the coordinate space of your local GUI (same as
        /// <see cref="UnityEngine.Event.mousePosition"/>).
        /// </remarks>
        public Vector2Control position { get; private set; }

        public Vector2Control delta { get; private set; }

        public Vector2Control tilt { get; private set; }
        public Vector2Control radius { get; private set; }

        /// <summary>
        /// Normalized pressure with which the pointer is currently pressed while in contact with the pointer surface.
        /// </summary>
        /// <remarks>
        /// This is only meaningful for pointing devices that support pressure. Mice do not, pens usually do, and touch
        /// usually does on mobile platforms.
        ///
        /// Note that it is possible for the value to go above 1 even though it is considered normalized. The reason is
        /// that calibration on the system can put the maximum pressure point below the physically supported maximum value.
        /// </remarks>
        public AxisControl pressure { get; private set; }

        /// <summary>
        /// Rotation of the pointer around its own axis. 0 means the pointer is facing away from the user (12 'o clock position)
        /// and ~1 means the pointer has been rotated clockwise almost one full rotation.
        /// </summary>
        /// <remarks>
        /// Twist is generally only supported by pens and even among pens, twist support is rare. An example product that
        /// supports twist is the Wacom Art Pen.
        ///
        /// The axis of rotation is the vector facing away from the pointer surface when the pointer is facing straight up
        /// (i.e. the surface normal of the pointer surface). When the pointer is tilted, the rotation axis is tilted along
        /// with it.
        /// </remarks>
        public AxisControl twist { get; private set; }

        public IntegerControl pointerId { get; private set; }
        public PointerPhaseControl phase { get; private set; }
        public IntegerControl displayIndex { get; private set; }////TODO: kill this

        ////TODO: give this a better name; primaryButton?
        public ButtonControl button { get; private set; }

        /// <summary>
        /// The pointer that was added or used last by the user or <c>null</c> if there is no pointer
        /// device connected to the system.
        /// </summary>
        public static Pointer current { get; internal set; }

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

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            position = builder.GetControl<Vector2Control>(this, "position");
            delta = builder.GetControl<Vector2Control>(this, "delta");
            tilt = builder.GetControl<Vector2Control>(this, "tilt");
            radius = builder.GetControl<Vector2Control>(this, "radius");
            pressure = builder.GetControl<AxisControl>(this, "pressure");
            twist = builder.GetControl<AxisControl>(this, "twist");
            pointerId = builder.GetControl<IntegerControl>(this, "pointerId");
            phase = builder.GetControl<PointerPhaseControl>(this, "phase");
            displayIndex = builder.GetControl<IntegerControl>(this, "displayIndex");
            button = builder.GetControl<ButtonControl>(this, "button");

            base.FinishSetup(builder);
        }

        protected unsafe bool ResetDelta(void* statePtr, InputControl<float> control)
        {
            ////FIXME: this should compare to default *state* (not value) and write default *state* (not value)
            var value = control.ReadValueFromState(statePtr);
            if (Mathf.Approximately(0f, value))
                return false;
            control.WriteValueIntoState(0f, statePtr);
            return true;
        }

        protected unsafe void AccumulateDelta(void* oldStatePtr, void* newStatePtr, InputControl<float> control)
        {
            ////FIXME: if there's processors on the delta, this is junk
            var oldDelta = control.ReadValueFromState(oldStatePtr);
            var newDelta = control.ReadValueFromState(newStatePtr);
            control.WriteValueIntoState(oldDelta + newDelta, newStatePtr);
        }

        unsafe bool IInputStateCallbackReceiver.OnCarryStateForward(void* statePtr)
        {
            var deltaXChanged = ResetDelta(statePtr, delta.x);
            var deltaYChanged = ResetDelta(statePtr, delta.y);
            return deltaXChanged || deltaYChanged;
        }

        unsafe void IInputStateCallbackReceiver.OnBeforeWriteNewState(void* oldStatePtr, void* newStatePtr)
        {
            AccumulateDelta(oldStatePtr, newStatePtr, delta.x);
            AccumulateDelta(oldStatePtr, newStatePtr, delta.y);
        }

        unsafe bool IInputStateCallbackReceiver.OnReceiveStateWithDifferentFormat(void* statePtr, FourCC stateFormat, uint stateSize, ref uint offsetToStoreAt)
        {
            return false;
        }
    }
}
