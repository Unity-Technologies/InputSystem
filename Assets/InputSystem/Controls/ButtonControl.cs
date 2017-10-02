using System;

namespace ISX
{
	////REVIEW: shouldn't this still have a float value type?
	public class ButtonControl : InputControl<bool>
	{
		public ButtonControl()
		{
			stateBlock.sizeInBits = 1;
		}
		
		public override bool value
		{
		    get { return GetValue(currentStatePtr); }
		}

	    public bool wasPressedThisFrame
	    {
	        get { return value != GetValue(previousStatePtr); }
	    }
		
		protected unsafe bool GetValue(IntPtr statePtr)
	    {
			var buffer = (byte*) statePtr;
	        return Process((buffer[stateBlock.byteOffset] & (1 << (int)stateBlock.bitOffset)) == (byte) (1 << (int)stateBlock.bitOffset));
	    }
	}
}