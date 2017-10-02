using UnityEngine;

namespace ISX
{
	// Computes the magnitude of a Vector3.
	// You can add this as a child control of a 3D vector, for example, so as to get a magnitude
	// input control in addition to the X, Y, and Z component controls. The state is shared with the
	// vector itself.
	public class Magnitude3Control : InputControl<float>
	{
		public Magnitude3Control()
		{
			m_StateBlock.sizeInBits = sizeof(float)*3*8;
		}

		public override unsafe float value
		{
			get
			{
				var buffer = (byte*) currentStatePtr;
				var x = *((float*) &buffer[m_StateBlock.byteOffset]);
				var y = *((float*) &buffer[m_StateBlock.byteOffset+4]);
				var z = *((float*) &buffer[m_StateBlock.byteOffset+8]);
				return new Vector3(x, y, z).magnitude;
			}
		}
	}
}
