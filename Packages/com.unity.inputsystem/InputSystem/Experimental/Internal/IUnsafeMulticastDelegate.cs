using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a multicast delegate that only supports static function pointers.
    /// </summary>
    public unsafe interface IUnsafeMulticastDelegate 
    {
        /// <summary>
        /// Adds a callback from this multi-cast delegate.
        /// </summary>
        /// <param name="callback">The callback to be added.</param>
        public void Add([NotNull] delegate*<void*, void> callback);
        
        /// <summary>
        /// Removes a callback from this multi-cast delegate.
        /// </summary>
        /// <param name="callback"></param>
        public void Remove([NotNull] delegate*<void*, void> callback);
        
        /// <summary>
        /// Invokes all currently registered callbacks. 
        /// </summary>
        /// <param name="pointer">An optional pointer to be passed to all registered callbacks.</param>
        public void Invoke([Optional] void* pointer);
    }
}