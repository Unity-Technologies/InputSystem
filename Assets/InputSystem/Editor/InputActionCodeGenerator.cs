#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace ISX.Editor
{
    // Utility to generate code that makes it easier to work with action sets.
    public static class InputActionCodeGenerator
    {
        [Serializable]
        public struct Options
        {
            public string className;
            public string namespaceName;
        }

        // Generate a string containing C# code that simplifies working with the given
        // action sets in code.
        public static string GenerateWrapperCode(IEnumerable<InputActionSet> sets, Options options)
        {
            throw new NotImplementedException();
        }

        // Updates the given file with wrapper code generated for the given action sets.
        // If the generated code is unchanged, does not touch the file.
        public static void GenerateWrapperCode(string filePath, IEnumerable<InputActionSet> sets, Options options)
        {
            throw new NotImplementedException();
        }
    }
}
#endif // UNITY_EDITOR
