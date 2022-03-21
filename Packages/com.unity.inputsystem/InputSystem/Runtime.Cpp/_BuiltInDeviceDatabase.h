
#pragma once
// Auto generated. Do not edit.

#ifndef INPUT_BINDING_GENERATION
#include "BuiltInControlTypes.h"

// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Enums
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

enum class InputControlTypeBuiltIn
{
    Invalid = 0,
    Button = 1, // 'Button'
    AxisOneWay = 2, // 'Axis One Way [0,1]'
    AxisTwoWay = 3, // 'Axis Two Way [-1,1]'
    DeltaAxisTwoWay = 4, // 'Delta Axis Two Way [-1,1] per actuation'
    Stick = 5, // 'Stick 2D [-1,1]'
    DeltaVector2D = 6, // 'Delta Vector 2D [-1,1] per actuation'
    Position2D = 7, // 'Absolute 2D Vector normalized to [0,1] with surface index'
};

enum class InputDeviceTraitBuiltIn
{
    Invalid = 0,
    ExplicitlyPollableDevice = 1, // 'Explicitly Pollable Device'
    Keyboard = 2, // 'Keyboard'
    Pointer = 3, // 'Pointer'
    Mouse = 4, // 'Mouse'
    Gamepad = 5, // 'Gamepad'
    DualSense = 6, // 'DualSense'
    GenericControls = 7, // 'Generic Controls'
};

enum class InputControlUsageBuiltIn
{
    Invalid = 0,
    Keyboard_EscapeButton = 1, // 'Keyboard/EscapeButton'
    Keyboard_EscapeButton_AsAxisOneWay = 2, // 'Keyboard/EscapeButton/AsAxisOneWay'
    Keyboard_SpaceButton = 3, // 'Keyboard/SpaceButton'
    Keyboard_SpaceButton_AsAxisOneWay = 4, // 'Keyboard/SpaceButton/AsAxisOneWay'
    Keyboard_EnterButton = 5, // 'Keyboard/EnterButton'
    Keyboard_EnterButton_AsAxisOneWay = 6, // 'Keyboard/EnterButton/AsAxisOneWay'
    Keyboard_TabButton = 7, // 'Keyboard/TabButton'
    Keyboard_TabButton_AsAxisOneWay = 8, // 'Keyboard/TabButton/AsAxisOneWay'
    Keyboard_BackquoteButton = 9, // 'Keyboard/BackquoteButton'
    Keyboard_BackquoteButton_AsAxisOneWay = 10, // 'Keyboard/BackquoteButton/AsAxisOneWay'
    Keyboard_QuoteButton = 11, // 'Keyboard/QuoteButton'
    Keyboard_QuoteButton_AsAxisOneWay = 12, // 'Keyboard/QuoteButton/AsAxisOneWay'
    Keyboard_SemicolonButton = 13, // 'Keyboard/SemicolonButton'
    Keyboard_SemicolonButton_AsAxisOneWay = 14, // 'Keyboard/SemicolonButton/AsAxisOneWay'
    Keyboard_CommaButton = 15, // 'Keyboard/CommaButton'
    Keyboard_CommaButton_AsAxisOneWay = 16, // 'Keyboard/CommaButton/AsAxisOneWay'
    Keyboard_PeriodButton = 17, // 'Keyboard/PeriodButton'
    Keyboard_PeriodButton_AsAxisOneWay = 18, // 'Keyboard/PeriodButton/AsAxisOneWay'
    Keyboard_SlashButton = 19, // 'Keyboard/SlashButton'
    Keyboard_SlashButton_AsAxisOneWay = 20, // 'Keyboard/SlashButton/AsAxisOneWay'
    Keyboard_BackslashButton = 21, // 'Keyboard/BackslashButton'
    Keyboard_BackslashButton_AsAxisOneWay = 22, // 'Keyboard/BackslashButton/AsAxisOneWay'
    Keyboard_LeftBracketButton = 23, // 'Keyboard/LeftBracketButton'
    Keyboard_LeftBracketButton_AsAxisOneWay = 24, // 'Keyboard/LeftBracketButton/AsAxisOneWay'
    Keyboard_RightBracketButton = 25, // 'Keyboard/RightBracketButton'
    Keyboard_RightBracketButton_AsAxisOneWay = 26, // 'Keyboard/RightBracketButton/AsAxisOneWay'
    Keyboard_MinusButton = 27, // 'Keyboard/MinusButton'
    Keyboard_MinusButton_AsAxisOneWay = 28, // 'Keyboard/MinusButton/AsAxisOneWay'
    Keyboard_EqualsButton = 29, // 'Keyboard/EqualsButton'
    Keyboard_EqualsButton_AsAxisOneWay = 30, // 'Keyboard/EqualsButton/AsAxisOneWay'
    Keyboard_UpArrowButton = 31, // 'Keyboard/UpArrowButton'
    Keyboard_UpArrowButton_AsAxisOneWay = 32, // 'Keyboard/UpArrowButton/AsAxisOneWay'
    Keyboard_DownArrowButton = 33, // 'Keyboard/DownArrowButton'
    Keyboard_DownArrowButton_AsAxisOneWay = 34, // 'Keyboard/DownArrowButton/AsAxisOneWay'
    Keyboard_LeftArrowButton = 35, // 'Keyboard/LeftArrowButton'
    Keyboard_LeftArrowButton_AsAxisOneWay = 36, // 'Keyboard/LeftArrowButton/AsAxisOneWay'
    Keyboard_RightArrowButton = 37, // 'Keyboard/RightArrowButton'
    Keyboard_RightArrowButton_AsAxisOneWay = 38, // 'Keyboard/RightArrowButton/AsAxisOneWay'
    Keyboard_AButton = 39, // 'Keyboard/AButton'
    Keyboard_AButton_AsAxisOneWay = 40, // 'Keyboard/AButton/AsAxisOneWay'
    Keyboard_BButton = 41, // 'Keyboard/BButton'
    Keyboard_BButton_AsAxisOneWay = 42, // 'Keyboard/BButton/AsAxisOneWay'
    Keyboard_CButton = 43, // 'Keyboard/CButton'
    Keyboard_CButton_AsAxisOneWay = 44, // 'Keyboard/CButton/AsAxisOneWay'
    Keyboard_DButton = 45, // 'Keyboard/DButton'
    Keyboard_DButton_AsAxisOneWay = 46, // 'Keyboard/DButton/AsAxisOneWay'
    Keyboard_EButton = 47, // 'Keyboard/EButton'
    Keyboard_EButton_AsAxisOneWay = 48, // 'Keyboard/EButton/AsAxisOneWay'
    Keyboard_FButton = 49, // 'Keyboard/FButton'
    Keyboard_FButton_AsAxisOneWay = 50, // 'Keyboard/FButton/AsAxisOneWay'
    Keyboard_GButton = 51, // 'Keyboard/GButton'
    Keyboard_GButton_AsAxisOneWay = 52, // 'Keyboard/GButton/AsAxisOneWay'
    Keyboard_HButton = 53, // 'Keyboard/HButton'
    Keyboard_HButton_AsAxisOneWay = 54, // 'Keyboard/HButton/AsAxisOneWay'
    Keyboard_IButton = 55, // 'Keyboard/IButton'
    Keyboard_IButton_AsAxisOneWay = 56, // 'Keyboard/IButton/AsAxisOneWay'
    Keyboard_JButton = 57, // 'Keyboard/JButton'
    Keyboard_JButton_AsAxisOneWay = 58, // 'Keyboard/JButton/AsAxisOneWay'
    Keyboard_KButton = 59, // 'Keyboard/KButton'
    Keyboard_KButton_AsAxisOneWay = 60, // 'Keyboard/KButton/AsAxisOneWay'
    Keyboard_LButton = 61, // 'Keyboard/LButton'
    Keyboard_LButton_AsAxisOneWay = 62, // 'Keyboard/LButton/AsAxisOneWay'
    Keyboard_MButton = 63, // 'Keyboard/MButton'
    Keyboard_MButton_AsAxisOneWay = 64, // 'Keyboard/MButton/AsAxisOneWay'
    Keyboard_NButton = 65, // 'Keyboard/NButton'
    Keyboard_NButton_AsAxisOneWay = 66, // 'Keyboard/NButton/AsAxisOneWay'
    Keyboard_OButton = 67, // 'Keyboard/OButton'
    Keyboard_OButton_AsAxisOneWay = 68, // 'Keyboard/OButton/AsAxisOneWay'
    Keyboard_PButton = 69, // 'Keyboard/PButton'
    Keyboard_PButton_AsAxisOneWay = 70, // 'Keyboard/PButton/AsAxisOneWay'
    Keyboard_QButton = 71, // 'Keyboard/QButton'
    Keyboard_QButton_AsAxisOneWay = 72, // 'Keyboard/QButton/AsAxisOneWay'
    Keyboard_RButton = 73, // 'Keyboard/RButton'
    Keyboard_RButton_AsAxisOneWay = 74, // 'Keyboard/RButton/AsAxisOneWay'
    Keyboard_SButton = 75, // 'Keyboard/SButton'
    Keyboard_SButton_AsAxisOneWay = 76, // 'Keyboard/SButton/AsAxisOneWay'
    Keyboard_TButton = 77, // 'Keyboard/TButton'
    Keyboard_TButton_AsAxisOneWay = 78, // 'Keyboard/TButton/AsAxisOneWay'
    Keyboard_UButton = 79, // 'Keyboard/UButton'
    Keyboard_UButton_AsAxisOneWay = 80, // 'Keyboard/UButton/AsAxisOneWay'
    Keyboard_VButton = 81, // 'Keyboard/VButton'
    Keyboard_VButton_AsAxisOneWay = 82, // 'Keyboard/VButton/AsAxisOneWay'
    Keyboard_WButton = 83, // 'Keyboard/WButton'
    Keyboard_WButton_AsAxisOneWay = 84, // 'Keyboard/WButton/AsAxisOneWay'
    Keyboard_XButton = 85, // 'Keyboard/XButton'
    Keyboard_XButton_AsAxisOneWay = 86, // 'Keyboard/XButton/AsAxisOneWay'
    Keyboard_YButton = 87, // 'Keyboard/YButton'
    Keyboard_YButton_AsAxisOneWay = 88, // 'Keyboard/YButton/AsAxisOneWay'
    Keyboard_ZButton = 89, // 'Keyboard/ZButton'
    Keyboard_ZButton_AsAxisOneWay = 90, // 'Keyboard/ZButton/AsAxisOneWay'
    Keyboard_Digit1Button = 91, // 'Keyboard/Digit1Button'
    Keyboard_Digit1Button_AsAxisOneWay = 92, // 'Keyboard/Digit1Button/AsAxisOneWay'
    Keyboard_Digit2Button = 93, // 'Keyboard/Digit2Button'
    Keyboard_Digit2Button_AsAxisOneWay = 94, // 'Keyboard/Digit2Button/AsAxisOneWay'
    Keyboard_Digit3Button = 95, // 'Keyboard/Digit3Button'
    Keyboard_Digit3Button_AsAxisOneWay = 96, // 'Keyboard/Digit3Button/AsAxisOneWay'
    Keyboard_Digit4Button = 97, // 'Keyboard/Digit4Button'
    Keyboard_Digit4Button_AsAxisOneWay = 98, // 'Keyboard/Digit4Button/AsAxisOneWay'
    Keyboard_Digit5Button = 99, // 'Keyboard/Digit5Button'
    Keyboard_Digit5Button_AsAxisOneWay = 100, // 'Keyboard/Digit5Button/AsAxisOneWay'
    Keyboard_Digit6Button = 101, // 'Keyboard/Digit6Button'
    Keyboard_Digit6Button_AsAxisOneWay = 102, // 'Keyboard/Digit6Button/AsAxisOneWay'
    Keyboard_Digit7Button = 103, // 'Keyboard/Digit7Button'
    Keyboard_Digit7Button_AsAxisOneWay = 104, // 'Keyboard/Digit7Button/AsAxisOneWay'
    Keyboard_Digit8Button = 105, // 'Keyboard/Digit8Button'
    Keyboard_Digit8Button_AsAxisOneWay = 106, // 'Keyboard/Digit8Button/AsAxisOneWay'
    Keyboard_Digit9Button = 107, // 'Keyboard/Digit9Button'
    Keyboard_Digit9Button_AsAxisOneWay = 108, // 'Keyboard/Digit9Button/AsAxisOneWay'
    Keyboard_Digit0Button = 109, // 'Keyboard/Digit0Button'
    Keyboard_Digit0Button_AsAxisOneWay = 110, // 'Keyboard/Digit0Button/AsAxisOneWay'
    Keyboard_LeftShiftButton = 111, // 'Keyboard/LeftShiftButton'
    Keyboard_LeftShiftButton_AsAxisOneWay = 112, // 'Keyboard/LeftShiftButton/AsAxisOneWay'
    Keyboard_RightShiftButton = 113, // 'Keyboard/RightShiftButton'
    Keyboard_RightShiftButton_AsAxisOneWay = 114, // 'Keyboard/RightShiftButton/AsAxisOneWay'
    Keyboard_ShiftButton = 115, // 'Keyboard/ShiftButton'
    Keyboard_ShiftButton_AsAxisOneWay = 116, // 'Keyboard/ShiftButton/AsAxisOneWay'
    Keyboard_LeftAltButton = 117, // 'Keyboard/LeftAltButton'
    Keyboard_LeftAltButton_AsAxisOneWay = 118, // 'Keyboard/LeftAltButton/AsAxisOneWay'
    Keyboard_RightAltButton = 119, // 'Keyboard/RightAltButton'
    Keyboard_RightAltButton_AsAxisOneWay = 120, // 'Keyboard/RightAltButton/AsAxisOneWay'
    Keyboard_AltButton = 121, // 'Keyboard/AltButton'
    Keyboard_AltButton_AsAxisOneWay = 122, // 'Keyboard/AltButton/AsAxisOneWay'
    Keyboard_LeftCtrlButton = 123, // 'Keyboard/LeftCtrlButton'
    Keyboard_LeftCtrlButton_AsAxisOneWay = 124, // 'Keyboard/LeftCtrlButton/AsAxisOneWay'
    Keyboard_RightCtrlButton = 125, // 'Keyboard/RightCtrlButton'
    Keyboard_RightCtrlButton_AsAxisOneWay = 126, // 'Keyboard/RightCtrlButton/AsAxisOneWay'
    Keyboard_CtrlButton = 127, // 'Keyboard/CtrlButton'
    Keyboard_CtrlButton_AsAxisOneWay = 128, // 'Keyboard/CtrlButton/AsAxisOneWay'
    Keyboard_LeftMetaButton = 129, // 'Keyboard/LeftMetaButton'
    Keyboard_LeftMetaButton_AsAxisOneWay = 130, // 'Keyboard/LeftMetaButton/AsAxisOneWay'
    Keyboard_RightMetaButton = 131, // 'Keyboard/RightMetaButton'
    Keyboard_RightMetaButton_AsAxisOneWay = 132, // 'Keyboard/RightMetaButton/AsAxisOneWay'
    Keyboard_ContextMenuButton = 133, // 'Keyboard/ContextMenuButton'
    Keyboard_ContextMenuButton_AsAxisOneWay = 134, // 'Keyboard/ContextMenuButton/AsAxisOneWay'
    Keyboard_BackspaceButton = 135, // 'Keyboard/BackspaceButton'
    Keyboard_BackspaceButton_AsAxisOneWay = 136, // 'Keyboard/BackspaceButton/AsAxisOneWay'
    Keyboard_PageDownButton = 137, // 'Keyboard/PageDownButton'
    Keyboard_PageDownButton_AsAxisOneWay = 138, // 'Keyboard/PageDownButton/AsAxisOneWay'
    Keyboard_PageUpButton = 139, // 'Keyboard/PageUpButton'
    Keyboard_PageUpButton_AsAxisOneWay = 140, // 'Keyboard/PageUpButton/AsAxisOneWay'
    Keyboard_HomeButton = 141, // 'Keyboard/HomeButton'
    Keyboard_HomeButton_AsAxisOneWay = 142, // 'Keyboard/HomeButton/AsAxisOneWay'
    Keyboard_EndButton = 143, // 'Keyboard/EndButton'
    Keyboard_EndButton_AsAxisOneWay = 144, // 'Keyboard/EndButton/AsAxisOneWay'
    Keyboard_InsertButton = 145, // 'Keyboard/InsertButton'
    Keyboard_InsertButton_AsAxisOneWay = 146, // 'Keyboard/InsertButton/AsAxisOneWay'
    Keyboard_DeleteButton = 147, // 'Keyboard/DeleteButton'
    Keyboard_DeleteButton_AsAxisOneWay = 148, // 'Keyboard/DeleteButton/AsAxisOneWay'
    Keyboard_CapsLockButton = 149, // 'Keyboard/CapsLockButton'
    Keyboard_CapsLockButton_AsAxisOneWay = 150, // 'Keyboard/CapsLockButton/AsAxisOneWay'
    Keyboard_NumLockButton = 151, // 'Keyboard/NumLockButton'
    Keyboard_NumLockButton_AsAxisOneWay = 152, // 'Keyboard/NumLockButton/AsAxisOneWay'
    Keyboard_PrintScreenButton = 153, // 'Keyboard/PrintScreenButton'
    Keyboard_PrintScreenButton_AsAxisOneWay = 154, // 'Keyboard/PrintScreenButton/AsAxisOneWay'
    Keyboard_ScrollLockButton = 155, // 'Keyboard/ScrollLockButton'
    Keyboard_ScrollLockButton_AsAxisOneWay = 156, // 'Keyboard/ScrollLockButton/AsAxisOneWay'
    Keyboard_PauseButton = 157, // 'Keyboard/PauseButton'
    Keyboard_PauseButton_AsAxisOneWay = 158, // 'Keyboard/PauseButton/AsAxisOneWay'
    Keyboard_NumpadEnterButton = 159, // 'Keyboard/NumpadEnterButton'
    Keyboard_NumpadEnterButton_AsAxisOneWay = 160, // 'Keyboard/NumpadEnterButton/AsAxisOneWay'
    Keyboard_NumpadDivideButton = 161, // 'Keyboard/NumpadDivideButton'
    Keyboard_NumpadDivideButton_AsAxisOneWay = 162, // 'Keyboard/NumpadDivideButton/AsAxisOneWay'
    Keyboard_NumpadMultiplyButton = 163, // 'Keyboard/NumpadMultiplyButton'
    Keyboard_NumpadMultiplyButton_AsAxisOneWay = 164, // 'Keyboard/NumpadMultiplyButton/AsAxisOneWay'
    Keyboard_NumpadPlusButton = 165, // 'Keyboard/NumpadPlusButton'
    Keyboard_NumpadPlusButton_AsAxisOneWay = 166, // 'Keyboard/NumpadPlusButton/AsAxisOneWay'
    Keyboard_NumpadMinusButton = 167, // 'Keyboard/NumpadMinusButton'
    Keyboard_NumpadMinusButton_AsAxisOneWay = 168, // 'Keyboard/NumpadMinusButton/AsAxisOneWay'
    Keyboard_NumpadPeriodButton = 169, // 'Keyboard/NumpadPeriodButton'
    Keyboard_NumpadPeriodButton_AsAxisOneWay = 170, // 'Keyboard/NumpadPeriodButton/AsAxisOneWay'
    Keyboard_NumpadEqualsButton = 171, // 'Keyboard/NumpadEqualsButton'
    Keyboard_NumpadEqualsButton_AsAxisOneWay = 172, // 'Keyboard/NumpadEqualsButton/AsAxisOneWay'
    Keyboard_Numpad1Button = 173, // 'Keyboard/Numpad1Button'
    Keyboard_Numpad1Button_AsAxisOneWay = 174, // 'Keyboard/Numpad1Button/AsAxisOneWay'
    Keyboard_Numpad2Button = 175, // 'Keyboard/Numpad2Button'
    Keyboard_Numpad2Button_AsAxisOneWay = 176, // 'Keyboard/Numpad2Button/AsAxisOneWay'
    Keyboard_Numpad3Button = 177, // 'Keyboard/Numpad3Button'
    Keyboard_Numpad3Button_AsAxisOneWay = 178, // 'Keyboard/Numpad3Button/AsAxisOneWay'
    Keyboard_Numpad4Button = 179, // 'Keyboard/Numpad4Button'
    Keyboard_Numpad4Button_AsAxisOneWay = 180, // 'Keyboard/Numpad4Button/AsAxisOneWay'
    Keyboard_Numpad5Button = 181, // 'Keyboard/Numpad5Button'
    Keyboard_Numpad5Button_AsAxisOneWay = 182, // 'Keyboard/Numpad5Button/AsAxisOneWay'
    Keyboard_Numpad6Button = 183, // 'Keyboard/Numpad6Button'
    Keyboard_Numpad6Button_AsAxisOneWay = 184, // 'Keyboard/Numpad6Button/AsAxisOneWay'
    Keyboard_Numpad7Button = 185, // 'Keyboard/Numpad7Button'
    Keyboard_Numpad7Button_AsAxisOneWay = 186, // 'Keyboard/Numpad7Button/AsAxisOneWay'
    Keyboard_Numpad8Button = 187, // 'Keyboard/Numpad8Button'
    Keyboard_Numpad8Button_AsAxisOneWay = 188, // 'Keyboard/Numpad8Button/AsAxisOneWay'
    Keyboard_Numpad9Button = 189, // 'Keyboard/Numpad9Button'
    Keyboard_Numpad9Button_AsAxisOneWay = 190, // 'Keyboard/Numpad9Button/AsAxisOneWay'
    Keyboard_Numpad0Button = 191, // 'Keyboard/Numpad0Button'
    Keyboard_Numpad0Button_AsAxisOneWay = 192, // 'Keyboard/Numpad0Button/AsAxisOneWay'
    Keyboard_F1Button = 193, // 'Keyboard/F1Button'
    Keyboard_F1Button_AsAxisOneWay = 194, // 'Keyboard/F1Button/AsAxisOneWay'
    Keyboard_F2Button = 195, // 'Keyboard/F2Button'
    Keyboard_F2Button_AsAxisOneWay = 196, // 'Keyboard/F2Button/AsAxisOneWay'
    Keyboard_F3Button = 197, // 'Keyboard/F3Button'
    Keyboard_F3Button_AsAxisOneWay = 198, // 'Keyboard/F3Button/AsAxisOneWay'
    Keyboard_F4Button = 199, // 'Keyboard/F4Button'
    Keyboard_F4Button_AsAxisOneWay = 200, // 'Keyboard/F4Button/AsAxisOneWay'
    Keyboard_F5Button = 201, // 'Keyboard/F5Button'
    Keyboard_F5Button_AsAxisOneWay = 202, // 'Keyboard/F5Button/AsAxisOneWay'
    Keyboard_F6Button = 203, // 'Keyboard/F6Button'
    Keyboard_F6Button_AsAxisOneWay = 204, // 'Keyboard/F6Button/AsAxisOneWay'
    Keyboard_F7Button = 205, // 'Keyboard/F7Button'
    Keyboard_F7Button_AsAxisOneWay = 206, // 'Keyboard/F7Button/AsAxisOneWay'
    Keyboard_F8Button = 207, // 'Keyboard/F8Button'
    Keyboard_F8Button_AsAxisOneWay = 208, // 'Keyboard/F8Button/AsAxisOneWay'
    Keyboard_F9Button = 209, // 'Keyboard/F9Button'
    Keyboard_F9Button_AsAxisOneWay = 210, // 'Keyboard/F9Button/AsAxisOneWay'
    Keyboard_F10Button = 211, // 'Keyboard/F10Button'
    Keyboard_F10Button_AsAxisOneWay = 212, // 'Keyboard/F10Button/AsAxisOneWay'
    Keyboard_F11Button = 213, // 'Keyboard/F11Button'
    Keyboard_F11Button_AsAxisOneWay = 214, // 'Keyboard/F11Button/AsAxisOneWay'
    Keyboard_F12Button = 215, // 'Keyboard/F12Button'
    Keyboard_F12Button_AsAxisOneWay = 216, // 'Keyboard/F12Button/AsAxisOneWay'
    Keyboard_OEM1Button = 217, // 'Keyboard/OEM1Button'
    Keyboard_OEM1Button_AsAxisOneWay = 218, // 'Keyboard/OEM1Button/AsAxisOneWay'
    Keyboard_OEM2Button = 219, // 'Keyboard/OEM2Button'
    Keyboard_OEM2Button_AsAxisOneWay = 220, // 'Keyboard/OEM2Button/AsAxisOneWay'
    Keyboard_OEM3Button = 221, // 'Keyboard/OEM3Button'
    Keyboard_OEM3Button_AsAxisOneWay = 222, // 'Keyboard/OEM3Button/AsAxisOneWay'
    Keyboard_OEM4Button = 223, // 'Keyboard/OEM4Button'
    Keyboard_OEM4Button_AsAxisOneWay = 224, // 'Keyboard/OEM4Button/AsAxisOneWay'
    Keyboard_OEM5Button = 225, // 'Keyboard/OEM5Button'
    Keyboard_OEM5Button_AsAxisOneWay = 226, // 'Keyboard/OEM5Button/AsAxisOneWay'
    Pointer_PositionPosition2D = 227, // 'Pointer/PositionPosition2D'
    Mouse_MotionDeltaVector2D = 228, // 'Mouse/MotionDeltaVector2D'
    Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay = 229, // 'Mouse/MotionDeltaVector2D/VerticalDeltaAxisTwoWay'
    Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay = 230, // 'Mouse/MotionDeltaVector2D/HorizontalDeltaAxisTwoWay'
    Mouse_MotionDeltaVector2D_LeftButton = 231, // 'Mouse/MotionDeltaVector2D/LeftButton'
    Mouse_MotionDeltaVector2D_UpButton = 232, // 'Mouse/MotionDeltaVector2D/UpButton'
    Mouse_MotionDeltaVector2D_RightButton = 233, // 'Mouse/MotionDeltaVector2D/RightButton'
    Mouse_MotionDeltaVector2D_DownButton = 234, // 'Mouse/MotionDeltaVector2D/DownButton'
    Mouse_ScrollDeltaVector2D = 235, // 'Mouse/ScrollDeltaVector2D'
    Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay = 236, // 'Mouse/ScrollDeltaVector2D/VerticalDeltaAxisTwoWay'
    Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay = 237, // 'Mouse/ScrollDeltaVector2D/HorizontalDeltaAxisTwoWay'
    Mouse_ScrollDeltaVector2D_LeftButton = 238, // 'Mouse/ScrollDeltaVector2D/LeftButton'
    Mouse_ScrollDeltaVector2D_UpButton = 239, // 'Mouse/ScrollDeltaVector2D/UpButton'
    Mouse_ScrollDeltaVector2D_RightButton = 240, // 'Mouse/ScrollDeltaVector2D/RightButton'
    Mouse_ScrollDeltaVector2D_DownButton = 241, // 'Mouse/ScrollDeltaVector2D/DownButton'
    Mouse_LeftButton = 242, // 'Mouse/LeftButton'
    Mouse_LeftButton_AsAxisOneWay = 243, // 'Mouse/LeftButton/AsAxisOneWay'
    Mouse_MiddleButton = 244, // 'Mouse/MiddleButton'
    Mouse_MiddleButton_AsAxisOneWay = 245, // 'Mouse/MiddleButton/AsAxisOneWay'
    Mouse_RightButton = 246, // 'Mouse/RightButton'
    Mouse_RightButton_AsAxisOneWay = 247, // 'Mouse/RightButton/AsAxisOneWay'
    Mouse_BackButton = 248, // 'Mouse/BackButton'
    Mouse_BackButton_AsAxisOneWay = 249, // 'Mouse/BackButton/AsAxisOneWay'
    Mouse_ForwardButton = 250, // 'Mouse/ForwardButton'
    Mouse_ForwardButton_AsAxisOneWay = 251, // 'Mouse/ForwardButton/AsAxisOneWay'
    Gamepad_WestButton = 252, // 'Gamepad/WestButton'
    Gamepad_WestButton_AsAxisOneWay = 253, // 'Gamepad/WestButton/AsAxisOneWay'
    Gamepad_NorthButton = 254, // 'Gamepad/NorthButton'
    Gamepad_NorthButton_AsAxisOneWay = 255, // 'Gamepad/NorthButton/AsAxisOneWay'
    Gamepad_EastButton = 256, // 'Gamepad/EastButton'
    Gamepad_EastButton_AsAxisOneWay = 257, // 'Gamepad/EastButton/AsAxisOneWay'
    Gamepad_SouthButton = 258, // 'Gamepad/SouthButton'
    Gamepad_SouthButton_AsAxisOneWay = 259, // 'Gamepad/SouthButton/AsAxisOneWay'
    Gamepad_LeftStick = 260, // 'Gamepad/LeftStick'
    Gamepad_LeftStick_VerticalAxisTwoWay = 261, // 'Gamepad/LeftStick/VerticalAxisTwoWay'
    Gamepad_LeftStick_HorizontalAxisTwoWay = 262, // 'Gamepad/LeftStick/HorizontalAxisTwoWay'
    Gamepad_LeftStick_LeftAxisOneWay = 263, // 'Gamepad/LeftStick/LeftAxisOneWay'
    Gamepad_LeftStick_UpAxisOneWay = 264, // 'Gamepad/LeftStick/UpAxisOneWay'
    Gamepad_LeftStick_RightAxisOneWay = 265, // 'Gamepad/LeftStick/RightAxisOneWay'
    Gamepad_LeftStick_DownAxisOneWay = 266, // 'Gamepad/LeftStick/DownAxisOneWay'
    Gamepad_LeftStick_LeftButton = 267, // 'Gamepad/LeftStick/LeftButton'
    Gamepad_LeftStick_UpButton = 268, // 'Gamepad/LeftStick/UpButton'
    Gamepad_LeftStick_RightButton = 269, // 'Gamepad/LeftStick/RightButton'
    Gamepad_LeftStick_DownButton = 270, // 'Gamepad/LeftStick/DownButton'
    Gamepad_RightStick = 271, // 'Gamepad/RightStick'
    Gamepad_RightStick_VerticalAxisTwoWay = 272, // 'Gamepad/RightStick/VerticalAxisTwoWay'
    Gamepad_RightStick_HorizontalAxisTwoWay = 273, // 'Gamepad/RightStick/HorizontalAxisTwoWay'
    Gamepad_RightStick_LeftAxisOneWay = 274, // 'Gamepad/RightStick/LeftAxisOneWay'
    Gamepad_RightStick_UpAxisOneWay = 275, // 'Gamepad/RightStick/UpAxisOneWay'
    Gamepad_RightStick_RightAxisOneWay = 276, // 'Gamepad/RightStick/RightAxisOneWay'
    Gamepad_RightStick_DownAxisOneWay = 277, // 'Gamepad/RightStick/DownAxisOneWay'
    Gamepad_RightStick_LeftButton = 278, // 'Gamepad/RightStick/LeftButton'
    Gamepad_RightStick_UpButton = 279, // 'Gamepad/RightStick/UpButton'
    Gamepad_RightStick_RightButton = 280, // 'Gamepad/RightStick/RightButton'
    Gamepad_RightStick_DownButton = 281, // 'Gamepad/RightStick/DownButton'
    Gamepad_LeftStickButton = 282, // 'Gamepad/LeftStickButton'
    Gamepad_LeftStickButton_AsAxisOneWay = 283, // 'Gamepad/LeftStickButton/AsAxisOneWay'
    Gamepad_RightStickButton = 284, // 'Gamepad/RightStickButton'
    Gamepad_RightStickButton_AsAxisOneWay = 285, // 'Gamepad/RightStickButton/AsAxisOneWay'
    Gamepad_DPadStick = 286, // 'Gamepad/DPadStick'
    Gamepad_DPadStick_VerticalAxisTwoWay = 287, // 'Gamepad/DPadStick/VerticalAxisTwoWay'
    Gamepad_DPadStick_HorizontalAxisTwoWay = 288, // 'Gamepad/DPadStick/HorizontalAxisTwoWay'
    Gamepad_DPadStick_LeftAxisOneWay = 289, // 'Gamepad/DPadStick/LeftAxisOneWay'
    Gamepad_DPadStick_UpAxisOneWay = 290, // 'Gamepad/DPadStick/UpAxisOneWay'
    Gamepad_DPadStick_RightAxisOneWay = 291, // 'Gamepad/DPadStick/RightAxisOneWay'
    Gamepad_DPadStick_DownAxisOneWay = 292, // 'Gamepad/DPadStick/DownAxisOneWay'
    Gamepad_DPadStick_LeftButton = 293, // 'Gamepad/DPadStick/LeftButton'
    Gamepad_DPadStick_UpButton = 294, // 'Gamepad/DPadStick/UpButton'
    Gamepad_DPadStick_RightButton = 295, // 'Gamepad/DPadStick/RightButton'
    Gamepad_DPadStick_DownButton = 296, // 'Gamepad/DPadStick/DownButton'
    Gamepad_LeftShoulderButton = 297, // 'Gamepad/LeftShoulderButton'
    Gamepad_LeftShoulderButton_AsAxisOneWay = 298, // 'Gamepad/LeftShoulderButton/AsAxisOneWay'
    Gamepad_RightShoulderButton = 299, // 'Gamepad/RightShoulderButton'
    Gamepad_RightShoulderButton_AsAxisOneWay = 300, // 'Gamepad/RightShoulderButton/AsAxisOneWay'
    Gamepad_LeftTriggerAxisOneWay = 301, // 'Gamepad/LeftTriggerAxisOneWay'
    Gamepad_LeftTriggerAxisOneWay_AsButton = 302, // 'Gamepad/LeftTriggerAxisOneWay/AsButton'
    Gamepad_RightTriggerAxisOneWay = 303, // 'Gamepad/RightTriggerAxisOneWay'
    Gamepad_RightTriggerAxisOneWay_AsButton = 304, // 'Gamepad/RightTriggerAxisOneWay/AsButton'
    DualSense_OptionsButton = 305, // 'DualSense/OptionsButton'
    DualSense_OptionsButton_AsAxisOneWay = 306, // 'DualSense/OptionsButton/AsAxisOneWay'
    DualSense_ShareButton = 307, // 'DualSense/ShareButton'
    DualSense_ShareButton_AsAxisOneWay = 308, // 'DualSense/ShareButton/AsAxisOneWay'
    DualSense_PlaystationButton = 309, // 'DualSense/PlaystationButton'
    DualSense_PlaystationButton_AsAxisOneWay = 310, // 'DualSense/PlaystationButton/AsAxisOneWay'
    DualSense_MicButton = 311, // 'DualSense/MicButton'
    DualSense_MicButton_AsAxisOneWay = 312, // 'DualSense/MicButton/AsAxisOneWay'
    GenericControls_Generic0Button = 313, // 'GenericControls/Generic0Button'
    GenericControls_Generic0Button_AsAxisOneWay = 314, // 'GenericControls/Generic0Button/AsAxisOneWay'
    GenericControls_Generic1Button = 315, // 'GenericControls/Generic1Button'
    GenericControls_Generic1Button_AsAxisOneWay = 316, // 'GenericControls/Generic1Button/AsAxisOneWay'
    GenericControls_Generic2Button = 317, // 'GenericControls/Generic2Button'
    GenericControls_Generic2Button_AsAxisOneWay = 318, // 'GenericControls/Generic2Button/AsAxisOneWay'
    GenericControls_Generic3Button = 319, // 'GenericControls/Generic3Button'
    GenericControls_Generic3Button_AsAxisOneWay = 320, // 'GenericControls/Generic3Button/AsAxisOneWay'
    GenericControls_Generic4Button = 321, // 'GenericControls/Generic4Button'
    GenericControls_Generic4Button_AsAxisOneWay = 322, // 'GenericControls/Generic4Button/AsAxisOneWay'
    GenericControls_Generic5Button = 323, // 'GenericControls/Generic5Button'
    GenericControls_Generic5Button_AsAxisOneWay = 324, // 'GenericControls/Generic5Button/AsAxisOneWay'
    GenericControls_Generic6Button = 325, // 'GenericControls/Generic6Button'
    GenericControls_Generic6Button_AsAxisOneWay = 326, // 'GenericControls/Generic6Button/AsAxisOneWay'
    GenericControls_Generic7Button = 327, // 'GenericControls/Generic7Button'
    GenericControls_Generic7Button_AsAxisOneWay = 328, // 'GenericControls/Generic7Button/AsAxisOneWay'
    GenericControls_Generic8Button = 329, // 'GenericControls/Generic8Button'
    GenericControls_Generic8Button_AsAxisOneWay = 330, // 'GenericControls/Generic8Button/AsAxisOneWay'
    GenericControls_Generic9Button = 331, // 'GenericControls/Generic9Button'
    GenericControls_Generic9Button_AsAxisOneWay = 332, // 'GenericControls/Generic9Button/AsAxisOneWay'
    GenericControls_Generic10Button = 333, // 'GenericControls/Generic10Button'
    GenericControls_Generic10Button_AsAxisOneWay = 334, // 'GenericControls/Generic10Button/AsAxisOneWay'
    GenericControls_Generic11Button = 335, // 'GenericControls/Generic11Button'
    GenericControls_Generic11Button_AsAxisOneWay = 336, // 'GenericControls/Generic11Button/AsAxisOneWay'
    GenericControls_Generic12Button = 337, // 'GenericControls/Generic12Button'
    GenericControls_Generic12Button_AsAxisOneWay = 338, // 'GenericControls/Generic12Button/AsAxisOneWay'
    GenericControls_Generic13Button = 339, // 'GenericControls/Generic13Button'
    GenericControls_Generic13Button_AsAxisOneWay = 340, // 'GenericControls/Generic13Button/AsAxisOneWay'
    GenericControls_Generic14Button = 341, // 'GenericControls/Generic14Button'
    GenericControls_Generic14Button_AsAxisOneWay = 342, // 'GenericControls/Generic14Button/AsAxisOneWay'
    GenericControls_Generic15Button = 343, // 'GenericControls/Generic15Button'
    GenericControls_Generic15Button_AsAxisOneWay = 344, // 'GenericControls/Generic15Button/AsAxisOneWay'
    GenericControls_Generic0AxisOneWay = 345, // 'GenericControls/Generic0AxisOneWay'
    GenericControls_Generic0AxisOneWay_AsButton = 346, // 'GenericControls/Generic0AxisOneWay/AsButton'
    GenericControls_Generic1AxisOneWay = 347, // 'GenericControls/Generic1AxisOneWay'
    GenericControls_Generic1AxisOneWay_AsButton = 348, // 'GenericControls/Generic1AxisOneWay/AsButton'
    GenericControls_Generic2AxisOneWay = 349, // 'GenericControls/Generic2AxisOneWay'
    GenericControls_Generic2AxisOneWay_AsButton = 350, // 'GenericControls/Generic2AxisOneWay/AsButton'
    GenericControls_Generic3AxisOneWay = 351, // 'GenericControls/Generic3AxisOneWay'
    GenericControls_Generic3AxisOneWay_AsButton = 352, // 'GenericControls/Generic3AxisOneWay/AsButton'
    GenericControls_Generic4AxisOneWay = 353, // 'GenericControls/Generic4AxisOneWay'
    GenericControls_Generic4AxisOneWay_AsButton = 354, // 'GenericControls/Generic4AxisOneWay/AsButton'
    GenericControls_Generic5AxisOneWay = 355, // 'GenericControls/Generic5AxisOneWay'
    GenericControls_Generic5AxisOneWay_AsButton = 356, // 'GenericControls/Generic5AxisOneWay/AsButton'
    GenericControls_Generic6AxisOneWay = 357, // 'GenericControls/Generic6AxisOneWay'
    GenericControls_Generic6AxisOneWay_AsButton = 358, // 'GenericControls/Generic6AxisOneWay/AsButton'
    GenericControls_Generic7AxisOneWay = 359, // 'GenericControls/Generic7AxisOneWay'
    GenericControls_Generic7AxisOneWay_AsButton = 360, // 'GenericControls/Generic7AxisOneWay/AsButton'
    GenericControls_Generic8AxisOneWay = 361, // 'GenericControls/Generic8AxisOneWay'
    GenericControls_Generic8AxisOneWay_AsButton = 362, // 'GenericControls/Generic8AxisOneWay/AsButton'
    GenericControls_Generic9AxisOneWay = 363, // 'GenericControls/Generic9AxisOneWay'
    GenericControls_Generic9AxisOneWay_AsButton = 364, // 'GenericControls/Generic9AxisOneWay/AsButton'
    GenericControls_Generic10AxisOneWay = 365, // 'GenericControls/Generic10AxisOneWay'
    GenericControls_Generic10AxisOneWay_AsButton = 366, // 'GenericControls/Generic10AxisOneWay/AsButton'
    GenericControls_Generic11AxisOneWay = 367, // 'GenericControls/Generic11AxisOneWay'
    GenericControls_Generic11AxisOneWay_AsButton = 368, // 'GenericControls/Generic11AxisOneWay/AsButton'
    GenericControls_Generic12AxisOneWay = 369, // 'GenericControls/Generic12AxisOneWay'
    GenericControls_Generic12AxisOneWay_AsButton = 370, // 'GenericControls/Generic12AxisOneWay/AsButton'
    GenericControls_Generic13AxisOneWay = 371, // 'GenericControls/Generic13AxisOneWay'
    GenericControls_Generic13AxisOneWay_AsButton = 372, // 'GenericControls/Generic13AxisOneWay/AsButton'
    GenericControls_Generic14AxisOneWay = 373, // 'GenericControls/Generic14AxisOneWay'
    GenericControls_Generic14AxisOneWay_AsButton = 374, // 'GenericControls/Generic14AxisOneWay/AsButton'
    GenericControls_Generic15AxisOneWay = 375, // 'GenericControls/Generic15AxisOneWay'
    GenericControls_Generic15AxisOneWay_AsButton = 376, // 'GenericControls/Generic15AxisOneWay/AsButton'
    GenericControls_Generic0AxisTwoWay = 377, // 'GenericControls/Generic0AxisTwoWay'
    GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay = 378, // 'GenericControls/Generic0AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay = 379, // 'GenericControls/Generic0AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic0AxisTwoWay_PositiveButton = 380, // 'GenericControls/Generic0AxisTwoWay/PositiveButton'
    GenericControls_Generic0AxisTwoWay_NegativeButton = 381, // 'GenericControls/Generic0AxisTwoWay/NegativeButton'
    GenericControls_Generic1AxisTwoWay = 382, // 'GenericControls/Generic1AxisTwoWay'
    GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay = 383, // 'GenericControls/Generic1AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay = 384, // 'GenericControls/Generic1AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic1AxisTwoWay_PositiveButton = 385, // 'GenericControls/Generic1AxisTwoWay/PositiveButton'
    GenericControls_Generic1AxisTwoWay_NegativeButton = 386, // 'GenericControls/Generic1AxisTwoWay/NegativeButton'
    GenericControls_Generic2AxisTwoWay = 387, // 'GenericControls/Generic2AxisTwoWay'
    GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay = 388, // 'GenericControls/Generic2AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay = 389, // 'GenericControls/Generic2AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic2AxisTwoWay_PositiveButton = 390, // 'GenericControls/Generic2AxisTwoWay/PositiveButton'
    GenericControls_Generic2AxisTwoWay_NegativeButton = 391, // 'GenericControls/Generic2AxisTwoWay/NegativeButton'
    GenericControls_Generic3AxisTwoWay = 392, // 'GenericControls/Generic3AxisTwoWay'
    GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay = 393, // 'GenericControls/Generic3AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay = 394, // 'GenericControls/Generic3AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic3AxisTwoWay_PositiveButton = 395, // 'GenericControls/Generic3AxisTwoWay/PositiveButton'
    GenericControls_Generic3AxisTwoWay_NegativeButton = 396, // 'GenericControls/Generic3AxisTwoWay/NegativeButton'
    GenericControls_Generic4AxisTwoWay = 397, // 'GenericControls/Generic4AxisTwoWay'
    GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay = 398, // 'GenericControls/Generic4AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay = 399, // 'GenericControls/Generic4AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic4AxisTwoWay_PositiveButton = 400, // 'GenericControls/Generic4AxisTwoWay/PositiveButton'
    GenericControls_Generic4AxisTwoWay_NegativeButton = 401, // 'GenericControls/Generic4AxisTwoWay/NegativeButton'
    GenericControls_Generic5AxisTwoWay = 402, // 'GenericControls/Generic5AxisTwoWay'
    GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay = 403, // 'GenericControls/Generic5AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay = 404, // 'GenericControls/Generic5AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic5AxisTwoWay_PositiveButton = 405, // 'GenericControls/Generic5AxisTwoWay/PositiveButton'
    GenericControls_Generic5AxisTwoWay_NegativeButton = 406, // 'GenericControls/Generic5AxisTwoWay/NegativeButton'
    GenericControls_Generic6AxisTwoWay = 407, // 'GenericControls/Generic6AxisTwoWay'
    GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay = 408, // 'GenericControls/Generic6AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay = 409, // 'GenericControls/Generic6AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic6AxisTwoWay_PositiveButton = 410, // 'GenericControls/Generic6AxisTwoWay/PositiveButton'
    GenericControls_Generic6AxisTwoWay_NegativeButton = 411, // 'GenericControls/Generic6AxisTwoWay/NegativeButton'
    GenericControls_Generic7AxisTwoWay = 412, // 'GenericControls/Generic7AxisTwoWay'
    GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay = 413, // 'GenericControls/Generic7AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay = 414, // 'GenericControls/Generic7AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic7AxisTwoWay_PositiveButton = 415, // 'GenericControls/Generic7AxisTwoWay/PositiveButton'
    GenericControls_Generic7AxisTwoWay_NegativeButton = 416, // 'GenericControls/Generic7AxisTwoWay/NegativeButton'
    GenericControls_Generic8AxisTwoWay = 417, // 'GenericControls/Generic8AxisTwoWay'
    GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay = 418, // 'GenericControls/Generic8AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay = 419, // 'GenericControls/Generic8AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic8AxisTwoWay_PositiveButton = 420, // 'GenericControls/Generic8AxisTwoWay/PositiveButton'
    GenericControls_Generic8AxisTwoWay_NegativeButton = 421, // 'GenericControls/Generic8AxisTwoWay/NegativeButton'
    GenericControls_Generic9AxisTwoWay = 422, // 'GenericControls/Generic9AxisTwoWay'
    GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay = 423, // 'GenericControls/Generic9AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay = 424, // 'GenericControls/Generic9AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic9AxisTwoWay_PositiveButton = 425, // 'GenericControls/Generic9AxisTwoWay/PositiveButton'
    GenericControls_Generic9AxisTwoWay_NegativeButton = 426, // 'GenericControls/Generic9AxisTwoWay/NegativeButton'
    GenericControls_Generic10AxisTwoWay = 427, // 'GenericControls/Generic10AxisTwoWay'
    GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay = 428, // 'GenericControls/Generic10AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay = 429, // 'GenericControls/Generic10AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic10AxisTwoWay_PositiveButton = 430, // 'GenericControls/Generic10AxisTwoWay/PositiveButton'
    GenericControls_Generic10AxisTwoWay_NegativeButton = 431, // 'GenericControls/Generic10AxisTwoWay/NegativeButton'
    GenericControls_Generic11AxisTwoWay = 432, // 'GenericControls/Generic11AxisTwoWay'
    GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay = 433, // 'GenericControls/Generic11AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay = 434, // 'GenericControls/Generic11AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic11AxisTwoWay_PositiveButton = 435, // 'GenericControls/Generic11AxisTwoWay/PositiveButton'
    GenericControls_Generic11AxisTwoWay_NegativeButton = 436, // 'GenericControls/Generic11AxisTwoWay/NegativeButton'
    GenericControls_Generic12AxisTwoWay = 437, // 'GenericControls/Generic12AxisTwoWay'
    GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay = 438, // 'GenericControls/Generic12AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay = 439, // 'GenericControls/Generic12AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic12AxisTwoWay_PositiveButton = 440, // 'GenericControls/Generic12AxisTwoWay/PositiveButton'
    GenericControls_Generic12AxisTwoWay_NegativeButton = 441, // 'GenericControls/Generic12AxisTwoWay/NegativeButton'
    GenericControls_Generic13AxisTwoWay = 442, // 'GenericControls/Generic13AxisTwoWay'
    GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay = 443, // 'GenericControls/Generic13AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay = 444, // 'GenericControls/Generic13AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic13AxisTwoWay_PositiveButton = 445, // 'GenericControls/Generic13AxisTwoWay/PositiveButton'
    GenericControls_Generic13AxisTwoWay_NegativeButton = 446, // 'GenericControls/Generic13AxisTwoWay/NegativeButton'
    GenericControls_Generic14AxisTwoWay = 447, // 'GenericControls/Generic14AxisTwoWay'
    GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay = 448, // 'GenericControls/Generic14AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay = 449, // 'GenericControls/Generic14AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic14AxisTwoWay_PositiveButton = 450, // 'GenericControls/Generic14AxisTwoWay/PositiveButton'
    GenericControls_Generic14AxisTwoWay_NegativeButton = 451, // 'GenericControls/Generic14AxisTwoWay/NegativeButton'
    GenericControls_Generic15AxisTwoWay = 452, // 'GenericControls/Generic15AxisTwoWay'
    GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay = 453, // 'GenericControls/Generic15AxisTwoWay/PositiveAxisOneWay'
    GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay = 454, // 'GenericControls/Generic15AxisTwoWay/NegativeAxisOneWay'
    GenericControls_Generic15AxisTwoWay_PositiveButton = 455, // 'GenericControls/Generic15AxisTwoWay/PositiveButton'
    GenericControls_Generic15AxisTwoWay_NegativeButton = 456, // 'GenericControls/Generic15AxisTwoWay/NegativeButton'
};

enum class InputDeviceBuiltIn
{
    Invalid = 0,
    KeyboardWindows = 1, // 'Keyboard (Windows)'
    MouseMacOS = 2, // 'Mouse (macOS)'
    WindowsGamingInputGamepad = 3, // 'Gamepad (Windows.Gaming.Input)'
};

// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Control Types
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----


struct InputButtonControlRef;
struct InputDerivedButtonControlRef;
struct InputAxisOneWayControlRef;
struct InputDerivedAxisOneWayControlRef;
struct InputAxisTwoWayControlRef;
struct InputDerivedAxisTwoWayControlRef;
struct InputDeltaAxisTwoWayControlRef;
struct InputDerivedDeltaAxisTwoWayControlRef;
struct InputStickControlRef;
struct InputDerivedStickControlRef;
struct InputDeltaVector2DControlRef;
struct InputDerivedDeltaVector2DControlRef;
struct InputPosition2DControlRef;
struct InputDerivedPosition2DControlRef;


struct InputDerivedButtonControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)};
    typedef InputButtonControlSample SampleType;
    typedef InputButtonControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedButtonControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedButtonControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedButtonControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputButtonControlSample sample) const
    {
        InputButtonControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputButtonControlSample* samples, const uint32_t count) const
    {
        InputButtonControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputButtonControlSample* samples, const uint32_t count) const
    {
        InputButtonControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputButtonControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputButtonControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputButtonControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputButtonControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputButtonControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputButtonControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedAxisOneWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)};
    typedef InputAxisOneWayControlSample SampleType;
    typedef InputAxisOneWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedAxisOneWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedAxisOneWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedAxisOneWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputAxisOneWayControlSample sample) const
    {
        InputAxisOneWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputAxisOneWayControlSample* samples, const uint32_t count) const
    {
        InputAxisOneWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputAxisOneWayControlSample* samples, const uint32_t count) const
    {
        InputAxisOneWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputAxisOneWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputAxisOneWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputAxisOneWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputAxisOneWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputAxisOneWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputAxisOneWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedAxisTwoWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)};
    typedef InputAxisTwoWayControlSample SampleType;
    typedef InputAxisTwoWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedAxisTwoWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedAxisTwoWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedAxisTwoWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputAxisTwoWayControlSample sample) const
    {
        InputAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputAxisTwoWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputAxisTwoWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputAxisTwoWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputAxisTwoWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputAxisTwoWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputAxisTwoWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputAxisTwoWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedDeltaAxisTwoWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)};
    typedef InputDeltaAxisTwoWayControlSample SampleType;
    typedef InputDeltaAxisTwoWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedDeltaAxisTwoWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedDeltaAxisTwoWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedDeltaAxisTwoWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputDeltaAxisTwoWayControlSample sample) const
    {
        InputDeltaAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputDeltaAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputDeltaAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputDeltaAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputDeltaAxisTwoWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputDeltaAxisTwoWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputDeltaAxisTwoWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputDeltaAxisTwoWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputDeltaAxisTwoWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputDeltaAxisTwoWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputDeltaAxisTwoWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedStickControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)};
    typedef InputStickControlSample SampleType;
    typedef InputStickControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedStickControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedStickControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedStickControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputStickControlSample sample) const
    {
        InputStickControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputStickControlSample* samples, const uint32_t count) const
    {
        InputStickControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputStickControlSample* samples, const uint32_t count) const
    {
        InputStickControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputStickControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputStickControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputStickControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputStickControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputStickControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputStickControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedDeltaVector2DControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaVector2D)};
    typedef InputDeltaVector2DControlSample SampleType;
    typedef InputDeltaVector2DControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedDeltaVector2DControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedDeltaVector2DControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedDeltaVector2DControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputDeltaVector2DControlSample sample) const
    {
        InputDeltaVector2DControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputDeltaVector2DControlSample* samples, const uint32_t count) const
    {
        InputDeltaVector2DControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputDeltaVector2DControlSample* samples, const uint32_t count) const
    {
        InputDeltaVector2DControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputDeltaVector2DControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputDeltaVector2DControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputDeltaVector2DControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputDeltaVector2DControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputDeltaVector2DControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputDeltaVector2DControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};

struct InputDerivedPosition2DControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Position2D)};
    typedef uint8_t SampleType;
    typedef uint8_t StateType;
 
    InputControlRef controlRef;

    static inline InputDerivedPosition2DControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerivedPosition2DControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerivedPosition2DControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const uint8_t sample) const
    {
        _TodoIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const uint8_t* samples, const uint32_t count) const
    {
        _TodoIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const uint8_t* samples, const uint32_t count) const
    {
        _TodoIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline uint8_t GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<uint8_t*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        uint8_t sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<uint8_t*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        uint8_t* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<uint8_t*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};



struct InputButtonControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)};
    typedef InputButtonControlSample SampleType;
    typedef InputButtonControlState StateType;
 
    InputControlRef controlRef;

    static inline InputButtonControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputButtonControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputButtonControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputButtonControlSample sample) const
    {
        InputButtonControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputButtonControlSample* samples, const uint32_t count) const
    {
        InputButtonControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputButtonControlSample* samples, const uint32_t count) const
    {
        InputButtonControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputButtonControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputButtonControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputButtonControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputButtonControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputButtonControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputButtonControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedAxisOneWayControlRef AsAxisOneWay() const;

    enum class AxisOneWays
    {
        As,
    };

    inline const InputDerivedAxisOneWayControlRef operator[](const AxisOneWays value) const;
};

struct InputAxisOneWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)};
    typedef InputAxisOneWayControlSample SampleType;
    typedef InputAxisOneWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputAxisOneWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputAxisOneWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputAxisOneWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputAxisOneWayControlSample sample) const
    {
        InputAxisOneWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputAxisOneWayControlSample* samples, const uint32_t count) const
    {
        InputAxisOneWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputAxisOneWayControlSample* samples, const uint32_t count) const
    {
        InputAxisOneWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputAxisOneWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputAxisOneWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputAxisOneWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputAxisOneWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputAxisOneWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputAxisOneWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedButtonControlRef AsButton() const;

    enum class Buttons
    {
        As,
    };

    inline const InputDerivedButtonControlRef operator[](const Buttons value) const;
};

struct InputAxisTwoWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)};
    typedef InputAxisTwoWayControlSample SampleType;
    typedef InputAxisTwoWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputAxisTwoWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputAxisTwoWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputAxisTwoWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputAxisTwoWayControlSample sample) const
    {
        InputAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputAxisTwoWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputAxisTwoWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputAxisTwoWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputAxisTwoWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputAxisTwoWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputAxisTwoWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputAxisTwoWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedAxisOneWayControlRef PositiveAxisOneWay() const;
    inline const InputDerivedAxisOneWayControlRef NegativeAxisOneWay() const;
    inline const InputDerivedButtonControlRef PositiveButton() const;
    inline const InputDerivedButtonControlRef NegativeButton() const;

    enum class AxisOneWays
    {
        Positive,
        Negative,
    };

    inline const InputDerivedAxisOneWayControlRef operator[](const AxisOneWays value) const;

    enum class Buttons
    {
        Positive,
        Negative,
    };

    inline const InputDerivedButtonControlRef operator[](const Buttons value) const;
};

struct InputDeltaAxisTwoWayControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)};
    typedef InputDeltaAxisTwoWayControlSample SampleType;
    typedef InputDeltaAxisTwoWayControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDeltaAxisTwoWayControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDeltaAxisTwoWayControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDeltaAxisTwoWayControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputDeltaAxisTwoWayControlSample sample) const
    {
        InputDeltaAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputDeltaAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputDeltaAxisTwoWayControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputDeltaAxisTwoWayControlSample* samples, const uint32_t count) const
    {
        InputDeltaAxisTwoWayControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputDeltaAxisTwoWayControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputDeltaAxisTwoWayControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputDeltaAxisTwoWayControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputDeltaAxisTwoWayControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputDeltaAxisTwoWayControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputDeltaAxisTwoWayControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedButtonControlRef PositiveButton() const;
    inline const InputDerivedButtonControlRef NegativeButton() const;

    enum class Buttons
    {
        Positive,
        Negative,
    };

    inline const InputDerivedButtonControlRef operator[](const Buttons value) const;
};

struct InputStickControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)};
    typedef InputStickControlSample SampleType;
    typedef InputStickControlState StateType;
 
    InputControlRef controlRef;

    static inline InputStickControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputStickControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputStickControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputStickControlSample sample) const
    {
        InputStickControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputStickControlSample* samples, const uint32_t count) const
    {
        InputStickControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputStickControlSample* samples, const uint32_t count) const
    {
        InputStickControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputStickControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputStickControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputStickControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputStickControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputStickControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputStickControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedAxisTwoWayControlRef VerticalAxisTwoWay() const;
    inline const InputDerivedAxisTwoWayControlRef HorizontalAxisTwoWay() const;
    inline const InputDerivedAxisOneWayControlRef LeftAxisOneWay() const;
    inline const InputDerivedAxisOneWayControlRef UpAxisOneWay() const;
    inline const InputDerivedAxisOneWayControlRef RightAxisOneWay() const;
    inline const InputDerivedAxisOneWayControlRef DownAxisOneWay() const;
    inline const InputDerivedButtonControlRef LeftButton() const;
    inline const InputDerivedButtonControlRef UpButton() const;
    inline const InputDerivedButtonControlRef RightButton() const;
    inline const InputDerivedButtonControlRef DownButton() const;

    enum class AxisTwoWays
    {
        Vertical,
        Horizontal,
    };

    inline const InputDerivedAxisTwoWayControlRef operator[](const AxisTwoWays value) const;

    enum class AxisOneWays
    {
        Left,
        Up,
        Right,
        Down,
    };

    inline const InputDerivedAxisOneWayControlRef operator[](const AxisOneWays value) const;

    enum class Buttons
    {
        Left,
        Up,
        Right,
        Down,
    };

    inline const InputDerivedButtonControlRef operator[](const Buttons value) const;
};

struct InputDeltaVector2DControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaVector2D)};
    typedef InputDeltaVector2DControlSample SampleType;
    typedef InputDeltaVector2DControlState StateType;
 
    InputControlRef controlRef;

    static inline InputDeltaVector2DControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDeltaVector2DControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDeltaVector2DControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const InputDeltaVector2DControlSample sample) const
    {
        InputDeltaVector2DControlIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const InputDeltaVector2DControlSample* samples, const uint32_t count) const
    {
        InputDeltaVector2DControlIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const InputDeltaVector2DControlSample* samples, const uint32_t count) const
    {
        InputDeltaVector2DControlIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline InputDeltaVector2DControlState GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<InputDeltaVector2DControlState*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        InputDeltaVector2DControlSample sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<InputDeltaVector2DControlSample*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        InputDeltaVector2DControlSample* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<InputDeltaVector2DControlSample*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


    inline const InputDerivedDeltaAxisTwoWayControlRef VerticalDeltaAxisTwoWay() const;
    inline const InputDerivedDeltaAxisTwoWayControlRef HorizontalDeltaAxisTwoWay() const;
    inline const InputDerivedButtonControlRef LeftButton() const;
    inline const InputDerivedButtonControlRef UpButton() const;
    inline const InputDerivedButtonControlRef RightButton() const;
    inline const InputDerivedButtonControlRef DownButton() const;

    enum class DeltaAxisTwoWays
    {
        Vertical,
        Horizontal,
    };

    inline const InputDerivedDeltaAxisTwoWayControlRef operator[](const DeltaAxisTwoWays value) const;

    enum class Buttons
    {
        Left,
        Up,
        Right,
        Down,
    };

    inline const InputDerivedButtonControlRef operator[](const Buttons value) const;
};

struct InputPosition2DControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::Position2D)};
    typedef uint8_t SampleType;
    typedef uint8_t StateType;
 
    InputControlRef controlRef;

    static inline InputPosition2DControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputPosition2DControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputPosition2DControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const uint8_t sample) const
    {
        _TodoIngress(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const uint8_t* samples, const uint32_t count) const
    {
        _TodoIngress(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const uint8_t* samples, const uint32_t count) const
    {
        _TodoIngress(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline uint8_t GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<uint8_t*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        uint8_t sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<uint8_t*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        uint8_t* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<uint8_t*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }


};


inline const InputDerivedAxisOneWayControlRef InputButtonControlRef::AsAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }

inline const InputDerivedAxisOneWayControlRef InputButtonControlRef::operator[](const InputButtonControlRef::AxisOneWays value) const
{
    switch(value)
    {
    case InputButtonControlRef::AxisOneWays::As: return AsAxisOneWay();
    default: InputAssert(false, "Unknown control"); return InputDerivedAxisOneWayControlRef::Setup(InputControlRefInvalid);
    }
}

inline const InputDerivedButtonControlRef InputAxisOneWayControlRef::AsButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }

inline const InputDerivedButtonControlRef InputAxisOneWayControlRef::operator[](const InputAxisOneWayControlRef::Buttons value) const
{
    switch(value)
    {
    case InputAxisOneWayControlRef::Buttons::As: return AsButton();
    default: InputAssert(false, "Unknown control"); return InputDerivedButtonControlRef::Setup(InputControlRefInvalid);
    }
}

inline const InputDerivedAxisOneWayControlRef InputAxisTwoWayControlRef::PositiveAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }
inline const InputDerivedAxisOneWayControlRef InputAxisTwoWayControlRef::NegativeAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(2)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputAxisTwoWayControlRef::PositiveButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(3)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputAxisTwoWayControlRef::NegativeButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(4)}, controlRef.deviceRef); }

inline const InputDerivedAxisOneWayControlRef InputAxisTwoWayControlRef::operator[](const InputAxisTwoWayControlRef::AxisOneWays value) const
{
    switch(value)
    {
    case InputAxisTwoWayControlRef::AxisOneWays::Positive: return PositiveAxisOneWay();
    case InputAxisTwoWayControlRef::AxisOneWays::Negative: return NegativeAxisOneWay();
    default: InputAssert(false, "Unknown control"); return InputDerivedAxisOneWayControlRef::Setup(InputControlRefInvalid);
    }
}
inline const InputDerivedButtonControlRef InputAxisTwoWayControlRef::operator[](const InputAxisTwoWayControlRef::Buttons value) const
{
    switch(value)
    {
    case InputAxisTwoWayControlRef::Buttons::Positive: return PositiveButton();
    case InputAxisTwoWayControlRef::Buttons::Negative: return NegativeButton();
    default: InputAssert(false, "Unknown control"); return InputDerivedButtonControlRef::Setup(InputControlRefInvalid);
    }
}

inline const InputDerivedButtonControlRef InputDeltaAxisTwoWayControlRef::PositiveButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputDeltaAxisTwoWayControlRef::NegativeButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(2)}, controlRef.deviceRef); }

inline const InputDerivedButtonControlRef InputDeltaAxisTwoWayControlRef::operator[](const InputDeltaAxisTwoWayControlRef::Buttons value) const
{
    switch(value)
    {
    case InputDeltaAxisTwoWayControlRef::Buttons::Positive: return PositiveButton();
    case InputDeltaAxisTwoWayControlRef::Buttons::Negative: return NegativeButton();
    default: InputAssert(false, "Unknown control"); return InputDerivedButtonControlRef::Setup(InputControlRefInvalid);
    }
}

inline const InputDerivedAxisTwoWayControlRef InputStickControlRef::VerticalAxisTwoWay() const { return InputDerivedAxisTwoWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }
inline const InputDerivedAxisTwoWayControlRef InputStickControlRef::HorizontalAxisTwoWay() const { return InputDerivedAxisTwoWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(2)}, controlRef.deviceRef); }
inline const InputDerivedAxisOneWayControlRef InputStickControlRef::LeftAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(3)}, controlRef.deviceRef); }
inline const InputDerivedAxisOneWayControlRef InputStickControlRef::UpAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(4)}, controlRef.deviceRef); }
inline const InputDerivedAxisOneWayControlRef InputStickControlRef::RightAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(5)}, controlRef.deviceRef); }
inline const InputDerivedAxisOneWayControlRef InputStickControlRef::DownAxisOneWay() const { return InputDerivedAxisOneWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(6)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputStickControlRef::LeftButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(7)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputStickControlRef::UpButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(8)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputStickControlRef::RightButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(9)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputStickControlRef::DownButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(10)}, controlRef.deviceRef); }

inline const InputDerivedAxisTwoWayControlRef InputStickControlRef::operator[](const InputStickControlRef::AxisTwoWays value) const
{
    switch(value)
    {
    case InputStickControlRef::AxisTwoWays::Vertical: return VerticalAxisTwoWay();
    case InputStickControlRef::AxisTwoWays::Horizontal: return HorizontalAxisTwoWay();
    default: InputAssert(false, "Unknown control"); return InputDerivedAxisTwoWayControlRef::Setup(InputControlRefInvalid);
    }
}
inline const InputDerivedAxisOneWayControlRef InputStickControlRef::operator[](const InputStickControlRef::AxisOneWays value) const
{
    switch(value)
    {
    case InputStickControlRef::AxisOneWays::Left: return LeftAxisOneWay();
    case InputStickControlRef::AxisOneWays::Up: return UpAxisOneWay();
    case InputStickControlRef::AxisOneWays::Right: return RightAxisOneWay();
    case InputStickControlRef::AxisOneWays::Down: return DownAxisOneWay();
    default: InputAssert(false, "Unknown control"); return InputDerivedAxisOneWayControlRef::Setup(InputControlRefInvalid);
    }
}
inline const InputDerivedButtonControlRef InputStickControlRef::operator[](const InputStickControlRef::Buttons value) const
{
    switch(value)
    {
    case InputStickControlRef::Buttons::Left: return LeftButton();
    case InputStickControlRef::Buttons::Up: return UpButton();
    case InputStickControlRef::Buttons::Right: return RightButton();
    case InputStickControlRef::Buttons::Down: return DownButton();
    default: InputAssert(false, "Unknown control"); return InputDerivedButtonControlRef::Setup(InputControlRefInvalid);
    }
}

inline const InputDerivedDeltaAxisTwoWayControlRef InputDeltaVector2DControlRef::VerticalDeltaAxisTwoWay() const { return InputDerivedDeltaAxisTwoWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(1)}, controlRef.deviceRef); }
inline const InputDerivedDeltaAxisTwoWayControlRef InputDeltaVector2DControlRef::HorizontalDeltaAxisTwoWay() const { return InputDerivedDeltaAxisTwoWayControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(2)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputDeltaVector2DControlRef::LeftButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(3)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputDeltaVector2DControlRef::UpButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(4)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputDeltaVector2DControlRef::RightButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(5)}, controlRef.deviceRef); }
inline const InputDerivedButtonControlRef InputDeltaVector2DControlRef::DownButton() const { return InputDerivedButtonControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>(6)}, controlRef.deviceRef); }

inline const InputDerivedDeltaAxisTwoWayControlRef InputDeltaVector2DControlRef::operator[](const InputDeltaVector2DControlRef::DeltaAxisTwoWays value) const
{
    switch(value)
    {
    case InputDeltaVector2DControlRef::DeltaAxisTwoWays::Vertical: return VerticalDeltaAxisTwoWay();
    case InputDeltaVector2DControlRef::DeltaAxisTwoWays::Horizontal: return HorizontalDeltaAxisTwoWay();
    default: InputAssert(false, "Unknown control"); return InputDerivedDeltaAxisTwoWayControlRef::Setup(InputControlRefInvalid);
    }
}
inline const InputDerivedButtonControlRef InputDeltaVector2DControlRef::operator[](const InputDeltaVector2DControlRef::Buttons value) const
{
    switch(value)
    {
    case InputDeltaVector2DControlRef::Buttons::Left: return LeftButton();
    case InputDeltaVector2DControlRef::Buttons::Up: return UpButton();
    case InputDeltaVector2DControlRef::Buttons::Right: return RightButton();
    case InputDeltaVector2DControlRef::Buttons::Down: return DownButton();
    default: InputAssert(false, "Unknown control"); return InputDerivedButtonControlRef::Setup(InputControlRefInvalid);
    }
}



// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Device Traits
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

struct InputExplicitlyPollableDevice
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::ExplicitlyPollableDevice)};
    InputDeviceRef deviceRef;

    static inline InputExplicitlyPollableDevice Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputExplicitlyPollableDevice r = {};
        r.deviceRef = deviceRef;
        r.poll = nullptr;
        return r;
    }

    typedef void (*PollType)();

    PollType poll;

};

struct InputKeyboard
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Keyboard)};
    InputDeviceRef deviceRef;

    static inline InputKeyboard Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputKeyboard r = {};
        r.deviceRef = deviceRef;
        return r;
    }
    inline const InputButtonControlRef EscapeButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton)}, deviceRef); }
    inline const InputButtonControlRef SpaceButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton)}, deviceRef); }
    inline const InputButtonControlRef EnterButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton)}, deviceRef); }
    inline const InputButtonControlRef TabButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton)}, deviceRef); }
    inline const InputButtonControlRef BackquoteButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton)}, deviceRef); }
    inline const InputButtonControlRef QuoteButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton)}, deviceRef); }
    inline const InputButtonControlRef SemicolonButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton)}, deviceRef); }
    inline const InputButtonControlRef CommaButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton)}, deviceRef); }
    inline const InputButtonControlRef PeriodButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton)}, deviceRef); }
    inline const InputButtonControlRef SlashButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton)}, deviceRef); }
    inline const InputButtonControlRef BackslashButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton)}, deviceRef); }
    inline const InputButtonControlRef LeftBracketButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton)}, deviceRef); }
    inline const InputButtonControlRef RightBracketButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton)}, deviceRef); }
    inline const InputButtonControlRef MinusButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton)}, deviceRef); }
    inline const InputButtonControlRef EqualsButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton)}, deviceRef); }
    inline const InputButtonControlRef UpArrowButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton)}, deviceRef); }
    inline const InputButtonControlRef DownArrowButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton)}, deviceRef); }
    inline const InputButtonControlRef LeftArrowButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton)}, deviceRef); }
    inline const InputButtonControlRef RightArrowButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton)}, deviceRef); }
    inline const InputButtonControlRef AButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton)}, deviceRef); }
    inline const InputButtonControlRef BButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton)}, deviceRef); }
    inline const InputButtonControlRef CButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton)}, deviceRef); }
    inline const InputButtonControlRef DButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton)}, deviceRef); }
    inline const InputButtonControlRef EButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton)}, deviceRef); }
    inline const InputButtonControlRef FButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton)}, deviceRef); }
    inline const InputButtonControlRef GButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton)}, deviceRef); }
    inline const InputButtonControlRef HButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton)}, deviceRef); }
    inline const InputButtonControlRef IButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton)}, deviceRef); }
    inline const InputButtonControlRef JButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton)}, deviceRef); }
    inline const InputButtonControlRef KButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton)}, deviceRef); }
    inline const InputButtonControlRef LButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton)}, deviceRef); }
    inline const InputButtonControlRef MButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton)}, deviceRef); }
    inline const InputButtonControlRef NButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton)}, deviceRef); }
    inline const InputButtonControlRef OButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton)}, deviceRef); }
    inline const InputButtonControlRef PButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton)}, deviceRef); }
    inline const InputButtonControlRef QButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton)}, deviceRef); }
    inline const InputButtonControlRef RButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton)}, deviceRef); }
    inline const InputButtonControlRef SButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton)}, deviceRef); }
    inline const InputButtonControlRef TButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton)}, deviceRef); }
    inline const InputButtonControlRef UButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton)}, deviceRef); }
    inline const InputButtonControlRef VButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton)}, deviceRef); }
    inline const InputButtonControlRef WButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton)}, deviceRef); }
    inline const InputButtonControlRef XButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton)}, deviceRef); }
    inline const InputButtonControlRef YButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton)}, deviceRef); }
    inline const InputButtonControlRef ZButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton)}, deviceRef); }
    inline const InputButtonControlRef Digit1Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button)}, deviceRef); }
    inline const InputButtonControlRef Digit2Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button)}, deviceRef); }
    inline const InputButtonControlRef Digit3Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button)}, deviceRef); }
    inline const InputButtonControlRef Digit4Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button)}, deviceRef); }
    inline const InputButtonControlRef Digit5Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button)}, deviceRef); }
    inline const InputButtonControlRef Digit6Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button)}, deviceRef); }
    inline const InputButtonControlRef Digit7Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button)}, deviceRef); }
    inline const InputButtonControlRef Digit8Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button)}, deviceRef); }
    inline const InputButtonControlRef Digit9Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button)}, deviceRef); }
    inline const InputButtonControlRef Digit0Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button)}, deviceRef); }
    inline const InputButtonControlRef LeftShiftButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton)}, deviceRef); }
    inline const InputButtonControlRef RightShiftButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton)}, deviceRef); }
    inline const InputButtonControlRef ShiftButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton)}, deviceRef); }
    inline const InputButtonControlRef LeftAltButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton)}, deviceRef); }
    inline const InputButtonControlRef RightAltButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton)}, deviceRef); }
    inline const InputButtonControlRef AltButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton)}, deviceRef); }
    inline const InputButtonControlRef LeftCtrlButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton)}, deviceRef); }
    inline const InputButtonControlRef RightCtrlButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton)}, deviceRef); }
    inline const InputButtonControlRef CtrlButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton)}, deviceRef); }
    inline const InputButtonControlRef LeftMetaButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton)}, deviceRef); }
    inline const InputButtonControlRef RightMetaButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton)}, deviceRef); }
    inline const InputButtonControlRef ContextMenuButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton)}, deviceRef); }
    inline const InputButtonControlRef BackspaceButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton)}, deviceRef); }
    inline const InputButtonControlRef PageDownButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton)}, deviceRef); }
    inline const InputButtonControlRef PageUpButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton)}, deviceRef); }
    inline const InputButtonControlRef HomeButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton)}, deviceRef); }
    inline const InputButtonControlRef EndButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton)}, deviceRef); }
    inline const InputButtonControlRef InsertButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton)}, deviceRef); }
    inline const InputButtonControlRef DeleteButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton)}, deviceRef); }
    inline const InputButtonControlRef CapsLockButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton)}, deviceRef); }
    inline const InputButtonControlRef NumLockButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton)}, deviceRef); }
    inline const InputButtonControlRef PrintScreenButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton)}, deviceRef); }
    inline const InputButtonControlRef ScrollLockButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton)}, deviceRef); }
    inline const InputButtonControlRef PauseButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadEnterButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadDivideButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadMultiplyButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadPlusButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadMinusButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadPeriodButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton)}, deviceRef); }
    inline const InputButtonControlRef NumpadEqualsButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton)}, deviceRef); }
    inline const InputButtonControlRef Numpad1Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad2Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad3Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad4Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad5Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad6Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad7Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad8Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad9Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button)}, deviceRef); }
    inline const InputButtonControlRef Numpad0Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button)}, deviceRef); }
    inline const InputButtonControlRef F1Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button)}, deviceRef); }
    inline const InputButtonControlRef F2Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button)}, deviceRef); }
    inline const InputButtonControlRef F3Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button)}, deviceRef); }
    inline const InputButtonControlRef F4Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button)}, deviceRef); }
    inline const InputButtonControlRef F5Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button)}, deviceRef); }
    inline const InputButtonControlRef F6Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button)}, deviceRef); }
    inline const InputButtonControlRef F7Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button)}, deviceRef); }
    inline const InputButtonControlRef F8Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button)}, deviceRef); }
    inline const InputButtonControlRef F9Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button)}, deviceRef); }
    inline const InputButtonControlRef F10Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button)}, deviceRef); }
    inline const InputButtonControlRef F11Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button)}, deviceRef); }
    inline const InputButtonControlRef F12Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button)}, deviceRef); }
    inline const InputButtonControlRef OEM1Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button)}, deviceRef); }
    inline const InputButtonControlRef OEM2Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button)}, deviceRef); }
    inline const InputButtonControlRef OEM3Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button)}, deviceRef); }
    inline const InputButtonControlRef OEM4Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button)}, deviceRef); }
    inline const InputButtonControlRef OEM5Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button)}, deviceRef); }

    enum class Buttons
    {
        Escape,
        Space,
        Enter,
        Tab,
        Backquote,
        Quote,
        Semicolon,
        Comma,
        Period,
        Slash,
        Backslash,
        LeftBracket,
        RightBracket,
        Minus,
        Equals,
        UpArrow,
        DownArrow,
        LeftArrow,
        RightArrow,
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,
        Digit1,
        Digit2,
        Digit3,
        Digit4,
        Digit5,
        Digit6,
        Digit7,
        Digit8,
        Digit9,
        Digit0,
        LeftShift,
        RightShift,
        Shift,
        LeftAlt,
        RightAlt,
        Alt,
        LeftCtrl,
        RightCtrl,
        Ctrl,
        LeftMeta,
        RightMeta,
        ContextMenu,
        Backspace,
        PageDown,
        PageUp,
        Home,
        End,
        Insert,
        Delete,
        CapsLock,
        NumLock,
        PrintScreen,
        ScrollLock,
        Pause,
        NumpadEnter,
        NumpadDivide,
        NumpadMultiply,
        NumpadPlus,
        NumpadMinus,
        NumpadPeriod,
        NumpadEquals,
        Numpad1,
        Numpad2,
        Numpad3,
        Numpad4,
        Numpad5,
        Numpad6,
        Numpad7,
        Numpad8,
        Numpad9,
        Numpad0,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        OEM1,
        OEM2,
        OEM3,
        OEM4,
        OEM5,
    };

    inline const InputButtonControlRef operator[](const Buttons value) const
    {
        switch(value)
        {
        case Buttons::Escape: return EscapeButton();
        case Buttons::Space: return SpaceButton();
        case Buttons::Enter: return EnterButton();
        case Buttons::Tab: return TabButton();
        case Buttons::Backquote: return BackquoteButton();
        case Buttons::Quote: return QuoteButton();
        case Buttons::Semicolon: return SemicolonButton();
        case Buttons::Comma: return CommaButton();
        case Buttons::Period: return PeriodButton();
        case Buttons::Slash: return SlashButton();
        case Buttons::Backslash: return BackslashButton();
        case Buttons::LeftBracket: return LeftBracketButton();
        case Buttons::RightBracket: return RightBracketButton();
        case Buttons::Minus: return MinusButton();
        case Buttons::Equals: return EqualsButton();
        case Buttons::UpArrow: return UpArrowButton();
        case Buttons::DownArrow: return DownArrowButton();
        case Buttons::LeftArrow: return LeftArrowButton();
        case Buttons::RightArrow: return RightArrowButton();
        case Buttons::A: return AButton();
        case Buttons::B: return BButton();
        case Buttons::C: return CButton();
        case Buttons::D: return DButton();
        case Buttons::E: return EButton();
        case Buttons::F: return FButton();
        case Buttons::G: return GButton();
        case Buttons::H: return HButton();
        case Buttons::I: return IButton();
        case Buttons::J: return JButton();
        case Buttons::K: return KButton();
        case Buttons::L: return LButton();
        case Buttons::M: return MButton();
        case Buttons::N: return NButton();
        case Buttons::O: return OButton();
        case Buttons::P: return PButton();
        case Buttons::Q: return QButton();
        case Buttons::R: return RButton();
        case Buttons::S: return SButton();
        case Buttons::T: return TButton();
        case Buttons::U: return UButton();
        case Buttons::V: return VButton();
        case Buttons::W: return WButton();
        case Buttons::X: return XButton();
        case Buttons::Y: return YButton();
        case Buttons::Z: return ZButton();
        case Buttons::Digit1: return Digit1Button();
        case Buttons::Digit2: return Digit2Button();
        case Buttons::Digit3: return Digit3Button();
        case Buttons::Digit4: return Digit4Button();
        case Buttons::Digit5: return Digit5Button();
        case Buttons::Digit6: return Digit6Button();
        case Buttons::Digit7: return Digit7Button();
        case Buttons::Digit8: return Digit8Button();
        case Buttons::Digit9: return Digit9Button();
        case Buttons::Digit0: return Digit0Button();
        case Buttons::LeftShift: return LeftShiftButton();
        case Buttons::RightShift: return RightShiftButton();
        case Buttons::Shift: return ShiftButton();
        case Buttons::LeftAlt: return LeftAltButton();
        case Buttons::RightAlt: return RightAltButton();
        case Buttons::Alt: return AltButton();
        case Buttons::LeftCtrl: return LeftCtrlButton();
        case Buttons::RightCtrl: return RightCtrlButton();
        case Buttons::Ctrl: return CtrlButton();
        case Buttons::LeftMeta: return LeftMetaButton();
        case Buttons::RightMeta: return RightMetaButton();
        case Buttons::ContextMenu: return ContextMenuButton();
        case Buttons::Backspace: return BackspaceButton();
        case Buttons::PageDown: return PageDownButton();
        case Buttons::PageUp: return PageUpButton();
        case Buttons::Home: return HomeButton();
        case Buttons::End: return EndButton();
        case Buttons::Insert: return InsertButton();
        case Buttons::Delete: return DeleteButton();
        case Buttons::CapsLock: return CapsLockButton();
        case Buttons::NumLock: return NumLockButton();
        case Buttons::PrintScreen: return PrintScreenButton();
        case Buttons::ScrollLock: return ScrollLockButton();
        case Buttons::Pause: return PauseButton();
        case Buttons::NumpadEnter: return NumpadEnterButton();
        case Buttons::NumpadDivide: return NumpadDivideButton();
        case Buttons::NumpadMultiply: return NumpadMultiplyButton();
        case Buttons::NumpadPlus: return NumpadPlusButton();
        case Buttons::NumpadMinus: return NumpadMinusButton();
        case Buttons::NumpadPeriod: return NumpadPeriodButton();
        case Buttons::NumpadEquals: return NumpadEqualsButton();
        case Buttons::Numpad1: return Numpad1Button();
        case Buttons::Numpad2: return Numpad2Button();
        case Buttons::Numpad3: return Numpad3Button();
        case Buttons::Numpad4: return Numpad4Button();
        case Buttons::Numpad5: return Numpad5Button();
        case Buttons::Numpad6: return Numpad6Button();
        case Buttons::Numpad7: return Numpad7Button();
        case Buttons::Numpad8: return Numpad8Button();
        case Buttons::Numpad9: return Numpad9Button();
        case Buttons::Numpad0: return Numpad0Button();
        case Buttons::F1: return F1Button();
        case Buttons::F2: return F2Button();
        case Buttons::F3: return F3Button();
        case Buttons::F4: return F4Button();
        case Buttons::F5: return F5Button();
        case Buttons::F6: return F6Button();
        case Buttons::F7: return F7Button();
        case Buttons::F8: return F8Button();
        case Buttons::F9: return F9Button();
        case Buttons::F10: return F10Button();
        case Buttons::F11: return F11Button();
        case Buttons::F12: return F12Button();
        case Buttons::OEM1: return OEM1Button();
        case Buttons::OEM2: return OEM2Button();
        case Buttons::OEM3: return OEM3Button();
        case Buttons::OEM4: return OEM4Button();
        case Buttons::OEM5: return OEM5Button();
        default: InputAssert(false, "Unknown control"); return InputButtonControlRef::Setup(InputControlRefInvalid);
        }
    }
};

struct InputPointer
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Pointer)};
    InputDeviceRef deviceRef;

    static inline InputPointer Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputPointer r = {};
        r.deviceRef = deviceRef;
        return r;
    }
    inline const InputPosition2DControlRef PositionPosition2D() const { return InputPosition2DControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Pointer_PositionPosition2D)}, deviceRef); }

    enum class Position2Ds
    {
        Position,
    };

    inline const InputPosition2DControlRef operator[](const Position2Ds value) const
    {
        switch(value)
        {
        case Position2Ds::Position: return PositionPosition2D();
        default: InputAssert(false, "Unknown control"); return InputPosition2DControlRef::Setup(InputControlRefInvalid);
        }
    }
};

struct InputMouse
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Mouse)};
    InputDeviceRef deviceRef;

    static inline InputMouse Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputMouse r = {};
        r.deviceRef = deviceRef;
        return r;
    }
    inline const InputDeltaVector2DControlRef MotionDeltaVector2D() const { return InputDeltaVector2DControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}, deviceRef); }
    inline const InputDeltaVector2DControlRef ScrollDeltaVector2D() const { return InputDeltaVector2DControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}, deviceRef); }
    inline const InputButtonControlRef LeftButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton)}, deviceRef); }
    inline const InputButtonControlRef MiddleButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton)}, deviceRef); }
    inline const InputButtonControlRef RightButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton)}, deviceRef); }
    inline const InputButtonControlRef BackButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton)}, deviceRef); }
    inline const InputButtonControlRef ForwardButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton)}, deviceRef); }

    enum class DeltaVector2Ds
    {
        Motion,
        Scroll,
    };

    inline const InputDeltaVector2DControlRef operator[](const DeltaVector2Ds value) const
    {
        switch(value)
        {
        case DeltaVector2Ds::Motion: return MotionDeltaVector2D();
        case DeltaVector2Ds::Scroll: return ScrollDeltaVector2D();
        default: InputAssert(false, "Unknown control"); return InputDeltaVector2DControlRef::Setup(InputControlRefInvalid);
        }
    }

    enum class Buttons
    {
        Left,
        Middle,
        Right,
        Back,
        Forward,
    };

    inline const InputButtonControlRef operator[](const Buttons value) const
    {
        switch(value)
        {
        case Buttons::Left: return LeftButton();
        case Buttons::Middle: return MiddleButton();
        case Buttons::Right: return RightButton();
        case Buttons::Back: return BackButton();
        case Buttons::Forward: return ForwardButton();
        default: InputAssert(false, "Unknown control"); return InputButtonControlRef::Setup(InputControlRefInvalid);
        }
    }
};

struct InputGamepad
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Gamepad)};
    InputDeviceRef deviceRef;

    static inline InputGamepad Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputGamepad r = {};
        r.deviceRef = deviceRef;
        return r;
    }
    inline const InputButtonControlRef WestButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton)}, deviceRef); }
    inline const InputButtonControlRef NorthButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton)}, deviceRef); }
    inline const InputButtonControlRef EastButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton)}, deviceRef); }
    inline const InputButtonControlRef SouthButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton)}, deviceRef); }
    inline const InputStickControlRef LeftStick() const { return InputStickControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}, deviceRef); }
    inline const InputStickControlRef RightStick() const { return InputStickControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}, deviceRef); }
    inline const InputButtonControlRef LeftStickButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton)}, deviceRef); }
    inline const InputButtonControlRef RightStickButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton)}, deviceRef); }
    inline const InputStickControlRef DPadStick() const { return InputStickControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}, deviceRef); }
    inline const InputButtonControlRef LeftShoulderButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton)}, deviceRef); }
    inline const InputButtonControlRef RightShoulderButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton)}, deviceRef); }
    inline const InputAxisOneWayControlRef LeftTriggerAxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef RightTriggerAxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay)}, deviceRef); }

    enum class Buttons
    {
        West,
        North,
        East,
        South,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,
    };

    inline const InputButtonControlRef operator[](const Buttons value) const
    {
        switch(value)
        {
        case Buttons::West: return WestButton();
        case Buttons::North: return NorthButton();
        case Buttons::East: return EastButton();
        case Buttons::South: return SouthButton();
        case Buttons::LeftStick: return LeftStickButton();
        case Buttons::RightStick: return RightStickButton();
        case Buttons::LeftShoulder: return LeftShoulderButton();
        case Buttons::RightShoulder: return RightShoulderButton();
        default: InputAssert(false, "Unknown control"); return InputButtonControlRef::Setup(InputControlRefInvalid);
        }
    }

    enum class Sticks
    {
        Left,
        Right,
        DPad,
    };

    inline const InputStickControlRef operator[](const Sticks value) const
    {
        switch(value)
        {
        case Sticks::Left: return LeftStick();
        case Sticks::Right: return RightStick();
        case Sticks::DPad: return DPadStick();
        default: InputAssert(false, "Unknown control"); return InputStickControlRef::Setup(InputControlRefInvalid);
        }
    }

    enum class AxisOneWays
    {
        LeftTrigger,
        RightTrigger,
    };

    inline const InputAxisOneWayControlRef operator[](const AxisOneWays value) const
    {
        switch(value)
        {
        case AxisOneWays::LeftTrigger: return LeftTriggerAxisOneWay();
        case AxisOneWays::RightTrigger: return RightTriggerAxisOneWay();
        default: InputAssert(false, "Unknown control"); return InputAxisOneWayControlRef::Setup(InputControlRefInvalid);
        }
    }
};

struct InputDualSense
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::DualSense)};
    InputDeviceRef deviceRef;

    static inline InputDualSense Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputDualSense r = {};
        r.deviceRef = deviceRef;
        r.setLED = nullptr;
        r.setColor = nullptr;
        return r;
    }

    typedef void (*SetLEDType)(int playerIndex);
    typedef void (*SetColorType)(float r, float g, float b, float a);

    SetLEDType setLED;
    SetColorType setColor;

    inline const InputButtonControlRef OptionsButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton)}, deviceRef); }
    inline const InputButtonControlRef ShareButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton)}, deviceRef); }
    inline const InputButtonControlRef PlaystationButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton)}, deviceRef); }
    inline const InputButtonControlRef MicButton() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton)}, deviceRef); }

    enum class Buttons
    {
        Options,
        Share,
        Playstation,
        Mic,
    };

    inline const InputButtonControlRef operator[](const Buttons value) const
    {
        switch(value)
        {
        case Buttons::Options: return OptionsButton();
        case Buttons::Share: return ShareButton();
        case Buttons::Playstation: return PlaystationButton();
        case Buttons::Mic: return MicButton();
        default: InputAssert(false, "Unknown control"); return InputButtonControlRef::Setup(InputControlRefInvalid);
        }
    }
};

struct InputGenericControls
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::GenericControls)};
    InputDeviceRef deviceRef;

    static inline InputGenericControls Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        InputGenericControls r = {};
        r.deviceRef = deviceRef;
        return r;
    }
    inline const InputButtonControlRef Generic0Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button)}, deviceRef); }
    inline const InputButtonControlRef Generic1Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button)}, deviceRef); }
    inline const InputButtonControlRef Generic2Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button)}, deviceRef); }
    inline const InputButtonControlRef Generic3Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button)}, deviceRef); }
    inline const InputButtonControlRef Generic4Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button)}, deviceRef); }
    inline const InputButtonControlRef Generic5Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button)}, deviceRef); }
    inline const InputButtonControlRef Generic6Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button)}, deviceRef); }
    inline const InputButtonControlRef Generic7Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button)}, deviceRef); }
    inline const InputButtonControlRef Generic8Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button)}, deviceRef); }
    inline const InputButtonControlRef Generic9Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button)}, deviceRef); }
    inline const InputButtonControlRef Generic10Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button)}, deviceRef); }
    inline const InputButtonControlRef Generic11Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button)}, deviceRef); }
    inline const InputButtonControlRef Generic12Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button)}, deviceRef); }
    inline const InputButtonControlRef Generic13Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button)}, deviceRef); }
    inline const InputButtonControlRef Generic14Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button)}, deviceRef); }
    inline const InputButtonControlRef Generic15Button() const { return InputButtonControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic0AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic1AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic2AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic3AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic4AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic5AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic6AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic7AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic8AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic9AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic10AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic11AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic12AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic13AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic14AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay)}, deviceRef); }
    inline const InputAxisOneWayControlRef Generic15AxisOneWay() const { return InputAxisOneWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic0AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic1AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic2AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic3AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic4AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic5AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic6AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic7AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic8AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic9AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic10AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic11AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic12AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic13AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic14AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}, deviceRef); }
    inline const InputAxisTwoWayControlRef Generic15AxisTwoWay() const { return InputAxisTwoWayControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}, deviceRef); }

    enum class Buttons
    {
        Generic0,
        Generic1,
        Generic2,
        Generic3,
        Generic4,
        Generic5,
        Generic6,
        Generic7,
        Generic8,
        Generic9,
        Generic10,
        Generic11,
        Generic12,
        Generic13,
        Generic14,
        Generic15,
    };

    inline const InputButtonControlRef operator[](const Buttons value) const
    {
        switch(value)
        {
        case Buttons::Generic0: return Generic0Button();
        case Buttons::Generic1: return Generic1Button();
        case Buttons::Generic2: return Generic2Button();
        case Buttons::Generic3: return Generic3Button();
        case Buttons::Generic4: return Generic4Button();
        case Buttons::Generic5: return Generic5Button();
        case Buttons::Generic6: return Generic6Button();
        case Buttons::Generic7: return Generic7Button();
        case Buttons::Generic8: return Generic8Button();
        case Buttons::Generic9: return Generic9Button();
        case Buttons::Generic10: return Generic10Button();
        case Buttons::Generic11: return Generic11Button();
        case Buttons::Generic12: return Generic12Button();
        case Buttons::Generic13: return Generic13Button();
        case Buttons::Generic14: return Generic14Button();
        case Buttons::Generic15: return Generic15Button();
        default: InputAssert(false, "Unknown control"); return InputButtonControlRef::Setup(InputControlRefInvalid);
        }
    }

    enum class AxisOneWays
    {
        Generic0,
        Generic1,
        Generic2,
        Generic3,
        Generic4,
        Generic5,
        Generic6,
        Generic7,
        Generic8,
        Generic9,
        Generic10,
        Generic11,
        Generic12,
        Generic13,
        Generic14,
        Generic15,
    };

    inline const InputAxisOneWayControlRef operator[](const AxisOneWays value) const
    {
        switch(value)
        {
        case AxisOneWays::Generic0: return Generic0AxisOneWay();
        case AxisOneWays::Generic1: return Generic1AxisOneWay();
        case AxisOneWays::Generic2: return Generic2AxisOneWay();
        case AxisOneWays::Generic3: return Generic3AxisOneWay();
        case AxisOneWays::Generic4: return Generic4AxisOneWay();
        case AxisOneWays::Generic5: return Generic5AxisOneWay();
        case AxisOneWays::Generic6: return Generic6AxisOneWay();
        case AxisOneWays::Generic7: return Generic7AxisOneWay();
        case AxisOneWays::Generic8: return Generic8AxisOneWay();
        case AxisOneWays::Generic9: return Generic9AxisOneWay();
        case AxisOneWays::Generic10: return Generic10AxisOneWay();
        case AxisOneWays::Generic11: return Generic11AxisOneWay();
        case AxisOneWays::Generic12: return Generic12AxisOneWay();
        case AxisOneWays::Generic13: return Generic13AxisOneWay();
        case AxisOneWays::Generic14: return Generic14AxisOneWay();
        case AxisOneWays::Generic15: return Generic15AxisOneWay();
        default: InputAssert(false, "Unknown control"); return InputAxisOneWayControlRef::Setup(InputControlRefInvalid);
        }
    }

    enum class AxisTwoWays
    {
        Generic0,
        Generic1,
        Generic2,
        Generic3,
        Generic4,
        Generic5,
        Generic6,
        Generic7,
        Generic8,
        Generic9,
        Generic10,
        Generic11,
        Generic12,
        Generic13,
        Generic14,
        Generic15,
    };

    inline const InputAxisTwoWayControlRef operator[](const AxisTwoWays value) const
    {
        switch(value)
        {
        case AxisTwoWays::Generic0: return Generic0AxisTwoWay();
        case AxisTwoWays::Generic1: return Generic1AxisTwoWay();
        case AxisTwoWays::Generic2: return Generic2AxisTwoWay();
        case AxisTwoWays::Generic3: return Generic3AxisTwoWay();
        case AxisTwoWays::Generic4: return Generic4AxisTwoWay();
        case AxisTwoWays::Generic5: return Generic5AxisTwoWay();
        case AxisTwoWays::Generic6: return Generic6AxisTwoWay();
        case AxisTwoWays::Generic7: return Generic7AxisTwoWay();
        case AxisTwoWays::Generic8: return Generic8AxisTwoWay();
        case AxisTwoWays::Generic9: return Generic9AxisTwoWay();
        case AxisTwoWays::Generic10: return Generic10AxisTwoWay();
        case AxisTwoWays::Generic11: return Generic11AxisTwoWay();
        case AxisTwoWays::Generic12: return Generic12AxisTwoWay();
        case AxisTwoWays::Generic13: return Generic13AxisTwoWay();
        case AxisTwoWays::Generic14: return Generic14AxisTwoWay();
        case AxisTwoWays::Generic15: return Generic15AxisTwoWay();
        default: InputAssert(false, "Unknown control"); return InputAxisTwoWayControlRef::Setup(InputControlRefInvalid);
        }
    }
};


#ifdef INPUT_NATIVE_DEVICE_DATABASE_PROVIDER

static inline uint32_t _InputStrToBuf(char* buffer, const uint32_t bufferCount, const char* str)
{
    const auto written = snprintf(buffer, bufferCount, "%s", str);
    return written > 0 ? static_cast<uint32_t>(written) : 0;
}

static inline InputDeviceDatabaseCallbacks _InputBuiltInDatabaseGetCallbacks()
{
    return {
        [](
            const InputControlTypeRef controlTypeRef,
            const InputControlRef controlRef,
            const InputControlTypeRef samplesType,
            const InputControlTimestamp* timestamps,
            const void* samples,
            const uint32_t count,
            const InputControlRef fromAnotherControl
        )
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Invalid: break;
            case InputControlTypeBuiltIn::Button: InputButtonControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::AxisOneWay: InputAxisOneWayControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::AxisTwoWay: InputAxisTwoWayControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::DeltaAxisTwoWay: InputDeltaAxisTwoWayControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::Stick: InputStickControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::DeltaVector2D: InputDeltaVector2DControlIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            case InputControlTypeBuiltIn::Position2D: _TodoIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
            default:
                InputAssert(false, "Trying to ingress to unknown type");
                break;
            }
        },
        [](
            const InputControlTypeRef controlTypeRef,
            const InputControlRef* controlRefs,
            void* controlStates,
            InputControlTimestamp* latestRecordedTimestamps,
            void* latestRecordedSamples,
            const uint32_t controlCount
        )
        {
            // TODO type check pointer conversion!
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Invalid: break;
            case InputControlTypeBuiltIn::Button: InputButtonControlFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputButtonControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputButtonControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::AxisOneWay: InputAxisOneWayFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputAxisOneWayControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputAxisOneWayControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::AxisTwoWay: InputAxisTwoWayFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputAxisTwoWayControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputAxisTwoWayControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::DeltaAxisTwoWay: InputDeltaAxisTwoWayFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputDeltaAxisTwoWayControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputDeltaAxisTwoWayControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::Stick: InputStickFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputStickControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputStickControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::DeltaVector2D: InputDeltaVector2DFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<InputDeltaVector2DControlState*>(controlStates), latestRecordedTimestamps, reinterpret_cast<InputDeltaVector2DControlSample*>(latestRecordedSamples), controlCount); break;
            case InputControlTypeBuiltIn::Position2D: _TodoFrameBegin(controlTypeRef, controlRefs, reinterpret_cast<uint8_t*>(controlStates), latestRecordedTimestamps, reinterpret_cast<uint8_t*>(latestRecordedSamples), controlCount); break;
            default:
                InputAssert(false, "Trying to frame begin unknown type");
                break;
            }
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef, InputDeviceTraitRef* o, const uint32_t count)->uint32_t // GetDeviceTraits
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
            case InputDeviceBuiltIn::KeyboardWindows:
                if (o != nullptr && count == 1)
                {
                    o[0] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Keyboard)};
                }
                else if (o != nullptr)
                    InputAssert(false, "Please provide 1 elements");
                return 1;
            case InputDeviceBuiltIn::MouseMacOS:
                if (o != nullptr && count == 2)
                {
                    o[0] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Pointer)};
                    o[1] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Mouse)};
                }
                else if (o != nullptr)
                    InputAssert(false, "Please provide 2 elements");
                return 2;
            case InputDeviceBuiltIn::WindowsGamingInputGamepad:
                if (o != nullptr && count == 2)
                {
                    o[0] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::ExplicitlyPollableDevice)};
                    o[1] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Gamepad)};
                }
                else if (o != nullptr)
                    InputAssert(false, "Please provide 2 elements");
                return 2;
            default:
                return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef)->uint32_t // GetTraitSizeInBytes
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
            case InputDeviceTraitBuiltIn::ExplicitlyPollableDevice: return static_cast<uint32_t>(sizeof(InputExplicitlyPollableDevice));
            case InputDeviceTraitBuiltIn::Keyboard: return static_cast<uint32_t>(sizeof(InputKeyboard));
            case InputDeviceTraitBuiltIn::Pointer: return static_cast<uint32_t>(sizeof(InputPointer));
            case InputDeviceTraitBuiltIn::Mouse: return static_cast<uint32_t>(sizeof(InputMouse));
            case InputDeviceTraitBuiltIn::Gamepad: return static_cast<uint32_t>(sizeof(InputGamepad));
            case InputDeviceTraitBuiltIn::DualSense: return static_cast<uint32_t>(sizeof(InputDualSense));
            case InputDeviceTraitBuiltIn::GenericControls: return static_cast<uint32_t>(sizeof(InputGenericControls));
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, const InputDeviceRef deviceRef, InputControlRef* o, const uint32_t count)->uint32_t // GetTraitControls
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
            case InputDeviceTraitBuiltIn::ExplicitlyPollableDevice:
                if (o != nullptr && count == 0)
                {}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 0 elements");
                return 0;
            case InputDeviceTraitBuiltIn::Keyboard:
                if (o != nullptr && count == 226)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton)}, deviceRef);
                    o[1] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton_AsAxisOneWay)}, deviceRef);
                    o[2] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton)}, deviceRef);
                    o[3] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton_AsAxisOneWay)}, deviceRef);
                    o[4] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton)}, deviceRef);
                    o[5] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton_AsAxisOneWay)}, deviceRef);
                    o[6] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton)}, deviceRef);
                    o[7] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton_AsAxisOneWay)}, deviceRef);
                    o[8] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton)}, deviceRef);
                    o[9] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton_AsAxisOneWay)}, deviceRef);
                    o[10] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton)}, deviceRef);
                    o[11] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton_AsAxisOneWay)}, deviceRef);
                    o[12] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton)}, deviceRef);
                    o[13] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton_AsAxisOneWay)}, deviceRef);
                    o[14] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton)}, deviceRef);
                    o[15] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton_AsAxisOneWay)}, deviceRef);
                    o[16] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton)}, deviceRef);
                    o[17] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton_AsAxisOneWay)}, deviceRef);
                    o[18] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton)}, deviceRef);
                    o[19] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton_AsAxisOneWay)}, deviceRef);
                    o[20] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton)}, deviceRef);
                    o[21] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton_AsAxisOneWay)}, deviceRef);
                    o[22] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton)}, deviceRef);
                    o[23] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton_AsAxisOneWay)}, deviceRef);
                    o[24] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton)}, deviceRef);
                    o[25] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton_AsAxisOneWay)}, deviceRef);
                    o[26] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton)}, deviceRef);
                    o[27] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton_AsAxisOneWay)}, deviceRef);
                    o[28] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton)}, deviceRef);
                    o[29] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton_AsAxisOneWay)}, deviceRef);
                    o[30] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton)}, deviceRef);
                    o[31] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton_AsAxisOneWay)}, deviceRef);
                    o[32] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton)}, deviceRef);
                    o[33] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton_AsAxisOneWay)}, deviceRef);
                    o[34] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton)}, deviceRef);
                    o[35] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton_AsAxisOneWay)}, deviceRef);
                    o[36] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton)}, deviceRef);
                    o[37] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton_AsAxisOneWay)}, deviceRef);
                    o[38] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton)}, deviceRef);
                    o[39] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton_AsAxisOneWay)}, deviceRef);
                    o[40] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton)}, deviceRef);
                    o[41] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton_AsAxisOneWay)}, deviceRef);
                    o[42] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton)}, deviceRef);
                    o[43] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton_AsAxisOneWay)}, deviceRef);
                    o[44] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton)}, deviceRef);
                    o[45] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton_AsAxisOneWay)}, deviceRef);
                    o[46] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton)}, deviceRef);
                    o[47] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton_AsAxisOneWay)}, deviceRef);
                    o[48] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton)}, deviceRef);
                    o[49] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton_AsAxisOneWay)}, deviceRef);
                    o[50] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton)}, deviceRef);
                    o[51] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton_AsAxisOneWay)}, deviceRef);
                    o[52] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton)}, deviceRef);
                    o[53] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton_AsAxisOneWay)}, deviceRef);
                    o[54] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton)}, deviceRef);
                    o[55] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton_AsAxisOneWay)}, deviceRef);
                    o[56] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton)}, deviceRef);
                    o[57] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton_AsAxisOneWay)}, deviceRef);
                    o[58] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton)}, deviceRef);
                    o[59] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton_AsAxisOneWay)}, deviceRef);
                    o[60] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton)}, deviceRef);
                    o[61] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton_AsAxisOneWay)}, deviceRef);
                    o[62] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton)}, deviceRef);
                    o[63] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton_AsAxisOneWay)}, deviceRef);
                    o[64] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton)}, deviceRef);
                    o[65] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton_AsAxisOneWay)}, deviceRef);
                    o[66] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton)}, deviceRef);
                    o[67] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton_AsAxisOneWay)}, deviceRef);
                    o[68] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton)}, deviceRef);
                    o[69] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton_AsAxisOneWay)}, deviceRef);
                    o[70] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton)}, deviceRef);
                    o[71] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton_AsAxisOneWay)}, deviceRef);
                    o[72] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton)}, deviceRef);
                    o[73] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton_AsAxisOneWay)}, deviceRef);
                    o[74] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton)}, deviceRef);
                    o[75] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton_AsAxisOneWay)}, deviceRef);
                    o[76] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton)}, deviceRef);
                    o[77] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton_AsAxisOneWay)}, deviceRef);
                    o[78] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton)}, deviceRef);
                    o[79] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton_AsAxisOneWay)}, deviceRef);
                    o[80] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton)}, deviceRef);
                    o[81] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton_AsAxisOneWay)}, deviceRef);
                    o[82] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton)}, deviceRef);
                    o[83] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton_AsAxisOneWay)}, deviceRef);
                    o[84] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton)}, deviceRef);
                    o[85] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton_AsAxisOneWay)}, deviceRef);
                    o[86] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton)}, deviceRef);
                    o[87] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton_AsAxisOneWay)}, deviceRef);
                    o[88] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton)}, deviceRef);
                    o[89] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton_AsAxisOneWay)}, deviceRef);
                    o[90] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button)}, deviceRef);
                    o[91] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button_AsAxisOneWay)}, deviceRef);
                    o[92] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button)}, deviceRef);
                    o[93] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button_AsAxisOneWay)}, deviceRef);
                    o[94] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button)}, deviceRef);
                    o[95] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button_AsAxisOneWay)}, deviceRef);
                    o[96] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button)}, deviceRef);
                    o[97] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button_AsAxisOneWay)}, deviceRef);
                    o[98] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button)}, deviceRef);
                    o[99] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button_AsAxisOneWay)}, deviceRef);
                    o[100] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button)}, deviceRef);
                    o[101] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button_AsAxisOneWay)}, deviceRef);
                    o[102] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button)}, deviceRef);
                    o[103] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button_AsAxisOneWay)}, deviceRef);
                    o[104] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button)}, deviceRef);
                    o[105] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button_AsAxisOneWay)}, deviceRef);
                    o[106] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button)}, deviceRef);
                    o[107] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button_AsAxisOneWay)}, deviceRef);
                    o[108] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button)}, deviceRef);
                    o[109] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button_AsAxisOneWay)}, deviceRef);
                    o[110] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton)}, deviceRef);
                    o[111] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton_AsAxisOneWay)}, deviceRef);
                    o[112] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton)}, deviceRef);
                    o[113] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton_AsAxisOneWay)}, deviceRef);
                    o[114] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton)}, deviceRef);
                    o[115] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton_AsAxisOneWay)}, deviceRef);
                    o[116] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton)}, deviceRef);
                    o[117] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton_AsAxisOneWay)}, deviceRef);
                    o[118] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton)}, deviceRef);
                    o[119] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton_AsAxisOneWay)}, deviceRef);
                    o[120] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton)}, deviceRef);
                    o[121] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton_AsAxisOneWay)}, deviceRef);
                    o[122] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton)}, deviceRef);
                    o[123] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton_AsAxisOneWay)}, deviceRef);
                    o[124] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton)}, deviceRef);
                    o[125] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton_AsAxisOneWay)}, deviceRef);
                    o[126] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton)}, deviceRef);
                    o[127] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton_AsAxisOneWay)}, deviceRef);
                    o[128] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton)}, deviceRef);
                    o[129] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton_AsAxisOneWay)}, deviceRef);
                    o[130] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton)}, deviceRef);
                    o[131] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton_AsAxisOneWay)}, deviceRef);
                    o[132] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton)}, deviceRef);
                    o[133] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton_AsAxisOneWay)}, deviceRef);
                    o[134] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton)}, deviceRef);
                    o[135] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton_AsAxisOneWay)}, deviceRef);
                    o[136] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton)}, deviceRef);
                    o[137] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton_AsAxisOneWay)}, deviceRef);
                    o[138] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton)}, deviceRef);
                    o[139] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton_AsAxisOneWay)}, deviceRef);
                    o[140] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton)}, deviceRef);
                    o[141] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton_AsAxisOneWay)}, deviceRef);
                    o[142] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton)}, deviceRef);
                    o[143] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton_AsAxisOneWay)}, deviceRef);
                    o[144] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton)}, deviceRef);
                    o[145] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton_AsAxisOneWay)}, deviceRef);
                    o[146] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton)}, deviceRef);
                    o[147] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton_AsAxisOneWay)}, deviceRef);
                    o[148] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton)}, deviceRef);
                    o[149] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton_AsAxisOneWay)}, deviceRef);
                    o[150] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton)}, deviceRef);
                    o[151] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton_AsAxisOneWay)}, deviceRef);
                    o[152] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton)}, deviceRef);
                    o[153] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton_AsAxisOneWay)}, deviceRef);
                    o[154] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton)}, deviceRef);
                    o[155] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton_AsAxisOneWay)}, deviceRef);
                    o[156] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton)}, deviceRef);
                    o[157] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton_AsAxisOneWay)}, deviceRef);
                    o[158] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton)}, deviceRef);
                    o[159] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton_AsAxisOneWay)}, deviceRef);
                    o[160] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton)}, deviceRef);
                    o[161] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton_AsAxisOneWay)}, deviceRef);
                    o[162] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton)}, deviceRef);
                    o[163] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton_AsAxisOneWay)}, deviceRef);
                    o[164] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton)}, deviceRef);
                    o[165] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton_AsAxisOneWay)}, deviceRef);
                    o[166] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton)}, deviceRef);
                    o[167] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton_AsAxisOneWay)}, deviceRef);
                    o[168] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton)}, deviceRef);
                    o[169] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton_AsAxisOneWay)}, deviceRef);
                    o[170] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton)}, deviceRef);
                    o[171] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton_AsAxisOneWay)}, deviceRef);
                    o[172] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button)}, deviceRef);
                    o[173] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button_AsAxisOneWay)}, deviceRef);
                    o[174] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button)}, deviceRef);
                    o[175] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button_AsAxisOneWay)}, deviceRef);
                    o[176] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button)}, deviceRef);
                    o[177] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button_AsAxisOneWay)}, deviceRef);
                    o[178] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button)}, deviceRef);
                    o[179] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button_AsAxisOneWay)}, deviceRef);
                    o[180] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button)}, deviceRef);
                    o[181] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button_AsAxisOneWay)}, deviceRef);
                    o[182] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button)}, deviceRef);
                    o[183] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button_AsAxisOneWay)}, deviceRef);
                    o[184] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button)}, deviceRef);
                    o[185] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button_AsAxisOneWay)}, deviceRef);
                    o[186] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button)}, deviceRef);
                    o[187] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button_AsAxisOneWay)}, deviceRef);
                    o[188] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button)}, deviceRef);
                    o[189] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button_AsAxisOneWay)}, deviceRef);
                    o[190] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button)}, deviceRef);
                    o[191] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button_AsAxisOneWay)}, deviceRef);
                    o[192] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button)}, deviceRef);
                    o[193] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button_AsAxisOneWay)}, deviceRef);
                    o[194] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button)}, deviceRef);
                    o[195] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button_AsAxisOneWay)}, deviceRef);
                    o[196] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button)}, deviceRef);
                    o[197] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button_AsAxisOneWay)}, deviceRef);
                    o[198] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button)}, deviceRef);
                    o[199] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button_AsAxisOneWay)}, deviceRef);
                    o[200] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button)}, deviceRef);
                    o[201] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button_AsAxisOneWay)}, deviceRef);
                    o[202] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button)}, deviceRef);
                    o[203] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button_AsAxisOneWay)}, deviceRef);
                    o[204] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button)}, deviceRef);
                    o[205] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button_AsAxisOneWay)}, deviceRef);
                    o[206] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button)}, deviceRef);
                    o[207] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button_AsAxisOneWay)}, deviceRef);
                    o[208] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button)}, deviceRef);
                    o[209] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button_AsAxisOneWay)}, deviceRef);
                    o[210] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button)}, deviceRef);
                    o[211] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button_AsAxisOneWay)}, deviceRef);
                    o[212] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button)}, deviceRef);
                    o[213] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button_AsAxisOneWay)}, deviceRef);
                    o[214] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button)}, deviceRef);
                    o[215] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button_AsAxisOneWay)}, deviceRef);
                    o[216] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button)}, deviceRef);
                    o[217] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button_AsAxisOneWay)}, deviceRef);
                    o[218] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button)}, deviceRef);
                    o[219] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button_AsAxisOneWay)}, deviceRef);
                    o[220] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button)}, deviceRef);
                    o[221] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button_AsAxisOneWay)}, deviceRef);
                    o[222] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button)}, deviceRef);
                    o[223] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button_AsAxisOneWay)}, deviceRef);
                    o[224] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button)}, deviceRef);
                    o[225] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button_AsAxisOneWay)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 226 elements");
                return 226;
            case InputDeviceTraitBuiltIn::Pointer:
                if (o != nullptr && count == 1)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Pointer_PositionPosition2D)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 1 elements");
                return 1;
            case InputDeviceTraitBuiltIn::Mouse:
                if (o != nullptr && count == 24)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}, deviceRef);
                    o[1] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay)}, deviceRef);
                    o[2] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay)}, deviceRef);
                    o[3] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_LeftButton)}, deviceRef);
                    o[4] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_UpButton)}, deviceRef);
                    o[5] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_RightButton)}, deviceRef);
                    o[6] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_DownButton)}, deviceRef);
                    o[7] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}, deviceRef);
                    o[8] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay)}, deviceRef);
                    o[9] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay)}, deviceRef);
                    o[10] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_LeftButton)}, deviceRef);
                    o[11] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_UpButton)}, deviceRef);
                    o[12] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_RightButton)}, deviceRef);
                    o[13] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_DownButton)}, deviceRef);
                    o[14] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton)}, deviceRef);
                    o[15] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton_AsAxisOneWay)}, deviceRef);
                    o[16] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton)}, deviceRef);
                    o[17] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton_AsAxisOneWay)}, deviceRef);
                    o[18] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton)}, deviceRef);
                    o[19] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton_AsAxisOneWay)}, deviceRef);
                    o[20] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton)}, deviceRef);
                    o[21] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton_AsAxisOneWay)}, deviceRef);
                    o[22] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton)}, deviceRef);
                    o[23] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton_AsAxisOneWay)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 24 elements");
                return 24;
            case InputDeviceTraitBuiltIn::Gamepad:
                if (o != nullptr && count == 53)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton)}, deviceRef);
                    o[1] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton_AsAxisOneWay)}, deviceRef);
                    o[2] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton)}, deviceRef);
                    o[3] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton_AsAxisOneWay)}, deviceRef);
                    o[4] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton)}, deviceRef);
                    o[5] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton_AsAxisOneWay)}, deviceRef);
                    o[6] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton)}, deviceRef);
                    o[7] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton_AsAxisOneWay)}, deviceRef);
                    o[8] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}, deviceRef);
                    o[9] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_VerticalAxisTwoWay)}, deviceRef);
                    o[10] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_HorizontalAxisTwoWay)}, deviceRef);
                    o[11] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_LeftAxisOneWay)}, deviceRef);
                    o[12] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_UpAxisOneWay)}, deviceRef);
                    o[13] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_RightAxisOneWay)}, deviceRef);
                    o[14] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_DownAxisOneWay)}, deviceRef);
                    o[15] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_LeftButton)}, deviceRef);
                    o[16] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_UpButton)}, deviceRef);
                    o[17] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_RightButton)}, deviceRef);
                    o[18] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_DownButton)}, deviceRef);
                    o[19] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}, deviceRef);
                    o[20] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_VerticalAxisTwoWay)}, deviceRef);
                    o[21] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_HorizontalAxisTwoWay)}, deviceRef);
                    o[22] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_LeftAxisOneWay)}, deviceRef);
                    o[23] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_UpAxisOneWay)}, deviceRef);
                    o[24] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_RightAxisOneWay)}, deviceRef);
                    o[25] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_DownAxisOneWay)}, deviceRef);
                    o[26] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_LeftButton)}, deviceRef);
                    o[27] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_UpButton)}, deviceRef);
                    o[28] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_RightButton)}, deviceRef);
                    o[29] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_DownButton)}, deviceRef);
                    o[30] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton)}, deviceRef);
                    o[31] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton_AsAxisOneWay)}, deviceRef);
                    o[32] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton)}, deviceRef);
                    o[33] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton_AsAxisOneWay)}, deviceRef);
                    o[34] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}, deviceRef);
                    o[35] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_VerticalAxisTwoWay)}, deviceRef);
                    o[36] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_HorizontalAxisTwoWay)}, deviceRef);
                    o[37] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_LeftAxisOneWay)}, deviceRef);
                    o[38] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_UpAxisOneWay)}, deviceRef);
                    o[39] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_RightAxisOneWay)}, deviceRef);
                    o[40] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_DownAxisOneWay)}, deviceRef);
                    o[41] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_LeftButton)}, deviceRef);
                    o[42] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_UpButton)}, deviceRef);
                    o[43] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_RightButton)}, deviceRef);
                    o[44] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_DownButton)}, deviceRef);
                    o[45] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton)}, deviceRef);
                    o[46] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton_AsAxisOneWay)}, deviceRef);
                    o[47] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton)}, deviceRef);
                    o[48] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton_AsAxisOneWay)}, deviceRef);
                    o[49] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay)}, deviceRef);
                    o[50] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay_AsButton)}, deviceRef);
                    o[51] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay)}, deviceRef);
                    o[52] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay_AsButton)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 53 elements");
                return 53;
            case InputDeviceTraitBuiltIn::DualSense:
                if (o != nullptr && count == 8)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton)}, deviceRef);
                    o[1] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton_AsAxisOneWay)}, deviceRef);
                    o[2] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton)}, deviceRef);
                    o[3] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton_AsAxisOneWay)}, deviceRef);
                    o[4] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton)}, deviceRef);
                    o[5] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton_AsAxisOneWay)}, deviceRef);
                    o[6] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton)}, deviceRef);
                    o[7] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton_AsAxisOneWay)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 8 elements");
                return 8;
            case InputDeviceTraitBuiltIn::GenericControls:
                if (o != nullptr && count == 144)
                {
                    o[0] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button)}, deviceRef);
                    o[1] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button_AsAxisOneWay)}, deviceRef);
                    o[2] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button)}, deviceRef);
                    o[3] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button_AsAxisOneWay)}, deviceRef);
                    o[4] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button)}, deviceRef);
                    o[5] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button_AsAxisOneWay)}, deviceRef);
                    o[6] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button)}, deviceRef);
                    o[7] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button_AsAxisOneWay)}, deviceRef);
                    o[8] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button)}, deviceRef);
                    o[9] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button_AsAxisOneWay)}, deviceRef);
                    o[10] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button)}, deviceRef);
                    o[11] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button_AsAxisOneWay)}, deviceRef);
                    o[12] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button)}, deviceRef);
                    o[13] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button_AsAxisOneWay)}, deviceRef);
                    o[14] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button)}, deviceRef);
                    o[15] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button_AsAxisOneWay)}, deviceRef);
                    o[16] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button)}, deviceRef);
                    o[17] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button_AsAxisOneWay)}, deviceRef);
                    o[18] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button)}, deviceRef);
                    o[19] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button_AsAxisOneWay)}, deviceRef);
                    o[20] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button)}, deviceRef);
                    o[21] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button_AsAxisOneWay)}, deviceRef);
                    o[22] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button)}, deviceRef);
                    o[23] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button_AsAxisOneWay)}, deviceRef);
                    o[24] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button)}, deviceRef);
                    o[25] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button_AsAxisOneWay)}, deviceRef);
                    o[26] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button)}, deviceRef);
                    o[27] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button_AsAxisOneWay)}, deviceRef);
                    o[28] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button)}, deviceRef);
                    o[29] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button_AsAxisOneWay)}, deviceRef);
                    o[30] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button)}, deviceRef);
                    o[31] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button_AsAxisOneWay)}, deviceRef);
                    o[32] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay)}, deviceRef);
                    o[33] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay_AsButton)}, deviceRef);
                    o[34] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay)}, deviceRef);
                    o[35] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay_AsButton)}, deviceRef);
                    o[36] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay)}, deviceRef);
                    o[37] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay_AsButton)}, deviceRef);
                    o[38] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay)}, deviceRef);
                    o[39] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay_AsButton)}, deviceRef);
                    o[40] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay)}, deviceRef);
                    o[41] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay_AsButton)}, deviceRef);
                    o[42] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay)}, deviceRef);
                    o[43] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay_AsButton)}, deviceRef);
                    o[44] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay)}, deviceRef);
                    o[45] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay_AsButton)}, deviceRef);
                    o[46] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay)}, deviceRef);
                    o[47] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay_AsButton)}, deviceRef);
                    o[48] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay)}, deviceRef);
                    o[49] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay_AsButton)}, deviceRef);
                    o[50] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay)}, deviceRef);
                    o[51] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay_AsButton)}, deviceRef);
                    o[52] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay)}, deviceRef);
                    o[53] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay_AsButton)}, deviceRef);
                    o[54] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay)}, deviceRef);
                    o[55] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay_AsButton)}, deviceRef);
                    o[56] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay)}, deviceRef);
                    o[57] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay_AsButton)}, deviceRef);
                    o[58] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay)}, deviceRef);
                    o[59] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay_AsButton)}, deviceRef);
                    o[60] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay)}, deviceRef);
                    o[61] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay_AsButton)}, deviceRef);
                    o[62] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay)}, deviceRef);
                    o[63] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay_AsButton)}, deviceRef);
                    o[64] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}, deviceRef);
                    o[65] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[66] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[67] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveButton)}, deviceRef);
                    o[68] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeButton)}, deviceRef);
                    o[69] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}, deviceRef);
                    o[70] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[71] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[72] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveButton)}, deviceRef);
                    o[73] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeButton)}, deviceRef);
                    o[74] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}, deviceRef);
                    o[75] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[76] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[77] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveButton)}, deviceRef);
                    o[78] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeButton)}, deviceRef);
                    o[79] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}, deviceRef);
                    o[80] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[81] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[82] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveButton)}, deviceRef);
                    o[83] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeButton)}, deviceRef);
                    o[84] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}, deviceRef);
                    o[85] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[86] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[87] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveButton)}, deviceRef);
                    o[88] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeButton)}, deviceRef);
                    o[89] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}, deviceRef);
                    o[90] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[91] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[92] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveButton)}, deviceRef);
                    o[93] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeButton)}, deviceRef);
                    o[94] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}, deviceRef);
                    o[95] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[96] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[97] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveButton)}, deviceRef);
                    o[98] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeButton)}, deviceRef);
                    o[99] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}, deviceRef);
                    o[100] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[101] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[102] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveButton)}, deviceRef);
                    o[103] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeButton)}, deviceRef);
                    o[104] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}, deviceRef);
                    o[105] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[106] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[107] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveButton)}, deviceRef);
                    o[108] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeButton)}, deviceRef);
                    o[109] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}, deviceRef);
                    o[110] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[111] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[112] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveButton)}, deviceRef);
                    o[113] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeButton)}, deviceRef);
                    o[114] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}, deviceRef);
                    o[115] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[116] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[117] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveButton)}, deviceRef);
                    o[118] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeButton)}, deviceRef);
                    o[119] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}, deviceRef);
                    o[120] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[121] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[122] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveButton)}, deviceRef);
                    o[123] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeButton)}, deviceRef);
                    o[124] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}, deviceRef);
                    o[125] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[126] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[127] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveButton)}, deviceRef);
                    o[128] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeButton)}, deviceRef);
                    o[129] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}, deviceRef);
                    o[130] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[131] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[132] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveButton)}, deviceRef);
                    o[133] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeButton)}, deviceRef);
                    o[134] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}, deviceRef);
                    o[135] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[136] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[137] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveButton)}, deviceRef);
                    o[138] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeButton)}, deviceRef);
                    o[139] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}, deviceRef);
                    o[140] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay)}, deviceRef);
                    o[141] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay)}, deviceRef);
                    o[142] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveButton)}, deviceRef);
                    o[143] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeButton)}, deviceRef);}
                else if (o != nullptr)
                    InputAssert(false, "Please provide 144 elements");
                return 144;
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, void* traitPointer, const InputDeviceRef deviceRef)->void // ConfigureTraitInstance
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
            case InputDeviceTraitBuiltIn::ExplicitlyPollableDevice: *reinterpret_cast<InputExplicitlyPollableDevice*>(traitPointer) = InputExplicitlyPollableDevice::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::Keyboard: *reinterpret_cast<InputKeyboard*>(traitPointer) = InputKeyboard::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::Pointer: *reinterpret_cast<InputPointer*>(traitPointer) = InputPointer::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::Mouse: *reinterpret_cast<InputMouse*>(traitPointer) = InputMouse::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::Gamepad: *reinterpret_cast<InputGamepad*>(traitPointer) = InputGamepad::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::DualSense: *reinterpret_cast<InputDualSense*>(traitPointer) = InputDualSense::Setup(deviceRef); break;
            case InputDeviceTraitBuiltIn::GenericControls: *reinterpret_cast<InputGenericControls*>(traitPointer) = InputGenericControls::Setup(deviceRef); break;
            default: break;
            }
        },
        [](const InputControlUsage usage)->InputDatabaseControlUsageDescr // GetControlUsageDescr
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
            case InputControlUsageBuiltIn::Keyboard_EscapeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_EscapeButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_SpaceButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_SpaceButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_EnterButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_EnterButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_TabButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_TabButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_QuoteButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_QuoteButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_CommaButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_CommaButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PeriodButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PeriodButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_SlashButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_SlashButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_BackslashButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_BackslashButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_MinusButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_MinusButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_EqualsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_EqualsButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_AButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_AButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_BButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_BButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_CButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_CButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_DButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_DButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_EButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_EButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_FButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_FButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_GButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_GButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_HButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_HButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_IButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_IButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_JButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_JButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_KButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_KButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_MButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_MButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_OButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_QButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_QButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_SButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_SButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_TButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_TButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_UButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_UButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_VButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_VButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_WButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_WButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_XButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_XButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_YButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_YButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_ZButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_ZButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit1Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit1Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit2Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit2Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit3Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit3Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit4Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit4Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit5Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit5Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit6Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit6Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit7Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit7Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit8Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit8Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit9Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit9Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Digit0Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Digit0Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_ShiftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_ShiftButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightAltButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightAltButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_AltButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_AltButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_CtrlButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_CtrlButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PageDownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PageDownButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PageUpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PageUpButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_HomeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_HomeButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_EndButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_EndButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_InsertButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_InsertButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_DeleteButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_DeleteButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumLockButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumLockButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_PauseButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_PauseButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F1Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F1Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F2Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F2Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F3Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F3Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F4Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F4Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F5Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F5Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F6Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F6Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F7Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F7Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F8Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F8Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F9Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F9Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F10Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F10Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F11Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F11Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_F12Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_F12Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_OEM1Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OEM1Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_OEM2Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OEM2Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_OEM3Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OEM3Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_OEM4Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OEM4Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button)}

            };
            case InputControlUsageBuiltIn::Keyboard_OEM5Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Keyboard_OEM5Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button)}

            };
            case InputControlUsageBuiltIn::Pointer_PositionPosition2D: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Position2D)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaVector2D)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_UpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_DownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaVector2D)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_UpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_DownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}

            };
            case InputControlUsageBuiltIn::Mouse_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_LeftButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton)}

            };
            case InputControlUsageBuiltIn::Mouse_MiddleButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_MiddleButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton)}

            };
            case InputControlUsageBuiltIn::Mouse_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_RightButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton)}

            };
            case InputControlUsageBuiltIn::Mouse_BackButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_BackButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton)}

            };
            case InputControlUsageBuiltIn::Mouse_ForwardButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Mouse_ForwardButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_WestButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_WestButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_NorthButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_NorthButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_EastButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_EastButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_SouthButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_SouthButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_VerticalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_HorizontalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_VerticalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_HorizontalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightStickButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_RightStickButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_VerticalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_HorizontalAxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton)}

            };
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay)}

            };
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay)}

            };
            case InputControlUsageBuiltIn::DualSense_OptionsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::DualSense_OptionsButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton)}

            };
            case InputControlUsageBuiltIn::DualSense_ShareButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::DualSense_ShareButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton)}

            };
            case InputControlUsageBuiltIn::DualSense_PlaystationButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::DualSense_PlaystationButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton)}

            };
            case InputControlUsageBuiltIn::DualSense_MicButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::DualSense_MicButton_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15Button: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15Button_AsAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay_AsButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)},
                InputControlRecordingMode::LatestOnly,
                InputControlUsageInvalid

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)},
                InputControlRecordingMode::LatestOnly,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}

            };
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeButton: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)},
                InputControlRecordingMode::AllMerged,
                {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}

            };
            default:
                return {};
            }
        },
        [](const InputControlTypeRef controlTypeRef)->InputDatabaseControlTypeDescr // GetControlTypeDescr
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Button: return {
                sizeof(InputButtonControlState),
                sizeof(InputButtonControlSample)
            };
            case InputControlTypeBuiltIn::AxisOneWay: return {
                sizeof(InputAxisOneWayControlState),
                sizeof(InputAxisOneWayControlSample)
            };
            case InputControlTypeBuiltIn::AxisTwoWay: return {
                sizeof(InputAxisTwoWayControlState),
                sizeof(InputAxisTwoWayControlSample)
            };
            case InputControlTypeBuiltIn::DeltaAxisTwoWay: return {
                sizeof(InputDeltaAxisTwoWayControlState),
                sizeof(InputDeltaAxisTwoWayControlSample)
            };
            case InputControlTypeBuiltIn::Stick: return {
                sizeof(InputStickControlState),
                sizeof(InputStickControlSample)
            };
            case InputControlTypeBuiltIn::DeltaVector2D: return {
                sizeof(InputDeltaVector2DControlState),
                sizeof(InputDeltaVector2DControlSample)
            };
            case InputControlTypeBuiltIn::Position2D: return {
                sizeof(uint8_t),
                sizeof(uint8_t)
            };
            default:
                return {};
            }
        },
        // TODO replace guid to id lookups with hashmap
        [](const InputGuid g)->InputDatabaseDeviceAssignedRef // GetDeviceAssignedRef
        {
            if (g.a == 0x1d4b8e4584e8378dull && g.b == 0xd1e9875942955f80ull) return {static_cast<uint32_t>(InputDeviceBuiltIn::KeyboardWindows)}; // 8d37e884-458e-4b1d-805f-95425987e9d1
            if (g.a == 0xd0454b7c1e5242b6ull && g.b == 0x22aa86e78460b7b3ull) return {static_cast<uint32_t>(InputDeviceBuiltIn::MouseMacOS)}; // b642521e-7c4b-45d0-b3b7-6084e786aa22
            if (g.a == 0x8944989cda9608ffull && g.b == 0x72c36241244bc394ull) return {static_cast<uint32_t>(InputDeviceBuiltIn::WindowsGamingInputGamepad)}; // ff0896da-9c98-4489-94c3-4b244162c372
            return InputDatabaseDeviceAssignedRefInvalid;
        },
        [](const InputGuid g)->InputDeviceTraitRef // GetTraitAssignedRef
        {
            if (g.a == 0xa34b2937e75892e1ull && g.b == 0x095ab1333d3b3e82ull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::ExplicitlyPollableDevice)}; // e19258e7-3729-4ba3-823e-3b3d33b15a09
            if (g.a == 0x8e43e7021b5ff12dull && g.b == 0xcfa4150ef44e429bull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Keyboard)}; // 2df15f1b-02e7-438e-9b42-4ef40e15a4cf
            if (g.a == 0xa04c398fdd44e771ull && g.b == 0xde47478f4437979eull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Pointer)}; // 71e744dd-8f39-4ca0-9e97-37448f4747de
            if (g.a == 0x2746517107bbb030ull && g.b == 0x84f5a2e5adaed191ull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Mouse)}; // 30b0bb07-7151-4627-91d1-aeade5a2f584
            if (g.a == 0xf2413a3793ae989full && g.b == 0x2c7a5863f1d96aacull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::Gamepad)}; // 9f98ae93-373a-41f2-ac6a-d9f163587a2c
            if (g.a == 0xe548955d73756725ull && g.b == 0x1ad3251b5a8b5c86ull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::DualSense)}; // 25677573-5d95-48e5-865c-8b5a1b25d31a
            if (g.a == 0x2049beedd5166fd5ull && g.b == 0x8e36e44e12afafbcull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::GenericControls)}; // d56f16d5-edbe-4920-bcaf-af124ee4368e
            return InputDeviceTraitRefInvalid;
        },
        [](const InputGuid g)->InputControlUsage // GetControlUsage
        {
            if (g.a == 0x68478fd8caae6ae4ull && g.b == 0x1ea321de9aa5f7a1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton)}; // e46aaeca-d88f-4768-a1f7-a59ade21a31e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EscapeButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd6440b56f57ee776ull && g.b == 0x11ba607435ef8090ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton)}; // 76e77ef5-560b-44d6-9080-ef357460ba11
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SpaceButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0e4ab3d4ecd2c1dbull && g.b == 0xcb1c3ea36aee5fbdull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton)}; // dbc1d2ec-d4b3-4a0e-bd5f-ee6aa33e1ccb
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EnterButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x994a28b6a000454dull && g.b == 0xc374dc727ea95780ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton)}; // 4d4500a0-b628-4a99-8057-a97e72dc74c3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TabButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xa34cd1155f569e3eull && g.b == 0x7ef2534c8c7189abull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton)}; // 3e9e565f-15d1-4ca3-ab89-718c4c53f27e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackquoteButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xfd42279c4a5bdef2ull && g.b == 0x4d3d889869db2781ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton)}; // f2de5b4a-9c27-42fd-8127-db6998883d4d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QuoteButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x8c42aef17367e2adull && g.b == 0x20fcfbc6291fca88ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton)}; // ade26773-f1ae-428c-88ca-1f29c6fbfc20
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SemicolonButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x534eb5c11cc79268ull && g.b == 0x7938c8a2851a1d9aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton)}; // 6892c71c-c1b5-4e53-9a1d-1a85a2c83879
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CommaButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe44a420b086bd6ebull && g.b == 0xcadc424061b54695ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton)}; // ebd66b08-0b42-4ae4-9546-b5614042dcca
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PeriodButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd9452c828e103867ull && g.b == 0xe438928c9105a487ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton)}; // 6738108e-822c-45d9-87a4-05918c9238e4
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SlashButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x444ce1386f21eea6ull && g.b == 0x8f25494649e52eb1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton)}; // a6ee216f-38e1-4c44-b12e-e5494649258f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackslashButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x8041dbaaf8286f13ull && g.b == 0x3bb61724a6cdeab7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton)}; // 136f28f8-aadb-4180-b7ea-cda62417b63b
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftBracketButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb94abe9c5963d506ull && g.b == 0xba49dffd68e6d4a6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton)}; // 06d56359-9cbe-4ab9-a6d4-e668fddf49ba
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightBracketButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x494888d4a794047cull && g.b == 0x35ed521944ca43b8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton)}; // 7c0494a7-d488-4849-b843-ca441952ed35
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MinusButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x524f7bef4f344facull && g.b == 0x00b5a258b3d452bbull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton)}; // ac4f344f-ef7b-4f52-bb52-d4b358a2b500
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EqualsButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb643ac22b9140078ull && g.b == 0x625876a5af4a7c9bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton)}; // 780014b9-22ac-43b6-9b7c-4aafa5765862
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UpArrowButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x704856cf670f4abfull && g.b == 0x66e38dbde894aa8dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton)}; // bf4a0f67-cf56-4870-8daa-94e8bd8de366
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DownArrowButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x7a41de905e226747ull && g.b == 0x2deb7c208324a8b9ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton)}; // 4767225e-90de-417a-b9a8-2483207ceb2d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftArrowButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5a40fc89377cbf5cull && g.b == 0x432e06d1dd7b85acull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton)}; // 5cbf7c37-89fc-405a-ac85-7bddd1062e43
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightArrowButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0e41e8c887617e10ull && g.b == 0xd6a20a268f0cc4a0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton)}; // 107e6187-c8e8-410e-a0c4-0c8f260aa2d6
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb54592b69f9f8576ull && g.b == 0xf0af3216ec454caaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton)}; // 76859f9f-b692-45b5-aa4c-45ec1632aff0
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd64b13b1c7c0e74aull && g.b == 0x62d2a6bef07eea86ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton)}; // 4ae7c0c7-b113-4bd6-86ea-7ef0bea6d262
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x554dcc71cda33545ull && g.b == 0xd61307dd6c70bc92ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton)}; // 4535a3cd-71cc-4d55-92bc-706cdd0713d6
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb949e541906f8a39ull && g.b == 0x2099c04e9e3aaa87ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton)}; // 398a6f90-41e5-49b9-87aa-3a9e4ec09920
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xaa47980ddf7e276bull && g.b == 0xf32c4defd00cfaafull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton)}; // 6b277edf-0d98-47aa-affa-0cd0ef4d2cf3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_FButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xce48f93e8552f8eaull && g.b == 0x2da00213b750699bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton)}; // eaf85285-3ef9-48ce-9b69-50b71302a02d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_GButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x4f44185f6e065bacull && g.b == 0xebc7277d582d3f9full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton)}; // ac5b066e-5f18-444f-9f3f-2d587d27c7eb
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x9c4e4e63533b9f5full && g.b == 0xe329650c351d9ea4ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton)}; // 5f9f3b53-634e-4e9c-a49e-1d350c6529e3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_IButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x7d44b2dbdc40d333ull && g.b == 0x74132f4a56f45480ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton)}; // 33d340dc-dbb2-447d-8054-f4564a2f1374
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_JButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x844c26c4387ea323ull && g.b == 0x01c7808018b9e682ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton)}; // 23a37e38-c426-4c84-82e6-b9188080c701
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_KButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0642783b2324caa7ull && g.b == 0x8b0922ecebb2538bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton)}; // a7ca2423-3b78-4206-8b53-b2ebec22098b
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x534f616e700931a2ull && g.b == 0x077a05e34b28aebcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton)}; // a2310970-6e61-4f53-bcae-284be3057a07
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_MButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x8543319faa544a34ull && g.b == 0x972f01240d3685beull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton)}; // 344a54aa-9f31-4385-be85-360d24012f97
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x674a8fde67caa26eull && g.b == 0x528b6f6e636d28a4ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton)}; // 6ea2ca67-de8f-4a67-a428-6d636e6f8b52
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x4841455dbeb75f0cull && g.b == 0x859573168421b3b6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton)}; // 0c5fb7be-5d45-4148-b6b3-218416739585
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x494b8cc5d4807d69ull && g.b == 0x66101af40034b093ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton)}; // 697d80d4-c58c-4b49-93b0-3400f41a1066
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_QButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3f4cf4365f50da8full && g.b == 0xa7512eba02a42bb7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton)}; // 8fda505f-36f4-4c3f-b72b-a402ba2e51a7
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0a47b10386eb0c51ull && g.b == 0x0d08157613f17ca4ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton)}; // 510ceb86-03b1-470a-a47c-f1137615080d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_SButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1d4928b022d6ae93ull && g.b == 0xcc4bda13a56283bcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton)}; // 93aed622-b028-491d-bc83-62a513da4bcc
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_TButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x474f54dbdddb670bull && g.b == 0x0142d545b76670a8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton)}; // 0b67dbdd-db54-4f47-a870-66b745d54201
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_UButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3d4cd6da5d80d793ull && g.b == 0xd4de83464cc70cbaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton)}; // 93d7805d-dad6-4c3d-ba0c-c74c4683ded4
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_VButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xa54f24feffafdbb9ull && g.b == 0x0e4eb7d84820ed8full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton)}; // b9dbafff-fe24-4fa5-8fed-2048d8b74e0e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_WButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x7d49e3ef8196fab2ull && g.b == 0xa5597af769d52780ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton)}; // b2fa9681-efe3-497d-8027-d569f77a59a5
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_XButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x504b936d7ffb418aull && g.b == 0xb31e7581416e22bcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton)}; // 8a41fb7f-6d93-4b50-bc22-6e4181751eb3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_YButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x784cf45431a138c4ull && g.b == 0x4c7bf509d7ed33a6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton)}; // c438a131-54f4-4c78-a633-edd709f57b4c
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ZButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x9b45583d09209be4ull && g.b == 0x9c80c8316640d1abull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button)}; // e49b2009-3d58-459b-abd1-406631c8809c
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit1Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1f437b1cd04a46fcull && g.b == 0x4c84f5e9f38a119bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button)}; // fc464ad0-1c7b-431f-9b11-8af3e9f5844c
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit2Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x4245e3252b0b3797ull && g.b == 0x5a971b11863a1ba1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button)}; // 97370b2b-25e3-4542-a11b-3a86111b975a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit3Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x2f433d19fe45f871ull && g.b == 0xe9d7e5a71d7b66a1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button)}; // 71f845fe-193d-432f-a166-7b1da7e5d7e9
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit4Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0a467831ffed5416ull && g.b == 0xcfa8ccb2a21ceeacull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button)}; // 1654edff-3178-460a-acee-1ca2b2cca8cf
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit5Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x144da16bc52b3d25ull && g.b == 0xca7220ba340b948bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button)}; // 253d2bc5-6ba1-4d14-8b94-0b34ba2072ca
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit6Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x98420e509774d848ull && g.b == 0xa160ae82336e46b0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button)}; // 48d87497-500e-4298-b046-6e3382ae60a1
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit7Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xca4a705cb110e3a9ull && g.b == 0x2983648c2a6ce1b7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button)}; // a9e310b1-5c70-4aca-b7e1-6c2a8c648329
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit8Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xba49ebf881c6f1bdull && g.b == 0x1090ca4e495a119dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button)}; // bdf1c681-f8eb-49ba-9d11-5a494eca9010
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit9Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5a4d5d6ec126d5e0ull && g.b == 0xcd2674a471cf77a1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button)}; // e0d526c1-6e5d-4d5a-a177-cf71a47426cd
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Digit0Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x6348cda1390bb3d3ull && g.b == 0x4308fe1c8d66cd90ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton)}; // d3b30b39-a1cd-4863-90cd-668d1cfe0843
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftShiftButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x9c4e423e924f8aa2ull && g.b == 0x4b90e7f6a90e7cbfull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton)}; // a28a4f92-3e42-4e9c-bf7c-0ea9f6e7904b
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightShiftButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x354d97c5bbd53b2cull && g.b == 0x24b9a537452b9484ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton)}; // 2c3bd5bb-c597-4d35-8494-2b4537a5b924
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ShiftButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x034a0c6a42f01a80ull && g.b == 0x2f0532b9c4a514bfull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton)}; // 801af042-6a0c-4a03-bf14-a5c4b932052f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftAltButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x4845d77b80ea9a3full && g.b == 0x5e4ddbaec2dc81b0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton)}; // 3f9aea80-7bd7-4548-b081-dcc2aedb4d5e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightAltButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x214206c827335b5full && g.b == 0xd06f676f6fcacfb4ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton)}; // 5f5b3327-c806-4221-b4cf-ca6f6f676fd0
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_AltButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb844c45a1ececa7aull && g.b == 0xe7a0cb8efb376692ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton)}; // 7acace1e-5ac4-44b8-9266-37fb8ecba0e7
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftCtrlButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x774db170f07199e7ull && g.b == 0x29c9e65dc22d73a9ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton)}; // e79971f0-70b1-4d77-a973-2dc25de6c929
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightCtrlButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x8a46b90c24ed911cull && g.b == 0x2481b0f26a3d1ca6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton)}; // 1c91ed24-0cb9-468a-a61c-3d6af2b08124
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CtrlButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd6479a32c785f92dull && g.b == 0xcf38effaf7bd40abull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton)}; // 2df985c7-329a-47d6-ab40-bdf7faef38cf
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_LeftMetaButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5d4b152d17a05cb8ull && g.b == 0x84d0e33e08a04591ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton)}; // b85ca017-2d15-4b5d-9145-a0083ee3d084
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_RightMetaButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x70448a8d32ffa707ull && g.b == 0x0eede33cf46d2497ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton)}; // 07a7ff32-8d8a-4470-9724-6df43ce3ed0e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ContextMenuButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xf847da8c28a9299cull && g.b == 0x7a2375f580b49b90ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton)}; // 9c29a928-8cda-47f8-909b-b480f575237a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_BackspaceButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe7410f2afadb426dull && g.b == 0x1d1575a44655f384ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton)}; // 6d42dbfa-2a0f-41e7-84f3-5546a475151d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageDownButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0b4d4fff5ae24eb8ull && g.b == 0x62bef4eec69b57a6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton)}; // b84ee25a-ff4f-4d0b-a657-9bc6eef4be62
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PageUpButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x504deaca6ace06a2ull && g.b == 0x0d033d99ab480697ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton)}; // a206ce6a-caea-4d50-9706-48ab993d030d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_HomeButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe441b3b0858b7e38ull && g.b == 0x5ae2b590d565268eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton)}; // 387e8b85-b0b3-41e4-8e26-65d590b5e25a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_EndButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb140a743f8e7cc68ull && g.b == 0x03d0ddba2c0b9bb0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton)}; // 68cce7f8-43a7-40b1-b09b-0b2cbaddd003
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_InsertButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x2448c4d6eae3769full && g.b == 0x0f95e41d5578b6a4ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton)}; // 9f76e3ea-d6c4-4824-a4b6-78551de4950f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_DeleteButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x884f5a3c72f3d451ull && g.b == 0x560874df62717cbaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton)}; // 51d4f372-3c5a-4f88-ba7c-7162df740856
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_CapsLockButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x7943be2009c09c36ull && g.b == 0x17ecffacac9f1a85ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton)}; // 369cc009-20be-4379-851a-9facacffec17
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumLockButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x114241d7b4c1e997ull && g.b == 0xdfb79d80d6e11da0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton)}; // 97e9c1b4-d741-4211-a01d-e1d6809db7df
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PrintScreenButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x8543f2abc4a857bdull && g.b == 0xf00ab2522ae28285ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton)}; // bd57a8c4-abf2-4385-8582-e22a52b20af0
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_ScrollLockButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1d4025ea0806e773ull && g.b == 0x4f8b526458574396ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton)}; // 73e70608-ea25-401d-9643-575864528b4f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_PauseButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5341c59b3ba50121ull && g.b == 0x5a215b226d7f0d8eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton)}; // 2101a53b-9bc5-4153-8e0d-7f6d225b215a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEnterButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x55498908317e34cdull && g.b == 0x2a8c64ef5f2e9a91ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton)}; // cd347e31-0889-4955-919a-2e5fef648c2a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadDivideButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe44835c66fe398a9ull && g.b == 0x3005001a6b1313bdull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton)}; // a998e36f-c635-48e4-bd13-136b1a000530
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb74f0c20bb566f97ull && g.b == 0x4153c1018b1921b3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton)}; // 976f56bb-200c-4fb7-b321-198b01c15341
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPlusButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3c4c5a36510cca26ull && g.b == 0x005c5c4a936c65beull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton)}; // 26ca0c51-365a-4c3c-be65-6c934a5c5c00
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadMinusButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x97485f8f41b4c6b7ull && g.b == 0x2b8bebe9679e24a8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton)}; // b7c6b441-8f5f-4897-a824-9e67e9eb8b2b
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x504a39fabe7b26e0ull && g.b == 0xff623fce649e06b3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton)}; // e0267bbe-fa39-4a50-b306-9e64ce3f62ff
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xa1492af531a99f03ull && g.b == 0x478f095bf5a4ce83ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button)}; // 039fa931-f52a-49a1-83ce-a4f55b098f47
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad1Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe34a9db2689cb691ull && g.b == 0xebbe176719a20d97ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button)}; // 91b69c68-b29d-4ae3-970d-a2196717beeb
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad2Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x7e45ff9cdb636cceull && g.b == 0xa46e2dc70646629cull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button)}; // ce6c63db-9cff-457e-9c62-4606c72d6ea4
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad3Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x2d4ef3e1d7a2270dull && g.b == 0xd35810ee6c23399bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button)}; // 0d27a2d7-e1f3-4e2d-9b39-236cee1058d3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad4Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xa04e059fbdef0d8full && g.b == 0x5e411976eb3d2ca2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button)}; // 8f0defbd-9f05-4ea0-a22c-3deb7619415e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad5Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x9443828a8cd9aef3ull && g.b == 0x130cb876a1df9782ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button)}; // f3aed98c-8a82-4394-8297-dfa176b80c13
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad6Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1e4ba103bfcff072ull && g.b == 0x4ddee0a487e4a9bdull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button)}; // 72f0cfbf-03a1-4b1e-bda9-e487a4e0de4d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad7Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xfc4215af21559bc2ull && g.b == 0x3d1168aba14c9da8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button)}; // c29b5521-af15-42fc-a89d-4ca1ab68113d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad8Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0c4cc73defc6e4c0ull && g.b == 0x0335df1738ed9aa9ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button)}; // c0e4c6ef-3dc7-4c0c-a99a-ed3817df3503
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad9Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe44a760d8335a7ffull && g.b == 0x376ff5d95514c29dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button)}; // ffa73583-0d76-4ae4-9dc2-1455d9f56f37
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_Numpad0Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0e45cbea0683540bull && g.b == 0xcd69132025b1a7a0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button)}; // 0b548306-eacb-450e-a0a7-b125201369cd
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F1Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5a4bf4824d6f9ca5ull && g.b == 0x8ee9071fe79639a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button)}; // a59c6f4d-82f4-4b5a-a339-96e71f07e98e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F2Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0a481153e2769482ull && g.b == 0xb3195727381155a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button)}; // 829476e2-5311-480a-a255-1138275719b3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F3Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xb943cd07b686afb9ull && g.b == 0x65c7f3b29ee36192ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button)}; // b9af86b6-07cd-43b9-9261-e39eb2f3c765
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F4Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xf549837d89e50020ull && g.b == 0xb0458ca612fdc8a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button)}; // 2000e589-7d83-49f5-a3c8-fd12a68c45b0
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F5Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0740784d4825e65dull && g.b == 0xa79fcc29ab7363b6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button)}; // 5de62548-4d78-4007-b663-73ab29cc9fa7
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F6Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x42423d3fbaa2de19ull && g.b == 0x1e1cc866969021a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button)}; // 19dea2ba-3f3d-4242-a321-909666c81c1e
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F7Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe74610ef7310aa1aull && g.b == 0xecbe967ee0bc5f99ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button)}; // 1aaa1073-ef10-46e7-995f-bce07e96beec
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F8Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xfb4bc619ed656737ull && g.b == 0xfcf10a826ee61695ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button)}; // 376765ed-19c6-4bfb-9516-e66e820af1fc
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F9Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x274223532fca3239ull && g.b == 0x1ac0377bc3e2ff8dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button)}; // 3932ca2f-5323-4227-8dff-e2c37b37c01a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F10Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xfd41ce91430ae5daull && g.b == 0x38bc2921253fca9cull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button)}; // dae50a43-91ce-41fd-9cca-3f252129bc38
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F11Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd44244170ae086d6ull && g.b == 0x0601569c0f0f2daeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button)}; // d686e00a-1744-42d4-ae2d-0f0f9c560106
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_F12Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3f4e2f7bf965c334ull && g.b == 0xef497cf3f3bfeab2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button)}; // 34c365f9-7b2f-4e3f-b2ea-bff3f37c49ef
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM1Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xd242dbb93848741eull && g.b == 0x0fc8f2b8933791b2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button)}; // 1e744838-b9db-42d2-b291-3793b8f2c80f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM2Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe94149e80831d8cbull && g.b == 0x97f2ba9891e5d7bfull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button)}; // cbd83108-e849-41e9-bfd7-e59198baf297
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM3Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x774e52f426354b3full && g.b == 0x503f2b96bfb627baull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button)}; // 3f4b3526-f452-4e77-ba27-b6bf962b3f50
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM4Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xce452f1f67d22aeaull && g.b == 0x92feec2011836098ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button)}; // ea2ad267-1f2f-45ce-9860-831120ecfe92
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Keyboard_OEM5Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xcc4a1dee63bb9131ull && g.b == 0xc40d4dd944fc0a8bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Pointer_PositionPosition2D)}; // 3191bb63-ee1d-4acc-8b0a-fc44d94d0dc4
            if (g.a == 0x9a4bbfcf4fba4fc3ull && g.b == 0xc1988c0527de23bdull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D)}; // c34fba4f-cfbf-4b9a-bd23-de27058c98c1
            if (g.a == 0x144d63918950b67bull && g.b == 0xdc8d25768e628fafull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay)}; // 7bb65089-9163-4d14-af8f-628e76258ddc
            if (g.a == 0xe347be75920d82cfull && g.b == 0xdb1f821f1a02b095ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay)}; // cf820d92-75be-47e3-95b0-021a1f821fdb
            if (g.a == 0x0a41127a7a78b1a9ull && g.b == 0x4fcd00f649c95cb5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_LeftButton)}; // a9b1787a-7a12-410a-b55c-c949f600cd4f
            if (g.a == 0xc44c053d449b4983ull && g.b == 0xdad3a6829047a4a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_UpButton)}; // 83499b44-3d05-4cc4-a2a4-479082a6d3da
            if (g.a == 0xf74ad47a3d67c3f7ull && g.b == 0xd02f4ce71392139full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_RightButton)}; // f7c3673d-7ad4-4af7-9f13-9213e74c2fd0
            if (g.a == 0x5349cfc620c0bc79ull && g.b == 0xa36e146fd2af939eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_DownButton)}; // 79bcc020-c6cf-4953-9e93-afd26f146ea3
            if (g.a == 0x3842dfc1ec82d94aull && g.b == 0xa09ad4def965db8full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D)}; // 4ad982ec-c1df-4238-8fdb-65f9ded49aa0
            if (g.a == 0x144d63918950b67bull && g.b == 0xdc8d25768e628fafull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay)}; // 7bb65089-9163-4d14-af8f-628e76258ddc
            if (g.a == 0xe347be75920d82cfull && g.b == 0xdb1f821f1a02b095ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay)}; // cf820d92-75be-47e3-95b0-021a1f821fdb
            if (g.a == 0x0a41127a7a78b1a9ull && g.b == 0x4fcd00f649c95cb5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_LeftButton)}; // a9b1787a-7a12-410a-b55c-c949f600cd4f
            if (g.a == 0xc44c053d449b4983ull && g.b == 0xdad3a6829047a4a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_UpButton)}; // 83499b44-3d05-4cc4-a2a4-479082a6d3da
            if (g.a == 0xf74ad47a3d67c3f7ull && g.b == 0xd02f4ce71392139full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_RightButton)}; // f7c3673d-7ad4-4af7-9f13-9213e74c2fd0
            if (g.a == 0x5349cfc620c0bc79ull && g.b == 0xa36e146fd2af939eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_DownButton)}; // 79bcc020-c6cf-4953-9e93-afd26f146ea3
            if (g.a == 0x2e47bc74227bd626ull && g.b == 0xb9ecd2b60ea63e81ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton)}; // 26d67b22-74bc-472e-813e-a60eb6d2ecb9
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_LeftButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x0840bb232edfb23eull && g.b == 0x126c5f3eab84e7adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton)}; // 3eb2df2e-23bb-4008-ade7-84ab3e5f6c12
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_MiddleButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe74bfd49efaf4cd3ull && g.b == 0x960aaaf1f00c7d9eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton)}; // d34cafef-49fd-4be7-9e7d-0cf0f1aa0a96
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_RightButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3542cc60de49f80cull && g.b == 0x1bece2c19b223194ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton)}; // 0cf849de-60cc-4235-9431-229bc1e2ec1b
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_BackButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x6c466f6de46a8222ull && g.b == 0x0ac10d4002e2348aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton)}; // 22826ae4-6d6f-466c-8a34-e202400dc10a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Mouse_ForwardButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x19424de08e79fcf4ull && g.b == 0xfdeb84559816b888ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton)}; // f4fc798e-e04d-4219-88b8-16985584ebfd
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_WestButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xe1428cb50f2367ceull && g.b == 0x90a48034c0121487ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton)}; // ce67230f-b58c-42e1-8714-12c03480a490
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_NorthButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xa84d91203413ccf2ull && g.b == 0x741ffcea7cda7d9eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton)}; // f2cc1334-2091-4da8-9e7d-da7ceafc1f74
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_EastButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x764b1deffce6a455ull && g.b == 0xc3a69026938fe89aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton)}; // 55a4e6fc-ef1d-4b76-9ae8-8f932690a6c3
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_SouthButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3b4a02fc745facdbull && g.b == 0x92345be67f875fbaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick)}; // dbac5f74-fc02-4a3b-ba5f-877fe65b3492
            if (g.a == 0x4b4a0a77987db54dull && g.b == 0xd68b54aa2459f79aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_VerticalAxisTwoWay)}; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            if (g.a == 0x8348948fb2d810a1ull && g.b == 0x302653e80491c49aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_HorizontalAxisTwoWay)}; // a110d8b2-8f94-4883-9ac4-9104e8532630
            if (g.a == 0x8b4506b3a4f8a96eull && g.b == 0x520cb923663c3ca8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_LeftAxisOneWay)}; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            if (g.a == 0xaf46c6827cd1bb74ull && g.b == 0xf1a365a2104e96a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_UpAxisOneWay)}; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            if (g.a == 0xb74e98d8b6ccdbdaull && g.b == 0x96fb0226c8c871a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_RightAxisOneWay)}; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            if (g.a == 0x77494296e38ad398ull && g.b == 0x98455f57f6d071b3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_DownAxisOneWay)}; // 98d38ae3-9642-4977-b371-d0f6575f4598
            if (g.a == 0x0745080b088bab76ull && g.b == 0xceb6b951d69035a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_LeftButton)}; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            if (g.a == 0xc54602eed744f7a0ull && g.b == 0xe7d8fd0a7d71fa97ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_UpButton)}; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            if (g.a == 0x664b0bb3d6a8a70full && g.b == 0xedadcdef408b43bcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_RightButton)}; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            if (g.a == 0x6c4e1ee2cc1b24a6ull && g.b == 0xbce39f466c017fa2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStick_DownButton)}; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            if (g.a == 0xdb4be13ea99a5b8dull && g.b == 0x0f8c09d1d7c41b8dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick)}; // 8d5b9aa9-3ee1-4bdb-8d1b-c4d7d1098c0f
            if (g.a == 0x4b4a0a77987db54dull && g.b == 0xd68b54aa2459f79aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_VerticalAxisTwoWay)}; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            if (g.a == 0x8348948fb2d810a1ull && g.b == 0x302653e80491c49aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_HorizontalAxisTwoWay)}; // a110d8b2-8f94-4883-9ac4-9104e8532630
            if (g.a == 0x8b4506b3a4f8a96eull && g.b == 0x520cb923663c3ca8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_LeftAxisOneWay)}; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            if (g.a == 0xaf46c6827cd1bb74ull && g.b == 0xf1a365a2104e96a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_UpAxisOneWay)}; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            if (g.a == 0xb74e98d8b6ccdbdaull && g.b == 0x96fb0226c8c871a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_RightAxisOneWay)}; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            if (g.a == 0x77494296e38ad398ull && g.b == 0x98455f57f6d071b3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_DownAxisOneWay)}; // 98d38ae3-9642-4977-b371-d0f6575f4598
            if (g.a == 0x0745080b088bab76ull && g.b == 0xceb6b951d69035a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_LeftButton)}; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            if (g.a == 0xc54602eed744f7a0ull && g.b == 0xe7d8fd0a7d71fa97ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_UpButton)}; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            if (g.a == 0x664b0bb3d6a8a70full && g.b == 0xedadcdef408b43bcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_RightButton)}; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            if (g.a == 0x6c4e1ee2cc1b24a6ull && g.b == 0xbce39f466c017fa2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStick_DownButton)}; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            if (g.a == 0x74412308de5db543ull && g.b == 0x9d93cd2593d88b85ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton)}; // 43b55dde-0823-4174-858b-d89325cd939d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftStickButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x034f2977b1d51058ull && g.b == 0x30f5103b39f7498full) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton)}; // 5810d5b1-7729-4f03-8f49-f7393b10f530
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightStickButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xc14c2fc9ce11912bull && g.b == 0x39098baacc30fb8cull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick)}; // 2b9111ce-c92f-4cc1-8cfb-30ccaa8b0939
            if (g.a == 0x4b4a0a77987db54dull && g.b == 0xd68b54aa2459f79aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_VerticalAxisTwoWay)}; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            if (g.a == 0x8348948fb2d810a1ull && g.b == 0x302653e80491c49aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_HorizontalAxisTwoWay)}; // a110d8b2-8f94-4883-9ac4-9104e8532630
            if (g.a == 0x8b4506b3a4f8a96eull && g.b == 0x520cb923663c3ca8ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_LeftAxisOneWay)}; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            if (g.a == 0xaf46c6827cd1bb74ull && g.b == 0xf1a365a2104e96a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_UpAxisOneWay)}; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            if (g.a == 0xb74e98d8b6ccdbdaull && g.b == 0x96fb0226c8c871a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_RightAxisOneWay)}; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            if (g.a == 0x77494296e38ad398ull && g.b == 0x98455f57f6d071b3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_DownAxisOneWay)}; // 98d38ae3-9642-4977-b371-d0f6575f4598
            if (g.a == 0x0745080b088bab76ull && g.b == 0xceb6b951d69035a3ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_LeftButton)}; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            if (g.a == 0xc54602eed744f7a0ull && g.b == 0xe7d8fd0a7d71fa97ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_UpButton)}; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            if (g.a == 0x664b0bb3d6a8a70full && g.b == 0xedadcdef408b43bcull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_RightButton)}; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            if (g.a == 0x6c4e1ee2cc1b24a6ull && g.b == 0xbce39f466c017fa2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_DPadStick_DownButton)}; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            if (g.a == 0x7a433bb14593432eull && g.b == 0x9762405be782f3b2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton)}; // 2e439345-b13b-437a-b2f3-82e75b406297
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftShoulderButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x58451d5d0315621full && g.b == 0x7fc9177bd0887a9eull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton)}; // 1f621503-5d1d-4558-9e7a-88d07b17c97f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightShoulderButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x724253db1d7d6345ull && g.b == 0x396b0e24c09729b6ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay)}; // 45637d1d-db53-4272-b629-97c0240e6b39
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x754b21fc9d57ca42ull && g.b == 0xe9d3f6eb1b3a4188ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay)}; // 42ca579d-fc21-4b75-8841-3a1bebf6d3e9
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x1b45c48587f5aeeeull && g.b == 0x72c7b476ae15a595ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton)}; // eeaef587-85c4-451b-95a5-15ae76b4c772
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_OptionsButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x6d416f5b66efa4faull && g.b == 0x7096eaae536ea1b5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton)}; // faa4ef66-5b6f-416d-b5a1-6e53aeea9670
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_ShareButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x60464ebec4f23bc7ull && g.b == 0x36d011bbf18fb48dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton)}; // c73bf2c4-be4e-4660-8db4-8ff1bb11d036
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_PlaystationButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x394c7b9fb3431af8ull && g.b == 0x7d497b6413652daeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton)}; // f81a43b3-9f7b-4c39-ae2d-6513647b497d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::DualSense_MicButton_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x214f635ef33f8a8dull && g.b == 0xbd3b9e71a84c8eacull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button)}; // 8d8a3ff3-5e63-4f21-ac8e-4ca8719e3bbd
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xbf4a1b92147ddedaull && g.b == 0x6fdc0147406c5791ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button)}; // dade7d14-921b-4abf-9157-6c404701dc6f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x764f85b1d602bec3ull && g.b == 0x313f6e7e711e68b5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button)}; // c3be02d6-b185-4f76-b568-1e717e6e3f31
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xfd43add121477d3bull && g.b == 0x3fdc92a8dca02790ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button)}; // 3b7d4721-d1ad-43fd-9027-a0dca892dc3f
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1346c144a5e9e60aull && g.b == 0x03a7bd04e96160abull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button)}; // 0ae6e9a5-44c1-4613-ab60-61e904bda703
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xeb43874cedf15c88ull && g.b == 0x7aa689799a2409aaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button)}; // 885cf1ed-4c87-43eb-aa09-249a7989a67a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xbc424f116adc7d98ull && g.b == 0xcdeb2a82d5d355a0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button)}; // 987ddc6a-114f-42bc-a055-d3d5822aebcd
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x484583c25b09df61ull && g.b == 0xda714dd68b97b3a7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button)}; // 61df095b-c283-4548-a7b3-978bd64d71da
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xdf43da43858050abull && g.b == 0xc8d79ac4112d1088ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button)}; // ab508085-43da-43df-8810-2d11c49ad7c8
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x25492433dccc6f5bull && g.b == 0xaf2842b21456ab9bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button)}; // 5b6fccdc-3324-4925-9bab-5614b24228af
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xbe4d5d3e41830f1full && g.b == 0x09385598d37fea84ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button)}; // 1f0f8341-3e5d-4dbe-84ea-7fd398553809
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x3f4fdeff8833c3e9ull && g.b == 0x5a04b194f02311a5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button)}; // e9c33388-ffde-4f3f-a511-23f094b1045a
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x5f4867ee3bc36b46ull && g.b == 0x51962984f28348aaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button)}; // 466bc33b-ee67-485f-aa48-83f284299651
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0xc146677d0cc083aaull && g.b == 0xf812d8ead0f5e1b5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button)}; // aa83c00c-7d67-46c1-b5e1-f5d0ead812f8
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x104da545e1369e1aull && g.b == 0x3d5f38ef47a10c94ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button)}; // 1a9e36e1-45a5-4d10-940c-a147ef385f3d
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x844edf7a3ddf387dull && g.b == 0xee59532ea54984a1ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button)}; // 7d38df3d-7adf-4e84-a184-49a52e5359ee
            if (g.a == 0x7a412fa119c0e195ull && g.b == 0x20341645d594a28bull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15Button_AsAxisOneWay)}; // 95e1c019-a12f-417a-8ba2-94d545163420
            if (g.a == 0x1a47834685db32f1ull && g.b == 0xdec9359b9843de93ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay)}; // f132db85-4683-471a-93de-43989b35c9de
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xf048d42877ed34feull && g.b == 0xe4a76338723f169dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay)}; // fe34ed77-28d4-48f0-9d16-3f723863a7e4
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x9547198aa9221022ull && g.b == 0xebd00447775b10b9ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay)}; // 221022a9-8a19-4795-b910-5b774704d0eb
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x5649e2701f4b31fdull && g.b == 0xd3dc779b1a3648a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay)}; // fd314b1f-70e2-4956-a248-361a9b77dcd3
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x3f491b18456895f8ull && g.b == 0x3f3efddb7a28d7baull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay)}; // f8956845-181b-493f-bad7-287adbfd3e3f
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x334962ff94fdaadcull && g.b == 0xca6c4c6be94a709dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay)}; // dcaafd94-ff62-4933-9d70-4ae96b4c6cca
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x944cf887f820092eull && g.b == 0xe0423aed57f0a791ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay)}; // 2e0920f8-87f8-4c94-91a7-f057ed3a42e0
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xb34d8952f22aec45ull && g.b == 0x1b9285288d8eb286ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay)}; // 45ec2af2-5289-4db3-86b2-8e8d2885921b
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xf048bde9d5ac4b5bull && g.b == 0xab036d8c0d908e8cull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay)}; // 5b4bacd5-e9bd-48f0-8c8e-900d8c6d03ab
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x96484e79b56f584cull && g.b == 0xb6cea18676745f85ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay)}; // 4c586fb5-794e-4896-855f-747686a1ceb6
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xc74f82da70367862ull && g.b == 0xb89c60456722a5b7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay)}; // 62783670-da82-4fc7-b7a5-226745609cb8
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xd04e52398ad814d4ull && g.b == 0x225f01577d1515baull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay)}; // d414d88a-3952-4ed0-ba15-157d57015f22
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x76482a78b2762138ull && g.b == 0x3d8a09dcbb2c41b2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay)}; // 382176b2-782a-4876-b241-2cbbdc098a3d
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xbf406f6558c38251ull && g.b == 0x6c8a82b1447daea5ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay)}; // 5182c358-656f-40bf-a5ae-7d44b1828a6c
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0xf2438badf4150dd8ull && g.b == 0xe45e4631b42ec2bfull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay)}; // d80d15f4-ad8b-43f2-bfc2-2eb431465ee4
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x104c5df8dc78c728ull && g.b == 0x65044a15813b5b90ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay)}; // 28c778dc-f85d-4c10-905b-3b81154a0465
            if (g.a == 0x254ad4127b1c74b4ull && g.b == 0x9fd79fb7460853adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay_AsButton)}; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            if (g.a == 0x2243226d6d2bf14full && g.b == 0x396f44ff56bfda97ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay)}; // 4ff12b6d-6d22-4322-97da-bf56ff446f39
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0xf54e06fed441d0b5ull && g.b == 0x182a3191fd6cca9aull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay)}; // b5d041d4-fe06-4ef5-9aca-6cfd91312a18
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x294878d6931fe2c0ull && g.b == 0x3eb220c9551ea488ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay)}; // c0e21f93-d678-4829-88a4-1e55c920b23e
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x284ccf7c0f269200ull && g.b == 0x2ba0c477ffea04beull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay)}; // 0092260f-7ccf-4c28-be04-eaff77c4a02b
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0xfe4babc7b0c703c1ull && g.b == 0x4f5899e87811b1adull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay)}; // c103c7b0-c7ab-4bfe-adb1-1178e899584f
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0xf7495b93a0f5a138ull && g.b == 0x3a77efe4cb86b28dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay)}; // 38a1f5a0-935b-49f7-8db2-86cbe4ef773a
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x774d8da2deb94d1full && g.b == 0x28c12a460f0a4babull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay)}; // 1f4db9de-a28d-4d77-ab4b-0a0f462ac128
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x9f47f8629f994c0bull && g.b == 0x8e4b21c60cebd9acull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay)}; // 0b4c999f-62f8-479f-acd9-eb0cc6214b8e
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x9d4b14517b5593eaull && g.b == 0x9121617db47e4caaull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay)}; // ea93557b-5114-4b9d-aa4c-7eb47d612191
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x1f456980d2d176eaull && g.b == 0x11d50e78c781509dull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay)}; // ea76d1d2-8069-451f-9d50-81c7780ed511
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x174aed6fa9f7d2e0ull && g.b == 0x5d27ee4213ac22a7ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay)}; // e0d2f7a9-6fed-4a17-a722-ac1342ee275d
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0xe548b031b5c1d0f2ull && g.b == 0x588de2018c682f80ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay)}; // f2d0c1b5-31b0-48e5-802f-688c01e28d58
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0xe34d283806ef892aull && g.b == 0xd3c342213e941598ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay)}; // 2a89ef06-3828-4de3-9815-943e2142c3d3
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x3446494353f88a0full && g.b == 0xb44e01174541bd93ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay)}; // 0f8af853-4349-4634-93bd-414517014eb4
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x23498d83b4f7dc6bull && g.b == 0xed5dd890bbbeb5bfull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay)}; // 6bdcf7b4-838d-4923-bfb5-bebb90d85ded
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            if (g.a == 0x4a4140a7c02bd9efull && g.b == 0xe3e95ff22ac3ceacull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay)}; // efd92bc0-a740-414a-acce-c32af25fe9e3
            if (g.a == 0xd542b05b7fcd46b4ull && g.b == 0x40f5853c242b5fa0ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay)}; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            if (g.a == 0x0447d95cd15f9255ull && g.b == 0xf932d92064bfedaeull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay)}; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            if (g.a == 0x7741ca9fd6e8c9d8ull && g.b == 0x3d89fdeed42988a2ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveButton)}; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            if (g.a == 0x7b469e6c87372754ull && g.b == 0x3e9e80756c528e82ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeButton)}; // 54273787-6c9e-467b-828e-526c75809e3e
            return InputControlUsageInvalid;
        },
        [](const InputGuid g)->InputControlTypeRef // GetControlTypeRef
        {
            if (g.a == 0xac437425a6fe048full && g.b == 0x054bf8a1108663aaull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::Button)}; // 8f04fea6-2574-43ac-aa63-8610a1f84b05
            if (g.a == 0x8d4dad7247bab1c3ull && g.b == 0xa123ca8256d2a8b4ull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisOneWay)}; // c3b1ba47-72ad-4d8d-b4a8-d25682ca23a1
            if (g.a == 0x454de2eb91149065ull && g.b == 0x5c9b5661e790e982ull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::AxisTwoWay)}; // 65901491-ebe2-4d45-82e9-90e761569b5c
            if (g.a == 0x1a466f5c0d0d1a9bull && g.b == 0xa85fd73659b6f281ull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaAxisTwoWay)}; // 9b1a0d0d-5c6f-461a-81f2-b65936d75fa8
            if (g.a == 0x9b41ead0872fc928ull && g.b == 0x32ed854026a52daaull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::Stick)}; // 28c92f87-d0ea-419b-aa2d-a5264085ed32
            if (g.a == 0xa54d5e676905efceull && g.b == 0xe3cf0a07bdf90886ull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::DeltaVector2D)}; // ceef0569-675e-4da5-8608-f9bd070acfe3
            if (g.a == 0x32403609bc463908ull && g.b == 0xe61c67afd0a90e9dull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::Position2D)}; // 083946bc-0936-4032-9d0e-a9d0af671ce6
            return InputControlTypeRefInvalid;
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef)->InputGuid // GetDeviceGuid
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
            case InputDeviceBuiltIn::KeyboardWindows: return { 0x1d4b8e4584e8378dull, 0xd1e9875942955f80ull }; // 8d37e884-458e-4b1d-805f-95425987e9d1 'Keyboard (Windows)'
            case InputDeviceBuiltIn::MouseMacOS: return { 0xd0454b7c1e5242b6ull, 0x22aa86e78460b7b3ull }; // b642521e-7c4b-45d0-b3b7-6084e786aa22 'Mouse (macOS)'
            case InputDeviceBuiltIn::WindowsGamingInputGamepad: return { 0x8944989cda9608ffull, 0x72c36241244bc394ull }; // ff0896da-9c98-4489-94c3-4b244162c372 'Gamepad (Windows.Gaming.Input)'
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputDeviceTraitRef traitRef)->InputGuid // GetTraitGuid
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
            case InputDeviceTraitBuiltIn::ExplicitlyPollableDevice: return { 0xa34b2937e75892e1ull, 0x095ab1333d3b3e82ull }; // e19258e7-3729-4ba3-823e-3b3d33b15a09
            case InputDeviceTraitBuiltIn::Keyboard: return { 0x8e43e7021b5ff12dull, 0xcfa4150ef44e429bull }; // 2df15f1b-02e7-438e-9b42-4ef40e15a4cf
            case InputDeviceTraitBuiltIn::Pointer: return { 0xa04c398fdd44e771ull, 0xde47478f4437979eull }; // 71e744dd-8f39-4ca0-9e97-37448f4747de
            case InputDeviceTraitBuiltIn::Mouse: return { 0x2746517107bbb030ull, 0x84f5a2e5adaed191ull }; // 30b0bb07-7151-4627-91d1-aeade5a2f584
            case InputDeviceTraitBuiltIn::Gamepad: return { 0xf2413a3793ae989full, 0x2c7a5863f1d96aacull }; // 9f98ae93-373a-41f2-ac6a-d9f163587a2c
            case InputDeviceTraitBuiltIn::DualSense: return { 0xe548955d73756725ull, 0x1ad3251b5a8b5c86ull }; // 25677573-5d95-48e5-865c-8b5a1b25d31a
            case InputDeviceTraitBuiltIn::GenericControls: return { 0x2049beedd5166fd5ull, 0x8e36e44e12afafbcull }; // d56f16d5-edbe-4920-bcaf-af124ee4368e
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputControlUsage usage)->InputGuid // GetControlGuid
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
            case InputControlUsageBuiltIn::Keyboard_EscapeButton: return { 0x68478fd8caae6ae4ull, 0x1ea321de9aa5f7a1ull }; // e46aaeca-d88f-4768-a1f7-a59ade21a31e
            case InputControlUsageBuiltIn::Keyboard_EscapeButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_SpaceButton: return { 0xd6440b56f57ee776ull, 0x11ba607435ef8090ull }; // 76e77ef5-560b-44d6-9080-ef357460ba11
            case InputControlUsageBuiltIn::Keyboard_SpaceButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_EnterButton: return { 0x0e4ab3d4ecd2c1dbull, 0xcb1c3ea36aee5fbdull }; // dbc1d2ec-d4b3-4a0e-bd5f-ee6aa33e1ccb
            case InputControlUsageBuiltIn::Keyboard_EnterButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_TabButton: return { 0x994a28b6a000454dull, 0xc374dc727ea95780ull }; // 4d4500a0-b628-4a99-8057-a97e72dc74c3
            case InputControlUsageBuiltIn::Keyboard_TabButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton: return { 0xa34cd1155f569e3eull, 0x7ef2534c8c7189abull }; // 3e9e565f-15d1-4ca3-ab89-718c4c53f27e
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_QuoteButton: return { 0xfd42279c4a5bdef2ull, 0x4d3d889869db2781ull }; // f2de5b4a-9c27-42fd-8127-db6998883d4d
            case InputControlUsageBuiltIn::Keyboard_QuoteButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton: return { 0x8c42aef17367e2adull, 0x20fcfbc6291fca88ull }; // ade26773-f1ae-428c-88ca-1f29c6fbfc20
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_CommaButton: return { 0x534eb5c11cc79268ull, 0x7938c8a2851a1d9aull }; // 6892c71c-c1b5-4e53-9a1d-1a85a2c83879
            case InputControlUsageBuiltIn::Keyboard_CommaButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PeriodButton: return { 0xe44a420b086bd6ebull, 0xcadc424061b54695ull }; // ebd66b08-0b42-4ae4-9546-b5614042dcca
            case InputControlUsageBuiltIn::Keyboard_PeriodButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_SlashButton: return { 0xd9452c828e103867ull, 0xe438928c9105a487ull }; // 6738108e-822c-45d9-87a4-05918c9238e4
            case InputControlUsageBuiltIn::Keyboard_SlashButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_BackslashButton: return { 0x444ce1386f21eea6ull, 0x8f25494649e52eb1ull }; // a6ee216f-38e1-4c44-b12e-e5494649258f
            case InputControlUsageBuiltIn::Keyboard_BackslashButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton: return { 0x8041dbaaf8286f13ull, 0x3bb61724a6cdeab7ull }; // 136f28f8-aadb-4180-b7ea-cda62417b63b
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton: return { 0xb94abe9c5963d506ull, 0xba49dffd68e6d4a6ull }; // 06d56359-9cbe-4ab9-a6d4-e668fddf49ba
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_MinusButton: return { 0x494888d4a794047cull, 0x35ed521944ca43b8ull }; // 7c0494a7-d488-4849-b843-ca441952ed35
            case InputControlUsageBuiltIn::Keyboard_MinusButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_EqualsButton: return { 0x524f7bef4f344facull, 0x00b5a258b3d452bbull }; // ac4f344f-ef7b-4f52-bb52-d4b358a2b500
            case InputControlUsageBuiltIn::Keyboard_EqualsButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton: return { 0xb643ac22b9140078ull, 0x625876a5af4a7c9bull }; // 780014b9-22ac-43b6-9b7c-4aafa5765862
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton: return { 0x704856cf670f4abfull, 0x66e38dbde894aa8dull }; // bf4a0f67-cf56-4870-8daa-94e8bd8de366
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton: return { 0x7a41de905e226747ull, 0x2deb7c208324a8b9ull }; // 4767225e-90de-417a-b9a8-2483207ceb2d
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton: return { 0x5a40fc89377cbf5cull, 0x432e06d1dd7b85acull }; // 5cbf7c37-89fc-405a-ac85-7bddd1062e43
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_AButton: return { 0x0e41e8c887617e10ull, 0xd6a20a268f0cc4a0ull }; // 107e6187-c8e8-410e-a0c4-0c8f260aa2d6
            case InputControlUsageBuiltIn::Keyboard_AButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_BButton: return { 0xb54592b69f9f8576ull, 0xf0af3216ec454caaull }; // 76859f9f-b692-45b5-aa4c-45ec1632aff0
            case InputControlUsageBuiltIn::Keyboard_BButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_CButton: return { 0xd64b13b1c7c0e74aull, 0x62d2a6bef07eea86ull }; // 4ae7c0c7-b113-4bd6-86ea-7ef0bea6d262
            case InputControlUsageBuiltIn::Keyboard_CButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_DButton: return { 0x554dcc71cda33545ull, 0xd61307dd6c70bc92ull }; // 4535a3cd-71cc-4d55-92bc-706cdd0713d6
            case InputControlUsageBuiltIn::Keyboard_DButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_EButton: return { 0xb949e541906f8a39ull, 0x2099c04e9e3aaa87ull }; // 398a6f90-41e5-49b9-87aa-3a9e4ec09920
            case InputControlUsageBuiltIn::Keyboard_EButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_FButton: return { 0xaa47980ddf7e276bull, 0xf32c4defd00cfaafull }; // 6b277edf-0d98-47aa-affa-0cd0ef4d2cf3
            case InputControlUsageBuiltIn::Keyboard_FButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_GButton: return { 0xce48f93e8552f8eaull, 0x2da00213b750699bull }; // eaf85285-3ef9-48ce-9b69-50b71302a02d
            case InputControlUsageBuiltIn::Keyboard_GButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_HButton: return { 0x4f44185f6e065bacull, 0xebc7277d582d3f9full }; // ac5b066e-5f18-444f-9f3f-2d587d27c7eb
            case InputControlUsageBuiltIn::Keyboard_HButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_IButton: return { 0x9c4e4e63533b9f5full, 0xe329650c351d9ea4ull }; // 5f9f3b53-634e-4e9c-a49e-1d350c6529e3
            case InputControlUsageBuiltIn::Keyboard_IButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_JButton: return { 0x7d44b2dbdc40d333ull, 0x74132f4a56f45480ull }; // 33d340dc-dbb2-447d-8054-f4564a2f1374
            case InputControlUsageBuiltIn::Keyboard_JButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_KButton: return { 0x844c26c4387ea323ull, 0x01c7808018b9e682ull }; // 23a37e38-c426-4c84-82e6-b9188080c701
            case InputControlUsageBuiltIn::Keyboard_KButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LButton: return { 0x0642783b2324caa7ull, 0x8b0922ecebb2538bull }; // a7ca2423-3b78-4206-8b53-b2ebec22098b
            case InputControlUsageBuiltIn::Keyboard_LButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_MButton: return { 0x534f616e700931a2ull, 0x077a05e34b28aebcull }; // a2310970-6e61-4f53-bcae-284be3057a07
            case InputControlUsageBuiltIn::Keyboard_MButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NButton: return { 0x8543319faa544a34ull, 0x972f01240d3685beull }; // 344a54aa-9f31-4385-be85-360d24012f97
            case InputControlUsageBuiltIn::Keyboard_NButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OButton: return { 0x674a8fde67caa26eull, 0x528b6f6e636d28a4ull }; // 6ea2ca67-de8f-4a67-a428-6d636e6f8b52
            case InputControlUsageBuiltIn::Keyboard_OButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PButton: return { 0x4841455dbeb75f0cull, 0x859573168421b3b6ull }; // 0c5fb7be-5d45-4148-b6b3-218416739585
            case InputControlUsageBuiltIn::Keyboard_PButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_QButton: return { 0x494b8cc5d4807d69ull, 0x66101af40034b093ull }; // 697d80d4-c58c-4b49-93b0-3400f41a1066
            case InputControlUsageBuiltIn::Keyboard_QButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RButton: return { 0x3f4cf4365f50da8full, 0xa7512eba02a42bb7ull }; // 8fda505f-36f4-4c3f-b72b-a402ba2e51a7
            case InputControlUsageBuiltIn::Keyboard_RButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_SButton: return { 0x0a47b10386eb0c51ull, 0x0d08157613f17ca4ull }; // 510ceb86-03b1-470a-a47c-f1137615080d
            case InputControlUsageBuiltIn::Keyboard_SButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_TButton: return { 0x1d4928b022d6ae93ull, 0xcc4bda13a56283bcull }; // 93aed622-b028-491d-bc83-62a513da4bcc
            case InputControlUsageBuiltIn::Keyboard_TButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_UButton: return { 0x474f54dbdddb670bull, 0x0142d545b76670a8ull }; // 0b67dbdd-db54-4f47-a870-66b745d54201
            case InputControlUsageBuiltIn::Keyboard_UButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_VButton: return { 0x3d4cd6da5d80d793ull, 0xd4de83464cc70cbaull }; // 93d7805d-dad6-4c3d-ba0c-c74c4683ded4
            case InputControlUsageBuiltIn::Keyboard_VButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_WButton: return { 0xa54f24feffafdbb9ull, 0x0e4eb7d84820ed8full }; // b9dbafff-fe24-4fa5-8fed-2048d8b74e0e
            case InputControlUsageBuiltIn::Keyboard_WButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_XButton: return { 0x7d49e3ef8196fab2ull, 0xa5597af769d52780ull }; // b2fa9681-efe3-497d-8027-d569f77a59a5
            case InputControlUsageBuiltIn::Keyboard_XButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_YButton: return { 0x504b936d7ffb418aull, 0xb31e7581416e22bcull }; // 8a41fb7f-6d93-4b50-bc22-6e4181751eb3
            case InputControlUsageBuiltIn::Keyboard_YButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_ZButton: return { 0x784cf45431a138c4ull, 0x4c7bf509d7ed33a6ull }; // c438a131-54f4-4c78-a633-edd709f57b4c
            case InputControlUsageBuiltIn::Keyboard_ZButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit1Button: return { 0x9b45583d09209be4ull, 0x9c80c8316640d1abull }; // e49b2009-3d58-459b-abd1-406631c8809c
            case InputControlUsageBuiltIn::Keyboard_Digit1Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit2Button: return { 0x1f437b1cd04a46fcull, 0x4c84f5e9f38a119bull }; // fc464ad0-1c7b-431f-9b11-8af3e9f5844c
            case InputControlUsageBuiltIn::Keyboard_Digit2Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit3Button: return { 0x4245e3252b0b3797ull, 0x5a971b11863a1ba1ull }; // 97370b2b-25e3-4542-a11b-3a86111b975a
            case InputControlUsageBuiltIn::Keyboard_Digit3Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit4Button: return { 0x2f433d19fe45f871ull, 0xe9d7e5a71d7b66a1ull }; // 71f845fe-193d-432f-a166-7b1da7e5d7e9
            case InputControlUsageBuiltIn::Keyboard_Digit4Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit5Button: return { 0x0a467831ffed5416ull, 0xcfa8ccb2a21ceeacull }; // 1654edff-3178-460a-acee-1ca2b2cca8cf
            case InputControlUsageBuiltIn::Keyboard_Digit5Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit6Button: return { 0x144da16bc52b3d25ull, 0xca7220ba340b948bull }; // 253d2bc5-6ba1-4d14-8b94-0b34ba2072ca
            case InputControlUsageBuiltIn::Keyboard_Digit6Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit7Button: return { 0x98420e509774d848ull, 0xa160ae82336e46b0ull }; // 48d87497-500e-4298-b046-6e3382ae60a1
            case InputControlUsageBuiltIn::Keyboard_Digit7Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit8Button: return { 0xca4a705cb110e3a9ull, 0x2983648c2a6ce1b7ull }; // a9e310b1-5c70-4aca-b7e1-6c2a8c648329
            case InputControlUsageBuiltIn::Keyboard_Digit8Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit9Button: return { 0xba49ebf881c6f1bdull, 0x1090ca4e495a119dull }; // bdf1c681-f8eb-49ba-9d11-5a494eca9010
            case InputControlUsageBuiltIn::Keyboard_Digit9Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Digit0Button: return { 0x5a4d5d6ec126d5e0ull, 0xcd2674a471cf77a1ull }; // e0d526c1-6e5d-4d5a-a177-cf71a47426cd
            case InputControlUsageBuiltIn::Keyboard_Digit0Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton: return { 0x6348cda1390bb3d3ull, 0x4308fe1c8d66cd90ull }; // d3b30b39-a1cd-4863-90cd-668d1cfe0843
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton: return { 0x9c4e423e924f8aa2ull, 0x4b90e7f6a90e7cbfull }; // a28a4f92-3e42-4e9c-bf7c-0ea9f6e7904b
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_ShiftButton: return { 0x354d97c5bbd53b2cull, 0x24b9a537452b9484ull }; // 2c3bd5bb-c597-4d35-8494-2b4537a5b924
            case InputControlUsageBuiltIn::Keyboard_ShiftButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton: return { 0x034a0c6a42f01a80ull, 0x2f0532b9c4a514bfull }; // 801af042-6a0c-4a03-bf14-a5c4b932052f
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightAltButton: return { 0x4845d77b80ea9a3full, 0x5e4ddbaec2dc81b0ull }; // 3f9aea80-7bd7-4548-b081-dcc2aedb4d5e
            case InputControlUsageBuiltIn::Keyboard_RightAltButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_AltButton: return { 0x214206c827335b5full, 0xd06f676f6fcacfb4ull }; // 5f5b3327-c806-4221-b4cf-ca6f6f676fd0
            case InputControlUsageBuiltIn::Keyboard_AltButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton: return { 0xb844c45a1ececa7aull, 0xe7a0cb8efb376692ull }; // 7acace1e-5ac4-44b8-9266-37fb8ecba0e7
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton: return { 0x774db170f07199e7ull, 0x29c9e65dc22d73a9ull }; // e79971f0-70b1-4d77-a973-2dc25de6c929
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_CtrlButton: return { 0x8a46b90c24ed911cull, 0x2481b0f26a3d1ca6ull }; // 1c91ed24-0cb9-468a-a61c-3d6af2b08124
            case InputControlUsageBuiltIn::Keyboard_CtrlButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton: return { 0xd6479a32c785f92dull, 0xcf38effaf7bd40abull }; // 2df985c7-329a-47d6-ab40-bdf7faef38cf
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton: return { 0x5d4b152d17a05cb8ull, 0x84d0e33e08a04591ull }; // b85ca017-2d15-4b5d-9145-a0083ee3d084
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton: return { 0x70448a8d32ffa707ull, 0x0eede33cf46d2497ull }; // 07a7ff32-8d8a-4470-9724-6df43ce3ed0e
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton: return { 0xf847da8c28a9299cull, 0x7a2375f580b49b90ull }; // 9c29a928-8cda-47f8-909b-b480f575237a
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PageDownButton: return { 0xe7410f2afadb426dull, 0x1d1575a44655f384ull }; // 6d42dbfa-2a0f-41e7-84f3-5546a475151d
            case InputControlUsageBuiltIn::Keyboard_PageDownButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PageUpButton: return { 0x0b4d4fff5ae24eb8ull, 0x62bef4eec69b57a6ull }; // b84ee25a-ff4f-4d0b-a657-9bc6eef4be62
            case InputControlUsageBuiltIn::Keyboard_PageUpButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_HomeButton: return { 0x504deaca6ace06a2ull, 0x0d033d99ab480697ull }; // a206ce6a-caea-4d50-9706-48ab993d030d
            case InputControlUsageBuiltIn::Keyboard_HomeButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_EndButton: return { 0xe441b3b0858b7e38ull, 0x5ae2b590d565268eull }; // 387e8b85-b0b3-41e4-8e26-65d590b5e25a
            case InputControlUsageBuiltIn::Keyboard_EndButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_InsertButton: return { 0xb140a743f8e7cc68ull, 0x03d0ddba2c0b9bb0ull }; // 68cce7f8-43a7-40b1-b09b-0b2cbaddd003
            case InputControlUsageBuiltIn::Keyboard_InsertButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_DeleteButton: return { 0x2448c4d6eae3769full, 0x0f95e41d5578b6a4ull }; // 9f76e3ea-d6c4-4824-a4b6-78551de4950f
            case InputControlUsageBuiltIn::Keyboard_DeleteButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton: return { 0x884f5a3c72f3d451ull, 0x560874df62717cbaull }; // 51d4f372-3c5a-4f88-ba7c-7162df740856
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumLockButton: return { 0x7943be2009c09c36ull, 0x17ecffacac9f1a85ull }; // 369cc009-20be-4379-851a-9facacffec17
            case InputControlUsageBuiltIn::Keyboard_NumLockButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton: return { 0x114241d7b4c1e997ull, 0xdfb79d80d6e11da0ull }; // 97e9c1b4-d741-4211-a01d-e1d6809db7df
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton: return { 0x8543f2abc4a857bdull, 0xf00ab2522ae28285ull }; // bd57a8c4-abf2-4385-8582-e22a52b20af0
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_PauseButton: return { 0x1d4025ea0806e773ull, 0x4f8b526458574396ull }; // 73e70608-ea25-401d-9643-575864528b4f
            case InputControlUsageBuiltIn::Keyboard_PauseButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton: return { 0x5341c59b3ba50121ull, 0x5a215b226d7f0d8eull }; // 2101a53b-9bc5-4153-8e0d-7f6d225b215a
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton: return { 0x55498908317e34cdull, 0x2a8c64ef5f2e9a91ull }; // cd347e31-0889-4955-919a-2e5fef648c2a
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton: return { 0xe44835c66fe398a9ull, 0x3005001a6b1313bdull }; // a998e36f-c635-48e4-bd13-136b1a000530
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton: return { 0xb74f0c20bb566f97ull, 0x4153c1018b1921b3ull }; // 976f56bb-200c-4fb7-b321-198b01c15341
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton: return { 0x3c4c5a36510cca26ull, 0x005c5c4a936c65beull }; // 26ca0c51-365a-4c3c-be65-6c934a5c5c00
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton: return { 0x97485f8f41b4c6b7ull, 0x2b8bebe9679e24a8ull }; // b7c6b441-8f5f-4897-a824-9e67e9eb8b2b
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton: return { 0x504a39fabe7b26e0ull, 0xff623fce649e06b3ull }; // e0267bbe-fa39-4a50-b306-9e64ce3f62ff
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button: return { 0xa1492af531a99f03ull, 0x478f095bf5a4ce83ull }; // 039fa931-f52a-49a1-83ce-a4f55b098f47
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button: return { 0xe34a9db2689cb691ull, 0xebbe176719a20d97ull }; // 91b69c68-b29d-4ae3-970d-a2196717beeb
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button: return { 0x7e45ff9cdb636cceull, 0xa46e2dc70646629cull }; // ce6c63db-9cff-457e-9c62-4606c72d6ea4
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button: return { 0x2d4ef3e1d7a2270dull, 0xd35810ee6c23399bull }; // 0d27a2d7-e1f3-4e2d-9b39-236cee1058d3
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button: return { 0xa04e059fbdef0d8full, 0x5e411976eb3d2ca2ull }; // 8f0defbd-9f05-4ea0-a22c-3deb7619415e
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button: return { 0x9443828a8cd9aef3ull, 0x130cb876a1df9782ull }; // f3aed98c-8a82-4394-8297-dfa176b80c13
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button: return { 0x1e4ba103bfcff072ull, 0x4ddee0a487e4a9bdull }; // 72f0cfbf-03a1-4b1e-bda9-e487a4e0de4d
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button: return { 0xfc4215af21559bc2ull, 0x3d1168aba14c9da8ull }; // c29b5521-af15-42fc-a89d-4ca1ab68113d
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button: return { 0x0c4cc73defc6e4c0ull, 0x0335df1738ed9aa9ull }; // c0e4c6ef-3dc7-4c0c-a99a-ed3817df3503
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button: return { 0xe44a760d8335a7ffull, 0x376ff5d95514c29dull }; // ffa73583-0d76-4ae4-9dc2-1455d9f56f37
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F1Button: return { 0x0e45cbea0683540bull, 0xcd69132025b1a7a0ull }; // 0b548306-eacb-450e-a0a7-b125201369cd
            case InputControlUsageBuiltIn::Keyboard_F1Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F2Button: return { 0x5a4bf4824d6f9ca5ull, 0x8ee9071fe79639a3ull }; // a59c6f4d-82f4-4b5a-a339-96e71f07e98e
            case InputControlUsageBuiltIn::Keyboard_F2Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F3Button: return { 0x0a481153e2769482ull, 0xb3195727381155a2ull }; // 829476e2-5311-480a-a255-1138275719b3
            case InputControlUsageBuiltIn::Keyboard_F3Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F4Button: return { 0xb943cd07b686afb9ull, 0x65c7f3b29ee36192ull }; // b9af86b6-07cd-43b9-9261-e39eb2f3c765
            case InputControlUsageBuiltIn::Keyboard_F4Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F5Button: return { 0xf549837d89e50020ull, 0xb0458ca612fdc8a3ull }; // 2000e589-7d83-49f5-a3c8-fd12a68c45b0
            case InputControlUsageBuiltIn::Keyboard_F5Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F6Button: return { 0x0740784d4825e65dull, 0xa79fcc29ab7363b6ull }; // 5de62548-4d78-4007-b663-73ab29cc9fa7
            case InputControlUsageBuiltIn::Keyboard_F6Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F7Button: return { 0x42423d3fbaa2de19ull, 0x1e1cc866969021a3ull }; // 19dea2ba-3f3d-4242-a321-909666c81c1e
            case InputControlUsageBuiltIn::Keyboard_F7Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F8Button: return { 0xe74610ef7310aa1aull, 0xecbe967ee0bc5f99ull }; // 1aaa1073-ef10-46e7-995f-bce07e96beec
            case InputControlUsageBuiltIn::Keyboard_F8Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F9Button: return { 0xfb4bc619ed656737ull, 0xfcf10a826ee61695ull }; // 376765ed-19c6-4bfb-9516-e66e820af1fc
            case InputControlUsageBuiltIn::Keyboard_F9Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F10Button: return { 0x274223532fca3239ull, 0x1ac0377bc3e2ff8dull }; // 3932ca2f-5323-4227-8dff-e2c37b37c01a
            case InputControlUsageBuiltIn::Keyboard_F10Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F11Button: return { 0xfd41ce91430ae5daull, 0x38bc2921253fca9cull }; // dae50a43-91ce-41fd-9cca-3f252129bc38
            case InputControlUsageBuiltIn::Keyboard_F11Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_F12Button: return { 0xd44244170ae086d6ull, 0x0601569c0f0f2daeull }; // d686e00a-1744-42d4-ae2d-0f0f9c560106
            case InputControlUsageBuiltIn::Keyboard_F12Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OEM1Button: return { 0x3f4e2f7bf965c334ull, 0xef497cf3f3bfeab2ull }; // 34c365f9-7b2f-4e3f-b2ea-bff3f37c49ef
            case InputControlUsageBuiltIn::Keyboard_OEM1Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OEM2Button: return { 0xd242dbb93848741eull, 0x0fc8f2b8933791b2ull }; // 1e744838-b9db-42d2-b291-3793b8f2c80f
            case InputControlUsageBuiltIn::Keyboard_OEM2Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OEM3Button: return { 0xe94149e80831d8cbull, 0x97f2ba9891e5d7bfull }; // cbd83108-e849-41e9-bfd7-e59198baf297
            case InputControlUsageBuiltIn::Keyboard_OEM3Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OEM4Button: return { 0x774e52f426354b3full, 0x503f2b96bfb627baull }; // 3f4b3526-f452-4e77-ba27-b6bf962b3f50
            case InputControlUsageBuiltIn::Keyboard_OEM4Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Keyboard_OEM5Button: return { 0xce452f1f67d22aeaull, 0x92feec2011836098ull }; // ea2ad267-1f2f-45ce-9860-831120ecfe92
            case InputControlUsageBuiltIn::Keyboard_OEM5Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Pointer_PositionPosition2D: return { 0xcc4a1dee63bb9131ull, 0xc40d4dd944fc0a8bull }; // 3191bb63-ee1d-4acc-8b0a-fc44d94d0dc4
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D: return { 0x9a4bbfcf4fba4fc3ull, 0xc1988c0527de23bdull }; // c34fba4f-cfbf-4b9a-bd23-de27058c98c1
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay: return { 0x144d63918950b67bull, 0xdc8d25768e628fafull }; // 7bb65089-9163-4d14-af8f-628e76258ddc
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay: return { 0xe347be75920d82cfull, 0xdb1f821f1a02b095ull }; // cf820d92-75be-47e3-95b0-021a1f821fdb
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_LeftButton: return { 0x0a41127a7a78b1a9ull, 0x4fcd00f649c95cb5ull }; // a9b1787a-7a12-410a-b55c-c949f600cd4f
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_UpButton: return { 0xc44c053d449b4983ull, 0xdad3a6829047a4a2ull }; // 83499b44-3d05-4cc4-a2a4-479082a6d3da
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_RightButton: return { 0xf74ad47a3d67c3f7ull, 0xd02f4ce71392139full }; // f7c3673d-7ad4-4af7-9f13-9213e74c2fd0
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_DownButton: return { 0x5349cfc620c0bc79ull, 0xa36e146fd2af939eull }; // 79bcc020-c6cf-4953-9e93-afd26f146ea3
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D: return { 0x3842dfc1ec82d94aull, 0xa09ad4def965db8full }; // 4ad982ec-c1df-4238-8fdb-65f9ded49aa0
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay: return { 0x144d63918950b67bull, 0xdc8d25768e628fafull }; // 7bb65089-9163-4d14-af8f-628e76258ddc
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay: return { 0xe347be75920d82cfull, 0xdb1f821f1a02b095ull }; // cf820d92-75be-47e3-95b0-021a1f821fdb
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_LeftButton: return { 0x0a41127a7a78b1a9ull, 0x4fcd00f649c95cb5ull }; // a9b1787a-7a12-410a-b55c-c949f600cd4f
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_UpButton: return { 0xc44c053d449b4983ull, 0xdad3a6829047a4a2ull }; // 83499b44-3d05-4cc4-a2a4-479082a6d3da
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_RightButton: return { 0xf74ad47a3d67c3f7ull, 0xd02f4ce71392139full }; // f7c3673d-7ad4-4af7-9f13-9213e74c2fd0
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_DownButton: return { 0x5349cfc620c0bc79ull, 0xa36e146fd2af939eull }; // 79bcc020-c6cf-4953-9e93-afd26f146ea3
            case InputControlUsageBuiltIn::Mouse_LeftButton: return { 0x2e47bc74227bd626ull, 0xb9ecd2b60ea63e81ull }; // 26d67b22-74bc-472e-813e-a60eb6d2ecb9
            case InputControlUsageBuiltIn::Mouse_LeftButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Mouse_MiddleButton: return { 0x0840bb232edfb23eull, 0x126c5f3eab84e7adull }; // 3eb2df2e-23bb-4008-ade7-84ab3e5f6c12
            case InputControlUsageBuiltIn::Mouse_MiddleButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Mouse_RightButton: return { 0xe74bfd49efaf4cd3ull, 0x960aaaf1f00c7d9eull }; // d34cafef-49fd-4be7-9e7d-0cf0f1aa0a96
            case InputControlUsageBuiltIn::Mouse_RightButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Mouse_BackButton: return { 0x3542cc60de49f80cull, 0x1bece2c19b223194ull }; // 0cf849de-60cc-4235-9431-229bc1e2ec1b
            case InputControlUsageBuiltIn::Mouse_BackButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Mouse_ForwardButton: return { 0x6c466f6de46a8222ull, 0x0ac10d4002e2348aull }; // 22826ae4-6d6f-466c-8a34-e202400dc10a
            case InputControlUsageBuiltIn::Mouse_ForwardButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_WestButton: return { 0x19424de08e79fcf4ull, 0xfdeb84559816b888ull }; // f4fc798e-e04d-4219-88b8-16985584ebfd
            case InputControlUsageBuiltIn::Gamepad_WestButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_NorthButton: return { 0xe1428cb50f2367ceull, 0x90a48034c0121487ull }; // ce67230f-b58c-42e1-8714-12c03480a490
            case InputControlUsageBuiltIn::Gamepad_NorthButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_EastButton: return { 0xa84d91203413ccf2ull, 0x741ffcea7cda7d9eull }; // f2cc1334-2091-4da8-9e7d-da7ceafc1f74
            case InputControlUsageBuiltIn::Gamepad_EastButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_SouthButton: return { 0x764b1deffce6a455ull, 0xc3a69026938fe89aull }; // 55a4e6fc-ef1d-4b76-9ae8-8f932690a6c3
            case InputControlUsageBuiltIn::Gamepad_SouthButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_LeftStick: return { 0x3b4a02fc745facdbull, 0x92345be67f875fbaull }; // dbac5f74-fc02-4a3b-ba5f-877fe65b3492
            case InputControlUsageBuiltIn::Gamepad_LeftStick_VerticalAxisTwoWay: return { 0x4b4a0a77987db54dull, 0xd68b54aa2459f79aull }; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            case InputControlUsageBuiltIn::Gamepad_LeftStick_HorizontalAxisTwoWay: return { 0x8348948fb2d810a1ull, 0x302653e80491c49aull }; // a110d8b2-8f94-4883-9ac4-9104e8532630
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftAxisOneWay: return { 0x8b4506b3a4f8a96eull, 0x520cb923663c3ca8ull }; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpAxisOneWay: return { 0xaf46c6827cd1bb74ull, 0xf1a365a2104e96a5ull }; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightAxisOneWay: return { 0xb74e98d8b6ccdbdaull, 0x96fb0226c8c871a5ull }; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownAxisOneWay: return { 0x77494296e38ad398ull, 0x98455f57f6d071b3ull }; // 98d38ae3-9642-4977-b371-d0f6575f4598
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftButton: return { 0x0745080b088bab76ull, 0xceb6b951d69035a3ull }; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpButton: return { 0xc54602eed744f7a0ull, 0xe7d8fd0a7d71fa97ull }; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightButton: return { 0x664b0bb3d6a8a70full, 0xedadcdef408b43bcull }; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownButton: return { 0x6c4e1ee2cc1b24a6ull, 0xbce39f466c017fa2ull }; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            case InputControlUsageBuiltIn::Gamepad_RightStick: return { 0xdb4be13ea99a5b8dull, 0x0f8c09d1d7c41b8dull }; // 8d5b9aa9-3ee1-4bdb-8d1b-c4d7d1098c0f
            case InputControlUsageBuiltIn::Gamepad_RightStick_VerticalAxisTwoWay: return { 0x4b4a0a77987db54dull, 0xd68b54aa2459f79aull }; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            case InputControlUsageBuiltIn::Gamepad_RightStick_HorizontalAxisTwoWay: return { 0x8348948fb2d810a1ull, 0x302653e80491c49aull }; // a110d8b2-8f94-4883-9ac4-9104e8532630
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftAxisOneWay: return { 0x8b4506b3a4f8a96eull, 0x520cb923663c3ca8ull }; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpAxisOneWay: return { 0xaf46c6827cd1bb74ull, 0xf1a365a2104e96a5ull }; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightAxisOneWay: return { 0xb74e98d8b6ccdbdaull, 0x96fb0226c8c871a5ull }; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownAxisOneWay: return { 0x77494296e38ad398ull, 0x98455f57f6d071b3ull }; // 98d38ae3-9642-4977-b371-d0f6575f4598
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftButton: return { 0x0745080b088bab76ull, 0xceb6b951d69035a3ull }; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpButton: return { 0xc54602eed744f7a0ull, 0xe7d8fd0a7d71fa97ull }; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightButton: return { 0x664b0bb3d6a8a70full, 0xedadcdef408b43bcull }; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownButton: return { 0x6c4e1ee2cc1b24a6ull, 0xbce39f466c017fa2ull }; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton: return { 0x74412308de5db543ull, 0x9d93cd2593d88b85ull }; // 43b55dde-0823-4174-858b-d89325cd939d
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_RightStickButton: return { 0x034f2977b1d51058ull, 0x30f5103b39f7498full }; // 5810d5b1-7729-4f03-8f49-f7393b10f530
            case InputControlUsageBuiltIn::Gamepad_RightStickButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_DPadStick: return { 0xc14c2fc9ce11912bull, 0x39098baacc30fb8cull }; // 2b9111ce-c92f-4cc1-8cfb-30ccaa8b0939
            case InputControlUsageBuiltIn::Gamepad_DPadStick_VerticalAxisTwoWay: return { 0x4b4a0a77987db54dull, 0xd68b54aa2459f79aull }; // 4db57d98-770a-4a4b-9af7-5924aa548bd6
            case InputControlUsageBuiltIn::Gamepad_DPadStick_HorizontalAxisTwoWay: return { 0x8348948fb2d810a1ull, 0x302653e80491c49aull }; // a110d8b2-8f94-4883-9ac4-9104e8532630
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftAxisOneWay: return { 0x8b4506b3a4f8a96eull, 0x520cb923663c3ca8ull }; // 6ea9f8a4-b306-458b-a83c-3c6623b90c52
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpAxisOneWay: return { 0xaf46c6827cd1bb74ull, 0xf1a365a2104e96a5ull }; // 74bbd17c-82c6-46af-a596-4e10a265a3f1
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightAxisOneWay: return { 0xb74e98d8b6ccdbdaull, 0x96fb0226c8c871a5ull }; // dadbccb6-d898-4eb7-a571-c8c82602fb96
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownAxisOneWay: return { 0x77494296e38ad398ull, 0x98455f57f6d071b3ull }; // 98d38ae3-9642-4977-b371-d0f6575f4598
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftButton: return { 0x0745080b088bab76ull, 0xceb6b951d69035a3ull }; // 76ab8b08-0b08-4507-a335-90d651b9b6ce
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpButton: return { 0xc54602eed744f7a0ull, 0xe7d8fd0a7d71fa97ull }; // a0f744d7-ee02-46c5-97fa-717d0afdd8e7
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightButton: return { 0x664b0bb3d6a8a70full, 0xedadcdef408b43bcull }; // 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownButton: return { 0x6c4e1ee2cc1b24a6ull, 0xbce39f466c017fa2ull }; // a6241bcc-e21e-4e6c-a27f-016c469fe3bc
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton: return { 0x7a433bb14593432eull, 0x9762405be782f3b2ull }; // 2e439345-b13b-437a-b2f3-82e75b406297
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton: return { 0x58451d5d0315621full, 0x7fc9177bd0887a9eull }; // 1f621503-5d1d-4558-9e7a-88d07b17c97f
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay: return { 0x724253db1d7d6345ull, 0x396b0e24c09729b6ull }; // 45637d1d-db53-4272-b629-97c0240e6b39
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay: return { 0x754b21fc9d57ca42ull, 0xe9d3f6eb1b3a4188ull }; // 42ca579d-fc21-4b75-8841-3a1bebf6d3e9
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::DualSense_OptionsButton: return { 0x1b45c48587f5aeeeull, 0x72c7b476ae15a595ull }; // eeaef587-85c4-451b-95a5-15ae76b4c772
            case InputControlUsageBuiltIn::DualSense_OptionsButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::DualSense_ShareButton: return { 0x6d416f5b66efa4faull, 0x7096eaae536ea1b5ull }; // faa4ef66-5b6f-416d-b5a1-6e53aeea9670
            case InputControlUsageBuiltIn::DualSense_ShareButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::DualSense_PlaystationButton: return { 0x60464ebec4f23bc7ull, 0x36d011bbf18fb48dull }; // c73bf2c4-be4e-4660-8db4-8ff1bb11d036
            case InputControlUsageBuiltIn::DualSense_PlaystationButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::DualSense_MicButton: return { 0x394c7b9fb3431af8ull, 0x7d497b6413652daeull }; // f81a43b3-9f7b-4c39-ae2d-6513647b497d
            case InputControlUsageBuiltIn::DualSense_MicButton_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic0Button: return { 0x214f635ef33f8a8dull, 0xbd3b9e71a84c8eacull }; // 8d8a3ff3-5e63-4f21-ac8e-4ca8719e3bbd
            case InputControlUsageBuiltIn::GenericControls_Generic0Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic1Button: return { 0xbf4a1b92147ddedaull, 0x6fdc0147406c5791ull }; // dade7d14-921b-4abf-9157-6c404701dc6f
            case InputControlUsageBuiltIn::GenericControls_Generic1Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic2Button: return { 0x764f85b1d602bec3ull, 0x313f6e7e711e68b5ull }; // c3be02d6-b185-4f76-b568-1e717e6e3f31
            case InputControlUsageBuiltIn::GenericControls_Generic2Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic3Button: return { 0xfd43add121477d3bull, 0x3fdc92a8dca02790ull }; // 3b7d4721-d1ad-43fd-9027-a0dca892dc3f
            case InputControlUsageBuiltIn::GenericControls_Generic3Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic4Button: return { 0x1346c144a5e9e60aull, 0x03a7bd04e96160abull }; // 0ae6e9a5-44c1-4613-ab60-61e904bda703
            case InputControlUsageBuiltIn::GenericControls_Generic4Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic5Button: return { 0xeb43874cedf15c88ull, 0x7aa689799a2409aaull }; // 885cf1ed-4c87-43eb-aa09-249a7989a67a
            case InputControlUsageBuiltIn::GenericControls_Generic5Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic6Button: return { 0xbc424f116adc7d98ull, 0xcdeb2a82d5d355a0ull }; // 987ddc6a-114f-42bc-a055-d3d5822aebcd
            case InputControlUsageBuiltIn::GenericControls_Generic6Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic7Button: return { 0x484583c25b09df61ull, 0xda714dd68b97b3a7ull }; // 61df095b-c283-4548-a7b3-978bd64d71da
            case InputControlUsageBuiltIn::GenericControls_Generic7Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic8Button: return { 0xdf43da43858050abull, 0xc8d79ac4112d1088ull }; // ab508085-43da-43df-8810-2d11c49ad7c8
            case InputControlUsageBuiltIn::GenericControls_Generic8Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic9Button: return { 0x25492433dccc6f5bull, 0xaf2842b21456ab9bull }; // 5b6fccdc-3324-4925-9bab-5614b24228af
            case InputControlUsageBuiltIn::GenericControls_Generic9Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic10Button: return { 0xbe4d5d3e41830f1full, 0x09385598d37fea84ull }; // 1f0f8341-3e5d-4dbe-84ea-7fd398553809
            case InputControlUsageBuiltIn::GenericControls_Generic10Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic11Button: return { 0x3f4fdeff8833c3e9ull, 0x5a04b194f02311a5ull }; // e9c33388-ffde-4f3f-a511-23f094b1045a
            case InputControlUsageBuiltIn::GenericControls_Generic11Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic12Button: return { 0x5f4867ee3bc36b46ull, 0x51962984f28348aaull }; // 466bc33b-ee67-485f-aa48-83f284299651
            case InputControlUsageBuiltIn::GenericControls_Generic12Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic13Button: return { 0xc146677d0cc083aaull, 0xf812d8ead0f5e1b5ull }; // aa83c00c-7d67-46c1-b5e1-f5d0ead812f8
            case InputControlUsageBuiltIn::GenericControls_Generic13Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic14Button: return { 0x104da545e1369e1aull, 0x3d5f38ef47a10c94ull }; // 1a9e36e1-45a5-4d10-940c-a147ef385f3d
            case InputControlUsageBuiltIn::GenericControls_Generic14Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic15Button: return { 0x844edf7a3ddf387dull, 0xee59532ea54984a1ull }; // 7d38df3d-7adf-4e84-a184-49a52e5359ee
            case InputControlUsageBuiltIn::GenericControls_Generic15Button_AsAxisOneWay: return { 0x7a412fa119c0e195ull, 0x20341645d594a28bull }; // 95e1c019-a12f-417a-8ba2-94d545163420
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay: return { 0x1a47834685db32f1ull, 0xdec9359b9843de93ull }; // f132db85-4683-471a-93de-43989b35c9de
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay: return { 0xf048d42877ed34feull, 0xe4a76338723f169dull }; // fe34ed77-28d4-48f0-9d16-3f723863a7e4
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay: return { 0x9547198aa9221022ull, 0xebd00447775b10b9ull }; // 221022a9-8a19-4795-b910-5b774704d0eb
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay: return { 0x5649e2701f4b31fdull, 0xd3dc779b1a3648a2ull }; // fd314b1f-70e2-4956-a248-361a9b77dcd3
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay: return { 0x3f491b18456895f8ull, 0x3f3efddb7a28d7baull }; // f8956845-181b-493f-bad7-287adbfd3e3f
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay: return { 0x334962ff94fdaadcull, 0xca6c4c6be94a709dull }; // dcaafd94-ff62-4933-9d70-4ae96b4c6cca
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay: return { 0x944cf887f820092eull, 0xe0423aed57f0a791ull }; // 2e0920f8-87f8-4c94-91a7-f057ed3a42e0
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay: return { 0xb34d8952f22aec45ull, 0x1b9285288d8eb286ull }; // 45ec2af2-5289-4db3-86b2-8e8d2885921b
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay: return { 0xf048bde9d5ac4b5bull, 0xab036d8c0d908e8cull }; // 5b4bacd5-e9bd-48f0-8c8e-900d8c6d03ab
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay: return { 0x96484e79b56f584cull, 0xb6cea18676745f85ull }; // 4c586fb5-794e-4896-855f-747686a1ceb6
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay: return { 0xc74f82da70367862ull, 0xb89c60456722a5b7ull }; // 62783670-da82-4fc7-b7a5-226745609cb8
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay: return { 0xd04e52398ad814d4ull, 0x225f01577d1515baull }; // d414d88a-3952-4ed0-ba15-157d57015f22
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay: return { 0x76482a78b2762138ull, 0x3d8a09dcbb2c41b2ull }; // 382176b2-782a-4876-b241-2cbbdc098a3d
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay: return { 0xbf406f6558c38251ull, 0x6c8a82b1447daea5ull }; // 5182c358-656f-40bf-a5ae-7d44b1828a6c
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay: return { 0xf2438badf4150dd8ull, 0xe45e4631b42ec2bfull }; // d80d15f4-ad8b-43f2-bfc2-2eb431465ee4
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay: return { 0x104c5df8dc78c728ull, 0x65044a15813b5b90ull }; // 28c778dc-f85d-4c10-905b-3b81154a0465
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay_AsButton: return { 0x254ad4127b1c74b4ull, 0x9fd79fb7460853adull }; // b4741c7b-12d4-4a25-ad53-0846b79fd79f
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay: return { 0x2243226d6d2bf14full, 0x396f44ff56bfda97ull }; // 4ff12b6d-6d22-4322-97da-bf56ff446f39
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay: return { 0xf54e06fed441d0b5ull, 0x182a3191fd6cca9aull }; // b5d041d4-fe06-4ef5-9aca-6cfd91312a18
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay: return { 0x294878d6931fe2c0ull, 0x3eb220c9551ea488ull }; // c0e21f93-d678-4829-88a4-1e55c920b23e
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay: return { 0x284ccf7c0f269200ull, 0x2ba0c477ffea04beull }; // 0092260f-7ccf-4c28-be04-eaff77c4a02b
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay: return { 0xfe4babc7b0c703c1ull, 0x4f5899e87811b1adull }; // c103c7b0-c7ab-4bfe-adb1-1178e899584f
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay: return { 0xf7495b93a0f5a138ull, 0x3a77efe4cb86b28dull }; // 38a1f5a0-935b-49f7-8db2-86cbe4ef773a
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay: return { 0x774d8da2deb94d1full, 0x28c12a460f0a4babull }; // 1f4db9de-a28d-4d77-ab4b-0a0f462ac128
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay: return { 0x9f47f8629f994c0bull, 0x8e4b21c60cebd9acull }; // 0b4c999f-62f8-479f-acd9-eb0cc6214b8e
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay: return { 0x9d4b14517b5593eaull, 0x9121617db47e4caaull }; // ea93557b-5114-4b9d-aa4c-7eb47d612191
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay: return { 0x1f456980d2d176eaull, 0x11d50e78c781509dull }; // ea76d1d2-8069-451f-9d50-81c7780ed511
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay: return { 0x174aed6fa9f7d2e0ull, 0x5d27ee4213ac22a7ull }; // e0d2f7a9-6fed-4a17-a722-ac1342ee275d
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay: return { 0xe548b031b5c1d0f2ull, 0x588de2018c682f80ull }; // f2d0c1b5-31b0-48e5-802f-688c01e28d58
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay: return { 0xe34d283806ef892aull, 0xd3c342213e941598ull }; // 2a89ef06-3828-4de3-9815-943e2142c3d3
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay: return { 0x3446494353f88a0full, 0xb44e01174541bd93ull }; // 0f8af853-4349-4634-93bd-414517014eb4
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay: return { 0x23498d83b4f7dc6bull, 0xed5dd890bbbeb5bfull }; // 6bdcf7b4-838d-4923-bfb5-bebb90d85ded
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay: return { 0x4a4140a7c02bd9efull, 0xe3e95ff22ac3ceacull }; // efd92bc0-a740-414a-acce-c32af25fe9e3
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay: return { 0xd542b05b7fcd46b4ull, 0x40f5853c242b5fa0ull }; // b446cd7f-5bb0-42d5-a05f-2b243c85f540
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay: return { 0x0447d95cd15f9255ull, 0xf932d92064bfedaeull }; // 55925fd1-5cd9-4704-aeed-bf6420d932f9
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveButton: return { 0x7741ca9fd6e8c9d8ull, 0x3d89fdeed42988a2ull }; // d8c9e8d6-9fca-4177-a288-29d4eefd893d
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeButton: return { 0x7b469e6c87372754ull, 0x3e9e80756c528e82ull }; // 54273787-6c9e-467b-828e-526c75809e3e
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputControlTypeRef controlTypeRef)->InputGuid // GetControlTypeGuid
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Button: return { 0xac437425a6fe048full, 0x054bf8a1108663aaull }; // 8f04fea6-2574-43ac-aa63-8610a1f84b05
            case InputControlTypeBuiltIn::AxisOneWay: return { 0x8d4dad7247bab1c3ull, 0xa123ca8256d2a8b4ull }; // c3b1ba47-72ad-4d8d-b4a8-d25682ca23a1
            case InputControlTypeBuiltIn::AxisTwoWay: return { 0x454de2eb91149065ull, 0x5c9b5661e790e982ull }; // 65901491-ebe2-4d45-82e9-90e761569b5c
            case InputControlTypeBuiltIn::DeltaAxisTwoWay: return { 0x1a466f5c0d0d1a9bull, 0xa85fd73659b6f281ull }; // 9b1a0d0d-5c6f-461a-81f2-b65936d75fa8
            case InputControlTypeBuiltIn::Stick: return { 0x9b41ead0872fc928ull, 0x32ed854026a52daaull }; // 28c92f87-d0ea-419b-aa2d-a5264085ed32
            case InputControlTypeBuiltIn::DeltaVector2D: return { 0xa54d5e676905efceull, 0xe3cf0a07bdf90886ull }; // ceef0569-675e-4da5-8608-f9bd070acfe3
            case InputControlTypeBuiltIn::Position2D: return { 0x32403609bc463908ull, 0xe61c67afd0a90e9dull }; // 083946bc-0936-4032-9d0e-a9d0af671ce6
            default:
                return InputGuidInvalid;
            }
        },
        []()->uint32_t // GetDeviceRefCount
        {
            return 3 + 1; // max value + 1
        },
        []()->uint32_t // GetTraitRefCount
        {
            return 7 + 1; // max value + 1
        },
        []()->uint32_t // GetControlUsageCount
        {
            return 456 + 1; // max value + 1
        },
        []()->uint32_t // GetControlTypeCount
        {
            return 7 + 1; // max value + 1
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef, char* o, const uint32_t c)->uint32_t // GetDeviceName
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
            case InputDeviceBuiltIn::KeyboardWindows: return _InputStrToBuf(o, c, "Keyboard (Windows)");
            case InputDeviceBuiltIn::MouseMacOS: return _InputStrToBuf(o, c, "Mouse (macOS)");
            case InputDeviceBuiltIn::WindowsGamingInputGamepad: return _InputStrToBuf(o, c, "Gamepad (Windows.Gaming.Input)");
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, char* o, const uint32_t c)->uint32_t // GetTraitName
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
            case InputDeviceTraitBuiltIn::ExplicitlyPollableDevice: return _InputStrToBuf(o, c, "Explicitly Pollable Device");
            case InputDeviceTraitBuiltIn::Keyboard: return _InputStrToBuf(o, c, "Keyboard");
            case InputDeviceTraitBuiltIn::Pointer: return _InputStrToBuf(o, c, "Pointer");
            case InputDeviceTraitBuiltIn::Mouse: return _InputStrToBuf(o, c, "Mouse");
            case InputDeviceTraitBuiltIn::Gamepad: return _InputStrToBuf(o, c, "Gamepad");
            case InputDeviceTraitBuiltIn::DualSense: return _InputStrToBuf(o, c, "DualSense");
            case InputDeviceTraitBuiltIn::GenericControls: return _InputStrToBuf(o, c, "Generic Controls");
            default: return 0;
            }
        },
        [](const InputControlUsage usage, char* o, const uint32_t c)->uint32_t // GetControlFullName
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
            case InputControlUsageBuiltIn::Keyboard_EscapeButton: return _InputStrToBuf(o, c, "Keyboard/EscapeButton");
            case InputControlUsageBuiltIn::Keyboard_EscapeButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/EscapeButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_SpaceButton: return _InputStrToBuf(o, c, "Keyboard/SpaceButton");
            case InputControlUsageBuiltIn::Keyboard_SpaceButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/SpaceButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_EnterButton: return _InputStrToBuf(o, c, "Keyboard/EnterButton");
            case InputControlUsageBuiltIn::Keyboard_EnterButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/EnterButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_TabButton: return _InputStrToBuf(o, c, "Keyboard/TabButton");
            case InputControlUsageBuiltIn::Keyboard_TabButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/TabButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton: return _InputStrToBuf(o, c, "Keyboard/BackquoteButton");
            case InputControlUsageBuiltIn::Keyboard_BackquoteButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/BackquoteButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_QuoteButton: return _InputStrToBuf(o, c, "Keyboard/QuoteButton");
            case InputControlUsageBuiltIn::Keyboard_QuoteButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/QuoteButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton: return _InputStrToBuf(o, c, "Keyboard/SemicolonButton");
            case InputControlUsageBuiltIn::Keyboard_SemicolonButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/SemicolonButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_CommaButton: return _InputStrToBuf(o, c, "Keyboard/CommaButton");
            case InputControlUsageBuiltIn::Keyboard_CommaButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/CommaButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PeriodButton: return _InputStrToBuf(o, c, "Keyboard/PeriodButton");
            case InputControlUsageBuiltIn::Keyboard_PeriodButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PeriodButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_SlashButton: return _InputStrToBuf(o, c, "Keyboard/SlashButton");
            case InputControlUsageBuiltIn::Keyboard_SlashButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/SlashButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_BackslashButton: return _InputStrToBuf(o, c, "Keyboard/BackslashButton");
            case InputControlUsageBuiltIn::Keyboard_BackslashButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/BackslashButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton: return _InputStrToBuf(o, c, "Keyboard/LeftBracketButton");
            case InputControlUsageBuiltIn::Keyboard_LeftBracketButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftBracketButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton: return _InputStrToBuf(o, c, "Keyboard/RightBracketButton");
            case InputControlUsageBuiltIn::Keyboard_RightBracketButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightBracketButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_MinusButton: return _InputStrToBuf(o, c, "Keyboard/MinusButton");
            case InputControlUsageBuiltIn::Keyboard_MinusButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/MinusButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_EqualsButton: return _InputStrToBuf(o, c, "Keyboard/EqualsButton");
            case InputControlUsageBuiltIn::Keyboard_EqualsButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/EqualsButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton: return _InputStrToBuf(o, c, "Keyboard/UpArrowButton");
            case InputControlUsageBuiltIn::Keyboard_UpArrowButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/UpArrowButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton: return _InputStrToBuf(o, c, "Keyboard/DownArrowButton");
            case InputControlUsageBuiltIn::Keyboard_DownArrowButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/DownArrowButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton: return _InputStrToBuf(o, c, "Keyboard/LeftArrowButton");
            case InputControlUsageBuiltIn::Keyboard_LeftArrowButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftArrowButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton: return _InputStrToBuf(o, c, "Keyboard/RightArrowButton");
            case InputControlUsageBuiltIn::Keyboard_RightArrowButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightArrowButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_AButton: return _InputStrToBuf(o, c, "Keyboard/AButton");
            case InputControlUsageBuiltIn::Keyboard_AButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/AButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_BButton: return _InputStrToBuf(o, c, "Keyboard/BButton");
            case InputControlUsageBuiltIn::Keyboard_BButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/BButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_CButton: return _InputStrToBuf(o, c, "Keyboard/CButton");
            case InputControlUsageBuiltIn::Keyboard_CButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/CButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_DButton: return _InputStrToBuf(o, c, "Keyboard/DButton");
            case InputControlUsageBuiltIn::Keyboard_DButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/DButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_EButton: return _InputStrToBuf(o, c, "Keyboard/EButton");
            case InputControlUsageBuiltIn::Keyboard_EButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/EButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_FButton: return _InputStrToBuf(o, c, "Keyboard/FButton");
            case InputControlUsageBuiltIn::Keyboard_FButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/FButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_GButton: return _InputStrToBuf(o, c, "Keyboard/GButton");
            case InputControlUsageBuiltIn::Keyboard_GButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/GButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_HButton: return _InputStrToBuf(o, c, "Keyboard/HButton");
            case InputControlUsageBuiltIn::Keyboard_HButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/HButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_IButton: return _InputStrToBuf(o, c, "Keyboard/IButton");
            case InputControlUsageBuiltIn::Keyboard_IButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/IButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_JButton: return _InputStrToBuf(o, c, "Keyboard/JButton");
            case InputControlUsageBuiltIn::Keyboard_JButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/JButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_KButton: return _InputStrToBuf(o, c, "Keyboard/KButton");
            case InputControlUsageBuiltIn::Keyboard_KButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/KButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LButton: return _InputStrToBuf(o, c, "Keyboard/LButton");
            case InputControlUsageBuiltIn::Keyboard_LButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_MButton: return _InputStrToBuf(o, c, "Keyboard/MButton");
            case InputControlUsageBuiltIn::Keyboard_MButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/MButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NButton: return _InputStrToBuf(o, c, "Keyboard/NButton");
            case InputControlUsageBuiltIn::Keyboard_NButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OButton: return _InputStrToBuf(o, c, "Keyboard/OButton");
            case InputControlUsageBuiltIn::Keyboard_OButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PButton: return _InputStrToBuf(o, c, "Keyboard/PButton");
            case InputControlUsageBuiltIn::Keyboard_PButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_QButton: return _InputStrToBuf(o, c, "Keyboard/QButton");
            case InputControlUsageBuiltIn::Keyboard_QButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/QButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RButton: return _InputStrToBuf(o, c, "Keyboard/RButton");
            case InputControlUsageBuiltIn::Keyboard_RButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_SButton: return _InputStrToBuf(o, c, "Keyboard/SButton");
            case InputControlUsageBuiltIn::Keyboard_SButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/SButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_TButton: return _InputStrToBuf(o, c, "Keyboard/TButton");
            case InputControlUsageBuiltIn::Keyboard_TButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/TButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_UButton: return _InputStrToBuf(o, c, "Keyboard/UButton");
            case InputControlUsageBuiltIn::Keyboard_UButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/UButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_VButton: return _InputStrToBuf(o, c, "Keyboard/VButton");
            case InputControlUsageBuiltIn::Keyboard_VButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/VButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_WButton: return _InputStrToBuf(o, c, "Keyboard/WButton");
            case InputControlUsageBuiltIn::Keyboard_WButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/WButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_XButton: return _InputStrToBuf(o, c, "Keyboard/XButton");
            case InputControlUsageBuiltIn::Keyboard_XButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/XButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_YButton: return _InputStrToBuf(o, c, "Keyboard/YButton");
            case InputControlUsageBuiltIn::Keyboard_YButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/YButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_ZButton: return _InputStrToBuf(o, c, "Keyboard/ZButton");
            case InputControlUsageBuiltIn::Keyboard_ZButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/ZButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit1Button: return _InputStrToBuf(o, c, "Keyboard/Digit1Button");
            case InputControlUsageBuiltIn::Keyboard_Digit1Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit1Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit2Button: return _InputStrToBuf(o, c, "Keyboard/Digit2Button");
            case InputControlUsageBuiltIn::Keyboard_Digit2Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit2Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit3Button: return _InputStrToBuf(o, c, "Keyboard/Digit3Button");
            case InputControlUsageBuiltIn::Keyboard_Digit3Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit3Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit4Button: return _InputStrToBuf(o, c, "Keyboard/Digit4Button");
            case InputControlUsageBuiltIn::Keyboard_Digit4Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit4Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit5Button: return _InputStrToBuf(o, c, "Keyboard/Digit5Button");
            case InputControlUsageBuiltIn::Keyboard_Digit5Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit5Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit6Button: return _InputStrToBuf(o, c, "Keyboard/Digit6Button");
            case InputControlUsageBuiltIn::Keyboard_Digit6Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit6Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit7Button: return _InputStrToBuf(o, c, "Keyboard/Digit7Button");
            case InputControlUsageBuiltIn::Keyboard_Digit7Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit7Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit8Button: return _InputStrToBuf(o, c, "Keyboard/Digit8Button");
            case InputControlUsageBuiltIn::Keyboard_Digit8Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit8Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit9Button: return _InputStrToBuf(o, c, "Keyboard/Digit9Button");
            case InputControlUsageBuiltIn::Keyboard_Digit9Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit9Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Digit0Button: return _InputStrToBuf(o, c, "Keyboard/Digit0Button");
            case InputControlUsageBuiltIn::Keyboard_Digit0Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Digit0Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton: return _InputStrToBuf(o, c, "Keyboard/LeftShiftButton");
            case InputControlUsageBuiltIn::Keyboard_LeftShiftButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftShiftButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton: return _InputStrToBuf(o, c, "Keyboard/RightShiftButton");
            case InputControlUsageBuiltIn::Keyboard_RightShiftButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightShiftButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_ShiftButton: return _InputStrToBuf(o, c, "Keyboard/ShiftButton");
            case InputControlUsageBuiltIn::Keyboard_ShiftButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/ShiftButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton: return _InputStrToBuf(o, c, "Keyboard/LeftAltButton");
            case InputControlUsageBuiltIn::Keyboard_LeftAltButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftAltButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightAltButton: return _InputStrToBuf(o, c, "Keyboard/RightAltButton");
            case InputControlUsageBuiltIn::Keyboard_RightAltButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightAltButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_AltButton: return _InputStrToBuf(o, c, "Keyboard/AltButton");
            case InputControlUsageBuiltIn::Keyboard_AltButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/AltButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton: return _InputStrToBuf(o, c, "Keyboard/LeftCtrlButton");
            case InputControlUsageBuiltIn::Keyboard_LeftCtrlButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftCtrlButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton: return _InputStrToBuf(o, c, "Keyboard/RightCtrlButton");
            case InputControlUsageBuiltIn::Keyboard_RightCtrlButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightCtrlButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_CtrlButton: return _InputStrToBuf(o, c, "Keyboard/CtrlButton");
            case InputControlUsageBuiltIn::Keyboard_CtrlButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/CtrlButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton: return _InputStrToBuf(o, c, "Keyboard/LeftMetaButton");
            case InputControlUsageBuiltIn::Keyboard_LeftMetaButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/LeftMetaButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton: return _InputStrToBuf(o, c, "Keyboard/RightMetaButton");
            case InputControlUsageBuiltIn::Keyboard_RightMetaButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/RightMetaButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton: return _InputStrToBuf(o, c, "Keyboard/ContextMenuButton");
            case InputControlUsageBuiltIn::Keyboard_ContextMenuButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/ContextMenuButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton: return _InputStrToBuf(o, c, "Keyboard/BackspaceButton");
            case InputControlUsageBuiltIn::Keyboard_BackspaceButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/BackspaceButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PageDownButton: return _InputStrToBuf(o, c, "Keyboard/PageDownButton");
            case InputControlUsageBuiltIn::Keyboard_PageDownButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PageDownButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PageUpButton: return _InputStrToBuf(o, c, "Keyboard/PageUpButton");
            case InputControlUsageBuiltIn::Keyboard_PageUpButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PageUpButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_HomeButton: return _InputStrToBuf(o, c, "Keyboard/HomeButton");
            case InputControlUsageBuiltIn::Keyboard_HomeButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/HomeButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_EndButton: return _InputStrToBuf(o, c, "Keyboard/EndButton");
            case InputControlUsageBuiltIn::Keyboard_EndButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/EndButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_InsertButton: return _InputStrToBuf(o, c, "Keyboard/InsertButton");
            case InputControlUsageBuiltIn::Keyboard_InsertButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/InsertButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_DeleteButton: return _InputStrToBuf(o, c, "Keyboard/DeleteButton");
            case InputControlUsageBuiltIn::Keyboard_DeleteButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/DeleteButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton: return _InputStrToBuf(o, c, "Keyboard/CapsLockButton");
            case InputControlUsageBuiltIn::Keyboard_CapsLockButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/CapsLockButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumLockButton: return _InputStrToBuf(o, c, "Keyboard/NumLockButton");
            case InputControlUsageBuiltIn::Keyboard_NumLockButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumLockButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton: return _InputStrToBuf(o, c, "Keyboard/PrintScreenButton");
            case InputControlUsageBuiltIn::Keyboard_PrintScreenButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PrintScreenButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton: return _InputStrToBuf(o, c, "Keyboard/ScrollLockButton");
            case InputControlUsageBuiltIn::Keyboard_ScrollLockButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/ScrollLockButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_PauseButton: return _InputStrToBuf(o, c, "Keyboard/PauseButton");
            case InputControlUsageBuiltIn::Keyboard_PauseButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/PauseButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton: return _InputStrToBuf(o, c, "Keyboard/NumpadEnterButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadEnterButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadEnterButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton: return _InputStrToBuf(o, c, "Keyboard/NumpadDivideButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadDivideButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadDivideButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton: return _InputStrToBuf(o, c, "Keyboard/NumpadMultiplyButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadMultiplyButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadMultiplyButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton: return _InputStrToBuf(o, c, "Keyboard/NumpadPlusButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadPlusButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadPlusButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton: return _InputStrToBuf(o, c, "Keyboard/NumpadMinusButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadMinusButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadMinusButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton: return _InputStrToBuf(o, c, "Keyboard/NumpadPeriodButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadPeriodButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadPeriodButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton: return _InputStrToBuf(o, c, "Keyboard/NumpadEqualsButton");
            case InputControlUsageBuiltIn::Keyboard_NumpadEqualsButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/NumpadEqualsButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button: return _InputStrToBuf(o, c, "Keyboard/Numpad1Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad1Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad1Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button: return _InputStrToBuf(o, c, "Keyboard/Numpad2Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad2Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad2Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button: return _InputStrToBuf(o, c, "Keyboard/Numpad3Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad3Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad3Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button: return _InputStrToBuf(o, c, "Keyboard/Numpad4Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad4Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad4Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button: return _InputStrToBuf(o, c, "Keyboard/Numpad5Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad5Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad5Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button: return _InputStrToBuf(o, c, "Keyboard/Numpad6Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad6Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad6Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button: return _InputStrToBuf(o, c, "Keyboard/Numpad7Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad7Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad7Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button: return _InputStrToBuf(o, c, "Keyboard/Numpad8Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad8Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad8Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button: return _InputStrToBuf(o, c, "Keyboard/Numpad9Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad9Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad9Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button: return _InputStrToBuf(o, c, "Keyboard/Numpad0Button");
            case InputControlUsageBuiltIn::Keyboard_Numpad0Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/Numpad0Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F1Button: return _InputStrToBuf(o, c, "Keyboard/F1Button");
            case InputControlUsageBuiltIn::Keyboard_F1Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F1Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F2Button: return _InputStrToBuf(o, c, "Keyboard/F2Button");
            case InputControlUsageBuiltIn::Keyboard_F2Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F2Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F3Button: return _InputStrToBuf(o, c, "Keyboard/F3Button");
            case InputControlUsageBuiltIn::Keyboard_F3Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F3Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F4Button: return _InputStrToBuf(o, c, "Keyboard/F4Button");
            case InputControlUsageBuiltIn::Keyboard_F4Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F4Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F5Button: return _InputStrToBuf(o, c, "Keyboard/F5Button");
            case InputControlUsageBuiltIn::Keyboard_F5Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F5Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F6Button: return _InputStrToBuf(o, c, "Keyboard/F6Button");
            case InputControlUsageBuiltIn::Keyboard_F6Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F6Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F7Button: return _InputStrToBuf(o, c, "Keyboard/F7Button");
            case InputControlUsageBuiltIn::Keyboard_F7Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F7Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F8Button: return _InputStrToBuf(o, c, "Keyboard/F8Button");
            case InputControlUsageBuiltIn::Keyboard_F8Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F8Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F9Button: return _InputStrToBuf(o, c, "Keyboard/F9Button");
            case InputControlUsageBuiltIn::Keyboard_F9Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F9Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F10Button: return _InputStrToBuf(o, c, "Keyboard/F10Button");
            case InputControlUsageBuiltIn::Keyboard_F10Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F10Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F11Button: return _InputStrToBuf(o, c, "Keyboard/F11Button");
            case InputControlUsageBuiltIn::Keyboard_F11Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F11Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_F12Button: return _InputStrToBuf(o, c, "Keyboard/F12Button");
            case InputControlUsageBuiltIn::Keyboard_F12Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/F12Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OEM1Button: return _InputStrToBuf(o, c, "Keyboard/OEM1Button");
            case InputControlUsageBuiltIn::Keyboard_OEM1Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OEM1Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OEM2Button: return _InputStrToBuf(o, c, "Keyboard/OEM2Button");
            case InputControlUsageBuiltIn::Keyboard_OEM2Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OEM2Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OEM3Button: return _InputStrToBuf(o, c, "Keyboard/OEM3Button");
            case InputControlUsageBuiltIn::Keyboard_OEM3Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OEM3Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OEM4Button: return _InputStrToBuf(o, c, "Keyboard/OEM4Button");
            case InputControlUsageBuiltIn::Keyboard_OEM4Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OEM4Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Keyboard_OEM5Button: return _InputStrToBuf(o, c, "Keyboard/OEM5Button");
            case InputControlUsageBuiltIn::Keyboard_OEM5Button_AsAxisOneWay: return _InputStrToBuf(o, c, "Keyboard/OEM5Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::Pointer_PositionPosition2D: return _InputStrToBuf(o, c, "Pointer/PositionPosition2D");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_VerticalDeltaAxisTwoWay: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/VerticalDeltaAxisTwoWay");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_HorizontalDeltaAxisTwoWay: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/HorizontalDeltaAxisTwoWay");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_LeftButton: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/LeftButton");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_UpButton: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/UpButton");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_RightButton: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/RightButton");
            case InputControlUsageBuiltIn::Mouse_MotionDeltaVector2D_DownButton: return _InputStrToBuf(o, c, "Mouse/MotionDeltaVector2D/DownButton");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_VerticalDeltaAxisTwoWay: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/VerticalDeltaAxisTwoWay");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_HorizontalDeltaAxisTwoWay: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/HorizontalDeltaAxisTwoWay");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_LeftButton: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/LeftButton");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_UpButton: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/UpButton");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_RightButton: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/RightButton");
            case InputControlUsageBuiltIn::Mouse_ScrollDeltaVector2D_DownButton: return _InputStrToBuf(o, c, "Mouse/ScrollDeltaVector2D/DownButton");
            case InputControlUsageBuiltIn::Mouse_LeftButton: return _InputStrToBuf(o, c, "Mouse/LeftButton");
            case InputControlUsageBuiltIn::Mouse_LeftButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Mouse/LeftButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Mouse_MiddleButton: return _InputStrToBuf(o, c, "Mouse/MiddleButton");
            case InputControlUsageBuiltIn::Mouse_MiddleButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Mouse/MiddleButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Mouse_RightButton: return _InputStrToBuf(o, c, "Mouse/RightButton");
            case InputControlUsageBuiltIn::Mouse_RightButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Mouse/RightButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Mouse_BackButton: return _InputStrToBuf(o, c, "Mouse/BackButton");
            case InputControlUsageBuiltIn::Mouse_BackButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Mouse/BackButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Mouse_ForwardButton: return _InputStrToBuf(o, c, "Mouse/ForwardButton");
            case InputControlUsageBuiltIn::Mouse_ForwardButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Mouse/ForwardButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_WestButton: return _InputStrToBuf(o, c, "Gamepad/WestButton");
            case InputControlUsageBuiltIn::Gamepad_WestButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/WestButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_NorthButton: return _InputStrToBuf(o, c, "Gamepad/NorthButton");
            case InputControlUsageBuiltIn::Gamepad_NorthButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/NorthButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_EastButton: return _InputStrToBuf(o, c, "Gamepad/EastButton");
            case InputControlUsageBuiltIn::Gamepad_EastButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/EastButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_SouthButton: return _InputStrToBuf(o, c, "Gamepad/SouthButton");
            case InputControlUsageBuiltIn::Gamepad_SouthButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/SouthButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick: return _InputStrToBuf(o, c, "Gamepad/LeftStick");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_VerticalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/VerticalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_HorizontalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/HorizontalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/LeftAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/UpAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/RightAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftStick/DownAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_LeftButton: return _InputStrToBuf(o, c, "Gamepad/LeftStick/LeftButton");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_UpButton: return _InputStrToBuf(o, c, "Gamepad/LeftStick/UpButton");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_RightButton: return _InputStrToBuf(o, c, "Gamepad/LeftStick/RightButton");
            case InputControlUsageBuiltIn::Gamepad_LeftStick_DownButton: return _InputStrToBuf(o, c, "Gamepad/LeftStick/DownButton");
            case InputControlUsageBuiltIn::Gamepad_RightStick: return _InputStrToBuf(o, c, "Gamepad/RightStick");
            case InputControlUsageBuiltIn::Gamepad_RightStick_VerticalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/VerticalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_HorizontalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/HorizontalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/LeftAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/UpAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/RightAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightStick/DownAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightStick_LeftButton: return _InputStrToBuf(o, c, "Gamepad/RightStick/LeftButton");
            case InputControlUsageBuiltIn::Gamepad_RightStick_UpButton: return _InputStrToBuf(o, c, "Gamepad/RightStick/UpButton");
            case InputControlUsageBuiltIn::Gamepad_RightStick_RightButton: return _InputStrToBuf(o, c, "Gamepad/RightStick/RightButton");
            case InputControlUsageBuiltIn::Gamepad_RightStick_DownButton: return _InputStrToBuf(o, c, "Gamepad/RightStick/DownButton");
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton: return _InputStrToBuf(o, c, "Gamepad/LeftStickButton");
            case InputControlUsageBuiltIn::Gamepad_LeftStickButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftStickButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightStickButton: return _InputStrToBuf(o, c, "Gamepad/RightStickButton");
            case InputControlUsageBuiltIn::Gamepad_RightStickButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightStickButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick: return _InputStrToBuf(o, c, "Gamepad/DPadStick");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_VerticalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/VerticalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_HorizontalAxisTwoWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/HorizontalAxisTwoWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/LeftAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/UpAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/RightAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/DPadStick/DownAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_LeftButton: return _InputStrToBuf(o, c, "Gamepad/DPadStick/LeftButton");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_UpButton: return _InputStrToBuf(o, c, "Gamepad/DPadStick/UpButton");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_RightButton: return _InputStrToBuf(o, c, "Gamepad/DPadStick/RightButton");
            case InputControlUsageBuiltIn::Gamepad_DPadStick_DownButton: return _InputStrToBuf(o, c, "Gamepad/DPadStick/DownButton");
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton: return _InputStrToBuf(o, c, "Gamepad/LeftShoulderButton");
            case InputControlUsageBuiltIn::Gamepad_LeftShoulderButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftShoulderButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton: return _InputStrToBuf(o, c, "Gamepad/RightShoulderButton");
            case InputControlUsageBuiltIn::Gamepad_RightShoulderButton_AsAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightShoulderButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/LeftTriggerAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_LeftTriggerAxisOneWay_AsButton: return _InputStrToBuf(o, c, "Gamepad/LeftTriggerAxisOneWay/AsButton");
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay: return _InputStrToBuf(o, c, "Gamepad/RightTriggerAxisOneWay");
            case InputControlUsageBuiltIn::Gamepad_RightTriggerAxisOneWay_AsButton: return _InputStrToBuf(o, c, "Gamepad/RightTriggerAxisOneWay/AsButton");
            case InputControlUsageBuiltIn::DualSense_OptionsButton: return _InputStrToBuf(o, c, "DualSense/OptionsButton");
            case InputControlUsageBuiltIn::DualSense_OptionsButton_AsAxisOneWay: return _InputStrToBuf(o, c, "DualSense/OptionsButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::DualSense_ShareButton: return _InputStrToBuf(o, c, "DualSense/ShareButton");
            case InputControlUsageBuiltIn::DualSense_ShareButton_AsAxisOneWay: return _InputStrToBuf(o, c, "DualSense/ShareButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::DualSense_PlaystationButton: return _InputStrToBuf(o, c, "DualSense/PlaystationButton");
            case InputControlUsageBuiltIn::DualSense_PlaystationButton_AsAxisOneWay: return _InputStrToBuf(o, c, "DualSense/PlaystationButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::DualSense_MicButton: return _InputStrToBuf(o, c, "DualSense/MicButton");
            case InputControlUsageBuiltIn::DualSense_MicButton_AsAxisOneWay: return _InputStrToBuf(o, c, "DualSense/MicButton/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0Button: return _InputStrToBuf(o, c, "GenericControls/Generic0Button");
            case InputControlUsageBuiltIn::GenericControls_Generic0Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic0Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic1Button: return _InputStrToBuf(o, c, "GenericControls/Generic1Button");
            case InputControlUsageBuiltIn::GenericControls_Generic1Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic1Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic2Button: return _InputStrToBuf(o, c, "GenericControls/Generic2Button");
            case InputControlUsageBuiltIn::GenericControls_Generic2Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic2Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic3Button: return _InputStrToBuf(o, c, "GenericControls/Generic3Button");
            case InputControlUsageBuiltIn::GenericControls_Generic3Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic3Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic4Button: return _InputStrToBuf(o, c, "GenericControls/Generic4Button");
            case InputControlUsageBuiltIn::GenericControls_Generic4Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic4Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic5Button: return _InputStrToBuf(o, c, "GenericControls/Generic5Button");
            case InputControlUsageBuiltIn::GenericControls_Generic5Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic5Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic6Button: return _InputStrToBuf(o, c, "GenericControls/Generic6Button");
            case InputControlUsageBuiltIn::GenericControls_Generic6Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic6Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic7Button: return _InputStrToBuf(o, c, "GenericControls/Generic7Button");
            case InputControlUsageBuiltIn::GenericControls_Generic7Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic7Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic8Button: return _InputStrToBuf(o, c, "GenericControls/Generic8Button");
            case InputControlUsageBuiltIn::GenericControls_Generic8Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic8Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic9Button: return _InputStrToBuf(o, c, "GenericControls/Generic9Button");
            case InputControlUsageBuiltIn::GenericControls_Generic9Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic9Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic10Button: return _InputStrToBuf(o, c, "GenericControls/Generic10Button");
            case InputControlUsageBuiltIn::GenericControls_Generic10Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic10Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic11Button: return _InputStrToBuf(o, c, "GenericControls/Generic11Button");
            case InputControlUsageBuiltIn::GenericControls_Generic11Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic11Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic12Button: return _InputStrToBuf(o, c, "GenericControls/Generic12Button");
            case InputControlUsageBuiltIn::GenericControls_Generic12Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic12Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic13Button: return _InputStrToBuf(o, c, "GenericControls/Generic13Button");
            case InputControlUsageBuiltIn::GenericControls_Generic13Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic13Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic14Button: return _InputStrToBuf(o, c, "GenericControls/Generic14Button");
            case InputControlUsageBuiltIn::GenericControls_Generic14Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic14Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic15Button: return _InputStrToBuf(o, c, "GenericControls/Generic15Button");
            case InputControlUsageBuiltIn::GenericControls_Generic15Button_AsAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic15Button/AsAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisOneWay_AsButton: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisOneWay/AsButton");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic0AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic0AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic1AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic1AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic2AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic2AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic3AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic3AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic4AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic4AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic5AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic5AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic6AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic6AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic7AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic7AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic8AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic8AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic9AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic9AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic10AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic10AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic11AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic11AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic12AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic12AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic13AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic13AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic14AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic14AxisTwoWay/NegativeButton");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisTwoWay");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisTwoWay/PositiveAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeAxisOneWay: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisTwoWay/NegativeAxisOneWay");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_PositiveButton: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisTwoWay/PositiveButton");
            case InputControlUsageBuiltIn::GenericControls_Generic15AxisTwoWay_NegativeButton: return _InputStrToBuf(o, c, "GenericControls/Generic15AxisTwoWay/NegativeButton");
            default: return 0;
            }
        },
        [](const InputControlTypeRef controlTypeRef, char* o, const uint32_t c)->uint32_t // GetControlTypeName
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Button: return _InputStrToBuf(o, c, "Button");
            case InputControlTypeBuiltIn::AxisOneWay: return _InputStrToBuf(o, c, "Axis One Way [0,1]");
            case InputControlTypeBuiltIn::AxisTwoWay: return _InputStrToBuf(o, c, "Axis Two Way [-1,1]");
            case InputControlTypeBuiltIn::DeltaAxisTwoWay: return _InputStrToBuf(o, c, "Delta Axis Two Way [-1,1] per actuation");
            case InputControlTypeBuiltIn::Stick: return _InputStrToBuf(o, c, "Stick 2D [-1,1]");
            case InputControlTypeBuiltIn::DeltaVector2D: return _InputStrToBuf(o, c, "Delta Vector 2D [-1,1] per actuation");
            case InputControlTypeBuiltIn::Position2D: return _InputStrToBuf(o, c, "Absolute 2D Vector normalized to [0,1] with surface index");
            default: return 0;
            }
        },
    };
}

#endif

#endif
