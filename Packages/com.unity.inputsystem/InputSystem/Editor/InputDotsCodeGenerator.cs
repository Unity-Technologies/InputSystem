#if UNITY_EDITOR
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
                writer.WriteLine($"struct {componentName} : IComponentData");
                writer.BeginBlock();

                var currentBitOffset = 0;
                var ids = new Dictionary<string, int>();

                // First values.
                foreach (var action in actionMap.Where(a => a.type == InputActionType.Value))
                {
                    string inputType = null;
                    var inputSizeInBits = 0;

                    switch (action.expectedControlType)
                    {
                        case "Stick":
                        case "Vector2":
                            inputType = "AxisInput";
                            inputSizeInBits = 8 * 8;
                            break;
                    }

                    if (inputType == null)
                        continue;

                    var name = CSharpCodeHelpers.MakeIdentifier(action.name);
                    writer.WriteLine($"{inputType} {name};");

                    ids[name] = currentBitOffset;
                    currentBitOffset += inputSizeInBits;
                }

                // Then buttons.
                foreach (var action in actionMap.Where(a => a.type == InputActionType.Button))
                {
                    var name = CSharpCodeHelpers.MakeIdentifier(action.name);
                    writer.WriteLine($"ButtonInput {name};");

                    ids[name] = currentBitOffset;
                    currentBitOffset += 8;
                }

                // IDs.
                writer.WriteLine("public enum Id : uint");
                writer.BeginBlock();
                foreach (var id in ids)
                    writer.WriteLine($"{id.Key} = {id.Value},");
                writer.EndBlock();

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
    }
}
#endif // UNITY_EDITOR
