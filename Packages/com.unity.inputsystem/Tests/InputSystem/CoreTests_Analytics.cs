// We always send analytics in the editor (though the actual sending may be disabled in Pro) but we
// only send analytics in the player if enabled.
#if UNITY_ANALYTICS || UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Input;

////TODO: restricting startup event to first run after installation (in player only)

partial class CoreTests
{
    [Test]
    [Category("Analytics")]
    public void Analytics_RegistersEventsWhenInitialized()
    {
        var receivedNames = new List<string>();
        var receivedMaxPerHours = new List<int>();
        var receivedMaxPropertiesPerEvents = new List<int>();

        testRuntime.onRegisterAnalyticsEvent =
            (name, maxPerHour, maxPropertiesPerEvent) =>
            {
                receivedNames.Add(name);
                receivedMaxPerHours.Add(maxPerHour);
                receivedMaxPropertiesPerEvents.Add(maxPropertiesPerEvent);
            };

        // The test fixture has already initialized the input system.
        // Create a new manager to test registration.
        var manager = new InputManager();
        manager.Initialize(testRuntime);

        Assert.That(receivedNames,
            Is.EquivalentTo(new[]
        {
            InputAnalytics.kEventStartup, InputAnalytics.kEventFirstUserInteraction, InputAnalytics.kEventShutdown
        }));
        Assert.That(receivedMaxPerHours.Count, Is.EqualTo(3));
        Assert.That(receivedMaxPropertiesPerEvents.Count, Is.EqualTo(3));
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesStartupEventOnFirstUpdate()
    {
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesEventOnFirstUserInteraction()
    {
    }

    [Test]
    [Category("Analytics")]
    public void Analytics_ReceivesEventOnShutdown()
    {
    }
}
#endif // UNITY_ANALYTICS || UNITY_EDITOR
