namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Enum used to identity the change type for the <see cref="InputSystem.onLayoutChange"/> event.
    /// </summary>
    public enum InputControlLayoutChange
    {
        /// <summary>A new layout was added to the system.</summary>
        Added,
        /// <summary>An existing layout was removed from the system.</summary>
        Removed,
        /// <summary>An existing layout was replaced with a new version.</summary>
        Replaced
    }
}
