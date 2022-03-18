// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.Editor;
//
// public class GlobalInputActionsEditorTests
// {
//     [Test]
//     public void WhenInputActionMapSelected_ListOfActionsUpdates()
//     {
//         var inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
//         var actionMap1 = inputActionAsset.AddActionMap("ActionMap1");
//         var actionMap2 = inputActionAsset.AddActionMap("ActionMap2");
//         actionMap2.AddAction("TestAction1");
//         actionMap2.AddAction("TestAction2");
//
//         var changedMethodCalled = false;
//         InputActionMap selectedActionMap = null;
//
//         var viewModel = new InputActionAssetViewModel(inputActionAsset);
//         viewModel.selectedActionMap.Changed += actionMap =>
//         {
//             changedMethodCalled = true;
//             selectedActionMap = actionMap;
//         };
//
//         viewModel.SelectActionMap("ActionMap2");
//
//         Assert.That(changedMethodCalled, Is.True);
//         Assert.That(selectedActionMap, Is.EqualTo(actionMap2));
//     }
// }
//
// public class InputActionAssetViewModelTests
// {
//     [Test]
//     public void WhenActionSelected_ViewModelBuildsListOfAllBindings()
//     {
//         var asset = ScriptableObject.CreateInstance<InputActionAsset>();
//         var map1 = asset.AddActionMap("Map1");
//         map1.AddAction("Jump", binding: "<Gamepad>/buttonSouth");
//         var moveAction = map1.AddAction("Move");
//         moveAction
//             .AddCompositeBinding("WSAD")
//             .With("Up", "<Keyboard>/w")
//             .With("Down", "<Keyboard>/s")
//             .With("Left", "<Keyboard>/a")
//             .With("Right", "<Keyboard>/d");
//         var leftStickBinding = moveAction.AddBinding("<Gamepad>/leftStick");
//
//         var viewModel = new InputActionAssetViewModel(asset);
//         viewModel.SelectActionMap("Map1");
//         viewModel.SelectAction("Move");
//
//         var expectedBindings = new List<BindingViewItem>
//         {
//             new() {name = "WSAD", isComposite = true},
//             new() {name = "Left Stick [Gamepad]"}
//         };
//         Assert.That(viewModel.bindingsForSelectedAction.value, Is.EquivalentTo(expectedBindings));
//     }
// }
