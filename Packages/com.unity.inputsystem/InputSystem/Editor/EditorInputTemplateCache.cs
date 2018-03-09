#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ISX.Utilities;

namespace ISX.Editor
{
    /// <summary>
    /// Caches <see cref="InputTemplate"/> instances.
    /// </summary>
    /// <remarks>
    /// In the editor we need access to the <see cref="InputTemplate">InputTemplates</see>
    /// registered with the system in order to facilitate various UI features. Instead of
    /// constructing template instances over and over, we keep them around in here.
    ///
    /// This class is only available in the editor (when <c>UNITY_EDITOR</c> is true).
    /// </remarks>
    public static class EditorInputTemplateCache
    {
        /// <summary>
        /// Iterate over all templates in the system.
        /// </summary>
        public static IEnumerable<InputTemplate> allTemplates
        {
            get
            {
                Refresh();
                return s_Cache.table.Values;
            }
        }

        /// <summary>
        /// Iterate over all unique usages and their respective lists of templates that use them.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> allUsages
        {
            get
            {
                Refresh();
                return s_Usages.Select(pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value.Select(x => x.ToString())));
            }
        }

        /// <summary>
        /// Iterate over all device templates that do not extend other templates.
        /// </summary>
        public static IEnumerable<InputTemplate> allBaseDeviceTemplates
        {
            get
            {
                foreach (var template in allTemplates)
                    if (string.IsNullOrEmpty(template.extendsTemplate) &&
                        typeof(InputDevice).IsAssignableFrom(template.type))
                        yield return template;
            }
        }

        /// <summary>
        /// Iterate over all device templates that don't try to match specific products.
        /// </summary>
        public static IEnumerable<InputTemplate> allNonProductTemplates
        {
            get
            {
                foreach (var template in allTemplates)
                {
                    if (!typeof(InputDevice).IsAssignableFrom(template.type))
                        continue;

                    var deviceDescription = template.deviceDescription;
                    if (string.IsNullOrEmpty(deviceDescription.product) &&
                        string.IsNullOrEmpty(deviceDescription.manufacturer))
                        yield return template;
                }
            }
        }

        /// <summary>
        /// Event that is triggered whenever the template setup in the system changes.
        /// </summary>
        public static event Action onRefresh
        {
            add
            {
                if (s_RefreshListeners == null)
                    s_RefreshListeners = new List<Action>();
                s_RefreshListeners.Add(value);
            }
            remove
            {
                if (s_RefreshListeners != null)
                    s_RefreshListeners.Remove(value);
            }
        }

        public static InputTemplate TryGetTemplate(string name)
        {
            return s_Cache.FindOrLoadTemplate(name);
        }

        internal static void Clear()
        {
            s_TemplateSetupVersion = 0;
            if (s_Cache.table != null)
                s_Cache.table.Clear();
            s_Usages.Clear();
        }

        // If our template data is outdated, rescan all the templates in the system.
        internal static void Refresh()
        {
            var manager = InputSystem.s_Manager;
            if (manager.m_TemplateSetupVersion == s_TemplateSetupVersion)
                return;

            Clear();

            var templateNames = new List<string>();
            manager.ListTemplates(templateNames);

            s_Cache.templates = manager.m_Templates;
            for (var i = 0; i < templateNames.Count; ++i)
            {
                var template = s_Cache.FindOrLoadTemplate(templateNames[i]);
                ScanTemplate(template);
            }

            s_TemplateSetupVersion = manager.m_TemplateSetupVersion;

            if (s_RefreshListeners != null)
                foreach (var listener in s_RefreshListeners)
                    listener();
        }

        private static int s_TemplateSetupVersion;
        private static InputTemplate.Cache s_Cache;
        private static List<Action> s_RefreshListeners;

        // We keep a map of all unique usages we find in templates and also
        // retain a list of the templates they are used with.
        private static SortedDictionary<InternedString, List<InternedString>> s_Usages =
            new SortedDictionary<InternedString, List<InternedString>>();

        private static void ScanTemplate(InputTemplate template)
        {
            foreach (var control in template.controls)
            {
                // Collect unique usages and the templates used with them.
                foreach (var usage in control.usages)
                {
                    var internedUsage = new InternedString(usage);
                    var internedControlTemplate = new InternedString(control.template);

                    List<InternedString> templateList;
                    if (!s_Usages.TryGetValue(internedUsage, out templateList))
                    {
                        templateList = new List<InternedString> {internedControlTemplate};
                        s_Usages[internedUsage] = templateList;
                    }
                    else
                    {
                        var templateAlreadyInList =
                            templateList.Any(x => x == internedControlTemplate);
                        if (!templateAlreadyInList)
                            templateList.Add(internedControlTemplate);
                    }
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
