using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Generator;

namespace UnityEditor.InputSystem.Experimental.Generator
{
    public static class PresetGenerator
    {
        // TODO This is temporary during development and should be removed
        [UnityEditor.MenuItem("Debug/Generate Presets Code")]
        public static void GenerateMenuItem() => Run();

        private sealed class Context : BasicSourceFormatter
        {
            private int m_Count;
            
            public Context(string presetIdentifier, string path, string sourceNamespace)
                : base(path)
            {
                WriteLine(SourceUtils.Header);
                WriteLine($"using {sourceNamespace};");
                NewLine();
                WriteLine("namespace UnityEditor.InputSystem.Experimental");
                WriteLine('{');
                IncreaseIndent();
                WriteLine($"public static class {presetIdentifier}");
                WriteLine("{");
                IncreaseIndent();
            }

            public void End()
            {
                DecreaseIndent();
                WriteLine("}");
                DecreaseIndent();
                WriteLine('}');

                Complete();
            }
            
            public void AddMethod(MethodInfo method, InputPresetAttribute attribute, Type valueType)
            {
                if (m_Count > 0)
                    NewLine();
                
                var presetMethodName = method.Name;
                var presetName = attribute.displayName ?? presetMethodName;
                var presetClass = method!.DeclaringType!.Name;
                var presetCategory = attribute.category;
                //var valueTypeName = valueType.Name;
                //WriteLine($"\t\t[MenuItem(Editor.Resources.InputBindingAssetPresetMenu + \"{presetCategory}/{presetName} ({valueTypeName})\")]");
                WriteLine($"[MenuItem(Resources.InputBindingAssetPresetMenu + \"{presetCategory}/{presetName}\")]");
                WriteLine($"public static void {presetMethodName}() => {presetClass}.{presetMethodName}().CreateAssetFromName(\"{presetMethodName}\");");
                
                ++m_Count;
            }
        }

        private static void Run()
        {
            var dict = new Dictionary<Type, Context>();

            var methods = UnityEditor.TypeCache.GetMethodsWithAttribute<InputPresetAttribute>();
            foreach (var method in methods)
            {
                if (!ValidateMethod(method, out var valueType)) 
                    continue;
                
                var attribute = GetPresetAttribute(method);
                var declaringType = method.DeclaringType;
                if (declaringType == null)
                    continue;
                if (!dict.TryGetValue(declaringType, out var ctx))
                {
                    var name = declaringType.Name;
                    ctx = new Context( name + "MenuItems", 
                        Path.Combine(Resources.PackageEditorPath, name + ".g.cs"), 
                        declaringType.Namespace);
                    dict.Add(declaringType, ctx);
                }
                ctx.AddMethod(method, attribute, valueType);
            }

            foreach (var value in dict.Values)
            {
                value.End();
                var content = value.ToString();
                SourceUtils.Generate(value.path, () => value.ToString(), Debug.unityLogger);
            }
        }

        private static InputPresetAttribute GetPresetAttribute(MethodInfo method) =>
            (InputPresetAttribute)method.GetCustomAttributes(typeof(InputPresetAttribute)).FirstOrDefault();

        private static bool ValidateMethod(MethodInfo method, out Type valueType)
        {
            // Requirement: A preset annotated method must be static TODO Move to analyzer
            if (!method.IsStatic)
            {
                Debug.LogError($"Invalid preset method {method}. Method must be static.");
                valueType = null;
                return false;
            }

            // Requirement: A preset annotated method must be non-void TODO Move to analyzer
            if (method.ReturnParameter == null)
            {
                Debug.LogError($"Invalid return type. Expected non-void type implementing IObservableInput&lt;T&gt;.");
                valueType = null;
                return false;
            }
            
            valueType = method.ReturnParameter.ParameterType.GetObservableInputValueType();
            return valueType != null;
        }
    }

    public static class TypeExtensions
    {
        public static bool IsObservableInputInterface(this Type type) => 
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservableInput<>);

        public static Type GetObservableInputValueType(this Type type)
        {
            if (type == null) 
                return null;
            if (type.IsObservableInputInterface()) 
                return type.GenericTypeArguments[0];
            foreach (var @interface in type.GetInterfaces())
            {
                if (IsObservableInputInterface(@interface))
                    return @interface.GenericTypeArguments[0];
            }
            return null;
        }
    }
}