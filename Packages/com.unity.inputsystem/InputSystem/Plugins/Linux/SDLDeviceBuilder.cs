using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using System.Text;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.Linux
{
    [Serializable]
    class SDLLayoutBuilder
    {
        [SerializeField]
        string parentLayout;
        [SerializeField]
        string interfaceName;
        [SerializeField]
        SDLDeviceDescriptor descriptor;

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
            if (description.interfaceName != DeviceInterfaces.kXRInterfaceCurrent)
            {
                return null;
            }

            // If the description doesn't come with a XR SDK descriptor, we're not
            // interested either.
            if (string.IsNullOrEmpty(description.capabilities))
            {
                return null;
            }

            // Try to parse the SDL descriptor.
            SDLDeviceDescriptor deviceDescriptor;
            try
            {
                deviceDescriptor = SDLDeviceDescriptor.FromJson(description.capabilities);
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
                matchedLayout = "Joystick";
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

            var layout = new SDLLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout, interfaceName = description.interfaceName };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(), layoutName, matchedLayout);
            
            return layoutName;
        }

        internal InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('L', 'J', 'O', 'Y'),
                extendsLayout = parentLayout
            };

            foreach (var feature in descriptor.inputFeatures)
            {
                switch (feature.featureType)
                {
                    case JoystickFeatureType.Axis:
                        {

                        }
                        break;
                    case JoystickFeatureType.Ball:
                        {

                        }
                        break;
                    case JoystickFeatureType.Button:
                        {

                        }
                        break;
                    case JoystickFeatureType.Hat:
                        {

                        }
                        break;
                    default:
                        {
                            throw new NotImplementedException(String.Format("SDLLayoutBuilder.Build: Trying to build an SDL device with an unknown feature named {0} of type {1}.", feature.name, feature.featureType));
                        }

                }
            }

            return builder.Build();
        }
    }
}
