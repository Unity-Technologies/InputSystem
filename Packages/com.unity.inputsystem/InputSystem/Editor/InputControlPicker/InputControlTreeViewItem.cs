#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor.InputControlPicker
{
    public class InputControlTreeViewItem : TreeViewItem
    {
        protected string m_ControlPath;
        protected string m_Device;
        protected string m_CommonUsage;
                
        InputControlTreeViewItem searchableElement = null;
        
        public string controlPath
        {
            get { return m_ControlPath; }
        }
        
        public virtual string controlPathWithDevice
        {
            get { return string.Format("<{0}>{1}/{2}", m_Device, m_CommonUsage, m_ControlPath); }
        }

        public virtual bool selectable
        {
            get { return false; }
        }

        public TreeViewItem GetSearchableItem()
        {
            if (searchableElement == null)
            {
                searchableElement = new InputControlTreeViewItem();
                searchableElement.m_ControlPath = m_ControlPath;
                searchableElement.m_Device = m_Device;
                if (string.IsNullOrEmpty(m_CommonUsage))
                    searchableElement.displayName = string.Format("{0} ({1})", m_ControlPath, m_Device);
                else
                    searchableElement.displayName = string.Format("{0} ({1} {2})", m_ControlPath, m_Device, m_CommonUsage);
            }

            return searchableElement;
        }
    }

    class UsageTreeViewItem : InputControlTreeViewItem
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

    class DeviceGroupTreeViewItem : InputControlTreeViewItem
    {
        public DeviceGroupTreeViewItem(InputControlLayout layout, string commonUsage)
        {
            displayName = layout.name;
            if (commonUsage != null)
                displayName += " (" + commonUsage + ")";
            id = (displayName).GetHashCode();
            depth = 1;
        }

        public DeviceGroupTreeViewItem(InputControlLayout layout):this(layout, null)
        {
        }
    }
    
    class ControlTreeViewItem : InputControlTreeViewItem
    {
        public ControlTreeViewItem(InputControlLayout.ControlItem control, string prefix, string deviceId, string commonUsage)
        {
            m_Device = deviceId;
            m_CommonUsage = commonUsage;
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
