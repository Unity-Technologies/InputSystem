using System;

namespace InputSystem
{
	////REVIEW: shouldn't this still have a float value type?
	public class ButtonControl : InputControl<bool>
	{
		public ButtonControl(string name)
			: base(name)
		{
		}

	    protected unsafe bool GetValue(IntPtr statePtr)
	    {
			var buffer = (byte*) statePtr;
	        return Process((buffer[stateBlock.byteOffset] & (1 << (int)stateBlock.bitOffset)) == (byte) (1 << (int)stateBlock.bitOffset));
	    }

		public override bool value
		{
		    get { return GetValue(currentStatePtr); }
		}

	    public bool wasPressedThisFrame
	    {
	        get { return value != GetValue(previousStatePtr); }
	    }
	}
}