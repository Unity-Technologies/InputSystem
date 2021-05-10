package com.unity.inputsystem;


import android.app.Dialog;
import android.content.Context;
import android.content.DialogInterface.*;
import android.content.DialogInterface;
import android.graphics.Rect;
import android.graphics.drawable.ColorDrawable;
import android.text.Editable;
import android.text.TextWatcher;
import android.text.Selection;
import android.text.InputFilter;
import android.text.method.TextKeyListener;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.View.OnClickListener;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputMethodManager;
import android.view.View.MeasureSpec;
import android.view.inputmethod.InputMethodSubtype;
import android.widget.Button;
import android.widget.EditText;
import android.widget.RelativeLayout;
import android.widget.TextView;
import android.util.*;
import com.unity3d.player.*;

import java.text.MessageFormat;
import java.util.Locale;

public class AndroidScreenKeyboard extends Dialog implements OnClickListener, TextWatcher, OnDismissListener, OnShowListener
{
    interface IScreenKeyboardCallbacks
    {
        void OnTextChanged(String text);
        void OnStateChanged(int state);
        void OnSelectionChanged(int start, int length);
    }

    private enum ScreenKeyboardState
    {
        Done(0),
        Visible(1),
        Canceled(2);

        private final int value;

        ScreenKeyboardState(int value) { this.value = value; }
    }

    private enum ScreenKeyboardType
    {
        Default(0),
        ASCIICapable(1),
        NumbersAndPunctuation(2),
        URL(3),
        NumberPad(4),
        PhonePad(5),
        NamePhonePad(6),
        EmailAddress(7),
        Social(8),
        Search(9);

        private final int value;

        ScreenKeyboardType(int value) { this.value = value; }
    }


    private static final class id
    {
        private static final int okButton    = 0x3f050002;
        private static final int txtInput    = 0x3f050001;
    }

    private Context m_Context = null;
    private IScreenKeyboardCallbacks m_Callbacks;
    private ScreenKeyboardState m_DismissReturnValue;

    private long m_LastSelection;
    private boolean m_InputFieldHidden;
    private boolean m_LoggingEnabled;

    private EditText m_EditText;

    public AndroidScreenKeyboard ()
    {
        super (UnityPlayer.currentActivity);
        m_LoggingEnabled = false;
        m_Context = UnityPlayer.currentActivity;
        m_DismissReturnValue = ScreenKeyboardState.Done;
        Window window = getWindow();
        window.requestFeature(Window.FEATURE_NO_TITLE);
        // Set transparent background
        // Because in Lollipop otherwise we get black frame around the dialog
        window.setBackgroundDrawable(new ColorDrawable(android.graphics.Color.TRANSPARENT));
        window.setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        // Don't dim the view behind the dialog
        window.clearFlags (WindowManager.LayoutParams.FLAG_DIM_BEHIND);
        window.addFlags(WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED);

        WindowManager.LayoutParams param = window.getAttributes();
        param.gravity = Gravity.BOTTOM;
        param.x = 0;
        param.y = 0;
        window.setAttributes(param);
    }

    private void debugLog(String format, Object... args)
    {
        if (!m_LoggingEnabled)
            return;
        Log.v("Unity", "ScreenKeyboard - " + MessageFormat.format(format, args));
    }

    public void show(
            IScreenKeyboardCallbacks callbacks,
            int keyboardType,
            String initialText,
            String placeholderText,
            boolean correction,
            boolean multiline,
            boolean secure,
            boolean alert,
            boolean inputFieldHidden)
    {
        m_Callbacks = callbacks;
        m_DismissReturnValue = ScreenKeyboardState.Done;
        m_InputFieldHidden = inputFieldHidden;

        View contentView = createSoftInputView();
        setContentView (contentView);
        m_EditText = (EditText) findViewById (id.txtInput);
        m_EditText.setImeOptions (EditorInfo.IME_ACTION_DONE | EditorInfo.IME_FLAG_NO_FULLSCREEN);
        m_EditText.setText (initialText);
        m_EditText.setHint (placeholderText);
        m_EditText.setHintTextColor (0x61000000);
        m_EditText.setInputType (convertInputType (ScreenKeyboardType.values()[keyboardType], correction, multiline, secure));
        m_EditText.addTextChangedListener (this);
        m_LastSelection = convertSelectionToLong(m_EditText.getText().length(), 0);
        m_EditText.setSelection(m_EditText.getText().length());
        m_EditText.setClickable (true);
        m_EditText.setOnFocusChangeListener (new View.OnFocusChangeListener ()
        {
            @Override
            public void onFocusChange (View v, boolean hasFocus)
            {
                debugLog("onFocusChange {0}", hasFocus);
                if (hasFocus)
                {
                    int vis = WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_VISIBLE;
                    getWindow ().setSoftInputMode (vis);
                }
            }
        });


        Button okButton = (Button) findViewById (id.okButton);
        okButton.setOnClickListener (this);

        if (m_InputFieldHidden)
        {
            m_EditText.setBackgroundColor(0);
            m_EditText.setTextColor(0);
            m_EditText.setCursorVisible(false);
            okButton.setClickable(false);
            okButton.setTextColor(0);
            contentView.setBackgroundColor(0);
        }

        setOnDismissListener(this);
        setOnShowListener(this);

        show();
    }

    public void afterTextChanged (Editable s)
    {
        debugLog("afterTextChanged: {0} Start {1} End {2}", m_EditText.getText(), m_EditText.getSelectionStart(), m_EditText.getSelectionEnd());
        m_Callbacks.OnTextChanged(s.toString());
    }

    public void beforeTextChanged (CharSequence s, int start, int count, int after)
    {
        debugLog("beforeTextChanged {0} start {1}, count {2}, after {3}", s.toString(), start, count, after);
    }

    public void onTextChanged (CharSequence s, int start, int before, int count)
    {
        debugLog("onTextChanged {0} start {1}, before {2}, count {3}", s.toString(), start, before, count);
    }

    private int convertInputType (ScreenKeyboardType keyboardType, boolean correction, boolean multiline, boolean secure)
    {
        int baseType = (correction ? EditorInfo.TYPE_TEXT_FLAG_AUTO_CORRECT : EditorInfo.TYPE_TEXT_FLAG_NO_SUGGESTIONS)
                       | (multiline ? EditorInfo.TYPE_TEXT_FLAG_MULTI_LINE : 0)
                       | (secure ? EditorInfo.TYPE_TEXT_VARIATION_PASSWORD : 0);

        switch (keyboardType)
        {
            case Default:
            default:
                return baseType | EditorInfo.TYPE_CLASS_TEXT;
            case ASCIICapable:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_FLAG_CAP_SENTENCES;
            case NumbersAndPunctuation:
                return EditorInfo.TYPE_CLASS_NUMBER | EditorInfo.TYPE_NUMBER_FLAG_DECIMAL | EditorInfo.TYPE_NUMBER_FLAG_SIGNED;
            case URL:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_VARIATION_URI;
            case NumberPad:
                return EditorInfo.TYPE_CLASS_NUMBER;
            case PhonePad:
                return baseType | EditorInfo.TYPE_CLASS_PHONE;
            case NamePhonePad:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_FLAG_CAP_WORDS | EditorInfo.TYPE_TEXT_VARIATION_PERSON_NAME;
            case EmailAddress:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_VARIATION_EMAIL_ADDRESS;
            case Social:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_FLAG_CAP_SENTENCES | EditorInfo.TYPE_TEXT_VARIATION_EMAIL_ADDRESS;
            case Search:
                return baseType | EditorInfo.TYPE_CLASS_TEXT | EditorInfo.TYPE_TEXT_VARIATION_URI;
        }
    }

    @Override public void onClick (View v)
    {
        dismiss();
    }

    @Override public void onShow(DialogInterface dialog)
    {
        debugLog("onShow");
        m_Callbacks.OnStateChanged(ScreenKeyboardState.Visible.value);
    }

    @Override public void onDismiss(DialogInterface dialog)
    {
        debugLog("onDismiss " + m_DismissReturnValue);
        m_Callbacks.OnStateChanged(m_DismissReturnValue.value);
    }

    protected View createSoftInputView ()
    {
        final int matchParent = ViewGroup.LayoutParams.MATCH_PARENT;
        final int wrapContent = ViewGroup.LayoutParams.WRAP_CONTENT;
        RelativeLayout rl = new RelativeLayout (m_Context);
        rl.setLayoutParams (new ViewGroup.LayoutParams (matchParent, matchParent));
        rl.setBackgroundColor(0xFFFFFFFF);
        RelativeLayout.LayoutParams lp = null;

        EditText et = new EditText (m_Context) {
            public boolean onKeyPreIme(int keyCode, KeyEvent event) {
                debugLog("onKeyPreIme: {0}", keyCode);
                // intercept BACK to make sure the dialog is close, and SEARCH to make sure it's ignored.
                if (keyCode == KeyEvent.KEYCODE_BACK)
                {
                    debugLog("    Back button");
                    m_DismissReturnValue = ScreenKeyboardState.Canceled;
                    dismiss();
                    return true;
                }
                if (keyCode == KeyEvent.KEYCODE_SEARCH)
                {
                    debugLog("    Search button");
                    return true;
                }
                return super.onKeyPreIme(keyCode, event);
            }

            public void onWindowFocusChanged(boolean hasWindowFocus) {
                super.onWindowFocusChanged(hasWindowFocus);
                debugLog("onWindowFocusChanged {0}", hasWindowFocus);
                // for some reason this code can NOT be in the OnFocusChangeListener; go figure..
                if (hasWindowFocus) {
                    InputMethodManager imm = (InputMethodManager) m_Context.getSystemService(Context.INPUT_METHOD_SERVICE);
                    imm.showSoftInput(this, 0);
                }
            }

            @Override
            protected void onSelectionChanged(int start, int end)
            {
                debugLog("onSelectionChanged {0} {1}", start, end - start);

                long currentSelection = convertSelectionToLong(start, end);
                if (m_LastSelection == currentSelection)
                {
                    debugLog("   didn't change from last time, ignoring");
                    return;
                }
                m_LastSelection = currentSelection;

                m_Callbacks.OnSelectionChanged(start, end - start);
            }
        };

        lp = new RelativeLayout.LayoutParams (matchParent, wrapContent);
        lp.addRule (RelativeLayout.CENTER_VERTICAL);
        lp.addRule (RelativeLayout.LEFT_OF, id.okButton);
        et.setLayoutParams (lp);
        et.setId (id.txtInput);
        rl.addView (et);

        Button b = new Button (m_Context);
        b.setText (m_Context.getResources ().getIdentifier ("ok", "string", "android"));
        lp = new RelativeLayout.LayoutParams (wrapContent, wrapContent);
        lp.addRule (RelativeLayout.CENTER_VERTICAL);
        lp.addRule (RelativeLayout.ALIGN_PARENT_RIGHT);
        b.setLayoutParams (lp);
        b.setId (id.okButton);
        // Transparent background
        b.setBackgroundColor(0);
        rl.addView (b);


        rl.setPadding(16, 16, 16, 16);
        et.requestFocus();
        return rl;
    }

    public String getText ()
    {
        if (m_EditText == null)
            return null;

        return m_EditText.getText().toString ();
    }

    public void setText (String text)
    {
        if (text.equals(getText()))
            return;

        if (m_EditText == null)
            return;

        debugLog("setText {0}", text);

        // setText implicitly changes selection to 0, 0, and will invoke selection changed callback
        // we want to ignore this, since we want for selection to be at the end of the text
        long temp = m_LastSelection;
        m_LastSelection = 0;
        m_EditText.setText(text);
        m_LastSelection = temp;
        m_EditText.setSelection(text.length());
    }

    public void setSelection(int start, int length)
    {
        debugLog("set selection {0}, {1}", start, length);
        if (m_EditText != null && m_EditText.getText().length() >= start + length)
            m_EditText.setSelection(start, start + length);
    }

    private long convertSelectionToLong(long start, long end)
    {
        // Saw cases where end is actually smaller than start
        if (end < start)
        {
            long tmp = start;
            start = end;
            end = tmp;
        }
        long length = end - start;
        return start | (length << 32);
    }

    public long getSelection()
    {
        if (m_EditText == null)
            return 0;
        long start = m_EditText.getSelectionStart();
        long end = m_EditText.getSelectionEnd();

        return convertSelectionToLong(start, end);
    }

    public int[] getArea()
    {
        Rect rect = new Rect();
        getWindow().getDecorView().getWindowVisibleDisplayFrame(rect);
        return new int[] {rect.left, rect.top, rect.right, rect.bottom};
    }

    public void simulateKeyEvent(int keyCode)
    {
        if (m_EditText == null)
            return;

        m_EditText.dispatchKeyEventPreIme(new KeyEvent(0, 0, KeyEvent.ACTION_DOWN, keyCode, 0));
        m_EditText.dispatchKeyEventPreIme(new KeyEvent(0, 0, KeyEvent.ACTION_UP, keyCode, 0));
    }

    public void setLogging(boolean enabled)
    {
        m_LoggingEnabled = enabled;
    }

    public boolean getLogging()
    {
        return m_LoggingEnabled;
    }
}
