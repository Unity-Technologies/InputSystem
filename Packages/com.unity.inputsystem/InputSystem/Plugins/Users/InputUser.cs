using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;

////FIXME: with BindOnlyToAssignedInputDevices on, we can easily end up in situations where reconfiguring the devices
////       and/or control schemes on a user will lead to bindings getting resolved multiple times

////TODO: kill InputDevice.userId

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
        /// List of all current users.
        /// </summary>
        public static ReadOnlyArray<IInputUser> all
        {
            get { return new ReadOnlyArray<IInputUser>(s_AllUsers, 0, s_AllUserCount); }
        }

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        public static event Action<IInputUser, InputUserChange> onChange
        {
            add { s_OnChange.AppendWithCapacity(value); }
            ////TODO: probably don't want to move tail
            remove { s_OnChange.RemoveByMovingTailWithCapacity(value); }
        }

        ////REVIEW: should this make the binding that triggered available to the callback?
        /// <summary>
        /// Event that is triggered when an action that is associated with a user is triggered from
        /// a device that is not currently assigned to the user.
        /// </summary>
        public static event Action<IInputUser, InputAction, InputControl> onUnassignedDeviceUsed
        {
            add
            {
                s_OnUnassignedDeviceUsed.AppendWithCapacity(value);
                if (!s_OnActionChangedHooked)
                {
                    if (s_OnActionTriggered == null)
                        s_OnActionTriggered = OnActionTriggered;
                    InputSystem.onActionChange += s_OnActionTriggered;
                    s_OnActionChangedHooked = true;
                }
            }
            remove
            {
                ////TODO: probably don't want to move tail
                s_OnUnassignedDeviceUsed.RemoveByMovingTailWithCapacity(value);
                if (s_OnUnassignedDeviceUsed.length == 0)
                {
                    InputSystem.onActionChange -= OnActionTriggered;
                    s_OnActionChangedHooked = false;
                }
            }
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
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            s_AllUserData[index].handle = handle;
            Notify(user, InputUserChange.HandleChanged);
        }

        ////REVIEW: do we really need/want this? should this be a property on IInputUser instead?
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

        public static IInputActionCollection GetInputActions<TUser>(this TUser user)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                return null;

            return s_AllUserData[index].actions;
        }

        /// <summary>
        /// Associate an .inputactions asset with the given user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="actions"></param>
        /// <typeparam name="TUser"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AssignInputActions<TUser>(this TUser user, IInputActionCollection actions)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (actions == null)
                throw new ArgumentNullException("actions");

            var userIndex = FindUserIndex(user);
            if (userIndex == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            s_AllUserData[userIndex].actions = actions;
            AssignDevicesToActionsIfNecessary(userIndex);
        }

        ////TODO: keep copy of reference and if two users end up with the same reference, automatically all MakePrivateCopyOfActions

        public static void AssignInputActions<TUser>(this TUser user, InputActionAssetReference assetReference)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (assetReference == null)
                throw new ArgumentNullException("assetReference");

            AssignInputActions(user, assetReference.asset);
        }

        /// <summary>
        /// Make bindings specific to the devices assigned to the user.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// By default, assigning devices (<see cref="InputUser.AssignInputDevice{TUser}(TUser,InputDevice)"/> to
        /// a user will not restrict which devices the user's actions will bind to. This is desirable, for example,
        /// to allow the user to be assigned on specific Gamepad in the system, yet also allow the user to switch
        /// to another Gamepad seamlessly.
        ///
        /// By passing true for <paramref name="value"/>, the user's binding will
        /// </remarks>
        public static void BindOnlyToAssignedInputDevices<TUser>(this TUser user, bool value = true)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var userIndex = FindUserIndex(user);
            if (userIndex == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            if (value)
            {
                // Do nothing if already enabled.
                if ((s_AllUserData[userIndex].flags & UserFlags.BindOnlyToAssignedInputDevices) != 0)
                    return;

                s_AllUserData[userIndex].flags |= UserFlags.BindOnlyToAssignedInputDevices;

                // If we already have actions assigned to the user, assign our set of devices (may be empty) to them.
                AssignDevicesToActionsIfNecessary(userIndex);
            }
            else
            {
                // Do nothing if already disabled.
                if ((s_AllUserData[userIndex].flags & UserFlags.BindOnlyToAssignedInputDevices) == 0)
                    return;

                s_AllUserData[userIndex].flags &= ~UserFlags.BindOnlyToAssignedInputDevices;

                // Reset device list on actions, if we have any.
                var actions = s_AllUserData[userIndex].actions;
                if (actions != null)
                    actions.devices = null;
            }
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

        public static ControlSchemeSyntax AssignControlScheme<TUser>(this TUser user, string schemeName)
            where TUser : class, IInputUser
        {
            return AssignControlScheme(user, new InputControlScheme(schemeName));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="scheme"></param>
        /// <typeparam name="TUser"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static ControlSchemeSyntax AssignControlScheme<TUser>(this TUser user, InputControlScheme scheme)
            where TUser : class, IInputUser
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var index = FindUserIndex(user);
            if (index == -1)
                throw new InvalidOperationException(string.Format("User '{0}' has not been added to the system", user));

            if (string.IsNullOrEmpty(scheme.name) && !s_AllUserData[index].controlScheme.HasValue)
                return new ControlSchemeSyntax {m_UserIndex = index};

            if (!string.IsNullOrEmpty(scheme.name) && s_AllUserData[index].controlScheme.HasValue &&
                string.Compare(
                    s_AllUserData[index].controlScheme.Value.m_Name, scheme.name,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                return new ControlSchemeSyntax {m_UserIndex = index};

            if (string.IsNullOrEmpty(scheme.name))
                s_AllUserData[index].controlScheme = null;
            else
                s_AllUserData[index].controlScheme = scheme;

            Notify(user, InputUserChange.ControlSchemeChanged);
            return new ControlSchemeSyntax {m_UserIndex = index};
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
            {
                AssignDevicesToActionsIfNecessary(userIndex);
                Notify(user, InputUserChange.DevicesChanged);
            }
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
            {
                AssignDevicesToActionsIfNecessary(userIndex);
                Notify(user, InputUserChange.DevicesChanged);
            }
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

        private static void AssignDevicesToActionsIfNecessary(int userIndex)
        {
            // Ignore if not enabled.
            if ((s_AllUserData[userIndex].flags & UserFlags.BindOnlyToAssignedInputDevices) == 0)
                return;

            // Ignore if not having actions.
            var actions = s_AllUserData[userIndex].actions;
            if (actions == null)
                return;

            actions.devices = s_AllUsers[userIndex].GetAssignedInputDevices();
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
        public static InputControlList<InputDevice> GetUnassignedInputDevices()
        {
            var unusedDevices = new InputControlList<InputDevice>(Allocator.Temp);

            foreach (var device in InputSystem.devices)
            {
                // If it's in s_AllDevices, there is *some* user that is using the device.
                // We don't care which one it is here.
                if (ArrayHelpers.ContainsReferenceTo(s_AllDevices, s_AllDeviceCount, device))
                    continue;

                unusedDevices.Add(device);
            }

            return unusedDevices;
        }

        public static void PauseHaptics(this IInputUser user)
        {
            ////TODO
        }

        public static void ResumeHaptics(this IInputUser user)
        {
            ////TODO
        }

        public static void ResetHaptics(this IInputUser user)
        {
            ////TODO
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

            var indexOfDevice = ArrayHelpers.IndexOfReference(s_AllDevices, s_AllDeviceCount, device);
            if (indexOfDevice == -1)
                return null;

            for (var i = 0; i < s_AllUserCount; ++i)
            {
                var startIndex = s_AllUserData[i].deviceStartIndex;
                if (startIndex >= indexOfDevice && indexOfDevice < startIndex + s_AllUserData[i].deviceCount)
                    return s_AllUsers[i];
            }

            Debug.Assert(false, "Should not reach here");
            return null;
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

        private static void OnActionTriggered(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionTriggered)
                return;

            ////REVIEW: filter for noise here?

            // Grab control that triggered the action.
            var action = (InputAction)obj;
            var control = action.lastTriggerControl;
            if (control == null)
                return;

            // See if it's coming from a device not belonging to any user.
            var device = control.device;
            if (ArrayHelpers.ContainsReferenceTo(s_AllDevices, s_AllDeviceCount, device))
            {
                // No, it's a device already assigned to a player so do nothing.
                return;
            }

            // Ok, we know it's a device not assigned to a user and it triggered action. The only
            // thing that remains is to determine whether it was from an action assigned to a user.
            var userIndex = -1;
            for (var i = 0; i < s_AllUserCount; ++i)
            {
                var actions = s_AllUserData[i].actions;
                if (actions != null && actions.Contains(action))
                {
                    userIndex = i;
                    break;
                }
            }

            if (userIndex != -1)
            {
                var user = s_AllUsers[userIndex];
                for (var i = 0; i < s_OnUnassignedDeviceUsed.length; ++i)
                    s_OnUnassignedDeviceUsed[i](user, action, control);
            }
        }

        /// <summary>
        /// Syntax for configuring a control scheme on a user.
        /// </summary>
        public struct ControlSchemeSyntax
        {
            /// <summary>
            /// Unassign the user's currently assigned devices (if any) and assign available
            /// devices matching the newly assigned control scheme.
            /// </summary>
            /// <returns></returns>
            public ControlSchemeSyntax AndAssignDevices()
            {
                return AndAssignDevicesInternal(addMissingOnly: false);
            }

            /// <summary>
            /// Leave the user's assigned devices in place but assign any available devices
            /// that are still required by the control scheme.
            /// </summary>
            /// <returns></returns>
            public ControlSchemeSyntax AndAssignMissingDevices()
            {
                return AndAssignDevicesInternal(addMissingOnly: true);
            }

            public ControlSchemeSyntax AndAssignDevices(out InputControlScheme.MatchResult matchResult)
            {
                return AndAssignDevicesInternal(out matchResult, addMissingOnly: false);
            }

            public ControlSchemeSyntax AndAssignMissingDevices(out InputControlScheme.MatchResult matchResult)
            {
                return AndAssignDevicesInternal(out matchResult, addMissingOnly: true);
            }

            private ControlSchemeSyntax AndAssignDevicesInternal(bool addMissingOnly)
            {
                var matchResult = new InputControlScheme.MatchResult();
                try
                {
                    return AndAssignDevicesInternal(out matchResult, addMissingOnly: addMissingOnly);
                }
                finally
                {
                    matchResult.Dispose();
                }
            }

            private ControlSchemeSyntax AndAssignDevicesInternal(out InputControlScheme.MatchResult matchResult, bool addMissingOnly)
            {
                matchResult = new InputControlScheme.MatchResult();

                // If we are currently assigned devices and we're supposed to assign devices from scratch,
                // unassign what we have first.
                var needToNotifyAboutChangedDevices = false;
                if (!addMissingOnly && s_AllUserData[m_UserIndex].deviceCount > 0)
                {
                    ClearAssignedInputDevicesInternal(m_UserIndex);
                    needToNotifyAboutChangedDevices = true;
                }

                // Nothing to do if we don't have a control scheme.
                if (!s_AllUserData[m_UserIndex].controlScheme.HasValue)
                    return this;

                // Look for matching devices now which aren't used by any other user.
                // Only go through the matching process if we actually have device requirements.
                var scheme = s_AllUserData[m_UserIndex].controlScheme.Value;
                if (scheme.deviceRequirements.Count > 0)
                {
                    // Grab all unused devices and then select a set of devices matching the scheme's
                    // requirements.
                    using (var availableDevices = GetUnassignedInputDevices())
                    {
                        // If we're only supposed to add missing devices, we need to take the devices already
                        // assigned to the user into account when picking devices. Add them to the beginning
                        // of the list so that they get matched first.
                        if (addMissingOnly)
                            availableDevices.AddSlice(s_AllUsers[m_UserIndex].GetAssignedInputDevices(), destinationIndex: 0);

                        matchResult = scheme.PickDevicesFrom(availableDevices);
                        if (matchResult.isSuccessfulMatch)
                        {
                            // Control scheme is satisfied with the devices we have available.
                            // Assign selected devices to user.
                            foreach (var device in matchResult.devices)
                            {
                                if (AssignDeviceInternal(m_UserIndex, device))
                                    needToNotifyAboutChangedDevices = true;
                            }
                        }
                        else
                        {
                            // Control scheme isn't satisfied with the devices we got.
                            m_Failure = true;
                        }
                    }
                }

                if (needToNotifyAboutChangedDevices)
                {
                    AssignDevicesToActionsIfNecessary(m_UserIndex);
                    Notify(s_AllUsers[m_UserIndex], InputUserChange.DevicesChanged);
                }

                return this;
            }

            /// <summary>
            /// Mask out (i.e. disable) every binding that doesn't belong to the assigned control scheme.
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// When using a specific control scheme, it can be desirable to either render any binding outside
            /// the control scheme unusable or to leave all bindings enabled such that it is possible to
            /// automatically detect when the user switches to a different control scheme.
            ///
            /// The first case, where we mask out any other binding, is supported by this method. When called,
            /// the binding group of the current control scheme (<see cref="InputControlScheme.bindingGroup)"/>)
            /// will be applied to the actions assigned to the user (<see
            /// cref="InputUser.AssignInputActions{TUser}(TUser,IInputActionCollection)"/>).
            ///
            /// Note that for this method to work, actions have to be assigned to the user first.
            ///
            /// By default, bindings will not get masked and every binding will activate when actions
            /// are enabled.
            /// </remarks>
            /// <seealso cref="InputUser.AssignInputActions{TUser}(TUser,IInputActionCollection)"/>
            /// <seealso cref="IInputActionCollection.bindingMask"/>
            /// <seealso cref="IInputActionCollection.SetBindingMask"/>
            public ControlSchemeSyntax AndMaskBindingsFromOtherControlSchemes()
            {
                // Nothing to do if we don't have a control scheme.
                if (!s_AllUserData[m_UserIndex].controlScheme.HasValue)
                    return this;

                var actions = s_AllUserData[m_UserIndex].actions;
                if (actions == null)
                    throw new InvalidOperationException(string.Format(
                        "No actions have been assigned to user '{0}'; call AssignInputActions() first",
                        s_AllUsers[m_UserIndex]));

                var bindingGroup = s_AllUserData[m_UserIndex].controlScheme.Value.bindingGroup;
                actions.bindingMask = new InputBinding {groups = bindingGroup};
                return this;
            }

            public static implicit operator bool(ControlSchemeSyntax syntax)
            {
                return !syntax.m_Failure;
            }

            internal int m_UserIndex;
            internal bool m_Failure;
        }

        [Flags]
        internal enum UserFlags
        {
            BindOnlyToAssignedInputDevices = 1 << 0,
        }

        /// <summary>
        /// Data we store for each user.
        /// </summary>
        internal struct UserData
        {
            public ulong id;
            public string userName;
            public InputUserHandle? handle;
            public UserFlags flags;

            public int deviceCount;
            public int deviceStartIndex;

            public IInputActionCollection actions;
            public InputControlScheme? controlScheme;
        }

        internal static uint s_LastUserId;
        internal static int s_AllUserCount;
        internal static int s_AllDeviceCount;
        internal static IInputUser[] s_AllUsers;
        internal static UserData[] s_AllUserData;
        internal static InputDevice[] s_AllDevices; // We keep a single array that we slice out to each user.
        internal static InlinedArray<Action<IInputUser, InputUserChange>> s_OnChange;
        internal static InlinedArray<Action<IInputUser, InputAction, InputControl>> s_OnUnassignedDeviceUsed;
        internal static Action<object, InputActionChange> s_OnActionTriggered;
        internal static bool s_OnActionChangedHooked;

        internal static void ResetGlobals()
        {
            s_AllUserCount = 0;
            s_AllDeviceCount = 0;
            s_AllUsers = null;
            s_AllUserData = null;
            s_AllDevices = null;
            s_OnChange = new InlinedArray<Action<IInputUser, InputUserChange>>();
            s_OnUnassignedDeviceUsed = new InlinedArray<Action<IInputUser, InputAction, InputControl>>();
            s_OnActionTriggered = null;
            s_OnActionChangedHooked = false;
        }

        ////WIP
        /*
        ////REVIEW: are control schemes affecting this?
        public ReadOnlyArray<InputBinding> customBindings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        */
    }
}
