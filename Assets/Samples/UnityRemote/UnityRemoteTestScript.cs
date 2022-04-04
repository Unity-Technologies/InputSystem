using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UnityRemoteTestScript : MonoBehaviour
{
    public Camera camera;

    public Text accelerometerInputText;
    public Text touchInputText;
    public Text gyroInputText;

    // We rotate this cube based on gyro input. Also, we sync its position on screen
    // the position of the primary touch.
    public Transform rotatingCube;

    private Vector3 m_Rotation;
    private float m_CubeOffsetFromCanvas;
    private Vector3 m_CubeStartingPosition;

    public void ResetCube()
    {
        rotatingCube.SetPositionAndRotation(m_CubeStartingPosition, default);
    }

    private void OnEnable()
    {
        m_CubeOffsetFromCanvas = rotatingCube.position.z - transform.position.z;
        m_CubeStartingPosition = rotatingCube.position;

        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        UpdateTouch();
        UpdateAccelerometer();
        UpdateGyro();
    }

    private void UpdateTouch()
    {
        var touchscreen = GetRemoteDevice<Touchscreen>();
        if (touchscreen == null)
        {
            touchInputText.text = "No remote touchscreen found.";
            return;
        }

        // Dump active touches.
        string activeTouches = null;
        foreach (var touch in Touch.activeTouches)
        {
            // Skip any touch not from our remote touchscreen.
            if (touch.screen != touchscreen)
                continue;

            if (activeTouches == null)
                activeTouches = "Active Touches:\n";

            activeTouches += $"\nid={touch.touchId} phase={touch.phase} position={touch.screenPosition} pressure={touch.pressure}\n";
        }
        if (activeTouches == null)
            activeTouches = "No active touches.";
        touchInputText.text = activeTouches;

        // Find world-space position of current primary touch (if any).
        if (touchscreen.primaryTouch.isInProgress)
        {
            var touchPosition = touchscreen.primaryTouch.position.ReadValue();
            var worldSpacePosition =
                camera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, transform.position.z + m_CubeOffsetFromCanvas));
            rotatingCube.position = worldSpacePosition;
        }
    }

    private void UpdateAccelerometer()
    {
        var accelerometer = GetRemoteDevice<Accelerometer>();
        if (accelerometer == null)
        {
            accelerometerInputText.text = "No remote accelerometer found.";
            return;
        }

        var value = accelerometer.acceleration.ReadValue();
        accelerometerInputText.text = $"Accelerometer: x={value.x} y={value.y} z={value.z}";
    }

    private void UpdateGyro()
    {
        var gyro = GetRemoteDevice<Gyroscope>();
        var attitude = GetRemoteDevice<AttitudeSensor>();
        var gravity = GetRemoteDevice<GravitySensor>();
        var acceleration = GetRemoteDevice<LinearAccelerationSensor>();

        // Enable gyro from remote, if needed.
        EnableDeviceIfNeeded(gyro);
        EnableDeviceIfNeeded(attitude);
        EnableDeviceIfNeeded(gravity);
        EnableDeviceIfNeeded(acceleration);

        string text;
        if (gyro == null && attitude == null && gravity == null && acceleration == null)
        {
            text = "No remote gyro found.";
        }
        else
        {
            string gyroText = null;
            string attitudeText = null;
            string gravityText = null;
            string accelerationText = null;

            if (gyro != null)
            {
                var rotation = gyro.angularVelocity.ReadValue();
                gyroText = $"Rotation: x={rotation.x} y={rotation.y} z={rotation.z}";

                // Update rotation of cube.
                m_Rotation += rotation;
                rotatingCube.localEulerAngles = m_Rotation;
            }

            if (attitude != null)
            {
                var attitudeValue = attitude.attitude.ReadValue();
                attitudeText = $"Attitude: x={attitudeValue.x} y={attitudeValue.y} z={attitudeValue.z} w={attitudeValue.w}";
            }

            if (gravity != null)
            {
                var gravityValue = gravity.gravity.ReadValue();
                gravityText = $"Gravity: x={gravityValue.x} y={gravityValue.y} z={gravityValue.z}";
            }

            if (acceleration != null)
            {
                var accelerationValue = acceleration.acceleration.ReadValue();
                accelerationText = $"Acceleration: x={accelerationValue.x} y={accelerationValue.y} z={accelerationValue.z}";
            }

            text = string.Join("\n", gyroText, attitudeText, gravityText, accelerationText);
        }

        gyroInputText.text = text;
    }

    private static void EnableDeviceIfNeeded(InputDevice device)
    {
        if (device != null && !device.enabled)
            InputSystem.EnableDevice(device);
    }

    // Make sure we're not thrown off track by locally having sensors on the device. Instead
    // explicitly grab the remote ones.
    private static TDevice GetRemoteDevice<TDevice>()
        where TDevice : InputDevice
    {
        foreach (var device in InputSystem.devices)
            if (device.remote && device is TDevice deviceOfType)
                return deviceOfType;
        return default;
    }
}
