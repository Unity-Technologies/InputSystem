using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
#if UNITY_2020_1_OR_NEWER
    // NOTE: Must not allocate!
    // A version of the binding path parser that uses Span<char>.
    internal struct FastPathParser
    {
        private string path;
        private int length;
        private int leftIndexInPath;
        private int rightIndexInPath; // Points either to a '/' character or one past the end of the path string.

        public InputControlPath.ParsedPathComponent current;

        public bool isAtEnd => rightIndexInPath == length;

        public FastPathParser(string path)
        {
            Debug.Assert(path != null);

            this.path = path;
            length = path.Length;
            leftIndexInPath = 0;
            rightIndexInPath = 0;
            current = new InputControlPath.ParsedPathComponent();
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

            ////FIXME: with multiple usages, this will allocate
            ////FIXME: Why the heck is this allocating? Should not allocate here! Worse yet, we do ToArray() down there.
            // Parse {...} usage part, if present.
            var usages = new InlinedArray<Substring>();
            while (rightIndexInPath < length && path[rightIndexInPath] == '{')
                usages.AppendWithCapacity(ParseComponentPart('}'));

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

            current = new InputControlPath.ParsedPathComponent
            {
                m_Layout = layout,
                m_Usages = usages,
                m_Name = name,
                m_DisplayName = displayName
            };

            return leftIndexInPath != rightIndexInPath;
        }

        private Substring ParseComponentPart(char terminator)
        {
            if (terminator != '/') // Name has no corresponding left side terminator.
                ++rightIndexInPath;

            var partStartIndex = rightIndexInPath;
            while (rightIndexInPath < length && path[rightIndexInPath] != terminator)
            {
                if (path[rightIndexInPath] == '\\' && rightIndexInPath + 1 < length)
                    ++rightIndexInPath;
                ++rightIndexInPath;
            }

            var partLength = rightIndexInPath - partStartIndex;
            if (rightIndexInPath < length && terminator != '/')
                ++rightIndexInPath; // Skip past terminator.

            return new Substring(path, partStartIndex, partLength);
        }
    }
#endif
}
