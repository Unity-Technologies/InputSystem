using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

// Disable irrelevant warning about there not being underscores in method names.
#pragma warning disable CA1707

public class XRIPackageTests
{
    static AddRequest XRAddRequest;
    static RemoveRequest XRRemoveRequest;

    /// <summary>
    /// TearDown removes the added XRI package again.
    /// If you are adding a 2nd test that needs the XRI package adjust this.
    /// </summary>
    /// <returns></returns>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        XRRemoveRequest = Client.Remove("com.unity.xr.interaction.toolkit");
        EditorApplication.update += RemoveProgress;
        while (!XRRemoveRequest.IsCompleted)
        {
            yield return null;
        }

        //Delete the Assets/XRI folder (and its content) that the XRI package creates
        if (AssetDatabase.IsValidFolder("Assets/XRI"))
        {
            AssetDatabase.DeleteAsset("Assets/XRI");
        }
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator AdddingLatestXRIPackageThrowsNoErrors()
    {
        Application.logMessageReceived += HandleLog;

        XRAddRequest = Client.Add("com.unity.xr.interaction.toolkit");
        EditorApplication.update += AddProgress;

        while (!XRAddRequest.IsCompleted)
        {
            yield return null;
        }

        AssetDatabase.Refresh();

        yield return new WaitForDomainReload();
    }

    static void AddProgress()
    {
        if (XRAddRequest.IsCompleted)
        {
            if (XRAddRequest.Status == StatusCode.Success)
                Debug.Log("Installed: " + XRAddRequest.Result.packageId);
            else if (XRAddRequest.Status >= StatusCode.Failure)
                Debug.Log(XRAddRequest.Error.message);

            EditorApplication.update -= AddProgress;
        }
    }

    static void RemoveProgress()
    {
        if (XRRemoveRequest.IsCompleted)
        {
            if (XRRemoveRequest.Status == StatusCode.Success)
                Debug.Log("Removed: XRI package");
            else if (XRRemoveRequest.Status >= StatusCode.Failure)
                Debug.Log(XRRemoveRequest.Error.message);

            EditorApplication.update -= RemoveProgress;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        Assert.That(type, Is.EqualTo(LogType.Log));
    }
}
