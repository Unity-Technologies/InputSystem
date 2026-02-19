#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Editor
{
    internal class MultiLevelDataSource : AdvancedDropdownDataSource
    {
        private string[] m_DisplayedOptions;
        internal string[] displayedOptions
        {
            set { m_DisplayedOptions = value; }
        }

        private string m_Label = "";
        internal string label
        {
            set { m_Label = value; }
        }

        internal MultiLevelDataSource()
        {
        }

        public MultiLevelDataSource(string[] displayOptions)
        {
            m_DisplayedOptions = displayOptions;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            var rootGroup = new AdvancedDropdownItem(m_Label);
            m_SearchableElements = new List<AdvancedDropdownItem>();

            for (int i = 0; i < m_DisplayedOptions.Length; i++)
            {
                var menuPath = m_DisplayedOptions[i];
                var paths = menuPath.Split('/');

                AdvancedDropdownItem parent = rootGroup;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];
                    if (j == paths.Length - 1)
                    {
                        var element = new MultiLevelItem(path, menuPath);
                        element.elementIndex = i;
                        parent.AddChild(element);
                        m_SearchableElements.Add(element);
                        continue;
                    }

                    var groupPathId = paths[0];
                    for (int k = 1; k <= j; k++)
                        groupPathId += "/" + paths[k];

                    var group = parent.children.SingleOrDefault(c => ((MultiLevelItem)c).stringId == groupPathId);
                    if (group == null)
                    {
                        group = new MultiLevelItem(path, groupPathId);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            return rootGroup;
        }

        class MultiLevelItem : AdvancedDropdownItem
        {
            internal string stringId;
            public MultiLevelItem(string path, string menuPath) : base(path)
            {
                stringId = menuPath;
                id = menuPath.GetHashCode();
            }

            public override string ToString()
            {
                return stringId;
            }
        }
    }
}

#endif // UNITY_EDITOR
