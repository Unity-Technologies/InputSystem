#pragma once

#include "Context.h"

#ifndef INPUT_BINDING_GENERATION

constexpr uint32_t InputIngressPipelineMaxBatchCount = 128;

template<typename TControlRefType, typename TControlStateType, typename TControlSampleType>
struct InputIngressPipelineProvider
{
    typedef TControlRefType ControlRefType;
    typedef TControlStateType ControlStateType;
    typedef TControlSampleType ControlSampleType;

    void (*ConvertAnotherSampleType)(
        const ControlRefType controlRef,
        const InputControlTypeRef sampleTypeRef,
        const void* inSample,
        ControlSampleType& outputSample,
        const InputControlRef fromAnotherControl
    );

    // if returns true, skip next sample
    // beware that this needs to be stateless, and order of merging is undefined, e.g.
    // for a sequence of 0 1 2 3 we might call merge as (1, 2), (2, 3), (0, 1)
    bool (*ForwardMerge)(
        InputControlTimestamp& currentTimestamp,
        ControlSampleType& currentSample,
        const InputControlTimestamp& nextTimestamp,
        const ControlSampleType& nextSample
    );

    void (*UpdateControlState)(
        ControlStateType& controlState,
        const InputControlTimestamp& currentTimestamp,
        const ControlSampleType& currentSample,
        const InputControlTimestamp& nextTimestamp,
        const ControlSampleType& nextSample
    );

    void (*UpdateVirtualControlsPreMerge)(
        const ControlRefType controlRef,
        const InputControlTimestamp* inTimestamps,
        const ControlSampleType* inSamples,
        const uint32_t inCount
    );

    void (*UpdateVirtualControlsPostMerge)(
        const ControlRefType controlRef,
        const InputControlTimestamp* mergedTimestamps,
        const ControlSampleType* mergedSamples,
        const uint32_t mergedCount
    );
};

template<typename PipelineProvider>
INPUT_INLINE_API void InputIngressPipeline(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* inTimestamps,
    const void* inSamples,
    const uint32_t inCount,
    const InputControlRef fromAnotherControl,
    const PipelineProvider provider
)
{
    InputContextGuard _guard(fromAnotherControl != InputControlRefInvalid); // if we're coming from another control, don't lock to avoid a deadlock

    auto ctx = _GetCtx();
    auto instance = ctx->GetControlInstance(controlRef);

    if (!instance || instance->recordingMode == InputControlRecordingMode::Disabled || inCount == 0)
        return;

    const auto recordingMode = instance->recordingMode;

    // producer is trying to ingress directly to a virtual control, forward to the parent instead
    if (fromAnotherControl == InputControlRefInvalid && instance->parentOfVirtualControl != InputControlRefInvalid)
    {
        const auto targetControlTypeRef = InputGetControlUsageDescrForRef(instance->parentOfVirtualControl).controlTypeRef;
        _InputDatabaseControlIngress(targetControlTypeRef, instance->parentOfVirtualControl, samplesTypeRef, inTimestamps, inSamples, inCount, controlRef);
        return;
    }

    // iterate over samples in batches so we don't run out of stack
    for (uint32_t batchStart = 0; batchStart < inCount; batchStart += InputIngressPipelineMaxBatchCount)
    {
        InputControlTimestamp                        mergedTimestamps[InputIngressPipelineMaxBatchCount];
        typename PipelineProvider::ControlSampleType mergedSamples[InputIngressPipelineMaxBatchCount];

        const uint32_t batchCount = std::min(inCount - batchStart, InputIngressPipelineMaxBatchCount);

        uint32_t mergedCount = 0;

        // If we're getting samples from another control, check if the current control type matches the sample type:
        // - If the types match but fromAnotherControl is not invalid, it means parent virtual control is trying to ingress a virtual control update.
        // - If the types don't match, it means someone is trying to ingress to a virtual control, and got forwarded to parent control ingress function.
        if (fromAnotherControl != InputControlRefInvalid && InputGetControlUsageDescr(controlRef.usage).controlTypeRef != samplesTypeRef)
        {
            // If someone is trying to ingress to a virtual control, we need to iterate over untyped array, so it's ok if it's not the fastest.
            const auto inSampleSizeInBytes = InputGetControlTypeDescr(samplesTypeRef).sampleSizeInBytes;

            // Convert all samples in the batch and do rolling merge at a same time
            for (uint32_t i = 0; i < batchCount; ++i)
            {
                const uint32_t j = mergedCount;
                mergedTimestamps[j] = inTimestamps[i + batchStart];

                provider.ConvertAnotherSampleType(
                    PipelineProvider::ControlRefType::Setup(controlRef),
                    samplesTypeRef,
                    reinterpret_cast<const uint8_t*>(inSamples) + (i + batchStart) * inSampleSizeInBytes,
                    mergedSamples[j],
                    fromAnotherControl
                );

                // Update virtual controls pre merge
                // TODO is updating by one is the best we can do here?
                if (instance->parentOfVirtualControl == InputControlRefInvalid) // check if current control is not a virtual control already
                    provider.UpdateVirtualControlsPreMerge(PipelineProvider::ControlRefType::Setup(controlRef), inTimestamps + i + batchStart, mergedSamples + j, 1);

                // Rolling merge
                if ((j > 0) && (recordingMode != InputControlRecordingMode::AllAsIs) && provider.ForwardMerge(
                    mergedTimestamps[j - 1],
                    mergedSamples[j - 1],
                    mergedTimestamps[j],
                    mergedSamples[j]
                ))
                    continue;

                ++mergedCount;
            }
        }
        else // And if we're getting expected sample type, run merging without conversion
        {
            const auto inSamplesTyped = reinterpret_cast<const typename PipelineProvider::ControlSampleType*>(inSamples);

            // Update virtual controls pre merge
            if (instance->parentOfVirtualControl == InputControlRefInvalid) // check if current control is not a virtual control already
                provider.UpdateVirtualControlsPreMerge(PipelineProvider::ControlRefType::Setup(controlRef), inTimestamps + batchStart, inSamplesTyped + batchStart, batchCount);

            for (uint32_t i = 0; i < batchCount; ++i)
            {
                const uint32_t j = mergedCount;
                mergedTimestamps[j] = inTimestamps[i + batchStart];
                mergedSamples[j] = inSamplesTyped[i + batchStart];

                if ((j > 0) && (recordingMode != InputControlRecordingMode::AllAsIs) && provider.ForwardMerge(
                    mergedTimestamps[j - 1],
                    mergedSamples[j - 1],
                    mergedTimestamps[j],
                    mergedSamples[j]
                ))
                    continue;

                ++mergedCount;
            }
        }

        InputAssert(mergedCount > 0, "Expected at least one sample at this point");

        // For each back framebuffer ...
        for(auto framebufferRef: ctx->FramebufferRefs())
        {
            auto v = ctx->GetControlVisitor<typename PipelineProvider::ControlStateType, typename PipelineProvider::ControlSampleType>(
                instance,
                framebufferRef,
                false
            );

            const bool mergedFirstSample = (recordingMode != InputControlRecordingMode::AllAsIs) &&
                provider.ForwardMerge(v.latestRecordedTimestamp, v.latestRecordedSample, mergedTimestamps[0], mergedSamples[0]);

            // If the only sample was adhoc merged into existing recording, just continue
            if (mergedFirstSample && mergedCount == 1)
                continue;

            // Update control state between current value and first value after merging
            provider.UpdateControlState(
                v.controlState,
                v.latestRecordedTimestamp,
                v.latestRecordedSample,
                mergedTimestamps[mergedFirstSample ? 1 : 0],
                mergedSamples[mergedFirstSample ? 1 : 0]
            );

            // Update control state for values after merging
            for (uint32_t i = (mergedFirstSample ? 2 : 1); i < mergedCount; ++i)
                provider.UpdateControlState(
                    v.controlState,
                    mergedTimestamps[i - 1],
                    mergedSamples[i - 1],
                    mergedTimestamps[i],
                    mergedSamples[i]
                );

            // Update latest recorded value
            v.latestRecordedTimestamp = mergedTimestamps[mergedCount - 1];
            v.latestRecordedSample = mergedSamples[mergedCount - 1];

            // Record samples if required
            if (recordingMode == InputControlRecordingMode::AllMerged || recordingMode == InputControlRecordingMode::AllAsIs)
            {
                ctx->RecordControlSamples(
                    instance,
                    framebufferRef,
                    false,
                    mergedTimestamps + (mergedFirstSample ? 1 : 0),
                    mergedSamples + (mergedFirstSample ? 1 : 0),
                    mergedCount - (mergedFirstSample ? 1 : 0));
            }
        }

        // Update virtual controls post merge
        if (instance->parentOfVirtualControl == InputControlRefInvalid) // check if current control is not a virtual control already
            provider.UpdateVirtualControlsPostMerge(PipelineProvider::ControlRefType::Setup(controlRef), mergedTimestamps, mergedSamples, mergedCount);
    }

}

#endif