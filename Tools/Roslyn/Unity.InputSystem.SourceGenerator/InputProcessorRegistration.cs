// Roslyn Incremental Source Generator to support automatic registration of Input System type extensions via
// in-memory generated source performing manual registration of types.
//
// Potential further improvements:
// - Assembly filtering to narrow scope.
// - For ImplementsInterface, consider type.AllInterfaces.Contains(symbol, SymbolEqualityComparer.Default);
// - Generate diagnostic warnings for types implementing interfaces with private visibility?
// - Improve generated type name generation to guarantee no clash.

using Microsoft.CodeAnalysis;

namespace Unity.InputSystem.SourceGenerator;

/// <summary>
/// Source generator that registers public types extending InputProcessor.
/// </summary>
[Generator]
public sealed class InputProcessorRegistration() : TypeRegistrationGenerator(Base, Template, 
    static (symbol, baseSymbol) => Helpers.IsAcceptedAssembly(symbol) &&
                                   Helpers.IsEffectivelyPublic(symbol) && 
                                   Helpers.IsOrInheritsFrom(symbol, baseSymbol))
{
    private const string Base = "UnityEngine.InputSystem.InputProcessor";
    private const string Template = RegistrationTemplateBegin + 
        "InputSystem.RegisterProcessor(typeof(@T));" +
        RegistrationTemplateEnd;
}

