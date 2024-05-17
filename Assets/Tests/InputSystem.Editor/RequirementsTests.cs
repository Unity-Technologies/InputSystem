using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;

namespace Tests.InputSystem.Editor
{
    class RequirementsTests
    {
        private const string kTestOwner = "TestOwner";
        private const string kImplication = "Bad things would happen";

        [Test]
        public void InputActionAssetRequirements_ConstructorShouldThrow_IfOwnerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new InputActionAssetRequirements(null, new InputActionRequirement[] {}, null, kImplication));
        }

        [Test]
        public void InputActionAssetRequirements_ConstructorShouldThrow_IfImplicationIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {}, null, null));
        }

        [Test]
        public void InputActionAssetRequirementsRegisterUnregister_ShouldRegisterAndUnregisterRequirementsAndReturnTrue_IfValidRequirements()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {},
                null, kImplication);

            try
            {
                Assert.That(InputActionAssetRequirements.Register(requirements), Is.True);
            }
            finally
            {
                Assert.That(InputActionAssetRequirements.Unregister(requirements), Is.True);
            }
        }

        [Test]
        public void InputActionAssetRequirementsRegister_ShouldFailAndLogError_IfRequirementsHaveAlreadyBeenRegistered()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {},
                null, kImplication);

            try
            {
                InputActionAssetRequirements.Register(requirements);
                LogAssert.Expect(LogType.Error, new Regex("^Failed to register requirements for \"TestOwner\""));
                Assert.That(InputActionAssetRequirements.Register(requirements), Is.False);
            }
            finally
            {
                Assert.That(InputActionAssetRequirements.Unregister(requirements), Is.True);
            }
        }

        [Test]
        public void InputActionAssetRequirementsRegister_ShouldThrow_IfGivenNullReference()
        {
            Assert.Throws<ArgumentNullException>(() => InputActionAssetRequirements.Register(null));
        }

        [Test]
        public void InputActionAssetRequirementsUnregister_ShouldThrow_IfGivenNullReference()
        {
            Assert.Throws<ArgumentNullException>(() => InputActionAssetRequirements.Unregister(null));
        }

        [Test]
        public void InputActionAssetRequirementsUnregister_ShouldFail_IfRequirementsHaveNotPreviouslyBeenRegistered()
        {
            LogAssert.Expect(LogType.Error, new Regex("^Failed to unregister requirements for \"TestOwner\""));
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {},
                null, kImplication);
            Assert.That(InputActionAssetRequirements.Unregister(requirements), Is.False);
        }

        [Test]
        public void InputActionAssetRequirements_Owner_ShouldReflectOwnerPassedToConstructor()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {},
                null, kImplication);
            Assert.That(requirements.owner, Is.EqualTo(kTestOwner));
        }

        [Test]
        public void InputActionAssetRequirements_ConstructorShouldThrow_IfOwnerIsNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new InputActionAssetRequirements(
                null, new InputActionRequirement[] {}, null, kImplication));
            Assert.Throws<ArgumentException>(() => new InputActionAssetRequirements(
                string.Empty, new InputActionRequirement[] {}, null, kImplication));
        }

        [Test]
        public void InputActionAssetRequirements_Implication_ShouldReflectImplicationPassedToConstructor()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {},
                null, kImplication);
            Assert.That(requirements.implication, Is.EqualTo(kImplication));
        }

        [Test]
        public void InputActionAssetRequirements_ConstructorShouldThrow_IfImplicationIsNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new InputActionAssetRequirements(
                kTestOwner, new InputActionRequirement[] {}, null, null));
            Assert.Throws<ArgumentException>(() => new InputActionAssetRequirements(
                kTestOwner, new InputActionRequirement[] {}, null, string.Empty));
        }

        [Test]
        public void InputActionAssetRequirements_RequirementsShouldReturnEmptyList_IfConstructedWithNullRequirements()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, null, null, kImplication);
            Assert.That(requirements.requirements, Is.Not.Null);
            Assert.That(requirements.requirements.Count, Is.EqualTo(0));
        }

        [Test]
        public void InputActionAssetRequirements_RequirementsShouldReturnEmptyList_IfConstructedWithEmptyRequirements()
        {
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[] {}, null, kImplication);
            Assert.That(requirements.requirements, Is.Not.Null);
            Assert.That(requirements.requirements.Count, Is.EqualTo(0));
        }

        [Test]
        public void InputActionAssetRequirements_RequirementsShouldReflectListPassedToConstructorAndBeUnaffected_IfContainerIsModifiedFromOutsideAfterCreation()
        {
            var firstRequirement =
                new InputActionRequirement("Map/First", InputActionType.Button, "Button", "No buttons for you");
            var secondRequirement =
                new InputActionRequirement("Map/Second", InputActionType.Value, "Vector2", "No vectors for you");
            var thirdRequirement =
                new InputActionRequirement("Map/Third", InputActionType.PassThrough, String.Empty, "No thing for you");

            var list = new List<InputActionRequirement>();
            list.Add(firstRequirement);
            list.Add(secondRequirement);
            list.Add(thirdRequirement);

            var requirements = new InputActionAssetRequirements(kTestOwner, list, null, kImplication);

            list.Remove(secondRequirement);
            Assert.That(requirements.requirements.Count, Is.EqualTo(3));
            Assert.That(requirements.requirements.Contains(firstRequirement));
            Assert.That(requirements.requirements.Contains(secondRequirement));
            Assert.That(requirements.requirements.Contains(thirdRequirement));
        }

        [Test]
        public void InputActionAssetRequirements_ResolversShouldReflectListPassedToConstructorAndBeUnaffected_IfContainerIsModifiedFromOutsideAfterCreation()
        {
            var resolver = new InputActionAssetRequirementFailureResolver("r1", (_) => {}, "abc");
            var list = new List<InputActionAssetRequirementFailureResolver>();
            list.Add(resolver);

            var requirements = new InputActionAssetRequirements(kTestOwner, null, list, kImplication);

            list.Clear();
            Assert.That(requirements.resolvers.Count, Is.EqualTo(1));
            Assert.That(requirements.resolvers.Contains(resolver));
        }

        [Test]
        public void InputActionAssetRequirements_XXXX()
        {
            var firstRequirement =
                new InputActionRequirement("Map/First", InputActionType.Button, "Button", "No buttons for you");
            var secondRequirement =
                new InputActionRequirement("Map/Second", InputActionType.Value, "Vector2", "No vectors for you");
            var thirdRequirement =
                new InputActionRequirement("Map/Third", InputActionType.PassThrough, String.Empty, "No thing for you");
            var requirements = new InputActionAssetRequirements(kTestOwner, new InputActionRequirement[]
                { firstRequirement, secondRequirement, thirdRequirement },
                null, kImplication);

            var verifier = new InputActionAssetRequirementVerifier(requirements);
            using (var asset = Scoped.Object(ScriptableObject.CreateInstance<InputActionAsset>()))
            {
                asset.value.name = "MyAsset";

                var result = verifier.Verify(asset.value);
                Assert.That(result.hasFailures, Is.True);
                Assert.That(result.failures.Count, Is.EqualTo(6));
                Assert.That(result.parts.Count, Is.EqualTo(1));

                // TODO Assert failures

                // TODO Partially construct to remedy failures

                // TODO Assert new failures

                // TODO Fix all issues

                // TODO Assert no failures
            }
        }

        // TODO Test resolver

        private static object[] FormattingTestCases =
        {
            new object[]
            {
                InputActionAssetRequirementFailure.Reason.InputActionMapDoNotExist,
                new InputActionRequirement(actionPath: "Map/Action1",
                    actionType: InputActionType.PassThrough,
                    expectedControlType: String.Empty,
                    implication: kImplication),
                null,
                $"Required InputActionMap with path 'Map' in asset \"MyAsset\" could not be found. {kImplication}.",
                "Required InputActionMap with path 'Map' in asset \"MyAsset\" could not be found.",
                $"Required InputActionMap with path 'Map' could not be found. {kImplication}.",
                "Required InputActionMap with path 'Map' could not be found.",
            },
            new object[]
            {
                InputActionAssetRequirementFailure.Reason.InputActionDoNotExist,
                new InputActionRequirement(actionPath: "Map/Action1",
                    actionType: InputActionType.PassThrough,
                    expectedControlType: String.Empty,
                    implication: kImplication),
                null,
                $"Required InputAction with path 'Map/Action1' in asset \"MyAsset\" could not be found. {kImplication}.",
                "Required InputAction with path 'Map/Action1' in asset \"MyAsset\" could not be found.",
                $"Required InputAction with path 'Map/Action1' could not be found. {kImplication}.",
                "Required InputAction with path 'Map/Action1' could not be found.",
            },
            new object[]
            {
                InputActionAssetRequirementFailure.Reason.InputActionNotBound,
                new InputActionRequirement(actionPath: "Map/Action1",
                    actionType: InputActionType.PassThrough,
                    expectedControlType: String.Empty,
                    implication: kImplication),
                new InputAction("MyAction"), // TODO Odd to pass action
                $"Required InputAction with path 'Map/Action1' in asset \"MyAsset\" do not have any configured bindings. {kImplication}.",
                "Required InputAction with path 'Map/Action1' in asset \"MyAsset\" do not have any configured bindings.",
                $"Required InputAction with path 'Map/Action1' do not have any configured bindings. {kImplication}.",
                "Required InputAction with path 'Map/Action1' do not have any configured bindings."
            },
            new object[]
            {
                InputActionAssetRequirementFailure.Reason.InputActionInputActionTypeMismatch,
                new InputActionRequirement(actionPath: "Map/Action1",
                    actionType: InputActionType.PassThrough,
                    expectedControlType: String.Empty,
                    implication: kImplication),
                new InputAction("MyAction", type: InputActionType.Button), // TODO Odd to pass action
                $"Required InputAction with path 'Map/Action1' in asset \"MyAsset\" has 'type' set to 'InputActionType.Button', but 'InputActionType.PassThrough' was expected. {kImplication}.",
                "Required InputAction with path 'Map/Action1' in asset \"MyAsset\" has 'type' set to 'InputActionType.Button', but 'InputActionType.PassThrough' was expected.",
                $"Required InputAction with path 'Map/Action1' has 'type' set to 'InputActionType.Button', but 'InputActionType.PassThrough' was expected. {kImplication}.",
                "Required InputAction with path 'Map/Action1' has 'type' set to 'InputActionType.Button', but 'InputActionType.PassThrough' was expected."
            }
        };

        [TestCaseSource(nameof(FormattingTestCases))]
        public void DefaultInputActionRequirementFailureFormatter_ShouldFormatFailureToString_IfGivenValidFailure(
            InputActionAssetRequirementFailure.Reason reason, InputActionRequirement requirement, InputAction actual,
            string expectedMessage, string expectedMessageNoImplication, string expectedMessageNoAssetReference,
            string expectedMessageNoImplicationNoAssetReference)
        {
            using (var asset = Scoped.Object(ScriptableObject.CreateInstance<InputActionAsset>()))
            {
                asset.value.name = "MyAsset";

                {
                    var formatter = new DefaultInputActionRequirementFailureFormatter();
                    var value = formatter.Format(new InputActionAssetRequirementFailure(
                        asset: asset.value, reason: reason, requirement: requirement, actual: actual));
                    Assert.That(value, Is.EqualTo(expectedMessage));
                }
                {
                    var formatter = new DefaultInputActionRequirementFailureFormatter(includeImplication: false);
                    var value = formatter.Format(new InputActionAssetRequirementFailure(
                        asset: asset.value, reason: reason, requirement: requirement, actual: actual));
                    Assert.That(value, Is.EqualTo(expectedMessageNoImplication));
                }
                {
                    var formatter = new DefaultInputActionRequirementFailureFormatter(includeAssetReference: false);
                    var value = formatter.Format(new InputActionAssetRequirementFailure(
                        asset: asset.value, reason: reason, requirement: requirement, actual: actual));
                    Assert.That(value, Is.EqualTo(expectedMessageNoAssetReference));
                }
                {
                    var formatter = new DefaultInputActionRequirementFailureFormatter(includeAssetReference: false,
                        includeImplication: false);
                    var value = formatter.Format(new InputActionAssetRequirementFailure(
                        asset: asset.value, reason: reason, requirement: requirement, actual: actual));
                    Assert.That(value, Is.EqualTo(expectedMessageNoImplicationNoAssetReference));
                }
            }
        }
    }
}
