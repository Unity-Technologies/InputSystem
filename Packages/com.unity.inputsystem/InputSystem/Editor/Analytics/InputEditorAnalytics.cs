#if UNITY_EDITOR

using System;

namespace UnityEngine.InputSystem.Editor
{
    internal static partial class InputEditorAnalytics
    {
        /// <summary>
        /// Represents notification behavior setting associated with <see cref="PlayerInput"/> and
        /// <see cref="PlayerInputManager"/>.
        /// </summary>
        internal enum PlayerNotificationBehavior
        {
            SendMessages = 0,
            BroadcastMessages = 1,
            UnityEvents = 2,
            CSharpEvents = 3
        }

        /// <summary>
        /// Converts from current <see cref="PlayerNotifications"/> type to analytics counterpart.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If there is no available remapping.</exception>
        internal static PlayerNotificationBehavior ToNotificationBehavior(PlayerNotifications value)
        {
            switch (value)
            {
                case PlayerNotifications.SendMessages:
                    return PlayerNotificationBehavior.SendMessages;
                case PlayerNotifications.BroadcastMessages:
                    return PlayerNotificationBehavior.BroadcastMessages;
                case PlayerNotifications.InvokeUnityEvents:
                    return PlayerNotificationBehavior.UnityEvents;
                case PlayerNotifications.InvokeCSharpEvents:
                    return PlayerNotificationBehavior.CSharpEvents;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}

#endif
