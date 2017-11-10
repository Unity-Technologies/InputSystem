#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

////TODO: sanitize set and action names into C# identifiers

////TODO: only generate @something if @ is really needed

////TODO: turn wrappers into structs, if possible (not sure how to make the property drawer stuff work with that)

////TODO: generate Clone() methods (both on toplevel wrapper and on action set wrappers)

////TODO: allow having an unnamed or default-named action set which spills actions directly into the toplevel wrapper

namespace ISX.Editor
{
    // Utility to generate code that makes it easier to work with action sets.
    public static class InputActionCodeGenerator
    {
        private const int kSpacesPerIndentLevel = 4;

        [Serializable]
        public struct Options
        {
            public string className;
            public string namespaceName;
            public string sourceAssetPath;
        }

        public static string GenerateWrapperCode(InputActionAsset asset, Options options = new Options())
        {
            if (string.IsNullOrEmpty(options.className))
                options.className = asset.name;
            if (string.IsNullOrEmpty(options.sourceAssetPath))
                options.sourceAssetPath = AssetDatabase.GetAssetPath(asset);
            return GenerateWrapperCode(asset.actionSets, options);
        }

        // Generate a string containing C# code that simplifies working with the given
        // action sets in code.
        public static string GenerateWrapperCode(IEnumerable<InputActionSet> sets, Options options)
        {
            var writer = new Writer
            {
                buffer = new StringBuilder()
            };

            // Header.
            writer.WriteLine($"// GENERATED AUTOMATICALLY FROM '{options.sourceAssetPath}'\n");

            // Begin namespace.
            var haveNamespace = !string.IsNullOrEmpty(options.namespaceName);
            if (haveNamespace)
            {
                writer.WriteLine($"namespace {options.namespaceName}");
                writer.BeginBlock();
            }

            // Begin class.
            writer.WriteLine("[System.Serializable]");
            writer.WriteLine($"public class {options.className} : ISX.InputActionWrapper");
            writer.BeginBlock();

            // Initialize method.
            writer.WriteLine("private bool m_Initialized;");
            writer.WriteLine("private void Initialize()");
            writer.BeginBlock();
            foreach (var set in sets)
            {
                writer.WriteLine($"// {set.name}");
                writer.WriteLine($"m_{set.name} = asset.FindActionSet(\"{set.name}\");");
                foreach (var action in set.actions)
                    writer.WriteLine($"m_{set.name}_{action.name} = m_{set.name}.GetAction(\"{action.name}\");");
            }
            writer.WriteLine("m_Initialized = true;");
            writer.EndBlock();

            // Action set accessors.
            foreach (var set in sets)
            {
                writer.WriteLine($"// {set.name}");
                var setStructName = MakeTypeName(set.name, "Actions");

                // Caching field for action set.
                writer.WriteLine($"private ISX.InputActionSet m_{set.name};");

                // Caching fields for all actions.
                foreach (var action in set.actions)
                    writer.WriteLine($"private ISX.InputAction m_{set.name}_{action.name};");

                // Struct wrapping access to action set.
                writer.WriteLine($"public struct {setStructName}");
                writer.BeginBlock();

                // Constructor.
                writer.WriteLine($"private {options.className} m_Wrapper;");
                writer.WriteLine($"public {setStructName}({options.className} wrapper) {{ m_Wrapper = wrapper; }}");

                // Getter for each action.
                foreach (var action in set.actions)
                    writer.WriteLine($"public ISX.InputAction @{action.name} {{ get {{ return m_Wrapper.m_{set.name}_{action.name}; }} }}");

                // Action set getter.
                writer.WriteLine($"public ISX.InputActionSet Get() {{ return m_Wrapper.m_{set.name}; }}");

                // Enable/disable methods.
                writer.WriteLine($"public void Enable() {{ Get().Enable(); }}");
                writer.WriteLine($"public void Disable() {{ Get().Disable(); }}");

                // Clone method.
                writer.WriteLine($"public ISX.InputActionSet Clone() {{ return Get().Clone(); }}");

                // Implicit conversion operator.
                writer.WriteLine($"public static implicit operator ISX.InputActionSet({setStructName} set) {{ return set.Get(); }}");

                writer.EndBlock();

                // Getter for instance of struct.
                writer.WriteLine($"public {setStructName} @{set.name}");
                writer.BeginBlock();

                writer.WriteLine($"get");
                writer.BeginBlock();
                writer.WriteLine("if (!m_Initialized) Initialize();");
                writer.WriteLine($"return new {setStructName}(this);");
                writer.EndBlock();

                writer.EndBlock();
            }

            // End class.
            writer.EndBlock();

            // End namespace.
            if (haveNamespace)
                writer.EndBlock();

            return writer.buffer.ToString();
        }

        private struct Writer
        {
            public StringBuilder buffer;
            public int indentLevel;

            public void BeginBlock()
            {
                WriteIndent();
                buffer.Append("{\n");
                ++indentLevel;
            }

            public void EndBlock()
            {
                --indentLevel;
                WriteIndent();
                buffer.Append("}\n");
            }

            public void WriteLine(string text)
            {
                WriteIndent();
                buffer.Append(text);
                buffer.Append('\n');
            }

            private void WriteIndent()
            {
                for (var i = 0; i < indentLevel; ++i)
                {
                    for (var n = 0; n < kSpacesPerIndentLevel; ++n)
                        buffer.Append(' ');
                }
            }
        }

        private static string MakeTypeName(string name, string suffix)
        {
            if (char.IsLower(name[0]))
                name = char.ToUpper(name[0]).ToString() + name.Substring(1);
            return $"{name}{suffix}";
        }

        // Updates the given file with wrapper code generated for the given action sets.
        // If the generated code is unchanged, does not touch the file.
        // Returns true if the file was touched, false otherwise.
        public static bool GenerateWrapperCode(string filePath, IEnumerable<InputActionSet> sets, Options options)
        {
            // Generate code.
            var code = GenerateWrapperCode(sets, options);

            // Check if the code changed. Don't write if it hasn't.
            if (File.Exists(filePath))
            {
                var existingCode = File.ReadAllText(filePath);
                if (existingCode == code)
                    return false;
            }

            // Write.
            File.WriteAllText(filePath, code);
            return true;
        }
    }
}
#endif // UNITY_EDITOR
