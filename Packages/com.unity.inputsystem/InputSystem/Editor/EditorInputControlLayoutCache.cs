#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.Switch;
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

        /// <summary>
        /// List the controls that may be present on controls or devices of the given layout by virtue
        /// of being defined in other layouts based on it.
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        public static IEnumerable<OptionalControl> GetOptionalControlsForLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentNullException("layoutName");

            Refresh();

            List<OptionalControl> list;
            if (!s_OptionalControls.TryGetValue(new InternedString(layoutName), out list))
                return Enumerable.Empty<OptionalControl>();

            return list;
        }

        ////TODO: support different resolutions
        public static Texture2D GetIconForLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentNullException("layoutName");

            Refresh();

            // See if we already have it in the cache.
            Texture2D icon;
            var internedName = new InternedString(layoutName);
            if (s_Icons.TryGetValue(internedName, out icon))
                return icon;

            // No, so see if we have an icon on disk for exactly the layout
            // we're looking at (i.e. with the same name).
            var skinPrefix = EditorGUIUtility.isProSkin ? "d_" : "";
            var path = Path.Combine(kIconPath, skinPrefix + layoutName + ".png");
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon != null)
                return icon;

            // No, not that either so start walking up the inheritance chain
            // until we either bump against the ceiling or find an icon.
            var layout = TryGetLayout(layoutName);
            if (layout != null)
            {
                foreach (var baseLayoutName in layout.baseLayouts)
                {
                    ////FIXME: remove this; looks like HIDs lose their base layout info on domain reloads
                    if (string.IsNullOrEmpty(baseLayoutName))
                        continue;

                    icon = GetIconForLayout(baseLayoutName);
                    if (icon != null)
                        return icon;
                }
            }

            // No icon for anything in this layout's chain.
            return null;
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
            s_Icons.Clear();
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

            // Remember which layout maps to which device matchers.
            var layoutMatchers = InputControlLayout.s_Layouts.layoutMatchers;
            for (var i = 0; i < layoutMatchers.Count; ++i)
            {
                var entry = layoutMatchers[i];

                InlinedArray<InputDeviceMatcher> matchers;
                s_DeviceMatchers.TryGetValue(entry.layoutName, out matchers);

                matchers.Append(entry.deviceMatcher);
                s_DeviceMatchers[entry.layoutName] = matchers;
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

        ////REVIEW: is this affected by how the package is installed?
        private const string kIconPath = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/";

        private static int s_LayoutRegistrationVersion;
        private static InputControlLayout.Cache s_Cache;
        private static List<Action> s_RefreshListeners;

        private static HashSet<InternedString> s_ControlLayouts = new HashSet<InternedString>();
        private static HashSet<InternedString> s_DeviceLayouts = new HashSet<InternedString>();
        private static HashSet<InternedString> s_ProductLayouts = new HashSet<InternedString>();
        private static Dictionary<InternedString, List<OptionalControl>> s_OptionalControls =
            new Dictionary<InternedString, List<OptionalControl>>();
        private static Dictionary<InternedString, InlinedArray<InputDeviceMatcher>> s_DeviceMatchers =
            new Dictionary<InternedString, InlinedArray<InputDeviceMatcher>>();
        private static Dictionary<InternedString, Texture2D> s_Icons =
            new Dictionary<InternedString, Texture2D>();

        // We keep a map of all unique usages we find in layouts and also
        // retain a list of the layouts they are used with.
        private static SortedDictionary<InternedString, List<InternedString>> s_Usages =
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
                if (control.isFirstDefinedInThisLayout && !control.isModifyingChildControlByPath && !control.layout.IsEmpty())
                {
                    foreach (var baseLayout in layout.baseLayouts)
                        AddOptionalControlRecursive(baseLayout, ref control);
                }

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

        private static void AddOptionalControlRecursive(InternedString layoutName, ref InputControlLayout.ControlItem controlItem)
        {
            Debug.Assert(!controlItem.isModifyingChildControlByPath);
            Debug.Assert(!controlItem.layout.IsEmpty());

            // Recurse into base.
            InternedString baseLayoutName;
            if (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(layoutName, out baseLayoutName))
                AddOptionalControlRecursive(baseLayoutName, ref controlItem);

            // See if we already have this optional control.
            List<OptionalControl> list;
            var alreadyPresent = false;
            if (!s_OptionalControls.TryGetValue(layoutName, out list))
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
        /// <see cref="DualShockGamepad.acceleration"/> and <see cref="NPad.acceleration"/>). This means gamepads
        /// MAY have a gyro and thus MAY have an "acceleration" control.
        ///
        /// In bindings (<see cref="InputBinding"/>), it is perfectly valid to deal with this opportunistically
        /// and create a binding to <c>"{Gamepad}/acceleration"</c> which will bind correctly IF the gamepad has
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
