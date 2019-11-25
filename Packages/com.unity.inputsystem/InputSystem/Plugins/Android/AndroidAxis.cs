#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android.LowLevel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "False positive")]
    internal enum AndroidAxis
    {
        X = 0,
        Y = 1,
        Pressure = 2,
        Size = 3,
        TouchMajor = 4,
        TouchMinor = 5,
        ToolMajor = 6,
        ToolMinor = 7,
        Orientation = 8,
        Vscroll = 9,
        Hscroll = 10,
        Z = 11,
        Rx = 12,
        Ry = 13,
        Rz = 14,
        HatX = 15,
        HatY = 16,
        Ltrigger = 17,
        Rtrigger = 18,
        Throttle = 19,
        Rudder = 20,
        Wheel = 21,
        Gas = 22,
        Brake = 23,
        Distance = 24,
        Tilt = 25,
        Generic1 = 32,
        Generic2 = 33,
        Generic3 = 34,
        Generic4 = 35,
        Generic5 = 36,
        Generic6 = 37,
        Generic7 = 38,
        Generic8 = 39,
        Generic9 = 40,
        Generic10 = 41,
        Generic11 = 42,
        Generic12 = 43,
        Generic13 = 44,
        Generic14 = 45,
        Generic15 = 46,
        Generic16 = 47,
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
