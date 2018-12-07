#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Editor
{
    internal abstract class InputControlTreeViewItem : AdvancedDropdownItem
    {
        protected string m_ControlPath;
        protected string m_Device;
        protected string m_Usage;

        private InputControlTreeViewItem m_SearchableElement;

        public string controlPath
        {
            get { return m_ControlPath; }
        }

        public virtual string controlPathWithDevice
        {
            get
            {
                var path = new StringBuilder(string.Format("<{0}>", m_Device));
                if (!string.IsNullOrEmpty(m_Usage))
                    path.Append(string.Format("{{{0}}}", m_Usage));
                if (!string.IsNullOrEmpty(m_ControlPath))
                    path.Append(string.Format("/{0}", m_ControlPath));
                return path.ToString();
            }
        }

        protected InputControlTreeViewItem(string name)
            : base(name) {}
    }

    internal class OptionalControlTreeViewItem : InputControlTreeViewItem
    {
        public OptionalControlTreeViewItem(EditorInputControlLayoutCache.OptionalControl optionalLayout, string deviceControlId, string commonUsage)
            : base(optionalLayout.name)
        {
            m_ControlPath = optionalLayout.name;
            m_Device = deviceControlId;
            m_Usage = commonUsage;
        }
    }

    internal class UsageTreeViewItem : InputControlTreeViewItem
    {
        public override string controlPathWithDevice
        {
            get { return string.Format("{0}/{{{1}}}", m_Device, m_ControlPath); }
        }

        public UsageTreeViewItem(KeyValuePair<string, IEnumerable<string>> usage) : base(usage.Key)
        {
            m_Device = "*";
            m_ControlPath = usage.Key;
            id = controlPathWithDevice.GetHashCode();
            m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }
    }

    internal class DeviceTreeViewItem : InputControlTreeViewItem
    {
        public DeviceTreeViewItem(InputControlLayout layout, string commonUsage) : base(layout.name)
        {
            m_Device = layout.name;
            m_Usage = commonUsage;
            if (commonUsage != null)
                name += " (" + commonUsage + ")";
            id = name.GetHashCode();
            m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }

        public DeviceTreeViewItem(InputControlLayout layout)
            : this(layout, null)
        {
        }
    }

    internal class ControlTreeViewItem : InputControlTreeViewItem
    {
        public ControlTreeViewItem(InputControlLayout.ControlItem control, string prefix, string deviceId, string usage)  : base("")
        {
            m_Device = deviceId;
            m_Usage = usage;
            if (!string.IsNullOrEmpty(prefix))
            {
                m_ControlPath = prefix + "/";
                name = prefix + "/";
            }
            m_ControlPath += control.name;
            name += control.name;
            id = controlPathWithDevice.GetHashCode();
            m_SearchableName = InputControlPath.ToHumanReadableString(controlPathWithDevice);
        }
    }
}

#endif // UNITY_EDITOR
