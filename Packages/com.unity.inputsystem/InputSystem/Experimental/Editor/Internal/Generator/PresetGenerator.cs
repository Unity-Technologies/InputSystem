using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace UnityEditor.InputSystem.Experimental.Generator
{
    public static class PresetGenerator
    {
        // TODO This is temporary during development and should be removed
        [UnityEditor.MenuItem("Debug/Generate Presets Code")]
        public static void GenerateMenuItem() => Run();

        private sealed class Context
        {
            private readonly string m_Path;
            private readonly string m_PresetIdentifier;
            private readonly string m_SourceNamespace;
            private readonly List<Item> m_Items;

            private struct Item
            {
                public MethodInfo Method;
                public InputPresetAttribute Attribute;
                public Type ValueType;
            }
            
            public Context(string presetIdentifier, string path, string sourceNamespace)
            {
                m_Items = new List<Item>();
                m_Path = path;
                m_PresetIdentifier = presetIdentifier;
                m_SourceNamespace = sourceNamespace;
            }
            
            public void AddMethod(MethodInfo method, InputPresetAttribute attribute, Type valueType)
            {
                m_Items.Add(new Item(){ Method = method, Attribute = attribute, ValueType = valueType});
            }

            public string path => m_Path;

            public override string ToString()
            {
                var b = new SourceBuilder(m_Path);
                
                b.WriteLine(SourceUtils.Header);
                b.WriteLine($"using {m_SourceNamespace};");
                b.NewLine();
                b.WriteLine("namespace UnityEditor.InputSystem.Experimental");
                b.BeginScope();
                b.WriteLine($"internal static class {m_PresetIdentifier}");
                b.BeginScope();

                WriteItem(b, m_Items[0]);
                for (var i = 1; i < m_Items.Count; ++i)
                {
                    b.NewLine();
                    WriteItem(b, m_Items[i]);
                }
                
                b.EndScope();
                b.EndScope();

                return b.ToString();
            }

            private void WriteItem(SourceBuilder b, in Item item)
            {
                var presetMethodName = item.Method.Name;
                var presetName = item.Attribute.displayName ?? presetMethodName;
                var presetClass = item.Method.DeclaringType!.Name;
                var presetCategory = item.Attribute.category;
                //var valueTypeName = valueType.Name;
                //WriteLine($"\t\t[MenuItem(Editor.Resources.InputBindingAssetPresetMenu + \"{presetCategory}/{presetName} ({valueTypeName})\")]");
                b.WriteLine($"[MenuItem(Resources.InputBindingAssetPresetMenu + \"{presetCategory}/{presetName}\")]");
                b.WriteLine($"public static void {presetMethodName}() => {presetClass}.{presetMethodName}().CreateAssetFromName(\"{presetMethodName}\");");
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
                ctx.AddMethod(method, GetPresetAttribute(method), valueType);
            }

            foreach (var value in dict.Values)
            {
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