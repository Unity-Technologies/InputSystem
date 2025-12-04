---
uid: input-system-use-in-editor
---
# Using Input in the Editor

Unlike Unity's old Input Manager, you can use the new Input System from within `EditorWindow` code as well. For example, you can gain access to pen pressure information like this:

```CSharp
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

This encompasses all code called from `OnGUI()` methods, which means that you can also use the Input System in property drawers, Inspectors, and other similar places.

> [!NOTE]
> Unity doesn't support Actions in Edit mode.

## Coordinate System

The coordinate system differs between `EditorWindow` code and `UnityEngine.Screen`. `EditorWindow` code has its origin in the upper-left corner, with Y down. `UnityEngine.Screen` has it in the bottom-left corner, with Y up.

The Input System compensates for that by automatically converting coordinates depending on whether you call it from your application or from Editor code. In other words, calling `Mouse.current.position.ReadValue()` from inside `EditorWindow` code returns mouse coordinates in Editor UI coordinates (Y down), and reading the position elsewhere returns it in application screen coordinates (Y up).

Internally, an editor-specific Processor called `AutoWindowSpace` handles this translation.
