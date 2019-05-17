using System;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Mark a field or property as representing/identifying an input control in some form.
    /// </summary>
    /// <remarks>
    /// This attribute is used in different places for different purposes.
    ///
    /// When creating input control layouts (<see cref="InputControlLayout"/>) in C#, applying the
    /// attribute to fields in a state struct (<see cref="IInputStateTypeInfo"/>, or <see cref="GamepadState"/>
    /// for an example) or to properties in an input device (<see cref="InputDevice"/>), will cause an
    /// <see cref="InputControl"/> to be created from the field or property at runtime. The attribute
    /// can be applied multiple times to create multiple input controls (e.g. when having an int field
    /// that represents a bitfield where each bit is a separate button).
    ///
    /// Another use is for marking <c>string</c> type fields that represent input control paths. Applying
    /// the attribute to them will cause them to automatically use <see cref="InputControlPathDrawer"/>
    /// when edited in inspectors.
    ///
    /// Finally, the attribute is also used in composite bindings (<see cref="InputBindingComposite"/>)
    /// to mark fields that reference parts of the composite. An example for this is <see cref="AxisComposite.negative"/>.
    /// </remarks>
    /// <seealso cref="InputControlLayout"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class InputControlAttribute : PropertyAttribute
    {
        public string layout { get; set; }
        public string variants { get; set; }
        public string name { get; set; }
        public string format { get; set; }
        public string usage { get; set; }
        public string[] usages { get; set; }
        public string parameters { get; set; }
        public string processors { get; set; }
        public string alias { get; set; }
        public string[] aliases { get; set; }
        public string useStateFrom { get; set; }
        public uint bit { get; set; } = InputStateBlock.InvalidOffset;
        public uint offset { get; set; } = InputStateBlock.InvalidOffset;
        public uint sizeInBits { get; set; }
        public int arraySize { get; set; }
        public string displayName { get; set; }
        public string shortDisplayName { get; set; }
        public bool noisy { get; set; }
        public bool synthetic { get; set; }
        public object defaultState { get; set; }
        public object minValue { get; set; }
        public object maxValue { get; set; }
    }
}
