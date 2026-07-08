#if UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Steam.Editor
{
    /// <summary>
    /// Converts input actions to and from Steam IGA file format.
    /// </summary>
    /// <remarks>
    /// The idea behind this converter is to enable users to use Unity's action editor to set up actions
    /// for their game and the be able, when targeting desktops through Steam, to convert the game's actions
    /// to a Steam VDF file that allows using the Steam Controller API with the game.
    ///
    /// The generated VDF file is meant to allow editing by hand in order to add localization strings or
    /// apply Steam-specific settings that cannot be inferred from Unity input actions.
    /// </remarks>
    public static class SteamIGAConverter
    {
        /// <summary>
        /// Generate C# code for an <see cref="InputDevice"/> derived class that exposes the controls
        /// for the actions found in the given Steam IGA description.
        /// </summary>
        /// <param name="vdf"></param>
        /// <param name="namespaceAndClassName"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "TODO: Refactor later.")]
        public static string GenerateInputDeviceFromSteamIGA(string vdf, string namespaceAndClassName)
        {
            if (string.IsNullOrEmpty(vdf))
                throw new ArgumentNullException("vdf");
            if (string.IsNullOrEmpty(namespaceAndClassName))
                throw new ArgumentNullException("namespaceAndClassName");

            // Parse VDF.
            var parsedVdf = ParseVDF(vdf);
            var actions = (Dictionary<string, object>)((Dictionary<string, object>)parsedVdf["In Game Actions"])["actions"];

            // Determine class and namespace name.
            var namespaceName = "";
            var className = "";
            var indexOfLastDot = namespaceAndClassName.LastIndexOf('.');
            if (indexOfLastDot != -1)
            {
                namespaceName = namespaceAndClassName.Substring(0, indexOfLastDot);
                className = namespaceAndClassName.Substring(indexOfLastDot + 1);
            }
            else
            {
                className = namespaceAndClassName;
            }
            var stateStructName = className + "State";

            var builder = new StringBuilder();

            builder.Append("// THIS FILE HAS BEEN AUTO-GENERATED\n");
            builder.Append("#if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT\n");
            builder.Append("using UnityEngine;\n");
            builder.Append("using UnityEngine.InputSystem;\n");
            builder.Append("using UnityEngine.InputSystem.Controls;\n");
            builder.Append("using UnityEngine.InputSystem.Layouts;\n");
            builder.Append("using UnityEngine.InputSystem.LowLevel;\n");
            builder.Append("using UnityEngine.InputSystem.Utilities;\n");
            builder.Append("using UnityEngine.InputSystem.Steam;\n");
            builder.Append("#if UNITY_EDITOR\n");
            builder.Append("using UnityEditor;\n");
            builder.Append("#endif\n");
            builder.Append("\n");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.Append("namespace ");
                builder.Append(namespaceName);
                builder.Append("\n{\n");
            }

            // InitializeOnLoad attribute.
            builder.Append("#if UNITY_EDITOR\n");
            builder.Append("[InitializeOnLoad]\n");
            builder.Append("#endif\n");

            // Control layout attribute.
            builder.Append("[InputControlLayout(stateType = typeof(");
            builder.Append(stateStructName);
            builder.Append("))]\n");

            // Class declaration.
            builder.Append("public class ");
            builder.Append(className);
            builder.Append(" : SteamController\n");
            builder.Append("{\n");

            // Device matcher.
            builder.Append("    private static InputDeviceMatcher deviceMatcher\n");
            builder.Append("    {\n");
            builder.Append("        get { return new InputDeviceMatcher().WithInterface(\"Steam\").WithProduct(\"");
            builder.Append(className);
            builder.Append("\"); }\n");
            builder.Append("    }\n");

            // Static constructor.
            builder.Append('\n');
            builder.Append("#if UNITY_EDITOR\n");
            builder.Append("    static ");
            builder.Append(className);
            builder.Append("()\n");
            builder.Append("    {\n");
            builder.Append("        InputSystem.RegisterLayout<");
            builder.Append(className);
            builder.Append(">(matches: deviceMatcher);\n");
            builder.Append("    }\n");
            builder.Append("#endif\n");

            // RuntimeInitializeOnLoadMethod.
            // NOTE: Not relying on static ctor here. See il2cpp bug 1014293.
            builder.Append('\n');
            builder.Append("    [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]\n");
            builder.Append("    private static void RuntimeInitializeOnLoad()\n");
            builder.Append("    {\n");
            builder.Append("        InputSystem.RegisterLayout<");
            builder.Append(className);
            builder.Append(">(matches: deviceMatcher);\n");
            builder.Append("    }\n");

            // Control properties.
            builder.Append('\n');
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    var entryProperties = (Dictionary<string, object>)entry.Value;
                    var isStick = entryProperties.ContainsKey("input_mode") && (string)entryProperties["input_mode"] == "joystick_move";
                    builder.Append("    [InputControl]\n");
                    builder.Append(
                        $"    public {(isStick ? "StickControl" : "Vector2Control")} {CSharpCodeHelpers.MakeIdentifier(entry.Key)} {{ get; protected set; }}\n");
                }

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                foreach (var entry in buttons)
                {
                    builder.Append("    [InputControl]\n");
                    builder.Append(
                        $"    public ButtonControl {CSharpCodeHelpers.MakeIdentifier(entry.Key)} {{ get; protected set; }}\n");
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append("    [InputControl]\n");
                    builder.Append(
                        $"    public AxisControl {CSharpCodeHelpers.MakeIdentifier(entry.Key)} {{ get; protected set; }}\n");
                }
            }

            // FinishSetup method.
            builder.Append('\n');
            builder.Append("    protected override void FinishSetup()\n");
            builder.Append("    {\n");
            builder.Append("        base.FinishSetup();\n");
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    var entryProperties = (Dictionary<string, object>)entry.Value;
                    var isStick = entryProperties.ContainsKey("input_mode") && (string)entryProperties["input_mode"] == "joystick_move";
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)} = GetChildControl<{(isStick ? "StickControl" : "Vector2Control")}>(\"{entry.Key}\");\n");
                }

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                foreach (var entry in buttons)
                {
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)} = GetChildControl<ButtonControl>(\"{entry.Key}\");\n");
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)} = GetChildControl<AxisControl>(\"{entry.Key}\");\n");
                }
            }
            builder.Append("    }\n");

            // ResolveSteamActions method.
            builder.Append('\n');
            builder.Append("    protected override void ResolveSteamActions(ISteamControllerAPI api)\n");
            builder.Append("    {\n");
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // Set handle.
                builder.Append(
                    $"        {CSharpCodeHelpers.MakeIdentifier(setEntry.Key)}SetHandle = api.GetActionSetHandle(\"{setEntry.Key}\");\n");

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle = api.GetAnalogActionHandle(\"{entry.Key}\");\n");
                }

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                foreach (var entry in buttons)
                {
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle = api.GetDigitalActionHandle(\"{entry.Key}\");\n");
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append(
                        $"        {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle = api.GetAnalogActionHandle(\"{entry.Key}\");\n");
                }
            }
            builder.Append("    }\n");

            // Handle cache fields.
            builder.Append('\n');
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // Set handle.
                builder.Append(
                    $"    public SteamHandle<InputActionMap> {CSharpCodeHelpers.MakeIdentifier(setEntry.Key)}SetHandle {{ get; private set; }}\n");

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    builder.Append(
                        $"    public SteamHandle<InputAction> {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle {{ get; private set; }}\n");
                }

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                foreach (var entry in buttons)
                {
                    builder.Append(
                        $"    public SteamHandle<InputAction> {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle {{ get; private set; }}\n");
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append(
                        $"    public SteamHandle<InputAction> {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle {{ get; private set; }}\n");
                }
            }

            // steamActionSets property.
            builder.Append('\n');
            builder.Append("    private SteamActionSetInfo[] m_ActionSets;\n");
            builder.Append("    public override ReadOnlyArray<SteamActionSetInfo> steamActionSets\n");
            builder.Append("    {\n");
            builder.Append("        get\n");
            builder.Append("        {\n");
            builder.Append("            if (m_ActionSets == null)\n");
            builder.Append("                m_ActionSets = new[]\n");
            builder.Append("                {\n");
            foreach (var setEntry in actions)
            {
                builder.Append(string.Format(
                    "                    new SteamActionSetInfo {{ name = \"{0}\", handle = {1}SetHandle }},\n",
                    setEntry.Key,
                    CSharpCodeHelpers.MakeIdentifier(setEntry.Key)));
            }
            builder.Append("                };\n");
            builder.Append("            return new ReadOnlyArray<SteamActionSetInfo>(m_ActionSets);\n");
            builder.Append("        }\n");
            builder.Append("    }\n");

            // Update method.
            builder.Append('\n');
            builder.Append("    protected override unsafe void Update(ISteamControllerAPI api)\n");
            builder.Append("    {\n");
            builder.Append($"        {stateStructName} state;\n");
            var currentButtonBit = 0;
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    builder.Append(string.Format("        state.{0} = api.GetAnalogActionData(steamControllerHandle, {0}Handle).position;\n",
                        CSharpCodeHelpers.MakeIdentifier(entry.Key)));
                }

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                foreach (var entry in buttons)
                {
                    builder.Append(
                        $"        if (api.GetDigitalActionData(steamControllerHandle, {CSharpCodeHelpers.MakeIdentifier(entry.Key)}Handle).pressed)\n");
                    builder.Append($"            state.buttons[{currentButtonBit / 8}] |= {currentButtonBit % 8};\n");
                    ++currentButtonBit;
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append(string.Format("        state.{0} = api.GetAnalogActionData(steamControllerHandle, {0}Handle).position.x;\n",
                        CSharpCodeHelpers.MakeIdentifier(entry.Key)));
                }
            }
            builder.Append("        InputSystem.QueueStateEvent(this, state);\n");
            builder.Append("    }\n");

            builder.Append("}\n");

            if (!string.IsNullOrEmpty(namespaceName))
                builder.Append("}\n");

            // State struct.
            builder.Append("public unsafe struct ");
            builder.Append(stateStructName);
            builder.Append(" : IInputStateTypeInfo\n");
            builder.Append("{\n");
            builder.Append("    public FourCC format\n");
            builder.Append("    {\n");
            builder.Append("        get {\n");
            ////TODO: handle class names that are shorter than 4 characters
            ////TODO: uppercase characters
            builder.Append(
                $"            return new FourCC('{className[0]}', '{className[1]}', '{className[2]}', '{className[3]}');\n");
            builder.Append("        }\n");
            builder.Append("    }\n");
            builder.Append("\n");
            var totalButtonCount = 0;
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // Buttons.
                var buttons = (Dictionary<string, object>)setEntryProperties["Button"];
                var buttonCount = buttons.Count;
                if (buttonCount > 0)
                {
                    foreach (var entry in buttons)
                    {
                        builder.Append(
                            $"    [InputControl(name = \"{entry.Key}\", layout = \"Button\", bit = {totalButtonCount})]\n");
                        ++totalButtonCount;
                    }
                }
            }
            if (totalButtonCount > 0)
            {
                var byteCount = (totalButtonCount + 7) / 8;
                builder.Append("    public fixed byte buttons[");
                builder.Append(byteCount.ToString());
                builder.Append("];\n");
            }
            foreach (var setEntry in actions)
            {
                var setEntryProperties = (Dictionary<string, object>)setEntry.Value;

                // StickPadGyros.
                var stickPadGyros = (Dictionary<string, object>)setEntryProperties["StickPadGyro"];
                foreach (var entry in stickPadGyros)
                {
                    var entryProperties = (Dictionary<string, object>)entry.Value;
                    var isStick = entryProperties.ContainsKey("input_mode") && (string)entryProperties["input_mode"] == "joystick_move";

                    builder.Append(
                        $"    [InputControl(name = \"{entry.Key}\", layout = \"{(isStick ? "Stick" : "Vector2")}\")]\n");
                    builder.Append($"    public Vector2 {CSharpCodeHelpers.MakeIdentifier(entry.Key)};\n");
                }

                // AnalogTriggers.
                var analogTriggers = (Dictionary<string, object>)setEntryProperties["AnalogTrigger"];
                foreach (var entry in analogTriggers)
                {
                    builder.Append($"    [InputControl(name = \"{entry.Key}\", layout = \"Axis\")]\n");
                    builder.Append($"    public float {CSharpCodeHelpers.MakeIdentifier(entry.Key)};\n");
                }
            }
            builder.Append("}\n");

            builder.Append("#endif\n");

            return builder.ToString();
        }

        /// <summary>
        /// Convert an .inputactions asset to Steam VDF format.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="locale"></param>
        /// <returns>A string in Steam VDF format describing "In Game Actions" corresponding to the actions in
        /// <paramref name="asset"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is null.</exception>
        public static string ConvertInputActionsToSteamIGA(InputActionAsset asset, string locale = "english")
        {
            if (asset == null)
                throw new ArgumentNullException("asset");
            return ConvertInputActionsToSteamIGA(asset.actionMaps, locale: locale);
        }

        public static string ConvertInputActionsToSteamIGA(IEnumerable<InputActionMap> actionMaps, string locale = "english")
        {
            if (actionMaps == null)
                throw new ArgumentNullException("actionMaps");

            var localizationStrings = new Dictionary<string, string>();

            var builder = new StringBuilder();
            builder.Append("\"In Game Actions\"\n");
            builder.Append("{\n");

            // Add actions.
            builder.Append("\t\"actions\"\n");
            builder.Append("\t{\n");

            // Add each action map.
            foreach (var actionMap in actionMaps)
            {
                var actionMapName = actionMap.name;
                var actionMapIdentifier = CSharpCodeHelpers.MakeIdentifier(actionMapName);

                builder.Append("\t\t\"");
                builder.Append(actionMapName);
                builder.Append("\"\n");
                builder.Append("\t\t{\n");

                // Title.
                builder.Append("\t\t\t\"title\"\t\"#Set_");
                builder.Append(actionMapIdentifier);
                builder.Append("\"\n");
                localizationStrings["Set_" + actionMapIdentifier] = actionMapName;

                // StickPadGyro actions.
                builder.Append("\t\t\t\"StickPadGyro\"\n");
                builder.Append("\t\t\t{\n");
                foreach (var action in actionMap.actions.Where(x => GetSteamControllerInputType(x) == "StickPadGyro"))
                    ConvertInputActionToVDF(action, builder, localizationStrings);
                builder.Append("\t\t\t}\n");

                // AnalogTrigger actions.
                builder.Append("\t\t\t\"AnalogTrigger\"\n");
                builder.Append("\t\t\t{\n");
                foreach (var action in actionMap.actions.Where(x => GetSteamControllerInputType(x) == "AnalogTrigger"))
                    ConvertInputActionToVDF(action, builder, localizationStrings);
                builder.Append("\t\t\t}\n");

                // Button actions.
                builder.Append("\t\t\t\"Button\"\n");
                builder.Append("\t\t\t{\n");
                foreach (var action in actionMap.actions.Where(x => GetSteamControllerInputType(x) == "Button"))
                    ConvertInputActionToVDF(action, builder, localizationStrings);
                builder.Append("\t\t\t}\n");

                builder.Append("\t\t}\n");
            }

            builder.Append("\t}\n");

            // Add localizations.
            builder.Append("\t\"localization\"\n");
            builder.Append("\t{\n");
            builder.Append("\t\t\"");
            builder.Append(locale);
            builder.Append("\"\n");
            builder.Append("\t\t{\n");
            foreach (var entry in localizationStrings)
            {
                builder.Append("\t\t\t\"");
                builder.Append(entry.Key);
                builder.Append("\"\t\"");
                builder.Append(entry.Value);
                builder.Append("\"\n");
            }
            builder.Append("\t\t}\n");
            builder.Append("\t}\n");

            builder.Append("}\n");

            return builder.ToString();
        }

        private static void ConvertInputActionToVDF(InputAction action, StringBuilder builder, Dictionary<string, string> localizationStrings)
        {
            builder.Append("\t\t\t\t\"");
            builder.Append(action.name);

            var mapIdentifier = CSharpCodeHelpers.MakeIdentifier(action.actionMap.name);
            var actionIdentifier = CSharpCodeHelpers.MakeIdentifier(action.name);
            var titleId = "Action_" + mapIdentifier + "_" + actionIdentifier;
            localizationStrings[titleId] = action.name;

            // StickPadGyros are objects. Everything else is just strings.
            var inputType = GetSteamControllerInputType(action);
            if (inputType == "StickPadGyro")
            {
                builder.Append("\"\n");
                builder.Append("\t\t\t\t{\n");

                // Title.
                builder.Append("\t\t\t\t\t\"title\"\t\"#");
                builder.Append(titleId);
                builder.Append("\"\n");

                // Decide on "input_mode". Assume "absolute_mouse" by default and take
                // anything built on StickControl as "joystick_move".
                var inputMode = "absolute_mouse";
                var controlType = EditorInputControlLayoutCache.TryGetLayout(action.expectedControlType).type;
                if (typeof(StickControl).IsAssignableFrom(controlType))
                    inputMode = "joystick_move";
                builder.Append("\t\t\t\t\t\"input_mode\"\t\"");
                builder.Append(inputMode);
                builder.Append("\"\n");

                builder.Append("\t\t\t\t}\n");
            }
            else
            {
                builder.Append("\"\t\"");
                builder.Append(titleId);
                builder.Append("\"\n");
            }
        }

        public static string GetSteamControllerInputType(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            // Make sure we have an expected control layout.
            var expectedControlLayout = action.expectedControlType;
            if (string.IsNullOrEmpty(expectedControlLayout))
                throw new ArgumentException($"Cannot determine Steam input type for action '{action}' that has no associated expected control layout",
                    nameof(action));

            // Try to fetch the layout.
            var layout = EditorInputControlLayoutCache.TryGetLayout(expectedControlLayout);
            if (layout == null)
                throw new ArgumentException($"Cannot determine Steam input type for action '{action}'; cannot find layout '{expectedControlLayout}'", nameof(action));

            // Map our supported control types.
            var controlType = layout.type;
            if (typeof(ButtonControl).IsAssignableFrom(controlType))
                return "Button";
            if (typeof(InputControl<float>).IsAssignableFrom(controlType))
                return "AnalogTrigger";
            if (typeof(Vector2Control).IsAssignableFrom(controlType))
                return "StickPadGyro";

            // Everything else throws.
            throw new ArgumentException($"Cannot determine Steam input type for action '{action}'; layout '{expectedControlLayout}' with control type '{ controlType.Name}' has no known representation in the Steam controller API", nameof(action));
        }

        public static Dictionary<string, object> ParseVDF(string vdf)
        {
            var parser = new VDFParser(vdf);
            return parser.Parse();
        }

        private struct VDFParser
        {
            public string vdf;
            public int length;
            public int position;

            public VDFParser(string vdf)
            {
                this.vdf = vdf;
                length = vdf.Length;
                position = 0;
            }

            public Dictionary<string, object> Parse()
            {
                var result = new Dictionary<string, object>();
                ParseKeyValuePair(result);
                SkipWhitespace();
                if (position < length)
                    throw new InvalidOperationException($"Parse error at {position} in '{vdf}'; not expecting any more input");
                return result;
            }

            private bool ParseKeyValuePair(Dictionary<string, object> result)
            {
                var key = ParseString();
                if (key.isEmpty)
                    return false;

                SkipWhitespace();
                if (position == length)
                    throw new InvalidOperationException($"Expecting value or object at position {position} in '{vdf}'");

                var nextChar = vdf[position];
                if (nextChar == '"')
                {
                    var value = ParseString();
                    result[key.ToString()] = value.ToString();
                }
                else if (nextChar == '{')
                {
                    var value = ParseObject();
                    result[key.ToString()] = value;
                }
                else
                {
                    throw new InvalidOperationException($"Expecting value or object at position {position} in '{vdf}'");
                }

                return true;
            }

            private Substring ParseString()
            {
                SkipWhitespace();
                if (position == length || vdf[position] != '"')
                    return new Substring();

                ++position;
                var startPos = position;
                while (position < length && vdf[position] != '"')
                    ++position;
                var endPos = position;

                if (position < length)
                    ++position;

                return new Substring(vdf, startPos, endPos - startPos);
            }

            private Dictionary<string, object> ParseObject()
            {
                SkipWhitespace();
                if (position == length || vdf[position] != '{')
                    return null;

                var result = new Dictionary<string, object>();

                ++position;
                while (position < length)
                {
                    if (!ParseKeyValuePair(result))
                        break;
                }

                SkipWhitespace();
                if (position == length || vdf[position] != '}')
                    throw new InvalidOperationException($"Expecting '}}' at position {position} in '{vdf}'");
                ++position;

                return result;
            }

            private void SkipWhitespace()
            {
                while (position < length && char.IsWhiteSpace(vdf[position]))
                    ++position;
            }
        }

        [MenuItem("Assets/Steam/Export to Steam In-Game Actions File...", true)]
        private static bool IsExportContextMenuItemEnabled()
        {
            return Selection.activeObject is InputActionAsset;
        }

        [MenuItem("Assets/Steam/Export to Steam In-Game Actions File...")]
        private static void ExportContextMenuItem()
        {
            var selectedAsset = (InputActionAsset)Selection.activeObject;

            // Determine default .vdf file name.
            var defaultVDFName = "";
            var directory = "";
            var assetPath = AssetDatabase.GetAssetPath(selectedAsset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                defaultVDFName = Path.GetFileNameWithoutExtension(assetPath) + ".vdf";
                directory = Path.GetDirectoryName(assetPath);
            }

            // Ask for save location.
            var fileName = EditorUtility.SaveFilePanel("Export Steam In-Game Actions File", directory, defaultVDFName, "vdf");
            if (!string.IsNullOrEmpty(fileName))
            {
                var text = ConvertInputActionsToSteamIGA(selectedAsset);
                File.WriteAllText(fileName, text);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Steam/Generate Unity Input Device...", true)]
        private static bool IsGenerateContextMenuItemEnabled()
        {
            // VDF files have no associated importer and so come in as DefaultAssets.
            if (!(Selection.activeObject is DefaultAsset))
                return false;

            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(assetPath) && Path.GetExtension(assetPath) == ".vdf")
                return true;

            return false;
        }

        ////TODO: support setting class and namespace name
        [MenuItem("Assets/Steam/Generate Unity Input Device...")]
        private static void GenerateContextMenuItem()
        {
            var selectedAsset = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(selectedAsset);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Cannot determine source asset path");
                return;
            }

            var defaultClassName = Path.GetFileNameWithoutExtension(assetPath);
            var defaultFileName = defaultClassName + ".cs";
            var defaultDirectory = Path.GetDirectoryName(assetPath);

            // Ask for save location.
            var fileName = EditorUtility.SaveFilePanel("Generate C# Input Device Class", defaultDirectory, defaultFileName, "cs");
            if (string.IsNullOrEmpty(fileName))
                return;

            // Load VDF file text.
            var vdf = File.ReadAllText(assetPath);

            // Generate and write output.
            var className = Path.GetFileNameWithoutExtension(fileName);
            var text = GenerateInputDeviceFromSteamIGA(vdf, className);
            File.WriteAllText(fileName, text);
            AssetDatabase.Refresh();
        }
    }
}

#endif // UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
