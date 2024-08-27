#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

internal class UGUITests
{
    Scene m_Scene;
    [SetUp]
    public void SetUp()
    {
        // Ensure that the scene is clean before starting the test
        m_Scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.CloseScene(m_Scene, true);
    }

    [Test]
    [Category("UGUITests")]
    // This test checks that when the Input System is enabled the EventSystem GameObject is created with the
    // InputSystemUIInputModule component.
    public void UGUITests_Editor_EventSystemGameObjectUsesUIInputModule()
    {
        m_Scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        // Creates the EventSystem GameObject using the Editor menu
        string menuItem = "GameObject/UI/Event System";
        Assert.AreEqual(2, m_Scene.rootCount);
        EditorApplication.ExecuteMenuItem(menuItem);
        Assert.AreEqual(3, m_Scene.rootCount);

        // Get the EventSystem GameObject from the scene to check that it has the correct input module
        var rootGameObjects = m_Scene.GetRootGameObjects();
        GameObject eventSystem = rootGameObjects[2].GetComponent<EventSystem>().gameObject;

        Assert.IsNotNull(eventSystem);
        Assert.IsNotNull(eventSystem.GetComponent<InputSystemUIInputModule>());
    }
}

#endif
