
#include "Controls.h"
#include "Context.h"
#include "ArgumentChecks.h"

void InputInstantiateControlsInternal(
    const InputControlRef* controlRefs,
    const uint32_t controlRefsCount
)
{
    if(!(NullPtrCheck(controlRefs) && NonZeroCountCheck(controlRefsCount)))
        return;

    auto ctx = _GetCtx();
    for(uint32_t i = 0; i < controlRefsCount; ++i)
    {
        const auto controlRef = controlRefs[i];
        const auto controlUsageDescr = InputGetControlUsageDescrForRef(controlRef);
        const auto controlTypeDescr = InputGetControlTypeDescr(controlUsageDescr.controlTypeRef);

        auto& storage = ctx->controlsStoragePerType[controlUsageDescr.controlTypeRef.transparent];
        const uint32_t storageIndex = storage.AllocateControlStorage(controlRef);

        const auto parentOfVirtualControl = controlUsageDescr.parentOfVirtualControl != InputControlUsageInvalid ? InputControlRef {controlUsageDescr.parentOfVirtualControl, controlRef.deviceRef} : InputControlRefInvalid;

        ctx->controls.emplace(std::make_pair(controlRef, InputControlInstance(controlRef, controlUsageDescr.controlTypeRef, parentOfVirtualControl, controlUsageDescr.defaultRecordingMode, storageIndex)));
    }

    for(auto& v : ctx->framebufferVisiblity)
        v.needsUpdatingVisibilityFlags = true;
}

void InputRemoveControlsInternal(
    const InputControlRef* controlRefs,
    const uint32_t controlRefsCount
)
{
    if(!(NullPtrCheck(controlRefs) && NonZeroCountCheck(controlRefsCount)))
        return;

    InputContextGuard _guard;

    auto ctx = _GetCtx();
    for(uint32_t i = 0; i < controlRefsCount; ++i)
    {
        auto controlRef = controlRefs[i];
        auto instance = ctx->GetControlInstance(controlRef);
        if (!instance)
            continue;

        instance->pendingDeletion = true;
    }

    for(auto& v : ctx->framebufferVisiblity)
        v.needsUpdatingVisibilityFlags = true;
}

void InputSetControlDescr(
    const InputControlRef controlRef,
    const InputControlDescr descr
) {
    InputContextGuard _guard;

    auto instance = _GetCtx()->GetControlInstance(controlRef);
    if(!instance)
        return;

    instance->descr = descr;
}

bool InputGetControlDescr(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlDescr* outDescr
)
{
    InputContextGuard _guard;

    auto instance = _GetCtx()->GetControlInstance(controlRef);
    if(!instance)
        return false;

    *outDescr = instance->descr;
    return true;
}

void InputSetRecordingMode(
    const InputControlRef controlRef,
    const InputControlRecordingMode recordingMode
)
{
    InputContextGuard _guard;

    auto instance = _GetCtx()->GetControlInstance(controlRef);
    if(!instance)
        return;

    instance->recordingMode = recordingMode;
}

InputControlRecordingMode InputGetRecordingMode(
    const InputControlRef controlRef
)
{
    InputContextGuard _guard;

    auto instance = _GetCtx()->GetControlInstance(controlRef);
    return instance ? instance->recordingMode : InputControlRecordingMode::Disabled;
}

void InputForceSyncControlInFrontbufferWithBackbuffer(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef
)
{
    InputContextGuard _guard;

    // TODO

//    if(!(ArgumentCheck(framebufferRef, false, controlRef) && ArgumentCheck(framebufferRef, true, controlRef)))
//        return;
//
//    auto& front = _GetFront(framebufferRef)->controls[controlRef];
//    auto& back = _GetBack(framebufferRef)->controls[controlRef];
//
//    if(front->recordingMode == InputControlRecordingMode::LatestOnly || front->recordingMode == InputControlRecordingMode::All)
//    {
//        front->latestRecordedTimestamp = back->latestRecordedTimestamp;
//        front->latestRecordedSample = back->latestRecordedSample;
//    }
//
//    if(front->recordingMode == InputControlRecordingMode::All)
//    {
//        front->allRecordedTimestamps = back->allRecordedTimestamps;
//        front->allRecordedSamples = back->allRecordedSamples;
//    }
}

void InputGetControlVisitorGenericState(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlVisitorGenericState* outVisitor
)
{
    InputContextGuard _guard;

    auto ctx = _GetCtx();
    auto instance = ctx->GetControlInstance(controlRef);
    if(!instance || !outVisitor)
        return;

    auto& storage = ctx->controlsStoragePerType[instance->typeRef.transparent];
    *outVisitor = {
        storage.controlState.GetElementPtr<void>(instance->indexInStorage, framebufferRef.transparent, true),
        storage.latestRecordedTimestamp.GetElementPtr<InputControlTimestamp>(instance->indexInStorage, framebufferRef.transparent, true),
        storage.latestRecordedSample.GetElementPtr<void>(instance->indexInStorage, framebufferRef.transparent, true)
    };
}

void InputGetControlVisitorGenericRecordings(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlVisitorGenericRecordings* outVisitor
)
{
    InputContextGuard _guard;

    auto ctx = _GetCtx();
    auto instance = ctx->GetControlInstance(controlRef);
    if(!instance || !outVisitor)
        return;

    auto& storage = ctx->controlsStoragePerType[instance->typeRef.transparent];
    *outVisitor = {
        storage.allRecordedTimestamps.GetElementsPtr<InputControlTimestamp>(instance->indexInStorage, framebufferRef.transparent, true),
        storage.allRecordedSamples.GetElementsPtr<void>(instance->indexInStorage, framebufferRef.transparent, true),
        storage.allRecordedSamples.GetElementsCount(instance->indexInStorage, framebufferRef.transparent, true)
    };
}

void InputGetCurrentTime(InputControlTimestamp* outTimestamp)
{
    if (!outTimestamp)
        return;
    // TODO
    *outTimestamp = {};
}
