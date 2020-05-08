namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal class DocumentationTests
    {
        /* Disabled until package sets are their own packages (or are public)
        private Documentation documentation;
        private MockDocumentationBuilder mock;

        [SetUp]
        public void OneTimeSetup()
        {
            mock = new MockDocumentationBuilder();
            documentation = new Documentation(mock);
        }

        [Test]
        public void Can_FindDocFx()
        {
            Assert.IsTrue(File.Exists(DocumentationBuilder.DocFxExecutable));
        }

        [Test]
        public void Can_FindMonoPath()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.IsTrue(File.Exists(DocumentationBuilder.MonoPath));
        }

        [Test]
        public void Opening_EmbeddedPackage_OpensLocalLink()
        {
            var packageInfo = PackageSets.Instance.Single();
            packageInfo.Origin = PackageOrigin.Embedded;
            documentation.View(packageInfo.Name, packageInfo.ShortVersionId, packageInfo.Origin == PackageOrigin.Embedded, packageInfo.IsCurrent);
            Assert.IsTrue(mock.OpenLocalWasCalled);
        }

        [Test]
        public void Opening_LocalPackage_OpensLocalLink()
        {
            var packageInfo = PackageSets.Instance.Single();
            packageInfo.Origin = PackageOrigin.Registry;
            documentation.View(packageInfo.Name, packageInfo.ShortVersionId, packageInfo.Origin == PackageOrigin.Embedded, packageInfo.IsCurrent);
            Assert.IsFalse(mock.OpenLocalWasCalled);
        }
        */
    }
}
