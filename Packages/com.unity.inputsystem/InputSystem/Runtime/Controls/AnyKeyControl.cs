using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////REVIEW: generalize this to AnyButton and add to more devices?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control that simply checks the entire state it's been assigned
    /// for whether there's any non-zero bytes. If there are, the control
    /// returns 1.0; otherwise it returns 0.0.
    /// </summary>
    /// <remarks>
    /// This control is used by <see cref="Keyboard.anyKey"/> to create a button
    /// that is toggled on as long as any of the keys on the keyboard is pressed.
    ///
    /// For <see cref="Keyboard.anyKey"/>, <see cref="ButtonControl.wasPressedThisFrame"/> is true in any
    /// frame in which a key goes down, even if other keys are already held.
    /// <see cref="ButtonControl.wasReleasedThisFrame"/> is only true in the frame in which the last held key
    /// is released. This means that there can be several presses in a row without a release in between.
    /// </remarks>
    /// <seealso cref="Keyboard.anyKey"/>
    [InputControlLayout(hideInUI = true)]
    public class AnyKeyControl : ButtonControl
    {
        // Whether each bit of the state is a key, so that a bit going from 0 to 1 is a key going down. Only then
        // does a key going down while another one is held count as a press. Other devices use this control
        // over other kinds of state, such as the float button values of WebGL joysticks, where a value change
        // can also set bits.
        internal bool tracksIndividualKeys;

        // The state as of the last press state update, to find keys that went down since. The editor has its own
        // state buffers, so it gets its own copy.
        private byte[] m_LastState;
#if UNITY_EDITOR
        private byte[] m_LastStateEditor;
#endif

        /// <summary>
        /// Default initialization. Sets state size to 1 bit and format to
        /// <see cref="InputStateBlock.FormatBit"/>.
        /// </summary>
        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.FormatBit;
        }

        /// <inheritdoc />
        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            return this.CheckStateIsAtDefault(statePtr) ? 0.0f : 1.0f;
        }

        internal override unsafe bool CheckForRepress(bool editorState, bool startTracking)
        {
            if (!tracksIndividualKeys)
                return false;

#if UNITY_EDITOR
            ref var lastState = ref editorState ? ref m_LastStateEditor : ref m_LastState;
#else
            ref var lastState = ref m_LastState;
#endif

            // Press tracking for editor and player state can be updated while the other one is current (for example
            // after a settings change), so read the state buffers of the requested one rather than the current ones.
            var updateType = editorState ? InputUpdateType.Editor : InputUpdateType.Dynamic;
            var statePtr = (byte*)GetStatePtr(updateType, previousFrame: false);
            if (statePtr == null)
                return false;
            statePtr += m_StateBlock.byteOffset;

            var sizeInBytes = (int)((m_StateBlock.bitOffset + m_StateBlock.sizeInBits + 7) >> 3);
            var isNewSnapshot = lastState == null || lastState.Length != sizeInBytes;
            if (isNewSnapshot)
                lastState = new byte[sizeInBytes];

            var keyWentDown = false;
            fixed(byte* lastStatePtr = lastState)
            {
                // Compare against the previous frame when tracking starts, so that a key that went down in this
                // frame still counts.
                if (startTracking)
                {
                    var previousStatePtr = (byte*)GetStatePtr(updateType, previousFrame: true) + m_StateBlock.byteOffset;
                    UnsafeUtility.MemCpy(lastStatePtr, previousStatePtr, sizeInBytes);
                }
                else if (isNewSnapshot)
                {
                    UnsafeUtility.MemCpy(lastStatePtr, statePtr, sizeInBytes);
                    return false;
                }

                var endBit = m_StateBlock.bitOffset + m_StateBlock.sizeInBits;
                for (var bit = m_StateBlock.bitOffset; bit < endBit; ++bit)
                {
                    if (MemoryHelpers.ReadSingleBit(statePtr, bit) && !MemoryHelpers.ReadSingleBit(lastStatePtr, bit))
                    {
                        keyWentDown = true;
                        break;
                    }
                }

                UnsafeUtility.MemCpy(lastStatePtr, statePtr, sizeInBytes);
            }

            return keyWentDown;
        }
    }
}
