using System;

////TODO: this control needs to be able to blur the line between input and output controls; it has to be both

namespace ISX
{
    public class AudioControl : InputControl<AudioBuffer>
    {
        protected override AudioBuffer ReadRawValueFrom(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }
    }
}
