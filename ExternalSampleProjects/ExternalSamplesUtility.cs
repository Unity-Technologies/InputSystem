using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Net;
using System.Diagnostics;
using UnityEngine.InputSystem;

public class ExternalSamplesUtility
{
    static string token => System.Environment.GetEnvironmentVariable("GITHUB_AUTH");

    struct ReleaseResponse
    {
        public string upload_url;
    }

    static string CallGithubAPI(string commandUrl, string contentType, string requestData = null)
    {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(commandUrl);
        httpWebRequest.ContentType = contentType;
        httpWebRequest.Method = requestData != null ? "POST" : "GET";
        httpWebRequest.Accept = "application/vnd.github.v3+json";
        httpWebRequest.Headers["Authorization"] = $"token {token}";
        httpWebRequest.UserAgent = "Unity";
        httpWebRequest.Timeout = 1000000;

        if (requestData != null)
        {
            var stream = httpWebRequest.GetRequestStream();
            if (contentType == "application/binary")
            {
                var data = File.ReadAllBytes(requestData);
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            else
            {
                using (var streamWriter = new StreamWriter(stream))
                {
                    streamWriter.Write(requestData);
                }
            }
        }

        try
        {
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                UnityEngine.Debug.Log("Server said " + result);
                return result;
            }
        }
        catch (WebException e)
        {
            UnityEngine.Debug.Log($"Failed request {commandUrl} with data: '{requestData}'. {e}");
            using (var streamReader = new StreamReader(e.Response.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                UnityEngine.Debug.Log("Server said " + result);
            }
            throw;
        }
    }

    static void RunGitCommannd(string arguments)
    {
        Process p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = "git";
        p.StartInfo.Arguments = arguments;
        p.Start();
        p.WaitForExit();
    }

    static string[] GetAssetsToPublish()
    {
        return
            Directory.GetFileSystemEntries("Assets").Where(x => !x.Contains("ExternalSamplesUtility"))
                .Concat(Directory.GetFileSystemEntries("ProjectSettingsBackup").Select(x => x.Replace("ProjectSettingsBackup", "ProjectSettings")))
                .ToArray();
    }

    public static void DryRun()
    {
        var packagePath = $"{PlayerSettings.productName}-{InputSystem.version}.unitypackage";
        AssetDatabase.ExportPackage(GetAssetsToPublish(), packagePath, ExportPackageOptions.Recurse);
        UnityEngine.Debug.Log($"Created package at {packagePath}");
        UnityEngine.Debug.Log("Done!");
        EditorApplication.Exit(0);
    }

    public static void Publish()
    {
        var packagePath = $"{PlayerSettings.productName}-{InputSystem.version}.unitypackage";
        AssetDatabase.ExportPackage(GetAssetsToPublish(), packagePath, ExportPackageOptions.Recurse);
        UnityEngine.Debug.Log($"Created package at {packagePath}");

        RunGitCommannd($"tag {InputSystem.version}");
        RunGitCommannd($"push --tags https://stefanunity:{token}@github.com/Unity-Technologies/InputSystem.git");

        string uploadUrl;

        try
        {
            var checkResponse = CallGithubAPI($"https://api.github.com/repos/Unity-Technologies/InputSystem/releases/tags/{InputSystem.version}", "application/json");
            uploadUrl = JsonUtility.FromJson<ReleaseResponse>(checkResponse).upload_url;
            uploadUrl = uploadUrl.Replace("{?name,label}", $"?name={packagePath}");
        }
        catch (WebException e)
        {
            if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotFound)
            {
                var createResponse = CallGithubAPI(
                    "https://api.github.com/repos/Unity-Technologies/InputSystem/releases",
                    "application/json",
                    $"{{\"tag_name\": \"{InputSystem.version}\",\n\"target_commitish\": \"develop\",\n\"name\": \"{InputSystem.version}\",\n\"body\": \"\",\n\"draft\": false,\n\"prerelease\": true\n}}"
                );

                uploadUrl = JsonUtility.FromJson<ReleaseResponse>(createResponse).upload_url;
                uploadUrl = uploadUrl.Replace("{?name,label}", $"?name={packagePath}");
            }
            else
                throw;
        }
        var uploadResponse = CallGithubAPI(uploadUrl, "application/binary", packagePath);

        UnityEngine.Debug.Log("Done!");
        EditorApplication.Exit(0);
    }
}
