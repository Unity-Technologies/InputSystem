
A new input system.

>NOTE: This system is still under active development and not yet stable. To run the project, you will need Unity 2018.2+.

Some [videos](https://www.youtube.com/playlist?list=PLXbAKDQVwztY0hyyeEy9gifk-ffkgoy_Y).

>DISCLAIMER: The videos are fairly outdated by now.

More info in the [wiki](https://github.com/Unity-Technologies/InputSystem/wiki) (varying degrees of up-to-dateness).

Issues are expected at this point. However, feel free to report what you find here on GitHub.

## How to Use This In Your Own Project

1. Copy the `Packages/com.unity.inputsystem/` folder to the `Packages` folder of your own project.
2. Open the player settings in the editor (`Edit >> Project Settings >> Player`) and change `Active Input Handling*` in the 'Configuration' section to either "Both" or "Input System (Preview)". Note that the latter will disable support for the old system and thus render most APIs in `UnityEngine.Input` non-functional. This will also have impact on systems using the API (e.g. `UnityEngine.UI`).
3. Restart the editor.

When you open the input debugger now (`Window >> Input Debugger`), you should see the available local devices listed in the debug view.

