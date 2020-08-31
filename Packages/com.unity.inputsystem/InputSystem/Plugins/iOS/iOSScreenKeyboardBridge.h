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

- (void)show:(iOSScreenKeyboardShowParamsNative)param withInitialTextCStr:(const char*)initialTextCStr withPlaceholderTextCStr:(const char*)placeholderTextCStr;
- (void)hide:(iOSScreenKeyboardState)hideState;

// These are all privates
/*
- (BOOL)textFieldShouldReturn:(UITextField*)textField;
- (void)textInputDone:(id)sender;
- (void)textInputCancel:(id)sender;
- (void)textInputLostFocus;
- (void)textViewDidChange:(UITextView *)textView;
- (void)keyboardWillShow:(NSNotification*)notification;
- (void)keyboardDidShow:(NSNotification*)notification;
- (void)keyboardWillHide:(NSNotification*)notification;

// on older devices initial keyboard creation might be slow, so it is good to init in on initial loading.
// on the other hand, if you dont use keyboard (or use it rarely), you can avoid having all related stuff in memory:
//     keyboard will be created on demand anyway (in Instance method)

- (id)init;
- (void)positionInput:(CGRect)keyboardRect x:(float)x y:(float)y;
- (void)shouldHideInput:(BOOL)hide;

+ (void)StartReorientation;
+ (void)FinishReorientation;

*/
- (NSString*)getText;
- (void)setText:(NSString*)newText;
- (NSRange)getSelection;
- (void)setSelection:(NSRange)newSelection;

@property (readonly, nonatomic, getter = queryArea)               CGRect          area;
//@property (readonly, nonatomic)                                 BOOL            active;
//@property (readonly, nonatomic)                                 KeyboardState  state;
//@property (retain, nonatomic, getter = getText, setter = setText:)  NSString*       text;
//@property (assign, nonatomic)   int characterLimit;
//@property (readonly, nonatomic)                                 BOOL        canGetSelection;
//@property (nonatomic, getter = querySelection, setter = assignSelection:)  NSRange   selection;

@end
