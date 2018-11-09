#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Event = UnityEngine.Event;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD ItemStyle";
            public static GUIStyle header = "DD HeaderStyle";
            public static GUIStyle checkMark = "DD ItemCheckmark";
            public static GUIStyle lineSeparator = "DefaultLineSeparator";
            public static GUIStyle rightArrow = "ArrowNavigationRight";
            public static GUIStyle leftArrow = "ArrowNavigationLeft";

            public static GUIStyle toolbarSearchField = "ToolbarSeachTextField";
        }

        //This should ideally match line height
        private Vector2 s_IconSize = new Vector2(13, 13);

        internal Rect m_SearchRect;
        internal Rect m_HeaderRect;

        internal virtual float searchHeight
        {
            get { return m_SearchRect.height; }
        }

        internal virtual float headerHeight
        {
            get { return m_HeaderRect.height; }
        }

        internal virtual GUIStyle lineStyle
        {
            get { return Styles.itemStyle; }
        }

        internal virtual Vector2 iconSize
        {
            get { return s_IconSize; }
        }

        internal AdvancedDropdownState state { get; set; }

        SearchField m_SearchField = new SearchField();

        internal virtual void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            var content = new GUIContent(name, icon);
            var imgTemp = content.image;
            //we need to pretend we have an icon to calculate proper width in case
            if (content.image == null)
                content.image = Texture2D.whiteTexture;
            var rect = GUILayoutUtility.GetRect(content, lineStyle, GUILayout.ExpandWidth(true));
            content.image = imgTemp;

            if (Event.current.type != EventType.Repaint)
                return;

            var imageTemp = content.image;
            if (content.image == null)
            {
                lineStyle.Draw(rect, GUIContent.none, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;
            }
            EditorGUI.BeginDisabledGroup(!enabled);
            lineStyle.Draw(rect, content, false, false, selected, selected);
            content.image = imageTemp;
            if (drawArrow)
            {
                var yOffset = (lineStyle.fixedHeight - Styles.rightArrow.fixedHeight) / 2;
                Rect arrowRect = new Rect(
                    rect.xMax - Styles.rightArrow.fixedWidth - Styles.rightArrow.margin.right,
                    rect.y + yOffset,
                    Styles.rightArrow.fixedWidth,
                    Styles.rightArrow.fixedHeight);
                Styles.rightArrow.Draw(arrowRect, false, false, false, false);
            }
            EditorGUI.EndDisabledGroup();
        }

        internal virtual void DrawLineSeparator()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;
            Color orgColor = GUI.color;
            Color tintColor = (EditorGUIUtility.isProSkin) ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        internal void DrawHeader(AdvancedDropdownItem group, Action backButtonPressed, bool hasParent)
        {
            var content = new GUIContent(group.name, group.icon);
            m_HeaderRect = GUILayoutUtility.GetRect(content, Styles.header, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                Styles.header.Draw(m_HeaderRect, content, false, false, false, false);

            // Back button
            if (hasParent)
            {
                var yOffset = (m_HeaderRect.height - Styles.leftArrow.fixedWidth) / 2;
                var arrowRect = new Rect(
                    m_HeaderRect.x + Styles.leftArrow.margin.left,
                    m_HeaderRect.y + yOffset,
                    Styles.leftArrow.fixedWidth,
                    Styles.leftArrow.fixedHeight);
                if (Event.current.type == EventType.Repaint)
                    Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && m_HeaderRect.Contains(Event.current.mousePosition))
                {
                    backButtonPressed();
                    Event.current.Use();
                }
            }
        }

        internal void DrawSearchField(bool isSearchFieldDisabled, string searchString, Action<string> searchChanged)
        {
            if (!isSearchFieldDisabled)
            {
                m_SearchField.SetFocus();
                GUIUtility.keyboardControl = m_SearchField.searchFieldControlID;
            }

            using (new EditorGUI.DisabledScope(isSearchFieldDisabled))
            {
                var newSearch = DrawSearchFieldControl(searchString);

                if (newSearch != searchString)
                {
                    searchChanged(newSearch);
                }
            }
        }

        internal virtual string DrawSearchFieldControl(string searchString)
        {
            var paddingX = 8f;
            var paddingY = 2f;
            var rect = GUILayoutUtility.GetRect(0, 0, Styles.toolbarSearchField);
            rect.x += paddingX;
            rect.y += paddingY + 1; // Add one for the border
            rect.height += Styles.toolbarSearchField.fixedHeight + paddingY * 3;
            rect.width -= paddingX * 2;
            m_SearchRect = rect;
            searchString = m_SearchField.OnToolbarGUI(m_SearchRect, searchString);
            return searchString;
        }

        internal Rect GetAnimRect(Rect position, float anim)
        {
            // Calculate rect for animated area
            var rect = new Rect(position);
            rect.x = position.x + position.width * anim;
            rect.y += searchHeight;
            rect.height -= searchHeight;
            return rect;
        }

        internal Vector2 CalculateContentSize(AdvancedDropdownDataSource dataSource)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            bool includeArrow = false;
            float arrowWidth = Styles.rightArrow.fixedWidth;

            foreach (var child in dataSource.mainTree.children)
            {
                var content = new GUIContent(child.name, child.icon);
                var a = lineStyle.CalcSize(content);
                a.x += iconSize.x + 1;

                if (maxWidth < a.x)
                {
                    maxWidth = a.x + 1;
                    includeArrow |= child.children.Any();
                }
                if (child.IsSeparator())
                {
                    maxHeight += Styles.lineSeparator.CalcHeight(content, maxWidth) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    maxHeight += lineStyle.CalcHeight(content, maxWidth);
                }
            }
            if (includeArrow)
            {
                maxWidth += arrowWidth;
            }
            return new Vector2(maxWidth, maxHeight);
        }

        internal float GetSelectionHeight(AdvancedDropdownDataSource dataSource, Rect buttonRect)
        {
            if (state.GetSelectedIndex(dataSource.mainTree) == -1)
                return 0;
            float heigth = 0;
            for (int i = 0; i < dataSource.mainTree.children.Count(); i++)
            {
                var child = dataSource.mainTree.children.ElementAt(i);
                var content = new GUIContent(child.name, child.icon);
                if (state.GetSelectedIndex(dataSource.mainTree) == i)
                {
                    var diff = (lineStyle.CalcHeight(content, 0) - buttonRect.height) / 2f;
                    return heigth + diff;
                }
                if (child.IsSeparator())
                {
                    heigth += Styles.lineSeparator.CalcHeight(content, 0) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    heigth += lineStyle.CalcHeight(content, 0);
                }
            }
            return heigth;
        }
    }
}

#endif // UNITY_EDITOR