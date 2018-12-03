#include "iOSScreenKeyboardDelegate.h"
#include "DisplayManager.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>

struct iOSScreenKeyboardShowParams
{
    int type;
    const char* initialText;
    const char* placeholderText;
    int autocorrection;
    int multiline;
    int secure;
    int alert;
};

extern "C" void _iOSScreenKeyboardShow(iOSScreenKeyboardShowParams* showParams, int sizeOfShowParams)
{
    if (sizeof(iOSScreenKeyboardShowParams) != sizeOfShowParams)
    {
        NSLog(@"ScreenKeyboardShowParams size mismatch, expected %lu was %d", sizeof(iOSScreenKeyboardShowParams), sizeOfShowParams);
        return;
    }
    
    
#if PLATFORM_TVOS
    // Not supported. The API for showing keyboard for editing multi-line text
    // is not available on tvOS
    multiline = false;
#endif
    
    static const UIKeyboardType keyboardTypes[] =
    {
        UIKeyboardTypeDefault,
        UIKeyboardTypeASCIICapable,
        UIKeyboardTypeNumbersAndPunctuation,
        UIKeyboardTypeURL,
        UIKeyboardTypeNumberPad,
        UIKeyboardTypePhonePad,
        UIKeyboardTypeNamePhonePad,
        UIKeyboardTypeEmailAddress,
        UIKeyboardTypeDefault, // Default is used in case Wii U specific NintendoNetworkAccount type is selected (indexed at 8 in UnityEngine.TouchScreenKeyboardType)
        UIKeyboardTypeTwitter,
        UIKeyboardTypeWebSearch
    };
    
    
    iOSScreenKeyboardShowParamsNative param =
    {
        // TODO is it safe to pass char pointers?
        showParams->initialText, showParams->placeholderText,
        keyboardTypes[showParams->type],
        showParams->autocorrection ? UITextAutocorrectionTypeDefault : UITextAutocorrectionTypeNo,
        showParams->alert ? UIKeyboardAppearanceAlert : UIKeyboardAppearanceDefault,
        (BOOL)showParams->multiline, (BOOL)showParams->secure,
        0 //TODO
    };
    
    [[iOSScreenKeyboardDelegate Instance] setKeyboardParams: param];
    [[iOSScreenKeyboardDelegate Instance] show];
}
