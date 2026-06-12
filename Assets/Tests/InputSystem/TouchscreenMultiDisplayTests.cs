using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Tests covering touch tracking across multiple physical touchscreen monitors.
// Regression coverage for IN-108611: touches from one screen incorrectly matching
// ongoing touches from another screen when both screens report the same touchId.
[TestFixture]
internal class TouchscreenMultiDisplayTests : CoreTestsFixture
{
    // When two physical touchscreens both have an active touch with the same touchId,
    // a Move event from screen 2 must update screen 2's touch slot, not screen 1's.
    //
    // Failure mode (pre-fix): OnStateEvent matches ongoing touches by touchId alone,
    // so screen 2's Move event finds screen 1's slot first (both have touchId=1) and
    // incorrectly updates it, leaving screen 1's position changed and screen 2 stale.
    [Test]
    [Category("Devices")]
    public void Devices_TouchMoveOnSecondDisplay_DoesNotUpdateTouchOnFirstDisplay()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Finger down on display 0 (touchId=1).
        InputSystem.QueueStateEvent(device, new TouchState
        {
            phase = TouchPhase.Began,
            touchId = 1,
            position = new Vector2(100, 100),
            displayIndex = 0,
        });

        // Finger down on display 1 — same touchId, different screen.
        InputSystem.QueueStateEvent(device, new TouchState
        {
            phase = TouchPhase.Began,
            touchId = 1,
            position = new Vector2(200, 200),
            displayIndex = 1,
        });

        InputSystem.Update();

        // Both touches should be allocated to separate slots.
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[1].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));

        var display0SlotIndex = -1;
        var display1SlotIndex = -1;
        for (var i = 0; i < 2; i++)
        {
            var displayIdx = device.touches[i].displayIndex.ReadValue();
            if (displayIdx == 0) display0SlotIndex = i;
            else if (displayIdx == 1) display1SlotIndex = i;
        }

        Assert.That(display0SlotIndex, Is.Not.EqualTo(-1), "No touch slot found for display 0");
        Assert.That(display1SlotIndex, Is.Not.EqualTo(-1), "No touch slot found for display 1");

        var display0PositionBefore = device.touches[display0SlotIndex].position.ReadValue();

        // Swipe on display 1 (same touchId=1 as display 0's held touch).
        InputSystem.QueueStateEvent(device, new TouchState
        {
            phase = TouchPhase.Moved,
            touchId = 1,
            position = new Vector2(300, 300),
            displayIndex = 1,
        });

        InputSystem.Update();

        // Display 0's touch must be unchanged.
        Assert.That(device.touches[display0SlotIndex].position.ReadValue(), Is.EqualTo(display0PositionBefore),
            "Touch on display 0 was incorrectly updated by a Move event from display 1");

        // Display 1's touch must reflect the new position.
        Assert.That(device.touches[display1SlotIndex].position.ReadValue(), Is.EqualTo(new Vector2(300, 300)),
            "Touch on display 1 was not updated by its own Move event");

        Assert.That(device.touches[display1SlotIndex].phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));
    }
}
