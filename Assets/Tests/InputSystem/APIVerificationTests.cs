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
        return type.Namespace.StartsWith("UnityEngine.InputSystem") || type.Name == "<Module>";
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
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.ReturnType.Name == "IntPtr");
        Assert.That(intptrMethods, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_MethodParameterTypesAreNotIntPtr()
    {
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.Parameters.Any(p => p.ParameterType.Name == "IntPtr"));
        Assert.That(intptrMethods, Is.Empty);
    }
}
#endif
