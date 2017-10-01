using UnityEngine;

namespace InputSystem
{
    public class QuaternionControl : InputControl<Quaternion>
    {
        // No component controls as doing individual operations on the xyzw components of a quaternion
        // doesn't really make sense as individual input controls.

        public QuaternionControl(string name)
            : base(name)
        {
	        stateBlock.sizeInBits = sizeof(float)*4*8;
        }

        public override Quaternion value
        {
			get
			{
				unsafe
				{
					var buffer = (byte*) currentStatePtr;
					var x = *((float*) &buffer[stateBlock.byteOffset]);
					var y = *((float*) &buffer[stateBlock.byteOffset+4]);
					var z = *((float*) &buffer[stateBlock.byteOffset+8]);
				    var w = *((float*) &buffer[stateBlock.byteOffset+12]);
				    return Process(new Quaternion(x, y, z, w));
				}
			}
        }
    }
}