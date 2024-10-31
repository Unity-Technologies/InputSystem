#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

internal class ProjectWideInputActionsEditorTests
{
    // Note that only a selected few tests verifies the behavior associated with the editor support for
    // creating a dedicated asset. For all other logical tests we are better off constructing an asset on
    // the fly for functional tests to avoid differences between editor and playmode tests.
    //
    // Note that player tests are currently lacking since it would require a proper asset to be configured
    // during edit mode and then built and then loaded indirectly via config object / resources.
    //
    // Note that any existing default created asset is preserved during test run by moving it via ADB.

    const string kTestCategory = "ProjectWideActions";
    const string kAssetDirectory = "Assets";
    const string kAssetBackupDirectory = kAssetDirectory + "/TestBackupFiles";
    const string kDefaultProjectWideAssetBackupPath = kAssetBackupDirectory + "/DefaultProjectWideAssetBackup.json";

    private string m_SavedAssetPath;

    private InputActionAsset m_Actions;
    private bool m_ActionsArePersisted;

    private InputActionAsset m_OtherActions;
    private bool m_OtherActionsArePersisted;

    private int m_CallbackCount;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Avoid changing the editor setting for project-wide input actions (if any). To achieve this we
        // store the asset path since we cannot only keep a reference in case its equal to moved asset.
        // If not equal to moved asset above a reference would be fine, but might as well use a more robust
        // method.
        m_SavedAssetPath = AssetDatabase.GetAssetPath(InputSystem.actions);

        // Avoid overwriting any default asset already in /Assets folder by making a backup file not visible to
        // AssetDatabase. This is for verifying the default output of templated actions from editor tools.
        if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
        {
            if (!Directory.Exists(kAssetBackupDirectory))
                Directory.CreateDirectory(kAssetBackupDirectory);
            File.Copy(sourceFileName: ProjectWideActionsAsset.defaultAssetPath,
                destFileName: kDefaultProjectWideAssetBackupPath, overwrite: true);
            var wasDeleted = AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
            Assert.That(wasDeleted, Is.True);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Restore default asset if we made a backup copy of it during setup
        if (File.Exists(kDefaultProjectWideAssetBackupPath))
        {
            if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
                AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
            File.Copy(sourceFileName: kDefaultProjectWideAssetBackupPath,
                destFileName: ProjectWideActionsAsset.defaultAssetPath);
            AssetDatabase.ImportAsset(ProjectWideActionsAsset.defaultAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabaseUtils.ExternalDeleteFileOrDirectory(kAssetBackupDirectory);
        }

        // Restore users project-wide input actions setting
        var asset = m_SavedAssetPath != null ? AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_SavedAssetPath) : null;
        InputSystem.actions = asset;
    }

    [SetUp]
    public void Setup()
    {
        TestUtils.MockDialogs();

        m_CallbackCount = 0;

        // Always start with action null since this represents a fresh project
        InputSystem.actions = null;
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.onActionsChange -= OnActionsChange;

        // Delete any default asset we may have created (backup is safe until test class is destroyed)
        AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);

        // Clean-up objects created during test
        if (m_Actions != null && !m_ActionsArePersisted)
            Object.DestroyImmediate(m_Actions);
        if (m_OtherActions != null && !m_OtherActionsArePersisted)
            Object.DestroyImmediate(m_OtherActions);

        InputSystem.actions = null;

        TestUtils.RestoreDialogs();
        AssetDatabaseUtils.Restore();
    }

    private void GivenActionsCallback()
    {
        InputSystem.onActionsChange += OnActionsChange;
    }

    private void GivenActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        m_Actions = ScriptableObject.CreateInstance<InputActionAsset>();
        m_Actions.name = "TestAsset";
        var one = m_Actions.AddActionMap("One");
        one.AddAction("A");
        one.AddAction("B");
        var two = m_Actions.AddActionMap("Two");
        two.AddAction("C");

        if (persisted)
        {
            var json = m_Actions.ToJson();
            Object.DestroyImmediate(m_Actions);
            m_Actions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            m_ActionsArePersisted = true;
        }
    }

    private void GivenOtherActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        m_OtherActions = ScriptableObject.CreateInstance<InputActionAsset>();
        m_OtherActions.name = "OtherTestAsset";
        var three = m_OtherActions.AddActionMap("Three");
        three.AddAction("D");
        three.AddAction("E");

        if (persisted)
        {
            var json = m_OtherActions.ToJson();
            Object.DestroyImmediate(m_OtherActions);
            m_OtherActions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            m_OtherActionsArePersisted = true;
        }
    }

    private void OnActionsChange()
    {
        ++m_CallbackCount;
    }

    [Test(Description = "Verifies that project-wide actions are not set by default")]
    [Category(kTestCategory)]
    public void ProjectWideActions_AreNotSetByDefault()
    {
        Assert.That(InputSystem.actions, Is.Null);
    }

    [Test(Description =
            "Verifies that when assigning InputSystem.actions actions are not enabled in edit mode")]
    [Category(kTestCategory)]
    public void
    ProjectWideActions_CanBeAssignedButAreNotEnabledInEditMode()
    {
        GivenActions(persisted: true);

        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions.enabled, Is.False);
    }

    [Test(Description =
            "Verifies that when reassigning InputSystem.actions only the last assigned asset is marked to be included in preloaded assets in player build")]
    [Category(kTestCategory)]
    public void
    ProjectWideActions_CanBeAssignedAndAreMarkedAsProjectWide()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);

        InputSystem.actions = m_Actions;
        Assert.That(m_Actions.m_IsProjectWide, Is.True);
        Assert.That(m_OtherActions.m_IsProjectWide, Is.False);

        InputSystem.actions = m_OtherActions;
        Assert.That(m_Actions.m_IsProjectWide, Is.False);
        Assert.That(m_OtherActions.m_IsProjectWide, Is.True);
    }

    [Test(Description = "Verifies that project-wide actions defaults are constructed as an asset on the default asset path")]
    [Category(kTestCategory)]
    public void ProjectWideActionsAsset_DefaultAssetFileHasDefaultContent()
    {
        // Expect asset name to be set to the file name
        var expectedName = Path.GetFileNameWithoutExtension(ProjectWideActionsAsset.defaultAssetPath);
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        Assert.That(asset.name, Is.EqualTo(expectedName));

        // Expect JSON name to be set to the file name
        var json = File.ReadAllText(EditorHelpers.GetPhysicalPath(ProjectWideActionsAsset.defaultAssetPath));
        var parsedAsset = InputActionAsset.FromJson(json);
        var parsedAssetName = parsedAsset.name;
        Object.DestroyImmediate(parsedAsset);
        Assert.That(parsedAssetName, Is.EqualTo(expectedName));
    }

    private class TestReporter : ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors
    {
        public const string kExceptionMessage = "Intentional Exception";
        public readonly List<string> messages;
        public bool throwsException;

        public TestReporter(List<string> messages = null, bool throwsException = false)
        {
            this.messages = messages;
            this.throwsException = throwsException;
        }

        public void Report(string message)
        {
            if (throwsException)
                throw new Exception(kExceptionMessage);
            messages?.Add(message);
        }
    }

    [Test(Description = "Verifies that the default asset do not generate any verification errors (Regardless of existing requirements)")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldSupportAssetVerification_AndHaveNoVerificationErrorsForDefaultAsset()
    {
        var messages = new List<string>();
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        ProjectWideActionsAsset.Verify(asset, new TestReporter(messages));
        Assert.That(messages.Count, Is.EqualTo(0));
    }

    class TestVerifier : ProjectWideActionsAsset.IInputActionAssetVerifier
    {
        public const string kFailureMessage = "Intentional failure";
        public InputActionAsset forwardedAsset;
        public bool throwsException;

        public TestVerifier(bool throwsException = false)
        {
            this.throwsException = throwsException;
        }

        public void Verify(InputActionAsset asset, ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter)
        {
            if (throwsException)
                throw new Exception(TestReporter.kExceptionMessage);
            forwardedAsset = asset;
            reporter.Report(kFailureMessage);
        }
    }

    [Test(Description = "Verifies that the default asset verification registers errors for a registered verifier)")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldSupportAssetVerification_IfVerifierHasBeenRegistered()
    {
        var messages = new List<string>();
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        var verifier = new TestVerifier();
        Func<ProjectWideActionsAsset.IInputActionAssetVerifier> factory = () => verifier;
        try
        {
            Assert.That(ProjectWideActionsAsset.RegisterInputActionAssetVerifier(factory), Is.True);
            ProjectWideActionsAsset.Verify(asset, new TestReporter(messages));
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0], Is.EqualTo(TestVerifier.kFailureMessage));
            Assert.That(verifier.forwardedAsset, Is.EqualTo(asset));
        }
        finally
        {
            Assert.That(ProjectWideActionsAsset.UnregisterInputActionAssetVerifier(factory), Is.True);
        }
    }

    [Test(Description = "Verifies that a verification factory cannot be registered twice")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldReturnError_IfFactoryHasAlreadyBeenRegisteredAndAttemptingToRegisterAgain()
    {
        Func<ProjectWideActionsAsset.IInputActionAssetVerifier> factory = () => null;
        try
        {
            Assert.That(ProjectWideActionsAsset.RegisterInputActionAssetVerifier(factory), Is.True);
            Assert.That(ProjectWideActionsAsset.RegisterInputActionAssetVerifier(factory), Is.False);
        }
        finally
        {
            Assert.That(ProjectWideActionsAsset.UnregisterInputActionAssetVerifier(factory), Is.True);
        }
    }

    [Test(Description = "Verifies that a verification factory cannot be registered twice")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldReturnError_IfAttemptingToUnregisterAFactoryThatHasNotBeenRegistered()
    {
        ProjectWideActionsAsset.IInputActionAssetVerifier Factory() => null;
        Assert.That(ProjectWideActionsAsset.UnregisterInputActionAssetVerifier(Factory), Is.False);
    }

    [Test(Description = "Verifies that a throwing reporter is handled gracefully")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldCatchAndReportException_IfReporterThrows()
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        var verifier = new TestVerifier();
        Func<ProjectWideActionsAsset.IInputActionAssetVerifier> factory = () => verifier;
        try
        {
            // Note that reporter failures shouldn't affect verification result
            Assert.That(ProjectWideActionsAsset.RegisterInputActionAssetVerifier(factory), Is.True);
            Assert.That(ProjectWideActionsAsset.Verify(asset, new TestReporter(throwsException: true)), Is.True);
        }
        finally
        {
            Assert.That(ProjectWideActionsAsset.UnregisterInputActionAssetVerifier(factory), Is.True);
        }

        LogAssert.Expect(LogType.Exception, new Regex($"{TestReporter.kExceptionMessage}"));
    }

    [Test(Description = "Verifies that a throwing verifier is handled gracefully and reported as a failure")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ShouldCatchAndReportException_IfVerifierThrows()
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        var verifier = new TestVerifier(throwsException: true);
        Func<ProjectWideActionsAsset.IInputActionAssetVerifier> factory = () => verifier;
        try
        {
            // Note that verifier failures should affect verification result
            Assert.That(ProjectWideActionsAsset.RegisterInputActionAssetVerifier(factory), Is.True);
            Assert.That(ProjectWideActionsAsset.Verify(asset, new TestReporter()), Is.False);
        }
        finally
        {
            Assert.That(ProjectWideActionsAsset.UnregisterInputActionAssetVerifier(factory), Is.True);
        }

        LogAssert.Expect(LogType.Exception, new Regex($"{TestReporter.kExceptionMessage}"));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired if value is different but not when value is not different")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(m_CallbackCount, Is.EqualTo(0));

        // Can assign asset from null to instance (change)
        InputSystem.actions = m_Actions;
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = m_Actions;
        Assert.That(m_CallbackCount, Is.EqualTo(1)); // no callback expected

        // Can assign another instance (change
        InputSystem.actions = m_OtherActions;
        Assert.That(m_CallbackCount, Is.EqualTo(2));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(m_CallbackCount, Is.EqualTo(3));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions in edit-mode, build settings are updated")]
    [Category(kTestCategory)]
    public void ProjectWideActions_WillUpdateBuildSettingsWhenChanged()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        Debug.Assert(EditorUtility.IsPersistent(m_Actions));

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));

        // Can assign asset from null to instance (change)
        InputSystem.actions = m_Actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_Actions));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = m_Actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_Actions));

        // Can assign another instance (change
        InputSystem.actions = m_OtherActions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_OtherActions));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));
    }

    [Test(Description =
            "Verifies that when assigning InputSystem.actions in edit-mode with a temporary object not persisted on disc, an exception is thrown")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ThrowsArgumentException_WhenAssignedFromNonPersistedObject()
    {
        GivenActions();

        Assert.Throws<ArgumentException>(() => InputSystem.actions = m_Actions);
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when currently being assigned to a deleted asset (destroyed object) and then assigning null")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedNullAndFiresCallback_WhenHavingDestroyedObjectAndAssignedNull()
    {
        GivenActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_Actions));
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Delete the associated asset make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when the previously assigned asset has been destroyed object")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenAssignedDestroyedObject()
    {
        GivenActions(persisted: true);
        GivenActionsCallback();

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);       // sanity check that it was destroyed

        // Assert that we can assign a destroyed object
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions == m_Actions); // note: we want to avoid cast to object since it would use another Equals
        Assert.That(!ReferenceEquals(InputSystem.actions, null)); // expecting missing reference
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when assigning and current object has been destroyed")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenAssignedAndDifferent_WhenHavingDestroyedObjectAndAssignedOther()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_Actions));
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = m_OtherActions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_OtherActions));
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }
}

#endif
