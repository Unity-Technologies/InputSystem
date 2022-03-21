using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

#if true

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    [Generator]
    public class ManagedSourceGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor SourceGenerationFailedError = new DiagnosticDescriptor(
            "ISXGEN001",
            "Input source generation failed",
            "Failed due to '{0}' with stack trace '{1}'.",
            "InputSourceGeneration",
            DiagnosticSeverity.Warning,
            true);


        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        // Report as much as possible via diagnostic, because source generator runs _inside_ compiler, so any debugging gonna be hard
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var rx = (SyntaxReceiver)context.SyntaxReceiver;
                if (rx == null || rx.InlineDatabaseInstances.Count == 0)
                    throw new InvalidOperationException("Can't find any InputInlineDeviceDatabase attributes");

                var builder = new GeneratorBuilder();
                foreach (var yamlString in rx.InlineDatabaseInstances.OrderBy(x => x.priority).Select(x => x.yamlString))
                    builder.AddYAMLString(yamlString);

                var source = builder.Build().GenerateManagedSourceCode();
                if (string.IsNullOrEmpty(source))
                    throw new InvalidOperationException("Failed to generate any source");

                context.AddSource($"DeviceDatabase.InputSourceGenerated.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception e)
            {
                var stackTrace = $"{e.StackTrace.Replace('\n', ';')}";
                context.ReportDiagnostic(Diagnostic.Create(SourceGenerationFailedError, Location.None, e.Message, stackTrace));
            }
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<(string yamlString, int priority)> InlineDatabaseInstances = new List<(string yamlString, int priority)>();

            public void OnVisitSyntaxNode(SyntaxNode node)
            {
                if (!(node is AttributeSyntax attrib && attrib.Name.ToString() == "InputInlineDeviceDatabase"))
                    return;

                if (attrib.ArgumentList == null)
                    return;

                var arguments = attrib.ArgumentList.Arguments;
                if (arguments.Count < 1)
                    return;

                // TODO this is some ridiculous BS, is there a better way to get argument _values_ from the compiler? 
                var value1 = ((LiteralExpressionSyntax)arguments[0].Expression).Token.ValueText;
                var value2 = (arguments.Count >= 2) ? int.Parse(((LiteralExpressionSyntax)arguments[1].Expression).Token.ValueText) : 1;
                
                InlineDatabaseInstances.Add((value1, value2));
            }
        }
    }
}

#endif