#pragma once

typedef void (*OnTextChangedCallback) (const char* text, int selectionStart, int selectionLength);
typedef void (*OnStatusChangedCallback) (int status);

struct iOSScreenKeyboardCallbacks
{
    OnTextChangedCallback textChangedCallback;
    OnStatusChangedCallback statusChangedCallback;
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



@interface iOSScreenKeyboardDelegate : NSObject<UITextFieldDelegate, UITextViewDelegate>

+ (iOSScreenKeyboardDelegate*)GetInstanceOrCreate;
+ (iOSScreenKeyboardDelegate*)GetInstance;

- (void)Show:(iOSScreenKeyboardShowParamsNative)param :(const char*)initialTextCStr :(const char*)placeholderTextCStr;
- (void)Hide;

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


- (NSString*)getText;
- (void)setText:(NSString*)newText;
 */

@property (readonly, nonatomic, getter = queryArea)               CGRect          area;
//@property (readonly, nonatomic)                                 BOOL            active;
//@property (readonly, nonatomic)                                 KeyboardStatus  status;
//@property (retain, nonatomic, getter = getText, setter = setText:)  NSString*       text;
//@property (assign, nonatomic)   int characterLimit;
//@property (readonly, nonatomic)                                 BOOL        canGetSelection;
//@property (nonatomic, getter = querySelection, setter = assignSelection:)  NSRange   selection;

@end

