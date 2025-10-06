using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

// Attempting to simplify code from ISXB-1637 in this file.

// What is this? Its a play-mode test defined in an editor assembly.
// This is similar to tests of input system itself. It works as expected.
public class InputTestFixtureTestsV1 : InputTestFixture
{
    private Keyboard _keyboard;

    [SetUp]
    public void Setup()
    {
        base.Setup();
        _keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.RemoveDevice(_keyboard);
        base.TearDown();
    }

    [Test]
    public void Test1()
    {
        Press(_keyboard.spaceKey);
    }

    [Test]
    public void Test2()
    {
        Press(_keyboard.spaceKey);
    }
}

// What is this? Its a play-mode test defined in an editor assembly.
// It uses OneTimeSetUp and OneTimeTearDown to create device which collides with SetUp and TearDown behavior
// of the inherited InputTestFixture.
//
// Results in (Similar to reported stack trace):
// System.ArgumentNullException : Value cannot be null.
//     Parameter name: source
//         ---
//     at (wrapper managed-to-native) Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(void*,void*,long)
// at UnityEngine.InputSystem.LowLevel.DeltaStateEvent.From (UnityEngine.InputSystem.InputControl control,
// UnityEngine.InputSystem.LowLevel.InputEventPtr& eventPtr, Unity.Collections.Allocator allocator) [0x000e5]
// in Packages/com.unity.inputsystem/InputSystem/Events/DeltaStateEvent.cs:99
public class InputTestFixtureTestsV2 : InputTestFixture
{
    private Keyboard _keyboard;

    [OneTimeSetUp]
    public void UnitySetup()
    {
        _keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [OneTimeTearDown]
    public void UnityTearDown()
    {
        InputSystem.RemoveDevice(_keyboard);
    }

    [Test]
    public void Test1()
    {
        Press(_keyboard.spaceKey);
    }

    [Test]
    public void Test2()
    {
        Press(_keyboard.spaceKey);
    }
}

// What is this? This is similar to the repro project for this bug.
public class InputTestFixtureTestsV3 : InputTestFixture
{
    private Keyboard _keyboard;

    [OneTimeSetUp]
    public void UnitySetup()
    {
        _keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [OneTimeTearDown]
    public void UnityTearDown()
    {
        InputSystem.RemoveDevice(_keyboard);
    }

    [UnityTest]
    public IEnumerator Test1()
    {
        Press(_keyboard.spaceKey);
        yield break;
    }

    [UnityTest]
    public IEnumerator Test2()
    {
        Press(_keyboard.spaceKey);
        yield break;
    }
}
