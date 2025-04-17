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

public enum SomeEnum
{
    OptionA = 10,
    OptionB = 20,
    OptionC = 50,
    OptionD = 100
}

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class CustomProcessor : InputProcessor<float>
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
        yield return base.UnitySetup();
    }
    
    [UnityTest]
    public IEnumerator CustomProcessorEnum_SerialiseTheValue_InAsset()
    {
        var json = m_Window.currentAssetInEditor.ToJson();
        
        Assert.That(json.Contains("Custom(SomeEnum=10)"), Is.True,
            "Serialized JSON does not contain the expected custom processor string for OptionA.");
        
        var dropdownList = m_Window.rootVisualElement.Query<DropdownField>().Where(d => d.choices != null && d.choices.Contains("OptionA") && d.choices.Contains("OptionB")).ToList();
        Assume.That(dropdownList.Count > 0, Is.True, "Enum parameter dropdown not found in the UI.");

        var dropdown = dropdownList.First();

        var newIndex = dropdown.choices.IndexOf("OptionB"); 
        var oldValue = dropdown.value;
        var newValue = dropdown.choices[newIndex];

        dropdown.Focus();
        dropdown.value = newValue;

        var changeEvent = ChangeEvent<Enum>.GetPooled(SomeEnum.OptionA, SomeEnum.OptionC);
        changeEvent.target = dropdown;
        dropdown.SendEvent(changeEvent);
        
        var saveButton = m_Window.rootVisualElement.Q<Button>("save-asset-toolbar-button");
        Assume.That(saveButton, Is.Not.Null, "Save Asset button not found in the UI.");
        saveButton.Focus();
        SimulateClickOn(saveButton);

        Assert.That(dropdown.value, Is.EqualTo(newValue));

        var updatedJson = m_Window.currentAssetInEditor.ToJson();
        
        Assert.That(updatedJson.Contains("Custom(SomeEnum=20)"), Is.True, "Serialized JSON does not contain the updated custom processor string for OptionB.");

        yield return null;
    }
}
#endif

