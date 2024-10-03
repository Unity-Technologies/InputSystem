using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

// Let's say we want to have a composite that takes an axis and uses
// it's value to multiply the length of a vector from a stick. This could
// be used, for example, to have the right trigger on the gamepad act as
// a strength multiplier on the value of the left stick.
//
// We start by creating a class that is based on InputBindingComposite<>.
// The type we give it is the type of value that we will compute. In this
// case, we will consume a Vector2 from the stick so that is the type
// of value we return.
//
// NOTE: By advertising the type of value we return, we also allow the
//       input system to filter out our composite if it is not applicable
//       to a specific type of action. For example, if an action is set
//       to "Value" as its type and its "Control Type" is set to "Axis",
//       our composite will not be shown as our value type (Vector2) is
//       incompatible with the value type of Axis (float).
//
// Also, we need to register our composite with the input system. And we
// want to do it in a way that makes the composite visible in the action
// editor of the input system.
//
// For that to happen, we need to call InputSystem.RegisterBindingComposite
// sometime during startup. We make that happen by using [InitializeOnLoad]
// in the editor and [RuntimeInitializeOnLoadMethod] in the player.
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
// We can customize the way display strings are formed for our composite by
// annotating it with DisplayStringFormatAttribute. The string is simply a
// list with elements to be replaced enclosed in curly braces. Everything
// outside those will taken verbatim. The fragments inside the curly braces
// in this case refer to the binding composite parts by name. Each such
// instance is replaced with the display text for the corresponding
// part binding.
[DisplayStringFormat("{multiplier}*{stick}")]
public class CustomComposite : InputBindingComposite<Vector2>
{
    // In the editor, the static class constructor will be called on startup
    // because of [InitializeOnLoad].
    #if UNITY_EDITOR
    static CustomComposite()
    {
        // Trigger our RegisterBindingComposite code in the editor.
        Initialize();
    }

    #endif

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // This registers the composite with the input system. After calling this
        // method, we can have bindings reference the composite. Also, the
        // composite will show up in the action editor.
        //
        // NOTE: We don't supply a name for the composite here. The default logic
        //       will take the name of the type ("CustomComposite" in our case)
        //       and snip off "Composite" if used as a suffix (which is the case
        //       for us) and then use that as the name. So in our case, we are
        //       registering a composite called "Custom" here.
        //
        //       If we were to use our composite with the AddCompositeBinding API,
        //       for example, it would look like this:
        //
        //       myAction.AddCompositeBinding("Custom")
        //           .With("Stick", "<Gamepad>/leftStick")
        //           .With("Multiplier", "<Gamepad>/rightTrigger");
        InputSystem.RegisterBindingComposite<CustomComposite>();
    }

    // So, we need two parts for our composite. The part that delivers the stick
    // value and the part that delivers the axis multiplier. Note that each part
    // may be bound to multiple controls. The input system handles that for us
    // by giving us an integer identifier for each part that reads a single value
    // from however many controls are bound to the part.
    //
    // In our case, this could be used, for example, to bind the "multiplier" part
    // to both the left and the right trigger on the gamepad.

    // To tell the input system of a "part" binding that we need for a composite,
    // we add a public field with an "int" type and annotated with an [InputControl]
    // attribute. We set the "layout" property on the attribute to tell the system
    // what kind of control we expect to be bound to the part.
    //
    // NOTE: These part binding need to be *public fields* for the input system
    //       to find them.
    //
    // So this is introduces a part to the composite called "multiplier" and
    // expecting an "Axis" control. The value of the field will be set by the
    // input system. It will be some internal, unique numeric ID for the part
    // which we can then use with InputBindingCompositeContext.ReadValue to
    // read out the value of just that part.
    [InputControl(layout = "Axis")]
    public int multiplier;

    // The other part we need is for the stick.
    //
    // NOTE: We could use "Stick" here but "Vector2" is a little less restrictive.
    [InputControl(layout = "Vector2")]
    public int stick;

    // We may also expose "parameters" on our composite. These can be configured
    // graphically in the action editor and also through AddCompositeBinding.
    //
    // Let's say we want to allow the user to specify an additional scale factor
    // to apply to the value of "multiplier". We can do so by simply adding a
    // public field of type float. Any public field that is not annotated with
    // [InputControl] will be treated as a possible parameter.
    //
    // If we added a composite with AddCompositeBinding, we could configure the
    // parameter like so:
    //
    //     myAction.AddCompositeBinding("Custom(scaleFactor=0.5)"
    //         .With("Multiplier", "<Gamepad>/rightTrigger")
    //         .With("Stick", "<Gamepad>/leftStick");
    public float scaleFactor = 1;

    // Ok, so now we have all the configuration in place. The final piece we
    // need is the actual logic that reads input from "multiplier" and "stick"
    // and computes a final input value.
    //
    // We can do that by defining a ReadValue method which is the actual workhorse
    // for our composite.
    public override Vector2 ReadValue(ref InputBindingCompositeContext context)
    {
        // We read input from the parts we have by simply
        // supplying the part IDs that the input system has set up
        // for us to ReadValue.
        //
        // NOTE: Vector2 is a less straightforward than primitive value types
        //       like int and float. If there are multiple controls bound to the
        //       "stick" part, we need to tell the input system which one to pick.
        //       We do so by giving it an IComparer. In this case, we choose
        //       Vector2MagnitudeComparer to return the Vector2 with the greatest
        //       length.
        var stickValue = context.ReadValue<Vector2, Vector2MagnitudeComparer>(stick);
        var multiplierValue = context.ReadValue<float>(multiplier);

        // The rest is simple. We just scale the vector we read by the
        // multiple from the axis and apply our scale factor.
        return stickValue * (multiplierValue * scaleFactor);
    }
}

// Our custom composite is complete and fully functional. We could stop here and
// call it a day. However, for the sake of demonstration, let's say we also want
// to customize how the parameters for our composite are edited. We have "scaleFactor"
// so let's say we want to replace the default float inspector with a slider.
//
// We can replace the default UI by simply deriving a custom InputParameterEditor
// for our composite.
#if UNITY_EDITOR
public class CustomCompositeEditor : InputParameterEditor<CustomComposite>
{
    public override void OnGUI()
    {
        // Using the 'target' property, we can access an instance of our composite.
        var currentValue = target.scaleFactor;

        // The easiest way to lay out our UI is to simply use EditorGUILayout.
        // We simply assign the changed value back to the 'target' object. The input
        // system will automatically detect a change in value.
        target.scaleFactor = EditorGUILayout.Slider(m_ScaleFactorLabel, currentValue, 0, 2);
    }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
    public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
    {
        var slider = new Slider(m_ScaleFactorLabel.text, 0, 2)
        {
            value = target.scaleFactor,
            showInputField = true
        };

        // Note: For UIToolkit sliders, as of Feb 2022, we can't register for the mouse up event directly
        // on the slider because an element inside the slider captures the event. The workaround is to
        // register for the event on the slider container. This will be fixed in a future version of
        // UIToolkit.
        slider.Q("unity-drag-container").RegisterCallback<MouseUpEvent>(evt =>
        {
            target.scaleFactor = slider.value;
            onChangedCallback?.Invoke();
        });

        root.Add(slider);
    }

#endif

    private GUIContent m_ScaleFactorLabel = new GUIContent("Scale Factor");
}
#endif
