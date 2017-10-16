#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace ISX
{
    // In the editor we need access to the InputTemplates registered with
    // the system in order to facilitate various UI features. Instead of
    // constructing template instances over and over, we keep them around
    // in here.
    public static class EditorInputTemplateCache
    {
        // Iterate over all templates in the system.
        public static IEnumerable<InputTemplate> allTemplates
        {
            get
            {
                Refresh();
                return s_Cache.table.Values;
            }
        }

        // Get all unique usages and the list of templates used with each one.
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> allUsages
        {
            get
            {
                Refresh();
                return s_Usages.Select(pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));
            }
        }

        // Iterate over all device templates that do not extend
        // other templates.
        public static IEnumerable<InputTemplate> allBaseDeviceTemplates
        {
            get
            {
                Refresh();
                foreach (var template in s_Cache.table.Values)
                    if (string.IsNullOrEmpty(template.extendsTemplate) &&
                        typeof(InputDevice).IsAssignableFrom(template.type))
                        yield return template;
            }
        }

        public static InputTemplate TryGetTemplate(string name)
        {
            return s_Cache.FindOrLoadTemplate(name);
        }

        internal static void Clear()
        {
            s_TemplateSetupVersion = 0;
            s_Cache.table?.Clear();
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

            for (var i = 0; i < templateNames.Count; ++i)
            {
                var template = s_Cache.FindOrLoadTemplate(templateNames[i]);
                ScanTemplate(template);
            }

            s_TemplateSetupVersion = manager.m_TemplateSetupVersion;
        }

        private static int s_TemplateSetupVersion;
        private static InputTemplate.Cache s_Cache;

        // We keep a map of all unique usages we find in templates and also
        // retain a list of the templates they are used with.
        private static SortedDictionary<string, List<string>> s_Usages = new SortedDictionary<string, List<string>>();

        private static void ScanTemplate(InputTemplate template)
        {
            foreach (var control in template.controls)
            {
                // Collect unique usages and the templates used with them.
                foreach (var usage in control.usages)
                {
                    var usageLowerCase = usage.ToLower();

                    List<string> templateList;
                    if (!s_Usages.TryGetValue(usageLowerCase, out templateList))
                    {
                        templateList = new List<string> {control.template};
                        s_Usages[usageLowerCase] = templateList;
                    }
                    else
                    {
                        var templateAlreadyInList =
                            templateList.Any(x =>
                                string.Compare(x, control.template, StringComparison.InvariantCultureIgnoreCase) == 0);
                        if (!templateAlreadyInList)
                            templateList.Add(control.template);
                    }
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
