namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Determines how the triggering of an action or other input-related events are relayed to other GameObjects.
    /// </summary>
    public enum PlayerNotifications
    {
        ////TODO: add a "None" behavior; for actions, users may want to poll (or use the generated interfaces)

        /// <summary>
        /// Use <see cref="GameObject.SendMessage(string,object)"/> to send a message to the <see cref="GameObject"/>
        /// that <see cref="PlayerInput"/> belongs to.
        /// </summary>
        /// <remarks>
        /// The message name will be the name of the action (e.g. "Jump"; it will not include the action map name),
        /// and the object will be the <see cref="PlayerInput"/> on which the action was triggered.
        ///
        /// If the notification is for an action that was triggered, <see cref="SendMessageOptions"/> will be
        /// <see cref="SendMessageOptions.RequireReceiver"/> (i.e. an error will be logged if there is no corresponding
        /// method). Otherwise it will be <see cref="SendMessageOptions.DontRequireReceiver"/>.
        /// </remarks>
        SendMessages,

        /// <summary>
        /// Like <see cref="SendMessages"/> but instead of using <see cref="GameObject.SendMessage(string,object)"/>,
        /// use <see cref="GameObject.BroadcastMessage(string,object)"/>.
        /// </summary>
        BroadcastMessages,

        InvokeUnityEvents,

        InvokeCSharpEvents,
    }
}
