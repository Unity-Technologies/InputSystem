// Roslyn Incremental Source Generator to support automatic registration of Input System type extensions via
// in-memory generated source performing manual registration of types.
//
// Potential further improvements:
// - Assembly filtering to narrow scope.
// - For ImplementsInterface, consider type.AllInterfaces.Contains(symbol, SymbolEqualityComparer.Default);
// - Generate diagnostic warnings for types implementing interfaces with private visibility?
// - Improve generated type name generation to guarantee no clash.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Unity.InputSystem.SourceGenerator;

/// <summary>
/// Syntax and symbol helpers.
/// </summary>
static class Helpers
{
    public static bool IsEffectivelyPublic(INamedTypeSymbol type)
    {
        // The type itself must be public
        if (type.DeclaredAccessibility != Accessibility.Public)
            return false;

        // Every containing type must also be public
        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            if (container.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }
    
    public static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceSymbol)
        => type.AllInterfaces.Contains(interfaceSymbol);

    public static bool ExtendsClass(INamedTypeSymbol type, INamedTypeSymbol baseSymbol)
        => SymbolEqualityComparer.Default.Equals(type, baseSymbol);
    
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

[Generator]
public class InterfaceTypeRegistrationGenerator : IIncrementalGenerator
{
    internal const string RegistrationTemplateBegin = @"using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
class @C 
{
#if UNITY_EDITOR
    static @C() { Register(); }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        Debug.Log(""Auto-registering @T via source generated type @C"");";
    
    internal const string RegistrationTemplateEnd = @"
    }
}
";
    
    private readonly string _interface;
    private readonly string _template;
    private readonly System.Func<INamedTypeSymbol, INamedTypeSymbol, bool> _accept;

    protected InterfaceTypeRegistrationGenerator(string @interface, string template, 
        System.Func<INamedTypeSymbol, INamedTypeSymbol, bool> accept)
    {
        _interface = @interface;
        _template = template;
        _accept = accept;
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter syntax
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Where(t => t is not null);

        // Combine syntax with compilation so we can do symbol checks
        var candidateTypes = context.CompilationProvider.Combine(typeDeclarations.Collect());

        // Finally, register source output generator
        context.RegisterSourceOutput(candidateTypes, (spc, source) =>
        {
            var (compilation, typeDecls) = source;
            Process(spc, compilation, typeDecls, _interface, _template, _accept);
        });
    }

    private static void Process(SourceProductionContext context, Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> declarations, string @interface, string template, 
            System.Func<INamedTypeSymbol, INamedTypeSymbol, bool> accept)
    {
        var interfaceSymbol = compilation.GetTypeByMetadataName(@interface);
        if (interfaceSymbol is null)
            return; // symbol not in this compilation

        foreach (var decl in declarations)
        {
            // Get semantic model
            var model = compilation.GetSemanticModel(decl.SyntaxTree);
            if (model.GetDeclaredSymbol(decl) is not { } typeSymbol)
                continue;

            // Skip if we shouldn't accept type
            if (!accept(typeSymbol, interfaceSymbol))
                continue;
            
            // Generate type registration code and add to source
            var source = GenerateFor(typeSymbol, template);
            context.AddSource($"{typeSymbol.Name}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }


    
    private static string GenerateFor(INamedTypeSymbol type, string template)
    {
        var fullyQualifiedTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return template.Replace("@C", type.Name + "Registration").Replace("@T", fullyQualifiedTypeName);
    }
}

/// <summary>
/// Source generator that registers public types implementing IInputProcessor.
/// </summary>
[Generator]
public sealed class ProcessorRegistration() : InterfaceTypeRegistrationGenerator(Base, Template, 
    static (symbol, baseSymbol) => Helpers.IsEffectivelyPublic(symbol) && 
                                   Helpers.IsOrInheritsFrom(symbol, baseSymbol))
{
    private const string Base = "UnityEngine.InputSystem.InputProcessor";

    private const string Template = RegistrationTemplateBegin + 
        "        InputSystem.RegisterProcessor(typeof(@T));" +
        RegistrationTemplateEnd;
}

/// <summary>
/// Source generator that registers public types implementing IInputInteraction.
/// </summary>
[Generator]
public sealed class InteractionRegistration() : InterfaceTypeRegistrationGenerator(Interface, Template,
    static (symbol, @interface) => Helpers.IsEffectivelyPublic(symbol) && 
                                   Helpers.ImplementsInterface(symbol, @interface))
{
    private const string Interface = "UnityEngine.InputSystem.IInputInteraction";

    private const string Template = RegistrationTemplateBegin + 
        "        InputSystem.RegisterInteraction(typeof(@T));" + 
        RegistrationTemplateEnd;
}
