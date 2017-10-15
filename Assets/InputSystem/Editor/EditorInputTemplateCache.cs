#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace ISX
{
    // In the editor we need access to the InputTemplates registered with
    // the system in order to facilitate various UI features. Instead of
    // constructing template instances over and over, we keep them around
    // in here.
    //
    // NOTE: The cache automatically refreshes if the set of registered
    //       templates changes.
    public static class EditorInputTemplateCache
    {
        // Iterate over all templates in the system.
        public static IEnumerable<InputTemplate> allTemplates
        {
            get
            {
                Initialize();
                return s_Cache.table.Values;
            }
        }

        // Iterate over all device templates that do not extend
        // other templates.
        public static IEnumerable<InputTemplate> baseDeviceTemplates
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static void Flush()
        {
        }

        private static bool s_Initialized;
        private static InputTemplate.Cache s_Cache;

        // We keep a map of all unique usages we find in templates and also
        // retain a list of the templates they are used with.
        private static Dictionary<string, List<string>> s_Usages;

        private static void Initialize()
        {
            if (s_Initialized)
                return;

            var templateNames = InputSystem.ListTemplates();
            foreach (var name in templateNames)
                s_Cache.FindOrLoadTemplate(name);

            s_Initialized = false;
        }
    }
}

#endif // UNITY_EDITOR
