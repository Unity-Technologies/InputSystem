// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Text;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.XR;

namespace UnityEngine.InputSystem.XR
{
    internal class XRLayoutBuilder
    {
        private string parentLayout;
        private string interfaceName;
        private XRDeviceDescriptor descriptor;

        private static uint GetSizeOfFeature(XRFeatureDescriptor featureDescriptor)
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
                case FeatureType.Hand:
                    return sizeof(uint) * 26;
                case FeatureType.Bone:
                    return sizeof(uint) + (sizeof(float) * 3) + (sizeof(float) * 4);
                case FeatureType.Eyes:
                    return (sizeof(float) * 3) * 3 + ((sizeof(float) * 4) * 2) + (sizeof(float) * 2);
                case FeatureType.Custom:
                    return featureDescriptor.customSize;
            }
            return 0;
        }

        private static string SanitizeString(string original, bool allowPaths = false)
        {
            var stringLength = original.Length;
            var sanitizedName = new StringBuilder(stringLength);
            for (var i = 0; i < stringLength; i++)
            {
                var letter = original[i];
                if (char.IsUpper(letter) || char.IsLower(letter) || char.IsDigit(letter) || letter == '_' || (allowPaths && (letter == '/')))
                {
                    sanitizedName.Append(letter);
                }
            }
            return sanitizedName.ToString();
        }

        internal static string OnFindLayoutForDevice(ref InputDeviceDescription description, string matchedLayout,
            InputDeviceExecuteCommandDelegate executeCommandDelegate)
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
                const InputDeviceCharacteristics controllerCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;
                if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
                    matchedLayout = "XRHMD";
                else if ((deviceDescriptor.characteristics & controllerCharacteristics) == controllerCharacteristics)
                    matchedLayout = "XRController";
            }

            string layoutName;
            if (string.IsNullOrEmpty(description.manufacturer))
            {
                layoutName = $"{SanitizeString(description.interfaceName)}::{SanitizeString(description.product)}";
            }
            else
            {
                layoutName =
                    $"{SanitizeString(description.interfaceName)}::{SanitizeString(description.manufacturer)}::{SanitizeString(description.product)}";
            }

            var layout = new XRLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout, interfaceName = description.interfaceName };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(), layoutName, matchedLayout);

            return layoutName;
        }

        private static string ConvertPotentialAliasToName(InputControlLayout layout, string nameOrAlias)
        {
            var internedNameOrAlias = new InternedString(nameOrAlias);
            var controls = layout.controls;
            for (var i = 0; i < controls.Count; i++)
            {
                var controlItem = controls[i];

                if (controlItem.name == internedNameOrAlias)
                    return nameOrAlias;

                var aliases = controlItem.aliases;
                for (var j = 0; j < aliases.Count; j++)
                {
                    if (aliases[j] == nameOrAlias)
                        return controlItem.name.ToString();
                }
            }
            return nameOrAlias;
        }

        private bool IsSubControl(string name)
        {
            return name.Contains('/');
        }

        private string GetParentControlName(string name)
        {
            int idx = name.IndexOf('/');
            return name.Substring(0, idx);
        }

        static readonly string[] poseSubControlNames =
        {
            "/isTracked",
            "/trackingState",
            "/position",
            "/rotation",
            "/velocity",
            "/angularVelocity"
        };

        static readonly FeatureType[] poseSubControlTypes =
        {
            FeatureType.Binary,
            FeatureType.DiscreteStates,
            FeatureType.Axis3D,
            FeatureType.Rotation,
            FeatureType.Axis3D,
            FeatureType.Axis3D
        };

        // A PoseControl consists of 6 subcontrols with specific names and types
        private bool IsPoseControl(List<XRFeatureDescriptor> features, int startIndex)
        {
            for (var i = 0; i < 6; i++)
            {
                if (!features[startIndex + i].name.EndsWith(poseSubControlNames[i]) ||
                    features[startIndex + i].featureType != poseSubControlTypes[i])
                    return false;
            }
            return true;
        }

        private InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('X', 'R', 'S', '0'),
                extendsLayout = parentLayout,
                updateBeforeRender = true
            };

            var inheritedLayout = !string.IsNullOrEmpty(parentLayout)
                ? InputSystem.LoadLayout(parentLayout)
                : null;

            var parentControls = new List<string>();
            var currentUsages = new List<string>();

            uint currentOffset = 0;
            for (var i = 0; i < descriptor.inputFeatures.Count; i++)
            {
                var feature = descriptor.inputFeatures[i];
                currentUsages.Clear();

                if (feature.usageHints != null)
                {
                    foreach (var usageHint in feature.usageHints)
                    {
                        if (!string.IsNullOrEmpty(usageHint.content))
                            currentUsages.Add(usageHint.content);
                    }
                }

                var featureName = feature.name;
                featureName = SanitizeString(featureName, true);
                if (inheritedLayout != null)
                    featureName = ConvertPotentialAliasToName(inheritedLayout, featureName);

                featureName = featureName.ToLower();

                if (IsSubControl(featureName))
                {
                    string parentControl = GetParentControlName(featureName);
                    if (!parentControls.Contains(parentControl))
                    {
                        if (IsPoseControl(descriptor.inputFeatures, i))
                        {
                            builder.AddControl(parentControl)
                                .WithLayout("Pose")
                                .WithByteOffset(0);
                            parentControls.Add(parentControl);
                        }
                    }
                }

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
                            .WithRange(-1, 1)
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatFloat)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis2D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Stick")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.FormatVector2)
                            .WithUsages(currentUsages);

                        builder.AddControl(featureName + "/x")
                            .WithLayout("Analog")
                            .WithRange(-1, 1);
                        builder.AddControl(featureName + "/y")
                            .WithLayout("Analog")
                            .WithRange(-1, 1);
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
                    case FeatureType.Hand:
                    {
                        break;
                    }
                    case FeatureType.Bone:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Bone")
                            .WithByteOffset(currentOffset)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Eyes:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Eyes")
                            .WithByteOffset(currentOffset)
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
#endif
