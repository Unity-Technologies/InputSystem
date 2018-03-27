using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: Don't! Instead of ActionEvents, have the system be able to generate *state change events*
////        from monitors set up in the system. These can then be consumed by other systems -- with
////        the action system being one consumer.

namespace UnityEngine.Experimental.Input.LowLevel
{
    // Phase change of an action.
    //
    // Captures the control state initiating the phase shift as well as action-related
    // information when the phase shift happened.
    //
    // Action events are representation-compatible with DeltaStateEvents. The
    // DeltaStateEvent portion of the event captures the control state that
    // triggered the action. The remainder of the action event contains
    // information about which binding triggered the action and such.
    //
    // Variable-size event.
    //
    // NOTE: ActionEvents do not surface on the native event queue (i.e. they do not come in
    //       through native updates received through NativeInputSystem.onUpdate). Instead,
    //       action events are handled separately by the action system.
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 9)]
    public unsafe struct ActionEvent : IInputEventTypeInfo
    {
        public const int Type = 0x4143544E; // 'ACTN'

        [FieldOffset(0)] public InputEvent baseEvent;
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public uint stateOffset;
        [FieldOffset(InputEvent.kBaseEventSize + 8)] public fixed byte stateData[1]; // Variable-sized.

        public uint stateSizeInBytes
        {
            get { return baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + 8); }
        }

        public IntPtr state
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return new IntPtr((void*)data);
                }
            }
        }

        // Action-specific fields are appended *after* the device state in the event.
        // This way we can use ActionEvent everywhere a DeltaStateEvent is expected.

        public int actionIndex
        {
            get { return *(int*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.ActionIndex); }
        }

        public int bindingIndex
        {
            get { return *(int*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.BindingIndex); }
        }

        public int modifierIndex
        {
            get { return *(int*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.ModifierIndex); }
        }

        public double startTime
        {
            get { return *(double*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.StartTime); }
        }

        public double endTime
        {
            get { return *(double*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.EndTime); }
        }

        public InputAction.Phase phase
        {
            get { return (InputAction.Phase)(*(int*)new IntPtr(actionData.ToInt64() + (int)ActionDataOffset.Phase)); }
        }

        ////TODO: give all currently enabled actions indices

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        internal IntPtr actionData
        {
            get { return new IntPtr(state.ToInt64() + stateSizeInBytes); }
        }

        internal enum ActionDataOffset
        {
            ActionIndex = 0,
            BindingIndex = 4,
            ModifierIndex = 8,
            StartTime = 12,
            EndTime = 20,
            Phase = 28
        }
    }
}
