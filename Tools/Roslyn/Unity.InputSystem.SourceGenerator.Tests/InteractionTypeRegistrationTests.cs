namespace Unity.InputSystem.SourceGenerator.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)] // Safe to run all tests in parallel
public class InteractionTypeRegistrationTests
{
    private static Task Verify(string source) => TestHelper.Verify(new InputInteractionRegistration(), 
        source, typeof(object), typeof(UnityEngine.InputSystem.IInputInteraction));

    [Test] public Task ShouldDoNothing_ForEmptySource() => Verify(string.Empty);
    
    [Test] public Task ShouldDoNothing_IfInternalClassImplementsInterface() => 
        Verify(@"using UnityEngine.InputSystem; class MyInteraction : IInputInteraction { }");
    
    [Test] public Task ShouldDoNothing_IfNestedPublicClassImplementInterfaceInsideConstrainedScope() =>
        Verify(@"using UnityEngine.InputSystem; 
class Internal 
{
    public class MyProcessor : IInputInteraction { }
}");

    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassImplementsInterfaceViaUsing() => 
        Verify(@"using UnityEngine.InputSystem; public class MyInteraction : IInputInteraction { }");
    
    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassImplementsInterface() => 
        Verify(@"public class MyInteraction : UnityEngine.InputSystem.IInputInteraction { }");
}