#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Mono.Cecil;

partial class APIVerificationTests
{
    private bool IsValidNameForConstant(string name)
    {
        return char.IsUpper(name[0]);
    }

    private bool TypeHasValidNamespace(TypeReference type)
    {
        // The compiler generates a <Module> type which we want to ignore
        return type.Namespace.StartsWith("UnityEngine.InputSystem") || type.Name == "<Module>";
    }

    // Generally, public API should always expose values as properties, and not as fields.
    // We currently have quite a few exceptions, which are handled here.
    private bool IsTypeWhichCanHavePublicFields(TypeReference type)
    {
        if (type == null)
            return false;

        // This is the base type of all structs
        if (type.FullName == typeof(ValueType).FullName)
            return false;
        if (type.FullName == typeof(Object).FullName)
            return false;

        if (
            // MonoBehaviour uses public fields for serialization
            type.FullName == typeof(UnityEngine.MonoBehaviour).FullName ||
            // MonoBehaviour-derived, but listed explicitly, as Cecil may not have the path to resolve the UI assembly.
            type.FullName == typeof(UnityEngine.EventSystems.BaseInputModule).FullName ||
            type.FullName == typeof(UnityEngine.EventSystems.EventSystem).FullName ||
            // These have fields popuplated by reflection in the Input System
            type.FullName == typeof(UnityEngine.InputSystem.InputProcessor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.InputControl).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.InputBindingComposite).FullName
        )
            return true;

        var resolved = type.Resolve();
        if (resolved == null)
            return false;

        if (
            // Interactions have fields populated by reflection in the Input System
            resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.IInputInteraction).FullName) ||

            // Input state structures use fields for the memory layout and construct Input Controls from the fields.
            resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.IInputStateTypeInfo).FullName) ||

            // These use fields for the explicit memory layout, and have a member for the base type. If we exposed that via a property,
            // base type values could not be written individually.
            resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo).FullName) ||
            resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.LowLevel.IInputEventTypeInfo).FullName) ||

            // serializable types may depend on the field names to match serialized data (eg. Json)
            resolved.Attributes.HasFlag(TypeAttributes.Serializable)
        )
            return true;

        return IsTypeWhichCanHavePublicFields(resolved.BaseType);
    }

    private IEnumerable<TypeDefinition> GetInputSystemPublicTypes()
    {
        var codeBase = typeof(UnityEngine.InputSystem.InputSystem).Assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var asmDef = AssemblyDefinition.ReadAssembly(path);
        return asmDef.MainModule.Types.Where(type => type.IsPublic);
    }

    private IEnumerable<FieldDefinition> GetInputSystemPublicFields() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Fields).Where(f => f.IsPublic);
    private IEnumerable<MethodDefinition> GetInputSystemPublicMethods() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Methods).Where(m => m.IsPublic);

    [Test]
    [Category("API")]
    public void API_ConstantsAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicFields().Where(field => field.HasConstant && !IsValidNameForConstant(field.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_StaticReadonlyFieldsAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicFields().Where(field => field.IsInitOnly && field.IsStatic && !IsValidNameForConstant(field.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }
    
    [Test]
    [Category("API")]
    public void API_EnumValuesAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicTypes().Where(t => t.IsEnum).SelectMany(t => t.Fields).Where(f => f.IsStatic && !IsValidNameForConstant(f.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_TypesHaveAnAppropriateNamespace()
    {
        var incorrectlyNamespacedTypes = GetInputSystemPublicTypes().Where(t => !TypeHasValidNamespace(t));
        Assert.That(incorrectlyNamespacedTypes, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_FieldsAreNotIntPtr()
    {
        var intptrFields = GetInputSystemPublicFields().Where(f => f.FieldType.Name == "IntPtr");
        Assert.That(intptrFields, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_MethodReturnTypesAreNotIntPtr()
    {
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.ReturnType.FullName == "System.IntPtr");
        Assert.That(intptrMethods, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_MethodParameterTypesAreNotIntPtr()
    {
        // Ignore IntPtr parameters on delegate constructors. These are generated by the compiler and not within our control
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.DeclaringType.BaseType?.FullName != "System.MulticastDelegate" && m.Parameters.Any(p => p.ParameterType.FullName == "System.IntPtr"));
        Assert.That(intptrMethods, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_DoesNotHaveDisallowedPublicFields()
    {
        var disallowedPublicFields = GetInputSystemPublicFields().Where(field => !field.HasConstant && !(field.IsInitOnly && field.IsStatic) && !IsTypeWhichCanHavePublicFields(field.DeclaringType));
        Assert.That(disallowedPublicFields, Is.Empty);
    }
}
#endif
