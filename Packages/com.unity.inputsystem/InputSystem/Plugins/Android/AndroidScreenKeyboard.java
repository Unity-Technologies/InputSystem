package com.unity.inputsystem;


import android.app.Dialog;
import android.content.Context;
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
import android.widget.Button;
import android.widget.EditText;
import android.widget.RelativeLayout;
import android.widget.TextView;
import android.util.*;
import com.unity3d.player.*;

import java.text.MessageFormat;

public class AndroidScreenKeyboard extends Dialog implements OnClickListener, TextWatcher
{
    interface IScreenKeyboardCallbacks
    {
        void OnTextChanged(String text);
    }


    private static final class id
    {
        private static final int okButton    = 0x3f050002;
        private static final int txtInput    = 0x3f050001;
    }
    private Context mContext = null;
    private IScreenKeyboardCallbacks m_Callbacks;
    private static int hintColor = 0x61000000;
    private static int backgroundColor = 0xFFFFFFFF;
    // Kitkat specific flags
    private static int LayoutParams_FLAG_TRANSLUCENT_NAVIGATION = 0x08000000;
    private static int LayoutParams_FLAG_TRANSLUCENT_STATUS = 0x04000000;

    public AndroidScreenKeyboard (IScreenKeyboardCallbacks callbacks)
    {
        super (UnityPlayer.currentActivity);
        mContext = UnityPlayer.currentActivity;
        m_Callbacks = callbacks;
        /*
        Context context, UnityPlayer player,
                            String initialText, int type, boolean correction,
                            boolean multiline, boolean secure,
                            boolean alert, String placeholder, int characterLimit)
*/


        getWindow().setGravity(Gravity.BOTTOM);
        getWindow().requestFeature(Window.FEATURE_NO_TITLE);
        // Set transparent background
        // Because in Lollipop otherwise we get black frame around the dialog
        getWindow().setBackgroundDrawable(new ColorDrawable(android.graphics.Color.TRANSPARENT));

        setContentView (createSoftInputView ());
        getWindow().setLayout(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);

        // Don't dim the view behind the dialog
        getWindow().clearFlags (WindowManager.LayoutParams.FLAG_DIM_BEHIND);
        // Workaround for the input field shown behind the keyboard when translucent enabled
        /*
        if (KITKAT_SUPPORT)
        {
            getWindow().clearFlags(LayoutParams_FLAG_TRANSLUCENT_NAVIGATION);
            getWindow().clearFlags(LayoutParams_FLAG_TRANSLUCENT_STATUS);
        }*/

        EditText txtInput = (EditText) findViewById (id.txtInput);
        Button okButton = (Button) findViewById (id.okButton);
        //setupTextInput (txtInput, initialText, type, correction, multiline,  secure, alert, placeholder, characterLimit);

        setupTextInput(txtInput, "Test", 0, false, false, false, false, "", 10);
        // set up click events
        okButton.setOnClickListener (this);

        // development build is shown on top of locked screen, but for some android versions need to
        // additionaly set FLAG_SHOW_WHEN_LOCKED for input dialog to see input keyboard.
        // FLAG_SHOW_WHEN_LOCKED is deprecated in API 27
        //if (UNITY_DEVELOPMENT_PLAYER)
            getWindow().addFlags(WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED);

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
    }

    private void setupTextInput (EditText txtInput, String initialText,
                                 int type, boolean correction,
                                 boolean multiline, boolean secure,
                                 boolean alert, String placeholder, int characterLimit)
    {
        txtInput.setImeOptions (EditorInfo.IME_ACTION_DONE);
        txtInput.setText (initialText);
        txtInput.setHint (placeholder);
        txtInput.setHintTextColor (hintColor);
        txtInput.setInputType (convertInputType (type, correction, multiline, secure));
        txtInput.setImeOptions(EditorInfo.IME_FLAG_NO_FULLSCREEN);

        if ( characterLimit > 0 )
            txtInput.setFilters (new InputFilter[] { new InputFilter.LengthFilter(characterLimit) });

        txtInput.addTextChangedListener (this);
        txtInput.setSelection(txtInput.getText().length());

        txtInput.setClickable (true);
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

    private int convertInputType (int type, boolean correction,
                                  boolean multiline, boolean secure)
    {
        int baseType = (correction ? EditorInfo.TYPE_TEXT_FLAG_AUTO_CORRECT : EditorInfo.TYPE_TEXT_FLAG_NO_SUGGESTIONS)
                       | (multiline ? EditorInfo.TYPE_TEXT_FLAG_MULTI_LINE : 0)
                       | (secure ? EditorInfo.TYPE_TEXT_VARIATION_PASSWORD : 0);

        // Max value should stay in sync with enum UnityEngine.TouchScreenKeyboardType size from TouchScreenKeyboardType.cs
        if (type < 0 || type > 10)
            return baseType;

        int flagsByType[] = {
            // Default = 0, Default keyboard layout.
            EditorInfo.TYPE_CLASS_TEXT,

            // ASCIICapable = 1, Keyboard displays standard ASCII characters.
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_FLAG_CAP_SENTENCES,

            // NumbersAndPunctuation = 2, Keyboard with numbers and punctuation.
            EditorInfo.TYPE_CLASS_NUMBER
            | EditorInfo.TYPE_NUMBER_FLAG_DECIMAL
            | EditorInfo.TYPE_NUMBER_FLAG_SIGNED,

            // URL = 3, Keyboard optimized for URL entry, features ".", "/", and
            // ".com".
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_VARIATION_URI,

            // NumberPad = 4, Numeric keypad designed for PIN entry, features the
            // numbers 0 through 9.
            EditorInfo.TYPE_CLASS_NUMBER,

            // PhonePad = 5, Keypad designed for entering telephone numbers,
            // features the numbers 0 through 9 and the "*" and "#" characters
            EditorInfo.TYPE_CLASS_PHONE,

            // NamePhonePad = 6, Keypad designed for entering a person's name or
            // phone number.
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_FLAG_CAP_WORDS
            | EditorInfo.TYPE_TEXT_VARIATION_PERSON_NAME,

            // EmailAddress = 7, Keyboard optimized for specifying email addresses,
            // features the "@", "." and space characters.
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_VARIATION_EMAIL_ADDRESS,

            // NintendoNetworkAccount = 8, Default keyboard in case Wii U specific NintendoNetworkAccount type is selected.
            EditorInfo.TYPE_CLASS_TEXT,

            // Social = 9, Keyboard optimized for text entry in social media applications such as Twitter,
            // features the "@", "." and space characters and capitalises the first letter of a sentence.
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_FLAG_CAP_SENTENCES
            | EditorInfo.TYPE_TEXT_VARIATION_EMAIL_ADDRESS,

            // Search = 10, Keyboard optimized for search terms,
            // features ".", "/" and space characters.
            EditorInfo.TYPE_CLASS_TEXT
            | EditorInfo.TYPE_TEXT_VARIATION_URI
        };

        // Discard TYPE_CLASS_TEXT bits if TYPE_CLASS_NUMBER bit is set
        if ((flagsByType[type] & EditorInfo.TYPE_CLASS_NUMBER) != 0)
            return flagsByType[type];

        return baseType | flagsByType[type];
    }

    /*
    private void reportStrAndHide (String str, boolean canceled)
    {
        //Fix for 953849. It looks like this method doesn't works correctly on some Sony and Samsung devices with non-Android soft keyboard
        //Selection.removeSelection(((EditText) findViewById (id.txtInput)).getEditableText());
        ((EditText) findViewById (id.txtInput)).setSelection(0, 0);
        mUnityPlayer.reportSoftInputStr (str, UnityPlayer.kbCommand.hide, canceled);
    }
    */

    @Override public void onClick (View v)
    {
        //reportStrAndHide (getSoftInputStr (), false);
    }

    public void Show()
    {
        show ();int s = 5;
    }

    /*
    public void onBackPressed ()
    {
        reportStrAndHide (getSoftInputStr (), true);
    }
    */
    protected View createSoftInputView ()
    {
        final int matchParent = ViewGroup.LayoutParams.MATCH_PARENT;
        final int wrapContent = ViewGroup.LayoutParams.WRAP_CONTENT;
        RelativeLayout rl = new RelativeLayout (mContext);
        rl.setLayoutParams (new ViewGroup.LayoutParams (matchParent, matchParent));
        rl.setBackgroundColor(backgroundColor);
        RelativeLayout.LayoutParams lp = null;

        {   // create text input field
            EditText et = new EditText (mContext)
            {
                public boolean onKeyPreIme(int keyCode, KeyEvent event)
                {
                    // intercept BACK to make sure the dialog is close, and SEARCH to make sure it's ignored.
                    if (keyCode == KeyEvent.KEYCODE_BACK)
                    {
                        // TODO
                        //reportStrAndHide (getSoftInputStr (), true);
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
                        InputMethodManager imm = (InputMethodManager)mContext.getSystemService(Context.INPUT_METHOD_SERVICE);
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
            Button b = new Button (mContext);
            b.setText (mContext.getResources ().getIdentifier ("ok", "string", "android"));
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
                    // TODO
                    //reportStrAndHide (getSoftInputStr (), false);
                }

                return false; // We never consume the action we get
            }
        });
        view.setPadding(16, 16, 16, 16);

        return view;
    }
    /*
    private String getSoftInputStr ()
    {
        EditText txtInput = (EditText) findViewById (id.txtInput);

        if (txtInput == null)
            return null;

        return txtInput.getText ().toString ().trim ();
    }

    public void setSoftInputStr (String text)
    {
        EditText txtInput = (EditText) findViewById (id.txtInput);
        if (txtInput != null)
        {
            txtInput.setText(text);
            txtInput.setSelection(text.length());
        }
    }

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
