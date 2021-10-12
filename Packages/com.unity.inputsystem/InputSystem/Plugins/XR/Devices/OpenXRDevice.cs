using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Packages.com.unity.inputsystem.InputSystem.Plugins.XR.Devices
{
    public class OpenXRDevice : InputDevice, IInputStateCallbackReceiver
    {
	    private Dictionary<string, InputControl> openXrActionToInputControlMap;

	    public OpenXRDevice()
	    {
		    UnityEngine.InputSystem.InputSystem.settings.globalInputActions
	    }

	    public void OnNextUpdate()
	    {
		    
	    }

	    public unsafe void OnStateEvent(InputEventPtr eventPtr)
	    {
		    var evt = (OpenXRState*)StateEvent.From(eventPtr);

			InputState.Change();
	    }

	    public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
	    {
		    throw new NotImplementedException();
	    }
    }

    public class OpenXRState
    {
    }
}
