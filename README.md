A new input system for Unity.

>NOTE: This system is still under active development and not yet stable. To run the project, you will need Unity 2019.1+.

Work-in-progress documentation can be found [here](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/InputSystem.md).

Issues are expected at this point. However, feel free to report what you find here on GitHub.

## How to Use This In Your Own Project
Please see [Installation](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/Installation.md).

The latest version is: `0.1.2-preview`.

Next version: `0.2-preview` (see [Roadmap](#roadmap)).

## Status

Rough assessment of current status:

- Feature Completeness: 80%
- Stability/Robustness: 40%
- Documentation: 10%

## Roadmap

At this point, we try to focus package releases on specific problem areas. The following is a tentative breakdown on the areas of focus we aim to address. This does not mean that the given area is necessarily fully finished in the given version but it will receive increased priority and attention at that point.

|Version|Focus|
|-------|-----|
|`0.2-preview`|- Actions<br>- PlayerInput<br>- XR bugs|
|`0.3-preview`|- Touch<br>- Documentation<br>- Move to 2019.1|
|`0.4-preview`|- Demo<br>- UI (Single- and Multi-Player)<br>- Documentation|
|`0.5-preview`|- Rebinding UI<br>- Debugger<br>- Bugs<br>- Documentation|
|`0.6-preview`|- Performance<br>- Fixed Update<br>- Bugs<br>- Documentation|
|`1.0-preview`|- Shipping quality release|
|`1.0`|- Verified, official Unity package<br>- Move to 2019.2|

## Timeline

The planned development timeline for the new input system is as follows:

- *Early November 2018*: Feature cut-off
- *November 2018 - March 2019*: Stabilization & documentation
- *January - March 2019*: Official beta
- *April 2019*: 1.0-preview release together with Unity 2019.1
- *Unity 2019.2*: "Verified" package status (i.e. full part of Unity proper)

Note that the existing input system in Unity (i.e. `UnityEngine.Input`) will be unaffected for now. The new input system is developed in parallel and presents a choice to the user to employ one or the other. Once the new input system has become both fully featured and fully stable, the old input system will likely be put on a path towards deprecation.

>Disclaimer: This is tentative. The usual disclaimer applies about these plans being subject to change according to whatever natural and unnatural disasters are on the menu of the day.
