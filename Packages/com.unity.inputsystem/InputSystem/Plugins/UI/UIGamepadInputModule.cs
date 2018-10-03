using System;
using UnityEngine.EventSystems;

//this should also work with joysticks

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// <see cref="EventSystem">UI input module</see> that translates <see cref="Gamepad"/>
    /// input to UI input.
    /// </summary>
    public class UIGamepadInputModule : UIInputModule
    {
        /// <summary>
        /// Gamepad devices
        /// </summary>
        /// <remarks>
        /// This can be used to set up receiving input from general types of devices as well
        /// as receiving input from specific devices.
        /// </remarks>
        /// <example>
        /// <code>
        ///
        /// </code>
        /// </example>
        public InputDeviceReference devices;

        //ability to set control paths that drive the various kinds of input

        public override void Process()
        {
            throw new NotImplementedException();
        }
    }
}
