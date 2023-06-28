#if UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

public class ControlSchemesEditorTests
{
    [Test]
    [Category("AssetEditor")]
    public void AddRequirementCommand_AddsDeviceRequirements()
    {
        var state = TestData.editorState.Generate()
            .With(selectedControlScheme: TestData.controlScheme.WithOptionalDevice().Generate());


        var deviceRequirement = TestData.deviceRequirement.Generate();
        var newState = ControlSchemeCommands.AddDeviceRequirement(deviceRequirement)(in state);


        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.EqualTo(2));
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].m_ControlPath, Is.EqualTo(deviceRequirement.controlPath));
    }

    [Test]
    [Category("AssetEditor")]
    public void RemoveRequirementCommand_RemovesDeviceRequirements()
    {
        var controlScheme = TestData.controlScheme.WithOptionalDevices(TestData.N(TestData.deviceRequirement, 2)).Generate();
        var state = TestData.editorState.Generate().With(selectedControlScheme: controlScheme);


        var newState = ControlSchemeCommands.RemoveDeviceRequirement(0)(in state);


        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.EqualTo(1));
        Assert.That(newState.selectedControlScheme.deviceRequirements[0].m_ControlPath,
            Is.EqualTo(controlScheme.deviceRequirements[1].controlPath));


        newState = ControlSchemeCommands.RemoveDeviceRequirement(0)(in newState);


        Assert.That(newState.selectedControlScheme.deviceRequirements.Count, Is.Zero);
    }

    [Test]
    [Category("AssetEditor")]
    public void ChangeRequirementCommand_ChangesSelectedRequirement()
    {
        var state = TestData.editorState.Generate().With(
            selectedControlScheme: TestData.controlScheme.Generate()
                .WithRequiredDevice()
                .WithOptionalDevice());


        var newState = ControlSchemeCommands.ChangeDeviceRequirement(0, true)(in state);
        newState = ControlSchemeCommands.ChangeDeviceRequirement(1, false)(in newState);


        Assert.That(newState.selectedControlScheme.deviceRequirements[0].isOptional, Is.False);
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].isOptional, Is.True);
    }

    [Test]
    [Category("AssetEditor")]
    public void AddNewControlSchemeCommand_ClearsSelectedControlScheme()
    {
        var state = TestData.editorState.Generate().With(selectedControlScheme: TestData.controlScheme.Generate());


        var newState = ControlSchemeCommands.AddNewControlScheme()(in state);


        Assert.That(newState.selectedControlScheme, Is.EqualTo(new InputControlScheme("New Control Scheme")));
    }

    [Test]
    [Category("AssetEditor")]
    public void AddNewControlSchemeCommand_GeneratesUniqueControlSchemeName()
    {
        var state = TestData.EditorStateWithAsset(TestData.inputActionAsset
            .WithControlScheme(TestData.controlScheme.Select(s => s.WithName("New Control Scheme")))
            .Generate())
            .Generate();


        var newState = ControlSchemeCommands.AddNewControlScheme()(in state);


        Assert.That(newState.selectedControlScheme.name, Is.EqualTo("New Control Scheme1"));
    }

    [Test]
    [Category("AssetEditor")]
    public void SaveControlSchemeCommand_PersistsControlSchemeInSerializedObject()
    {
        var state = TestData.editorState.Generate()
            .With(selectedControlScheme: TestData.controlScheme
                .Generate()
                .WithOptionalDevice()
                .WithRequiredDevice());


        ControlSchemeCommands.SaveControlScheme()(in state);


        state.serializedObject.Update();

        var serializedArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
        var deviceRequirements = serializedArray.FirstOrDefault().FindPropertyRelative(nameof(InputControlScheme.m_DeviceRequirements));
        var first = deviceRequirements.GetArrayElementAtIndex(0);
        var second = deviceRequirements.GetArrayElementAtIndex(1);

        Assert.That(serializedArray.arraySize, Is.EqualTo(1));
        Assert.That(serializedArray.FirstOrDefault().FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue,
            Is.EqualTo(state.selectedControlScheme.name));

        Assert.That(first.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_ControlPath)).stringValue,
            Is.EqualTo(state.selectedControlScheme.deviceRequirements[0].controlPath));
        Assert.That(first.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_Flags)).enumValueIndex,
            Is.EqualTo((int)InputControlScheme.DeviceRequirement.Flags.Optional));

        Assert.That(second.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_ControlPath)).stringValue,
            Is.EqualTo(state.selectedControlScheme.deviceRequirements[1].controlPath));
        Assert.That(second.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_Flags)).enumValueIndex,
            Is.EqualTo((int)InputControlScheme.DeviceRequirement.Flags.None));
    }

    [Test]
    [Category("AssetEditor")]
    public void SaveControlSchemeCommand_EnsuresUniqueControlSchemeName()
    {
        var asset = TestData.inputActionAsset
            .WithControlScheme(TestData.controlScheme.Select(s => s.WithName("Test")))
            .Generate();
        var state = TestData.EditorStateWithAsset(asset).Generate().With(selectedControlScheme: asset.controlSchemes[0]);


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
        var state = TestData.EditorStateWithAsset(
            TestData.inputActionAsset
                .WithControlSchemes(TestData.N(TestData.controlScheme, 2))
                .Generate()
        )
            .Generate()
            .With(selectedControlScheme: TestData.controlScheme.Generate());


        var newState = ControlSchemeCommands.SaveControlScheme()(in state);


        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(2));
    }

    [Test]
    [Category("AssetEditor")]
    public void WhenControlSchemeIsSelected_SelectedControlSchemeIndexIsSet()
    {
        var state = TestData.EditorStateWithAsset(
            TestData.inputActionAsset
                .WithControlSchemes(TestData.N(TestData.controlScheme, 2))
                .Generate()
        )
            .Generate();

        var newState = ControlSchemeCommands.SelectControlScheme(1)(in state);


        Assert.That(newState.selectedControlSchemeIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("AssetEditor")]
    public void WhenControlSchemeIsSelected_SelectedControlSchemeIsPopulatedWithSelection()
    {
        var asset = TestData.inputActionAsset
            .WithControlSchemes(TestData.N(TestData.controlScheme, 2))
            .Generate();
        var state = TestData.EditorStateWithAsset(asset).Generate();


        var newState = ControlSchemeCommands.SelectControlScheme(0)(in state);


        Assert.That(newState.selectedControlScheme, Is.EqualTo(asset.controlSchemes[0]));


        newState = ControlSchemeCommands.SelectControlScheme(1)(in state);


        Assert.That(newState.selectedControlScheme, Is.EqualTo(asset.controlSchemes[1]));
    }

    [Test]
    [Category("AssetEditor")]
    public void DuplicateControlSchemeCommand_CreatesCopyOfControlSchemeWithUniqueName()
    {
        var asset = TestData.inputActionAsset.WithControlScheme(TestData.controlScheme).Generate();
        var state = TestData.EditorStateWithAsset(asset).Generate().With(selectedControlScheme: asset.controlSchemes[0]);


        var newState = ControlSchemeCommands.DuplicateSelectedControlScheme()(in state);


        Assert.That(newState.selectedControlScheme.name, Is.EqualTo(state.selectedControlScheme.name + "1"));
        Assert.That(newState.selectedControlScheme.deviceRequirements, Is.EqualTo(state.selectedControlScheme.deviceRequirements));
    }

    [Test]
    [Category("AssetEditor")]
    public void DeleteControlSchemeCommand_DeletesSelectedControlScheme()
    {
        var asset = TestData.inputActionAsset.WithControlScheme(TestData.controlScheme.WithOptionalDevice()).Generate();
        var state = TestData.EditorStateWithAsset(asset).Generate().With(selectedControlScheme: asset.controlSchemes[0]);


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
        var asset = TestData.inputActionAsset.Generate();
        for (var i = 0; i < controlSchemeCount; i++)
        {
            asset.AddControlScheme(new InputControlScheme($"Test{i}"));
        }

        var state = TestData.EditorStateWithAsset(asset)
            .Generate()
            .With(selectedControlScheme: asset.controlSchemes[selectedControlSchemeIndex]);


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
        var state = TestData.editorState.Generate()
            .With(selectedControlScheme: TestData.controlSchemeWithTwoDeviceRequirements.Generate());


        var newState = ControlSchemeCommands.ReorderDeviceRequirements(1, 0)(in state);


        Assert.That(newState.selectedControlScheme.deviceRequirements[0].controlPath,
            Is.EqualTo(state.selectedControlScheme.m_DeviceRequirements[1].controlPath));
        Assert.That(newState.selectedControlScheme.deviceRequirements[1].controlPath,
            Is.EqualTo(state.selectedControlScheme.m_DeviceRequirements[0].controlPath));
    }

    [Test]
    [Category("AssetEditor")]
    public void ChangeBindingsControlSchemesCommand_CanAddControlSchemes()
    {
        var controlScheme = TestData.controlScheme.Generate();
        var state = TestData.EditorStateWithAsset(TestData.inputActionAsset
            .Generate()
            .WithControlScheme(controlScheme))
            .Generate()
            .With(selectedControlScheme: controlScheme, selectedActionMapIndex: 0, selectedActionIndex: 0,
                selectedBindingIndex: 0);


        ControlSchemeCommands.ChangeSelectedBindingsControlSchemes("TestControlScheme", true)(in state);


        var actionMapSO = state.serializedObject
            ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
            ?.GetArrayElementAtIndex(state.selectedActionMapIndex);
        var serializedProperty = actionMapSO?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
            ?.GetArrayElementAtIndex(state.selectedBindingIndex);

        var groupsProperty = serializedProperty.FindPropertyRelative(nameof(InputBinding.m_Groups));

        Assert.That(groupsProperty.stringValue.Split(InputBinding.kSeparatorString), Contains.Item("TestControlScheme"));
    }

    [Test]
    [Category("AssetEditor")]
    public void ChangeBindingsControlSchemesCommand_CanRemoveControlSchemes()
    {
        var controlScheme = TestData.controlScheme.Generate();
        var state = TestData.EditorStateWithAsset(TestData.inputActionAsset
            .Select(a =>
            {
                a.m_ActionMaps[0].m_Bindings[0].groups = "TestControlScheme";
                return a;
            })
            .Generate()
            .WithControlScheme(controlScheme))
            .Generate()
            .With(selectedControlScheme: controlScheme, selectedActionMapIndex: 0, selectedActionIndex: 0,
                selectedBindingIndex: 0);


        ControlSchemeCommands.ChangeSelectedBindingsControlSchemes("TestControlScheme", false)(in state);


        var actionMapSO = state.serializedObject
            ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
            ?.GetArrayElementAtIndex(state.selectedActionMapIndex);
        var serializedProperty = actionMapSO?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
            ?.GetArrayElementAtIndex(state.selectedBindingIndex);

        var groupsProperty = serializedProperty.FindPropertyRelative(nameof(InputBinding.m_Groups));

        Assert.That(groupsProperty.stringValue, Is.EqualTo(string.Empty));
    }
}
#endif
