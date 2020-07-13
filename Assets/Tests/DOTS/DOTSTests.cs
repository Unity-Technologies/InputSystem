#if UNITY_EDITOR
using System.CodeDom.Compiler;
using System.Reflection;
using UnityEngine.InputSystem;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Input;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

public class DOTSTests
{
    #if UNITY_EDITOR
    [Test]
    [Category("DOTS")]
    [TestCase("Mouse")]
    public void DOTS_CanGenerateInputPipelinesForDevice(string layoutName)
    {
        // Generate code for the layout as well as for all base layouts.
        var allLayouts = InputControlLayout.s_Layouts.GetBaseLayouts(new InternedString(layoutName), includeSelf: true).ToArray();
        var assembly = Compile(
            allLayouts.Select(l =>
                UnityEngine.InputSystem.Editor.InputDotsCodeGenerator.GenerateECSInputPipeline(l, "Test")).ToArray(), "/unsafe");

        Assert.That(assembly.GetType("Test." + layoutName + "Input"), Is.Not.Null);
        Assert.That(assembly.GetType("Test." + layoutName + "Event"), Is.Not.Null);

        var device = InputDevice.Build<InputDevice>(layoutName);
        Assert.That(Marshal.SizeOf(assembly.GetType("Test." + layoutName + "Event")),
            Is.EqualTo(device.stateBlock.alignedSizeInBytes.AlignToMultipleOf(4)),
            "Size of event struct must match size of state for InputDevice");

        var fragments = new Dictionary<ulong, DOTSInput.InputPipelineFragment>();
        try
        {
            // Collect all pipeline fragments.
            foreach (var type in allLayouts.SelectMany(l => new[] { "Test." + l + "Input", "Test." + l + "Event" }).Select(t => assembly.GetType(t, true)))
                using (var parts = ((IInputData)Activator.CreateInstance(type)).InputPipelineParts)
                    DOTSInput.AddPipelineFragments(parts, fragments);

            // Ensure we can go from the device event struct to each of the component input formats.
            var inputFormat = CRC32.crc32(layoutName + "Event");
            foreach (var layout in allLayouts)
            {
                var outputFormat = CRC32.crc32(layout + "Input");
                var pipeline = new DOTSInput.InputPipeline();
                try
                {
                    Assert.That(DOTSInput.CreateInputPipeline(fragments, inputFormat, outputFormat, ref pipeline), Is.True,
                        $"Cannot create input pipeline from '{layoutName}Event' to '{layout}Input'.");
                    Assert.That(pipeline.Transforms, Is.Not.Empty);
                    Assert.That(pipeline.StructMappings, Is.Not.Empty);
                }
                finally
                {
                    pipeline.Dispose();
                }
            }
        }
        finally
        {
            DOTSInput.ReleasePipelineFragments(fragments);
        }
    }

    internal static Assembly Compile(string[] code, string options = null)
    {
        var codeProvider = CodeDomProvider.CreateProvider("CSharp");
        var cp = new CompilerParameters { CompilerOptions = options };
        cp.ReferencedAssemblies.Add($"{EditorApplication.applicationContentsPath}/Managed/UnityEngine/UnityEngine.CoreModule.dll");
        cp.ReferencedAssemblies.Add("Library/ScriptAssemblies/Unity.InputSystem.dll"); ////TODO: remove
        cp.ReferencedAssemblies.Add("Library/ScriptAssemblies/Unity.Input.dll");
        cp.ReferencedAssemblies.Add("Library/ScriptAssemblies/Unity.Entities.dll");
        var cr = codeProvider.CompileAssemblyFromSource(cp, code);
        Assert.That(cr.Errors, Is.Empty);
        var assembly = cr.CompiledAssembly;
        Assert.That(assembly, Is.Not.Null);
        return assembly;
    }

    #endif

    [Test]
    [Category("DOTS")]
    public unsafe void DOTS_CanConvertPs4HidEventToGamepadInput()
    {
        var hidInput = new DualShock4GamepadHIDEvent();
        var gamepadInput = new GamepadInput();

        var hidInputPipelines = hidInput.InputPipelineParts;
        var gamepadInputPipelines = gamepadInput.InputPipelineParts;

        try
        {
            // Pass 1.
            DOTSInput.Transform(UnsafeUtility.AddressOf(ref hidInput), UnsafeUtility.AddressOf(ref gamepadInput), ref hidInputPipelines, 0);

            Assert.That(gamepadInput.LeftStick.X, Is.Zero);
            Assert.That(gamepadInput.LeftStick.Y, Is.Zero);
            Assert.That(gamepadInput.RightStick.X, Is.Zero);
            Assert.That(gamepadInput.RightStick.Y, Is.Zero);
            Assert.That(gamepadInput.LeftStickLeft.Value, Is.Zero);
            Assert.That(gamepadInput.ButtonLeftStickLeft.IsPressed, Is.False);
            Assert.That(gamepadInput.ButtonLeftStickLeft.WasJustPressed, Is.False);
            Assert.That(gamepadInput.LeftStickX, Is.EqualTo(-1f).Within(0.00001));
            Assert.That(gamepadInput.RightStickLeft.Value, Is.Zero);
            Assert.That(gamepadInput.ButtonRightStickLeft.IsPressed, Is.False);
            Assert.That(gamepadInput.ButtonRightStickLeft.WasJustPressed, Is.False);
            Assert.That(gamepadInput.RightStickX, Is.EqualTo(-1f).Within(0.00001));

            // Pass 2.
            DOTSInput.Transform(UnsafeUtility.AddressOf(ref gamepadInput), UnsafeUtility.AddressOf(ref gamepadInput), ref gamepadInputPipelines,
                0);

            Assert.That(gamepadInput.LeftStick.X, Is.EqualTo(-1).Within(0.00001));
            Assert.That(gamepadInput.LeftStick.Y, Is.EqualTo(-1).Within(0.00001));
            Assert.That(gamepadInput.RightStick.X, Is.EqualTo(-1).Within(0.00001));
            Assert.That(gamepadInput.RightStick.Y, Is.EqualTo(-1).Within(0.00001));
            Assert.That(gamepadInput.LeftStickLeft.Value, Is.EqualTo(1f).Within(0.00001));
            Assert.That(gamepadInput.ButtonLeftStickLeft.IsPressed, Is.True);
            Assert.That(gamepadInput.ButtonLeftStickLeft.WasJustPressed, Is.True);
            Assert.That(gamepadInput.LeftStickX, Is.EqualTo(-1f).Within(0.00001));
            Assert.That(gamepadInput.RightStickLeft.Value, Is.EqualTo(1f).Within(0.00001));
            Assert.That(gamepadInput.ButtonRightStickLeft.IsPressed, Is.True);
            Assert.That(gamepadInput.ButtonRightStickLeft.WasJustPressed, Is.True);
            Assert.That(gamepadInput.RightStickX, Is.EqualTo(-1f).Within(0.00001));
        }
        finally
        {
            hidInputPipelines.Dispose();
            gamepadInputPipelines.Dispose();
        }
    }

    [Test]
    [Category("DOTS")]
    public unsafe void DOTS_CanConvertPs4HidEventToComponentInput()
    {
        var fragments = new Dictionary<ulong, DOTSInput.InputPipelineFragment>();
        var pipeline = new DOTSInput.InputPipeline();
        try
        {
            DOTSInput.AddPipelineFragments<DualShock4GamepadHIDEvent>(fragments);
            DOTSInput.AddPipelineFragments<GamepadInput>(fragments);
            DOTSInput.AddPipelineFragments<ComponentInput>(fragments);

            var result = DOTSInput.CreateInputPipeline(fragments, new DualShock4GamepadHIDEvent().Format, new ComponentInput().Format, ref pipeline);

            Assert.That(result, Is.True);
            Assert.That(pipeline.StructMappings.Length, Is.EqualTo(3));
            Assert.That(pipeline.StructMappings[0].InputFormat, Is.EqualTo(CRC32.crc32("DualShock4GamepadHIDEvent")));
            Assert.That(pipeline.StructMappings[0].OutputFormat, Is.EqualTo(CRC32.crc32("GamepadInput")));
            Assert.That(pipeline.StructMappings[1].InputFormat, Is.EqualTo(CRC32.crc32("GamepadInput")));
            Assert.That(pipeline.StructMappings[1].OutputFormat, Is.EqualTo(CRC32.crc32("GamepadInput")));
            Assert.That(pipeline.StructMappings[2].InputFormat, Is.EqualTo(CRC32.crc32("GamepadInput")));
            Assert.That(pipeline.StructMappings[2].OutputFormat, Is.EqualTo(CRC32.crc32("ComponentInput")));

            var hidEvent = new DualShock4GamepadHIDEvent
            {
                ButtonSouth = true
            };
            var component = new ComponentInput
            {
                PlayerNumber = 1234
            };

            DOTSInput.Transform(UnsafeUtility.AddressOf(ref hidEvent), UnsafeUtility.AddressOf(ref component), ref pipeline);

            Assert.That(component.PlayerNumber, Is.EqualTo(1234));
            Assert.That(component.Move.X, Is.EqualTo(-1).Within(0.00001));
            Assert.That(component.Move.Y, Is.EqualTo(-1).Within(0.00001));
            Assert.That(component.Look.X, Is.EqualTo(-1).Within(0.00001));
            Assert.That(component.Look.Y, Is.EqualTo(-1).Within(0.00001));
            Assert.That(component.Fire.IsPressed, Is.True);
            Assert.That(component.Fire.WasJustPressed, Is.True);
            Assert.That(component.Fire.WasJustReleased, Is.False);
        }
        finally
        {
            pipeline.Dispose();
            DOTSInput.ReleasePipelineFragments(fragments);
        }
    }

    private struct ComponentInput : IInputData
    {
        public int PlayerNumber;

        public Float2Input Move;
        public Float2Input Look;
        public ButtonInput Fire;

        public enum Id : uint
        {
            Move = 4 * 8,
            Look = Move + 8 * 8,
            Fire = Look + 8 * 8
        }

        public uint Format => CRC32.crc32("ComponentInput");

        public DOTSInput.InputPipeline InputPipelineParts
        {
            get
            {
                var structMappings = new NativeArray<DOTSInput.InputStructMapping>(1, Allocator.Persistent)
                {
                    [0] = new DOTSInput.InputStructMapping
                    {
                        InputFormat = CRC32.crc32("GamepadInput"),
                        OutputFormat = CRC32.crc32("ComponentInput"),
                        InputSizeInBytes = (uint)UnsafeUtility.SizeOf<GamepadInput>(),
                        OutputSizeInBytes = (uint)UnsafeUtility.SizeOf<ComponentInput>(),
                        TransformStartIndex = 0,
                        TransformCount = 3
                    }
                };

                var transforms = new NativeArray<DOTSInput.InputTransform>(3, Allocator.Persistent)
                {
                    [0] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToCopyOperation(8 * 8),
                        InputId1 = (uint)GamepadInput.Id.LeftStick,
                        OutputId = (uint)Id.Move
                    },
                    [1] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToCopyOperation(8 * 8),
                        InputId1 = (uint)GamepadInput.Id.RightStick,
                        OutputId = (uint)Id.Look
                    },
                    [2] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToCopyOperation(1 * 8),
                        InputId1 = (uint)GamepadInput.Id.ButtonSouth,
                        OutputId = (uint)Id.Fire
                    },
                };

                return new DOTSInput.InputPipeline
                {
                    StructMappings = structMappings,
                    Transforms = transforms
                };
            }
        }
    }
}
