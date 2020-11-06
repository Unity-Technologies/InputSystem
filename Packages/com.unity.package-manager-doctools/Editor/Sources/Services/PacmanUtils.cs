using System;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    public class PacmanUtils
    {
        internal static string GetShortVersionId(string packageName, string version)
        {
            var shortVersions = version.Split('.');
            if (shortVersions.Length < 3)
                throw new Exception("Semver has in invalid format: " + version);

            var shortVersion = string.Format("{0}.{1}", shortVersions[0], shortVersions[1]);
            var shortVersionId = string.Format("{0}@{1}", packageName, shortVersion);

            return shortVersionId;
        }

        // Get the latest version so that we can always redirect from the web to the latest version
        //         eg: Don't use preview version in the returned list, unless there is no other versions
        internal static string LatestRelease(PackageInfo packageInfo)
        {
            var latestRelease = packageInfo.versions.all.Where(v => !v.Contains("-"));
            if (!latestRelease.Any())
                return packageInfo.versions.latest;

            return latestRelease.LastOrDefault() ?? string.Empty;
        }

        internal static List<string> GetVersions(string packageName)
        {
            var versions = new List<string>(32);

            using (WebClient wc = new WebClient())
            {
                try
                {
                    var json = wc.DownloadString("https://packages.unity.com/" + packageName);
                    var metadata = JObject.Parse(json);

                    var versionHistory = metadata["time"].ToObject<Dictionary<string, DateTime>>();
                    foreach (KeyValuePair<string, DateTime> version in versionHistory.OrderByDescending(key => key.Value))
                    {
                        versions.Add(version.Key);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }

            return versions;
        }
    }
}