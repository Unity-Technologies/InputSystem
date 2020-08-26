package com.unity.inputsystem;


import android.app.Dialog;
import android.content.Context;
import android.content.DialogInterface.*;
import android.content.DialogInterface;
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
        void OnStatusChanged(int status);
        void OnSelectionChanged(int start, int length);
    }

    private enum ScreenKeyboardStatus
    {
        Done(0),
        Visible(1),
        Canceled(2);

        private final int value;

        ScreenKeyboardStatus(int value) { this.value = value; }
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
    private ScreenKeyboardStatus m_DismissReturnValue;

    private boolean m_MoveSelectionToEnd;

    public AndroidScreenKeyboard ()
    {
        super (UnityPlayer.currentActivity);
        m_Context = UnityPlayer.currentActivity;
        m_DismissReturnValue = ScreenKeyboardStatus.Done;
        Window window = getWindow();
        window.requestFeature(Window.FEATURE_NO_TITLE);
        // Set transparent background
        // Because in Lollipop otherwise we get black frame around the dialog
        window.setBackgroundDrawable(new ColorDrawable(android.graphics.Color.TRANSPARENT));
        window.setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        // Don't dim the view behind the dialog
        window.clearFlags (WindowManager.LayoutParams.FLAG_DIM_BEHIND);
        window.addFlags(WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED);
    }

    private void debugLog(String format, Object... args)
    {
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
        m_DismissReturnValue = ScreenKeyboardStatus.Done;
        setHideInputField(inputFieldHidden);

        setContentView (createSoftInputView ());
        EditText txtInput = (EditText) findViewById (id.txtInput);
        txtInput.setImeOptions (EditorInfo.IME_ACTION_DONE | EditorInfo.IME_FLAG_NO_FULLSCREEN);
        txtInput.setText (initialText);
        txtInput.setHint (placeholderText);
        txtInput.setHintTextColor (0x61000000);
        txtInput.setInputType (convertInputType (ScreenKeyboardType.values()[keyboardType], correction, multiline, secure));
        txtInput.addTextChangedListener (this);
        txtInput.setSelection(txtInput.getText().length());
        txtInput.setClickable (true);
        txtInput.setOnFocusChangeListener (new View.OnFocusChangeListener ()
        {
            @Override
            public void onFocusChange (View v, boolean hasFocus)
            {
                if (hasFocus)
                {
                    int vis = WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_VISIBLE;
                    getWindow ().setSoftInputMode (vis);
                }
            }
        });

        Button okButton = (Button) findViewById (id.okButton);
        okButton.setOnClickListener (this);

        setOnDismissListener(this);
        setOnShowListener(this);

        show();
    }

    public void setHideInputField(boolean isInputFieldHidden)
    {
        Window window = getWindow();
        WindowManager.LayoutParams param = window.getAttributes();
        if (isInputFieldHidden)
        {
            // There's no reliable API for hiding input field
            // So we're drawing it outside screen and thus making an illusion that it's hidden
            // Alternatively we could make input field fully transparent, but that raises a lot of other problems:
            // - Need to make cursor transparent as well
            // - Need to make selection box transparent
            // - Ignore and forward input when you touch invisible input field
            param.gravity = Gravity.AXIS_CLIP;
            param.x = 20000;
            param.y = 20000;
        }
        else
        {
            param.gravity = Gravity.BOTTOM;
            param.x = 0;
            param.y = 0;
        }
        window.setAttributes(param);
    }


    public void afterTextChanged (Editable s)
    {
        EditText txtInput = (EditText) findViewById (id.txtInput);
        debugLog("afterTextChanged: {0} Start {1} End {2}",txtInput.getText(), txtInput.getSelectionStart(), txtInput.getSelectionEnd());

        // TODO: For IME SelectionEnd and Start doesn't return what you would expect
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
        m_Callbacks.OnStatusChanged(ScreenKeyboardStatus.Visible.value);
    }

    @Override public void onDismiss(DialogInterface dialog)
    {
        debugLog("onDismiss " + m_DismissReturnValue);
        m_Callbacks.OnStatusChanged(m_DismissReturnValue.value);
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
                // intercept BACK to make sure the dialog is close, and SEARCH to make sure it's ignored.
                if (keyCode == KeyEvent.KEYCODE_BACK) {
                    m_DismissReturnValue = ScreenKeyboardStatus.Canceled;
                    dismiss();
                    return true;
                }
                if (keyCode == KeyEvent.KEYCODE_SEARCH)
                    return true;
                return super.onKeyPreIme(keyCode, event);
            }

            public void onWindowFocusChanged(boolean hasWindowFocus) {
                super.onWindowFocusChanged(hasWindowFocus);
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

                boolean moveSelectionToEnd = m_MoveSelectionToEnd;
                m_MoveSelectionToEnd = false;
                if (moveSelectionToEnd && start != length() && end != length())
                {
                    debugLog("moving selection to {0} {1}", length(), 0);
                    // This will implicitly invoke onSelectionChanged again
                    setSelection(length());
                }
                else
                {
                    m_Callbacks.OnSelectionChanged(start, end - start);
                }
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
        EditText txtInput = (EditText) findViewById (id.txtInput);

        if (txtInput == null)
            return null;

        return txtInput.getText ().toString ().trim ();
    }

    public void setText (String text)
    {
        if (text.equals(getText()))
            return;
        EditText txtInput = (EditText) findViewById (id.txtInput);
        if (txtInput != null)
        {
			debugLog("setText {0}", text);
            m_MoveSelectionToEnd = true;
            txtInput.setText(text);
        }
    }

    public void setSelection(int start, int length)
    {
        debugLog("set selection {0}, {1}", start, length);
        EditText txtInput = (EditText) findViewById(id.txtInput);
        if (txtInput != null && txtInput.getText().length() >= start + length)
            txtInput.setSelection(start, start + length);
    }

    public long getSelection()
    {
        EditText txtInput = (EditText) findViewById(id.txtInput);
        if (txtInput == null)
            return 0;
        long start = txtInput.getSelectionStart();
        long end = txtInput.getSelectionEnd();
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
}
