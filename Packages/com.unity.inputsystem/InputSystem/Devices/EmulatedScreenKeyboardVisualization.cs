using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem
{
    class EmulatedScreenKeyboardVisualization : MonoBehaviour
    {
        enum ShiftState
        {
            Disabled,
            EnableForOneLetter,
            Enable
        }

        enum LayoutState
        {
            Letters,
            Symbols
        }

        static class Styles
        {
            internal static float ButtonHeight = 70;
            internal static float InputFieldHeight = 40;
            internal static GUIStyle ButtonStyle = new GUIStyle("button") { fontSize = 30, richText = true};
            internal static GUIStyle InputFieldStyle = new GUIStyle("textArea") { fontSize = 30, richText = true };
        }

        private class KeyboardKey
        {
            protected EmulatedScreenKeyboardVisualization m_Parent;

            private string m_DisplayName;

            public string DisplayName
            {
                set => m_DisplayName = value;

                get
                {
                    return m_Parent.m_ShiftKey == null || m_Parent.m_ShiftKey.State == ShiftState.Disabled && m_DisplayName.Length == 1
                        ? m_DisplayName.ToLowerInvariant() : m_DisplayName.ToUpperInvariant();
                }
            }

            public GUILayoutOption[] GUIOptions { set; get; }

            internal KeyboardKey(EmulatedScreenKeyboardVisualization parent)
            {
                m_Parent = parent;
                GUIOptions = new GUILayoutOption[] { GUILayout.Height(70), GUILayout.MinWidth(Styles.ButtonHeight) };
            }

            internal virtual bool DoGUI()
            {
                if (GUILayout.Button(DisplayName, Styles.ButtonStyle, GUIOptions))
                {
                    m_Parent.OnKey(this);
                    return true;
                }

                return false;
            }
        }

        private class ShiftKey : KeyboardKey
        {
            public ShiftState State { set; get; }

            internal ShiftKey(EmulatedScreenKeyboardVisualization parent) : base(parent) {}

            internal override bool DoGUI()
            {
                var name = "Shift";
                switch (State)
                {
                    case ShiftState.EnableForOneLetter:
                        name = "<i>Shift</i>";
                        break;
                    case ShiftState.Enable:
                        name = "<b>Shift</b>";
                        break;
                }

                if (GUILayout.Button(name, Styles.ButtonStyle, GUIOptions))
                {
                    if (State == ShiftState.Disabled) State = ShiftState.EnableForOneLetter;
                    else if (State == ShiftState.EnableForOneLetter) State = ShiftState.Enable;
                    else if (State == ShiftState.Enable) State = ShiftState.Disabled;
                    return true;
                }

                return false;
            }
        }

        private class LayoutKey : KeyboardKey
        {
            public LayoutState State { set; get; }

            internal LayoutKey(EmulatedScreenKeyboardVisualization parent) : base(parent) {}

            internal override bool DoGUI()
            {
                var name = State == LayoutState.Symbols ? "/123" : "ABC";
                if (GUILayout.Button(name, Styles.ButtonStyle, GUIOptions))
                {
                    State = State == LayoutState.Symbols ? LayoutState.Letters : LayoutState.Symbols;

                    var lines = m_Parent.m_KeyboaradLines;
                    lines.Clear();

                    if (State == LayoutState.Letters)
                        lines.AddRange(m_Parent.m_LetterLines);
                    else if (State == LayoutState.Symbols)
                        lines.AddRange(m_Parent.m_SymbolLines);
                    lines.Add(m_Parent.m_BottomLine);
                    return true;
                }
                return false;
            }
        }

        private class BackspaceKey : KeyboardKey
        {
            internal BackspaceKey(EmulatedScreenKeyboardVisualization parent) : base(parent) {}

            internal override bool DoGUI()
            {
                if (GUILayout.Button("Backspace", Styles.ButtonStyle, GUIOptions))
                {
                    m_Parent.OnKey(this);
                    return true;
                }
                return false;
            }
        }

        private class OkKey : KeyboardKey
        {
            internal OkKey(EmulatedScreenKeyboardVisualization parent) : base(parent) {}

            internal override bool DoGUI()
            {
                if (GUILayout.Button("Ok", Styles.ButtonStyle, GUIOptions))
                {
                    m_Parent.m_ScreenKeyboard.Hide();
                    return true;
                }
                return false;
            }
        }


        private class LettersLine
        {
            public KeyboardKey[] Letters { get; internal set; }
        }

        private ShiftKey m_ShiftKey;
        private LayoutKey m_LayoutKey;
        private float m_KeyboardHeightOffset;
        private List<LettersLine> m_Lines;
        private ScreenKeyboardShowParams m_ShowParams;
        /// <summary>
        /// Called when selection is changed via UI means, for ex., clicking on input field via mouse
        /// </summary>
        private Action<RangeInt> m_ReportSelectionChange;
        private EmulatedScreenKeyboard m_ScreenKeyboard;


        private LettersLine[] m_LetterLines;
        private LettersLine[] m_SymbolLines;
        private LettersLine m_BottomLine;

        private List<LettersLine> m_KeyboaradLines;
        private float m_KeyboardHeight;
        private bool m_ResetSelection;
        private List<KeyboardKey> m_KeyQueue = new List<KeyboardKey>();

        internal void SetCallbacks(EmulatedScreenKeyboard screenKeyboard, Action<RangeInt> reportSelectionChange)
        {
            m_ScreenKeyboard = screenKeyboard;
            m_ReportSelectionChange = reportSelectionChange;
        }

        public void Show(ScreenKeyboardShowParams showParams)
        {
            m_ShowParams = showParams;
            m_ShiftKey = null;
            m_LayoutKey = null;
            m_LetterLines = null;
            m_SymbolLines = null;
            m_BottomLine = null;

            m_KeyQueue.Clear();
            m_KeyboaradLines = new List<LettersLine>();
            switch (m_ShowParams.type)
            {
                case ScreenKeyboardType.Default:
                case ScreenKeyboardType.ASCIICapable:
                case ScreenKeyboardType.URL:
                case ScreenKeyboardType.NamePhonePad:
                case ScreenKeyboardType.EmailAddress:
                case ScreenKeyboardType.Social:
                case ScreenKeyboardType.Search:
                {
                    m_ShiftKey = new ShiftKey(this);
                    m_LayoutKey = new LayoutKey(this);
                    var backspace = new BackspaceKey(this);
                    m_LetterLines = new LettersLine[]
                    {
                        new LettersLine() {
                            Letters = new[] {"q", "w", "e", "r", "t", "y", "u", "i", "o", "p"}
                                .Select(c => new KeyboardKey(this){DisplayName = c}).ToArray()
                        },
                        new LettersLine() {
                            Letters = new[] {"a", "s", "d", "f", "g", "h", "j", "k", "l"}
                                .Select(c => new KeyboardKey(this){DisplayName = c}).ToArray()
                        },
                        new LettersLine() {
                            Letters = new[] { m_ShiftKey }
                                .Concat(new[] {"z", "x", "c", "v", "b", "n", "m"}
                                .Select(c => new KeyboardKey(this){DisplayName = c})
                                .Concat(new[] {backspace}))
                                .ToArray()
                        },
                    };

                    m_SymbolLines = new LettersLine[]
                    {
                        new LettersLine() {
                            Letters = new[] {"1", "2", "3", "4", "5", "6", "7", "8", "9", "0"}
                                .Select(c => new KeyboardKey(this){DisplayName = c}).ToArray()
                        },
                        new LettersLine() {
                            Letters = new[] {"@", "#", "$", "_", "&", "-", "+", "(", ")", "/"}
                                .Select(c => new KeyboardKey(this){DisplayName = c}).ToArray()
                        },
                        new LettersLine() {
                            Letters = new[] {"*", "\"", "'", ":", ";", "!", "?"}
                                .Select(c => new KeyboardKey(this){DisplayName = c})
                                .Concat(new[] {new BackspaceKey(this)})
                                .ToArray()
                        },
                    };

                    m_BottomLine = new LettersLine()
                    {
                        Letters = new[] { m_LayoutKey }.Concat(new[] { ",", "SPACE", "." }
                            .Select(c => new KeyboardKey(this) { DisplayName = c })
                            .Concat(new[] {new OkKey(this)})
                            ).ToArray()
                    };

                    m_KeyboaradLines.AddRange(m_LetterLines);
                    m_KeyboaradLines.Add(m_BottomLine);
                }
                break;
                case ScreenKeyboardType.NumbersAndPunctuation:
                case ScreenKeyboardType.NumberPad:
                case ScreenKeyboardType.PhonePad:
                    var options = new GUILayoutOption[] {GUILayout.MinWidth(Screen.width / 5), GUILayout.Height(Styles.ButtonHeight)};
                    m_KeyboaradLines.AddRange(new[]
                    {
                        new LettersLine(){ Letters = new[] {"1", "2", "3", "_"}.Select(c => new KeyboardKey(this){DisplayName = c, GUIOptions = options}).ToArray() },
                        new LettersLine(){ Letters = new[] {"4", "5", "6", "SPACE"}.Select(c => new KeyboardKey(this){DisplayName = c, GUIOptions = options}).ToArray() },
                        new LettersLine(){
                            Letters = new[] {"7", "8", "9", }
                                .Select(c => new KeyboardKey(this){DisplayName = c, GUIOptions = options})
                                .Concat(new[] {new BackspaceKey(this){GUIOptions = options}})
                                .ToArray()
                        },
                        new LettersLine(){
                            Letters = new[] {",", "0", ".", "SPACE"}
                                .Select(c => new KeyboardKey(this){DisplayName = c, GUIOptions = options})
                                .Concat(new[] {new OkKey(this){GUIOptions = options } })
                                .ToArray()
                        }
                    });
                    break;
            }

            m_KeyboardHeight = m_KeyboaradLines.Count * Styles.ButtonHeight;
            if (!m_ShowParams.inputFieldHidden)
                m_KeyboardHeight += Styles.InputFieldHeight;
            m_ResetSelection = true;
        }

        private void Update()
        {
            float velocity = Time.deltaTime * 3000.0f;
            if (m_ScreenKeyboard.state == ScreenKeyboardState.Visible)
                m_KeyboardHeightOffset = Mathf.Min(m_KeyboardHeightOffset + velocity, m_KeyboardHeight);
            else
                m_KeyboardHeightOffset = Mathf.Max(m_KeyboardHeightOffset - velocity, 0.0f);
        }

        private void OnKey(KeyboardKey key)
        {
            m_KeyQueue.Add(key);
        }

        private void ProcessShiftAfterKey(KeyboardKey key)
        {
            if (m_ShiftKey != null && m_ShiftKey.State == ShiftState.EnableForOneLetter && !(key is ShiftKey))
                m_ShiftKey.State = ShiftState.Disabled;
        }

        private bool DoLineGUI(LettersLine line)
        {
            bool result = false;
            GUILayout.BeginHorizontal();
            foreach (var letter in line.Letters)
            {
                result = letter.DoGUI();
                if (result)
                    break;
            }
            GUILayout.EndHorizontal();
            return result;
        }

        private void DoInputFieldGUI()
        {
            var inputfieldName = "FakeInputField";
            if (m_ScreenKeyboard.state == ScreenKeyboardState.Visible)
                GUI.FocusControl(inputfieldName);

            GUI.SetNextControlName(inputfieldName);
            m_ScreenKeyboard.inputFieldText = GUILayout.TextArea(m_ScreenKeyboard.inputFieldText, Styles.InputFieldStyle, GUILayout.Height(Styles.InputFieldHeight));
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

            var type = Event.current.type;
            if (te != null)
            {
                if (type == EventType.Repaint && m_ResetSelection)
                {
                    te.MoveTextEnd();
                    m_ResetSelection = false;
                }

                var b = te.selectIndex;
                var e = te.cursorIndex;
                if (e < b)
                {
                    var tmp = e;
                    e = b;
                    b = tmp;
                }

                m_ReportSelectionChange.Invoke(new RangeInt(b, e - b));


                if (m_KeyQueue.Count > 0)
                {
                    for (int i = 0; i < m_KeyQueue.Count; i++)
                    {
                        var key = m_KeyQueue[i];
                        if (key is BackspaceKey)
                            te.Backspace();
                        else if (key.DisplayName.Length == 1)
                            te.Insert(key.DisplayName[0]);

                        ProcessShiftAfterKey(key);
                    }


                    m_ScreenKeyboard.inputFieldText = te.text;
                    m_KeyQueue.Clear();
                }
            }
        }

        private void DoHiddenInputFieldGUI()
        {
            if (m_KeyQueue.Count > 0)
            {
                for (int i = 0; i < m_KeyQueue.Count; i++)
                {
                    var key = m_KeyQueue[i];
                    var text = m_ScreenKeyboard.inputFieldText;
                    var selection = m_ScreenKeyboard.selection;
                    if (key is BackspaceKey)
                    {
                        if (selection.length > 0)
                            m_ScreenKeyboard.inputFieldText = text.Remove(selection.start, selection.length);
                        else if (text.Length > 0)
                            m_ScreenKeyboard.inputFieldText = text.Substring(0, text.Length - 1);
                    }
                    else if (key.DisplayName.Length == 1)
                    {
                        text = text.Remove(selection.start, selection.length);
                        m_ScreenKeyboard.inputFieldText = text.Insert(selection.start, key.DisplayName);
                    }

                    ProcessShiftAfterKey(key);
                }
                m_KeyQueue.Clear();
            }
        }

        public void OnGUI()
        {
            if (m_KeyboaradLines == null)
                return;

            var rc = area;
            GUI.Box(rc, GUIContent.none);
            GUILayout.BeginArea(rc);
            GUILayout.BeginVertical(GUILayout.Height(Screen.height), GUILayout.Width(Screen.width));
            if (m_ShowParams.inputFieldHidden)
                DoHiddenInputFieldGUI();
            else
                DoInputFieldGUI();

            foreach (var line in m_KeyboaradLines)
            {
                if (DoLineGUI(line))
                    break;
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public Rect area
        {
            get
            {
                return new Rect(0, Screen.height - m_KeyboardHeightOffset, Screen.width, m_KeyboardHeight);
            }
        }
    }
}
