using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
#if !UNITY_INPUTSYSTEM_SUPPORTS_FOCUS_EVENTS
    // Prior to 6000.5.a8 Input System mixed application focus with deferred events causing
    // incorrect reasoning regarding which events happened in-focus vs out-of-focus.
    // When running on an older editor, we define the enum here instead to reduce redundancy.
    /// <summary>
    /// Flags indicating various focus states for the application and editor.
    /// </summary>
    internal enum FocusFlags : ushort
    {
        /// <summary>
        /// No focus state is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// In editor this means the GameView has focus. In a built player this means the player has focus.
        /// </summary>
        ApplicationFocus = (1 << 0),
    };

    internal partial class InputManager
    {
        internal void OnFocusChanged(bool focus)
        {
            // We set this to temporarily override applicationHasFocus before processing the focus change
            // as defaultUpdateType is influenced by it. Before returning from this method, clear the
            // bool to stop overriding to indicate this manager has finished processing the focus change.
            m_IsHandlingFocusChange = true;
            m_ApplicationHadFocus = !focus;

#if UNITY_EDITOR
            var shouldClearCurrentUpdateInFinally = false;
#endif

            try
            {
#if UNITY_EDITOR
                SyncAllDevicesWhenEditorIsActivated();

                if (!m_Runtime.isInPlayMode)
                    return;

                var gameViewFocus = m_Settings.editorInputBehaviorInPlayMode;
#endif

                var runInBackground =
#if UNITY_EDITOR
                    // In the editor, the player loop will always be run even if the Game View does not have focus. This
                    // amounts to runInBackground being always true in the editor, regardless of what the setting in
                    // the Player Settings window is.
                    //
                    // If, however, "Game View Focus" is set to "Exactly As In Player", we force code here down the same
                    // path as in the player.
                    gameViewFocus != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView || m_Runtime.runInBackground;
#else
                    m_Runtime.runInBackground;
#endif

                if (m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus && runInBackground)
                {
                    // If runInBackground is true, no device changes should happen, even when focus is gained. So early out.
                    // If runInBackground is false, we still want to sync devices when focus is gained. So we need to continue further.
                    return;
                }

#if UNITY_EDITOR
                // Set the current update type while we process the focus changes to make sure we
                // feed into the right buffer. No need to do this in the player as it doesn't have
                // the editor/player confusion.
                m_CurrentUpdate = m_UpdateMask.GetUpdateTypeForPlayer();
                shouldClearCurrentUpdateInFinally = true;
#endif

                if (!focus)
                {
                    // We only react to loss of focus when we will keep running in the background. If not,
                    // we'll do nothing and just wait for focus to come back (where we then try to sync all devices).
                    if (runInBackground)
                    {
                        for (var i = 0; i < m_DevicesCount; ++i)
                        {
                            // Determine whether to run this device in the background.
                            var device = m_Devices[i];
                            if (!device.enabled || ShouldRunDeviceInBackground(device))
                                continue;

                            // Disable the device. This will also soft-reset it.
                            EnableOrDisableDevice(device, false, DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

                            // In case we invoked a callback that messed with our device array, adjust our index.
                            var index = m_Devices.IndexOfReference(device, m_DevicesCount);
                            if (index == -1)
                                --i;
                            else
                                i = index;
                        }
                    }
                }
                else
                {
                    m_DiscardOutOfFocusEvents = true;
                    m_FocusRegainedTime = m_Runtime.currentTime;

                    // On focus gain, reenable and sync devices.
                    for (var i = 0; i < m_DevicesCount; ++i)
                    {
                        var device = m_Devices[i];

                        // Re-enable the device if we disabled it on focus loss. This will also issue a sync.
                        if (device.disabledWhileInBackground)
                            EnableOrDisableDevice(device, true, DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

                        // Try to sync. If it fails and we didn't run in the background, perform
                        // a reset instead. This is to cope with backends that are unable to sync but
                        // may still retain state which now may be outdated because the input device may
                        // have changed state while we weren't running. So at least make the backend flush
                        // its state (if any).
                        else if (device.enabled && !runInBackground && !device.RequestSync() && m_Settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
                            ResetDevice(device);
                    }
                }
            }
            finally
            {
#if UNITY_EDITOR
                if (shouldClearCurrentUpdateInFinally)
                    m_CurrentUpdate = InputUpdateType.None;
#endif

                m_IsHandlingFocusChange = false;
            }
        }

        /// <summary>
        /// Determines if the event buffer should be flushed without processing events.
        /// </summary>
        /// <returns>True if the buffer should be flushed, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldFlushEventBuffer()
        {
#if UNITY_EDITOR
            // If out of focus and runInBackground is off and ExactlyAsInPlayer is on, discard input.
            if (!gameHasFocus &&
                m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView
                &&
                (!m_Runtime.runInBackground || m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.ResetAndDisableAllDevices))
                return true;
#else
            // In player builds, flush if out of focus and not running in background
            if (!gameHasFocus && !m_Runtime.runInBackground)
                return true;
#endif
            return false;
        }

        /// <summary>
        /// Determines if we should exit early from event processing without handling events.
        /// </summary>
        /// <param name="updateType">The current update type</param>
        /// <returns>True if we should exit early, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldExitEarlyFromEventProcessing(InputUpdateType updateType)
        {
#if UNITY_EDITOR
            // Check various PlayMode specific early exit conditions
            if (ShouldExitEarlyBasedOnBackgroundBehavior(updateType))
                return true;

            // When the game is playing and has focus, we never process input in editor updates.
            // All we do is just switch to editor state buffers and then exit.
            if ((gameIsPlaying && gameHasFocus && updateType == InputUpdateType.Editor))
                return true;
#endif

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Checks background behavior conditions for early exit from event processing.
        /// </summary>
        /// <param name="updateType">The current update type</param>
        /// <returns>True if we should exit early, false otherwise.</returns>
        /// <remarks>
        /// Whenever this method returns true, it usually means that events are left in the buffer and should be
        /// processed in a next update call.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldExitEarlyBasedOnBackgroundBehavior(InputUpdateType updateType)
        {
            // In Play Mode, if we're in the background and not supposed to process events in this update
            if ((!gameHasFocus || gameShouldGetInputRegardlessOfFocus) && updateType != InputUpdateType.Editor)
            {
                if (m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.ResetAndDisableAllDevices ||
                    m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    return true;
            }

            // Special case for IgnoreFocus behavior with AllDeviceInputAlwaysGoesToGameView in editor updates
            if ((!gameHasFocus || gameShouldGetInputRegardlessOfFocus) &&
                m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus &&
                m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView &&
                updateType == InputUpdateType.Editor)
                return true;

            return false;
        }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool LegacyEarlyOutFromEventProcessing(InputUpdateType updateType, ref InputEventBuffer eventBuffer, ref bool dropStatusEvents)
        {
            var shouldProcessActionTimeouts = updateType.IsPlayerUpdate() && gameIsPlaying;
            // Determine if we should flush the event buffer which would imply we exit early and do not process
            // any of those events, ever.
            var shouldFlushEventBuffer = ShouldFlushEventBuffer();
            // When we exit early, we may or may not flush the event buffer. It depends if we want to process events
            // later once this method is called.
            var shouldExitEarly = eventBuffer.eventCount == 0 || shouldFlushEventBuffer || ShouldExitEarlyFromEventProcessing(updateType);
            dropStatusEvents = ShouldDropStatusEvents(eventBuffer, ref shouldExitEarly);

            // we exit early as we have no events in the buffer
            if (shouldExitEarly)
            {
                // Normally, we process action timeouts after first processing all events. If we have no
                // events, we still need to check timeouts.
                if (shouldProcessActionTimeouts)
                    m_StateMonitors.ProcessTimeouts();

                if (shouldFlushEventBuffer)
                    eventBuffer.Reset();
                InvokeAfterUpdateCallback(updateType);
                m_CurrentUpdate = InputUpdateType.None;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if status events should be dropped and modifies early exit behavior accordingly.
        /// </summary>
        /// <param name="eventBuffer">The current event buffer</param>
        /// <param name="canEarlyOut">Reference to the early exit flag that may be modified</param>
        /// <returns>True if status events should be dropped, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldDropStatusEvents(InputEventBuffer eventBuffer, ref bool canEarlyOut)
        {
            // If the game is not playing but we're sending all input events to the game,
            // the buffer can just grow unbounded. So, in that case, set a flag to say we'd
            // like to drop status events, and do not early out.
            if (!gameIsPlaying && gameShouldGetInputRegardlessOfFocus && (eventBuffer.sizeInBytes > (100 * 1024)))
            {
                canEarlyOut = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if an event should be discarded because it occurred while out of focus, under specific settings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldDiscardOutOfFocusEvent(double eventTime)
        {
            // If we care about focus, check if the event occurred while out of focus based on its timestamp.
            if (gameHasFocus && m_Settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
                return m_DiscardOutOfFocusEvents && eventTime < m_FocusRegainedTime;
            return false;
        }
    }
#endif // !UNITY_INPUTSYSTEM_SUPPORTS_FOCUS_EVENTS
}
