using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.ActionsV2;
using UnityEngine.InputSystem.LowLevel;

public class CoreTests_ActionsV2 : InputTestFixture
{
    [Test]
    public void WhenControlIsActuated_ActionExecuteEventIsFired()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var binding = new Binding<float>("<Gamepad>/buttonSouth");
        var action = new InputAction<float, float>(ref binding);
        action.Enable();

        var callbackWasCalled = false;
        action.Executed += (ref CallbackContext<float, float> context) =>
        {
            callbackWasCalled = true;
            Assert.That(context.value, Is.EqualTo(1));
        };

        Set(gamepad.buttonSouth, 1);

        Assert.That(callbackWasCalled, Is.True);
    }

    [Test]
    public void WhenMultiTapIsDetected_InteractionHandlerFires()
    {
	    var gamepad = InputSystem.AddDevice<Gamepad>();

	    var action = new InputAction<float, float>(new Binding<float>("<Gamepad>/buttonSouth"));
	    action.AddInteraction(new MultiTapInteraction());
        action.Enable();

        var multiTapTriggered = false;
        action.GetInteraction<MultiTapInteraction>().Tapped += () =>
        {
	        multiTapTriggered = true;
        };

        PressAndRelease(gamepad.buttonSouth);
        PressAndRelease(gamepad.buttonSouth);

        Assert.That(multiTapTriggered, Is.True);
    }

    [Test]
    public void Vector2CompositeWorks()
    {
	    var keyboard = InputSystem.AddDevice<Keyboard>();

	    var action = new InputAction<Vector2, float>(new Vector2Composite(
		    new Binding<float>("<Keyboard>/w"),
		    new Binding<float>("<Keyboard>/s"),
		    new Binding<float>("<Keyboard>/a"),
		    new Binding<float>("<Keyboard>/d")));

        action.Enable();

        var callbackCalled = false;
        action.Executed += (ref CallbackContext<Vector2, float> context) =>
        {
	        callbackCalled = true;
            Assert.That(context.value, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        };

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
        InputSystem.Update();

        Assert.That(callbackCalled, Is.True);
    }
}
