#if UNITY_EDITOR || UNITY_STANDALONE_LINUX
using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Text;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Linux
{
    [Serializable]
    internal class SDLLayoutBuilder
    {
        [SerializeField] private string m_ParentLayout;
        [SerializeField] private SDLDeviceDescriptor m_Descriptor;

        internal static string OnFindLayoutForDevice(ref InputDeviceDescription description, string matchedLayout,
            InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            if (description.interfaceName != LinuxSupport.kInterfaceName)
                return null;

            if (string.IsNullOrEmpty(description.capabilities))
                return null;

            // Try to parse the SDL descriptor.
            SDLDeviceDescriptor deviceDescriptor;
            try
            {
                deviceDescriptor = SDLDeviceDescriptor.FromJson(description.capabilities);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{exception} while trying to parse descriptor for SDL device: {description.capabilities}");
                return null;
            }

            if (deviceDescriptor == null)
                return null;

            string layoutName;
            if (string.IsNullOrEmpty(description.manufacturer))
            {
                layoutName = $"{SanitizeName(description.interfaceName)}::{SanitizeName(description.product)}";
            }
            else
            {
                layoutName =
                    $"{SanitizeName(description.interfaceName)}::{SanitizeName(description.manufacturer)}::{SanitizeName(description.product)}";
            }

            var layout = new SDLLayoutBuilder { m_Descriptor = deviceDescriptor, m_ParentLayout = matchedLayout };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(), layoutName, matchedLayout);

            return layoutName;
        }

        private static string SanitizeName(string originalName)
        {
            var stringLength = originalName.Length;
            var sanitizedName = new StringBuilder(stringLength);
            for (var i = 0; i < stringLength; i++)
            {
                var letter = originalName[i];
                if (char.IsUpper(letter) || char.IsLower(letter) || char.IsDigit(letter))
                    sanitizedName.Append(letter);
            }
            return sanitizedName.ToString();
        }

        private static bool IsAxis(SDLFeatureDescriptor feature, SDLAxisUsage axis)
        {
            return feature.featureType == JoystickFeatureType.Axis
                && feature.usageHint == (int)axis;
        }

        private static void BuildStickFeature(ref InputControlLayout.Builder builder, SDLFeatureDescriptor xFeature, SDLFeatureDescriptor yFeature)
        {
            int byteOffset;
            if (xFeature.offset <= yFeature.offset)
                byteOffset = xFeature.offset;
            else
                byteOffset = yFeature.offset;

            const string stickName = "Stick";
            builder.AddControl(stickName)
                .WithLayout("Stick")
                .WithByteOffset((uint)byteOffset)
                .WithSizeInBits((uint)xFeature.featureSize * 8 + (uint)yFeature.featureSize * 8)
                .WithUsages(CommonUsages.Primary2DMotion);

            builder.AddControl(stickName + "/x")
                .WithFormat(InputStateBlock.FormatInt)
                .WithByteOffset(0)
                .WithSizeInBits((uint)xFeature.featureSize * 8)
                .WithParameters("clamp=1,clampMin=-1,clampMax=1,scale,scaleFactor=65538");

            builder.AddControl(stickName + "/y")
                .WithFormat(InputStateBlock.FormatInt)
                .WithByteOffset(4)
                .WithSizeInBits((uint)xFeature.featureSize * 8)
                .WithParameters("clamp=1,clampMin=-1,clampMax=1,scale,scaleFactor=65538,invert");

            builder.AddControl(stickName + "/up")
                .WithParameters("clamp=1,clampMin=-1,clampMax=0,scale,scaleFactor=65538,invert");

            builder.AddControl(stickName + "/down")
                .WithParameters("clamp=1,clampMin=0,clampMax=1,scale,scaleFactor=65538,invert=false");

            builder.AddControl(stickName + "/left")
                .WithParameters("clamp=1,clampMin=-1,clampMax=0,scale,scaleFactor=65538,invert");

            builder.AddControl(stickName + "/right")
                .WithParameters("clamp=1,clampMin=0,clampMax=1,scale,scaleFactor=65538");
        }

        private static bool IsHatX(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Hat
                && (feature.usageHint == (int)SDLAxisUsage.Hat0X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat1X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat2X
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat3X);
        }

        private static bool IsHatY(SDLFeatureDescriptor feature)
        {
            return feature.featureType == JoystickFeatureType.Hat
                && (feature.usageHint == (int)SDLAxisUsage.Hat0Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat1Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat2Y
                    ||  feature.usageHint == (int)SDLAxisUsage.Hat3Y);
        }

        private static int HatNumber(SDLFeatureDescriptor feature)
        {
            Debug.Assert(feature.featureType == JoystickFeatureType.Hat);
            return 1 + (feature.usageHint - (int)SDLAxisUsage.Hat0X) / 2;
        }

        private static void BuildHatFeature(ref InputControlLayout.Builder builder, SDLFeatureDescriptor xFeature, SDLFeatureDescriptor yFeature)
        {
            Debug.Assert(xFeature.offset < yFeature.offset, "Order of features must be X followed by Y");

            var hat = HatNumber(xFeature);
            var hatName = hat > 1 ? $"Hat{hat}" : "Hat";

            builder.AddControl(hatName)
                .WithLayout("Dpad")
                .WithByteOffset((uint)xFeature.offset)
                .WithSizeInBits((uint)xFeature.featureSize * 8 + (uint)yFeature.featureSize * 8)
                .WithUsages(CommonUsages.Hatswitch);

            builder.AddControl(hatName + "/up")
                .WithFormat(InputStateBlock.FormatInt)
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=0,invert")
                .WithByteOffset(4)
                .WithBitOffset(0)
                .WithSizeInBits((uint)yFeature.featureSize * 8);

            builder.AddControl(hatName + "/down")
                .WithFormat(InputStateBlock.FormatInt)
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=0,clampMax=1")
                .WithByteOffset(4)
                .WithBitOffset(0)
                .WithSizeInBits((uint)yFeature.featureSize * 8);

            builder.AddControl(hatName + "/left")
                .WithFormat(InputStateBlock.FormatInt)
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=-1,clampMax=0,invert")
                .WithByteOffset(0)
                .WithBitOffset(0)
                .WithSizeInBits((uint)xFeature.featureSize * 8);

            builder.AddControl(hatName + "/right")
                .WithFormat(InputStateBlock.FormatInt)
                .WithParameters("scale,scaleFactor=2147483647,clamp,clampMin=0,clampMax=1")
                .WithByteOffset(0)
                .WithBitOffset(0)
                .WithSizeInBits((uint)xFeature.featureSize * 8);
        }

        internal InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                stateFormat = new FourCC('L', 'J', 'O', 'Y'),
                extendsLayout = m_ParentLayout
            };

            for (var i = 0; i < m_Descriptor.controls.LengthSafe(); i++)
            {
                var feature = m_Descriptor.controls[i];
                switch (feature.featureType)
                {
                    case JoystickFeatureType.Axis:
                    {
                        var usage = (SDLAxisUsage)feature.usageHint;
                        var featureName = LinuxSupport.GetAxisNameFromUsage(usage);
                        var parameters = "scale,scaleFactor=65538,clamp=1,clampMin=-1,clampMax=1";

                        // If X is followed by Y, build a stick out of the two.
                        if (IsAxis(feature, SDLAxisUsage.X) && i + 1 < m_Descriptor.controls.Length)
                        {
                            var nextFeature = m_Descriptor.controls[i + 1];
                            if (IsAxis(nextFeature, SDLAxisUsage.Y))
                            {
                                BuildStickFeature(ref builder, feature, nextFeature);
                                ++i;
                                continue;
                            }
                        }

                        if (IsAxis(feature, SDLAxisUsage.Y))
                            parameters += ",invert";

                        var control = builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset((uint)feature.offset)
                            .WithFormat(InputStateBlock.FormatInt)
                            .WithParameters(parameters);

                        if (IsAxis(feature, SDLAxisUsage.RotateZ))
                            control.WithUsages(CommonUsages.Twist);
                        break;
                    }

                    case JoystickFeatureType.Ball:
                    {
                        //TODO
                        break;
                    }

                    case JoystickFeatureType.Button:
                    {
                        var usage = (SDLButtonUsage)feature.usageHint;
                        var featureName = LinuxSupport.GetButtonNameFromUsage(usage);
                        if (featureName != null)
                        {
                            builder.AddControl(featureName)
                                .WithLayout("Button")
                                .WithByteOffset((uint)feature.offset)
                                .WithBitOffset((uint)feature.bit)
                                .WithFormat(InputStateBlock.FormatBit);
                        }
                        break;
                    }

                    case JoystickFeatureType.Hat:
                    {
                        var usage = (SDLAxisUsage)feature.usageHint;
                        var featureName = LinuxSupport.GetAxisNameFromUsage(usage);
                        var parameters = "scale,scaleFactor=2147483647,clamp=1,clampMin=-1,clampMax=1";

                        if (i + 1 < m_Descriptor.controls.Length)
                        {
                            var nextFeature = m_Descriptor.controls[i + 1];
                            if (IsHatY(nextFeature) && HatNumber(feature) == HatNumber(nextFeature))
                            {
                                BuildHatFeature(ref builder, feature, nextFeature);
                                ++i;
                                continue;
                            }
                        }

                        if (IsHatY(feature))
                            parameters += ",invert";

                        builder.AddControl(featureName)
                            .WithLayout("Analog")
                            .WithByteOffset((uint)feature.offset)
                            .WithFormat(InputStateBlock.FormatInt)
                            .WithParameters(parameters);
                        break;
                    }

                    default:
                    {
                        throw new NotImplementedException(
                            $"SDLLayoutBuilder.Build: Trying to build an SDL device with an unknown feature of type {feature.featureType}.");
                    }
                }
            }

            return builder.Build();
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_LINUX
