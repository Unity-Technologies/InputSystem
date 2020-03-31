using System.Diagnostics;
using System.IO;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal static class ZipUtils
    {
        private static string Get7zPath()
        {
#if (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
            string execFilename = "7za";
#else
            string execFilename = "7z.exe";
#endif
            string zipper = EditorApplication.applicationContentsPath + "/Tools/" + execFilename;
            if (!File.Exists(zipper))
                throw new FileNotFoundException("Could not find " + zipper);
            return zipper;
        }

        internal static bool Unzip(string zipFilePath, string destPath)
        {
            string zipper = Get7zPath();
            string inputArguments = string.Format("x -y -o\"{0}\" \"{1}\"", destPath, zipFilePath);
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.FileName = zipper;
            startInfo.Arguments = inputArguments;
            startInfo.RedirectStandardError = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new IOException(string.Format("Failed to unzip:\n{0} {1}\n\n{2}", zipper, inputArguments, process.StandardError.ReadToEnd()));

            return true;
        }
    }
}
