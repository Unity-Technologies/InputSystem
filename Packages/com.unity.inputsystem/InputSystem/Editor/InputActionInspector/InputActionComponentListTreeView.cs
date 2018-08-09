#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class InputActionComponentListTreeView : TreeView
    {
        SerializedProperty m_ActionProperty;
        public Action<SerializedProperty> OnContextClick;
        
        public static InputActionComponentListTreeView Create(SerializedProperty actionProperty)
        {
            var treeView = new InputActionComponentListTreeView(actionProperty);
            
            ////FIXME: this requires 2018.3 to compile
            //treeView.foldoutOverride += OnFoldoutDraw;
            treeView.ExpandAll();
            return treeView;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        static bool OnFoldoutDraw(Rect position, bool expandedstate, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedstate, GUIContent.none, style);
        }

        InputActionComponentListTreeView(SerializedProperty actionProperty) : base(new TreeViewState())
        {
            useScrollView = false;
            m_ActionProperty = actionProperty;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };

            root.children = new List<TreeViewItem>();
            var bindingsArrayProperty = m_ActionProperty.FindPropertyRelative("m_SingletonActionBindings");
            CompositeGroupTreeItem compositeGroupTreeItem = null;
            
            for (var i = 0; i < bindingsArrayProperty.arraySize; i++)
            {
                var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);
                var bindingItem = new BindingTreeItem("", bindingProperty, i);

                if (bindingItem.isComposite)
                {
                    compositeGroupTreeItem = new CompositeGroupTreeItem("", bindingProperty, i);
                    root.AddChild(compositeGroupTreeItem);
                    continue;
                }
                if (bindingItem.isPartOfComposite)
                {
                    var compositeItem = new CompositeTreeItem("", bindingProperty, i);
                    compositeItem.depth = 1;
                    if (compositeGroupTreeItem != null)
                        compositeGroupTreeItem.AddChild(compositeItem);
                    continue;
                }
                
                root.AddChild(bindingItem);
            }
            
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is InputTreeViewLine)
            {
                var bindingItem = (args.item as InputTreeViewLine);
                
                // We try to predict the indentation
                var indent = (args.item.depth + 2) * 6 + 10;
                bindingItem.OnGUI(args.rowRect, args.selected, args.focused, indent);
                
                var btnRect = args.rowRect;
                btnRect.x = btnRect.width - 20;
                btnRect.width = 20;

                if (!bindingItem.hasProperties)
                    return;
                
                if (GUI.Button(btnRect, "..."))
                {
                    var screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(btnRect.x, btnRect.y));
                    btnRect.x = screenPoint.x;
                    btnRect.y = screenPoint.y;
                    BindingPropertiesPopup.Show(btnRect, bindingItem, Reload);
                }
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var element = (InputTreeViewLine)FindItem(id, rootItem);
            var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
            BindingPropertiesPopup.Show(rect, element, Reload);
        }

        protected override void ContextClicked()
        {
            OnContextClick(m_ActionProperty);
        }

        public InputTreeViewLine GetSelectedRow()
        {
            if (!HasSelection())
                return null;

            return (InputTreeViewLine)FindItem(GetSelection().First(), rootItem);
        }
    }
}
#endif // UNITY_EDITOR
