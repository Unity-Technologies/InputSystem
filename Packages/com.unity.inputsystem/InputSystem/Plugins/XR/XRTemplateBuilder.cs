using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using System.Text;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
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
            if (description.interfaceName != XRUtilities.kXRInterfaceCurrent && description.interfaceName != XRUtilities.kXRInterfaceV1)
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
                if (deviceDescriptor.deviceRole == DeviceRole.LeftHanded || deviceDescriptor.deviceRole == DeviceRole.RightHanded)
                    matchedLayout = "XRController";
                else if (deviceDescriptor.deviceRole == DeviceRole.Generic)
                    matchedLayout = "XRHMD";
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

            // If we are already using a generated layout, we just want to use what's there currently, instead of creating a brand new layout.
            if (layoutName == matchedLayout)
                return layoutName;

            var layout = new XRLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout, interfaceName = description.interfaceName };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(), layoutName, matchedLayout, InputDeviceMatcher.FromDeviceDescription(description));

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

            var inherittedLayout = InputSystem.TryLoadLayout(parentLayout);

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

                if (interfaceName == XRUtilities.kXRInterfaceV1)
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
                            .WithFormat(InputStateBlock.kTypeBit)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.DiscreteStates:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Integer")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.kTypeInt)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis1D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.kTypeFloat)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis2D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Vector2")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.kTypeVector2)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis3D:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Vector3")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.kTypeVector3)
                            .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Rotation:
                    {
                        builder.AddControl(featureName)
                            .WithLayout("Quaternion")
                            .WithByteOffset(currentOffset)
                            .WithFormat(InputStateBlock.kTypeQuaternion)
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
