using System;
using System.Text;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: allow stuff like "/gamepad/**/<button>"
////TODO: add support for | (e.g. "<Gamepad>|<Joystick>/{PrimaryMotion}"
////TODO: handle arrays

////REVIEW: change "*/{PrimaryAction}" to "*/**/{PrimaryAction}" so that the hierarchy crawling becomes explicit?

////REVIEW: rename to `InputPath`?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Functions to working with control path specs (like "/gamepad/*stick").
    /// </summary>
    /// <remarks>
    /// The thinking here is somewhat similar to System.IO.Path, i.e. have a range
    /// of static methods that perform various operations on paths.
    ///
    /// Has both methods that work just on paths themselves (like <see cref="TryGetControlLayout"/>)
    /// and methods that work on paths in combination with controls (like <see cref="TryFindControls"/>).
    /// </remarks>
    public static class InputControlPath
    {
        public const string Wildcard = "*";
        public const string DoubleWildcard = "**";

        public const char Separator = '/';

        public static string Combine(InputControl parent, string path)
        {
            if (parent == null)
            {
                if (string.IsNullOrEmpty(path))
                    return string.Empty;

                if (path[0] != Separator)
                    return Separator + path;

                return path;
            }
            if (string.IsNullOrEmpty(path))
                return parent.path;

            return $"{parent.path}/{path}";
        }

        /// <summary>
        /// Create a human readable string from the given control path.
        /// </summary>
        /// <param name="path">A control path such as "&lt;XRController>{LeftHand}/position".</param>
        /// <returns>A string such as "leftStick/x [Gamepad]".</returns>
        public static string ToHumanReadableString(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var buffer = new StringBuilder();
            var parser = new PathParser(path);

            ////REVIEW: ideally, we'd use display names of controls rather than the control paths directly from the path

            // First level is taken to be device.
            if (parser.MoveToNextComponent())
            {
                var device = parser.current.ToHumanReadableString();

                // Any additional levels (if present) are taken to form a control path on the device.
                var isFirstControlLevel = true;
                while (parser.MoveToNextComponent())
                {
                    if (!isFirstControlLevel)
                        buffer.Append('/');

                    buffer.Append(parser.current.ToHumanReadableString());
                    isFirstControlLevel = false;
                }

                if (!string.IsNullOrEmpty(device))
                {
                    buffer.Append(" [");
                    buffer.Append(device);
                    buffer.Append(']');
                }
            }

            // If we didn't manage to figure out a display name, default to displaying
            // the path as is.
            if (buffer.Length == 0)
                return path;

            return buffer.ToString();
        }

        public static string TryGetDeviceUsage(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.usage.length > 0)
                return parser.current.usage.ToString();

            return null;
        }

        /// <summary>
        /// From the given control path, try to determine the device layout being used.
        /// </summary>
        /// <remarks>
        /// This function will only use information available in the path itself or
        /// in layouts referenced by the path. It will not look at actual devices
        /// in the system. This is to make the behavior predictable and not dependent
        /// on whether you currently have the right device connected or not.
        /// </remarks>
        /// <param name="path">A control path (like "/&lt;gamepad&gt;/leftStick")</param>
        /// <returns>The name of the device layout used by the given control path or null
        /// if the path does not specify a device layout or does so in a way that is not
        /// supported by the function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <example>
        /// <code>
        /// InputControlPath.TryGetDeviceLayout("/&lt;gamepad&gt;/leftStick"); // Returns "gamepad".
        /// InputControlPath.TryGetDeviceLayout("/*/leftStick"); // Returns "*".
        /// InputControlPath.TryGetDeviceLayout("/gamepad/leftStick"); // Returns null. "gamepad" is a device name here.
        /// </code>
        /// </example>
        public static string TryGetDeviceLayout(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.layout.length > 0)
                return parser.current.layout.ToString();

            if (parser.current.isWildcard)
                return Wildcard;

            return null;
        }

        ////TODO: return Substring and use path parser; should get rid of allocations

        // From the given control path, try to determine the control layout being used.
        //
        // NOTE: This function will only use information available in the path itself or
        //       in layouts referenced by the path. It will not look at actual devices
        //       in the system. This is to make the behavior predictable and not dependent
        //       on whether you currently have the right device connected or not.
        // NOTE: Allocates!
        public static string TryGetControlLayout(string path)
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

            // Simplest case where control layout is mentioned explicitly with '<..>'.
            // Note this will only catch if the control is *only* referenced by layout and not by anything else
            // in addition (like usage or name).
            if (pathLength > indexOfLastSlash + 2 && path[indexOfLastSlash + 1] == '<' && path[pathLength - 1] == '>')
            {
                var layoutNameStart = indexOfLastSlash + 2;
                var layoutNameLength = pathLength - layoutNameStart - 1;
                return path.Substring(layoutNameStart, layoutNameLength);
            }

            // Have to actually look at the path in detail.
            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.isWildcard)
                throw new NotImplementedException();

            if (parser.current.layout.length == 0)
                return null;

            var deviceLayoutName = parser.current.layout.ToString();
            if (!parser.MoveToNextComponent())
                return null; // No control component.

            if (parser.current.isWildcard)
                return Wildcard;

            return FindControlLayoutRecursive(ref parser, deviceLayoutName);
        }

        private static string FindControlLayoutRecursive(ref PathParser parser, string layoutName)
        {
            ////TODO: add a static InputControlLayout.Cache instance that we look up layouts from and flush the cache every frame

            // Load layout.
            var layout = InputControlLayout.s_Layouts.TryLoadLayout(new InternedString(layoutName));
            if (layout == null)
                return null;

            // Search for control layout. May have to jump to other layouts
            // and search in them.
            return FindControlLayoutRecursive(ref parser, layout);
        }

        private static string FindControlLayoutRecursive(ref PathParser parser, InputControlLayout layout)
        {
            string currentResult = null;

            var controlCount = layout.controls.Count;
            for (var i = 0; i < controlCount; ++i)
            {
                if (layout.m_Controls[i].isModifyingChildControlByPath)
                    throw new NotImplementedException();

                ////TODO: shortcut the search if we have a match and there's no wildcards to consider

                // Skip control layout if it doesn't match.
                if (!ControlLayoutMatchesPathComponent(ref layout.m_Controls[i], ref parser))
                    continue;

                var controlLayoutName = layout.m_Controls[i].layout;

                // If there's more in the path, try to dive into children by jumping to the
                // control's layout.
                if (!parser.isAtEnd)
                {
                    var childPathParser = parser;
                    if (childPathParser.MoveToNextComponent())
                    {
                        var childControlLayoutName = FindControlLayoutRecursive(ref childPathParser, controlLayoutName);
                        if (childControlLayoutName != null)
                        {
                            if (currentResult != null && childControlLayoutName != currentResult)
                                return null;
                            currentResult = childControlLayoutName;
                        }
                    }
                }
                else if (currentResult != null && controlLayoutName != currentResult)
                    return null;
                else
                    currentResult = controlLayoutName.ToString();
            }

            return currentResult;
        }

        private static bool ControlLayoutMatchesPathComponent(ref InputControlLayout.ControlItem controlItem, ref PathParser parser)
        {
            // Match layout.
            var layout = parser.current.layout;
            if (layout.length > 0)
            {
                if (!StringMatches(layout, controlItem.layout))
                    return false;
            }

            // Match usage.
            var usage = parser.current.usage;
            if (usage.length > 0)
            {
                var usageCount = controlItem.usages.Count;
                var anyUsageMatches = false;
                for (var i = 0; i < usageCount; ++i)
                {
                    if (StringMatches(usage, controlItem.usages[i]))
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
                if (!StringMatches(name, controlItem.name))
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

            return posInMatchTo == matchToLength && posInStr == strLength; // Check if we have consumed all input. Prevent prefix-only match.
        }

        public static InputControl TryFindControl(InputControl control, string path, int indexInPath = 0)
        {
            return TryFindControl<InputControl>(control, path, indexInPath);
        }

        public static InputControl[] TryFindControls(InputControl control, string path, int indexInPath = 0)
        {
            var matches = new InputControlList<InputControl>(Allocator.Temp);
            try
            {
                TryFindControls(control, path, indexInPath, ref matches);
                return matches.ToArray();
            }
            finally
            {
                matches.Dispose();
            }
        }

        public static int TryFindControls(InputControl control, string path, ref InputControlList<InputControl> matches, int indexInPath = 0)
        {
            return TryFindControls(control, path, indexInPath, ref matches);
        }

        /// <summary>
        /// Return the first control that matches the given path.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="path"></param>
        /// <param name="indexInPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        public static TControl TryFindControl<TControl>(InputControl control, string path, int indexInPath = 0)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

            var none = new InputControlList<TControl>();
            return MatchControlsRecursive(control, path, indexInPath, ref none, matchMultiple: false);
        }

        /// <summary>
        /// Perform a search for controls starting with the given control as root and matching
        /// the given path from the given position. Puts all matching controls on the list and
        /// returns the number of controls that have been matched.
        /// </summary>
        /// <param name="control">Control at which the given path is rooted.</param>
        /// <param name="path"></param>
        /// <param name="indexInPath"></param>
        /// <param name="matches"></param>
        /// <typeparam name="TControl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// Matching is case-insensitive.
        ///
        /// Does not allocate managed memory.
        /// </remarks>
        public static int TryFindControls<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

            var countBefore = matches.Count;
            MatchControlsRecursive(control, path, indexInPath, ref matches, matchMultiple: true);
            return matches.Count - countBefore;
        }

        public static InputControl TryFindChild(InputControl control, string path, int indexInPath = 0)
        {
            return TryFindChild<InputControl>(control, path, indexInPath);
        }

        public static TControl TryFindChild<TControl>(InputControl control, string path, int indexInPath = 0)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var childCount = control.m_ChildrenReadOnly.Count;
            for (var i = 0; i < childCount; ++i)
            {
                var child = control.m_ChildrenReadOnly[i];
                var match = TryFindControl<TControl>(child, path, indexInPath);
                if (match != null)
                    return match;
            }

            return null;
        }

        ////REVIEW: probably would be good to have a Matches(string,string) version

        public static bool Matches(string expected, InputControl control)
        {
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentNullException(nameof(expected));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var parser = new PathParser(expected);
            return MatchesRecursive(ref parser, control);
        }

        public static bool MatchesPrefix(string expected, InputControl control)
        {
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentNullException(nameof(expected));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            ////REVIEW: this can probably be done more efficiently
            for (var current = control; current != null; current = current.parent)
            {
                var parser = new PathParser(expected);
                if (MatchesRecursive(ref parser, current) && parser.isAtEnd)
                    return true;
            }

            return false;
        }

        private static bool MatchesRecursive(ref PathParser parser, InputControl currentControl)
        {
            // Recurse into parent before looking at the current control. This
            // will advance the parser to where our control is in the path.
            var parent = currentControl.parent;
            if (parent != null && !MatchesRecursive(ref parser, parent))
                return false;

            // Fail if there's no more path left.
            if (!parser.MoveToNextComponent())
                return false;

            // Match current path component against current control.
            return parser.current.Matches(currentControl);
        }

        ////TODO: refactor this to use the new PathParser

        /// <summary>
        /// Recursively match path elements in <paramref name="path"/>.
        /// </summary>
        /// <param name="control">Current control we're at.</param>
        /// <param name="path">Control path we are matching against.</param>
        /// <param name="indexInPath">Index of current component in <paramref name="path"/>.</param>
        /// <param name="matches"></param>
        /// <param name="matchMultiple"></param>
        /// <typeparam name="TControl"></typeparam>
        /// <returns></returns>
        private static TControl MatchControlsRecursive<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
        {
            var pathLength = path.Length;

            // Try to get a match. A path spec has three components:
            //    "<layout>{usage}name"
            // All are optional but at least one component must be present.
            // Names can be aliases, too.
            // We don't tap InputControl.path strings of controls so as to not create a
            // bunch of string objects while feeling our way down the hierarchy.

            var controlIsMatch = true;

            // Match by layout.
            if (path[indexInPath] == '<')
            {
                ++indexInPath;
                controlIsMatch =
                    MatchPathComponent(control.layout, path, ref indexInPath, PathComponentType.Layout);

                // If the layout isn't a match, walk up the base layout
                // chain and match each base layout.
                if (!controlIsMatch)
                {
                    var baseLayout = control.m_Layout;
                    while (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout))
                    {
                        controlIsMatch = MatchPathComponent(baseLayout, path, ref indexInPath,
                            PathComponentType.Layout);
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

            // Match by display name.
            if (indexInPath < pathLength - 1 && controlIsMatch && path[indexInPath] == '#' &&
                path[indexInPath + 1] == '(')
            {
                indexInPath += 2;
                controlIsMatch = MatchPathComponent(control.displayName, path, ref indexInPath,
                    PathComponentType.DisplayName);
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
                    // Check type.
                    if (!(control is TControl match))
                        return null;

                    if (matchMultiple)
                        matches.Add(match);
                    return match;
                }

                // If we've reached a separator, dive into our children.
                if (path[indexInPath] == '/')
                {
                    ++indexInPath;

                    // Silently accept trailing slashes.
                    if (indexInPath == pathLength)
                    {
                        // Check type.
                        if (!(control is TControl match))
                            return null;

                        if (matchMultiple)
                            matches.Add(match);
                        return match;
                    }

                    // See if we want to match children by usage or by name.
                    TControl lastMatch;
                    if (path[indexInPath] == '{')
                    {
                        ////TODO: support scavenging a subhierarchy for usages
                        if (!ReferenceEquals(control.device, control))
                            throw new NotImplementedException(
                                "Matching usages inside subcontrols instead of at device root");

                        // Usages are kind of like entry points that can route to anywhere else
                        // on a device's control hierarchy and then we keep going from that re-routed
                        // point.
                        lastMatch = MatchByUsageAtDeviceRootRecursive(control.device, path, indexInPath, ref matches, matchMultiple);
                    }
                    else
                    {
                        // Go through children and see what we can match.
                        lastMatch = MatchChildrenRecursive(control, path, indexInPath, ref matches, matchMultiple);
                    }

                    return lastMatch;
                }
            }

            return null;
        }

        private static TControl MatchByUsageAtDeviceRootRecursive<TControl>(InputDevice device, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
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

            TControl lastMatch = null;

            for (var i = 0; i < usageCount; ++i)
            {
                var usage = usages[i];

                // Match usage against path.
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
                        ref matches, matchMultiple);

                    // We can stop going through usages if we matched something and the
                    // path component covering usage does not contain wildcards.
                    if (lastMatch != null && !pathCanMatchMultiple)
                        break;

                    // We can stop going through usages if we have a match and are only
                    // looking for a single one.
                    if (lastMatch != null && !matchMultiple)
                        break;
                }
                else
                {
                    lastMatch = controlMatchedByUsage as TControl;
                    if (lastMatch != null)
                    {
                        if (matchMultiple)
                            matches.Add(lastMatch);
                        else
                        {
                            // Only looking for single match and we have one.
                            break;
                        }
                    }
                }
            }

            return lastMatch;
        }

        private static TControl MatchChildrenRecursive<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
        {
            var childCount = control.m_ChildrenReadOnly.Count;
            TControl lastMatch = null;
            var pathCanMatchMultiple = PathComponentCanYieldMultipleMatches(path, indexInPath);

            for (var i = 0; i < childCount; ++i)
            {
                var child = control.m_ChildrenReadOnly[i];
                var childMatch = MatchControlsRecursive(child, path, indexInPath, ref matches, matchMultiple);

                if (childMatch == null)
                    continue;

                // If the child matched something an there's no wildcards in the child
                // portion of the path, we can stop searching.
                if (!pathCanMatchMultiple)
                    return childMatch;

                // If we are only looking for the first match and a child matched,
                // we can also stop.
                if (!matchMultiple)
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
            DisplayName,
            Usage,
            Layout
        }

        private static bool MatchPathComponent(string component, string path, ref int indexInPath, PathComponentType componentType)
        {
            Debug.Assert(component != null, "Component string is null");
            Debug.Assert(path != null, "Path is null");

            var nameLength = component.Length;
            var pathLength = path.Length;
            var startIndex = indexInPath;

            // Try to walk the name as far as we can.
            var indexInName = 0;
            while (indexInPath < pathLength)
            {
                // Check if we've reached a terminator in the path.
                var nextCharInPath = path[indexInPath];
                if (nextCharInPath == '\\' && indexInPath + 1 < pathLength)
                {
                    // Escaped character. Bypass treatment of special characters below.
                    ++indexInPath;
                    nextCharInPath = path[indexInPath];
                }
                else
                {
                    if (nextCharInPath == '/')
                        break;
                    if ((nextCharInPath == '>' && componentType == PathComponentType.Layout)
                        || (nextCharInPath == '}' && componentType == PathComponentType.Usage)
                        || (nextCharInPath == ')' && componentType == PathComponentType.DisplayName))
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
            public Substring layout;
            public Substring usage;
            public Substring name;
            public Substring displayName;

            public bool isWildcard => name == Wildcard;

            public bool isDoubleWildcard => name == DoubleWildcard;

            public string ToHumanReadableString()
            {
                var result = string.Empty;
                if (isWildcard)
                    result += "Any";
                if (!usage.isEmpty)
                {
                    if (result != string.Empty)
                        result += ' ' + ToHumanReadableString(usage);
                    else
                        result += ToHumanReadableString(usage);
                }

                if (!layout.isEmpty)
                {
                    if (result != string.Empty)
                        result += ' ' + ToHumanReadableString(layout);
                    else
                        result += ToHumanReadableString(layout);
                }

                if (!name.isEmpty && !isWildcard)
                {
                    if (result != string.Empty)
                        result += ' ' + ToHumanReadableString(name);
                    else
                        result += ToHumanReadableString(name);
                }

                if (!displayName.isEmpty)
                {
                    var str = $"\"{ToHumanReadableString(displayName)}\"";
                    if (result != string.Empty)
                        result += ' ' + str;
                    else
                        result += str;
                }

                return result;
            }

            private static string ToHumanReadableString(Substring substring)
            {
                return substring.ToString().Unescape("/*{<", "/*{<");
            }

            /// <summary>
            /// Whether the given control matches the constraints of this path component.
            /// </summary>
            /// <param name="control">Control to match against the path spec.</param>
            /// <returns></returns>
            public bool Matches(InputControl control)
            {
                // Match layout.
                if (!layout.isEmpty)
                {
                    // Check for direct match.
                    var layoutMatches = Substring.Compare(layout, control.layout,
                        StringComparison.InvariantCultureIgnoreCase) == 0;
                    if (!layoutMatches)
                    {
                        // No direct match but base layout may match.
                        var baseLayout = control.m_Layout;
                        while (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout) && !layoutMatches)
                        {
                            layoutMatches = Substring.Compare(layout, baseLayout.ToString(),
                                StringComparison.InvariantCultureIgnoreCase) == 0;
                        }
                    }

                    if (!layoutMatches)
                        return false;
                }

                // Match usage.
                if (!usage.isEmpty)
                {
                    var usages = control.usages;
                    var haveUsageMatch = false;
                    for (var i = 0; i < usages.Count; ++i)
                        if (Substring.Compare(usages[i].ToString(), usage, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            haveUsageMatch = true;
                            break;
                        }

                    if (!haveUsageMatch)
                        return false;
                }

                // Match name.
                if (!name.isEmpty && !isWildcard)
                {
                    ////FIXME: unlike the matching path we have in MatchControlsRecursive, this does not take aliases into account
                    if (Substring.Compare(control.name, name, StringComparison.InvariantCultureIgnoreCase) != 0)
                        return false;
                }

                // Match display name.
                if (!displayName.isEmpty)
                {
                    if (Substring.Compare(control.displayName, displayName,
                        StringComparison.InvariantCultureIgnoreCase) != 0)
                        return false;
                }

                return true;
            }
        }

        ////TODO: expose PathParser

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

                // Parse <...> layout part, if present.
                var layout = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] == '<')
                    layout = ParseComponentPart('>');

                // Parse {...} usage part, if present.
                var usage = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] == '{')
                    usage = ParseComponentPart('}');

                // Parse display name part, if present.
                var displayName = new Substring();
                if (rightIndexInPath < length - 1 && path[rightIndexInPath] == '#' && path[rightIndexInPath + 1] == '(')
                {
                    ++rightIndexInPath;
                    displayName = ParseComponentPart(')');
                }

                // Parse name part, if present.
                var name = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] != '/')
                    name = ParseComponentPart('/');

                current = new ParsedPathComponent
                {
                    layout = layout,
                    usage = usage,
                    name = name,
                    displayName = displayName
                };

                return leftIndexInPath != rightIndexInPath;
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
