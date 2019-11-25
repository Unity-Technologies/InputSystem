#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;

namespace UnityEngine.InputSystem.Editor
{
    internal class AdvancedDropdownWindow : EditorWindow
    {
        private static readonly float kBorderThickness = 1f;
        private static readonly float kRightMargin = 13f;

        private AdvancedDropdownGUI m_Gui;
        private AdvancedDropdownDataSource m_DataSource;
        private AdvancedDropdownState m_State;

        private AdvancedDropdownItem m_CurrentlyRenderedTree;

        protected AdvancedDropdownItem renderedTreeItem => m_CurrentlyRenderedTree;

        private AdvancedDropdownItem m_AnimationTree;
        private float m_NewAnimTarget;
        private long m_LastTime;
        private bool m_ScrollToSelected = true;
        private float m_InitialSelectionPosition;
        ////FIXME: looks like a bug?
        #pragma warning disable CS0649
        private Rect m_ButtonRectScreenPos;
        private Stack<AdvancedDropdownItem> m_ViewsStack = new Stack<AdvancedDropdownItem>();
        private bool m_DirtyList = true;

        private string m_Search = "";
        private bool hasSearch => !string.IsNullOrEmpty(m_Search);

        protected internal string searchString
        {
            get => m_Search;
            set
            {
                var isNewSearch = string.IsNullOrEmpty(m_Search) && !string.IsNullOrEmpty(value);
                m_Search = value;
                m_DataSource.RebuildSearch(m_Search);
                m_CurrentlyRenderedTree = m_DataSource.mainTree;
                if (hasSearch)
                {
                    m_CurrentlyRenderedTree = m_DataSource.searchTree;
                    if (isNewSearch || state.GetSelectedIndex(m_CurrentlyRenderedTree) < 0)
                        state.SetSelectedIndex(m_CurrentlyRenderedTree, 0);
                    m_ViewsStack.Clear();
                }
            }
        }

        internal bool m_ShowHeader = true;
        internal bool showHeader
        {
            get => m_ShowHeader;
            set => m_ShowHeader = value;
        }
        internal bool m_Searchable = true;
        internal bool searchable
        {
            get => m_Searchable;
            set => m_Searchable = value;
        }
        internal bool m_closeOnSelection = true;
        internal bool closeOnSelection
        {
            get => m_closeOnSelection;
            set => m_closeOnSelection = value;
        }

        protected virtual bool isSearchFieldDisabled { get; set; }

        protected bool m_SetInitialSelectionPosition = true;

        public AdvancedDropdownWindow()
        {
            m_InitialSelectionPosition = 0f;
        }

        protected virtual bool setInitialSelectionPosition => m_SetInitialSelectionPosition;

        protected internal AdvancedDropdownState state
        {
            get => m_State;
            set => m_State = value;
        }

        protected internal AdvancedDropdownGUI gui
        {
            get => m_Gui;
            set => m_Gui = value;
        }

        protected internal AdvancedDropdownDataSource dataSource
        {
            get => m_DataSource;
            set => m_DataSource = value;
        }

        public event Action<AdvancedDropdownWindow> windowClosed;
        public event Action windowDestroyed;
        public event Action<AdvancedDropdownItem> selectionChanged;

        protected virtual void OnEnable()
        {
            m_DirtyList = true;
        }

        protected virtual void OnDestroy()
        {
            // This window sets 'editingTextField = true' continuously, through EditorGUI.FocusTextInControl(),
            // for the searchfield in its AdvancedDropdownGUI so here we ensure to clean up. This fixes the issue that
            // EditorGUI.IsEditingTextField() was returning true after e.g the Add Component Menu closes
            EditorGUIUtility.editingTextField = false;
            GUIUtility.keyboardControl = 0;
            windowDestroyed?.Invoke();
        }

        public static T CreateAndInit<T>(Rect rect, AdvancedDropdownState state) where T : AdvancedDropdownWindow
        {
            var instance = CreateInstance<T>();
            instance.m_State = state;
            instance.Init(rect);
            return instance;
        }

        public void Init(Rect buttonRect)
        {
            var screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));
            m_ButtonRectScreenPos.x = screenPoint.x;
            m_ButtonRectScreenPos.y = screenPoint.y;

            if (m_State == null)
                m_State = new AdvancedDropdownState();
            if (m_DataSource == null)
                m_DataSource = new MultiLevelDataSource();
            if (m_Gui == null)
                m_Gui = new AdvancedDropdownGUI();
            m_Gui.state = m_State;
            m_Gui.Init();

            // Has to be done before calling Show / ShowWithMode
            screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));
            buttonRect.x = screenPoint.x;
            buttonRect.y = screenPoint.y;

            OnDirtyList();
            m_CurrentlyRenderedTree = hasSearch ? m_DataSource.searchTree : m_DataSource.mainTree;
            ShowAsDropDown(buttonRect, CalculateWindowSize(m_ButtonRectScreenPos, out var requiredDropdownSize));

            // If the dropdown is as height as the screen height, give it some margin
            if (position.height  < requiredDropdownSize.y)
            {
                var pos = position;
                pos.y += 5;
                pos.height -= 10;
                position = pos;
            }

            if (setInitialSelectionPosition)
            {
                m_InitialSelectionPosition = m_Gui.GetSelectionHeight(m_DataSource, buttonRect);
            }

            wantsMouseMove = true;
            SetSelectionFromState();
        }

        void SetSelectionFromState()
        {
            var selectedIndex = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
            while (selectedIndex >= 0)
            {
                var child = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
                if (child == null)
                    break;
                selectedIndex = m_State.GetSelectedIndex(child);
                if (selectedIndex < 0)
                    break;
                m_ViewsStack.Push(m_CurrentlyRenderedTree);
                m_CurrentlyRenderedTree = child;
            }
        }

        protected virtual Vector2 CalculateWindowSize(Rect buttonRect, out Vector2 requiredDropdownSize)
        {
            requiredDropdownSize = m_Gui.CalculateContentSize(m_DataSource);
            // Add 1 pixel for each border
            requiredDropdownSize.x += kBorderThickness * 2;
            requiredDropdownSize.y += kBorderThickness * 2;
            requiredDropdownSize.x += kRightMargin;

            requiredDropdownSize.y += m_Gui.searchHeight;

            if (showHeader)
            {
                requiredDropdownSize.y += m_Gui.headerHeight;
            }

            requiredDropdownSize.y = Mathf.Clamp(requiredDropdownSize.y, minSize.y, maxSize.y);

            var adjustedButtonRect = buttonRect;
            adjustedButtonRect.y = 0;
            adjustedButtonRect.height = requiredDropdownSize.y;

            // Stretch to the width of the button
            if (requiredDropdownSize.x < buttonRect.width)
            {
                requiredDropdownSize.x = buttonRect.width;
            }
            // Apply minimum size
            if (requiredDropdownSize.x < minSize.x)
            {
                requiredDropdownSize.x = minSize.x;
            }
            if (requiredDropdownSize.y < minSize.y)
            {
                requiredDropdownSize.y = minSize.y;
            }

            return requiredDropdownSize;
        }

        internal void OnGUI()
        {
            m_Gui.BeginDraw(this);

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, Styles.background);

            if (m_DirtyList)
            {
                OnDirtyList();
            }

            HandleKeyboard();
            if (searchable)
                OnGUISearch();

            if (m_NewAnimTarget != 0 && Event.current.type == EventType.Layout)
            {
                var now = DateTime.Now.Ticks;
                var deltaTime = (now - m_LastTime) / (float)TimeSpan.TicksPerSecond;
                m_LastTime = now;

                m_NewAnimTarget = Mathf.MoveTowards(m_NewAnimTarget, 0, deltaTime * 4);

                if (m_NewAnimTarget == 0)
                {
                    m_AnimationTree = null;
                }
                Repaint();
            }

            var anim = m_NewAnimTarget;
            // Smooth the animation
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            if (anim == 0)
            {
                DrawDropdown(0, m_CurrentlyRenderedTree);
            }
            else if (anim < 0)
            {
                // Go to parent
                // m_NewAnimTarget goes -1 -> 0
                DrawDropdown(anim, m_CurrentlyRenderedTree);
                DrawDropdown(anim + 1, m_AnimationTree);
            }
            else // > 0
            {
                // Go to child
                // m_NewAnimTarget 1 -> 0
                DrawDropdown(anim - 1, m_AnimationTree);
                DrawDropdown(anim, m_CurrentlyRenderedTree);
            }

            m_Gui.EndDraw(this);
        }

        public void ReloadData()
        {
            OnDirtyList();
        }

        private void OnDirtyList()
        {
            m_DirtyList = false;
            m_DataSource.ReloadData();
            if (hasSearch)
            {
                m_DataSource.RebuildSearch(searchString);
                if (state.GetSelectedIndex(m_CurrentlyRenderedTree) < 0)
                {
                    state.SetSelectedIndex(m_CurrentlyRenderedTree, 0);
                }
            }
        }

        private void OnGUISearch()
        {
            m_Gui.DrawSearchField(isSearchFieldDisabled, m_Search, (newSearch) =>
            {
                searchString = newSearch;
            });
        }

        private void HandleKeyboard()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                // Special handling when in new script panel
                if (SpecialKeyboardHandling(evt))
                {
                    return;
                }

                // Always do these
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    m_State.MoveDownSelection(m_CurrentlyRenderedTree);
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    m_State.MoveUpSelection(m_CurrentlyRenderedTree);
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    var selected = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
                    if (selected != null)
                    {
                        if (selected.children.Any())
                        {
                            GoToChild();
                        }
                        else
                        {
                            if (selectionChanged != null)
                            {
                                selectionChanged(m_State.GetSelectedChild(m_CurrentlyRenderedTree));
                            }
                            if (closeOnSelection)
                            {
                                CloseWindow();
                            }
                        }
                    }
                    evt.Use();
                }

                // Do these if we're not in search mode
                if (!hasSearch)
                {
                    if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                    {
                        GoToParent();
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.RightArrow)
                    {
                        var idx = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
                        if (idx > -1 && m_CurrentlyRenderedTree.children.ElementAt(idx).children.Any())
                        {
                            GoToChild();
                        }
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        Close();
                        evt.Use();
                    }
                }
            }
        }

        private void CloseWindow()
        {
            windowClosed?.Invoke(this);
            Close();
        }

        internal AdvancedDropdownItem GetSelectedItem()
        {
            return m_State.GetSelectedChild(m_CurrentlyRenderedTree);
        }

        protected virtual bool SpecialKeyboardHandling(Event evt)
        {
            return false;
        }

        private void DrawDropdown(float anim, AdvancedDropdownItem group)
        {
            // Start of animated area (the part that moves left and right)
            var areaPosition = new Rect(0, 0, position.width, position.height);
            // Adjust to the frame
            areaPosition.x += kBorderThickness;
            areaPosition.y += kBorderThickness;
            areaPosition.height -= kBorderThickness * 2;
            areaPosition.width -= kBorderThickness * 2;

            GUILayout.BeginArea(m_Gui.GetAnimRect(areaPosition, anim));
            // Header
            if (showHeader)
                m_Gui.DrawHeader(group, GoToParent, m_ViewsStack.Count > 0);

            DrawList(group);
            GUILayout.EndArea();
        }

        private void DrawList(AdvancedDropdownItem item)
        {
            // Start of scroll view list
            m_State.SetScrollState(item, GUILayout.BeginScrollView(m_State.GetScrollState(item), GUIStyle.none, GUI.skin.verticalScrollbar));
            EditorGUIUtility.SetIconSize(m_Gui.iconSize);
            Rect selectedRect = new Rect();
            for (var i = 0; i < item.children.Count(); i++)
            {
                var child = item.children.ElementAt(i);
                var selected = m_State.GetSelectedIndex(item) == i;

                if (child.IsSeparator())
                {
                    GUIHelpers.DrawLineSeparator(child.name);
                }
                else
                {
                    m_Gui.DrawItem(child, child.name, child.icon, child.enabled, child.children.Any(), selected, hasSearch);
                }

                var r = GUILayoutUtility.GetLastRect();
                if (selected)
                    selectedRect = r;

                // Skip input handling for the tree used for animation
                if (item != m_CurrentlyRenderedTree)
                    continue;

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if ((Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag) && child.enabled)
                {
                    if (!selected && r.Contains(Event.current.mousePosition))
                    {
                        m_State.SetSelectedIndex(item, i);
                        Event.current.Use();
                    }
                }
                if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) && child.enabled)
                {
                    m_State.SetSelectedIndex(item, i);
                    var selectedChild = m_State.GetSelectedChild(item);
                    if (selectedChild.children.Any())
                    {
                        GoToChild();
                    }
                    else
                    {
                        if (!selectedChild.IsSeparator() && selectionChanged != null)
                        {
                            selectionChanged(selectedChild);
                        }
                        if (closeOnSelection)
                        {
                            CloseWindow();
                            GUIUtility.ExitGUI();
                        }
                    }
                    Event.current.Use();
                }
            }
            EditorGUIUtility.SetIconSize(Vector2.zero);
            GUILayout.EndScrollView();

            // Scroll to selected on windows creation
            if (m_ScrollToSelected && m_InitialSelectionPosition != 0)
            {
                var diffOfPopupAboveTheButton = m_ButtonRectScreenPos.y - position.y;
                diffOfPopupAboveTheButton -= m_Gui.searchHeight + m_Gui.headerHeight;
                m_State.SetScrollState(item, new Vector2(0, m_InitialSelectionPosition - diffOfPopupAboveTheButton));
                m_ScrollToSelected = false;
                m_InitialSelectionPosition = 0;
            }
            // Scroll to show selected
            else if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > m_State.GetScrollState(item).y)
                {
                    m_State.SetScrollState(item, new Vector2(0, selectedRect.yMax - scrollRect.height));
                    Repaint();
                }
                if (selectedRect.y < m_State.GetScrollState(item).y)
                {
                    m_State.SetScrollState(item, new Vector2(0, selectedRect.y));
                    Repaint();
                }
            }
        }

        protected void GoToParent()
        {
            if (m_ViewsStack.Count == 0)
                return;
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget > 0)
                m_NewAnimTarget = -1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = -1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_ViewsStack.Pop();
        }

        private void GoToChild()
        {
            m_ViewsStack.Push(m_CurrentlyRenderedTree);
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget < 0)
                m_NewAnimTarget = 1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = 1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
        }

        [DidReloadScripts]
        private static void OnScriptReload()
        {
            CloseAllOpenWindows<AdvancedDropdownWindow>();
        }

        protected static void CloseAllOpenWindows<T>()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(T));
            foreach (var window in windows)
            {
                try
                {
                    ((EditorWindow)window).Close();
                }
                catch
                {
                    DestroyImmediate(window);
                }
            }
        }

        private static class Styles
        {
            public static readonly GUIStyle background = "grey_border";
            public static readonly GUIStyle previewHeader = new GUIStyle(EditorStyles.label).WithPadding(new RectOffset(5, 5, 1, 2));
            public static readonly GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel).WithPadding(new RectOffset(3, 5, 4, 4));
        }
    }
}

#endif // UNITY_EDITOR
