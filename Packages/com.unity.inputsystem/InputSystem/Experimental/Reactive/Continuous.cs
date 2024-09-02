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