using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.InputSystem.Experimental.Generator;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Generator;

namespace UnityEditor.InputSystem.Experimental
{
    /// <summary>
    /// 
    /// </summary>
    /// <example>
    /// [InputDeviceReading]
    /// struct MouseReading
    /// {
    ///     
    /// }
    /// </example>
    public static class DeviceGenerator
    {
        // TODO This is temporary during development an
        [UnityEditor.MenuItem("Debug/Generate Devices")]
        public static void GenerateMenuItem() => Run();

        private struct Item
        {
            public Type Type;
            public InputDeviceReadingAttribute ReadingAttribute;
        }
        
        private static void Run()
        {
            foreach (var item in GetItems())
                Generate(item);
        }
        
        private static IEnumerable<Item> GetItems()
        {
            var types = TypeCache.GetTypesWithAttribute<InputDeviceReadingAttribute>();
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<InputDeviceReadingAttribute>();
                yield return new Item{ Type = type, ReadingAttribute = attribute };
            }
        }
        
        private static void Generate(Item item)
        {
            var path = Path.Combine(Resources.PackagePath, "Experimental/PoC.cs");
            
            var ctx = new SourceContext();
            //ctx.root.Snippet(SourceUtils.Header);
            var ns = ctx.root.Namespace(item.Type.Namespace);
            var name = item.Type.Name;
            var deviceName = item.ReadingAttribute.deviceClassName;
            
            var clazz = ns.DeclareStruct(deviceName + "2");
            clazz.visibility = Syntax.Visibility.Public;
            //clazz.ImplementInterface($"IObservableInputNode<{item.Type}>");

            var deviceId = clazz.DeclareField<ushort>("m_DeviceId");
            deviceId.visibility = Syntax.Visibility.Private;
            
            var ctor = clazz.DefineMethod(deviceName + "2", Syntax.Visibility.Private);
            ctor.isConstructor = true;
            ctor.Parameter("deviceId", typeof(ushort));
            ctor.Statement($"{deviceId.name} = deviceId");
            
            var any = clazz.DefineProperty("any", clazz);
            any.visibility = Syntax.Visibility.Public;
            // TODO Want extensions for getter and setter
            // TODO Need getter any.Statement("return new(0);");
            
            SourceUtils.Generate(path, () => ctx.ToSource());
        }
    }
}