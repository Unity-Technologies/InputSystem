using System;
using UnityEngine.Scripting;

////REVIEW: should this *not* be inherited? inheritance can lead to surprises

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Attribute to control layout settings of a type used to generate an <see cref="InputControlLayout"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class InputControlLayoutAttribute : Attribute
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
        public Type stateType { get; set; }

        public string stateFormat { get; set; }

        ////TODO: rename this to just "usages"; "commonUsages" is such a weird name
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "According to MSDN, this message can be ignored for attribute parameters, as there are no better alternatives.")]
        public string[] commonUsages { get; set; }

        public string variants { get; set; }

        internal bool? updateBeforeRenderInternal;

        /// <summary>
        /// Whether the device should receive events in <see cref="LowLevel.InputUpdateType.BeforeRender"/> updates.
        /// </summary>
        /// <seealso cref="InputDevice.updateBeforeRender"/>
        public bool updateBeforeRender
        {
            get => updateBeforeRenderInternal.Value;
            set => updateBeforeRenderInternal = value;
        }

        /// <summary>
        /// If true, the layout describes a generic class of devices such as "gamepads" or "mice".
        /// </summary>
        /// <remarks>
        /// This property also determines how the layout is presented in the UI. All the device layouts
        /// that are marked as generic kinds of devices are displayed with their own entry at the root level of
        /// the control picker (<see cref="UnityEngine.InputSystem.Editor.InputControlPicker"/>), for example.
        /// </remarks>
        public bool isGenericTypeOfDevice { get; set; }

        /// <summary>
        /// Gives a name to display in the UI. By default, the name is the same as the class the attribute
        /// is applied to.
        /// </summary>
        public string displayName { get; set; }

        public string description { get; set; }

        /// <summary>
        /// If true, don't include the layout when presenting picking options in the UI.
        /// </summary>
        /// <remarks>
        /// This will keep device layouts out of the control picker and will keep control layouts out of
        /// action type dropdowns.
        /// </remarks>
        public bool hideInUI { get; set; }
    }
}
