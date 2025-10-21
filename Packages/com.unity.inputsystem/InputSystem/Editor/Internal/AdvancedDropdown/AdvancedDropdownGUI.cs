#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.InputSystem.Editor
{
    internal class AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static readonly GUIStyle toolbarSearchField = EditorStyles.toolbarSearchField;
            public static readonly GUIStyle itemStyle = new GUIStyle("PR Label")
                .WithAlignment(TextAnchor.MiddleLeft)
                .WithPadding(new RectOffset())
                .WithMargin(new RectOffset())
                .WithFixedHeight(17);
            public static readonly GUIStyle richTextItemStyle = new GUIStyle("PR Label")
                .WithAlignment(TextAnchor.MiddleLeft)
                .WithPadding(new RectOffset())
                .WithMargin(new RectOffset())
                .WithFixedHeight(17)
                .WithRichText();
            public static readonly GUIStyle header = new GUIStyle("In BigTitle")
                .WithFont(EditorStyles.boldLabel.font)
                .WithMargin(new RectOffset())
                .WithBorder(new RectOffset(0, 0, 3, 3))
                .WithPadding(new RectOffset(6, 6, 6, 6))
                .WithContentOffset(Vector2.zero);
            public static readonly GUIStyle headerArrow = new GUIStyle()
                .WithAlignment(TextAnchor.MiddleCenter)
                .WithFontSize(20)
                .WithNormalTextColor(Color.gray);
            public static readonly GUIStyle checkMark = new GUIStyle("PR Label")
                .WithAlignment(TextAnchor.MiddleCenter)
                .WithPadding(new RectOffset())
                .WithMargin(new RectOffset())
                .WithFixedHeight(17);
            public static readonly GUIContent arrowRightContent = new GUIContent("▸");
            public static readonly GUIContent arrowLeftContent = new GUIContent("◂");
        }

        //This should ideally match line height
        private static readonly Vector2 s_IconSize = new Vector2(13, 13);

        internal Rect m_SearchRect;
        internal Rect m_HeaderRect;
        private bool m_FocusSet;

        internal virtual float searchHeight => m_SearchRect.height;

        internal virtual float headerHeight => m_HeaderRect.height;

        internal virtual GUIStyle lineStyle => Styles.itemStyle;
        internal virtual GUIStyle richTextLineStyle => Styles.richTextItemStyle;
        internal GUIStyle headerStyle => Styles.header;

        internal virtual Vector2 iconSize => s_IconSize;

        internal AdvancedDropdownState state { get; set; }

        private readonly SearchField m_SearchField = new SearchField();

        public void Init()
        {
            m_FocusSet = false;
        }

        private const float k_IndentPerLevel = 20f;

        internal virtual void BeginDraw(EditorWindow window)
        {
        }

        internal virtual void EndDraw(EditorWindow window)
        {
        }

        internal virtual void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled,
            bool drawArrow, bool selected, bool hasSearch, bool richText = false)
        {
            var content = new GUIContent(name, icon);
            var imgTemp = content.image;
            //we need to pretend we have an icon to calculate proper width in case
            if (content.image == null)
                content.image = Texture2D.whiteTexture;
            var style = richText ? richTextLineStyle : lineStyle;
            var rect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true));
            content.image = imgTemp;

            if (Event.current.type != EventType.Repaint)
                return;

            style.Draw(rect, GUIContent.none, false, false, selected, selected);
            if (!hasSearch)
            {
                rect.x += item.indent * k_IndentPerLevel;
                rect.width -= item.indent * k_IndentPerLevel;
            }

            var imageTemp = content.image;
            if (content.image == null)
            {
                style.Draw(rect, GUIContent.none, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;
            }
            rect.x += EditorGUIUtility.standardVerticalSpacing;
            rect.width -= EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginDisabledGroup(!enabled);
            style.Draw(rect, content, false, false, selected, selected);
            content.image = imageTemp;
            if (drawArrow)
            {
                var size = style.lineHeight;
                var arrowRect = new Rect(rect.x + rect.width - size, rect.y, size, size);
                style.Draw(arrowRect, Styles.arrowRightContent, false, false, false, false);
            }
            EditorGUI.EndDisabledGroup();
        }

        internal virtual void DrawHeader(AdvancedDropdownItem group, Action backButtonPressed, bool hasParent)
        {
            var content = new GUIContent(group.name, group.icon);
            m_HeaderRect = GUILayoutUtility.GetRect(content, Styles.header, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                Styles.header.Draw(m_HeaderRect, content, false, false, false, false);

            // Back button
            if (hasParent)
            {
                var arrowWidth = 13;
                var arrowRect = new Rect(m_HeaderRect.x, m_HeaderRect.y, arrowWidth, m_HeaderRect.height);
                if (Event.current.type == EventType.Repaint)
                    Styles.headerArrow.Draw(arrowRect, Styles.arrowLeftContent, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && m_HeaderRect.Contains(Event.current.mousePosition))
                {
                    backButtonPressed();
                    Event.current.Use();
                }
            }
        }

        internal virtual void DrawFooter(AdvancedDropdownItem selectedItem)
        {
        }

        internal void DrawSearchField(bool isSearchFieldDisabled, string searchString, Action<string> searchChanged)
        {
            if (!isSearchFieldDisabled && !m_FocusSet)
            {
                m_FocusSet = true;
                m_SearchField.SetFocus();
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
            //rect.x += paddingX;
            rect.y += paddingY + 1; // Add one for the border
            rect.height += Styles.toolbarSearchField.fixedHeight + paddingY * 3;
            rect.width -= paddingX;// * 2;
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
            var maxWidth = 0f;
            var maxHeight = 0f;
            var includeArrow = false;
            var arrowWidth = 0f;

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
                    maxHeight += GUIHelpers.Styles.lineSeparator.CalcHeight(content, maxWidth) + GUIHelpers.Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    maxHeight += lineStyle.CalcHeight(content, maxWidth);
                }
                if (arrowWidth == 0)
                {
                    lineStyle.CalcMinMaxWidth(Styles.arrowRightContent, out arrowWidth, out arrowWidth);
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
            var height = 0f;
            for (var i = 0; i < dataSource.mainTree.children.Count(); i++)
            {
                var child = dataSource.mainTree.children.ElementAt(i);
                var content = new GUIContent(child.name, child.icon);
                if (state.GetSelectedIndex(dataSource.mainTree) == i)
                {
                    var diff = (lineStyle.CalcHeight(content, 0) - buttonRect.height) / 2f;
                    return height + diff;
                }
                if (child.IsSeparator())
                {
                    height += GUIHelpers.Styles.lineSeparator.CalcHeight(content, 0) + GUIHelpers.Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    height += lineStyle.CalcHeight(content, 0);
                }
            }
            return height;
        }
    }
}

#endif // UNITY_EDITOR
