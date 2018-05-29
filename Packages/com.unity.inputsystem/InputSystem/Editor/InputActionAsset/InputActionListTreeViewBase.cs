using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    partial class InputActionListTreeView : TreeView
    {
        protected static class Styles
        {
            public static GUIStyle actionItemRowStyle = new GUIStyle("Label");
            public static GUIStyle actionSetItemStyle = new GUIStyle("Label");
            public static GUIStyle actionItemLabelStyle = new GUIStyle("Label");
            
            public static GUIStyle yellowRect = new GUIStyle("Label");
            public static GUIStyle orangeRect = new GUIStyle("Label");
            public static GUIStyle greenRect = new GUIStyle("Label");
            public static GUIStyle blueRect = new GUIStyle("Label");
            public static GUIStyle cyanRect = new GUIStyle("Label");
            public static GUIStyle magentaRect = new GUIStyle("Label");
            
            static Styles()
            {
                var whiteBackgroundWithBorderTexture = CreateTextureWithBorder(Color.white);
                var blueBackgroundWithBorderTexture = CreateTextureWithBorder(new Color32(62, 125, 231, 255));
                
                actionItemRowStyle.normal.background = whiteBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onFocused.background = blueBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onNormal.background = blueBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionSetItemStyle.alignment = TextAnchor.MiddleLeft;

                actionItemLabelStyle.alignment = TextAnchor.MiddleLeft;

                yellowRect.normal.background = CreateTextureWithBorder(new Color(256f/256f, 230f/256f, 148f/256f));
                yellowRect.border = new RectOffset(2, 2, 2, 2);
                orangeRect.normal.background = CreateTextureWithBorder(new Color(246f/256f, 192f/256f, 129f/256f));
                orangeRect.border = new RectOffset(2, 2, 2, 2);
                greenRect.normal.background = CreateTextureWithBorder(new Color(168/256f, 208/256f, 152/256f));
                greenRect.border = new RectOffset(2, 2, 2, 2);
                blueRect.normal.background = CreateTextureWithBorder(new Color(162/256f, 196/256f, 200/256f));
                blueRect.border = new RectOffset(2, 2, 2, 2);
                cyanRect.normal.background = CreateTextureWithBorder(Color.cyan);
                cyanRect.border = new RectOffset(2, 2, 2, 2);
                magentaRect.normal.background = CreateTextureWithBorder(Color.magenta);
                magentaRect.border = new RectOffset(2, 2, 2, 2);
            }

            static Texture2D CreateTextureWithBorder(Color innerColor)
            {
                var texture = new Texture2D(5, 5);
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        texture.SetPixel(i, j, Color.black);
                    }
                }

                for (int i = 1; i < 4; i++)
                {
                    for (int j = 1; j < 4; j++)
                    {
                        texture.SetPixel(i, j, innerColor);
                    }
                }

                texture.filterMode = FilterMode.Point;
                texture.Apply();
                return texture;
            }
        }
        
        public Action<InputTreeViewLine> OnSelectionChanged;
        
        protected Action m_ApplyAction { get; private set; }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (!HasSelection())
                return;
            
            var item = FindItem(selectedIds.First(), rootItem);
            OnSelectionChanged((InputTreeViewLine)item);
        }

        public InputTreeViewLine GetSelectedRow()
        {
            
            if (!HasSelection())
                return null;
            
            return (InputTreeViewLine) FindItem(GetSelection().First(), rootItem);
        }

        public SerializedProperty GetSelectedProperty()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);
            
            if (item == null)
                return null;
            
            return (item as InputTreeViewLine).elementProperty;
        }
        
        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 18;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is InputTreeViewLine && !(item is BindingItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;
            if(item is BindingItem)
                return;
            BeginRename(item);
            (item as InputTreeViewLine).renaming = true;
        }
        
        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItem(args.itemID, rootItem);
            if (item == null)
                return;
            
            (item as InputTreeViewLine).renaming = false;
            
            if (!args.acceptedRename)
                return;

            ////TODO: verify that name is unique; if it isn't, make it unique automatically by appending some suffix

            var actionItem = item as InputTreeViewLine;

            if (actionItem == null)
                return;
            
            var nameProperty = actionItem.elementProperty.FindPropertyRelative("m_Name");
            nameProperty.stringValue = args.newName;
            m_ApplyAction();
            
            item.displayName = args.newName;
            Reload();
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            var indent = GetContentIndent(args.item);
            if (args.item is InputTreeViewLine)
            {
                (args.item as InputTreeViewLine).OnGUI(args.rowRect, args.selected, args.focused, indent);
            }
        }

        internal abstract class InputTreeViewLine : TreeViewItem
        {
            public bool renaming;
            
            protected SerializedProperty m_SetProperty;
            protected SerializedProperty m_ElementProperty;
            protected int m_Index;
            
            public SerializedProperty setProperty
            {
                get { return m_SetProperty; }
            }

            public virtual SerializedProperty elementProperty
            {
                get { return m_SetProperty.GetArrayElementAtIndex(index); }
            }
            public int index
            {
                get { return m_Index; }
            }

            public InputTreeViewLine(SerializedProperty setProperty, int index)
            {
                m_SetProperty = setProperty;
                m_Index = index;
                depth = 1;
            }

            public void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
            {
                var rect = rowRect;
                if (Event.current.type == EventType.Repaint)
                {
                    rowRect.height += 1;
                    Styles.actionItemRowStyle.Draw(rowRect, "", false, false, selected, focused);

                    rect.x += indent;
                    rect.width -= indent + 2;
                    rect.y += 1;
                    rect.height -= 2;

                    if (!renaming)
                        Styles.actionSetItemStyle.Draw(rect, displayName, false, false, selected, focused);

                    DrawCustomRect(rowRect);
                }
            }

            protected abstract GUIStyle rectStyle { get; }
            
            public virtual void DrawCustomRect(Rect rowRect)
            {
                var boxRect = rowRect;
                boxRect.width = depth * 12;
                rectStyle.Draw(boxRect, "", false, false, false, false);
            }
        }
    }
}
