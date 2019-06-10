#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    internal abstract class InputControlDropdownItem : AdvancedDropdownItem
    {
        protected string m_ControlPath;
        protected string m_Device;
        protected string m_Usage;

        private InputControlDropdownItem m_SearchableElement;

        public string controlPath => m_ControlPath;

        public virtual string controlPathWithDevice
        {
            get
            {
                var path = new StringBuilder($"<{m_Device}>");
                if (!string.IsNullOrEmpty(m_Usage))
                    path.Append($"{{{m_Usage}}}");
                if (!string.IsNullOrEmpty(m_ControlPath))
                    path.Append($"/{m_ControlPath}");
                return path.ToString();
            }
        }

        public override string searchableName => m_SearchableName ?? string.Empty;

        protected InputControlDropdownItem(string name)
            : base(name) {}
    }

    internal class OptionalControlDropdownItem : InputControlDropdownItem
    {
        public OptionalControlDropdownItem(EditorInputControlLayoutCache.OptionalControl optionalLayout, string deviceControlId, string commonUsage)
            : base(optionalLayout.name)
        {
            m_ControlPath = optionalLayout.name;
            m_Device = deviceControlId;
            m_Usage = commonUsage;
            // Not searchable.
        }
    }

    internal class UsageDropdownItem : InputControlDropdownItem
    {
        public override string controlPathWithDevice => $"{m_Device}/{{{m_ControlPath}}}";

        public UsageDropdownItem(string usage)
            : base(usage)
        {
            m_Device = "*";
            m_ControlPath = usage;
            id = controlPathWithDevice.GetHashCode();
            m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }
    }

    internal class DeviceDropdownItem : InputControlDropdownItem
    {
        public DeviceDropdownItem(InputControlLayout layout, string usage = null, bool searchable = true)
            : base(layout.m_DisplayName ?? ObjectNames.NicifyVariableName(layout.name))
        {
            m_Device = layout.name;
            m_Usage = usage;
            if (usage != null)
                name += " (" + usage + ")";
            id = name.GetHashCode();
            if (searchable)
                m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }
    }

    internal class ControlDropdownItem : InputControlDropdownItem
    {
        public ControlDropdownItem(ControlDropdownItem parent, string controlName, string displayName, string device, string usage, bool searchable)
            : base("")
        {
            m_Device = device;
            m_Usage = usage;

            if (parent != null)
                m_ControlPath = $"{parent.controlPath}/{controlName}";
            else
                m_ControlPath = controlName;

            name = !string.IsNullOrEmpty(displayName) ? displayName : ObjectNames.NicifyVariableName(controlName);

            id = controlPathWithDevice.GetHashCode();
            indent = parent?.indent + 1 ?? 0;

            if (searchable)
                m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }
    }
}

#endif // UNITY_EDITOR
