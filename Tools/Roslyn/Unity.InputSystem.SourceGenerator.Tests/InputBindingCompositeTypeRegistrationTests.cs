namespace Unity.InputSystem.SourceGenerator.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)] // Safe to run all tests in parallel
public class InputBindingCompositeTypeRegistrationTests
{
    private static Task Verify(string source) => TestHelper.Verify(new InputBindingCompositeRegistration(), 
        source, typeof(object), typeof(UnityEngine.InputSystem.InputBindingComposite));

    [Test] public Task ShouldDoNothing_ForEmptySource() => Verify(string.Empty);
    
    [Test] public Task ShouldDoNothing_IfInternalClassImplementsIInputInteraction() => Verify(string.Empty);

    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassExtendsBaseViaUsing() => 
        Verify(@"using UnityEngine.InputSystem; public class MyBindingComposite : InputBindingComposite { }");
    
    [Test] public Task ShouldGenerateRegistrationCode_IfPublicClassExtendsBase() => 
        Verify("public class MyBindingComposite : UnityEngine.InputSystem.InputBindingComposite { }");
}