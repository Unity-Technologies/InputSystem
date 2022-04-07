using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: Rename all the xxxSyntax structs to xxxAccessor

////TODO: Replace all 'WithXXX' in the accessors with just 'SetXXX'; the 'WithXXX' reads too awkwardly

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
        /// map with the same name -or- <paramref name="map"/> is currently enabled -or- <paramref name="map"/> is part of
        /// an <see cref="InputActionAsset"/> that has <see cref="InputActionMap"/>s that are enabled.</exception>
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

            map.OnWantToChangeSetup();
            asset.OnWantToChangeSetup();

            ArrayHelpers.Append(ref asset.m_ActionMaps, map);
            map.m_Asset = asset;
            asset.OnSetupChanged();
        }

        /// <summary>
        /// Remove the given action map from the asset.
        /// </summary>
        /// <param name="asset">Asset to add the action map to.</param>
        /// <param name="map">An action map. If the given map is not part of the asset, the method
        /// does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> or <paramref name="map"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> is currently enabled (see <see
        /// cref="InputActionMap.enabled"/>) or is part of an <see cref="InputActionAsset"/> that has <see cref="InputActionMap"/>s
        /// that are currently enabled.</exception>
        /// <seealso cref="RemoveActionMap(InputActionAsset,string)"/>
        /// <seealso cref="InputActionAsset.actionMaps"/>
        public static void RemoveActionMap(this InputActionAsset asset, InputActionMap map)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            map.OnWantToChangeSetup();
            asset.OnWantToChangeSetup();

            // Ignore if not part of this asset.
            if (map.m_Asset != asset)
                return;

            ArrayHelpers.Erase(ref asset.m_ActionMaps, map);
            map.m_Asset = null;
            asset.OnSetupChanged();
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

        /// <summary>
        /// Add a new <see cref="InputAction"/> to the given <paramref name="map"/>.
        /// </summary>
        /// <param name="map">Action map to add the action to. The action will be appended to
        /// <see cref="InputActionMap.actions"/> of the map. The map must be disabled (see
        /// <see cref="InputActionMap.enabled"/>).</param>
        /// <param name="name">Name to give to the action. Must not be <c>null</c> or empty. Also,
        /// no other action that already exists in <paramref name="map"/> must have this name already.</param>
        /// <param name="type">Action type. See <see cref="InputAction.type"/>.</param>
        /// <param name="binding">If not <c>null</c>, a binding is automatically added to the newly created action
        /// with the value of this parameter being used as the binding's <see cref="InputBinding.path"/>.</param>
        /// <param name="interactions">If <paramref name="binding"/> is not <c>null</c>, this string is used for
        /// <see cref="InputBinding.interactions"/> of the binding that is automatically added for the action.</param>
        /// <param name="processors">If <paramref name="binding"/> is not <c>null</c>, this string is used for
        /// <see cref="InputBinding.processors"/> of the binding that is automatically added for the action.</param>
        /// <param name="groups">If <paramref name="binding"/> is not <c>null</c>, this string is used for
        /// <see cref="InputBinding.groups"/> of the binding that is automatically added for the action.</param>
        /// <param name="expectedControlLayout">Value for <see cref="InputAction.expectedControlType"/>; <c>null</c>
        /// by default.</param>
        /// <returns>The newly added input action.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c> or empty.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> is enabled (see <see cref="InputActionMap.enabled"/>)
        /// or is part of an <see cref="InputActionAsset"/> that has <see cref="InputActionMap"/>s that are <see cref="InputActionMap.enabled"/>
        /// -or- <paramref name="map"/> already contains an action called <paramref name="name"/> (case-insensitive).</exception>
        public static InputAction AddAction(this InputActionMap map, string name, InputActionType type = default, string binding = null,
            string interactions = null, string processors = null, string groups = null, string expectedControlLayout = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", nameof(name));
            map.OnWantToChangeSetup();
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

            // Add binding, if supplied.
            if (!string.IsNullOrEmpty(binding))
            {
                // Will trigger OnSetupChanged.
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

                map.OnSetupChanged();
            }

            return action;
        }

        /// <summary>
        /// Remove the given action from its <see cref="InputActionMap"/>.
        /// </summary>
        /// <param name="action">An input action that is part of an <see cref="InputActionMap"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="action"/> is a standalone action
        /// that is not part of an <see cref="InputActionMap"/> and thus cannot be removed from anything.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="action"/> is part of an <see cref="InputActionMap"/>
        /// or <see cref="InputActionAsset"/> that has at least one enabled action.</exception>
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
            actionMap.OnWantToChangeSetup();

            var bindingsForAction = action.bindings.ToArray();

            var index = actionMap.m_Actions.IndexOfReference(action);
            Debug.Assert(index != -1, "Could not find action in map");
            ArrayHelpers.EraseAt(ref actionMap.m_Actions, index);

            action.m_ActionMap = null;
            action.m_SingletonActionBindings = bindingsForAction;

            // Remove bindings to action from map.
            var newActionMapBindingCount = actionMap.m_Bindings.Length - bindingsForAction.Length;
            if (newActionMapBindingCount == 0)
            {
                actionMap.m_Bindings = null;
            }
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

            actionMap.OnSetupChanged();
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
        /// the given <paramref cref="action"/>.
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
        /// <param name="action">An action to add the binding to.</param>
        /// <param name="binding">Binding to add to the action or default. Binding can be further configured via
        /// the struct returned by the method.</param>
        /// <returns>
        /// Returns a fluent-style syntax structure that allows performing additional modifications
        /// based on the new binding.
        /// </returns>
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

            ////REVIEW: should this reference actions by ID?
            Debug.Assert(action.m_Name != null || action.isSingletonAction);
            binding.action = action.name;

            var actionMap = action.GetOrCreateActionMap();
            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, bindingIndex);
        }

        /// <summary>
        /// Add a new binding to the given action map.
        /// </summary>
        /// <param name="actionMap">Action map to add the binding to.</param>
        /// <param name="path">Path of the control(s) to bind to. See <see cref="InputControlPath"/> and
        /// <see cref="InputBinding.path"/>.</param>
        /// <param name="interactions">Names and parameters for interactions to apply to the
        /// binding. See <see cref="InputBinding.interactions"/>.</param>
        /// <param name="groups">Optional list of groups to apply to the binding. See <see cref="InputBinding.groups"/>.</param>
        /// <param name="action">Action to trigger from the binding. See <see cref="InputBinding.action"/>.</param>
        /// <param name="processors">Optional list of processors to apply to the binding. See <see cref="InputBinding.processors"/>.</param>
        /// <returns>A write-accessor to the newly added binding.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Add a binding for the A button the gamepad and make it trigger
        /// // the "fire" action.
        /// var gameplayActions = playerInput.actions.FindActionMap("gameplay");
        /// gameplayActions.AddBinding("&lt;Gamepad&gt;/buttonSouth", action: "fire");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding"/>
        /// <seealso cref="InputActionMap.bindings"/>
        public static BindingSyntax AddBinding(this InputActionMap actionMap, string path,
            string interactions = null, string groups = null, string action = null, string processors = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), "Binding path cannot be null");

            return AddBinding(actionMap,
                new InputBinding
                {
                    path = path,
                    interactions = interactions,
                    groups = groups,
                    action = action,
                    processors = processors,
                });
        }

        /// <summary>
        /// Add a new binding that triggers the given action to the given action map.
        /// </summary>
        /// <param name="actionMap">Action map to add the binding to.</param>
        /// <param name="action">Action to trigger from the binding. See <see cref="InputBinding.action"/>.
        /// Must be part of <paramref name="actionMap"/>.</param>
        /// <param name="path">Path of the control(s) to bind to. See <see cref="InputControlPath"/> and
        /// <see cref="InputBinding.path"/>.</param>
        /// <param name="interactions">Names and parameters for interactions to apply to the
        /// binding. See <see cref="InputBinding.interactions"/>.</param>
        /// <param name="groups">Binding groups to apply to the binding. See <see cref="InputBinding.groups"/>.</param>
        /// <returns>A write-accessor to the newly added binding.</returns>
        /// <exception cref="ArgumentException"><paramref name="action"/> is not part of <paramref name="actionMap"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <seealso cref="InputBinding"/>
        /// <seealso cref="InputActionMap.bindings"/>
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
            return new BindingSyntax(actionMap, bindingIndex);
        }

        /// <summary>
        /// Add a composite binding to the <see cref="InputAction.bindings"/> of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">Action to add the binding to.</param>
        /// <param name="composite">Type of composite to add. This needs to be the name the composite
        /// has been registered under using <see cref="InputSystem.RegisterBindingComposite{T}"/>. Case-insensitive.</param>
        /// <param name="interactions">Interactions to add to the binding. See <see cref="InputBinding.interactions"/>.</param>
        /// <param name="processors">Processors to add to the binding. See <see cref="InputBinding.processors"/>.</param>
        /// <returns>A write accessor to the newly added composite binding.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="composite"/> is <c>null</c> or empty.</exception>
        public static CompositeSyntax AddCompositeBinding(this InputAction action, string composite,
            string interactions = null, string processors = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentException("Composite name cannot be null or empty", nameof(composite));

            var actionMap = action.GetOrCreateActionMap();

            var binding = new InputBinding
            {
                name = NameAndParameters.ParseName(composite),
                path = composite,
                interactions = interactions,
                processors = processors,
                isComposite = true,
                action = action.name
            };

            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new CompositeSyntax(actionMap, action, bindingIndex);
        }

        ////TODO: AddCompositeBinding<T>

        private static int AddBindingInternal(InputActionMap map, InputBinding binding, int bindingIndex = -1)
        {
            Debug.Assert(map != null);

            // Make sure the binding has an ID.
            if (string.IsNullOrEmpty(binding.m_Id))
                binding.GenerateId();

            // Append to bindings in set.
            if (bindingIndex < 0)
                bindingIndex = ArrayHelpers.Append(ref map.m_Bindings, binding);
            else
                ArrayHelpers.InsertAt(ref map.m_Bindings, bindingIndex, binding);

            // Make sure this asset is reloaded from disk when exiting play mode so it isn't inadvertently
            // changed between play sessions. Only applies when running in the editor.
            if (map.asset != null)
                map.asset.MarkAsDirty();

            // If we're looking at a singleton action, make sure m_Bindings is up to date just
            // in case the action gets serialized.
            if (map.m_SingletonAction != null)
                map.m_SingletonAction.m_SingletonActionBindings = map.m_Bindings;

            // NOTE: We treat this as a mere binding modification, even though we have added something.
            //       InputAction.RestoreActionStatesAfterReResolvingBindings() can deal with bindings
            //       having been removed or added.
            map.OnBindingModified();

            return bindingIndex;
        }

        /// <summary>
        /// Get write access to the binding in <see cref="InputAction.bindings"/> of <paramref name="action"/>
        /// at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to change.</param>
        /// <param name="index">Index in <paramref name="action"/>'s <see cref="InputAction.bindings"/> of the binding to be changed.</param>
        /// <returns>A write accessor to the given binding.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "fire" action from PlayerInput.
        /// var fireAction = playerInput.actions["fire"];
        ///
        /// // Change its second binding to go to the left mouse button.
        /// fireAction.ChangeBinding(1)
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range (as per <see cref="InputAction.bindings"/>
        /// of <paramref name="action"/>).</exception>
        public static BindingSyntax ChangeBinding(this InputAction action, int index)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var indexOnMap = action.BindingIndexOnActionToBindingIndexOnMap(index);
            return new BindingSyntax(action.GetOrCreateActionMap(), indexOnMap, action);
        }

        public static BindingSyntax ChangeBinding(this InputAction action, string name)
        {
            return action.ChangeBinding(new InputBinding { name = name });
        }

        /// <summary>
        /// Get write access to the binding in <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>
        /// at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="actionMap">Action map whose bindings to change.</param>
        /// <param name="index">Index in <paramref name="actionMap"/>'s <see cref="InputActionMap.bindings"/> of the binding to be changed.</param>
        /// <returns>A write accessor to the given binding.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "gameplay" actions from PlayerInput.
        /// var gameplayActions = playerInput.actions.FindActionMap("gameplay");
        ///
        /// // Change its second binding to go to the left mouse button.
        /// gameplayActions.ChangeBinding(1)
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range (as per <see cref="InputActionMap.bindings"/>
        /// of <paramref name="actionMap"/>).</exception>
        public static BindingSyntax ChangeBinding(this InputActionMap actionMap, int index)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (index < 0 || index >= actionMap.m_Bindings.LengthSafe())
                throw new ArgumentOutOfRangeException(nameof(index));

            return new BindingSyntax(actionMap, index);
        }

        /// <summary>
        /// Get write access to the binding in <see cref="InputAction.bindings"/> of <paramref name="action"/>
        /// that has the given <paramref name="id"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to change.</param>
        /// <param name="id">ID of the binding as per <see cref="InputBinding.id"/>.</param>
        /// <returns>A write accessor to the binding with the given ID.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "fire" action from PlayerInput.
        /// var fireAction = playerInput.actions["fire"];
        ///
        /// // Change the binding with the given ID to go to the left mouse button.
        /// fireAction.ChangeBindingWithId("c3de9215-31c3-4654-8562-854bf2f7864f")
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">No binding with the given <paramref name="id"/> exists
        /// on <paramref name="action"/>.</exception>
        public static BindingSyntax ChangeBindingWithId(this InputAction action, string id)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return action.ChangeBinding(new InputBinding {m_Id = id});
        }

        /// <summary>
        /// Get write access to the binding in <see cref="InputAction.bindings"/> of <paramref name="action"/>
        /// that has the given <paramref name="id"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to change.</param>
        /// <param name="id">ID of the binding as per <see cref="InputBinding.id"/>.</param>
        /// <returns>A write accessor to the binding with the given ID.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "fire" action from PlayerInput.
        /// var fireAction = playerInput.actions["fire"];
        ///
        /// // Change the binding with the given ID to go to the left mouse button.
        /// fireAction.ChangeBindingWithId(new Guid("c3de9215-31c3-4654-8562-854bf2f7864f"))
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">No binding with the given <paramref name="id"/> exists
        /// on <paramref name="action"/>.</exception>
        public static BindingSyntax ChangeBindingWithId(this InputAction action, Guid id)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return action.ChangeBinding(new InputBinding {id = id});
        }

        /// <summary>
        /// Get write access to the first binding in <see cref="InputAction.bindings"/> of <paramref name="action"/>
        /// that is assigned to the given binding <paramref name="group"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to change.</param>
        /// <param name="group">Name of the binding group as per <see cref="InputBinding.groups"/>.</param>
        /// <returns>A write accessor to the first binding on <paramref name="action"/> that is assigned to the
        /// given binding <paramref name="group"/>.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "fire" action from PlayerInput.
        /// var fireAction = playerInput.actions["fire"];
        ///
        /// // Change the binding in the "Keyboard&amp;Mouse" group to go to the left mouse button.
        /// fireAction.ChangeBindingWithGroup("Keyboard&amp;Mouse")
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">No binding on the <paramref name="action"/> is assigned
        /// to the given binding <paramref name="group"/>.</exception>
        public static BindingSyntax ChangeBindingWithGroup(this InputAction action, string group)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return action.ChangeBinding(new InputBinding {groups = group});
        }

        /// <summary>
        /// Get write access to the binding in <see cref="InputAction.bindings"/> of <paramref name="action"/>
        /// that is bound to the given <paramref name="path"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to change.</param>
        /// <param name="path">Path of the binding as per <see cref="InputBinding.path"/>.</param>
        /// <returns>A write accessor to the binding on <paramref name="action"/> that is assigned the
        /// given <paramref name="path"/>.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Grab "fire" action from PlayerInput.
        /// var fireAction = playerInput.actions["fire"];
        ///
        /// // Change the binding to the right mouse button to go to the left mouse button instead.
        /// fireAction.ChangeBindingWithPath("&lt;Mouse&gt;/rightButton")
        ///     .WithPath("&lt;Mouse&gt;/leftButton");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">No binding on the <paramref name="action"/> is assigned
        /// the given <paramref name="path"/>.</exception>
        public static BindingSyntax ChangeBindingWithPath(this InputAction action, string path)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return action.ChangeBinding(new InputBinding {path = path});
        }

        /// <summary>
        /// Get write access to the binding on <paramref name="action"/> that matches the given
        /// <paramref name="match"/>.
        /// </summary>
        /// <param name="action">Action whose bindings to match against.</param>
        /// <param name="match">A binding mask. See <see cref="InputBinding.Matches"/> for
        /// details.</param>
        /// <returns>A write-accessor to the first binding matching <paramref name="match"/> or
        /// an invalid accessor (see <see cref="BindingSyntax.valid"/>) if no binding was found to
        /// match the mask.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        public static BindingSyntax ChangeBinding(this InputAction action, InputBinding match)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var actionMap = action.GetOrCreateActionMap();

            int bindingIndexInMap = -1;
            var id = action.idDontGenerate;
            if (id != null)
            {
                // Prio1: Attempt to match action id (stronger)
                match.action = action.id.ToString();
                bindingIndexInMap = actionMap.FindBindingRelativeToMap(match);
            }
            if (bindingIndexInMap == -1)
            {
                // Prio2: Attempt to match action name (weaker)
                match.action = action.name;
                bindingIndexInMap = actionMap.FindBindingRelativeToMap(match);
            }
            if (bindingIndexInMap == -1)
                return default;

            return new BindingSyntax(actionMap, bindingIndexInMap);
        }

        /// <summary>
        /// Get a write accessor to the binding of <paramref name="action"/> that is both a composite
        /// (see <see cref="InputBinding.isComposite"/>) and has the given binding name or composite
        /// type.
        /// </summary>
        /// <param name="action">Action to look up the binding on. All bindings in the action's
        /// <see cref="InputAction.bindings"/> property will be considered.</param>
        /// <param name="compositeName">Either the name of the composite binding (see <see cref="InputBinding.name"/>)
        /// to look for or the name of the composite type used in the binding (such as "1DAxis"). Case-insensitive.</param>
        /// <returns>A write accessor to the given composite binding or an invalid accessor if no composite
        /// matching <paramref name="compositeName"/> could be found on <paramref name="action"/>.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Add arrow keys as alternatives to the WASD Vector2 composite.
        /// playerInput.actions["move"]
        ///     .ChangeCompositeBinding("WASD")
        ///         .InsertPartBinding("Up", "&lt;Keyboard&gt;/upArrow")
        ///         .InsertPartBinding("Down", "&lt;Keyboard&gt;/downArrow")
        ///         .InsertPartBinding("Left", "&lt;Keyboard&gt;/leftArrow")
        ///         .InsertPartBinding("Right", "&lt;Keyboard&gt;/rightArrow");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="compositeName"/>
        /// is <c>null</c> or empty.</exception>
        /// <seealso cref="InputBinding.isComposite"/>
        /// <seealso cref="InputBindingComposite"/>
        public static BindingSyntax ChangeCompositeBinding(this InputAction action, string compositeName)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(compositeName))
                throw new ArgumentNullException(nameof(compositeName));

            var actionMap = action.GetOrCreateActionMap();
            var bindings = actionMap.m_Bindings;
            var numBindings = bindings.LengthSafe();

            for (var i = 0; i < numBindings; ++i)
            {
                ref var binding = ref bindings[i];
                if (!binding.isComposite || !binding.TriggersAction(action))
                    continue;

                ////REVIEW: should this do a registration lookup to deal with aliases?
                if (compositeName.Equals(binding.name, StringComparison.InvariantCultureIgnoreCase)
                    || compositeName.Equals(NameAndParameters.ParseName(binding.path),
                        StringComparison.InvariantCultureIgnoreCase))
                    return new BindingSyntax(actionMap, i, action);
            }

            return default;
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
            actionMap?.ClearActionLookupTable();

            if (actionMap?.asset != null)
                actionMap?.asset.MarkAsDirty();

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

            asset.MarkAsDirty();
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
        /// <remarks>
        /// <example>
        /// <code>
        /// // Create an .inputactions asset.
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        ///
        /// // Add an action map to it.
        /// var actionMap = asset.AddActionMap("actions");
        ///
        /// // Add an action to it and bind it to the A button on the gamepad.
        /// // Also, associate that binding with the "Gamepad" control scheme.
        /// var action = actionMap.AddAction("action");
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth", groups: "Gamepad");
        ///
        /// // Add a control scheme called "Gamepad" that requires a Gamepad device.
        /// asset.AddControlScheme("Gamepad")
        ///     .WithRequiredDevice&lt;Gamepad&gt;();
        /// </code>
        /// </example>
        /// </remarks>
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

            asset.MarkAsDirty();
        }

        public static InputControlScheme WithBindingGroup(this InputControlScheme scheme, string bindingGroup)
        {
            return new ControlSchemeSyntax(scheme).WithBindingGroup(bindingGroup).Done();
        }

        public static InputControlScheme WithDevice(this InputControlScheme scheme, string controlPath, bool required)
        {
            if (required)
                return new ControlSchemeSyntax(scheme).WithRequiredDevice(controlPath).Done();
            return new ControlSchemeSyntax(scheme).WithOptionalDevice(controlPath).Done();
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
        /// Write accessor to a binding on either an <see cref="InputAction"/> or an
        /// <see cref="InputActionMap"/>.
        /// </summary>
        /// <remarks>
        /// Both <see cref="InputAction.bindings"/> and <see cref="InputActionMap.bindings"/> are
        /// read-only. To modify bindings (other than setting overrides which you can do
        /// through <see cref="InputActionRebindingExtensions.ApplyBindingOverride(InputAction,int,InputBinding)"/>),
        /// it is necessary to gain indirect write access through this structure.
        ///
        /// <example>
        /// <code>
        /// playerInput.actions["fire"]
        ///     .ChangeBinding(0)
        ///     .WithPath("&lt;Keyboard&gt;/space");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="AddBinding(InputAction,InputBinding)"/>
        /// <seealso cref="ChangeBinding(InputAction,int)"/>
        public struct BindingSyntax
        {
            private readonly InputActionMap m_ActionMap;
            private readonly InputAction m_Action;
            internal readonly int m_BindingIndexInMap;

            /// <summary>
            /// True if the if binding accessor is valid.
            /// </summary>
            public bool valid => m_ActionMap != null && m_BindingIndexInMap >= 0 && m_BindingIndexInMap < m_ActionMap.m_Bindings.LengthSafe();

            /// <summary>
            /// Index of the binding that the accessor refers to.
            /// </summary>
            /// <remarks>
            /// When accessing bindings on an <see cref="InputAction"/>, this is the index in
            /// <see cref="InputAction.bindings"/> of the action. When accessing bindings on an
            /// <see cref="InputActionMap"/>, it is the index <see cref="InputActionMap.bindings"/>
            /// of the map.
            /// </remarks>
            public int bindingIndex
            {
                get
                {
                    if (!valid)
                        return -1;
                    if (m_Action != null)
                        return m_Action.BindingIndexOnMapToBindingIndexOnAction(m_BindingIndexInMap);
                    return m_BindingIndexInMap;
                }
            }

            /// <summary>
            /// The current binding in entirety.
            /// </summary>
            /// <exception cref="InvalidOperationException">The accessor is not <see cref="valid"/>.</exception>
            public InputBinding binding
            {
                get
                {
                    if (!valid)
                        throw new InvalidOperationException("BindingSyntax accessor is not valid");
                    return m_ActionMap.m_Bindings[m_BindingIndexInMap];
                }
            }

            internal BindingSyntax(InputActionMap map, int bindingIndexInMap, InputAction action = null)
            {
                m_ActionMap = map;
                m_BindingIndexInMap = bindingIndexInMap;
                m_Action = action;
            }

            /// <summary>
            /// Set the <see cref="InputBinding.name"/> of the binding.
            /// </summary>
            /// <param name="name">Name for the binding.</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <exception cref="InvalidOperationException">The binding accessor is not <see cref="valid"/>.</exception>
            /// <seealso cref="InputBinding.name"/>
            public BindingSyntax WithName(string name)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                m_ActionMap.m_Bindings[m_BindingIndexInMap].name = name;
                m_ActionMap.OnBindingModified();
                return this;
            }

            /// <summary>
            /// Set the <see cref="InputBinding.path"/> of the binding.
            /// </summary>
            /// <param name="path">Path for the binding.</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <exception cref="InvalidOperationException">The binding accessor is not <see cref="valid"/>.</exception>
            /// <seealso cref="InputBinding.path"/>
            public BindingSyntax WithPath(string path)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                m_ActionMap.m_Bindings[m_BindingIndexInMap].path = path;
                m_ActionMap.OnBindingModified();
                return this;
            }

            /// <summary>
            /// Add <paramref name="group"/> to the list of <see cref="InputBinding.groups"/> of the binding.
            /// </summary>
            /// <param name="group">Name of the binding group (such as "Gamepad").</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <exception cref="ArgumentException"><paramref name="group"/> is <c>null</c> or empty -or- it contains
            /// a <see cref="InputBinding.Separator"/> character.</exception>
            public BindingSyntax WithGroup(string group)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(group))
                    throw new ArgumentException("Group name cannot be null or empty", nameof(group));
                if (group.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Group name cannot contain separator character '{InputBinding.Separator}'", nameof(group));

                return WithGroups(group);
            }

            public BindingSyntax WithGroups(string groups)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(groups))
                    return this;

                // Join with existing group, if any.
                var currentGroups = m_ActionMap.m_Bindings[m_BindingIndexInMap].groups;
                if (!string.IsNullOrEmpty(currentGroups))
                    groups = string.Join(InputBinding.kSeparatorString, currentGroups, groups);

                // Set groups on binding.
                m_ActionMap.m_Bindings[m_BindingIndexInMap].groups = groups;
                m_ActionMap.OnBindingModified();

                return this;
            }

            public BindingSyntax WithInteraction(string interaction)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(interaction))
                    throw new ArgumentException("Interaction cannot be null or empty", nameof(interaction));
                if (interaction.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Interaction string cannot contain separator character '{InputBinding.Separator}'", nameof(interaction));

                return WithInteractions(interaction);
            }

            public BindingSyntax WithInteractions(string interactions)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(interactions))
                    return this;

                // Join with existing interaction string, if any.
                var currentInteractions = m_ActionMap.m_Bindings[m_BindingIndexInMap].interactions;
                if (!string.IsNullOrEmpty(currentInteractions))
                    interactions = string.Join(InputBinding.kSeparatorString, currentInteractions, interactions);

                // Set interactions on binding.
                m_ActionMap.m_Bindings[m_BindingIndexInMap].interactions = interactions;
                m_ActionMap.OnBindingModified();

                return this;
            }

            public BindingSyntax WithInteraction<TInteraction>()
                where TInteraction : IInputInteraction
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");

                var interactionName = InputProcessor.s_Processors.FindNameForType(typeof(TInteraction));
                if (interactionName.IsEmpty())
                    throw new NotSupportedException($"Type '{typeof(TInteraction)}' has not been registered as a processor");

                return WithInteraction(interactionName);
            }

            public BindingSyntax WithProcessor(string processor)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(processor))
                    throw new ArgumentException("Processor cannot be null or empty", nameof(processor));
                if (processor.IndexOf(InputBinding.Separator) != -1)
                    throw new ArgumentException(
                        $"Interaction string cannot contain separator character '{InputBinding.Separator}'", nameof(processor));

                return WithProcessors(processor);
            }

            public BindingSyntax WithProcessors(string processors)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (string.IsNullOrEmpty(processors))
                    return this;

                // Join with existing processor string, if any.
                var currentProcessors = m_ActionMap.m_Bindings[m_BindingIndexInMap].processors;
                if (!string.IsNullOrEmpty(currentProcessors))
                    processors = string.Join(InputBinding.kSeparatorString, currentProcessors, processors);

                // Set processors on binding.
                m_ActionMap.m_Bindings[m_BindingIndexInMap].processors = processors;
                m_ActionMap.OnBindingModified();

                return this;
            }

            public BindingSyntax WithProcessor<TProcessor>()
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");

                var processorName = InputProcessor.s_Processors.FindNameForType(typeof(TProcessor));
                if (processorName.IsEmpty())
                    throw new NotSupportedException($"Type '{typeof(TProcessor)}' has not been registered as a processor");

                return WithProcessor(processorName);
            }

            public BindingSyntax Triggering(InputAction action)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                if (action.isSingletonAction)
                    throw new ArgumentException(
                        $"Cannot change the action a binding triggers on singleton action '{action}'", nameof(action));
                m_ActionMap.m_Bindings[m_BindingIndexInMap].action = action.name;
                m_ActionMap.OnBindingModified();
                return this;
            }

            /// <summary>
            /// Replace the current binding with the given one.
            /// </summary>
            /// <param name="binding">An input binding.</param>
            /// <returns>The same binding syntax for further configuration.</returns>
            /// <remarks>
            /// This method replaces the current binding wholesale, i.e. it will overwrite all fields.
            /// Be aware that this has the potential of corrupting the binding data in case the given
            /// binding is a composite.
            /// </remarks>
            public BindingSyntax To(InputBinding binding)
            {
                if (!valid)
                    throw new InvalidOperationException("Accessor is not valid");

                m_ActionMap.m_Bindings[m_BindingIndexInMap] = binding;

                // If it's a singleton action, we force the binding to stay with the action.
                if (m_ActionMap.m_SingletonAction != null)
                    m_ActionMap.m_Bindings[m_BindingIndexInMap].action = m_ActionMap.m_SingletonAction.name;

                m_ActionMap.OnBindingModified();

                return this;
            }

            /// <summary>
            /// Switch to configuring the next binding.
            /// </summary>
            /// <returns>An instance configured to edit the next binding or an invalid (see <see cref="valid"/>) instance if
            /// there is no next binding.</returns>
            /// <remarks>If the BindingSyntax is restricted to a single action, the result will be invalid (see <see cref="valid"/>)
            /// if there is no next binding on the action. If the BindingSyntax is restricted to an <see cref="InputActionMap"/>, the result will
            /// be be invalid if there is no next binding in the map.</remarks>
            public BindingSyntax NextBinding()
            {
                return Iterate(true);
            }

            /// <summary>
            /// Switch to configuring the previous binding.
            /// </summary>
            /// <returns>An instance configured to edit the previous binding or an invalid (see <see cref="valid"/>) instance if
            /// there is no previous binding.</returns>
            /// <remarks>If the BindingSyntax is restricted to a single action, the result will be invalid (see <see cref="valid"/>)
            /// if there is no previous binding on the action. If the BindingSyntax is restricted to an <see cref="InputActionMap"/>, the result will
            /// be be invalid if there is no previous binding in the map.</remarks>
            public BindingSyntax PreviousBinding()
            {
                return Iterate(false);
            }

            /// <summary>
            /// Iterate to the next part binding of the current composite with the given part name.
            /// </summary>
            /// <param name="partName">Name of the part of the binding, such as <c>"Positive"</c>.</param>
            /// <returns>An accessor to the next part binding with the given name or an invalid (see <see cref="valid"/>)
            /// accessor if there is no such binding.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="partName"/> is <c>null</c> or empty.</exception>
            /// <remarks>
            /// Each binding that is part of a composite is marked with <see cref="InputBinding.isPartOfComposite"/>
            /// set to true. The name of the part is determined by <see cref="InputBinding.name"/> (comparison is
            /// case-insensitive). Which parts are relevant to a specific composite is determined by the type of
            /// composite. An <see cref="Composites.AxisComposite"/>, for example, has <c>"Negative"</c> and a
            /// <c>"Positive"</c> part.
            ///
            /// <example>
            /// <code>
            /// // Delete first "Positive" part of "Axis" composite.
            /// action.ChangeCompositeBinding("Axis")
            ///     .NextPartBinding("Positive").Erase();
            /// </code>
            /// </example>
            /// </remarks>
            /// <seealso cref="InputBinding.isPartOfComposite"/>
            /// <seealso cref="InputBinding.isComposite"/>
            /// <seealso cref="InputBindingComposite"/>
            public BindingSyntax NextPartBinding(string partName)
            {
                if (string.IsNullOrEmpty(partName))
                    throw new ArgumentNullException(nameof(partName));
                return IteratePartBinding(true, partName);
            }

            /// <summary>
            /// Iterate to the previous part binding of the current composite with the given part name.
            /// </summary>
            /// <param name="partName">Name of the part of the binding, such as <c>"Positive"</c>.</param>
            /// <returns>An accessor to the previous part binding with the given name or an invalid (see <see cref="valid"/>)
            /// accessor if there is no such binding.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="partName"/> is <c>null</c> or empty.</exception>
            /// <remarks>
            /// Each binding that is part of a composite is marked with <see cref="InputBinding.isPartOfComposite"/>
            /// set to true. The name of the part is determined by <see cref="InputBinding.name"/> (comparison is
            /// case-insensitive). Which parts are relevant to a specific composite is determined by the type of
            /// composite. An <see cref="Composites.AxisComposite"/>, for example, has <c>"Negative"</c> and a
            /// <c>"Positive"</c> part.
            /// </remarks>
            /// <seealso cref="InputBinding.isPartOfComposite"/>
            /// <seealso cref="InputBinding.isComposite"/>
            /// <seealso cref="InputBindingComposite"/>
            public BindingSyntax PreviousPartBinding(string partName)
            {
                if (string.IsNullOrEmpty(partName))
                    throw new ArgumentNullException(nameof(partName));
                return IteratePartBinding(false, partName);
            }

            /// <summary>
            /// Iterate to the next composite binding.
            /// </summary>
            /// <param name="compositeName">If <c>null</c> (default), an accessor to the next composite binding,
            /// regardless of name or type, is returned. If it is not <c>null</c>, can be either the name of
            /// the binding (see <see cref="InputBinding.name"/>) or the name of the composite used in the
            /// binding (see <see cref="InputSystem.RegisterBindingComposite"/></param>).
            /// <returns>A write accessor to the next composite binding or an invalid accessor (see
            /// <see cref="valid"/>) if no such binding was found.</returns>
            /// <remarks>
            /// <example>
            /// <code>
            /// var accessor = playerInput.actions["fire"].ChangeCompositeBinding("WASD")
            /// </code>
            /// </example>
            /// </remarks>
            public BindingSyntax NextCompositeBinding(string compositeName = null)
            {
                return IterateCompositeBinding(true, compositeName);
            }

            public BindingSyntax PreviousCompositeBinding(string compositeName = null)
            {
                return IterateCompositeBinding(false, compositeName);
            }

            private BindingSyntax Iterate(bool next)
            {
                if (m_ActionMap == null)
                    return default;

                var bindings = m_ActionMap.m_Bindings;
                if (bindings == null)
                    return default;

                // To find the next binding for a specific action, we may have to jump
                // over unrelated bindings in-between.
                var index = m_BindingIndexInMap;
                while (true)
                {
                    index += next ? 1 : -1;
                    if (index < 0 || index >= bindings.Length)
                        return default;

                    if (m_Action == null || bindings[index].TriggersAction(m_Action))
                        break;
                }

                return new BindingSyntax(m_ActionMap, index, m_Action);
            }

            private BindingSyntax IterateCompositeBinding(bool next, string compositeName)
            {
                for (var accessor = Iterate(next); accessor.valid; accessor = accessor.Iterate(next))
                {
                    if (!accessor.binding.isComposite)
                        continue;

                    if (compositeName == null)
                        return accessor;

                    // Try name of binding.
                    if (compositeName.Equals(accessor.binding.name, StringComparison.InvariantCultureIgnoreCase))
                        return accessor;

                    // Try composite type name.
                    var name = NameAndParameters.ParseName(accessor.binding.path);
                    if (compositeName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        return accessor;
                }

                return default;
            }

            private BindingSyntax IteratePartBinding(bool next, string partName)
            {
                if (!valid)
                    return default;

                if (binding.isComposite)
                {
                    // If we're at the composite, only proceed if we're iterating down
                    // instead of up.
                    if (!next)
                        return default;
                }
                else if (!binding.isPartOfComposite)
                    return default;

                for (var accessor = Iterate(next); accessor.valid; accessor = accessor.Iterate(next))
                {
                    if (!accessor.binding.isPartOfComposite)
                        return default;

                    if (partName.Equals(accessor.binding.name, StringComparison.InvariantCultureIgnoreCase))
                        return accessor;
                }

                return default;
            }

            ////TODO: allow setting overrides through this accessor

            /// <summary>
            /// Remove the binding.
            /// </summary>
            /// <remarks>
            /// If the binding is a composite (see <see cref="InputBinding.isComposite"/>), part bindings of the
            /// composite will be removed as well.
            ///
            /// Note that the accessor will not necessarily be invalidated. Instead, it will point to what used
            /// to be the next binding in the array (though that means the accessor will be invalid if the binding
            /// that got erased was the last one in the array).
            /// </remarks>
            /// <exception cref="InvalidOperationException">The instance is not <see cref="valid"/>.</exception>
            public void Erase()
            {
                if (!valid)
                    throw new InvalidOperationException("Instance not valid");

                var isComposite = m_ActionMap.m_Bindings[m_BindingIndexInMap].isComposite;
                ArrayHelpers.EraseAt(ref m_ActionMap.m_Bindings, m_BindingIndexInMap);

                // If it's a composite, also erase part bindings.
                if (isComposite)
                {
                    while (m_BindingIndexInMap < m_ActionMap.m_Bindings.LengthSafe() && m_ActionMap.m_Bindings[m_BindingIndexInMap].isPartOfComposite)
                        ArrayHelpers.EraseAt(ref m_ActionMap.m_Bindings, m_BindingIndexInMap);
                }

                m_ActionMap.OnBindingModified();

                // We have switched to a different binding array. For singleton actions, we need to
                // sync up the reference that the action itself has.
                if (m_ActionMap.m_SingletonAction != null)
                    m_ActionMap.m_SingletonAction.m_SingletonActionBindings = m_ActionMap.m_Bindings;
            }

            public BindingSyntax InsertPartBinding(string partName, string path)
            {
                if (string.IsNullOrEmpty(partName))
                    throw new ArgumentNullException(nameof(partName));
                if (!valid)
                    throw new InvalidOperationException("Binding accessor is not valid");
                var binding = this.binding;
                if (!binding.isPartOfComposite && !binding.isComposite)
                    throw new InvalidOperationException("Binding accessor must point to composite or part binding");

                AddBindingInternal(m_ActionMap,
                    new InputBinding { path = path, isPartOfComposite = true, name = partName },
                    m_BindingIndexInMap + 1);

                return new BindingSyntax(m_ActionMap, m_BindingIndexInMap + 1, m_Action);
            }
        }

        ////TODO: remove this and merge it into BindingSyntax
        public struct CompositeSyntax
        {
            private readonly InputAction m_Action;
            private readonly InputActionMap m_ActionMap;
            private int m_BindingIndexInMap;

            /// <summary>
            /// Index of the binding that the accessor refers to.
            /// </summary>
            /// <remarks>
            /// When accessing bindings on an <see cref="InputAction"/>, this is the index in
            /// <see cref="InputAction.bindings"/> of the action. When accessing bindings on an
            /// <see cref="InputActionMap"/>, it is the index <see cref="InputActionMap.bindings"/>
            /// of the map.
            /// </remarks>
            public int bindingIndex
            {
                get
                {
                    if (m_ActionMap == null)
                        return -1;
                    if (m_Action != null)
                        return m_Action.BindingIndexOnMapToBindingIndexOnAction(m_BindingIndexInMap);
                    return m_BindingIndexInMap;
                }
            }

            internal CompositeSyntax(InputActionMap map, InputAction action, int compositeIndex)
            {
                m_Action = action;
                m_ActionMap = map;
                m_BindingIndexInMap = compositeIndex;
            }

            /// <summary>
            /// Add a part binding to the composite.
            /// </summary>
            /// <param name="name">Name of the part. This is dependent on the type of composite. For
            /// <see cref="Composites.Vector2Composite"/>, for example, the valid parts are <c>"Up"</c>, <c>"Down"</c>,
            /// <c>"Left"</c>, and <c>"Right"</c>.</param>
            /// <param name="binding">Control path to binding to. See <see cref="InputBinding.path"/>.</param>
            /// <param name="groups">Binding groups to assign to the part binding. See <see cref="InputBinding.groups"/>.</param>
            /// <param name="processors">Optional list of processors to apply to the binding. See <see cref="InputBinding.processors"/>.</param>
            /// <returns>The same composite syntax for further configuration.</returns>
            public CompositeSyntax With(string name, string binding, string groups = null, string processors = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                using (InputActionRebindingExtensions.DeferBindingResolution())
                {
                    int bindingIndex;
                    if (m_Action != null)
                        bindingIndex = m_Action.AddBinding(path: binding, groups: groups, processors: processors)
                            .m_BindingIndexInMap;
                    else
                        bindingIndex = m_ActionMap.AddBinding(path: binding, groups: groups, processors: processors)
                            .m_BindingIndexInMap;

                    m_ActionMap.m_Bindings[bindingIndex].name = name;
                    m_ActionMap.m_Bindings[bindingIndex].isPartOfComposite = true;
                }

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
