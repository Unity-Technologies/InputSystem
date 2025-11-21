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
/// Source generator that registers public types implementing IInputInteraction.
/// </summary>
[Generator]
public sealed class InputInteractionRegistration() : TypeRegistrationGenerator(Interface, Template,
    static (symbol, @interface) => Helpers.IsAcceptedAssembly(symbol) &&
                                   Helpers.IsEffectivelyPublic(symbol) && 
                                   Helpers.ImplementsInterface(symbol, @interface))
{
    private const string Interface = "UnityEngine.InputSystem.IInputInteraction";
    private const string Template = RegistrationTemplateBegin + 
        "InputSystem.RegisterInteraction(typeof(@T));" + 
        RegistrationTemplateEnd;
}

