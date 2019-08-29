using System;
using System.Collections.Generic;
#if UNITY_INPUT_SYSTEM_ENABLE_XR
using UnityEngine.XR;
#endif
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputSystem.XR
{
    public static class XRUtilities
    {
        /// <summary>
        /// A simple Regex pattern that allows InputDeviceMatchers to match to any version of the XRInput interface.
        /// </summary>
        public const string kXRInterfaceMatchAnyVersion = "^(XRInput)";

        /// <summary>
        /// The initial, now deprecated interface for XRInput.  This version handles button packing for Android differently from current.
        /// </summary>
        public const string kXRInterfaceV1 = "XRInput";

        /// <summary>
        /// The current interface code sent with devices to identify as XRInput devices.
        /// </summary>
        public const string kXRInterfaceCurrent = "XRInputV1";
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

#if !UNITY_2019_3_OR_NEWER
    // Sync to UnityXRInputDeviceRole in IUnityXRInput.h
    /// <summary>
    /// The generalized role that the device plays.  This can help in grouping devices by type (HMD, vs. hardware tracker vs. handed controller).
    /// </summary>
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
#endif

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class XRDeviceDescriptor
    {
        public string deviceName;
        public string manufacturer;
        public string serialNumber;
#if UNITY_INPUT_SYSTEM_ENABLE_XR
#if UNITY_2019_3_OR_NEWER
        public InputDeviceCharacteristics characteristics;
#else //UNITY_2019_3_OR_NEWER
        public InputDeviceRole deviceRole;
#endif //UNITY_2019_3_OR_NEWER
#endif //UNITY_INPUT_SYSTEM_ENABLE_XR
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

    public struct Bone
    {
        // Todo, this should reference another bone in some way
        public uint parentBoneIndex;
        public Vector3 position;
        public Quaternion rotation;
    }

    public struct Eyes
    {
        public Vector3 leftEyePosition;
        public Quaternion leftEyeRotation;
        public Vector3 rightEyePosition;
        public Quaternion rightEyeRotation;
        public Vector3 fixationPoint;
        public float leftEyeOpenAmount;
        public float rightEyeOpenAmount;
    }

    public class BoneControl : InputControl<Bone>
    {
        [InputControl(offset = 0, displayName = "parentBoneIndex")]
        public IntegerControl parentBoneIndex { get; private set; }
        [InputControl(offset = 4, displayName = "Position")]
        public Vector3Control position { get; private set; }
        [InputControl(offset = 16, displayName = "Rotation")]
        public QuaternionControl rotation { get; private set; }

        public BoneControl()
        {}

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

        public EyesControl()
        {}

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

            InputSystem.RegisterLayout<KnucklesController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("Valve")
                    .WithProduct(@"^(OpenVR Controller\(Knuckles)"));

            InputSystem.onFindLayoutForDevice += XRLayoutBuilder.OnFindLayoutForDevice;
        }
    }
}
