using UnityEngine;

namespace ISX
{
	// Computes the magnitude of a Vector2.
	// You can add this as a child control of a 2D vector, for example, so as to get a magnitude
	// input control in addition to the X and Y component controls. The state is shared with the
	// vector itself.
	public class Magnitude2Control : InputControl<float>
	{
		public Magnitude2Control()
		{
			m_StateBlock.sizeInBits = sizeof(float)*2*8;
		}

		public override unsafe float value
		{
			get
			{
                var buffer = (byte*) currentStatePtr;
                var x = *((float*) &buffer[m_StateBlock.byteOffset]);
                var y = *((float*) &buffer[m_StateBlock.byteOffset+4]);
				return new Vector2(x, y).magnitude;
			}
		}
	}
}