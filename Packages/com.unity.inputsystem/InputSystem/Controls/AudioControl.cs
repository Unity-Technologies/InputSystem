using System;
using ISX.Utilities;

////TODO: this control needs to be able to blur the line between input and output controls; it has to be both

namespace ISX.Controls
{
    public class AudioControl : InputControl<AudioBuffer>
    {
        public override AudioBuffer ReadRawValueFrom(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }
    }
}
