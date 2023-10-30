using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.IMEInput
{
    /// <summary>
    /// An example IME Input Handler showing how IME input can be handled using
    /// input system provided events.
    /// </summary>
    /// <seealso cref="Keyboard.OnIMECompositionChanged"/>
    public class IMEInputHandler : MonoBehaviour
    {
        // Handles text passed via Keyboard.onTextInput event
        private void OnTextInput(char character)
        {
            AssertReferencesAreValid();

            // Assumes the current IME composition text has been submitted
            if (m_ComposingViaIME)
            {
                m_CompositionField.text = string.Empty;
                m_ComposingViaIME = false;
            }

            m_CurrentInput += character;
            m_TextField.text = m_CurrentInput;
            m_CombinedField.text = m_CurrentInput;
        }

        // Handles text passed via Keyboard.onIMECompositionChange event
        private void OnIMECompositionChange(IMECompositionString text)
        {
            AssertReferencesAreValid();

            // IME composition strings without length can also mean
            // the composition has been submitted
            if (text.Count == 0)
            {
                m_ComposingViaIME = false;
                m_CompositionField.text = string.Empty;
                return;
            }

            var compositionText = text.ToString();
            m_ComposingViaIME = true;
            m_CompositionField.text = compositionText;

            // The combined text contains both the current input and the current status of the composition
            m_CombinedField.text = string.Format("{0}{1}", m_CurrentInput, compositionText);
        }

        // Adds keyboard and input field listeners
        private void OnEnable()
        {
            if (m_EventListenersAdded)
                return;

            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard is null)
                return;

            keyboard.onTextInput += OnTextInput;
            keyboard.onIMECompositionChange += OnIMECompositionChange;
            m_InputField.onValueChanged.AddListener(OnValueChanged);

            m_EventListenersAdded = true;
        }

        // Removes keyboard and input field listeners
        private void OnDisable()
        {
            if (!m_EventListenersAdded)
                return;

            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard is null)
                return;

            keyboard.onTextInput -= OnTextInput;
            keyboard.onIMECompositionChange -= OnIMECompositionChange;
            m_InputField.onValueChanged.RemoveListener(OnValueChanged);

            Clear();

            m_EventListenersAdded = false;
        }

        // Called when the input field's text is changed
        private void OnValueChanged(string value)
        {
            AssertReferencesAreValid();

            if (!string.IsNullOrEmpty(value))
                return;

            Clear();
        }

        // Clears the text from all of the fields
        private void Clear()
        {
            m_CompositionField.text = string.Empty;
            m_TextField.text = string.Empty;
            m_CombinedField.text = string.Empty;

            m_CurrentInput = string.Empty;
            m_ComposingViaIME = false;
        }

        // Ensures all fields are correctly referenced
        private void AssertReferencesAreValid()
        {
            Debug.Assert(m_CompositionField != null, "Composition field cannot be null");
            Debug.Assert(m_TextField != null, "Text field field cannot be null");
            Debug.Assert(m_CombinedField != null, "Combined field field cannot be null");
            Debug.Assert(m_InputField != null, "Input field field cannot be null");
        }

        [Tooltip("Text field intended to display the string received via OnIMECompositionChanged")]
        [SerializeField] private InputField m_CompositionField;

        [Tooltip("Text field intended to display characters received via OnTextInput")]
        [SerializeField] private InputField m_TextField;

        [Tooltip("Text field intended to display a combination of characters received by "
            + "both the OnIMECompositionChanged & OnTextInput events")]
        [SerializeField] private InputField m_CombinedField;

        [Tooltip("Text field intended for user input")]
        [SerializeField] private InputField m_InputField;

        private bool m_EventListenersAdded = false;
        private bool m_ComposingViaIME = false;
        private string m_CurrentInput;
    }
}
