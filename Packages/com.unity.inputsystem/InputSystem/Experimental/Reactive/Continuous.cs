using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Object passed with Subscribe to affect properties of nodes further down the dependency chain.
    /// </summary>
    /// <remarks>
    /// These settings apply to the whole underlying chain.
    /// </remarks>
    public struct Chain : IChainable
    {
        private ChainSettings m_Settings;
        private uint m_PlayerId;

        public bool this[ChainSettings key] => (m_Settings & key) != 0;

        public void Mark(ChainSettings settings)
        {
            switch (settings)
            { 
                case ChainSettings.Continuous:
                    m_Settings = settings | ChainSettings.Continuous;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetPlayer(uint playerId)
        {
            m_PlayerId = playerId;
        }
    }

    [Flags]
    public enum ChainSettings
    {
        /// <summary>
        /// No flags set on dependency chain.
        /// </summary>
        None,
        
        /// <summary>
        /// Indicates that the dependency chain result should fire each update even if the result have not changed.
        /// </summary>
        /// <remarks>
        /// Note that this only affects the last node of the processing chain.
        /// </remarks>
        Continuous
    }

    public interface IChainable
    {
        public void Mark(ChainSettings settings);
        public void SetPlayer(uint playerId);
    }
    
    // TODO Continuous would need to to cache result to fire this would be convenient but may also be unnecessary overhead if we desire to interaction.
    // TODO Best use case is WASD to move (sampled and time scaled) and mouse delta (relative) on the same binding.
    // TODO Do we need absolute and relative Vector2?
    // 
    // InputBinding<Vector2> relativeMovement;
    // relativeMovement.AddBinding(Gamepad.leftStick.Last()); // E.g. (1.0f, 0.0f)
    // relativeMovement.AddBinding(Mouse.delta.Sum().Last()); // Eg. (1f + 2f + 3f, 0.0f)
    
    // TODO Consider usages to support aggregated versions out of the box.
    //      E.g. this means sample-and-hold for absolute controls and Sum for relative controls.
    // TODO Basically backend could have a 2 element array for aggregated if only one consumer context
    
    // TODO Note that Sum() is accumulated as we go. Last()
    //
    // TODO Consider allowing subscribe with ref field
    
    public static class ContinuousExtensions
    {
        public static TSource Continuous<TSource>(this TSource source) 
            where TSource : IChainable
        {
            source.Mark(ChainSettings.Continuous);
            return source;
        }
        
        public static TSource Player<TSource>(this TSource source, uint playerId) 
            where TSource : IChainable
        {
            source.SetPlayer(playerId);
            return source;
        }
    }
}