using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A reusable component with a self-contained UI for rebinding a single action.
    /// </summary>
    public class RebindActionUI : MonoBehaviour
    {
        /// <summary>
        /// Reference to the action that is to be rebound.
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Text component that receives the name of the action. Optional.
        /// </summary>
        public Text actionLabel
        {
            get => m_ActionLabel;
            set
            {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        /// <summary>
        /// Text component that receives the display string of the binding. Can be <c>null</c> in which
        /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
        /// </summary>
        public Text bindingText
        {
            get => m_BindingText;
            set
            {
                m_BindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Optional text component that receives a text prompt when waiting for a control to be actuated.
        /// </summary>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindOverlay"/>
        public Text rebindPrompt
        {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        /// <summary>
        /// Optional text component that shows relevant information when waiting for a control to be actuated.
        /// </summary>
        /// <seealso cref="rebindPrompt"/>
        /// <seealso cref="rebindOverlay"/>
        public Text rebindInfo
        {
            get => m_RebindInfo;
            set => m_RebindInfo = value;
        }

        /// <summary>
        /// Optional button to manually cancel rebinding while waiting.
        /// </summary>
        public Button rebindCancelButton
        {
            get => m_RebindCancelButton;
            set => m_RebindCancelButton = value;
        }

        /// <summary>
        /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
        /// is finished. This is normally used to display an overlay over the current UI while the system is
        /// waiting for a control to be actuated.
        /// </summary>
        /// <remarks>
        /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
        /// replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
        /// </remarks>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindPrompt"/>
        public GameObject rebindOverlay
        {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        /// <summary>
        /// Return the action and binding index for the binding that is targeted by the component
        /// according to the binding ID property.
        /// </summary>
        /// <param name="action">The action returned by reference.</param>
        /// <param name="bindingIndex">The binding index returned by reference.</param>
        /// <returns>true if able to resolve, otherwise false.</returns>
        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            action = m_Action?.action;

            bindingIndex = action.FindBindingById(m_BindingId);
            if (bindingIndex >= 0)
                return true;

            if (action != null && !string.IsNullOrEmpty(m_BindingId))
                Debug.LogError($"Cannot find binding with ID '{m_BindingId}' on '{action}'", this);
            return false;
        }

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action.
            var action = m_Action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            // Set on label (if any).
            if (m_BindingText != null)
                m_BindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                // It's a composite. Remove overrides from part bindings.
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                    action.RemoveBindingOverride(i);
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Attempts to swap associated binding of this instance with another instance.
        /// </summary>
        /// <remarks>It is expected that the other control is of a compatible type.</remarks>
        /// <param name="other">The other instance to swap binding with.</param>
        /// <returns>true if successfully swapped, else false.</returns>
        public void SwapBinding(RebindActionUI other)
        {
            if (this == other)
                return; // Silently ignore any request to swap binding with itself
            if (ongoingRebind != null || other.ongoingRebind != null)
                throw new Exception("Cannot swap bindings when interactive rebinding is ongoing");
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                throw new Exception("Failed to resolve action and binding index");
            if (!other.ResolveActionAndBinding(out var otherAction, out var otherBindingIndex))
                throw new Exception("Failed to resolve action and binding index");

            // Apply binding override to target binding based on swapped effective binding paths.
            var effectivePath = action.bindings[bindingIndex].effectivePath;
            var otherEffectivePath = otherAction.bindings[otherBindingIndex].effectivePath;
            action.ApplyBindingOverride(bindingIndex, otherEffectivePath);
            otherAction.ApplyBindingOverride(otherBindingIndex, effectivePath);
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            // Extract enabled state to allow restoring enabled state after rebind completes
            var actionWasEnabledPriorToRebind = action.enabled;

            void CleanUp()
            {
                // Restore monitoring cancel button clicks
                if (m_RebindCancelButton != null)
                    m_RebindCancelButton.onClick.RemoveListener(CancelRebind);

                m_RebindOperation?.Dispose();
                m_RebindOperation = null;

                // Restore action enabled state based on state prior to rebind
                if (actionWasEnabledPriorToRebind)
                    action.actionMap.Enable();
            }

            // An "InvalidOperationException: Cannot rebind action x while it is enabled" will
            // be thrown if rebinding is attempted on an action that is enabled.
            //
            // On top of disabling the target action while rebinding, it is recommended to
            // disable any actions (or action maps) that could interact with the rebinding UI
            // or gameplay - it would be undesirable for rebinding to cause the player
            // character to jump.
            //
            // In this example, we explicitly disable both the UI input action map and
            // the action map containing the target action if it was initially enabled.
            if (actionWasEnabledPriorToRebind)
                action.actionMap.Disable();

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .OnCancel(
                    operation =>
                    {
                        m_RebindStopEvent?.Invoke(this, operation);
                        if (m_RebindOverlay != null)
                            m_RebindOverlay.SetActive(false);
                        UpdateBindingDisplay();
                        CleanUp();
                    })
                // We want matching events to be suppressed during rebinding (this is also default).
                //.WithMatchingEventsBeingSuppressed()
                // Since this sample has no interactable UI during rebinding we also want to suppress non-matching events.
                //.WithNonMatchingEventsBeingSuppressed()
                // We want device state to update but not actions firing during rebinding.
                .WithActionEventNotificationsBeingSuppressed()
                // We use a timeout to illustrate that its possible to skip cancel buttons and let rebind timeout.
                .WithTimeout(m_RebindTimeout)
                .OnComplete(
                    operation =>
                    {
                        if (m_RebindOverlay != null)
                            m_RebindOverlay.SetActive(false);
                        m_RebindStopEvent?.Invoke(this, operation);
                        UpdateBindingDisplay();
                        CleanUp();

                        // If there's more composite parts we should bind, initiate a rebind
                        // for the next part.
                        if (allCompositeParts)
                        {
                            var nextBindingIndex = bindingIndex + 1;
                            if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                                PerformInteractiveRebind(action, nextBindingIndex, true);
                        }
                    });

            // If it's a part binding, show the name of the part in the UI.
            var partName = default(string);
            if (action.bindings[bindingIndex].isPartOfComposite)
                partName = $"Binding '{action.bindings[bindingIndex].name}'. ";

            // Bring up rebind overlay, if we have one.
            m_RebindOverlay?.SetActive(true);
            if (m_RebindText != null)
            {
                var text = !string.IsNullOrEmpty(m_RebindOperation.expectedControlType)
                    ? $"{partName}Waiting for {m_RebindOperation.expectedControlType} input..."
                    : $"{partName}Waiting for input...";
                m_RebindText.text = text;
            }

            // Optionally allow canceling rebind via a button if it applicable for the use-case
            if (m_RebindCancelButton != null)
            {
                m_RebindCancelButton.onClick.AddListener(CancelRebind);
            }

            // Update rebind overlay information, if we have one.
            if (m_RebindInfo != null)
            {
                m_RebindStartTime = Time.realtimeSinceStartup;
                UpdateRebindInfo(m_RebindStartTime);
            }

            // If we have no rebind overlay and no callback but we have a binding text label,
            // temporarily set the binding text label to "<Waiting>".
            if (m_RebindOverlay == null && m_RebindText == null && m_RebindStartEvent == null && m_BindingText != null)
                m_BindingText.text = "<Waiting...>";

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }

        private void UpdateRebindInfo(double now)
        {
            if (m_RebindOperation == null)
                return;

            var elapsed = now - m_RebindStartTime;
            var remainingTimeoutWholeSeconds = (int)Math.Floor(m_RebindOperation.timeout - elapsed);
            if (remainingTimeoutWholeSeconds == m_LastRemainingTimeoutSeconds)
                return;

            var text = (m_RebindOperation.timeout > 0.0f)
                ? $"Cancels in <b>{remainingTimeoutWholeSeconds}</b> seconds if no matching input is provided."
                : string.Empty;
            m_RebindInfo.text = text;
            m_LastRemainingTimeoutSeconds = remainingTimeoutWholeSeconds;
        }

        private void CancelRebind()
        {
            m_RebindOperation?.Cancel();
        }

        protected void Update()
        {
            if (m_RebindInfo != null)
                UpdateRebindInfo(Time.realtimeSinceStartupAsDouble);
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<RebindActionUI>();
            s_RebindActionUIs.Add(this);
            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            UpdateBindingDisplay();
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            s_RebindActionUIs.Remove(this);
            if (s_RebindActionUIs.Count == 0)
            {
                s_RebindActionUIs = null;
                InputSystem.onActionChange -= OnActionChange;
            }
            UpdateBindingDisplay();
        }

        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference m_Action;

        [SerializeField]
        private string m_BindingId;

        [SerializeField]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the "
            + "rebind UI not show a label for the action.")]
        [SerializeField]
        private Text m_ActionLabel;

        [Tooltip("Text label that will receive the current, formatted binding string.")]
        [SerializeField]
        private Text m_BindingText;

        [Tooltip("Optional UI that will be shown while a rebind is in progress.")]
        [SerializeField]
        private GameObject m_RebindOverlay;

        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private Text m_RebindText;

        [Tooltip("Optional text label that will be updated with relevant information during rebinding.")]
        [SerializeField]
        private Text m_RebindInfo;

        [Tooltip("Optional cancellation UI button for rebinding overlay.")]
        [SerializeField]
        private Button m_RebindCancelButton;

        [Tooltip("Optional rebinding timeout in seconds. If zero, no timeout will be used.")]
        [SerializeField]
        private float m_RebindTimeout;

        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

        private static List<RebindActionUI> s_RebindActionUIs;

        private double m_RebindStartTime = -1;
        private int m_LastRemainingTimeoutSeconds;

        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        #endif

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null)
            {
                var action = m_Action?.action;
                m_ActionLabel.text = action != null ? action.name : string.Empty;
            }
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}
