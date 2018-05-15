using System;
using UnityEngine.Experimental.Input.Utilities;

////TODO: support for removing bindings

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Extensions to set up <see cref="InputAction">InputActions</see> and <see cref="InputActionMap">
    /// InputActionMaps</see>.
    /// </summary>
    public static class InputActionSetupExtensions
    {
        /// <summary>
        /// Syntax to configure a binding added to an <see cref="InputAction"/> or an
        /// <see cref="InputActionMap"/>.
        /// </summary>
        public struct BindingSyntax
        {
            internal InputAction m_Action;
            internal InputActionMap m_ActionMap;
            internal int m_BindingIndex;

            internal BindingSyntax(InputActionMap map, InputAction action, int bindingIndex)
            {
                m_ActionMap = map;
                m_Action = action;
                m_BindingIndex = bindingIndex;
            }

            public BindingSyntax ChainedWith(string binding, string modifiers = null, string group = null)
            {
                throw new NotImplementedException();
                /*
                if (m_Action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException(
                        "Must not add other bindings in-between calling AddBindings() and ChainedWith()");

                var result = m_Action.AppendBinding(binding, modifiers: modifiers, groups: @group);
                m_Action.m_SingletonActionBindings[m_Action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
                */
            }

            public BindingSyntax WithGroup(string group)
            {
                if (string.IsNullOrEmpty(group))
                    throw new ArgumentException("Group name cannot be null or empty", "group");
                if (group.IndexOf(InputBinding.kGroupSeparator) != -1)
                    throw new ArgumentException(
                        string.Format("Group name cannot contain separator character '{0}'",
                            InputBinding.kGroupSeparator), "group");

                return WithGroups(group);
            }

            public BindingSyntax WithGroups(string groups)
            {
                if (string.IsNullOrEmpty(groups))
                    return this;

                // Join with existing group, if any.
                var currentGroups = m_ActionMap.m_Bindings[m_BindingIndex].groups;
                if (!string.IsNullOrEmpty(currentGroups))
                    groups = string.Join(InputBinding.kGroupSeparatorString, new[] { currentGroups, groups });

                // Set groups on binding.
                m_ActionMap.m_Bindings[m_BindingIndex].groups = groups;
                m_ActionMap.ClearPerActionCachedBindingData();

                return this;
            }

            public BindingSyntax WithModifier(string modifier)
            {
                if (string.IsNullOrEmpty(modifier))
                    throw new ArgumentException("Modifier cannot be null or empty", "group");
                if (modifier.IndexOf(InputBinding.kModifierSeparator) != -1)
                    throw new ArgumentException(
                        string.Format("Modifier string cannot contain separator character '{0}'",
                            InputBinding.kModifierSeparator), "modifier");

                return WithModifiers(modifier);
            }

            public BindingSyntax WithModifiers(string modifiers)
            {
                if (string.IsNullOrEmpty(modifiers))
                    return this;

                // Join with existing modifier string, if any.
                var currentModifiers = m_ActionMap.m_Bindings[m_BindingIndex].modifiers;
                if (!string.IsNullOrEmpty(currentModifiers))
                    modifiers = string.Join(InputBinding.kModifierSeparatorString, new[] { currentModifiers, modifiers });

                // Set modifiers on binding.
                m_ActionMap.m_Bindings[m_BindingIndex].modifiers = modifiers;
                m_ActionMap.ClearPerActionCachedBindingData();

                return this;
            }

            public BindingSyntax WithChild(string binding, string modifiers = null)
            {
                throw new NotImplementedException();
            }

            public BindingSyntax Triggering(InputAction action)
            {
                throw new NotImplementedException();
            }

            public BindingSyntax And
            {
                get { throw new NotImplementedException(); }
            }
        }

        public struct CompositeSyntax
        {
            internal InputAction m_Action;
            internal InputActionMap m_ActionMap;
            internal int m_CompositeIndex;

            internal CompositeSyntax(InputActionMap map, InputAction action, int compositeIndex)
            {
                m_Action = action;
                m_ActionMap = map;
                m_CompositeIndex = compositeIndex;
            }

            public CompositeSyntax With(string name, string binding, string modifiers = null, string groups = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                int bindingIndex;
                if (m_Action != null)
                    bindingIndex = m_Action.AppendBinding(path: binding, modifiers: modifiers, groups: groups)
                        .m_BindingIndex;
                else
                    bindingIndex = m_ActionMap.AppendBinding(path: binding, modifiers: modifiers, groups: groups)
                        .m_BindingIndex;

                m_ActionMap.m_Bindings[bindingIndex].name = name;
                m_ActionMap.m_Bindings[bindingIndex].isPartOfComposite = true;

                return this;
            }
        }

        public static InputAction AddAction(this InputActionMap map, string name, string binding = null, string modifiers = null, string groups = null)
        {
            if (map == null)
                throw new ArgumentNullException("map");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", "name");
            if (map.enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add action '{0}' to map '{1}' while it the map is enabled", name, map));
            if (map.TryGetAction(name) != null)
                throw new InvalidOperationException(
                    string.Format("Cannot add action with duplicate name '{0}' to set '{1}'", name, map.name));

            // Append action to array.
            var action = new InputAction(name);
            ArrayHelpers.Append(ref map.m_Actions, action);
            action.m_ActionMap = map;

            ////TODO: make sure we blast out existing action map state

            // Add binding, if supplied.
            if (!string.IsNullOrEmpty(binding))
                action.AppendBinding(binding, modifiers: modifiers, groups: groups);

            return action;
        }

        public static BindingSyntax AppendBinding(this InputAction action, string path, string modifiers = null, string groups = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Binding path cannot be null or empty", "path");

            return AppendBinding(action, new InputBinding
            {
                path = path,
                modifiers = modifiers,
                groups = groups
            });
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
        public static BindingSyntax AppendBinding(this InputAction action, InputBinding binding)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (string.IsNullOrEmpty(binding.path))
                throw new ArgumentException("Binding path cannot be null or empty", "binding");
            action.ThrowIfModifyingBindingsIsNotAllowed();

            Debug.Assert(action.m_Name != null || action.isSingletonAction);
            binding.action = action.m_Name;

            var actionMap = action.GetOrCreateActionMap();
            var bindingIndex = AppendBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, action, bindingIndex);
        }

        public static BindingSyntax AppendBinding(this InputActionMap actionMap, string path, string modifiers = null, string groups = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Binding path cannot be null or empty", "path");

            return AppendBinding(actionMap, new InputBinding
            {
                path = path,
                modifiers = modifiers,
                groups = groups,
            });
        }

        public static BindingSyntax AppendBinding(this InputActionMap actionMap, InputBinding binding)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            if (string.IsNullOrEmpty(binding.path))
                throw new ArgumentException("Binding path cannot be null or empty", "binding");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

            var bindingIndex = AppendBindingInternal(actionMap, binding);
            return new BindingSyntax(actionMap, null, bindingIndex);
        }

        public static CompositeSyntax AppendCompositeBinding(this InputAction action, string composite)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentException("Composite name cannot be null or empty", "composite");

            var actionMap = action.GetOrCreateActionMap();
            ////REVIEW: use 'name' instead of 'path' field here?
            var binding = new InputBinding {path = composite, flags = InputBinding.Flags.Composite};
            var bindingIndex = AppendBindingInternal(actionMap, binding);
            return new CompositeSyntax(actionMap, action, bindingIndex);
        }

        private static int AppendBindingInternal(InputActionMap map, InputBinding binding)
        {
            Debug.Assert(map != null);

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
    }
}
