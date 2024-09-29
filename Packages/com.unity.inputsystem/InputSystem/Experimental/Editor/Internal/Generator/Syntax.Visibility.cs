namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        /// <summary>
        /// Defines visibility scope.
        /// </summary>
        public enum Visibility
        {
            /// <summary>
            /// Maps to C# default visibility.
            /// </summary>
            Default = 0,
            
            /// <summary>
            /// Maps to C# internal visibility.
            /// </summary>
            Internal,
            
            /// <summary>
            /// Maps to C# private visibility.
            /// </summary>
            Private,
            
            /// <summary>
            /// Maps to C# public visibility.
            /// </summary>
            Public
        }
    }
}