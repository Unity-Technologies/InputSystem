// Roslyn Incremental Source Generator to support automatic registration of Input System type extensions via
// in-memory generated source performing manual registration of types.
//
// Potential further improvements:
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

public class TypeRegistrationGenerator : IIncrementalGenerator
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Register() => ";
    
    internal const string RegistrationTemplateEnd = @"
}
";
    
    private readonly string _interface;
    private readonly string _template;
    private readonly System.Func<INamedTypeSymbol, INamedTypeSymbol, bool> _accept;

    protected TypeRegistrationGenerator(string @interface, string template, 
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
            var fullyQualifiedTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var source = template.Replace("@C", typeSymbol.Name + "Registration")
                .Replace("@T", fullyQualifiedTypeName);
            
            // Finally, add source to compilation context
            context.AddSource($"{typeSymbol.Name}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }
}
