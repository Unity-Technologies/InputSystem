////REVIEW: Ideally, we'd just have an IoC solution in the engine proper and would be able to use that.
////        AFAIK it's coming and when we have that, we should switch over to that.

namespace ISX
{
    /// <summary>
    /// A plugin manager determines which InputPlugins are being used.
    /// </summary>
    /// <remarks>
    /// Having plugin managers supersedes the default behavior of using
    /// reflection to find all types marked with [InputPlugin] instances
    /// in the system and initializing all of them.
    /// </remarks>
    public interface IInputPluginManager
    {
        void InitializePlugins();
    }
}
