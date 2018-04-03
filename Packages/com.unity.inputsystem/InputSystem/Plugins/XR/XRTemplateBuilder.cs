using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using System.Text;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [Serializable]
    class XRTemplateBuilder
    {
        static List<Func<XRDeviceDescriptor, string>> availableTemplates = new List<Func<XRDeviceDescriptor, string>>();

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

        public static void RegisterTemplateFilter(Func<XRDeviceDescriptor, string> templateChecker)
        {
            availableTemplates.Add(templateChecker);
        }

        static string SanitizeTemplateName(string templateName)
        {
            int stringLength = templateName.Length;
            var sanitizedTemplateName = new StringBuilder(stringLength);
            for (int i = 0; i < stringLength; i++)
            {
                char letter = templateName[i];
                if (char.IsUpper(letter) || char.IsLower(letter) || char.IsDigit(letter) || letter == ':')
                {
                    sanitizedTemplateName.Append(letter);
                }
            }
            return sanitizedTemplateName.ToString();
        }

        internal static string OnFindTemplateForDevice(int deviceId, ref InputDeviceDescription description, string matchedTemplate, IInputRuntime runtime)
        {
            // If the system found a matching template, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedTemplate))
            {
                return null;
            }

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

            for (int i = 0; i < availableTemplates.Count; i++)
            {
                string templateMatch = availableTemplates[i](deviceDescriptor);
                if (templateMatch != null)
                {
                    return templateMatch;
                }
            }

            // We don't want to forward the Capabilities along due to how template fields are Regex compared.
            var templateMatchingDescription = description;
            templateMatchingDescription.capabilities = null;

            var templateName = SanitizeTemplateName(string.Format("{0}::{1}::{2}", XRUtilities.kXRInterface, description.manufacturer, description.product));
            var template = new XRTemplateBuilder { descriptor = deviceDescriptor };
            InputSystem.RegisterTemplateFactory(() => template.Build(), templateName, null, templateMatchingDescription);

            return templateName;
        }

        public InputTemplate Build()
        {
            Type deviceType = null;
            switch (descriptor.deviceRole)
            {
                case DeviceRole.LeftHanded:
                case DeviceRole.RightHanded:
                {
                    deviceType = typeof(XRController);
                }
                break;
                default:
                {
                    deviceType = typeof(XRHMD);
                }
                break;
            }

            var builder = new InputTemplate.Builder
            {
                type = deviceType,
                stateFormat = new FourCC('X', 'R', 'S', '0'),
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
                        .WithTemplate("Button")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeBit)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.DiscreteStates:
                    {
                        builder.AddControl(feature.name)
                        .WithTemplate("Integer")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeInt)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis1D:
                    {
                        builder.AddControl(feature.name)
                        .WithTemplate("Analog")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeFloat)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis2D:
                    {
                        builder.AddControl(feature.name)
                        .WithTemplate("Vector2")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeVector2)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Axis3D:
                    {
                        builder.AddControl(feature.name)
                        .WithTemplate("Vector3")
                        .WithOffset(currentOffset)
                        .WithFormat(InputStateBlock.kTypeVector3)
                        .WithUsages(currentUsages);
                        break;
                    }
                    case FeatureType.Rotation:
                    {
                        builder.AddControl(feature.name)
                        .WithTemplate("Quaternion")
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
