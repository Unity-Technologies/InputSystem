#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal class MatchingControlPath
    {
        public string deviceName
        {
            get;
        }
        public string controlName
        {
            get;
        }

        public bool isRoot
        {
            get;
        }
        public List<MatchingControlPath> children
        {
            get;
        }


        public MatchingControlPath(string deviceName, string controlName, bool isRoot)
        {
            this.deviceName = deviceName;
            this.controlName = controlName;
            this.isRoot = isRoot;
            this.children = new List<MatchingControlPath>();
        }

#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public static List<TreeViewItemData<MatchingControlPath>> BuildMatchingControlPathsTreeData(List<MatchingControlPath> matchingControlPaths)
        {
            int id = 0;
            return BuildMatchingControlPathsTreeDataRecursive(ref id, matchingControlPaths);
        }

        private static List<TreeViewItemData<MatchingControlPath>> BuildMatchingControlPathsTreeDataRecursive(ref int id, List<MatchingControlPath> matchingControlPaths)
        {
            var treeViewList = new List<TreeViewItemData<MatchingControlPath>>(matchingControlPaths.Count);
            foreach (var matchingControlPath in matchingControlPaths)
            {
                var childTreeViewList = BuildMatchingControlPathsTreeDataRecursive(ref id, matchingControlPath.children);

                var treeViewItem = new TreeViewItemData<MatchingControlPath>(id++, matchingControlPath, childTreeViewList);
                treeViewList.Add(treeViewItem);
            }

            return treeViewList;
        }

#endif

        public static List<MatchingControlPath> CollectMatchingControlPaths(string path, bool showPaths, ref bool controlPathUsagePresent)
        {
            var matchingControlPaths = new List<MatchingControlPath>();

            if (path == string.Empty)
                return matchingControlPaths;

            var deviceLayoutPath = InputControlPath.TryGetDeviceLayout(path);
            var parsedPath = InputControlPath.Parse(path).ToArray();

            // If the provided path is parseable into device and control components, draw UI which shows control layouts that match the path.
            if (parsedPath.Length >= 2 && !string.IsNullOrEmpty(deviceLayoutPath))
            {
                bool matchExists = false;

                var rootDeviceLayout = EditorInputControlLayoutCache.TryGetLayout(deviceLayoutPath);
                bool isValidDeviceLayout = deviceLayoutPath == InputControlPath.Wildcard || (rootDeviceLayout != null && !rootDeviceLayout.isOverride && !rootDeviceLayout.hideInUI);
                // Exit early if a malformed device layout was provided,
                if (!isValidDeviceLayout)
                    return matchingControlPaths;

                controlPathUsagePresent = parsedPath[1].usages.Count() > 0;
                bool hasChildDeviceLayouts = deviceLayoutPath == InputControlPath.Wildcard || EditorInputControlLayoutCache.HasChildLayouts(rootDeviceLayout.name);

                // If the path provided matches exactly one control path (i.e. has no ui-facing child device layouts or uses control usages), then exit early
                if (!controlPathUsagePresent && !hasChildDeviceLayouts)
                    return matchingControlPaths;

                // Otherwise, we will show either all controls that match the current binding (if control usages are used)
                // or all controls in derived device layouts (if a no control usages are used).

                // If our control path contains a usage, make sure we render the binding that belongs to the root device layout first
                if (deviceLayoutPath != InputControlPath.Wildcard && controlPathUsagePresent)
                {
                    matchExists |= CollectMatchingControlPathsForLayout(rootDeviceLayout, in parsedPath, true, matchingControlPaths);
                }
                // Otherwise, just render the bindings that belong to child device layouts. The binding that matches the root layout is
                // already represented by the user generated control path itself.
                else
                {
                    IEnumerable<InputControlLayout> matchedChildLayouts = Enumerable.Empty<InputControlLayout>();
                    if (deviceLayoutPath == InputControlPath.Wildcard)
                    {
                        matchedChildLayouts = EditorInputControlLayoutCache.allLayouts
                            .Where(x => x.isDeviceLayout && !x.hideInUI && !x.isOverride && x.isGenericTypeOfDevice && x.baseLayouts.Count() == 0).OrderBy(x => x.displayName);
                    }
                    else
                    {
                        matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(rootDeviceLayout.name);
                    }

                    foreach (var childLayout in matchedChildLayouts)
                    {
                        matchExists |= CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, matchingControlPaths);
                    }
                }

                // Otherwise, indicate that no layouts match the current path.
                if (!matchExists)
                {
                    return null;
                }
            }

            return matchingControlPaths;
        }

        /// <summary>
        /// Returns true if the deviceLayout or any of its children has controls which match the provided parsed path. exist matching registered control paths.
        /// </summary>
        /// <param name="deviceLayout">The device layout to draw control paths for</param>
        /// <param name="parsedPath">The parsed path containing details of the Input Controls that can be matched</param>
        private static bool CollectMatchingControlPathsForLayout(InputControlLayout deviceLayout, in InputControlPath.ParsedPathComponent[] parsedPath, bool isRoot, List<MatchingControlPath> matchingControlPaths)
        {
            string deviceName = deviceLayout.displayName;
            string controlName = string.Empty;
            bool matchExists = false;

            for (int i = 0; i < deviceLayout.m_Controls.Length; i++)
            {
                ref InputControlLayout.ControlItem controlItem = ref deviceLayout.m_Controls[i];
                if (InputControlPath.MatchControlComponent(ref parsedPath[1], ref controlItem, true))
                {
                    // If we've already located a match, append a ", " to the control name
                    // This is to accomodate cases where multiple control items match the same path within a single device layout
                    // Note, some controlItems have names but invalid displayNames (i.e. the Dualsense HID > leftTriggerButton)
                    // There are instance where there are 2 control items with the same name inside a layout definition, however they are not
                    // labeled significantly differently.
                    // The notable example is that the Android Xbox and Android Dualshock layouts have 2 d-pad definitions, one is a "button"
                    // while the other is an axis.
                    controlName += matchExists ? $", {controlItem.name}" : controlItem.name;

                    // if the parsePath has a 3rd component, try to match it with items in the controlItem's layout definition.
                    if (parsedPath.Length == 3)
                    {
                        var controlLayout = EditorInputControlLayoutCache.TryGetLayout(controlItem.layout);
                        if (controlLayout.isControlLayout && !controlLayout.hideInUI)
                        {
                            for (int j = 0; j < controlLayout.m_Controls.Count(); j++)
                            {
                                ref InputControlLayout.ControlItem controlLayoutItem = ref controlLayout.m_Controls[j];
                                if (InputControlPath.MatchControlComponent(ref parsedPath[2], ref controlLayoutItem))
                                {
                                    controlName += $"/{controlLayoutItem.name}";
                                    matchExists = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        matchExists = true;
                    }
                }
            }

            IEnumerable<InputControlLayout> matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(deviceLayout.name);

            // If this layout does not have a match, or is the top level root layout,
            // skip over trying to draw any items for it, and immediately try processing the child layouts
            if (!matchExists)
            {
                foreach (var childLayout in matchedChildLayouts)
                {
                    matchExists |= CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, matchingControlPaths);
                }
            }
            // Otherwise, draw the items for it, and then only process the child layouts if the foldout is expanded.
            else
            {
                var newMatchingControlPath = new MatchingControlPath(deviceName, controlName, isRoot);
                matchingControlPaths.Add(newMatchingControlPath);

                foreach (var childLayout in matchedChildLayouts)
                {
                    CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, newMatchingControlPath.children);
                }
            }

            return matchExists;
        }
    }
}
#endif
