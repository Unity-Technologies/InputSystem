using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Methods to change the setup of <see cref="InputAction"/>, <see cref="InputActionMap"/>,
    /// and <see cref="InputActionAsset"/> objects.
    /// </summary>
    /// <remarks>
    /// Unlike the methods in <see cref="InputActionRebindingExtensions"/>, the methods here are
    /// generally destructive, i.e. they will rearrange the data for actions.
    /// </remarks>
    public static class InputActionSetupExtensions
    {
        /// <summary>
        /// Create an action map with the given name and add it to the asset.
        /// </summary>
        /// <param name="asset">Asset to add the action map to</param>
        /// <param name="name">Name to assign to the </param>
        /// <returns>The newly added action map.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> or
        /// <exception cref="InvalidOperationException">An action map with the given <paramref name="name"/>
        /// already exists in <paramref name="asset"/>.</exception>
        /// <paramref name="name"/> is <c>null</c> or empty.</exception>
        public static InputActionMap AddActionMap(this InputActionAsset asset, string name)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (asset.FindActionMap(name) != null)
                throw new InvalidOperationException(
                    $"An action map called '{name}' already exists in the asset");

            var map = new InputActionMap(name);
            map.GenerateId();
            asset.AddActionMap(map);
            return map;
        }

        /// <summary>
        /// Add an action map to the asset.
        /// </summary>
        /// <param name="asset">Asset to add the map to.</param>
        /// <param name="map">A named action map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="asset"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> has no name or asset already contains a
        /// map with the same name.</exception>
        /// <seealso cref="InputActionAsset.actionMaps"/>
        public static void AddActionMap(this InputActionAsset asset, InputActionMap map)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(map.name))
                throw new InvalidOperationException("Maps added to an input action asset must be named");
            if (map.asset != null)
                throw new InvalidOperationException(
                    $"Cannot add map '{map}' to asset '{asset}' as it has already been added to asset '{map.asset}'");
            ////REVIEW: some of the rules here seem stupid; just replace?
            if (asset.FindActionMap(map.name) != null)
                throw new InvalidOperationException(
                    $"An action map called '{map.name}' already exists in the asset");

            ArrayHelpers.Append(ref asset.m_ActionMaps, map);
            map.m_Asset = asset;
        }

        /// <summary>
        /// Remove the given action map from the asset.
        /// </summary>
        /// <param name="asset">Asset to add the action map to.</param>
        /// <param name="map">An action map. If the given map is not part of the asset, the method
        /// does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> or <paramref name="map"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> is currently enabled (see <see
        /// cref="InputActionMap.enabled"/>).</exception>
        /// <seealso cref="RemoveActionMap(InputActionAsset,string)"/>
        /// <seealso cref="InputActionAsset.actionMaps"/>
        public static void RemoveActionMap(this InputActionAsset asset, InputActionMap map)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (map.enabled)
                throw new InvalidOperationException("Cannot remove an action map from the asset while it is enabled");

            // Ignore if not part of this asset.
            if (map.m_Asset != asset)
                return;

            ArrayHelpers.Erase(ref asset.m_ActionMaps, map);
            map.m_Asset = null;
        }

        /// <summary>
        /// Remove the action map with the given name or ID from the asset.
        /// </summary>
        /// <param name="asset">Asset to remove the action map from.</param>
        /// <param name="nameOrId">The name or ID (see <see cref="InputActionMap.id"/>) of a map in the
        /// asset. Note that lookup is case-insensitive. If no map with the given name or ID is found,
        /// the method does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> or <paramref name="nameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The map referenced by <paramref name="nameOrId"/> is currently enabled
        /// (see <see cref="InputActionMap.enabled"/>).</exception>
        /// <seealso cref="RemoveActionMap(InputActionAsset,string)"/>
        /// <seealso cref="InputActionAsset.actionMaps"/>
        public static void RemoveActionMap(this InputActionAsset asset, string nameOrId)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (nameOrId == null)
                throw new ArgumentNullException(nameof(nameOrId));
            var map = asset.FindActionMap(nameOrId);
            if (map != null)
                asset.RemoveActionMap(map);
        }

        ////TODO: add method to add an existing InputAction to a map

        public static InputAction AddAction(this InputActionMap map, string name, InputActionType type = default, string binding = null,
            string interactions = null, string processors = null, string groups = null, string expectedControlLayout = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", nameof(name));
            if (map.enabled)
                throw new InvalidOperationException(
                    $"Cannot add action '{name}' to map '{map}' while it the map is enabled");
            if (map.FindAction(name) != null)
                throw new InvalidOperationException(
                    $"Cannot add action with duplicate name '{name}' to set '{map.name}'");

            // Append action to array.
            var action = new InputAction(name, type)
            {
                expectedControlType = expectedControlLayout
            };
            action.GenerateId();
            ArrayHelpers.Append(ref map.m_Actions, action);
            action.m_ActionMap = map;

            ////TODO: make sure we blast out existing action map state

            // Add binding, if supplied.
            if (!string.IsNullOrEmpty(binding))
            {
                action.AddBinding(binding, interactions: interactions, processors: processors, groups: groups);
            }
            else
            {
                if (!string.IsNullOrEmpty(groups))
                    throw new ArgumentException(
                        $"No binding path was specified for action '{action}' but groups was specified ('{groups}'); cannot apply groups without binding",
                        nameof(groups));

                // If no binding has been supplied but there are interactions and processors, they go on the action itself.
                action.m_Interactions = interactions;
                action.m_Processors = processors;
            }

            return action;
        }

        /// <summary>
        /// Remove the given action from its <see cref="InputActionMap"/>.
        /// </summary>
        /// <param name="action">An input action that is part of an <see cref="InputActionMap"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="action"/> is part of an <see cref="InputActionMap"/>
        /// that has at least one enabled action -or- <paramref name="action"/> is a standalone action
        /// that is not part of an <see cref="InputActionMap"/> and thus cannot be removed from anything.</exception>
        /// <remarks>
        /// After removal, the action's <see cref="InputAction.actionMap"/> will be set to <c>null</c>
        /// and the action will effectively become a standalone action that is not associated with
        /// any action map. Bindings on the action will be preserved. On the action map, the bindings
        /// for the action will be removed.
        /// </remarks>
        /// <seealso cref="AddAction"/>
        public static void RemoveAction(this InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var actionMap = action.actionMap;
            if (actionMap == null)
                throw new ArgumentException(
                    $"Action '{action}' does not belong to an action map; nowhere to remove from", nameof(action));
            if (actionMap.enabled)
                throw new ArgumentException($"Cannot remove action '{action}' while its action map is enabled");

            var bindingsForAction = action.bindings.ToArray();

            var index = ArrayHelpers.IndexOfReference(actionMap.m_Actions, action);
            Debug.Assert(index != -1, "Could not find action in map");
            ArrayHelpers.EraseAt(ref actionMap.m_Actions, index);

            action.m_ActionMap = null;
            action.m_SingletonActionBindings = bindingsForAction;

            actionMap.ClearPerActionCachedBindingData();

            // Remove bindings to action from map.
            var newActionMapBindingCount = actionMap.m_Bindings.Length - bindingsForAction.Length;
            if (newActionMapBindingCount == 0)
                actionMap.m_Bindings = null;
            else
            {
                var newActionMapBindings = new InputBinding[newActionMapBindingCount];
                var oldActionMapBindings = actionMap.m_Bindings;
                var bindingIndex = 0;
                for (var i = 0; i < oldActionMapBindings.Length; ++i)
                {
                    var binding = oldActionMapBindings[i];
                    if (bindingsForAction.IndexOf(b => b == binding) == -1)
                        newActionMapBindings[bindingIndex++] = binding;
                }
                actionMap.m_Bindings = newActionMapBindings;
            }
        }

        /// <summary>
        /// Remove the action with the given name from the asset.
        /// </summary>
        /// <param name="asset">Asset to remove the action from.</param>
        /// <param name="nameOrId">Name or ID of the action. See <see cref="InputActionAsset.FindAction(string,bool)"/> for
        /// details.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> -or- <paramref name="nameOrId"/>
        /// is <c>null</c> or empty.</exception>
        /// <seealso cref="RemoveAction(InputAction)"/>
        public static void RemoveAction(this InputActionAsset asset, string nameOrId)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (nameOrId == null)
                throw new ArgumentNullException(nameof(nameOrId));
            var action = asset.FindAction(nameOrId);
            action?.RemoveAction();
        }

        /// <summary>
        /// Add a new binding to the given action.
        /// </summary>
        /// <param name="action">Action to add the binding to. If the action is part of an <see cref="InputActionMap"/>,
        /// the newly added binding will be visible on <see cref="InputActionMap.bindings"/>.</param>
        /// <param name="path">Binding path string. See <see cref="InputBinding.path"/> for details.</param>
        /// <param name="interactions">Optional list of interactions to apply to the binding. See <see
        /// cref="InputBinding.interactions"/> for details.</param>
        /// <param name="processors">Optional list of processors to apply to the binding. See <see
        /// cref="InputBinding.processors"/> for details.</param>
        /// <param name="groups">Optional list of binding groups that should be assigned to the binding. See
        /// <see cref="InputBinding.groups"/> for details.</param>
        /// <returns>Fluent-style syntax to further configure the binding.</returns>
        public static BindingSyntax AddBinding(this InputAction action, string path, string interactions = null,
            string processors = null, string groups = null)
        {
            return AddBinding(action, new InputBinding
            {
                path = path,
                interactions = interactions,
                processors = processors,
                groups = groups
            });
        }

        /// <summary>
        /// Add a binding that references the given <paramref name="control"/> and triggers
        /// the given <seealso cref="action"/>.
        /// </summary>
        /// <param name="action">Action to trigger.</param>
        /// <param name="control">Control to bind to. The full <see cref="InputControl.path"/> of the control will
        /// be used in the resulting <see cref="InputBinding">binding</see>.</param>
        /// <returns>Syntax to configure the binding further.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null or <paramref name="control"/> is null.</exception>
        /// <seealso cref="InputAction.bindings"/>
        public static BindingSyntax AddBinding(this InputAction action, InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            return AddBinding(action, control.path);
        }

        /// <summary>
        /// Add a new binding to the action.
        /// </summary>
        /// <param name="action">A disabled action to add the binding to.</param>
        /// <param name="binding"></param>
        /// <returns>
        /// Returns a fluent-style syntax structure that allows performing additional modifications
        /// based on the new binding.
        /// </returns>
        /// <exception cref="InvalidOperationException"><paramref name="action"/> is enabled or is part
        /// of an <see cref="InputActionMap"/> that is enabled.</exception>
        /// <remarks>
        /// This works both with actions that are part of an action set as well as with actions that aren't.
        ///
        /// Note that actions must be disabled while altering their binding sets. Also, if the action belongs
        /// to a set, all actions in the set must be disabled.
        ///
        /// <example>
        /// <code>
        /// fireAction.AddBinding()
        ///     .WithPath("&lt;Gamepad&gt;/buttonSouth")
        ///     .WithGroup("Gamepad");
        /// </code>
        /// </example>
        /// </remarks>
        public static BindingSyntax AddBinding(this InputAction action, InputBinding binding = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (binding.path == null)
                throw new ArgumentException("Binding path cannot be null", nameof(binding));

            ////REVIEW: should this reference actions by ID?
            Debug.Assert(action.m_Name != null || action.isSingletonAction);
            binding.action = action.name;

            var actionMap = action.GetOrCreateActionMap();
            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, action, bindingIndex);
        }

        public static BindingSyntax AddBinding(this InputActionMap actionMap, string path,
            string interactions = null, string groups = null, string action = null)
        {
            if (path == null)
                throw new ArgumentException("Binding path cannot be null", nameof(path));

            return AddBinding(actionMap, new InputBinding
            {
                path = path,
                interactions = interactions,
                groups = groups,
                action = action
            });
        }

        public static BindingSyntax AddBinding(this InputActionMap actionMap, string path, InputAction action,
            string interactions = null, string groups = null)
        {
            if (action != null && action.actionMap != actionMap)
                throw new ArgumentException(
                    $"Action '{action}' is not part of action map '{actionMap}'", nameof(action));

            if (action == null)
                return AddBinding(actionMap, path: path, interactions: interactions, groups: groups);

            return AddBinding(actionMap, path: path, interactions: interactions, groups: groups,
                action: action.id);
        }

        public static BindingSyntax AddBinding(this InputActionMap actionMap, string path, Guid action,
            string interactions = null, string groups = null)
        {
            if (action == Guid.Empty)
                return AddBinding(actionMap, path: path, interactions: interactions, groups: groups);
            return AddBinding(actionMap, path: path, interactions: interactions, groups: groups,
                action: action.ToString());
        }

        public static BindingSyntax AddBinding(this InputActionMap actionMap, InputBinding binding)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (binding.path == null)
                throw new ArgumentException("Binding path cannot be null", nameof(binding));

            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, null, bindingIndex);
        }

        public static CompositeSyntax AddCompositeBinding(this InputAction action, string composite, string interactions = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentException("Composite name cannot be null or empty", nameof(composite));

            var actionMap = action.GetOrCreateActionMap();

            ////REVIEW: use 'name' instead of 'path' field here?
            var binding = new InputBinding {path = composite, interactions = interactions, isComposite = true, action = action.name};
            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new CompositeSyntax(actionMap, action, bindingIndex);
        }

        private static int AddBindingInternal(InputActionMap map, InputBinding binding)
        {
            Debug.Assert(map != null);

            // Make sure the binding has an ID.
            if (string.IsNullOrEmpty(binding.m_Id))
                binding.GenerateId();

            // Append to bindings in set.
            var bindingIndex = ArrayHelpers.Append(ref map.m_Bindings, binding);

            // Invalidate per-action binding sets so that this gets refreshed if
            // anyone queries it.
            map.ClearPerActionCachedBindingData();

            // If we're looking at a singleton action, make sure m_Bindings is up to date just
            // in case the action gets serialized.
            if (map.m_SingletonAction != null)
                map.m_SingletonAction.m_SingletonActionBindings = map.m_Bindings;

            return bindingIndex;
        }

        public static BindingSyntax ChangeBinding(this InputAction action, int index)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var indexOnMap = action.BindingIndexOnActionToBindingIndexOnMap(index);
            return new BindingSyntax(action.GetOrCreateActionMap(), action, indexOnMap);
        }

        public static BindingSyntax ChangeBindingWithId(this InputAction action, string id)
        {
            return action.ChangeBinding(new InputBinding {m_Id = id});
        }

        public static BindingSyntax ChangeBindingWithId(this InputAction action, Guid id)
        {
            return action.ChangeBinding(new InputBinding {id = id});
        }

        public static BindingSyntax ChangeBindingWithGroup(this InputAction action, string group)
        {
            return action.ChangeBinding(new InputBinding {groups = group});
        }

        public static BindingSyntax ChangeBindingWithPath(this InputAction action, string path)
        {
            return action.ChangeBinding(new InputBinding {path = path});
        }

        public static BindingSyntax ChangeBinding(this InputAction action, InputBinding match)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var actionMap = action.GetOrCreateActionMap();
            var bindingIndex = actionMap.FindBinding(match);
            if (bindingIndex == -1)
                throw new ArgumentException($"Cannot find binding matching '{match}' in '{action}'", nameof(match));

            return new BindingSyntax(actionMap, action, bindingIndex);
        }

        ////TODO: update binding mask if necessary
        /// <summary>
        /// Rename an existing action.
        /// </summary>
        /// <param name="action">Action to assign a new name to. Can be singleton action or action that
        /// is part of a map.</param>
        /// <param name="newName">New name to assign to action. Cannot be empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null or <paramref name="newName"/> is
        /// null or empty.</exception>
        /// <exception cref="InvalidOperationException"><see cref="InputAction.actionMap"/> of <paramref name="action"/>
        /// already contains an action called <paramref name="newName"/>.</exception>
        /// <remarks>
        /// Renaming an action will also update the bindings that refer to the action.
        /// </remarks>
        public static void Rename(this InputAction action, string newName)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));

            if (action.name == newName)
                return;

            // Make sure name isn't already taken in map.
            var actionMap = action.actionMap;
            if (actionMap?.FindAction(newName) != null)
                throw new InvalidOperationException(
                    $"Cannot rename '{action}' to '{newName}' in map '{actionMap}' as the map already contains an action with that name");

            var oldName = action.m_Name;
            action.m_Name = newName;

            // Update bindings.
            var bindings = action.GetOrCreateActionMap().m_Bindings;
            var bindingCount = bindings.LengthSafe();
            for (var i = 0; i < bindingCount; ++i)
                if (string.Compare(bindings[i].action, oldName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    bindings[i].action = newName;
        }

        /// <summary>
        /// Add a new control scheme to the asset.
        /// </summary>
        /// <param name="asset">Asset to add the control scheme to.</param>
        /// <param name="controlScheme">Control scheme to add.</param>
        /// <exception cref="ArgumentException"><paramref name="controlScheme"/> has no name.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">A control scheme with the same name as <paramref name="controlScheme"/>
        /// already exists in the asset.</exception>
        /// <remarks>
        /// </remarks>
        public static void AddControlScheme(this InputActionAsset asset, InputControlScheme controlScheme)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(controlScheme.name))
                throw new ArgumentException("Cannot add control scheme without name to asset " + asset.name, nameof(controlScheme));
            if (asset.FindControlScheme(controlScheme.name) != null)
                throw new InvalidOperationException(
                    $"Asset '{asset.name}' already contains a control scheme called '{controlScheme.name}'");

            ArrayHelpers.Append(ref asset.m_ControlSchemes, controlScheme);
        }

        /// <summary>
        /// Add a new control scheme to the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">Asset to add the control scheme to.</param>
        /// <param name="name">Name to give to the control scheme. Must be unique within the control schemes of the
        /// asset. Also used as default name of <see cref="InputControlScheme.bindingGroup">binding group</see> associated
        /// with the control scheme.</param>
        /// <returns>Syntax to allow providing additional configuration for the newly added control scheme.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> -or- <paramref name="name"/>
        /// is <c>null</c> or empty.</exception>
        public static ControlSchemeSyntax AddControlScheme(this InputActionAsset asset, string name)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var index = asset.controlSchemes.Count;
            asset.AddControlScheme(new InputControlScheme(name));

            return new ControlSchemeSyntax(asset, index);
        }

        /// <summary>
        /// Remove the control scheme with the given name from the asset.
        /// </summary>
        /// <param name="asset">Asset to remove the control scheme from.</param>
        /// <param name="name">Name of the control scheme. Matching is case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is null -or- <paramref name="name"/>
        /// is <c>null</c> or empty.</exception>
        /// <remarks>
        /// If no control scheme with the given name can be found, the method does nothing.
        /// </remarks>
        public static void RemoveControlScheme(this InputActionAsset asset, string name)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var index = asset.FindControlSchemeIndex(name);
            if (index != -1)
                ArrayHelpers.EraseAt(ref asset.m_ControlSchemes, index);
        }

        public static InputControlScheme WithBindingGroup(this InputControlScheme scheme, string bindingGroup)
        {
            return new ControlSchemeSyntax(scheme).WithBindingGroup(bindingGroup).Done();
        }

        public static InputControlScheme WithRequiredDevice(this InputControlScheme scheme, string controlPath)
        {
            return new ControlSchemeSyntax(scheme).WithRequiredDevice(controlPath).Done();
        }

        public static InputControlScheme WithOptionalDevice(this InputControlScheme scheme, string controlPath)
        {
            return new ControlSchemeSyntax(scheme).WithOptionalDevice(controlPath).Done();
        }

        public static InputControlScheme OrWithRequiredDevice(this InputControlScheme scheme, string controlPath)
        {
            return new ControlSchemeSyntax(scheme).OrWithRequiredDevice(controlPath).Done();
        }

        public static InputControlScheme OrWithOptionalDevice(this InputControlScheme scheme, string controlPath)
        {
            return new ControlSchemeSyntax(scheme).OrWithOptionalDevice(controlPath).Done();
        }

        /// <summary>
        /// Syntax to configure a binding added to an <see cref="InputAction"/> or an
        /// <see cref="InputActionMap"/>.
        /// </summary>
        /// <seealso cref="AddBinding(InputAction,InputBinding)"/>
        public struct BindingSyntax
        {
            private readonly InputActionMap m_ActionMap;
            private readonly InputAction m_Action;
            internal readonly int m_BindingIndex;

            internal BindingSyntax(InputActionMap map, InputAction action, int bindingIndex)
            {
                m_ActionMap = map;
                m_Action = action;
                m_BindingIndex = bindingIndex;
            }

            /// <summary>
            /// Set the <see cref="InputBinding.name"/> of the binding.
            /// </summary>
            /// <param name="name">Name for the binding.</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <seealso cref="InputBinding.name"/>
            /// <seealso cref="AddBinding"/>
            public BindingSyntax WithName(string name)
            {
                m_ActionMap.m_Bindings[m_BindingIndex].name = name;
                m_ActionMap.ClearPerActionCachedBindingData();
                return this;
            }

            /// <summary>
            /// Set the <see cref="InputBinding.path"/> of the binding.
            /// </summary>
            /// <param name="path">Path for the binding.</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <seealso cref="InputBinding.path"/>
            public BindingSyntax WithPath(string path)
            {
                m_ActionMap.m_Bindings[m_BindingIndex].path = path;
                m_ActionMap.ClearPerActionCachedBindingData();
                return this;
            }

            public BindingSyntax WithGroup(string group)
            {
                if (string.IsNullOrEmpty(group))
                    throw new ArgumentException("Group name cannot be null or empty", nameof(group));
                if (group.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Group name cannot contain separator character '{InputBinding.Separator}'", nameof(group));

                return WithGroups(group);
            }

            public BindingSyntax WithGroups(string groups)
            {
                if (string.IsNullOrEmpty(groups))
                    return this;

                // Join with existing group, if any.
                var currentGroups = m_ActionMap.m_Bindings[m_BindingIndex].groups;
                if (!string.IsNullOrEmpty(currentGroups))
                    groups = string.Join(InputBinding.kSeparatorString, currentGroups, groups);

                // Set groups on binding.
                m_ActionMap.m_Bindings[m_BindingIndex].groups = groups;
                m_ActionMap.ClearPerActionCachedBindingData();

                return this;
            }

            public BindingSyntax WithInteraction(string interaction)
            {
                if (string.IsNullOrEmpty(interaction))
                    throw new ArgumentException("Interaction cannot be null or empty", nameof(interaction));
                if (interaction.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Interaction string cannot contain separator character '{InputBinding.Separator}'", nameof(interaction));

                return WithInteractions(interaction);
            }

            public BindingSyntax WithInteractions(string interactions)
            {
                if (string.IsNullOrEmpty(interactions))
                    return this;

                // Join with existing interaction string, if any.
                var currentInteractions = m_ActionMap.m_Bindings[m_BindingIndex].interactions;
                if (!string.IsNullOrEmpty(currentInteractions))
                    interactions = string.Join(InputBinding.kSeparatorString, currentInteractions, interactions);

                // Set interactions on binding.
                m_ActionMap.m_Bindings[m_BindingIndex].interactions = interactions;
                m_ActionMap.ClearPerActionCachedBindingData();

                return this;
            }

            public BindingSyntax WithInteraction<TInteraction>()
                where TInteraction : IInputInteraction
            {
                var interactionName = InputProcessor.s_Processors.FindNameForType(typeof(TInteraction));
                if (interactionName.IsEmpty())
                    throw new NotSupportedException($"Type '{typeof(TInteraction)}' has not been registered as a processor");

                return WithInteraction(interactionName);
            }

            public BindingSyntax WithProcessor(string processor)
            {
                if (string.IsNullOrEmpty(processor))
                    throw new ArgumentException("Processor cannot be null or empty", nameof(processor));
                if (processor.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Interaction string cannot contain separator character '{InputBinding.Separator}'", nameof(processor));

                return WithProcessors(processor);
            }

            public BindingSyntax WithProcessors(string processors)
            {
                if (string.IsNullOrEmpty(processors))
                    return this;

                // Join with existing processor string, if any.
                var currentProcessors = m_ActionMap.m_Bindings[m_BindingIndex].processors;
                if (!string.IsNullOrEmpty(currentProcessors))
                    processors = string.Join(InputBinding.kSeparatorString, currentProcessors, processors);

                // Set processors on binding.
                m_ActionMap.m_Bindings[m_BindingIndex].processors = processors;
                m_ActionMap.ClearPerActionCachedBindingData();

                return this;
            }

            public BindingSyntax WithProcessor<TProcessor>()
            {
                var processorName = InputProcessor.s_Processors.FindNameForType(typeof(TProcessor));
                if (processorName.IsEmpty())
                    throw new NotSupportedException($"Type '{typeof(TProcessor)}' has not been registered as a processor");

                return WithProcessor(processorName);
            }

            public BindingSyntax Triggering(InputAction action)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                if (action.isSingletonAction)
                    throw new ArgumentException(
                        $"Cannot change the action a binding triggers on singleton action '{action}'", nameof(action));
                m_ActionMap.m_Bindings[m_BindingIndex].action = action.name;
                m_ActionMap.ClearPerActionCachedBindingData();
                return this;
            }

            public BindingSyntax To(InputBinding binding)
            {
                m_ActionMap.m_Bindings[m_BindingIndex] = binding;
                m_ActionMap.ClearPerActionCachedBindingData();

                // If it's a singleton action, we force the binding to stay with the action.
                if (m_ActionMap.m_SingletonAction != null)
                    m_ActionMap.m_Bindings[m_BindingIndex].action = m_Action.name;

                return this;
            }

            public void Erase()
            {
                ArrayHelpers.EraseAt(ref m_ActionMap.m_Bindings, m_BindingIndex);
                m_ActionMap.ClearPerActionCachedBindingData();

                // We have switched to a different binding array. For singleton actions, we need to
                // sync up the reference that the action itself has.
                if (m_ActionMap.m_SingletonAction != null)
                    m_ActionMap.m_SingletonAction.m_SingletonActionBindings = m_ActionMap.m_Bindings;
            }

            internal BindingSyntax And => throw new NotImplementedException();
        }

        public struct CompositeSyntax
        {
            private readonly InputAction m_Action;
            private readonly InputActionMap m_ActionMap;
            private int m_CompositeIndex;

            internal CompositeSyntax(InputActionMap map, InputAction action, int compositeIndex)
            {
                m_Action = action;
                m_ActionMap = map;
                m_CompositeIndex = compositeIndex;
            }

            public CompositeSyntax With(string name, string binding, string groups = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                int bindingIndex;
                if (m_Action != null)
                    bindingIndex = m_Action.AddBinding(path: binding, groups: groups)
                        .m_BindingIndex;
                else
                    bindingIndex = m_ActionMap.AddBinding(path: binding, groups: groups)
                        .m_BindingIndex;

                m_ActionMap.m_Bindings[bindingIndex].name = name;
                m_ActionMap.m_Bindings[bindingIndex].isPartOfComposite = true;

                return this;
            }
        }

        public struct ControlSchemeSyntax
        {
            private readonly InputActionAsset m_Asset;
            private readonly int m_ControlSchemeIndex;
            private InputControlScheme m_ControlScheme;

            internal ControlSchemeSyntax(InputActionAsset asset, int index)
            {
                m_Asset = asset;
                m_ControlSchemeIndex = index;
                m_ControlScheme = new InputControlScheme();
            }

            internal ControlSchemeSyntax(InputControlScheme controlScheme)
            {
                m_Asset = null;
                m_ControlSchemeIndex = -1;
                m_ControlScheme = controlScheme;
            }

            public ControlSchemeSyntax WithBindingGroup(string bindingGroup)
            {
                if (string.IsNullOrEmpty(bindingGroup))
                    throw new ArgumentNullException(nameof(bindingGroup));

                if (m_Asset == null)
                    m_ControlScheme.m_BindingGroup = bindingGroup;
                else
                    m_Asset.m_ControlSchemes[m_ControlSchemeIndex].bindingGroup = bindingGroup;

                return this;
            }

            public ControlSchemeSyntax WithRequiredDevice<TDevice>()
                where TDevice : InputDevice
            {
                return WithRequiredDevice(DeviceTypeToControlPath<TDevice>());
            }

            public ControlSchemeSyntax WithOptionalDevice<TDevice>()
                where TDevice : InputDevice
            {
                return WithOptionalDevice(DeviceTypeToControlPath<TDevice>());
            }

            public ControlSchemeSyntax OrWithRequiredDevice<TDevice>()
                where TDevice : InputDevice
            {
                return WithRequiredDevice(DeviceTypeToControlPath<TDevice>());
            }

            public ControlSchemeSyntax OrWithOptionalDevice<TDevice>()
                where TDevice : InputDevice
            {
                return WithOptionalDevice(DeviceTypeToControlPath<TDevice>());
            }

            public ControlSchemeSyntax WithRequiredDevice(string controlPath)
            {
                AddDeviceEntry(controlPath, InputControlScheme.DeviceRequirement.Flags.None);
                return this;
            }

            public ControlSchemeSyntax WithOptionalDevice(string controlPath)
            {
                AddDeviceEntry(controlPath, InputControlScheme.DeviceRequirement.Flags.Optional);
                return this;
            }

            public ControlSchemeSyntax OrWithRequiredDevice(string controlPath)
            {
                AddDeviceEntry(controlPath, InputControlScheme.DeviceRequirement.Flags.Or);
                return this;
            }

            public ControlSchemeSyntax OrWithOptionalDevice(string controlPath)
            {
                AddDeviceEntry(controlPath,
                    InputControlScheme.DeviceRequirement.Flags.Optional |
                    InputControlScheme.DeviceRequirement.Flags.Or);
                return this;
            }

            private string DeviceTypeToControlPath<TDevice>()
                where TDevice : InputDevice
            {
                var layoutName = InputControlLayout.s_Layouts.TryFindLayoutForType(typeof(TDevice)).ToString();
                if (string.IsNullOrEmpty(layoutName))
                    layoutName = typeof(TDevice).Name;
                return $"<{layoutName}>";
            }

            public InputControlScheme Done()
            {
                if (m_Asset != null)
                    return m_Asset.m_ControlSchemes[m_ControlSchemeIndex];
                return m_ControlScheme;
            }

            private void AddDeviceEntry(string controlPath, InputControlScheme.DeviceRequirement.Flags flags)
            {
                if (string.IsNullOrEmpty(controlPath))
                    throw new ArgumentNullException(nameof(controlPath));

                var scheme = m_Asset != null ? m_Asset.m_ControlSchemes[m_ControlSchemeIndex] : m_ControlScheme;
                ArrayHelpers.Append(ref scheme.m_DeviceRequirements,
                    new InputControlScheme.DeviceRequirement
                    {
                        m_ControlPath = controlPath,
                        m_Flags = flags,
                    });

                if (m_Asset == null)
                    m_ControlScheme = scheme;
                else
                    m_Asset.m_ControlSchemes[m_ControlSchemeIndex] = scheme;
            }
        }
    }
}
