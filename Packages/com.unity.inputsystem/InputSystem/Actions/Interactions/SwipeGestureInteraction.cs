using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is moved continually along the horizontal axis in a single direction
    /// a minimum distance <see cref="completionDistance"/>.
    /// </summary>
    /// <remarks>
    /// The action is canceled if the horizontal direction is changed or if the timeout is reached
    /// before the gesture is completed <see cref="timeout"/>.
    /// The action will enter the Started phase after a short distance has been travelled continuously along a
    /// single horizontal direction <see cref="recognitionDistance"/>.
    /// As soon as gesture completion distance is travelled the action is performed and then immediately released/canceled.
    /// </remarks>
    [DisplayName("SwipeGesture")]
    public class SwipeGestureInteraction : IInputInteraction<Vector2>
    {
        /// <summary>
        /// Timeout in seconds that the gesture must be completed within from the intitial point in time that the gesture was detected
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultGestureTimeout"/> is used.
        ///
        /// Timeout is expressed in real time and measured against the timestamps of input events
        /// (<see cref="LowLevel.InputEvent.time"/>) not against game time (<see cref="Time.time"/>).
        /// </remarks>
        public float timeout;

        /// <summary>
        /// Total horizontal distance that needs to be travelled during the gesture for it to be considered finished.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="SwipeGestureInteraction.defaultCompletionDistance"/> is used.
        /// </remarks>
        public float completionDistance;

        /// <summary>
        /// Horizontal distance that needs to be travelled in a single direction before we start to consider this gesture as beginning.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="SwipeGestureInteraction.defaultRecognitionDistance"/> is used.
        /// </remarks>
        public float recognizeDistance;

        private const float defaultCompletionDistance = 0.6f;
        private const float defaultRecognitionDistance = 0.2f;

        private float completionDistanceOrDefault => completionDistance > 0.0 ? completionDistance : defaultCompletionDistance;
        private float recognizeDistanceOrDefault => recognizeDistance > 0.0 ? recognizeDistance : defaultRecognitionDistance;
        private float timeoutOrDefault => timeout > 0.0 ? timeout : InputSystem.settings.defaultGestureTimeout;
        private double timePressed;

        // TODO: Y tolerance before cancelling

        // Position when started tracking gesture or changed direction. Used to calulate total distance covered.
        private Vector2 startOrInflectionPoint = Vector2.zero;
        private Vector2 lastPos = Vector2.zero;

        private enum Direction { Left, Right, None };
        private Direction direction = Direction.None;

        /// <inheritdoc />
        public void Process(ref InputInteractionContext context)
        {
            // Continually track position and watch for changes in direction.
            var currentPos = context.ReadValue<Vector2>();
            var currentDirection = (currentPos.x < lastPos.x) ? Direction.Left : Direction.Right;
            if (direction == Direction.None)
            {
                direction = currentDirection; // Initialise with first direction detected
            }

            // Changed direction before completing gesture, therefore it can't be completed
            // Start tracking again
            if (direction != currentDirection)
            {
                direction = currentDirection;
                startOrInflectionPoint = lastPos;
                lastPos = currentPos;
                if (context.phase == InputActionPhase.Started)
                {
                    context.Canceled();
                }
                return;
            }
            lastPos = currentPos;

            // Used to check the total distance covered in a single direction
            var deltaFromInflectionX = Mathf.Abs(startOrInflectionPoint.x - currentPos.x);

            if (context.timerHasExpired)
            {
                context.Canceled();
                return;
            }

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    // Check if we reached enough distance to start recognizing a potential gesture
                    if (deltaFromInflectionX > recognizeDistanceOrDefault)
                    {
                        timePressed = context.time;
                        context.Started();
                        context.SetTimeout(timeoutOrDefault);
                    }
                    break;

                case InputActionPhase.Started:
                    // Timelimit to complete the gesture has exceeded.
                    if (context.time - timePressed >= timeoutOrDefault)
                    {
                        context.Canceled();
                        return;
                    }

                    // Check if we reached enough distance to call the gesture completed
                    if (deltaFromInflectionX > completionDistanceOrDefault)
                    {
                        context.Performed();
                        return;
                    }

                    break;

                case InputActionPhase.Performed:
                    // Once it's performed we are finished and begin checking for new gestures
                    context.Canceled();
                    break;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            timePressed = 0;
        }
    }
}
