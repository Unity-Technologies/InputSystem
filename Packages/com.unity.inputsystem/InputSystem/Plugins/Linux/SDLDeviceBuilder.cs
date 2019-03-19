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

        internal bool IsAxisX(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Axis
                && feature.usageHint == (int)SDLAxisUsage.X;
        }

        internal bool IsAxisY(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Axis
                && feature.usageHint == (int)SDLAxisUsage.Y;
        }

        internal void BuildStickFeature(ref InputControlLayout.Builder builder, SDLFeatureDescriptor xFeature, SDLFeatureDescriptor yFeature)
        {
            int byteOffset;
            if (xFeature.offset <= yFeature.offset)
            {
                byteOffset = xFeature.offset;
            }
            else
            {
                byteOffset = yFeature.offset;
            }

            var stickName = "Stick";
            var control = builder.AddControl(stickName)
                .WithLayout("Stick")
                .WithByteOffset((uint)byteOffset)
                .WithSizeInBits((uint)xFeature.size * 8)
                .WithUsages(new InternedString[] { CommonUsages.Primary2DMotion });

            builder.AddControl(stickName + "/x")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Axis")
                .WithByteOffset(0)
                .WithSizeInBits((uint)xFeature.size * 8)
                .WithParameters("clamp,clampMin=-1,clampMax=1,scale,scaleFactor=65538");

            builder.AddControl(stickName + "/y")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Axis")
                .WithByteOffset((uint)4)
                .WithSizeInBits((uint)xFeature.size * 8)
                .WithParameters("clamp,clampMin=-1,clampMax=1,scale,scaleFactor=65538,invert");

            //Need to handle Up/Down/Left/Right
            builder.AddControl(stickName + "/up")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("clamp,clampMin=-1,clampMax=0,scale,scaleFactor=65538,invert")
                .WithByteOffset((uint)4)
                .WithSizeInBits((uint)yFeature.size * 8);

            builder.AddControl(stickName + "/down")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("clamp,clampMin=0,clampMax=1,scale,scaleFactor=65538,invert=false")
                .WithByteOffset((uint)4)
                .WithSizeInBits((uint)yFeature.size * 8);

            builder.AddControl(stickName + "/left")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("clamp,clampMin=-1,clampMax=0,scale,scaleFactor=65538,invert")
                .WithByteOffset((uint)0)
                .WithSizeInBits((uint)xFeature.size * 8);

            builder.AddControl(stickName + "/right")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("clamp,clampMin=0,clampMax=1,scale,scaleFactor=65538")
                .WithByteOffset((uint)0)
                .WithSizeInBits((uint)xFeature.size * 8);
        }

        internal bool IsHatX(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Hat
                && (feature.usageHint == (int)SDLAxisUsage.Hat0X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat1X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat2X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat3X);
        }

        internal bool IsHatY(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Hat
                && (feature.usageHint == (int)SDLAxisUsage.Hat0Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat1Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat2Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat3Y);
        }

        internal int HatNumber(SDLFeatureDescriptor feature)
        {
            Debug.Assert(feature.featureType == JoystickFeatureType.Hat);
            return 1 + ((int)feature.usageHint - (int)SDLAxisUsage.Hat0X) / 2;
        }

        internal void BuildHatFeature(ref InputControlLayout.Builder builder, SDLFeatureDescriptor xFeature, SDLFeatureDescriptor yFeature)
        {
            string xFeatureName = SDLSupport.GetAxisNameFromUsage((SDLAxisUsage)xFeature.usageHint);
            string yFeatureName = SDLSupport.GetAxisNameFromUsage((SDLAxisUsage)yFeature.usageHint);
            var hat = HatNumber(xFeature);
            var hatName = (hat > 1) ? $"Hat{hat}" : "Hat";

            var control = builder.AddControl(hatName)
                .WithLayout("Dpad")
                .WithByteOffset((uint)xFeature.offset)
                .WithSizeInBits((uint)xFeature.size * 8)
                .WithUsages(new InternedString[] { CommonUsages.Hatswitch });

            builder.AddControl(hatName + "/up")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=0,invert")
                .WithByteOffset(4)
                .WithSizeInBits((uint)yFeature.size * 8);

            builder.AddControl(hatName + "/down")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=0,clampMax=1")
                .WithByteOffset(4)
                .WithSizeInBits((uint)yFeature.size * 8);

            builder.AddControl(hatName + "/left")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=0,invert")
                .WithByteOffset(0)
                .WithSizeInBits((uint)xFeature.size * 8);

            builder.AddControl(hatName + "/right")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Button")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=0,clampMax=1")
                .WithByteOffset(0)
                .WithSizeInBits((uint)xFeature.size * 8);

            builder.AddControl(hatName + "/x")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Analog")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=0,clampMax=1")
                .WithByteOffset(0)
                .WithSizeInBits((uint)xFeature.size * 8);

            builder.AddControl(hatName + "/y")
                .WithFormat(InputStateBlock.kTypeInt)
                .WithLayout("Analog")
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=1,invert")
                .WithByteOffset(4)
                .WithSizeInBits((uint)yFeature.size * 8);
        }

        internal InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('L', 'J', 'O', 'Y'),
                extendsLayout = parentLayout
            };

            for (var i = 0; i < descriptor.controls.Count; i++)
            {
                SDLFeatureDescriptor feature = descriptor.controls[i];
                switch (feature.featureType)
                {
                    case JoystickFeatureType.Axis:
                    {
                        SDLAxisUsage usage = (SDLAxisUsage)feature.usageHint;
                        string featureName = SDLSupport.GetAxisNameFromUsage(usage);
                        string parameters = "scale,scaleFactor=65538,clamp,clampMin=-1,clampMax=1";

                        if (IsAxisX(feature) && i + 1 < descriptor.controls.Count)
                        {
                            SDLFeatureDescriptor nextFeature = descriptor.controls[i + 1];
                            if (IsAxisY(nextFeature))
                                BuildStickFeature(ref builder, feature, nextFeature);
                        }

                        if (IsAxisY(feature))
                            parameters += ",invert";

                        builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset((uint)feature.offset)
                            .WithFormat(InputStateBlock.kTypeInt)
                            .WithParameters(parameters);
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
                        SDLAxisUsage usage = (SDLAxisUsage)feature.usageHint;
                        string featureName = SDLSupport.GetAxisNameFromUsage(usage);
                        string parameters = "scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=1";

                        if (i + 1 < descriptor.controls.Count)
                        {
                            SDLFeatureDescriptor nextFeature = descriptor.controls[i + 1];
                            if (IsHatY(nextFeature) && HatNumber(feature) == HatNumber(nextFeature))
                                BuildHatFeature(ref builder, feature, nextFeature);
                        }

                        if (IsHatY(feature))
                            parameters += ",invert";

                        builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset((uint)feature.offset)
                            .WithFormat(InputStateBlock.kTypeInt)
                            .WithParameters(parameters);
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
