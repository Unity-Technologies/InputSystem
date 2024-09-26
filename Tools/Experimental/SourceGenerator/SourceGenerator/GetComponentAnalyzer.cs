using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GetComponentAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "GetComponentAttributeAnalyzer";
        private const string Category = "InitializationSafety";
        private static readonly LocalizableString Title = "InitializeComponents method should be called";
        private static readonly LocalizableString MessageFormat = "InitializeComponents method should be called";
        private static readonly LocalizableString Description = "InitializeComponents method should be called";
        private const string HelpLinkUri = "";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description,
            helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclarationSyntax = (FieldDeclarationSyntax)context.Node;
            var fieldSymbol = (IFieldSymbol)context.ContainingSymbol;


            if (HasGetComponentAttribute(fieldSymbol))
            {
                var classNode = fieldSymbol.ContainingType.DeclaringSyntaxReferences.FirstOrDefault()
                    ?.GetSyntax();

                foreach (var expressionSyntax in classNode.DescendantNodes()
                             .OfType<InvocationExpressionSyntax>())
                {
                    var methodSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax).Symbol as IMethodSymbol;
                    if (methodSymbol == null) continue;
                    if (methodSymbol.Name == "InitializeComponents")
                        return;
                }

                var diagnostic = Diagnostic.Create(Rule, fieldDeclarationSyntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }


        private static bool HasGetComponentAttribute(ISymbol fieldSymbol)
        {
            return fieldSymbol.GetAttributes()
                .Any(ad => ad?.AttributeClass?.ToDisplayString() == "GetComponentAttribute");
        }
    }
}