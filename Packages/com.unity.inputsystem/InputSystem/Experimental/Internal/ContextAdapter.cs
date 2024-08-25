using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A temporary implementation of an adapter from Input System 1.x run-time data formats to the experimental
    /// data format. Note that this adapter is expected to cause overhead and is merely a stepping stone for
    /// evaluating an alternative model.
    /// </summary>
    internal static class InputSystemAdapter
    {
        private const int Keys = ('K' << 24) | ('E' << 16) | ('Y' << 8) | ('S' << 0); // KEYS
        private const int Mouse = ('M' << 24) | ('O' << 16) | ('U' << 8) | ('S' << 0); // MOUS
        private const int Gamepad = ('G' << 24) | ('A' << 16) | ('M' << 8) | ('P' << 0); // GAMP
        
        private static readonly StringBuilder LogBuffer = new StringBuilder();
        
        private static unsafe void LogEvent(UnityEngine.InputSystem.LowLevel.InputEvent* eventPtr)
        {
            LogBuffer.Clear();
            LogBuffer.Append(eventPtr->type);
            LogBuffer.Append(", deviceId: ").Append(eventPtr->deviceId);
            LogBuffer.Append(", sizeInBytes: ").Append(eventPtr->sizeInBytes.ToString());
            LogBuffer.Append(", time: ").Append(eventPtr->time);
            LogBuffer.Append(", eventId: ").Append(eventPtr->eventId);
            Debug.Log(LogBuffer);
        }

        private static void Log(string value)
        {
            Debug.Log(value);
        }
        
        // TODO As long as KeyboardState sits in native memory there is no need for fixed
        // TODO Poor way of representing keyboard state, a leaner way would be either button groups mapping to uint
        //      or just ID of those being on/down. ?
        private static unsafe void Handle(LowLevel.KeyboardState* @state)
        {
            // TODO Remap to desired output type and/or use usages to forward
            //var c = MemoryHelpers.ReadSingleBit(state, (uint)Key.C);

            
            // TODO Should be compute xor with previous state here?! We need previous state which isn't support over legacy event queue, we likely need a Converter node for it to keep state
            
            var context = Context.instance.GetOrCreateStreamContext<KeyboardState>(Usages.Devices.Keyboard);
            if (context.observerCount > 0)
            {
                KeyboardState keyboard;
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(keyboard.keys, state->keys, 16);
                
                context.OnNext(ref keyboard);
            }
            
            // TODO We cannot reasonably scan for observers of individual keys here
            
            // Forward key state of individual keys to their corresponding observers
            //Log(c.ToString());
        }

        private static unsafe void Handle(LowLevel.MouseState* @state)
        {
            // TODO Implement            
        }

        private static unsafe void Handle(Context context, LowLevel.GamepadState* @state)
        {
            // Return directly if there is no subscription stream for the device interface.
            // (Basically this should only be needed during transitional phases since backend
            // should not send data unless there is a subscription.)
            //if (!router.TryGetValue(Usages.Devices.Gamepad, out var stream))
            //    return;
            
            // TODO It would be beneficial to do a single query here when encoded like this

            var gamepad = Context.instance.GetOrCreateStreamContext<GamepadState>(Usages.Devices.Gamepad); // TODO Instead consider a dictionary of IObserver<T>, or rather in this case a list of observers since known type?
            if (gamepad.observerCount > 0)
            {
                // Convert to desired format (adapter)
                var v = new GamepadState()
                {
                    buttons = (GamepadState.GamepadButton)( 
                        ((state->buttons >> (int)LowLevel.GamepadButton.Select) << (int)GamepadState.GamepadButtonBitShift.Select) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.Start) << (int)GamepadState.GamepadButtonBitShift.Start) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.LeftShoulder) << (int)GamepadState.GamepadButtonBitShift.LeftShoulder) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.RightShoulder) << (int)GamepadState.GamepadButtonBitShift.RightShoulder) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.North) << (int)GamepadState.GamepadButtonBitShift.North) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.South) << (int)GamepadState.GamepadButtonBitShift.South) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.West) << (int)GamepadState.GamepadButtonBitShift.West) |
                        ((state->buttons >> (int)LowLevel.GamepadButton.East) << (int)GamepadState.GamepadButtonBitShift.East) 
                    ),
                    leftStick = state->leftStick,
                    rightStick = state->rightStick,
                    leftTrigger = state->leftTrigger,
                    rightTrigger = state->rightTrigger
                };
                
                gamepad.OnNext(ref v);
            }

            var leftStick = Context.instance.GetOrCreateStreamContext<Vector2>(Devices.Usages.GamepadUsages.LeftStick);
            if (leftStick.observerCount > 0)
                leftStick.OnNext(state->leftStick);

            var buttonSouth = Context.instance.GetOrCreateStreamContext<bool>(Devices.Usages.GamepadUsages.ButtonSouth);
            if (buttonSouth.observerCount > 0)
                buttonSouth.OnNext(0 != (state->buttons & (uint)LowLevel.GamepadButton.South));
            
            // TODO Send value to observable
            //Devices.Gamepad.leftStick.OnNext(gamepad.leftStick);
        }

        private static unsafe void HandleStateEvent(Context context, LowLevel.StateEvent* @event)
        {
            var format = @event->stateFormat;
            
            switch ((int)format)
            {
                case Keys:
                    Handle((LowLevel.KeyboardState*)@event->state);
                    break;
                case Mouse:
                    Handle((LowLevel.MouseState*)@event->state);
                    break;
                case Gamepad:
                    Handle(context, (LowLevel.GamepadState*)@event->state);
                    break;
            }
        }
        
        // Temporary adapter from current low level implementation into another representation suitable for
        // a subscription based model.
        public static unsafe void Handle(UnityEngine.InputSystem.LowLevel.InputEvent* @event)
        {
            var context = Context.instance;
            
            switch (@event->type)
            {
                case LowLevel.StateEvent.Type:
                    HandleStateEvent(context, (LowLevel.StateEvent*)@event);
                    break;
                
                case LowLevel.DeltaStateEvent.Type:
                    break;
                
                case LowLevel.DeviceConfigurationEvent.Type:
                    break;
                
                case LowLevel.DeviceRemoveEvent.Type:
                    break;
            }
        }
    }
}