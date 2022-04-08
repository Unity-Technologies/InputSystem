using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

public class ControlSchemesEditorTests
{
    [Test]
    [Category("AssetEditor")]
    public void AddRequirementCommand_AddsDeviceRequirements()
    {
        var state = new InputActionsEditorState(new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>()),
            selectedControlScheme: new InputControlScheme("Test", new[] {new InputControlScheme.DeviceRequirement {controlPath = "<Device>"}}));

        var newState = ControlSchemeCommands.AddDeviceRequirement(new InputControlScheme.DeviceRequirement
        {
            controlPath = "<Gamepad>"
        })(in state);

        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.EqualTo(2));
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].m_ControlPath, Is.EqualTo("<Gamepad>"));
    }

    [Test]
    [Category("AssetEditor")]
    public void RemoveRequirementCommand_RemovesDeviceRequirements()
    {
        var state = new InputActionsEditorState(new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>()),
            selectedControlScheme: new InputControlScheme("Test", new[]
            {
                new InputControlScheme.DeviceRequirement {controlPath = "<Device>"},
                new InputControlScheme.DeviceRequirement {controlPath = "<Device2>"}
            }));

        var newState = ControlSchemeCommands.RemoveDeviceRequirement(0)(in state);

        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.EqualTo(1));
        Assert.That(newState.selectedControlScheme.deviceRequirements[0].m_ControlPath, Is.EqualTo("<Device2>"));

        newState = ControlSchemeCommands.RemoveDeviceRequirement(0)(in newState);

        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.Zero);
    }

    [Test]
    [Category("AssetEditor")]
    public void ChangeRequirementCommand_ChangesSelectedRequirement()
    {
        var state = new InputActionsEditorState(new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>()),
            selectedControlScheme: new InputControlScheme("Test", new[]
            {
                new InputControlScheme.DeviceRequirement {controlPath = "<Device>", isOptional = false},
                new InputControlScheme.DeviceRequirement {controlPath = "<Device2>", isOptional = true}
            }));

        var newState = ControlSchemeCommands.ChangeDeviceRequirement(0, true)(in state);
        newState = ControlSchemeCommands.ChangeDeviceRequirement(1, false)(in newState);

        Assert.That(newState.selectedControlScheme.deviceRequirements[0].isOptional, Is.False);
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].isOptional, Is.True);
    }

    [Test]
    [Category("AssetEditor")]
    public void AddNewControlSchemeCommand_ClearsSelectedControlScheme()
    {
        var state = new InputActionsEditorState(new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>()),
            selectedControlScheme: new InputControlScheme("Test", new[] {new InputControlScheme.DeviceRequirement {controlPath = "<Device>"}}));

        var newState = ControlSchemeCommands.AddNewControlScheme()(in state);

        Assert.That(newState.selectedControlScheme, Is.EqualTo(new InputControlScheme("New Control Scheme")));
    }

    [Test]
    [Category("AssetEditor")]
    public void AddNewControlSchemeCommand_GeneratesUniqueControlSchemeName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(new InputControlScheme("New Control Scheme",
            new[] { new InputControlScheme.DeviceRequirement { controlPath = "<Device>" } }));
        var state = new InputActionsEditorState(new SerializedObject(asset));

        var newState = ControlSchemeCommands.AddNewControlScheme()(in state);

        Assert.That(newState.selectedControlScheme.name, Is.EqualTo("New Control Scheme1"));
    }

    [Test]
    [Category("AssetEditor")]
    public void SaveControlSchemeCommand_PersistsControlSchemeInSerializedObject()
    {
        var inputControlScheme = new InputControlScheme("Test", new List<InputControlScheme.DeviceRequirement>
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>", isOptional = true},
            new InputControlScheme.DeviceRequirement { controlPath = "<Keyboard>", isOptional = false}
        });

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var state = new InputActionsEditorState(new SerializedObject(asset), selectedControlScheme: inputControlScheme);


        ControlSchemeCommands.SaveControlScheme()(in state);


        state.serializedObject.Update();

        var serializedArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
        var deviceRequirements = serializedArray.FirstOrDefault().FindPropertyRelative(nameof(InputControlScheme.m_DeviceRequirements));
        var first = deviceRequirements.GetArrayElementAtIndex(0);
        var second = deviceRequirements.GetArrayElementAtIndex(1);

        Assert.That(serializedArray.arraySize, Is.EqualTo(1));
        Assert.That(serializedArray.FirstOrDefault().FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue, Is.EqualTo("Test"));

        Assert.That(first.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_ControlPath)).stringValue, Is.EqualTo("<Gamepad>"));
        Assert.That(first.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_Flags)).enumValueIndex,
            Is.EqualTo((int)InputControlScheme.DeviceRequirement.Flags.Optional));

        Assert.That(second.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_ControlPath)).stringValue, Is.EqualTo("<Keyboard>"));
        Assert.That(second.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_Flags)).enumValueIndex,
            Is.EqualTo((int)InputControlScheme.DeviceRequirement.Flags.None));
    }

    [Test]
    [Category("AssetEditor")]
    public void SaveControlSchemeCommand_EnsuresUniqueControlSchemeName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(new InputControlScheme("Test", Array.Empty<InputControlScheme.DeviceRequirement>()));

        var state = new InputActionsEditorState(new SerializedObject(asset),
            selectedControlScheme: new InputControlScheme("Test"));


        var newState = ControlSchemeCommands.SaveControlScheme()(in state);


        var serializedControlScheme = newState.serializedObject
            .FindProperty(nameof(InputActionAsset.m_ControlSchemes))
            .FirstOrDefault(sp => sp.FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue == "Test1");
        Assert.That(serializedControlScheme, Is.Not.Null);
    }

    [Test]
    [Category("AssetEditor")]
    public void SaveControlSchemeCommand_SelectsNewControlSchemeAfterSaving()
    {
        // create state with multiple control schemes
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(new InputControlScheme("Scheme0", new[]
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>" }
        }));
        asset.AddControlScheme(new InputControlScheme("Scheme1", new[]
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Keyboard>" }
        }));
        var state = new InputActionsEditorState(new SerializedObject(asset),
            selectedControlScheme: new InputControlScheme("Scheme2"));


        var newState = ControlSchemeCommands.SaveControlScheme()(in state);


        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(2));
    }

    [Test]
    [Category("AssetEditor")]
    public void WhenControlSchemeIsSelected_SelectedControlSchemeIndexIsSet()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(new InputControlScheme("Test1"));
        asset.AddControlScheme(new InputControlScheme("Test2"));
        var state = new InputActionsEditorState(new SerializedObject(asset));


        var newState = ControlSchemeCommands.SelectControlScheme(1)(in state);


        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("AssetEditor")]
    public void WhenControlSchemeIsSelected_SelectedControlSchemeIsPopulatedWithSelection()
    {
        var inputControlScheme = new InputControlScheme("Test", new List<InputControlScheme.DeviceRequirement>
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>", isOptional = true},
            new InputControlScheme.DeviceRequirement { controlPath = "<Keyboard>", isOptional = false}
        });

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(inputControlScheme);
        var state = new InputActionsEditorState(new SerializedObject(asset));
        var stateContainer = new StateContainer(new VisualElement(), state);


        var newState = ControlSchemeCommands.SelectControlScheme(0)(in state);


        Assert.That(newState.selectedControlScheme, Is.EqualTo(inputControlScheme));
    }

    [Test]
    [Category("AssetEditor")]
    public void DuplicateControlSchemeCommand_CreatesCopyOfControlSchemeWithUniqueName()
    {
        var inputControlScheme = new InputControlScheme("Test", new List<InputControlScheme.DeviceRequirement>
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>", isOptional = true},
        });

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(inputControlScheme);
        var state = new InputActionsEditorState(new SerializedObject(asset),
            selectedControlScheme: inputControlScheme);


        var newState = ControlSchemeCommands.DuplicateSelectedControlScheme()(in state);


        Assert.That(newState.selectedControlScheme.name, Is.EqualTo("Test1"));
        Assert.That(newState.selectedControlScheme.deviceRequirements, Is.EqualTo(new InputControlScheme.DeviceRequirement[]
        {
            new InputControlScheme.DeviceRequirement {controlPath = "<Gamepad>", isOptional = true}
        }));
    }

    [Test]
    [Category("AssetEditor")]
    public void DeleteControlSchemeCommand_DeletesSelectedControlScheme()
    {
        var inputControlScheme = new InputControlScheme("Test", new List<InputControlScheme.DeviceRequirement>
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>", isOptional = true},
        });

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddControlScheme(inputControlScheme);
        var state = new InputActionsEditorState(new SerializedObject(asset),
            selectedControlScheme: inputControlScheme);


        var newState = ControlSchemeCommands.DeleteSelectedControlScheme()(in state);


        state.serializedObject.Update();
        var serializedArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
        Assert.That(serializedArray.arraySize, Is.Zero);
        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(-1));
        Assert.That(newState.selectedControlScheme, Is.EqualTo(new InputControlScheme()));
    }

    [Test]
    [TestCase(3, 1, 1, "Test2")]
    [TestCase(3, 2, 1, "Test1")]
    [TestCase(1, 0, -1, null)]
    [Category("AssetEditor")]
    public void DeleteControlSchemeCommand_SelectsAnotherControlSchemeAfterDelete(
        int controlSchemeCount,
        int selectedControlSchemeIndex,
        int expectedNewSelectedControlSchemeIndex,
        string expectedNewSelectedControlSchemeName)
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        for (var i = 0; i < controlSchemeCount; i++)
        {
            asset.AddControlScheme(new InputControlScheme($"Test{i}"));
        }
        var state = new InputActionsEditorState(new SerializedObject(asset),
            selectedControlScheme: asset.controlSchemes[selectedControlSchemeIndex]);


        var newState = ControlSchemeCommands.DeleteSelectedControlScheme()(in state);


        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(expectedNewSelectedControlSchemeIndex));
        Assert.That(newState.selectedControlScheme.name, expectedNewSelectedControlSchemeName != null
            ? Is.EqualTo(expectedNewSelectedControlSchemeName)
            : Is.EqualTo(null));
    }

    [Test]
    [Category("AssetEditor")]
    public void ReorderDeviceRequirementsCommand_ChangesTheOrderOfTheSpecifiedRequirements()
    {
        var inputControlScheme = new InputControlScheme("Test", new List<InputControlScheme.DeviceRequirement>
        {
            new InputControlScheme.DeviceRequirement { controlPath = "<Gamepad>"},
            new InputControlScheme.DeviceRequirement { controlPath = "<Keyboard>"}
        });

        var state = new InputActionsEditorState(new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>()),
            selectedControlScheme: inputControlScheme);


        var newState = ControlSchemeCommands.ReorderDeviceRequirements(1, 0)(in state);


        Assert.That(newState.selectedControlScheme.deviceRequirements[0].controlPath, Is.EqualTo("<Keyboard>"));
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].controlPath, Is.EqualTo("<Gamepad>"));
    }
}
