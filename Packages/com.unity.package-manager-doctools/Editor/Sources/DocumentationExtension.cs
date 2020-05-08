using UnityEditor.PackageManager.UI;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    [InitializeOnLoad]
    internal class DocumentationExtension : IPackageManagerExtension
    {
        private PackageInfo packageInfo;
        private DocumentationPackageManagerUI ui;

        public VisualElement CreateExtensionUI()
        {
            return ui ?? (ui = DocumentationPackageManagerUI.CreateUI()) ?? new VisualElement();
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == this.packageInfo)
                return;

            this.packageInfo = packageInfo;

            if (ui != null)
                ui.OnPackageChanged(this.packageInfo);
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
        public void OnPackageRemoved(PackageInfo packageInfo) {}

        static DocumentationExtension()
        {
            PackageManagerExtensions.RegisterExtension(new DocumentationExtension());
        }
    }
}
