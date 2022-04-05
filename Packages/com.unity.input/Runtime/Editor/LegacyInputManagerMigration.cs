#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

////TODO: Add a parallel implementation that migrates from Rewired instead of InputManager

////TODO: When we convert the InputManager's data, we should put a little, dismissable notification in the settings window that this is what we did

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Converts the input setup found in InputManager.asset into InputSettings including a global InputActionAsset.
    /// </summary>
    internal class LegacyInputManagerMigration
    {
        [MenuItem("Tests/Convert Legacy Input Stuff")]
        public static void ConvertLegacyInputStuff()
        {
            InputSystem.settings = MigrateLegacyInputManager();
        }

        /// <summary>
        /// Loads the <c>InputManager</c> object at <c>ProjectSettings/InputManager.asset</c> and
        /// creates and <see cref="InputSettings"/> object from it complete with a newly created
        /// <see cref="InputActionAsset"/>. Both of these newly created objects will be stored
        /// in <c>InputManager.asset</c> as well.
        /// </summary>
        /// <returns></returns>
        public static InputSettings MigrateLegacyInputManager()
        {
            var actions = ConvertInputAxisSettings();
            var settings = ScriptableObject.CreateInstance<InputSettings>();
            settings.actions = actions;

            //var legacyManager = AssetDatabase.LoadMainAssetAtPath(InputSettings.kProjectSettings);
            AssetDatabase.AddObjectToAsset(actions, InputSettings.kProjectSettings);
            AssetDatabase.AddObjectToAsset(settings, InputSettings.kProjectSettings);
            //ADB doesn't like this; do we need it?
            //AssetDatabase.SetMainObject(legacyManager, InputSettings.kProjectSettings);
            AssetDatabase.SaveAssets();

            return settings;
        }

        public static InputActionAsset ConvertInputAxisSettings()
        {
            // Load.
            var json = LegacyInputManagerToJson();
            var legacyManager = JsonUtility.FromJson<InputManager>(json);

            // Convert.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Player");

            if (legacyManager.axes != null)
            {
                var axes = new Dictionary<string, List<InputAxis>>();

                // First collect all the axes. Multiple InputAxis settings may refer to
                // the same axis to give it multiple different configuration.
                foreach (var axis in legacyManager.axes)
                {
                    if (string.IsNullOrEmpty(axis.name))
                        continue;
                    if (!axes.ContainsKey(axis.name))
                        axes[axis.name] = new List<InputAxis>();
                    axes[axis.name].Add(axis);
                }

                foreach (var axisList in axes.Values)
                {
                    // An InputAxis is always a float but specific inputs may feed into as buttons.
                    // If those are present, we make the action a button rather than a just a float value.
                    var isButton = axisList.Any(a => a.type == AxisType.Button);

                    // Add the action.
                    var action = map.AddAction(axisList[0].name, type: isButton ? InputActionType.Button : InputActionType.Value,
                        expectedControlLayout: isButton ? "Button" : "Axis");

                    // Add bindings.
                    foreach (var axis in axisList)
                    {
                        if (!string.IsNullOrEmpty(axis.positiveButton) ||
                            !string.IsNullOrEmpty(axis.negativeButton) ||
                            !string.IsNullOrEmpty(axis.altPositiveButton) ||
                            !string.IsNullOrEmpty(axis.altNegativeButton))
                        {
                            if ((!string.IsNullOrEmpty(axis.positiveButton) && !string.IsNullOrEmpty(axis.negativeButton)) ||
                                (!string.IsNullOrEmpty(axis.altPositiveButton) && !string.IsNullOrEmpty(axis.altNegativeButton)))
                            {
                                // It's an axis. Add an AxisComposite.

                                ////TODO: gravity, snap
                                var composite = action.AddCompositeBinding("1DAxis");

                                if (!string.IsNullOrEmpty(axis.positiveButton))
                                    BindIfPathIsNotEmpty(axis.positiveButton,
                                        p => composite.With("Positive", p));
                                if (!string.IsNullOrEmpty(axis.negativeButton))
                                    BindIfPathIsNotEmpty(axis.negativeButton,
                                        p => composite.With("Negative", p));
                                if (!string.IsNullOrEmpty(axis.altPositiveButton))
                                    BindIfPathIsNotEmpty(axis.altPositiveButton,
                                        p => composite.With("Positive", p));
                                if (!string.IsNullOrEmpty(axis.altNegativeButton))
                                    BindIfPathIsNotEmpty(axis.altNegativeButton,
                                        p => composite.With("Negative", p));
                            }
                            else
                            {
                                // It's a button.

                                if (!string.IsNullOrEmpty(axis.positiveButton))
                                    BindIfPathIsNotEmpty(axis.positiveButton, p => action.AddBinding(p));
                                if (!string.IsNullOrEmpty(axis.altPositiveButton))
                                    BindIfPathIsNotEmpty(axis.altPositiveButton, p => action.AddBinding(p));

                                ////REVIEW: Is binding *only* negative buttons a thing?
                            }
                        }
                    }
                }
            }

            return asset;
        }

        private static void BindIfPathIsNotEmpty(string legacyControlName, Action<string> bind)
        {
            var path = LegacyControlNameToBindingPath(legacyControlName);
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var p in path.Split(kPathSeparator))
                bind(p);
        }

        private static string LegacyControlNameToBindingPath(string legacyControlName)
        {
            if (s_LegacyControlNamesToBindingPath.TryGetValue(legacyControlName, out var value))
                return value;
            return string.Empty;
        }

        // JsonUtility won't let us turn internal UnityEngine.Objects into JSON. This uses
        // SerializedProperty to work around that.
        private static string LegacyInputManagerToJson()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(InputSettings.kProjectSettings);
            var legacyInputManager = objects.First(o => o.name == "InputManager");
            var obj = new SerializedObject(legacyInputManager);

            var buffer = new StringBuilder();
            buffer.Append("{\n");
            buffer.Append("     \"axes\" : [\n");

            var axisArray = obj.FindProperty("m_Axes");
            for (var i = 0; i < axisArray.arraySize; ++i)
            {
                var element = axisArray.GetArrayElementAtIndex(i);
                if (i != 0)
                    buffer.Append(",\n");
                buffer.Append("        ");
                element.CopyToJson(buffer);
            }

            buffer.Append("     ]\n");
            buffer.Append("}\n");

            return buffer.ToString();
        }

        [Serializable]
        internal enum AxisType
        {
            Button,
            Mouse,
            Joystick,
        }

        [Serializable]
        internal struct InputAxis
        {
            public string name
            {
                get => m_Name;
                set => m_Name = value;
            }

            public AxisType type;
            public string descriptiveName;
            public string descriptiveNegativeName;
            public string negativeButton;
            public string positiveButton;
            public string altNegativeButton;
            public string altPositiveButton;
            public float gravity;
            public float dead;
            public float sensitivity;
            public int axis;

            [SerializeField] private string m_Name;
        }

        [Serializable]
        internal class InputManager
        {
            public InputAxis[] axes;

            public bool usePhysicalKeys
            {
                get => m_UsePhysicalKeys;
                set => m_UsePhysicalKeys = value;
            }

            [SerializeField] private bool m_UsePhysicalKeys;
        }

        private const char kPathSeparator = '\u0000';

        private static Dictionary<string, string> s_LegacyControlNamesToBindingPath = new Dictionary<string, string>()
        {
            { "backspace", "<Keyboard>/backspace" }, // SDLK_BACKSPACE
            { "tab", "<Keyboard>/tab" }, // SDLK_TAB
            { "clear", "" }, // SDLK_CLEAR
            { "return", "<Keyboard>/enter" }, // SDLK_RETURN
            { "pause", "" }, // SDLK_PAUSE
            { "escape", "<Keyboard>/escape" }, // SDLK_ESCAPE
            { "space", "<Keyboard>/space" }, // SDLK_SPACE
            { "!", "" }, // SDLK_EXCLAIM
            { "\"", "" }, // SDLK_QUOTEDBL
            { "#", "" }, // SDLK_HASH
            { "$", "" }, // SDLK_DOLLAR
            { "%", "" }, // SDLK_PERCENT
            { "&", "" }, // SDLK_AMPERSAND
            { "'", "" }, // SDLK_QUOTE
            { "(", "" }, // SDLK_LEFTPAREN
            { ")", "" }, // SDLK_RIGHTPAREN
            { "*", "" }, // SDLK_ASTERISK
            { "+", "" }, // SDLK_PLUS
            { ",", "" }, // SDLK_COMMA
            { "-", "" }, // SDLK_MINUS
            { ".", "" }, // SDLK_PERIOD
            { "/", "" }, // SDLK_SLASH
            { "0", "<Keyboard>/0" }, // SDLK_0
            { "1", "<Keyboard>/1" }, // SDLK_1
            { "2", "<Keyboard>/2" }, // SDLK_2
            { "3", "<Keyboard>/3" }, // SDLK_3
            { "4", "<Keyboard>/4" }, // SDLK_4
            { "5", "<Keyboard>/5" }, // SDLK_5
            { "6", "<Keyboard>/6" }, // SDLK_6
            { "7", "<Keyboard>/7" }, // SDLK_7
            { "8", "<Keyboard>/8" }, // SDLK_8
            { "9", "<Keyboard>/9" }, // SDLK_9
            { ":", "" }, // SDLK_COLON
            { ";", "" }, // SDLK_SEMICOLON
            { "<", "" }, // SDLK_LESS
            { "=", "" }, // SDLK_EQUALS
            { ">", "" }, // SDLK_GREATER
            { "?", "" }, // SDLK_QUESTION
            { "@", "" }, // SDLK_AT
            { "[", "" }, // SDLK_LEFTBRACKET
            { "\\", "" }, // SDLK_BACKSLASH
            { "]", "" }, // SDLK_RIGHTBRACKET
            { "^", "" }, // SDLK_CARET
            { "_", "" }, // SDLK_UNDERSCORE
            { "`", "" }, // SDLK_BACKQUOTE
            { "a", "<Keyboard>/a" }, // SDLK_a
            { "b", "<Keyboard>/b" }, // SDLK_b
            { "c", "<Keyboard>/c" }, // SDLK_c
            { "d", "<Keyboard>/d" }, // S
            { "e", "<Keyboard>/e" }, // SDLK_e
            { "f", "<Keyboard>/f" }, // SDLK_f
            { "g", "<Keyboard>/g" }, // SDLK_g
            { "h", "<Keyboard>/h" }, // SDLK_h
            { "i", "<Keyboard>/i" }, // SDLK_i
            { "j", "<Keyboard>/j" }, // SDLK_j
            { "k", "<Keyboard>/k" }, // SDLK_k
            { "l", "<Keyboard>/l" }, // SDLK_l
            { "m", "<Keyboard>/m" }, // SDLK_m
            { "n", "<Keyboard>/n" }, // SDLK_n
            { "o", "<Keyboard>/o" }, // SDLK_o
            { "p", "<Keyboard>/p" }, // SDLK_p
            { "q", "<Keyboard>/q" }, // SDLK_q
            { "r", "<Keyboard>/r" }, // SDLK_r
            { "s", "<Keyboard>/s" }, // SDLK_s
            { "t", "<Keyboard>/t" }, // SDLK_t
            { "u", "<Keyboard>/u" }, // SDLK_u
            { "v", "<Keyboard>/v" }, // SDLK_v
            { "w", "<Keyboard>/w" }, // SDLK_w
            { "x", "<Keyboard>/x" }, // SDLK_x
            { "y", "<Keyboard>/y" }, // SDLK_y
            { "z", "<Keyboard>/z" }, // SDLK_z
            { "{", "" }, // SDLK_LEFTCURLYBRACKET
            { "|", "" }, // SDLK_PIPE
            { "}", "" }, // SDLK_RIGHTCURLYBRACKET
            { "~", "" }, // SDLK_TILDE
            { "delete", "" }, // SDLK_DELETE
            // -- SDLK_WORLD_x not supported --
            { "[0]", "<Keyboard>/numpad0" }, // SDLK_KP0
            { "[1]", "<Keyboard>/numpad1" }, // SDLK_KP1
            { "[2]", "<Keyboard>/numpad2" }, // SDLK_KP2
            { "[3]", "<Keyboard>/numpad3" }, // SDLK_KP3
            { "[4]", "<Keyboard>/numpad4" }, // SDLK_KP4
            { "[5]", "<Keyboard>/numpad5" }, // SDLK_KP5
            { "[6]", "<Keyboard>/numpad6" }, // SDLK_KP6
            { "[7]", "<Keyboard>/numpad7" }, // SDLK_KP7
            { "[8]", "<Keyboard>/numpad8" }, // SDLK_KP8
            { "[9]", "<Keyboard>/numpad9" }, // SDLK_KP9
            { "[.]", "" }, // SDLK_KP_PERIOD
            { "[/]", "" }, // SDLK_KP_DIVIDE
            { "[*]", "" }, // SDLK_KP_MULTIPLY
            { "[-]", "" }, // SDLK_KP_MINUS
            { "[+]", "" }, // SDLK_KP_PLUS
            { "enter", "" }, // SDLK_KP_ENTER
            { "equals", "" }, // SDLK_KP_EQUALS
            { "up", "" }, // SDLK_UP
            { "down", "" }, // SDLK_DOWN
            { "right", "" }, // SDLK_RIGHT
            { "left", "" }, // SDLK_LEFT
            { "insert", "" }, // SDLK_INSERT
            { "home", "" }, // SDLK_HOME
            { "end", "" }, // SDLK_END
            { "page up", "" }, // SDLKP_PAGEUP
            { "page down", "" }, // SDLK_PAGEDOWN
            { "f1", "<Keyboard>/f1" }, // SDLK_F1
            { "f2", "<Keyboard>/f2" }, // SDLK_F2
            { "f3", "<Keyboard>/f3" }, // SDLK_F3
            { "f4", "<Keyboard>/f4" }, // SDLK_F4
            { "f5", "<Keyboard>/f5" }, // SDLK_F5
            { "f6", "<Keyboard>/f6" }, // SDLK_F6
            { "f7", "<Keyboard>/f7" }, // SDLK_F7
            { "f8", "<Keyboard>/f8" }, // SDLK_F8
            { "f9", "<Keyboard>/f9" }, // SDLK_F9
            { "f10", "<Keyboard>/f10" }, // SDLK_F10
            { "f11", "<Keyboard>/f11" }, // SDLK_F11
            { "f12", "<Keyboard>/f12" }, // SDLK_F12
            { "f13", "" }, // Not supported.
            { "f14", "" }, // Not supported.
            { "f15", "" }, // Not supported.
            { "numlock", "" }, // SDLK_NUMLOCK
            { "caps lock", "" }, // SDLK_CAPSLOCK
            { "scroll lock", "" }, // SDLK_SCROLLOCK
            { "right shift", "" }, // SDLK_RSHIFT
            { "left shift", "" }, // SDLK_LSHIFT
            { "right ctrl", "" }, // SDLK_RCTRL
            { "left ctrl", "" }, // SDLK_LCTRL
            { "right alt", "" }, // SDLK_RALT
            { "left alt", "" }, // SDLK_LALT
            { "right cmd", "" }, // SDLK_RMETA
            { "left cmd", "" }, // SDLK_LMETA
            { "left super", "" }, // SDLK_LSUPER
            { "right super", "" }, // SDLK_RSUPER
            { "alt gr", "" }, // SDLK_MODE
            { "compose", "" }, // SDLK_COMPOSE
            { "help", "" }, // SDLK_HELP
            { "print screen", "" }, // SDLK_PRINT
            { "sys req", "" }, // SDLK_SYSREQ
            { "break", "" }, // SDLK_BREAK
            { "menu", "" }, // SDLK_MENU
            { "power", "" }, // SDLK_POWER
            { "euro", "" }, // SDLK_EURO
            { "undo", "" }, // SDLK_UNDO
            { "mouse 0", "<Mouse>/leftButton" },
            { "mouse 1", "<Mouse>/rightButton" },
            { "mouse 2", "<Mouse>/middleButton" },
            { "mouse 3", "<Mouse>/backButton" },
            { "mouse 4", "<Mouse>/forwardButton" },
            { "mouse 5", "" }, // Not supported.
            { "mouse 6", "" }, // Not supported.
            // Our HID fallback turns GenericDesktop.Button1 into "trigger" and then goes "buttonN" from there.
            // For the gamepad mappings, we use the Xbox layout on Windows as reference.
            { "joystick button 0", "<Joystick>/trigger" + kPathSeparator + "<Gamepad>/buttonSouth" },
            { "joystick button 1", "<Joystick>/button2" + kPathSeparator + "<Gamepad>/buttonEast" },
            { "joystick button 2", "<Joystick>/button3" + kPathSeparator + "<Gamepad>/buttonWest" },
            { "joystick button 3", "<Joystick>/button4" + kPathSeparator + "<Gamepad>/buttonNorth" },
            { "joystick button 4", "<Joystick>/button5" + kPathSeparator + "<Gamepad>/leftShoulder" },
            { "joystick button 5", "<Joystick>/button6" + kPathSeparator + "<Gamepad>/rightShoulder" },
            { "joystick button 6", "<Joystick>/button7" + kPathSeparator + "<Gamepad>/select" },
            { "joystick button 7", "<Joystick>/button8" + kPathSeparator + "<Gamepad>/start" },
            { "joystick button 8", "<Joystick>/button9" + kPathSeparator + "<Gamepad>/leftStickPress" },
            { "joystick button 9", "<Joystick>/button10" + kPathSeparator + "<Gamepad>/rightStickPress" },
            { "joystick button 10", "<Joystick>/button11" },
            { "joystick button 11", "<Joystick>/button12" },
            { "joystick button 12", "<Joystick>/button13" },
            { "joystick button 13", "<Joystick>/button14" },
            { "joystick button 14", "<Joystick>/button15" },
            { "joystick button 15", "<Joystick>/button16" },
            { "joystick button 16", "<Joystick>/button17" },
            { "joystick button 17", "<Joystick>/button18" },
            { "joystick button 18", "<Joystick>/button19" },
            { "joystick button 19", "<Joystick>/button20" },
            { "joystick 1 button 0", "<Joystick>{Joystick1}/trigger" + kPathSeparator + "<Gamepad>{Joystick1}/buttonSouth" },
            { "joystick 1 button 1", "<Joystick>{Joystick1}/button2" + kPathSeparator + "<Gamepad>{Joystick1}/buttonEast" },
            { "joystick 1 button 2", "<Joystick>{Joystick1}/button3" + kPathSeparator + "<Gamepad>{Joystick1}/buttonWest" },
            { "joystick 1 button 3", "<Joystick>{Joystick1}/button4" + kPathSeparator + "<Gamepad>{Joystick1}/buttonNorth" },
            { "joystick 1 button 4", "<Joystick>{Joystick1}/button5" + kPathSeparator + "<Gamepad>{Joystick1}/leftShoulder" },
            { "joystick 1 button 5", "<Joystick>{Joystick1}/button6" + kPathSeparator + "<Gamepad>{Joystick1}/rightShoulder" },
            { "joystick 1 button 6", "<Joystick>{Joystick1}/button7" + kPathSeparator + "<Gamepad>{Joystick1}/select" },
            { "joystick 1 button 7", "<Joystick>{Joystick1}/button8" + kPathSeparator + "<Gamepad>{Joystick1}/start" },
            { "joystick 1 button 8", "<Joystick>{Joystick1}/button9" + kPathSeparator + "<Gamepad>{Joystick1}/leftStickPress" },
            { "joystick 1 button 9", "<Joystick>{Joystick1}/button10" + kPathSeparator + "<Gamepad>{Joystick1}/rightStickPress" },
            { "joystick 1 button 10", "<Joystick>{Joystick1}/button11" },
            { "joystick 1 button 11", "<Joystick>{Joystick1}/button12" },
            { "joystick 1 button 12", "<Joystick>{Joystick1}/button13" },
            { "joystick 1 button 13", "<Joystick>{Joystick1}/button14" },
            { "joystick 1 button 14", "<Joystick>{Joystick1}/button15" },
            { "joystick 1 button 15", "<Joystick>{Joystick1}/button16" },
            { "joystick 1 button 16", "<Joystick>{Joystick1}/button17" },
            { "joystick 1 button 17", "<Joystick>{Joystick1}/button18" },
            { "joystick 1 button 18", "<Joystick>{Joystick1}/button19" },
            { "joystick 1 button 19", "<Joystick>{Joystick1}/button20" },
            { "joystick 2 button 0", "<Joystick>{Joystick2}/trigger" + kPathSeparator + "<Gamepad>{Joystick2}/buttonSouth" },
            { "joystick 2 button 1", "<Joystick>{Joystick2}/button2" + kPathSeparator + "<Gamepad>{Joystick2}/buttonEast" },
            { "joystick 2 button 2", "<Joystick>{Joystick2}/button3" + kPathSeparator + "<Gamepad>{Joystick2}/buttonWest" },
            { "joystick 2 button 3", "<Joystick>{Joystick2}/button4" + kPathSeparator + "<Gamepad>{Joystick2}/buttonNorth" },
            { "joystick 2 button 4", "<Joystick>{Joystick2}/button5" + kPathSeparator + "<Gamepad>{Joystick2}/leftShoulder" },
            { "joystick 2 button 5", "<Joystick>{Joystick2}/button6" + kPathSeparator + "<Gamepad>{Joystick2}/rightShoulder" },
            { "joystick 2 button 6", "<Joystick>{Joystick2}/button7" + kPathSeparator + "<Gamepad>{Joystick2}/select" },
            { "joystick 2 button 7", "<Joystick>{Joystick2}/button8" + kPathSeparator + "<Gamepad>{Joystick2}/start" },
            { "joystick 2 button 8", "<Joystick>{Joystick2}/button9" + kPathSeparator + "<Gamepad>{Joystick2}/leftStickPress" },
            { "joystick 2 button 9", "<Joystick>{Joystick2}/button10" + kPathSeparator + "<Gamepad>{Joystick2}/rightStickPress" },
            { "joystick 2 button 10", "<Joystick>{Joystick2}/button11" },
            { "joystick 2 button 11", "<Joystick>{Joystick2}/button12" },
            { "joystick 2 button 12", "<Joystick>{Joystick2}/button13" },
            { "joystick 2 button 13", "<Joystick>{Joystick2}/button14" },
            { "joystick 2 button 14", "<Joystick>{Joystick2}/button15" },
            { "joystick 2 button 15", "<Joystick>{Joystick2}/button16" },
            { "joystick 2 button 16", "<Joystick>{Joystick2}/button17" },
            { "joystick 2 button 17", "<Joystick>{Joystick2}/button18" },
            { "joystick 2 button 18", "<Joystick>{Joystick2}/button19" },
            { "joystick 2 button 19", "<Joystick>{Joystick2}/button20" },
            { "joystick 3 button 0", "<Joystick>{Joystick3}/trigger" + kPathSeparator + "<Gamepad>{Joystick3}/buttonSouth" },
            { "joystick 3 button 1", "<Joystick>{Joystick3}/button2" + kPathSeparator + "<Gamepad>{Joystick3}/buttonEast" },
            { "joystick 3 button 2", "<Joystick>{Joystick3}/button3" + kPathSeparator + "<Gamepad>{Joystick3}/buttonWest" },
            { "joystick 3 button 3", "<Joystick>{Joystick3}/button4" + kPathSeparator + "<Gamepad>{Joystick3}/buttonNorth" },
            { "joystick 3 button 4", "<Joystick>{Joystick3}/button5" + kPathSeparator + "<Gamepad>{Joystick3}/leftShoulder" },
            { "joystick 3 button 5", "<Joystick>{Joystick3}/button6" + kPathSeparator + "<Gamepad>{Joystick3}/rightShoulder" },
            { "joystick 3 button 6", "<Joystick>{Joystick3}/button7" + kPathSeparator + "<Gamepad>{Joystick3}/select" },
            { "joystick 3 button 7", "<Joystick>{Joystick3}/button8" + kPathSeparator + "<Gamepad>{Joystick3}/start" },
            { "joystick 3 button 8", "<Joystick>{Joystick3}/button9" + kPathSeparator + "<Gamepad>{Joystick3}/leftStickPress" },
            { "joystick 3 button 9", "<Joystick>{Joystick3}/button10" + kPathSeparator + "<Gamepad>{Joystick3}/rightStickPress" },
            { "joystick 3 button 10", "<Joystick>{Joystick3}/button11" },
            { "joystick 3 button 11", "<Joystick>{Joystick3}/button12" },
            { "joystick 3 button 12", "<Joystick>{Joystick3}/button13" },
            { "joystick 3 button 13", "<Joystick>{Joystick3}/button14" },
            { "joystick 3 button 14", "<Joystick>{Joystick3}/button15" },
            { "joystick 3 button 15", "<Joystick>{Joystick3}/button16" },
            { "joystick 3 button 16", "<Joystick>{Joystick3}/button17" },
            { "joystick 3 button 17", "<Joystick>{Joystick3}/button18" },
            { "joystick 3 button 18", "<Joystick>{Joystick3}/button19" },
            { "joystick 3 button 19", "<Joystick>{Joystick3}/button20" },
            { "joystick 4 button 0", "<Joystick>{Joystick4}/trigger" + kPathSeparator + "<Gamepad>{Joystick4}/buttonSouth" },
            { "joystick 4 button 1", "<Joystick>{Joystick4}/button2" + kPathSeparator + "<Gamepad>{Joystick4}/buttonEast" },
            { "joystick 4 button 2", "<Joystick>{Joystick4}/button3" + kPathSeparator + "<Gamepad>{Joystick4}/buttonWest" },
            { "joystick 4 button 3", "<Joystick>{Joystick4}/button4" + kPathSeparator + "<Gamepad>{Joystick4}/buttonNorth" },
            { "joystick 4 button 4", "<Joystick>{Joystick4}/button5" + kPathSeparator + "<Gamepad>{Joystick4}/leftShoulder" },
            { "joystick 4 button 5", "<Joystick>{Joystick4}/button6" + kPathSeparator + "<Gamepad>{Joystick4}/rightShoulder" },
            { "joystick 4 button 6", "<Joystick>{Joystick4}/button7" + kPathSeparator + "<Gamepad>{Joystick4}/select" },
            { "joystick 4 button 7", "<Joystick>{Joystick4}/button8" + kPathSeparator + "<Gamepad>{Joystick4}/start" },
            { "joystick 4 button 8", "<Joystick>{Joystick4}/button9" + kPathSeparator + "<Gamepad>{Joystick4}/leftStickPress" },
            { "joystick 4 button 9", "<Joystick>{Joystick4}/button10" + kPathSeparator + "<Gamepad>{Joystick4}/rightStickPress" },
            { "joystick 4 button 10", "<Joystick>{Joystick4}/button11" },
            { "joystick 4 button 11", "<Joystick>{Joystick4}/button12" },
            { "joystick 4 button 12", "<Joystick>{Joystick4}/button13" },
            { "joystick 4 button 13", "<Joystick>{Joystick4}/button14" },
            { "joystick 4 button 14", "<Joystick>{Joystick4}/button15" },
            { "joystick 4 button 15", "<Joystick>{Joystick4}/button16" },
            { "joystick 4 button 16", "<Joystick>{Joystick4}/button17" },
            { "joystick 4 button 17", "<Joystick>{Joystick4}/button18" },
            { "joystick 4 button 18", "<Joystick>{Joystick4}/button19" },
            { "joystick 4 button 19", "<Joystick>{Joystick4}/button20" },
            { "joystick 5 button 0", "<Joystick>{Joystick5}/trigger" + kPathSeparator + "<Gamepad>{Joystick5}/buttonSouth" },
            { "joystick 5 button 1", "<Joystick>{Joystick5}/button2" + kPathSeparator + "<Gamepad>{Joystick5}/buttonEast" },
            { "joystick 5 button 2", "<Joystick>{Joystick5}/button3" + kPathSeparator + "<Gamepad>{Joystick5}/buttonWest" },
            { "joystick 5 button 3", "<Joystick>{Joystick5}/button4" + kPathSeparator + "<Gamepad>{Joystick5}/buttonNorth" },
            { "joystick 5 button 4", "<Joystick>{Joystick5}/button5" + kPathSeparator + "<Gamepad>{Joystick5}/leftShoulder" },
            { "joystick 5 button 5", "<Joystick>{Joystick5}/button6" + kPathSeparator + "<Gamepad>{Joystick5}/rightShoulder" },
            { "joystick 5 button 6", "<Joystick>{Joystick5}/button7" + kPathSeparator + "<Gamepad>{Joystick5}/select" },
            { "joystick 5 button 7", "<Joystick>{Joystick5}/button8" + kPathSeparator + "<Gamepad>{Joystick5}/start" },
            { "joystick 5 button 8", "<Joystick>{Joystick5}/button9" + kPathSeparator + "<Gamepad>{Joystick5}/leftStickPress" },
            { "joystick 5 button 9", "<Joystick>{Joystick5}/button10" + kPathSeparator + "<Gamepad>{Joystick5}/rightStickPress" },
            { "joystick 5 button 10", "<Joystick>{Joystick5}/button11" },
            { "joystick 5 button 11", "<Joystick>{Joystick5}/button12" },
            { "joystick 5 button 12", "<Joystick>{Joystick5}/button13" },
            { "joystick 5 button 13", "<Joystick>{Joystick5}/button14" },
            { "joystick 5 button 14", "<Joystick>{Joystick5}/button15" },
            { "joystick 5 button 15", "<Joystick>{Joystick5}/button16" },
            { "joystick 5 button 16", "<Joystick>{Joystick5}/button17" },
            { "joystick 5 button 17", "<Joystick>{Joystick5}/button18" },
            { "joystick 5 button 18", "<Joystick>{Joystick5}/button19" },
            { "joystick 5 button 19", "<Joystick>{Joystick5}/button20" },
            { "joystick 6 button 0", "<Joystick>{Joystick6}/trigger" + kPathSeparator + "<Gamepad>{Joystick6}/buttonSouth" },
            { "joystick 6 button 1", "<Joystick>{Joystick6}/button2" + kPathSeparator + "<Gamepad>{Joystick6}/buttonEast" },
            { "joystick 6 button 2", "<Joystick>{Joystick6}/button3" + kPathSeparator + "<Gamepad>{Joystick6}/buttonWest" },
            { "joystick 6 button 3", "<Joystick>{Joystick6}/button4" + kPathSeparator + "<Gamepad>{Joystick6}/buttonNorth" },
            { "joystick 6 button 4", "<Joystick>{Joystick6}/button5" + kPathSeparator + "<Gamepad>{Joystick6}/leftShoulder" },
            { "joystick 6 button 5", "<Joystick>{Joystick6}/button6" + kPathSeparator + "<Gamepad>{Joystick6}/rightShoulder" },
            { "joystick 6 button 6", "<Joystick>{Joystick6}/button7" + kPathSeparator + "<Gamepad>{Joystick6}/select" },
            { "joystick 6 button 7", "<Joystick>{Joystick6}/button8" + kPathSeparator + "<Gamepad>{Joystick6}/start" },
            { "joystick 6 button 8", "<Joystick>{Joystick6}/button9" + kPathSeparator + "<Gamepad>{Joystick6}/leftStickPress" },
            { "joystick 6 button 9", "<Joystick>{Joystick6}/button10" + kPathSeparator + "<Gamepad>{Joystick6}/rightStickPress" },
            { "joystick 6 button 10", "<Joystick>{Joystick6}/button11" },
            { "joystick 6 button 11", "<Joystick>{Joystick6}/button12" },
            { "joystick 6 button 12", "<Joystick>{Joystick6}/button13" },
            { "joystick 6 button 13", "<Joystick>{Joystick6}/button14" },
            { "joystick 6 button 14", "<Joystick>{Joystick6}/button15" },
            { "joystick 6 button 15", "<Joystick>{Joystick6}/button16" },
            { "joystick 6 button 16", "<Joystick>{Joystick6}/button17" },
            { "joystick 6 button 17", "<Joystick>{Joystick6}/button18" },
            { "joystick 6 button 18", "<Joystick>{Joystick6}/button19" },
            { "joystick 6 button 19", "<Joystick>{Joystick6}/button20" },
            { "joystick 7 button 0", "<Joystick>{Joystick7}/trigger" + kPathSeparator + "<Gamepad>{Joystick7}/buttonSouth" },
            { "joystick 7 button 1", "<Joystick>{Joystick7}/button2" + kPathSeparator + "<Gamepad>{Joystick7}/buttonEast" },
            { "joystick 7 button 2", "<Joystick>{Joystick7}/button3" + kPathSeparator + "<Gamepad>{Joystick7}/buttonWest" },
            { "joystick 7 button 3", "<Joystick>{Joystick7}/button4" + kPathSeparator + "<Gamepad>{Joystick7}/buttonNorth" },
            { "joystick 7 button 4", "<Joystick>{Joystick7}/button5" + kPathSeparator + "<Gamepad>{Joystick7}/leftShoulder" },
            { "joystick 7 button 5", "<Joystick>{Joystick7}/button6" + kPathSeparator + "<Gamepad>{Joystick7}/rightShoulder" },
            { "joystick 7 button 6", "<Joystick>{Joystick7}/button7" + kPathSeparator + "<Gamepad>{Joystick7}/select" },
            { "joystick 7 button 7", "<Joystick>{Joystick7}/button8" + kPathSeparator + "<Gamepad>{Joystick7}/start" },
            { "joystick 7 button 8", "<Joystick>{Joystick7}/button9" + kPathSeparator + "<Gamepad>{Joystick7}/leftStickPress" },
            { "joystick 7 button 9", "<Joystick>{Joystick7}/button10" + kPathSeparator + "<Gamepad>{Joystick7}/rightStickPress" },
            { "joystick 7 button 10", "<Joystick>{Joystick7}/button11" },
            { "joystick 7 button 11", "<Joystick>{Joystick7}/button12" },
            { "joystick 7 button 12", "<Joystick>{Joystick7}/button13" },
            { "joystick 7 button 13", "<Joystick>{Joystick7}/button14" },
            { "joystick 7 button 14", "<Joystick>{Joystick7}/button15" },
            { "joystick 7 button 15", "<Joystick>{Joystick7}/button16" },
            { "joystick 7 button 16", "<Joystick>{Joystick7}/button17" },
            { "joystick 7 button 17", "<Joystick>{Joystick7}/button18" },
            { "joystick 7 button 18", "<Joystick>{Joystick7}/button19" },
            { "joystick 7 button 19", "<Joystick>{Joystick7}/button20" },
            { "joystick 8 button 0", "<Joystick>{Joystick8}/trigger" + kPathSeparator + "<Gamepad>{Joystick8}/buttonSouth" },
            { "joystick 8 button 1", "<Joystick>{Joystick8}/button2" + kPathSeparator + "<Gamepad>{Joystick8}/buttonEast" },
            { "joystick 8 button 2", "<Joystick>{Joystick8}/button3" + kPathSeparator + "<Gamepad>{Joystick8}/buttonWest" },
            { "joystick 8 button 3", "<Joystick>{Joystick8}/button4" + kPathSeparator + "<Gamepad>{Joystick8}/buttonNorth" },
            { "joystick 8 button 4", "<Joystick>{Joystick8}/button5" + kPathSeparator + "<Gamepad>{Joystick8}/leftShoulder" },
            { "joystick 8 button 5", "<Joystick>{Joystick8}/button6" + kPathSeparator + "<Gamepad>{Joystick8}/rightShoulder" },
            { "joystick 8 button 6", "<Joystick>{Joystick8}/button7" + kPathSeparator + "<Gamepad>{Joystick8}/select" },
            { "joystick 8 button 7", "<Joystick>{Joystick8}/button8" + kPathSeparator + "<Gamepad>{Joystick8}/start" },
            { "joystick 8 button 8", "<Joystick>{Joystick8}/button9" + kPathSeparator + "<Gamepad>{Joystick8}/leftStickPress" },
            { "joystick 8 button 9", "<Joystick>{Joystick8}/button10" + kPathSeparator + "<Gamepad>{Joystick8}/rightStickPress" },
            { "joystick 8 button 10", "<Joystick>{Joystick8}/button11" },
            { "joystick 8 button 11", "<Joystick>{Joystick8}/button12" },
            { "joystick 8 button 12", "<Joystick>{Joystick8}/button13" },
            { "joystick 8 button 13", "<Joystick>{Joystick8}/button14" },
            { "joystick 8 button 14", "<Joystick>{Joystick8}/button15" },
            { "joystick 8 button 15", "<Joystick>{Joystick8}/button16" },
            { "joystick 8 button 16", "<Joystick>{Joystick8}/button17" },
            { "joystick 8 button 17", "<Joystick>{Joystick8}/button18" },
            { "joystick 8 button 18", "<Joystick>{Joystick8}/button19" },
            { "joystick 8 button 19", "<Joystick>{Joystick8}/button20" },
            { "joystick 9 button 0", "<Joystick>{Joystick9}/trigger" + kPathSeparator + "<Gamepad>{Joystick9}/buttonSouth" },
            { "joystick 9 button 1", "<Joystick>{Joystick9}/button2" + kPathSeparator + "<Gamepad>{Joystick9}/buttonEast" },
            { "joystick 9 button 2", "<Joystick>{Joystick9}/button3" + kPathSeparator + "<Gamepad>{Joystick9}/buttonWest" },
            { "joystick 9 button 3", "<Joystick>{Joystick9}/button4" + kPathSeparator + "<Gamepad>{Joystick9}/buttonNorth" },
            { "joystick 9 button 4", "<Joystick>{Joystick9}/button5" + kPathSeparator + "<Gamepad>{Joystick9}/leftShoulder" },
            { "joystick 9 button 5", "<Joystick>{Joystick9}/button6" + kPathSeparator + "<Gamepad>{Joystick9}/rightShoulder" },
            { "joystick 9 button 6", "<Joystick>{Joystick9}/button7" + kPathSeparator + "<Gamepad>{Joystick9}/select" },
            { "joystick 9 button 7", "<Joystick>{Joystick9}/button8" + kPathSeparator + "<Gamepad>{Joystick9}/start" },
            { "joystick 9 button 8", "<Joystick>{Joystick9}/button9" + kPathSeparator + "<Gamepad>{Joystick9}/leftStickPress" },
            { "joystick 9 button 9", "<Joystick>{Joystick9}/button10" + kPathSeparator + "<Gamepad>{Joystick9}/rightStickPress" },
            { "joystick 9 button 10", "<Joystick>{Joystick9}/button11" },
            { "joystick 9 button 11", "<Joystick>{Joystick9}/button12" },
            { "joystick 9 button 12", "<Joystick>{Joystick9}/button13" },
            { "joystick 9 button 13", "<Joystick>{Joystick9}/button14" },
            { "joystick 9 button 14", "<Joystick>{Joystick9}/button15" },
            { "joystick 9 button 15", "<Joystick>{Joystick9}/button16" },
            { "joystick 9 button 16", "<Joystick>{Joystick9}/button17" },
            { "joystick 9 button 17", "<Joystick>{Joystick9}/button18" },
            { "joystick 9 button 18", "<Joystick>{Joystick9}/button19" },
            { "joystick 9 button 19", "<Joystick>{Joystick9}/button20" },
            { "joystick 10 button 0", "<Joystick>{Joystick10}/trigger" + kPathSeparator + "<Gamepad>{Joystick10}/buttonSouth" },
            { "joystick 10 button 1", "<Joystick>{Joystick10}/button2" + kPathSeparator + "<Gamepad>{Joystick10}/buttonEast" },
            { "joystick 10 button 2", "<Joystick>{Joystick10}/button3" + kPathSeparator + "<Gamepad>{Joystick10}/buttonWest" },
            { "joystick 10 button 3", "<Joystick>{Joystick10}/button4" + kPathSeparator + "<Gamepad>{Joystick10}/buttonNorth" },
            { "joystick 10 button 4", "<Joystick>{Joystick10}/button5" + kPathSeparator + "<Gamepad>{Joystick10}/leftShoulder" },
            { "joystick 10 button 5", "<Joystick>{Joystick10}/button6" + kPathSeparator + "<Gamepad>{Joystick10}/rightShoulder" },
            { "joystick 10 button 6", "<Joystick>{Joystick10}/button7" + kPathSeparator + "<Gamepad>{Joystick10}/select" },
            { "joystick 10 button 7", "<Joystick>{Joystick10}/button8" + kPathSeparator + "<Gamepad>{Joystick10}/start" },
            { "joystick 10 button 8", "<Joystick>{Joystick10}/button9" + kPathSeparator + "<Gamepad>{Joystick10}/leftStickPress" },
            { "joystick 10 button 9", "<Joystick>{Joystick10}/button10" + kPathSeparator + "<Gamepad>{Joystick10}/rightStickPress" },
            { "joystick 10 button 10", "<Joystick>{Joystick10}/button11" },
            { "joystick 10 button 11", "<Joystick>{Joystick10}/button12" },
            { "joystick 10 button 12", "<Joystick>{Joystick10}/button13" },
            { "joystick 10 button 13", "<Joystick>{Joystick10}/button14" },
            { "joystick 10 button 14", "<Joystick>{Joystick10}/button15" },
            { "joystick 10 button 15", "<Joystick>{Joystick10}/button16" },
            { "joystick 10 button 16", "<Joystick>{Joystick10}/button17" },
            { "joystick 10 button 17", "<Joystick>{Joystick10}/button18" },
            { "joystick 10 button 18", "<Joystick>{Joystick10}/button19" },
            { "joystick 10 button 19", "<Joystick>{Joystick10}/button20" },
            { "joystick 11 button 0", "<Joystick>{Joystick11}/trigger" + kPathSeparator + "<Gamepad>{Joystick11}/buttonSouth" },
            { "joystick 11 button 1", "<Joystick>{Joystick11}/button2" + kPathSeparator + "<Gamepad>{Joystick11}/buttonEast" },
            { "joystick 11 button 2", "<Joystick>{Joystick11}/button3" + kPathSeparator + "<Gamepad>{Joystick11}/buttonWest" },
            { "joystick 11 button 3", "<Joystick>{Joystick11}/button4" + kPathSeparator + "<Gamepad>{Joystick11}/buttonNorth" },
            { "joystick 11 button 4", "<Joystick>{Joystick11}/button5" + kPathSeparator + "<Gamepad>{Joystick11}/leftShoulder" },
            { "joystick 11 button 5", "<Joystick>{Joystick11}/button6" + kPathSeparator + "<Gamepad>{Joystick11}/rightShoulder" },
            { "joystick 11 button 6", "<Joystick>{Joystick11}/button7" + kPathSeparator + "<Gamepad>{Joystick11}/select" },
            { "joystick 11 button 7", "<Joystick>{Joystick11}/button8" + kPathSeparator + "<Gamepad>{Joystick11}/start" },
            { "joystick 11 button 8", "<Joystick>{Joystick11}/button9" + kPathSeparator + "<Gamepad>{Joystick11}/leftStickPress" },
            { "joystick 11 button 9", "<Joystick>{Joystick11}/button10" + kPathSeparator + "<Gamepad>{Joystick11}/rightStickPress" },
            { "joystick 11 button 10", "<Joystick>{Joystick11}/button11" },
            { "joystick 11 button 11", "<Joystick>{Joystick11}/button12" },
            { "joystick 11 button 12", "<Joystick>{Joystick11}/button13" },
            { "joystick 11 button 13", "<Joystick>{Joystick11}/button14" },
            { "joystick 11 button 14", "<Joystick>{Joystick11}/button15" },
            { "joystick 11 button 15", "<Joystick>{Joystick11}/button16" },
            { "joystick 11 button 16", "<Joystick>{Joystick11}/button17" },
            { "joystick 11 button 17", "<Joystick>{Joystick11}/button18" },
            { "joystick 11 button 18", "<Joystick>{Joystick11}/button19" },
            { "joystick 11 button 19", "<Joystick>{Joystick11}/button20" },
            { "joystick 12 button 0", "<Joystick>{Joystick12}/trigger" + kPathSeparator + "<Gamepad>{Joystick12}/buttonSouth" },
            { "joystick 12 button 1", "<Joystick>{Joystick12}/button2" + kPathSeparator + "<Gamepad>{Joystick12}/buttonEast" },
            { "joystick 12 button 2", "<Joystick>{Joystick12}/button3" + kPathSeparator + "<Gamepad>{Joystick12}/buttonWest" },
            { "joystick 12 button 3", "<Joystick>{Joystick12}/button4" + kPathSeparator + "<Gamepad>{Joystick12}/buttonNorth" },
            { "joystick 12 button 4", "<Joystick>{Joystick12}/button5" + kPathSeparator + "<Gamepad>{Joystick12}/leftShoulder" },
            { "joystick 12 button 5", "<Joystick>{Joystick12}/button6" + kPathSeparator + "<Gamepad>{Joystick12}/rightShoulder" },
            { "joystick 12 button 6", "<Joystick>{Joystick12}/button7" + kPathSeparator + "<Gamepad>{Joystick12}/select" },
            { "joystick 12 button 7", "<Joystick>{Joystick12}/button8" + kPathSeparator + "<Gamepad>{Joystick12}/start" },
            { "joystick 12 button 8", "<Joystick>{Joystick12}/button9" + kPathSeparator + "<Gamepad>{Joystick12}/leftStickPress" },
            { "joystick 12 button 9", "<Joystick>{Joystick12}/button10" + kPathSeparator + "<Gamepad>{Joystick12}/rightStickPress" },
            { "joystick 12 button 10", "<Joystick>{Joystick12}/button11" },
            { "joystick 12 button 11", "<Joystick>{Joystick12}/button12" },
            { "joystick 12 button 12", "<Joystick>{Joystick12}/button13" },
            { "joystick 12 button 13", "<Joystick>{Joystick12}/button14" },
            { "joystick 12 button 14", "<Joystick>{Joystick12}/button15" },
            { "joystick 12 button 15", "<Joystick>{Joystick12}/button16" },
            { "joystick 12 button 16", "<Joystick>{Joystick12}/button17" },
            { "joystick 12 button 17", "<Joystick>{Joystick12}/button18" },
            { "joystick 12 button 18", "<Joystick>{Joystick12}/button19" },
            { "joystick 12 button 19", "<Joystick>{Joystick12}/button20" },
            { "joystick 13 button 0", "<Joystick>{Joystick13}/trigger" + kPathSeparator + "<Gamepad>{Joystick13}/buttonSouth" },
            { "joystick 13 button 1", "<Joystick>{Joystick13}/button2" + kPathSeparator + "<Gamepad>{Joystick13}/buttonEast" },
            { "joystick 13 button 2", "<Joystick>{Joystick13}/button3" + kPathSeparator + "<Gamepad>{Joystick13}/buttonWest" },
            { "joystick 13 button 3", "<Joystick>{Joystick13}/button4" + kPathSeparator + "<Gamepad>{Joystick13}/buttonNorth" },
            { "joystick 13 button 4", "<Joystick>{Joystick13}/button5" + kPathSeparator + "<Gamepad>{Joystick13}/leftShoulder" },
            { "joystick 13 button 5", "<Joystick>{Joystick13}/button6" + kPathSeparator + "<Gamepad>{Joystick13}/rightShoulder" },
            { "joystick 13 button 6", "<Joystick>{Joystick13}/button7" + kPathSeparator + "<Gamepad>{Joystick13}/select" },
            { "joystick 13 button 7", "<Joystick>{Joystick13}/button8" + kPathSeparator + "<Gamepad>{Joystick13}/start" },
            { "joystick 13 button 8", "<Joystick>{Joystick13}/button9" + kPathSeparator + "<Gamepad>{Joystick13}/leftStickPress" },
            { "joystick 13 button 9", "<Joystick>{Joystick13}/button10" + kPathSeparator + "<Gamepad>{Joystick13}/rightStickPress" },
            { "joystick 13 button 10", "<Joystick>{Joystick13}/button11" },
            { "joystick 13 button 11", "<Joystick>{Joystick13}/button12" },
            { "joystick 13 button 12", "<Joystick>{Joystick13}/button13" },
            { "joystick 13 button 13", "<Joystick>{Joystick13}/button14" },
            { "joystick 13 button 14", "<Joystick>{Joystick13}/button15" },
            { "joystick 13 button 15", "<Joystick>{Joystick13}/button16" },
            { "joystick 13 button 16", "<Joystick>{Joystick13}/button17" },
            { "joystick 13 button 17", "<Joystick>{Joystick13}/button18" },
            { "joystick 13 button 18", "<Joystick>{Joystick13}/button19" },
            { "joystick 13 button 19", "<Joystick>{Joystick13}/button20" },
            { "joystick 14 button 0", "<Joystick>{Joystick14}/trigger" + kPathSeparator + "<Gamepad>{Joystick14}/buttonSouth" },
            { "joystick 14 button 1", "<Joystick>{Joystick14}/button2" + kPathSeparator + "<Gamepad>{Joystick14}/buttonEast" },
            { "joystick 14 button 2", "<Joystick>{Joystick14}/button3" + kPathSeparator + "<Gamepad>{Joystick14}/buttonWest" },
            { "joystick 14 button 3", "<Joystick>{Joystick14}/button4" + kPathSeparator + "<Gamepad>{Joystick14}/buttonNorth" },
            { "joystick 14 button 4", "<Joystick>{Joystick14}/button5" + kPathSeparator + "<Gamepad>{Joystick14}/leftShoulder" },
            { "joystick 14 button 5", "<Joystick>{Joystick14}/button6" + kPathSeparator + "<Gamepad>{Joystick14}/rightShoulder" },
            { "joystick 14 button 6", "<Joystick>{Joystick14}/button7" + kPathSeparator + "<Gamepad>{Joystick14}/select" },
            { "joystick 14 button 7", "<Joystick>{Joystick14}/button8" + kPathSeparator + "<Gamepad>{Joystick14}/start" },
            { "joystick 14 button 8", "<Joystick>{Joystick14}/button9" + kPathSeparator + "<Gamepad>{Joystick14}/leftStickPress" },
            { "joystick 14 button 9", "<Joystick>{Joystick14}/button10" + kPathSeparator + "<Gamepad>{Joystick14}/rightStickPress" },
            { "joystick 14 button 10", "<Joystick>{Joystick14}/button11" },
            { "joystick 14 button 11", "<Joystick>{Joystick14}/button12" },
            { "joystick 14 button 12", "<Joystick>{Joystick14}/button13" },
            { "joystick 14 button 13", "<Joystick>{Joystick14}/button14" },
            { "joystick 14 button 14", "<Joystick>{Joystick14}/button15" },
            { "joystick 14 button 15", "<Joystick>{Joystick14}/button16" },
            { "joystick 14 button 16", "<Joystick>{Joystick14}/button17" },
            { "joystick 14 button 17", "<Joystick>{Joystick14}/button18" },
            { "joystick 14 button 18", "<Joystick>{Joystick14}/button19" },
            { "joystick 14 button 19", "<Joystick>{Joystick14}/button20" },
            { "joystick 15 button 0", "<Joystick>{Joystick15}/trigger" + kPathSeparator + "<Gamepad>{Joystick15}/buttonSouth" },
            { "joystick 15 button 1", "<Joystick>{Joystick15}/button2" + kPathSeparator + "<Gamepad>{Joystick15}/buttonEast" },
            { "joystick 15 button 2", "<Joystick>{Joystick15}/button3" + kPathSeparator + "<Gamepad>{Joystick15}/buttonWest" },
            { "joystick 15 button 3", "<Joystick>{Joystick15}/button4" + kPathSeparator + "<Gamepad>{Joystick15}/buttonNorth" },
            { "joystick 15 button 4", "<Joystick>{Joystick15}/button5" + kPathSeparator + "<Gamepad>{Joystick15}/leftShoulder" },
            { "joystick 15 button 5", "<Joystick>{Joystick15}/button6" + kPathSeparator + "<Gamepad>{Joystick15}/rightShoulder" },
            { "joystick 15 button 6", "<Joystick>{Joystick15}/button7" + kPathSeparator + "<Gamepad>{Joystick15}/select" },
            { "joystick 15 button 7", "<Joystick>{Joystick15}/button8" + kPathSeparator + "<Gamepad>{Joystick15}/start" },
            { "joystick 15 button 8", "<Joystick>{Joystick15}/button9" + kPathSeparator + "<Gamepad>{Joystick15}/leftStickPress" },
            { "joystick 15 button 9", "<Joystick>{Joystick15}/button10" + kPathSeparator + "<Gamepad>{Joystick15}/rightStickPress" },
            { "joystick 15 button 10", "<Joystick>{Joystick15}/button11" },
            { "joystick 15 button 11", "<Joystick>{Joystick15}/button12" },
            { "joystick 15 button 12", "<Joystick>{Joystick15}/button13" },
            { "joystick 15 button 13", "<Joystick>{Joystick15}/button14" },
            { "joystick 15 button 14", "<Joystick>{Joystick15}/button15" },
            { "joystick 15 button 15", "<Joystick>{Joystick15}/button16" },
            { "joystick 15 button 16", "<Joystick>{Joystick15}/button17" },
            { "joystick 15 button 17", "<Joystick>{Joystick15}/button18" },
            { "joystick 15 button 18", "<Joystick>{Joystick15}/button19" },
            { "joystick 15 button 19", "<Joystick>{Joystick15}/button20" },
            { "joystick 16 button 0", "<Joystick>{Joystick16}/trigger" + kPathSeparator + "<Gamepad>{Joystick16}/buttonSouth" },
            { "joystick 16 button 1", "<Joystick>{Joystick16}/button2" + kPathSeparator + "<Gamepad>{Joystick16}/buttonEast" },
            { "joystick 16 button 2", "<Joystick>{Joystick16}/button3" + kPathSeparator + "<Gamepad>{Joystick16}/buttonWest" },
            { "joystick 16 button 3", "<Joystick>{Joystick16}/button4" + kPathSeparator + "<Gamepad>{Joystick16}/buttonNorth" },
            { "joystick 16 button 4", "<Joystick>{Joystick16}/button5" + kPathSeparator + "<Gamepad>{Joystick16}/leftShoulder" },
            { "joystick 16 button 5", "<Joystick>{Joystick16}/button6" + kPathSeparator + "<Gamepad>{Joystick16}/rightShoulder" },
            { "joystick 16 button 6", "<Joystick>{Joystick16}/button7" + kPathSeparator + "<Gamepad>{Joystick16}/select" },
            { "joystick 16 button 7", "<Joystick>{Joystick16}/button8" + kPathSeparator + "<Gamepad>{Joystick16}/start" },
            { "joystick 16 button 8", "<Joystick>{Joystick16}/button9" + kPathSeparator + "<Gamepad>{Joystick16}/leftStickPress" },
            { "joystick 16 button 9", "<Joystick>{Joystick16}/button10" + kPathSeparator + "<Gamepad>{Joystick16}/rightStickPress" },
            { "joystick 16 button 10", "<Joystick>{Joystick16}/button11" },
            { "joystick 16 button 11", "<Joystick>{Joystick16}/button12" },
            { "joystick 16 button 12", "<Joystick>{Joystick16}/button13" },
            { "joystick 16 button 13", "<Joystick>{Joystick16}/button14" },
            { "joystick 16 button 14", "<Joystick>{Joystick16}/button15" },
            { "joystick 16 button 15", "<Joystick>{Joystick16}/button16" },
            { "joystick 16 button 16", "<Joystick>{Joystick16}/button17" },
            { "joystick 16 button 17", "<Joystick>{Joystick16}/button18" },
            { "joystick 16 button 18", "<Joystick>{Joystick16}/button19" },
            { "joystick 16 button 19", "<Joystick>{Joystick16}/button20" },
        };
    }
}
#endif // UNITY_EDITOR
