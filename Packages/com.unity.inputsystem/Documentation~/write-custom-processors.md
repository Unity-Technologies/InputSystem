# Writing custom Processors

You can also write custom Processors to use in your Project. Custom Processors are available in the UI and code in the same way as the built-in Processors. Add a class derived from [`InputProcessor<TValue>`](../api/UnityEngine.InputSystem.InputProcessor-1.html), and implement the [`Process`](../api/UnityEngine.InputSystem.InputProcessor-1.html#UnityEngine_InputSystem_InputProcessor_1_Process__0_UnityEngine_InputSystem_InputControl_) method:

>__IMPORTANT__: Processors must be __stateless__. This means you cannot store local state in a processor that will change depending on the input being processed. The reason for this is because processors are not part of the [input state](./Controls.md#control-state) that the Input System keeps.

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

Now, you need to tell the Input System about your Processor. Call [`InputSystem.RegisterProcessor`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterProcessor__1_System_String_) in your initialization code. You can do so locally within the Processor class like this:

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

Your new Processor is now available in the in the [Input Actions Editor](ActionsEditor.md) and you can also add it in code like this:

```CSharp
var action = new InputAction(processors: "myvalueshift(valueShift=2.3)");
```

If you want to customize the UI for editing your Processor, create a custom [`InputParameterEditor`](../api/UnityEngine.InputSystem.Editor.InputParameterEditor-1.html) class for it:

```CSharp
// No registration is necessary for an InputParameterEditor.
// The system will automatically find subclasses based on the
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
