#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS && UNITY_6000_0_OR_NEWER

using System;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Processors;
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

    [Test]
    [Description("Regression test for case ISXB-1674")]
    public void Migration_ShouldProduceValidActionAsset_WithEnumProcessorConverted()
    {
        const string k_Json = @"
            {
                ""name"": ""InputSystem_Actions"",
                ""maps"": [
                    {
                        ""name"": ""Player"",
                        ""id"": ""df70fa95-8a34-4494-b137-73ab6b9c7d37"",
                        ""actions"": [
                            {
                                ""name"": ""Move"",
                                ""type"": ""Value"",
                                ""id"": ""938a78a0-f8c6-4b2e-b8a7-d3c26d83a4e9"",
                                ""expectedControlType"": ""Vector2"",
                                ""processors"": ""StickDeadzone,InvertVector2(invertX=false),Custom(SomeEnum=1)"",
                                ""interactions"": """",
                                ""initialStateCheck"": true
                            }
                        ],
                        ""bindings"": [
                            {
                                ""name"": """",
                                ""id"": ""c9a175a0-a5ed-4e2c-b3a9-1d4d3d3a7a9a"",
                                ""path"": ""<Gamepad>/leftStick"",
                                ""interactions"": """",
                                ""processors"": """",
                                ""groups"": """",
                                ""action"": ""Move"",
                                ""isComposite"": false,
                                ""isPartOfComposite"": false
                            }
                        ]
                    }
                ],
                ""controlSchemes"": []
            }";

        var asset = InputActionAsset.FromJson(k_Json);

        // Enable the action to call rebinding
        asset.FindAction("Move").Enable();

        var map = asset.FindActionMap("Player");

        // Directly tests the outcome of the migration and parsing logic, which is the core goal.
        var instantiatedProcessors = map.m_State.processors;
        Assert.That(map.m_State.totalProcessorCount, Is.EqualTo(3), "Should create exactly three processors.");

        // Verify the order and type of each processor.
        Assert.That(instantiatedProcessors[0], Is.TypeOf<StickDeadzoneProcessor>(), "First processor should be StickDeadzone.");
        Assert.That(instantiatedProcessors[1], Is.TypeOf<InvertVector2Processor>(), "Second processor should be InvertVector2.");
        Assert.That(instantiatedProcessors[2], Is.TypeOf<CustomProcessor>(), "Third processor should be the custom type.");

        // Verify the specific data for processors with parameters.
        var invertProcessor = (InvertVector2Processor)instantiatedProcessors[1];
        Assert.That(invertProcessor.invertX, Is.False, "invertX parameter should be false.");

        var customProcessor = (CustomProcessor)instantiatedProcessors[2];
        Assert.That(customProcessor.SomeEnum, Is.EqualTo(SomeEnum.OptionB), "Enum parameter should be parsed correctly to OptionB.");
    }
}
#endif
