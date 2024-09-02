using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents an input event that do not carry data.
    /// </summary>
    [Serializable]
    public readonly struct InputEvent
    {
    }

    /// <summary>
    /// Represents an input event that carry data of type <typeparamref name="TEventArg"/>. 
    /// </summary>
    /// <typeparam name="TEventArg"></typeparam>
    public readonly struct InputEvent<TEventArg> where TEventArg : struct
    {
        private readonly TEventArg m_EventArg;
        
        public InputEvent(TEventArg eventArgs)
        {
            m_EventArg = eventArgs;
        }
    }
}
