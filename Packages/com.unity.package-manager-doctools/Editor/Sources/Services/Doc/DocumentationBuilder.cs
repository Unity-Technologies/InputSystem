using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor.PackageManager.Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal class DocToolsBuildConfig
    {
        public string DefineConstants;
    }

    internal class DocumentationBuilder : IDocumentationBuilder
    {
        public virtual bool CheckForInternetConnection()
        {
            try
            {
                TcpClient client = new TcpClient("clients3.google.com", 80);
                client.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        // Package documentation template building paths
        internal static string PackageRoot {get { return Path.GetFullPath("packages/com.unity.package-manager-doctools"); }}
        internal static string PackageUIDocumentationRoot {get { return Path.Combine(PackageRoot, ".docgen"); }}
        internal static string DocFxZip { get { return Path.Combine(PackageUIDocumentationRoot, "docfx-2.40.12.7z"); } }
        internal static string DocFxRoot { get { return Path.Combine(DocumentationBuildRoot, "docfx-2.40.12"); } }
        internal static string DocFxExecutable {get { return Path.Combine(DocFxRoot, "docfx.exe"); }}
        internal static string DocFxTemplateRoot {get { return Path.Combine(PackageUIDocumentationRoot, "docfx_packages"); }}
        internal static string EditorMonoPath {get { return Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono"); }}
        internal static string MonoZip { get { return Path.Combine(PackageUIDocumentationRoot, "mono-5.16.0.7z"); } }
        internal static string MonoRootPath {get { return Path.Combine(DocumentationBuildRoot, "mono-5.16.0"); }}
        internal static string MonoPath { get; set; }

        internal static string WebPackageRoot = "http://docs.unity3d.com/Packages";
        internal static string WebPackageRootRelative = "/Packages";

        // Documentation paths
        private static string _persistentDataPath;
        internal static string PersistentDataPath
        {
            get
            {
                if (string.IsNullOrEmpty(_persistentDataPath))
                    _persistentDataPath = Application.persistentDataPath;

                return _persistentDataPath;
            }
        }

        internal static string UnityEditorDataPathRoot {get {return Path.GetFullPath(Path.Combine(PersistentDataPath, "../../unity"));}}
        internal static string DocumentationRoot {get {return Path.Combine(UnityEditorDataPathRoot, "editor/documentation");}}
        internal static string DocumentationSiteRoot {get { return Path.Combine(DocumentationRoot, "packages"); }}

        internal static string DocumentationBuildRoot
        {
            get
            {
                // Some packages have deep paths which end up exceeding windows' path name limit
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    return "c:\\temp";

                return Path.Combine(DocumentationRoot, "build");
            }
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

        bool serveCompleted;
        private Action serveCallback;
        public event Action OnServing = delegate {};

        private Thread serveThread;
        private Process serveProcess;

        private int servePort;
        private bool serving;
        private string serveLog;
        private bool stopServeLogWatch;

        internal DocumentationBuilder() {}

        string GetPackageBuildFolder(string shortVersionId)
        {
            return Path.Combine(DocumentationBuildRoot, shortVersionId);
        }

        public virtual string GetPackageSiteFolder(string shortVersionId)
        {
            return Path.Combine(DocumentationSiteRoot, shortVersionId);
        }

        private bool RunDocFx(string docFxArguments, bool serveMode = false)
        {
            //
            // Build using docfx
            Process docfxProcess = new Process();
            var startInfo = docfxProcess.StartInfo;

            if (serveMode)
                serveProcess = docfxProcess;

            if (!Directory.Exists(DocFxRoot))
            {
                ZipUtils.Unzip(DocFxZip, DocFxRoot);
            }

            var applicationExec = MonoPath;
            string arguments = string.Format("\"{0}\" {1}", DocFxExecutable, docFxArguments);
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                applicationExec = DocFxExecutable;
                arguments = docFxArguments;
            }

            string processLog = "";
            var success = true;

            try
            {
                startInfo.UseShellExecute = false;
                startInfo.FileName = applicationExec;
                startInfo.Arguments = arguments;
                startInfo.WorkingDirectory = DocFxTemplateRoot;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                if (serveMode)
                    startInfo.RedirectStandardInput = true;

                // Set our event handler to asynchronously read the sort output.
                docfxProcess.OutputDataReceived += DocFxOutputHandler;

                var startMessage = string.Format("DocFx Command line ran: \"{0}\" {1}", applicationExec, arguments);
                if (GlobalSettings.Verbose)
                    if (serveMode)
                        serveLog += "Doc Fx Serve Output: " + startMessage + "\n";
                    else
                        Debug.Log(startMessage);

                docfxProcess.Start();

                docfxProcess.BeginOutputReadLine();

                // To avoid deadlocks, always read the output stream first and then wait.
                // Also don't read to end both standard outputs.
                processLog += docfxProcess.StandardError.ReadToEnd();

                if (!serveMode)
                    docfxProcess.WaitForExit();
            }
            catch (ThreadAbortException) {}    // Don't consider this an error.
            catch (Exception e)
            {
                success = false;
                Debug.LogError(e.Message);
            }

            if (docfxProcess.HasExited && docfxProcess.ExitCode != 0)
                success = false;

            var message = string.Format("DocFx output:\n{0}", processLog);
            if (GlobalSettings.Verbose && !string.IsNullOrEmpty(processLog))
                if (serveMode)
                    serveLog += "Doc Fx Serve Output: " + message + "\n";
                else
                    Debug.Log(message);

            stopServeLogWatch = true;

            if (!success)
            {
                Debug.LogError(message);
            }

            return success;
        }

        private void DocFxOutputHandler(object sender, DataReceivedEventArgs outLine)
        {
            // Collect the docfx command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                if (!string.IsNullOrEmpty(outLine.Data.Trim()))
                    serveLog += "Doc Fx Serve Output: " + outLine.Data + "\n";

                if (outLine.Data.Contains("on http://localhost:"))
                {
                    serveCompleted = true;        // Will be stalled on next line since docfx waits for input
                }
            }
        }

        private void Serve(Action onServe = null)
        {
            if (serveThread != null)
            {
                if (serveProcess != null)
                    serveProcess.StandardInput.Write("\n");        // Any key cancels docfx serving
                serveThread.Abort();
            }

            if (servePort == 0)
                servePort = GetFreeTcpPort();

            var servePortStr = Convert.ToString(servePort);
            if (GlobalSettings.Verbose)
                Debug.Log(string.Format("Serving to port {0}", servePortStr));

            var arguments = string.Format(" serve \"{0}\" -p {1}", DocumentationSiteRoot, servePortStr);

            serveCompleted = false;
            serveLog = "";
            stopServeLogWatch = false;

            EditorApplication.update -= ServeProgress;
            EditorApplication.update -= ServeLogWatch;

            var ths = new ThreadStart(() => RunDocFx(arguments, true));

            OnServing += onServe;

            serveThread = new Thread(ths);
            serveThread.Start();

            EditorApplication.update += ServeProgress;
            EditorApplication.update += ServeLogWatch;

            serving = true;
        }

        void ServeLogWatch()
        {
            if (GlobalSettings.Verbose && !string.IsNullOrEmpty(serveLog))
            {
                Debug.Log(serveLog);
                serveLog = "";
            }

            if (stopServeLogWatch)
                EditorApplication.update -= ServeLogWatch;
        }

        void ServeProgress()
        {
            if (!serveCompleted)
                return;

            EditorApplication.update -= ServeProgress;

            OnServing();
            OnServing = delegate {};         // Clear all listeners so they don't alway get called back.
        }

        // Make sure the documentation is being served.
        // Because of domain reload, we cannot simply keep a serving process in-memory.
        private void EnsureServe(Action onServe)
        {
            if (!serving)
                Serve(onServe);
            else
                onServe();
        }

        public virtual void BuildWithProgress(string packageName, string shortVersionId, string siteFolder = null)
        {
            EditorUtility.DisplayProgressBar("Documentation", "Generating Documentation...", 0);

            try
            {
                Build(packageName, shortVersionId, siteFolder);
            }
            catch (Exception error)
            {
                Debug.LogError(error);
            }

            EditorUtility.ClearProgressBar();
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
                Debug.LogError(processLog + " -- " + e.Message);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="packageName">eg: com.unity.package-manager-ui</param>
        /// <param name="shortVersionId">eg: com.unity.package-manager-ui@1.2 (note: not @1.2.0)</param>
        ///<param name="siteFolder">Output folder where the doc site should be created.</param>
        public virtual void Build(string packageName, string shortVersionId, string siteFolder = null)
        {
            if (string.IsNullOrEmpty(siteFolder))
                siteFolder = GetPackageSiteFolder(shortVersionId);

            string packageFolder = Path.GetFullPath(string.Format("packages/{0}", packageName));
            string buildFolder = GetPackageBuildFolder(shortVersionId);
            string docfxJson = Path.Combine(buildFolder, "docfx.json");
            string logoSource = Path.Combine(buildFolder, "logo.svg");
            string logoTarget = Path.Combine(siteFolder, "logo.svg");
            string manualSource = Path.Combine(buildFolder, "manual");

            if (!Directory.Exists(MonoRootPath))
            {
                ZipUtils.Unzip(MonoZip, MonoRootPath);
            }

            //
            // Setup Mono for Mac/Linux
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                var monoPath = Path.Combine(DocumentationRoot, "mono");
                if (!Directory.Exists(monoPath))
                {
                    FileUtil.CopyFileOrDirectory(MonoRootPath, monoPath);
                    SetFolderPermission(monoPath, "777");
                }

                MonoPath = Path.Combine(monoPath, "bin/mono-sgen64");
            }

            //
            // Set target folders to initial condition
            Directory.CreateDirectory(buildFolder);

            // Clear build folder
            FileUtil.DeleteFileOrDirectory(buildFolder);

            // Clear site folder prior to building
            FileUtil.DeleteFileOrDirectory(siteFolder);

            //
            // Prepare build folder
            string packageCloneFolder = Path.Combine(buildFolder, "package");
            FileUtil.CopyFileOrDirectory(DocFxTemplateRoot, buildFolder);
            FileUtil.CopyFileOrDirectory(packageFolder, packageCloneFolder);

            // Remove ignored folders in the generated doc
            foreach (string dirPath in Directory.GetDirectories(packageCloneFolder, ".*", SearchOption.AllDirectories))
                FileUtil.DeleteFileOrDirectory(dirPath);

            foreach (string dirPath in Directory.GetDirectories(packageCloneFolder, "*~", SearchOption.AllDirectories))
                FileUtil.DeleteFileOrDirectory(dirPath);

            // When used from the cache (which is read-only), it is important to set the folder properties to write.
            SetFolderPermission(buildFolder);

            // Prepare feature doc for docfx
            string packageDocs = Path.Combine(packageFolder, "Documentation");
            string packageDocsTilde = Path.Combine(packageFolder, "Documentation~");
            string packageDocsDot = Path.Combine(packageFolder, ".Documentation");
            if (Directory.Exists(packageDocsTilde))
                packageDocs = packageDocsTilde;
            else if (Directory.Exists(packageDocsDot))
                packageDocs = packageDocsDot;

            var sourceManualFiles = Directory.GetFiles(packageDocs, "*.md", SearchOption.AllDirectories);

            if (Directory.Exists(packageDocs) && sourceManualFiles.Any())
            {
                DirectoryCopy(packageDocs, manualSource);
            }
            else
            {
                Debug.LogWarning("Package Documentation Build: Feature Documentation does not exist. An empty placeholder will be used instead");
                string manualFile = Path.Combine(manualSource, "index.md");
                File.WriteAllText(manualFile, "Information\n-----------\n<span style=\"color:red\">There is currently no documentation for this package.</span>");
            }

            var filterFilesource = Path.Combine(manualSource, "filter.yml");
            if (File.Exists(filterFilesource))
            {
                SetFolderPermission(manualSource, "777");
                var filterTarget = Path.Combine(buildFolder, "filter.yml");
                File.Delete(filterTarget);
                File.Copy(filterFilesource, filterTarget);
                File.Delete(filterFilesource);
            }

            var projectMetadataFileSource = Path.Combine(manualSource, "projectMetadata.json");
            if (File.Exists(projectMetadataFileSource))
            {
                SetFolderPermission(manualSource, "777");
                var projectMetadataFileTarget = Path.Combine(buildFolder, "projectMetadata.json");
                File.Delete(projectMetadataFileTarget);
                File.Copy(projectMetadataFileSource, projectMetadataFileTarget);
                File.Delete(projectMetadataFileSource);
            }

            // Setup the default title - replace all appearances of `DEFAULT_APP_TITLE` with the real default `PACKAGE DISPLAY NAME | VERSION`
            SetupDefaultTitle(packageFolder, buildFolder);

            // Generate table of content for manual
            var docfxToc = Path.Combine(manualSource, "toc.md");
            if (File.Exists(docfxToc))
                Debug.LogError("Your table of content file is named 'toc.md' and should be named 'TableOfContents.md'. Please rename it.");

            var docWorksToC = Path.Combine(manualSource, "TableOfContents.md");
            if (File.Exists(docWorksToC))
            {
                // DocWorks ToC support
                // Index.md is necessary for DocFx for now since it's embedded in the template.
                if (!File.Exists(Path.Combine(manualSource, "index.md")))
                {
                    Debug.LogError("Package Documentation generation error. Need an index.md when using a toc.md. Resulting site will not work.");
                    return;
                }

                docfxJson = Path.Combine(buildFolder, "docfx_toc_enabled.json");    // Force ToC to be enabled on manual page

                // Convert DocWorks style ToC to DocFx ToC format
                ConvertDocWorksToC(docWorksToC, Path.Combine(buildFolder, "manual/toc.md"));
            }
            else
            {
                //
                // Simple manual support
                var manualFiles = Directory.GetFiles(manualSource, "*.md", SearchOption.AllDirectories);
                var manualIndexCandidateFile = manualFiles.First();
                var manualIndexFile = Path.Combine(manualSource, "index.md");
                if (!File.Exists(manualIndexFile))
                    FileUtil.MoveFileOrDirectory(manualIndexCandidateFile, manualIndexFile);

                // Landing page for feature doc is alway index.md since that's required to make sure
                // that the docfx home page always redirects to this specific url.
                string ymlToc = "- name: Introduction\n  href: index.md\n";
                string manualToc = Path.Combine(manualSource, "toc.yml");
                File.WriteAllText(manualToc, ymlToc);
            }

            // Add License section
            string ymlLicenseToc = string.Format("- name: {0}\n  href: LICENSE.md\n", "License");

            // Will create an index.html that can always be linked to that will redirect either to third party license (if any) or the companion license
            string licenseIndexFile = Path.Combine(buildFolder, "license/index.md");
            string licenseIndexName = "LICENSE";

            var licenseFile = Path.Combine(packageFolder, "LICENSE.md");
            if (!File.Exists(licenseFile))
                licenseFile = Path.Combine(packageFolder, "LICENSE");

            if (File.Exists(licenseFile))
                File.Copy(licenseFile, Path.Combine(buildFolder, "license/LICENSE.md"), true);
            else
                throw new Exception("No valid LICENSE.md file found at the root of the package");

            var licenseThirdPartyFile = Path.Combine(packageFolder, "Third Party Notices.md");
            var licenseThirdPartyFileTarget = Path.Combine(buildFolder, "license/Third Party Notices.md");
            if (File.Exists(licenseThirdPartyFile))
            {
                File.Copy(licenseThirdPartyFile, licenseThirdPartyFileTarget, true);
                licenseIndexName = "Third%20Party%20Notices";
                ymlLicenseToc = "- name: Third Party Notices\n  href: Third Party Notices.md\n" + ymlLicenseToc;
            }

            File.WriteAllText(licenseIndexFile, string.Format("<script>window.location.replace('{0}.html')</script>", licenseIndexName));

            string licenseToc = Path.Combine(buildFolder, "license/toc.yml");
            File.WriteAllText(licenseToc, ymlLicenseToc);

            var versionToken = shortVersionId.Split('@')[1];
            Version version;
            Version.TryParse(versionToken, out version);

            if (packageName == "com.unity.tiny" && version < new Version(0, 15))
            {
                File.Copy(Path.Combine(buildFolder, "toc_tiny.yml"), Path.Combine(buildFolder, "toc.yml"), true);
                docfxJson = Path.Combine(buildFolder, "docfx_toc_tiny.json");
                CreateTinyRuntimeDoc(buildFolder);
            }

            // Use the changelog, otherwise keep the empty default changelog.
            var changelogFile = Path.Combine(packageFolder, "CHANGELOG.md");
            if (File.Exists(changelogFile))
                File.Copy(changelogFile, Path.Combine(buildFolder, "changelog/CHANGELOG.md"), true);

            // Generate solution file
            // This is necessary to have proper cross linking across namespaces.
            // Otherwise, the links in NamespaceA.ClassA.MemberA to NamespaceB.ClassB will not be clickable in the generated doc
            // It also allows having preprocessor defines for code generation
            var constantsDefines = "PACKAGE_DOCS_GENERATION;";
            var docToolsConfigFile = Path.Combine(packageDocs, "config.json");
            var docToolsConfig = new DocToolsBuildConfig();
            if (File.Exists(docToolsConfigFile))
                docToolsConfig = JsonUtility.FromJson<DocToolsBuildConfig>(File.ReadAllText(docToolsConfigFile));

            var configInManual = Path.Combine(manualSource, "config.json");
            if (File.Exists(configInManual))
                File.Delete(configInManual);

            docToolsConfig.DefineConstants = constantsDefines + docToolsConfig.DefineConstants;

            var solutionPrefix = "<Project ToolsVersion=\"4.0\" DefaultTargets=\"FullPublish\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup><DefineConstants>" + docToolsConfig.DefineConstants + "</DefineConstants></PropertyGroup><ItemGroup>";
            var solutionSuffix = "</ItemGroup></Project>";
            var solutionItems = "";
            foreach (string cs in Directory.GetFiles(packageCloneFolder, "*.cs", SearchOption.AllDirectories))
                solutionItems += string.Format("<Compile Include=\"{0}\"/>", cs);

            var solution = string.Format("{0}\n{1}\n{2}", solutionPrefix, solutionItems, solutionSuffix);
            File.WriteAllText(Path.Combine(packageCloneFolder, "solution.csproj"), solution);

            //
            // Build the doc
            string arguments = string.Format("\"{0}\"", docfxJson);
            var success = RunDocFx(arguments);

            // Copy svg logo
            if (success)
            {
                FileUtil.MoveFileOrDirectory(Path.Combine(buildFolder, "_site"), siteFolder);

                File.Copy(logoSource, logoTarget, true);

                // Clear build folder (unless in internal mode for easier debugging)
                if (!Unsupported.IsDeveloperMode())
                    FileUtil.DeleteFileOrDirectory(buildFolder);
            }
        }

        class PackageJsonHelper
        {
            public string displayName = string.Empty;
            public string version = string.Empty;
        }

        private static void SetupDefaultTitle(string packageFolder, string buildFolder)
        {
            var defaultTitle = "Unity Documentation";
            var defaultVersion = "";
            var packageJsonPath = Path.Combine(packageFolder, "package.json");

            if (File.Exists(packageJsonPath))
            {
                var parsedJson = JsonUtility.FromJson<PackageJsonHelper>(File.ReadAllText(packageJsonPath));
                defaultTitle = $"{parsedJson.displayName}";
                defaultVersion = $"{parsedJson.version}";
            }

            var buildFiles = Directory.GetFiles(buildFolder);
            foreach (var file in buildFiles)
            {
                var fileName = Path.GetFileName(file);
                if (!fileName.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (fileName.StartsWith("docfx", StringComparison.InvariantCultureIgnoreCase) ||
                    fileName.Equals("projectMetadata.json", StringComparison.InvariantCultureIgnoreCase))
                {
                    var jsonContent = File.ReadAllText(file);
                    jsonContent = jsonContent.Replace("DEFAULT_APP_TITLE", defaultTitle);
                    jsonContent = jsonContent.Replace("DEFAULT_PACKAGE_VERSION", defaultVersion);

                    File.WriteAllText(file, jsonContent);
                }
            }
        }

        private void CreateTinyRuntimeDoc(string buildFolder)
        {
            var runtimeDocTarget = Path.Combine(buildFolder, "rt/tiny_runtime");

            var tempRoot = Application.temporaryCachePath;

            InstallTypeDoc(tempRoot);
            RunTypeDoc(tempRoot, runtimeDocTarget);
        }

        private void UncompressTineRuntimeDoc(string archiveFilepath, string targetFolder)
        {
            var zapp = Application.platform == RuntimePlatform.WindowsEditor ? "7z.exe" : "7za";
            var appCommand = Path.Combine(EditorApplication.applicationContentsPath, "Tools/" + zapp);
            var args = "x \"" + archiveFilepath + "\" -y \"-o" + targetFolder + "\" Tiny/Dist/runtimedll/*.ts";

            var sevenZ = new Process
            {
                StartInfo = new ProcessStartInfo(appCommand)
                {
                    Arguments = args,
                    WorkingDirectory = targetFolder,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            sevenZ.Start();
            sevenZ.WaitForExit(10000);

            if (sevenZ.ExitCode != 0)
                throw new Exception("Error decompressing tiny runtime documentation: " + sevenZ.ExitCode + " -- command: " + appCommand + " " + args);
        }

        private void InstallTypeDoc(string tempRoot)
        {
            var archiveFilepath = Path.GetFullPath("packages/com.unity.tiny/tiny-runtime-dist.zip");
            if (!File.Exists(archiveFilepath))
                throw new Exception("Could not find tiny runtime dist file in: " + archiveFilepath);

            var launcher = new NodeLauncher();
            launcher.WorkingDirectory = tempRoot;
            launcher.NpmInstall("typedoc");

            // Create code folder
            var code = Path.Combine(tempRoot, "code");
            if (!Directory.Exists(code))
                Directory.CreateDirectory(code);

            UncompressTineRuntimeDoc(archiveFilepath, tempRoot);
            var targetTs = Path.Combine(code, "RuntimeFull.ts");    // Need to rename to .ts since d.ts will not generate a site with typedoc
            var sourceTs = Path.Combine(tempRoot, "Tiny/Dist/runtimedll/RuntimeFull.d.ts");
            File.Copy(sourceTs, targetTs, true);
        }

        private void RunTypeDoc(string target, string runtimeDocTarget)
        {
            var args = "\"" + Path.Combine(target, "node_modules/typedoc/bin/typedoc") + "\" --out \"" + runtimeDocTarget + "\" code --ignoreCompilerErrors  --target ES5 --module commonjs";

            Process process = new Process();
            process.StartInfo.FileName = NodeLauncher.NodePath;
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = target;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            var log = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60000);
            int code = process.ExitCode;
            log += process.StandardOutput.ReadToEnd();
            if (code != 0)
            {
                Debug.Log("From Working Directory: " + target);
                var command = NodeLauncher.NodePath + " " + args;
                throw new Exception("Could not complete tiny doc generation. Problem running typedoc: " + code + "\nwith command: " + command +
                    "\n-- error:\n" + log);
            }

            if (!Directory.Exists(runtimeDocTarget))
                throw new Exception("Unknown error while creating tiny doc. Problem running typedoc.");
        }

        private void ConvertDocWorksToC(string docWorksToC, string output)
        {
            var content = "";
            int minimumIndentSize = 1;                // The minimum indent is 1 (eg: always one '#' in docfx toc)
            int indent = minimumIndentSize;
            int spaceCount = -1;
            int indentSize = 0;
            foreach (var line in File.ReadAllLines(docWorksToC))
            {
                var tokens = line.Split('*').ToList();
                if (tokens.Count < 2) continue;

                if (tokens[0].Length > spaceCount)
                {
                    if (tokens[0].Length > 0)
                    {
                        indent++;
                        if (indentSize == 0)
                            indentSize = tokens[0].Length;
                    }
                }
                else if (tokens[0].Length < spaceCount)
                    indent = Math.Max(minimumIndentSize, (tokens[0].Length / Math.Max(1, indentSize)) + minimumIndentSize);

                spaceCount = tokens[0].Length;

                tokens.RemoveAt(0);
                var lineContent = String.Join(String.Empty, tokens.ToArray());


                // The following code converts DocWorks toc link format to DocFx toc link format
                // Example 1:
                // `[Documentation Tool](index)` =>
                // `[Documentation Tool](index.md)`
                // Example 2:
                // `[Table Of Content](index#table-of-content)` =>
                // `[Table Of Content](index.md#table-of-content)`
                var match = Regex.Match(lineContent, @"\(([^\(\)]*)\)");
                if (match.Success)
                {
                    // We use Regex to match content that's between `()`, so that we only modify the links but not the text themselves
                    var link = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(link) && !link.Contains(".md") && !link.Contains(".html"))
                    {
                        if (link.Contains("#"))
                            link = link.Replace("#", ".md#");
                        else
                            link = link + ".md";
                    }
                    lineContent = lineContent.Substring(0, match.Index) + $"({link})" + lineContent.Substring(match.Index + match.Length);
                }

                lineContent = new String('#', indent) + lineContent;

                content += lineContent + "\n";
            }

            if (string.IsNullOrEmpty(content))
                throw new Exception("Table of content has no elements in it. Use the DocWorks ToC format:\n* [My Secion](index)");

            File.WriteAllText(output, content);
        }

        void TryBuild(string packageName, string shortVersionId, bool isEmbedded)
        {
            // Build if it hasn't been built before or if it's an embedded package, always rebuild as it is in development
            if (!Directory.Exists(GetPackageSiteFolder(shortVersionId)) || isEmbedded)
                BuildWithProgress(packageName, shortVersionId);
        }

        string GetSiteUrl(string root, string versionId, string path = "index.html")
        {
            if (!string.IsNullOrEmpty(path))
                path = "/" + path;

            return string.Format("{0}/{1}{2}", root, versionId, path);
        }

        string GetSiteUrlAbsolute(string versionId, string path = "index.html")
        {
            return GetSiteUrl(WebPackageRoot, versionId, path);
        }

        string GetSiteUrlRelative(string versionId)
        {
            return GetSiteUrl(WebPackageRootRelative, versionId, "");
        }

        public virtual void BuildRedirectPage(string sitefolder, string redirectUrl, string absoluteLatestUrl = "", string outputFilePath = "index.html", string redirectDefaultPath = "index.html")
        {
            if (string.IsNullOrEmpty(absoluteLatestUrl))
                absoluteLatestUrl = redirectUrl;
            if (!string.IsNullOrEmpty(redirectDefaultPath))
                redirectDefaultPath = "/" + redirectDefaultPath;

            var html = "";
            html += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">\n";
            html += "<html lang=\"en\" xml:lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\n";
            html += "<head>\n";
            html += "{1}";
            html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n";
            html += "{2}";
            html += "<script type=\"text/javascript\">window.location.replace((getParameterByName('preview')  === '1' ? \"{4}\" : \"{0}\") + (getParameterByName('subfolder') || '{3}'))</script>\n";
            html += "<title>Redirect to... title of new-page</title>\n";
            html += "</head>\n";
            html += "<body>\n";
            html += "<noscript><iframe src=\"https://www.googletagmanager.com/ns.html?id=GTM-5V25JL6\" height=\"0\" width=\"0\" style=\"display:none;visibility:hidden\"></iframe></noscript>\n";
            html += "<h1>Re-directing...</h1>\n";
            html += "<p>You are being re-directed, if nothing happens, please <a href=\"{0}{3}\">follow this link</a></p>\n";
            html += "</body>\n";
            html += "</html>\n";

            // Add the analytics tag in string.Format, since it contains '{' brackets
            var analyticsTag = "<script>(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':  new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],   j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=   'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);  })(window,document,'script','dataLayer','GTM-5V25JL6');</script>\n";
            var utilsScript = "<script type=\"text/javascript\">function getParameterByName(name, url) {if (!url) url = window.location.href;name = name.replace(/[\\[\\]]/g, \"\\\\$&\");var regex = new RegExp(\"[?&]\" + name + \"(=([^&#]*)|&|#|$)\"),results = regex.exec(url);if (!results) return null;if (!results[2]) return '';return decodeURIComponent(results[2].replace(/\\+/g, \" \"));}</script>\n";
            html = string.Format(html, redirectUrl, analyticsTag, utilsScript, redirectDefaultPath, absoluteLatestUrl);

            Directory.CreateDirectory(sitefolder);
            var filepath = Path.Combine(sitefolder, outputFilePath);
            File.WriteAllText(filepath, html);
        }

        // Method content must be matched in package manager UI
        public static string GetPackageUrlRedirect(string packageName, string shortVersionId)
        {
            var versionToken = shortVersionId.Split('@')[1];
            Version version;
            System.Version.TryParse(versionToken, out version);

            var redirectUrl = "";
            if (packageName == "com.unity.ads")
            {
                if (version < new Version(3, 0))
                    redirectUrl = "https://docs.unity3d.com/Manual/UnityAds.html";
            }
            else if (packageName == "com.unity.analytics")
            {
                if (version < new Version(3, 2))
                    redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalytics.html";
            }
            else if (packageName == "com.unity.purchasing")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityIAP.html";
            else if (packageName == "com.unity.standardevents")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsStandardEvents.html";
            else if (packageName == "com.unity.xiaomi")
                redirectUrl = "https://unity3d.com/cn/partners/xiaomi/guide";
            else if (packageName == "com.unity.shadergraph")
            {
                if (version < new Version(4, 1))
                    redirectUrl = "https://github.com/Unity-Technologies/ShaderGraph/wiki";
            }

            return redirectUrl;
        }

        // Will build a redirect to manual page if needed. Returns false if not needed.
        public virtual bool TryBuildRedirectToManual(string packageName, string shortVersionId)
        {
            var redirectUrl = GetPackageUrlRedirect(packageName, shortVersionId);

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Debug.Log("Building site with Direct Documentation Url: " + redirectUrl);

                string sitefolder = GetPackageSiteFolder(shortVersionId);
                BuildRedirectPage(sitefolder, redirectUrl, "index.html", "index.html", "");
                BuildRedirectPage(Path.Combine(sitefolder, "changelog"), redirectUrl, "CHANGELOG.html", "CHANGELOG.html", "");
                BuildRedirectPage(Path.Combine(sitefolder, "license"), redirectUrl, "LICENSE.html", "LICENSE.html", "");

                return true;
            }

            return false;
        }

        public virtual void BuildRedirectToLatestPage(string packageName , string latestShortVersionId, string absoluteLatestShortVersionId = "", string siteFolder = null)
        {
            if (string.IsNullOrEmpty(siteFolder))
                siteFolder = Path.Combine(DocumentationSiteRoot, string.Format("{0}@latest", packageName));
            if (string.IsNullOrEmpty(absoluteLatestShortVersionId))
                absoluteLatestShortVersionId = latestShortVersionId;

            var redirectUrl = GetSiteUrlRelative(latestShortVersionId);
            var absoluteLatestUrl = GetSiteUrlRelative(absoluteLatestShortVersionId);

            BuildRedirectPage(siteFolder, redirectUrl, absoluteLatestUrl);
        }

        public virtual void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

        public virtual void OpenLocalPackageUrl(string packageName, string shortVersionId, bool isEmbedded, string path = "index.html", bool doNotRebuild = false)
        {
            EnsureServe(() =>
            {
                if (!doNotRebuild)
                    TryBuild(packageName, shortVersionId, isEmbedded);
                var url = string.Format("http://localhost:{0}/{1}/{2}", Convert.ToString(servePort), shortVersionId,
                    path);

                OpenUrl(url);
            });
        }

        public virtual void OpenPackageUrl(string packageName, string shortVersionId, bool isEmbedded, bool isInstalled, string path = "index.html")
        {
            var isConnected = CheckForInternetConnection();
            var showLocal = isEmbedded || (isInstalled && !isConnected);

            // If package is installed and user is offline, use local url, otherwise use on the web.
            if (showLocal)
            {
                OpenLocalPackageUrl(packageName, shortVersionId, isEmbedded, path);
            }
            else
            {
                var url = GetSiteUrlAbsolute(shortVersionId, path);
                OpenUrl(url);
            }
        }
    }
}
