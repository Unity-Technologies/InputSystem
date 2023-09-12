using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An input device that has its orientation and position in space tracked.
    /// </summary>
    /// <seealso cref="UnityEngine.InputSystem.XR.XRController"/>
    /// <seealso cref="UnityEngine.InputSystem.XR.XRHMD"/>
    [InputControlLayout(displayName = "Tracked Device", isGenericTypeOfDevice = true)]
    public class TrackedDevice : InputDevice
    {
        [InputControl(synthetic = true)]
        public IntegerControl trackingState { get; protected set; }
        [InputControl(synthetic = true)]
        public ButtonControl isTracked { get; protected set; }
        [InputControl(noisy = true, dontReset = true)]
        public Vector3Control devicePosition { get; protected set; }
        [InputControl(noisy = true, dontReset = true)]
        public QuaternionControl deviceRotation { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
        }
    }
}
