using System;
using UnityEngine.Experimental.Input.Composites;
using UnityEngine.Experimental.Input.LowLevel;

#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Editor;
#endif

namespace UnityEngine.Experimental.Input.Layouts
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
        public string layout;
        public string variants;
        public string name;
        public string format;
        public string usage;
        public string[] usages;
        public string parameters;
        public string processors;
        public string alias;
        public string[] aliases;
        public string useStateFrom;
        public uint bit = InputStateBlock.kInvalidOffset;
        public uint offset = InputStateBlock.kInvalidOffset;
        public uint sizeInBits;
        public int arraySize;
        public string displayName;
        public string shortDisplayName;
        public bool noisy;
        public bool synthetic;
        public object defaultState;
        public object minValue;
        public object maxValue;
    }
}
