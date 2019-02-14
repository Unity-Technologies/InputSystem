    ////WIP

# Use In Editor

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

- Actions are not supported in edit mode

## Coordinate System
