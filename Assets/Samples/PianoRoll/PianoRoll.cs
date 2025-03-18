using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Samples.PianoRoll
{
    // TODO:
    // - Finish implementation and clean up .cs, .uss, .uxml. Needs to clear and recreate keys when properties change.
    // - Make sure piano-roll is conveniently scalable and support arbitrary selected scale or physically accurate
    //   scale on small touch screens (e.g. iPhone), which may require som logic.
    // - If needed refactor existing OnScreen code so that we can validate use with a UI Toolkit implementation like
    //   this one.
    // - For simplicity (as a first stage), consider just letting each key be a Float control and sort out note
    //   as part of binding. For that reason one can make an InputActionAsset with notes, e.g. "C0", "C#0", "D", etc,
    //   where each note/key is a float action. These actions can be bound to keyboard keys similar to a typical
    //   virtual piano roll or just mimic the one in synth example. Hence the piano should be possible to use for
    //   typing letters, e.g. 'C' note is typically 'A' I believe and then eoctave switch allows keyboard to go
    //   multiple octaves since keyboard has too few lined up keys.
    // - Consider using UI events as step one (will likely introduce a frame of latency in current setup).
    // - Consider using a UI agnostic approach in step two to validate UI agnostic on-screen controls. This would
    //   require seeing the UI framework merely as a tool for obtaining hit-testing areas and performing transformation
    //   of logical bounds.
    // - Add labels indicating octave notes below keys.
    /// <summary>
    /// A simple piano-roll that constitutes a virtual keyboard.
    /// Currently, do not support polyphonic (multi-key presses), only single note.
    /// </summary>
    [UxmlElement]
    public partial class PianoRoll : VisualElement
    {
        private const int kNotesPerOctave = 12;

        private static readonly string[] Tooltips = new string[]
        {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };

        private static readonly string[] Names = new string[]
        {
            "c", "c-sharp", "d", "d-sharp", "e", "f", "f-sharp", "g", "g-sharp", "a", "a-sharp", "b"
        };

        private const string kClear = "-";

        public const string ussClassName = "piano-roll";
        public const string ussLabelClassName = ussClassName + "__label";
        public const string ussValueLabelClassName = ussClassName + "__value-label";
        public const string ussTopContainerClassName = ussClassName + "__top-container";
        public const string ussBottomContainerClassName = ussClassName + "__bottom-container";
        public const string ussOuterContainerClassName = ussClassName + "__outer-container";
        public const string ussContainerClassName = ussClassName + "__container";
        public static readonly string LabelUssClassName = "piano-roll__label";
        public static readonly string KeyUssClassName = "piano-roll__key";
        public static readonly string WhiteKeyUssClassName = "piano-roll__white-key";
        public static readonly string BlackKeyUssClassName = "piano-roll__black-key";
        public static readonly string KeyHoverUssClassName = "piano-roll__key-hover";
        public static readonly string KeyOnUssClassName = "piano-roll__key-on";

        private Color m_BlackKeyColor;
        private Color m_WhiteKeyColor;

        private Label m_OctaveLabel;
        private Label m_OctaveValueLabel;
        private Label m_NoteLabel;

        public PianoRoll()
        {
            m_OctaveLabel = new Label($"Root: ");
            m_OctaveLabel.AddToClassList(ussLabelClassName);

            m_OctaveValueLabel = new Label(m_OctaveRoot.ToString());
            m_OctaveValueLabel.AddToClassList(ussValueLabelClassName);

            m_NoteLabel = new Label(kClear);
            m_NoteLabel.AddToClassList(ussValueLabelClassName);

            AddToClassList(ussClassName);

            CreateKeys();
        }

        private int m_OctaveRoot = 3;

        /// <summary>
        /// Specifies the octave of the left-most C key of the octaves on the piano-roll.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [UxmlAttribute]
        public int octave
        {
            get => m_OctaveRoot;
            set
            {
                if (value < minimumOctave || value > maximumOctave)
                    throw new ArgumentOutOfRangeException(nameof(value));

                m_OctaveRoot = value;
                MarkDirtyRepaint();
            }
        }

        private int m_MinimumOctave = 3;

        /// <summary>
        /// Specifies the minimum octave (inclusive) for which the piano roll octave shift is clamped.
        /// </summary>
        [UxmlAttribute]
        public int minimumOctave
        {
            get => m_MinimumOctave;
            set
            {
                m_MinimumOctave = value;
                MarkDirtyRepaint();
            }
        }

        private int m_MaximumOctave = 5;

        /// <summary>
        /// Specifies the maximum octave (inclusive) for which the piano roll octave shift is clamped.
        /// </summary>
        [UxmlAttribute]
        public int maximumOctave
        {
            get => m_MaximumOctave;
            set
            {
                m_MaximumOctave = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Returns the number of octaves on the piano-roll.
        /// </summary>
        public int octaveCount => Math.Max((m_MaximumOctave - m_MinimumOctave) + 1, 0);

        private void OnMouseDown(MouseDownEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            target?.AddToClassList(KeyOnUssClassName);
            SetNote(target?.userData);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            target?.RemoveFromClassList(KeyOnUssClassName);
            target?.AddToClassList(KeyHoverUssClassName);
            ClearNote();
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            if (target == null)
                return;

            var mouseDown = (evt.pressedButtons == 1);
            if (mouseDown)
            {
                target.AddToClassList(KeyOnUssClassName);
                SetNote(target.userData);
            }
            else
            {
                target.AddToClassList(KeyHoverUssClassName);
            }
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            VisualElement target = (VisualElement)evt.currentTarget;
            if (target == null)
                return;

            target.RemoveFromClassList(KeyOnUssClassName);
            target.RemoveFromClassList(KeyHoverUssClassName);
            ClearNote();
        }

        private void SetNote(object userData)
        {
            var pair = userData as Tuple<int, int>;
            if (pair != null)
                SetNote(pair.Item1, pair.Item2);
            else
                ClearNote();
        }

        private void SetNote(int noteIndex, int octaveIndex)
        {
            m_NoteLabel.text = Tooltips[noteIndex] + octaveIndex.ToString();

            // TODO Transform to input
        }

        private void ClearNote()
        {
            m_NoteLabel.text = "-";

            // TODO Transform to input
        }

        private bool IsWhiteKey(int noteIndexWithinOctave) => Names[noteIndexWithinOctave].Length == 1;

        private VisualElement CreateKey(int noteIndex, int octaveIndex, string ussClass)
        {
            var key = new VisualElement
            {
                name = Names[noteIndex] + octaveIndex.ToString()
            };
            key.tooltip = Tooltips[noteIndex];
            key.userData = new Tuple<int, int>(noteIndex, octaveIndex);
            key.AddToClassList("piano-roll__" + Names[noteIndex]);
            key.AddToClassList(KeyUssClassName);
            key.AddToClassList(ussClass);
            key.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            key.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            key.RegisterCallback<MouseDownEvent>(OnMouseDown);
            key.RegisterCallback<MouseUpEvent>(OnMouseUp);
            return key;
        }

        private void CreateKeys()
        {
            // TODO Only recreate if number of octaves changes, no need to recreate if shifting root octave

            var top = new VisualElement();
            top.AddToClassList(ussTopContainerClassName);

            top.Add(m_OctaveLabel);
            top.Add(m_OctaveValueLabel);
            top.Add(m_NoteLabel);

            var bottom = new VisualElement();
            bottom.AddToClassList(ussBottomContainerClassName);

            var octCount = octaveCount;
            for (var octaveIndex = 0; octaveIndex < octCount; ++octaveIndex)
            {
                var octaveContainer = new VisualElement();
                octaveContainer.AddToClassList(ussContainerClassName);

                for (var noteIndexWithinOctave = 0; noteIndexWithinOctave < kNotesPerOctave; ++noteIndexWithinOctave)
                {
                    if (IsWhiteKey(noteIndexWithinOctave))
                    {
                        var key = CreateKey(noteIndexWithinOctave, octaveIndex, WhiteKeyUssClassName);
                        octaveContainer.Add(key);
                    }
                }
                for (var noteIndexWithinOctave = 0; noteIndexWithinOctave < kNotesPerOctave; ++noteIndexWithinOctave)
                {
                    if (!IsWhiteKey(noteIndexWithinOctave))
                    {
                        var key = CreateKey(noteIndexWithinOctave, octaveIndex, BlackKeyUssClassName);
                        octaveContainer.Add(key);
                    }
                }

                bottom.Add(octaveContainer);
            }

            Add(top);
            Add(bottom);
        }
    }
}
