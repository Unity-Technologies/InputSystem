#if DEVELOPMENT_BUILD || UNITY_EDITOR
using NUnit.Framework;

namespace ISX
{
    /// <summary>
    /// A test fixture for writing tests that use the input system. Can be derived from
    /// or simply instantiated from another test fixture.
    /// </summary>
    /// <remarks>
    /// The fixture will put the input system into a known state where it has only the
    /// built-in set of basic templates and no devices. The state of the system before
    /// starting a test is recorded and restored when the test finishes.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyTests : InputTestFixture
    /// {
    ///     public override void Setup()
    ///     {
    ///         base.Setup();
    ///
    ///         InputSystem.RegisterTemplate<MyDevice>();
    ///     }
    ///
    ///     [Test]
    ///     public void CanCreateMyDevice()
    ///     {
    ///         InputSystem.AddDevice("MyDevice");
    ///         Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<MyDevice>());
    ///     }
    /// }
    /// </code>
    /// </example>
    public class InputTestFixture
    {
        [SetUp]
        public virtual void Setup()
        {
            InputSystem.Save();

            ////FIXME: ATM events fired by platform layers for mice and keyboard etc.
            ////       interfere with tests; we need to isolate the system from them
            ////       during testing (probably also from native device discoveries)
            ////       Put a switch in native that blocks events except those coming
            ////       in from C# through SendEvent and which supresses flushing device
            ////       discoveries to managed

            // Put system in a blank state where it has all the templates but has
            // none of the native devices.
            InputSystem.Reset();

            #if UNITY_EDITOR
            // Make sure we're not affected by the user giving focus away from the
            // game view.
            InputConfiguration.LockInputToGame = true;
            #endif

            if (InputSystem.devices.Count > 0)
                Assert.Fail("Input system should not have devices after reset");
        }

        [TearDown]
        public virtual void TearDown()
        {
            ////REVIEW: What's the right thing to do here? ATM InputSystem.Restore() will not disable
            ////        actions and readding devices we refresh all enabled actions. That means that when
            ////        we restore, the action above will get refreshed and not find a 'test' modifier
            ////        registered in the system. Should we force-disable all actions on Restore()?
            InputSystem.DisableAllEnabledActions();

            InputSystem.Restore();
        }
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
