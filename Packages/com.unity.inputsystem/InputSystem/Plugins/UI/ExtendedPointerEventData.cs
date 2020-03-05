using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;

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
            stringBuilder.AppendLine("<b>device</b>: " + device);
            stringBuilder.AppendLine("<b>pointerType</b>: " + pointerType);
            stringBuilder.AppendLine("<b>touchId</b>: " + touchId);
            stringBuilder.AppendLine("<b>trackedDevicePosition</b>: " + trackedDevicePosition);
            stringBuilder.AppendLine("<b>trackedDeviceOrientation</b>: " + trackedDeviceOrientation);
            return stringBuilder.ToString();
        }

        internal static int MakePointerIdForTouch(int deviceId, int touchId)
        {
            unchecked
            {
                return (deviceId << 24) + touchId;
            }
        }

        ////TODO: adder pressure and tile support (probably add after 1.0; probably should have separate actions)
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
