using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputSystem.Switch
{
    /// <summary>
    /// Base class for Nintendo Switch Pro Controllers that provides the correct button mappings for Nintendo's face button layout where A is east, B is south, X is north, and Y is west.
    /// If you use InputSystem.GetDevice and the ABXY properties to represent the labels on the device, you must query for this class
    /// </summary>
    public abstract class SwitchProController : Gamepad
    {
        /// <summary>
        /// A Button for a Nintendo Switch Pro Controller.
        /// If querying via script, ensure you cast the device to a Switch Pro Controller class, rather than using the Gamepad class.
        /// The gamepad class will return the state of buttonSouth, whereas this class returns the state of buttonEast
        /// </summary>
        public new ButtonControl aButton => buttonEast;

        /// <summary>
        /// B Button for a Nintendo Switch Pro Controller.
        /// If querying via script, ensure you cast the device to a Switch Pro Controller class, rather than using the Gamepad class.
        /// The gamepad class will return the state of buttonEast, whereas this class returns the state of buttonSouth
        /// </summary>
        public new ButtonControl bButton => buttonSouth;

        /// <summary>
        /// Y Button for a Nintendo Switch Pro Controller.
        /// If querying via script, ensure you cast the device to a Switch Pro Controller class, rather than using the Gamepad class.
        /// The gamepad class will return the state of buttonNorth, whereas this class returns the state of buttonWest
        /// </summary>
        public new ButtonControl yButton => buttonWest;

        /// <summary>
        /// X Button for a Nintendo Switch Pro Controller.
        /// If querying via script, ensure you cast the device to a Switch Pro Controller class, rather than using the Gamepad class.
        /// The gamepad class will return the state of buttonWest, whereas this class returns the state of buttonNorth
        /// </summary>
        public new ButtonControl xButton => buttonNorth;
    }
}
