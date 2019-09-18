# Using the Input System in the Editor

Unlike Unity's old Input Manager, you can use the new input system from within  `EditorWindow` code as well. For example, you can gain access to pen pressure information like this:

```
class MyEditorWindow : EditorWindow
{
    public void OnGUI()
    {
        var pen = Pen.current;
        if (pen != null)
        {
            var position = pen.position.ReadValue();
            var pressure = pen.pressure.ReadValue();

            //...
        }
    }
}
```

This encompasses all code called from `OnGUI()` methods. This means that you can also use the Input System in property drawers, Inspectors, and other similar places.

>__Note__: Unity doesn't support actions in edit mode.

## Coordinate System

The coordinate system differs between `EditorWindow` code and `UnityEngine.Screen`. `EditorWindow` code has its origin in the upper left corner, with Y down. `UnityEngine.Screen` has it in the bottom left corner, with Y up.

The Input System compensates for that by automatically converting coordinates depending on whether you call it from the game or from editor code. In other words, calling `Mouse.current.position.ReadValue()` from inside `EditorWindow` code returns mouse coordinates in editor UI coordinates (Y down), and reading the position elsewhere returns it in game screen coordinates (Y up).

Internally, this translation is handled by an editor-specific Processor called `AutoWindowSpace`.
