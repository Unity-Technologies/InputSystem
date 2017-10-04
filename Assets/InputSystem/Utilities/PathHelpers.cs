using System;
using System.Collections.Generic;

namespace ISX
{
    internal static class PathHelpers
    {
        // Does not tap 'path' strings of controls so we don't create a bunch of
        // string objects while feeling our way down the hierarchy.
        public static int MatchControlsRecursive(InputControl control, string path, int indexInPath, List<InputControl> matches)
        {
            var name = control.name;
            var startIndex = indexInPath;
            var pathLength = path.Length;

            // Try to walk the name as far as we can.
            var nameLength = name.Length;
            var indexInName = 0;
            while (indexInName < nameLength && indexInPath < pathLength)
            {
                // If we've reached a slash in the path, the control's name
                // doesn't match the current path component.
                if (path[indexInPath] == '/')
                    return 0;

                if (char.ToLower(name[indexInName]) == char.ToLower(path[indexInPath]))
                {
                    ++indexInName;
                    ++indexInPath;
                }
                else
                {
                    // If we've reached a '*' in the path, skip character in name.
                    if (path[indexInPath] == '*')
                    {
                        ++indexInName;
                    }
                    else
                    {
                        // Name isn't a match.
                        return 0;
                    }
                }
            }

            if (indexInName == nameLength)
            {
                // We matched the control name in full.

                // If we've reached the end of the path, we have a match.
                if (indexInPath == pathLength)
                {
                    matches.Add(control);
                    return 1;
                }

                // If we've reached a separator, dive into our children.
                if (path[indexInPath] == '/')
                {
                    var childCount = control.m_ChildrenReadOnly.Count;
                    for (var i = 0; i < childCount; ++i)
                    {
                        var child = control.m_ChildrenReadOnly[i];
                        var childMatchCount = MatchControlsRecursive(child, path, indexInPath + 1, matches);

                        if (childMatchCount != 0)
                            return childMatchCount;
                    }
                }
            }

            return 0;
        }
    }
}
