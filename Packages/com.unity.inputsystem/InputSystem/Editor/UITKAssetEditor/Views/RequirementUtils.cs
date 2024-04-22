#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Utility class to reduce code bloat required to visualize requirement failures.
    /// </summary>
    class FailureValue
    {
        private IReadOnlyList<InputActionRequirement> m_Requirements;
        private IReadOnlyList<InputActionAssetRequirementFailure> m_Failures;

        private readonly VisualElement m_WarningIcon;
        private readonly VisualElement m_DependencyIcon;
        private readonly string m_Entity;
        private readonly VisualElement m_Parent;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="parent">The parent visual element.</param>
        /// <param name="entity">A friendly-name representing the associated element.</param>
        /// <param name="warningIcon">A reference to a visual element acting as warning icon.</param>
        /// <param name="dependencyIcon">A reference to a visual element acting as dependency icon.</param>
        public FailureValue(VisualElement parent, string entity, VisualElement warningIcon, VisualElement dependencyIcon)
        {
            m_Parent = parent;
            m_Requirements = null;
            m_Failures = null;
            m_Entity = entity;
            m_WarningIcon = warningIcon;
            m_DependencyIcon = dependencyIcon;
        }

        /// <summary>
        /// Sets the requirements to be displayed.
        /// </summary>
        /// <param name="value">A list of requirements associated with this element. May be <c>null</c>.</param>
        public void SetRequirements(IReadOnlyList<InputActionRequirement> value)
        {
            if (m_DependencyIcon == null)
                return;

            var newHasRequirements = value != null && value.Count > 0;
            m_Requirements = value;
            var isVisible = !m_DependencyIcon.ClassListContains(InputActionsEditorConstants.HiddenStyleClassName);
            if (newHasRequirements)
            {
                // TODO To achieve this in a good way we need to have another object for InputActionRequirement lists providing this for us, e.g. foreach (var owner in requirements.owners) requirements.Count(owner)
                // TODO Extract all requirement owners and write message as e.g.
                // UI Toolkit Input System Integration has 1 dependency requirement on "UI" action map.
                var sb = new StringBuilder($"This {m_Entity} currently has {m_Requirements.Count} dependency requirements(s):\n");
                m_DependencyIcon.tooltip = sb.ToString();
                if (!isVisible)
                    m_DependencyIcon.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            }
            else if (isVisible)
                m_DependencyIcon.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
        }

        /// <summary>
        /// Sets the failures to be displayed.
        /// </summary>
        /// <param name="value">A list of failures associated with this element. May be <c>null</c>.</param>
        public void SetFailures(IReadOnlyList<InputActionAssetRequirementFailure> value)
        {
            if (m_WarningIcon == null)
                return;

            var newHasFailures = value != null && value.Count > 0;
            m_Failures = value;
            var isVisible = !m_WarningIcon.ClassListContains(InputActionsEditorConstants.HiddenStyleClassName);
            if (newHasFailures)
            {
                var sb = new StringBuilder($"This {m_Entity} currently has {m_Failures.Count} warning(s):\n");
                const int maxErrorsInTooltip = 3;
                for (var i = 0; i < m_Failures.Count; ++i)
                {
                    // Limit the maximum numbers of warnings that may be seen at one point in time.
                    if (i == maxErrorsInTooltip)
                    {
                        sb.Append($"\\n...");
                        break;
                    }

                    // Note that we exclude asset reference since implicit while editing an asset
                    sb.Append($"\\n- {value[i].Describe(includeAssetReference: false, includeImplication: true)}\n");
                }
                m_WarningIcon.tooltip = sb.ToString();

                if (!isVisible)
                    m_WarningIcon.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            }
            else if (isVisible)
                m_WarningIcon.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
        }
    }
}

#endif
