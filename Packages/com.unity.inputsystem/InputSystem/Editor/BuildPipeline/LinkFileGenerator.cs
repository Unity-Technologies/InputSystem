#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.UnityLinker;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    public class LinkFileGenerator : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            //AppDomain.CurrentDomain.GetAssemblies()
            //var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var typesByAssemblies = new Dictionary<System.Reflection.Assembly, Type[]>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    //var assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(unityAssembly.outputPath);
                    var types = assembly.GetTypes().Where(x => ShouldPreserveType(x)).ToArray();
                    if (types.Length > 0)
                       typesByAssemblies.Add(assembly, types);
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.LogWarning($"Couldn't load types from assembly: {assembly.FullName}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("<linker>");

            foreach (var assembly in typesByAssemblies.Keys.OrderBy(a => a.GetName().Name))
            {
                sb.AppendLine($"  <assembly fullname=\"{assembly.GetName().Name}\">");

                var types = typesByAssemblies[assembly];
                foreach (var type in types.OrderBy(t => t.FullName))
                    sb.AppendLine(
                        $"    <type fullname=\"{FormatForXml(ToCecilName(type.FullName))}\" preserve=\"all\"/>");

                sb.AppendLine("  </assembly>");
            }

            sb.AppendLine("</linker>");

            var filePathName = Path.Combine(data.inputDirectory, "InputSystemStripping.xml");
            File.WriteAllText(filePathName, sb.ToString());
            return filePathName;
        }
        
        static bool IsTypeUsedViaReflectionByInputSystem(Type type)
        {
            return type.IsSubclassOf(typeof(InputControl)) ||
                   typeof(IInputStateTypeInfo).IsAssignableFrom(type) ||
                   typeof(IInputInteraction).IsAssignableFrom(type) ||
                   typeof(InputProcessor).IsAssignableFrom(type) ||
                   typeof(InputBindingComposite).IsAssignableFrom(type) ||
                   type.GetCustomAttributes<InputControlAttribute>().Any();
        }

        static bool IsFieldInfoControlLayoutRelated(FieldInfo field)
        {
            return IsTypeUsedViaReflectionByInputSystem(field.GetType()) ||
                   field.GetCustomAttributes<InputControlAttribute>().Any();
        }

        static bool IsPropertyInfoControlLayoutRelated(PropertyInfo property)
        {
            return IsTypeUsedViaReflectionByInputSystem(property.GetType()) ||
                   property.GetCustomAttributes<InputControlAttribute>().Any();
        }

        static bool ShouldPreserveType(Type type)
        {
            if (IsTypeUsedViaReflectionByInputSystem(type))
                return true;

            foreach (var field in type.GetFields())
                if (IsFieldInfoControlLayoutRelated(field))
                    return true;

            foreach (var property in type.GetProperties())
                if (IsPropertyInfoControlLayoutRelated(property))
                    return true;

            return false;
        }

        static string ToCecilName(string fullTypeName)
        {
            return fullTypeName.Replace('+', '/');
        }

        static string FormatForXml(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
    }
}
#endif