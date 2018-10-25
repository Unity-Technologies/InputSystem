using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;

//make users actually *own* their actions

//do we need to make users have ties to assets?

//does this take control of enabling/disabling actions in some form?
//does it have all possible actions for the user or just whatever applies in the user's current context?

////REVIEW: should we reference the control scheme by name only instead of passing InputControlSchemes around?

////REVIEW: is detecting control scheme switches really that important? the UI hints depend more on when bindings change

////TODO: add something to deal with the situation of a controller losing battery power; in the current model
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
    public static class InputUser
    {
        public const ulong kInvalidId = 0;

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        public static event Action<IInputUser, InputUserChange> onChange
        {
            add { s_OnChange.Append(value); }
            remove { s_OnChange.Remove(value); }
        }

        /// <summary>
        /// List of all current users.
        /// </summary>
        public static ReadOnlyArray<IInputUser> all
        {
            get { return new ReadOnlyArray<IInputUser>(s_AllUsers, 0, s_AllUserCount); }
        }

        /// <summary>
        /// Get the unique numeric ID of the user.
        /// </summary>
        /// <remarks>
        /// The ID of a user cannot be changed over its lifetime. Also, while the user
        /// is active, no other player can have the same ID.
        /// </remarks>
        public static ulong GetUserId<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return kInvalidId;

            return s_AllUserData[index].id;
        }

        /// <summary>
        /// Get the sequence number of the user.
        /// </summary>
        /// <remarks>
        /// It can be useful to establish a sorting of players locally such that it is
        /// known who is the first player, who is the second, and so on. This property
        /// gives the positioning of the user within the globally established sequence.
        ///
        /// Note that the index of a user may change as users are added and removed.
        /// </remarks>
        public static int GetUserIndex<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");
            return FindUserIndex(user);
        }

        /// <summary>
        /// If the user is defined by an external API, this returns a handle to the external definition.
        /// </summary>
        /// <remarks>
        /// Users may be defined by the platform we are running on. Consoles, for example, have
        /// user management built into the OS and marketplaces like Steam also have APIs
        /// for user management.
        ///
        /// </remarks>
        public static InputUserHandle? GetUserHandle<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return null;

            return s_AllUserData[index].handle;
        }

        public static void SetUserHandle<TUser>(this TUser user, InputUserHandle? handle)
            where TUser : class, IInputUser
        {
            throw new NotImplementedException();
        }

        ////REVIEW: do we really need/want this?
        /// <summary>
        /// Get the human-readable name of the user.
        /// </summary>
        /// <remarks>
        /// The system places no constraints on the contents of this string. In particular, it does
        /// not ensure that no two users have the same name or that a user even has a name assigned to it.
        /// </remarks>
        public static string GetUserName<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return null;

            return s_AllUserData[index].userName;
        }

        public static void SetUserName<TUser>(this TUser user, string userName)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            if (string.Compare(s_AllUserData[index].userName, userName) == 0)
                return;

            s_AllUserData[index].userName = userName;

            Notify(user, InputUserChange.NameChanged);
        }

        public static bool IsInputActive<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            return user.GetInputActions().enabled;
        }

        public static void ActivateInput<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            user.GetInputActions().Enable();
        }

        /// <summary>
        /// Silence all input on <paramref name="user"/>'s actions.
        /// </summary>
        /// <param name="user"></param>
        /// <typeparam name="TUser"></typeparam>
        public static void PassivateInput<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            user.GetInputActions().Disable();
        }

        /// <summary>
        /// Clear the current action stack and switch to the given set of actions.
        /// </summary>
        /// <param name="user">An input user that has been <see cref="Add">added</see> to the system.</param>
        /// <param name="actions">Set of actions to switch to. Can be null in which case the stack is simply
        /// cleared and no further action is taken.</param>
        /// <typeparam name="TUser">Type of <see cref="IInputUser"/>.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="user"/> is null.</exception>
        public static void SetInputActions<TUser>(this TUser user, InputActionMap actions)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var stack = user.GetInputActions();
            stack.Clear();

            if (actions != null)
                stack.Push(actions);
        }

        /// <summary>
        /// Get the <see cref="InputActionMap">input action maps</see> that are currently active for the user.
        /// </summary>
        public static InputActionStack GetInputActions<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return null;

            var result = s_AllUserData[index].actionStack;
            if (result == null)
            {
                result = new InputActionStack();
                s_AllUserData[index].actionStack = result;
            }

            return result;
        }

        /// <summary>
        /// Get the control scheme currently employed by the user.
        /// </summary>
        /// <remarks>
        ///
        /// Note that if bindings are enabled that are not part of the active control scheme,
        /// this property will automatically change value according to what bindings are being
        /// used.
        ///
        /// Any time the value of this property change (whether by <see cref="SetControlScheme"/>
        /// or by automatic switching), a notification is sent on <see cref="onChange"/> with
        /// <see cref="InputUserChange.ControlSchemeChanged"/>.
        /// </remarks>
        public static InputControlScheme? GetControlScheme<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return null;

            return s_AllUserData[index].controlScheme;
        }

        public static bool AssignControlScheme<TUser>(this TUser user, InputControlScheme scheme, bool assignMatchingUnusedDevices = false)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            // Ignore if the control scheme is already set on the user.
            if (s_AllUserData[index].controlScheme.HasValue && s_AllUserData[index].controlScheme == scheme)
                return true;

            s_AllUserData[index].controlScheme = scheme;

            // If we're supposed to automatically add devices, look for matching devices now which aren't
            // used by any other user.
            if (assignMatchingUnusedDevices)
            {
                var needToNotify = false;

                // If we are currently assigned devices, unassign them first.
                if (s_AllUserData[index].deviceCount > 0)
                {
                    ClearAssignedInputDevicesInternal(index);
                    needToNotify = true;
                }

                // Only go through the matching process if we actually have device requirements.
                if (scheme.deviceRequirements.Count > 0)
                {
                    // Grab all unused devices and then select a set of devices matching the scheme's
                    // requirements.
                    using (var availableDevices = GetUnusedDevices())
                    using (var pickedDevices = scheme.PickDevicesFrom(availableDevices))
                    {
                        if (!pickedDevices.isSuccessfulMatch)
                        {
                            // Control scheme isn't satisfied with the devices we have available.
                            // Fail setting the control scheme.
                            s_AllUserData[index].controlScheme = null;
                            return false;
                        }

                        // Assign selected devices to user.
                        if (availableDevices.Count > 0)
                        {
                            foreach (var device in pickedDevices.devices)
                                AssignDeviceInternal(index, device);
                            needToNotify = true;
                        }
                    }
                }

                if (needToNotify)
                    Notify(user, InputUserChange.DevicesChanged);
            }

            Notify(user, InputUserChange.ControlSchemeChanged);
            return true;
        }

        public static void AssignControlScheme<TUser>(this TUser user, string schemeName)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            if (string.IsNullOrEmpty(schemeName) && !s_AllUserData[index].controlScheme.HasValue)
                return;
            if (!string.IsNullOrEmpty(schemeName) && s_AllUserData[index].controlScheme.HasValue &&
                string.Compare(
                    s_AllUserData[index].controlScheme.Value.m_Name, schemeName,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                return;

            if (string.IsNullOrEmpty(schemeName))
                s_AllUserData[index].controlScheme = null;
            else
                s_AllUserData[index].controlScheme = new InputControlScheme(schemeName);

            Notify(user, InputUserChange.ControlSchemeChanged);
        }

        /// <summary>
        /// Get the list of <see cref="InputDevice">input devices</see> assigned to the user.
        /// </summary>
        /// <remarks>
        /// Devices do not necessarily need to be unique to a user. For example, two users may both
        /// be assigned the same keyboard in a split-screen game where one user uses the left side and
        /// another user uses the right side of the keyboard. Another example is a game where players
        /// take turns on the same machine.
        /// </remarks>
        public static ReadOnlyArray<InputDevice> GetAssignedInputDevices<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return new ReadOnlyArray<InputDevice>();

            return new ReadOnlyArray<InputDevice>(s_AllDevices, s_AllUserData[index].deviceStartIndex,
                s_AllUserData[index].deviceCount);
        }

        public static void AssignInputDevice<TUser>(this TUser user, InputDevice device)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (device == null)
                throw new ArgumentNullException("device");

            var userIndex = FindUserIndex(user);
            if (userIndex == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            if (AssignDeviceInternal(userIndex, device))
                Notify(user, InputUserChange.DevicesChanged);
        }

        public static void AssignInputDevices<TUser, TDevices>(this TUser user, TDevices devices)
            where TUser : class, IInputUser
            where TDevices : IEnumerable<InputDevice> // Parameter so that compiler can know enumerable type instead of having to go through the interface.
        {
            if (devices == null)
                throw new ArgumentNullException("devices");

            var userIndex = FindUserIndex(user);
            if (userIndex == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            var wasAdded = false;
            foreach (var device in devices)
            {
                if (device == null)
                    continue;
                wasAdded |= AssignDeviceInternal(userIndex, device);
            }

            if (wasAdded)
                Notify(user, InputUserChange.DevicesChanged);
        }

        private static bool AssignDeviceInternal(int userIndex, InputDevice device)
        {
            var deviceCount = s_AllUserData[userIndex].deviceCount;
            var deviceStartIndex = s_AllUserData[userIndex].deviceStartIndex;

            // Ignore if already assigned to user.
            for (var i = 0; i < deviceCount; ++i)
                if (s_AllDevices[deviceStartIndex + i] == device)
                    return false;

            // Move our devices to end of array.
            if (deviceCount > 0)
            {
                ArrayHelpers.MoveSlice(s_AllDevices, deviceStartIndex, s_AllDeviceCount - deviceCount,
                    deviceCount);

                // Adjust users that have been impacted by the change.
                for (var i = 0; i < s_AllUserCount; ++i)
                {
                    if (i == userIndex)
                        continue;

                    if (s_AllUserData[i].deviceStartIndex <= deviceStartIndex)
                        continue;

                    s_AllUserData[i].deviceStartIndex -= deviceCount;
                }
            }

            // Append to array.
            deviceStartIndex = s_AllDeviceCount - deviceCount;
            s_AllUserData[userIndex].deviceStartIndex = deviceStartIndex;
            ArrayHelpers.AppendWithCapacity(ref s_AllDevices, ref s_AllDeviceCount, device);
            ++s_AllUserData[userIndex].deviceCount;

            return true;
        }

        public static void ClearAssignedInputDevices<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return; // User hasn't been added and thus owns no devices.

            ClearAssignedInputDevicesInternal(index);

            Notify(user, InputUserChange.DevicesChanged);
        }

        private static void ClearAssignedInputDevicesInternal(int index)
        {
            var deviceCount = s_AllUserData[index].deviceCount;
            if (deviceCount == 0)
                return;

            var deviceStartIndex = s_AllUserData[index].deviceStartIndex;
            ArrayHelpers.EraseSliceWithCapacity(ref s_AllDevices, ref s_AllDeviceCount, deviceCount, deviceStartIndex);

            s_AllUserData[index].deviceCount = 0;
            s_AllUserData[index].deviceStartIndex = -1;

            // Adjust indices of other users.
            for (var i = 0; i < s_AllUserCount; ++i)
            {
                if (s_AllUserData[i].deviceStartIndex <= deviceStartIndex)
                    continue;

                s_AllUserData[i].deviceStartIndex -= deviceCount;
            }
        }

        /// <summary>
        /// Return a list of all currently added devices that are not assigned to any user.
        /// </summary>
        /// <returns>A (possibly empty) list of devices that are currently not assigned to a user.</returns>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="InputUser.AssignInputDevice"/>
        /// <remarks>
        /// The resulting list uses <see cref="Allocator.Temp"> temporary, unmanaged memory</see>. If not disposed of
        /// explicitly, the list will automatically be deallocated at the end of the frame and will become unusable.
        /// </remarks>
        public static InputControlList<InputDevice> GetUnusedDevices()
        {
            var unusedDevices = new InputControlList<InputDevice>(Allocator.Temp);

            foreach (var device in InputSystem.devices)
            {
                // If it's in s_AllDevices, there is *some* user that is using the device.
                // We don't care which one it is here.
                if (ArrayHelpers.ContainsReferenceTo(s_AllDevices, device))
                    continue;

                unusedDevices.Add(device);
            }

            return unusedDevices;
        }

        public static void PauseHaptics(this IInputUser user)
        {
            throw new NotImplementedException();
        }

        public static void ResumeHaptics(this IInputUser user)
        {
            throw new NotImplementedException();
        }

        public static void ResetHaptics(this IInputUser user)
        {
            throw new NotImplementedException();
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
        public static void Add(IInputUser user, string userName = null)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var userData = new UserData
            {
                id = ++s_LastUserId,
                userName = userName,
            };

            // Add to list.
            var userCount = s_AllUserCount;
            ArrayHelpers.AppendWithCapacity(ref s_AllUsers, ref userCount, user);
            ArrayHelpers.AppendWithCapacity(ref s_AllUserData, ref s_AllUserCount, userData);

            // Send notification.
            Notify(user, InputUserChange.Added);
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
        public static void Remove(IInputUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return;

            // Remove devices.
            var userData = s_AllUserData[index];
            if (userData.deviceCount > 0)
            {
                ArrayHelpers.EraseSliceWithCapacity(ref s_AllDevices, ref s_AllDeviceCount, userData.deviceStartIndex,
                    userData.deviceCount);
            }

            // Remove.
            var userCount = s_AllUserCount;
            ArrayHelpers.EraseAtWithCapacity(ref s_AllUsers, ref userCount, index);
            ArrayHelpers.EraseAtWithCapacity(ref s_AllUserData, ref s_AllUserCount, index);

            // Send notification.
            Notify(user, InputUserChange.Removed);
        }

        /// <summary>
        /// Find the user (if any) that is currently assigned the given <paramref name="device"/>.
        /// </summary>
        /// <param name="device">An input device that has been added to the system.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <returns>The user that has <paramref name="device"/> among its <see cref="devices"/> or null if
        /// no user is currently assigned the given device.</returns>
        public static IInputUser FindUserForDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            throw new NotImplementedException();
        }

        private static void Notify(IInputUser user, InputUserChange change)
        {
            for (var i = 0; i < s_OnChange.length; ++i)
                s_OnChange[i](user, change);
        }

        private static int FindUserIndex(IInputUser user)
        {
            for (var i = 0; i < s_AllUserCount; ++i)
            {
                if (s_AllUsers[i] == user)
                    return i;
            }
            return -1;
        }

        internal struct UserData
        {
            public ulong id;
            public string userName;
            public int deviceCount;
            public int deviceStartIndex;
            public InputActionStack actionStack;
            public InputControlScheme? controlScheme; ////TODO: this will have to be relayed to actions/bindings
            public InputUserHandle? handle;
        }

        internal static uint s_LastUserId;
        internal static int s_AllUserCount;
        internal static int s_AllDeviceCount;
        internal static IInputUser[] s_AllUsers;
        internal static UserData[] s_AllUserData;
        internal static InputDevice[] s_AllDevices; // We keep a single array that we slice out to each user.
        internal static InlinedArray<Action<IInputUser, InputUserChange>> s_OnChange;

        ////WIP
        /*
        ////REVIEW: are control schemes affecting this?
        public ReadOnlyArray<InputBinding> customBindings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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

        */
    }
}
