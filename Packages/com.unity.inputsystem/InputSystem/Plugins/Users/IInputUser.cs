namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Represents a user that can be assigned <see cref="InputDevice">devices</see> and <see cref="InputAction">
    /// actions</see>.
    /// </summary>
    /// <remarks>
    /// This interface has to be used in combination with a class. The APIs in <see cref="InputUser"/> will not work
    /// with structs that implement this interface as they object reference itself serves as a user identifier.
    ///
    /// There is no functionality directly on this interface. Instead, all functionality is added through extension
    /// methods in <see cref="InputUser"/> to whichever object implements this interface.
    ///
    /// Each user has a <see cref="InputUser.GetUserId{TUser}">unique ID</see> which is assigned when the user is
    /// <see cref="InputUser.Add">added</see> to the system. No two users, even if not added to the system at the
    /// same time, will receive the same ID.
    ///
    /// All currently added users can be queried with <see cref="InputUser.all"/>. The index of an user in the array
    /// can be queried with <see cref="InputUser.GetUserIndex{TUser}"/>
    ///
    /// Changes to the input user setup can be listened to with <see cref="InputUser.onChange"/>.
    ///
    /// Each user may be assigned <see cref="InputDevice">input devices</see>
    ///
    /// </remarks>
    /// <seealso cref="InputUser"/>
    /// <seealso cref="InputUser.Add"/>
    /// <seealso cref="InputUser.Remove"/>
    /// <seealso cref="InputUser.all"/>
    /// <example>
    /// <code>
    /// public class PlayerController : MonoBehaviour, IInputUser
    /// {
    ///     public MyControls controls;
    ///
    ///     public void OnEnable()
    ///     {
    ///         InputUser.Add(this);
    ///
    ///         this.AssignInputDevice(InputSystem.GetDevice&lt;Gamepad&gt;());
    ///         this.AssignActions(controls);
    ///         this.AssignControlScheme(controls.KeyboardMouse);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IInputUser
    {
    }
}
