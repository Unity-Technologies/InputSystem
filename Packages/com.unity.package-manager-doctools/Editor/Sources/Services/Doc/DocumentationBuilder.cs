using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using UnityEditor.PackageManager.Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;
//using UnityEditor.PackageManager;

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
        internal static string DocFxZip { get { return Path.Combine(PackageUIDocumentationRoot, "docfx-2.51.7z"); } }
        internal static string DocFxRoot { get { return Path.Combine(DocumentationBuildRoot, "docfx-2.51"); } }
        internal static string DocFxExecutable {get { return Path.Combine(DocFxRoot, "docfx.exe"); }}
        internal static string DocFxTemplateRoot {get { return Path.Combine(PackageUIDocumentationRoot, "docfx_packages"); }}
        internal static string EditorMonoPath {get { return Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono"); }}
        internal static string MonoZip { get { return Path.Combine(PackageUIDocumentationRoot, "mono-5.16.0.7z"); } }
        internal static string MonoLinuxZip {get { return Path.Combine(PackageUIDocumentationRoot, "mono-linux-5.16.0.7z"); }}
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
        internal static string DocumentationRoot {get {return Path.Combine(UnityEditorDataPathRoot, "editor", "documentation");}}

        // Use default path if no path has been explicitly set
        internal static string DocumentationSiteRoot
        {
            get
            {
                if (Directory.Exists(GlobalSettings.DestinationPath) && FileUtils.IsDirectoryWritable(GlobalSettings.DestinationPath))
                    return GlobalSettings.DestinationPath;
                else
                    return Path.Combine(DocumentationRoot, "packages");
            }
        }
            //=> GlobalSettings.DestinationPath != String.Empty ? GlobalSettings.DestinationPath : Path.Combine(DocumentationRoot, "packages");

        // Some packages have deep paths which end up exceeding windows' path name limit
        internal static string DocumentationBuildRoot
            => Application.platform == RuntimePlatform.WindowsEditor ? "c:\\temp" : Path.Combine(DocumentationRoot, "build");

        bool skipDocFXOutputNoise = true; //docfx repeats warnings & errors at end, so skip to the good part

        bool serveCompleted;
        private Action serveCallback;
        public event Action OnServing = delegate {};

        private Thread serveThread;
        private Process serveProcess;

        private int servePort;
        private bool serving;
        private string serveLog;
        private bool stopServeLogWatch;

        private string buildLog;
        internal DocumentationBuilder() {}

        string GetPackageBuildFolder(string shortVersionId)
        {
            return Path.Combine(DocumentationBuildRoot, shortVersionId);
        }

        public virtual string GetPackageSiteFolder(string shortVersionId, string siteFolder = null)
        {
            if (string.IsNullOrEmpty(siteFolder))
                siteFolder = DocumentationSiteRoot;
            return Path.Combine(siteFolder, shortVersionId);
        }

        private bool RunDocFx(string docFxArguments, bool serveMode = false)
        {

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
                docfxProcess.ErrorDataReceived += DocFxOutputHandler;

                var startMessage = string.Format("DocFx Command line ran: \"{0}\" {1}", applicationExec, arguments);
                if (GlobalSettings.Debug)
                    if (serveMode)
                        serveLog += "Doc Fx Serve Output: " + startMessage + "\n";
                    else
                        Debug.Log(startMessage);

                docfxProcess.Start();
                skipDocFXOutputNoise = true;
                docfxProcess.BeginOutputReadLine();

                // To avoid deadlocks, always read the output stream first and then wait.
                // Also don't read to end both standard outputs.
                processLog += docfxProcess.StandardError.ReadToEnd();

                //if (!serveMode)
                //docfxProcess.WaitForExit();
                while (!docfxProcess.WaitForExit(250)) //For some reason this never timesout
                {
                    if (UpdateProgressOrCancel($"{GlobalSettings.PackageInformation.displayName}... DocFX running..."))
                    {
                        docfxProcess.Kill();
                        success = false;
                    }
                }
                docfxProcess.WaitForExit(100); //Make sure we get last few IO events from the process
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
            if (GlobalSettings.Debug && !string.IsNullOrEmpty(processLog))
                if (serveMode)
                    serveLog += "Doc Fx Serve Output: " + message + "\n";
                else
                    Debug.Log(message);

            stopServeLogWatch = true;

            if (!success)
            {
                Debug.LogError(message);
            }

            docfxProcess.Close();
            return success;
        }

        private void DocFxOutputHandler(object sender, DataReceivedEventArgs outLine)
        {
            // Collect the docfx command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                if (!skipDocFXOutputNoise || GlobalSettings.Debug) // Show everything in Debug mode
                    buildLog += outLine.Data + "\n";

                if (outLine.Data.Contains("Info:Completed in")) //Start adding lines to report
                {
                    skipDocFXOutputNoise = false;
                }
                if (!string.IsNullOrEmpty(outLine.Data.Trim()))
                {
                    serveLog += "Doc Fx Serve Output: " + outLine.Data + "\n";
                }
                if (outLine.Data.Contains("on http://localhost:"))
                {
                    serveCompleted = true;        // Will be stalled on next line since docfx waits for input
                }
            }
        }

        private void DocFxErrorHandler(object sender, DataReceivedEventArgs outLine)
        {
            Debug.LogWarning($"DocFX Error: {outLine.Data}");
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
            if (GlobalSettings.Validate)
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
            if (GlobalSettings.Validate && !string.IsNullOrEmpty(serveLog))
            {
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

        public virtual string BuildWithProgress(PackageInfo packageInfo, string shortVersionId, string siteFolder = null)
        {
            var log = "";
            UpdateProgressOrCancel($"{packageInfo.displayName}... starting...");

            try
            {
                log = Build(packageInfo, shortVersionId, siteFolder);
            }
            catch (Exception error)
            {
                Debug.LogError(error);
            }

            EditorUtility.ClearProgressBar();
            return log;
        }
        
        /// <summary>
        /// Sets up a doc build by copying files to a build folder, creating a docFx config and top-level TOC.
        /// </summary>
        /// <param name="packageInfo">The info object for the package.</param>
        /// <param name="shortVersionId">Shortened version without patch number, eg: com.unity.package-manager-ui@1.2 (note: not @1.2.0)</param>
        /// <param name="siteFolder">Output folder where the doc site should be created.</param>
        public virtual string Build(PackageInfo packageInfo, string shortVersionId, string siteFolder = null)
        {
            #region setup

            bool AbortBuild = false; //Set true if problems encountered that should nullify results
            buildLog = ""; //Stores commandline output for error report

            // The docfx config (serialized into build folder)
            DocFXConfig docFXConfig = new DocFXConfig(packageInfo); //DocFX config file
            // The main DocFX TOC that sets the links in the top title bar (i.e. Manual, Scripting API, ...)
            DocFXTOC topTOC = new DocFXTOC();

            // Where the finished output is copied after generation
            
            siteFolder = GetPackageSiteFolder(shortVersionId, siteFolder);

            //Various paths
            string unityProjectFolder = Directory.GetCurrentDirectory(); //Current Unity project
            string packageFolder = Path.GetFullPath("Packages");
            string buildFolder = GetPackageBuildFolder(shortVersionId); //Where the intermediate build files are marshalled
            string manualSource = Path.Combine(buildFolder, packageInfo.assetPath, "Documentation~"); //The package we are generating docs for -- after copying to build folder
            string logoSource = Path.Combine(buildFolder, "logo.svg"); //TODO: why is logo copied and not part of template?
            string logoTarget = Path.Combine(siteFolder, "logo.svg");

            SetupMono(); //Unzips and "installs" mono, needed to run docfx tool.

            // Set build folders to initial condition

            // Clear build folder
            FileUtil.DeleteFileOrDirectory(buildFolder);
            // Clear site folder prior to building
            FileUtil.DeleteFileOrDirectory(siteFolder);

            #endregion

            if(UpdateProgressOrCancel($"{packageInfo.displayName}... copying package files..."))
            {
                AbortBuild = true;
            }
            #region copy-to-build-folder
            // Copy working files into build folder
            string packageCloneFolder = Path.Combine(buildFolder, "Packages");
            FileUtil.CopyFileOrDirectory(DocFxTemplateRoot, buildFolder); //The html template files
            FileUtil.CopyFileOrDirectory(packageFolder, packageCloneFolder); // The Packages in the current folder
            FileUtils.DirectoryCopyAll(Path.Combine(unityProjectFolder, "Library", "PackageCache"),
                                       Path.Combine(buildFolder, "Library", "PackageCache"), true); // The cached dependencies
            FileUtils.DirectoryCopyAll(Path.Combine(unityProjectFolder, "Library", "ScriptAssemblies"),
                                       Path.Combine(buildFolder, "Library", "ScriptAssemblies"), true); // The compiled dlls

            // if package isn't actually in the project (i.e. it is a built-in or in the global package cache),
            // then copy it into the build folder
            if (packageInfo.source != PackageSource.Embedded)
            {
                FileUtils.DirectoryCopyIgnorePaths(
                    Path.GetFullPath(string.Format("Packages/{0}", packageInfo.name)),
                    Path.Combine(packageCloneFolder, packageInfo.name));
            }

            // When used from the cache (which is read-only), it is important to set the folder properties to write.
            FileUtils.SetFolderPermission(buildFolder);

            #endregion

            #region handle-manual-section
            // Add manual file references to the config
            var manualDestination = Path.Combine(buildFolder, "manual");
            //FileUtils.DirectoryCopyAll(manualSource, manualDestination, true);
            docFXConfig.AddContentToSection(
                FileUtils.RelativePathBelow(buildFolder, manualSource), "manual",
                new string[] { "**.md", "**.yml"}, new string[] {"snippets/**.md" });
            string relPath = "Packages/" + packageInfo.name + "/Documentation~";
            topTOC.Add(new TOCItem("Manual", relPath + "/", relPath + "/index.md"));

            //Which image folder: images or Images? Surprisingly complicated...
            string[] dirs = Directory.GetDirectories(manualSource, "?mages");
            string imgFolder = "";
            foreach (var imgDir in dirs) //There could be other folders that match in "?mages"
            {
                imgFolder = FileUtils.RelativePathBelow(manualSource, imgDir);
                if (imgFolder.ToLower() == "images")
                    break;
            }
            if (imgFolder != String.Empty)
            {
                docFXConfig.AddImageFolder(
                    Path.Combine(FileUtils.RelativePathBelow(buildFolder, manualSource), imgFolder),
                    Path.Combine("manual", "images"),
                    new string[] {"**"});
            }

            // If there aren't any manual files, make a fake one
            //            var sourceManualFiles = Directory.GetFiles(manualDestination, "*.md", SearchOption.AllDirectories);
            var sourceManualFiles = Directory.GetFiles(manualSource, "*.md", SearchOption.AllDirectories);
            if (!sourceManualFiles.Any())
            {
                var missingManual = "Error: (non fatal) Package Documentation Build: Feature Documentation does not exist. An empty placeholder will be used instead";
                Debug.LogWarning(missingManual);
                buildLog += missingManual + "\n";
                string manualFile = Path.Combine(manualSource, "index.md");
                //string manualFile = Path.Combine(manualDestination, "index.md");
                File.WriteAllText(manualFile, "Information\n-----------\n<span style=\"color:red\">There is currently no documentation for this package.</span>");
            }

            // Set up table of content for manual
            var tocSuccess = HandleTOC(manualSource, buildFolder, out var enableTOC);
            docFXConfig.SetTOCEnabled(enableTOC);
            if (!tocSuccess)
                AbortBuild = true;
            #endregion

            #region move-optional-files
            //Move optional files
            FileUtils.MoveAndReplaceFile(Path.Combine(manualSource, "filter.yml"), Path.Combine(buildFolder, "filter.yml"));
            FileUtils.MoveAndReplaceFile(Path.Combine(manualSource, "api_index.md"), Path.Combine(buildFolder, "api", "index.md"));


            //SetGlobalMetadata (optional)
            var projectMetadataFile = Path.Combine(manualSource, "projectMetadata.json");
            if (File.Exists(projectMetadataFile))
                SetGlobalMetadataFromFile(docFXConfig, projectMetadataFile);

            #endregion

            #region handle-license-changelog-sections
            // Add License section
            SetupLicenseSection(Path.Combine(packageFolder, packageInfo.name), buildFolder);
            topTOC.Add(new TOCItem("License", "license/", Path.Combine("license", "LICENSE.md")));

            // Use the changelog, if it exists, otherwise keep the empty default changelog.
            var changelogFile = Path.Combine(Path.Combine(packageFolder, packageInfo.name), "CHANGELOG.md");
            if (File.Exists(changelogFile))
                File.Copy(changelogFile, Path.Combine(buildFolder, "changelog/CHANGELOG.md"), true);
            topTOC.AddBefore("license/", new TOCItem("Changelog", "changelog/", Path.Combine("changelog", "CHANGELOG.md")));

            #endregion

            if (UpdateProgressOrCancel($"{packageInfo.displayName}... creating solution..."))
            {
                AbortBuild = true;
            }

            #region handle-api-section
            if (GlobalSettings.DoScriptRef)
            { 
                //Make  project (csproj) file
                var config = Path.Combine(manualSource, "config.json");
                CreateSolution(packageInfo, packageCloneFolder, config);

                docFXConfig.AddCSProject("solution.csproj", "Packages");
                topTOC.AddItem(new TOCItem("Scripting API", "api/", Path.Combine("api", "index.md")), 1);

                bool hasGNSSetting = docFXConfig.GetGlobalMetadata().TryGetValue("hideGlobalNamespace", out var hideGlobalNamespaceSetting);
                if(!(hasGNSSetting && (bool)hideGlobalNamespaceSetting))
                    docFXConfig.SetGlobalNamespace("Global Namespace");
            }
            #endregion

            #region handle-external-references
            AddXrefMaps(docFXConfig, packageInfo); // For dependencies

            // Use specific editor version if available, otherwise use current
            string unityManualXrefs;
            string unityXrefs;
            string unityEditorXrefs;
            var currentVersion = Application.unityVersion;
            if (currentVersion.StartsWith("2019.3", StringComparison.CurrentCulture))
            {
                unityManualXrefs = Path.Combine(buildFolder, "unitymanual_xrefmap_193.yml");
                unityXrefs = Path.Combine(buildFolder, "unityengine_xrefmap_193.yml");
                unityEditorXrefs = Path.Combine(buildFolder, "unityeditor_xrefmap_193.yml");
            }
            if (currentVersion.StartsWith("2019.4", StringComparison.CurrentCulture))
            {
                unityManualXrefs = Path.Combine(buildFolder, "unitymanual_xrefmap_194.yml");
                unityXrefs = Path.Combine(buildFolder, "unityengine_xrefmap_194.yml");
                unityEditorXrefs = Path.Combine(buildFolder, "unityeditor_xrefmap_194.yml");
            }
            else if (currentVersion.StartsWith("2020.1", StringComparison.CurrentCulture))
            {
                unityManualXrefs = Path.Combine(buildFolder, "unitymanual_xrefmap_201.yml");
                unityXrefs = Path.Combine(buildFolder, "unityengine_xrefmap_201.yml");
                unityEditorXrefs = Path.Combine(buildFolder, "unityeditor_xrefmap_201.yml");
            }
            else if (currentVersion.StartsWith("2020.2", StringComparison.CurrentCulture))
            {
                unityManualXrefs = Path.Combine(buildFolder, "unitymanual_xrefmap_202.yml");
                unityXrefs = Path.Combine(buildFolder, "unityengine_xrefmap_202.yml");
                unityEditorXrefs = Path.Combine(buildFolder, "unityeditor_xrefmap_202.yml");
            }
            else
            {
                unityManualXrefs = Path.Combine(buildFolder, "unitymanual_xrefmap.yml");
                unityXrefs = Path.Combine(buildFolder, "unityengine_xrefmap.yml");
                unityEditorXrefs = Path.Combine(buildFolder, "unityeditor_xrefmap.yml");
            }
            if (File.Exists(unityXrefs))
                docFXConfig.AddXrefmapURL(unityXrefs);
            if (File.Exists(unityEditorXrefs))
                docFXConfig.AddXrefmapURL(unityEditorXrefs);
            if (File.Exists(unityManualXrefs))
                docFXConfig.AddXrefmapURL(unityManualXrefs);

            // xrefs to C# language keywords
            var langWords = Path.Combine(buildFolder, "langwordMapping.yml");
            if (File.Exists(langWords))
            {
                docFXConfig.AddXrefmapURL(langWords);
                docFXConfig.SetNoLangKeywordOption(true);
            }
            #endregion

            #region set-ui-texts
            
            SetGlobalMetadataFromFile(docFXConfig, Path.Combine(buildFolder, "ui.json"));
            var globalMetadata = docFXConfig.GetGlobalMetadata();
            var uiKeys = docFXConfig.GetGlobalMetadata().Keys.Where(key => key.StartsWith("_UI_", StringComparison.Ordinal));
            foreach (var tocItem in topTOC)
            {
                var uiKey = string.Format("_UI_{0}", tocItem.name.Replace(" ", "_"));
                if (uiKeys.Contains(uiKey))
                {
                    tocItem.name = globalMetadata[uiKey] as string;
                }
            }

            var docFxJsPath = Path.Combine(buildFolder, "_exported_templates", "packages", "styles", "docfx.js");

            using (var sw = File.AppendText(docFxJsPath))
            {
                foreach (var uiKey in uiKeys)
                { 
                    sw.WriteLine(string.Format("appendUITexts('{0}', '{1}')", uiKey, globalMetadata[uiKey]));
                }
            }	
            
            #endregion
            
            if (UpdateProgressOrCancel($"{packageInfo.displayName}... running DocFX..."))
            {
                AbortBuild = true;
            }

            #region add-memberpage-plugin
            bool hasSetting = docFXConfig.GetGlobalMetadata().TryGetValue("useMemberPages", out var memberPluginSetting);
            if (hasSetting && (bool) memberPluginSetting)
            {
                docFXConfig.AddDocFXTemplate(Path.Combine("Plugins", "memberpage.2.56.2", "content"));
            }
            #endregion

            #region run-docfx
            // Build the doc...
            if (AbortBuild) //If setup error occured, generate the docs to get error messages, but don't save the html files
            {
                var abortMessage = "*** Build aborted due to errors -- see Error Report, doc site for package will not be created.\n\n";
                buildLog += abortMessage;
                Debug.LogError(abortMessage);
            }

            if (GlobalSettings.Validate || AbortBuild) // If in validate mode always regenerate everything to get full error reports
                docFXConfig.SetForceRebuildOption(true); //Note: this is effectively always true when building from Unity, since the build folder is recreated, but not true if you use commandline

            // Write the config json file
            var docfxJson = docFXConfig.WriteConfig(Path.Combine(buildFolder, "dfx_cfg.json"));
            // Write top-level TOC file
            File.WriteAllText(Path.Combine(buildFolder, "toc.yml"), topTOC.ToString());

            // Run the docfx tool
            string arguments = string.Format("\"{0}\"", docfxJson);
            var success = RunDocFx(arguments);
            #endregion

            #region handle-build-completion
                        // Copy generated html and svg logo
            if (success && !AbortBuild)
            {
                FileUtil.MoveFileOrDirectory(Path.Combine(buildFolder, "_site"), siteFolder);
                File.Copy(logoSource, logoTarget, true);
            }

            // Delete the build folder (unless in Debug mode for easier debugging)
            if (!GlobalSettings.Debug)
                FileUtil.DeleteFileOrDirectory(buildFolder);

            #endregion


            return buildLog;
        }

        private void SetGlobalMetadataFromFile(DocFXConfig docFXConfig, string filePath)
        {
            var metadata = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));

            //prohibited keys
            var prohibitedKeys = "_enableSearch,_appLogoPath,_disableToc,_packageVersion";
            foreach (var entry in metadata)
            {
                if (prohibitedKeys.Contains(entry.Key))
                {
                    buildLog += "Warning: setting metadata: " + entry.Key + " is not allowed. Value ignored.\n";
                    continue;
                }

                docFXConfig.SetGlobalMetadata(entry.Key, entry.Value);
            }
        }

        private void CreateSolution(PackageInfo packageInfo, string packageCloneFolder, string docToolsConfigFile = "")
        {
            // Generate solution file
            // This is necessary to have proper cross linking across namespaces.
            // Otherwise, the links in NamespaceA.ClassA.MemberA to NamespaceB.ClassB will not be clickable in the generated doc
            // It also allows having preprocessor defines for code generation
            var constantsDefines = "PACKAGE_DOCS_GENERATION;";
            //var docToolsConfigFile = Path.Combine(packageDocs, "config.json");
            var docToolsConfig = new DocToolsBuildConfig();
            if (File.Exists(docToolsConfigFile))
                docToolsConfig = JsonUtility.FromJson<DocToolsBuildConfig>(File.ReadAllText(docToolsConfigFile));

            docToolsConfig.DefineConstants = constantsDefines + docToolsConfig.DefineConstants;

            var solutionPrefix = "<Project ToolsVersion=\"4.0\" DefaultTargets=\"FullPublish\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">";
            var solutionPropertyGroup = string.Format("<PropertyGroup><DefineConstants>{0}</DefineConstants></PropertyGroup>", docToolsConfig.DefineConstants);
            var solutionSuffix = "<Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" /></Project>";
            var solutionItems = "";
            var packageCodeFolder = Path.Combine(packageCloneFolder, packageInfo.name);
            foreach (string cs in Directory.GetFiles(packageCodeFolder, "*.cs", SearchOption.AllDirectories))
                solutionItems += string.Format("<Compile Include=\"{0}\"/>", cs);
            var solutionCompileItemGroup = string.Format("<ItemGroup>{0}</ItemGroup>", solutionItems);
            string referenceItems = "";
            foreach (string dll in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Library", "ScriptAssemblies"), "*.dll", SearchOption.AllDirectories))
            {
                var asmName = System.Reflection.AssemblyName.GetAssemblyName(dll);
                referenceItems += string.Format("<Reference Include=\"{0}\"><HintPath>{1}</HintPath></Reference>", asmName.FullName, dll);
            }
            //Add UnityEngine.dll for current version
            string UnityEnginePath =
                Path.Combine(EditorApplication.applicationContentsPath, "Managed", "UnityEngine.dll");
            var unityEngineASMName = System.Reflection.AssemblyName.GetAssemblyName(UnityEnginePath);
            referenceItems += string.Format("<Reference Include=\"{0}\"><HintPath>{1}</HintPath></Reference>", unityEngineASMName.FullName, UnityEnginePath);
            //Add UnityEditor.dll for current version
            string UnityEditorPath =
                Path.Combine(EditorApplication.applicationContentsPath, "Managed", "UnityEditor.dll");
            var unityEditorASMName = System.Reflection.AssemblyName.GetAssemblyName(UnityEditorPath);
            referenceItems += string.Format("<Reference Include=\"{0}\"><HintPath>{1}</HintPath></Reference>", unityEditorASMName.FullName, UnityEditorPath);

            var solutionReferenceGroup = string.Format("<ItemGroup>{0}</ItemGroup>", referenceItems);
            var solution = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n", solutionPrefix, solutionPropertyGroup, solutionCompileItemGroup, solutionReferenceGroup, solutionSuffix);
            File.WriteAllText(Path.Combine(packageCloneFolder, "solution.csproj"), solution);

        }

        // Uses the Assembly-CSharp.csproj file to get references to Unity dlls on current machine
        // Changes compiled file list to the package whose docs are being generated.
        private void RecycleSolution(PackageInfo packageInfo, string packageCloneFolder, string docToolsConfigFile = "")
        {
            var constantsDefines = "PACKAGE_DOCS_GENERATION;";
            var docToolsConfig = new DocToolsBuildConfig();
            if (File.Exists(docToolsConfigFile))
                docToolsConfig = JsonUtility.FromJson<DocToolsBuildConfig>(File.ReadAllText(docToolsConfigFile));

            docToolsConfig.DefineConstants = constantsDefines + docToolsConfig.DefineConstants; //TODO insert the defines in the project

            // Load the existing project file
            var assemCSharp = Path.Combine(Directory.GetCurrentDirectory(), "Assembly-CSharp.csproj");
            if (File.Exists(assemCSharp))
            {
                XmlDocument asmCSharp = new XmlDocument();
                asmCSharp.Load(assemCSharp);
                var root = asmCSharp.DocumentElement;

                // Remove unwanted elements
                var existingCompiles = root.GetElementsByTagName("Compile");
                var compileItemGroup = existingCompiles[0].ParentNode;
                compileItemGroup.RemoveAll();
                var projectRefs = root.GetElementsByTagName("ProjectReference");
                var projRefGroup = projectRefs[0].ParentNode;
                projRefGroup.RemoveAll();
                root.RemoveChild(projRefGroup);

                // Add package .cs files to compile list
                var packageCodeFolder = Path.Combine(packageCloneFolder, packageInfo.name);
                foreach (string cs in Directory.GetFiles(packageCodeFolder, "*.cs", SearchOption.AllDirectories))
                {
                    var compileElement = asmCSharp.CreateElement("Compile", asmCSharp.DocumentElement.NamespaceURI);
                    compileElement.SetAttribute("Include", cs);
                    compileItemGroup.AppendChild(compileElement);
                }

                // Add script assemblies as references
                var referenceItemGroup = asmCSharp.CreateElement("ItemGroup", asmCSharp.DocumentElement.NamespaceURI);
                root.AppendChild(referenceItemGroup);
                foreach (string dll in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Library", "ScriptAssemblies"), "*.dll", SearchOption.AllDirectories))
                {
                    var asmName = System.Reflection.AssemblyName.GetAssemblyName(dll);
                    var reference = asmCSharp.CreateElement("Reference", asmCSharp.DocumentElement.NamespaceURI);
                    reference.SetAttribute("Include", asmName.FullName);
                    referenceItemGroup.AppendChild(reference);
                    var hint = asmCSharp.CreateElement("HintPath", asmCSharp.DocumentElement.NamespaceURI);
                    hint.InnerText = dll;
                    reference.AppendChild(hint);
                }

                //Write the final project file
                asmCSharp.Save(Path.Combine(packageCloneFolder, "solution.csproj"));
            }
            else // Make the solution from scratch -- which is missing the Unity .dlls
            {
                CreateSolution(packageInfo, packageCloneFolder, docToolsConfigFile);
            }
        }

        private bool HandleTOC(string manualSource, string buildFolder, out bool enableTOC)
        {
            bool tocOkay = false;
            enableTOC = false;
            // Generate table of content for manual
            var docfxMdToc = Path.Combine(manualSource, "toc.md");
            var docfxYmlToc = Path.Combine(manualSource, "toc.yml");
            var docWorksToC = Path.Combine(manualSource, "TableOfContents.md"); //use this if it exists
            if (File.Exists(docWorksToC))
            {
                // DocWorks ToC support
                // Index.md is necessary for DocFx for now since it's embedded in the template.
                if (!File.Exists(Path.Combine(manualSource, "index.md")))
                {
                    var noIndexMD = "Error: Package Documentation generation error. Need an index.md when using a toc.md. Resulting site will not work.";
                    Debug.LogError(noIndexMD);
                    buildLog += noIndexMD + "\n";
                    tocOkay = false;
                }

                enableTOC = true;

                // Convert DocWorks style ToC to DocFx ToC format
                // ConvertDocWorksToC(docWorksToC, Path.Combine(buildFolder, "manual/toc.md"));
                ConvertDocWorksToC(docWorksToC, Path.Combine(manualSource, "toc.md"));
                tocOkay = true;
            }
            else if(File.Exists(docfxMdToc) || File.Exists(docfxYmlToc))
            {
                //Allow alternate formats
                enableTOC = true;
                tocOkay = true;
                buildLog += "Found alternate TOC, toc.md or toc.yml, using that.";
            }
            else
            {
                //
                // Simple manual support -- uses only the first file found
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
                tocOkay = true;
            }

            return tocOkay;

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
                    if (!string.IsNullOrEmpty(link) && !link.Contains(".md") && !link.Contains(".html") && !link.Contains(".yml"))
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

        void TryBuild(PackageInfo packageInfo, string shortVersionId, bool isEmbedded, string siteFolder = null)
        {
            // Build if it hasn't been built before or if it's an embedded package, always rebuild as it is in development
            if (!Directory.Exists(GetPackageSiteFolder(shortVersionId, siteFolder)) || isEmbedded)
                BuildWithProgress(packageInfo, shortVersionId);
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
            return GetSiteUrl("..", versionId, "");
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
            {
                if (version < new Version(3, 0))
                    redirectUrl = "https://docs.unity3d.com/Manual/UnityIAP.html";
            }
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
        public virtual bool TryBuildRedirectToManual(string packageName, string shortVersionId, string siteFolder = null)
        {
            var redirectUrl = GetPackageUrlRedirect(packageName, shortVersionId);
            
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                Debug.Log("Building site with Direct Documentation Url: " + redirectUrl);

                    
                siteFolder = GetPackageSiteFolder(shortVersionId, siteFolder);
                BuildRedirectPage(siteFolder, redirectUrl, "index.html", "index.html", "");
                BuildRedirectPage(Path.Combine(siteFolder, "changelog"), redirectUrl, "CHANGELOG.html", "CHANGELOG.html", "");
                BuildRedirectPage(Path.Combine(siteFolder, "license"), redirectUrl, "LICENSE.html", "LICENSE.html", "");

                return true;
            }

            return false;
        }

        public virtual void BuildRedirectToLatestPage(string packageName , string latestShortVersionId, string absoluteLatestShortVersionId = "", string siteFolder = null)
        {
            if (string.IsNullOrEmpty(siteFolder)) 
                siteFolder = DocumentationSiteRoot;
                
            siteFolder = Path.Combine(siteFolder, string.Format("{0}@latest", packageName));
            
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

        public virtual void OpenLocalPackageUrl(PackageInfo packageInfo, string shortVersionId, bool isEmbedded, string path = "index.html", bool doNotRebuild = false)
        {
            EnsureServe(() =>
            {
                if (!doNotRebuild)
                    TryBuild(packageInfo, shortVersionId, isEmbedded);
                var url = string.Format("http://localhost:{0}/{1}/{2}", Convert.ToString(servePort), shortVersionId,
                    path);

                OpenUrl(url);
            });
        }

        public virtual void OpenPackageUrl(PackageInfo packageInfo, string shortVersionId, bool isEmbedded, bool isInstalled, string path = "index.html")
        {
            var isConnected = CheckForInternetConnection();
            var showLocal = isEmbedded || (isInstalled && !isConnected);

            // If package is installed and user is offline, use local url, otherwise use on the web.
            if (showLocal)
            {
                OpenLocalPackageUrl(packageInfo, shortVersionId, isEmbedded, path);
            }
            else
            {
                var url = GetSiteUrlAbsolute(shortVersionId, path);
                OpenUrl(url);
            }
        }

        public void BuildPackageMetaData(string packageName,string siteFolder = null)
        {

            if (string.IsNullOrEmpty(siteFolder))
                siteFolder = DocumentationSiteRoot;
            
            string metaFolder = Path.Combine(siteFolder, "metadata");
            string packageMetaFolder = Path.Combine(metaFolder, packageName);
            string metaFilePath = Path.Combine(packageMetaFolder, "metadata.json");

            if (!Directory.Exists(metaFolder))
            {
                Directory.CreateDirectory(metaFolder);
            }

            if (!Directory.Exists(packageMetaFolder))
            {
                Directory.CreateDirectory(packageMetaFolder);
            }

            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }

            try
            {
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString("https://packages.unity.com/" + packageName);
                    System.IO.File.WriteAllText(metaFilePath, json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error building metadata for package docs: {e.Message}");
            }
        }

        void SetupMono()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return;

            if (!Directory.Exists(MonoRootPath))
                ZipUtils.Unzip(MonoZip, MonoRootPath);

            var monoBinary = Application.platform == RuntimePlatform.LinuxEditor ?
                "bin/mono-sgen" :
                "bin/mono-sgen64";

            // We check for the specific path to the binary here
            // because earlier versions could have unpacked a mono distribution
            // without linux binaries
            if (Application.platform == RuntimePlatform.LinuxEditor && !File.Exists(Path.Combine(MonoRootPath, monoBinary)))
                ZipUtils.Unzip(MonoLinuxZip, MonoRootPath);
            
            var monoPath = Path.Combine(DocumentationRoot, "mono");
            MonoPath = Path.Combine(monoPath, monoBinary);

            if (!Directory.Exists(monoPath) || !File.Exists(MonoPath))
            {
                FileUtil.DeleteFileOrDirectory(monoPath);
                FileUtil.CopyFileOrDirectory(MonoRootPath, monoPath);
                FileUtils.SetFolderPermission(monoPath, "777");
            }
        }
        bool SetupLicenseSection(string packageFolder, string buildFolder)
        {
            bool validLicenseInPackage = false;
            // Add License section
            string ymlLicenseToc = string.Format("- name: {0}\n  href: LICENSE.md\n", "License");

            // Will create an index.html that can always be linked to that will redirect either to third party license (if any) or the companion license
            string licenseIndexFile = Path.Combine(buildFolder, "license/index.md");
            string licenseIndexName = "LICENSE";

            var licenseFile = Path.Combine(packageFolder, "LICENSE.md");
            if (!File.Exists(licenseFile))
                licenseFile = Path.Combine(packageFolder, "LICENSE");

            if (File.Exists(licenseFile))
            {
                File.Copy(licenseFile, Path.Combine(buildFolder, "license/LICENSE.md"), true);
                validLicenseInPackage = true;
            }
            else
            {
                buildLog += "Error: No valid LICENSE.md file found at the root of the package\n";
                validLicenseInPackage = true;
            }
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

            return validLicenseInPackage;
        }

        void AddXrefMaps(DocFXConfig docFXConfig, PackageInfo packageInfo)
        {
            foreach (var dep in packageInfo.resolvedDependencies)
            {
                var shortID = Documentation.GetShortVersionId(dep.name, dep.version);
                var xrefmapURL = "https://docs.unity3d.com/Packages/" + shortID + "/xrefmap.yml";

                if (FileUtils.CheckAccess(xrefmapURL)) //Can fail if dependency doesn't have published docs yet
                {
                    docFXConfig.AddXrefmapURL(xrefmapURL);
                }
                else //Try the latest published package
                {
                    foreach (var dependentVersion in PacmanUtils.GetVersions(dep.name))
                    {
                        shortID = Documentation.GetShortVersionId(dep.name, dependentVersion);
                        xrefmapURL = "https://docs.unity3d.com/Packages/" + shortID + "/xrefmap.yml";
                        if (FileUtils.CheckAccess(xrefmapURL))
                        {
                            docFXConfig.AddXrefmapURL(xrefmapURL);
                            break;
                        }
                    }
                }
            }
        }

        bool UpdateProgressOrCancel(string message)
        {
            float progress = GlobalSettings.Progress++;
            return EditorUtility.DisplayCancelableProgressBar("Documentation", message, progress/(progress + 4));
        }
    }
}
