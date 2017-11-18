using System;
using UnityEngine;

////REVIEW: do we want to have a separate control for this or should this just use Vector3Control?

namespace ISX
{
    public class ColorControl : InputControl<Color>
    {
        protected override Color ReadRawValueFrom(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }
    }
}
