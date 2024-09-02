using UnityEngine.InputSystem.Users;

namespace UnityEngine.InputSystem.Experimental
{
    public static class PlayerExtensions
    {
        public static TSource Player<TSource>(this TSource source, uint playerId) 
            where TSource : IObservableInput
        {
            // TODO Check that this dependency chain has not already been associated with player ID
            // TODO Set player constraint
            return source;
        }
    }
}