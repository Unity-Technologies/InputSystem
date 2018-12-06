#include "iOSScreenKeyboardDelegate.h"
#include "DisplayManager.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>

// TODO PLATFORM IOS AND TV DEFINES

static iOSScreenKeyboardDelegate* s_Keyboard = nil;

static bool                 _shouldHideInput = false;
static bool                 _shouldHideInputChanged = false;
static const unsigned kToolBarHeight = 40;
static const unsigned       kSystemButtonsSpace = 2 * 60 + 3 * 18; // empirical value, there is no way to know the exact widths of the system bar buttons


@implementation iOSScreenKeyboardDelegate
{
    // UI handling
    // in case of single line we use UITextField inside UIToolbar
    // in case of multi-line input we use UITextView with UIToolbar as accessory view
    // toolbar buttons are kept around to prevent releasing them
    // tvOS does not support multiline input thus only UITextField option is implemented
#if PLATFORM_IOS
    UITextView*     textView;
    
    UIToolbar*      viewToolbar;
    NSArray*        viewToolbarItems;
    
    NSLayoutConstraint* widthConstraint;
    
    UIToolbar*      fieldToolbar;
    NSArray*        fieldToolbarItems;
#endif
    
    UITextField*    textField;
    
    // inputView is view used for actual input (it will be responder): UITextField [single-line] or UITextView [multi-line]
    // editView is the "root" view for keyboard: UIToolbar [single-line] or UITextView [multi-line]
    UIView*         inputView;
    UIView*         editView;
    iOSScreenKeyboardShowParamsNative cachedKeyboardParam;
    
    CGRect          _area;
    NSString*       initialText;
    
    UIKeyboardType  keyboardType;
    
    BOOL            _multiline;
    BOOL            _inputHidden;
    BOOL            _active;
    KeyboardStatus          _status;
    
    // not pretty but seems like easiest way to keep "we are rotating" status
    BOOL            _rotating;
}

@synthesize area;
@synthesize active      = _active;
@synthesize status      = _status;
@synthesize text;
@synthesize selection;


- (BOOL)textFieldShouldReturn:(UITextField*)textFieldObj
{
    [self textInputDone: nil];
    return YES;
}

- (void)textInputDone:(id)sender
{
    if (_status == Visible)
    {
        _status = Done;
        // TODO
        //UnityKeyboard_StatusChanged(_status);
    }
    [self Hide];
}

- (void)textInputCancel:(id)sender
{
    _status = Canceled;
    // TODO
    //UnityKeyboard_StatusChanged(_status);
    [self Hide];
}

- (void)textInputLostFocus
{
    if (_status == Visible)
    {
        _status = LostFocus;
        // TODO
        //UnityKeyboard_StatusChanged(_status);
    }
    [self Hide];
}

- (void)textViewDidChange:(UITextView *)textView
{
    // TODO
    //UnityKeyboard_TextChanged(textView.text);
}

- (void)textFieldDidChange:(UITextField*)textField
{
    // TODO
    //UnityKeyboard_TextChanged(textField.text);
}

- (BOOL)textViewShouldBeginEditing:(UITextView*)view
{
#if !PLATFORM_TVOS
    view.inputAccessoryView = viewToolbar;
#endif
    return YES;
}

#if PLATFORM_IOS

- (void)keyboardWillShow:(NSNotification *)notification
{
    if (notification.userInfo == nil || inputView == nil)
        return;
    
    CGRect srcRect  = [[notification.userInfo objectForKey: UIKeyboardFrameEndUserInfoKey] CGRectValue];
    CGRect rect     = [UnityGetGLView() convertRect: srcRect fromView: nil];
    rect.origin.y = [UnityGetGLView() frame].size.height - rect.size.height; // iPhone X sometimes reports wrong y value for keyboard
    
    [self positionInput: rect x: rect.origin.x y: rect.origin.y];
}

- (void)keyboardDidShow:(NSNotification*)notification
{
    _active = YES;
}

- (void)keyboardWillHide:(NSNotification*)notification
{
    [self systemHideKeyboard];
}

- (void)keyboardDidChangeFrame:(NSNotification*)notification
{
    _active = true;
    
    CGRect srcRect  = [[notification.userInfo objectForKey: UIKeyboardFrameEndUserInfoKey] CGRectValue];
    CGRect rect     = [UnityGetGLView() convertRect: srcRect fromView: nil];
    
    if (rect.origin.y >= [UnityGetGLView() bounds].size.height)
        [self systemHideKeyboard];
    else
    {
        rect.origin.y = [UnityGetGLView() frame].size.height - rect.size.height; // iPhone X sometimes reports wrong y value for keyboard
        [self positionInput: rect x: rect.origin.x y: rect.origin.y];
    }
}

#endif

+ (void)Initialize
{
    NSAssert(s_Keyboard == nil, @"[iOSScreenKeyboardDelegate Initialize] called after creating keyboard");
    if (!s_Keyboard)
        s_Keyboard = [[iOSScreenKeyboardDelegate alloc] init];
}

+ (iOSScreenKeyboardDelegate*)Instance
{
    if (!s_Keyboard)
        s_Keyboard = [[iOSScreenKeyboardDelegate alloc] init];
    
    return s_Keyboard;
}

- (void)Show:(iOSScreenKeyboardShowParamsNative)param :(const char*)initialTextCStr :(const char*)placeholderTextCStr
{
    if (!editView.hidden)
    {
        [NSObject cancelPreviousPerformRequestsWithTarget: self];
        if (cachedKeyboardParam.multiline != param.multiline ||
            cachedKeyboardParam.secure != param.secure ||
            cachedKeyboardParam.keyboardType != param.keyboardType ||
            cachedKeyboardParam.autocorrectionType != param.autocorrectionType ||
            cachedKeyboardParam.appearance != param.appearance)
        {
            [self hideUIDelayed];
        }
    }
    cachedKeyboardParam = param;
    
    if (_active)
        [self Hide];
    
    initialText = initialTextCStr ? [[NSString alloc] initWithUTF8String: initialTextCStr] : @"";
    
    // TODO
    //_characterLimit = param.characterLimit;
    
    UITextAutocapitalizationType capitalization = UITextAutocapitalizationTypeSentences;
    if (param.keyboardType == UIKeyboardTypeURL || param.keyboardType == UIKeyboardTypeEmailAddress || param.keyboardType == UIKeyboardTypeWebSearch)
        capitalization = UITextAutocapitalizationTypeNone;
    
#if PLATFORM_IOS
    _multiline = param.multiline;
    if (_multiline)
    {
        textView.text = initialText;
        [self setTextInputTraits: textView withParam: param withCap: capitalization];
        
        UITextPosition* end = [textView endOfDocument];
        UITextRange* endTextRange = [textView textRangeFromPosition: end toPosition: end];
        [textView setSelectedTextRange: endTextRange];
    }
    else
    {
        textField.text = initialText;
        [self setTextInputTraits: textField withParam: param withCap: capitalization];
        textField.placeholder = placeholderTextCStr ? [NSString stringWithUTF8String: placeholderTextCStr] : @"";
        
        UITextPosition* end = [textField endOfDocument];
        UITextRange* endTextRange = [textField textRangeFromPosition: end toPosition: end];
        [textField setSelectedTextRange: endTextRange];
    }
    inputView = _multiline ? textView : textField;
    editView = _multiline ? textView : fieldToolbar;
    
#else // PLATFORM_TVOS
    textField.text = initialText;
    [self setTextInputTraits: textField withParam: param withCap: capitalization];
    textField.placeholder = [NSString stringWithUTF8String: param.placeholder];
    inputView = textField;
    editView = textField;
    
    UITextPosition* end = [textField endOfDocument];
    UITextRange* endTextRange = [textField textRangeFromPosition: end toPosition: end];
    [textField setSelectedTextRange: endTextRange];
#endif
    
    // TODO
    //[self shouldHideInput: _shouldHideInput];
    
    _status     = Visible;
    // TODO
    //UnityKeyboard_StatusChanged(_status);
    _active     = YES;
    
    [self showUI];
}

- (void)Hide
{
    [self hideUI];
}

#if PLATFORM_IOS
struct CreateToolbarResult
{
    UIToolbar*  toolbar;
    NSArray*    items;
};
- (CreateToolbarResult)createToolbarWithView:(UIView*)view
{
    UIToolbar* toolbar = [[UIToolbar alloc] initWithFrame: CGRectMake(0, 840, 320, kToolBarHeight)];
    UnitySetViewTouchProcessing(toolbar, touchesIgnored);
    toolbar.hidden = NO;
    
    UIBarButtonItem* inputItem  = view ? [[UIBarButtonItem alloc] initWithCustomView: view] : nil;
    UIBarButtonItem* doneItem   = [[UIBarButtonItem alloc] initWithBarButtonSystemItem: UIBarButtonSystemItemDone target: self action: @selector(textInputDone:)];
    UIBarButtonItem* cancelItem = [[UIBarButtonItem alloc] initWithBarButtonSystemItem: UIBarButtonSystemItemCancel target: self action: @selector(textInputCancel:)];
    
    NSArray* items = view ? @[inputItem, doneItem, cancelItem] : @[doneItem, cancelItem];
    toolbar.items = items;
    
    inputItem = nil;
    doneItem = nil;
    cancelItem = nil;
    
    CreateToolbarResult ret = {toolbar, items};
    return ret;
}

#endif

- (id)init
{
    NSAssert(s_Keyboard == nil, @"You can have only one instance of iOSScreenKeyboardDelegate");
    self = [super init];
    if (self)
    {
#if PLATFORM_IOS
        textView = [[UITextView alloc] initWithFrame: CGRectMake(0, 840, 480, 30)];
        textView.delegate = self;
        textView.font = [UIFont systemFontOfSize: 18.0];
        textView.hidden = YES;
#endif
        
        textField = [[UITextField alloc] initWithFrame: CGRectMake(0, 0, 120, 30)];
        textField.delegate = self;
        textField.borderStyle = UITextBorderStyleRoundedRect;
        textField.font = [UIFont systemFontOfSize: 20.0];
        textField.clearButtonMode = UITextFieldViewModeWhileEditing;
        
#if PLATFORM_IOS
        widthConstraint = [NSLayoutConstraint constraintWithItem: textField attribute: NSLayoutAttributeWidth relatedBy: NSLayoutRelationEqual toItem: nil attribute: NSLayoutAttributeNotAnAttribute multiplier: 1.0 constant: textField.frame.size.width];
        [textField addConstraint: widthConstraint];
#endif
        [textField addTarget: self action: @selector(textFieldDidChange:) forControlEvents: UIControlEventEditingChanged];
        
#define CREATE_TOOLBAR(t, i, v)                                 \
do {                                                            \
CreateToolbarResult res = [self createToolbarWithView:v];   \
t = res.toolbar;                                            \
i = res.items;                                              \
} while(0)
        
#if PLATFORM_IOS
        CREATE_TOOLBAR(viewToolbar, viewToolbarItems, nil);
        CREATE_TOOLBAR(fieldToolbar, fieldToolbarItems, textField);
#endif
        
#undef CREATE_TOOLBAR
        
#if PLATFORM_IOS
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(keyboardWillShow:) name: UIKeyboardWillShowNotification object: nil];
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(keyboardDidShow:) name: UIKeyboardDidShowNotification object: nil];
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(keyboardWillHide:) name: UIKeyboardWillHideNotification object: nil];
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(keyboardDidChangeFrame:) name: UIKeyboardDidChangeFrameNotification object: nil];
#endif
        
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(textInputDone:) name: UITextFieldTextDidEndEditingNotification object: nil];
    }
    
    return self;
}

- (void)setTextInputTraits:(id<UITextInputTraits>)traits
                 withParam:(iOSScreenKeyboardShowParamsNative)param
                   withCap:(UITextAutocapitalizationType)capitalization
{
    traits.keyboardType = param.keyboardType;
    traits.autocorrectionType = param.autocorrectionType;
    traits.secureTextEntry = param.secure;
    traits.keyboardAppearance = param.appearance;
    traits.autocapitalizationType = capitalization;
}

// we need to show/hide keyboard to react to orientation too, so extract we extract UI fiddling

- (void)showUI
{
    // if we unhide everything now the input will be shown smaller then needed quickly (and resized later)
    // so unhide only when keyboard is actually shown (we will update it when reacting to ios notifications)
    
    [NSObject cancelPreviousPerformRequestsWithTarget: self];
    if (!inputView.isFirstResponder)
    {
        editView.hidden = YES;
        
        [UnityGetGLView() addSubview: editView];
        [inputView becomeFirstResponder];
    }
}

- (void)hideUI
{
    [NSObject cancelPreviousPerformRequestsWithTarget: self];
    [self performSelector: @selector(hideUIDelayed) withObject: nil afterDelay: 0.05]; // to avoid unnecessary hiding
}

- (void)hideUIDelayed
{
    [inputView resignFirstResponder];
    
    [editView removeFromSuperview];
    editView.hidden = YES;
}

- (void)systemHideKeyboard
{
    // when we are rotating os will bombard us with keyboardWillHide: and keyboardDidChangeFrame:
    // ignore all of them (we do it here only to simplify code: we call systemHideKeyboard only from these notification handlers)
    if (_rotating)
        return;
    
    _active = editView.isFirstResponder;
    editView.hidden = YES;
    
    _area = CGRectMake(0, 0, 0, 0);
}

- (void)updateInputHidden
{
    if (_shouldHideInputChanged)
    {
        [self shouldHideInput: _shouldHideInput];
        _shouldHideInputChanged = false;
    }
    
    textField.returnKeyType = _inputHidden ? UIReturnKeyDone : UIReturnKeyDefault;
    
    editView.hidden     = _inputHidden ? YES : NO;
    inputView.hidden    = _inputHidden ? YES : NO;
}

#if PLATFORM_IOS
- (void)positionInput:(CGRect)kbRect x:(float)x y:(float)y
{
    float safeAreaInsetLeft = 0;
    float safeAreaInsetRight = 0;
    
#if UNITY_HAS_IOSSDK_11_0
    if (@available(iOS 11.0, *))
    {
        safeAreaInsetLeft = [UnityGetGLView() safeAreaInsets].left;
        safeAreaInsetRight = [UnityGetGLView() safeAreaInsets].right;
    }
#endif
    
    if (_multiline)
    {
        // use smaller area for iphones and bigger one for ipads
        int height = UnityDeviceDPI() > 300 ? 75 : 100;
        
        editView.frame  = CGRectMake(safeAreaInsetLeft, y - height, kbRect.size.width - safeAreaInsetLeft - safeAreaInsetRight, height);
    }
    else
    {
        editView.frame  = CGRectMake(0, y - kToolBarHeight, kbRect.size.width, kToolBarHeight);
        
        // old constraint must be removed, changing value while constraint is active causes conflict when changing inputView.frame
        [inputView removeConstraint: widthConstraint];
        
        inputView.frame = CGRectMake(inputView.frame.origin.x,
                                     inputView.frame.origin.y,
                                     kbRect.size.width - safeAreaInsetLeft - safeAreaInsetRight - kSystemButtonsSpace,
                                     inputView.frame.size.height);
        
        // required to avoid auto-resizing on iOS 11 in case if input text is too long
        widthConstraint.constant = inputView.frame.size.width;
        [inputView addConstraint: widthConstraint];
    }
    
    _area = CGRectMake(x, y, kbRect.size.width, kbRect.size.height);
    [self updateInputHidden];
}

#endif

- (CGRect)queryArea
{
    return editView.hidden ? _area : CGRectUnion(_area, editView.frame);
}

- (NSRange)querySelection
{
    UIView<UITextInput>* textInput;
    
#if PLATFORM_TVOS
    textInput = textField;
#else
    textInput = _multiline ? textView : textField;
#endif
    
    UITextPosition* beginning = textInput.beginningOfDocument;
    
    UITextRange* selectedRange = textInput.selectedTextRange;
    UITextPosition* selectionStart = selectedRange.start;
    UITextPosition* selectionEnd = selectedRange.end;
    
    const NSInteger location = [textInput offsetFromPosition: beginning toPosition: selectionStart];
    const NSInteger length = [textInput offsetFromPosition: selectionStart toPosition: selectionEnd];
    
    return NSMakeRange(location, length);
}

- (void)assignSelection:(NSRange)range
{
    UIView<UITextInput>* textInput;
    
#if PLATFORM_TVOS
    textInput = textField;
#else
    textInput = _multiline ? textView : textField;
#endif
    
    UITextPosition* begin = [textInput beginningOfDocument];
    UITextPosition* caret = [textInput positionFromPosition: begin offset: range.location];
    UITextPosition* select = [textInput positionFromPosition: caret offset: range.length];
    UITextRange* textRange = [textInput textRangeFromPosition: caret toPosition: select];
    
    [textInput setSelectedTextRange: textRange];
}

+ (void)StartReorientation
{
    if (s_Keyboard && s_Keyboard.active)
        s_Keyboard->_rotating = YES;
}

+ (void)FinishReorientation
{
    if (s_Keyboard)
        s_Keyboard->_rotating = NO;
}

- (NSString*)getText
{
    if (_status == Canceled)
        return initialText;
    else
    {
#if PLATFORM_TVOS
        return [textField text];
#else
        return _multiline ? [textView text] : [textField text];
#endif
    }
}

- (void)setText:(NSString*)newText
{
#if PLATFORM_IOS
    if (_multiline)
        textView.text = newText;
    else
        textField.text = newText;
#else
    textField.text = newText;
#endif
}

- (void)shouldHideInput:(BOOL)hide
{
    if (hide)
    {
        switch (keyboardType)
        {
            case UIKeyboardTypeDefault:                 hide = YES; break;
            case UIKeyboardTypeASCIICapable:            hide = YES; break;
            case UIKeyboardTypeNumbersAndPunctuation:   hide = YES; break;
            case UIKeyboardTypeURL:                     hide = YES; break;
            case UIKeyboardTypeNumberPad:               hide = NO;  break;
            case UIKeyboardTypePhonePad:                hide = NO;  break;
            case UIKeyboardTypeNamePhonePad:            hide = NO;  break;
            case UIKeyboardTypeEmailAddress:            hide = YES; break;
            case UIKeyboardTypeTwitter:                 hide = YES; break;
            case UIKeyboardTypeWebSearch:               hide = YES; break;
            default:                                    hide = NO;  break;
        }
    }
    
    _inputHidden = hide;
}

#if FILTER_EMOJIS_IOS_KEYBOARD

static bool StringContainsEmoji(NSString *string);
- (BOOL)textField:(UITextField*)textField shouldChangeCharactersInRange:(NSRange)range replacementString:(NSString*)string_
{
    if (range.length + range.location > textField.text.length)
        return NO;
    
    return [self currentText: textField.text shouldChangeInRange: range replacementText: string_] && !StringContainsEmoji(string_);
}

- (BOOL)textView:(UITextView*)textView shouldChangeTextInRange:(NSRange)range replacementText:(NSString*)text_
{
    if (range.length + range.location > textView.text.length)
        return NO;
    
    return [self currentText: textView.text shouldChangeInRange: range replacementText: text_] && !StringContainsEmoji(text_);
}

#else

- (BOOL)textField:(UITextField *)textField shouldChangeCharactersInRange:(NSRange)range replacementString:(NSString*)string_
{
    if (range.length + range.location > textField.text.length)
        return NO;
    
    return [self currentText: textField.text shouldChangeInRange: range replacementText: string_];
}

- (BOOL)textView:(UITextView *)textView shouldChangeTextInRange:(NSRange)range replacementText:(NSString*)text_
{
    if (range.length + range.location > textView.text.length)
        return NO;
    
    return [self currentText: textView.text shouldChangeInRange: range replacementText: text_];
}

#endif // FILTER_EMOJIS_IOS_KEYBOARD

- (BOOL)currentText:(NSString*)currentText shouldChangeInRange:(NSRange)range  replacementText:(NSString*)text_
{
    NSUInteger newLength = currentText.length + (text_.length - range.length);
    if (newLength > _characterLimit && _characterLimit != 0 && newLength >= currentText.length)
    {
        NSString* newReplacementText = @"";
        if ((currentText.length - range.length) < _characterLimit)
            newReplacementText = [text_ substringWithRange: NSMakeRange(0, _characterLimit - (currentText.length - range.length))];
        
        NSString* newText = [currentText stringByReplacingCharactersInRange: range withString: newReplacementText];
        
#if PLATFORM_IOS
        if (_multiline)
            [textView setText: newText];
        else
            [textField setText: newText];
#else
        [textField setText: newText];
#endif
        
        return NO;
    }
    else
    {
        return YES;
    }
}

@end

