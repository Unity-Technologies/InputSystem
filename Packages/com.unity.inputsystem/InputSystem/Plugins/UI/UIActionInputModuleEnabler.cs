using UnityEngine;
using UnityEngine.Experimental.Input.Plugins.UI;

/// <summary>
/// This is a small helper to enable and disable any active controls on the UIActionInputModule.
/// Used as a placeholder for now until action ownership becomes more clear.
/// </summary>
public class UIActionInputModuleEnabler : MonoBehaviour
{
    void OnEnable()
    {
        UIActionInputModule inputModule = GetComponent<UIActionInputModule>();
        if (inputModule != null)
        {
            var pointAction = inputModule.point.action;
            if (pointAction != null && !pointAction.enabled)
                pointAction.Enable();

            var leftClickAction = inputModule.leftClick.action;
            if (leftClickAction != null && !leftClickAction.enabled)
                leftClickAction.Enable();

            var rightClickAction = inputModule.rightClick.action;
            if (rightClickAction != null && !rightClickAction.enabled)
                rightClickAction.Enable();

            var middleClickAction = inputModule.middleClick.action;
            if (middleClickAction != null && !middleClickAction.enabled)
                middleClickAction.Enable();

            var moveAction = inputModule.move.action;
            if (moveAction != null && !moveAction.enabled)
                moveAction.Enable();

            var submitAction = inputModule.submit.action;
            if (submitAction != null && !submitAction.enabled)
                submitAction.Enable();

            var cancelAction = inputModule.cancel.action;
            if (cancelAction != null && !cancelAction.enabled)
                cancelAction.Enable();

            var trackedPositionAction = inputModule.trackedPosition.action;
            if (trackedPositionAction != null && !trackedPositionAction.enabled)
                trackedPositionAction.Enable();

            var trackedOrientationAction = inputModule.trackedOrientation.action;
            if (trackedOrientationAction != null && !trackedOrientationAction.enabled)
                trackedOrientationAction.Enable();

            var trackedSelectAction = inputModule.trackedSelect.action;
            if (trackedSelectAction != null && !trackedSelectAction.enabled)
                trackedSelectAction.Enable();

            var touchPositionAction = inputModule.touchPosition.action;
            if (touchPositionAction != null && !touchPositionAction.enabled)
                touchPositionAction.Enable();

            var touchPhaseAction = inputModule.touchPhase.action;
            if (touchPhaseAction != null && !touchPhaseAction.enabled)
                touchPhaseAction.Enable();
        }
    }

    void OnDisable()
    {
        UIActionInputModule inputModule = GetComponent<UIActionInputModule>();
        if (inputModule != null)
        {
            var pointAction = inputModule.point.action;
            if (pointAction != null && pointAction.enabled)
                pointAction.Disable();

            var leftClickAction = inputModule.leftClick.action;
            if (leftClickAction != null && leftClickAction.enabled)
                leftClickAction.Disable();

            var rightClickAction = inputModule.rightClick.action;
            if (rightClickAction != null && rightClickAction.enabled)
                rightClickAction.Disable();

            var middleClickAction = inputModule.middleClick.action;
            if (middleClickAction != null && middleClickAction.enabled)
                middleClickAction.Disable();

            var moveAction = inputModule.move.action;
            if (moveAction != null && moveAction.enabled)
                moveAction.Disable();

            var submitAction = inputModule.submit.action;
            if (submitAction != null && submitAction.enabled)
                submitAction.Disable();

            var cancelAction = inputModule.cancel.action;
            if (cancelAction != null && cancelAction.enabled)
                cancelAction.Disable();

            var trackedPositionAction = inputModule.trackedPosition.action;
            if (trackedPositionAction != null && trackedPositionAction.enabled)
                trackedPositionAction.Disable();

            var trackedOrientationAction = inputModule.trackedOrientation.action;
            if (trackedOrientationAction != null && trackedOrientationAction.enabled)
                trackedOrientationAction.Disable();

            var trackedSelectAction = inputModule.trackedSelect.action;
            if (trackedSelectAction != null && trackedSelectAction.enabled)
                trackedSelectAction.Disable();

            var touchPositionAction = inputModule.touchPosition.action;
            if (touchPositionAction != null && touchPositionAction.enabled)
                touchPositionAction.Disable();

            var touchPhaseAction = inputModule.touchPhase.action;
            if (touchPhaseAction != null && touchPhaseAction.enabled)
                touchPhaseAction.Disable();
        }
    }
}
