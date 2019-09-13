#if UNITY_WEBGL || UNITY_EDITOR
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.WebGL.LowLevel;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System;

namespace UnityEngine.InputSystem.WebGL
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class WebGLSupport
    {
        private const string InterfaceName = "WebGL";
        public static void Initialize()
        {
            // We only turn gamepads with the "standard" mapping into actual Gamepads.
            InputSystem.RegisterLayout<WebGLGamepad>(
                matches: new InputDeviceMatcher()
                    .WithInterface(InterfaceName)
                    .WithDeviceClass("Gamepad")
                    .WithCapability("mapping", "standard"));

            InputSystem.onFindLayoutForDevice += OnFindLayoutForDevice;
        }

        internal static string OnFindLayoutForDevice(ref InputDeviceDescription description,
            string matchedLayout, InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            // If the device isn't a WebGL device, we're not interested.
            if (string.Compare(description.interfaceName, InterfaceName, StringComparison.InvariantCultureIgnoreCase) != 0)
                return null;

            // If it was matched by the standard mapping, we don't need to fall back to generating a layout.
            if (!string.IsNullOrEmpty(matchedLayout) && matchedLayout != "Gamepad")
                return null;

            var deviceMatcher = InputDeviceMatcher.FromDeviceDescription(description);

            var layout = new WebGLLayoutBuilder {capabilities = WebGLDeviceCapabilities.FromJson(description.capabilities)};
            InputSystem.RegisterLayoutBuilder(() => layout.Build(),
                description.product, "Joystick", deviceMatcher);

            return description.product;
        }

        [Serializable]
        private class WebGLLayoutBuilder
        {
            public WebGLDeviceCapabilities capabilities;

            public InputControlLayout Build()
            {
                var builder = new InputControlLayout.Builder
                {
                    type = typeof(WebGLJoystick),
                    extendsLayout = "Joystick",
                    stateFormat = new FourCC('H', 'T', 'M', 'L')
                };

                // Best guess: Treat first two axes as stick
                uint offset = 0;
                if (capabilities.numAxes >= 2)
                {
                    var stickName = "Stick";
                    builder.AddControl(stickName)
                        .WithLayout("Stick")
                        .WithByteOffset(offset)
                        .WithSizeInBits(64)
                        .WithFormat(InputStateBlock.FormatFloat);

                    builder.AddControl(stickName + "/x")
                        .WithLayout("Axis")
                        .WithByteOffset(offset)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    builder.AddControl(stickName + "/y")
                        .WithLayout("Axis")
                        .WithByteOffset(offset + 4)
                        .WithParameters("invert")
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    //Need to handle Up/Down/Left/Right
                    builder.AddControl(stickName + "/up")
                        .WithLayout("Button")
                        .WithParameters("clamp=1,clampMin=-1,clampMax=0,invert")
                        .WithByteOffset(offset + 4)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    builder.AddControl(stickName + "/down")
                        .WithLayout("Button")
                        .WithParameters("clamp=1,clampMin=0,clampMax=1")
                        .WithByteOffset(offset + 4)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    builder.AddControl(stickName + "/left")
                        .WithLayout("Button")
                        .WithParameters("clamp=1,clampMin=-1,clampMax=0,invert")
                        .WithByteOffset(offset)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    builder.AddControl(stickName + "/right")
                        .WithLayout("Button")
                        .WithParameters("clamp=1,clampMin=0,clampMax=1")
                        .WithByteOffset(offset)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);

                    offset += 8;
                }

                for (var axis = 2; axis < capabilities.numAxes; axis++)
                {
                    builder.AddControl($"Axis {axis - 1}")
                        .WithLayout("Axis")
                        .WithByteOffset(offset)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);
                    offset += 4;
                }

                var buttonStartOffset = offset;

                for (var button = 0; button < capabilities.numButtons; button++)
                {
                    builder.AddControl($"Button {button + 1}")
                        .WithLayout("Button")
                        .WithByteOffset(offset)
                        .WithSizeInBits(32)
                        .WithFormat(InputStateBlock.FormatFloat);
                    offset += 4;
                }

                builder.AddControl("Trigger")
                    .WithLayout("AnyKey")
                    .WithByteOffset(buttonStartOffset)
                    .IsSynthetic(true)
                    .WithSizeInBits((uint)(32 * capabilities.numButtons))
                    .WithFormat(InputStateBlock.FormatBit);

                return builder.Build();
            }
        }
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
