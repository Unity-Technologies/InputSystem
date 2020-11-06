
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal static class FileUtils
    {
        internal static void FileCopy(string file, string sourceDir, string destinationDir)
        {
            string fileName = file.Substring(sourceDir.Length + 1);
            FileUtil.ReplaceFile(file, Path.Combine(destinationDir, fileName));
        }

        // Need to re-create this method since Unity's FileUtil equivalent (with overwrite) is internal only
        // From: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        private static void DirectoryCopy(string SourcePath, string DestinationPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
        }

        internal static void DirectoryCopyAll(string sourceDir, string destinationDir, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDir);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destinationDir, subdir.Name);
                    DirectoryCopyAll(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        internal static void MoveAndReplaceFile(string sourcePath, string targetPath)
        {
            if (File.Exists(sourcePath))
            {
                File.Delete(targetPath); //Delete it if already exists in target dir
                File.Copy(sourcePath, targetPath);
                File.Delete(sourcePath);
            }
        }

        public static void SetFolderPermission(string folder, string permission = "+rw")
        {
            var command = "chmod";
            var arguments = string.Format("-R {1} \"{0}\"", folder, permission);

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                command = "attrib";
                arguments = string.Format(" -R \"{0}\\*.*\" /D /S", folder);
            }

            Process process = new Process();
            var startInfo = process.StartInfo;
            string processLog = "";

            try
            {
                startInfo.UseShellExecute = false;
                startInfo.FileName = command;
                startInfo.Arguments = arguments;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;

                process.Start();

                // To avoid deadlocks, always read the output stream first and then wait.
                // Also don't read to end both standard outputs.
                processLog = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(processLog + " -- " + e.Message);
            }
        }

        public static string RelativePathBelow(string currentDir, string targetPath)
        {
            var curFull = Path.GetFullPath(currentDir);
            var targetFull = Path.GetFullPath(targetPath);
            if (targetFull.StartsWith(curFull, StringComparison.CurrentCulture))
            {
                return targetFull.Substring(curFull.Length + 1);
            }
            return targetPath;
        }

        internal static bool CheckAccess(string url)
        {
            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception exc)
            {
                if(GlobalSettings.Debug)
                    UnityEngine.Debug.LogWarning("CheckAccess exception for " + url + ": " + exc.Message);
                return false;
            }

        }

        internal static bool IsDirectoryWritable(string dirPath)
        {
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Write, dirPath));
            return permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
        }

    }

}