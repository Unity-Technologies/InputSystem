#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Text;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Implements a default formatter for formatting <c>InputActionAssetRequirementFailure</c> instances.
    /// </summary>
    /// <remarks>
    /// Usage of this class is not thread-safe between threads.
    /// </remarks>
    sealed class DefaultInputActionRequirementFailureFormatter
    {
        private readonly bool m_IncludeAssetReference;
        private readonly bool m_IncludeImplication;
        private readonly StringBuilder m_Buffer;

        /// <summary>
        /// Creates a new <c>DefaultInputActionRequirementFailureFormatter</c>.
        /// </summary>
        /// <param name="includeAssetReference">Specifies whether to include a reference to the asset in the
        /// description of the failure.</param>
        /// <param name="includeImplication">Specifies whether to include the implication of the failed requirement
        /// in the description of the failure.</param>
        public DefaultInputActionRequirementFailureFormatter(bool includeAssetReference = true,
                                                             bool includeImplication = true)
        {
            m_IncludeAssetReference = includeAssetReference;
            m_IncludeImplication = includeImplication;
            m_Buffer = new StringBuilder(128);
        }

        /// <summary>
        /// Formats the given failure to a string based on the configuration of the formatter.
        /// </summary>
        /// <param name="failure">The failure to be formatted to string representation.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="failure"/> couldn't be converted to string by this formatter.</exception>
        public string Format(InputActionAssetRequirementFailure failure)
        {
            switch (failure.reason)
            {
                case InputActionAssetRequirementFailure.Reason.InputActionMapDoNotExist:
                    return FormatActionMapProblem(failure, "could not be found");
                case InputActionAssetRequirementFailure.Reason.InputActionDoNotExist:
                    return FormatActionProblem(failure, "could not be found");
                case InputActionAssetRequirementFailure.Reason.InputActionNotBound:
                    return FormatActionProblem(failure, "do not have any configured bindings");
                case InputActionAssetRequirementFailure.Reason.InputActionInputActionTypeMismatch:
                    return FormatActionProblem(failure, $"has 'type' set to '{nameof(InputActionType)}.{failure.inputActionType}', but '{nameof(InputActionType)}.{failure.requirement.actionType}' was expected");
                case InputActionAssetRequirementFailure.Reason.InputActionExpectedControlTypeMismatch:
                    return FormatActionProblem(failure, $"has 'expectedControlType' set to '{failure.expectedControlType}', but '{failure.requirement.expectedControlType}' was expected");
                default:
                    throw new ArgumentException(nameof(failure.reason));
            }
        }

        private static string GetAssetReference(InputActionAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? '"' + asset.name + '"' : EditorHelpers.GetHyperlink(path);
        }

        private string FormatProblem(InputActionAssetRequirementFailure failure, string reason, string message)
        {
            m_Buffer.Clear();
            m_Buffer.Append(message);
            if (m_IncludeAssetReference)
            {
                m_Buffer.Append("in asset ");
                var path = AssetDatabase.GetAssetPath(failure.asset);
                if (string.IsNullOrEmpty(path))
                {
                    m_Buffer.Append('"');
                    m_Buffer.Append(failure.asset.name);
                    m_Buffer.Append('"');
                }
                else
                {
                    m_Buffer.Append(EditorHelpers.GetHyperlink(path));
                }
                m_Buffer.Append(' ');
            }
            m_Buffer.Append(reason);
            m_Buffer.Append('.');
            if (m_IncludeImplication)
            {
                m_Buffer.Append(' ');
                m_Buffer.Append(failure.requirement.implication);
                m_Buffer.Append('.');
            }
            return m_Buffer.ToString();
        }

        private string FormatActionMapProblem(InputActionAssetRequirementFailure failure,  string reason)
        {
            return FormatProblem(failure: failure, reason: reason,
                message: $"Required {nameof(InputActionMap)} with path '{failure.requirement.actionMapName}' ");
        }

        private string FormatActionProblem(InputActionAssetRequirementFailure failure, string reason)
        {
            return FormatProblem(failure: failure, reason: reason,
                message: $"Required {nameof(InputAction)} with path '{failure.requirement.actionPath}' ");
        }
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
