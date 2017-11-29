using System;
using UnityEngine;

////REVIEW: set up the plugin stuff such that if *nothing* is set up, we pull in whatever we got on the fly?

namespace ISX
{
    /// <summary>
    /// Marks a class that as an input plugin which registers additional functionality with
    /// the input system.
    /// </summary>
    /// <remarks>
    /// The class should be static and should have a <c>public static void Initialize()</c>
    /// method which performs any registration/initialization steps necessary for the module.
    /// </remarks>
    /// <example>
    /// <code>
    /// [InputPlugin]
    /// public static class MyDeviceSupport
    /// {
    ///     public static void Initialize()
    ///     {
    ///         InputSystem.RegisterTemplate<MyDevice>();
    ///         InputSystem.AddDevice("MyDevice");
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class InputPluginAttribute : Attribute
    {
        public string description;
        public RuntimePlatform[] supportedPlatforms;
    }
}
