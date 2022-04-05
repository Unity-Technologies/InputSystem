// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) || PACKAGE_DOCS_GENERATION
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;

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
    /// <summary>
    /// The type of data a <see cref="XRFeatureDescriptor"/> exposes.
    /// </summary>
    public enum FeatureType
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

    /// <summary>
    /// Contextual strings that identify the contextual, cross-platform use that a feature represents.  <see cref="UnityEngine.XR.CommonUsages"/> for a list of unity's built-in shared usages.
    /// </summary>
#pragma warning disable 0649
    [Serializable]
    public struct UsageHint
    {
        public string content;
    }

    //Sync to XRInputFeatureDefinition in XRInputDeviceDefinition.h
    /// <summary>
    /// Describes an individual input on a device, such as a trackpad, or button, or trigger.
    /// </summary>
    [Serializable]
    public struct XRFeatureDescriptor
    {
        /// <summary>
        /// The name of the feature.
        /// </summary>
        public string name;
        /// <summary>
        /// The uses that this feature should represent, such as trigger, or grip, or touchpad.
        /// </summary>
        public List<UsageHint> usageHints;
        /// <summary>
        /// The type of data this feature exposes.
        /// </summary>
        public FeatureType featureType;
        /// <summary>
        /// The overall size of the feature.  This is only filled in when the <see cref="featureType"/> is <see cref="FeatureType.Custom"/>.
        /// </summary>
        public uint customSize;
    }

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    /// <summary>
    /// Describes an input device: what it can do and how it should be used.  These are reported during device connection, and help identify devices and map input data to the right controls.
    /// </summary>
    [Serializable]
    public class XRDeviceDescriptor
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        public string deviceName;
        /// <summary>
        /// The manufacturer of the device.
        /// </summary>
        public string manufacturer;
        /// <summary>
        /// The serial number of the device.  An empty string if no serial number is available.
        /// </summary>
        public string serialNumber;
        /// <summary>
        /// The capabilities of the device, used to help filter and identify devices that server a certain purpose (e.g. controller, or headset, or hardware tracker).
        /// </summary>
        public InputDeviceCharacteristics characteristics;
        /// <summary>
        /// The underlying deviceId, this can be used with <see cref="UnityEngine.XR.InputDevices"/> to create a device.
        /// </summary>
        public int deviceId;
        /// <summary>
        /// A list of all input features.  <seealso cref="XRFeatureDescriptor"/>
        /// </summary>
        public List<XRFeatureDescriptor> inputFeatures;

        /// <summary>
        /// Converts this structure to a JSON string.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Converts a json string to a new <see cref="XRDeviceDescriptor"/>.
        /// </summary>
        /// <param name="json">The JSON string containing <see cref="XRDeviceDescriptor"/> data.</param>
        /// <returns>A new <see cref="XRDeviceDescriptor"/></returns>
        public static XRDeviceDescriptor FromJson(string json)
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

    public class BoneControl : InputControl<Bone>
    {
        [InputControl(offset = 0, displayName = "parentBoneIndex")]
        public IntegerControl parentBoneIndex { get; private set; }
        [InputControl(offset = 4, displayName = "Position")]
        public Vector3Control position { get; private set; }
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

    public class EyesControl : InputControl<Eyes>
    {
        [InputControl(offset = 0, displayName = "LeftEyePosition")]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl(offset = 12, displayName = "LeftEyeRotation")]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl(offset = 28, displayName = "RightEyePosition")]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl(offset = 40, displayName = "RightEyeRotation")]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl(offset = 56, displayName = "FixationPoint")]
        public Vector3Control fixationPoint { get; private set; }
        [InputControl(offset = 68, displayName = "LeftEyeOpenAmount")]
        public AxisControl leftEyeOpenAmount { get; private set; }
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
#if !UNITY_FORCE_INPUTSYSTEM_XR_OFF
            InputSystem.RegisterLayout<PoseControl>("Pose");
            InputSystem.RegisterLayout<BoneControl>("Bone");
            InputSystem.RegisterLayout<EyesControl>("Eyes");

            InputSystem.RegisterLayout<XRHMD>();
            InputSystem.RegisterLayout<XRController>();

            InputSystem.onFindLayoutForDevice += XRLayoutBuilder.OnFindLayoutForDevice;

            // Built-in layouts replaced by the com.unity.xr.windowsmr package.
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

            // Built-in layouts replaced by the com.unity.xr.oculus package.
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

            // Built-in layouts replaced by the com.unity.xr.googlevr package.
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

            // Built-in layouts replaced by the com.unity.xr.openvr package.
#if !DISABLE_BUILTIN_INPUT_SYSTEM_OPENVR
            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("^(OpenVR Headset)|^(Vive Pro)")
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
#endif
        }
    }
}
#endif
