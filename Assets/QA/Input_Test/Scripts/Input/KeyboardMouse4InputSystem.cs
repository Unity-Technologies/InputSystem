using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class KeyboardMouse4InputSystem : MonoBehaviour
{
    public SpriteRenderer hightlight_key_input_system;
    public InputField unmapped_key_list;
    public Text mouse_pos_text;

    private InputAction keyboard_press_action;
    private InputAction mouse_press_action;

    private int mouse_move_highlight_threshold = 20;

    // Use this for initialization
    void Start()
    {
        keyboard_press_action = new InputAction(name: "KeyboardPressAction", binding: "/<keyboard>/<key>");
        keyboard_press_action.performed += callbackContext => KeyboardKeyPress(callbackContext.control as KeyControl);
        keyboard_press_action.Enable();

        mouse_press_action = new InputAction(name: "MousePressAction", binding: "/<mouse>/<button>");
        mouse_press_action.performed += callbackContext => MouseKeyPress();
        mouse_press_action.Enable();

        //InputSystem.onEvent +=
        //    eventPtr =>
        //    {
        //        if (eventPtr.IsA<StateEvent>())
        //        {
        //            if (eventPtr.deviceId == Keyboard.current.id)
        //                ;
        //        }
        //    };
    }

    void Update()
    {
        // Show mouse actions
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float moveX = mouse.delta.x.ReadValue();
            float moveY = mouse.delta.y.ReadValue();

            float scrollX = mouse.scroll.x.ReadValue();
            float scrollY = mouse.scroll.y.ReadValue();

            // Mouse move horizontally
            if (Mathf.Abs(moveX) > mouse_move_highlight_threshold)
            {
                if (moveX > 0)
                {
                    StartMouseHighlight("Move_Right", moveX);
                    StopMouseHighlight("Move_Left");
                }
                else
                {
                    StartMouseHighlight("Move_Left", moveX);
                    StopMouseHighlight("Move_Right");
                }
            }
            else
            {
                StopMouseHighlight("Move_Right");
                StopMouseHighlight("Move_Left");
            }

            // Mouse move vertically
            if (Mathf.Abs(moveY) > mouse_move_highlight_threshold)
            {
                if (moveY > 0)
                {
                    StartMouseHighlight("Move_Up", moveY);
                    StopMouseHighlight("Move_Down");
                }
                else
                {
                    StartMouseHighlight("Move_Down", moveY);
                    StopMouseHighlight("Move_Up");
                }
            }
            else
            {
                StopMouseHighlight("Move_Up");
                StopMouseHighlight("Move_Down");
            }

            // Mouse Wheel scroll
            // Only horizontal scroll has UI. Vertical scroll is shown in text box.
            if (scrollY > 0)
            {
                StartMouseHighlight("Wheel_Down", scrollY);
                StopMouseHighlight("Wheel_Up");
            }
            else if (scrollY < 0)
            {
                StartMouseHighlight("Wheel_Up", scrollY);
                StopMouseHighlight("Wheel_Down");
            }
            else
            {
                StopMouseHighlight("Wheel_Up");
                StopMouseHighlight("Wheel_Down");
            }

            if (Mathf.Abs(scrollX) > 0)
                AddUnmappedKey("Scroll X: " + scrollX);

            // Update mouse position
            mouse_pos_text.text = mouse.position.ReadValue().ToString("F0");
        }
    }

    private void Keyboard_onTextInput(char obj)
    {
        throw new NotImplementedException();
    }

    // callback function when a key is pressed on Keyboard
    private void KeyboardKeyPress(KeyControl control)
    {
        string keyName = control.keyCode.ToString();

        if (control.ReadValue() > 0)
            StartKeyHightlight(keyName);
        else
            StopKeyHighlight(keyName);
    }

    // callback function when a button is pressed on Mouse
    private void MouseKeyPress()
    {
        if (Mouse.current.leftButton.ReadValue() == 0)
            StopKeyHighlight("Mouse0");
        else
            StartKeyHightlight("Mouse0");

        if (Mouse.current.rightButton.ReadValue() == 0)
            StopKeyHighlight("Mouse1");
        else
            StartKeyHightlight("Mouse1");

        if (Mouse.current.middleButton.ReadValue() == 0)
            StopKeyHighlight("Mouse2");
        else
            StartKeyHightlight("Mouse2");
    }

    // Generate the red square over the key or mouse button
    private void StartKeyHightlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);
        if (key == null)
            AddUnmappedKey(keyName);
        else
        {
            SpriteRenderer sr = key.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
                Instantiate(hightlight_key_input_system, key.transform.position, key.transform.rotation, key);
            else
                sr.gameObject.SetActive(true);
        }
    }

    private void StopKeyHighlight(string keyName)
    {
        Transform key = transform.Find("Keys/" + keyName);
        if (key != null)
        {
            SpriteRenderer[] sr = key.GetComponentsInChildren<SpriteRenderer>();
            if (sr.Length > 0)
            {
                foreach (SpriteRenderer s in sr)
                    Destroy(s.gameObject);
            }
        }
    }

    private void StartMouseHighlight(string mouseAction, float move)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction);
        if (mAction != null)
            mAction.GetComponentInChildren<TextHighlight>().Play(move.ToString("F0"));
    }

    private void StopMouseHighlight(string mouseAction)
    {
        Transform mAction = transform.Find("Mouse/" + mouseAction);
        if (mAction != null)
            mAction.GetComponentInChildren<TextHighlight>().Stop();
    }

    private string FirstLetterToUpper(string str)
    {
        if (String.IsNullOrEmpty(str))
            return null;
        else if (str.Length == 1)
            return str.ToUpper();
        else
            return char.ToUpper(str[0]) + str.Substring(1);
    }

    // Show the unmapped key name in the text field
    private void AddUnmappedKey(string keyName)
    {
        unmapped_key_list.text += "<color=red>" + keyName + "</color>\n";
    }
}
