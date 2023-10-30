using System.Linq;
using UnityEngine.InputSystem;

/// <summary>
/// Extension methods for applying presets to Input Actions.
/// </summary>
/*public static class InputActionBindingPresetExtensions
{
    /// <summary>
    /// Unconditionally applies the given binding presets to the associated action.
    /// </summary>
    /// <param name="action">The action for which to apply preset bindings.</param>
    /// <param name="preset">The associated binding preset.</param>
    public static void ApplyPreset(this InputAction action, IBindingPreset preset)
    {
        preset.Apply(action);
    }
    
    /// <summary>
    /// Conditionally applies the given binding presets to the associated action if not already having bindings.
    /// </summary>
    /// <param name="action">The action for which to conditionally apply preset bindings.</param>
    /// <param name="preset">The associated binding preset.</param>
    public static void ApplyPresetIfNotBound(this InputAction action, IBindingPreset preset)
    {
        if (!action.bindings.Any())
            ApplyPreset(action, preset);
    }
    
    // TODO Should we implement this for InputActionReference as well?
}*/