using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Threading.Tasks;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    // Note that importing or removing a package also triggers a domain reload,
    // which causes any state or event listeners to be lost. If something works
    // for the first package in the list, but not others, a domain reload might be the culprit.
    public class Batch
    {
        internal const string packageListCommand = "-packages=";
        internal static AddRequest AddPackageRequest;
        internal static ListRequest ListPackageRequest;

        private static List<string> packageList;

        // Code for testing batch functions in Editor:
        private const string testString = "com.unity.render-pipelines.high-definition-config@9.0.0-preview.35 com.unity.render-pipelines.universal@9.0.0-preview.35";
        private const string removeTestString = "com.unity.entities com.unity.ugui com.unity.remote-config";

        public static void AddPackagesFromString(string packageSet = testString)
        {
            packageSet = "-packages=" + packageSet;
            parseArgs(packageSet);
            AddPackages();
        }

        public static void RemovePackagesFromString(string packageSet = removeTestString)
        {
            packageSet = "-packages=" + packageSet;
            parseArgs(packageSet);
            RemovePackages();
        }

        public static void GenerateDocsFromString(string packageSet = testString)
        {
            packageSet = "-packages=" + packageSet;
            parseArgs(packageSet);
            GenerateDocs();
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public async static void AddPackages()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            parseArgs();

            EditorApplication.LockReloadAssemblies();
            foreach (var package in packageList)
            {
                Debug.Log("Adding " + package);
                await AddPackageToProject(package);
            }
            EditorApplication.UnlockReloadAssemblies();
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public async static void RemovePackages()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            parseArgs();

            EditorApplication.LockReloadAssemblies();
            foreach (var package in packageList)
            {
                Debug.Log("Removing " + package);
                await RemovePackage(package);
            }
            EditorApplication.UnlockReloadAssemblies();
        }
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public async static void GenerateDocs()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            GlobalSettings.ServeAfterGeneration = false;
            parseArgs();

            PackageCollection allPackageInfos = await GetInfo();
            
            foreach (var packageInfo in allPackageInfos)
            {
                var packageId = packageInfo.packageId.Contains("@file:") ? packageInfo.packageId.Split('@')[0] : packageInfo.packageId;
                if (packageList.Contains(packageId))
                {
                    Debug.Log($"Generating docs for {packageInfo.name}.");
                    GlobalSettings.PackageInformation = packageInfo;
                    GlobalSettings.ServeAfterGeneration = false;
                    GeneratePackageDoc(packageInfo);
                }
            }
        }

        private static async Task<PackageInfo> AddPackageToProject(string packageID)
        {
            AddRequest request = Client.Add(packageID);
            while (!request.IsCompleted)
            {
                await Task.Yield();
            }
            if (request.Status == StatusCode.Failure)
                Debug.LogError("Adding " + packageID + " failed with error code " + request.Error.errorCode + ": " + request.Error.message);
            return request.Result;
        }

        private static async Task<RemoveRequest> RemovePackage(string packageID)
        {
            RemoveRequest request = Client.Remove(packageID);
            while (!request.IsCompleted)
            {
                await Task.Yield();
            }
            if(request.Status == StatusCode.Failure)
                Debug.LogError("Removing " + packageID + " failed with error code " + request.Error.errorCode + ": " + request.Error.message);
            return request;
        }

        private static async Task<PackageCollection> GetInfo()
        {
            ListRequest request = Client.List(true, true);
            while (!request.IsCompleted)
            {
                await Task.Yield();
            }

            if (request.Status == StatusCode.Failure)
                Debug.LogError("Listing packages failed with error code " + request.Error.errorCode + ": " + request.Error.message);

            return request.Result;
        }

        private static void parseArgs(string test = "")
        {
            if (packageList == null)
                packageList = new List<string>(8);

            var args = System.Environment.GetCommandLineArgs();

            if (test != string.Empty)
            {
                args = new string[1];
                args[0] = test;
            }

            foreach (var arg in args)
            {
                if (arg.StartsWith(packageListCommand, System.StringComparison.CurrentCulture))
                    packageList.AddRange(parsePackageArgString(arg));
            }
        }

        private static string[] parsePackageArgString(string arg)
        {
            return arg.Substring(packageListCommand.Length).Split(new Char[] { ' ', ',', ':', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void GeneratePackageDoc(PackageInfo packageInfo)
        {

            // Get latest version
            string latestShortVersionId = null;
            string latestAbsoluteVersionId = null;    // Can be a preview
            if (!string.IsNullOrEmpty(packageInfo.versions.latest))
            {
                latestShortVersionId = PacmanUtils.GetShortVersionId(packageInfo.name, PacmanUtils.LatestRelease(packageInfo));
                latestAbsoluteVersionId = PacmanUtils.GetShortVersionId(packageInfo.name, packageInfo.versions.latest);
            }

            string shortVersionId = PacmanUtils.GetShortVersionId(packageInfo.name, packageInfo.version);

            var buildLog = Documentation.Instance.GenerateFullSite(packageInfo, shortVersionId,
                packageInfo.source == PackageSource.Embedded, latestShortVersionId, latestAbsoluteVersionId, false,
                false);
            Validator.Validate(buildLog);
        }

    }
}
