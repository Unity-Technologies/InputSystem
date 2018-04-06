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
        public System.UInt32 customSize;
    }

    // Sync to UnityXRInputDeviceRole in IUnityXRInput.h
    enum DeviceRole
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

            InputSystem.RegisterControlLayout<WMRHMD>(deviceDescription: new InputDeviceDescription
            {
                product = "Windows Mixed Reality HMD",
                manufacturer = "Microsoft",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<WMRSpatialController>(deviceDescription: new InputDeviceDescription
            {
                product = "Spatial Controller",
                manufacturer = "Microsoft",
                interfaceName = XRUtilities.kXRInterface
            });

            InputSystem.RegisterControlLayout<OculusHMD>(deviceDescription: new InputDeviceDescription
            {
                product = "Oculus Rift",
                manufacturer = "Oculus",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<OculusTouchController>(deviceDescription: new InputDeviceDescription
            {
                product = "^(Oculus Touch Controller)",
                manufacturer = "Oculus",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<OculusTrackingReference>(deviceDescription: new InputDeviceDescription
            {
                product = "^(Tracking Reference)",
                manufacturer = "Oculus",
                interfaceName = XRUtilities.kXRInterface
            });


            InputSystem.RegisterControlLayout<GearVRHMD>(deviceDescription: new InputDeviceDescription
            {
                product = "Oculus HMD",
                manufacturer = "Samsung",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<GearVRTrackedController>(deviceDescription: new InputDeviceDescription
            {
                product = "^(Oculus Tracked Remote)",
                manufacturer = "Samsung",
                interfaceName = XRUtilities.kXRInterface
            });

            InputSystem.RegisterControlLayout<DaydreamHMD>(deviceDescription: new InputDeviceDescription
            {
                product = "Daydream HMD",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<DaydreamController>(deviceDescription: new InputDeviceDescription
            {
                product = "Daydream Controller",
                interfaceName = XRUtilities.kXRInterface
            });

            InputSystem.RegisterControlLayout<ViveHMD>(deviceDescription: new InputDeviceDescription
            {
                product = "Vive MV.",
                manufacturer = "HTC",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<ViveWand>(deviceDescription: new InputDeviceDescription
            {
                product = @"^(OpenVR Controller\(Vive Controller)",
                manufacturer = "HTC",
                interfaceName = XRUtilities.kXRInterface
            });
            InputSystem.RegisterControlLayout<ViveLighthouse>(deviceDescription: new InputDeviceDescription
            {
                product = @"^(HTC V2-XD/XE)",
                manufacturer = "HTC",
                interfaceName = XRUtilities.kXRInterface
            });

            InputSystem.RegisterControlLayout<KnucklesController>(deviceDescription: new InputDeviceDescription
            {
                product = @"^(OpenVR Controller\(Knuckles)",
                manufacturer = "Valve",
                interfaceName = XRUtilities.kXRInterface
            });


            InputSystem.onFindControlLayoutForDevice += XRLayoutBuilder.OnFindControlLayoutForDevice;
        }
    }
}
