#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// An extension to <c>PointerEventData</c> which makes additional data about the input event available.
    /// </summary>
    /// <remarks>
    /// Instances of this class are sent instead of <see cref="PointerEventData"/>  by <see cref="InputSystemUIInputModule"/>
    /// for all pointer-type input.
    ///
    /// The <see cref="PointerEventData.pointerId"/> property will generally correspond to the <see cref="InputDevice.deviceId"/>
    /// of <see cref="device"/>. An exception to this are touches as each <see cref="Touchscreen"/> may generate several pointers
    /// (one for each active finger).
    /// </remarks>
    public class ExtendedPointerEventData : PointerEventData
    {
        public ExtendedPointerEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }

        /// <summary>
        /// The <see cref="InputControl"/> that generated the pointer input.
        /// The device associated with this control should be the same as this event's device.
        /// </summary>
        /// <seealso cref="device"/>
        public InputControl control { get; set; }

        /// <summary>
        /// The <see cref="InputDevice"/> that generated the pointer input.
        /// </summary>
        /// <seealso cref="Pointer"/>
        /// <seealso cref="Touchscreen"/>
        /// <seealso cref="Mouse"/>
        /// <seealso cref="Pen"/>
        public InputDevice device { get; set; }

        /// <summary>
        /// For <see cref="UIPointerType.Touch"/> type pointer input, this is the touch ID as reported by the
        /// <see cref="Touchscreen"/> device.
        /// </summary>
        /// <remarks>
        /// For pointer input that is not coming from touch, this will be 0 (which is not considered a valid touch ID
        /// by the input system).
        ///
        /// Note that for touch input, <see cref="PointerEventData.pointerId"/> will be a combination of the
        /// device ID of <see cref="device"/> and the touch ID to generate a unique pointer ID even if there
        /// are multiple touchscreens.
        /// </remarks>
        /// <seealso cref="TouchControl.touchId"/>
        public int touchId { get; set; }

        /// <summary>
        /// Type of pointer that generated the input.
        /// </summary>
        public UIPointerType pointerType { get; set; }

        public int uiToolkitPointerId { get; set; }

        /// <summary>
        /// For <see cref="UIPointerType.Tracked"/> type pointer input, this is the world-space position of
        /// the <see cref="TrackedDevice"/>.
        /// </summary>
        /// <seealso cref="InputSystemUIInputModule.trackedDevicePosition"/>
        public Vector3 trackedDevicePosition { get; set; }

        /// <summary>
        /// For <see cref="UIPointerType.Tracked"/> type pointer input, this is the world-space orientation of
        /// the <see cref="TrackedDevice"/>.
        /// </summary>
        /// <seealso cref="InputSystemUIInputModule.trackedDeviceOrientation"/>
        public Quaternion trackedDeviceOrientation { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(base.ToString());
            stringBuilder.AppendLine("button: " + button); // Defined in PointerEventData but PointerEventData.ToString() does not include it.
            stringBuilder.AppendLine("clickTime: " + clickTime); // Same here.
            stringBuilder.AppendLine("clickCount: " + clickCount); // Same here.
            stringBuilder.AppendLine("device: " + device);
            stringBuilder.AppendLine("pointerType: " + pointerType);
            stringBuilder.AppendLine("touchId: " + touchId);
            stringBuilder.AppendLine("pressPosition: " + pressPosition);
            stringBuilder.AppendLine("trackedDevicePosition: " + trackedDevicePosition);
            stringBuilder.AppendLine("trackedDeviceOrientation: " + trackedDeviceOrientation);
            #if UNITY_2021_1_OR_NEWER
            stringBuilder.AppendLine("pressure" + pressure);
            stringBuilder.AppendLine("radius: " + radius);
            stringBuilder.AppendLine("azimuthAngle: " + azimuthAngle);
            stringBuilder.AppendLine("altitudeAngle: " + altitudeAngle);
            stringBuilder.AppendLine("twist: " + twist);
            #endif
            #if UNITY_2022_3_OR_NEWER
            stringBuilder.AppendLine("displayIndex: " + displayIndex);
            #endif
            return stringBuilder.ToString();
        }

        internal static int MakePointerIdForTouch(int deviceId, int touchId)
        {
            unchecked
            {
                return (deviceId << 24) + touchId;
            }
        }

        internal static int TouchIdFromPointerId(int pointerId)
        {
            return pointerId & 0xff;
        }

        ////TODO: add pressure and tilt support (probably add after 1.0; probably should have separate actions)
        /*
        /// <summary>
        /// If supported by the input device, this is the pressure level of the pointer contact. This is generally
        /// only supported by <see cref="Pen"/> devices as well as by <see cref="Touchscreen"/>s on phones. If not
        /// supported, this will be 1.
        /// </summary>
        /// <seealso cref="Pointer.pressure"/>
        public float pressure { get; set; }

        /// <summary>
        /// If the pointer input is coming from a <see cref="Pen"/>, this is pen's <see cref="Pen.tilt"/>.
        /// </summary>
        public Vector2 tilt { get; set; }
        */

        internal void ReadDeviceState()
        {
            if (control.parent is Pen pen)
            {
                uiToolkitPointerId = GetPenPointerId(pen);
                #if UNITY_2021_1_OR_NEWER
                pressure = pen.pressure.magnitude;
                azimuthAngle = (pen.tilt.value.x + 1) * Mathf.PI / 2;
                altitudeAngle = (pen.tilt.value.y + 1) * Mathf.PI / 2;
                twist = pen.twist.value * Mathf.PI * 2;
                #endif
                #if UNITY_2022_3_OR_NEWER
                displayIndex = pen.displayIndex.ReadValue();
                #endif
            }
            else if (control.parent is TouchControl touchControl)
            {
                uiToolkitPointerId = GetTouchPointerId(touchControl);
                #if UNITY_2021_1_OR_NEWER
                pressure = touchControl.pressure.magnitude;
                radius = touchControl.radius.value;
                #endif
                #if UNITY_2022_3_OR_NEWER
                displayIndex = touchControl.displayIndex.ReadValue();
                #endif
            }
            else if (control.parent is Touchscreen touchscreen)
            {
                uiToolkitPointerId = GetTouchPointerId(touchscreen.primaryTouch);
                #if UNITY_2021_1_OR_NEWER
                pressure = touchscreen.pressure.magnitude;
                radius = touchscreen.radius.value;
                #endif
                #if UNITY_2022_3_OR_NEWER
                displayIndex = touchscreen.displayIndex.ReadValue();
                #endif
            }
            else
            {
                uiToolkitPointerId = UIElements.PointerId.mousePointerId;
            }
        }

        private static int GetPenPointerId(Pen pen)
        {
            var n = 0;
            foreach (var otherDevice in InputSystem.devices)
                if (otherDevice is Pen otherPen)
                {
                    if (pen == otherPen)
                    {
                        return UIElements.PointerId.penPointerIdBase +
                            Mathf.Min(n, UIElements.PointerId.penPointerCount - 1);
                    }
                    n++;
                }
            return UIElements.PointerId.penPointerIdBase;
        }

        private static int GetTouchPointerId(TouchControl touchControl)
        {
            var i = ((Touchscreen)touchControl.device).touches.IndexOfReference(touchControl);
            return UIElements.PointerId.touchPointerIdBase +
                Mathf.Clamp(i, 0, UIElements.PointerId.touchPointerCount - 1);
        }
    }

    /// <summary>
    /// General type of pointer that generated a <see cref="PointerEventData"/> pointer event.
    /// </summary>
    public enum UIPointerType
    {
        None,

        /// <summary>
        /// A <see cref="Mouse"/> or <see cref="Pen"/> or other general <see cref="Pointer"/>.
        /// </summary>
        MouseOrPen,

        /// <summary>
        /// A <see cref="Touchscreen"/>.
        /// </summary>
        Touch,

        /// <summary>
        /// A <see cref="TrackedDevice"/>.
        /// </summary>
        Tracked,
    }

    /// <summary>
    /// Determine how the UI behaves in the presence of multiple pointer devices.
    /// </summary>
    /// <remarks>
    /// While running, an application may, for example, have both a <see cref="Mouse"/> and a <see cref="Touchscreen"/> device
    /// and both may end up getting bound to the actions of <see cref="InputSystemUIInputModule"/> and thus both may route
    /// input into the UI. When this happens, the pointer behavior decides how the UI input module resolves the ambiguity.
    /// </remarks>
    public enum UIPointerBehavior
    {
        /// <summary>
        /// Any input that isn't <see cref="Touchscreen"/> or <see cref="TrackedDevice"/> input is
        /// treated as a single unified pointer.
        ///
        /// This is the default behavior based on the expectation that mice and pens will generally drive a single on-screen
        /// cursor whereas touch and tracked devices have an inherent ability to generate multiple pointers.
        ///
        /// Note that when input from touch or tracked devices is received, the combined pointer for mice and pens (if it exists)
        /// will be removed. If it was over UI objects, <c>IPointerExitHandler</c>s will be invoked.
        /// </summary>
        SingleMouseOrPenButMultiTouchAndTrack,

        /// <summary>
        /// All input is unified to a single pointer. This means that all input from all pointing devices (<see cref="Mouse"/>,
        /// <see cref="Pen"/>, <see cref="Touchscreen"/>, and <see cref="TrackedDevice"/>) is routed into a single pointer
        /// instance. There is only one position on screen which can be controlled from any of these devices.
        /// </summary>
        SingleUnifiedPointer,

        /// <summary>
        /// Any pointing device, whether it's <see cref="Mouse"/>, <see cref="Pen"/>, <see cref="Touchscreen"/>,
        /// or <see cref="TrackedDevice"/> input, is treated as its own independent pointer and arbitrary many
        /// such pointers can be active at any one time.
        /// </summary>
        AllPointersAsIs,
    }
}
#endif
