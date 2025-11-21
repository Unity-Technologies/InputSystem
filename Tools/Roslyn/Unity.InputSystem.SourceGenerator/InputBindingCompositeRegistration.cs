using Microsoft.CodeAnalysis;

namespace Unity.InputSystem.SourceGenerator;

/// <summary>
/// Source generator that registers public types derived from InputBindingComposite.
/// </summary>
[Generator]
public sealed class InputBindingCompositeRegistration() : TypeRegistrationGenerator(Base, Template, 
    static (symbol, baseSymbol) => Helpers.IsAcceptedAssembly(symbol) &&
                                   Helpers.IsEffectivelyPublic(symbol) && 
                                   Helpers.IsOrInheritsFrom(symbol, baseSymbol))
{
    private const string Base = "UnityEngine.InputSystem.InputBindingComposite";
    private const string Template = RegistrationTemplateBegin + 
                                    "InputSystem.RegisterBindingComposite(typeof(@T), null);" +
                                    RegistrationTemplateEnd;
}