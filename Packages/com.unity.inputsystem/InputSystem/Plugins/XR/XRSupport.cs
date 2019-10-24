#if ENABLE_VR || PACKAGE_DOCS_GENERATION
using System;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// A set of static utilities for registering XR Input Devices externally.
    /// </summary>
    public static class XRUtilities
    {
        /// <summary>
        /// A simple Regex pattern that allows InputDeviceMatchers to match to any version of the XRInput interface.
        /// </summary>
        public const string InterfaceMatchAnyVersion = "^(XRInput)";

        /// <summary>
        /// The initial, now deprecated interface for XRInput.  This version handles button packing for Android differently from current.
        /// </summary>
        public const string InterfaceV1 = "XRInput";

        /// <summary>
        /// The current interface code sent with devices to identify as XRInput devices.
        /// </summary>
        public const string InterfaceCurrent = "XRInputV1";
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
        Hand,
        Bone,
        Eyes
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

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class XRDeviceDescriptor
    {
        public string deviceName;
        public string manufacturer;
        public string serialNumber;
#if UNITY_2019_3_OR_NEWER
        public InputDeviceCharacteristics characteristics;
#else //UNITY_2019_3_OR_NEWER
        public InputDeviceRole deviceRole;
#endif //UNITY_2019_3_OR_NEWER
        public int deviceId;
        public List<XRFeatureDescriptor> inputFeatures;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        internal static XRDeviceDescriptor FromJson(string json)
        {
            return JsonUtility.FromJson<XRDeviceDescriptor>(json);
        }
    }

    /// <summary>
    /// Represents a 3 dimensional, tracked bone within a hierarchy of other bones.
    /// </summary>
    public struct Bone
    {
        /// <summary>
        /// The index with the device's controls array where the parent bone resides.
        /// </summary>
        public uint parentBoneIndex { get; set; }
        /// <summary>
        /// The tracked position of the bone.
        /// </summary>
        public Vector3 position { get; set; }
        /// <summary>
        /// The tracked rotation of the bone.
        /// </summary>
        public Quaternion rotation { get; set; }
    }

    /// <summary>
    /// Represents a pair of tracked eyes.
    /// </summary>
    public struct Eyes
    {
        /// <summary>
        /// The tracked position of the left eye.
        /// </summary>
        public Vector3 leftEyePosition { get; set; }
        /// <summary>
        /// The tracked rotation of the left eye.
        /// </summary>
        public Quaternion leftEyeRotation { get; set; }
        /// <summary>
        /// The tracked position of the right eye.
        /// </summary>
        public Vector3 rightEyePosition { get; set; }
        /// <summary>
        /// The tracked rotation of the right eye.
        /// </summary>
        public Quaternion rightEyeRotation { get; set; }
        /// <summary>
        /// The point in 3D space that the pair of eyes is looking.
        /// </summary>
        public Vector3 fixationPoint { get; set; }
        /// <summary>
        /// The amount [0-1] the left eye is open or closed.  1.0 is fully open.
        /// </summary>
        public float leftEyeOpenAmount { get; set; }
        /// <summary>
        /// The amount [0-1] the right eye is open or closed.  1.0 is fully open.
        /// </summary>
        public float rightEyeOpenAmount { get; set; }
    }

    [Preserve]
    public class BoneControl : InputControl<Bone>
    {
        [Preserve]
        [InputControl(offset = 0, displayName = "parentBoneIndex")]
        public IntegerControl parentBoneIndex { get; private set; }
        [Preserve]
        [InputControl(offset = 4, displayName = "Position")]
        public Vector3Control position { get; private set; }
        [Preserve]
        [InputControl(offset = 16, displayName = "Rotation")]
        public QuaternionControl rotation { get; private set; }

        protected override void FinishSetup()
        {
            parentBoneIndex = GetChildControl<IntegerControl>("parentBoneIndex");
            position = GetChildControl<Vector3Control>("position");
            rotation = GetChildControl<QuaternionControl>("rotation");

            base.FinishSetup();
        }

        public override unsafe Bone ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Bone()
            {
                parentBoneIndex = (uint)parentBoneIndex.ReadUnprocessedValueFromState(statePtr),
                position = position.ReadUnprocessedValueFromState(statePtr),
                rotation = rotation.ReadUnprocessedValueFromState(statePtr)
            };
        }

        public override unsafe void WriteValueIntoState(Bone value, void* statePtr)
        {
            parentBoneIndex.WriteValueIntoState((int)value.parentBoneIndex, statePtr);
            position.WriteValueIntoState(value.position, statePtr);
            rotation.WriteValueIntoState(value.rotation, statePtr);
        }
    }

    [Preserve]
    public class EyesControl : InputControl<Eyes>
    {
        [Preserve]
        [InputControl(offset = 0, displayName = "LeftEyePosition")]
        public Vector3Control leftEyePosition { get; private set; }
        [Preserve]
        [InputControl(offset = 12, displayName = "LeftEyeRotation")]
        public QuaternionControl leftEyeRotation { get; private set; }
        [Preserve]
        [InputControl(offset = 28, displayName = "RightEyePosition")]
        public Vector3Control rightEyePosition { get; private set; }
        [Preserve]
        [InputControl(offset = 40, displayName = "RightEyeRotation")]
        public QuaternionControl rightEyeRotation { get; private set; }
        [Preserve]
        [InputControl(offset = 56, displayName = "FixationPoint")]
        public Vector3Control fixationPoint { get; private set; }
        [Preserve]
        [InputControl(offset = 68, displayName = "LeftEyeOpenAmount")]
        public AxisControl leftEyeOpenAmount { get; private set; }
        [Preserve]
        [InputControl(offset = 72, displayName = "RightEyeOpenAmount")]
        public AxisControl rightEyeOpenAmount { get; private set; }

        protected override void FinishSetup()
        {
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            fixationPoint = GetChildControl<Vector3Control>("fixationPoint");
            leftEyeOpenAmount = GetChildControl<AxisControl>("leftEyeOpenAmount");
            rightEyeOpenAmount = GetChildControl<AxisControl>("rightEyeOpenAmount");

            base.FinishSetup();
        }

        public override unsafe Eyes ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Eyes()
            {
                leftEyePosition = leftEyePosition.ReadUnprocessedValueFromState(statePtr),
                leftEyeRotation = leftEyeRotation.ReadUnprocessedValueFromState(statePtr),
                rightEyePosition = rightEyePosition.ReadUnprocessedValueFromState(statePtr),
                rightEyeRotation = rightEyeRotation.ReadUnprocessedValueFromState(statePtr),
                fixationPoint = fixationPoint.ReadUnprocessedValueFromState(statePtr),
                leftEyeOpenAmount = leftEyeOpenAmount.ReadUnprocessedValueFromState(statePtr),
                rightEyeOpenAmount = rightEyeOpenAmount.ReadUnprocessedValueFromState(statePtr)
            };
        }

        public override unsafe void WriteValueIntoState(Eyes value, void* statePtr)
        {
            leftEyePosition.WriteValueIntoState(value.leftEyePosition, statePtr);
            leftEyeRotation.WriteValueIntoState(value.leftEyeRotation, statePtr);
            rightEyePosition.WriteValueIntoState(value.rightEyePosition, statePtr);
            rightEyeRotation.WriteValueIntoState(value.rightEyeRotation, statePtr);
            fixationPoint.WriteValueIntoState(value.fixationPoint, statePtr);
            leftEyeOpenAmount.WriteValueIntoState(value.leftEyeOpenAmount, statePtr);
            rightEyeOpenAmount.WriteValueIntoState(value.rightEyeOpenAmount, statePtr);
        }
    }
#pragma warning restore 0649

    /// <summary>
    /// A small helper class to aid in initializing and registering XR devices and layout builders.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class XRSupport
    {
        /// <summary>
        /// Registers all initial templates and the generalized layout builder with the InputSystem.
        /// </summary>
        public static void Initialize()
        {
            InputSystem.RegisterLayout<BoneControl>("Bone");
            InputSystem.RegisterLayout<EyesControl>("Eyes");

            InputSystem.RegisterLayout<XRHMD>();
            InputSystem.RegisterLayout<XRController>();

            InputSystem.onFindLayoutForDevice += XRLayoutBuilder.OnFindLayoutForDevice;

            #if !DISABLE_BUILTIN_INPUT_SYSTEM_WINDOWSMR
            InputSystem.RegisterLayout<UnityEngine.XR.WindowsMR.Input.WMRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("(Windows Mixed Reality HMD)|(Microsoft HoloLens)|(^(WindowsMR Headset))")
            );
            InputSystem.RegisterLayout<UnityEngine.XR.WindowsMR.Input.WMRSpatialController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"(^(Spatial Controller))|(^(OpenVR Controller\(WindowsMR))")
            );
            InputSystem.RegisterLayout<UnityEngine.XR.WindowsMR.Input.HololensHand>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"(^(Hand -))")
            );
            #endif

            #if !DISABLE_BUILTIN_INPUT_SYSTEM_OCULUS
            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.OculusHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(Oculus Rift)|^(Oculus Quest)|^(Oculus Go)"));
            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.OculusTouchController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"(^(Oculus Touch Controller))|(^(Oculus Quest Controller))"));
            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.OculusRemote>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"Oculus Remote"));
            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.OculusTrackingReference>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"((Tracking Reference)|(^(Oculus Rift [a-zA-Z0-9]* \(Camera)))"));

            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.OculusHMDExtended>(
                name: "GearVR",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("Oculus HMD"));
            InputSystem.RegisterLayout<Unity.XR.Oculus.Input.GearVRTrackedController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(Oculus Tracked Remote)"));
            #endif

            #if !DISABLE_BUILTIN_INPUT_SYSTEM_GOOGLEVR
            InputSystem.RegisterLayout<Unity.XR.GoogleVr.DaydreamHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("Daydream HMD"));
            InputSystem.RegisterLayout<Unity.XR.GoogleVr.DaydreamController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(Daydream Controller)"));
            #endif

            #if !DISABLE_BUILTIN_INPUT_SYSTEM_OPENVR
            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(OpenVR Headset)")
            );
            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRControllerWMR>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(OpenVR Controller\\(WindowsMR)")
            );
            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveWand>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(((Vive. Controller)|(VIVE. Controller)|(Vive Controller)))")
            );
            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVROculusTouchController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(Oculus)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveTracker>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(VIVE Tracker)")
            );
            InputSystem.RegisterLayout<Unity.XR.OpenVR.HandedViveTracker>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(VIVE Tracker)")
            );
            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveLighthouse>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(HTC V2-XD/XE)")
            );
            #endif
        }
    }
}
#endif // ENABLE_VR
