using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using System.Text;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [Serializable]
    class XRLayoutBuilder
    {
        public string parentLayout;
        public XRDeviceDescriptor descriptor;

        static uint GetSizeOfFeature(XRFeatureDescriptor featureDescriptor)
        {
            switch (featureDescriptor.featureType)
            {
                case FeatureType.Binary:
#if UNITY_ANDROID
                    return 4;
#else
                    return 1;
#endif
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

        static string SanitizeLayoutName(string layoutName)
        {
            int stringLength = layoutName.Length;
            var sanitizedLayoutName = new StringBuilder(stringLength);
            for (int i = 0; i < stringLength; i++)
            {
                char letter = layoutName[i];
                if (char.IsUpper(letter) || char.IsLower(letter) || char.IsDigit(letter) || letter == ':')
                {
                    sanitizedLayoutName.Append(letter);
                }
            }
            return sanitizedLayoutName.ToString();
        }

        internal static string OnFindControlLayoutForDevice(int deviceId, ref InputDeviceDescription description, string matchedLayout, IInputRuntime runtime)
        {
            // If the device isn't a XRInput, we're not interested.
            if (description.interfaceName != XRUtilities.kXRInterface)
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
                else
                    return null;
            }

            // We don't want to forward the Capabilities along due to how template fields are Regex compared.
            var layoutMatchingDescription = description;
            layoutMatchingDescription.capabilities = null;

            var layoutName = SanitizeLayoutName(string.Format("{0}::{1}::{2}", XRUtilities.kXRInterface, description.manufacturer, description.product));
            var layout = new XRLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout };
            InputSystem.RegisterControlLayoutBuilder(() => layout.Build(), layoutName, matchedLayout, layoutMatchingDescription);

            return layoutName;
        }

        public InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('X', 'R', 'S', '0'),
                extendsLayout = parentLayout,
                updateBeforeRender = true
            };

            var currentUsages = new List<string>();

            uint currentOffset = 0;
            foreach (var feature in descriptor.inputFeatures)
            {
                currentUsages.Clear();

                if (feature.usageHints != null)
                {
                    foreach (var usageHint in feature.usageHints)
                    {
                        if (string.IsNullOrEmpty(usageHint.content))
                            currentUsages.Add(usageHint.content);
                    }
                }

                uint nextOffset = GetSizeOfFeature(feature);
                switch (feature.featureType)
                {
                    case FeatureType.Binary:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Button")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.DiscreteStates:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Integer")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeInt)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis1D:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Analog")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeFloat)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis2D:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Vector2")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeVector2)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis3D:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Vector3")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeVector3)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Rotation:
                    {
                        builder.AddControl(feature.name)
                        .WithLayout("Quaternion")
                        .WithOffset(currentOffset)
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
