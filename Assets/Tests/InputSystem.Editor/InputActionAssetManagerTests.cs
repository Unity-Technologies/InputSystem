#if UNITY_EDITOR

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Utilities;

class InputActionAssetManagerEditorTests
{
    [TearDown]
    public void TearDown()
    {
        AssetDatabaseUtils.Restore();
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_ThrowsIfCreatedFromNullReference()
    {
        Assert.Throws<NullReferenceException>(() =>
        {
            using (var x = new InputActionAssetManager(null))
            {
            }
        });
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_ThrowsIfCreatedFromNonPersistedAsset()
    {
        InputActionAsset asset = null;
        try
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            Assert.Throws<Exception>(() =>
            {
                using (var x = new InputActionAssetManager(asset))
                {
                }
            });
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_SerializedObjectIsAWorkingCopyOfImportedAsset()
    {
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        using (var sut = new InputActionAssetManager(asset))
        {
            Assert.That(sut.serializedObject.targetObject == null, Is.False);
            Assert.That(sut.serializedObject.targetObject == asset, Is.False);
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_isDirtyReflectsWhetherWorkingCopyIsDifferentFromImportedAsset()
    {
        // Start from an asset, since we haven't done modifications expect it to not be marked dirty
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        using (var sut = new InputActionAssetManager(asset))
        {
            Assert.That(sut.dirty, Is.False);

            // Add a map to editable object and apply changes, expect it to not be dirty since we haven't explicitly updated dirty state
            var mapProperty = InputActionSerializationHelpers.AddActionMap(sut.serializedObject);
            sut.ApplyChanges();
            Assert.That(sut.dirty, Is.False); // TODO Note: dirty flag is not updated only by applying changes

            // Update the dirty state, finally expect it to indicate that it has changed
            sut.UpdateAssetDirtyState();
            Assert.That(sut.dirty, Is.True);

            // Remove the map we previously added and apply changes, expect it to still be dirty since we haven't explicitly updated dirty state
            var editedAsset = sut.serializedObject.targetObject as InputActionAsset;
            InputActionSerializationHelpers.DeleteActionMap(sut.serializedObject, editedAsset.actionMaps[0].id);
            sut.ApplyChanges();
            Assert.That(sut.dirty, Is.True);

            // Update the dirty state, expect it to be false since even though we carried out changes we are now back on square one
            sut.UpdateAssetDirtyState();
            Assert.That(sut.dirty, Is.False);
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_CanMoveAssetOnDisk()
    {
        const string filename = "InputAsset." + InputActionAsset.Extension;
        var directoryBeforeMove = AssetDatabaseUtils.CreateDirectory();
        var directoryAfterMove = AssetDatabaseUtils.RandomDirectoryPath();

        const string kDefaultContents = "{}";
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>(directoryPath: directoryBeforeMove, filename: filename, content: kDefaultContents);

        using (var inputActionAssetManager = new InputActionAssetManager(asset))
        {
            inputActionAssetManager.Initialize();

            AssetDatabaseUtils.ExternalMoveFileOrDirectory(directoryBeforeMove, directoryAfterMove); // EDIT
            AssetDatabase.Refresh();

            inputActionAssetManager.SaveChangesToAsset();

            var path = AssetDatabase.GetAssetPath(asset);
            var fileContents = File.ReadAllText(path);
            Assert.AreNotEqual(kDefaultContents, fileContents, "Expected file contents to have been modified after SaveChangesToAsset was called.");
            Assert.That(path, Is.EqualTo(directoryAfterMove + $"/{filename}"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_CanDeleteAssetOnDisk()
    {
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();

        using (var inputActionAssetManager = new InputActionAssetManager(asset))
        {
            inputActionAssetManager.Initialize();

            AssetDatabaseUtils.ExternalDeleteFileOrDirectory(AssetDatabase.GetAssetPath(asset));
            AssetDatabase.Refresh();

            // Expecting SaveChangesToAsset to throw when asset no longer exist
            Assert.Throws<Exception>(() => inputActionAssetManager.SaveChangesToAsset());
        }
    }

    [Test]
    [Category("Editor")]
    [Description("A regression test for ISXB-1406")]
    public void Editor_InputActionAssetManager_ActionsCodeGeneration_TypeNamesAreNotAffectedByCultureChange()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("tr-TR");

        var name = CSharpCodeHelpers.MakeTypeName("info");

        Thread.CurrentThread.CurrentCulture = culture;

        Assert.AreEqual('I', name[0], "Unexpected first letter in a type name.");
    }
}

#endif // UNITY_EDITOR
