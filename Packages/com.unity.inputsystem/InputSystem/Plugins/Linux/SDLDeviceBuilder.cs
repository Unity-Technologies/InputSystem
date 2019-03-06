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
            if (description.interfaceName != SDLSupport.kXRInterfaceCurrent)
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
                //matchedLayout = "Joystick";
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

            var layout = new SDLLayoutBuilder { descriptor = deviceDescriptor, parentLayout = matchedLayout };
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

            var xElement = descriptor.controls.Find(feature => feature.featureType == JoystickFeatureType.Axis && feature.usageHint == 1); // ABS_X
            var yElement = descriptor.controls.Find(feature => feature.featureType == JoystickFeatureType.Axis && feature.usageHint == 2); // ABS_Y

            if (xElement.usageHint == 1 && yElement.usageHint == 2)
            {
                int byteOffset;
                if (xElement.offset <= yElement.offset)
                {
                    byteOffset = xElement.offset;
                }
                else
                {
                    byteOffset = yElement.offset;
                }

                var stickName = "Stick";
                var control = builder.AddControl(stickName)
                    .WithLayout("Stick")
                    .WithByteOffset((uint)byteOffset)
                    .WithSizeInBits((uint)xElement.size * 8)
                    .WithUsages(new InternedString[] { CommonUsages.Primary2DMotion });

                builder.AddControl(stickName + "/x")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Axis")
                    .WithByteOffset(0)
                    .WithSizeInBits((uint)xElement.size * 8)
                    .WithParameters("scale,scaleFactor=65538.01467");

                builder.AddControl(stickName + "/y")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Axis")
                    .WithByteOffset((uint)4)
                    .WithSizeInBits((uint)xElement.size * 8)
                    .WithParameters("scale,scaleFactor=65538.01467,invert");

                //Need to handle Up/Down/Left/Right
                builder.AddControl(stickName + "/up")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Button")
                    .WithParameters("clamp,clampMin=-1,clampMax=0,scale,scaleFactor=65538.01467,invert")
                    .WithByteOffset((uint)4)
                    .WithSizeInBits((uint)yElement.size * 8);

                builder.AddControl(stickName + "/down")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Button")
                    .WithParameters("clamp,clampMin=0,clampMax=1,scale,scaleFactor=65538.01467")
                    .WithByteOffset((uint)4)
                    .WithSizeInBits((uint)yElement.size * 8);

                builder.AddControl(stickName + "/down-inverted")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Button")
                    .WithParameters("clamp,clampMin=0,clampMax=1,scale,scaleFactor=65538.01467,invert")
                    .WithByteOffset((uint)4)
                    .WithSizeInBits((uint)yElement.size * 8);

                builder.AddControl(stickName + "/left")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Button")
                    .WithParameters("clamp,clampMin=-1,clampMax=0,scale,scaleFactor=65538.01467,invert")
                    .WithByteOffset((uint)0)
                    .WithSizeInBits((uint)xElement.size * 8);

                builder.AddControl(stickName + "/right")
                    .WithFormat(InputStateBlock.kTypeInt)
                    .WithLayout("Button")
                    .WithParameters("clamp,clampMin=0,clampMax=1,scale,scaleFactor=65538.01467")
                    .WithByteOffset((uint)0)
                    .WithSizeInBits((uint)xElement.size * 8);
            }

            foreach (var feature in descriptor.controls)
            {
                switch (feature.featureType)
                {
                    case JoystickFeatureType.Axis:
                        {
                            SDLAxisUsage usage = (SDLAxisUsage)feature.usageHint;
                            string featureName = SDLSupport.GetAxisNameFromUsage(usage);
                            builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset((uint)feature.offset)
                            .WithFormat(InputStateBlock.kTypeInt)
                            .WithParameters("clamp,clampMin=-1,clampMax=1,scale,scaleFactor=65538.01467");
                        }
                        break;
                    case JoystickFeatureType.Ball:
                        {
                            //TODO
                        }
                        break;
                    case JoystickFeatureType.Button:
                        {
                            SDLButtonUsage usage = (SDLButtonUsage)feature.usageHint;
                            string featureName = SDLSupport.GetButtonNameFromUsage(usage);
                            if (featureName != null)
                            {
                                builder.AddControl(featureName)
                                .WithLayout("Button")
                                .WithByteOffset((uint)feature.offset)
                                .WithBitOffset((uint)feature.bit)
                                .WithFormat(InputStateBlock.kTypeBit);
                            }
                        }
                        break;
                    case JoystickFeatureType.Hat:
                        {
                            //TODO
                        }
                        break;
                    default:
                        {
                            throw new NotImplementedException(String.Format("SDLLayoutBuilder.Build: Trying to build an SDL device with an unknown feature of type {0}.", feature.featureType));
                        }

                }
            }

            return builder.Build();
        }
    }
}
