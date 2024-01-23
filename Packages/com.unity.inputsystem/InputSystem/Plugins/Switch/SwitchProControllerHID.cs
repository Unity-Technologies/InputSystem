#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA || PACKAGE_DOCS_GENERATION
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch.LowLevel;
using UnityEngine.InputSystem.Utilities;

////REVIEW: The Switch controller can be used to point at things; can we somehow help leverage that?

namespace UnityEngine.InputSystem.Switch.LowLevel
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA
    /// <summary>
    /// Structure of HID input reports for Switch Pro controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 7)]
    internal struct SwitchProControllerHIDInputState : IInputStateTypeInfo
    {
        public static FourCC Format = new FourCC('S', 'P', 'V', 'S'); // Switch Pro Virtual State

        public FourCC format => Format;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "leftStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85")]
        [InputControl(name = "leftStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "leftStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85,invert=false")]
        [FieldOffset(0)] public byte leftStickX;
        [FieldOffset(1)] public byte leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "rightStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5")]
        [InputControl(name = "rightStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.15,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0.15,normalizeMax=0.85,normalizeZero=0.5,clamp=1,clampMin=0.5,clampMax=0.85,invert=false")]
        [FieldOffset(2)] public byte rightStickX;
        [FieldOffset(3)] public byte rightStickY;

        [InputControl(name = "dpad", format = "BIT", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/up", bit = (int)Button.Up)]
        [InputControl(name = "dpad/right", bit = (int)Button.Right)]
        [InputControl(name = "dpad/down", bit = (int)Button.Down)]
        [InputControl(name = "dpad/left", bit = (int)Button.Left)]
        [InputControl(name = "buttonWest", displayName = "Y", shortDisplayName = "Y", bit = (int)Button.Y, usage = "SecondaryAction")]
        [InputControl(name = "buttonNorth", displayName = "X", shortDisplayName = "X", bit = (int)Button.X)]
        [InputControl(name = "buttonSouth", displayName = "B", shortDisplayName = "B", bit = (int)Button.B, usages = new[] { "Back", "Cancel" })]
        [InputControl(name = "buttonEast", displayName = "A", shortDisplayName = "A", bit = (int)Button.A, usages = new[] { "PrimaryAction", "Submit" })]
        [InputControl(name = "leftShoulder", displayName = "L", shortDisplayName = "L", bit = (uint)Button.L)]
        [InputControl(name = "rightShoulder", displayName = "R", shortDisplayName = "R", bit = (uint)Button.R)]
        [InputControl(name = "leftStickPress", displayName = "Left Stick", bit = (uint)Button.StickL)]
        [InputControl(name = "rightStickPress", displayName = "Right Stick", bit = (uint)Button.StickR)]
        [InputControl(name = "leftTrigger", displayName = "ZL", shortDisplayName = "ZL", format = "BIT", bit = (uint)Button.ZL)]
        [InputControl(name = "rightTrigger", displayName = "ZR", shortDisplayName = "ZR", format = "BIT", bit = (uint)Button.ZR)]
        [InputControl(name = "start", displayName = "Plus", bit = (uint)Button.Plus, usage = "Menu")]
        [InputControl(name = "select", displayName = "Minus", bit = (uint)Button.Minus)]
        [FieldOffset(4)] public ushort buttons1;

        [InputControl(name = "capture", layout = "Button", displayName = "Capture", bit = (uint)Button.Capture - 16)]
        [InputControl(name = "home", layout = "Button", displayName = "Home", bit = (uint)Button.Home - 16)]
        [FieldOffset(6)] public byte buttons2;

        public enum Button
        {
            Up = 0,
            Right = 1,
            Down = 2,
            Left = 3,

            West = 4,
            North = 5,
            South = 6,
            East = 7,

            L = 8,
            R = 9,
            StickL = 10,
            StickR = 11,

            ZL = 12,
            ZR = 13,
            Plus = 14,
            Minus = 15,
            Capture = 16,
            Home = 17,

            X = North,
            B = South,
            Y = West,
            A = East,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SwitchProControllerHIDInputState WithButton(Button button, bool value = true)
        {
            Set(button, value);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Button button, bool state)
        {
            Debug.Assert((int)button < 18, $"Expected button < 18");
            if ((int)button < 16)
            {
                var bit = (ushort)(1U << (int)button);
                if (state)
                    buttons1 = (ushort)(buttons1 | bit);
                else
                    buttons1 &= (ushort)~bit;
            }
            else if ((int)button < 18)
            {
                var bit = (byte)(1U << ((int)button - 16));
                if (state)
                    buttons2 = (byte)(buttons2 | bit);
                else
                    buttons2 &= (byte)~bit;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Press(Button button)
        {
            Set(button, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(Button button)
        {
            Set(button, false);
        }
    }
#endif
}

namespace UnityEngine.InputSystem.Switch
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA || PACKAGE_DOCS_GENERATION
    /// <summary>
    /// A Nintendo Switch Pro controller connected to a desktop mac/windows PC using the HID interface.
    /// </summary>
    [InputControlLayout(stateType = typeof(SwitchProControllerHIDInputState), displayName = "Switch Pro Controller")]
    public class SwitchProControllerHID : Gamepad, IInputStateCallbackReceiver, IEventPreProcessor
    {
        [InputControl(name = "capture", displayName = "Capture")]
        public ButtonControl captureButton { get; protected set; }

        [InputControl(name = "home", displayName = "Home")]
        public ButtonControl homeButton { get; protected set; }

        protected override void OnAdded()
        {
            base.OnAdded();

            captureButton = GetChildControl<ButtonControl>("capture");
            homeButton = GetChildControl<ButtonControl>("home");

            HandshakeRestart();
        }

        private static readonly SwitchMagicOutputReport.CommandIdType[] s_HandshakeSequence = new[]
        {
            SwitchMagicOutputReport.CommandIdType.Status,
            SwitchMagicOutputReport.CommandIdType.Handshake,
            SwitchMagicOutputReport.CommandIdType.Highspeed,
            SwitchMagicOutputReport.CommandIdType.Handshake,
            SwitchMagicOutputReport.CommandIdType.ForceUSB
            ////TODO: Should we add a step to revert back to simple interface?
            //// Because currently full reports don't work in old input system.
        };

        private int m_HandshakeStepIndex;
        private double m_HandshakeTimer;

        private void HandshakeRestart()
        {
            // Delay first command issue until some time into the future
            m_HandshakeStepIndex = -1;
            m_HandshakeTimer = InputRuntime.s_Instance.currentTime;
        }

        private void HandshakeTick()
        {
            const double handshakeRestartTimeout = 2.0;
            const double handshakeNextStepTimeout = 0.1;

            var currentTime = InputRuntime.s_Instance.currentTime;

            // There were no events for last few seconds, restart handshake
            if (currentTime >= m_LastUpdateTimeInternal + handshakeRestartTimeout &&
                currentTime >= m_HandshakeTimer + handshakeRestartTimeout)
                m_HandshakeStepIndex = 0;
            // If handshake is complete, ignore the tick.
            else if (m_HandshakeStepIndex + 1 >= s_HandshakeSequence.Length)
                return;
            // If we timeout, proceed to next step after some time is elapsed.
            else if (currentTime > m_HandshakeTimer + handshakeNextStepTimeout)
                m_HandshakeStepIndex++;
            // If we haven't timed out on handshake step, skip the tick.
            else
                return;

            m_HandshakeTimer = currentTime;

            var command = s_HandshakeSequence[m_HandshakeStepIndex];

            // Native backend rejects one of the commands based on size of descriptor.
            // So just report both at a same time.
            ////TODO: fix this.
            var commandBt = SwitchMagicOutputHIDBluetooth.Create(command);
            if (ExecuteCommand(ref commandBt) > 0)
                return;

            var commandUsb = SwitchMagicOutputHIDUSB.Create(command);
            ExecuteCommand(ref commandUsb);
        }

        public void OnNextUpdate()
        {
            HandshakeTick();
        }

        // filter out three lower bits as jitter noise
        internal const byte JitterMaskLow = 0b01111000;
        internal const byte JitterMaskHigh = 0b10000111;

        public unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            if (eventPtr.type == StateEvent.Type && eventPtr.stateFormat == SwitchProControllerHIDInputState.Format)
            {
                var currentState = (SwitchProControllerHIDInputState*)((byte*)currentStatePtr + m_StateBlock.byteOffset);
                var newState = (SwitchProControllerHIDInputState*)StateEvent.FromUnchecked(eventPtr)->state;

                var actuated =
                    // we need to make device current if axes are outside of deadzone specifying hardware jitter of sticks around zero point
                    newState->leftStickX<JitterMaskLow
                                         || newState->leftStickX> JitterMaskHigh
                    || newState->leftStickY<JitterMaskLow
                                            || newState->leftStickY> JitterMaskHigh
                    || newState->rightStickX<JitterMaskLow
                                             || newState->rightStickX> JitterMaskHigh
                    || newState->rightStickY<JitterMaskLow
                                             || newState->rightStickY> JitterMaskHigh
                    // we need to make device current if buttons state change
                    || newState->buttons1 != currentState->buttons1
                    || newState->buttons2 != currentState->buttons2;

                if (!actuated)
                    InputSystem.s_Manager.DontMakeCurrentlyUpdatingDeviceCurrent();
            }

            InputState.Change(this, eventPtr);
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }

        public unsafe bool PreProcessEvent(InputEventPtr eventPtr)
        {
            if (eventPtr.type == DeltaStateEvent.Type)
                // if someone queued delta state SPVS directly, just use as-is
                // otherwise skip all delta state events
                return DeltaStateEvent.FromUnchecked(eventPtr)->stateFormat == SwitchProControllerHIDInputState.Format;

            // use all other non-state/non-delta-state events
            if (eventPtr.type != StateEvent.Type)
                return true;

            var stateEvent = StateEvent.FromUnchecked(eventPtr);
            var size = stateEvent->stateSizeInBytes;

            if (stateEvent->stateFormat == SwitchProControllerHIDInputState.Format)
                return true; // if someone queued SPVS directly, just use as-is

            if (stateEvent->stateFormat != SwitchHIDGenericInputReport.Format || size < sizeof(SwitchHIDGenericInputReport))
                return false; // skip unrecognized state events otherwise they will corrupt control states

            var genericReport = (SwitchHIDGenericInputReport*)stateEvent->state;
            if (genericReport->reportId == SwitchSimpleInputReport.ExpectedReportId && size >= SwitchSimpleInputReport.kSize)
            {
                var data = ((SwitchSimpleInputReport*)stateEvent->state)->ToHIDInputReport();
                *((SwitchProControllerHIDInputState*)stateEvent->state) = data;
                stateEvent->stateFormat = SwitchProControllerHIDInputState.Format;
                return true;
            }
            else if (genericReport->reportId == SwitchFullInputReport.ExpectedReportId && size >= SwitchFullInputReport.kSize)
            {
                var data = ((SwitchFullInputReport*)stateEvent->state)->ToHIDInputReport();
                *((SwitchProControllerHIDInputState*)stateEvent->state) = data;
                stateEvent->stateFormat = SwitchProControllerHIDInputState.Format;
                return true;
            }
            else if (size == 8 || size == 9) // official accessories send 8 byte reports
            {
                // On Windows HID stack we somehow get 1 byte extra prepended, so if we get 9 bytes, subtract one, see ISX-993
                // This is written in such way that if we fix it in backend, we wont break the package (Unity will report 8 bytes instead of 9 bytes).
                var bugOffset = size == 9 ? 1 : 0;
                var data = ((SwitchInputOnlyReport*)((byte*)stateEvent->state + bugOffset))->ToHIDInputReport();
                *((SwitchProControllerHIDInputState*)stateEvent->state) = data;
                stateEvent->stateFormat = SwitchProControllerHIDInputState.Format;
                return true;
            }
            else
                return false; // skip unrecognized reportId
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        private struct SwitchInputOnlyReport
        {
            public const int kSize = 7;

            [FieldOffset(0)] public byte buttons0;
            [FieldOffset(1)] public byte buttons1;
            [FieldOffset(2)] public byte hat;
            [FieldOffset(3)] public byte leftX;
            [FieldOffset(4)] public byte leftY;
            [FieldOffset(5)] public byte rightX;
            [FieldOffset(6)] public byte rightY;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SwitchProControllerHIDInputState ToHIDInputReport()
            {
                var state = new SwitchProControllerHIDInputState
                {
                    leftStickX = leftX,
                    leftStickY = leftY,
                    rightStickX = rightX,
                    rightStickY = rightY
                };

                state.Set(SwitchProControllerHIDInputState.Button.Y, (buttons0 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.B, (buttons0 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.A, (buttons0 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.X, (buttons0 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.L, (buttons0 & 0x10) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.R, (buttons0 & 0x20) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZL, (buttons0 & 0x40) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZR, (buttons0 & 0x80) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Minus, (buttons1 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Plus, (buttons1 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickL, (buttons1 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickR, (buttons1 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Home, (buttons1 & 0x10) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Capture, (buttons1 & 0x20) != 0);

                var left = false;
                var up = false;
                var right = false;
                var down = false;

                switch (hat)
                {
                    case 0:
                        up = true;
                        break;
                    case 1:
                        up = true;
                        right = true;
                        break;
                    case 2:
                        right = true;
                        break;
                    case 3:
                        down = true;
                        right = true;
                        break;
                    case 4:
                        down = true;
                        break;
                    case 5:
                        down = true;
                        left = true;
                        break;
                    case 6:
                        left = true;
                        break;
                    case 7:
                        up = true;
                        left = true;
                        break;
                }

                state.Set(SwitchProControllerHIDInputState.Button.Left, left);
                state.Set(SwitchProControllerHIDInputState.Button.Up, up);
                state.Set(SwitchProControllerHIDInputState.Button.Right, right);
                state.Set(SwitchProControllerHIDInputState.Button.Down, down);
                return state;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        private struct SwitchSimpleInputReport
        {
            public const int kSize = 12;
            public const byte ExpectedReportId = 0x3f;

            [FieldOffset(0)] public byte reportId;
            [FieldOffset(1)] public byte buttons0;
            [FieldOffset(2)] public byte buttons1;
            [FieldOffset(3)] public byte hat;
            [FieldOffset(4)] public ushort leftX;
            [FieldOffset(6)] public ushort leftY;
            [FieldOffset(8)] public ushort rightX;
            [FieldOffset(10)] public ushort rightY;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SwitchProControllerHIDInputState ToHIDInputReport()
            {
                var leftXByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(leftX, 16, 8);
                var leftYByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(leftY, 16, 8);
                var rightXByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(rightX, 16, 8);
                var rightYByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(rightY, 16, 8);

                var state = new SwitchProControllerHIDInputState
                {
                    leftStickX = leftXByte,
                    leftStickY = leftYByte,
                    rightStickX = rightXByte,
                    rightStickY = rightYByte
                };

                state.Set(SwitchProControllerHIDInputState.Button.B, (buttons0 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.A, (buttons0 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Y, (buttons0 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.X, (buttons0 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.L, (buttons0 & 0x10) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.R, (buttons0 & 0x20) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZL, (buttons0 & 0x40) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZR, (buttons0 & 0x80) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Minus, (buttons1 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Plus, (buttons1 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickL, (buttons1 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickR, (buttons1 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Home, (buttons1 & 0x10) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Capture, (buttons1 & 0x20) != 0);

                var left = false;
                var up = false;
                var right = false;
                var down = false;

                switch (hat)
                {
                    case 0:
                        up = true;
                        break;
                    case 1:
                        up = true;
                        right = true;
                        break;
                    case 2:
                        right = true;
                        break;
                    case 3:
                        down = true;
                        right = true;
                        break;
                    case 4:
                        down = true;
                        break;
                    case 5:
                        down = true;
                        left = true;
                        break;
                    case 6:
                        left = true;
                        break;
                    case 7:
                        up = true;
                        left = true;
                        break;
                }

                state.Set(SwitchProControllerHIDInputState.Button.Left, left);
                state.Set(SwitchProControllerHIDInputState.Button.Up, up);
                state.Set(SwitchProControllerHIDInputState.Button.Right, right);
                state.Set(SwitchProControllerHIDInputState.Button.Down, down);

                return state;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        private struct SwitchFullInputReport
        {
            public const int kSize = 25;
            public const byte ExpectedReportId = 0x30;

            [FieldOffset(0)] public byte reportId;
            [FieldOffset(3)] public byte buttons0;
            [FieldOffset(4)] public byte buttons1;
            [FieldOffset(5)] public byte buttons2;
            [FieldOffset(6)] public byte left0;
            [FieldOffset(7)] public byte left1;
            [FieldOffset(8)] public byte left2;
            [FieldOffset(9)] public byte right0;
            [FieldOffset(10)] public byte right1;
            [FieldOffset(11)] public byte right2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SwitchProControllerHIDInputState ToHIDInputReport()
            {
                ////TODO: calibration curve

                var leftXRaw = (uint)(left0 | ((left1 & 0x0F) << 8));
                var leftYRaw = (uint)(((left1 & 0xF0) >> 4) | (left2 << 4));
                var rightXRaw = (uint)(right0 | ((right1 & 0x0F) << 8));
                var rightYRaw = (uint)(((right1 & 0xF0) >> 4) | (right2 << 4));

                var leftXByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(leftXRaw, 12, 8);
                var leftYByte = (byte)(0xff - (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(leftYRaw, 12, 8));
                var rightXByte = (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(rightXRaw, 12, 8);
                var rightYByte = (byte)(0xff - (byte)NumberHelpers.RemapUIntBitsToNormalizeFloatToUIntBits(rightYRaw, 12, 8));

                var state = new SwitchProControllerHIDInputState
                {
                    leftStickX = leftXByte,
                    leftStickY = leftYByte,
                    rightStickX = rightXByte,
                    rightStickY = rightYByte
                };

                state.Set(SwitchProControllerHIDInputState.Button.Y, (buttons0 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.X, (buttons0 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.B, (buttons0 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.A, (buttons0 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.R, (buttons0 & 0x40) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZR, (buttons0 & 0x80) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Minus, (buttons1 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Plus, (buttons1 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickR, (buttons1 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.StickL, (buttons1 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Home, (buttons1 & 0x10) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Capture, (buttons1 & 0x20) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Down, (buttons2 & 0x01) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Up, (buttons2 & 0x02) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Right, (buttons2 & 0x04) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.Left, (buttons2 & 0x08) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.L, (buttons2 & 0x40) != 0);
                state.Set(SwitchProControllerHIDInputState.Button.ZL, (buttons2 & 0x80) != 0);

                return state;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SwitchHIDGenericInputReport
        {
            public static FourCC Format => new FourCC('H', 'I', 'D');

            [FieldOffset(0)] public byte reportId;
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        internal struct SwitchMagicOutputReport
        {
            public const int kSize = 49;

            public const byte ExpectedReplyInputReportId = 0x81;

            [FieldOffset(0)] public byte reportType;
            [FieldOffset(1)] public byte commandId;

            internal enum ReportType
            {
                Magic = 0x80
            }

            public enum CommandIdType
            {
                Status = 0x01,
                Handshake = 0x02,
                Highspeed = 0x03,
                ForceUSB  = 0x04,
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        internal struct SwitchMagicOutputHIDBluetooth : IInputDeviceCommandInfo
        {
            public static FourCC Type => new FourCC('H', 'I', 'D', 'O');
            public FourCC typeStatic => Type;

            public const int kSize = InputDeviceCommand.kBaseCommandSize + 49;

            [FieldOffset(0)] public InputDeviceCommand baseCommand;
            [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public SwitchMagicOutputReport report;

            public static SwitchMagicOutputHIDBluetooth Create(SwitchMagicOutputReport.CommandIdType type)
            {
                return new SwitchMagicOutputHIDBluetooth
                {
                    baseCommand = new InputDeviceCommand(Type, kSize),
                    report = new SwitchMagicOutputReport
                    {
                        reportType = (byte)SwitchMagicOutputReport.ReportType.Magic,
                        commandId = (byte)type
                    }
                };
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = kSize)]
        internal struct SwitchMagicOutputHIDUSB : IInputDeviceCommandInfo
        {
            public static FourCC Type => new FourCC('H', 'I', 'D', 'O');
            public FourCC typeStatic => Type;

            public const int kSize = InputDeviceCommand.kBaseCommandSize + 64;

            [FieldOffset(0)] public InputDeviceCommand baseCommand;
            [FieldOffset(InputDeviceCommand.kBaseCommandSize + 0)] public SwitchMagicOutputReport report;

            public static SwitchMagicOutputHIDUSB Create(SwitchMagicOutputReport.CommandIdType type)
            {
                return new SwitchMagicOutputHIDUSB
                {
                    baseCommand = new InputDeviceCommand(Type, kSize),
                    report = new SwitchMagicOutputReport
                    {
                        reportType = (byte)SwitchMagicOutputReport.ReportType.Magic,
                        commandId = (byte)type
                    }
                };
            }
        }
    }
#endif
}
#endif // UNITY_EDITOR || UNITY_SWITCH
