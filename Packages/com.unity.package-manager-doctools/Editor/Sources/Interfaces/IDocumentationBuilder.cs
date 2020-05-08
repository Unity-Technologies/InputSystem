namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal interface IDocumentationBuilder
    {
        void Build(string packageName, string shortVersionId, string siteFolder = null);
        void OpenPackageUrl(string packageName, string shortVersionId, bool isEmbedded, bool isInstalled, string path = "index.html");
        void BuildWithProgress(string packageName, string shortVersionId, string siteFolder = null);
        void BuildRedirectPage(string sitefolder, string redirectUrl, string absoluteLatestUrl = "", string outputFilePath = "index.html", string redirectDefaultPath = "index.html");
        bool TryBuildRedirectToManual(string packageName, string shortVersionId);
        void BuildRedirectToLatestPage(string packageName , string latestShortVersionId, string absoluteLatestShortVersionId = "", string siteFolder = null);
        string GetPackageSiteFolder(string shortVersionId);
        void OpenLocalPackageUrl(string packageName, string shortVersionId, bool isEmbedded, string path = "index.html", bool doNotRebuild = false);
        void OpenUrl(string url);
        bool CheckForInternetConnection();
    }
}
