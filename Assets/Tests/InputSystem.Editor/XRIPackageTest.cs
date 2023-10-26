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
	
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        XRRemoveRequest = Client.Remove("com.unity.xr.interaction.toolkit");
        EditorApplication.update += RemoveProgress;
        while (!XRRemoveRequest.IsCompleted)
        {
            yield return null;
        }
    }    	

    [UnityTest]    
    [Category("Integration")]
    public IEnumerator AddingXRIPackageThrowsNoErrors()
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

        Assert.That(true);                           
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

    void HandleLog(string logString, string stackTrace, LogType type) {
        
        if (type != LogType.Log) {
            Assert.That(false);
        }   
    }
}