using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Unity.InputSystem.SourceGenerator;

static class Helpers
{
    private static readonly HashSet<string> ExcludedAssemblies =
    [
        "Unity.InputSystem"
    ];

    public static bool IsAcceptedAssembly(INamedTypeSymbol symbol)
    {
        return !ExcludedAssemblies.Contains(symbol.ContainingAssembly.Identity.Name);
    }
    
    public static bool IsEffectivelyPublic(INamedTypeSymbol type)
    {
        // The type itself must be public
        if (type.DeclaredAccessibility != Accessibility.Public)
            return false;

        // Every containing type must also be public
        for (var container = type.ContainingType; 
             container is not null; 
             container = container.ContainingType)
        {
            if (container.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }
    
    public static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceSymbol)
        => type.AllInterfaces.Contains(interfaceSymbol);

    public static bool IsOrInheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseSymbol)
    {
        // If you *don't* want to match A itself, start from type.BaseType instead.
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseSymbol))
                return true;
        }

        return false;
    }
}
