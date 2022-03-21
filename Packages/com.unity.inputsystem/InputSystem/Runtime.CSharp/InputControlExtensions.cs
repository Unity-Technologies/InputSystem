using System;
using Unity.InputSystem.Runtime;

#if true

namespace Unity.InputSystem
{
    public partial struct InputButtonControl
    {
        public bool isPressed => value;
        public bool isNotPressed => !value;
        public bool wasPressedThisIOFrame => GetState().wasPressedThisIOFrame;
        public bool wasReleasedThisIOFrame => GetState().wasReleasedThisIOFrame;
    }
    
    public partial struct InputDerivedButtonControl
    {
        public bool isPressed => value;
        public bool isNotPressed => !value;
        public bool wasPressedThisIOFrame => GetState().wasPressedThisIOFrame;
        public bool wasReleasedThisIOFrame => GetState().wasReleasedThisIOFrame;
    }
}

#endif