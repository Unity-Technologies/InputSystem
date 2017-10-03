using UnityEngine;

namespace ISX
{
    public class QuaternionControl : InputControl<Quaternion>
    {
        // No component controls as doing individual operations on the xyzw components of a quaternion
        // doesn't really make sense as individual input controls.

        public QuaternionControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 4 * 8;
        }

        public override Quaternion value
        {
            get
            {
                unsafe
                {
                    var values = (float*)currentValuePtr;
                    var x = values[0];
                    var y = values[1];
                    var z = values[2];
                    var w = values[3];
                    return Process(new Quaternion(x, y, z, w));
                }
            }
        }
    }
}
