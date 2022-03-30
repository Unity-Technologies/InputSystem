// Tests for the UnityEngine.Input API.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

partial class CoreTests
{
    #if UNITY_EDITOR
    [Test]
    [Category("API")]
    public void API_SettingsAreStoredInInputManagerAsset()
    {
        // Get rid of InputTestFixture to switch us back to the "live" system.
        TearDown();

        Assert.That(AssetDatabase.GetAssetPath(InputSystem.settings),
            Is.EqualTo("ProjectSettings/InputManager.asset"));
    }

    #endif

    //Q: under what circumstances do we migrate InputManager axis settings instead?
    [Test]
    [Category("API")]
    public void API_GlobalActionsDefaultToDefaultInputActionAsset()
    {
        Assert.That(InputSystem.actions.Select(a => $"{a.actionMap.name}/{a.name}"),
            Is.EquivalentTo(new DefaultInputActions().asset.Select(a => $"{a.actionMap.name}/{a.name}")));
        Assert.That(InputSystem.actions.SelectMany(a => a.bindings),
            Is.EquivalentTo(new DefaultInputActions().asset.SelectMany(a => a.bindings)));
    }

    [Test]
    [Category("API")]
    public void API_GlobalActionsAreAutomaticallyEnabled()
    {
        Assert.That(InputSystem.actions.enabled, Is.True);
    }

    //really?
    [Test]
    [Category("API")]
    public void API_CanSwitchGlobalActionsOnTheFly()
    {
        Assert.Fail();
    }
}
