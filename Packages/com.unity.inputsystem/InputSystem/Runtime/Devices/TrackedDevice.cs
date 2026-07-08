using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An input device that has its orientation and position in space tracked.
    /// </summary>
    /// <remarks>
    /// These values are typically read from input actions and fed into the
    /// [Tracked Pose Driver](xref:input-system-tracked-input-devices#tracked-pose-driver)
    /// component rather than being read directly from this class.
    ///
    /// Refer to the [Starter Assets](xref:xri-samples-starter-assets)
    /// sample in the XR Interaction Toolkit package for a Demo Scene with an XR rig
    /// hierarchy that uses these concepts.
    /// </remarks>
    /// <seealso cref="UnityEngine.InputSystem.XR.XRController"/>
    /// <seealso cref="UnityEngine.InputSystem.XR.XRHMD"/>
    /// <seealso href="https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest/index.html?subfolder=/api/UnityEngine.XR.OpenXR.Input.Pose.html">UnityEngine.XR.OpenXR.Input.Pose</seealso>
    [InputControlLayout(displayName = "Tracked Device", isGenericTypeOfDevice = true)]
    public class TrackedDevice : InputDevice
    {
        /// <summary>
        /// Indicates which of the tracked pose components are valid by using an integer containing a
        /// bitwise OR of the [Unity XR module enum values](https://docs.unity3d.com/ScriptReference/XR.InputTrackingState.html),
        /// for example `InputTrackingState.Position | InputTrackingState.Rotation`.
        /// </summary>
        /// <remarks>
        /// This property determines whether you can retrieve valid values from the
        /// <see cref="devicePosition"/> and the <see cref="deviceRotation"/> properties:
        /// - The Position bit must be set for the <see cref="devicePosition"/> property to have a valid Vector3 value.
        /// - The Rotation bit must be set for the <see cref="deviceRotation"/> property to have a valid Quaternion.
        /// </remarks>
        [InputControl(synthetic = true)]
        public IntegerControl trackingState { get; protected set; }

        /// <summary>
        /// Indicates whether the input device is actively tracked (1) or not (0).
        /// </summary>
        /// <remarks>
        /// For more information about how OpenXR represents inferred position vs. actual position, refer to
        /// [Reference Spaces](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#spaces-reference-spaces)
        /// (OpenXR Specification).
        /// </remarks>
        [InputControl(synthetic = true)]
        public ButtonControl isTracked { get; protected set; }

        /// <summary>
        /// Represents the position portion of the input device's primary
        /// [pose](xref:input-system-tracked-input-devices#tracked-pose-driver). For an HMD
        /// device, this means the "center" eye pose. For XR controllers, it means the "grip" pose.
        /// </summary>
        /// <remarks>
        /// For more information about how OpenXR represents the grip pose, refer to
        /// [Standard pose identifiers](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#semantic-paths-standard-identifiers)
        /// (OpenXR Specification).
        ///
        /// > [!NOTE]
        /// > The position value is in the tracking space reported by the device, which doesn't match
        /// > Unity world space. Using a combination of the XR Origin component with the
        /// > [Tracked Pose Driver](xref:input-system-tracked-input-devices#tracked-pose-driver) component
        /// > to manage that conversion automatically is more reliable than managing it through scripting.
        /// </remarks>
        [InputControl(noisy = true, dontReset = true)]
        public Vector3Control devicePosition { get; protected set; }

        /// <summary>
        /// Represents the rotation portion of the input device's primary
        /// [pose](xref:openxr-input#pose-data). For an HMD
        /// device, this means the "center" eye pose. For XR controllers, it means the "grip" pose.
        /// </summary>
        /// <remarks>
        /// For more information about how OpenXR represents the grip pose, refer to
        /// [Standard pose identifiers](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#semantic-paths-standard-identifiers)
        /// (OpenXR Specification).
        ///
        /// > [!NOTE]
        /// > The rotation value is in the tracking space reported by the device, which doesn't match
        /// > Unity world space. Using a combination of the XR Origin component with the
        /// > [Tracked Pose Driver](xref:input-system-tracked-input-devices#tracked-pose-driver) component
        /// > to manage that conversion automatically is more reliable than managing it through scripting.
        /// </remarks>
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
