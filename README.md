
A new input system for Unity.

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

## Status

Rough assessment of current status:

- Feature Completeness: 80%
- Stability/Robustness: 40%
- Documentation: 10%

## Release Timeline

The planned development timeline for the new input system is as follows:

- *Early November 2018*: Feature cut-off
- *November 2018 - March 2019*: Stabilization & documentation
- *January - March 2019*: Official beta
- *March 2019*: 1.0-preview release together with Unity 2019.1
- *Unity 2019.2*: "Verified" package status (i.e. full part of Unity proper)

Note that the existing input system in Unity (i.e. `UnityEngine.Input`) will be unaffected for now. The new input system is developed in parallel and presents a choice to the user to employ one or the other. Once the new input system has become both fully featured and fully stable, the old input system will likely be put on a path towards deprecation.

>Disclaimer: This is tentative. The usual disclaimer applies about these plans being subject to change according to whatever natural and unnatural disasters are on the menu of the day.
