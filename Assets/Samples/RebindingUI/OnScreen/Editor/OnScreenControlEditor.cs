// This should be in an editor only assembly.

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    //[CustomEditor(typeof(OnScreenControlUI))]
    public class OnScreenControlEditor : UnityEditor.Editor
    {
        #region Unity Editor Menu Extensions

        private const int Priority = 10;
        private const int RigPriority = 21; // Note: Diff > 10 inserts separator
        private const string PrimaryStickControlPath = "<Gamepad>/leftStick";
        private const string SecondaryStickControlPath = "<Gamepad>/rightStick";
        private const string PrimaryButtonControlPath = "<Gamepad>/buttonSouth";
        private const string SecondaryButtonControlPath = "<Gamepad>/buttonEast";
        private const string Menu = "GameObject/Input System/";

        private enum UIIntegration
        {
            None = 0,
            UGUI = 1,
            UIElements = 2,
        }

        [MenuItem(Menu + "On-Screen Button", false, Priority)]
        private static void CreateOnScreenButton(MenuCommand menuCommand)
        {
            FinalizeGameObject(CreateButton(PrimaryButtonControlPath, UIIntegration.None), menuCommand.context);
        }

        [MenuItem(Menu + "On-Screen Button (UI)", false, Priority)]
        private static void CreateOnScreenButtonUI(MenuCommand menuCommand)
        {
            FinalizeGameObject(CreateButton(PrimaryButtonControlPath, UIIntegration.UGUI), menuCommand.context);
        }

        [MenuItem(Menu + "On-Screen Stick", false, Priority)]
        private static void CreateOnScreenStick(MenuCommand menuCommand)
        {
            var go = CreateStick(PrimaryStickControlPath, UIIntegration.None);
            FinalizeGameObject(go, menuCommand.context);
        }

        [MenuItem(Menu + "On-Screen Gamepad (1 Stick, 2 Buttons)", isValidateFunction: false, RigPriority)]
        private static void CreateOnScreenGamepad1Stick2Buttons(MenuCommand menuCommand)
        {
            var go = CreateGamepad1Stick2Buttons(UIIntegration.None);
            FinalizeGameObject(go: go, menuCommand.context);
        }

        [MenuItem(Menu + "On-Screen Gamepad (2 Sticks, 2 Buttons)", isValidateFunction: false, RigPriority)]
        private static void CreateOnScreenGamepad2Sticks2Buttons(MenuCommand menuCommand)
        {
            var go = CreateGamepad2Stick2Buttons(UIIntegration.None);
            FinalizeGameObject(go, menuCommand.context);
        }

        private static void FinalizeGameObject(GameObject go, Object parent)
        {
            // Ensure it gets parented correctly if a context object was selected
            GameObjectUtility.SetParentAndAlign(go, parent as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");

            // Select the newly created object
            Selection.activeObject = go;
        }

        private static GameObject CreateStick(string controlPath, UIIntegration uiIntegration, string name = "OnScreenStick")
        {
            // There is currently no difference between stick and button apart from path.
            return CreateButton(controlPath, uiIntegration, name);
        }

        private static GameObject CreateButton(string controlPath, UIIntegration uiIntegration, string name = "OnScreenButton")
        {
            var go = new GameObject(name: name);

            var control = go.AddComponent<CustomOnScreenControl>();
            control.curve = Curve.Linear;
            control.stickRadiusMillimeters = 7.2f;
            control.bounds = new Rect(0.0f, 0.5f, 0.5f, 0.0f);
            control.controlPath = controlPath;

            switch (uiIntegration)
            {
                case UIIntegration.UGUI:
                {
                    // When used with UGUI we want the on-screen control to be a canvas object
                    go.AddComponent<RectTransform>();

                    var uiButtonGo = new GameObject("Button");
                    var rectTransform = uiButtonGo.AddComponent<RectTransform>();
                    var uiButton = uiButtonGo.AddComponent<RawImage>();
                    uiButton.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    uiButton.raycastTarget = false;
                    uiButton.transform.parent = go.transform;

                    var controlUI = go.AddComponent<OnScreenControlUI>();
                    controlUI.control = control;
                    controlUI.bounds = rectTransform;
                }
                break;
                case UIIntegration.UIElements:
                    throw new NotImplementedException("UIElements support not yet implemented");
                    break;
                case UIIntegration.None:
                default:
                    break;
            }

            return go;
        }

        private static GameObject CreateGamepad1Stick2Buttons(UIIntegration uiIntegration)
        {
            var gamepad = new GameObject(MakeName("Gamepad 1-Stick 2-Buttons", uiIntegration));

            var stick = CreateStick(PrimaryStickControlPath, uiIntegration);
            stick.transform.SetParent(gamepad.transform);

            var primaryButton = CreateButton(PrimaryButtonControlPath, uiIntegration, "PrimaryButton");
            primaryButton.transform.SetParent(gamepad.transform);

            var secondaryButton = CreateButton(SecondaryButtonControlPath, uiIntegration, "SecondaryButton");
            secondaryButton.transform.SetParent(gamepad.transform);

            return gamepad;
        }

        private static GameObject CreateGamepad2Stick2Buttons(UIIntegration uiIntegration)
        {
            var gamepad = new GameObject(MakeName("Gamepad 2-Sticks 2-Buttons", uiIntegration));

            var primaryStick = CreateStick(PrimaryStickControlPath, uiIntegration, "Primary Stick");
            primaryStick.transform.SetParent(gamepad.transform);

            var secondaryStick = CreateStick(SecondaryStickControlPath, uiIntegration, "Secondary Stick");
            secondaryStick.transform.SetParent(gamepad.transform);

            var primaryButton = CreateButton(PrimaryButtonControlPath, uiIntegration, "PrimaryButton");
            primaryButton.transform.SetParent(gamepad.transform);

            var secondaryButton = CreateButton(SecondaryButtonControlPath, uiIntegration, "SecondaryButton");
            secondaryButton.transform.SetParent(gamepad.transform);

            return gamepad;
        }

        private static string MakeName(string name, UIIntegration uiIntegration)
        {
            switch (uiIntegration)
            {
                case UIIntegration.UGUI: return name + " (UI)";
                case UIIntegration.None: return name + " (UI Elements)";
                default: return name;
            }
        }

        #endregion
    }
}

#endif // UNITY_EDITOR
