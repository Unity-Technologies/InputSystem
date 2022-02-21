using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class NameAndParametersListView : UIToolkitView
    {
        private readonly VisualElement m_Root;
        private readonly Func<GlobalInputActionsEditorState, IEnumerable<ParameterListView>> m_ParameterListViewSelector;

        public NameAndParametersListView(VisualElement root, StateContainer stateContainer,
                                         Func<GlobalInputActionsEditorState, IEnumerable<ParameterListView>> parameterListViewSelector)
            : base(stateContainer)
        {
            m_Root = root;
            m_ParameterListViewSelector = parameterListViewSelector;
            stateContainer.Bind(state => state.selectionType, CreateUI);
        }

        public override void CreateUI(GlobalInputActionsEditorState state)
        {
            m_Root.Clear();

            foreach (var parameterListView in m_ParameterListViewSelector(state))
            {
                new NameAndParametersListViewItem(m_Root, parameterListView);
            }
        }
    }

    internal class NameAndParametersListViewItem
    {
        public NameAndParametersListViewItem(VisualElement root, ParameterListView parameterListView)
        {
            var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                GlobalInputActionsConstants.PackagePath +
                GlobalInputActionsConstants.ResourcesPath +
                GlobalInputActionsConstants.NameAndParametersListViewItem);

            var container = itemTemplate.CloneTree();
            root.Add(container);

            var header = container.Q<Toggle>();

            var moveItemUpButton = new Button();
            moveItemUpButton.AddToClassList("up");
            moveItemUpButton.AddToClassList("name-and-parameters-list-foldout-button");

            var moveItemDownButton = new Button();
            moveItemDownButton.AddToClassList("down");
            moveItemDownButton.AddToClassList("name-and-parameters-list-foldout-button");

            var deleteItemButton = new Button();
            deleteItemButton.AddToClassList("delete");
            deleteItemButton.AddToClassList("name-and-parameters-list-foldout-button");

            header.Add(moveItemUpButton);
            header.Add(moveItemDownButton);
            header.Add(deleteItemButton);

            var foldout = container.Q<Foldout>("Foldout");
            foldout.text = parameterListView.name;
            parameterListView.OnDrawVisualElements(foldout);
        }
    }
}
