using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: allow stuff like "/gamepad/**/<button>"
////TODO: add support for | (e.g. "<Gamepad>|<Joystick>/{PrimaryMotion}"
////TODO: handle arrays
////TODO: add method to extract control path

////REVIEW: change "*/{PrimaryAction}" to "*/**/{PrimaryAction}" so that the hierarchy crawling becomes explicit?

////REVIEW: rename to `InputPath`?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Functions for working with control path specs (like "/gamepad/*stick").
    /// </summary>
    /// <remarks>
    /// Control paths are a mini-language similar to regular expressions. They are used throughout
    /// the input system as string "addresses" of input controls. At runtime, they can be matched
    /// against the devices and controls present in the system to retrieve the actual endpoints to
    /// receive input from.
    ///
    /// Like on a file system, a path is made up of components that are each separated by a
    /// forward slash (<c>/</c>). Each such component in turn is made up of a set of fields that are
    /// individually optional. However, one of the fields must be present (e.g. at least a name or
    /// a wildcard).
    ///
    /// <example>
    /// Field structure of each path component
    /// <code>
    /// &lt;Layout&gt;{Usage}#(DisplayName)Name
    /// </code>
    /// </example>
    ///
    /// * <c>Layout</c>: The name of the layout that the control must be based on (either directly or indirectly).
    /// * <c>Usage</c>: The usage that the control or device has to have, i.e. must be found in <see
    ///                 cref="InputControl.usages"/> This field can be repeated several times to require
    ///                 multiple usages (e.g. <c>"{LeftHand}{Vertical}"</c>).
    /// * <c>DisplayName</c>: The name that <see cref="InputControl.displayName"/> of the control or device
    ///                       must match.
    /// * <c>Name</c>: The name that <see cref="InputControl.name"/> or one of the entries in
    ///                <see cref="InputControl.aliases"/> must match. Alternatively, this can be a
    ///                wildcard (<c>*</c>) to match any name.
    ///
    /// Note that all matching is case-insensitive.
    ///
    /// <example>
    /// Various examples of control paths
    /// <code>
    /// // Matches all gamepads (also gamepads *based* on the Gamepad layout):
    /// "&lt;Gamepad&gt;"
    ///
    /// // Matches the "Submit" control on all devices:
    /// "*/{Submit}"
    ///
    /// // Matches the key that prints the "a" character on the current keyboard layout:
    /// "&lt;Keyboard&gt;/#(a)"
    ///
    /// // Matches the X axis of the left stick on a gamepad.
    /// "&lt;Gamepad&gt;/leftStick/x"
    ///
    /// // Matches the orientation control of the right-hand XR controller:
    /// "&lt;XRController&gt;{RightHand}/orientation"
    ///
    /// // Matches all buttons on a gamepad.
    /// "&lt;Gamepad&gt;/&lt;Button&gt;"
    /// </code>
    /// </example>
    ///
    /// The structure of the API of this class is similar in spirit to <c>System.IO.Path</c>, i.e. it offers
    /// a range of static methods that perform various operations on path strings.
    ///
    /// To query controls on devices present in the system using control paths, use
    /// <see cref="InputSystem.FindControls"/>. Also, control paths can be used with
    /// <see cref="InputControl.this[string]"/> on every control. This makes it possible
    /// to do things like:
    ///
    /// <example>
    /// Find key that prints "t" on current keyboard:
    /// <code>
    /// Keyboard.current["#(t)"]
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputControl.path"/>
    /// <seealso cref="InputSystem.FindControls"/>
    public static class InputControlPath
    {
        public const string Wildcard = "*";
        public const string DoubleWildcard = "**";

        public const char Separator = '/';

        // We consider / a reserved character in control names. So, when this character does creep
        // in there (e.g. from a device product name), we need to do something about it. We replace
        // it with this character here.
        // NOTE: Display names have no such restriction.
        // NOTE: There are some Unicode characters that look sufficiently like a slash (e.g. FULLWIDTH SOLIDUS)
        //       but that only makes for rather confusing output. So we just replace with a blank.
        internal const char SeparatorReplacement = ' ';

        internal static string CleanSlashes(this String pathComponent)
        {
            return pathComponent.Replace(Separator, SeparatorReplacement);
        }

        public static string Combine(InputControl parent, string path)
        {
            if (parent == null)
            {
                if (string.IsNullOrEmpty(path))
                    return string.Empty;

                if (path[0] != Separator)
                    return Separator + path;

                return path;
            }
            if (string.IsNullOrEmpty(path))
                return parent.path;

            return $"{parent.path}/{path}";
        }

        /// <summary>
        /// Options for customizing the behavior of <see cref="ToHumanReadableString(string,HumanReadableStringOptions,InputControl)"/>.
        /// </summary>
        [Flags]
        public enum HumanReadableStringOptions
        {
            /// <summary>
            /// The default behavior.
            /// </summary>
            None = 0,

            /// <summary>
            /// Do not mention the device of the control. For example, instead of "A [Gamepad]",
            /// return just "A".
            /// </summary>
            OmitDevice = 1 << 1,

            /// <summary>
            /// When available, use short display names instead of long ones. For example, instead of "Left Button",
            /// return "LMB".
            /// </summary>
            UseShortNames = 1 << 2,
        }

        ////TODO: factor out the part that looks up an InputControlLayout.ControlItem from a given path
        ////      and make that available as a stand-alone API
        ////TODO: add option to customize path separation character
        /// <summary>
        /// Create a human readable string from the given control path.
        /// </summary>
        /// <param name="path">A control path such as "&lt;XRController>{LeftHand}/position".</param>
        /// <param name="options">Customize the resulting string.</param>
        /// <param name="control">An optional control. If supplied and the control or one of its children
        /// matches the given <paramref name="path"/>, display names will be based on the matching control
        /// rather than on static information available from <see cref="InputControlLayout"/>s.</param>
        /// <returns>A string such as "Left Stick/X [Gamepad]".</returns>
        /// <remarks>
        /// This function is most useful for turning binding paths (see <see cref="InputBinding.path"/>)
        /// into strings that can be displayed in UIs (such as rebinding screens). It is used by
        /// the Unity editor itself to display binding paths in the UI.
        ///
        /// The method uses display names (see <see cref="InputControlAttribute.displayName"/>,
        /// <see cref="InputControlLayoutAttribute.displayName"/>, and <see cref="InputControlLayout.ControlItem.displayName"/>)
        /// where possible. For example, "&lt;XInputController&gt;/buttonSouth" will be returned as
        /// "A [Xbox Controller]" as the display name of <see cref="XInput.XInputController"/> is "XBox Controller"
        /// and the display name of its "buttonSouth" control is "A".
        ///
        /// Note that these lookups depend on the currently registered control layouts (see <see
        /// cref="InputControlLayout"/>) and different strings may thus be returned for the same control
        /// path depending on the layouts registered with the system.
        ///
        /// <example>
        /// <code>
        /// InputControlPath.ToHumanReadableString("*/{PrimaryAction"); // -> "PrimaryAction [Any]"
        /// InputControlPath.ToHumanReadableString("&lt;Gamepad&gt;/buttonSouth"); // -> "Button South [Gamepad]"
        /// InputControlPath.ToHumanReadableString("&lt;XInputController&gt;/buttonSouth"); // -> "A [Xbox Controller]"
        /// InputControlPath.ToHumanReadableString("&lt;Gamepad&gt;/leftStick/x"); // -> "Left Stick/X [Gamepad]"
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.path"/>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingDisplayString(InputAction,int,InputBinding.DisplayStringOptions)"/>
        public static string ToHumanReadableString(string path,
            HumanReadableStringOptions options = HumanReadableStringOptions.None,
            InputControl control = null)
        {
            return ToHumanReadableString(path, out _, out _, options, control);
        }

        /// <summary>
        /// Create a human readable string from the given control path.
        /// </summary>
        /// <param name="path">A control path such as "&lt;XRController>{LeftHand}/position".</param>
        /// <param name="deviceLayoutName">Receives the name of the device layout that the control path was resolved to.
        /// This is useful </param>
        /// <param name="controlPath">Receives the path to the referenced control on the device or <c>null</c> if not applicable.
        /// For example, with a <paramref name="path"/> of <c>"&lt;Gamepad&gt;/dpad/up"</c>, the resulting control path
        /// will be <c>"dpad/up"</c>. This is useful when trying to look up additional resources (such as images) based on the
        /// control that is being referenced.</param>
        /// <param name="options">Customize the resulting string.</param>
        /// <param name="control">An optional control. If supplied and the control or one of its children
        /// matches the given <paramref name="path"/>, display names will be based on the matching control
        /// rather than on static information available from <see cref="InputControlLayout"/>s.</param>
        /// <returns>A string such as "Left Stick/X [Gamepad]".</returns>
        /// <remarks>
        /// This function is most useful for turning binding paths (see <see cref="InputBinding.path"/>)
        /// into strings that can be displayed in UIs (such as rebinding screens). It is used by
        /// the Unity editor itself to display binding paths in the UI.
        ///
        /// The method uses display names (see <see cref="InputControlAttribute.displayName"/>,
        /// <see cref="InputControlLayoutAttribute.displayName"/>, and <see cref="InputControlLayout.ControlItem.displayName"/>)
        /// where possible. For example, "&lt;XInputController&gt;/buttonSouth" will be returned as
        /// "A [Xbox Controller]" as the display name of <see cref="XInput.XInputController"/> is "XBox Controller"
        /// and the display name of its "buttonSouth" control is "A".
        ///
        /// Note that these lookups depend on the currently registered control layouts (see <see
        /// cref="InputControlLayout"/>) and different strings may thus be returned for the same control
        /// path depending on the layouts registered with the system.
        ///
        /// <example>
        /// <code>
        /// InputControlPath.ToHumanReadableString("*/{PrimaryAction"); // -> "PrimaryAction [Any]"
        /// InputControlPath.ToHumanReadableString("&lt;Gamepad&gt;/buttonSouth"); // -> "Button South [Gamepad]"
        /// InputControlPath.ToHumanReadableString("&lt;XInputController&gt;/buttonSouth"); // -> "A [Xbox Controller]"
        /// InputControlPath.ToHumanReadableString("&lt;Gamepad&gt;/leftStick/x"); // -> "Left Stick/X [Gamepad]"
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.path"/>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingDisplayString(InputAction,int,InputBinding.DisplayStringOptions)"/>
        public static string ToHumanReadableString(string path,
            out string deviceLayoutName,
            out string controlPath,
            HumanReadableStringOptions options = HumanReadableStringOptions.None,
            InputControl control = null)
        {
            deviceLayoutName = null;
            controlPath = null;

            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // If we have a control, see if the path matches something in its hierarchy. If so,
            // don't both parsing the path and just use the matched control for creating a display
            // string.
            if (control != null)
            {
                var childControl = TryFindControl(control, path);
                var matchedControl = childControl ?? (Matches(path, control) ? control : null);

                if (matchedControl != null)
                {
                    var text = (options & HumanReadableStringOptions.UseShortNames) != 0 &&
                        !string.IsNullOrEmpty(matchedControl.shortDisplayName)
                        ? matchedControl.shortDisplayName
                        : matchedControl.displayName;

                    if ((options & HumanReadableStringOptions.OmitDevice) == 0)
                        text = $"{text} [{matchedControl.device.displayName}]";

                    deviceLayoutName = matchedControl.device.layout;
                    if (!(matchedControl is InputDevice))
                        controlPath = matchedControl.path.Substring(matchedControl.device.path.Length + 1);

                    return text;
                }
            }

            var buffer = new StringBuilder();
            var parser = new PathParser(path);

            // For display names of controls and devices, we need to look at InputControlLayouts.
            // If none is in place here, we establish a temporary layout cache while we go through
            // the path. If one is in place already, we reuse what's already there.
            using (InputControlLayout.CacheRef())
            {
                // First level is taken to be device.
                if (parser.MoveToNextComponent())
                {
                    // Keep track of which control layout we're on (if any) as we're crawling
                    // down the path.
                    var device = parser.current.ToHumanReadableString(null, null, out var currentLayoutName, out var _, options);
                    deviceLayoutName = currentLayoutName;

                    // Any additional levels (if present) are taken to form a control path on the device.
                    var isFirstControlLevel = true;
                    while (parser.MoveToNextComponent())
                    {
                        if (!isFirstControlLevel)
                            buffer.Append('/');

                        buffer.Append(parser.current.ToHumanReadableString(
                            currentLayoutName, controlPath, out currentLayoutName, out controlPath, options));
                        isFirstControlLevel = false;
                    }

                    if ((options & HumanReadableStringOptions.OmitDevice) == 0 && !string.IsNullOrEmpty(device))
                    {
                        buffer.Append(" [");
                        buffer.Append(device);
                        buffer.Append(']');
                    }
                }

                // If we didn't manage to figure out a display name, default to displaying
                // the path as is.
                if (buffer.Length == 0)
                    return path;

                return buffer.ToString();
            }
        }

        public static string[] TryGetDeviceUsages(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.m_Usages.length > 0)
                return parser.current.m_Usages.ToArray(x => x.ToString());

            return null;
        }

        /// <summary>
        /// From the given control path, try to determine the device layout being used.
        /// </summary>
        /// <remarks>
        /// This function will only use information available in the path itself or
        /// in layouts referenced by the path. It will not look at actual devices
        /// in the system. This is to make the behavior predictable and not dependent
        /// on whether you currently have the right device connected or not.
        /// </remarks>
        /// <param name="path">A control path (like "/&lt;gamepad&gt;/leftStick")</param>
        /// <returns>The name of the device layout used by the given control path or null
        /// if the path does not specify a device layout or does so in a way that is not
        /// supported by the function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <example>
        /// <code>
        /// InputControlPath.TryGetDeviceLayout("/&lt;gamepad&gt;/leftStick"); // Returns "gamepad".
        /// InputControlPath.TryGetDeviceLayout("/*/leftStick"); // Returns "*".
        /// InputControlPath.TryGetDeviceLayout("/gamepad/leftStick"); // Returns null. "gamepad" is a device name here.
        /// </code>
        /// </example>
        public static string TryGetDeviceLayout(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.m_Layout.length > 0)
                return parser.current.m_Layout.ToString().Unescape();

            if (parser.current.isWildcard)
                return Wildcard;

            return null;
        }

        ////TODO: return Substring and use path parser; should get rid of allocations

        // From the given control path, try to determine the control layout being used.
        // NOTE: Allocates!
        public static string TryGetControlLayout(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var pathLength = path.Length;

            var indexOfLastSlash = path.LastIndexOf('/');
            if (indexOfLastSlash == -1 || indexOfLastSlash == 0)
            {
                // If there's no '/' at all in the path, it surely does not mention
                // a control. Same if the '/' is the first thing in the path.
                return null;
            }

            // Simplest case where control layout is mentioned explicitly with '<..>'.
            // Note this will only catch if the control is *only* referenced by layout and not by anything else
            // in addition (like usage or name).
            if (pathLength > indexOfLastSlash + 2 && path[indexOfLastSlash + 1] == '<' && path[pathLength - 1] == '>')
            {
                var layoutNameStart = indexOfLastSlash + 2;
                var layoutNameLength = pathLength - layoutNameStart - 1;
                return path.Substring(layoutNameStart, layoutNameLength);
            }

            // Have to actually look at the path in detail.
            var parser = new PathParser(path);
            if (!parser.MoveToNextComponent())
                return null;

            if (parser.current.isWildcard)
                throw new NotImplementedException();

            if (parser.current.m_Layout.length == 0)
                return null;

            var deviceLayoutName = parser.current.m_Layout.ToString();
            if (!parser.MoveToNextComponent())
                return null; // No control component.

            if (parser.current.isWildcard)
                return Wildcard;

            return FindControlLayoutRecursive(ref parser, deviceLayoutName.Unescape());
        }

        private static string FindControlLayoutRecursive(ref PathParser parser, string layoutName)
        {
            using (InputControlLayout.CacheRef())
            {
                // Load layout.
                var layout = InputControlLayout.cache.FindOrLoadLayout(new InternedString(layoutName), throwIfNotFound: false);
                if (layout == null)
                    return null;

                // Search for control layout. May have to jump to other layouts
                // and search in them.
                return FindControlLayoutRecursive(ref parser, layout);
            }
        }

        private static string FindControlLayoutRecursive(ref PathParser parser, InputControlLayout layout)
        {
            string currentResult = null;

            var controlCount = layout.controls.Count;
            for (var i = 0; i < controlCount; ++i)
            {
                ////TODO: shortcut the search if we have a match and there's no wildcards to consider

                // Skip control layout if it doesn't match.
                if (!ControlLayoutMatchesPathComponent(ref layout.m_Controls[i], ref parser))
                    continue;

                var controlLayoutName = layout.m_Controls[i].layout;

                // If there's more in the path, try to dive into children by jumping to the
                // control's layout.
                if (!parser.isAtEnd)
                {
                    var childPathParser = parser;
                    if (childPathParser.MoveToNextComponent())
                    {
                        var childControlLayoutName = FindControlLayoutRecursive(ref childPathParser, controlLayoutName);
                        if (childControlLayoutName != null)
                        {
                            if (currentResult != null && childControlLayoutName != currentResult)
                                return null;
                            currentResult = childControlLayoutName;
                        }
                    }
                }
                else if (currentResult != null && controlLayoutName != currentResult)
                    return null;
                else
                    currentResult = controlLayoutName.ToString();
            }

            return currentResult;
        }

        private static bool ControlLayoutMatchesPathComponent(ref InputControlLayout.ControlItem controlItem, ref PathParser parser)
        {
            // Match layout.
            var layout = parser.current.m_Layout;
            if (layout.length > 0)
            {
                if (!StringMatches(layout, controlItem.layout))
                    return false;
            }

            // Match usage.
            if (parser.current.m_Usages.length > 0)
            {
                // All of usages should match to the one of usage in the control
                for (int usageIndex = 0; usageIndex < parser.current.m_Usages.length; ++usageIndex)
                {
                    var usage = parser.current.m_Usages[usageIndex];

                    if (usage.length > 0)
                    {
                        var usageCount = controlItem.usages.Count;
                        var anyUsageMatches = false;
                        for (var i = 0; i < usageCount; ++i)
                        {
                            if (StringMatches(usage, controlItem.usages[i]))
                            {
                                anyUsageMatches = true;
                                break;
                            }
                        }

                        if (!anyUsageMatches)
                            return false;
                    }
                }
            }

            // Match name.
            var name = parser.current.m_Name;
            if (name.length > 0)
            {
                if (!StringMatches(name, controlItem.name))
                    return false;
            }

            return true;
        }

        // Match two name strings allowing for wildcards.
        // 'str' may contain wildcards. 'matchTo' may not.
        private static bool StringMatches(Substring str, InternedString matchTo)
        {
            var strLength = str.length;
            var matchToLength = matchTo.length;

            // Can't compare lengths here because str may contain wildcards and
            // thus be shorter than matchTo and still match.

            var matchToLowerCase = matchTo.ToLower();

            // We manually walk the string here so that we can deal with "normal"
            // comparisons as well as with wildcards.
            var posInMatchTo = 0;
            var posInStr = 0;
            while (posInStr < strLength && posInMatchTo < matchToLength)
            {
                var nextChar = str[posInStr];
                if (nextChar == '\\' && posInStr + 1 < strLength)
                    nextChar = str[++posInStr];
                if (nextChar == '*')
                {
                    ////TODO: make sure we don't end up with ** here

                    if (posInStr == strLength - 1)
                        return true; // Wildcard at end of string so rest is matched.

                    ++posInStr;
                    nextChar = char.ToLower(str[posInStr], CultureInfo.InvariantCulture);

                    while (posInMatchTo < matchToLength && matchToLowerCase[posInMatchTo] != nextChar)
                        ++posInMatchTo;

                    if (posInMatchTo == matchToLength)
                        return false; // Matched all the way to end of matchTo but there's more in str after the wildcard.
                }
                else if (char.ToLower(nextChar, CultureInfo.InvariantCulture) != matchToLowerCase[posInMatchTo])
                {
                    return false;
                }

                ++posInMatchTo;
                ++posInStr;
            }

            return posInMatchTo == matchToLength && posInStr == strLength; // Check if we have consumed all input. Prevent prefix-only match.
        }

        public static InputControl TryFindControl(InputControl control, string path, int indexInPath = 0)
        {
            return TryFindControl<InputControl>(control, path, indexInPath);
        }

        public static InputControl[] TryFindControls(InputControl control, string path, int indexInPath = 0)
        {
            var matches = new InputControlList<InputControl>(Allocator.Temp);
            try
            {
                TryFindControls(control, path, indexInPath, ref matches);
                return matches.ToArray();
            }
            finally
            {
                matches.Dispose();
            }
        }

        public static int TryFindControls(InputControl control, string path, ref InputControlList<InputControl> matches, int indexInPath = 0)
        {
            return TryFindControls(control, path, indexInPath, ref matches);
        }

        /// <summary>
        /// Return the first child control that matches the given path.
        /// </summary>
        /// <param name="control">Control root at which to start the search.</param>
        /// <param name="path">Path of the control to find. Can be <c>null</c> or empty, in which case <c>null</c>
        /// is returned.</param>
        /// <param name="indexInPath">Index in <paramref name="path"/> at which to start parsing. Defaults to
        /// 0, i.e. parsing starts at the first character in the path.</param>
        /// <returns>The first (direct or indirect) child control of <paramref name="control"/> that matches
        /// <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Does not allocate.
        ///
        /// Note that if multiple child controls match the given path, which one is returned depends on the
        /// ordering of controls. The result should be considered indeterministic in this case.
        ///
        /// <example>
        /// <code>
        /// // Find X control of left stick on current gamepad.
        /// InputControlPath.TryFindControl(Gamepad.current, "leftStick/x");
        ///
        /// // Find control with PrimaryAction usage on current mouse.
        /// InputControlPath.TryFindControl(Mouse.current, "{PrimaryAction}");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControl.this[string]"/>
        public static TControl TryFindControl<TControl>(InputControl control, string path, int indexInPath = 0)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (string.IsNullOrEmpty(path))
                return null;

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

            var none = new InputControlList<TControl>();
            return MatchControlsRecursive(control, path, indexInPath, ref none, matchMultiple: false);
        }

        /// <summary>
        /// Perform a search for controls starting with the given control as root and matching
        /// the given path from the given position. Puts all matching controls on the list and
        /// returns the number of controls that have been matched.
        /// </summary>
        /// <param name="control">Control at which the given path is rooted.</param>
        /// <param name="path"></param>
        /// <param name="indexInPath"></param>
        /// <param name="matches"></param>
        /// <typeparam name="TControl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// Matching is case-insensitive.
        ///
        /// Does not allocate managed memory.
        /// </remarks>
        public static int TryFindControls<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (indexInPath == 0 && path[0] == '/')
                ++indexInPath;

            var countBefore = matches.Count;
            MatchControlsRecursive(control, path, indexInPath, ref matches, matchMultiple: true);
            return matches.Count - countBefore;
        }

        ////REVIEW: what's the difference between TryFindChild and TryFindControl??

        public static InputControl TryFindChild(InputControl control, string path, int indexInPath = 0)
        {
            return TryFindChild<InputControl>(control, path, indexInPath);
        }

        public static TControl TryFindChild<TControl>(InputControl control, string path, int indexInPath = 0)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var children = control.children;
            var childCount = children.Count;
            for (var i = 0; i < childCount; ++i)
            {
                var child = children[i];
                var match = TryFindControl<TControl>(child, path, indexInPath);
                if (match != null)
                    return match;
            }

            return null;
        }

        ////REVIEW: probably would be good to have a Matches(string,string) version

        public static bool Matches(string expected, InputControl control)
        {
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentNullException(nameof(expected));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var parser = new PathParser(expected);
            return MatchesRecursive(ref parser, control);
        }

        internal static bool MatchControlComponent(ref ParsedPathComponent expectedControlComponent, ref InputControlLayout.ControlItem controlItem, bool matchAlias = false)
        {
            bool controlItemNameMatched = false;
            var anyUsageMatches = false;

            // Check to see that there is a match with the name or alias if specified
            // Exit early if we can't create a match.
            if (!expectedControlComponent.m_Name.isEmpty)
            {
                if (StringMatches(expectedControlComponent.m_Name, controlItem.name))
                    controlItemNameMatched = true;
                else if (matchAlias)
                {
                    var aliases = controlItem.aliases;
                    for (var i = 0; i < aliases.Count; i++)
                    {
                        if (StringMatches(expectedControlComponent.m_Name, aliases[i]))
                        {
                            controlItemNameMatched = true;
                            break;
                        }
                    }
                }
                else
                    return false;
            }

            // All of usages should match to the one of usage in the control
            foreach (var usage in expectedControlComponent.m_Usages)
            {
                if (!usage.isEmpty)
                {
                    var usageCount = controlItem.usages.Count;
                    for (var i = 0; i < usageCount; ++i)
                    {
                        if (StringMatches(usage, controlItem.usages[i]))
                        {
                            anyUsageMatches = true;
                            break;
                        }
                    }
                }
            }

            // Return whether or not we were able to match an alias or a usage
            return controlItemNameMatched || anyUsageMatches;
        }

        /// <summary>
        /// Check whether the given path matches <paramref name="control"/> or any of its parents.
        /// </summary>
        /// <param name="expected">A control path.</param>
        /// <param name="control">An input control.</param>
        /// <returns>True if the given path matches at least a partial path to <paramref name="control"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c> or empty -or-
        /// <paramref name="control"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// // True as the path matches the Keyboard device itself, i.e. the parent of
        /// // Keyboard.aKey.
        /// InputControlPath.MatchesPrefix("&lt;Keyboard&gt;", Keyboard.current.aKey);
        ///
        /// // False as the path matches none of the controls leading to Keyboard.aKey.
        /// InputControlPath.MatchesPrefix("&lt;Gamepad&gt;", Keyboard.current.aKey);
        ///
        /// // True as the path matches Keyboard.aKey itself.
        /// InputControlPath.MatchesPrefix("&lt;Keyboard&gt;/a", Keyboard.current.aKey);
        /// </code>
        /// </example>
        /// </remarks>
        public static bool MatchesPrefix(string expected, InputControl control)
        {
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentNullException(nameof(expected));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var parser = new PathParser(expected);
            if (MatchesRecursive(ref parser, control, prefixOnly: true) && parser.isAtEnd)
                return true;

            return false;
        }

        private static bool MatchesRecursive(ref PathParser parser, InputControl currentControl, bool prefixOnly = false)
        {
            // Recurse into parent before looking at the current control. This
            // will advance the parser to where our control is in the path.
            var parent = currentControl.parent;
            if (parent != null && !MatchesRecursive(ref parser, parent, prefixOnly))
                return false;

            // Stop if there's no more path left.
            if (!parser.MoveToNextComponent())
                return prefixOnly; // Failure if we match full path, success if we match prefix only.

            // Match current path component against current control.
            return parser.current.Matches(currentControl);
        }

        ////TODO: refactor this to use the new PathParser

        /// <summary>
        /// Recursively match path elements in <paramref name="path"/>.
        /// </summary>
        /// <param name="control">Current control we're at.</param>
        /// <param name="path">Control path we are matching against.</param>
        /// <param name="indexInPath">Index of current component in <paramref name="path"/>.</param>
        /// <param name="matches"></param>
        /// <param name="matchMultiple"></param>
        /// <typeparam name="TControl"></typeparam>
        /// <returns></returns>
        private static TControl MatchControlsRecursive<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
        {
            var pathLength = path.Length;

            // Try to get a match. A path spec has three components:
            //    "<layout>{usage}name"
            // All are optional but at least one component must be present.
            // Names can be aliases, too.
            // We don't tap InputControl.path strings of controls so as to not create a
            // bunch of string objects while feeling our way down the hierarchy.

            var controlIsMatch = true;

            // Match by layout.
            if (path[indexInPath] == '<')
            {
                ++indexInPath;
                controlIsMatch =
                    MatchPathComponent(control.layout, path, ref indexInPath, PathComponentType.Layout);

                // If the layout isn't a match, walk up the base layout
                // chain and match each base layout.
                if (!controlIsMatch)
                {
                    var baseLayout = control.m_Layout;
                    while (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout))
                    {
                        controlIsMatch = MatchPathComponent(baseLayout, path, ref indexInPath,
                            PathComponentType.Layout);
                        if (controlIsMatch)
                            break;
                    }
                }
            }

            // Match by usage.
            while (indexInPath < pathLength && path[indexInPath] == '{' && controlIsMatch)
            {
                ++indexInPath;

                for (var i = 0; i < control.usages.Count; ++i)
                {
                    controlIsMatch = MatchPathComponent(control.usages[i], path, ref indexInPath, PathComponentType.Usage);
                    if (controlIsMatch)
                        break;
                }
            }

            // Match by display name.
            if (indexInPath < pathLength - 1 && controlIsMatch && path[indexInPath] == '#' &&
                path[indexInPath + 1] == '(')
            {
                indexInPath += 2;
                controlIsMatch = MatchPathComponent(control.displayName, path, ref indexInPath,
                    PathComponentType.DisplayName);
            }

            // Match by name.
            if (indexInPath < pathLength && controlIsMatch && path[indexInPath] != '/')
            {
                // Normal name match.
                controlIsMatch = MatchPathComponent(control.name, path, ref indexInPath, PathComponentType.Name);

                // Alternative match by alias.
                if (!controlIsMatch)
                {
                    for (var i = 0; i < control.aliases.Count && !controlIsMatch; ++i)
                    {
                        controlIsMatch = MatchPathComponent(control.aliases[i], path, ref indexInPath,
                            PathComponentType.Name);
                    }
                }
            }

            // If we have a match, return it or, if there's children, recurse into them.
            if (controlIsMatch)
            {
                // If we ended up on a wildcard, we've successfully matched it.
                if (indexInPath < pathLength && path[indexInPath] == '*')
                    ++indexInPath;

                // If we've reached the end of the path, we have a match.
                if (indexInPath == pathLength)
                {
                    // Check type.
                    if (!(control is TControl match))
                        return null;

                    if (matchMultiple)
                        matches.Add(match);
                    return match;
                }

                // If we've reached a separator, dive into our children.
                if (path[indexInPath] == '/')
                {
                    ++indexInPath;

                    // Silently accept trailing slashes.
                    if (indexInPath == pathLength)
                    {
                        // Check type.
                        if (!(control is TControl match))
                            return null;

                        if (matchMultiple)
                            matches.Add(match);
                        return match;
                    }

                    // See if we want to match children by usage or by name.
                    TControl lastMatch;
                    if (path[indexInPath] == '{')
                    {
                        // Usages are kind of like entry points that can route to anywhere else
                        // on a device's control hierarchy and then we keep going from that re-routed
                        // point.
                        lastMatch = MatchByUsageAtDeviceRootRecursive(control.device, path, indexInPath, ref matches, matchMultiple);
                    }
                    else
                    {
                        // Go through children and see what we can match.
                        lastMatch = MatchChildrenRecursive(control, path, indexInPath, ref matches, matchMultiple);
                    }

                    return lastMatch;
                }
            }

            return null;
        }

        private static TControl MatchByUsageAtDeviceRootRecursive<TControl>(InputDevice device, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
        {
            // NOTE: m_UsagesForEachControl includes usages for the device. m_UsageToControl does not.

            var usages = device.m_UsagesForEachControl;
            if (usages == null)
                return null;

            var usageCount = device.m_UsageToControl.LengthSafe();
            var startIndex = indexInPath + 1;
            var pathCanMatchMultiple = PathComponentCanYieldMultipleMatches(path, indexInPath);
            var pathLength = path.Length;

            Debug.Assert(path[indexInPath] == '{');
            ++indexInPath;
            if (indexInPath == pathLength)
                throw new ArgumentException($"Invalid path spec '{path}'; trailing '{{'", nameof(path));

            TControl lastMatch = null;

            for (var i = 0; i < usageCount; ++i)
            {
                var usage = usages[i];
                Debug.Assert(!string.IsNullOrEmpty(usage), "Usage entry is empty");

                // Match usage against path.
                var usageIsMatch = MatchPathComponent(usage, path, ref indexInPath, PathComponentType.Usage);

                // If it isn't a match, go to next usage.
                if (!usageIsMatch)
                {
                    indexInPath = startIndex;
                    continue;
                }

                var controlMatchedByUsage = device.m_UsageToControl[i];

                // If there's more to go in the path, dive into the children of the control.
                if (indexInPath < pathLength && path[indexInPath] == '/')
                {
                    lastMatch = MatchChildrenRecursive(controlMatchedByUsage, path, indexInPath + 1,
                        ref matches, matchMultiple);

                    // We can stop going through usages if we matched something and the
                    // path component covering usage does not contain wildcards.
                    if (lastMatch != null && !pathCanMatchMultiple)
                        break;

                    // We can stop going through usages if we have a match and are only
                    // looking for a single one.
                    if (lastMatch != null && !matchMultiple)
                        break;
                }
                else
                {
                    lastMatch = controlMatchedByUsage as TControl;
                    if (lastMatch != null)
                    {
                        if (matchMultiple)
                            matches.Add(lastMatch);
                        else
                        {
                            // Only looking for single match and we have one.
                            break;
                        }
                    }
                }
            }

            return lastMatch;
        }

        private static TControl MatchChildrenRecursive<TControl>(InputControl control, string path, int indexInPath,
            ref InputControlList<TControl> matches, bool matchMultiple)
            where TControl : InputControl
        {
            var children = control.children;
            var childCount = children.Count;
            TControl lastMatch = null;
            var pathCanMatchMultiple = PathComponentCanYieldMultipleMatches(path, indexInPath);

            for (var i = 0; i < childCount; ++i)
            {
                var child = children[i];
                var childMatch = MatchControlsRecursive(child, path, indexInPath, ref matches, matchMultiple);

                if (childMatch == null)
                    continue;

                // If the child matched something an there's no wildcards in the child
                // portion of the path, we can stop searching.
                if (!pathCanMatchMultiple)
                    return childMatch;

                // If we are only looking for the first match and a child matched,
                // we can also stop.
                if (!matchMultiple)
                    return childMatch;

                // Otherwise we have to go hunting through the hierarchy in case there are
                // more matches.
                lastMatch = childMatch;
            }

            return lastMatch;
        }

        private enum PathComponentType
        {
            Name,
            DisplayName,
            Usage,
            Layout
        }

        private static bool MatchPathComponent(string component, string path, ref int indexInPath, PathComponentType componentType, int startIndexInComponent = 0)
        {
            Debug.Assert(component != null, "Component string is null");
            Debug.Assert(path != null, "Path is null");

            var componentLength = component.Length;
            var pathLength = path.Length;
            var startIndex = indexInPath;

            // Try to walk the name as far as we can.
            var indexInComponent = startIndexInComponent;
            while (indexInPath < pathLength)
            {
                // Check if we've reached a terminator in the path.
                var nextCharInPath = path[indexInPath];
                if (nextCharInPath == '\\' && indexInPath + 1 < pathLength)
                {
                    // Escaped character. Bypass treatment of special characters below.
                    ++indexInPath;
                    nextCharInPath = path[indexInPath];
                }
                else
                {
                    if (nextCharInPath == '/' && componentType == PathComponentType.Name)
                        break;
                    if ((nextCharInPath == '>' && componentType == PathComponentType.Layout)
                        || (nextCharInPath == '}' && componentType == PathComponentType.Usage)
                        || (nextCharInPath == ')' && componentType == PathComponentType.DisplayName))
                    {
                        ++indexInPath;
                        break;
                    }

                    ////TODO: allow only single '*' and recognize '**'
                    // If we've reached a '*' in the path, skip character in name.
                    if (nextCharInPath == '*')
                    {
                        // But first let's see if we have something after the wildcard that matches the rest of the component.
                        // This could be when, for example, we hit "T" on matching "leftTrigger" against "*Trigger". We have to stop
                        // gobbling up characters for the wildcard when reaching "Trigger" in the component name.
                        //
                        // NOTE: Just looking at the very next character only is *NOT* enough. We need to match the entire rest of
                        //       the path. Otherwise, in the example above, we would stop on seeing the lowercase 't' and then be left
                        //       trying to match "tTrigger" against "Trigger".
                        var indexAfterWildcard = indexInPath + 1;
                        if (indexInPath < (pathLength - 1) &&
                            indexInComponent < componentLength &&
                            MatchPathComponent(component, path, ref indexAfterWildcard, componentType, indexInComponent))
                        {
                            indexInPath = indexAfterWildcard;
                            return true;
                        }

                        if (indexInComponent < componentLength)
                            ++indexInComponent;
                        else
                            return true;

                        continue;
                    }
                }

                // If we've reached the end of the component name, we did so before
                // we've reached a terminator
                if (indexInComponent == componentLength)
                {
                    indexInPath = startIndex;
                    return false;
                }

                var charInComponent = component[indexInComponent];
                if (charInComponent == nextCharInPath || char.ToLower(charInComponent, CultureInfo.InvariantCulture) == char.ToLower(nextCharInPath, CultureInfo.InvariantCulture))
                {
                    ++indexInComponent;
                    ++indexInPath;
                }
                else
                {
                    // Name isn't a match.
                    indexInPath = startIndex;
                    return false;
                }
            }

            if (indexInComponent == componentLength)
                return true;

            indexInPath = startIndex;
            return false;
        }

        private static bool PathComponentCanYieldMultipleMatches(string path, int indexInPath)
        {
            var indexOfNextSlash = path.IndexOf('/', indexInPath);
            if (indexOfNextSlash == -1)
                return path.IndexOf('*', indexInPath) != -1 || path.IndexOf('<', indexInPath) != -1;

            var length = indexOfNextSlash - indexInPath;
            return path.IndexOf('*', indexInPath, length) != -1 || path.IndexOf('<', indexInPath, length) != -1;
        }

        /// <summary>
        /// A single component of a parsed control path as returned by <see cref="Parse"/>. For example, in the
        /// control path <c>"&lt;Gamepad&gt;/buttonSouth"</c>, there are two parts: <c>"&lt;Gamepad&gt;"</c>
        /// and <c>"buttonSouth"</c>.
        /// </summary>
        /// <seealso cref="Parse"/>
        public struct ParsedPathComponent
        {
            // Accessing these means no allocations (except when there are multiple usages).
            internal Substring m_Layout;
            internal InlinedArray<Substring> m_Usages;
            internal Substring m_Name;
            internal Substring m_DisplayName;

            /// <summary>
            /// Name of the layout (the part between '&lt;' and '&gt;') referenced in the component or <c>null</c> if no layout
            /// is specified. In <c>"&lt;Gamepad&gt;/buttonSouth"</c> the first component will return
            /// <c>"Gamepad"</c> from this property and the second component will return <c>null</c>.
            /// </summary>
            /// <seealso cref="InputControlLayout"/>
            /// <seealso cref="InputSystem.LoadLayout"/>
            /// <seealso cref="InputControl.layout"/>
            public string layout => m_Layout.ToString();

            /// <summary>
            /// List of device or control usages (the part between '{' and '}') referenced in the component or an empty
            /// enumeration. In <c>"&lt;XRController&gt;{RightHand}/trigger"</c>, for example, the
            /// first component will have a single element <c>"RightHand"</c> in the enumeration
            /// and the second component will have an empty enumeration.
            /// </summary>
            /// <seealso cref="InputControl.usages"/>
            /// <seealso cref="InputSystem.AddDeviceUsage(InputDevice,string)"/>
            public IEnumerable<string> usages => m_Usages.Select(x => x.ToString());

            /// <summary>
            /// Name of the device or control referenced in the component or <c>null</c> In
            /// <c>"&lt;Gamepad&gt;/buttonSouth"</c>, for example, the first component will
            /// have a <c>null</c> name and the second component will have <c>"buttonSouth"</c>
            /// in the name.
            /// </summary>
            /// <seealso cref="InputControl.name"/>
            public string name => m_Name.ToString();

            /// <summary>
            /// Display name of the device or control (the part inside of '#(...)') referenced in the component
            /// or <c>null</c>. In <c>"&lt;Keyboard&gt;/#(*)"</c>, for example, the first component will
            /// have a null displayName and the second component will have a displayName of <c>"*"</c>.
            /// </summary>
            /// <seealso cref="InputControl.displayName"/>
            public string displayName => m_DisplayName.ToString();

            ////REVIEW: This one isn't well-designed enough yet to be exposed. And double-wildcards are not yet supported.
            internal bool isWildcard => m_Name == Wildcard;
            internal bool isDoubleWildcard => m_Name == DoubleWildcard;

            internal string ToHumanReadableString(string parentLayoutName, string parentControlPath, out string referencedLayoutName,
                out string controlPath, HumanReadableStringOptions options)
            {
                referencedLayoutName = null;
                controlPath = null;

                var result = string.Empty;
                if (isWildcard)
                    result += "Any";

                if (m_Usages.length > 0)
                {
                    var combinedUsages = string.Empty;

                    for (var i = 0; i < m_Usages.length; ++i)
                    {
                        if (m_Usages[i].isEmpty)
                            continue;

                        if (combinedUsages != string.Empty)
                            combinedUsages += " & " + ToHumanReadableString(m_Usages[i]);
                        else
                            combinedUsages = ToHumanReadableString(m_Usages[i]);
                    }
                    if (combinedUsages != string.Empty)
                    {
                        if (result != string.Empty)
                            result += ' ' + combinedUsages;
                        else
                            result += combinedUsages;
                    }
                }

                if (!m_Layout.isEmpty)
                {
                    referencedLayoutName = m_Layout.ToString();

                    // Where possible, use the displayName of the given layout rather than
                    // just the internal layout name.
                    string layoutString;
                    var referencedLayout = InputControlLayout.cache.FindOrLoadLayout(referencedLayoutName, throwIfNotFound: false);
                    if (referencedLayout != null && !string.IsNullOrEmpty(referencedLayout.m_DisplayName))
                        layoutString = referencedLayout.m_DisplayName;
                    else
                        layoutString = ToHumanReadableString(m_Layout);

                    if (!string.IsNullOrEmpty(result))
                        result += ' ' + layoutString;
                    else
                        result += layoutString;
                }

                if (!m_Name.isEmpty && !isWildcard)
                {
                    // If we have a layout from a preceding path component, try to find
                    // the control by name on the layout. If we find it, use its display
                    // name rather than the name referenced in the binding.
                    string nameString = null;
                    if (!string.IsNullOrEmpty(parentLayoutName))
                    {
                        // NOTE: This produces a fully merged layout. We should thus pick up display names
                        //       from base layouts automatically wherever applicable.
                        var parentLayout =
                            InputControlLayout.cache.FindOrLoadLayout(new InternedString(parentLayoutName), throwIfNotFound: false);
                        if (parentLayout != null)
                        {
                            var controlName = new InternedString(m_Name.ToString());
                            var control = parentLayout.FindControlIncludingArrayElements(controlName, out var arrayIndex);
                            if (control != null)
                            {
                                // Synthesize path of control.
                                if (string.IsNullOrEmpty(parentControlPath))
                                {
                                    if (arrayIndex != -1)
                                        controlPath = $"{control.Value.name}{arrayIndex}";
                                    else
                                        controlPath = control.Value.name;
                                }
                                else
                                {
                                    if (arrayIndex != -1)
                                        controlPath = $"{parentControlPath}/{control.Value.name}{arrayIndex}";
                                    else
                                        controlPath = $"{parentControlPath}/{control.Value.name}";
                                }

                                var shortDisplayName = (options & HumanReadableStringOptions.UseShortNames) != 0
                                    ? control.Value.shortDisplayName
                                    : null;

                                var displayName = !string.IsNullOrEmpty(shortDisplayName)
                                    ? shortDisplayName
                                    : control.Value.displayName;

                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    if (arrayIndex != -1)
                                        nameString = $"{displayName} #{arrayIndex}";
                                    else
                                        nameString = displayName;
                                }

                                // If we don't have an explicit <layout> part in the component,
                                // remember the name of the layout referenced by the control name so
                                // that path components further down the line can keep looking up their
                                // display names.
                                if (string.IsNullOrEmpty(referencedLayoutName))
                                    referencedLayoutName = control.Value.layout;
                            }
                        }
                    }

                    if (nameString == null)
                        nameString = ToHumanReadableString(m_Name);

                    if (!string.IsNullOrEmpty(result))
                        result += ' ' + nameString;
                    else
                        result += nameString;
                }

                if (!m_DisplayName.isEmpty)
                {
                    var str = $"\"{ToHumanReadableString(m_DisplayName)}\"";
                    if (!string.IsNullOrEmpty(result))
                        result += ' ' + str;
                    else
                        result += str;
                }

                return result;
            }

            private static string ToHumanReadableString(Substring substring)
            {
                return substring.ToString().Unescape("/*{<", "/*{<");
            }

            /// <summary>
            /// Whether the given control matches the constraints of this path component.
            /// </summary>
            /// <param name="control">Control to match against the path spec.</param>
            /// <returns>True if <paramref name="control"/> matches the constraints.</returns>
            public bool Matches(InputControl control)
            {
                // Match layout.
                if (!m_Layout.isEmpty)
                {
                    // Check for direct match.
                    var layoutMatches = ComparePathElementToString(m_Layout, control.layout);
                    if (!layoutMatches)
                    {
                        // No direct match but base layout may match.
                        var baseLayout = control.m_Layout;
                        while (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout) && !layoutMatches)
                            layoutMatches = ComparePathElementToString(m_Layout, baseLayout.ToString());
                    }

                    if (!layoutMatches)
                        return false;
                }

                // Match usage.
                if (m_Usages.length > 0)
                {
                    for (var i = 0; i < m_Usages.length; ++i)
                    {
                        if (!m_Usages[i].isEmpty)
                        {
                            var controlUsages = control.usages;
                            var haveUsageMatch = false;
                            for (var ci = 0; ci < controlUsages.Count; ++ci)
                                if (ComparePathElementToString(m_Usages[i], controlUsages[ci]))
                                {
                                    haveUsageMatch = true;
                                    break;
                                }

                            if (!haveUsageMatch)
                                return false;
                        }
                    }
                }

                // Match name.
                if (!m_Name.isEmpty && !isWildcard)
                {
                    ////FIXME: unlike the matching path we have in MatchControlsRecursive, this does not take aliases into account
                    if (!ComparePathElementToString(m_Name, control.name))
                        return false;
                }

                // Match display name.
                if (!m_DisplayName.isEmpty)
                {
                    if (!ComparePathElementToString(m_DisplayName, control.displayName))
                        return false;
                }

                return true;
            }

            // In a path, characters may be escaped so in those cases, we can't just compare
            // character-by-character.
            private static bool ComparePathElementToString(Substring pathElement, string element)
            {
                var pathElementLength = pathElement.length;
                var elementLength = element.Length;

                for (int i = 0, j = 0;; i++, j++)
                {
                    var pathElementDone = i == pathElementLength;
                    var elementDone     = j == elementLength;

                    if (pathElementDone || elementDone)
                        return pathElementDone == elementDone;

                    var ch = pathElement[i];
                    if (ch == '\\' && i + 1 < pathElementLength)
                        ch = pathElement[++i];

                    if (char.ToLowerInvariant(ch) != char.ToLowerInvariant(element[j]))
                        return false;
                }
            }
        }

        /// <summary>
        /// Splits a control path into its separate components.
        /// </summary>
        /// <param name="path">A control path such as <c>"&lt;Gamepad&gt;/buttonSouth"</c>.</param>
        /// <returns>An enumeration of the parsed components. The enumeration is empty if the given
        /// <paramref name="path"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// You can use this method, for example, to separate out the components in a binding's <see cref="InputBinding.path"/>.
        ///
        /// <example>
        /// <code>
        /// var parsed = InputControlPath.Parse("&lt;XRController&gt;{LeftHand}/trigger").ToArray();
        ///
        /// Debug.Log(parsed.Length); // Prints 2.
        /// Debug.Log(parsed[0].layout); // Prints "XRController".
        /// Debug.Log(parsed[0].name); // Prints an empty string.
        /// Debug.Log(parsed[0].usages.First()); // Prints "LeftHand".
        /// Debug.Log(parsed[1].layout); // Prints null.
        /// Debug.Log(parsed[1].name); // Prints "trigger".
        ///
        /// // Find out if the given device layout is based on "TrackedDevice".
        /// Debug.Log(InputSystem.IsFirstLayoutBasedOnSecond(parsed[0].layout, "TrackedDevice")); // Prints true.
        ///
        /// // Load the device layout referenced by the path.
        /// var layout = InputSystem.LoadLayout(parsed[0].layout);
        /// Debug.Log(layout.baseLayouts.First()); // Prints "TrackedDevice".
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.path"/>
        /// <seealso cref="InputSystem.FindControl"/>
        public static IEnumerable<ParsedPathComponent> Parse(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var parser = new PathParser(path);
            while (parser.MoveToNextComponent())
                yield return parser.current;
        }

        // NOTE: Must not allocate!
        private struct PathParser
        {
            private string path;
            private int length;
            private int leftIndexInPath;
            private int rightIndexInPath; // Points either to a '/' character or one past the end of the path string.

            public ParsedPathComponent current;

            public bool isAtEnd => rightIndexInPath == length;

            public PathParser(string path)
            {
                Debug.Assert(path != null);

                this.path = path;
                length = path.Length;
                leftIndexInPath = 0;
                rightIndexInPath = 0;
                current = new ParsedPathComponent();
            }

            // Update parsing state and 'current' to next component in path.
            // Returns true if the was another component or false if the end of the path was reached.
            public bool MoveToNextComponent()
            {
                // See if we've the end of the path string.
                if (rightIndexInPath == length)
                    return false;

                // Make our current right index our new left index and find
                // a new right index from there.
                leftIndexInPath = rightIndexInPath;
                if (path[leftIndexInPath] == '/')
                {
                    ++leftIndexInPath;
                    rightIndexInPath = leftIndexInPath;
                    if (leftIndexInPath == length)
                        return false;
                }

                // Parse <...> layout part, if present.
                var layout = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] == '<')
                    layout = ParseComponentPart('>');

                ////FIXME: with multiple usages, this will allocate
                ////FIXME: Why the heck is this allocating? Should not allocate here! Worse yet, we do ToArray() down there.
                // Parse {...} usage part, if present.
                var usages = new InlinedArray<Substring>();
                while (rightIndexInPath < length && path[rightIndexInPath] == '{')
                    usages.AppendWithCapacity(ParseComponentPart('}'));

                // Parse display name part, if present.
                var displayName = new Substring();
                if (rightIndexInPath < length - 1 && path[rightIndexInPath] == '#' && path[rightIndexInPath + 1] == '(')
                {
                    ++rightIndexInPath;
                    displayName = ParseComponentPart(')');
                }

                // Parse name part, if present.
                var name = new Substring();
                if (rightIndexInPath < length && path[rightIndexInPath] != '/')
                    name = ParseComponentPart('/');

                current = new ParsedPathComponent
                {
                    m_Layout = layout,
                    m_Usages = usages,
                    m_Name = name,
                    m_DisplayName = displayName
                };

                return leftIndexInPath != rightIndexInPath;
            }

            private Substring ParseComponentPart(char terminator)
            {
                if (terminator != '/') // Name has no corresponding left side terminator.
                    ++rightIndexInPath;

                var partStartIndex = rightIndexInPath;
                while (rightIndexInPath < length && path[rightIndexInPath] != terminator)
                {
                    if (path[rightIndexInPath] == '\\' && rightIndexInPath + 1 < length)
                        ++rightIndexInPath;
                    ++rightIndexInPath;
                }

                var partLength = rightIndexInPath - partStartIndex;
                if (rightIndexInPath < length && terminator != '/')
                    ++rightIndexInPath; // Skip past terminator.

                return new Substring(path, partStartIndex, partLength);
            }
        }
    }
}
