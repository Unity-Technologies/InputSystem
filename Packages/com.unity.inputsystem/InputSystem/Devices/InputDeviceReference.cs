using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Reference to one or more <see cref="InputDevice">input devices</see> which can be persisted
    /// and also graphically edited in the editor.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Serializable]
    public struct InputDeviceReference
    {
        public string devicePath
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ReadOnlyArray<InputDevice> devices
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Set reference to point to one specific device.
        /// </summary>
        /// <param name="device"></param>
        public void Set(InputDevice device)
        {
            throw new NotImplementedException();
        }
    }
}
