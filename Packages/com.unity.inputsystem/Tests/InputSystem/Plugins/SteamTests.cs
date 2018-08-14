#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Steam.Editor;

public class SteamTests : InputTestFixture
{
#if UNITY_EDITOR

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateInputDeviceBasedOnSteamIGAFile()
    {
        // Create an InputActions setup and convert it to Steam IGA.
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap1 = new InputActionMap("map1");
        var actionMap2 = new InputActionMap("map2");
        actionMap1.AddAction("buttonAction", expectedControlLayout: "Button");
        actionMap1.AddAction("axisAction", expectedControlLayout: "Axis");
        actionMap1.AddAction("stickAction", expectedControlLayout: "Stick");
        actionMap2.AddAction("vector2Action", expectedControlLayout: "Vector2");

        asset.AddActionMap(actionMap1);
        asset.AddActionMap(actionMap2);

        var vdf = SteamIGAConverter.ConvertInputActionsToSteamIGA(asset);

        // Generate a C# input device from the Steam IGA file.
        var generatedCode = SteamIGAConverter.GenerateInputDeviceFromSteamIGA(vdf, "My.Namespace.MySteamController");

        Assert.That(generatedCode, Does.StartWith("// THIS FILE HAS BEEN AUTO-GENERATED"));
        Assert.That(generatedCode, Contains.Substring("#if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT"));
        Assert.That(generatedCode, Contains.Substring("namespace My.Namespace\n"));
        Assert.That(generatedCode, Contains.Substring("public class MySteamController : SteamController, IInputUpdateCallbackReceiver\n"));
        Assert.That(generatedCode, Contains.Substring("public unsafe struct MySteamControllerState : IInputStateTypeInfo\n"));
        Assert.That(generatedCode, Contains.Substring("[InitializeOnLoad]"));
        Assert.That(generatedCode, Contains.Substring("[RuntimeInitializeOnLoadMethod"));
        Assert.That(generatedCode, Contains.Substring("new FourCC('M', 'y', 'S', 't')"));
        Assert.That(generatedCode, Contains.Substring("protected override void FinishSetup(InputDeviceBuilder builder)"));
        Assert.That(generatedCode, Contains.Substring("base.FinishSetup(builder);"));
        Assert.That(generatedCode, Contains.Substring("new InputDeviceMatcher"));
        Assert.That(generatedCode, Contains.Substring("WithInterface(\"Steam\")"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanConvertInputActionsToSteamIGAFormat()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap1 = new InputActionMap("map1");
        var actionMap2 = new InputActionMap("map2");
        actionMap1.AddAction("buttonAction", expectedControlLayout: "Button");
        actionMap1.AddAction("axisAction", expectedControlLayout: "Axis");
        actionMap1.AddAction("stickAction", expectedControlLayout: "Stick");
        actionMap2.AddAction("vector2Action", expectedControlLayout: "Vector2");

        asset.AddActionMap(actionMap1);
        asset.AddActionMap(actionMap2);

        var vdf = SteamIGAConverter.ConvertInputActionsToSteamIGA(asset);
        var dictionary = SteamIGAConverter.ParseVDF(vdf);

        // Top-level key "In Game Actions".
        Assert.That(dictionary.Count, Is.EqualTo(1));
        Assert.That(dictionary, Contains.Key("In Game Actions").With.TypeOf<Dictionary<string, object>>());

        // "actions" and "localization" inside "In Game Actions".
        var inGameActions = (Dictionary<string, object>)dictionary["In Game Actions"];
        Assert.That(inGameActions, Contains.Key("actions"));
        Assert.That(inGameActions["actions"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(inGameActions, Contains.Key("localization"));
        Assert.That(inGameActions["localization"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(inGameActions.Count, Is.EqualTo(2));

        // Two action maps inside "actions".
        var actions = (Dictionary<string, object>)inGameActions["actions"];
        Assert.That(actions, Contains.Key("map1"));
        Assert.That(actions["map1"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(actions, Contains.Key("map2"));
        Assert.That(actions["map2"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(actions.Count, Is.EqualTo(2));

        // Three actions inside "map1".
        var map1 = (Dictionary<string, object>)actions["map1"];
        Assert.That(map1, Contains.Key("title"));
        Assert.That(map1, Contains.Key("StickPadGyro"));
        Assert.That(map1, Contains.Key("AnalogTrigger"));
        Assert.That(map1, Contains.Key("Button"));
        Assert.That(map1.Count, Is.EqualTo(4));
        Assert.That(map1["title"], Is.EqualTo("#Set_map1"));
        Assert.That(map1["StickPadGyro"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(map1["AnalogTrigger"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(map1["Button"], Is.TypeOf<Dictionary<string, object>>());
        var stickPadGyro1 = (Dictionary<string, object>)map1["StickPadGyro"];
        Assert.That(stickPadGyro1, Has.Count.EqualTo(1));
        Assert.That(stickPadGyro1, Contains.Key("stickAction"));
        Assert.That(stickPadGyro1["stickAction"], Is.TypeOf<Dictionary<string, object>>());
        var stickAction = (Dictionary<string, object>)stickPadGyro1["stickAction"];
        Assert.That(stickAction, Contains.Key("title"));
        Assert.That(stickAction.Count, Is.EqualTo(2));
        Assert.That(stickAction["title"], Is.EqualTo("#Action_map1_stickAction"));

        // One action inside "map2".
        var map2 = (Dictionary<string, object>)actions["map2"];
        Assert.That(map2, Contains.Key("title"));
        Assert.That(map2["title"], Is.EqualTo("#Set_map2"));

        // Localization strings.
        var localization = (Dictionary<string, object>)inGameActions["localization"];
        Assert.That(localization.Count, Is.EqualTo(1));
        Assert.That(localization, Contains.Key("english"));
        Assert.That(localization["english"], Is.TypeOf<Dictionary<string, object>>());
        var english = (Dictionary<string, object>)localization["english"];
        Assert.That(english, Contains.Key("Set_map1"));
        Assert.That(english, Contains.Key("Set_map2"));
        Assert.That(english, Contains.Key("Action_map1_buttonAction"));
        Assert.That(english, Contains.Key("Action_map1_axisAction"));
        Assert.That(english, Contains.Key("Action_map1_stickAction"));
        Assert.That(english, Contains.Key("Action_map2_vector2Action"));
        Assert.That(english["Set_map1"], Is.EqualTo("map1"));
        Assert.That(english["Set_map2"], Is.EqualTo("map2"));
        Assert.That(english["Action_map1_buttonAction"], Is.EqualTo("buttonAction"));
        Assert.That(english["Action_map1_axisAction"], Is.EqualTo("axisAction"));
        Assert.That(english["Action_map1_stickAction"], Is.EqualTo("stickAction"));
        Assert.That(english["Action_map2_vector2Action"], Is.EqualTo("vector2Action"));
        Assert.That(english.Count, Is.EqualTo(6));
    }

#endif
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
