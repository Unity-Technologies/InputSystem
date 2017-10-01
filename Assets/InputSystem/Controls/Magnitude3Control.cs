using UnityEngine;

namespace InputSystem
{
	// Computes the magnitude of a Vector3.
	// You can add this as a child control of a 3D vector, for example, so as to get a magnitude
	// input control in addition to the X, Y, and Z component controls. The state is shared with the
	// vector itself.
	public class Magnitude3Control : InputControl<float>
	{
		public Magnitude3Control(string name)
			: base(name)
		{
			stateBlock.sizeInBits = sizeof(float)*3*8;
		}

		public override unsafe float value
		{
			get
			{
				var buffer = (byte*) currentStatePtr;
				var x = *((float*) &buffer[stateBlock.byteOffset]);
				var y = *((float*) &buffer[stateBlock.byteOffset+4]);
				var z = *((float*) &buffer[stateBlock.byteOffset+8]);
				return new Vector3(x, y, z).magnitude;
			}
		}
	}
}
