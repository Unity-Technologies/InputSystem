using System;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Mark a field or property as representing/identifying an input control in some form.
    /// </summary>
    /// <remarks>
    /// This attribute is used in different places for different purposes.
    ///
    /// When creating input control layouts (<see cref="InputControlLayout"/>) in C#, applying the
    /// attribute to fields in a state struct (see <see cref="IInputStateTypeInfo"/> or <see cref="GamepadState"/>
    /// for an example) or to properties in an input device (<see cref="InputDevice"/>), will cause an
    /// <see cref="InputControl"/> to be created from the field or property at runtime. The attribute
    /// can be applied multiple times to create multiple input controls (e.g. when having an int field
    /// that represents a bitfield where each bit is a separate button).
    ///
    /// <example>
    /// <code>
    /// public class MyDevice : InputDevice
    /// {
    ///     // Adds an InputControl with name=myButton and layout=Button to the device.
    ///     [InputControl]
    ///     public ButtonControl myButton { get; set; }
    /// }
    /// </code>
    /// </example>
    ///
    /// Another use is for marking <c>string</c> type fields that represent input control paths. Applying
    /// the attribute to them will cause them to automatically use a custom inspector similar to the one
    /// found in the action editor. For this use, only the <see cref="layout"/> property is taken into
    /// account.
    ///
    /// <example>
    /// <code>
    /// public class MyBehavior : MonoBehaviour
    /// {
    ///     // In the inspector, shows a control selector that is restricted to
    ///     // selecting buttons. As a result, controlPath will be set to path
    ///     // representing the control that was picked (e.g. "&lt;Gamepad&gt;/buttonSouth").
    ///     [InputControl(layout = "Button")]
    ///     public string controlPath;
    ///
    ///     protected void OnEnable()
    ///     {
    ///         // Find controls by path.
    ///         var controls = InputSystem.FindControl(controlPath);
    ///         //...
    ///     }
    /// }
    /// </code>
    /// </example>
    ///
    /// Finally, the attribute is also used in composite bindings (<see cref="InputBindingComposite"/>)
    /// to mark fields that reference parts of the composite. An example for this is <see cref="AxisComposite.negative"/>.
    /// In this use, also only the <see cref="layout"/> property is taken into account while other properties
    /// are ignored.
    ///
    /// <example>
    /// <code>
    /// public class MyComposite : InputBindingComposite&lt;float&gt;
    /// {
    ///     // Add a part to the composite called 'firstControl' which expects
    ///     // AxisControls.
    ///     [InputControl(layout = "Axis")]
    ///     public int firstControl;
    ///
    ///     // Add a part to the composite called 'secondControl' which expects
    ///     // Vector3Controls.
    ///     [InputControl(layout = "Vector3")]
    ///     public int secondControl;
    ///
    ///     //...
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputControlLayout"/>
    /// <seealso cref="InputBindingComposite"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class InputControlAttribute : PropertyAttribute
    {
        /// <summary>
        /// Layout to use for the control.
        /// </summary>
        /// <value>Layout to use for the control.</value>
        /// <remarks>
        /// If this is not set, the system tries to infer the layout type from the value type of
        /// the field or property. If the value type is itself registered as a layout, that layout
        /// will be used (e.g. when you have a property of type <see cref="Controls.ButtonControl"/>, the layout
        /// will be inferred to be "Button"). Otherwise, if a layout with the same name as the type is registered,
        /// that layout will be used (e.g. when you have a field of type <see cref="Vector3"/>, the layout
        /// will be inferred to be "Vector3").
        /// </remarks>
        /// <seealso cref="InputControlLayout"/>
        public string layout { get; set; }

        /// <summary>
        /// Layout variant to use for the control.
        /// </summary>
        /// <value>Layout variant to use for the control.</value>
        public string variants { get; set; }

        /// <summary>
        /// Name to give to the name. If null or empty, the name of the property or
        /// field the attribute is applied to will be used.
        /// </summary>
        /// <value>Name to give to the control.</value>
        /// <seealso cref="InputControl.name"/>
        public string name { get; set; }

        /// <summary>
        /// Storage format to use for the control. If not set, default storage format
        /// for the given <see cref="layout"/> is used.
        /// </summary>
        /// <value>Memory storage format to use for the control.</value>
        /// <seealso cref="InputStateBlock.format"/>
        public string format { get; set; }

        /// <summary>
        /// Usage to apply to the control.
        /// </summary>
        /// <value>Usage for the control.</value>
        /// <remarks>
        /// This property can be used in place of <see cref="usages"/> to set just a single
        /// usage on the control.
        /// </remarks>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="InputControlLayout.ControlItem.usages"/>
        /// <seealso cref="CommonUsages"/>
        public string usage { get; set; }

        /// <summary>
        /// Usages to apply to the control.
        /// </summary>
        /// <value>Usages for the control.</value>
        /// <remarks>
        /// This property should be used instead of <see cref="usage"/> when a control has multiple usages.
        /// </remarks>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="InputControlLayout.ControlItem.usages"/>
        /// <seealso cref="CommonUsages"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "According to MSDN, this message can be ignored for attribute parameters, as there are no better alternatives.")]
        public string[] usages { get; set; }

        /// <summary>
        /// Optional list of parameters to apply to the control.
        /// </summary>
        /// <value>Parameters to apply to the control.</value>
        /// <remarks>
        /// An <see cref="InputControl"/> may expose public fields which can be set as
        /// parameters. An example of this is <see cref="Controls.AxisControl.clamp"/>.
        ///
        /// <example>
        /// <code>
        /// public struct MyStateStruct : IInputStateTypeInfo
        /// {
        ///     [InputControl(parameters = "clamp,clampMin=-0.5,clampMax=0.5")]
        ///     public float axis;
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.parameters"/>
        public string parameters { get; set; }

        /// <summary>
        /// Optional list of processors to add to the control.
        /// </summary>
        /// <value>Processors to apply to the control.</value>
        /// <remarks>
        /// Each element in the list is a name of a processor (as registered with
        /// <see cref="InputSystem.RegisterProcessor{T}"/>) followed by an optional
        /// list of parameters.
        ///
        /// For example, <c>"normalize(min=0,max=256)"</c> is one element that puts
        /// a <c>NormalizeProcessor</c> on the control and sets its <c>min</c> field
        /// to 0 and its its <c>max</c> field to 256.
        ///
        /// Multiple processors can be put on a control by separating them with a comma.
        /// For example, <c>"normalize(max=256),scale(factor=2)"</c> puts both a <c>NormalizeProcessor</c>
        /// and a <c>ScaleProcessor</c> on the control. Processors are applied in the
        /// order they are listed.
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.processors"/>
        /// <seealso cref="InputBinding.processors"/>
        public string processors { get; set; }

        /// <summary>
        /// An alternative name that can be used in place of <see cref="name"/> to find
        /// the control.
        /// </summary>
        /// <value>Alternative name for the control.</value>
        /// <remarks>
        /// This property can be used instead of <see cref="aliases"/> when there is only a
        /// single alias for the control.
        ///
        /// Aliases, like names, are case-insensitive. Any control may have arbitrary many
        /// aliases.
        /// </remarks>
        /// <seealso cref="InputControl.aliases"/>
        /// <seealso cref="InputControlLayout.ControlItem.aliases"/>
        public string alias { get; set; }

        /// <summary>
        /// A list of alternative names that can be used in place of <see cref="name"/> to
        /// find the control.
        /// </summary>
        /// <value>Alternative names for the control.</value>
        /// <remarks>
        /// This property should be used instead of <see cref="alias"/> when a control has
        /// multiple aliases.
        ///
        /// Aliases, like names, are case-insensitive. Any control may have arbitrary many
        /// aliases.
        /// </remarks>
        /// <seealso cref="InputControl.aliases"/>
        /// <seealso cref="InputControlLayout.ControlItem.aliases"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "According to MSDN, this message can be ignored for attribute parameters, as there are no better alternatives.")]
        public string[] aliases { get; set; }

        public string useStateFrom { get; set; }

        public uint bit { get; set; } = InputStateBlock.InvalidOffset;

        /// <summary>
        /// Offset in bytes to where the memory of the control starts. Relative to
        /// the offset of the parent control (which may be the device itself).
        /// </summary>
        /// <value>Byte offset of the control.</value>
        /// <remarks>
        /// If the attribute is applied to fields in an <see cref="InputControlLayout"/> and
        /// this property is not set, the offset of the field is used instead.
        ///
        /// <example>
        /// <code>
        /// public struct MyStateStruct : IInputStateTypeInfo
        /// {
        ///     public int buttons;
        ///
        ///     [InputControl] // Automatically uses the offset of 'axis'.
        ///     public float axis;
        /// }
        ///
        /// [InputControlLayout(stateType = typeof(MyStateStruct))]
        /// public class MyDevice : InputDevice
        /// {
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.offset"/>
        public uint offset { get; set; } = InputStateBlock.InvalidOffset;

        /// <summary>
        /// Size of the memory storage for the control in bits.
        /// </summary>
        /// <value>Size of the control in bits.</value>
        /// <remarks>
        /// If the attribute is applied to fields in an <see cref="InputControlLayout"/> and
        /// this property is not set, the size is taken from the field.
        ///
        /// <example>
        /// <code>
        /// public struct MyStateStruct : IInputStateTypeInfo
        /// {
        ///     public int buttons;
        ///
        ///     [InputControl] // Automatically uses sizeof(float).
        ///     public float axis;
        /// }
        ///
        /// [InputControlLayout(stateType = typeof(MyStateStruct))]
        /// public class MyDevice : InputDevice
        /// {
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.sizeInBits"/>
        /// <seealso cref="InputStateBlock.sizeInBits"/>
        public uint sizeInBits { get; set; }

        public int arraySize { get; set; }

        /// <summary>
        /// Display name to assign to the control.
        /// </summary>
        /// <value>Display name for the control.</value>
        /// <seealso cref="InputControl.displayName"/>
        /// <seealso cref="InputControlLayout.ControlItem.displayName"/>
        public string displayName { get; set; }

        /// <summary>
        /// Short display name to assign to the control.
        /// </summary>
        /// <value>Short display name for the control.</value>
        /// <seealso cref="InputControl.shortDisplayName"/>
        /// <seealso cref="InputControlLayout.ControlItem.shortDisplayName"/>
        public string shortDisplayName { get; set; }

        /// <summary>
        /// Retrieves whether the control is [noisy](xref:input-system-controls#noisy-controls). Off by default.
        /// </summary>
        /// <value>Whether control is noisy.</value>
        /// <seealso cref="InputControl.noisy"/>
        /// <seealso cref="InputControlLayout.ControlItem.isNoisy"/>
        public bool noisy { get; set; }

        /// <summary>
        /// Retrieves whether the control is [synthetic](xref:input-system-controls#synthetic-controls). Off by default.
        /// </summary>
        /// <value>Whether control is synthetic.</value>
        /// <seealso cref="InputControl.synthetic"/>
        /// <seealso cref="InputControlLayout.ControlItem.isSynthetic"/>
        public bool synthetic { get; set; }

        /// <summary>
        /// Allows you to specify that a control should not be reset when its device is reset.
        /// </summary>
        /// <value>If true, resets of the device will leave the value of the control untouched except if a "hard" reset
        /// is explicitly enforced.</value>
        /// <seealso cref="InputSystem.ResetDevice"/>
        /// <seealso cref="InputControlLayout.ControlItem.dontReset"/>
        public bool dontReset { get; set; }

        /// <summary>
        /// Default state to write into the control's memory.
        /// </summary>
        /// <value>Default memory state for the control.</value>
        /// <remarks>
        /// This is not the default <em>value</em> but rather the default memory state, i.e.
        /// the raw memory value read and the processed and returned as a value. By default
        /// this is <c>null</c> and result in a control's memory to be initialized with all
        /// zeroes.
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.defaultState"/>
        public object defaultState { get; set; }

        /// <summary>
        /// Lower limit for values of the control.
        /// </summary>
        /// <value>Lower limit for values of the control.</value>
        /// <remarks>
        /// This is null by default in which case no lower bound is applied to the TODO
        /// </remarks>
        public object minValue { get; set; }
        public object maxValue { get; set; }
    }
}
