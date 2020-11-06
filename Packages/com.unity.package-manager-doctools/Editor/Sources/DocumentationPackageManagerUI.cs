using System;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal class DocumentationPackageManagerUI : VisualElement
    {
        public const string PackagePath = "Packages/com.unity.package-manager-doctools/";
        public const string ResourcesPath = PackagePath + "Editor/Resources/";
        public const string ReportDir = "Logs/DocToolReports";
        private const string TemplatePath = ResourcesPath + "Templates/DocumentationExtension.uxml";

        private readonly VisualElement root;
        private PackageInfo packageInfo;
        private string ReportPath;

        public static DocumentationPackageManagerUI CreateUI()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);
            return asset == null ? null : new DocumentationPackageManagerUI(asset);
        }

        private DocumentationPackageManagerUI(VisualTreeAsset asset)
        {
#if UNITY_2019_1_OR_NEWER
            root = asset.CloneTree();
            var resourcesRoot = "packages/com.unity.package-manager-doctools/Editor/Resources/";
            string path = EditorGUIUtility.isProSkin ? resourcesRoot + "Styles/Dark.uss" : resourcesRoot + "Styles/Light.uss";
            var styleSheet = EditorGUIUtility.Load(path) as StyleSheet;
            root.styleSheets.Add(styleSheet);
#else
            root = asset.CloneTree(null);
            root.AddStyleSheetPath(EditorGUIUtility.isProSkin ? "Styles/Dark" : "Styles/Light");
#endif
            Add(root);

            GenerateButton.clickable.clicked += GenerateDocClick;
            ReportButton.clickable.clicked += ReportDocClick;
            DebugState.RegisterValueChangedCallback(evt => DebugToggle());
            Verbose.RegisterValueChangedCallback(evt => VerbosityToggle());
            ServeState.RegisterValueChangedCallback(evt => ServeToggle());
            OutputPath.RegisterValueChangedCallback((evt) => PathChange());
            Verbose.value = GlobalSettings.Validate;
            DebugState.value = GlobalSettings.Debug;
            ServeState.value  = GlobalSettings.ServeAfterGeneration;
            OutputPath.value = GlobalSettings.DestinationPath;
        }

        private void PathChange()
        {
            GlobalSettings.DestinationPath = OutputPath.value;
        }

        private void VerbosityToggle()
        {
            GlobalSettings.Validate = Verbose.value;
        }
        private void DebugToggle()
        {
            GlobalSettings.Debug = DebugState.value;
        }
        private void ServeToggle()
        {
            GlobalSettings.ServeAfterGeneration = ServeState.value;
        }

        public void OnPackageChanged(PackageInfo package)
        {
            if (package == null)
                return;

            packageInfo = package;
            UpdateErrorReportButton();
        }

        // Get the latest version so that we can always redirect from the web to the latest version
        //         eg: Don't use preview version in the returned list, unless there is no other versions
        private string LatestRelease(PackageInfo packageInfo)
        {
            var latestRelease = packageInfo.versions.all.Where(v => !v.Contains("-"));
            if (!latestRelease.Any())
                return packageInfo.versions.latest;

            return latestRelease.LastOrDefault() ?? string.Empty;
        }

        private void UpdateErrorReportButton()
        {
            if (packageInfo == null)
                return;

            ReportPath = System.IO.Path.Combine(DocumentationPackageManagerUI.ReportDir,
                packageInfo.name +
                "@" + packageInfo.version +
                ".txt");

            if (System.IO.File.Exists(ReportPath))
            {
                ReportButton.SetEnabled(true);
            }
            else
            {
                ReportButton.SetEnabled(false);
            }
        }

        private void ReportDocClick()
        {
            System.Diagnostics.Process.Start(ReportPath);
        }

        private void GenerateDocClick()
        {
            if (packageInfo == null)
                return;

            GlobalSettings.Progress = 0;
            // Get latest version
            string latestShortVersionId = null;
            string latestAbsoluteVersionId = null;    // Can be a preview
            if (!string.IsNullOrEmpty(packageInfo.versions.latest))
            {
                latestShortVersionId = Documentation.GetShortVersionId(packageInfo.name, LatestRelease(packageInfo));
                latestAbsoluteVersionId = Documentation.GetShortVersionId(packageInfo.name, packageInfo.versions.latest);
            }

            string shortVersionId = Documentation.GetShortVersionId(packageInfo.name, packageInfo.version);

            var buildLog = Documentation.Instance.GenerateFullSite(packageInfo,
                                                    shortVersionId,
                                                    packageInfo.source == PackageSource.Embedded,
                                                    latestShortVersionId,
                                                    latestAbsoluteVersionId,
                                                    GlobalSettings.ServeAfterGeneration);
            Validator.Validate(buildLog);
            UpdateErrorReportButton();
        }

       

        private Button GenerateButton { get { return root.Q<Button>("generateButton");} }
        private Toggle Verbose { get { return root.Q<Toggle>("validate");} }
        private Button ReportButton { get { return root.Q<Button>("reportButton"); } }
        private Toggle ServeState { get { return root.Q<Toggle>("serveState"); } }
        private TextField OutputPath { get { return root.Q<TextField>("outputPath"); } }
        private Toggle DebugState { get { return root.Q<Toggle>("debugDocGeneration"); } }
    }
}
