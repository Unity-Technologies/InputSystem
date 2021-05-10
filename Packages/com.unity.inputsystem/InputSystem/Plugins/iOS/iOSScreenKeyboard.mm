#include "iOSScreenKeyboardBridge.h"
#include "DisplayManager.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>

#define TOKENPASTE(x, y) x ## y
#define UNIQUE(x, y) TOKENPASTE(x, y)
#define KEYBOARD_LOG(...) LoggingScope UNIQUE(loggingScope, __LINE__)([NSString stringWithFormat: __VA_ARGS__])

class LoggingScope
{
    static int s_Indentation;

    static NSString* GetIndentation()
    {
        return [@"" stringByPaddingToLength: (s_Indentation * 2) withString: @" " startingAtIndex: 0];
    }

public:
    static bool s_Enabled;

    LoggingScope(NSString* message)
    {
        if (s_Enabled)
            NSLog(@"ScreenKeyboard - %@%@", GetIndentation(), message);
        s_Indentation++;
    }

    ~LoggingScope()
    {
        s_Indentation--;
    }
};

int LoggingScope::s_Indentation = 0;
bool LoggingScope::s_Enabled = false;

static iOSScreenKeyboardBridge* s_Keyboard = nil;
static const unsigned kToolBarHeight = 40;
static const unsigned kSystemButtonsSpace = 2 * 60 + 3 * 18; // empirical value, there is no way to know the exact widths of the system bar buttons

@interface iOSScreenKeyboardBridge ()
- (void)textDidChangeImpl:(NSString*)text;
- (void)selectionChangeImpl:(NSRange)range;
+ (NSRange)getSelectionFromTextInput:(UIView<UITextInput>*)textInput;
+ (BOOL)simulateTextSelection;
@end

@implementation iOSScreenKeyboardBridge
{
    // UI handling
    // in case of single line we use UITextField inside UIToolbar
    // in case of multi-line input we use UITextView with UIToolbar as accessory view
    // toolbar buttons are kept around to prevent releasing them
    // tvOS does not support multiline input thus only UITextField option is implemented
#if PLATFORM_IOS
    UITextView*         m_TextView;
    UIToolbar*          m_ViewToolbar;
    NSArray*            m_ViewToolbarItems;
    NSLayoutConstraint* m_WidthConstraint;
    UIToolbar*          m_FieldToolbar;
    NSArray*            m_FieldToolbarItems;
#endif

    UITextField*        m_TextField;

    // inputView is view used for actual input (it will be responder): UITextField [single-line] or UITextView [multi-line]
    // editView is the "root" view for keyboard: UIToolbar [single-line] or UITextView [multi-line]
    UIView*             m_InputView;
    UIView*             m_EditView;

    iOSScreenKeyboardShowParamsNative m_ShowParams;

    CGRect                      m_Area;
    NSString*                   m_InitialText;
    BOOL                        m_Active;
    iOSScreenKeyboardState      m_State;
    NSRange                     m_LastSelection;
}

@synthesize area;

- (BOOL)textFieldShouldReturn:(UITextField*)textFieldObj
{
    [self textInputDone: nil];
    return YES;
}

- (void)textInputDone:(id)sender
{
    KEYBOARD_LOG(@"textInputDone");
    if (m_State != StateVisible)
        return;

    [self hide: StateDone];
}

- (void)textInputCancel:(id)sender
{
    KEYBOARD_LOG(@"textInputCancel");
    [self hide: StateCanceled];
}

- (void)textInputLostFocus
{
    KEYBOARD_LOG(@"textInputLostFocus");
    if (m_State != StateVisible)
        return;

    [self hide: StateLostFocus];
}

- (void)textDidChangeImpl:(NSString*)text
{
    KEYBOARD_LOG(@"textDidChangeImpl %@", text);
    if (m_ShowParams.callbacks.textChangedCallback)
        m_ShowParams.callbacks.textChangedCallback([text UTF8String]);
    else
        NSLog(@"textViewDidChange: Missing callback");
}

- (void)selectionChangeImpl:(NSRange)range
{
    KEYBOARD_LOG(@"selectionChangeImpl %u, %u", (unsigned int)range.location, (unsigned int)range.length);

    if (NSEqualRanges(m_LastSelection, range))
    {
        KEYBOARD_LOG(@"selection hasn't changed, will not invoke callback");
        return;
    }
    m_LastSelection = range;
    if (m_ShowParams.callbacks.selectionChanagedCallback)
    {
        m_ShowParams.callbacks.selectionChanagedCallback((int)range.location, (int)range.length);
    }
    else
        NSLog(@"selectionChanagedCallback: Missing callback");
}

- (void)textViewDidChange:(UITextView *)textView
{
    [self textDidChangeImpl: [textView text]];
}

- (void)textViewDidChangeSelection:(UITextView *)textView
{
    [self selectionChangeImpl: [iOSScreenKeyboardBridge getSelectionFromTextInput: textView]];
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary *)change context:(void *)context
{
    if ([keyPath isEqualToString: @"selectedTextRange"] && m_TextField == object)
        [self selectionChangeImpl: [iOSScreenKeyboardBridge getSelectionFromTextInput: m_TextField]];
}

// Note: This callback is available only from iOS 14.0 or higher
- (void)textFieldDidChangeSelection:(UITextField *)textField
{
    [self selectionChangeImpl: [iOSScreenKeyboardBridge getSelectionFromTextInput: textField]];
}

- (void)textFieldDidChange:(UITextField*)textField
{
    // Note: Regarding selections
    //       When text changes in text view, textViewDidChangeSelection is triggered
    //       When text changes in text field, observeValueForKeyPath with selectedTextRange doesn't get triggered.
    //       When selection changes in text view, textViewDidChangeSelection is triggered
    //       When selection changes in text field, observeValueForKeyPath with selectedTextRange is triggered

    // Workaround issue with selection not being triggered
    if ([iOSScreenKeyboardBridge simulateTextSelection])
    {
        [self selectionChangeImpl: [iOSScreenKeyboardBridge getSelectionFromTextInput: m_TextField]];
    }
    [self textDidChangeImpl: textField.text];
}

- (BOOL)textViewShouldBeginEditing:(UITextView*)view
{
#if !PLATFORM_TVOS
    view.inputAccessoryView = m_ViewToolbar;
#endif
    return YES;
}

#if PLATFORM_IOS

- (void)keyboardWillShow:(NSNotification *)notification
{
    if (notification.userInfo == nil || m_InputView == nil)
        return;

    CGRect srcRect  = [[notification.userInfo objectForKey: UIKeyboardFrameEndUserInfoKey] CGRectValue];
    CGRect rect     = [UnityGetGLView() convertRect: srcRect fromView: nil];
    rect.origin.y = [UnityGetGLView() frame].size.height - rect.size.height; // iPhone X sometimes reports wrong y value for keyboard

    [self positionInput: rect x: rect.origin.x y: rect.origin.y];
}

- (void)keyboardDidShow:(NSNotification*)notification
{
    KEYBOARD_LOG(@"keyboardDidShow");
    m_Active = YES;
}

- (void)keyboardWillHide:(NSNotification*)notification
{
    KEYBOARD_LOG(@"keyboardWillHide");
    [self systemHideKeyboard];
}

- (void)keyboardDidChangeFrame:(NSNotification*)notification
{
    KEYBOARD_LOG(@"keyboardDidChangeFrame");
    m_Active = YES;

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

+ (iOSScreenKeyboardBridge*)getInstanceOrCreate
{
    if (!s_Keyboard)
    {
        KEYBOARD_LOG(@"creating keyboard instance");
        s_Keyboard = [[iOSScreenKeyboardBridge alloc] init];
    }

    return s_Keyboard;
}

+ (iOSScreenKeyboardBridge*)getInstance
{
    return s_Keyboard;
}

+ (void)cleanup
{
    KEYBOARD_LOG(@"cleanup");
    if (s_Keyboard != nil)
        s_Keyboard = nil;
}

+ (bool)getLogging
{
    return LoggingScope::s_Enabled;
}

+ (void)setLogging:(bool)enabled
{
    LoggingScope::s_Enabled = enabled;
}

- (void)show:(iOSScreenKeyboardShowParamsNative)param withInitialTextCStr:(const char*)initialTextCStr withPlaceholderTextCStr:(const char*)placeholderTextCStr
{
    KEYBOARD_LOG(@"keyboard show");
    if (!m_EditView.hidden)
    {
        [NSObject cancelPreviousPerformRequestsWithTarget: self];
        if (m_ShowParams.multiline != param.multiline ||
            m_ShowParams.secure != param.secure ||
            m_ShowParams.keyboardType != param.keyboardType ||
            m_ShowParams.autocorrectionType != param.autocorrectionType ||
            m_ShowParams.appearance != param.appearance ||
            m_ShowParams.inputFieldHidden != param.inputFieldHidden)
        {
            [self hideUIDelayed];
        }
    }
    m_ShowParams = param;

    if (m_Active)
        [self hide: StateDone];

    m_InitialText = initialTextCStr ? [NSString stringWithUTF8String: initialTextCStr] : @"";

    UITextAutocapitalizationType capitalization = UITextAutocapitalizationTypeSentences;
    if (param.keyboardType == UIKeyboardTypeURL || param.keyboardType == UIKeyboardTypeEmailAddress || param.keyboardType == UIKeyboardTypeWebSearch)
        capitalization = UITextAutocapitalizationTypeNone;

#if PLATFORM_IOS
    if (m_ShowParams.multiline)
    {
        m_TextView.text = m_InitialText;
        [self setTextInputTraits: m_TextView withParam: param withCap: capitalization];

        UITextPosition* end = [m_TextView endOfDocument];
        UITextRange* endTextRange = [m_TextView textRangeFromPosition: end toPosition: end];
        [m_TextView setSelectedTextRange: endTextRange];
    }
    else
#endif
    {
        m_TextField.text = m_InitialText;
        [self setTextInputTraits: m_TextField withParam: param withCap: capitalization];
        m_TextField.placeholder = placeholderTextCStr ? [NSString stringWithUTF8String: placeholderTextCStr] : @"";

        UITextPosition* end = [m_TextField endOfDocument];
        UITextRange* endTextRange = [m_TextField textRangeFromPosition: end toPosition: end];
        [m_TextField setSelectedTextRange: endTextRange];
    }

#if PLATFORM_IOS
    m_InputView = m_ShowParams.multiline ? m_TextView : m_TextField;
    m_EditView = m_ShowParams.multiline ? m_TextView : m_FieldToolbar;
#else
    m_InputView = m_TextField;
    m_EditView = m_TextField;
#endif

    m_TextField.returnKeyType = m_ShowParams.inputFieldHidden ? UIReturnKeyDone : UIReturnKeyDefault;

    m_LastSelection.length = 0;
    m_LastSelection.location = m_InitialText.length;

    m_State     = StateVisible;
    m_ShowParams.callbacks.stateChangedCallback(m_State);
    m_Active     = YES;

    // if we unhide everything now the input will be shown smaller then needed quickly (and resized later)
    // so unhide only when keyboard is actually shown (we will update it when reacting to ios notifications)

    [NSObject cancelPreviousPerformRequestsWithTarget: self];
    if (!m_InputView.isFirstResponder)
    {
        m_EditView.hidden = YES;

        [UnityGetGLView() addSubview: m_EditView];
        [m_InputView becomeFirstResponder];
    }
}

- (void)hide:(iOSScreenKeyboardState)hideState
{
    KEYBOARD_LOG(@"hide");
    m_State     = hideState;

    [NSObject cancelPreviousPerformRequestsWithTarget: self];
    [self performSelector: @selector(hideUIDelayedWithCallback) withObject: nil afterDelay: 0.05]; // to avoid unnecessary hiding
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
        m_TextView = [[UITextView alloc] initWithFrame: CGRectMake(0, 840, 480, 30)];
        m_TextView.delegate = self;
        m_TextView.font = [UIFont systemFontOfSize: 18.0];
        m_TextView.hidden = YES;
#endif

        m_TextField = [[UITextField alloc] initWithFrame: CGRectMake(0, 0, 120, 30)];
        m_TextField.delegate = self;
        m_TextField.borderStyle = UITextBorderStyleRoundedRect;
        m_TextField.font = [UIFont systemFontOfSize: 20.0];
        m_TextField.clearButtonMode = UITextFieldViewModeWhileEditing;

#if PLATFORM_IOS
        m_WidthConstraint = [NSLayoutConstraint constraintWithItem: m_TextField attribute: NSLayoutAttributeWidth relatedBy: NSLayoutRelationEqual toItem: nil attribute: NSLayoutAttributeNotAnAttribute multiplier: 1.0 constant: m_TextField.frame.size.width];
        [m_TextField addConstraint: m_WidthConstraint];
#endif
        [m_TextField addTarget: self action: @selector(textFieldDidChange:) forControlEvents: UIControlEventEditingChanged];

        // Workaround missing textFieldDidChangeSelection callback on earlier versions
        if ([iOSScreenKeyboardBridge simulateTextSelection])
        {
            [m_TextField addObserver: self forKeyPath: @"selectedTextRange" options: NSKeyValueObservingOptionNew | NSKeyValueObservingOptionOld  context: nil];
        }
#define CREATE_TOOLBAR(t, i, v)                                 \
do {                                                            \
CreateToolbarResult res = [self createToolbarWithView:v];   \
t = res.toolbar;                                            \
i = res.items;                                              \
} while(0)

#if PLATFORM_IOS
        CREATE_TOOLBAR(m_ViewToolbar, m_ViewToolbarItems, nil);
        CREATE_TOOLBAR(m_FieldToolbar, m_FieldToolbarItems, m_TextField);
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

- (void)dealloc
{
    KEYBOARD_LOG(@"dealloc");
    if (m_State == StateVisible)
    {
        m_State = StateDone;
        [self hideUIDelayedWithCallback];
    }
#if PLATFORM_IOS
    [[NSNotificationCenter defaultCenter] removeObserver: self name: UIKeyboardWillShowNotification object: nil];
    [[NSNotificationCenter defaultCenter] removeObserver: self name: UIKeyboardDidShowNotification object: nil];
    [[NSNotificationCenter defaultCenter] removeObserver: self name: UIKeyboardWillHideNotification object: nil];
    [[NSNotificationCenter defaultCenter] removeObserver: self name: UIKeyboardDidChangeFrameNotification object: nil];
#endif
    if ([iOSScreenKeyboardBridge simulateTextSelection])
    {
        [m_TextField removeObserver: self forKeyPath: @"selectedTextRange"];
    }

    [[NSNotificationCenter defaultCenter] removeObserver: self name: UITextFieldTextDidEndEditingNotification object: nil];
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

- (void)hideUIDelayed
{
    KEYBOARD_LOG(@"hideUIDelayed");
    [m_InputView resignFirstResponder];

    [m_EditView removeFromSuperview];
    m_EditView.hidden = YES;
}

- (void)hideUIDelayedWithCallback
{
    KEYBOARD_LOG(@"hideUIDelayedWithCallback");
    [self hideUIDelayed];
    m_ShowParams.callbacks.stateChangedCallback(m_State);
}

- (void)systemHideKeyboard
{
    KEYBOARD_LOG(@"systemHideKeyboard");

    m_Active = m_EditView.isFirstResponder;
    m_EditView.hidden = YES;

    m_Area = CGRectMake(0, 0, 0, 0);
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

    if (m_ShowParams.multiline)
    {
        // use smaller area for iphones and bigger one for ipads
        int height = UnityDeviceDPI() > 300 ? 75 : 100;

        m_EditView.frame  = CGRectMake(safeAreaInsetLeft, y - height, kbRect.size.width - safeAreaInsetLeft - safeAreaInsetRight, height);
    }
    else
    {
        m_EditView.frame  = CGRectMake(0, y - kToolBarHeight, kbRect.size.width, kToolBarHeight);

        // old constraint must be removed, changing value while constraint is active causes conflict when changing m_InputView.frame
        [m_InputView removeConstraint: m_WidthConstraint];

        m_InputView.frame = CGRectMake(m_InputView.frame.origin.x,
            m_InputView.frame.origin.y,
            kbRect.size.width - safeAreaInsetLeft - safeAreaInsetRight - kSystemButtonsSpace,
            m_InputView.frame.size.height);

        // required to avoid auto-resizing on iOS 11 in case if input text is too long
        m_WidthConstraint.constant = m_InputView.frame.size.width;
        [m_InputView addConstraint: m_WidthConstraint];
    }

    m_Area = CGRectMake(x, y, kbRect.size.width, kbRect.size.height);

    m_TextField.returnKeyType = m_ShowParams.inputFieldHidden ? UIReturnKeyDone : UIReturnKeyDefault;

    m_EditView.hidden     = m_ShowParams.inputFieldHidden;
    m_InputView.hidden    = m_ShowParams.inputFieldHidden;
}

#endif

- (CGRect)queryArea
{
    return m_EditView.hidden ? m_Area : CGRectUnion(m_Area, m_EditView.frame);
}

- (NSString*)getText
{
#if PLATFORM_TVOS
    return [m_TextField text];
#else
    return m_ShowParams.multiline ? [m_TextView text] : [m_TextField text];
#endif
}

- (void)setText:(NSString*)newText
{
    KEYBOARD_LOG(@"setText %@", newText);
    NSString* originalText = self.getText;
    if ([originalText isEqualToString: newText])
        return;
    UIView<UITextInput>* textInput;
#if PLATFORM_IOS
    if (m_ShowParams.multiline)
    {
        m_TextView.text = newText;
        textInput = m_TextView;
    }
    else
#endif
    {
        m_TextField.text = newText;
        textInput = m_TextField;

        if ([iOSScreenKeyboardBridge simulateTextSelection])
        {
            [self selectionChangeImpl: [iOSScreenKeyboardBridge getSelectionFromTextInput: textInput]];
        }
    }

    // Setting text doesn't trigger callbacks, do it manually
    [self textDidChangeImpl: newText];
}

+ (NSRange)getSelectionFromTextInput:(UIView<UITextInput>*)textInput
{
    UITextPosition* beginning = textInput.beginningOfDocument;

    UITextRange* selectedRange = textInput.selectedTextRange;
    UITextPosition* selectionStart = selectedRange.start;
    UITextPosition* selectionEnd = selectedRange.end;

    const NSInteger location = [textInput offsetFromPosition: beginning toPosition: selectionStart];
    const NSInteger length = [textInput offsetFromPosition: selectionStart toPosition: selectionEnd];

    return NSMakeRange(location, length);
}

+ (BOOL)simulateTextSelection
{
    if (@available(iOS 13.0, tvOS 13.0, *))
    {
        return NO;
    }
    else
    {
        return YES;
    }
}

- (NSRange)getSelection
{
    UIView<UITextInput>* textInput;

#if PLATFORM_TVOS
    textInput = m_TextField;
#else
    textInput = m_ShowParams.multiline ? m_TextView : m_TextField;
#endif

    return [iOSScreenKeyboardBridge getSelectionFromTextInput: textInput];
}

- (void)setSelection:(NSRange)newSelection
{
    KEYBOARD_LOG(@"setSelection %@", NSStringFromRange(newSelection));

    if (NSEqualRanges(self.getSelection, newSelection))
        return;
    UIView<UITextInput>* textInput;

#if PLATFORM_TVOS
    textInput = m_TextField;
#else
    textInput = m_ShowParams.multiline ? m_TextView : m_TextField;
#endif

    NSString* text = [self getText];
    // Check for out of bounds
    if (newSelection.location > text.length ||
        newSelection.location + newSelection.length > text.length)
        return;

    UITextPosition* begin = [textInput beginningOfDocument];
    UITextPosition* caret = [textInput positionFromPosition: begin offset: newSelection.location];
    UITextPosition* select = [textInput positionFromPosition: caret offset: newSelection.length];
    UITextRange* textRange = [textInput textRangeFromPosition: caret toPosition: select];

    [textInput setSelectedTextRange: textRange];
}

@end
