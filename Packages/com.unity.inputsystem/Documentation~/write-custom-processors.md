---
uid: input-system-custom-processors
---

# Write custom processors

You can write custom processors to use with [bindings](bindings.md), [actions](actions.md) and [controls](controls.md) in your Project. Custom processors are available in the UI and code in the same way as the [built-in processors](built-in-processors.md).

To create a custom processor:

1. [Add a processor class and method](#add-a-processor-class-and-method)
1. [Register the new processor to the Input System](#register-the-new-processor-to-the-input-system)
1. [Customize the Editor UI](#customize-the-editor-ui) for the new processor, if necessary.

## Add a processor class and method

**1.** Add a class derived from [`InputProcessor<TValue>`](xref:UnityEngine.InputSystem.InputProcessor`1), and implement the [`Process`](xref:UnityEngine.InputSystem.InputProcessor`1) method:

```CSharp
public class MyValueShiftProcessor : InputProcessor<float>
{
    [Tooltip("Number to add to incoming values.")]
    public float valueShift = 0;

    public override float Process(float value, InputControl control)
    {
        return value + valueShift;
    }
}
```

> [!IMPORTANT]
> Processors must be __stateless__, because they are not part of the [input state](control-state.md) that the Input System keeps. For this reason, you can't store local states in a processor if the processor changes based on the input value.

## Register the new processor to the Input System

Register the new processor to the Input System. Call [`InputSystem.RegisterProcessor`](xref:UnityEngine.InputSystem.InputSystem) in your initialization code. You can do this locally within the Processor class:

```CSharp
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class MyValueShiftProcessor : InputProcessor<float>
{
    #if UNITY_EDITOR
    static MyValueShiftProcessor()
    {
        Initialize();
    }
    #endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        InputSystem.RegisterProcessor<MyValueShiftProcessor>();
    }

    //...
}
```

Your new Processor is now available in the in the [Input Actions Editor](actions-editor.md) and you can also add it in code like this:

```CSharp
var action = new InputAction(processors: "myvalueshift(valueShift=2.3)");
```

## Customize the Editor UI

To customize the UI for editing your Processor, create a custom [`InputParameterEditor`](xref:UnityEngine.InputSystem.Editor.InputParameterEditor`1) class for it:

```CSharp
// No registration is necessary for an InputParameterEditor.
// The system automatically finds subclasses based on the
// <..> type parameter.
#if UNITY_EDITOR
public class MyValueShiftProcessorEditor : InputParameterEditor<MyValueShiftProcessor>
{
    private GUIContent m_SliderLabel = new GUIContent("Shift By");

    public override void OnEnable()
    {
        // Put initialization code here. Use 'target' to refer
        // to the instance of MyValueShiftProcessor that is being
        // edited.
    }

    public override void OnGUI()
    {
        // Define your custom UI here using EditorGUILayout.
        target.valueShift = EditorGUILayout.Slider(m_SliderLabel,
            target.valueShift, 0, 10);
    }
}
#endif
```
