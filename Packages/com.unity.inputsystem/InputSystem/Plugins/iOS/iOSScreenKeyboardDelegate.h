#pragma once

typedef void (*OnTextChangedCallback) (int deviceId, const char* text);
typedef void (*OnStatusChangedCallback) (int deviceId, int status);
typedef void (*OnSelectionChangedCallback) (int deviceId, int start, int length);

struct iOSScreenKeyboardCallbacks
{
    int deviceId;
    OnTextChangedCallback textChangedCallback;
    OnStatusChangedCallback statusChangedCallback;
    OnSelectionChangedCallback selectionChanagedCallback;
};

struct iOSScreenKeyboardShowParamsNative
{
    UIKeyboardType              keyboardType;
    UITextAutocorrectionType    autocorrectionType;
    UIKeyboardAppearance        appearance;

    BOOL multiline;
    BOOL secure;
    iOSScreenKeyboardCallbacks  callbacks;
};

// Must be in sync with com.unity.inputsystem/InputSystem/Devices/ScreenKeyboard.cs ScreenKeyboardStatus
enum iOSScreenKeyboardStatus
{
    StatusDone        = 0,
    StatusVisible     = 1,
    StatusCanceled    = 2,
    StatusLostFocus   = 3,
};

@interface iOSScreenKeyboardDelegate : NSObject<UITextFieldDelegate, UITextViewDelegate>

+ (iOSScreenKeyboardDelegate*)getInstanceOrCreate;
+ (iOSScreenKeyboardDelegate*)getInstance;

- (void)show: (iOSScreenKeyboardShowParamsNative)param withInitialTextCStr:(const char*)initialTextCStr withPlaceholderTextCStr:(const char*)placeholderTextCStr;
- (void)hide: (iOSScreenKeyboardStatus)hideStatus;

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
//@property (readonly, nonatomic)                                 KeyboardStatus  status;
//@property (retain, nonatomic, getter = getText, setter = setText:)  NSString*       text;
//@property (assign, nonatomic)   int characterLimit;
//@property (readonly, nonatomic)                                 BOOL        canGetSelection;
//@property (nonatomic, getter = querySelection, setter = assignSelection:)  NSRange   selection;

@end
