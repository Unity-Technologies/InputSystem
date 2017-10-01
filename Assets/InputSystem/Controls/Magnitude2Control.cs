using UnityEngine;

namespace InputSystem
{
	// Computes the magnitude of a Vector2.
	// You can add this as a child control of a 2D vector, for example, so as to get a magnitude
	// input control in addition to the X and Y component controls. The state is shared with the
	// vector itself.
	public class Magnitude2Control : InputControl<float>
	{
		public Magnitude2Control(string name)
			: base(name)
		{
			stateBlock.sizeInBits = sizeof(float)*2*8;
		}

		public override unsafe float value
		{
			get
			{
                var buffer = (byte*) currentStatePtr;
                var x = *((float*) &buffer[stateBlock.byteOffset]);
                var y = *((float*) &buffer[stateBlock.byteOffset+4]);
				return new Vector2(x, y).magnitude;
			}
		}
	}
}