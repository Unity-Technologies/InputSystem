using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Haptics;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add something to deal with the situation of a controller loosing battery power; in the current model
////      the controller would disappear as a device but we want to handle the situation such that when the device
////      comes back online, we connect the user to the same device; also, some platforms have APIs to help
////      dealing with this situation and we want to make use of that; probably want to only disable input devices
////      on those platforms instead of removing them
////      (add something that allows remembering devices as best as we can)

namespace UnityEngine.Experimental.Input.Plugins.Users
{
    ////REVIEW: can we use this to funnel action input through?
    /// <summary>
    /// Allows tracking <see cref="InputDevice">devices</see> and (optionally) <see cref="InputActionMap">
    /// actions</see> assigned to a particular user/player.
    /// </summary>
    public class InputUser : IHaptics
    {
        public const ulong kInvalidId = 0;

        /// <summary>
        /// Unique numeric ID of the user.
        /// </summary>
        /// <remarks>
        /// The ID of a user cannot be changed over its lifetime. Also, while the user
        /// is active, no other player can have the same ID.
        /// </remarks>
        public ulong id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// Sequence number of the user.
        /// </summary>
        /// <remarks>
        /// It can be useful to establish a sorting of players locally such that it is
        /// known who is the first player, who is the second, and so on. This property
        /// gives the positioning of the user within the globally established sequence.
        ///
        /// Note that the index of a user may change as users are added and removed.
        /// </remarks>
        public int index
        {
            get { return m_Index; }
        }

        /// <summary>
        /// If the user is defined by an external API, this is a handle to the external definition.
        /// </summary>
        /// <remarks>
        /// Users may be defined by the platform we are running on. Consoles, for example, have
        /// user management built into the OS and marketplaces like Steam also have APIs
        /// for user management.
        ///
        /// </remarks>
        public InputUserHandle? handle
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Human-readable name of the user.
        /// </summary>
        /// <remarks>
        /// The system places no constraints on the contents of this string. In particular, it does
        /// not ensure that no two users have the same name or that a user even has a name assigned to it.
        /// </remarks>
        public string userName
        {
            get { return m_UserName; }
            set
            {
                if (string.Compare(m_UserName, value) == 0)
                    return;

                m_UserName = value;
                Notify(InputUserChange.NameChanged);
            }
        }

        /// <summary>
        /// List of devices assigned to the user.
        /// </summary>
        /// <remarks>
        /// Devices do not necessarily need to be unique to a user. For example, two users may both
        /// be assigned the same keyboard in a split-screen game where one user uses the left side and
        /// another user uses the right side of the keyboard. Another example is a game where players
        /// take turns on the same machine.
        /// </remarks>
        public ReadOnlyArray<InputDevice> devices
        {
            get { return new ReadOnlyArray<InputDevice>(s_AllDevices, m_DeviceStartIndex, m_DeviceCount);}
        }

        ////REVIEW: the way action maps are designed, does it make sense to constrain this to a single map?
        ////REVIEW: should this be a stack and automatically handle conflicting bindings?
        /// <summary>
        /// Action map currently associated with the user.
        /// </summary>
        /// <remarks>
        /// This is optional and may be <c>null</c>. User management can be used without
        /// also using action maps.
        ///
        /// To set actions on a user, call <see cref="SwitchActions"/>.
        /// </remarks>
        public InputActionMap actions
        {
            get { throw new NotImplementedException(); }
        }

        ////REVIEW: allow multiple schemes?
        /// <summary>
        /// Control scheme currently employed by the user.
        /// </summary>
        /// <remarks>
        ///
        /// Note that if bindings are enabled that are not part of the active control scheme,
        /// this property will automatically change value according to what bindings are being
        /// used.
        ///
        /// Any time the value of this property change (whether by <see cref="SwitchControlScheme"/>
        /// or by automatic switching), a notification is sent on <see cref="onChange"/> with
        /// <see cref="InputUserChange.ControlSchemeChanged"/>.
        /// </remarks>
        public InputControlScheme? controlScheme
        {
            get { throw new NotImplementedException(); }
        }

        ////REVIEW: are control schemes affecting this?
        public ReadOnlyArray<InputBinding> customBindings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Change the set of actions active for the user.
        /// </summary>
        /// <param name="actions"></param>
        public void SwitchActions(InputActionMap actions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="controlScheme"></param>
        public void SwitchControlScheme(InputControlScheme controlScheme)
        {
            throw new NotImplementedException();
        }

        public void AssignDevice(InputDevice device)
        {
            if (AssignDeviceInternal(device))
                Notify(InputUserChange.DevicesChanged);
        }

        public void AssignDevices<TDevices>(TDevices devices)
            where TDevices : IEnumerable<InputDevice> // Parameter so that compiler can know enumerable type instead of having to go through the interface.
        {
            if (devices == null)
                throw new ArgumentNullException("devices");

            var wasAdded = false;
            foreach (var device in devices)
                wasAdded |= AssignDeviceInternal(device);

            if (wasAdded)
                Notify(InputUserChange.DevicesChanged);
        }

        private bool AssignDeviceInternal(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // Ignore if already assigned to user.
            for (var i = 0; i < m_DeviceCount; ++i)
                if (s_AllDevices[m_DeviceStartIndex + i] == device)
                    return false;

            // Move our devices to end of array.
            if (m_DeviceCount > 0)
            {
                ArrayHelpers.MoveSlice(s_AllDevices, m_DeviceStartIndex, s_AllDeviceCount - m_DeviceCount,
                    m_DeviceCount);

                // Adjust users that have been impacted by the change.
                for (var i = 0; i < s_AllUserCount; ++i)
                {
                    var user = s_AllUsers[i];
                    if (user == this || user.m_DeviceStartIndex <= m_DeviceStartIndex)
                        continue;

                    user.m_DeviceStartIndex -= m_DeviceCount;
                }

                m_DeviceStartIndex = s_AllDeviceCount - m_DeviceCount;
            }

            // Append to array.
            if (m_DeviceCount == 0)
                m_DeviceStartIndex = s_AllDeviceCount;
            ArrayHelpers.AppendWithCapacity(ref s_AllDevices, ref s_AllDeviceCount, device);
            ++m_DeviceCount;

            return true;
        }

        public void EnableControls()
        {
            throw new NotImplementedException();
        }

        public void DisableControls()
        {
            throw new NotImplementedException();
        }

        private void Notify(InputUserChange change)
        {
            for (var i = 0; i < s_OnChange.length; ++i)
                s_OnChange[i](this, change);
        }

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        public static event Action<InputUser, InputUserChange> onChange
        {
            add { s_OnChange.Append(value); }
            remove { s_OnChange.Remove(value); }
        }

        /// <summary>
        /// List of all current users.
        /// </summary>
        public static ReadOnlyArray<InputUser> all
        {
            get { return new ReadOnlyArray<InputUser>(s_AllUsers, 0, s_AllUserCount); }
        }

        ////REVIEW: should this be some generic join hook? needs to be explored more; involve console team
        /// <summary>
        /// If true, on platforms that have built-in support for user management (e.g. Xbox and PS4),
        /// automatically create users and assign them devices to reflect
        /// </summary>
        public static bool autoAddPlatformUsers
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Add a new user.
        /// </summary>
        /// <param name="userName">Optional <see cref="userName"/> to assign to the newly created user.</param>
        /// <returns>A newly created user.</returns>
        /// <remarks>
        /// Adding a user sends a notification with <see cref="InputUserChange.Added"/> through <see cref="onChange"/>.
        ///
        /// The user will start out with no devices and no actions assigned.
        ///
        /// The user is added to <see cref="all"/>.
        /// </remarks>
        public static InputUser Add(string userName = null)
        {
            var user = new InputUser
            {
                m_UserName = userName,
                m_Id = ++s_LastUserId,
            };

            // Add to list.
            var index = ArrayHelpers.AppendWithCapacity(ref s_AllUsers, ref s_AllUserCount, user);
            user.m_Index = index;

            // Send notification.
            user.Notify(InputUserChange.Added);

            return user;
        }

        /// <summary>
        /// Remove an active user.
        /// </summary>
        /// <param name="user">An active user.</param>
        /// <exception cref="ArgumentNullException"><paramref name="user"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Removing a user also unassigns all currently assigned devices from the user. On completion of this
        /// method, <see cref="devices"/> of <paramref name="user"/> will be empty.
        /// </remarks>
        public static void Remove(InputUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            // Check index.
            var index = user.m_Index;
            if (index < 0 || index >= s_AllUserCount || s_AllUsers[index] != user)
                return;

            // Remove.
            ArrayHelpers.EraseAt(ref s_AllUsers, index);
            --s_AllUserCount;

            // Adjust indices of remaining users.
            for (var i = index; i < s_AllUserCount; ++i)
                s_AllUsers[i].m_Index = i;

            // Remove devices.
            if (user.m_DeviceCount > 0)
            {
                ArrayHelpers.EraseSliceWithCapacity(ref s_AllDevices, ref s_AllDeviceCount, user.m_DeviceStartIndex,
                    user.m_DeviceCount);
                user.m_DeviceCount = 0;
                user.m_DeviceStartIndex = 0;
            }

            // Send notification.
            user.Notify(InputUserChange.Removed);
        }

        /// <summary>
        /// Find the user (if any) that is currently assigned the given <paramref name="device"/>.
        /// </summary>
        /// <param name="device">An input device that has been added to the system.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <returns>The user that has <paramref name="device"/> among its <see cref="devices"/> or null if
        /// no user is currently assigned the given device.</returns>
        public static InputUser FindUserForDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            throw new NotImplementedException();
        }

        private ulong m_Id;
        private int m_Index;
        private string m_UserName;
        private int m_DeviceCount;
        private int m_DeviceStartIndex;
        private InputActionMap m_ActionMap;

        internal static uint s_LastUserId;
        internal static int s_AllUserCount;
        internal static int s_AllDeviceCount;
        internal static InputUser[] s_AllUsers;
        internal static InputDevice[] s_AllDevices; // We keep a single array that we slice out to each user.
        internal static InlinedArray<Action<InputUser, InputUserChange>> s_OnChange;

        public void PauseHaptics()
        {
            throw new NotImplementedException();
        }

        public void ResumeHaptics()
        {
            throw new NotImplementedException();
        }

        public void ResetHaptics()
        {
            throw new NotImplementedException();
        }
    }
}
