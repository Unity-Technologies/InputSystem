#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputControlTreeViewItem : TreeViewItem
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

        public virtual bool selectable
        {
            get { return false; }
        }

        public TreeViewItem GetSearchableItem()
        {
            if (m_SearchableElement == null)
            {
                m_SearchableElement = new InputControlTreeViewItem();
                m_SearchableElement.m_ControlPath = m_ControlPath;
                m_SearchableElement.m_Device = m_Device;
                if (string.IsNullOrEmpty(m_Usage))
                    m_SearchableElement.displayName = string.Format("{0} ({1})", m_ControlPath, m_Device);
                else
                    m_SearchableElement.displayName = string.Format("{0} ({1} {2})", m_ControlPath, m_Device, m_Usage);
                m_SearchableElement.id = id;
            }

            return m_SearchableElement;
        }
    }

    internal class UsageTreeViewItem : InputControlTreeViewItem
    {
        public override string controlPathWithDevice
        {
            get { return string.Format("{0}/{1}", m_Device, m_ControlPath); }
        }

        public UsageTreeViewItem(KeyValuePair<string, IEnumerable<string>> usage)
        {
            m_Device = "*";
            displayName = usage.Key;
            m_ControlPath = usage.Key;
            depth = 1;
            id = (controlPathWithDevice).GetHashCode();
        }

        public override bool selectable
        {
            get { return true; }
        }
    }

    internal class DeviceTreeViewItem : InputControlTreeViewItem
    {
        public DeviceTreeViewItem(InputControlLayout layout, string commonUsage)
        {
            displayName = layout.name;
            m_Device = layout.name;
            m_Usage = commonUsage;
            if (commonUsage != null)
                displayName += " (" + commonUsage + ")";
            id = (displayName).GetHashCode();
            depth = 1;
        }

        public DeviceTreeViewItem(InputControlLayout layout)
            : this(layout, null)
        {
        }
    }

    internal class ControlTreeViewItem : InputControlTreeViewItem
    {
        public ControlTreeViewItem(InputControlLayout.ControlItem control, string prefix, string deviceId, string usage)
        {
            m_Device = deviceId;
            m_Usage = usage;
            if (!string.IsNullOrEmpty(prefix))
            {
                m_ControlPath = prefix + "/";
                displayName = prefix + "/";
            }
            m_ControlPath += control.name;
            displayName += control.name;
            id = controlPathWithDevice.GetHashCode();
        }

        public override bool selectable
        {
            get { return true; }
        }
    }
}
#endif // UNITY_EDITOR
