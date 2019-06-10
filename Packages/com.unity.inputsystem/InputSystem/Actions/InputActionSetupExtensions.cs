using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Extensions to set up <see cref="InputAction">InputActions</see> and <see cref="InputActionMap">
    /// InputActionMaps</see>.
    /// </summary>
    public static class InputActionSetupExtensions
    {
        public static InputActionMap AddActionMap(this InputActionAsset asset, string name)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var map = new InputActionMap(name);
            map.GenerateId();
            asset.AddActionMap(map);
            return map;
        }

        public static InputAction AddAction(this InputActionMap map, string name, string binding = null,
            string interactions = null, string processors = null, string groups = null, string expectedControlLayout = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", nameof(name));
            if (map.enabled)
                throw new InvalidOperationException(
                    $"Cannot add action '{name}' to map '{map}' while it the map is enabled");
            if (map.TryGetAction(name) != null)
                throw new InvalidOperationException(
                    $"Cannot add action with duplicate name '{name}' to set '{map.name}'");

            // Append action to array.
            var action = new InputAction(name)
            {
                expectedControlLayout = expectedControlLayout
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

        ////REVIEW: these multiple string args are so easy to mess up; put into syntax instead?
        public static BindingSyntax AddBinding(this InputAction action, string path, string interactions = null, string processors = null, string groups = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Binding path cannot be null or empty", nameof(path));

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
        /// <param name="action">Action to trigger. Also determines where to add the binding. If the action is not part
        /// of an <see cref="InputActionMap">action map</see>, the binding is added directly to <paramref name="action"/>.
        /// If it is part of a map, the binding is added to the action map (<see cref="InputAction.actionMap"/>).</param>
        /// <param name="control">Control to binding to. The full <see cref="InputControl.path"/> of the control will
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
        /// <remarks>
        /// This works both with actions that are part of an action set as well as with actions that aren't.
        ///
        /// Note that actions must be disabled while altering their binding sets. Also, if the action belongs
        /// to a set, all actions in the set must be disabled.
        /// </remarks>
        public static BindingSyntax AddBinding(this InputAction action, InputBinding binding)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(binding.path))
                throw new ArgumentException("Binding path cannot be null or empty", nameof(binding));
            action.ThrowIfModifyingBindingsIsNotAllowed();

            ////REVIEW: should this reference actions by ID?
            Debug.Assert(action.m_Name != null || action.isSingletonAction);
            binding.action = action.name;

            var actionMap = action.GetOrCreateActionMap();
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();
            var bindingIndex = AddBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, action, bindingIndex);
        }

        public static BindingSyntax AddBinding(this InputActionMap actionMap, string path,
            string interactions = null, string groups = null, string action = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Binding path cannot be null or empty", nameof(path));

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
            if (string.IsNullOrEmpty(binding.path))
                throw new ArgumentException("Binding path cannot be null or empty", nameof(binding));
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

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
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

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
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();
            var bindingIndex = actionMap.FindBinding(match);
            if (bindingIndex == -1)
                throw new ArgumentException($"Cannot find binding matching '{match}' in '{action}'", nameof(match));

            return new BindingSyntax(actionMap, action, bindingIndex);
        }

        ////TODO: update binding mask if necessary
        ////REVIEW: should we allow renaming singleton actions to empty/null names?
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
            if (actionMap?.TryGetAction(newName) != null)
                throw new InvalidOperationException(
                    $"Cannot rename '{action}' to '{newName}' in map '{actionMap}' as the map already contains an action with that name");

            action.m_Name = newName;
        }

        public static void Rename(this InputActionAsset asset, InputActionMap map)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a new control scheme to the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">Asset to add the control scheme to.</param>
        /// <param name="name">Name to give to the control scheme. Must be unique within the control schemes of the
        /// asset. Also used as default name of <see cref="InputControlScheme.bindingGroup">binding group</see> associated
        /// with the control scheme.</param>
        /// <returns>Syntax to allow providing additional configuration for the newly added control scheme.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is null or <paramref name="name"/>
        /// is null or empty.</exception>
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

            ////TODO: implement chained bindings and make public
            internal BindingSyntax ChainedWith(string binding, string interactions = null, string group = null)
            {
                throw new NotImplementedException();
                /*
                if (m_Action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException(
                        "Must not add other bindings in-between calling AddBindings() and ChainedWith()");

                var result = m_Action.AddBinding(binding, interactions: interactions, groups: @group);
                m_Action.m_SingletonActionBindings[m_Action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
                */
            }

            public BindingSyntax WithName(string name)
            {
                m_ActionMap.m_Bindings[m_BindingIndex].name = name;
                // No need to clear cached data.
                return this;
            }

            public BindingSyntax WithPath(string path)
            {
                m_ActionMap.m_Bindings[m_BindingIndex].path = path;
                // No need to clear cached data.
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
                    throw new ArgumentException(
                        $"Type '{typeof(TInteraction)}' has not been registered as a processor",
                        "TInteraction");

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
                    throw new ArgumentException(
                        $"Type '{typeof(TProcessor)}' has not been registered as a processor",
                        "TProcessor");

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

            ////REVIEW: do we really want to go this direction?
            internal BindingSyntax WithChild(string binding, string interactions = null, string groups = null)
            {
                /*
                var child = m_Action != null
                    ? m_Action.AddBinding(binding, interactions, groups)
                    : m_ActionMap.AddBinding(binding, interactions, groups);
                m_ActionMap.m_Bindings[child.m_BindingIndex].flags |= InputBinding.Flags.PushBindingLevel;

                return child;
                */
                throw new NotImplementedException();
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

            public CompositeSyntax With(string name, string binding, string interactions = null, string groups = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                int bindingIndex;
                if (m_Action != null)
                    bindingIndex = m_Action.AddBinding(path: binding, interactions: interactions, groups: groups)
                        .m_BindingIndex;
                else
                    bindingIndex = m_ActionMap.AddBinding(path: binding, interactions: interactions, groups: groups)
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

            public ControlSchemeSyntax BasedOn(string baseControlScheme)
            {
                if (string.IsNullOrEmpty(baseControlScheme))
                    throw new ArgumentNullException(nameof(baseControlScheme));

                if (m_Asset == null)
                    m_ControlScheme.m_BaseSchemeName = baseControlScheme;
                else
                    m_Asset.m_ControlSchemes[m_ControlSchemeIndex].m_BaseSchemeName = baseControlScheme;

                return this;
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
