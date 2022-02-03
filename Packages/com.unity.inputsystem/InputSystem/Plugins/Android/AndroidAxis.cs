#if UNITY_EDITOR || UNITY_ANDROID || PACKAGE_DOCS_GENERATION
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android.LowLevel
{
    /// <summary>
    /// Enum used to identity the axis type in the Android motion input event. See <see cref="AndroidGameControllerState.axis"/>.
    /// See https://developer.android.com/reference/android/view/MotionEvent#constants_1 for more details.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "False positive")]
    public enum AndroidAxis
    {
        /// <summary>
        /// X axis of a motion event.
        /// </summary>
        X = 0,

        /// <summary>
        /// Y axis of a motion event.
        /// </summary>
        Y = 1,

        /// <summary>
        /// Pressure axis of a motion event.
        /// </summary>
        Pressure = 2,

        /// <summary>
        /// Size axis of a motion event.
        /// </summary>
        Size = 3,

        /// <summary>
        /// TouchMajor axis  of a motion event.
        /// </summary>
        TouchMajor = 4,

        /// <summary>
        /// TouchMinor axis of a motion event.
        /// </summary>
        TouchMinor = 5,

        /// <summary>
        /// ToolMajor axis of a motion event.
        /// </summary>
        ToolMajor = 6,

        /// <summary>
        /// ToolMinor axis of a motion event.
        /// </summary>
        ToolMinor = 7,

        /// <summary>
        /// Orientation axis of a motion event.
        /// </summary>
        Orientation = 8,

        /// <summary>
        /// Vertical Scroll of a motion event.
        /// </summary>
        Vscroll = 9,

        /// <summary>
        /// Horizontal Scroll axis of a motion event.
        /// </summary>
        Hscroll = 10,

        /// <summary>
        /// Z axis of a motion event.
        /// </summary>
        Z = 11,

        /// <summary>
        /// X Rotation axis of a motion event.
        /// </summary>
        Rx = 12,

        /// <summary>
        /// Y Rotation axis of a motion event.
        /// </summary>
        Ry = 13,

        /// <summary>
        /// Z Rotation axis of a motion event.
        /// </summary>
        Rz = 14,

        /// <summary>
        /// Hat X axis of a motion event.
        /// </summary>
        HatX = 15,

        /// <summary>
        /// Hat Y axis of a motion event.
        /// </summary>
        HatY = 16,

        /// <summary>
        /// Left Trigger axis of a motion event.
        /// </summary>
        Ltrigger = 17,

        /// <summary>
        /// Right Trigger axis of a motion event.
        /// </summary>
        Rtrigger = 18,

        /// <summary>
        /// Throttle axis of a motion event.
        /// </summary>
        Throttle = 19,

        /// <summary>
        /// Rudder axis of a motion event.
        /// </summary>
        Rudder = 20,

        /// <summary>
        /// Wheel axis of a motion event.
        /// </summary>
        Wheel = 21,

        /// <summary>
        /// Gas axis of a motion event.
        /// </summary>
        Gas = 22,

        /// <summary>
        /// Break axis of a motion event.
        /// </summary>
        Brake = 23,

        /// <summary>
        /// Distance axis of a motion event.
        /// </summary>
        Distance = 24,

        /// <summary>
        /// Tilt axis of a motion event.
        /// </summary>
        Tilt = 25,

        /// <summary>
        /// Generic 1 axis of a motion event.
        /// </summary>
        Generic1 = 32,

        /// <summary>
        /// Generic 2 axis of a motion event.
        /// </summary>
        Generic2 = 33,

        /// <summary>
        /// Generic 3 axis of a motion event.
        /// </summary>
        Generic3 = 34,

        /// <summary>
        /// Generic 4 axis of a motion event.
        /// </summary>
        Generic4 = 35,

        /// <summary>
        /// Generic 5 axis of a motion event.
        /// </summary>
        Generic5 = 36,

        /// <summary>
        /// Generic 6 axis of a motion event.
        /// </summary>
        Generic6 = 37,

        /// <summary>
        /// Generic 7 axis of a motion event.
        /// </summary>
        Generic7 = 38,

        /// <summary>
        /// Generic 8 axis of a motion event.
        /// </summary>
        Generic8 = 39,

        /// <summary>
        /// Generic 9 axis of a motion event.
        /// </summary>
        Generic9 = 40,

        /// <summary>
        /// Generic 10 axis of a motion event.
        /// </summary>
        Generic10 = 41,

        /// <summary>
        /// Generic 11 axis of a motion event.
        /// </summary>
        Generic11 = 42,

        /// <summary>
        /// Generic 12 axis of a motion event.
        /// </summary>
        Generic12 = 43,

        /// <summary>
        /// Generic 13 axis of a motion event.
        /// </summary>
        Generic13 = 44,

        /// <summary>
        /// Generic 14 axis of a motion event.
        /// </summary>
        Generic14 = 45,

        /// <summary>
        /// Generic 15 axis of a motion event.
        /// </summary>
        Generic15 = 46,

        /// <summary>
        /// Generic 16 axis of a motion event.
        /// </summary>
        Generic16 = 47,
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
