using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

namespace Packages.com.unity.inputsystem.InputSystem.Plugins.XR.Devices
{
    public class OpenXRDevice : InputDevice, IInputStateCallbackReceiver
    {
	    private Dictionary<int, InputControl> openXrActionToInputControlMap;

	    public OpenXRDevice()
	    {
		    openXrActionToInputControlMap = new Dictionary<int, InputControl>();
	    }

	    public void OnNextUpdate()
	    {
		    
	    }

	    public unsafe void OnStateEvent(InputEventPtr eventPtr)
	    {
		    var evt = StateEvent.From(eventPtr);
		    var openXRState = (OpenXRState*)evt->state;

		    if (openXrActionToInputControlMap.TryGetValue(openXRState->ActionId, out var control) == false)
			    return;

		    switch (control.stateBlock.format)
		    {
			    case InputStateBlock.kFormatFloat:
				    InputState.Change(control, openXRState->FloatValue, eventPtr: eventPtr);
				    break;

			    case InputStateBlock.kFormatVector2:
				    InputState.Change(control, openXRState->VectorValue, eventPtr: eventPtr);
				    break;
				    
				default:
					throw new InvalidOperationException("Unsupported XR control type.");
		    }
	    }

	    public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
	    {
		    throw new NotImplementedException();
	    }

	    public PoseState GetPose(float time, int actionId)
	    {
			// IOCTL call here
			return new PoseState();
	    }

	    public static InputControlLayout BuildLayout(string layoutName, IEnumerable<InputAction> actions)
	    {
		    var builder = new InputControlLayout.Builder()
			    .WithName(layoutName)
			    .WithType<OpenXRDevice>();
  
		    foreach (var action in actions)
		    {
				builder.AddControl(action.name) // Must not have actions in separate maps with the same name.
				    .WithLayout(action.expectedControlType);
		    }
  
		    return builder.Build();
	    }

	    protected override void FinishSetup()
	    {
		    base.FinishSetup();

		    foreach (var inputControl in allControls)
		    {
			    openXrActionToInputControlMap.Add(inputControl.name.GetHashCode(), inputControl);
		    }
	    }
    }

	[StructLayout(LayoutKind.Explicit)]
    public struct OpenXRState : IInputStateTypeInfo
    {
		[FieldOffset(0)] public int ActionId;

		[FieldOffset(4)] public float BooleanValue;
		[FieldOffset(4)] public float FloatValue;
		[FieldOffset(4)] public Vector2 VectorValue;
		[FieldOffset(4)] public bool PoseValue; // active

		public FourCC format => new FourCC("OPXR");
    }

    public class OpenXRBindingPathParser : IBindingPathParser
    {
	    public InputControl TryFindInputControl(InputControl parent, string path, string action)
	    {
		    if (!path.StartsWith("OpenXR:"))
			    return null;

		    if (!(parent is OpenXRDevice device))
			    return null;

		    return InputControlPath.TryFindChild(device, action);
	    }

	    public string ToHumanReadableString(string path)
	    {
		    throw new NotImplementedException();
	    }
    }

    public interface IBindingPathParser
    {
	    InputControl TryFindInputControl(InputControl parent, string path, string action = null);
	    string ToHumanReadableString(string path);
    }
}
