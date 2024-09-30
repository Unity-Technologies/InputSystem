using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.InputSystem.Experimental.Generator;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace UnityEditor.InputSystem.Experimental
{
    public class BindingTypeGenerator
    {
        // TODO This is temporary during development and should be removed
        [UnityEditor.MenuItem("Debug/Generate Binding Types (Edit-mode)")]
        public static void GenerateMenuItem() => Run();

        private static IEnumerable<Type> GetBindingValueTypes()
        {
            var referenceTypes = TypeCache.GetTypesWithAttribute<InputValueTypeReferenceAttribute>();
            foreach (var referenceType in referenceTypes)
            {
                var attributes = referenceType.GetCustomAttributes(typeof(InputValueTypeReferenceAttribute));
                foreach (var attribute in attributes)
                {
                    var attrib = attribute as InputValueTypeReferenceAttribute;
                    if (attrib == null)
                        continue;
                    yield return attrib.type;
                }
            }

            var types = TypeCache.GetTypesWithAttribute<InputValueTypeAttribute>();
            foreach (var type in types)
                yield return type;
        }

        private static void Generate(IEnumerable<Type> types)
        {
            foreach (var type in GetBindingValueTypes())
            {
                var ns = "UnityEngine.InputSystem.Experimental";
                var typeName = SourceUtils.GetTypeName(type);
                var sourceTypeName = char.ToUpper(typeName[0]) + typeName.Substring(1);
                var bindingTypeName = $"{sourceTypeName}InputBinding"; 
                var fileName = $"{bindingTypeName}{SourceUtils.CSharpSuffix}"; 
                var path = Path.Combine(Resources.PackageBindingsPath, fileName);
                
                var b = new SourceBuilder(path);
                b.WriteLine(SourceUtils.Header);
                if (!type.IsPrimitive && type.Namespace != null && type.Namespace.Length > 0 && !ns.StartsWith(type.Namespace))
                    b.WriteLine($"using {type.Namespace};");
                b.WriteLine($"namespace {ns}");
                b.WriteLine("{");
                b.WriteLine($"    public class {bindingTypeName} : WrappedScriptableInputBinding<{typeName}> {{ }}");
                b.WriteLine("");
                b.WriteLine($"    struct Bootstrap{bindingTypeName}");
                b.WriteLine("    {");
                b.WriteLine("        [UnityEditor.InitializeOnLoadMethod]");
                b.WriteLine("        public static void Install()");
                b.WriteLine("        {");
                b.WriteLine($"            ScriptableInputBinding.RegisterInputBindingType(typeof({typeName}), typeof({bindingTypeName}));");
                b.WriteLine("        }");
                b.WriteLine("    }");
                b.WriteLine("}");
                b.WriteLine("");
                
                SourceUtils.Generate(path, () => b.ToString());
            }
        }
        
        private static void Run()
        {
            Generate(GetBindingValueTypes());
        }
    }
}