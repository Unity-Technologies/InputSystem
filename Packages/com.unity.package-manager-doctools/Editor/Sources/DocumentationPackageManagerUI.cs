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
        private const string TemplatePath = ResourcesPath + "Templates/DocumentationExtension.uxml";

        private readonly VisualElement root;
        private PackageInfo packageInfo;

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

            if (!Unsupported.IsDeveloperMode())
                Verbose.visible = false;

            GenerateButton.clickable.clicked += GenerateDocClick;
            Verbose.RegisterValueChangedCallback(evt => VerbosityToggle());
            VerbosityToggle();
        }

        private void VerbosityToggle()
        {
            GlobalSettings.Verbose = Verbose.value;
        }

        public void OnPackageChanged(PackageInfo package)
        {
            if (package == null)
                return;

            packageInfo = package;
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

        private void GenerateDocClick()
        {
            if (packageInfo == null)
                return;

            // Get latest version
            string latestShortVersionId = null;
            string latestAbsoluteVersionId = null;    // Can be a preview
            if (!string.IsNullOrEmpty(packageInfo.versions.latest))
            {
                latestShortVersionId = Documentation.GetShortVersionId(packageInfo.name, LatestRelease(packageInfo));
                latestAbsoluteVersionId = Documentation.GetShortVersionId(packageInfo.name, packageInfo.versions.latest);
            }

            string shortVersionId = Documentation.GetShortVersionId(packageInfo.name, packageInfo.version);

            Documentation.Instance.GenerateFullSite(packageInfo.name, shortVersionId, packageInfo.source == PackageSource.Embedded, latestShortVersionId, latestAbsoluteVersionId);
        }

        private Button GenerateButton { get { return root.Q<Button>("generateButton");} }
        private Toggle Verbose { get { return root.Q<Toggle>("verbose");} }
    }
}
