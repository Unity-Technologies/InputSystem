# Use of Input System in Editor

Unlike Unity's old input system, the new input system can be used from within `EditorWindow` code as well. This can be used, for example, to gain access to pen pressure information.

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

Note that this encompases all code running in `OnGUI()` methods, not just the code running directly in EditorWindows. This means that the input system can also be used in property drawers, inspectors, and similar places.

## Limitations

- Actions are not supported in edit mode.

## Coordinate System

The coordinate system differs between `EditorWindow` code and `UnityEngine.Screen`. The former has the origin in the upper left corner with Y down whereas the latter has it in the bottom left corner with Y up.

The input system compensates for that by automatically converting coordinates depending on whether it is used from game or from editor code. In other words, calling `Mouse.current.position.ReadValue()` from inside `EditorWindow` code will return mouse coordinates in editor UI coordinates (Y down) whereas reading the position elsewhere will return it in game screen coordinates (Y up).

Internally, this translation is handled by an editor-specific processor called `AutoWindowSpace`.
