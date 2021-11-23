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
    /// <summary>
    /// Input system uses runtime reflection to instantiate and discover some capabilities like layouts, processors, interactions, etc.
    /// Managed linker on high stripping modes is very keen on removing parts of classes or whole classes.
    /// One way to preserve the classes is to put [Preserve] on class itself and every field/property we're interested in,
    /// this was proven to be error prone as it's easy to forget an attribute and tedious as everything needs an attribute now.
    ///
    /// Instead this LinkFileGenerator inspects all types in the domain, and if they could be used via reflection,
    /// we preserve them in all entirety.
    ///
    /// In a long run we would like to remove usage of reflection all together, and then this mechanism will be gone too.
    ///
    /// Beware, this uses "AppDomain.CurrentDomain.GetAssemblies" which returns editor assemblies,
    /// but not all classes are available on all platforms, most of platform specific code is wrapped into defines like
    /// "#if UNITY_EDITOR || UNITY_IOS || PACKAGE_DOCS_GENERATION", and when compiling for Android,
    /// that particular class wouldn't be available in the final executable, though our link.xml here would still specify it,
    /// potentially creating linker warnings that we need to later ignore.
    /// </summary>
    internal class LinkFileGenerator : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var currentAssemblyName = typeof(UnityEngine.InputSystem.InputSystem).Assembly.GetName().Name;

            var typesByAssemblies = new Dictionary<System.Reflection.Assembly, Type[]>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    // Skip any assembly that doesn't reference the input system assembly.
                    if (assembly.GetName().Name != currentAssemblyName && !assembly
                        .GetReferencedAssemblies().Any(x => x.Name == currentAssemblyName))
                        continue;

                    var types = assembly.GetTypes().Where(ShouldPreserveType).ToArray();
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

            var filePathName = Path.Combine(Application.dataPath, "..", "Temp", "InputSystemLink.xml");
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

        static bool IsFieldRelatedToControlLayouts(FieldInfo field)
        {
            return IsTypeUsedViaReflectionByInputSystem(field.GetType()) ||
                field.GetCustomAttributes<InputControlAttribute>().Any();
        }

        static bool IsPropertyRelatedToControlLayouts(PropertyInfo property)
        {
            return IsTypeUsedViaReflectionByInputSystem(property.GetType()) ||
                property.GetCustomAttributes<InputControlAttribute>().Any();
        }

        static bool ShouldPreserveType(Type type)
        {
            if (IsTypeUsedViaReflectionByInputSystem(type))
                return true;

            foreach (var field in type.GetFields())
                if (IsFieldRelatedToControlLayouts(field))
                    return true;

            foreach (var property in type.GetProperties())
                if (IsPropertyRelatedToControlLayouts(property))
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
