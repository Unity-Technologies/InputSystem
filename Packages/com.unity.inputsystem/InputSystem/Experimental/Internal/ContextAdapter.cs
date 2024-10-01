using System.Diagnostics;
using System.Text;
using UnityEngine.InputSystem.Experimental.Devices;
using GamepadState = UnityEngine.InputSystem.Experimental.GamepadState;
using KeyboardState = UnityEngine.InputSystem.Experimental.KeyboardState;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A temporary implementation of an adapter from Input System 1.x run-time data formats to the experimental
    /// data format. Note that this adapter is expected to cause overhead and is merely a stepping stone for
    /// evaluating an alternative model.
    /// </summary>
    internal static class InputSystemAdapter
    {
        // Define four CC codes for state event types since this isn't properly defined in module.
        // Note that this replicates information defined in native headers.
        // This should not really exist, module should define these in managed code.
        private const int Keys = ('K' << 24) | ('E' << 16) | ('Y' << 8) | ('S' << 0);    // 'KEYS'
        private const int Mouse = ('M' << 24) | ('O' << 16) | ('U' << 8) | ('S' << 0);   // 'MOUS'
        private const int Gamepad = ('G' << 24) | ('A' << 16) | ('M' << 8) | ('P' << 0); // 'GAMP'
        private const int Pointer = ('P' << 24) | ('T' << 16) | ('R' << 8) | (' ' << 0); // 'PTR '
        
        #region Development support
        
        private static readonly StringBuilder LogBuffer = new ();
        
        [Conditional("DEVELOPMENT_BUILD")]
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
        
        [Conditional("DEVELOPMENT_BUILD")]
        private static void Log(string value)
        {
            Debug.Log(value);
        }

        #endregion

        private static KeyboardState _previous; // TEMPORARY
        
        // TODO As long as KeyboardState sits in native memory there is no need for fixed
        // TODO Poor way of representing keyboard state, a leaner way would be either button groups mapping to uint
        //      or just ID of those being on/down. ?
        private static unsafe void Handle(Context context, LowLevel.KeyboardState* @state)
        {
            // TODO Remap to desired output type and/or use usages to forward
            //var c = MemoryHelpers.ReadSingleBit(state, (uint)Key.W);

            //Debug.Log("Handle: " + c);
            // TODO Should be compute xor with previous state here?! We need previous state which isn't support over legacy event queue, we likely need a Converter node for it to keep state

            // TODO Inject into device stream if supported
            
            // TODO If we had a proper stream here we could query only changed bits for their corresponding keys
            
            // TODO If we wanted to be really strict we would not have previous state in stream if not relevant.
            //      Previous state is only relevant when a subsequent node need to
            
            // For now, let nodes track state when needed since its the simplest, reconsider stream state later, basically we could reallocate stream if needed when there is no data to defragment. Producer have good locality when doing this but producer may also just use static? 
            
            // TODO Being able to filter before invoking callbacks is key if abstract enough. Hence there is a need to guard on previous state. This means that ObservableInput needs knowledge of where it comes from. Hence maybe the callbacks into first node is different?
            
            //
            if (context.TryGetStreamContext(Endpoint.FromDeviceAndUsage(0, Usages.Devices.Keyboard),
                    out Context.StreamContext<KeyboardState> keyboardStreamContext) && 
                keyboardStreamContext.observerCount > 0)
            {
                // Convert and forward to observers of keyboard interface
                KeyboardState keyboard;
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(keyboard.keys, state->keys, 16);
                
                // TODO Should process deferred calls before doing this one
                
                keyboardStreamContext.OnNext(ref keyboard); // TODO Lets look into gating this instead
                context.InvokeDeferred();
                context.InvokeDeferred2();
            }
        }

        // Mouse interface adapter
        private static unsafe void Handle(Context context, LowLevel.MouseState* @state)
        {
            if (context.TryGetStreamContext(Endpoint.FromDeviceAndUsage(0, Usages.Devices.Mouse),
                    out Context.StreamContext<MouseReading> mouseStreamContext) &&
                mouseStreamContext.observerCount > 0)
            {
                var mouse = new MouseReading()
                {
                    deltaX = state->delta.x,
                    deltaY = state->delta.y,
                    scrollX = state->scroll.x,
                    scrollY = state->scroll.y,
                    buttons = new MouseReading.Buttons(state->buttons)
                };
                // discard: state->clickcount
                // discard: state->displayIndex
                mouseStreamContext.OnNext(ref mouse);
            }
        }

        private static unsafe void Handle(Context context, LowLevel.GamepadState* @state)
        {
            // Return directly if there is no subscription stream for the device interface.
            // (Basically this should only be needed during transitional phases since backend
            // should not send data unless there is a subscription.)
            //if (!router.TryGetValue(Usages.Devices.Gamepad, out var stream))
            //    return;
            
            // TODO It would be beneficial to do a single query here when encoded like this

            // TODO Note that we cannot establish if something changed without a stream, this is the main issue with the single queue approach
            // TODO For now we assume it has changed

            var endpoint = Endpoint.FromUsage(Usages.Devices.Gamepad);
            var gamepad = context.GetOrCreateStreamContext<GamepadState>(endpoint); // TODO Instead consider a dictionary of IObserver<T>, or rather in this case a list of observers since known type?
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
                
                // Forward data
                gamepad.OnNext(ref v);
            }

            var leftStick = Context.instance.GetOrCreateStreamContext<Vector2>(Endpoint.FromUsage(Usages.Gamepad.LeftStick));
            if (leftStick.observerCount > 0)
                leftStick.OnNext(state->leftStick);

            var buttonSouth = Context.instance.GetOrCreateStreamContext<bool>(Endpoint.FromUsage(Usages.Gamepad.ButtonSouth));
            if (buttonSouth.observerCount > 0)
                buttonSouth.OnNext(0 != (state->buttons & (uint)LowLevel.GamepadButton.South));
            
            // TODO Send value to observable
            //Devices.Gamepad.leftStick.OnNext(gamepad.leftStick);
        }

        private static unsafe void Handle(Context context, LowLevel.PointerState* @state)
        {
            // TODO Implement
        }

        private static unsafe void HandleStateEvent(Context context, LowLevel.StateEvent* @event)
        {
            var format = @event->stateFormat;
            
            switch ((int)format)
            {
                case Keys:
                    Handle(context, (LowLevel.KeyboardState*)@event->state);
                    break;
                case Mouse:
                    Handle(context, (LowLevel.MouseState*)@event->state);
                    break;
                case Gamepad:
                    Handle(context, (LowLevel.GamepadState*)@event->state);
                    break;
                case Pointer:
                    Handle(context, (LowLevel.PointerState*)@event->state);
                    break;
                // TODO Add support for more event types
                default:
                    break;
            }
        }
        
        // Temporary adapter from current low level implementation into another representation suitable for
        // a subscription based model.
        public static unsafe void Handle(UnityEngine.InputSystem.LowLevel.InputEvent* @event)
        {
            var context = Context.instance;
            var timestamp = @event->time;
            
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

        public static void OnUpdate(LowLevel.InputUpdateType updateType, in LowLevel.InputEventBuffer eventBuffer)
        {
            // TODO Consider doing our own processing of the eventBuffer here transforming it into a stream context instead to properly mimic another underlying system. Downside is that it would cost us.
            // TODO Consider having separate contexts for player and editor to allow both to execute individually

            var updateStartTimestamp = Stopwatch.GetTimestamp();
            
            // TODO Process event buffer here instead of requiring callback form InputManager loop
            
            // Update input context via the Input System update
            Context.instance.Update();

            var updateEndTimestamp = Stopwatch.GetTimestamp() - updateStartTimestamp;
        }
    }
}