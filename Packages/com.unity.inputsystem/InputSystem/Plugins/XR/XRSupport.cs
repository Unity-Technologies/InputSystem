using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    static class XRUtilities
    {
        public const string kXRInterface = "XRInput";
    }

    // Sync to UnityXRInputFeatureType in IUnityXRInput.h
    enum FeatureType
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
    struct UsageHint
    {
        public string content;
    }

    //Sync to XRInputFeatureDefinition in XRInputDeviceDefinition.h
    [Serializable]
    struct XRFeatureDescriptor
    {
        public string name;
        public List<UsageHint> usageHints;
        public FeatureType featureType;
        public uint customSize;
    }

    // Sync to UnityXRInputDeviceRole in IUnityXRInput.h
    public enum DeviceRole
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
        public DeviceRole deviceRole;
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
            InputSystem.RegisterControlLayout<XRHMD>();
            InputSystem.RegisterControlLayout<XRController>();

            InputSystem.RegisterControlLayout<WMRHMD>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Microsoft")
                .WithProduct("Windows Mixed Reality HMD"));
            InputSystem.RegisterControlLayout<WMRSpatialController>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Microsoft")
                .WithProduct("Spatial Controller"));

            InputSystem.RegisterControlLayout<OculusHMD>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Oculus")
                .WithProduct("Oculus Rift"));
            InputSystem.RegisterControlLayout<OculusTouchController>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Oculus")
                .WithProduct("^(Oculus Touch Controller)"));
            InputSystem.RegisterControlLayout<OculusTrackingReference>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Oculus")
                .WithProduct("^(Tracking Reference)"));

            InputSystem.RegisterControlLayout<GearVRHMD>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Samsung")
                .WithProduct("Oculus HMD"));
            InputSystem.RegisterControlLayout<GearVRTrackedController>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Samsung")
                .WithProduct("^(Oculus Tracked Remote)"));

            InputSystem.RegisterControlLayout<DaydreamHMD>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithProduct("Daydream HMD"));
            InputSystem.RegisterControlLayout<DaydreamController>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithProduct("Daydream Controller"));

            InputSystem.RegisterControlLayout<ViveHMD>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("HTC")
                .WithProduct(@"Vive MV\."));
            InputSystem.RegisterControlLayout<ViveWand>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("HTC")
                .WithProduct(@"^(OpenVR Controller\(Vive Controller)"));
            InputSystem.RegisterControlLayout<ViveLighthouse>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("HTC")
                .WithProduct(@"^(HTC V2-XD/XE)"));

            InputSystem.RegisterControlLayout<KnucklesController>(
                matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.kXRInterface)
                .WithManufacturer("Valve")
                .WithProduct(@"^(OpenVR Controller\(Knuckles)"));

            InputSystem.onFindControlLayoutForDevice += XRLayoutBuilder.OnFindControlLayoutForDevice;
        }
    }
}
