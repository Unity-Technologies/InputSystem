---
uid: input-system-set-up-test-fixtures
---

# Set up test fixtures

Use [`InputTestFixture`](xref:UnityEngine.InputSystem.InputTestFixture) to create an isolated version of the Input System for tests. The fixture sets up a blank, default-initialized version of the Input System for each test, and restores the Input System to its original state after the test completes. The default-initialized version has all built-in registrations (such as layout and processors), but doesn't have any pre-existing Input Devices.

> [!NOTE]
> [`InputTestFixture`](xref:UnityEngine.InputSystem.InputTestFixture) will not have custom registrations performed from Unity startup code such as `[InitializeOnLoad]` or `[RuntimeInitializeOnLoadMethod]`. Layouts needed during tests have to be manually registered as part of the test setup.

You can use the fixture as a base class for your own fixture:

```CSharp
class MyTests : InputTestFixture
{
    [Test]
    public void CanPressButtonOnGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Press(gamepad.buttonSouth);
    }

    // If you need custom setup and tear-down logic, override the methods inherited
    // from InputTestFixture.
    // IMPORTANT: If you use NUnit's [Setup] and [TearDown] attributes on methods in your
    //            test fixture, this will *override* the methods inherited from
    //            InputTestFixture and thus cause them to not get executed. Either
    //            override the methods as illustrated here or call the Setup() and
    //            TearDown() methods of InputTestFixture explicitly.
    public override void Setup()
    {
        base.Setup();
        // Add setup code here.
    }
    public override void TearDown()
    {
        // Add teardown code here.
        base.TearDown();
    }
}
```

> [!IMPORTANT]
> If you do this, do __not__ add a `[SetUp]` or `[TearDown]` method. Doing so will cause the methods in [`InputTestFixture`](xref:UnityEngine.InputSystem.InputTestFixture) to not be called, thus leading to the test fixture not properly initializing or shutting down. Instead, override the `Setup` and/or `TearDown` method inherited from `InputTestFixture`.

Alternatively, you can instantiate it in your fixture:

```CSharp
[TestFixture]
class MyTestFixture
{
    private InputTestFixture input = new InputTestFixture();

    // NOTE: You have to manually call Setup() and TearDown() in this scenario.

    [SetUp]
    void Setup()
    {
        input.Setup();
    }

    [TearDown]
    void TearDown()
    {
        input.TearDown();
    }
}
```

This is especially useful when creating a larger setup for game testing using `PrebuiltSetup`.

```CSharp
[PrebuildSetup("GameTestPrebuildSetup")]
public class GameTestFixture
{
    public Game game { get; set; }
    public InputTestFixture input { get; set; }

    public Mouse mouse { get; set; }
    public Keyboard keyboard { get; set; }
    public Touchscreen touchscreen { get; set; }
    public Gamepad gamepad { get; set; }

    //...
}

#if UNITY_EDITOR
public class GameTestPrebuildSetup : IPrebuildSetup
{
    public void Setup()
    {
        UnityEditor.EditorBuildSettings.scenes = new[]
        {
            new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
        };
    }
}
#endif
```

Note that you do __not__ generally need to clean up any input-related data you set up. This includes devices you add, layouts you registered, [`InputSettings`](xref:UnityEngine.InputSystem.InputSystem) you modify, and any other alteration to the state of [`InputSystem`](xref:UnityEngine.InputSystem.InputSystem). [`InputTestFixture`](xref:UnityEngine.InputSystem.InputTestFixture) will automatically throw away the current state of the Input System and restore the state from before the test was started.
