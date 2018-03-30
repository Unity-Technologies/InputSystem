using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.XR.Haptics;
using System.Text;

namespace UnityEngine.Experimental.Input.XR
{
    [Serializable]
    class XRTemplateBuilder
    {
        static List<Func<XRDeviceDescriptor, string>> availableTemplates = new List<Func<XRDeviceDescriptor, string>>();

        public XRDeviceDescriptor descriptor;

        static uint GetOffsetForFeatureType(XRFeatureDescriptor featureDescriptor)
        {
            switch (featureDescriptor.featureType)
            {
                case EFeatureType.Binary:
#if UNITY_ANDROID
                        return 4;
#else
                    return 1;
#endif
                case EFeatureType.DiscreteStates:
                    return sizeof(int);
                case EFeatureType.Axis1D:
                    return sizeof(float);
                case EFeatureType.Axis2D:
                    return sizeof(float) * 2;
                case EFeatureType.Axis3D:
                    return sizeof(float) * 3;
                case EFeatureType.Rotation:
                    return sizeof(float) * 4;
                case EFeatureType.Custom:
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
            StringBuilder sanitizedTemplateName = new StringBuilder(stringLength);
            for (int i = 0; i < stringLength; i++)
            {
                char letter = templateName[i];
                if(Char.IsUpper(letter) || Char.IsLower(letter) || Char.IsDigit(letter) || letter == ':')
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

            string templateName = SanitizeTemplateName(string.Format("{0}::{1}::{2}", XRUtilities.kXRInterface, description.manufacturer, description.product));
            XRTemplateBuilder template = new XRTemplateBuilder { descriptor = deviceDescriptor };
            InputSystem.RegisterTemplateFactory(() => template.Build(), templateName, null, description);

            return templateName;
        }

        public InputTemplate Build()
        {
            Type deviceType = null;
            switch(descriptor.deviceRole)
            {
                case EDeviceRole.LeftHanded:
                case EDeviceRole.RightHanded:
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

            List<string> currentUsages = new List<string>();

            uint currentOffset = 0;
            foreach (var feature in descriptor.inputFeatures)
            {
                currentUsages.Clear();
                foreach (var usageHint in feature.usageHints)
                {
                    if (usageHint.content != null && usageHint.content.Length > 0)
                        currentUsages.Add(usageHint.content);
                }

                uint nextOffset = GetOffsetForFeatureType(feature);
                switch (feature.featureType)
                {
                    case EFeatureType.Binary:
                        {
                            builder.AddControl(feature.name)
                                .WithTemplate("Button")
                                .WithOffset(currentOffset)
                                .WithFormat(InputStateBlock.kTypeBit)
                                .WithUsages(currentUsages);
                            break;
                        }
                    case EFeatureType.DiscreteStates:
                        {
                            builder.AddControl(feature.name)
                                .WithTemplate("Integer")
                                .WithOffset(currentOffset)
                                .WithFormat(InputStateBlock.kTypeInt)
                                .WithUsages(currentUsages);
                            break;
                        }
                    case EFeatureType.Axis1D:
                        {
                            builder.AddControl(feature.name)
                                .WithTemplate("Analog")
                                .WithOffset(currentOffset)
                                .WithFormat(InputStateBlock.kTypeFloat)
                                .WithUsages(currentUsages);
                            break;
                        }
                    case EFeatureType.Axis2D:
                        {
                            builder.AddControl(feature.name)
                                .WithTemplate("Vector2")
                                .WithOffset(currentOffset)
                                .WithFormat(InputStateBlock.kTypeVector2)
                                .WithUsages(currentUsages);
                            break;
                        }
                    case EFeatureType.Axis3D:
                        {
                            builder.AddControl(feature.name)
                                .WithTemplate("Vector3")
                                .WithOffset(currentOffset)
                                .WithFormat(InputStateBlock.kTypeVector3)
                                .WithUsages(currentUsages);
                            break;
                        }
                    case EFeatureType.Rotation:
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
