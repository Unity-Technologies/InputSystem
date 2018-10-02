#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Caches <see cref="InputControlLayout"/> instances.
    /// </summary>
    /// <remarks>
    /// In the editor we need access to the <see cref="InputControlLayout">InputControlLayouts</see>
    /// registered with the system in order to facilitate various UI features. Instead of
    /// constructing layout instances over and over, we keep them around in here.
    ///
    /// This class is only available in the editor (when <c>UNITY_EDITOR</c> is true).
    /// </remarks>
    public static class EditorInputControlLayoutCache
    {
        /// <summary>
        /// Iterate over all control layouts in the system.
        /// </summary>
        public static IEnumerable<InputControlLayout> allLayouts
        {
            get
            {
                Refresh();
                return s_Cache.table.Values;
            }
        }

        /// <summary>
        /// Iterate over all unique usages and their respective lists of layouts that use them.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> allUsages
        {
            get
            {
                Refresh();
                return s_Usages.Select(pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value.Select(x => x.ToString())));
            }
        }

        public static IEnumerable<InputControlLayout> allControlLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_ControlLayouts)
                    yield return s_Cache.FindOrLoadLayout(name.ToString());
            }
        }

        public static IEnumerable<InputControlLayout> allDeviceLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_DeviceLayouts)
                    yield return s_Cache.FindOrLoadLayout(name.ToString());
            }
        }

        public static IEnumerable<InputControlLayout> allProductLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_ProductLayouts)
                    yield return s_Cache.FindOrLoadLayout(name.ToString());
            }
        }

        /// <summary>
        /// Event that is triggered whenever the layout setup in the system changes.
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

        public static InputControlLayout TryGetLayout(string name)
        {
            Refresh();
            return s_Cache.FindOrLoadLayout(name);
        }

        public static IEnumerable<InputDeviceMatcher> GetDeviceMatchers(string name)
        {
            Refresh();
            InlinedArray<InputDeviceMatcher> matchers;
            s_DeviceMatchers.TryGetValue(new InternedString(name), out matchers);
            return matchers;
        }

        internal static void Clear()
        {
            s_LayoutRegistrationVersion = 0;
            if (s_Cache.table != null)
                s_Cache.table.Clear();
            s_Usages.Clear();
            s_ControlLayouts.Clear();
            s_DeviceLayouts.Clear();
            s_ProductLayouts.Clear();
            s_DeviceMatchers.Clear();
        }

        // If our layout data is outdated, rescan all the layouts in the system.
        internal static void Refresh()
        {
            var manager = InputSystem.s_Manager;
            if (manager.m_LayoutRegistrationVersion == s_LayoutRegistrationVersion)
                return;

            Clear();

            var layoutNames = new List<string>();
            manager.ListControlLayouts(layoutNames);

            s_Cache.layouts = manager.m_Layouts;

            // Remember which layout maps to which device matchers.
            for (var i = 0; i < s_Cache.layouts.layoutMatcherCount; ++i)
            {
                var entry = s_Cache.layouts.layoutMatchers[i];
                var layoutName = entry.Value;
                var matcher = entry.Key;

                InlinedArray<InputDeviceMatcher> matchers;
                s_DeviceMatchers.TryGetValue(layoutName, out matchers);

                matchers.Append(matcher);
                s_DeviceMatchers[layoutName] = matchers;
            }

            // Load and store all layouts.
            for (var i = 0; i < layoutNames.Count; ++i)
            {
                var layout = s_Cache.FindOrLoadLayout(layoutNames[i]);
                ScanLayout(layout);

                if (layout.isControlLayout)
                    s_ControlLayouts.Add(layout.name);
                else if (s_DeviceMatchers.ContainsKey(layout.name))
                    s_ProductLayouts.Add(layout.name);
                else
                    s_DeviceLayouts.Add(layout.name);
            }

            // Move all device layouts without a device description but derived from
            // a layout that has one over to the product list.
            foreach (var name in s_DeviceLayouts)
            {
                var layout = s_Cache.FindOrLoadLayout(name);

                if (layout.m_BaseLayouts.length > 1)
                    throw new NotImplementedException();

                for (var baseLayoutName = layout.baseLayouts.FirstOrDefault(); !baseLayoutName.IsEmpty();)
                {
                    if (s_ProductLayouts.Contains(baseLayoutName))
                    {
                        // Defer removing from s_DeviceLayouts to keep iteration stable.
                        s_ProductLayouts.Add(name);
                        break;
                    }

                    var baseLayout = s_Cache.FindOrLoadLayout(baseLayoutName);
                    if (baseLayout.m_BaseLayouts.length > 1)
                        throw new NotImplementedException();
                    baseLayoutName = baseLayout.baseLayouts.FirstOrDefault();
                }
            }

            // Remove every product device layout now.
            s_DeviceLayouts.ExceptWith(s_ProductLayouts);

            s_LayoutRegistrationVersion = manager.m_LayoutRegistrationVersion;

            if (s_RefreshListeners != null)
                foreach (var listener in s_RefreshListeners)
                    listener();
        }

        private static int s_LayoutRegistrationVersion;
        private static InputControlLayout.Cache s_Cache;
        private static List<Action> s_RefreshListeners;

        private static HashSet<InternedString> s_ControlLayouts = new HashSet<InternedString>();
        private static HashSet<InternedString> s_DeviceLayouts = new HashSet<InternedString>();
        private static HashSet<InternedString> s_ProductLayouts = new HashSet<InternedString>();
        private static Dictionary<InternedString, InlinedArray<InputDeviceMatcher>> s_DeviceMatchers =
            new Dictionary<InternedString, InlinedArray<InputDeviceMatcher>>();

        // We keep a map of all unique usages we find in layouts and also
        // retain a list of the layouts they are used with.
        private static SortedDictionary<InternedString, List<InternedString>> s_Usages =
            new SortedDictionary<InternedString, List<InternedString>>();

        private static void ScanLayout(InputControlLayout layout)
        {
            foreach (var control in layout.controls)
            {
                // Collect unique usages and the layouts used with them.
                foreach (var usage in control.usages)
                {
                    var internedUsage = new InternedString(usage);
                    var internedLayout = new InternedString(control.layout);

                    List<InternedString> layoutList;
                    if (!s_Usages.TryGetValue(internedUsage, out layoutList))
                    {
                        layoutList = new List<InternedString> {internedLayout};
                        s_Usages[internedUsage] = layoutList;
                    }
                    else
                    {
                        var layoutAlreadyInList =
                            layoutList.Any(x => x == internedLayout);
                        if (!layoutAlreadyInList)
                            layoutList.Add(internedLayout);
                    }
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
