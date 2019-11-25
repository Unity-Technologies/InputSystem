#if UNITY_EDITOR || UNITY_STANDALONE_LINUX
using System;

namespace UnityEngine.InputSystem.Linux
{
    // These structures are not explicitly assigned, but they are filled in via JSON serialization coming from matching structs in native.
#pragma warning disable 0649

    internal enum JoystickFeatureType
    {
        Invalid = 0,
        Axis,
        Ball,
        Button,
        Hat,

        Max
    }

    internal enum SDLAxisUsage
    {
        Unknown = 0,
        X,
        Y,
        Z,
        RotateX,
        RotateY,
        RotateZ,
        Throttle,
        Rudder,
        Wheel,
        Gas,
        Brake,
        Hat0X,
        Hat0Y,
        Hat1X,
        Hat1Y,
        Hat2X,
        Hat2Y,
        Hat3X,
        Hat3Y,

        Count
    }

    internal enum SDLButtonUsage
    {
        Unknown = 0,
        Trigger,
        Thumb,
        Thumb2,
        Top,
        Top2,
        Pinkie,
        Base,
        Base2,
        Base3,
        Base4,
        Base5,
        Base6,
        Dead,

        A,
        B,
        X,
        Y,
        Z,
        TriggerLeft,
        TriggerRight,
        TriggerLeft2,
        TriggerRight2,
        Select,
        Start,
        Mode,
        ThumbLeft,
        ThumbRight,

        Count
    }

    // JSON must match JoystickFeatureDefinition in native.
    [Serializable]
    internal struct SDLFeatureDescriptor
    {
        public JoystickFeatureType featureType;
        public int usageHint;
        public int featureSize;
        public int offset;
        public int bit;
        public int min;
        public int max;
    }

    [Serializable]
    internal class SDLDeviceDescriptor
    {
        public SDLFeatureDescriptor[] controls;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        internal static SDLDeviceDescriptor FromJson(string json)
        {
            return JsonUtility.FromJson<SDLDeviceDescriptor>(json);
        }
    }

#pragma warning restore 0649

    /// <summary>
    /// A small helper class to aid in initializing and registering SDL devices and layout builders.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class LinuxSupport
    {
        /// <summary>
        /// The current interface code sent with devices to identify as Linux SDL devices.
        /// </summary>
        internal const string kInterfaceName = "Linux";

        public static string GetAxisNameFromUsage(SDLAxisUsage usage)
        {
            return Enum.GetName(typeof(SDLAxisUsage), usage);
        }

        public static string GetButtonNameFromUsage(SDLButtonUsage usage)
        {
            return Enum.GetName(typeof(SDLButtonUsage), usage);
        }

        /// <summary>
        /// Registers all initial templates and the generalized layout builder with the InputSystem.
        /// </summary>
        public static void Initialize()
        {
            InputSystem.onFindLayoutForDevice += SDLLayoutBuilder.OnFindLayoutForDevice;
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_LINUX
