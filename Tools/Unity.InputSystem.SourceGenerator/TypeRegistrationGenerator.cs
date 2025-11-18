// Roslyn Incremental Source Generator to support automatic registration of Input System type extensions via
// in-memory generated source performing manual registration of types.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Xml.Linq;

[Generator]
public sealed class MyInterfaceGenerator : IIncrementalGenerator
{
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
        context.RegisterSourceOutput(candidateTypes, static (spc, source) =>
        {
            var (compilation, typeDecls) = source;
            Process(spc, compilation, typeDecls);
        });
    }

    private static void Process(SourceProductionContext context, Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> declarations)
    {
        var interfaceSymbol = compilation.GetTypeByMetadataName("UnityEngine.InputSystem.IInputInteraction");
        if (interfaceSymbol is null)
            return; // interface not in this compilation

        foreach (var decl in declarations)
        {
            // Get semantic model
            var model = compilation.GetSemanticModel(decl.SyntaxTree);
            if (model.GetDeclaredSymbol(decl) is not INamedTypeSymbol typeSymbol)
                continue;
            
            // Skip if evaluated symbol is not implementing interface
            if (!ImplementsInterface(typeSymbol, interfaceSymbol)) 
                continue;
            
            // Generate type registration code and add to source
            var source = GenerateFor(typeSymbol);
            context.AddSource($"{typeSymbol.Name}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol symbol)
        => type.AllInterfaces.Contains(symbol);
        //=> type.AllInterfaces.Contains(;, SymbolEqualityComparer.Default);

    private static string GenerateFor(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        return sb.ToString();
    }
}
