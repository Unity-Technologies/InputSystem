using System;

////REVIEW: should this *not* be inherited? inheritance can lead to surprises

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// Attribute to control layout settings of a type used to generate an <see cref="InputControlLayout"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class InputControlLayoutAttribute : Attribute
    {
        /// <summary>
        /// Associates a state representation with an input device and drives
        /// the control layout generated for the device from its state rather
        /// than from the device class.
        /// </summary>
        /// <remarks>This is *only* useful if you have a state struct dictating a specific
        /// state layout and you want the device layout to automatically take offsets from
        /// the fields annotated with <see cref="InputControlAttribute"/>.
        /// </remarks>
        public Type stateType;

        public string stateFormat;

        ////TODO: rename this to just "usages"; "commonUsages" is such a weird name
        public string[] commonUsages;

        public string variants;

        public bool? updateBeforeRender;

        public bool beforeRender
        {
            get => updateBeforeRender.Value;
            set => updateBeforeRender = value;
        }

        /// <summary>
        /// If true, the layout describes a generic class of devices such as "gamepads" or "mice".
        /// </summary>
        /// <remarks>
        /// This property also determines how the layout is presented in the UI. All the device layouts
        /// that are marked as generic kinds of devices are displayed with their own entry at the root level of
        /// the control picker (<see cref="UnityEngine.Experimental.Input.Editor.InputControlPicker"/>), for example.
        /// </remarks>
        public bool isGenericTypeOfDevice;

        /// <summary>
        /// Gives a name to display in the UI. By default, the name is the same as the class the attribute
        /// is applied to.
        /// </summary>
        public string displayName;

        public string description;

        /// <summary>
        /// If true, don't include the layout when presenting picking options in the UI.
        /// </summary>
        /// <remarks>
        /// This will keep device layouts out of the control picker and will keep control layouts out of
        /// action type dropdowns.
        /// </remarks>
        public bool hideInUI;
    }
}
