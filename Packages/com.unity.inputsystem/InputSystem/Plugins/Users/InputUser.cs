using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;

////REVIEW: remove users automatically when exiting play mode?

////REVIEW: do we need to handle the case where devices are added to a user that are each associated with a different user account

////REVIEW: how should we handle pairings of devices *not* called for by a control scheme? should that result in a failed match?

////TODO: option to bind to *all* devices instead of just the paired ones (bindToAllDevices)

////TODO: the account selection stuff needs cleanup; the current flow is too convoluted

namespace UnityEngine.InputSystem.Users
{
    /// <summary>
    /// Represents a specific user/player interacting with one or more devices and input actions.
    /// </summary>
    /// <remarks>
    /// Principally, an InputUser represents a human interacting with the application. Moreover, at any point
    /// each InputUser represents a human actor distinct from all other InputUsers in the system.
    ///
    /// Each user has one or more paired devices. In general, these devices are unique to each user. However,
    /// it is permitted to use <see cref="PerformPairingWithDevice"/> to pair the same device to multiple users.
    /// This can be useful in setups such as split-keyboard (e.g. one user using left side of keyboard and the
    /// other the right one) use or hotseat-style gameplay (e.g. two players taking turns on the same game controller).
    ///
    /// Note that the InputUser API, like <see cref="InputAction"/>) is a play mode-only feature. When exiting play mode,
    /// all users are automatically removed and all devices automatically unpaired.
    /// </remarks>
    /// <seealso cref="InputUserChange"/>
    public struct InputUser : IEquatable<InputUser>
    {
        public const uint InvalidId = 0;

        /// <summary>
        /// Whether this is a currently active user record in <see cref="all"/>.
        /// </summary>
        /// <remarks>
        /// Users that are removed (<see cref="UnpairDevicesAndRemoveUser"/>) will become invalid.
        /// </remarks>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.Removed"/>
        public bool valid
        {
            get
            {
                if (m_Id == InvalidId)
                    return false;

                // See if there's a currently active user with the given ID.
                for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                    if (s_GlobalState.allUsers[i].m_Id == m_Id)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// The sequence number of the user.
        /// </summary>
        /// <remarks>
        /// It can be useful to establish a sorting of players locally such that it is
        /// known who is the first player, who is the second, and so on. This property
        /// gives the positioning of the user within <see cref="all"/>.
        ///
        /// Note that the index of a user may change as users are added and removed.
        /// </remarks>
        /// <seealso cref="all"/>
        public int index
        {
            get
            {
                if (m_Id == InvalidId)
                    throw new InvalidOperationException("Invalid user");

                var userIndex = TryFindUserIndex(m_Id);
                if (userIndex == -1)
                    throw new InvalidOperationException($"User with ID {m_Id} is no longer valid");

                return userIndex;
            }
        }

        /// <summary>
        /// The unique numeric ID of the user.
        /// </summary>
        /// <remarks>
        /// The ID of a user is internally assigned and cannot be changed over its lifetime. No two users, even
        /// if not concurrently active, will receive the same ID.
        ///
        /// The ID stays valid and unique even if the user is removed and no longer <see cref="valid"/>.
        /// </remarks>
        public uint id => m_Id;

        ////TODO: bring documentation for these back when user management is implemented on Xbox and PS
        public InputUserAccountHandle? platformUserAccountHandle => s_GlobalState.allUserData[index].platformUserAccountHandle;
        public string platformUserAccountName => s_GlobalState.allUserData[index].platformUserAccountName;
        public string platformUserAccountId => s_GlobalState.allUserData[index].platformUserAccountId;

        ////REVIEW: Does it make sense to track used devices separately from paired devices?
        /// <summary>
        /// Devices assigned/paired/linked to the user.
        /// </summary>
        /// <remarks>
        /// It is generally valid for a device to be assigned to multiple users. For example, two users could
        /// both use the local keyboard in a split-keyboard or hot seat setup. However, a platform may restrict this
        /// and mandate that a device never belong to more than one user. This is the case on Xbox and PS4, for
        /// example.
        ///
        /// To associate devices with users, use <see cref="PerformPairingWithDevice"/>. To remove devices, use
        /// <see cref="UnpairDevice"/> or <see cref="UnpairDevicesAndRemoveUser"/>.
        ///
        /// The array will be empty for a user who is currently not paired to any devices.
        ///
        /// If <see cref="actions"/> is set (<see cref="AssociateActionsWithUser(IInputActionCollection)"/>), then
        /// <see cref="IInputActionCollection.devices"/> will be kept synchronized with the devices paired to the user.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DevicePaired"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public ReadOnlyArray<InputDevice> pairedDevices
        {
            get
            {
                var userIndex = index;
                return new ReadOnlyArray<InputDevice>(s_GlobalState.allPairedDevices, s_GlobalState.allUserData[userIndex].deviceStartIndex,
                    s_GlobalState.allUserData[userIndex].deviceCount);
            }
        }

        /// <summary>
        /// Devices that were removed while they were still paired to the user.
        /// </summary>
        /// <remarks>
        ///
        /// This list is cleared once the user has either regained lost devices or has regained other devices
        /// such that the <see cref="controlScheme"/> is satisfied.
        /// </remarks>
        /// <seealso cref="InputUserChange.DeviceRegained"/>
        /// <seealso cref="InputUserChange.DeviceLost"/>
        public ReadOnlyArray<InputDevice> lostDevices
        {
            get
            {
                var userIndex = index;
                return new ReadOnlyArray<InputDevice>(s_GlobalState.allLostDevices, s_GlobalState.allUserData[userIndex].lostDeviceStartIndex,
                    s_GlobalState.allUserData[userIndex].lostDeviceCount);
            }
        }


        /// <summary>
        /// Actions associated with the user.
        /// </summary>
        /// <remarks>
        /// Associating actions with a user will synchronize the actions with the devices paired to the
        /// user. Also, it makes it possible to use support for control scheme activation (<see
        /// cref="ActivateControlScheme(InputControlScheme)"/> and related APIs like <see cref="controlScheme"/>
        /// and <see cref="controlSchemeMatch"/>).
        ///
        /// Note that is generally does not make sense for users to share actions. Instead, each user should
        /// receive a set of actions private to the user.
        /// </remarks>
        /// <seealso cref="AssociateActionsWithUser(IInputActionCollection)"/>
        /// <seealso cref="InputActionMap"/>
        /// <seealso cref="InputActionAsset"/>
        /// <seealso cref="InputUserChange.ControlsChanged"/>
        public IInputActionCollection actions => s_GlobalState.allUserData[index].actions;

        /// <summary>
        /// The control scheme currently employed by the user.
        /// </summary>
        /// <remarks>
        /// This is null by default.
        ///
        /// Any time the value of this property changes (whether by <see cref="ActivateControlScheme(string)"/>
        /// or by automatic switching), a notification is sent on <see cref="onChange"/> with
        /// <see cref="InputUserChange.ControlSchemeChanged"/>.
        ///
        /// Be aware that using control schemes with InputUsers requires <see cref="actions"/> to
        /// be set, i.e. input actions to be associated with the user (<see
        /// cref="AssociateActionsWithUser(IInputActionCollection)"/>).
        /// </remarks>
        /// <seealso cref="ActivateControlScheme(string)"/>
        /// <seealso cref="ActivateControlScheme(InputControlScheme)"/>
        /// <seealso cref="InputUserChange.ControlSchemeChanged"/>
        public InputControlScheme? controlScheme => s_GlobalState.allUserData[index].controlScheme;

        /// <summary>
        /// The result of matching the device requirements given by <see cref="controlScheme"/> against
        /// the devices paired to the user (<see cref="pairedDevices"/>).
        /// </summary>
        /// <remarks>
        /// When devices are paired to or unpaired from a user, as well as when a new control scheme is
        /// activated on a user, this property is updated automatically.
        /// </remarks>
        /// <seealso cref="InputControlScheme.deviceRequirements"/>
        /// <seealso cref="InputControlScheme.PickDevicesFrom{TDevices}"/>
        public InputControlScheme.MatchResult controlSchemeMatch => s_GlobalState.allUserData[index].controlSchemeMatch;

        /// <summary>
        /// Whether the user is missing devices required by the <see cref="controlScheme"/> activated
        /// on the user.
        /// </summary>
        /// <remarks>
        /// This will only take required devices into account. Device requirements marked optional (<see
        /// cref="InputControlScheme.DeviceRequirement.isOptional"/>) will not be considered missing
        /// devices if they cannot be satisfied based on the devices paired to the user.
        /// </remarks>
        /// <seealso cref="InputControlScheme.deviceRequirements"/>
        public bool hasMissingRequiredDevices => s_GlobalState.allUserData[index].controlSchemeMatch.hasMissingRequiredDevices;

        /// <summary>
        /// List of all current users.
        /// </summary>
        /// <remarks>
        /// Use <see cref="PerformPairingWithDevice"/> to add new users and <see cref="UnpairDevicesAndRemoveUser"/> to
        /// remove users.
        ///
        /// Note that this array does not necessarily correspond to the list of users present at the platform level
        /// (e.g. Xbox and PS4). There can be users present at the platform level that are not present in this array
        /// (e.g. because they are not joined to the game) and users can even be present more than once (e.g. if
        /// playing on the user account but as two different players in the game). Also, there can be users in the array
        /// that are not present at the platform level.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        public static ReadOnlyArray<InputUser> all => new ReadOnlyArray<InputUser>(s_GlobalState.allUsers, 0, s_GlobalState.allUserCount);

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        /// <remarks>
        /// Each notification receives the user that was affected by the change and, in the form of <see cref="InputUserChange"/>,
        /// a description of what has changed about the user. The third parameter may be null but if the change will be related
        /// to an input device, will reference the device involved in the change.
        /// </remarks>
        public static event Action<InputUser, InputUserChange, InputDevice> onChange
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onChange.AddCallback(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onChange.RemoveCallback(value);
            }
        }

        /// <summary>
        /// Event that is triggered when a device is used that is not currently paired to any user.
        /// </summary>
        /// <remarks>
        /// A device is considered "used" when it has magnitude (<see cref="InputControl.EvaluateMagnitude()"/>) greater than zero
        /// on a control that is not noisy (<see cref="InputControl.noisy"/>) and not synthetic (i.e. not a control that is
        /// "made up" like <see cref="Keyboard.anyKey"/>; <see cref="InputControl.synthetic"/>).
        ///
        /// Detecting the use of unpaired devices has a non-zero cost. While multiple levels of tests are applied to try to
        /// cheaply ignore devices that have events sent to them that do not contain user activity, finding out whether
        /// a device had real user activity will eventually require going through the device control by control.
        ///
        /// To enable detection of the use of unpaired devices, set <see cref="listenForUnpairedDeviceActivity"/> to true.
        /// It is disabled by default.
        ///
        /// The callback is invoked for each non-leaf, non-synthetic, non-noisy control that has been actuated on the device.
        /// It being restricted to non-leaf controls means that if, say, the stick on a gamepad is actuated in both X and Y
        /// direction, you will see two calls: one with stick/x and one with stick/y.
        ///
        /// The reason that the callback is invoked for each individual control is that pairing often relies on checking
        /// for specific kinds of interactions. For example, a pairing callback may listen exclusively for button presses.
        ///
        /// Note that whether the use of unpaired devices leads to them getting paired is under the control of the application.
        /// If the device should be paired, invoke <see cref="PerformPairingWithDevice"/> from the callback. If you do so,
        /// no further callbacks will get triggered for other controls that may have been actuated in the same event.
        ///
        /// Be aware that the callback is fired <em>before</em> input is actually incorporated into the device (it is
        /// indirectly triggered from <see cref="InputSystem.onEvent"/>). This means at the time the callback is run,
        /// the state of the given device does not yet have the input that triggered the callback. For this reason, the
        /// callback receives a second argument that references the event from which the use of an unpaired device was
        /// detected.
        ///
        /// What this sequence allows is to make changes to the system before the input is processed. For example, an
        /// action that is enabled as part of the callback will subsequently respond to the input that triggered the
        /// callback.
        ///
        /// <example>
        /// <code>
        /// // Activate support for listening to device activity.
        /// ++InputUser.listenForUnpairedDeviceActivity;
        ///
        /// // When a button on an unpaired device is pressed, pair the device to a new
        /// // or existing user.
        /// InputUser.onUnpairedDeviceUsed +=
        ///     usedControl =>
        ///     {
        ///         // Only react to button presses on unpaired devices.
        ///         if (!(usedControl is ButtonControl))
        ///             return;
        ///
        ///         // Pair the device to a user.
        ///         InputUser.PerformPairingWithDevice(usedControl.device);
        ///     };
        /// </code>
        /// </example>
        ///
        /// Another possible use of the callback is for implementing automatic control scheme switching for a user such that
        /// the user can, for example, switch from keyboard&amp;mouse to gamepad seamlessly by simply picking up the gamepad
        /// and starting to play.
        /// </remarks>
        public static event Action<InputControl, InputEventPtr> onUnpairedDeviceUsed
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onUnpairedDeviceUsed.AddCallback(value);
                if (s_GlobalState.listenForUnpairedDeviceActivity > 0)
                    HookIntoEvents();
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onUnpairedDeviceUsed.RemoveCallback(value);
                if (s_GlobalState.onUnpairedDeviceUsed.length == 0)
                    UnhookFromDeviceStateChange();
            }
        }

        /// <summary>
        /// Callback that works in combination with <see cref="onUnpairedDeviceUsed"/>. If all callbacks
        /// added to this event return <c>false</c> for a
        /// </summary>
        /// <remarks>
        /// Checking a given event for activity of interest is relatively fast but is still costlier than
        /// not doing it all. In case only certain devices are of interest for <see cref="onUnpairedDeviceUsed"/>,
        /// this "pre-filter" can be used to quickly reject entire devices and thus skip looking closer at
        /// an event.
        ///
        /// The first argument is the <see cref="InputDevice"/> than an event has been received for.
        /// The second argument is the <see cref="InputEvent"/> that is being looked at.
        ///
        /// A callback should return <c>true</c> if it wants <see cref="onUnpairedDeviceUsed"/> to proceed
        /// looking at the event and should return <c>false</c> if the event should be skipped.
        ///
        /// If multiple callbacks are added to the event, it is enough for any single one callback
        /// to return <c>true</c> for the event to get looked at.
        /// </remarks>
        /// <seealso cref="onUnpairedDeviceUsed"/>
        /// <seealso cref="listenForUnpairedDeviceActivity"/>
        public static event Func<InputDevice, InputEventPtr, bool> onPrefilterUnpairedDeviceActivity
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onPreFilterUnpairedDeviceUsed.AddCallback(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                s_GlobalState.onPreFilterUnpairedDeviceUsed.RemoveCallback(value);
            }
        }

        ////TODO: After 1.0, make this a simple bool API that *underneath* uses a counter rather than exposing the counter
        ////      directly to the user.
        /// <summary>
        /// Whether to listen for user activity on currently unpaired devices and invoke <see cref="onUnpairedDeviceUsed"/>
        /// if such activity is detected.
        /// </summary>
        /// <remarks>
        /// This is off by default.
        ///
        /// Note that enabling this has a non-zero cost. Whenever the state changes of a device that is not currently paired
        /// to a user, the system has to spend time figuring out whether there was a meaningful change or whether it's just
        /// noise on the device.
        ///
        /// This is an integer rather than a bool to allow multiple systems to concurrently use to listen for unpaired
        /// device activity without treading on each other when enabling/disabling the code path.
        /// </remarks>
        /// <seealso cref="onUnpairedDeviceUsed"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static int listenForUnpairedDeviceActivity
        {
            get => s_GlobalState.listenForUnpairedDeviceActivity;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Cannot be negative");
                if (value > 0 && s_GlobalState.onUnpairedDeviceUsed.length > 0)
                    HookIntoEvents();
                else if (value == 0)
                    UnhookFromDeviceStateChange();
                s_GlobalState.listenForUnpairedDeviceActivity = value;
            }
        }

        public override string ToString()
        {
            if (!valid)
                return $"<Invalid> (id: {m_Id})";

            var deviceList = string.Join(",", pairedDevices);
            return $"User #{index} (id: {m_Id}, devices: {deviceList}, actions: {actions})";
        }

        /// <summary>
        /// Associate a collection of <see cref="InputAction"/>s with the user.
        /// </summary>
        /// <param name="actions">Actions to associate with the user, either an <see cref="InputActionAsset"/>
        /// or an <see cref="InputActionMap"/>. Can be <c>null</c> to unset the current association.</param>
        /// <exception cref="InvalidOperationException">The user instance is invalid.</exception>
        /// <remarks>
        /// Associating actions with a user will ensure that the <see cref="IInputActionCollection.devices"/> and
        /// <see cref="IInputActionCollection.bindingMask"/> property of the action collection are automatically
        /// kept in sync with the device paired to the user (see <see cref="pairedDevices"/>) and the control
        /// scheme active on the user (see <see cref="controlScheme"/>).
        ///
        /// <example>
        /// <code>
        /// var gamepad = Gamepad.all[0];
        ///
        /// // Pair the gamepad to a user.
        /// var user = InputUser.PerformPairingWithDevice(gamepad);
        ///
        /// // Create an action map with an action.
        /// var actionMap = new InputActionMap():
        /// actionMap.AddAction("Fire", binding: "&lt;Gamepad&gt;/buttonSouth");
        ///
        /// // Associate the action map with the user (the same works for an asset).
        /// user.AssociateActionsWithUser(actionMap);
        ///
        /// // Now the action map is restricted to just the gamepad that is paired
        /// // with the user, even if there are more gamepads currently connected.
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="actions"/>
        public void AssociateActionsWithUser(IInputActionCollection actions)
        {
            var userIndex = index; // Throws if user is invalid.
            if (s_GlobalState.allUserData[userIndex].actions == actions)
                return;

            // If we already had actions associated, reset the binding mask and device list.
            var oldActions = s_GlobalState.allUserData[userIndex].actions;
            if (oldActions != null)
            {
                oldActions.devices = null;
                oldActions.bindingMask = null;
            }

            s_GlobalState.allUserData[userIndex].actions = actions;

            // If we've switched to a different set of actions, synchronize our state.
            if (actions != null)
            {
                HookIntoActionChange();

                actions.devices = pairedDevices;
                if (s_GlobalState.allUserData[userIndex].controlScheme != null)
                    ActivateControlSchemeInternal(userIndex, s_GlobalState.allUserData[userIndex].controlScheme.Value);
            }
        }

        public ControlSchemeChangeSyntax ActivateControlScheme(string schemeName)
        {
            // Look up control scheme by name in actions.
            if (!string.IsNullOrEmpty(schemeName))
            {
                FindControlScheme(schemeName, out InputControlScheme scheme); // throws if not found
                return ActivateControlScheme(scheme);
            }
            return ActivateControlScheme(new InputControlScheme());
        }

        private bool TryFindControlScheme(string schemeName, out InputControlScheme scheme)
        {
            if (string.IsNullOrEmpty(schemeName))
            {
                scheme = default;
                return false;
            }

            // Need actions to be available to be able to activate control schemes by name only.
            if (s_GlobalState.allUserData[index].actions == null)
                throw new InvalidOperationException(
                    $"Cannot set control scheme '{schemeName}' by name on user #{index} as not actions have been associated with the user yet (AssociateActionsWithUser)");

            // Attempt to find control scheme by name
            var controlSchemes = s_GlobalState.allUserData[index].actions.controlSchemes;
            for (var i = 0; i < controlSchemes.Count; ++i)
            {
                if (string.Compare(controlSchemes[i].name, schemeName,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    scheme = controlSchemes[i];
                    return true;
                }
            }

            scheme = default;
            return false;
        }

        internal void FindControlScheme(string schemeName, out InputControlScheme scheme)
        {
            if (TryFindControlScheme(schemeName, out scheme))
                return;
            throw new ArgumentException(
                $"Cannot find control scheme '{schemeName}' in actions '{s_GlobalState.allUserData[index].actions}'");
        }

        public ControlSchemeChangeSyntax ActivateControlScheme(InputControlScheme scheme)
        {
            var userIndex = index; // Throws if user is invalid.

            if (s_GlobalState.allUserData[userIndex].controlScheme != scheme ||
                (scheme == default && s_GlobalState.allUserData[userIndex].controlScheme != null))
            {
                ActivateControlSchemeInternal(userIndex, scheme);
                Notify(userIndex, InputUserChange.ControlSchemeChanged, null);
            }

            return new ControlSchemeChangeSyntax { m_UserIndex = userIndex };
        }

        private void ActivateControlSchemeInternal(int userIndex, InputControlScheme scheme)
        {
            var isEmpty = scheme == default;

            if (isEmpty)
                s_GlobalState.allUserData[userIndex].controlScheme = null;
            else
                s_GlobalState.allUserData[userIndex].controlScheme = scheme;

            if (s_GlobalState.allUserData[userIndex].actions != null)
            {
                if (isEmpty)
                {
                    s_GlobalState.allUserData[userIndex].actions.bindingMask = null;
                    s_GlobalState.allUserData[userIndex].controlSchemeMatch.Dispose();
                    s_GlobalState.allUserData[userIndex].controlSchemeMatch = new InputControlScheme.MatchResult();
                }
                else
                {
                    s_GlobalState.allUserData[userIndex].actions.bindingMask = new InputBinding { groups = scheme.bindingGroup };
                    UpdateControlSchemeMatch(userIndex);

                    // If we had lost some devices, flush the list. We haven't regained the device
                    // but we're no longer missing devices to play.
                    if (s_GlobalState.allUserData[userIndex].controlSchemeMatch.isSuccessfulMatch)
                        RemoveLostDevicesForUser(userIndex);
                }
            }
        }

        /// <summary>
        /// Unpair a single device from the user.
        /// </summary>
        /// <param name="device">Device to unpair from the user. If the device is not currently paired to the user,
        /// the method does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is automatically updated.
        ///
        /// If a control scheme is activated on the user (<see cref="controlScheme"/>), <see cref="controlSchemeMatch"/>
        /// is automatically updated.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/>.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public void UnpairDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var userIndex = index; // Throws if user is invalid.

            // Ignore if not currently paired to user.
            if (!pairedDevices.ContainsReference(device))
                return;

            RemoveDeviceFromUser(userIndex, device);
        }

        /// <summary>
        /// Unpair all devices from the user.
        /// </summary>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is automatically updated.
        ///
        /// If a control scheme is activated on the user (<see cref="controlScheme"/>), <see cref="controlSchemeMatch"/>
        /// is automatically updated.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/> for every device
        /// unpaired from the user.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public void UnpairDevices()
        {
            var userIndex = index; // Throws if user is invalid.

            RemoveLostDevicesForUser(userIndex);

            using (InputActionRebindingExtensions.DeferBindingResolution())
            {
                // We could remove the devices in bulk here but we still have to notify one
                // by one which ends up being more complicated than just unpairing the devices
                // individually here.
                while (s_GlobalState.allUserData[userIndex].deviceCount > 0)
                    UnpairDevice(s_GlobalState.allPairedDevices[s_GlobalState.allUserData[userIndex].deviceStartIndex + s_GlobalState.allUserData[userIndex].deviceCount - 1]);
            }

            // Update control scheme, if necessary.
            if (s_GlobalState.allUserData[userIndex].controlScheme != null)
                UpdateControlSchemeMatch(userIndex);
        }

        private static void RemoveLostDevicesForUser(int userIndex)
        {
            var lostDeviceCount = s_GlobalState.allUserData[userIndex].lostDeviceCount;
            if (lostDeviceCount > 0)
            {
                var lostDeviceStartIndex = s_GlobalState.allUserData[userIndex].lostDeviceStartIndex;
                ArrayHelpers.EraseSliceWithCapacity(ref s_GlobalState.allLostDevices, ref s_GlobalState.allLostDeviceCount,
                    lostDeviceStartIndex, lostDeviceCount);

                s_GlobalState.allUserData[userIndex].lostDeviceCount = 0;
                s_GlobalState.allUserData[userIndex].lostDeviceStartIndex = 0;

                // Adjust indices of other users.
                for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                {
                    if (s_GlobalState.allUserData[i].lostDeviceStartIndex > lostDeviceStartIndex)
                        s_GlobalState.allUserData[i].lostDeviceStartIndex -= lostDeviceCount;
                }
            }
        }

        /// <summary>
        /// Unpair all devices from the user and remove the user.
        /// </summary>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is reset as is the binding mask (<see
        /// cref="IInputActionCollection.bindingMask"/>) in case a control scheme is activated on the user.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/> for every device
        /// unpaired from the user.
        ///
        /// Sends <see cref="InputUserChange.Removed"/>.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        /// <seealso cref="InputUserChange.Removed"/>
        public void UnpairDevicesAndRemoveUser()
        {
            UnpairDevices();

            var userIndex = index;
            RemoveUser(userIndex);

            m_Id = default;
        }

        /// <summary>
        /// Return a list of all currently added devices that are not paired to any user.
        /// </summary>
        /// <returns>A (possibly empty) list of devices that are currently not paired to a user.</returns>
        /// <remarks>
        /// The resulting list uses <see cref="Allocator.Temp"> temporary, unmanaged memory</see>. If not disposed of
        /// explicitly, the list will automatically be deallocated at the end of the frame and will become unusable.
        /// </remarks>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static InputControlList<InputDevice> GetUnpairedInputDevices()
        {
            var list = new InputControlList<InputDevice>(Allocator.Temp);
            GetUnpairedInputDevices(ref list);
            return list;
        }

        /// <summary>
        /// Add all currently added devices that are not paired to any user to <paramref name="list"/>.
        /// </summary>
        /// <param name="list">List to add the devices to. Devices will be added to the end.</param>
        /// <returns>Number of devices added to <paramref name="list"/>.</returns>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static int GetUnpairedInputDevices(ref InputControlList<InputDevice> list)
        {
            var countBefore = list.Count;
            foreach (var device in InputSystem.devices)
            {
                // If it's in s_AllPairedDevices, there is *some* user that is using the device.
                // We don't care which one it is here.
                if (ArrayHelpers.ContainsReference(s_GlobalState.allPairedDevices, s_GlobalState.allPairedDeviceCount, device))
                    continue;

                list.Add(device);
            }

            return list.Count - countBefore;
        }

        /// <summary>
        /// Find the user (if any) that <paramref name="device"/> is currently paired to.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <returns>The user that <paramref name="device"/> is currently paired to or <c>null</c> if the device
        /// is not currently paired to an user.</returns>
        /// <remarks>
        /// Note that multiple users may be paired to the same device. If that is the case for <paramref name="device"/>,
        /// the method will return one of the users with no guarantee which one it is.
        ///
        /// To find all users paired to a device requires manually going through the list of users and their paired
        /// devices.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static InputUser? FindUserPairedToDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var userIndex = TryFindUserIndex(device);
            if (userIndex == -1)
                return null;

            return s_GlobalState.allUsers[userIndex];
        }

        public static InputUser? FindUserByAccount(InputUserAccountHandle platformUserAccountHandle)
        {
            if (platformUserAccountHandle == default(InputUserAccountHandle))
                throw new ArgumentException("Empty platform user account handle", nameof(platformUserAccountHandle));

            var userIndex = TryFindUserIndex(platformUserAccountHandle);
            if (userIndex == -1)
                return null;

            return s_GlobalState.allUsers[userIndex];
        }

        public static InputUser CreateUserWithoutPairedDevices()
        {
            var userIndex = AddUser();
            return s_GlobalState.allUsers[userIndex];
        }

        ////REVIEW: allow re-adding a user through this method?
        /// <summary>
        /// Pair the given device to a user.
        /// </summary>
        /// <param name="device">Device to pair to a user.</param>
        /// <param name="user">Optional parameter. If given, instead of creating a new user to pair the device
        /// to, the device is paired to the given user.</param>
        /// <param name="options">Optional set of options to modify pairing behavior.</param>
        /// <remarks>
        /// By default, a new user is created and <paramref name="device"/> is added <see cref="pairedDevices"/>
        /// of the user and <see cref="InputUserChange.DevicePaired"/> is sent on <see cref="onChange"/>.
        ///
        /// If a valid user is supplied to <paramref name="user"/>, the device is paired to the given user instead
        /// of creating a new user. By default, the device is added to the list of already paired devices for the user.
        /// This can be changed by using <see cref="InputUserPairingOptions.UnpairCurrentDevicesFromUser"/> which causes
        /// devices currently paired to the user to first be unpaired.
        ///
        /// The method will not prevent pairing of the same device to multiple users.
        ///
        /// Note that if the user has an associated set of actions (<see cref="actions"/>), the list of devices on the
        /// actions (<see cref="IInputActionCollection.devices"/>) will automatically be updated meaning that the newly
        /// paired devices will automatically reflect in the set of devices available to the user's actions. If the
        /// user has a control scheme that is currently activated (<see cref="controlScheme"/>), then <see cref="controlSchemeMatch"/>
        /// will also automatically update to reflect the matching of devices to the control scheme's device requirements.
        ///
        /// <example>
        /// <code>
        /// // Pair device to new user.
        /// var user = InputUser.PerformPairingWithDevice(wand1);
        ///
        /// // Pair another device to the same user.
        /// InputUser.PerformPairingWithDevice(wand2, user: user);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DevicePaired"/>
        public static InputUser PerformPairingWithDevice(InputDevice device,
            InputUser user = default,
            InputUserPairingOptions options = InputUserPairingOptions.None)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (user != default && !user.valid)
                throw new ArgumentException("Invalid user", nameof(user));

            // Create new user, if needed.
            int userIndex;
            if (user == default)
            {
                userIndex = AddUser();
            }
            else
            {
                // We have an existing user.
                userIndex = user.index;

                // See if we're supposed to clear out the user's currently paired devices first.
                if ((options & InputUserPairingOptions.UnpairCurrentDevicesFromUser) != 0)
                    user.UnpairDevices();

                // Ignore call if device is already paired to user.
                if (user.pairedDevices.ContainsReference(device))
                {
                    // Still might have to initiate user account selection.
                    if ((options & InputUserPairingOptions.ForcePlatformUserAccountSelection) != 0)
                        InitiateUserAccountSelection(userIndex, device, options);
                    return user;
                }
            }

            // Handle the user account side of pairing.
            var accountSelectionInProgress = InitiateUserAccountSelection(userIndex, device, options);

            // Except if we have initiate user account selection, pair the device to
            // to the user now.
            if (!accountSelectionInProgress)
                AddDeviceToUser(userIndex, device);

            return s_GlobalState.allUsers[userIndex];
        }

        private static bool InitiateUserAccountSelection(int userIndex, InputDevice device,
            InputUserPairingOptions options)
        {
            // See if there's a platform user account we can get from the device.
            // NOTE: We don't query the current user account if the caller has opted to force account selection.
            var queryUserAccountResult =
                (options & InputUserPairingOptions.ForcePlatformUserAccountSelection) == 0
                ? UpdatePlatformUserAccount(userIndex, device)
                : 0;

            ////REVIEW: what should we do if there already is an account selection in progress? InvalidOperationException?
            // If the device supports user account selection but we didn't get one,
            // try to initiate account selection.
            if ((options & InputUserPairingOptions.ForcePlatformUserAccountSelection) != 0 ||
                (queryUserAccountResult != InputDeviceCommand.GenericFailure &&
                 (queryUserAccountResult & (long)QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount) == 0 &&
                 (options & InputUserPairingOptions.ForceNoPlatformUserAccountSelection) == 0))
            {
                if (InitiateUserAccountSelectionAtPlatformLevel(device))
                {
                    s_GlobalState.allUserData[userIndex].flags |= UserFlags.UserAccountSelectionInProgress;
                    s_GlobalState.ongoingAccountSelections.Append(
                        new OngoingAccountSelection
                        {
                            device = device,
                            userId = s_GlobalState.allUsers[userIndex].id,
                        });

                    // Make sure we receive a notification for the configuration event.
                    HookIntoDeviceChange();

                    // Tell listeners that we started an account selection.
                    Notify(userIndex, InputUserChange.AccountSelectionInProgress, device);

                    return true;
                }
            }

            return false;
        }

        public bool Equals(InputUser other)
        {
            return m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputUser && Equals((InputUser)obj);
        }

        public override int GetHashCode()
        {
            return (int)m_Id;
        }

        public static bool operator==(InputUser left, InputUser right)
        {
            return left.m_Id == right.m_Id;
        }

        public static bool operator!=(InputUser left, InputUser right)
        {
            return left.m_Id != right.m_Id;
        }

        /// <summary>
        /// Add a new user.
        /// </summary>
        /// <returns>Index of the newly created user.</returns>
        /// <remarks>
        /// Adding a user sends a notification with <see cref="InputUserChange.Added"/> through <see cref="onChange"/>.
        ///
        /// The user will start out with no devices and no actions assigned.
        ///
        /// The user is added to <see cref="all"/>.
        /// </remarks>
        private static int AddUser()
        {
            var id = ++s_GlobalState.lastUserId;

            // Add to list.
            var userCount = s_GlobalState.allUserCount;
            ArrayHelpers.AppendWithCapacity(ref s_GlobalState.allUsers, ref userCount, new InputUser { m_Id = id });
            var userIndex = ArrayHelpers.AppendWithCapacity(ref s_GlobalState.allUserData, ref s_GlobalState.allUserCount, new UserData());

            // Send notification.
            Notify(userIndex, InputUserChange.Added, null);

            return userIndex;
        }

        /// <summary>
        /// Remove an active user.
        /// </summary>
        /// <param name="userIndex">Index of active user.</param>
        /// <remarks>
        /// Removing a user also unassigns all currently assigned devices from the user. On completion of this
        /// method, <see cref="pairedDevices"/> of <paramref name="user"/> will be empty.
        /// </remarks>
        private static void RemoveUser(int userIndex)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");
            Debug.Assert(s_GlobalState.allUserData[userIndex].deviceCount == 0, "User must not have paired devices still");

            // Reset data from control scheme.
            if (s_GlobalState.allUserData[userIndex].controlScheme != null)
            {
                if (s_GlobalState.allUserData[userIndex].actions != null)
                    s_GlobalState.allUserData[userIndex].actions.bindingMask = null;
            }
            s_GlobalState.allUserData[userIndex].controlSchemeMatch.Dispose();

            // Remove lost devices.
            RemoveLostDevicesForUser(userIndex);

            // Remove account selections that are in progress.
            for (var i = 0; i < s_GlobalState.ongoingAccountSelections.length; ++i)
            {
                if (s_GlobalState.ongoingAccountSelections[i].userId != s_GlobalState.allUsers[userIndex].id)
                    continue;

                s_GlobalState.ongoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                --i;
            }

            // Send notification (do before we actually remove the user).
            Notify(userIndex, InputUserChange.Removed, null);

            // Remove.
            var userCount = s_GlobalState.allUserCount;
            s_GlobalState.allUsers.EraseAtWithCapacity(ref userCount, userIndex);
            s_GlobalState.allUserData.EraseAtWithCapacity(ref s_GlobalState.allUserCount, userIndex);

            // Remove our hook if we no longer need it.
            if (s_GlobalState.allUserCount == 0)
            {
                UnhookFromDeviceChange();
                UnhookFromActionChange();
            }
        }

        private static void Notify(int userIndex, InputUserChange change, InputDevice device)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");

            if (s_GlobalState.onChange.length == 0)
                return;
            Profiler.BeginSample("InputUser.onChange");
            s_GlobalState.onChange.LockForChanges();
            for (var i = 0; i < s_GlobalState.onChange.length; ++i)
            {
                try
                {
                    s_GlobalState.onChange[i](s_GlobalState.allUsers[userIndex], change, device);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{exception.GetType().Name} while executing 'InputUser.onChange' callbacks");
                    Debug.LogException(exception);
                }
            }
            s_GlobalState.onChange.UnlockForChanges();
            Profiler.EndSample();
        }

        private static int TryFindUserIndex(uint userId)
        {
            Debug.Assert(userId != InvalidId, "User ID is invalid");

            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
            {
                if (s_GlobalState.allUsers[i].m_Id == userId)
                    return i;
            }
            return -1;
        }

        private static int TryFindUserIndex(InputUserAccountHandle platformHandle)
        {
            Debug.Assert(platformHandle != default, "User platform handle is invalid");

            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
            {
                if (s_GlobalState.allUserData[i].platformUserAccountHandle == platformHandle)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Find the user (if any) that is currently assigned the given <paramref name="device"/>.
        /// </summary>
        /// <param name="device">An input device that has been added to the system.</param>
        /// <returns>Index of the user that has <paramref name="device"/> among its <see cref="pairedDevices"/> or -1 if
        /// no user is currently assigned the given device.</returns>
        private static int TryFindUserIndex(InputDevice device)
        {
            Debug.Assert(device != null, "Device cannot be null");

            var indexOfDevice = s_GlobalState.allPairedDevices.IndexOfReference(device, s_GlobalState.allPairedDeviceCount);
            if (indexOfDevice == -1)
                return -1;

            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
            {
                var startIndex = s_GlobalState.allUserData[i].deviceStartIndex;
                if (startIndex <= indexOfDevice && indexOfDevice < startIndex + s_GlobalState.allUserData[i].deviceCount)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Add the given device to the user as either a lost device or a paired device.
        /// </summary>
        /// <param name="userIndex"></param>
        /// <param name="device"></param>
        /// <param name="asLostDevice"></param>
        private static void AddDeviceToUser(int userIndex, InputDevice device, bool asLostDevice = false, bool dontUpdateControlScheme = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");
            Debug.Assert(device != null, "Device cannot be null");
            if (asLostDevice)
                Debug.Assert(!s_GlobalState.allUsers[userIndex].lostDevices.ContainsReference(device), "Device already in set of lostDevices for user");
            else
                Debug.Assert(!s_GlobalState.allUsers[userIndex].pairedDevices.ContainsReference(device), "Device already in set of pairedDevices for user");

            var deviceCount = asLostDevice
                ? s_GlobalState.allUserData[userIndex].lostDeviceCount
                : s_GlobalState.allUserData[userIndex].deviceCount;
            var deviceStartIndex = asLostDevice
                ? s_GlobalState.allUserData[userIndex].lostDeviceStartIndex
                : s_GlobalState.allUserData[userIndex].deviceStartIndex;

            ++s_GlobalState.pairingStateVersion;

            // Move our devices to end of array.
            if (deviceCount > 0)
            {
                ArrayHelpers.MoveSlice(asLostDevice ? s_GlobalState.allLostDevices : s_GlobalState.allPairedDevices, deviceStartIndex,
                    asLostDevice ? s_GlobalState.allLostDeviceCount - deviceCount : s_GlobalState.allPairedDeviceCount - deviceCount,
                    deviceCount);

                // Adjust users that have been impacted by the change.
                for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                {
                    if (i == userIndex)
                        continue;

                    if ((asLostDevice ? s_GlobalState.allUserData[i].lostDeviceStartIndex : s_GlobalState.allUserData[i].deviceStartIndex) <= deviceStartIndex)
                        continue;

                    if (asLostDevice)
                        s_GlobalState.allUserData[i].lostDeviceStartIndex -= deviceCount;
                    else
                        s_GlobalState.allUserData[i].deviceStartIndex -= deviceCount;
                }
            }

            // Append to array.
            if (asLostDevice)
            {
                s_GlobalState.allUserData[userIndex].lostDeviceStartIndex = s_GlobalState.allLostDeviceCount - deviceCount;
                ArrayHelpers.AppendWithCapacity(ref s_GlobalState.allLostDevices, ref s_GlobalState.allLostDeviceCount, device);
                ++s_GlobalState.allUserData[userIndex].lostDeviceCount;
            }
            else
            {
                s_GlobalState.allUserData[userIndex].deviceStartIndex = s_GlobalState.allPairedDeviceCount - deviceCount;
                ArrayHelpers.AppendWithCapacity(ref s_GlobalState.allPairedDevices, ref s_GlobalState.allPairedDeviceCount, device);
                ++s_GlobalState.allUserData[userIndex].deviceCount;

                // If the user has actions, sync the devices on them with what we have now.
                var actions = s_GlobalState.allUserData[userIndex].actions;
                if (actions != null)
                {
                    actions.devices = s_GlobalState.allUsers[userIndex].pairedDevices;

                    // Also, if we have a control scheme, update the matching of device requirements
                    // against the device we now have.
                    if (!dontUpdateControlScheme && s_GlobalState.allUserData[userIndex].controlScheme != null)
                        UpdateControlSchemeMatch(userIndex);
                }
            }

            // Make sure we get OnDeviceChange notifications.
            HookIntoDeviceChange();

            // Let listeners know.
            Notify(userIndex, asLostDevice ? InputUserChange.DeviceLost : InputUserChange.DevicePaired, device);
        }

        private static void RemoveDeviceFromUser(int userIndex, InputDevice device, bool asLostDevice = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");
            Debug.Assert(device != null, "Device cannot be null");

            var deviceIndex = asLostDevice
                ? s_GlobalState.allLostDevices.IndexOfReference(device, s_GlobalState.allLostDeviceCount)
                : s_GlobalState.allPairedDevices.IndexOfReference(device, s_GlobalState.allUserData[userIndex].deviceStartIndex,
                s_GlobalState.allUserData[userIndex].deviceCount);
            if (deviceIndex == -1)
            {
                // Device not in list. Ignore.
                return;
            }

            if (asLostDevice)
            {
                s_GlobalState.allLostDevices.EraseAtWithCapacity(ref s_GlobalState.allLostDeviceCount, deviceIndex);
                --s_GlobalState.allUserData[userIndex].lostDeviceCount;
            }
            else
            {
                ++s_GlobalState.pairingStateVersion;
                s_GlobalState.allPairedDevices.EraseAtWithCapacity(ref s_GlobalState.allPairedDeviceCount, deviceIndex);
                --s_GlobalState.allUserData[userIndex].deviceCount;
            }

            // Adjust indices of other users.
            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
            {
                if ((asLostDevice ? s_GlobalState.allUserData[i].lostDeviceStartIndex : s_GlobalState.allUserData[i].deviceStartIndex) <= deviceIndex)
                    continue;

                if (asLostDevice)
                    --s_GlobalState.allUserData[i].lostDeviceStartIndex;
                else
                    --s_GlobalState.allUserData[i].deviceStartIndex;
            }

            if (!asLostDevice)
            {
                // Remove any ongoing account selections for the user on the given device.
                for (var i = 0; i < s_GlobalState.ongoingAccountSelections.length; ++i)
                {
                    if (s_GlobalState.ongoingAccountSelections[i].userId != s_GlobalState.allUsers[userIndex].id ||
                        s_GlobalState.ongoingAccountSelections[i].device != device)
                        continue;

                    s_GlobalState.ongoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                    --i;
                }

                // If the user has actions, sync the devices on them with what we have now.
                var actions = s_GlobalState.allUserData[userIndex].actions;
                if (actions != null)
                {
                    actions.devices = s_GlobalState.allUsers[userIndex].pairedDevices;

                    if (s_GlobalState.allUsers[userIndex].controlScheme != null)
                        UpdateControlSchemeMatch(userIndex);
                }

                // Notify listeners.
                Notify(userIndex, InputUserChange.DeviceUnpaired, device);
            }
        }

        private static void UpdateControlSchemeMatch(int userIndex, bool autoPairMissing = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");

            // Nothing to do if we don't have a control scheme.
            if (s_GlobalState.allUserData[userIndex].controlScheme == null)
                return;

            // Get rid of last match result and start new match.
            s_GlobalState.allUserData[userIndex].controlSchemeMatch.Dispose();
            var matchResult = new InputControlScheme.MatchResult();
            try
            {
                // Match the control scheme's requirements against the devices paired to the user.
                var scheme = s_GlobalState.allUserData[userIndex].controlScheme.Value;
                if (scheme.deviceRequirements.Count > 0)
                {
                    var availableDevices = new InputControlList<InputDevice>(Allocator.Temp);
                    try
                    {
                        // Add devices already paired to user.
                        availableDevices.AddSlice(s_GlobalState.allUsers[userIndex].pairedDevices);

                        // If we're supposed to grab whatever additional devices we need from what's
                        // available, add all unpaired devices to the list.
                        // NOTE: These devices go *after* the devices already paired (if any) meaning that
                        //       the control scheme matching will grab already paired devices *first*.
                        if (autoPairMissing)
                        {
                            var startIndex = availableDevices.Count;
                            var count = GetUnpairedInputDevices(ref availableDevices);

                            // We want to favor devices that are already assigned to the same platform user account.
                            // Sort the unpaired devices we've added to the list such that the ones belonging to the
                            // same user account come first.
                            if (s_GlobalState.allUserData[userIndex].platformUserAccountHandle != null)
                                availableDevices.Sort(startIndex, count,
                                    new CompareDevicesByUserAccount
                                    {
                                        platformUserAccountHandle = s_GlobalState.allUserData[userIndex].platformUserAccountHandle.Value
                                    });
                        }

                        matchResult = scheme.PickDevicesFrom(availableDevices);
                        if (matchResult.isSuccessfulMatch)
                        {
                            // Control scheme is satisfied with the devices we have available.
                            // If we may have grabbed as of yet unpaired devices, go and pair them to the user.
                            if (autoPairMissing)
                            {
                                // Update match result on user before potentially invoking callbacks.
                                s_GlobalState.allUserData[userIndex].controlSchemeMatch = matchResult;

                                foreach (var device in matchResult.devices)
                                {
                                    // Skip if already paired to user.
                                    if (s_GlobalState.allUsers[userIndex].pairedDevices.ContainsReference(device))
                                        continue;

                                    AddDeviceToUser(userIndex, device, dontUpdateControlScheme: true);
                                }
                            }
                        }
                    }
                    finally
                    {
                        availableDevices.Dispose();
                    }
                }

                s_GlobalState.allUserData[userIndex].controlSchemeMatch = matchResult;
            }
            catch (Exception)
            {
                // If we had an exception and are bailing out, make sure we aren't leaking native memory
                // we allocated.
                matchResult.Dispose();
                throw;
            }
        }

        private static long UpdatePlatformUserAccount(int userIndex, InputDevice device)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_GlobalState.allUserCount, "User index is invalid");

            // Fetch account details from backend.
            var queryResult = QueryPairedPlatformUserAccount(device, out var platformUserAccountHandle,
                out var platformUserAccountName, out var platformUserAccountId);

            // Nothing much to do if not supported by device.
            if (queryResult == InputDeviceCommand.GenericFailure)
            {
                // Check if there's an account selection in progress. There shouldn't be as it's
                // weird for the device to no signal it does not support querying user account, but
                // just to be safe, we check.
                if ((s_GlobalState.allUserData[userIndex].flags & UserFlags.UserAccountSelectionInProgress) != 0)
                    Notify(userIndex, InputUserChange.AccountSelectionCanceled, null);

                s_GlobalState.allUserData[userIndex].platformUserAccountHandle = null;
                s_GlobalState.allUserData[userIndex].platformUserAccountName = null;
                s_GlobalState.allUserData[userIndex].platformUserAccountId = null;

                return queryResult;
            }

            // Check if there's an account selection that we have initiated.
            if ((s_GlobalState.allUserData[userIndex].flags & UserFlags.UserAccountSelectionInProgress) != 0)
            {
                // Yes, there is. See if it is complete.

                if ((queryResult & (long)QueryPairedUserAccountCommand.Result.UserAccountSelectionInProgress) != 0)
                {
                    // No, still in progress.
                }
                else if ((queryResult & (long)QueryPairedUserAccountCommand.Result.UserAccountSelectionCanceled) != 0)
                {
                    // Got canceled.
                    Notify(userIndex, InputUserChange.AccountSelectionCanceled, device);
                }
                else
                {
                    // Yes, it is complete.
                    s_GlobalState.allUserData[userIndex].flags &= ~UserFlags.UserAccountSelectionInProgress;

                    s_GlobalState.allUserData[userIndex].platformUserAccountHandle = platformUserAccountHandle;
                    s_GlobalState.allUserData[userIndex].platformUserAccountName = platformUserAccountName;
                    s_GlobalState.allUserData[userIndex].platformUserAccountId = platformUserAccountId;

                    Notify(userIndex, InputUserChange.AccountSelectionComplete, device);
                }
            }
            // Check if user account details have changed.
            else if (s_GlobalState.allUserData[userIndex].platformUserAccountHandle != platformUserAccountHandle ||
                     s_GlobalState.allUserData[userIndex].platformUserAccountId != platformUserAccountId)
            {
                s_GlobalState.allUserData[userIndex].platformUserAccountHandle = platformUserAccountHandle;
                s_GlobalState.allUserData[userIndex].platformUserAccountName = platformUserAccountName;
                s_GlobalState.allUserData[userIndex].platformUserAccountId = platformUserAccountId;

                Notify(userIndex, InputUserChange.AccountChanged, device);
            }
            else if (s_GlobalState.allUserData[userIndex].platformUserAccountName != platformUserAccountName)
            {
                Notify(userIndex, InputUserChange.AccountNameChanged, device);
            }

            return queryResult;
        }

        ////TODO: bring documentation for these back when user management is implemented on Xbox and PS
        private static long QueryPairedPlatformUserAccount(InputDevice device,
            out InputUserAccountHandle? platformAccountHandle, out string platformAccountName, out string platformAccountId)
        {
            Debug.Assert(device != null, "Device cannot be null");

            // Query user account info from backend.
            var queryPairedUser = QueryPairedUserAccountCommand.Create();
            var result = device.ExecuteCommand(ref queryPairedUser);
            if (result == InputDeviceCommand.GenericFailure)
            {
                // Not currently paired to user account in backend.
                platformAccountHandle = null;
                platformAccountName = null;
                platformAccountId = null;
                return InputDeviceCommand.GenericFailure;
            }

            // Success. There is a user account currently paired to the device and we now have the
            // platform's user account details.

            if ((result & (long)QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount) != 0)
            {
                platformAccountHandle =
                    new InputUserAccountHandle(device.description.interfaceName ?? "<Unknown>", queryPairedUser.handle);
                platformAccountName = queryPairedUser.name;
                platformAccountId = queryPairedUser.id;
            }
            else
            {
                // The device supports QueryPairedUserAccountCommand but reports that the
                // device is not currently paired to a user.
                //
                // NOTE: On Switch, where the system itself does not store account<->pairing, we will always
                //       end up here until we've initiated an account selection through the backend itself.
                platformAccountHandle = null;
                platformAccountName = null;
                platformAccountId = null;
            }

            return result;
        }

        /// <summary>
        /// Try to initiate user account pairing for the given device at the platform level.
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True if the device accepted the request and an account picker has been raised.</returns>
        /// <remarks>
        /// Sends <see cref="InitiateUserAccountPairingCommand"/> to the device.
        /// </remarks>
        private static bool InitiateUserAccountSelectionAtPlatformLevel(InputDevice device)
        {
            Debug.Assert(device != null, "Device cannot be null");

            var initiateUserPairing = InitiateUserAccountPairingCommand.Create();
            var initiatePairingResult = device.ExecuteCommand(ref initiateUserPairing);
            if (initiatePairingResult == (long)InitiateUserAccountPairingCommand.Result.ErrorAlreadyInProgress)
                throw new InvalidOperationException("User pairing already in progress");

            return initiatePairingResult == (long)InitiateUserAccountPairingCommand.Result.SuccessfullyInitiated;
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
            {
                for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                {
                    ref var user = ref s_GlobalState.allUsers[i];
                    if (ReferenceEquals(user.actions, obj))
                        Notify(i, InputUserChange.ControlsChanged, null);
                }
            }
        }

        /// <summary>
        /// Invoked in response to <see cref="InputSystem.onDeviceChange"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="change"></param>
        /// <remarks>
        /// We monitor the device setup in the system for activity that impacts the user setup.
        /// </remarks>
        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                // Existing device removed. May mean a user has lost a device due to the battery running
                // out or the device being unplugged.
                // NOTE: We ignore Disconnected here. Removed is what gets sent whenever a device is taken off of
                //       InputSystem.devices -- which is what we're interested in here.
                case InputDeviceChange.Removed:
                {
                    // Could have been removed from multiple users. Repeatedly search in s_AllPairedDevices
                    // until we can't find the device anymore.
                    var deviceIndex = s_GlobalState.allPairedDevices.IndexOfReference(device, s_GlobalState.allPairedDeviceCount);
                    while (deviceIndex != -1)
                    {
                        // Find user. Must be there as we found the device in s_AllPairedDevices.
                        var userIndex = -1;
                        for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                        {
                            var deviceStartIndex = s_GlobalState.allUserData[i].deviceStartIndex;
                            if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_GlobalState.allUserData[i].deviceCount)
                            {
                                userIndex = i;
                                break;
                            }
                        }

                        // Add device to list of lost devices.
                        // NOTE: This will also send a DeviceLost notification.
                        // NOTE: Temporarily the device is on both lists.
                        AddDeviceToUser(userIndex, device, asLostDevice: true);

                        // Remove it from the user.
                        RemoveDeviceFromUser(userIndex, device);

                        // Search for another user paired to the same device.
                        deviceIndex = s_GlobalState.allPairedDevices.IndexOfReference(device, s_GlobalState.allPairedDeviceCount);
                    }
                    break;
                }

                // New device was added. See if it was a device we previously lost on a user.
                case InputDeviceChange.Added:
                {
                    // Search all lost devices. Could affect multiple users.
                    // Note that RemoveDeviceFromUser removes one element, hence no advancement of deviceIndex.
                    for (var deviceIndex = FindLostDevice(device); deviceIndex != -1;
                         deviceIndex = FindLostDevice(device, deviceIndex))
                    {
                        // Find user. Must be there as we found the device in s_AllLostDevices.
                        var userIndex = -1;
                        for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                        {
                            var deviceStartIndex = s_GlobalState.allUserData[i].lostDeviceStartIndex;
                            if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_GlobalState.allUserData[i].lostDeviceCount)
                            {
                                userIndex = i;
                                break;
                            }
                        }

                        // Remove from list of lost devices. No notification. Notice that we need to use device
                        // from lost device list even if its another instance.
                        RemoveDeviceFromUser(userIndex, s_GlobalState.allLostDevices[deviceIndex], asLostDevice: true);

                        // Notify.
                        Notify(userIndex, InputUserChange.DeviceRegained, device);

                        // Add back as normally paired device.
                        AddDeviceToUser(userIndex, device);
                    }
                    break;
                }

                // Device had its configuration changed which may mean we have a different user account paired
                // to the device now.
                case InputDeviceChange.ConfigurationChanged:
                {
                    // See if the this is a device that we were waiting for an account selection on. If so, pair
                    // it to the user that was waiting.
                    var wasOngoingAccountSelection = false;
                    for (var i = 0; i < s_GlobalState.ongoingAccountSelections.length; ++i)
                    {
                        if (s_GlobalState.ongoingAccountSelections[i].device != device)
                            continue;

                        var userIndex = new InputUser { m_Id = s_GlobalState.ongoingAccountSelections[i].userId }.index;
                        var queryResult = UpdatePlatformUserAccount(userIndex, device);
                        if ((queryResult & (long)QueryPairedUserAccountCommand.Result.UserAccountSelectionInProgress) == 0)
                        {
                            wasOngoingAccountSelection = true;
                            s_GlobalState.ongoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                            --i;

                            // If the device wasn't paired to the user, pair it now.
                            if (!s_GlobalState.allUsers[userIndex].pairedDevices.ContainsReference(device))
                                AddDeviceToUser(userIndex, device);
                        }
                    }

                    // If it wasn't a configuration change event from an account selection, go and check whether
                    // there was a user account change that happened outside the application.
                    if (!wasOngoingAccountSelection)
                    {
                        // Could be paired to multiple users. Repeatedly search in s_AllPairedDevices
                        // until we can't find the device anymore.
                        var deviceIndex = s_GlobalState.allPairedDevices.IndexOfReference(device, s_GlobalState.allPairedDeviceCount);
                        while (deviceIndex != -1)
                        {
                            // Find user. Must be there as we found the device in s_AllPairedDevices.
                            var userIndex = -1;
                            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                            {
                                var deviceStartIndex = s_GlobalState.allUserData[i].deviceStartIndex;
                                if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_GlobalState.allUserData[i].deviceCount)
                                {
                                    userIndex = i;
                                    break;
                                }
                            }

                            // Check user account.
                            UpdatePlatformUserAccount(userIndex, device);

                            // Search for another user paired to the same device.
                            // Note that action is tied to user and hence we can skip to end of slice associated
                            // with the current user or at least one element forward.
                            var offsetNextSlice = deviceIndex + Math.Max(1, s_GlobalState.allUserData[userIndex].deviceCount);
                            deviceIndex = s_GlobalState.allPairedDevices.IndexOfReference(device, offsetNextSlice, s_GlobalState.allPairedDeviceCount - offsetNextSlice);
                        }
                    }
                    break;
                }
            }
        }

        private static int FindLostDevice(InputDevice device, int startIndex = 0)
        {
            // Compare both by device ID and by reference. We may be looking at a device that was recreated
            // due to layout changes (new InputDevice instance, same ID) or a device that was reconnected
            // and thus fetched out of `disconnectedDevices` (same InputDevice instance, new ID).

            var newDeviceId = device.deviceId;
            for (var i = startIndex; i < s_GlobalState.allLostDeviceCount; ++i)
            {
                var lostDevice = s_GlobalState.allLostDevices[i];
                if (device == lostDevice || lostDevice.deviceId == newDeviceId) return i;
            }

            return -1;
        }

        // We hook this into InputSystem.onEvent when listening for activity on unpaired devices.
        // What this means is that we get to run *before* state reaches the device. This in turn
        // means that should the device get paired as a result, actions that are enabled as part
        // of the pairing will immediately get triggered. This would not be the case if we hook
        // into InputState.onDeviceChange instead which only triggers once state has been altered.
        //
        // NOTE: This also means that unpaired device activity will *only* be detected from events,
        //       NOT from state changes applied directly through InputState.Change.
        private static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            Debug.Assert(s_GlobalState.listenForUnpairedDeviceActivity != 0,
                "This should only be called while listening for unpaired device activity");
            if (s_GlobalState.listenForUnpairedDeviceActivity == 0)
                return;

            // Ignore input in editor.
#if UNITY_EDITOR
            if (InputState.currentUpdateType == InputUpdateType.Editor)
                return;
#endif

            // Ignore any state change not triggered from a state event.
            var eventType = eventPtr.type;
            if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                return;

            // Ignore event if device is disabled.
            if (!device.enabled)
                return;

            // See if it's a device not belonging to any user.
            if (ArrayHelpers.ContainsReference(s_GlobalState.allPairedDevices, s_GlobalState.allPairedDeviceCount, device))
            {
                // No, it's a device already paired to a player so do nothing.
                return;
            }

            Profiler.BeginSample("InputCheckForUnpairedDeviceActivity");

            // Apply the pre-filter. If there's callbacks and none of them return true,
            // we early out and ignore the event entirely.
            if (!DelegateHelpers.InvokeCallbacksSafe_AnyCallbackReturnsTrue(
                ref s_GlobalState.onPreFilterUnpairedDeviceUsed, device, eventPtr, "InputUser.onPreFilterUnpairedDeviceActivity"))
            {
                Profiler.EndSample();
                return;
            }

            // Go through the changed controls in the event and look for ones actuated
            // above a magnitude of a little above zero.
            foreach (var control in eventPtr.EnumerateChangedControls(device: device, magnitudeThreshold: 0.0001f))
            {
                var deviceHasBeenPaired = false;
                s_GlobalState.onUnpairedDeviceUsed.LockForChanges();
                for (var n = 0; n < s_GlobalState.onUnpairedDeviceUsed.length; ++n)
                {
                    var pairingStateVersionBefore = s_GlobalState.pairingStateVersion;

                    try
                    {
                        s_GlobalState.onUnpairedDeviceUsed[n](control, eventPtr);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"{exception.GetType().Name} while executing 'InputUser.onUnpairedDeviceUsed' callbacks");
                        Debug.LogException(exception);
                    }

                    if (pairingStateVersionBefore != s_GlobalState.pairingStateVersion
                        && FindUserPairedToDevice(device) != null)
                    {
                        deviceHasBeenPaired = true;
                        break;
                    }
                }
                s_GlobalState.onUnpairedDeviceUsed.UnlockForChanges();

                // If the device was paired in one of the callbacks, stop processing
                // changes on it.
                if (deviceHasBeenPaired)
                    break;
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Syntax for configuring a control scheme on a user.
        /// </summary>
        public struct ControlSchemeChangeSyntax
        {
            /// <summary>
            /// Leave the user's paired devices in place but pair any available devices
            /// that are still required by the control scheme.
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// If there are unpaired devices that, at the platform level, are associated with the same
            /// user account, those will take precedence over other unpaired devices.
            /// </remarks>
            public ControlSchemeChangeSyntax AndPairRemainingDevices()
            {
                UpdateControlSchemeMatch(m_UserIndex, autoPairMissing: true);
                return this;
            }

            internal int m_UserIndex;
        }

        private uint m_Id;

        [Flags]
        internal enum UserFlags
        {
            BindToAllDevices = 1 << 0,

            /// <summary>
            /// Whether we have initiated a user account selection.
            /// </summary>
            UserAccountSelectionInProgress = 1 << 1,
        }

        /// <summary>
        /// Data we store for each user.
        /// </summary>
        private struct UserData
        {
            /// <summary>
            /// The platform handle associated with the user.
            /// </summary>
            /// <remarks>
            /// If set, this identifies the user on the platform. It also means that the devices
            /// assigned to the user may be paired at the platform level.
            /// </remarks>
            public InputUserAccountHandle? platformUserAccountHandle;

            /// <summary>
            /// Plain-text user name as returned by the underlying platform. Null if not associated with user on platform.
            /// </summary>
            public string platformUserAccountName;

            /// <summary>
            /// Platform-specific ID that identifies the user across sessions even if the user
            /// name changes.
            /// </summary>
            /// <remarks>
            /// This might not be a human-readable string.
            /// </remarks>
            public string platformUserAccountId;

            /// <summary>
            /// Number of devices in <see cref="InputUser.s_AllPairedDevices"/> assigned to the user.
            /// </summary>
            public int deviceCount;

            /// <summary>
            /// Index in <see cref="InputUser.s_AllPairedDevices"/> where the devices for this user start. Only valid
            /// if <see cref="deviceCount"/> is greater than zero.
            /// </summary>
            public int deviceStartIndex;

            /// <summary>
            /// Input actions associated with the user.
            /// </summary>
            public IInputActionCollection actions;

            /// <summary>
            /// Currently active control scheme or null if no control scheme has been set on the user.
            /// </summary>
            /// <remarks>
            /// This also dictates the binding mask that we're using with <see cref="actions"/>.
            /// </remarks>
            public InputControlScheme? controlScheme;

            public InputControlScheme.MatchResult controlSchemeMatch;

            /// <summary>
            /// Number of devices in <see cref="InputUser.s_AllLostDevices"/> assigned to the user.
            /// </summary>
            public int lostDeviceCount;

            /// <summary>
            /// Index in <see cref="InputUser.s_AllLostDevices"/> where the lost devices for this user start. Only valid
            /// if <see cref="lostDeviceCount"/> is greater than zero.
            /// </summary>
            public int lostDeviceStartIndex;

            ////TODO
            //public InputUserSettings settings;

            public UserFlags flags;
        }

        /// <summary>
        /// Compare two devices for being associated with a specific platform user account.
        /// </summary>
        private struct CompareDevicesByUserAccount : IComparer<InputDevice>
        {
            public InputUserAccountHandle platformUserAccountHandle;

            public int Compare(InputDevice x, InputDevice y)
            {
                var firstAccountHandle = GetUserAccountHandleForDevice(x);
                var secondAccountHandle = GetUserAccountHandleForDevice(x);

                if (firstAccountHandle == platformUserAccountHandle &&
                    secondAccountHandle == platformUserAccountHandle)
                    return 0;

                if (firstAccountHandle == platformUserAccountHandle)
                    return -1;

                if (secondAccountHandle == platformUserAccountHandle)
                    return 1;

                return 0;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "device", Justification = "Keep this for future implementation")]
            private static InputUserAccountHandle? GetUserAccountHandleForDevice(InputDevice device)
            {
                ////TODO (need to cache this)
                return null;
            }
        }

        private struct OngoingAccountSelection
        {
            public InputDevice device;
            public uint userId;
        }

        private struct GlobalState
        {
            internal int pairingStateVersion;
            internal uint lastUserId;
            internal int allUserCount;
            internal int allPairedDeviceCount;
            internal int allLostDeviceCount;
            internal InputUser[] allUsers;
            internal UserData[] allUserData;
            internal InputDevice[] allPairedDevices; // We keep a single array that we slice out to each user.
            internal InputDevice[] allLostDevices;   // We keep a single array that we slice out to each user.
            internal InlinedArray<OngoingAccountSelection> ongoingAccountSelections;
            internal CallbackArray<Action<InputUser, InputUserChange, InputDevice>> onChange;
            internal CallbackArray<Action<InputControl, InputEventPtr>> onUnpairedDeviceUsed;
            internal CallbackArray<Func<InputDevice, InputEventPtr, bool>> onPreFilterUnpairedDeviceUsed;
            internal Action<object, InputActionChange> actionChangeDelegate;
            internal Action<InputDevice, InputDeviceChange> onDeviceChangeDelegate;
            internal Action<InputEventPtr, InputDevice> onEventDelegate;
            internal bool onActionChangeHooked;
            internal bool onDeviceChangeHooked;
            internal bool onEventHooked;
            internal int listenForUnpairedDeviceActivity;
        }

        private static GlobalState s_GlobalState;

        internal static ISavedState SaveAndResetState()
        {
            // Save current state and provide an opaque interface to restore it
            var savedState = new SavedStructState<GlobalState>(
                ref s_GlobalState,
                (ref GlobalState state) => s_GlobalState = state, // restore
                () => DisposeAndResetGlobalState()); // static dispose

            // Reset global state
            s_GlobalState = default;

            return savedState;
        }

        private static void HookIntoActionChange()
        {
            if (s_GlobalState.onActionChangeHooked)
                return;
            if (s_GlobalState.actionChangeDelegate == null)
                s_GlobalState.actionChangeDelegate = OnActionChange;
            InputSystem.onActionChange += OnActionChange;
            s_GlobalState.onActionChangeHooked = true;
        }

        private static void UnhookFromActionChange()
        {
            if (!s_GlobalState.onActionChangeHooked)
                return;
            InputSystem.onActionChange -= OnActionChange;
            s_GlobalState.onActionChangeHooked = false;
        }

        private static void HookIntoDeviceChange()
        {
            if (s_GlobalState.onDeviceChangeHooked)
                return;
            if (s_GlobalState.onDeviceChangeDelegate == null)
                s_GlobalState.onDeviceChangeDelegate = OnDeviceChange;
            InputSystem.onDeviceChange += s_GlobalState.onDeviceChangeDelegate;
            s_GlobalState.onDeviceChangeHooked = true;
        }

        private static void UnhookFromDeviceChange()
        {
            if (!s_GlobalState.onDeviceChangeHooked)
                return;
            InputSystem.onDeviceChange -= s_GlobalState.onDeviceChangeDelegate;
            s_GlobalState.onDeviceChangeHooked = false;
        }

        private static void HookIntoEvents()
        {
            if (s_GlobalState.onEventHooked)
                return;
            if (s_GlobalState.onEventDelegate == null)
                s_GlobalState.onEventDelegate = OnEvent;
            InputSystem.onEvent += s_GlobalState.onEventDelegate;
            s_GlobalState.onEventHooked = true;
        }

        private static void UnhookFromDeviceStateChange()
        {
            if (!s_GlobalState.onEventHooked)
                return;
            InputSystem.onEvent -= s_GlobalState.onEventDelegate;
            s_GlobalState.onEventHooked = false;
        }

        private static void DisposeAndResetGlobalState()
        {
            // Release native memory held by control scheme match results.
            for (var i = 0; i < s_GlobalState.allUserCount; ++i)
                s_GlobalState.allUserData[i].controlSchemeMatch.Dispose();

            // Don't reset s_LastUserId and just let it increment instead so we never generate
            // the same ID twice.

            var storedLastUserId = s_GlobalState.lastUserId;
            s_GlobalState = default;
            s_GlobalState.lastUserId = storedLastUserId;
        }

        internal static void ResetGlobals()
        {
            UnhookFromActionChange();
            UnhookFromDeviceChange();
            UnhookFromDeviceStateChange();

            DisposeAndResetGlobalState();
        }
    }
}
