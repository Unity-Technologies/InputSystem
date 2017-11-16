using System;
using System.Collections.Generic;
using UnityEngine;

////TODO: allow double wildcards to look arbitrarily deep into the hierarchy
////TODO: allow stuff like "/gamepad/**/<button>"
////TODO: store CRC32s of names on controls and use that for first-level matching of name components

////TODO: add ability to cache images and display names for an entire action set

////REVIEW: change "*/{PrimaryAction}" to "*/**/{PrimaryAction}" so that the hierarchy crawling becomes explicit?

namespace ISX
{
    // Functions to working with control path specs (like "/gamepad/*stick").
    //
    // The thinking here is somewhat similar to System.IO.Path, i.e. have a range
    // of static methods that perform various operations on paths.
    //
    // Has both methods that work just on paths themselves (like TryGetControlTemplate())
    // and methods that work on paths in combination with controls (like FindControls()).
    public static class InputControlPath
    {
        public const string kWildcard = "*";
        public const string kDoubleWildcard = "**";

        public static string TryGetDisplayName(string path)
        {
            throw new NotImplementedException();
        }

        public static string TryGetImageName(string path)
        {
            throw new NotImplementedException();
        }

        // From the given control path, try to determine the device template being used.
        //
        // NOTE: This function will only use information available in the path itself or
        //       in templates referenced by the path. It will not look at actual devices
        //       in the system. This is to make the behavior predictable and not dependent
        //       on whether you currently have the right device connected or not.
        // NOTE: Allocates!
        public static string TryGetDeviceTemplate(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.template.length > 0)
                return parser.current.template.ToString();

            if (parser.current.isWildcard)
                return kWildcard;

            return null;
        }

        // From the given control path, try to determine the control template being used.
        //
        // NOTE: This function will only use information available in the path itself or
        //       in templates referenced by the path. It will not look at actual devices
        //       in the system. This is to make the behavior predictable and not dependent
        //       on whether you currently have the right device connected or not.
        // NOTE: Allocates!
        public static string TryGetControlTemplate(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var pathLength = path.Length;

            var indexOfLastSlash = path.LastIndexOf('/');
            if (indexOfLastSlash == -1 || indexOfLastSlash == 0)
            {
                // If there's no '/' at all in the path, it surely does not mention
                // a control. Same if the '/' is the first thing in the path.
                return null;
            }

            // Simplest case where control template is mentioned explicitly with '<..>'.
            // Note this will only catch if the control is *only* referenced by template and not by anything else
            // in addition (like usage or name).
            if (pathLength > indexOfLastSlash + 2 && path[indexOfLastSlash + 1] == '<' && path[pathLength - 1] == '>')
            {
                var templateNameStart = indexOfLastSlash + 2;
                var templateNameLength = pathLength - templateNameStart - 1;
                return path.Substring(templateNameStart, templateNameLength);
            }

            // Have to actually look at the path in detail.
            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.isWildcard)
                throw new NotImplementedException();

            if (parser.current.template.length == 0)
                return null;

            var deviceTemplateName = parser.current.template.ToString();
            if (!parser.MoveToNextComponent())
                return null; // No control component.

            if (parser.current.isWildcard)
                return kWildcard;

            return FindControlTemplateRecursive(ref parser, deviceTemplateName);
        }

        private static string FindControlTemplateRecursive(ref PathParser parser, string templateName)
        {
            ////TODO: add a static InputTemplate.Cache instance that we look up templates from and flush the cache every frame

            // Load template.
            var template = InputTemplate.TryLoadTemplate(new InternedString(templateName));
            if (template == null)
                return null;

            // Search for control template. May have to jump to other templates
            // and search in them.
            return FindControlTemplateRecursive(ref parser, template);
        }

        private static string FindControlTemplateRecursive(ref PathParser parser, InputTemplate template)
        {
            string currentResult = null;

            var controlCount = template.controls.Count;
            for (var i = 0; i < controlCount; ++i)
            {
                if (template.m_Controls[i].isModifyingChildControlByPath)
                    throw new NotImplementedException();

                ////TODO: shortcut the search if we have a match and there's no wildcards to consider

                // Skip control template if it doesn't match.
                if (!ControlTemplateMatchesPathComponent(ref template.m_Controls[i], ref parser))
                    continue;

                var controlTemplateName = template.m_Controls[i].template;

                // If there's more in the path, try to dive into children by jumping to the
                // control's template.
                if (!parser.isAtEnd)
                {
                    var childPathParser = parser;
                    if (childPathParser.MoveToNextComponent())
                    {
                        var childControlTemplateName = FindControlTemplateRecursive(ref childPathParser, controlTemplateName);
                        if (childControlTemplateName != null)
                        {
                            if (currentResult != null && childControlTemplateName != currentResult)
                                return null;
                            currentResult = childControlTemplateName;
                        }
                    }
                }
                else if (currentResult != null && controlTemplateName != currentResult)
                    return null;
                else
                    currentResult = controlTemplateName.ToString();
            }

            return currentResult;
        }

        private static bool ControlTemplateMatchesPathComponent(ref InputTemplate.ControlTemplate controlTemplate, ref PathParser parser)
        {
            // Match template.
            var template = parser.current.template;
            if (template.length > 0)
            {
                if (!StringMatches(template, controlTemplate.template))
                    return false;
            }

            // Match usage.
            var usage = parser.current.usage;
            if (usage.length > 0)
            {
                var usageCount = controlTemplate.usages.Count;
                var anyUsageMatches = false;
                for (var i = 0; i < usageCount; ++i)
                {
                    if (StringMatches(usage, controlTemplate.usages[i]))
                    {
                        anyUsageMatches = true;
                        break;
                    }
                }

                if (!anyUsageMatches)
                    return false;
            }

            // Match name.
            var name = parser.current.name;
            if (name.length > 0)
            {
                if (!StringMatches(name, controlTemplate.name))
                    return false;
            }

            return true;
        }

        // Match two name strings allowing for wildcards.
        // 'str' may contain wildcards. 'matchTo' may not.
        private static bool StringMatches(Substring str, InternedString matchTo)
        {
            var strLength = str.length;
            var matchToLength = matchTo.length;

            // Can't compare lengths here because str may contain wildcards and
            // thus be shorter than matchTo and still match.

            var matchToLowerCase = matchTo.ToLower();

            // We manually walk the string here so that we can deal with "normal"
            // comparisons as well as with wildcards.
            var posInMatchTo = 0;
            var posInStr = 0;
            while (posInStr < strLength && posInMatchTo < matchToLength)
            {
                var nextChar = str[posInStr];
                if (nextChar == '*')
                {
                    ////TODO: make sure we don't end up with ** here

                    if (posInStr == strLength - 1)
                        return true; // Wildcard at end of string so rest is matched.

                    ++posInStr;
                    nextChar = char.ToLower(str[posInStr]);

                    while (posInMatchTo < matchToLength && matchToLowerCase[posInMatchTo] != nextChar)
                        ++posInMatchTo;

                    if (posInMatchTo == matchToLength)
                        return false; // Matched all the way to end of matchTo but there's more in str after the wildcard.
                }
                else if (char.ToLower(nextChar) != matchToLowerCase[posInMatchTo])
                {
                    return false;
                }

                ++posInMatchTo;
                ++posInStr;
            }

            return (posInMatchTo == matchToLength && posInStr == strLength); // Check if we have consumed all input. Prevent prefix-only match.
        }

        // Return the first control that matches the given path.
        //
        // NOTE: Does not allocate!
        public static InputControl FindControl(InputControl control, string path, int indexInPath = 0)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

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
        //
        // NOTE: Does not allocate!
        public static int FindControls(InputControl control, string path, int indexInPath,
            List<InputControl> matches)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

            var countBefore = matches.Count;
            MatchControlsRecursive(control, path, indexInPath, matches);
            return matches.Count - countBefore;
        }

        ////TODO: refactor this to use the new PathParser

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

        // Parsed element between two '/../'.
        internal struct ParsedPathComponent
        {
            public Substring template;
            public Substring usage;
            public Substring name;

            public bool isWildcard => name == kWildcard;
            public bool isDoubleWildcard => name == kDoubleWildcard;
        }

        // NOTE: Must not allocate!
        internal struct PathParser
        {
            public string path;
            public int length;
            public int leftIndexInPath;
            public int rightIndexInPath; // Points either to a '/' character or one past the end of the path string.
            public ParsedPathComponent current;

            public bool isAtEnd => rightIndexInPath == length;

            public PathParser(string path)
            {
                Debug.Assert(path != null);

                this.path = path;
                length = path.Length;
                leftIndexInPath = 0;
                rightIndexInPath = 0;
                current = new ParsedPathComponent();
            }

            // Update parsing state and 'current' to next component in path.
            // Returns true if the was another component or false if the end of the path was reached.
            public bool MoveToNextComponent()
            {
                // See if we've the end of the path string.
                if (rightIndexInPath == length)
                    return false;

                // Make our current right index our new left index and find
                // a new right index from there.
                leftIndexInPath = rightIndexInPath;
                if (path[leftIndexInPath] == '/')
                {
                    ++leftIndexInPath;
                    rightIndexInPath = leftIndexInPath;
                    if (leftIndexInPath == length)
                        return false;
                }

                // Parse <...> template part, if present.
                var template = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] == '<')
                    template = ParseComponentPart('>');

                // Parse {...} usage part, if present.
                var usage = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] == '{')
                    usage = ParseComponentPart('}');

                // Parse name part, if present.
                var name = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] != '/')
                    name = ParseComponentPart('/');

                current = new ParsedPathComponent
                {
                    template = template,
                    usage = usage,
                    name = name
                };

                return (leftIndexInPath != rightIndexInPath);
            }

            private Substring ParseComponentPart(char terminator)
            {
                if (terminator != '/') // Name has no corresponding left side terminator.
                    ++rightIndexInPath;

                var partStartIndex = rightIndexInPath;
                while (rightIndexInPath < length && path[rightIndexInPath] != terminator)
                    ++rightIndexInPath;

                var partLength = rightIndexInPath - partStartIndex;
                if (rightIndexInPath < length && terminator != '/')
                    ++rightIndexInPath; // Skip past terminator.

                return new Substring(path, partStartIndex, partLength);
            }
        }
    }
}
