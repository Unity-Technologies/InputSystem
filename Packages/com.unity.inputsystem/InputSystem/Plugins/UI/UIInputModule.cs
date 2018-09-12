using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// Base class for <see cref="BaseInputModule">input modules</see> that send
    /// UI input.
    /// </summary>
    /// <remarks>
    /// Multiple input modules may be placed on the same event system. In such a setup,
    /// the modules will synchronize with each other to not send
    /// </remarks>
    public abstract class UIInputModule : BaseInputModule
    {
    }
}
