using System;
using System.Collections.Generic;
using UnityEngine;

////TODO: allow double wildcards to look arbitrarily deep into the hierarchy
////TODO: allow stuff like "/gamepad/**/<button>"
////TODO: store CRC32s of names on controls and use that for first-level matching of name components

namespace ISX
{
    // Functions to deal with control path specs (like "/gamepad/*stick").
    // Only two entry points: either find first control that matches path
    // or find all controls that match a path.
    internal static class PathHelpers
    {
        // Return the first control that matches the given path.
        public static InputControl FindControl(InputControl control, string path, int indexInPath = 0)
        {
            return MatchControlsRecursive(control, path, indexInPath, null);
        }

        // Perform a search for controls starting with the given control as root and matching
        // the given path from the given position. Puts all matching controls on the list and
        // returns the number of controls that have been matched.
        //
        // Does not tap 'path' strings of controls so we don't create a bunch of
        // string objects while feeling our way down the hierarchy.
        //
        // Matching is case-insensitive.
        public static int FindControls(InputControl control, string path, int indexInPath,
            List<InputControl> matches)
        {
            var countBefore = matches.Count;
            MatchControlsRecursive(control, path, indexInPath, matches);
            return matches.Count - countBefore;
        }

        private static InputControl MatchControlsRecursive(InputControl control, string path, int indexInPath, List<InputControl> matches)
        {
            var pathLength = path.Length;

            // Try to get a match. A path spec has three components:
            //    "<template>{usage}name"
            // All are optional but at least one component must be present.
            // Names can be aliases, too.

            var controlIsMatch = true;

            // Match by template.
            if (path[indexInPath] == '<')
            {
                ++indexInPath;
                controlIsMatch =
                    MatchPathComponent(control.template, path, ref indexInPath, PathComponentType.Template);

                // If the template isn't a match, walk up the base template
                // chain and match each base template.
                if (!controlIsMatch)
                {
                    var baseTemplate = control.m_Template;
                    while (InputTemplate.s_BaseTemplateTable.TryGetValue(baseTemplate, out baseTemplate))
                    {
                        controlIsMatch = MatchPathComponent(baseTemplate, path, ref indexInPath,
                                PathComponentType.Template);
                        if (controlIsMatch)
                            break;
                    }
                }
            }

            // Match by usage.
            if (indexInPath < pathLength && path[indexInPath] == '{' && controlIsMatch)
            {
                ++indexInPath;

                for (var i = 0; i < control.usages.Count; ++i)
                {
                    controlIsMatch = MatchPathComponent(control.usages[i], path, ref indexInPath, PathComponentType.Usage);
                    if (controlIsMatch)
                        break;
                }
            }

            // Match by name.
            if (indexInPath < pathLength && controlIsMatch && path[indexInPath] != '/')
            {
                // Normal name match.
                controlIsMatch = MatchPathComponent(control.name, path, ref indexInPath, PathComponentType.Name);

                // Alternative match by alias.
                if (!controlIsMatch)
                {
                    for (var i = 0; i < control.aliases.Count && !controlIsMatch; ++i)
                    {
                        controlIsMatch = MatchPathComponent(control.aliases[i], path, ref indexInPath,
                                PathComponentType.Name);
                    }
                }
            }

            // If we have a match, return it or, if there's children, recurse into them.
            if (controlIsMatch)
            {
                // If we ended up on a wildcard, we've successfully matched it.
                if (indexInPath < pathLength && path[indexInPath] == '*')
                    ++indexInPath;

                // If we've reached the end of the path, we have a match.
                if (indexInPath == pathLength)
                {
                    if (matches != null)
                        matches.Add(control);
                    return control;
                }

                // If we've reached a separator, dive into our children.
                if (path[indexInPath] == '/')
                {
                    ++indexInPath;

                    // Silently accept trailing slashes.
                    if (indexInPath == pathLength)
                    {
                        if (matches != null)
                            matches.Add(control);
                        return control;
                    }

                    // See if we want to match children by usage or by name.
                    InputControl lastMatch = null;
                    if (path[indexInPath] == '{')
                    {
                        ////TODO: support scavenging a subhierarchy for usages
                        if (!ReferenceEquals(control.device, control))
                            throw new NotImplementedException(
                                "Matching usages inside subcontrols instead of at device root");

                        // Usages are kind of like entry points that can route to anywhere else
                        // on a device's control hierarchy and then we keep going from that re-routed
                        // point.
                        lastMatch = MatchByUsageAtDeviceRootRecursive(control.device, path, indexInPath, matches);
                    }
                    else
                    {
                        // Go through children and see what we can match.
                        lastMatch = MatchChildrenRecursive(control, path, indexInPath, matches);
                    }

                    return lastMatch;
                }
            }

            return null;
        }

        private static InputControl MatchByUsageAtDeviceRootRecursive(InputDevice device, string path, int indexInPath,
            List<InputControl> matches)
        {
            var usages = device.m_UsagesForEachControl;
            if (usages == null)
                return null;

            var usageCount = usages.Length;
            var startIndex = indexInPath + 1;
            var pathCanMatchMultiple = PathComponentCanYieldMultipleMatches(path, indexInPath);
            var pathLength = path.Length;

            Debug.Assert(path[indexInPath] == '{');
            ++indexInPath;
            if (indexInPath == pathLength)
                throw new Exception($"Invalid path spec '{path}'; trailing '{{'");

            InputControl lastMatch = null;

            for (var i = 0; i < usageCount; ++i)
            {
                var usage = usages[i];

                // Match usage agaist path.
                var usageIsMatch = MatchPathComponent(usage, path, ref indexInPath, PathComponentType.Usage);

                // If it isn't a match, go to next usage.
                if (!usageIsMatch)
                {
                    indexInPath = startIndex;
                    continue;
                }

                var controlMatchedByUsage = device.m_UsageToControl[i];

                // If there's more to go in the path, dive into the children of the control.
                if (indexInPath < pathLength && path[indexInPath] == '/')
                {
                    lastMatch = MatchChildrenRecursive(controlMatchedByUsage, path, indexInPath + 1,
                            matches);

                    // We can stop going through usages if we matched something and the
                    // path component covering usage does not contain wildcards.
                    if (lastMatch != null && !pathCanMatchMultiple)
                        break;

                    // We can stop going through usages if we have a match and are only
                    // looking for a single one.
                    if (lastMatch != null && matches == null)
                        break;
                }
                else
                {
                    lastMatch = controlMatchedByUsage;
                    if (matches != null)
                        matches.Add(controlMatchedByUsage);
                    else
                    {
                        // Only looking for single match and we have one.
                        break;
                    }
                }
            }

            return lastMatch;
        }

        private static InputControl MatchChildrenRecursive(InputControl control, string path, int indexInPath,
            List<InputControl> matches)
        {
            var childCount = control.m_ChildrenReadOnly.Count;
            InputControl lastMatch = null;
            var pathCanMatchMultiple = PathComponentCanYieldMultipleMatches(path, indexInPath);

            for (var i = 0; i < childCount; ++i)
            {
                var child = control.m_ChildrenReadOnly[i];
                var childMatch = MatchControlsRecursive(child, path, indexInPath, matches);

                if (childMatch == null)
                    continue;

                // If the child matched something an there's no wildcards in the child
                // portion of the path, we can stop searching.
                if (!pathCanMatchMultiple)
                    return childMatch;

                // If we are only looking for the first match and a child matched,
                // we can also stop.
                if (matches == null)
                    return childMatch;

                // Otherwise we have to go hunting through the hierarchy in case there are
                // more matches.
                lastMatch = childMatch;
            }

            return lastMatch;
        }

        private enum PathComponentType
        {
            Name,
            Usage,
            Template
        }

        private static bool MatchPathComponent(string component, string path, ref int indexInPath, PathComponentType componentType)
        {
            var nameLength = component.Length;
            var pathLength = path.Length;
            var startIndex = indexInPath;

            // Try to walk the name as far as we can.
            var indexInName = 0;
            while (indexInPath < pathLength)
            {
                // Check if we've reached a terminator in the path.
                var nextCharInPath = path[indexInPath];
                if (nextCharInPath == '/')
                    break;
                if ((nextCharInPath == '>' && componentType == PathComponentType.Template)
                    || (nextCharInPath == '}' && componentType == PathComponentType.Usage))
                {
                    ++indexInPath;
                    break;
                }

                ////TODO: allow only single '*' and recognize '**'
                // If we've reached a '*' in the path, skip character in name.
                if (nextCharInPath == '*')
                {
                    // But first let's see if the following character is a match.
                    if (indexInPath < (pathLength - 1) &&
                        indexInName < nameLength &&
                        char.ToLower(path[indexInPath + 1]) == char.ToLower(component[indexInName]))
                    {
                        ++indexInName;
                        indexInPath += 2; // Match '*' and following character.
                    }
                    else if (indexInName < nameLength)
                    {
                        ++indexInName;
                    }
                    else
                    {
                        return true;
                    }
                    continue;
                }

                // If we've reached the end of the component name, we did so before
                // we've reached a terminator
                if (indexInName == nameLength)
                {
                    indexInPath = startIndex;
                    return false;
                }

                if (char.ToLower(component[indexInName]) == char.ToLower(nextCharInPath))
                {
                    ++indexInName;
                    ++indexInPath;
                }
                else
                {
                    // Name isn't a match.
                    indexInPath = startIndex;
                    return false;
                }
            }

            if (indexInName == nameLength)
                return true;

            indexInPath = startIndex;
            return false;
        }

        private static bool PathComponentCanYieldMultipleMatches(string path, int indexInPath)
        {
            var indexOfNextSlash = path.IndexOf('/', indexInPath);
            if (indexOfNextSlash == -1)
            {
                return path.IndexOf('*', indexInPath) != -1 || path.IndexOf('<', indexInPath) != -1;
            }
            else
            {
                var length = indexOfNextSlash - indexInPath;
                return path.IndexOf('*', indexInPath, length) != -1 || path.IndexOf('<', indexInPath, length) != -1;
            }
        }
    }
}
