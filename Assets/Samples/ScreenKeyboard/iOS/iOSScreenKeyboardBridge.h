#pragma once

typedef void (*OnTextChangedCallback) (const char* text);
typedef void (*OnStateChangedCallback) (int state);
typedef void (*OnSelectionChangedCallback) (int start, int length);

struct iOSScreenKeyboardCallbacks
{
    OnTextChangedCallback textChangedCallback;
    OnStateChangedCallback stateChangedCallback;
    OnSelectionChangedCallback selectionChanagedCallback;
};

struct iOSScreenKeyboardShowParamsNative
{
    UIKeyboardType              keyboardType;
    UITextAutocorrectionType    autocorrectionType;
    UIKeyboardAppearance        appearance;

    BOOL                        multiline;
    BOOL                        secure;
    BOOL                        inputFieldHidden;
    iOSScreenKeyboardCallbacks  callbacks;
};

// Must be in sync with com.unity.inputsystem/InputSystem/Devices/ScreenKeyboard.cs ScreenKeyboardState
enum iOSScreenKeyboardState
{
    StateDone        = 0,
    StateVisible     = 1,
    StateCanceled    = 2,
    StateLostFocus   = 3,
};

@interface iOSScreenKeyboardBridge : NSObject<UITextFieldDelegate, UITextViewDelegate>

+ (iOSScreenKeyboardBridge*)getInstanceOrCreate;
+ (iOSScreenKeyboardBridge*)getInstance;
+ (void)cleanup;

+ (bool)getLogging;
+ (void)setLogging:(bool)enabled;

- (void)show:(iOSScreenKeyboardShowParamsNative)param withInitialTextCStr:(const char*)initialTextCStr withPlaceholderTextCStr:(const char*)placeholderTextCStr;
- (void)hide:(iOSScreenKeyboardState)hideState;
- (NSString*)getText;
- (void)setText:(NSString*)newText;
- (NSRange)getSelection;
- (void)setSelection:(NSRange)newSelection;

@property (readonly, nonatomic, getter = queryArea)               CGRect          area;

@end
