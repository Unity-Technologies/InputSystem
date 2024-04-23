#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Utility class to reduce code bloat required to visualize dependency requirements or dependency requirement failures.
    /// </summary>
    sealed class InputActionDependency
    {
        private readonly VisualElement m_DependencyIcon;
        private readonly string m_Entity;
        private readonly VisualElement m_Parent;
        private readonly DependencyType m_Type;

        public enum DependencyType
        {
            None,
            ActionMap,
            Action
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="parent">The parent visual element.</param>
        /// <param name="entity">A friendly-name representing the associated element.</param>
        /// <param name="type"></param>
        /// <param name="dependencyIcon">A reference to a visual element acting as dependency icon.</param>
        public InputActionDependency(VisualElement parent, string entity, DependencyType type, VisualElement dependencyIcon)
        {
            m_Parent = parent;
            m_Entity = entity;
            m_DependencyIcon = dependencyIcon;
            m_Type = type;
        }

        public InputActionDependency(VisualElement parent, string entity, DependencyType type)
            : this(parent, entity, type, parent.Q<VisualElement>("dependency-icon"))
        {}

        public void Update(string actionPath, IEnumerable<InputActionAssetRequirements> requirements,
            IReadOnlyList<InputActionAssetRequirementFailure> failures)
        {
            // If we do not have any visual representation we cannot do much
            if (m_DependencyIcon == null)
                return;

            const char bullet = '\u2022';
            StringBuilder message = null;

            // Handle failures first since if failures are present we show them instead of requirements
            var hasFailures = failures != null && failures.Count > 0;
            if (hasFailures)
            {
                message = new StringBuilder($"This {m_Entity} currently has {failures.Count} warning");
                if (failures.Count > 1)
                    message.Append('s');
                message.Append(":\n");
                const int maxErrorsInTooltip = 3;
                for (var i = 0; i < failures.Count; ++i)
                {
                    if (i > 0)
                        message.Append('\n');
                    if (i == maxErrorsInTooltip)
                    {
                        message.Append($"...");
                        break;
                    }

                    // Note that we exclude asset reference since implicit while editing an asset.
                    // We also exclude implication since this is shown in header warning box.
                    message.Append($"{bullet} {failures[i].Describe(includeAssetReference: false, includeImplication: false)}");
                }
            }

            // In case no failures are present, show requirements (if present)
            if (!hasFailures && requirements != null)
            {
                int reqsCount = 0;
                StringBuilder reqs = null;
                foreach (var requirementSet in requirements)
                {
                    reqs ??= new StringBuilder();
                    foreach (var requirement in requirementSet.EnumerateRequirement(actionPath)) // TODO Need filter
                    {
                        if (reqs.Length > 0)
                            reqs.Append('\n');
                        reqs.Append(m_Type == DependencyType.ActionMap
                            ? $"{bullet} Action named \"{requirement.actionPath}\" need to exist."
                            : $"{bullet} Action named \"{requirement.actionPath}\" with 'Action Type' set to '{requirement.actionType}' and 'Control Type' set to '{requirement.expectedControlType}' need to exist.");
                        ++reqsCount;
                    }

                    message ??= new StringBuilder();
                    message.Append($"<b>\"{requirementSet.owner}\"</b> has {reqsCount} dependency requirement");
                    if (reqsCount > 1)
                        message.Append('s');
                    message.Append($" on this {m_Entity}:\n");
                    message.Append(reqs);
                }
            }

            // Update tooltip based on either requirements or failures text
            m_DependencyIcon.tooltip = message?.ToString();

            // Change icon
            const string dependencyWarningIconClass = "warning-icon";
            const string dependencyIconClass = "dependency-icon";
            var hasRequirementsOrFailures = message != null;
            if (hasFailures && !m_DependencyIcon.ClassListContains(dependencyWarningIconClass))
            {
                m_DependencyIcon.RemoveFromClassList(dependencyIconClass);
                m_DependencyIcon.AddToClassList(dependencyWarningIconClass);
            }
            else if (hasRequirementsOrFailures && !m_DependencyIcon.ClassListContains(dependencyIconClass))
            {
                m_DependencyIcon.RemoveFromClassList(dependencyWarningIconClass);
                m_DependencyIcon.AddToClassList(dependencyIconClass);
            }

            // Change icon visibility
            var isCurrentlyVisible = !m_DependencyIcon.ClassListContains(InputActionsEditorConstants.HiddenStyleClassName);
            if (!isCurrentlyVisible && hasRequirementsOrFailures)
                m_DependencyIcon.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            else if (isCurrentlyVisible && !hasRequirementsOrFailures)
                m_DependencyIcon.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
        }
    }
}

#endif
