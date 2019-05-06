using System;
using System.Linq;
using NUnit.Framework;
using Mono.Cecil;
using Mono.Collections.Generic;

partial class APIVerification
{
    private bool IsValidNameForConstant(string name)
    {
        return char.IsUpper(name[0]);
    }

    private Collection<TypeDefinition> GetInputSystemTypes()
    {
        var codeBase = typeof(UnityEngine.Experimental.Input.InputSystem).Assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var asmDef = AssemblyDefinition.ReadAssembly(path);
        return asmDef.MainModule.Types;
    }

    [Test]
    [Category("API")]
    public void API_ConstantsAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemTypes().SelectMany(t => t.Resolve().Fields)
            .Where(field => field.HasConstant && field.IsPublic && field.DeclaringType.IsPublic && !IsValidNameForConstant(field.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }
}
