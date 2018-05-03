using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Extensions to modify <see cref="InputAction">InputActions</see> and <see cref="InputActionMap">
    /// InputActionSets</see> with fluent-style APIs.
    /// </summary>
    public static class InputActionSyntaxExtensions
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
                m_Action = action;
                m_ActionMap = map;
                m_BindingIndex = bindingIndex;
            }

            public BindingSyntax ChainedWith(string binding, string modifiers = null, string group = null)
            {
                if (m_Action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException(
                        "Must not add other bindings in-between calling AddBindings() and ChainedWith()");

                var result = m_Action.AppendBinding(binding, modifiers: modifiers, groups: @group);
                m_Action.m_SingletonActionBindings[m_Action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
            }

            public BindingSyntax WithChild(string binding, string modifiers = null)
            {
                throw new NotImplementedException();
            }

            public BindingSyntax Triggering(InputAction action)
            {
                throw new NotImplementedException();
            }

            public BindingSyntax RedirectingTo(InputControlScheme bindingList)
            {
                throw new NotImplementedException();
            }

            public BindingSyntax And
            {
                get { throw new NotImplementedException(); }
            }

            public BindingSyntax WithModifiers(string modifiers)
            {
                m_Action.m_SingletonActionBindings[m_Action.m_BindingsStartIndex + m_BindingIndex].modifiers = modifiers;
                return this;
            }
        }

        public struct CompositeSyntax
        {
            internal InputAction m_Action;
            internal InputActionMap m_ActionMap;
            internal int m_CompositeIndex;
            internal int m_BindingIndex;

            internal CompositeSyntax(InputActionMap map, InputAction action, int compositeIndex)
            {
                m_Action = action;
                m_ActionMap = map;
                m_CompositeIndex = compositeIndex;
                m_BindingIndex = -1;
            }

            public CompositeSyntax With(string name, string binding, string modifiers = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                var result = m_Action.AppendBinding(path: binding, modifiers: modifiers);

                var bindingIndex = m_Action.m_BindingsStartIndex + result.m_BindingIndex;
                m_Action.m_SingletonActionBindings[bindingIndex].name = name;
                m_Action.m_SingletonActionBindings[bindingIndex].isPartOfComposite = true;

                return this;
            }
        }

        ////TODO: remove binding arguments and make this return a syntax struct
        public static InputAction AddAction(this InputActionMap map, string name, string binding = null, string modifiers = null, string groups = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", "name");
            if (map.TryGetAction(name) != null)
                throw new InvalidOperationException(
                    string.Format("Cannot add action with duplicate name '{0}' to set '{1}'", name, map.name));

            var action = new InputAction(name);
            ArrayHelpers.Append(ref map.m_Actions, action);
            action.m_ActionMap = map;

            if (!string.IsNullOrEmpty(binding))
                action.AppendBinding(binding, modifiers: modifiers, groups: groups);

            return action;
        }

        public static BindingSyntax AppendBinding(this InputActionMap map, string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a new binding to the action.
        /// </summary>
        /// <param name="action">A disabled action to add the binding to.</param>
        /// <param name="path"></param>
        /// <param name="modifiers"></param>
        /// <param name="groups"></param>
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
        public static BindingSyntax AppendBinding(this InputAction action, string path, string modifiers = null,
            string groups = null)
        {
            var binding = new InputBinding {path = path, modifiers = modifiers, group = groups};
            var bindingIndex = AppendBindingInternal(action.internalMap, action, binding);
            return new BindingSyntax(action.map, action, bindingIndex);
        }

        public static CompositeSyntax AppendCompositeBinding(this InputAction action, string composite)
        {
            ////REVIEW: use 'name' instead of 'path' field here?
            var binding = new InputBinding {path = composite, flags = InputBinding.Flags.Composite};
            var bindingIndex = AppendBindingInternal(action.internalMap, action, binding);
            return new CompositeSyntax(action.map, action, bindingIndex);
        }

        private static int AppendBindingInternal(InputActionMap map, InputAction action, InputBinding binding)
        {
            Debug.Assert(map != null);

            // Set can't be enabled.
            if (map.enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add bindings to set '{0}' while the set is enabled", map));

            // Action can't be enabled.
            if (action != null && action.enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add bindings to action '{0}' while the action is enabled", action));

            // Append to bindings in set.
            var bindingIndex = ArrayHelpers.Append(ref map.m_Bindings, binding);

            // Invalidate per-action binding sets so that this gets refreshed if
            // anyone queries it.
            map.m_BindingsForEachAction = null;

            // If it's a singleton action, make sure m_Bindings is up to date just
            // in case the action gets serialized.
            if (action != null && action.isSingletonAction)
                action.m_SingletonActionBindings = map.m_Bindings;

            return bindingIndex;
        }
    }
}
