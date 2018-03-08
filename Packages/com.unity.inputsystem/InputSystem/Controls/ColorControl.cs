using System;
using UnityEngine;

////REVIEW: do we want to have a separate control for this or should this just use Vector3Control?

namespace ISX.Controls
{
    public class ColorControl : InputControl<Color>
    {
        public override Color ReadRawValueFrom(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }
    }
}
