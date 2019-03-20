using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.Linux
{
    // These structures are not explicitly assigned, but they are filled in via JSON serialization coming from matching structs in native.
#pragma warning disable 0649

    public enum JoystickFeatureType
    {
        Invalid = 0,
        Axis,
        Ball,
        Button,
        Hat,

        Max
    }

    public enum SDLAxisUsage
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

    public enum SDLButtonUsage
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

    [Serializable]
    struct SDLFeatureDescriptor
    {
        public JoystickFeatureType featureType;
        public int usageHint;
        public int size;
        public int offset;
        public int bit;
        public Int32 min;
        public Int32 max;
    }

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class SDLDeviceDescriptor
    {
        public List<SDLFeatureDescriptor> controls;

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
    public static class SDLSupport
    {
        /// <summary>
        /// The current interface code sent with devices to identify as Linux SDL devices.
        /// </summary>
        public const string kXRInterfaceCurrent = "Linux";

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
