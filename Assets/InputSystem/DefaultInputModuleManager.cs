namespace ISX
{
    // If no module manager has been set up, this one is taken as a fallback.
    // Scans for all input modules present in the code and initializes any
    // that is compatible with the current runtime platform.
    public class DefaultInputModuleManager : IInputModuleManager
    {
        public void Initialize()
        {
        }
    }
}
