using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Haptics;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add something to deal with the situation of a controller loosing battery power; in the current model
////      the controller would disappear as a device but we want to handle the situation such that when the device
////      comes back online, we connect the user to the same device; also, some platforms have APIs to help
////      dealing with this situation and we want to make use of that; probably want to only disable input devices
////      on those platforms instead of removing them

////TODO: ideally, we'd have a way to also scope UIs to users

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

        public const int kInvalidIndex = -1;

        /// <summary>
        /// Unique numeric ID of the user.
        /// </summary>
        /// <remarks>
        /// The ID of a user cannot be changed over its lifetime. Also, while the user
        /// is active, no other player can have the same ID.
        /// </remarks>
        public ulong id
        {
            get { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
        }

        ////REVIEW: allow multiple?
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
        public string userName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        ////REVIEW: how does this handle assets and the need for cloning?
        ////REVIEW: the way action maps are designed, does it make sense to constrain this to a single map?
        /// <summary>
        /// Action map currently associated with the user.
        /// </summary>
        /// <remarks>
        /// This is optional and may be <c>null</c>. User management can be used without
        /// also using action maps.
        /// </remarks>
        public InputActionMap actions
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        ////REVIEW: allow multiple schemes?
        /// <summary>
        /// Control scheme currently employed by the user.
        /// </summary>
        public InputControlScheme? controlScheme
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        ////REVIEW: are control schemes affecting this?
        public ReadOnlyArray<InputBinding> customBindings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void AssignDevices<TDevices>(TDevices devices)
            where TDevices : IEnumerable<InputDevice>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        public static event Action<InputUser, InputUserChange> onChange
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public static ReadOnlyArray<InputUser> all
        {
            get { return new ReadOnlyArray<InputUser>(s_AllUsers, 0, s_UserCount); }
        }

        public static InputUser first
        {
            get { throw new NotImplementedException(); }
        }

        public static InputUser Add(ulong userId = InputUser.kInvalidId, string userName = null)
        {
            throw new NotImplementedException();
        }

        public static void Remove(InputUser user)
        {
            throw new NotImplementedException();
        }

        private ulong m_Id;
        private int m_Index;
        private string m_UserName;
        private ReadOnlyArray<InputDevice> m_Devices;
        private InputActionMap m_ActionMap;

        private static int s_UserCount;
        private static InputUser[] s_AllUsers;

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
