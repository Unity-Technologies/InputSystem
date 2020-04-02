#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal static class UIUtils
    {
        private const string DisplayNone = "display-none";

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (value)
                element.RemoveFromClassList(DisplayNone);
            else
                element.AddToClassList(DisplayNone);

            element.visible = value;
        }
    }
}
