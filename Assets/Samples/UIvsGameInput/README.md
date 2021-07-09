# UI vs Game Input

>NOTE: More information related to ambiguities between UI and game input may be found [here in the documentation](........).

When interactive UI elements are overlaid over a game view, ambiguity may arise for inputs.

If, for example, there is a `UI.Button` on screen that can be clicked/tapped, while clicking/tapping on the scene itself also has associated functionality, clicking on the UI button should not also trigger the corresponding action on the scene.

This sample demonstrates how to handle input in such a situation.

## The Sample Scene

![PICTURE]

The sample scene has a UI button in each of the corners of the screen. When clicking on any of the buttons, the "status bar" text along the bottom edge of the screen changes. Additionally, the camera

There are two ways to control the game:

1. "Pointer", i.e. mouse or touch input (optionally combined with keyboard input), and
2. "Navigation", i.e.

### Mouse/Touch/Keyboard Input

### Gamepad/Joystick Input


The sample scene has a UI button in each of the corners of the screen. Clicking/tapping any of the buttons will result in text being displayed along the bottom edge of the screen.

Additionally, by pressing down within the screen but outside any of the buttons, camera control is engaged and the camera is rotated by dragging horizontally and/or vertically. Upon depressing, camera control is relinquished.

Also, a double-tap on the screen will reset the camera to its initial rotation.

Finally, when the escape key is pressed on the keyboard, it brings up a menu which disables in-game actions while active.

## How It Works

### Pointer Input

### Navigation Input

[ ] Make it possible to just query the UI with a 2D position.
