using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unity.InputSystem.SourceGenerator.Tests;

static class TestHelper
{
    private class DefaultDiagnosticFilter
    {
        private readonly DiagnosticSeverity _diagnosticSeverity;

        public DefaultDiagnosticFilter(DiagnosticSeverity minSeverity = DiagnosticSeverity.Warning)
        {
            _diagnosticSeverity = minSeverity;
        }

        public bool Accept(Diagnostic diagnostic)
        {
            return diagnostic.Severity >= _diagnosticSeverity && 

                // We want to ignore "error CS5001: Program does not contain a static 'Main' method suitable for an entry point"
                // since its expected for this scenario.
                diagnostic.Id != "CS5001";
        }
    }
    
    public static IEnumerable<Diagnostic> FilterDiagnostics(ImmutableArray<Diagnostic> diagnostics, 
        Predicate<Diagnostic> filter)
    {
        for (int i = 0; i < diagnostics.Length; ++i)
        {
            if (filter(diagnostics[i]))
                yield return diagnostics[i];
        }
    }
    
    public static string DiagnosticsToString(ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics == null)
            return string.Empty;
        if (diagnostics.Length == 0)
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < diagnostics.Length; ++i)
        {
            if (i > 0)
                builder.Append('\n');
            builder.Append(diagnostics[i]);
        }
        return builder.ToString();
    }

    // public static Task Verify<T>(string source) where T : IIncrementalGenerator, new() => 
    //     Verify(new T(), source);
    
    // public static Task Verify(IIncrementalGenerator generator, string source, bool checkDriverResult = true)
    // {
    //     return Verify(generator, source, ImmutableArray.Create<MetadataReference>(), checkDriverResult);
    // }
    
    public static Task Verify(IIncrementalGenerator generator, string source, 
        IEnumerable<MetadataReference> references, bool checkDriverResult = true)
    {
        // Configure custom compilation to include optional references
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        // Run generators
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        
        // Assert that compilation was successful and did not generate diagnostics.
        // Note that warnings and errors related to not being a complete program may be expected.
        // E.g. warning CS0649: Field 'MyInputStruct.length' is never assigned to, and will always have its default value 0.
        var compilationDiagnostics = compilation.GetDiagnostics();
        if (compilationDiagnostics.Length > 0)
        {
            var filter = new DefaultDiagnosticFilter();
            var filteredDiagnostics = FilterDiagnostics(compilationDiagnostics, 
                (d) => filter.Accept(d)).ToImmutableArray();
            Assert.That(filteredDiagnostics.Length, Is.EqualTo(0), 
                DiagnosticsToString(filteredDiagnostics));
        }

        // Optionally check returned driver result (This may generate warnings for referenced assemblies if any)
        if (checkDriverResult)
        {
            var result = driver.GetRunResult();
            Assert.That(result.Diagnostics.Length, Is.EqualTo(0));
        }

        // Pass the driver to Verify for output verification
        return Verifier.Verify(driver);
    }
    
    public static Task Verify(IIncrementalGenerator generator, string source, params Type[] types)
    {
        // Construct references to assembly locations and then utilize referenced assemblies to solve
        // problem with referencing "implementation assemblies" instead of "reference assemblies".
        var referencedAssemblies = Assembly.GetEntryAssembly()!.GetReferencedAssemblies();
        var references = new List<PortableExecutableReference>(types.Length + referencedAssemblies.Length);
        foreach (var type in types)
            references.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        foreach (var assembly in referencedAssemblies)
            references.Add(MetadataReference.CreateFromFile(Assembly.Load(assembly).Location));

        return Verify(generator, source, references);
    }
}
