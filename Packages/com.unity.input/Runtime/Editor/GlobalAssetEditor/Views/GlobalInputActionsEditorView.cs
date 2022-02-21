using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class GlobalInputActionsEditorView : UIToolkitView
    {
        private VisualTreeAsset m_MainEditorAsset;

        public GlobalInputActionsEditorView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_MainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                GlobalInputActionsConstants.PackagePath +
                GlobalInputActionsConstants.ResourcesPath +
                GlobalInputActionsConstants.MainEditorViewName);

            m_MainEditorAsset.CloneTree(root);
            new ActionMapsView(root, stateContainer);
            new ActionsPanelView(root, stateContainer);
            new BindingsPanelView(root, stateContainer);
            new PropertiesView(root, stateContainer);
        }

        public override void CreateUI(GlobalInputActionsEditorState state)
        {
        }
    }
}
