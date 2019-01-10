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
import android.widget.Button;
import android.widget.EditText;
import android.widget.RelativeLayout;
import android.widget.TextView;
import android.util.*;
import com.unity3d.player.*;

import java.text.MessageFormat;

public class AndroidScreenKeyboard extends Dialog implements OnClickListener, TextWatcher, OnDismissListener
{
    interface IScreenKeyboardCallbacks
    {
        void OnTextChanged(String text, int selectionStart, int selectionLength);
        void OnStatusChanged(int status);
    }

    private enum ScreenKeyboardStatus
    {
        Visible(0),
        Done(1),
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

    public AndroidScreenKeyboard ()
    {
        super (UnityPlayer.currentActivity);
        m_Context = UnityPlayer.currentActivity;

        Window window = getWindow();
        window.setGravity(Gravity.BOTTOM);
        window.requestFeature(Window.FEATURE_NO_TITLE);
        // Set transparent background
        // Because in Lollipop otherwise we get black frame around the dialog
        window.setBackgroundDrawable(new ColorDrawable(android.graphics.Color.TRANSPARENT));
        window.setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        // Don't dim the view behind the dialog
        window.clearFlags (WindowManager.LayoutParams.FLAG_DIM_BEHIND);
        window.addFlags(WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED);
    }

    public void show(
            IScreenKeyboardCallbacks callbacks,
            int keyboardType,
            String initialText,
            String placeholderText,
            boolean correction,
            boolean multiline,
            boolean secure,
            boolean alert)
    {
        m_Callbacks = callbacks;

        setContentView (createSoftInputView ());

        EditText txtInput = (EditText) findViewById (id.txtInput);
        txtInput.setImeOptions (EditorInfo.IME_ACTION_DONE);
        txtInput.setText (initialText);
        txtInput.setHint (placeholderText);
        txtInput.setHintTextColor (0x61000000);
        txtInput.setInputType (convertInputType (ScreenKeyboardType.values()[keyboardType], correction, multiline, secure));
        txtInput.setImeOptions(EditorInfo.IME_FLAG_NO_FULLSCREEN);

        // if ( characterLimit > 0 )
        //    txtInput.setFilters (new InputFilter[] { new InputFilter.LengthFilter(characterLimit) });

        txtInput.addTextChangedListener (this);
        txtInput.setSelection(txtInput.getText().length());
        txtInput.setClickable (true);

        Button okButton = (Button) findViewById (id.okButton);

        // set up click events
        okButton.setOnClickListener (this);

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

        setOnDismissListener(this);

        show();
        m_Callbacks.OnStatusChanged(ScreenKeyboardStatus.Visible.value);
    }

    public void afterTextChanged (Editable s)
    {
        Log.v("Unity", "afterTextChanged");
       // mUnityPlayer.reportSoftInputStr (s.toString (), kbCommand.dontHide, false);
        m_Callbacks.OnTextChanged(s.toString());
    }

    public void beforeTextChanged (CharSequence s, int start, int count, int after)
    {
        Log.v("Unity", "beforeTextChanged");
    }

    public void onTextChanged (CharSequence s, int start, int before, int count)
    {
        Log.v("Unity", MessageFormat.format("onTextChanged {0}, {1}, {2}", start, before, count));
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

    @Override public void onDismiss(DialogInterface dialog)
    {
        Log.v("Unity", "onDismiss");
        m_Callbacks.OnStatusChanged(ScreenKeyboardStatus.Done.value);
    }


    protected View createSoftInputView ()
    {
        final int matchParent = ViewGroup.LayoutParams.MATCH_PARENT;
        final int wrapContent = ViewGroup.LayoutParams.WRAP_CONTENT;
        RelativeLayout rl = new RelativeLayout (m_Context);
        rl.setLayoutParams (new ViewGroup.LayoutParams (matchParent, matchParent));
        rl.setBackgroundColor(0xFFFFFFFF);
        RelativeLayout.LayoutParams lp = null;

        {   // create text input field
            EditText et = new EditText (m_Context)
            {
                public boolean onKeyPreIme(int keyCode, KeyEvent event)
                {
                    // intercept BACK to make sure the dialog is close, and SEARCH to make sure it's ignored.
                    if (keyCode == KeyEvent.KEYCODE_BACK)
                    {
                        m_Callbacks.OnStatusChanged(ScreenKeyboardStatus.Canceled.value);
                        return true;
                    }
                    if (keyCode == KeyEvent.KEYCODE_SEARCH)
                        return true;
                    return super.onKeyPreIme(keyCode, event);
                }

                public void onWindowFocusChanged(boolean hasWindowFocus)
                {
                    super.onWindowFocusChanged(hasWindowFocus);
                    // for some reason this code can NOT be in the OnFocusChangeListener; go figure..
                    if (hasWindowFocus)
                    {
                        InputMethodManager imm = (InputMethodManager)m_Context.getSystemService(Context.INPUT_METHOD_SERVICE);
                        imm.showSoftInput(this, 0);
                    }
                }

                @Override
                protected void onSelectionChanged(int start, int end)
                {
                    //TODO
                   // mUnityPlayer.reportSoftInputSelection (start, end - start);
                }
            };
            lp = new RelativeLayout.LayoutParams (matchParent, wrapContent);
            lp.addRule (RelativeLayout.CENTER_VERTICAL);
            lp.addRule (RelativeLayout.LEFT_OF, id.okButton);
            et.setLayoutParams (lp);
            et.setId (id.txtInput);
            rl.addView (et);
        }

        {   // create ok button
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
        }

        View view = rl;

        // This will be called when "Done" button gets pressed. Unfortunately,
        // not when soft keyb is dismissed with back button...
        EditText txtInput = (EditText) view.findViewById (id.txtInput);
        txtInput.setOnEditorActionListener (new TextView.OnEditorActionListener () {
            public boolean onEditorAction (TextView v, int actionId, KeyEvent event) {
                if (actionId == EditorInfo.IME_ACTION_DONE)
                {
                    m_Callbacks.OnStatusChanged(ScreenKeyboardStatus.Done.value);
                }

                return false; // We never consume the action we get
            }
        });
        view.setPadding(16, 16, 16, 16);

        return view;
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
        EditText txtInput = (EditText) findViewById (id.txtInput);
        if (txtInput != null)
        {
            txtInput.setText(text);
            txtInput.setSelection(text.length());
        }
    }

    /*
    public void setCharacterLimit(int characterLimit)
    {
        EditText txtInput = (EditText) findViewById(id.txtInput);
        if(txtInput != null)
        {
            if (characterLimit > 0)
                txtInput.setFilters(new InputFilter[] { new InputFilter.LengthFilter(characterLimit) });
            else
                txtInput.setFilters(new InputFilter[] { });

        }
    }

    public void setSelection(int start, int length) {
        EditText txtInput = (EditText) findViewById(id.txtInput);
        if (txtInput != null && txtInput.getText().length() >= start + length) {
            txtInput.setSelection(start, start + length);
        }
    }
    */
}
