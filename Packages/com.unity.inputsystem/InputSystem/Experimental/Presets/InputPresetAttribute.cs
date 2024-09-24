using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Specifies that the attributed static method returning a type derived from <see cref="IObservableInput{T}"/>
    /// should be considered a preset input binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InputPresetAttribute : Attribute
    {
        /// <summary>
        /// Constructs a <c>InputPresetAttribute</c> instance.
        /// </summary>
        /// <param name="category">Optional preset category in case the preset should be sorted into a category.
        /// If <c>null</c> the preset will be considered to be uncategorized.</param>
        /// <param name="displayName">Optional display name, if <c>null</c> a name is generated from the method name.</param>
        public InputPresetAttribute(string category = null, string displayName = null)
        {
            this.category = category;
            this.displayName = displayName;
        }

        /// <summary>
        /// Returns the category, if any, of the associated preset.
        /// </summary>
        /// <remarks>If <c>null</c>, the preset should be considered "uncategorized".</remarks>
        public string category { get; }
        
        /// <summary>
        /// Returns the display name, if any, of the associated preset.
        /// </summary>
        /// <remarks>If <c>null</c>, the preset should be considered to be named by its associated method.</remarks>
        public string displayName { get; }
    }
}