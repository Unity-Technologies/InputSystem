using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.Linux
{
    static class DeviceInterfaces
    {
        /// <summary>
        /// The current interface code sent with devices to identify as Linux SDL devices.
        /// </summary>
        public const string kXRInterfaceCurrent = "SDL";
    }

    // These structures are not explicitly assigned, but they are filled in via JSON serialization coming from matching structs in native.
#pragma warning disable 0649

    enum JoystickFeatureType
    {
        Invalid = 0,
        Axis,
        Ball,
        Button, 
        Hat,

        Max
    }

    struct SDLFeatureDescriptor
    {
        public string name;
        public JoystickFeatureType featureType;
        public int size;
        public int offset;
        public int bit;
        public Int32 min;
        public Int32 max;
        public Int32 fuzz;
        public Int32 flat;
    }

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class SDLDeviceDescriptor
    {
        public string product;
        public string manufacturer;
        public string version;
        public string guid;
        public List<SDLFeatureDescriptor> inputFeatures;

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
        /// Registers all initial templates and the generalized layout builder with the InputSystem.
        /// </summary>
        public static void Initialize()
        {
            InputSystem.onFindLayoutForDevice += SDLLayoutBuilder.OnFindLayoutForDevice;
        }
    }
}
