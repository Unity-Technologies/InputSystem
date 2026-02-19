using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A typed and named source of input values in a hierarchy of controls.
    /// </summary>
    /// <remarks>
    /// Controls can have children which in turn may have children. At the root of the child
    /// hierarchy is always an <see cref="InputDevice"/> (which themselves are InputControls).
    ///
    /// Controls can be looked up by their <see cref="path"/> (see <see cref="InputControlPath.TryFindControl"/>).
    ///
    /// Each control must have a unique <see cref="name"/> within the <see cref="children"/> of
    /// its <see cref="parent"/>. Multiple names can be assigned to controls using aliases (see
    /// <see cref="aliases"/>). Name lookup is case-insensitive.
    ///
    /// For display purposes, a control may have a separate <see cref="displayName"/>. This name
    /// will usually correspond to what the control is caused on the actual underlying hardware.
    /// For example, on an Xbox gamepad, the control with the name "buttonSouth" will have a display
    /// name of "A". Controls that have very long display names may also have a <see cref="shortDisplayName"/>.
    /// This is the case for the "Left Button" on the <see cref="Mouse"/>, for example, which is
    /// commonly abbreviated "LMB".
    ///
    /// In addition to names, a control may have usages associated with it (see <see cref="usages"/>).
    /// A usage indicates how a control is meant to be used. For example, a button can be assigned
    /// the "PrimaryAction" usage to indicate it is the primary action button the device. Within a
    /// device, usages have to be unique. See <see cref="CommonUsages"/> for a list of standardized usages.
    ///
    /// Controls do not actually store values. Instead, every control receives an <see cref="InputStateBlock"/>
    /// which, after the control's device has been added to the system, is used to read out values
    /// from the device's backing store. This backing store is referred to as "state" in the API
    /// as opposed to "values" which represent the data resulting from reading state. The format that
    /// each control stores state in is specific to the control. It can vary not only between controls
    /// of different types but also between controls of the same type. An <see cref="AxisControl"/>,
    /// for example, can be stored as a float or as a byte or in a number of other formats. <see cref="stateBlock"/>
    /// identifies both where the control stores its state as well as the format it stores it in.
    ///
    /// Controls are generally not created directly but are created internally by the input system
    /// from data known as "layouts" (see <see cref="InputControlLayout"/>). Each such layout describes
    /// the setup of a specific hierarchy of controls. The system internally maintains a registry of
    /// layouts and produces devices and controls from them as needed. The layout that a control has
    /// been created from can be queried using <see cref="layout"/>. For most purposes, the intricacies
    /// of the control layout mechanisms can be ignored and it is sufficient to know the names of a
    /// small set of common device layouts such as "Keyboard", "Mouse", "Gamepad", and "Touchscreen".
    ///
    /// Each control has a single, fixed value type. The type can be queried at runtime using
    /// <see cref="valueType"/>. Most types of controls are derived from <see cref="InputControl{TValue}"/>
    /// which has APIs specific to the type of value of the control (e.g. <see cref="InputControl{TValue}.ReadValue()"/>.
    ///
    /// The following example demonstrates various common operations performed on input controls:
    /// </remarks>
    /// <example>
    /// <code>
    /// // Look up dpad/up control on current gamepad.
    /// var dpadUpControl = Gamepad.current["dpad/up"];
    ///
    /// // Look up the back button on the current gamepad.
    /// var backButton = Gamepad.current["{Back}"];
    ///
    /// // Look up all dpad/up controls on all gamepads in the system.
    /// using (var controls = InputSystem.FindControls("&lt;Gamepad&gt;/dpad/up"))
    ///     Debug.Log($"Found {controls.Count} controls");
    ///
    /// // Display the value of all controls on the current gamepad.
    /// foreach (var control in Gamepad.current.allControls)
    ///     Debug.Log(controls.ReadValueAsObject());
    ///
    /// // Track the value of the left stick on the current gamepad over time.
    /// var leftStickHistory = new InputStateHistory(Gamepad.current.leftStick);
    /// leftStickHistory.Enable();
    /// </code>
    /// </example>
    /// <seealso cref="InputControl{TValue}"/>
    /// <seealso cref="InputDevice"/>
    /// <seealso cref="InputControlPath"/>
    /// <seealso cref="InputStateBlock"/>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public abstract class InputControl
    {
        /// <summary>
        /// The name of the control, i.e. the final name part in its path.
        /// </summary>
        /// <remarks>
        /// Names of controls must be unique within the context of their parent.
        ///
        /// Note that this is the name of the control as assigned internally (like "buttonSouth")
        /// and not necessarily a good display name. Use <see cref="displayName"/> for
        /// getting more readable names for display purposes (where available).
        ///
        /// Lookup of names is case-insensitive.
        ///
        /// This is set from the name of the control in the layout.
        /// See <see cref="path"/>, <see cref="aliases"/>, <see cref="InputControlAttribute.name"/> and <see cref="InputControlLayout.ControlItem.name"/>.
        /// </remarks>
        public string name => m_Name;

        ////TODO: protect against empty strings
        /// <summary>
        /// The text to display as the name of the control.
        /// </summary>
        /// <remarks>
        /// Note that the display name of a control may change over time. For example, when changing
        /// from a QWERTY keyboard layout to an AZERTY keyboard layout, the "q" key (which will keep
        /// that <see cref="name"/>) will change its display name from "q" to "a".
        ///
        /// By default, a control's display name will come from its layout. If it is not assigned
        /// a display name there, the display name will default to <see cref="name"/>. However, specific
        /// controls may override this behavior. <see cref="KeyControl"/>, for example, will set the
        /// display name to the actual key name corresponding to the current keyboard layout.
        ///
        /// For nested controls, the display name will include the display names of all parent controls,
        /// i.e. the display name will fully identify the control on the device. For example, the display
        /// name for the left D-Pad button on a gamepad is "D-Pad Left" and not just "Left".
        ///
        /// There is a short name version, see <see cref="shortDisplayName"/>.
        /// </remarks>
        public string displayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                if (m_DisplayName != null)
                    return m_DisplayName;
                if (m_DisplayNameFromLayout != null)
                    return m_DisplayNameFromLayout;
                return m_Name;
            }
            // This is not public as a domain reload will wipe the change. This should really
            // come from the control itself *if* the control wants to have a custom display name
            // not driven by its layout.
            protected set => m_DisplayName = value;
        }

        /// <summary>
        /// An alternate, abbreviated <see cref="displayName"/> (for example "LMB" instead of "Left Button").
        /// </summary>
        /// <remarks>
        /// If the control has no abbreviated version, this will be null. Note that this behavior is different
        /// from <see cref="displayName"/> which will fall back to <see cref="name"/> if no display name has
        /// been assigned to the control.
        ///
        /// For nested controls, the short display name will include the short display names of all parent controls,
        /// that is, the display name will fully identify the control on the device. For example, the display
        /// name for the left D-Pad button on a gamepad is "D-Pad \u2190" and not just "\u2190". Note that if a parent
        /// control has no short name, its long name will be used instead. See <see cref="displayName"/>.
        /// </remarks>
        public string shortDisplayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                if (m_ShortDisplayName != null)
                    return m_ShortDisplayName;
                if (m_ShortDisplayNameFromLayout != null)
                    return m_ShortDisplayNameFromLayout;
                return null;
            }
            protected set => m_ShortDisplayName = value;
        }

        /// <summary>
        /// Full path all the way from the root.
        /// </summary>
        /// <remarks>
        /// This will always be the "effective" path of the control, i.e. it will not contain
        /// elements such as usages (<c>"{Back}"</c>) and other elements that can be part of
        /// control paths used for matching. Instead, this property will always be a simple
        /// linear ordering of names leading from the device at the top to the control with each
        /// element being separated by a forward slash (<c>/</c>).
        ///
        /// Allocates on first hit. Paths are not created until someone asks for them.
        ///
        /// For more details on the path see <see cref="InputControlPath"/>.
        /// </remarks>
        /// <example>
        /// Example: "/gamepad/leftStick/x"
        /// </example>
        public string path
        {
            get
            {
                if (m_Path == null)
                    m_Path = InputControlPath.Combine(m_Parent, m_Name);
                return m_Path;
            }
        }

        /// <summary>
        /// Layout name for the control it is based on.
        /// </summary>
        /// <remarks>
        /// This is the layout name rather than a reference to an <see cref="InputControlLayout"/> as
        /// we only create layout instances during device creation and treat them
        /// as temporaries in general so as to not waste heap space during normal operation.
        /// </remarks>
        public string layout => m_Layout;

        /// <summary>
        /// Semicolon-separated list of variants of the control layout or "default".
        /// </summary>
        /// <example>
        /// "Lefty" when using the "Lefty" gamepad layout.
        /// </example>
        public string variants => m_Variants;

        /// <summary>
        /// The device that this control is a part of.
        /// </summary>
        /// <remarks>
        /// This is the root of the control hierarchy. For the device at the root, this
        /// will point to itself (See <see cref="InputDevice.allControls"/>).
        /// </remarks>
        public InputDevice device => m_Device;

        /// <summary>
        /// The immediate parent of the control.
        /// </summary>
        /// <value>
        /// The immediate parent of the control or null if the control has no parent
        /// (which, once fully constructed, will only be the case for InputDevices).
        /// See the related <see cref="children"/> field.
        /// </value>
        public InputControl parent => m_Parent;

        /// <summary>
        /// List of immediate child controls below this.
        /// </summary>
        /// <remarks>
        /// Does not allocate.
        /// See the related <see cref="parent"/> field.
        /// </remarks>
        public ReadOnlyArray<InputControl> children =>
            new ReadOnlyArray<InputControl>(m_Device.m_ChildrenForEachControl, m_ChildStartIndex, m_ChildCount);

        /// <summary>
        /// List of usage tags associated with the control.
        /// </summary>
        /// <remarks>
        /// Usages apply "semantics" to a control. Whereas the name of a control identifies a particular
        /// "endpoint" within the control hierarchy, the usages of a control identify particular roles
        /// of specific control. A simple example is <see cref="CommonUsages.Back"/> which identifies a
        /// control generally used to move backwards in the navigation history of a UI. On a keyboard,
        /// it is the escape key that generally fulfills this role whereas on a gamepad, it is generally
        /// the "B" / "Circle" button. Some devices may not have a control that generally fulfills this
        /// function and thus may not have any control with the "Back" usage.
        ///
        /// By looking up controls by usage rather than by name, it is possible to locate the correct
        /// control to use for certain standardized situation without having to know the particulars of
        /// the device or platform.
        ///
        /// Note that usages on devices work slightly differently than usages of controls on devices.
        /// They are also queried through this property but unlike the usages of controls, the set of
        /// usages of a device can be changed dynamically as the role of the device changes. For details,
        /// see <see cref="InputSystem.SetDeviceUsage(InputDevice,string)"/>. Controls, on the other hand,
        /// can currently only be assigned usages through layouts (<see cref="InputControlAttribute.usage"/>
        /// or <see cref="InputControlAttribute.usages"/>).
        ///
        /// <see cref="InputSystem.AddDeviceUsage(InputDevice,string)"/> can be used to add a device.
        /// <see cref="InputSystem.RemoveDeviceUsage(InputDevice,string)"/> can be used to remove a device.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Bind to any control which is tagged with the "Back" usage on any device.
        /// var backAction = new InputAction(binding: "*/{Back}");
        /// </code>
        /// </example>
        public ReadOnlyArray<InternedString> usages =>
            new ReadOnlyArray<InternedString>(m_Device.m_UsagesForEachControl, m_UsageStartIndex, m_UsageCount);

        /// <summary>
        /// List of alternate names for the control.
        /// </summary>
        /// <value>
        /// List of aliased alternate names for the control.
        /// An example of an alias would be '<c>North</c>' for the '<c>Triangle</c>' button on a Playstation pad (or '<c>Y</c>' button on Xbox pad).
        /// </value>
        public ReadOnlyArray<InternedString> aliases =>
            new ReadOnlyArray<InternedString>(m_Device.m_AliasesForEachControl, m_AliasStartIndex, m_AliasCount);

        /// <summary>
        /// Information about where the control stores its state, such as format, offset and size.
        /// </summary>
        public InputStateBlock stateBlock => m_StateBlock;

        /// <summary>
        /// Retrieves whether the control is considered [noisy](xref:input-system-controls#noisy-controls).
        /// </summary>
        /// <value>True if the control produces noisy input.</value>
        /// <remarks>
        /// A control is considered "noisy" if it produces different values without necessarily requiring user
        /// interaction. For example, <see cref="UnityEngine.InputSystem.XR.XRHMD">XR head mounted displays</see>
        /// or <see cref="Sensor">sensors</see> such as a <see cref="Gyroscope"/>. For more information, refer to
        /// [Noisy controls](xref:input-system-controls#noisy-controls).
        ///
        /// The value of this property is determined by the <see cref="InputControlLayout">layout</see> that the
        /// control has been built from (using <see cref="InputControlLayout.ControlItem.isNoisy"/>).
        ///
        /// > [!NOTE]
        /// > For <see cref="InputDevice">devices</see>, this property is true if any control on the device
        /// is marked as noisy.
        ///
        /// The primary effect of being noisy is on <see cref="InputDevice.MakeCurrent"/> and
        /// on interactive rebinding (see <see cref="InputActionRebindingExtensions.RebindingOperation"/>).
        /// However, being noisy also affects automatic resetting of controls that happens when the application
        /// loses focus. For more information, refer to [Noisy controls](xref:input-system-controls#noisy-controls).
        /// </remarks>
        /// <seealso cref="InputControlAttribute.noisy"/>
        public bool noisy
        {
            get => (m_ControlFlags & ControlFlags.IsNoisy) != 0;
            internal set
            {
                if (value)
                {
                    m_ControlFlags |= ControlFlags.IsNoisy;
                    // Making a control noisy makes all its children noisy.
                    var list = children;
                    for (var i = 0; i < list.Count; ++i)
                    {
                        if (null != list[i])
                            list[i].noisy = true;
                    }
                }
                else
                    m_ControlFlags &= ~ControlFlags.IsNoisy;
            }
        }

        /// <summary>
        /// Retrieves whether the control is considered [synthetic](xref:input-system-controls#synthetic-controls).
        /// </summary>
        /// <value>True if the control does not represent an actual physical control on the device.</value>
        /// <remarks>
        /// A control is considered "synthetic" if it does not correspond to an actual, physical control on the
        /// device. For example, <see cref="Keyboard.anyKey"/> or the up/down/left/right buttons added
        /// by <see cref="StickControl"/>. For more information, refer to
        /// [Synthetic controls](xref:input-system-controls#synthetic-controls).
        ///
        /// The value of this property is determined by the <see cref="InputControlLayout">layout</see> that the
        /// control has been built from (using <see cref="InputControlLayout.ControlItem.isSynthetic"/>).
        ///
        /// The primary effect of being synthetic is on interactive rebinding (see
        /// <see cref="InputActionRebindingExtensions.RebindingOperation"/>) where the input system favors
        /// non-synthetic controls over synthetic ones for rebinding. For more information, refer to
        /// [Synthetic controls](xref:input-system-controls#synthetic-controls).
        /// </remarks>
        /// <seealso cref="InputControlAttribute.synthetic"/>
        public bool synthetic
        {
            get => (m_ControlFlags & ControlFlags.IsSynthetic) != 0;
            internal set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.IsSynthetic;
                else
                    m_ControlFlags &= ~ControlFlags.IsSynthetic;
            }
        }

        /// <summary>
        /// Fetch a control from the control's hierarchy by name.
        /// </summary>
        /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
        /// <remarks>
        /// Note that <see cref="path"/> matching is case-insensitive.
        /// (see <see cref="InputControlPath"/>).
        /// An alternative method is <see cref="TryGetChildControl"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// gamepad["leftStick"] // Returns Gamepad.leftStick
        /// gamepad["leftStick/x"] // Returns Gamepad.leftStick.x
        /// gamepad["{PrimaryAction}"] // Returns the control with PrimaryAction usage, that is, Gamepad.aButton
        /// </code>
        /// </example>
        /// <exception cref="KeyNotFoundException"><paramref name="path"/> cannot be found.</exception>
        public InputControl this[string path]
        {
            get
            {
                var control = InputControlPath.TryFindChild(this, path);
                if (control == null)
                    throw new KeyNotFoundException(
                        $"Cannot find control '{path}' as child of '{this}'");
                return control;
            }
        }

        /// <summary>
        /// Returns the underlying value type of this control.
        /// </summary>
        /// <value>Type of values produced by the control.</value>
        /// <remarks>
        /// This is the type of values that are returned when reading the current value of a control
        /// or when reading a value of a control from an event with <see cref="ReadValueFromStateAsObject"/>.
        /// The size can be determined with <see cref="valueSizeInBytes"/>.
        /// </remarks>
        public abstract Type valueType { get; }

        /// <summary>
        /// Size in bytes of values that the control returns.
        /// </summary>
        /// <remarks>
        /// The type can be determined with <see cref="valueType"/>.
        /// </remarks>
        public abstract int valueSizeInBytes { get; }

        /// <summary>
        /// Compute an absolute, normalized magnitude value that indicates the extent to which the control
        /// is actuated. Shortcut for <see cref="EvaluateMagnitude()"/>.
        /// </summary>
        /// <value>
        /// Amount of actuation of the control or -1 if it cannot be determined.
        /// </value>
        public float magnitude => EvaluateMagnitude();

        /// <summary>
        /// Return a string representation of the control.
        /// </summary>
        /// <remarks>
        /// Return a string representation of the control. Useful for debugging.
        /// </remarks>
        /// <returns>A string representation of the control.</returns>
        public override string ToString()
        {
            return $"{layout}:{path}";
        }

        private string DebuggerDisplay()
        {
            // If the device hasn't been added, don't try to read the control's value.
            if (!device.added)
                return ToString();

            // ReadValueAsObject might throw. Revert to just ToString() in that case.
            try
            {
                return $"{layout}:{path}={this.ReadValueAsObject()}";
            }
            catch (Exception)
            {
                return ToString();
            }
        }

        ////REVIEW: The -1 behavior seems bad; probably better to just return 1 for controls that do not support finer levels of actuation
        /// <summary>
        /// Compute an absolute, normalized magnitude value that indicates the extent to which the control
        /// is actuated.
        /// </summary>
        /// <returns>Amount of actuation of the control or -1 if it cannot be determined.</returns>
        /// <remarks>
        /// Magnitudes do not make sense for all types of controls. For example, for a control that represents
        /// an enumeration of values (such as <see cref="TouchPhaseControl"/>), there is no meaningful
        /// linear ordering of values (one could derive a linear ordering through the actual enum values but
        /// their assignment may be entirely arbitrary; it is unclear whether a state of <see cref="TouchPhase.Canceled"/>
        /// has a higher or lower "magnitude" as a state of <see cref="TouchPhase.Began"/>).
        ///
        /// Controls that have no meaningful magnitude will return -1 when calling this method. Any negative
        /// return value should be considered an invalid value.
        /// </remarks>
        /// <seealso cref="EvaluateMagnitude(void*)"/>
        public unsafe float EvaluateMagnitude()
        {
            return EvaluateMagnitude(currentStatePtr);
        }

        /// <summary>
        /// Compute an absolute, normalized magnitude value that indicates the extent to which the control
        /// is actuated in the given state.
        /// </summary>
        /// <remarks>
        /// Magnitudes do not make sense for all types of controls. For example, for a control that represents
        /// an enumeration of values (such as <see cref="TouchPhaseControl"/>), there is no meaningful
        /// linear ordering of values (one could derive a linear ordering through the actual enum values but
        /// their assignment may be entirely arbitrary; it is unclear whether a state of <see cref="TouchPhase.Canceled"/>
        /// has a higher or lower "magnitude" as a state of <see cref="TouchPhase.Began"/>).
        ///
        /// Controls that have no meaningful magnitude will return -1 when calling this method. Any negative
        /// return value should be considered an invalid value.
        /// </remarks>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>.</param>
        /// <returns>Amount of actuation of the control or -1 if it cannot be determined.</returns>
        /// <seealso cref="EvaluateMagnitude()"/>
        public virtual unsafe float EvaluateMagnitude(void* statePtr)
        {
            return -1;
        }

        /// <summary>
        /// Read the control's final, processed value from the given buffer and return the value as an object.
        /// </summary>
        /// <param name="buffer">Buffer to read the value from.</param>
        /// <param name="bufferSize">Size of <paramref name="buffer"/> in bytes, which must be large enough to store the value.</param>
        /// <returns>The control's value as stored in <paramref name="buffer"/>.</returns>
        /// <remarks>
        /// Read the control's final, processed value from the given buffer and return the value as an object.
        ///
        /// This method allocates GC memory and should not be used during normal gameplay operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is smaller than the size of the value to be read.</exception>
        /// <seealso cref="ReadValueFromStateAsObject"/>
        public abstract unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize);

        /// <summary>
        /// Read the control's final, processed value from the given state and return the value as an object.
        /// </summary>
        /// <param name="statePtr">State to read the value for the control from.</param>
        /// <returns>The control's value as stored in <paramref name="statePtr"/>.</returns>
        /// <remarks>
        /// Read the control's final, processed value from the given state and return the value as an object.
        ///
        /// This method allocates GC memory and should not be used during normal gameplay operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="statePtr"/> is null.</exception>
        /// <seealso cref="ReadValueFromStateIntoBuffer"/>
        public abstract unsafe object ReadValueFromStateAsObject(void* statePtr);

        /// <summary>
        /// Read the control's final, processed value from the given state and store it in the given buffer.
        /// </summary>
        /// <param name="statePtr">State to read the value for the control from.</param>
        /// <param name="bufferPtr">Buffer to store the value in.</param>
        /// <param name="bufferSize">Size of <paramref name="bufferPtr"/> in bytes. Must be at least <see cref="valueSizeInBytes"/>.
        /// If it is smaller, <see cref="ArgumentException"/> will be thrown.</param>
        /// <exception cref="ArgumentNullException"><paramref name="statePtr"/> is null, or <paramref name="bufferPtr"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is smaller than <see cref="valueSizeInBytes"/>.</exception>
        /// <seealso cref="ReadValueFromStateAsObject"/>
        /// <seealso cref="WriteValueFromBufferIntoState"/>
        public abstract unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize);

        /// <summary>
        /// Read a value from the given memory and store it as state.
        /// </summary>
        /// <param name="bufferPtr">Memory containing value, to store into the state.</param>
        /// <param name="bufferSize">Size of <paramref name="bufferPtr"/> in bytes. Must be at least <see cref="valueSizeInBytes"/>.</param>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>. Will receive the state
        /// as converted from the given value.</param>
        /// <remarks>
        /// Writing values will NOT apply processors to the given value. This can mean that when reading a value
        /// from a control after it has been written to its state, the resulting value differs from what was
        /// written.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="ReadValueFromStateIntoBuffer"/>
        /// <seealso cref="WriteValueFromObjectIntoState"/>
        public virtual unsafe void WriteValueFromBufferIntoState(void* bufferPtr, int bufferSize, void* statePtr)
        {
            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <summary>
        /// Read a value object and store it as state in the given memory.
        /// </summary>
        /// <param name="value">Value for the control to store in the state.</param>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>. Will receive
        /// the state as converted from the given value.</param>
        /// <remarks>
        /// Writing values will NOT apply processors to the given value. This can mean that when reading a value
        /// from a control after it has been written to its state, the resulting value differs from what was
        /// written.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="WriteValueFromBufferIntoState"/>
        public virtual unsafe void WriteValueFromObjectIntoState(object value, void* statePtr)
        {
            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <summary>
        /// Compare the value of the control as read from <paramref name="firstStatePtr"/> to that read from
        /// <paramref name="secondStatePtr"/> and return true if they are equal.
        /// </summary>
        /// <param name="firstStatePtr">Memory containing the control's <see cref="stateBlock"/>.</param>
        /// <param name="secondStatePtr">Memory containing the control's <see cref="stateBlock"/></param>
        /// <returns>True if the value of the control is equal in both <paramref name="firstStatePtr"/> and
        /// <paramref name="secondStatePtr"/>.</returns>
        /// <remarks>
        /// Unlike <see cref="CompareValue"/>, this method will have to do more than just compare the memory
        /// for the control in the two state buffers. It will have to read out state for the control and run
        /// the full processing machinery for the control to turn the state into a final, processed value.
        /// CompareValue is thus more costly than <see cref="CompareValue"/>.
        ///
        /// This method will apply epsilons (<see cref="Mathf.Epsilon"/>) when comparing floats.
        /// </remarks>
        /// <seealso cref="CompareValue"/>
        public abstract unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr);

        /// <summary>
        /// Try to find a child control matching the given path.
        /// </summary>
        /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
        /// <returns>The first direct or indirect child control that matches the given <paramref name="path"/>
        /// or null if no control was found to match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// Note that if the given path matches multiple child controls, only the first control
        /// encountered in the search will be returned.
        ///
        /// This method is equivalent to calling <see cref="InputControlPath.TryFindChild"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Returns the leftStick control of the current gamepad.
        /// Gamepad.current.TryGetChildControl("leftStick");
        ///
        /// // Returns the X axis control of the leftStick on the current gamepad.
        /// Gamepad.current.TryGetChildControl("leftStick/x");
        ///
        /// // Returns the first control ending with "stick" in its name. Note that it
        /// // undetermined whether this is leftStick or rightStick (or even another stick
        /// // added by the given gamepad).
        /// Gamepad.current.TryGetChildControl("*stick");
        /// </code>
        /// </example>
        public InputControl TryGetChildControl(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            return InputControlPath.TryFindChild(this, path);
        }

        /// <summary>
        /// Try to find a child control matching the given path.
        /// </summary>
        /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
        /// <typeparam name="TControl">The type of control to locate.</typeparam>
        /// <returns>The first direct or indirect child control that matches the given <paramref name="path"/>
        /// or null if no control was found to match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <exception cref="InvalidOperationException">No control found with the specified type.</exception>
        /// <remarks>
        /// Note that if the given path matches multiple child controls, only the first control
        /// encountered in the search will be returned.
        /// </remarks>
        public TControl TryGetChildControl<TControl>(string path)
            where TControl : InputControl
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var control = TryGetChildControl(path);
            if (control == null)
                return null;

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new InvalidOperationException(
                    $"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

            return controlOfType;
        }

        /// <summary>
        /// Find a child control matching the given path.
        /// </summary>
        /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
        /// <returns>The first direct or indirect child control that matches the given <paramref name="path"/>
        /// or null if no control was found to match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <exception cref="InvalidOperationException">No control found with the specified type.</exception>
        /// <exception cref="ArgumentException">The control cannot be found.</exception>
        /// <remarks>
        /// Note that if the given path matches multiple child controls, only the first control
        /// encountered in the search will be returned.
        /// </remarks>
        public InputControl GetChildControl(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var control = TryGetChildControl(path);
            if (control == null)
                throw new ArgumentException($"Cannot find input control '{MakeChildPath(path)}'", nameof(path));

            return control;
        }

        /// <summary>
        /// Find a child control matching the given path.
        /// </summary>
        /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
        /// <typeparam name="TControl">The type of control to locate.</typeparam>
        /// <returns>The first direct or indirect child control that matches the given <paramref name="path"/>
        /// or null if no control was found to match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <exception cref="InvalidOperationException">No control found with the specified type.</exception>
        /// <exception cref="ArgumentException">The control cannot be found.</exception>
        /// <remarks>
        /// Note that if the given path matches multiple child controls, only the first control
        /// encountered in the search will be returned.
        /// </remarks>
        public TControl GetChildControl<TControl>(string path)
            where TControl : InputControl
        {
            var control = GetChildControl(path);

            if (!(control is TControl controlOfType))
                throw new ArgumentException(
                    $"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!", nameof(path));

            return controlOfType;
        }

        /// <summary>
        /// Constructor for the InputControl
        /// </summary>
        /// <remarks>
        /// Constructor for the InputControl
        /// </remarks>
        protected InputControl()
        {
            // Set defaults for state block setup. Subclasses may override.
            m_StateBlock.byteOffset = InputStateBlock.AutomaticOffset; // Request automatic layout by default.
        }

        /// <summary>
        /// Perform final initialization tasks after the control hierarchy has been put into place.
        /// </summary>
        /// <remarks>
        /// This method can be overridden to perform control- or device-specific setup work. The most
        /// common use case is for looking up child controls and storing them in local getters.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyDevice : InputDevice
        /// {
        ///     public ButtonControl button { get; private set; }
        ///     public AxisControl axis { get; private set; }
        ///
        ///     protected override void OnFinishSetup()
        ///     {
        ///         // Cache controls in getters.
        ///         button = GetChildControl("button");
        ///         axis = GetChildControl("axis");
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void FinishSetup()
        {
        }

        /// <summary>
        /// Call <see cref="RefreshConfiguration"/> if the configuration has in the interim been invalidated
        /// by a <see cref="DeviceConfigurationEvent"/>.
        /// </summary>
        /// <remarks>
        /// This method is only relevant if you are implementing your own devices or new
        /// types of controls which are fetching configuration data from the devices (such
        /// as <see cref="KeyControl"/> which is fetching display names for individual keys
        /// from the underlying platform).
        ///
        /// This method should be called if you are accessing cached data set up by
        /// <see cref="RefreshConfiguration"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.Utilities;
        ///
        /// // Let's say your device has an associated orientation which it can be held with
        /// // and you want to surface both as a property and as a usage on the device.
        /// // Whenever your backend code detects a change in orientation, it should send
        /// // a DeviceConfigurationEvent to your device to signal that the configuration
        /// // of the device has changed. You can then implement RefreshConfiguration() to
        /// // read out and update the device orientation on the managed InputDevice instance.
        /// public class MyDevice : InputDevice
        /// {
        ///     public enum Orientation
        ///     {
        ///         Horizontal,
        ///         Vertical,
        ///     }
        ///
        ///     private Orientation m_Orientation;
        ///     public Orientation orientation
        ///     {
        ///         get
        ///         {
        ///             // Call RefreshOrientation if the configuration of the device has been
        ///             // invalidated since last time we initialized m_Orientation.
        ///             RefreshConfigurationIfNeeded();
        ///             return m_Orientation;
        ///         }
        ///     }
        ///     protected override void RefreshConfiguration()
        ///     {
        ///         // Fetch the current orientation from the backend. How you do this
        ///         // depends on your device. Using DeviceCommands is one way.
        ///         var fetchOrientationCommand = new FetchOrientationCommand();
        ///         ExecuteCommand(ref fetchOrientationCommand);
        ///         m_Orientation = fetchOrientation;
        ///
        ///         // Reflect the orientation on the device.
        ///         switch (m_Orientation)
        ///         {
        ///             case Orientation.Vertical:
        ///                 InputSystem.RemoveDeviceUsage(this, s_Horizontal);
        ///                 InputSystem.AddDeviceUsage(this, s_Vertical);
        ///                 break;
        ///
        ///             case Orientation.Horizontal:
        ///                 InputSystem.RemoveDeviceUsage(this, s_Vertical);
        ///                 InputSystem.AddDeviceUsage(this, s_Horizontal);
        ///                 break;
        ///         }
        ///     }
        ///
        ///     private static InternedString s_Vertical = new InternedString("Vertical");
        ///     private static InternedString s_Horizontal = new InternedString("Horizontal");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="RefreshConfiguration"/>
        protected void RefreshConfigurationIfNeeded()
        {
            if (!isConfigUpToDate)
            {
                RefreshConfiguration();
                isConfigUpToDate = true;
            }
        }

        /// <summary>
        /// Refresh the configuration of the control. This is used to update the control's state (e.g. Keyboard Layout or display Name of Keys).
        /// </summary>
        /// <remarks>
        /// The system will call this method automatically whenever a change is made to one of the control's configuration properties.
        /// This method is only relevant if you are implementing your own devices or new
        /// types of controls which are fetching configuration data from the devices (such
        /// as <see cref="KeyControl"/> which is fetching display names for individual keys
        /// from the underlying platform).
        /// See <see cref="RefreshConfigurationIfNeeded"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.Utilities;
        ///
        /// public class MyDevice : InputDevice
        /// {
        ///     public enum Orientation
        ///     {
        ///         Horizontal,
        ///         Vertical,
        ///     }
        ///     private Orientation m_Orientation;
        ///     private static InternedString s_Vertical = new InternedString("Vertical");
        ///     private static InternedString s_Horizontal = new InternedString("Horizontal");
        ///
        ///     public Orientation orientation
        ///     {
        ///         get
        ///         {
        ///             // Call RefreshOrientation if the configuration of the device has been
        ///             // invalidated since last time we initialized m_Orientation.
        ///             // Calling RefreshConfigurationIfNeeded() is sufficient in most cases, RefreshConfiguration() forces the refresh.
        ///             RefreshConfiguration();
        ///             return m_Orientation;
        ///         }
        ///     }
        ///     protected override void RefreshConfiguration()
        ///     {
        ///         // Set Orientation back to horizontal. Alternatively fetch from device.
        ///         m_Orientation = Orientation.Horizontal;
        ///         // Reflect the orientation on the device.
        ///         switch (m_Orientation)
        ///         {
        ///             case Orientation.Vertical:
        ///                 InputSystem.RemoveDeviceUsage(this, s_Horizontal);
        ///                 InputSystem.AddDeviceUsage(this, s_Vertical);
        ///                 break;
        ///
        ///             case Orientation.Horizontal:
        ///                 InputSystem.RemoveDeviceUsage(this, s_Vertical);
        ///                 InputSystem.AddDeviceUsage(this, s_Horizontal);
        ///                 break;
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void RefreshConfiguration()
        {
        }

        ////TODO: drop protected access
        /// <summary>
        /// Information about a memory region storing input state.
        /// </summary>
        protected internal InputStateBlock m_StateBlock;

        ////REVIEW: shouldn't these sit on the device?
        /// <summary>
        /// The state data buffer for the device.
        /// </summary>
        /// <value>
        /// The state data buffer for the device.
        /// </value>
        protected internal unsafe void* currentStatePtr => InputStateBuffers.GetFrontBufferForDevice(GetDeviceIndex());

        /// <summary>
        /// The state data buffer for the device from the previous frame.
        /// </summary>
        /// <value>
        /// The state data buffer for the device from the previous frame.
        /// </value>
        protected internal unsafe void* previousFrameStatePtr => InputStateBuffers.GetBackBufferForDevice(GetDeviceIndex());

        /// <summary>
        /// The default state data buffer
        /// </summary>
        /// <value>
        /// Buffer that has state for each device initialized with default values.
        /// </value>
        protected internal unsafe void* defaultStatePtr => InputStateBuffers.s_DefaultStateBuffer;

        /// <summary>
        /// Return the memory that holds the noise mask for the control.
        /// </summary>
        /// <value>Noise bit mask for the control.</value>
        /// <remarks>
        /// Like with all state blocks, the specific memory block for the control is found at the memory
        /// region specified by <see cref="stateBlock"/>.
        ///
        /// The noise mask can be overlaid as a bit mask over the state for the control. When doing so, all state
        /// that is noise will be masked out whereas all state that isn't will come through unmodified. In other words,
        /// any bit that is set in the noise mask indicates that the corresponding bit in the control's state memory
        /// is noise.
        ///
        /// A control can be marked as <see cref="noisy"/>.
        /// </remarks>
        protected internal unsafe void* noiseMaskPtr => InputStateBuffers.s_NoiseMaskBuffer;

        /// <summary>
        /// The offset of this control's state relative to its device root.
        /// </summary>
        /// <remarks>
        /// Once a device has been added to the system, its state block will get allocated
        /// in the global state buffers and the offset of the device's state block will
        /// get baked into all the controls on the device. This property always returns
        /// the "unbaked" offset.
        /// </remarks>
        protected internal uint stateOffsetRelativeToDeviceRoot
        {
            get
            {
                var deviceStateOffset = device.m_StateBlock.byteOffset;
                Debug.Assert(deviceStateOffset <= m_StateBlock.byteOffset);
                return m_StateBlock.byteOffset - deviceStateOffset;
            }
        }

        // This data is initialized by InputDeviceBuilder.
        internal InternedString m_Name;
        internal string m_Path;
        internal string m_DisplayName; // Display name set by the control itself (may be null).
        internal string m_DisplayNameFromLayout; // Display name coming from layout (may be null).
        internal string m_ShortDisplayName; // Short display name set by the control itself (may be null).
        internal string m_ShortDisplayNameFromLayout; // Short display name coming from layout (may be null).
        internal InternedString m_Layout;
        internal InternedString m_Variants;
        internal InputDevice m_Device;
        internal InputControl m_Parent;
        internal int m_UsageCount;
        internal int m_UsageStartIndex;
        internal int m_AliasCount;
        internal int m_AliasStartIndex;
        internal int m_ChildCount;
        internal int m_ChildStartIndex;
        internal ControlFlags m_ControlFlags;

        // Value caching
        // These values will be set to true during state updates if the control has actually changed value.
        // Set to true initially so default state will be returned on the first call
        internal bool m_CachedValueIsStale = true;
        internal bool m_UnprocessedCachedValueIsStale = true;

        ////REVIEW: store these in arrays in InputDevice instead?
        internal PrimitiveValue m_DefaultState;
        internal PrimitiveValue m_MinValue;
        internal PrimitiveValue m_MaxValue;

        internal FourCC m_OptimizedControlDataType;

        /// <summary>
        /// The type of the state memory associated with the control.
        /// </summary>
        /// <remarks>
        /// For some types of control you can safely read/write state memory directly
        /// which is much faster than calling ReadUnprocessedValueFromState/WriteValueIntoState.
        /// This method returns a type that you can use for reading/writing the control directly,
        /// or it returns <see cref="InputStateBlock.FormatInvalid"/> if it's not possible for this type of control.
        ///
        /// For example, AxisControl <see cref="valueType"/> might be a "float" in state memory, and if no processing is applied during reading (e.g. no invert/scale/etc),
        /// then you could read it as float in memory directly without calling ReadUnprocessedValueFromState, which is faster.
        /// Additionally, if you have a Vector3Control which uses 3 AxisControls as consecutive floats in memory,
        /// you can cast the Vector3Control state memory directly to Vector3 without calling ReadUnprocessedValueFromState on x/y/z axes.
        ///
        /// The value returned for any given control is computed automatically by the Input System, when the control's setup configuration changes. <see cref="InputControl.CalculateOptimizedControlDataType"/>
        /// There are some parameter changes which don't trigger a configuration change (such as the clamp, invert, normalize, and scale parameters on AxisControl),
        /// so if you modify these, the optimized data type is not automatically updated. In this situation, you should manually update it by calling <see cref="InputControl.ApplyParameterChanges"/>.
        /// </remarks>
        public FourCC optimizedControlDataType => m_OptimizedControlDataType;

        /// <summary>
        /// Calculates and returns an optimized data type that can represent a control's value in memory directly.
        /// </summary>
        /// <remarks>
        /// The value then is cached in <see cref="InputControl.optimizedControlDataType"/>.
        /// This method is for internal use only, you should not call this from your own code.
        /// </remarks>
        /// <returns>
        /// An optimized data type that can represent a control's value in memory directly. <see cref="InputStateBlock"/>
        /// </returns>
        protected virtual FourCC CalculateOptimizedControlDataType()
        {
            return InputStateBlock.kFormatInvalid;
        }

        /// <summary>
        /// Apply built-in parameters changes.
        /// </summary>
        /// <remarks>
        /// Apply built-in parameters changes (e.g. <see cref="AxisControl.invert"/>, others).
        /// Recompute <see cref="InputControl.optimizedControlDataType"/> for impacted controls and clear cached value
        /// </remarks>
        /// <example>
        /// <code>
        /// Gamepad.all[0].leftTrigger.WriteValueIntoState(0.5f, Gamepad.all[0].currentStatePtr);
        /// Gamepad.all[0].ApplyParameterChanges();
        /// </code>
        /// </example>
        public void ApplyParameterChanges()
        {
            // First we go through all children of our own hierarchy
            SetOptimizedControlDataTypeRecursively();

            // Then we go through all parents up to the root, because our own change might influence their optimization status
            // e.g. let's say we have a tree where root is Vector3 and children are three AxisControl
            // And user is calling this method on AxisControl which goes from Float to NotOptimized.
            // Then we need to also transition Vector3 to NotOptimized as well.

            var currentParent = parent;
            while (currentParent != null)
            {
                currentParent.SetOptimizedControlDataType();
                currentParent = currentParent.parent;
            }

            // Also use this method to mark cached values as stale
            MarkAsStaleRecursively();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOptimizedControlDataType()
        {
            // setting check need to be inline so we clear optimizations if setting is disabled after the fact
            m_OptimizedControlDataType = InputSystem.s_Manager.optimizedControlsFeatureEnabled
                ? CalculateOptimizedControlDataType()
                : (FourCC)InputStateBlock.kFormatInvalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetOptimizedControlDataTypeRecursively()
        {
            // Need to go depth-first because CalculateOptimizedControlDataType might depend on computed values of children
            if (m_ChildCount > 0)
            {
                foreach (var inputControl in children)
                    inputControl.SetOptimizedControlDataTypeRecursively();
            }

            SetOptimizedControlDataType();
        }

        // This function exists to warn users to start using ApplyParameterChanges for edge cases that were previously not intentionally supported,
        // where control properties suddenly change underneath us without us anticipating that.
        // This is mainly to AxisControl fields being public and capable of changing at any time even if we were not anticipated such a usage pattern.
        // Also it's not clear if InputControl.stateBlock.format can potentially change at any time, likely not.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Only do this check in and editor in hope that it will be sufficient to catch any misuse during development.
        // It is not done in debug builds because it has a performance cost and it will show up when profiled.
        [Conditional("UNITY_EDITOR")]
        internal void EnsureOptimizationTypeHasNotChanged()
        {
            if (!InputSystem.s_Manager.optimizedControlsFeatureEnabled)
                return;

            var currentOptimizedControlDataType = CalculateOptimizedControlDataType();
            if (currentOptimizedControlDataType != optimizedControlDataType)
            {
                Debug.LogError(
                    $"Control '{name}' / '{path}' suddenly changed optimization state due to either format " +
                    $"change or control parameters change (was '{optimizedControlDataType}' but became '{currentOptimizedControlDataType}'), " +
                    "this hinders control hot path optimization, please call control.ApplyParameterChanges() " +
                    "after the changes to the control to fix this error.");

                // Automatically fix the issue
                // Note this function is only executed in the editor
                m_OptimizedControlDataType = currentOptimizedControlDataType;
            }

            if (m_ChildCount > 0)
            {
                foreach (var inputControl in children)
                    inputControl.EnsureOptimizationTypeHasNotChanged();
            }
        }

        [Flags]
        internal enum ControlFlags
        {
            ConfigUpToDate = 1 << 0,
            IsNoisy = 1 << 1,
            IsSynthetic = 1 << 2,
            IsButton = 1 << 3,
            DontReset = 1 << 4,
            SetupFinished = 1 << 5, // Can't be modified once this is set.
            UsesStateFromOtherControl = 1 << 6,
        }

        internal bool isSetupFinished
        {
            get => (m_ControlFlags & ControlFlags.SetupFinished) == ControlFlags.SetupFinished;
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.SetupFinished;
                else
                    m_ControlFlags &= ~ControlFlags.SetupFinished;
            }
        }

        internal bool isButton
        {
            get => (m_ControlFlags & ControlFlags.IsButton) == ControlFlags.IsButton;
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.IsButton;
                else
                    m_ControlFlags &= ~ControlFlags.IsButton;
            }
        }

        internal bool isConfigUpToDate
        {
            get => (m_ControlFlags & ControlFlags.ConfigUpToDate) == ControlFlags.ConfigUpToDate;
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.ConfigUpToDate;
                else
                    m_ControlFlags &= ~ControlFlags.ConfigUpToDate;
            }
        }

        internal bool dontReset
        {
            get => (m_ControlFlags & ControlFlags.DontReset) == ControlFlags.DontReset;
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.DontReset;
                else
                    m_ControlFlags &= ~ControlFlags.DontReset;
            }
        }

        internal bool usesStateFromOtherControl
        {
            get => (m_ControlFlags & ControlFlags.UsesStateFromOtherControl) == ControlFlags.UsesStateFromOtherControl;
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.UsesStateFromOtherControl;
                else
                    m_ControlFlags &= ~ControlFlags.UsesStateFromOtherControl;
            }
        }

        internal bool hasDefaultState => !m_DefaultState.isEmpty;

        // This method exists only to not slap the internal interaction on all overrides of
        // FinishSetup().
        internal void CallFinishSetupRecursive()
        {
            var list = children;
            for (var i = 0; i < list.Count; ++i)
                list[i].CallFinishSetupRecursive();
            FinishSetup();
            SetOptimizedControlDataTypeRecursively();
        }

        internal string MakeChildPath(string path)
        {
            if (this is InputDevice)
                return path;
            return $"{this.path}/{path}";
        }

        internal void BakeOffsetIntoStateBlockRecursive(uint offset)
        {
            m_StateBlock.byteOffset += offset;

            var list = children;
            for (var i = 0; i < list.Count; ++i)
                list[i].BakeOffsetIntoStateBlockRecursive(offset);
        }

        internal int GetDeviceIndex()
        {
            var deviceIndex = m_Device.m_DeviceIndex;
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot query value of control '{path}' before '{device.name}' has been added to system!");
            return deviceIndex;
        }

        internal bool IsValueConsideredPressed(float value)
        {
            if (isButton)
                return ((ButtonControl)this).IsValueConsideredPressed(value);
            return value >= ButtonControl.s_GlobalDefaultButtonPressPoint;
        }

        internal virtual void AddProcessor(object first)
        {
        }

        internal void MarkAsStale()
        {
            m_CachedValueIsStale = true;
            m_UnprocessedCachedValueIsStale = true;
        }

        internal void MarkAsStaleRecursively()
        {
            MarkAsStale();

            foreach (var inputControl in children)
            {
                inputControl.MarkAsStale();
                if (inputControl is ButtonControl buttonControl)
                {
                    // If everything is becoming stale, update all press states so we can reevaluate
                    buttonControl.UpdateWasPressed();
                    #if UNITY_EDITOR
                    buttonControl.UpdateWasPressedEditor();
                    #endif
                }
            }
        }

        #if UNITY_EDITOR
        internal virtual IEnumerable<object> GetProcessors()
        {
            yield return null;
        }

        #endif
    }

    /// <summary>
    /// Base class for input controls with a specific value type.
    /// </summary>
    /// <typeparam name="TValue">Type of value captured by the control. Note that this does not mean
    /// that the control has to store data in the given value format. A control that captures float
    /// values, for example, may be stored in state as byte values instead.</typeparam>
    public abstract class InputControl<TValue> : InputControl
        where TValue : struct
    {
        /// <inheritdoc/>
        public override Type valueType => typeof(TValue);

        /// <inheritdoc/>
        public override int valueSizeInBytes => UnsafeUtility.SizeOf<TValue>();

        /// <summary>
        /// Returns the current value of the control after processors have been applied.
        /// </summary>
        /// <value>The controls current value.</value>
        /// <remarks>
        /// This can only be called on devices that have been added to the system (<see cref="InputDevice.added"/>).
        ///
        /// If internal feature "USE_READ_VALUE_CACHING" is enabled, then this property implements caching
        /// to avoid applying processors when the underlying control has not changed.
        /// With this in mind, be aware of processors that use global state, such as the <see cref="Processors.AxisDeadzoneProcessor"/>.
        /// Unless the control unprocessed value has been changed, input system settings changed or <see cref="InputControl.ApplyParameterChanges()"/> invoked,
        /// the processors will not run and calls to <see cref="value"/> will return the same result as previous calls.
        ///
        /// If a processor requires to be run on every read, override <see cref="InputProcessor.cachingPolicy"/> property
        /// in the processor and set it to <see cref="InputProcessor.CachingPolicy.EvaluateOnEveryRead"/>.
        ///
        /// To improve debugging try setting "PARANOID_READ_VALUE_CACHING_CHECKS" internal feature flag to check if cache value is still consistent.
        ///
        /// Also note that this property returns the result as ref readonly. If custom control states are in use, i.e.
        /// any controls not shipped with the Input System package, be careful of accidental defensive copies
        /// <a href="https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#avoid-defensive-copies">https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#avoid-defensive-copies</a>.
        /// </remarks>
        /// <seealso cref="ReadValue"/>
        public ref readonly TValue value
        {
            get
            {
#if UNITY_EDITOR
                if (InputUpdate.s_LatestUpdateType.IsEditorUpdate())
                    return ref ReadStateInEditor();
#endif

                if (
                    // if feature is disabled we re-evaluate every call
                    !InputSystem.s_Manager.readValueCachingFeatureEnabled
                    // if cached value is stale we re-evaluate and clear the flag
                    || m_CachedValueIsStale
                    // if a processor in stack needs to be re-evaluated, but unprocessedValue is still can be cached
                    || evaluateProcessorsEveryRead
                )
                {
                    m_CachedValue = ProcessValue(unprocessedValue);
                    m_CachedValueIsStale = false;
                }
#if DEBUG
                else if (InputSystem.s_Manager.paranoidReadValueCachingChecksEnabled)
                {
                    var oldUnprocessedValue = m_UnprocessedCachedValue;
                    var newUnprocessedValue = unprocessedValue;
                    var currentProcessedValue = ProcessValue(newUnprocessedValue);

                    if (CompareValue(ref newUnprocessedValue, ref oldUnprocessedValue))
                    {
                        // don't warn if unprocessedValue caching failed
                        m_CachedValue = currentProcessedValue;
                    }
                    else if (CompareValue(ref currentProcessedValue, ref m_CachedValue))
                    {
                        // processors are not behaving as expected if unprocessedValue stays the same but processedValue changed
                        var namesList = new List<string>();
                        foreach (var inputProcessor in m_ProcessorStack)
                            namesList.Add(inputProcessor.ToString());
                        var names = string.Join(", ", namesList);
                        Debug.LogError(
                            "Cached processed value unexpectedly became outdated due to InputProcessor's returning a different value, " +
                            $"new value '{currentProcessedValue}' old value '{m_CachedValue}', current processors are: {names}. " +
                            "If your processor need to be recomputed on every read please add \"public override CachingPolicy cachingPolicy => CachingPolicy.EvaluateOnEveryRead;\" to the processor.");
                        m_CachedValue = currentProcessedValue;
                    }
                }
#endif

                return ref m_CachedValue;
            }
        }

        internal unsafe ref readonly TValue unprocessedValue
        {
            get
            {
#if UNITY_EDITOR
                if (InputUpdate.s_LatestUpdateType.IsEditorUpdate())
                    return ref ReadUnprocessedStateInEditor();
#endif
                // Case ISXB-606
                // If an object reference has the underlying object deleted then a device can go
                // away which means that the underlying state buffers will have been resized.
                //
                // The currentStatePtr accessor uses GetDeviceIndex() to index into the state
                // buffers but this index can then be out of bounds.
                //
                // InputStateBuffers.Get{Front,Back}Buffer() now check for the requested index being
                // in-bounds and return null if not - check that here to avoid null derefence later.
                //
                if (currentStatePtr == null)
                {
                    return ref m_UnprocessedCachedValue;
                }

                if (
                    // if feature is disabled we re-evaluate every call
                    !InputSystem.s_Manager.readValueCachingFeatureEnabled
                    // if cached value is stale we re-evaluate and clear the flag
                    || m_UnprocessedCachedValueIsStale
                )
                {
                    m_UnprocessedCachedValue = ReadUnprocessedValueFromState(currentStatePtr);
                    m_UnprocessedCachedValueIsStale = false;
                }
#if DEBUG
                else if (InputSystem.s_Manager.paranoidReadValueCachingChecksEnabled)
                {
                    var currentUnprocessedValue = ReadUnprocessedValueFromState(currentStatePtr);
                    if (CompareValue(ref currentUnprocessedValue, ref m_UnprocessedCachedValue))
                    {
                        Debug.LogError($"Cached unprocessed value unexpectedly became outdated for unknown reason, new value '{currentUnprocessedValue}' old value '{m_UnprocessedCachedValue}'.");
                        m_UnprocessedCachedValue = currentUnprocessedValue;
                    }
                }
#endif

                return ref m_UnprocessedCachedValue;
            }
        }

        /// <summary>
        /// Returns the current value of the control after processors have been applied.
        /// </summary>
        /// <returns>The controls current value.</returns>
        /// <remarks>
        /// This can only be called on devices that have been added to the system (<see cref="InputDevice.added"/>).
        ///
        /// If internal feature "USE_READ_VALUE_CACHING" is enabled, then this property implements caching
        /// to avoid applying processors when the underlying control has not changed.
        /// With this in mind, be aware of processors that use global state, such as the <see cref="Processors.AxisDeadzoneProcessor"/>.
        /// Unless the control unprocessed value has been changed, input system settings changed or <see cref="InputControl.ApplyParameterChanges()"/> invoked,
        /// the processors will not run and calls to <see cref="value"/> will return the same result as previous calls.
        ///
        /// If a processor requires to be run on every read, override <see cref="InputProcessor.cachingPolicy"/> property
        /// in the processor and set it to <see cref="InputProcessor.CachingPolicy.EvaluateOnEveryRead"/>.
        ///
        /// To improve debugging try setting "PARANOID_READ_VALUE_CACHING_CHECKS" internal feature flag to check if cache value is still consistent.
        /// <a href="https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#avoid-defensive-copies">https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#avoid-defensive-copies</a>.
        /// </remarks>
        /// <seealso cref="value"/>
        public TValue ReadValue()
        {
            return value;
        }

        ////REVIEW: is 'frame' really the best wording here?
        /// <summary>
        /// Get the control's value from the previous frame (<see cref="InputControl.previousFrameStatePtr"/>).
        /// </summary>
        /// <returns>The control's value in the previous frame.</returns>
        public TValue ReadValueFromPreviousFrame()
        {
            unsafe
            {
                return ReadValueFromState(previousFrameStatePtr);
            }
        }

        /// <summary>
        /// Get the control's default value.
        /// </summary>
        /// <returns>The control's default value.</returns>
        /// <remarks>
        /// This is not necessarily equivalent to <c>default(TValue)</c>. A control's default value is determined
        /// by reading its value from the default state (<see cref="InputControl.defaultStatePtr"/>) which in turn
        /// is determined from settings in the control's registered layout (<see cref="InputControlLayout.ControlItem.defaultState"/>).
        /// </remarks>
        public TValue ReadDefaultValue()
        {
            unsafe
            {
                return ReadValueFromState(defaultStatePtr);
            }
        }

        /// <summary>
        /// Get the control's default value.
        /// </summary>
        /// <param name="statePtr">State containing the control's <see cref="InputControl.stateBlock"/>.</param>
        /// <returns>The control's default value.</returns>
        /// <remarks>
        /// This is not necessarily equivalent to <c>default(TValue)</c>. A control's default value is determined
        /// by reading its value from the default state (<see cref="InputControl.defaultStatePtr"/>) which in turn
        /// is determined from settings in the control's registered layout (<see cref="InputControlLayout.ControlItem.defaultState"/>).
        /// </remarks>
        public unsafe TValue ReadValueFromState(void* statePtr)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            return ProcessValue(ReadUnprocessedValueFromState(statePtr));
        }

        /// <summary>
        /// Read value from provided <paramref name="statePtr"/> and apply processors. Try cache result if possible.
        /// </summary>
        /// <param name="statePtr">State pointer to read from.</param>
        /// <returns>The controls current value.</returns>
        /// <remarks>
        /// If <paramref name="statePtr"/> is "currentStatePtr", then read will be done via <see cref="value"/> property to improve performance.
        /// </remarks>
        /// <seealso cref="value"/>
        public unsafe TValue ReadValueFromStateWithCaching(void* statePtr)
        {
            return statePtr == currentStatePtr ? value : ReadValueFromState(statePtr);
        }

        /// <summary>
        /// Read value from provided <paramref name="statePtr"/>. Try cache result if possible.
        /// </summary>
        /// <param name="statePtr">State pointer to read from.</param>
        /// <returns>The controls current value.</returns>
        /// <remarks>
        /// If <paramref name="statePtr"/> is "currentStatePtr", then read will be done via <see cref="unprocessedValue"/> property to improve performance.
        /// </remarks>
        /// <seealso cref="value"/>
        public unsafe TValue ReadUnprocessedValueFromStateWithCaching(void* statePtr)
        {
            return statePtr == currentStatePtr ? unprocessedValue : ReadUnprocessedValueFromState(statePtr);
        }

        /// <summary>
        /// Read value from control
        /// </summary>
        /// <returns>The controls current value.</returns>
        /// <remarks>
        /// This is the locally cached value. Use <see cref="ReadUnprocessedValueFromStateWithCaching"/>to get the uncached version.
        /// </remarks>
        /// <seealso cref="value"/>
        public TValue ReadUnprocessedValue()
        {
            return unprocessedValue;
        }

        /// <summary>
        /// Read value from provided <paramref name="statePtr"/>.
        /// </summary>
        /// <param name="statePtr">State pointer to read from.</param>
        /// <returns>The controls current value.</returns>
        /// <remarks>
        /// Read value from provided <paramref name="statePtr"/> without any caching.
        /// </remarks>
        /// <seealso cref="value"/>
        public abstract unsafe TValue ReadUnprocessedValueFromState(void* statePtr);

        /// <inheritdoc />
        public override unsafe object ReadValueFromStateAsObject(void* statePtr)
        {
            return ReadValueFromState(statePtr);
        }

        /// <inheritdoc />
        public override unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            if (bufferPtr == null)
                throw new ArgumentNullException(nameof(bufferPtr));

            var numBytes = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < numBytes)
                throw new ArgumentException(
                    $"bufferSize={bufferSize} < sizeof(TValue)={numBytes}", nameof(bufferSize));

            var value = ReadValueFromState(statePtr);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            UnsafeUtility.MemCpy(bufferPtr, valuePtr, numBytes);
        }

        /// <inheritdoc />
        public override unsafe void WriteValueFromBufferIntoState(void* bufferPtr, int bufferSize, void* statePtr)
        {
            if (bufferPtr == null)
                throw new ArgumentNullException(nameof(bufferPtr));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            var numBytes = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < numBytes)
                throw new ArgumentException(
                    $"bufferSize={bufferSize} < sizeof(TValue)={numBytes}", nameof(bufferSize));

            // C# won't let us use a pointer to a generically defined type. Work
            // around this by using UnsafeUtility.
            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, bufferPtr, numBytes);

            WriteValueIntoState(value, statePtr);
        }

        /// <inheritdoc />
        public override unsafe void WriteValueFromObjectIntoState(object value, void* statePtr)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // If value is not of expected type, try to convert.
            if (!(value is TValue))
                value = Convert.ChangeType(value, typeof(TValue));

            var valueOfType = (TValue)value;
            WriteValueIntoState(valueOfType, statePtr);
        }

        /// <summary>
        /// Write a value into state at the given memory.
        /// </summary>
        /// <param name="value">Value for the control to store in the state.</param>
        /// <param name="statePtr">State containing the control's <see cref="InputControl.stateBlock"/>. Will receive
        /// the state as converted from the given value.</param>
        /// <remarks>
        /// Writing values will NOT apply processors to the given value. This can mean that when reading a value
        /// from a control after it has been written to its state, the resulting value differs from what was
        /// written.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="WriteValueFromBufferIntoState"/>
        public virtual unsafe void WriteValueIntoState(TValue value, void* statePtr)
        {
            ////REVIEW: should we be able to even tell from layouts which controls support writing and which don't?

            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <inheritdoc />
        public override unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var valueSize = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < valueSize)
                throw new ArgumentException(
                    $"Expecting buffer of at least {valueSize} bytes for value of type {typeof(TValue).Name} but got buffer of only {bufferSize} bytes instead",
                    nameof(bufferSize));

            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, buffer, valueSize);

            return value;
        }

        private static unsafe bool CompareValue(ref TValue firstValue, ref TValue secondValue)
        {
            var firstValuePtr = UnsafeUtility.AddressOf(ref firstValue);
            var secondValuePtr = UnsafeUtility.AddressOf(ref secondValue);

            // NOTE: We're comparing raw memory of processed values here (which are guaranteed to be structs or
            //       primitives), not state. Means we don't have to take bits into account here.

            return UnsafeUtility.MemCmp(firstValuePtr, secondValuePtr, UnsafeUtility.SizeOf<TValue>()) != 0;
        }

        /// <summary>
        /// Compared values in state buffers.
        /// </summary>
        /// <param name="firstStatePtr">The first state buffer to read value from.</param>
        /// <param name="secondStatePtr">The second state buffer to read value from.</param>
        /// <returns>True if the buffer values match. False if they differ.</returns>
        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            ////REVIEW: should we first compare state here? if there's no change in state, there can be no change in value and we can skip the rest

            var firstValue = ReadValueFromState(firstStatePtr);
            var secondValue = ReadValueFromState(secondStatePtr);

            return CompareValue(ref firstValue, ref secondValue);
        }

        /// <summary>
        /// Applies all control processors to the passed value.
        /// </summary>
        /// <param name="value">value to run processors on.</param>
        /// <returns>The processed value.</returns>
        /// <remarks>
        /// Applies all control processors to the passed value.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue ProcessValue(TValue value)
        {
            ProcessValue(ref value);
            return value;
        }

        /// <summary>
        /// Applies all control processors to the passed value.
        /// </summary>
        /// <param name="value">value to run processors on.</param>
        /// <remarks>
        /// Use this overload when your state struct is large to avoid creating copies of the state.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessValue(ref TValue value)
        {
            if (m_ProcessorStack.length <= 0)
                return;

            value = m_ProcessorStack.firstValue.Process(value, this);
            if (m_ProcessorStack.additionalValues == null)
                return;

            for (var i = 0; i < m_ProcessorStack.length - 1; ++i)
                value = m_ProcessorStack.additionalValues[i].Process(value, this);
        }

        internal InlinedArray<InputProcessor<TValue>> m_ProcessorStack;

        private TValue m_CachedValue;
        private TValue m_UnprocessedCachedValue;

        #if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref readonly TValue ReadStateInEditor()
        {
            // we don't use cached values during editor updates because editor updates cause controls to look at a
            // different block of state memory, and since the cached values are from the play mode memory, we'd
            // end up returning the wrong values.
            m_EditorValue = ReadValueFromState(currentStatePtr);
            return ref m_EditorValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref readonly TValue ReadUnprocessedStateInEditor()
        {
            m_UnprocessedEditorValue = ReadUnprocessedValueFromState(currentStatePtr);
            return ref m_UnprocessedEditorValue;
        }

        // these fields are just to work with the fact that the 'value' property is ref readonly, so we
        // need somewhere with a known lifetime to store these so they can be returned by ref.
        private TValue m_EditorValue;
        private TValue m_UnprocessedEditorValue;
        #endif

        // Only layouts are allowed to modify the processor stack.
        internal TProcessor TryGetProcessor<TProcessor>()
            where TProcessor : InputProcessor<TValue>
        {
            if (m_ProcessorStack.length > 0)
            {
                if (m_ProcessorStack.firstValue is TProcessor processor)
                    return processor;
                if (m_ProcessorStack.additionalValues != null)
                    for (var i = 0; i < m_ProcessorStack.length - 1; ++i)
                        if (m_ProcessorStack.additionalValues[i] is TProcessor result)
                            return result;
            }
            return default;
        }

        internal override void AddProcessor(object processor)
        {
            if (!(processor is InputProcessor<TValue> processorOfType))
                throw new ArgumentException(
                    $"Cannot add processor of type '{processor.GetType().Name}' to control of type '{GetType().Name}'", nameof(processor));
            m_ProcessorStack.Append(processorOfType);
        }

        #if UNITY_EDITOR
        internal override IEnumerable<object> GetProcessors()
        {
            foreach (var processor in m_ProcessorStack)
                yield return processor;
        }

        #endif

        internal bool evaluateProcessorsEveryRead = false;

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            foreach (var processor in m_ProcessorStack)
                if (processor.cachingPolicy == InputProcessor.CachingPolicy.EvaluateOnEveryRead)
                    evaluateProcessorsEveryRead = true;

            base.FinishSetup();
        }

        internal InputProcessor<TValue>[] processors => m_ProcessorStack.ToArray();
    }
}
