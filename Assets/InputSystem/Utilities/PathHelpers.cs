using System.Collections.Generic;

namespace ISX
{
    // Functions to deal with control path specs (like "/gamepad/*stick").
    internal static class PathHelpers
    {
        // Return the first control that matches the given path.
        public static InputControl FindControl(InputControl control, string path, int indexInPath = 0)
        {
            return MatchControlsRecursive(control, path, indexInPath, null);
        }
        
        // Perform a search for controls starting with the given control as root and matching
        // the given path from the given possition. Puts all matching controls on the list and
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
            var name = control.name;
            var pathLength = path.Length;

            // Try to walk the name as far as we can.
            var nameLength = name.Length;
            var indexInName = 0;
            while (indexInName < nameLength && indexInPath < pathLength)
            {
                // If we've reached a slash in the path, the control's name
                // doesn't match the current path component.
                if (path[indexInPath] == '/')
                    return null;

                // If we've reached a '*' in the path, skip character in name.
                if (path[indexInPath] == '*')
                {
                    // But first let's see if the following character is a match.
                    if (indexInPath < (pathLength - 1) &&
                        char.ToLower(path[indexInPath + 1]) == char.ToLower(name[indexInName]))
                    {
                        ++indexInName;
                        indexInPath += 2; // Match '*' and following character.
                    }
                    else
                    {
                        ++indexInName;
                    }
                }
                else if (char.ToLower(name[indexInName]) == char.ToLower(path[indexInPath]))
                {
                    ++indexInName;
                    ++indexInPath;
                }
                else
                {
                    // Name isn't a match.
                    return null;
                }
            }

            if (indexInName == nameLength)
            {
                // We matched the control name in full.

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
                    var childCount = control.m_ChildrenReadOnly.Count;
                    var matchCount = 0;
                    InputControl lastMatch = null;

                    for (var i = 0; i < childCount; ++i)
                    {
                        var child = control.m_ChildrenReadOnly[i];
                        var childMatch = MatchControlsRecursive(child, path, indexInPath + 1, matches);

                        // If the child matched something an there's no wildcards in the child
                        // portion of the path, we can stop searching.
                        if (childMatch != null && path.IndexOf('*', indexInPath + 1) == -1)
                            return childMatch;
                        
                        // If we are only looking for the first match and we a child matched,
                        // we can also stop.
                        if (childMatch != null && matches == null)
                            return childMatch;

                        // Otherwise we have to go hunting through the hierarchy in case there are
                        // more matches.
                        lastMatch = childMatch;
                    }

                    return lastMatch;
                }
            }

            return null;
        }
    }
}
