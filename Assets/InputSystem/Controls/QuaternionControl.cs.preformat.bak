using UnityEngine;

namespace ISX
{
    public class QuaternionControl : InputControl<Quaternion>
    {
        // No component controls as doing individual operations on the xyzw components of a quaternion
        // doesn't really make sense as individual input controls.

        public QuaternionControl()
        {
	        m_StateBlock.sizeInBits = sizeof(float)*4*8;
        }

        public override Quaternion value
        {
			get
			{
				unsafe
				{
					var buffer = (byte*) currentStatePtr;
					var x = *((float*) &buffer[m_StateBlock.byteOffset]);
					var y = *((float*) &buffer[m_StateBlock.byteOffset+4]);
					var z = *((float*) &buffer[m_StateBlock.byteOffset+8]);
				    var w = *((float*) &buffer[m_StateBlock.byteOffset+12]);
				    return Process(new Quaternion(x, y, z, w));
				}
			}
        }
    }
}