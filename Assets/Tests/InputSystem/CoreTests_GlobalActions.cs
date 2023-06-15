using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevel;
using UnityEngine.InputSystem.HighLevel.Editor;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Input = UnityEngine.InputSystem.HighLevel.Input;

internal partial class CoreTests
{
    private const string TestCategory = "GlobalActions";
    private string m_TemplateAssetPath;

    [SetUp]
    public override void Setup()
    {
        // this asset takes the place of InputManager.asset for the sake of testing, as we don't really want to go changing
        // that asset in every test.
        var testInputManager = ScriptableObject.CreateInstance<TestInputManager>();
        AssetDatabase.CreateAsset(testInputManager, "Assets/TestInputManager.asset");

        // create a template input action asset from which the input action asset stuffed inside the InputManager will be created
        var globalActions = ScriptableObject.CreateInstance<InputActionAsset>();
        globalActions.AddActionMap("ActionMapOne").AddAction("ActionOne");
        m_TemplateAssetPath = Path.Combine(Environment.CurrentDirectory, "Assets/TestGlobalActions.inputactions");
        File.WriteAllText(m_TemplateAssetPath, globalActions.ToJson());

        InputSystem.SetGlobalActionAssetPaths(m_TemplateAssetPath, "Assets/TestInputManager.asset");

        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
        if (File.Exists(m_TemplateAssetPath))
            File.Delete(m_TemplateAssetPath);

        AssetDatabase.DeleteAsset("Assets/TestInputManager.asset");

        base.TearDown();
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_TemplateAssetIsInstalledOnFirstUse()
    {
        var asset = GlobalActionsAsset.GetOrCreateGlobalActionsAsset("Assets/TestInputManager.asset", m_TemplateAssetPath);

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(1));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(1));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanQueryActionsByStringName()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputSystem.actions.FindAction("ActionOne").AddBinding("<keyboard>/w");

        Set(keyboard.wKey, 1);

        Assert.That(Input.IsControlDown("ActionOne"), Is.True);
        Assert.That(Input.IsControlPressed("ActionOne"), Is.True);

        Set(keyboard.wKey, 0);

        Assert.That(Input.IsControlPressed("ActionOne"), Is.False);
        Assert.That(Input.IsControlUp("ActionOne"), Is.True);

        InputSystem.Update();

        Assert.That(Input.IsControlUp("ActionOne"), Is.False);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_NoExceptionIsThrownWhenReadingTheValueOfAnIncompatibleControl()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // binding <keyboard>/a will return a float type
        var action = new InputAction(binding: "<keyboard>/a");

        // create the Input<T> as a Vector2 type which should be incompatible with the A key binding
        var input = new Input<Vector2>(action);
        Press(keyboard.aKey);

        Assert.DoesNotThrow(() =>
        {
            var actionValue = input.value;
        });

        // don't really care about the contents of the message, just that we received the warning
        LogAssert.Expect(LogType.Warning, new Regex("[a-zA-Z0-9]*"));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InputValueReturnsDefaultValueWithProcessorsApplied_WhenIncompatibleTypeIsRead()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // the normalize processor on this binding with a zero point of -1 should force the resting position of the
        // left stick to be 0.5
        var action = new InputAction(binding: "<gamepad>/leftStick", processors: "Normalize(min=-1, max=1, zero=-1)");

        var input = new Input<float>(action);
        Set(gamepad.leftStick, new Vector2(0.5f, 0.5f));

        // because the action last had input from the leftStick, that will be the active control, so reading
        // the value will attempt to do ReadValue<float> on that Vector2 control, which won't work, and should
        // fall back to ReadDefaultValue<>
        Assert.That(input.value, Is.EqualTo(0.5f));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanAddNewInteraction_ToAllBindingsOnActionAtOnce()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddBinding("<keyboard>/a");
        action.AddCompositeBinding("1DAxis")
            .With("negative", "<keyboard>/w")
            .With("positive", "<keyboard>/s");
        action.AddBinding("<keyboard>/b");


        var input = new Input<float>(action);
        input.AddInteraction<HoldInteraction>();

        var holdPerformed = false;
        input.action.performed += ctx =>
        {
            holdPerformed = true;
        };

        Press(keyboard.aKey, currentTime);
        currentTime += 0.5; // default hold time is 0.4
        InputSystem.Update();

        Assert.That(holdPerformed, Is.True);

        Release(keyboard.aKey);
        holdPerformed = false;
        Press(keyboard.wKey, currentTime);
        currentTime += 0.5;
        InputSystem.Update();

        Assert.That(holdPerformed, Is.True);

        Release(keyboard.wKey);
        holdPerformed = false;
        Press(keyboard.bKey, currentTime);
        currentTime += 0.5;
        InputSystem.Update();

        Assert.That(holdPerformed, Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanAddNewInteraction_ToSpecificBindingsOnAction()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var action = new InputAction();
        action.AddBinding("<keyboard>/a");
        action.AddCompositeBinding("1DAxis")
            .With("negative", "<keyboard>/w")
            .With("positive", "<keyboard>/s");
        action.AddBinding("<keyboard>/b");


        var input = new Input<float>(action);
        input.AddInteraction<HoldInteraction>(input.bindings[1]);

        var holdPerformed = false;
        input.action.performed += ctx =>
        {
            // when this callback fires from a binding that didn't have the interaction added to it, the
            // CallbackContext interaction should be null
            holdPerformed = ctx.interaction != null;
        };

        Press(keyboard.aKey, currentTime);
        currentTime += 0.5; // default hold time is 0.4
        InputSystem.Update();

        // make sure this didn't fire for the a key
        Assert.That(holdPerformed, Is.False);

        Release(keyboard.aKey);
        holdPerformed = false;
        Press(keyboard.wKey, currentTime);
        currentTime += 0.5;
        InputSystem.Update();

        Assert.That(holdPerformed, Is.True);

        Release(keyboard.wKey);
        holdPerformed = false;
        Press(keyboard.bKey, currentTime);
        currentTime += 0.5;
        InputSystem.Update();

        Assert.That(holdPerformed, Is.False);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanPollInteractionPhaseChanges()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap("ActionMapOne");
        var map = asset.AddActionMap("ActionMapTwo");
        var input = new Input<float>(map.AddAction("ActionOne", binding: "<keyboard>/a", interactions: "hold(duration=0.4)"));

        Press(keyboard.aKey, time: currentTime);

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(), Is.True);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(), Is.False);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(), Is.False);

        currentTime += 1;
        InputSystem.Update();

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(), Is.False);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(), Is.True);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(), Is.False);

        Release(keyboard.aKey, time: currentTime + 0.01);

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(), Is.False);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(), Is.False);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(), Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanPollInteractionPhaseChanges_WhenBindingIndexIsSpecified()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap("ActionMapOne").AddBinding("<keyboard>/x");
        var map = asset.AddActionMap("ActionMapTwo");
        var action = map.AddAction("ActionOne", binding: "<keyboard>/a", interactions: "hold(duration=0.4)");
        action.AddBinding("<keyboard>/s");

        asset.Enable();

        var input = new Input<float>(action);

        Press(keyboard.aKey, time: currentTime);

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(input.bindings[0]), Is.True);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(input.bindings[0]), Is.False);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(input.bindings[0]), Is.False);

        currentTime += 1;
        InputSystem.Update();

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(input.bindings[0]), Is.False);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(input.bindings[0]), Is.True);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(input.bindings[0]), Is.False);

        Release(keyboard.aKey, time: currentTime + 0.01);

        Assert.That(input.WasStartedThisFrame<HoldInteraction>(input.bindings[0]), Is.False);
        Assert.That(input.WasPerformedThisFrame<HoldInteraction>(input.bindings[0]), Is.False);
        Assert.That(input.WasCanceledThisFrame<HoldInteraction>(input.bindings[0]), Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_WasPerformedThisFrame_BindingIndexIgnoresCompositeParts()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var map = new InputActionMap();
        var actionOne = map.AddAction("ActionOne");
        actionOne.AddCompositeBinding("1DAxis")
            .With("negative", "<keyboard>/a")
            .With("positive", "<keyboard>/d");
        actionOne.AddBinding("<keyboard>/space");
        actionOne.AddCompositeBinding("2DVector", interactions: "tap(duration=0.2)")
            .With("up", "<keyboard>/w")
            .With("down", "<keyboard>/s")
            .With("left", "<keyboard>/a")
            .With("right", "<keyboard>/d");

        var input = new Input<Vector2>(actionOne);

        Press(keyboard.wKey, time: currentTime, queueEventOnly: true);
        Release(keyboard.wKey, time: currentTime + 0.1);

        Assert.That(input.WasPerformedThisFrame<TapInteraction>(input.bindings[2]), Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanRemoveInteraction()
    {
        var map = new InputActionMap();
        var action = map.AddAction("Action", binding: "<keyboard>/a", interactions: "hold,tap");

        var input = new Input<float>(action);
        input.RemoveInteraction<HoldInteraction>(input.bindings[0]);

        Assert.That(action.bindings[0].interactions, Is.EqualTo("tap"));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_CanSetInteractionParameter()
    {
        var map = new InputActionMap();
        var action = map.AddAction("Action", binding: "<keyboard>/a", interactions: "hold(duration=0.5)");

        var input = new Input<float>(action);
        input.SetInteractionParameter<HoldInteraction, float>(input.bindings[0], x => x.duration, 1.5f);

        var newValue = input.GetInteractionParameter<HoldInteraction, float>(input.bindings[0], x => x.duration);
        Assert.That(newValue, Is.EqualTo(1.5f));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_GetInteractionWorks()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap("ActionMapOne").AddBinding("<keyboard>/x");
        var map = asset.AddActionMap("ActionMapTwo");
        var action = map.AddAction("ActionOne", binding: "<keyboard>/a", interactions: "hold");
        action.AddBinding("<keyboard>/b", interactions: "tap");
        var input = new Input<float>(action);

        Assert.That(input.GetInteraction<HoldInteraction>(input.bindings[0]).isValid, Is.True);
        Assert.That(input.GetInteraction<TapInteraction>(input.bindings[0]).isValid, Is.False);

        Assert.That(input.GetInteraction<HoldInteraction>(input.bindings[1]).isValid, Is.False);
        Assert.That(input.GetInteraction<TapInteraction>(input.bindings[1]).isValid, Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InteractionIsValid_WhenNoBindingIndexIsSpecified()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap("ActionMapOne").AddBinding("<keyboard>/x");
        var map = asset.AddActionMap("ActionMapTwo");
        var action = map.AddAction("ActionOne", binding: "<keyboard>/a", interactions: "hold");
        action.AddBinding("<keyboard>/b", interactions: "tap");
        var input = new Input<float>(action);

        Assert.That(input.GetInteraction<HoldInteraction>(BindingIndex.None).isValid, Is.True);
        Assert.That(input.GetInteraction<TapInteraction>(BindingIndex.None).isValid, Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InteractionModificationsSurviveFullBindingReResolution()
    {
        var map = new InputActionMap();
        var action = map.AddAction("Action", binding: "<keyboard>/a", interactions: "hold(duration=0.5)");

        var input = new Input<float>(action);
        input.SetInteractionParameter<HoldInteraction, float>(input.bindings[0], x => x.duration, 1.5f);

        // adding a binding causes a full binding resolution
        action.AddBinding("<Gamepad>/rightTrigger");

        Assert.That(input.GetInteractionParameter<HoldInteraction, float>(input.bindings[0], x => x.duration), Is.EqualTo(1.5f));
    }

    [Test]
    [Category(TestCategory)]
    [TestCase(InputBindingsCollection.EnumerationBehaviour.SkipCompositeParts)]
    [TestCase(InputBindingsCollection.EnumerationBehaviour.IncludeCompositeParts)]
    public void GlobalActions_InputBindingsCollection_CanIterateDependingOnMode(InputBindingsCollection.EnumerationBehaviour enumerationBehaviour)
    {
        var action = new InputAction(binding: "<keyboard>/w");
        action.AddBinding("<keyboard>/a");
        action.AddCompositeBinding("OneModifier")
            .With("modifier", "<keyboard>/leftShift")
            .With("binding", "<keyboard>/a");
        action.AddBinding("<gamepad>/leftStick");
        action.AddCompositeBinding("2DVector")
            .With("up", "<keyboard>/w")
            .With("down", "<keyboard>/s")
            .With("left", "<keyboard>/a")
            .With("right", "<keyboard>/d");

        var collection = new InputBindingsCollection(action, enumerationBehaviour);

        var bindings = action.bindings;
        if (enumerationBehaviour == InputBindingsCollection.EnumerationBehaviour.SkipCompositeParts)
        {
            Assert.That(collection.Select(b => b.binding),
                Is.EqualTo(new[] { bindings[0], bindings[1], bindings[2], bindings[5], bindings[6] }));
        }
        else
        {
            Assert.That(collection.Select(b => b.binding), Is.EqualTo(bindings));
        }
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InputBindingsCollection_CanGetBindingsWithControlScheme()
    {
        var action = new InputAction(binding: "<keyboard>/w");
        action.AddBinding("<keyboard>/a").WithGroup("Gamepad");
        action.AddCompositeBinding("OneModifier")
            .With("modifier", "<keyboard>/leftShift")
            .With("binding", "<keyboard>/a");
        action.AddBinding("<gamepad>/leftStick").WithGroup("Gamepad");

        var bindingCollection = new InputBindingsCollection(action, InputBindingsCollection.EnumerationBehaviour.SkipCompositeParts);
        Assert.That(bindingCollection.WithControlScheme("Gamepad").Select(b => b.binding),
            Is.EqualTo(new[] { action.bindings[1], action.bindings[5] }));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InputBindingsCollection_CanUseEnumerator()
    {
        var action = new InputAction();
        action.AddBinding("<keyboard>/a");
        action.AddCompositeBinding("1DAxis")
            .With("negative", "<keyboard>/w")
            .With("positive", "<keyboard>/s");
        action.AddBinding("<keyboard>/b");

        var input = new Input<float>(action);

        var count = 0;
        foreach (var inputBinding in input.bindings)
        {
            count++;
        }

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    [Category(TestCategory)]
    public void GlobalActions_InputBindingsCollection_EnumeratorThrowsIfBindingsCollectionChanges()
    {
        var action = new InputAction();
        action.AddBinding("<keyboard>/a");
        action.AddCompositeBinding("1DAxis")
            .With("negative", "<keyboard>/w")
            .With("positive", "<keyboard>/s");
        action.AddBinding("<keyboard>/b");

        var input = new Input<float>(action);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var inputBinding in input.bindings)
            {
                action.AddBinding("<keyboard>/x");
            }
        });
    }

    [Test]
    [Category(TestCategory)]
    [TestCase(InputBindingsCollection.EnumerationBehaviour.SkipCompositeParts)]
    [TestCase(InputBindingsCollection.EnumerationBehaviour.IncludeCompositeParts)]
    public void GlobalActions_InputBindingsCollection_ReturnsBindingIndexesWithCorrectType(InputBindingsCollection.EnumerationBehaviour enumerationBehaviour)
    {
        var action = new InputAction(binding: "<keyboard>/w");
        action.AddBinding("<keyboard>/a");

        var collection = new InputBindingsCollection(action, enumerationBehaviour);

        if (enumerationBehaviour == InputBindingsCollection.EnumerationBehaviour.IncludeCompositeParts)
            Assert.That(collection[0].index.type, Is.EqualTo(BindingIndex.IndexType.IncludeCompositeParts));
        else
            Assert.That(collection[0].index.type, Is.EqualTo(BindingIndex.IndexType.SkipCompositeParts));
    }
}
