using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is moved continually along the horizontal axis in a single direction for aset distance and then moved quickly back in the opposite direction.
    /// a minimum distance <see cref="completionDistance"/>.
    /// </summary>
    /// <remarks>
    /// The action is canceled if the horizontal direction before the completionDistance <see cref="completionDistance"/> or if the timeout is reached
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
        /// Default value to use for the completionDistance <see cref="SwipeGestureInteraction.completionDistance"/> if it has not been set.
        /// </summary>
        public const float defaultCompletionDistance = 0.6f;

        /// <summary>
        /// Horizontal distance that needs to be travelled in a single direction before we start to consider this gesture as beginning.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="SwipeGestureInteraction.defaultRecognitionDistance"/> is used.
        /// </remarks>
        public float recognizeDistance;

        /// <summary>
        /// Default value to use for the recognizeDistance <see cref="SwipeGestureInteraction.recognizeDistance"/> if it has not been set.
        /// </summary>
        public const float defaultRecognitionDistance = 0.2f;

        /// <summary>
        /// Total vertical distance that is allowed to be travelled up or down without causing the gesture to be canceled.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="SwipeGestureInteraction.defaultVerticalTolerance"/> is used.
        /// </remarks>
        public float verticalTolerance;

        /// <summary>
        /// Default value to use for the verticalTolerance <see cref="SwipeGestureInteraction.verticalTolerance"/> if it has not been set.
        /// </summary>
        public const float defaultVerticalTolerance = 0.07f;

        private float completionDistanceOrDefault => completionDistance > 0.0 ? completionDistance : defaultCompletionDistance;
        private float recognizeDistanceOrDefault => recognizeDistance > 0.0 ? recognizeDistance : defaultRecognitionDistance;
        private float timeoutOrDefault => timeout > 0.0 ? timeout : InputSystem.settings.defaultGestureTimeout;
        private double timePressed;
        private float verticalToleranceOrDefault => verticalTolerance > 0.0 ? verticalTolerance : defaultVerticalTolerance;

        // Position when started tracking gesture or changed direction. Used to calulate total distance covered.
        private Vector2 startOrInflectionPoint = Vector2.zero;
        private Vector2 lastPos = Vector2.zero;

        private enum Direction { Left, Right, None };
        private Direction direction = Direction.None;

        private enum GestureStage { FirstSwipe, WaitingForSecondSwipeStart, SecondSwipe };
        private GestureStage stage = GestureStage.FirstSwipe;

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
            bool isDirectionChanged = (direction != currentDirection);
            direction = currentDirection; // Setup for next round

            // Used to check the total distance covered so far in gesture
            var deltaFromInflectionX = Mathf.Abs(startOrInflectionPoint.x - currentPos.x);
            var deltaFromInflectionY = Mathf.Abs(startOrInflectionPoint.y - currentPos.y);
            bool isExceededYTolerance = (deltaFromInflectionY > verticalToleranceOrDefault);

            // Check for cancelation due to unexpected directional changes or moving vertically
            if (isDirectionChanged)
            {
                // For direction changes the Inflection Point X will be last point before direction changed occurred.
                // The Y point should be preserved.
                startOrInflectionPoint.x = lastPos.x;

                if (stage == GestureStage.WaitingForSecondSwipeStart)
                {
                    stage = GestureStage.SecondSwipe;  // Entering second swipe stage
                    return;
                }
                else
                {
                    var inflectionX = startOrInflectionPoint.x;
                    OnCancel(ref context); // Cancel if direction change was unexpectedly
                    startOrInflectionPoint.x = inflectionX; // Preserve inflection point after OnCancel reset it
                    return;
                }
            }
            // Too much vertical movement to consider the gesture
            if (isExceededYTolerance)
            {
                OnCancel(ref context);
                return;
            }
            if (context.timerHasExpired)
            {
                OnCancel(ref context);
                return;
            }

            // Cancel and start tracking again
            void OnCancel(ref InputInteractionContext context)
            {
                OnReset();
                if (context.phase == InputActionPhase.Started)
                {
                    context.Canceled();
                }
            }
            void OnReset()
            {
                // Important to restart from current position so that there is zero deltaY from the beginning.
                startOrInflectionPoint = currentPos;
                lastPos = currentPos;
                stage = GestureStage.FirstSwipe;
            }

            lastPos = currentPos; // Setup for next round

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
                        OnCancel(ref context);
                        return;
                    }

                    // Check if we reached enough distance to call the gesture completed
                    if (deltaFromInflectionX > completionDistanceOrDefault)
                    {
                        if (stage == GestureStage.SecondSwipe)
                        {
                            OnReset();
                            context.Performed();
                            return;
                        }
                        else
                        {
                            stage = GestureStage.WaitingForSecondSwipeStart;
                        }
                    }

                    break;

                case InputActionPhase.Performed:
                    // Once it's performed we are finished and begin checking for new gestures
                    OnReset();
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

    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="SwipeGestureInteraction"/> in the editor.
    /// </summary>
    internal class SwipeGestureInteractionEditor : InputParameterEditor<SwipeGestureInteraction>
    {
        protected override void OnEnable()
        {
            m_TimeoutSetting.Initialize("Gesture Timeout",
                "Time (in seconds) within which time the gesture has be be completed otherwise it will not register",
                "Default Gesture Timeout",
                () => target.timeout, v => target.timeout = v, () => InputSystem.settings.defaultGestureTimeout);
            m_CompletionDistanceSetting.Initialize("Completion Distance",
                "Horizontal distance that the gesture has to travel before it can be considered a completed gesture",
                "Default Completion Distance",
                () => target.completionDistance, x => target.completionDistance = x, () => SwipeGestureInteraction.defaultCompletionDistance);
            m_StartRecognitionDistanceSetting.Initialize("Start Gesture Recognition Distance",
                "Horizontal distance that needs to be travelled before we begin to recognize the start of a potential gesture",
                "Default Start Recognition Distance",
                () => target.recognizeDistance, x => target.recognizeDistance = x, () => SwipeGestureInteraction.defaultRecognitionDistance);
            m_VerticalToleranceSetting.Initialize("Vertical Tolerance Distance",
                "Vertical distance that is allowed to be travelled without causing the gesture to be canceled",
                "Default Vertical Tolerance Distance",
                () => target.recognizeDistance, x => target.recognizeDistance = x, () => SwipeGestureInteraction.defaultRecognitionDistance);
        }
        public override void OnGUI()
        {
            m_TimeoutSetting.OnGUI();
            m_CompletionDistanceSetting.OnGUI();
            m_StartRecognitionDistanceSetting.OnGUI();
            m_VerticalToleranceSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_TimeoutSetting;
        private CustomOrDefaultSetting m_CompletionDistanceSetting;
        private CustomOrDefaultSetting m_StartRecognitionDistanceSetting;
        private CustomOrDefaultSetting m_VerticalToleranceSetting;
    }
    #endif

}
