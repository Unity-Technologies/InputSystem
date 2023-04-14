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
        protected bool m_Searchable;

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

        public override string searchableName
        {
            get
            {
                // ToHumanReadableString is expensive, especially given that we build the whole tree
                // every time the control picker comes up. Build searchable names only on demand
                // to save some time.
                if (m_SearchableName == null)
                {
                    if (m_Searchable)
                        m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
                    else
                        m_SearchableName = string.Empty;
                }
                return m_SearchableName;
            }
        }

        protected InputControlDropdownItem(string name)
            : base(name) {}
    }

    // NOTE: Optional control items, unlike normal control items, are displayed with their internal control
    //       names rather that their display names. The reason is that we're looking at controls that have
    //       the same internal name in one or more derived layouts but each of those derived layouts may
    //       give the control a different display name.
    //
    //       Also, if we generate a control path for an optional binding, InputControlPath.ToHumanReadableName()
    //       not find the referenced control on the referenced device layout and will thus not be able to
    //       find a display name for it either. So, in the binding UI, these paths will also show with their
    //       internal control names rather than display names.
    internal sealed class OptionalControlDropdownItem : InputControlDropdownItem
    {
        public OptionalControlDropdownItem(EditorInputControlLayoutCache.OptionalControl optionalControl, string deviceControlId, string commonUsage)
            : base(optionalControl.name)
        {
            m_ControlPath = optionalControl.name;
            m_Device = deviceControlId;
            m_Usage = commonUsage;
            // Not searchable.
        }
    }

    internal sealed class ControlUsageDropdownItem : InputControlDropdownItem
    {
        public override string controlPathWithDevice => BuildControlPath();
        private string BuildControlPath()
        {
            if (m_Device == "*")
            {
                var path = new StringBuilder(m_Device);
                if (!string.IsNullOrEmpty(m_Usage))
                    path.Append($"{{{m_Usage}}}");
                if (!string.IsNullOrEmpty(m_ControlPath))
                    path.Append($"/{m_ControlPath}");
                return path.ToString();
            }
            else
                return base.controlPathWithDevice;
        }

        public ControlUsageDropdownItem(string device, string usage, string controlUsage)
            : base(usage)
        {
            m_Device = string.IsNullOrEmpty(device) ? "*" : device;
            m_Usage = usage;
            m_ControlPath = $"{{{ controlUsage }}}";
            name = controlUsage;
            id = controlPathWithDevice.GetHashCode();
            m_Searchable = true;
        }
    }

    internal sealed class DeviceDropdownItem : InputControlDropdownItem
    {
        public DeviceDropdownItem(InputControlLayout layout, string usage = null, bool searchable = true)
            : base(layout.m_DisplayName ?? ObjectNames.NicifyVariableName(layout.name))
        {
            m_Device = layout.name;
            m_Usage = usage;
            if (usage != null)
                name += " (" + usage + ")";
            id = name.GetHashCode();
            m_Searchable = searchable;
        }
    }

    internal sealed class ControlDropdownItem : InputControlDropdownItem
    {
        public ControlDropdownItem(ControlDropdownItem parent, string controlName, string displayName, string device, string usage, bool searchable)
            : base("")
        {
            m_Device = device;
            m_Usage = usage;
            m_Searchable = searchable;

            if (parent != null)
                m_ControlPath = $"{parent.controlPath}/{controlName}";
            else
                m_ControlPath = controlName;

            name = !string.IsNullOrEmpty(displayName) ? displayName : ObjectNames.NicifyVariableName(controlName);

            id = controlPathWithDevice.GetHashCode();
            indent = parent?.indent + 1 ?? 0;
        }
    }
}

#endif // UNITY_EDITOR
