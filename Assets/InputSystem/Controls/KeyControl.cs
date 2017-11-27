////TODO: expose key information

////TODO: make it so that you can query a display name from a KeyControl (any control really) such that it correctly
////      takes the current keyboard layout into account

namespace ISX
{
    public class KeyControl : ButtonControl
    {
        public int scanCode
        {
            get { return 0; }
        }
    }
}
