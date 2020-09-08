#include "iOSScreenKeyboardBridge.h"
#include "DisplayManager.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>

// Keep in sync with ScreenKeyboardType from com.unity.inputsystem/InputSystem/Devices/ScreenKeyboard.cs
enum iOSScreenKeyboardType : int
{
    kDefault = 0,
    kASCIICapable = 1,
    kNumbersAndPunctuation = 2,
    kURL = 3,
    kNumberPad = 4,
    kPhonePad = 5,
    kNamePhonePad = 6,
    kEmailAddress = 7,
    kSocial = 8,
    kSearch = 9
};

struct iOSScreenKeyboardShowParams
{
    iOSScreenKeyboardType type;
    const char* initialText;
    const char* placeholderText;
    int autocorrection;
    int multiline;
    int secure;
    int alert;
    int inputFieldHidden;
};

struct UnityRect
{
    float x;
    float y;
    float width;
    float height;
};

UIKeyboardType GetUIKeyboardType(iOSScreenKeyboardType type)
{
    switch (type)
    {
        case kDefault:
            return UIKeyboardTypeDefault;
        case kASCIICapable:
            return UIKeyboardTypeASCIICapable;
        case kNumbersAndPunctuation:
            return UIKeyboardTypeNumbersAndPunctuation;
        case kURL:
            return UIKeyboardTypeURL;
        case kNumberPad:
            return UIKeyboardTypeNumberPad;
        case kPhonePad:
            return UIKeyboardTypePhonePad;
        case kNamePhonePad:
            return UIKeyboardTypeNamePhonePad;
        case kEmailAddress:
            return UIKeyboardTypeEmailAddress;
        case kSocial:
            return UIKeyboardTypeTwitter;
        case kSearch:
            return UIKeyboardTypeWebSearch;
        default:
            NSLog(@"Unknown keyboard type: %d", type);
            return UIKeyboardTypeDefault;
    }
}

extern "C" void _iOSScreenKeyboardShow(iOSScreenKeyboardShowParams* showParams, int sizeOfShowParams, iOSScreenKeyboardCallbacks* callbacks, int sizeOfCallbacks)
{
    if (sizeof(iOSScreenKeyboardShowParams) != sizeOfShowParams)
    {
        NSLog(@"ScreenKeyboardShowParams size mismatch, expected %lu was %d", sizeof(iOSScreenKeyboardShowParams), sizeOfShowParams);
        return;
    }

    if (sizeof(iOSScreenKeyboardCallbacks) != sizeOfCallbacks)
    {
        NSLog(@"iOSScreenKeyboardCallbacks size mismatch, expected %lu was %d", sizeof(iOSScreenKeyboardCallbacks), sizeOfCallbacks);
        return;
    }

    iOSScreenKeyboardShowParamsNative param =
    {
        GetUIKeyboardType(showParams->type),
        showParams->autocorrection ? UITextAutocorrectionTypeDefault : UITextAutocorrectionTypeNo,
        showParams->alert ? UIKeyboardAppearanceAlert : UIKeyboardAppearanceDefault,
#if PLATFORM_TVOS
        //The API for showing keyboard for editing multi-line text is not available on tvOS
        FALSE,
#else
        (BOOL)showParams->multiline,
#endif
        (BOOL)showParams->secure,
        (BOOL)showParams->inputFieldHidden,
        *callbacks
    };

    [[iOSScreenKeyboardBridge getInstanceOrCreate] show: param withInitialTextCStr: showParams->initialText withPlaceholderTextCStr: showParams->placeholderText];
}

extern "C" void _iOSScreenKeyboardHide()
{
    [[iOSScreenKeyboardBridge getInstanceOrCreate] hide: StateDone];
}

extern "C" UnityRect _iOSScreenKeyboardOccludingArea()
{
    iOSScreenKeyboardBridge* keyboard = [iOSScreenKeyboardBridge getInstance];
    if (keyboard == NULL)
    {
        UnityRect zero = {0, 0, 0, 0};
        return zero;
    }
    CGRect rc = keyboard.area;
    UnityRect unityRC = {(float)rc.origin.x, (float)rc.origin.y, (float)rc.size.width, (float)rc.size.height};
    return unityRC;
}

extern "C" void _iOSScreenKeyboardSetInputFieldText(const char* text)
{
    iOSScreenKeyboardBridge* keyboard = [iOSScreenKeyboardBridge getInstance];
    if (keyboard == NULL)
        return;
    NSString* convertedText = text ? [NSString stringWithUTF8String: text] : @"";
    [keyboard setText: convertedText];
}

extern "C" const char* _iOSScreenKeyboardGetInputFieldText()
{
    iOSScreenKeyboardBridge* keyboard = [iOSScreenKeyboardBridge getInstance];
    if (keyboard == NULL)
        return NULL;

    return strdup([[keyboard getText] UTF8String]);
}

extern "C" void _iOSScreenKeyboardSetSelection(int start, int length)
{
    iOSScreenKeyboardBridge* keyboard = [iOSScreenKeyboardBridge getInstance];
    if (keyboard == NULL)
        return;
    [keyboard setSelection: NSMakeRange(start, length)];
}

extern "C" long _iOSScreenKeyboardGetSelection()
{
    iOSScreenKeyboardBridge* keyboard = [iOSScreenKeyboardBridge getInstance];
    if (keyboard == NULL)
        return 0;

    NSRange range = keyboard.getSelection;
    return range.location | (range.length << 32);
}

extern "C" void _iOSScreenKeyboardSetLogging(int enabled)
{
    [iOSScreenKeyboardBridge setLogging: enabled > 0];
}

extern "C" int _iOSScreenKeyboardGetLogging()
{
    return [iOSScreenKeyboardBridge getLogging] ? 1 : 0;
}

extern "C" void _iOSScreenKeyboardCleanup()
{
    [iOSScreenKeyboardBridge cleanup];
}
