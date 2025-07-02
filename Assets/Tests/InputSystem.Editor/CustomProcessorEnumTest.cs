#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS && UNITY_6000_0_OR_NEWER

using System;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

internal enum SomeEnum
{
    OptionA = 10,
    OptionB = 20
}

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
internal class CustomProcessor : InputProcessor<float>
{
    public SomeEnum SomeEnum;

#if UNITY_EDITOR
    static CustomProcessor()
    {
        Initialize();
    }

#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InputSystem.RegisterProcessor<CustomProcessor>();
    }

    public override float Process(float value, InputControl control)
    {
        return value;
    }
}

internal class CustomProcessorEnumTest : UIToolkitBaseTestWindow<InputActionsEditorWindow>
{
    InputActionAsset m_Asset;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        m_Asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();

        var actionMap = m_Asset.AddActionMap("Action Map");

        actionMap.AddAction("Action", InputActionType.Value, processors: "Custom(SomeEnum=10)");
    }

    public override void OneTimeTearDown()
    {
        AssetDatabaseUtils.Restore();
        base.OneTimeTearDown();
    }

    public override IEnumerator UnitySetup()
    {
        m_Window = InputActionsEditorWindow.OpenEditor(m_Asset);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProcessorEnum_ShouldSerializeByValue_WhenSerializedToAsset()
    {
        // Serialize current asset to JSON, and check that initial JSON contains default enum value for OptionA
        var json = m_Window.currentAssetInEditor.ToJson();

        Assert.That(json.Contains("Custom(SomeEnum=10)"), Is.True,
            "Serialized JSON does not contain the expected custom processor string for OptionA.");

        // Query the dropdown with exactly two enum choices and check that the drop down is present in the UI
        var dropdownList = m_Window.rootVisualElement.Query<DropdownField>().Where(d => d.choices.Count == 2).ToList();
        Assume.That(dropdownList.Count > 0, Is.True, "Enum parameter dropdown not found in the UI.");

        // Determine the new value to be set in the dropdown, focus the dropdown before dispatching the change
        var dropdown = dropdownList.First();
        var newValue = dropdown.choices[1];
        dropdown.Focus();
        dropdown.value = newValue;

        // Create and send a change event from OptionA to OptionB
        var changeEvent = ChangeEvent<Enum>.GetPooled(SomeEnum.OptionA, SomeEnum.OptionB);
        changeEvent.target = dropdown;
        dropdown.SendEvent(changeEvent);

        // Find the save button in the window, focus and click the save button to persist the changes
        var saveButton = m_Window.rootVisualElement.Q<Button>("save-asset-toolbar-button");
        Assume.That(saveButton, Is.Not.Null, "Save Asset button not found in the UI.");
        saveButton.Focus();
        SimulateClickOn(saveButton);

        Assert.That(dropdown.value, Is.EqualTo(newValue));

        // Verify that the updated JSON contains the new enum value for OpitonB
        var updatedJson = m_Window.currentAssetInEditor.ToJson();
        Assert.That(updatedJson.Contains("Custom(SomeEnum=20)"), Is.True, "Serialized JSON does not contain the updated custom processor string for OptionB.");

        yield return null;
    }
}
#endif
