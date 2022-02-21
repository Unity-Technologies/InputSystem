#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
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
    internal static class EditorInputControlLayoutCache
    {
        /// <summary>
        /// Iterate over all control layouts in the system.
        /// </summary>
        public static IEnumerable<InputControlLayout> allLayouts
        {
            get
            {
                Refresh();
                return InputControlLayout.cache.table.Values;
            }
        }

        /// <summary>
        /// Iterate over all unique usages and their respective lists of layouts that use them.
        /// </summary>
        public static IEnumerable<Tuple<string, IEnumerable<string>>> allUsages
        {
            get
            {
                Refresh();
                return s_Usages.Select(pair => new Tuple<string, IEnumerable<string>>(pair.Key, pair.Value.Select(x => x.ToString())));
            }
        }

        public static IEnumerable<InputControlLayout> allControlLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_ControlLayouts)
                    yield return InputControlLayout.cache.FindOrLoadLayout(name.ToString());
            }
        }

        public static IEnumerable<InputControlLayout> allDeviceLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_DeviceLayouts)
                    yield return InputControlLayout.cache.FindOrLoadLayout(name.ToString());
            }
        }

        public static IEnumerable<InputControlLayout> allProductLayouts
        {
            get
            {
                Refresh();
                foreach (var name in s_ProductLayouts)
                    yield return InputControlLayout.cache.FindOrLoadLayout(name.ToString());
            }
        }

        public static InputControlLayout TryGetLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentException("Layout name cannot be null or empty", nameof(layoutName));

            Refresh();
            return InputControlLayout.cache.FindOrLoadLayout(layoutName, throwIfNotFound: false);
        }

        public static Type GetValueType(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentException("Layout name cannot be null or empty", nameof(layoutName));

            // Load layout.
            var layout = TryGetLayout(layoutName);
            if (layout == null)
                return null;

            // Grab type.
            var type = layout.type;
            Debug.Assert(type != null, "Layout should have associated type");
            Debug.Assert(typeof(InputControl).IsAssignableFrom(type),
                "Layout's associated type should be derived from InputControl");

            return layout.GetValueType();
        }

        public static IEnumerable<InputDeviceMatcher> GetDeviceMatchers(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentException("Layout name cannot be null or empty", nameof(layoutName));

            Refresh();
            s_DeviceMatchers.TryGetValue(new InternedString(layoutName), out var matchers);
            return matchers;
        }

        public static string GetDisplayName(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentException("Layout name cannot be null or empty", nameof(layoutName));

            var layout = TryGetLayout(layoutName);
            if (layout == null)
                return layoutName;

            if (!string.IsNullOrEmpty(layout.displayName))
                return layout.displayName;
            return layout.name;
        }

        /// <summary>
        /// List the controls that may be present on controls or devices of the given layout by virtue
        /// of being defined in other layouts based on it.
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        public static IEnumerable<OptionalControl> GetOptionalControlsForLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentException("Layout name cannot be null or empty", nameof(layoutName));

            Refresh();

            if (!s_OptionalControls.TryGetValue(new InternedString(layoutName), out var list))
                return Enumerable.Empty<OptionalControl>();

            return list;
        }

        public static Texture2D GetIconForLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentNullException(nameof(layoutName));

            Refresh();

            // See if we already have it in the cache.
            var internedName = new InternedString(layoutName);
            if (s_Icons.TryGetValue(internedName, out var icon))
                return icon;

            // No, so see if we have an icon on disk for exactly the layout
            // we're looking at (i.e. with the same name).
            icon = GUIHelpers.LoadIcon(layoutName);
            if (icon != null)
            {
                s_Icons.Add(internedName, icon);
                return icon;
            }

            // No, not that either so start walking up the inheritance chain
            // until we either bump against the ceiling or find an icon.
            var layout = TryGetLayout(layoutName);
            if (layout != null)
            {
                foreach (var baseLayoutName in layout.baseLayouts)
                {
                    icon = GetIconForLayout(baseLayoutName);
                    if (icon != null)
                        return icon;
                }

                // If it's a control and there's no specific icon, return a generic one.
                if (layout.isControlLayout)
                {
                    var genericIcon = GUIHelpers.LoadIcon("InputControl");
                    if (genericIcon != null)
                    {
                        s_Icons.Add(internedName, genericIcon);
                        return genericIcon;
                    }
                }
            }

            // No icon for anything in this layout's chain.
            return null;
        }

        public struct ControlSearchResult
        {
            public string controlPath;
            public InputControlLayout layout;
            public InputControlLayout.ControlItem item;
        }

        internal static void Clear()
        {
            s_LayoutRegistrationVersion = 0;
            s_LayoutCacheRef.Dispose();
            s_Usages.Clear();
            s_ControlLayouts.Clear();
            s_DeviceLayouts.Clear();
            s_ProductLayouts.Clear();
            s_DeviceMatchers.Clear();
            s_Icons.Clear();
        }

        // If our layout data is outdated, rescan all the layouts in the system.
        private static void Refresh()
        {
            var manager = InputSystem.s_Manager;
            if (manager.m_LayoutRegistrationVersion == s_LayoutRegistrationVersion)
                return;

            Clear();

            if (!s_LayoutCacheRef.valid)
            {
                // In the editor, we keep a permanent reference on the global layout
                // cache. Means that in the editor, we always have all layouts loaded in full
                // at all times whereas in the player, we load layouts only while we need
                // them and then release them again.
                s_LayoutCacheRef = InputControlLayout.CacheRef();
            }

            var layoutNames = manager.ListControlLayouts().ToArray();

            // Remember which layout maps to which device matchers.
            var layoutMatchers = InputControlLayout.s_Layouts.layoutMatchers;
            foreach (var entry in layoutMatchers)
            {
                s_DeviceMatchers.TryGetValue(entry.layoutName, out var matchers);

                matchers.Append(entry.deviceMatcher);
                s_DeviceMatchers[entry.layoutName] = matchers;
            }

            // Load and store all layouts.
            foreach (var layoutName in layoutNames)
            {
                ////FIXME: does not protect against exceptions
                var layout = InputControlLayout.cache.FindOrLoadLayout(layoutName, throwIfNotFound: false);
                if (layout == null)
                    continue;

                ScanLayout(layout);

                if (layout.isOverride)
                    continue;

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
                var layout = InputControlLayout.cache.FindOrLoadLayout(name);

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

                    var baseLayout = InputControlLayout.cache.FindOrLoadLayout(baseLayoutName, throwIfNotFound: false);
                    if (baseLayout == null)
                        continue;
                    if (baseLayout.m_BaseLayouts.length > 1)
                        throw new NotImplementedException();
                    baseLayoutName = baseLayout.baseLayouts.FirstOrDefault();
                }
            }

            // Remove every product device layout now.
            s_DeviceLayouts.ExceptWith(s_ProductLayouts);

            s_LayoutRegistrationVersion = manager.m_LayoutRegistrationVersion;
        }

        private static int s_LayoutRegistrationVersion;
        private static InputControlLayout.CacheRefInstance s_LayoutCacheRef;

        private static readonly HashSet<InternedString> s_ControlLayouts = new HashSet<InternedString>();
        private static readonly HashSet<InternedString> s_DeviceLayouts = new HashSet<InternedString>();
        private static readonly HashSet<InternedString> s_ProductLayouts = new HashSet<InternedString>();
        private static readonly Dictionary<InternedString, List<OptionalControl>> s_OptionalControls =
            new Dictionary<InternedString, List<OptionalControl>>();
        private static readonly Dictionary<InternedString, InlinedArray<InputDeviceMatcher>> s_DeviceMatchers =
            new Dictionary<InternedString, InlinedArray<InputDeviceMatcher>>();
        private static Dictionary<InternedString, Texture2D> s_Icons =
            new Dictionary<InternedString, Texture2D>();

        // We keep a map of all unique usages we find in layouts and also
        // retain a list of the layouts they are used with.
        private static readonly SortedDictionary<InternedString, List<InternedString>> s_Usages =
            new SortedDictionary<InternedString, List<InternedString>>();

        private static void ScanLayout(InputControlLayout layout)
        {
            var controls = layout.controls;
            for (var i = 0; i < controls.Count; ++i)
            {
                var control = controls[i];

                // If it's not just a control modifying some inner child control, add control to all base
                // layouts as an optional control.
                //
                // NOTE: We're looking at layouts post-merging here. Means we have already picked up all the
                //       controls present on the base.
                if (control.isFirstDefinedInThisLayout && !control.isModifyingExistingControl && !control.layout.IsEmpty())
                {
                    foreach (var baseLayout in layout.baseLayouts)
                        AddOptionalControlRecursive(baseLayout, ref control);
                }

                // Collect unique usages and the layouts used with them.
                foreach (var usage in control.usages)
                {
                    // Empty usages can occur for controls that want to reset inherited usages.
                    if (string.IsNullOrEmpty(usage))
                        continue;

                    var internedUsage = new InternedString(usage);
                    var internedLayout = new InternedString(control.layout);

                    if (!s_Usages.TryGetValue(internedUsage, out var layoutList))
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

        private static void AddOptionalControlRecursive(InternedString layoutName, ref InputControlLayout.ControlItem controlItem)
        {
            Debug.Assert(!controlItem.isModifyingExistingControl);
            Debug.Assert(!controlItem.layout.IsEmpty());

            // Recurse into base.
            if (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(layoutName, out var baseLayoutName))
                AddOptionalControlRecursive(baseLayoutName, ref controlItem);

            // See if we already have this optional control.
            var alreadyPresent = false;
            if (!s_OptionalControls.TryGetValue(layoutName, out var list))
            {
                list = new List<OptionalControl>();
                s_OptionalControls[layoutName] = list;
            }
            else
            {
                // See if we already have this control.
                foreach (var item in list)
                {
                    if (item.name == controlItem.name && item.layout == controlItem.layout)
                    {
                        alreadyPresent = true;
                        break;
                    }
                }
            }
            if (!alreadyPresent)
                list.Add(new OptionalControl {name = controlItem.name, layout = controlItem.layout});
        }

        /// <summary>
        /// An optional control is a control that is not defined on a layout but which is defined
        /// on a derived layout.
        /// </summary>
        /// <remarks>
        /// An example is the "acceleration" control defined by some layouts based on <see cref="Gamepad"/> (e.g.
        /// <see cref="DualShockGamepad.acceleration"/>. This means gamepads
        /// MAY have a gyro and thus MAY have an "acceleration" control.
        ///
        /// In bindings (<see cref="InputBinding"/>), it is perfectly valid to deal with this opportunistically
        /// and create a binding to <c>"&lt;Gamepad&gt;/acceleration"</c> which will bind correctly IF the gamepad has
        /// an acceleration control but will do nothing if it doesn't.
        ///
        /// The concept of optional controls permits setting up such bindings in the UI by making controls that
        /// are present on more specific layouts than the one currently looked at available directly on the
        /// base layout.
        /// </remarks>
        public struct OptionalControl
        {
            public InternedString name;
            public InternedString layout;
            ////REVIEW: do we want to have the list of layouts that define the control?
        }
    }
}
#endif // UNITY_EDITOR
