using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Text;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.XR;

namespace UnityEngine.InputSystem.XR
{
    [Serializable]
    class XRLayoutBuilder
    {
        [SerializeField]
        string parentLayout;
        [SerializeField]
        string interfaceName;
        [SerializeField]
        XRDeviceDescriptor descriptor;

        static uint GetSizeOfFeature(XRFeatureDescriptor featureDescriptor)
        {
            switch (featureDescriptor.featureType)
            {
                case FeatureType.Binary:
                    return sizeof(byte);
                case FeatureType.DiscreteStates:
                    return sizeof(int);
                case FeatureType.Axis1D:
                    return sizeof(float);
                case FeatureType.Axis2D:
                    return sizeof(float) * 2;
                case FeatureType.Axis3D:
                    return sizeof(float) * 3;
                case FeatureType.Rotation:
                    return sizeof(float) * 4;
                case FeatureType.Custom:
                    return featureDescriptor.customSize;
            }
            return 0;
        }

        static string SanitizeName(string originalName)
        {
            int stringLength = originalName.Length;
            var sanitizedName = new StringBuilder(stringLength);
            for (int i = 0; i < stringLength; i++)
            {
                char letter = originalName[i];
                if (char.IsUpper(letter) || char.IsLower(letter) || char.IsDigit(letter))
                {
                    sanitizedName.Append(letter);
                }
            }
            return sanitizedName.ToString();
        }

        internal static string OnFindLayoutForDevice(int deviceId, ref InputDeviceDescription description, string matchedLayout, IInputRuntime runtime)
        {
            // If the device isn't a XRInput, we're not interested.
            if (description.interfaceName != XRUtilities.InterfaceCurrent && description.interfaceName != XRUtilities.InterfaceV1)
            {
                return null;
            }

            // If the description doesn't come with a XR SDK descriptor, we're not
            // interested either.
            if (string.IsNullOrEmpty(description.capabilities))
            {
                return null;
            }

            // Try to parse the XR descriptor.
            XRDeviceDescriptor deviceDescriptor;
            try
            {
                deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);
            }
            catch (Exception)
            {
                return null;
            }

            if (deviceDescriptor == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(matchedLayout))
            {
#if UNITY_2019_3_OR_NEWER
                const InputDeviceCharacteristics controllerCharacteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller);
                if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
                    matchedLayout = "XRHMD";
                else if ((deviceDescriptor.characteristics & controllerCharacteristics) == controllerCharacteristics)
                    matchedLayout = "XRController";
#else
                if (deviceDescriptor.deviceRole == InputDeviceRole.LeftHanded || deviceDescriptor.deviceRole == InputDeviceRole.RightHanded)
                    matchedLayout = "XRController";
                else if (deviceDescriptor.deviceRole == InputDeviceRole.Generic)
                    matchedLayout = "XRHMD";
#endif
            }

            string layoutName = null;
            if (string.IsNullOrEmpty(description.manufacturer))
            {
                layoutName = string.Format("{0}::{1}", SanitizeName(description.interfaceName),
                    SanitizeName(description.product));
            }
            else
            {
                layoutName = string.Format("{0}::{1}::{2}", SanitizeName(description.interfaceName), SanitizeName(description.manufacturer), SanitizeName(description.product));
            }

            var layout = new XRLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout, interfaceName = description.interfaceName };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(), layoutName, matchedLayout);

            return layoutName;
        }

        string ConvertPotentialAliasToName(InputControlLayout layout, string nameOrAlias)
        {
            InternedString internedNameOrAlias = new InternedString(nameOrAlias);
            ReadOnlyArray<InputControlLayout.ControlItem> controls = layout.controls;
            for (int i = 0; i < controls.Count; i++)
            {
                InputControlLayout.ControlItem controlItem = controls[i];

                if (controlItem.name == internedNameOrAlias)
                    return nameOrAlias;

                ReadOnlyArray<InternedString> aliases = controlItem.aliases;
                for (int j = 0; j < aliases.Count; j++)
                {
                    if (aliases[j] == nameOrAlias)
                        return controlItem.name.ToString();
                }
            }
            return nameOrAlias;
        }

        internal InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('X', 'R', 'S', '0'),
                extendsLayout = parentLayout,
                updateBeforeRender = true
            };

            var inherittedLayout = InputSystem.LoadLayout(parentLayout);

            var currentUsages = new List<string>();

            uint currentOffset = 0;
            foreach (var feature in descriptor.inputFeatures)
            {
                currentUsages.Clear();

                if (feature.usageHints != null)
                {
                    foreach (var usageHint in feature.usageHints)
                    {
                        if (!string.IsNullOrEmpty(usageHint.content))
                            currentUsages.Add(usageHint.content);
                    }
                }

                string featureName = feature.name;
                featureName = SanitizeName(featureName);
                if (inherittedLayout != null)
                    featureName = ConvertPotentialAliasToName(inherittedLayout, featureName);

                featureName = featureName.ToLower();

                uint nextOffset = GetSizeOfFeature(feature);

                if (interfaceName == XRUtilities.InterfaceV1)
                {
#if UNITY_ANDROID
                    if (nextOffset < 4)
                        nextOffset = 4;
#endif
                }
                else
                {
                    if (nextOffset >= 4 && (currentOffset % 4 != 0))
                        currentOffset += (4 - (currentOffset % 4));
                }


                switch (feature.featureType)
                {
                    case FeatureType.Binary:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Button")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatBit)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.DiscreteStates:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Integer")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatInt)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis1D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatFloat)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis2D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Vector2")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatVector2)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis3D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Vector3")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatVector3)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Rotation:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Quaternion")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatQuaternion)
                            .WithUsages(currentUsages);
                        break;
                    }
                }
                currentOffset += nextOffset;
            }

            return builder.Build();
        }
    }
}
