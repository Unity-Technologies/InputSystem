#if UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.Utilities;

////TODO: also need to build a layout based on SteamController that has controls representing the current set of actions
////      (might need this in the runtime)

////TODO: localization support (allow loading existing VDF file and preserving localization strings)

////TODO: allow having actions that are ignored by Steam VDF export

namespace UnityEngine.Experimental.Input.Plugins.Steam.Editor
{
    /// <summary>
    /// Converts input actions to and from Steam .VDF format.
    /// </summary>
    /// <remarks>
    /// The idea behind this converter is to enable users to use Unity's action editor to set up actions
    /// for their game and the be able, when targeting desktops through Steam, to convert the game's actions
    /// to a Steam VDF file that allows using the Steam Controller API with the game.
    ///
    /// The generated VDF file is meant to allow editing by hand in order to add localization strings or
    /// apply Steam-specific settings that cannot be inferred from Unity input actions.
    /// </remarks>
    public static class SteamVDFConverter
    {
        /// <summary>
        /// Convert an .inputactions asset to Steam VDF format.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="locale"></param>
        /// <returns>A string in Steam VDF format describing "In Game Actions" corresponding to the actions in
        /// <paramref name="asset"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is null.</exception>
        public static string ConvertInputActionsToVDF(InputActionAsset asset, string locale = "english")
        {
            if (asset == null)
                throw new ArgumentNullException("asset");
            return ConvertInputActionsToVDF(asset.actionMaps, locale: locale);
        }

        public static string ConvertInputActionsToVDF(IEnumerable<InputActionMap> actionMaps, string locale = "english")
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
                var controlType = EditorInputControlLayoutCache.TryGetLayout(action.expectedControlLayout).type;
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

        public static InputActionMap[] ConvertInputActionsFromVDF(string vdf)
        {
            throw new NotImplementedException();
        }

        public static string GetSteamControllerInputType(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            // Make sure we have an expected control layout.
            var expectedControlLayout = action.expectedControlLayout;
            if (string.IsNullOrEmpty(expectedControlLayout))
                throw new Exception(string.Format(
                    "Cannot determine Steam input type for action '{0}' that has no associated expected control layout",
                    action));

            // Try to fetch the layout.
            var layout = EditorInputControlLayoutCache.TryGetLayout(expectedControlLayout);
            if (layout == null)
                throw new Exception(string.Format(
                    "Cannot determine Steam input type for action '{0}'; cannot find layout '{1}'", action,
                    expectedControlLayout));

            // Map our supported control types.
            var controlType = layout.type;
            if (typeof(ButtonControl).IsAssignableFrom(controlType))
                return "Button";
            if (typeof(InputControl<float>).IsAssignableFrom(controlType))
                return "AnalogTrigger";
            if (typeof(Vector2Control).IsAssignableFrom(controlType))
                return "StickPadGyro";

            // Everything else throws.
            throw new Exception(string.Format(
                "Cannot determine Steam input type for action '{0}'; layout '{1}' with control type '{2}' has no known representation in the Steam controller API",
                action, expectedControlLayout, controlType.Name));
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
                    throw new Exception(string.Format("Parse error at {0} in '{1}'; not expecting any more input", position, vdf));
                return result;
            }

            private bool ParseKeyValuePair(Dictionary<string, object> result)
            {
                var key = ParseString();
                if (key.isEmpty)
                    return false;

                SkipWhitespace();
                if (position == length)
                    throw new Exception(string.Format("Expecting value or object at position {0} in '{1}'",
                        position, vdf));

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
                    throw new Exception(string.Format("Expecting value or object at position {0} in '{1}'",
                        position, vdf));
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
                    throw new Exception(string.Format("Expecting '}}' at position {0} in '{1}'", position, vdf));
                ++position;

                return result;
            }

            private void SkipWhitespace()
            {
                while (position < length && char.IsWhiteSpace(vdf[position]))
                    ++position;
            }
        }

        [MenuItem("Assets/Export to Steam In-Game Actions File...", true)]
        private static bool IsAssetContextMenuItemEnabled()
        {
            return Selection.activeObject is InputActionAsset;
        }

        [MenuItem("Assets/Export to Steam In-Game Actions File...")]
        private static void AssetContextMenuItem()
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
                var text = ConvertInputActionsToVDF(selectedAsset);
                File.WriteAllText(fileName, text);
            }
        }
    }
}

#endif // UNITY_EDITOR && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
