namespace Unity.InputSystem.SourceGenerator.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)] // Safe to run all tests in parallel
public class InputProcessorTypeRegistrationTests
{
    private static Task Verify(string source) => TestHelper.Verify(new InputProcessorRegistration(), 
        source, typeof(object), typeof(UnityEngine.InputSystem.InputProcessor));

    [Test] public Task ShouldDoNothing_ForEmptySource() => Verify(string.Empty);
    
    [Test] public Task ShouldDoNothing_IfInternalClassImplementsIInputInteraction() => 
        Verify(@"using UnityEngine.InputSystem; class MyProcessor : InputProcessor { }");
    
    [Test] public Task ShouldDoNothing_IfNestedPublicClassExtendsBaseInsideRestrictedScope() =>
        Verify(@"using UnityEngine.InputSystem; 
class Internal 
{
    public class MyProcessor : InputProcessor { }
}");

    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassImplementsIInputInteractionViaUsing() => 
        Verify(@"using UnityEngine.InputSystem; public class MyProcessor : InputProcessor { }");
    
    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassImplementsIInputInteraction() => 
        Verify("public class MyProcessor : UnityEngine.InputSystem.InputProcessor { }");
}