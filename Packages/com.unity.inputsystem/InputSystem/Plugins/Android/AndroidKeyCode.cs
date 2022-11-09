#if UNITY_EDITOR || UNITY_ANDROID || PACKAGE_DOCS_GENERATION
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android.LowLevel
{
    /// <summary>
    /// Enum used to identity the key in the Android key event. See <see cref="AndroidGameControllerState.buttons"/>.
    /// See https://developer.android.com/reference/android/view/KeyEvent#constants_1 for more details.
    /// </summary>
    public enum AndroidKeyCode
    {
        /// <summary>
        /// Unknown key code.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Soft Left key. Usually situated below the display on phones and used as a multi-function feature key for selecting a software defined function shown on the bottom left of the display.
        /// </summary>
        SoftLeft = 1,

        /// <summary>
        /// Soft Right key. Usually situated below the display on phones and used as a multi-function feature key for selecting a software defined function shown on the bottom right of the display.
        /// </summary>
        SoftRight = 2,

        /// <summary>
        /// Home key. This key is handled by the framework and is never delivered to applications.
        /// </summary>
        Home = 3,

        /// <summary>
        /// Back key.
        /// </summary>
        Back = 4,

        /// <summary>
        /// Call key.
        /// </summary>
        Call = 5,

        /// <summary>
        /// End Call key.
        /// </summary>
        Endcall = 6,

        /// <summary>
        /// '0' key.
        /// </summary>
        Alpha0 = 7,

        /// <summary>
        /// '1' key.
        /// </summary>
        Alpha1 = 8,

        /// <summary>
        /// '2' key.
        /// </summary>
        Alpha2 = 9,

        /// <summary>
        /// '3' key.
        /// </summary>
        Alpha3 = 10,

        /// <summary>
        /// '4' key.
        /// </summary>
        Alpha4 = 11,

        /// <summary>
        /// '5' key.
        /// </summary>
        Alpha5 = 12,

        /// <summary>
        /// '6' key.
        /// </summary>
        Alpha6 = 13,

        /// <summary>
        /// '7' key.
        /// </summary>
        Alpha7 = 14,

        /// <summary>
        /// '8' key.
        /// </summary>
        Alpha8 = 15,

        /// <summary>
        /// '9' key.
        /// </summary>
        Alpha9 = 16,

        /// <summary>
        /// '*' key.
        /// </summary>
        Star = 17,

        /// <summary>
        /// '#' key.
        /// </summary>
        Pound = 18,

        /// <summary>
        /// Directional Pad Up key. May also be synthesized from trackball motions.
        /// </summary>
        DpadUp = 19,

        /// <summary>
        /// Directional Pad Down key. May also be synthesized from trackball motions.
        /// </summary>
        DpadDown = 20,

        /// <summary>
        /// Directional Pad Left key. May also be synthesized from trackball motions.
        /// </summary>
        DpadLeft = 21,

        /// <summary>
        /// Directional Pad Right key. May also be synthesized from trackball motions.
        /// </summary>
        DpadRight = 22,

        /// <summary>
        /// Directional Pad Center key. May also be synthesized from trackball motions.
        /// </summary>
        DpadCenter = 23,

        /// <summary>
        /// Volume Up key. Adjusts the speaker volume up.
        /// </summary>
        VolumeUp = 24,

        /// <summary>
        /// Volume Down key. Adjusts the speaker volume down.
        /// </summary>
        VolumeDown = 25,

        /// <summary>
        /// Power key.
        /// </summary>
        Power = 26,

        /// <summary>
        /// Camera key. Used to launch a camera application or take pictures.
        /// </summary>
        Camera = 27,

        /// <summary>
        /// Clear key.
        /// </summary>
        Clear = 28,

        /// <summary>
        /// 'A' key.
        /// </summary>
        A = 29,

        /// <summary>
        /// 'B' key.
        /// </summary>
        B = 30,

        /// <summary>
        /// 'C' key.
        /// </summary>
        C = 31,

        /// <summary>
        /// 'D' key.
        /// </summary>
        D = 32,

        /// <summary>
        /// 'E' key.
        /// </summary>
        E = 33,

        /// <summary>
        /// 'F' key.
        /// </summary>
        F = 34,

        /// <summary>
        /// 'G' key.
        /// </summary>
        G = 35,

        /// <summary>
        /// 'H' key.
        /// </summary>
        H = 36,

        /// <summary>
        /// 'I' key.
        /// </summary>
        I = 37,

        /// <summary>
        /// 'J' key.
        /// </summary>
        J = 38,

        /// <summary>
        /// 'K' key.
        /// </summary>
        K = 39,

        /// <summary>
        /// 'L' key.
        /// </summary>
        L = 40,

        /// <summary>
        /// 'M' key.
        /// </summary>
        M = 41,

        /// <summary>
        /// 'N' key.
        /// </summary>
        N = 42,

        /// <summary>
        /// 'O' key.
        /// </summary>
        O = 43,

        /// <summary>
        /// 'P' key.
        /// </summary>
        P = 44,

        /// <summary>
        /// 'Q' key.
        /// </summary>
        Q = 45,

        /// <summary>
        /// 'R' key.
        /// </summary>
        R = 46,

        /// <summary>
        /// 'S' key.
        /// </summary>
        S = 47,

        /// <summary>
        /// 'T' key.
        /// </summary>
        T = 48,

        /// <summary>
        /// 'U' key.
        /// </summary>
        U = 49,

        /// <summary>
        /// 'V' key.
        /// </summary>
        V = 50,

        /// <summary>
        /// 'W' key.
        /// </summary>
        W = 51,

        /// <summary>
        /// 'X' key.
        /// </summary>
        X = 52,

        /// <summary>
        /// 'Y' key.
        /// </summary>
        Y = 53,

        /// <summary>
        /// 'Z' key.
        /// </summary>
        Z = 54,

        /// <summary>
        /// ',' key.
        /// </summary>
        Comma = 55,

        /// <summary>
        /// '.' key.
        /// </summary>
        Period = 56,

        /// <summary>
        /// Left Alt modifier key.
        /// </summary>
        AltLeft = 57,

        /// <summary>
        /// Right Alt modifier key.
        /// </summary>
        AltRight = 58,

        /// <summary>
        /// Left Shift modifier key.
        /// </summary>
        ShiftLeft = 59,

        /// <summary>
        /// Right Shift modifier key.
        /// </summary>
        ShiftRight = 60,

        /// <summary>
        /// Tab key.
        /// </summary>
        Tab = 61,

        /// <summary>
        /// Space key.
        /// </summary>
        Space = 62,

        /// <summary>
        /// Symbol modifier key. Used to enter alternate symbols.
        /// </summary>
        Sym = 63,

        /// <summary>
        /// Explorer special function key. Used to launch a browser application.
        /// </summary>
        Explorer = 64,

        /// <summary>
        /// Envelope special function key. Used to launch a mail application.
        /// </summary>
        Envelope = 65,

        /// <summary>
        /// Enter key.
        /// </summary>
        Enter = 66,

        /// <summary>
        /// Backspace key. Deletes characters before the insertion point, unlike <see cref="AndroidKeyCode.ForwardDel"/>.
        /// </summary>
        Del = 67,

        /// <summary>
        /// '`' (backtick) key.
        /// </summary>
        Grave = 68,

        /// <summary>
        /// '-' key.
        /// </summary>
        Minus = 69,

        /// <summary>
        /// '=' key.
        /// </summary>
        Equals = 70,

        /// <summary>
        /// '[' key.
        /// </summary>
        LeftBracket = 71,

        /// <summary>
        /// ']' key.
        /// </summary>
        RightBracket = 72,

        /// <summary>
        /// '\' key.
        /// </summary>
        Backslash = 73,

        /// <summary>
        /// ';' key.
        /// </summary>
        Semicolon = 74,

        /// <summary>
        /// ''' (apostrophe) key.
        /// </summary>
        Apostrophe = 75,

        /// <summary>
        /// '/' key.
        /// </summary>
        Slash = 76,

        /// <summary>
        /// '@' key.
        /// </summary>
        At = 77,

        /// <summary>
        /// Number modifier key. Used to enter numeric symbols. This key is not Num Lock; it is more like <see cref="AndroidKeyCode.AltLeft"/>.
        /// </summary>
        Num = 78,

        /// <summary>
        /// Headset Hook key. Used to hang up calls and stop media.
        /// </summary>
        Headsethook = 79,

        /// <summary>
        /// Camera Focus key. Used to focus the camera.
        /// </summary>
        Focus = 80,

        /// <summary>
        /// '+' key.
        /// </summary> // *Camera* focus
        Plus = 81,

        /// <summary>
        /// Menu key.
        /// </summary>
        Menu = 82,

        /// <summary>
        /// Notification key.
        /// </summary>
        Notification = 83,

        /// <summary>
        /// Search key.
        /// </summary>
        Search = 84,

        /// <summary>
        /// Play/Pause media key.
        /// </summary>
        MediaPlayPause = 85,

        /// <summary>
        /// Stop media key.
        /// </summary>
        MediaStop = 86,

        /// <summary>
        /// Play Next media key.
        /// </summary>
        MediaNext = 87,

        /// <summary>
        /// Play Previous media key.
        /// </summary>
        MediaPrevious = 88,

        /// <summary>
        /// Rewind media key.
        /// </summary>
        MediaRewind = 89,

        /// <summary>
        /// Fast Forward media key.
        /// </summary>
        MediaFastForward = 90,

        /// <summary>
        /// Mute key. Mutes the microphone, unlike <see cref="AndroidKeyCode.VolumeMute"/>.
        /// </summary>
        Mute = 91,

        /// <summary>
        /// Page Up key.
        /// </summary>
        PageUp = 92,

        /// <summary>
        /// Page Down key.
        /// </summary>
        PageDown = 93,

        /// <summary>
        /// Picture Symbols modifier key. Used to switch symbol sets (Emoji, Kao-moji).
        /// </summary>
        Pictsymbols = 94,

        /// <summary>
        /// Switch Charset modifier key. Used to switch character sets (Kanji, Katakana).
        /// </summary>
        SwitchCharset = 95,

        /// <summary>
        /// A Button key. On a game controller, the A button should be either the button labeled A or the first button on the bottom row of controller buttons.
        /// </summary>
        ButtonA = 96,

        /// <summary>
        /// B Button key. On a game controller, the B button should be either the button labeled B or the second button on the bottom row of controller buttons.
        /// </summary>
        ButtonB = 97,

        /// <summary>
        /// C Button key. On a game controller, the C button should be either the button labeled C or the third button on the bottom row of controller buttons.
        /// </summary>
        ButtonC = 98,

        /// <summary>
        /// X Button key. On a game controller, the X button should be either the button labeled X or the first button on the upper row of controller buttons.
        /// </summary>
        ButtonX = 99,

        /// <summary>
        /// Y Button key. On a game controller, the Y button should be either the button labeled Y or the second button on the upper row of controller buttons.
        /// </summary>
        ButtonY = 100,

        /// <summary>
        /// Z Button key. On a game controller, the Z button should be either the button labeled Z or the third button on the upper row of controller buttons.
        /// </summary>
        ButtonZ = 101,

        /// <summary>
        /// L1 Button key. On a game controller, the L1 button should be either the button labeled L1 (or L) or the top left trigger button.
        /// </summary>
        ButtonL1 = 102,

        /// <summary>
        /// R1 Button key. On a game controller, the R1 button should be either the button labeled R1 (or R) or the top right trigger button.
        /// </summary>
        ButtonR1 = 103,

        /// <summary>
        /// L2 Button key. On a game controller, the L2 button should be either the button labeled L2 or the bottom left trigger button.
        /// </summary>
        ButtonL2 = 104,

        /// <summary>
        /// R2 Button key. On a game controller, the R2 button should be either the button labeled R2 or the bottom right trigger button.
        /// </summary>
        ButtonR2 = 105,

        /// <summary>
        /// Left Thumb Button key. On a game controller, the left thumb button indicates that the left (or only) joystick is pressed.
        /// </summary>
        ButtonThumbl = 106,

        /// <summary>
        /// Right Thumb Button key. On a game controller, the right thumb button indicates that the right joystick is pressed.
        /// </summary>
        ButtonThumbr = 107,

        /// <summary>
        /// Start Button key. On a game controller, the button labeled Start.
        /// </summary>
        ButtonStart = 108,

        /// <summary>
        /// Select Button key. On a game controller, the button labeled Select.
        /// </summary>
        ButtonSelect = 109,

        /// <summary>
        /// Mode Button key. On a game controller, the button labeled Mode.
        /// </summary>
        ButtonMode = 110,

        /// <summary>
        /// Escape key.
        /// </summary>
        Escape = 111,

        /// <summary>
        /// Forward Delete key. Deletes characters ahead of the insertion point, unlike <see cref="AndroidKeyCode.Del"/>.
        /// </summary>
        ForwardDel = 112,

        /// <summary>
        /// Left Control modifier key.
        /// </summary>
        CtrlLeft = 113,

        /// <summary>
        /// Right Control modifier key.
        /// </summary>
        CtrlRight = 114,

        /// <summary>
        /// Caps Lock key.
        /// </summary>
        CapsLock = 115,

        /// <summary>
        /// Scroll Lock key.
        /// </summary>
        ScrollLock = 116,

        /// <summary>
        /// Left Meta modifier key.
        /// </summary>
        MetaLeft = 117,

        /// <summary>
        /// Right Meta modifier key.
        /// </summary>
        MetaRight = 118,

        /// <summary>
        /// Function modifier key.
        /// </summary>
        Function = 119,

        /// <summary>
        /// System Request / Print Screen key.
        /// </summary>
        Sysrq = 120,

        /// <summary>
        /// Break / Pause key.
        /// </summary>
        Break = 121,

        /// <summary>
        /// Home Movement key. Used for scrolling or moving the cursor around to the start of a line or to the top of a list.
        /// </summary>
        MoveHome = 122,

        /// <summary>
        /// End Movement key. Used for scrolling or moving the cursor around to the end of a line or to the bottom of a list.
        /// </summary>
        MoveEnd = 123,

        /// <summary>
        /// Insert key. Toggles insert / overwrite edit mode.
        /// </summary>
        Insert = 124,

        /// <summary>
        /// Forward key. Navigates forward in the history stack. Complement of <see cref="AndroidKeyCode.Back"/>.
        /// </summary>
        Forward = 125,

        /// <summary>
        /// Play media key.
        /// </summary>
        MediaPlay = 126,

        /// <summary>
        /// Play/Pause media key.
        /// </summary>
        MediaPause = 127,

        /// <summary>
        /// Close media key. May be used to close a CD tray, for example.
        /// </summary>
        MediaClose = 128,

        /// <summary>
        /// Eject media key. May be used to eject a CD tray, for example.
        /// </summary>
        MediaEject = 129,

        /// <summary>
        /// Record media key.
        /// </summary>
        MediaRecord = 130,

        /// <summary>
        /// F1 key.
        /// </summary>
        F1 = 131,

        /// <summary>
        /// F2 key.
        /// </summary>
        F2 = 132,

        /// <summary>
        /// F3 key.
        /// </summary>
        F3 = 133,

        /// <summary>
        /// F4 key.
        /// </summary>
        F4 = 134,

        /// <summary>
        /// F5 key.
        /// </summary>
        F5 = 135,

        /// <summary>
        /// F6 key.
        /// </summary>
        F6 = 136,

        /// <summary>
        /// F7 key.
        /// </summary>
        F7 = 137,

        /// <summary>
        /// F8 key.
        /// </summary>
        F8 = 138,

        /// <summary>
        /// F9 key.
        /// </summary>
        F9 = 139,

        /// <summary>
        /// F10 key.
        /// </summary>
        F10 = 140,

        /// <summary>
        /// F11 key.
        /// </summary>
        F11 = 141,

        /// <summary>
        /// F12 key.
        /// </summary>
        F12 = 142,

        /// <summary>
        /// Num Lock key. This is the Num Lock key; it is different from <see cref="AndroidKeyCode.Num"/>. This key alters the behavior of other keys on the numeric keypad.
        /// </summary>
        NumLock = 143,

        /// <summary>
        /// Numeric keypad '0' key.
        /// </summary>
        Numpad0 = 144,

        /// <summary>
        /// Numeric keypad '1' key.
        /// </summary>
        Numpad1 = 145,

        /// <summary>
        /// Numeric keypad '2' key.
        /// </summary>
        Numpad2 = 146,

        /// <summary>
        /// Numeric keypad '3' key.
        /// </summary>
        Numpad3 = 147,

        /// <summary>
        /// Numeric keypad '4' key.
        /// </summary>
        Numpad4 = 148,

        /// <summary>
        /// Numeric keypad '5' key.
        /// </summary>
        Numpad5 = 149,

        /// <summary>
        /// 'Numeric keypad '6' key.
        /// </summary>
        Numpad6 = 150,

        /// <summary>
        /// 'Numeric keypad '7' key.
        /// </summary>
        Numpad7 = 151,

        /// <summary>
        /// Numeric keypad '8' key.
        /// </summary>
        Numpad8 = 152,

        /// <summary>
        /// Numeric keypad '9' key.
        /// </summary>
        Numpad9 = 153,

        /// <summary>
        /// Numeric keypad '/' key (for division).
        /// </summary>
        NumpadDivide = 154,

        /// <summary>
        /// Numeric keypad '*' key (for multiplication).
        /// </summary>
        NumpadMultiply = 155,

        /// <summary>
        /// Numeric keypad '-' key (for subtraction).
        /// </summary>
        NumpadSubtract = 156,

        /// <summary>
        /// Numeric keypad '+' key (for addition).
        /// </summary>
        NumpadAdd = 157,

        /// <summary>
        /// Numeric keypad '.' key (for decimals or digit grouping).
        /// </summary>
        NumpadDot = 158,

        /// <summary>
        /// Numeric keypad ',' key (for decimals or digit grouping).
        /// </summary>
        NumpadComma = 159,

        /// <summary>
        /// Numeric keypad Enter key.
        /// </summary>
        NumpadEnter = 160,

        /// <summary>
        /// Numeric keypad '=' key.
        /// </summary>
        NumpadEquals = 161,

        /// <summary>
        /// Numeric keypad '(' key.
        /// </summary>
        NumpadLeftParen = 162,

        /// <summary>
        /// Numeric keypad ')' key.
        /// </summary>
        NumpadRightParen = 163,

        /// <summary>
        /// Volume Mute key. Mutes the speaker, unlike <see cref="AndroidKeyCode.Mute"/>. This key should normally be implemented as a toggle such that the first press mutes the speaker and the second press restores the original volum
        /// </summary>
        VolumeMute = 164,

        /// <summary>
        /// Info key. Common on TV remotes to show additional information related to what is currently being viewed.
        /// </summary>
        Info = 165,

        /// <summary>
        /// Channel up key. On TV remotes, increments the television channel.
        /// </summary>
        ChannelUp = 166,

        /// <summary>
        /// Channel down key. On TV remotes, increments the television channel.
        /// </summary>
        ChannelDown = 167,

        /// <summary>
        /// Zoom in key.
        /// </summary>
        ZoomIn = 168,

        /// <summary>
        /// Zoom out key.
        /// </summary>
        ZoomOut = 169,

        /// <summary>
        /// TV key. On TV remotes, switches to viewing live TV.
        /// </summary>
        Tv = 170,

        /// <summary>
        /// Window key. On TV remotes, toggles picture-in-picture mode or other windowing functions. On Android Wear devices, triggers a display offset.
        /// </summary>
        Window = 171,

        /// <summary>
        /// Guide key. On TV remotes, shows a programming guide.
        /// </summary>
        Guide = 172,

        /// <summary>
        /// DVR key. On some TV remotes, switches to a DVR mode for recorded shows.
        /// </summary>
        Dvr = 173,

        /// <summary>
        /// Bookmark key. On some TV remotes, bookmarks content or web pages.
        /// </summary>
        Bookmark = 174,

        /// <summary>
        /// Toggle captions key. Switches the mode for closed-captioning text, for example during television shows.
        /// </summary>
        Captions = 175,

        /// <summary>
        /// Settings key. Starts the system settings activity.
        /// </summary>
        Settings = 176,

        /// <summary>
        /// TV power key. On HDMI TV panel devices and Android TV devices that don't support HDMI, toggles the power state of the device. On HDMI source devices, toggles the power state of the HDMI-connected TV via HDMI-CEC and makes the source device follow this power state.
        /// </summary>
        TvPower = 177,

        /// <summary>
        /// TV input key. On TV remotes, switches the input on a television screen.
        /// </summary>
        TvInput = 178,

        /// <summary>
        /// Set-top-box power key. On TV remotes, toggles the power on an external Set-top-box.
        /// </summary>
        StbPower = 179,

        /// <summary>
        /// Set-top-box input key. On TV remotes, switches the input mode on an external Set-top-box.
        /// </summary>
        StbInput = 180,

        /// <summary>
        /// A/V Receiver power key. On TV remotes, toggles the power on an external A/V Receiver.
        /// </summary>
        AvrPower = 181,

        /// <summary>
        /// A/V Receiver input key. On TV remotes, switches the input mode on an external A/V Receive
        /// </summary>
        AvrInput = 182,

        /// <summary>
        /// Red "programmable" key. On TV remotes, acts as a contextual/programmable key.
        /// </summary>
        ProgRed = 183,

        /// <summary>
        /// Green "programmable" key. On TV remotes, actsas a contextual/programmable key.
        /// </summary>
        ProgGreen = 184,

        /// <summary>
        /// Yellow "programmable" key. On TV remotes, actsas a contextual/programmable key.
        /// </summary>
        ProgYellow = 185,

        /// <summary>
        /// Blue "programmable" key. On TV remotes, actsas a contextual/programmable key.
        /// </summary>
        ProgBlue = 186,

        /// <summary>
        /// App switch key. Should bring up the application switcher dialog.
        /// </summary>
        AppSwitch = 187,

        /// <summary>
        /// Generic Game Pad Button #1.
        /// </summary>
        Button1 = 188,

        /// <summary>
        /// Generic Game Pad Button #2.
        /// </summary>
        Button2 = 189,

        /// <summary>
        /// Generic Game Pad Button #3.
        /// </summary>
        Button3 = 190,

        /// <summary>
        /// Generic Game Pad Button #4.
        /// </summary>
        Button4 = 191,

        /// <summary>
        /// Generic Game Pad Button #5.
        /// </summary>
        Button5 = 192,

        /// <summary>
        /// Generic Game Pad Button #6.
        /// </summary>
        Button6 = 193,

        /// <summary>
        /// Generic Game Pad Button #7.
        /// </summary>
        Button7 = 194,

        /// <summary>
        /// Generic Game Pad Button #8.
        /// </summary>
        Button8 = 195,

        /// <summary>
        /// Generic Game Pad Button #9.
        /// </summary>
        Button9 = 196,

        /// <summary>
        /// Generic Game Pad Button #10.
        /// </summary>
        Button10 = 197,

        /// <summary>
        /// Generic Game Pad Button #11.
        /// </summary>
        Button11 = 198,

        /// <summary>
        /// Generic Game Pad Button #12.
        /// </summary>
        Button12 = 199,

        /// <summary>
        /// Generic Game Pad Button #13.
        /// </summary>
        Button13 = 200,

        /// <summary>
        /// Generic Game Pad Button #14.
        /// </summary>
        Button14 = 201,

        /// <summary>
        /// Generic Game Pad Button #15.
        /// </summary>
        Button15 = 202,

        /// <summary>
        /// Generic Game Pad Button #16.
        /// </summary>
        Button16 = 203,

        /// <summary>
        /// Language Switch key. Toggles the current input language such as switching between English and Japanese on a QWERTY keyboard. On some devices, the same function may be performed by pressing Shift+Spacebar.
        /// </summary>
        LanguageSwitch = 204,

        /// <summary>
        /// 'Manner Mode key. Toggles silent or vibrate mode on and off to make the device behave more politely in certain settings such as on a crowded train. On some devices, the key may only operate when long-pressed.
        /// </summary>
        MannerMode = 205,

        /// <summary>
        /// 3D Mode key. Toggles the display between 2D and 3D mode.
        /// </summary>
        Mode3D = 206,

        /// <summary>
        /// Contacts special function key. Used to launch an address book application.
        /// </summary>
        Contacts = 207,

        /// <summary>
        /// Calendar special function key. Used to launch a calendar application.
        /// </summary>
        Calendar = 208,

        /// <summary>
        /// Music special function key. Used to launch a music player application.
        /// </summary>
        Music = 209,

        /// <summary>
        /// Calculator special function key. Used to launch a calculator application.
        /// </summary>
        Calculator = 210,

        /// <summary>
        /// Japanese full-width / half-width key.
        /// </summary>
        ZenkakuHankaku = 211,

        /// <summary>
        /// Japanese alphanumeric key.
        /// </summary>
        Eisu = 212,

        /// <summary>
        /// Japanese non-conversion key.
        /// </summary>
        Muhenkan = 213,

        /// <summary>
        /// Japanese conversion key.
        /// </summary>
        Henkan = 214,

        /// <summary>
        /// Japanese katakana / hiragana key.
        /// </summary>
        KatakanaHiragana = 215,

        /// <summary>
        /// Japanese Yen key.
        /// </summary>
        Yen = 216,

        /// <summary>
        /// Japanese Ro key.
        /// </summary>
        Ro = 217,

        /// <summary>
        /// Japanese kana key.
        /// </summary>
        Kana = 218,

        /// <summary>
        /// Assist key. Launches the global assist activity. Not delivered to applications.
        /// </summary>
        Assist = 219,
    }
}

#endif // UNITY_EDITOR || UNITY_ANDROID
