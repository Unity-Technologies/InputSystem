#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;

////TODO: only generate @something if @ is really needed

////TODO: turn wrappers into structs, if possible (not sure how to make the property drawer stuff work with that)

////TODO: generate Clone() methods (both on toplevel wrapper and on action set wrappers)

////TODO: allow having an unnamed or default-named action set which spills actions directly into the toplevel wrapper

namespace UnityEngine.Experimental.Input.Editor
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
            if (string.IsNullOrEmpty(options.sourceAssetPath))
                options.sourceAssetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(options.className) && !string.IsNullOrEmpty(asset.name))
                options.className =
                    CSharpCodeHelpers.MakeTypeName(asset.name);
            return GenerateWrapperCode(asset.actionMaps, options);
        }

        // Generate a string containing C# code that simplifies working with the given
        // action sets in code.
        public static string GenerateWrapperCode(IEnumerable<InputActionMap> sets, Options options)
        {
            if (string.IsNullOrEmpty(options.sourceAssetPath))
                throw new ArgumentException("options.sourceAssetPath");

            if (string.IsNullOrEmpty(options.className))
                options.className =
                    CSharpCodeHelpers.MakeTypeName(Path.GetFileNameWithoutExtension(options.sourceAssetPath));

            var writer = new Writer
            {
                buffer = new StringBuilder()
            };

            // Header.
            writer.WriteLine(string.Format("// GENERATED AUTOMATICALLY FROM '{0}'\n", options.sourceAssetPath));

            // Begin namespace.
            var haveNamespace = !string.IsNullOrEmpty(options.namespaceName);
            if (haveNamespace)
            {
                writer.WriteLine(string.Format("namespace {0}", options.namespaceName));
                writer.BeginBlock();
            }

            // Begin class.
            writer.WriteLine("[System.Serializable]");
            writer.WriteLine(string.Format("public class {0} : UnityEngine.Experimental.Input.InputActionWrapper", options.className));
            writer.BeginBlock();

            // Initialize method.
            writer.WriteLine("private bool m_Initialized;");
            writer.WriteLine("private void Initialize()");
            writer.BeginBlock();
            foreach (var set in sets)
            {
                var setName = CSharpCodeHelpers.MakeIdentifier(set.name);
                writer.WriteLine(string.Format("// {0}", set.name));
                writer.WriteLine(string.Format("m_{0} = asset.GetActionMap(\"{1}\");", setName, set.name));
                foreach (var action in set.actions)
                    writer.WriteLine(string.Format("m_{0}_{1} = m_{2}.GetAction(\"{3}\");", setName, CSharpCodeHelpers.MakeIdentifier(action.name),
                        setName, action.name));
            }
            writer.WriteLine("m_Initialized = true;");
            writer.EndBlock();

            // Action set accessors.
            foreach (var set in sets)
            {
                writer.WriteLine(string.Format("// {0}", set.name));

                var setName = CSharpCodeHelpers.MakeIdentifier(set.name);
                var setStructName = CSharpCodeHelpers.MakeTypeName(setName, "Actions");

                // Caching field for action set.
                writer.WriteLine(string.Format("private UnityEngine.Experimental.Input.InputActionMap m_{0};", setName));

                // Caching fields for all actions.
                foreach (var action in set.actions)
                    writer.WriteLine(string.Format("private UnityEngine.Experimental.Input.InputAction m_{0}_{1};", setName, CSharpCodeHelpers.MakeIdentifier(action.name)));

                // Struct wrapping access to action set.
                writer.WriteLine(string.Format("public struct {0}", setStructName));
                writer.BeginBlock();

                // Constructor.
                writer.WriteLine(string.Format("private {0} m_Wrapper;", options.className));
                writer.WriteLine(string.Format("public {0}({1} wrapper) {{ m_Wrapper = wrapper; }}", setStructName,
                    options.className));

                // Getter for each action.
                foreach (var action in set.actions)
                {
                    var actionName = CSharpCodeHelpers.MakeIdentifier(action.name);
                    writer.WriteLine(string.Format(
                        "public UnityEngine.Experimental.Input.InputAction @{0} {{ get {{ return m_Wrapper.m_{1}_{2}; }} }}", actionName,
                        setName, actionName));
                }

                // Action set getter.
                writer.WriteLine(string.Format("public UnityEngine.Experimental.Input.InputActionMap Get() {{ return m_Wrapper.m_{0}; }}",
                    setName));

                // Enable/disable methods.
                writer.WriteLine("public void Enable() { Get().Enable(); }");
                writer.WriteLine("public void Disable() { Get().Disable(); }");

                // Clone method.
                writer.WriteLine("public UnityEngine.Experimental.Input.InputActionMap Clone() { return Get().Clone(); }");

                // Implicit conversion operator.
                writer.WriteLine(string.Format(
                    "public static implicit operator UnityEngine.Experimental.Input.InputActionMap({0} set) {{ return set.Get(); }}",
                    setStructName));

                writer.EndBlock();

                // Getter for instance of struct.
                writer.WriteLine(string.Format("public {0} @{1}", setStructName, setName));
                writer.BeginBlock();

                writer.WriteLine("get");
                writer.BeginBlock();
                writer.WriteLine("if (!m_Initialized) Initialize();");
                writer.WriteLine(string.Format("return new {0}(this);", setStructName));
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

        // Updates the given file with wrapper code generated for the given action sets.
        // If the generated code is unchanged, does not touch the file.
        // Returns true if the file was touched, false otherwise.
        public static bool GenerateWrapperCode(string filePath, IEnumerable<InputActionMap> sets, Options options)
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
