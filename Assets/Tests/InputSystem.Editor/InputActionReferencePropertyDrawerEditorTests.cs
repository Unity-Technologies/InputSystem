#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS // Mimic implementation guard

using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// Tests for custom property drawer associated with InputActionReference.
/// </summary>
internal sealed class InputActionReferencePropertyDrawerEditorTests
{
    private const string assetPath = "Assets/__InputActionReferencePropertyDrawerEditorTests.inputactions";

    private GameObject go;
    private InputActionBehaviour behaviour;
    private SerializedObject serializedObject;
    private SerializedProperty serializedProperty;
    private bool onGuiCalled;
    private bool fieldCalled;
    private EditorWindow window;

    private Object fieldObj;
    private Object fieldResult;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("Host");
        behaviour = go.AddComponent<InputActionBehaviour>();
        serializedObject = new SerializedObject(behaviour);
        serializedProperty = serializedObject.FindProperty(nameof(InputActionBehaviour.referenceAsField));
        window = EditorWindow.GetWindow<TestHostWindow>();
    }

    [TearDown]
    public void TearDown()
    {
        window.Close();
        serializedObject.Dispose();
        Object.DestroyImmediate(go);

        AssetDatabase.DeleteAsset(assetPath);
    }

    private void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map  = new InputActionMap("map");
        map.AddAction("action1");
        map.AddAction("action2");
        asset.AddActionMap(map);

        System.IO.File.WriteAllText(assetPath, asset.ToJson());
        Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(assetPath);
    }

    private Object Field(Rect position, Object obj, Type type, GUIContent label)
    {
        fieldObj = obj;
        fieldCalled = true;
        return fieldResult;
    }

    private IEnumerator RenderDrawer(InputActionReferencePropertyDrawer drawer, bool mockField = false)
    {
        onGuiCalled = false;
        fieldCalled = false;

        var container = new IMGUIContainer(() =>
        {
            var rect = new Rect(0, 0, 200, 20);
            var label = new GUIContent("Reference");
            if (mockField)
                drawer.OnGUI(rect, serializedProperty, label, Field);
            else
                drawer.OnGUI(rect, serializedProperty, label);
            onGuiCalled = true;
        });

        window.rootVisualElement.Add(container);
        window.Repaint();

        var timeoutTime = Time.realtimeSinceStartupAsDouble + 3.0;
        do
        {
            yield return null; // let Unity process a frame
        }
        while (!onGuiCalled && Time.realtimeSinceStartupAsDouble < timeoutTime);
    }

    private class TestHostWindow : EditorWindow {}

    [UnityTest]
    [Description("This is a weak test but is mainly for coverage of the regular OnGUI")]
    public IEnumerator ExecutesWithoutExceptions()
    {
        var drawer = new InputActionReferencePropertyDrawer();
        yield return RenderDrawer(drawer);
        Assert.That(onGuiCalled, Is.True);
        Assert.That(fieldCalled, Is.False);
    }

    [UnityTest]
    [Description("Verifies that when InputActionReference field is null, null is passed to Inspector field UI")]
    public IEnumerator FieldObjectIsNullWhenReferenceIsNull()
    {
        var drawer = new InputActionReferencePropertyDrawer();
        yield return RenderDrawer(drawer, true);
        Assert.That(onGuiCalled, Is.True);
        Assert.That(fieldCalled, Is.True);
        Assert.That(fieldObj, Is.Null);
    }

    [UnityTest]
    [Description("Verifies that when InputActionReference field is not null and reference is valid, " +
        "null is passed to Inspector field UI")]
    public IEnumerator FieldObjectIsReferenceWhenReferenceIsValid()
    {
        CreateAsset();
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        var action = asset.FindAction("map/action1");
        Assert.That(action, Is.Not.Null); // Sanity check

        // Simulate reference being assigned
        var reference = InputActionReference.Create(action);
        serializedProperty.objectReferenceValue = reference;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        var drawer = new InputActionReferencePropertyDrawer();
        yield return RenderDrawer(drawer, true);
        Assert.That(onGuiCalled, Is.True);
        Assert.That(fieldCalled, Is.True);
        Assert.That(fieldObj, Is.SameAs(reference));
    }

    [UnityTest]
    [Description("Verifies that when InputActionReference field is not null and reference is broken, " +
        "null is passed to Inspector field UI")]
    public IEnumerator FieldObjectIsNullWhenReferenceIsInvalid()
    {
        CreateAsset();
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        var action = asset.FindAction("map/action1");
        Assert.That(action, Is.Not.Null); // Sanity check

        // Simulate reference being assigned
        var reference = InputActionReference.Create(action);
        serializedProperty.objectReferenceValue = reference;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        // Delete the action to make the reference invalid
        asset.RemoveAction("map/action1");
        InputActionAssetManager.SaveAsset(assetPath, asset.ToJson());

        var drawer = new InputActionReferencePropertyDrawer();
        yield return RenderDrawer(drawer, true);
        Assert.That(onGuiCalled, Is.True);
        Assert.That(fieldCalled, Is.True);
        Assert.That(fieldObj, Is.Null);
    }
}

#endif // UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
