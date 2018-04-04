using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    static class XRUtilities
    {
        public const string kXRInterface = "XRInput";
    }

    // Sync to UnityXRInputFeatureType in IUnityXRInput.h
    enum EFeatureType
    {
        Custom = 0,
        Binary,
        DiscreteStates,
        Axis1D,
        Axis2D,
        Axis3D,
        Rotation,
    }

    // These structures are not explicitly assigned, but they are filled in via JSON serialization coming from matching structs in native.
#pragma warning disable 0649
    [Serializable]
    class UsageHint
    {
        public string content;
    }

    //Sync to XRInputFeatureDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class XRFeatureDescriptor
    {
        public string name;
        public List<UsageHint> usageHints;
        public EFeatureType featureType;
        public System.UInt32 customSize;
    }

    // Sync to UnityXRInputDeviceRole in IUnityXRInput.h
    enum EDeviceRole
    {
        Unknown = 0,
        Generic,
        LeftHanded,
        RightHanded,
        GameController,
        TrackingReference,
        HardwareTracker,
    }

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class XRDeviceDescriptor
    {
        public string deviceName;
        public string manufacturer;
        public string serialNumber;
        public EDeviceRole deviceRole;
        public int deviceId;
        public List<XRFeatureDescriptor> inputFeatures;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static XRDeviceDescriptor FromJson(string json)
        {
            return JsonUtility.FromJson<XRDeviceDescriptor>(json);
        }
    }
#pragma warning restore 0649

    public static class XRSupport
    {
        public static void Initialize()
        {
            XRLayoutBuilder.RegisterLayoutFilter(WMRSupport.FilterLayout);
            InputSystem.RegisterControlLayout<WMRHMD>();
            InputSystem.RegisterControlLayout<WMRSpatialController>();

            XRLayoutBuilder.RegisterLayoutFilter(OculusSupport.FilterLayout);
            InputSystem.RegisterControlLayout<OculusHMD>();
            InputSystem.RegisterControlLayout<OculusTouchController>();

            XRLayoutBuilder.RegisterLayoutFilter(GearVRSupport.FilterLayout);
            InputSystem.RegisterControlLayout<GearVRHMD>();
            InputSystem.RegisterControlLayout<GearVRTrackedController>();

            XRLayoutBuilder.RegisterLayoutFilter(DaydreamSupport.FilterLayout);
            InputSystem.RegisterControlLayout<DaydreamHMD>();
            InputSystem.RegisterControlLayout<DaydreamController>();

            InputSystem.onFindControlLayoutForDevice += XRLayoutBuilder.OnFindControlLayoutForDevice;
        }
    }
}
