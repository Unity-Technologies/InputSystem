namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal interface IDocumentationBuilder
    {
        string Build(PackageInfo packageInfo, string shortVersionId, string siteFolder = null);
        void OpenPackageUrl(PackageInfo packageInfo, string shortVersionId, bool isEmbedded, bool isInstalled, string path = "index.html");
        string BuildWithProgress(PackageInfo packageInfo, string shortVersionId, string siteFolder = null);
        void BuildRedirectPage(string sitefolder, string redirectUrl, string absoluteLatestUrl = "", string outputFilePath = "index.html", string redirectDefaultPath = "index.html");
        bool TryBuildRedirectToManual(string packageName, string shortVersionId, string siteFolder = null);
        void BuildRedirectToLatestPage(string packageName , string latestShortVersionId, string absoluteLatestShortVersionId = "", string siteFolder = null);
        string GetPackageSiteFolder(string shortVersionId, string siteFolder = null);
        void OpenLocalPackageUrl(PackageInfo packageInfo, string shortVersionId, bool isEmbedded, string path = "index.html", bool doNotRebuild = false);
        void OpenUrl(string url);
        bool CheckForInternetConnection();

        void BuildPackageMetaData(string packageName, string siteFolder = null);
    }
}
