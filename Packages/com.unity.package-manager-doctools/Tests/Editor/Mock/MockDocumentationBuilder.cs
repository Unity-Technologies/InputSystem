namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal class MockDocumentationBuilder : DocumentationBuilder
    {
        public bool OpenLocalWasCalled = false;
        public bool IsConnected = true;

        public MockDocumentationBuilder()
        {
        }

        public override bool CheckForInternetConnection() {return IsConnected;}


        // Don't do anything
        public override void OpenUrl(string url) {}

        public override void OpenLocalPackageUrl(string packageName, string shortVersionId, bool isEmbedded, string path, bool doNotRebuild)
        {
            OpenLocalWasCalled = true;
        }
    }
}
