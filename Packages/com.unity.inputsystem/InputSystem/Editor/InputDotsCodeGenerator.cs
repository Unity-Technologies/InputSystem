#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputDotsCodeGenerator
    {
        public static bool GenerateECSComponents(string filePath, InputActionAsset asset, InputActionCodeGenerator.Options options)
        {
            ////TODO: control schemes

            var anyFileWritten = false;
            foreach (var actionMap in asset.actionMaps)
            {
                var componentName = CSharpCodeHelpers.MakeTypeName(actionMap.name);
                if (!string.IsNullOrEmpty(options.className))
                    componentName = options.className + componentName;
                componentName += "Input";

                var writer = new InputActionCodeGenerator.Writer { buffer = new StringBuilder() };

                writer.WriteLine("using Unity.Collections;");
                writer.WriteLine("using Unity.Entities;");
                writer.WriteLine("using Unity.Input;");
                writer.WriteLine();

                var haveNamespace = !string.IsNullOrEmpty(options.namespaceName);
                if (haveNamespace)
                {
                    writer.WriteLine("namespace " + options.namespaceName);
                    writer.BeginBlock();
                }

                writer.WriteLine("[GenerateAuthoringComponent]");
                writer.WriteLine($"public struct {componentName} : IComponentData, IInputData");
                writer.BeginBlock();
                writer.WriteLine("public int PlayerNumber;");
                writer.WriteLine();

                var currentBitOffset = 4 * 8; // sizeof(PlayerNumber)
                var ids = new Dictionary<string, int>();
                var sizes = new Dictionary<string, int>();

                // First values.
                foreach (var action in actionMap.Where(a => a.type == InputActionType.Value))
                {
                    string inputType = null;
                    var inputSizeInBits = 0;

                    switch (action.expectedControlType)
                    {
                        case "Stick":
                        case "Vector2":
                            inputType = "Float2Input";
                            inputSizeInBits = 8 * 8;
                            break;

                        case "Axis":
                            inputType = "AxisInput";
                            inputSizeInBits = 4 * 8;
                            break;
                    }

                    if (inputType == null)
                        continue;

                    var name = CSharpCodeHelpers.MakeIdentifier(action.name);
                    writer.WriteLine($"public {inputType} {name};");

                    ids[name] = currentBitOffset;
                    sizes[action.name] = inputSizeInBits;
                    currentBitOffset += inputSizeInBits;
                }

                // Then buttons.
                foreach (var action in actionMap.Where(a => a.type == InputActionType.Button))
                {
                    var name = CSharpCodeHelpers.MakeIdentifier(action.name);
                    writer.WriteLine($"public ButtonInput {name};");

                    ids[name] = currentBitOffset;
                    sizes[action.name] = 8;
                    currentBitOffset += 8;
                }

                // IDs.
                writer.WriteLine();
                writer.WriteLine("public enum Id : uint");
                writer.BeginBlock();
                foreach (var id in ids)
                    writer.WriteLine($"{id.Key} = {id.Value},");
                writer.EndBlock();

                // Format.
                var outputFormatName = componentName;
                var outputFormatCode = CRC32.crc32(outputFormatName);
                writer.WriteLine();
                writer.WriteLine($"public uint Format => {outputFormatCode};");

                // Input pipelines.
                writer.WriteLine();
                writer.WriteLine("public DOTSInput.InputPipeline InputPipelineParts");
                writer.BeginBlock();
                writer.WriteLine("get");
                writer.BeginBlock();
                writer.WriteLine("var structMappings = new NativeArray<DOTSInput.InputStructMapping>(kNumStructMappings, Allocator.Persistent);");
                writer.WriteLine("var transforms = new NativeArray<DOTSInput.InputTransform>(kNumTransforms, Allocator.Persistent);");
                var transformIndex = 0;
                var structMappingIndex = 0;
                foreach (var controlScheme in asset.controlSchemes)
                {
                    if (controlScheme.deviceRequirements.Count > 1)
                        throw new NotImplementedException("support for control schemes with more than a single device requirement");

                    ////TODO: support stuff such as LeftHand/RightHand (i.e. device usages)

                    // Determine input format ID.
                    var deviceLayout = InputControlPath.TryGetDeviceLayout(controlScheme.deviceRequirements[0].controlPath);
                    if (string.IsNullOrEmpty(deviceLayout))
                        continue;
                    var device = InputDevice.Build<InputDevice>(deviceLayout);
                    var inputFormatName = deviceLayout + "Input";
                    var inputFormatCode = CRC32.crc32(inputFormatName);

                    // Add transforms.
                    var transformStartIndex = transformIndex;
                    foreach (var binding in actionMap.bindings)
                    {
                        // Ignore binding if it's not in the current control scheme.
                        if (!StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(binding.groups, controlScheme.bindingGroup,
                            ';'))
                            continue;

                        ////TODO: deal with multiple bindings for a single action

                        ////TODO: support composites
                        if (binding.isComposite)
                            throw new NotImplementedException("transforms for composites");

                        // Find control.
                        var control = InputControlPath.TryFindControl(device, binding.path);
                        if (control == null)
                            continue;

                        // Find action.
                        var action = actionMap.FindAction(binding.action);
                        if (action == null)
                            continue;

                        // ATM this is super unintelligent. We only support copy ops ATM which means the source data already has
                        // to be 1:1.

                        var id = MapControlToId(control);

                        writer.WriteLine($"transforms[{transformIndex}] = new DOTSInput.InputTransform");
                        writer.BeginBlock();
                        writer.WriteLine($"Operation = DOTSInput.ToCopyOperation({sizes[action.name]}),");
                        writer.WriteLine($"InputId1 = (uint){inputFormatName}.Id.{id},");
                        writer.WriteLine($"OutputId = (uint)Id.{CSharpCodeHelpers.MakeIdentifier(action.name)},");
                        writer.EndBlock(true);

                        ++transformIndex;
                    }

                    // Add a struct mapping.
                    writer.WriteLine($"structMappings[{structMappingIndex}] = new DOTSInput.InputStructMapping");
                    writer.BeginBlock();
                    writer.WriteLine($"InputFormat = {inputFormatCode},");
                    writer.WriteLine($"OutputFormat = {outputFormatCode},");
                    writer.WriteLine($"TransformStartIndex = {transformStartIndex},");
                    writer.WriteLine($"TransformCount = {transformIndex - transformStartIndex},");
                    writer.EndBlock(true);
                    structMappingIndex++;
                }
                writer.WriteLine("return new DOTSInput.InputPipeline { Transforms = transforms, StructMappings = structMappings };");
                writer.EndBlock();
                writer.EndBlock();
                writer.WriteLine($"private const int kNumStructMappings = {structMappingIndex};");
                writer.WriteLine($"private const int kNumTransforms = {transformIndex};");

                // End component.
                writer.EndBlock();

                // System.
                writer.WriteLine($"public class {componentName}Update : InputSystem<{componentName}>");
                writer.BeginBlock();
                writer.EndBlock();

                if (haveNamespace)
                    writer.EndBlock();

                var code = writer.buffer.ToString();

                var file = Path.Combine(Path.GetDirectoryName(filePath), componentName + ".cs");
                if (File.Exists(file))
                {
                    var existingCode = File.ReadAllText(file);
                    if (existingCode == code || existingCode.WithAllWhitespaceStripped() == code.WithAllWhitespaceStripped())
                        continue;
                }

                File.WriteAllText(file, code);
                anyFileWritten = true;
            }

            return anyFileWritten;
        }

        private static string MapControlToId(InputControl control)
        {
            var devicePath = control.device.path;
            var controlPath = control.path;
            var pathOnDevice = controlPath.Substring(devicePath.Length + 1);
            var parts = pathOnDevice.Split('/');
            return string.Join("", parts.Select(p => CSharpCodeHelpers.MakeTypeName(p)));
        }
    }
}
#endif // UNITY_EDITOR
