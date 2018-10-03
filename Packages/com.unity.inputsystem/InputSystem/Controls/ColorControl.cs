using System;

////REVIEW: do we want to have a separate control for this or should this just use Vector3Control?

namespace UnityEngine.Experimental.Input.Controls
{
    public class ColorControl : InputControl<Color>
    {
        public override Color ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }
    }
}
