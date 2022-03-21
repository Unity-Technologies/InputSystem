using Unity.InputSystem.Runtime;

namespace Unity.InputSystem
{
    public static class InputCurrentAPIContext
    {
        public static InputFramebufferRef
            CurrentFramebuffer = new InputFramebufferRef {transparent = 0};
    }
}