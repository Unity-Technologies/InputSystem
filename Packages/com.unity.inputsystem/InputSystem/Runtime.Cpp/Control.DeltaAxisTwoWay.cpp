#include "Control.DeltaAxisTwoWay.h"
#include "Control.Button.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

void InputDeltaAxisTwoWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputDeltaAxisTwoWayControlRef, InputDeltaAxisTwoWayControlState, InputDeltaAxisTwoWayControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputDeltaAxisTwoWayControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputDeltaAxisTwoWayControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.PositiveButton().controlRef)
              {
                  auto buttonSample = reinterpret_cast<const InputButtonControlSample*>(inSample);
                  outputSample = {buttonSample->IsPressed() ? 1.0f : 0.0f};
              }
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.NegativeButton().controlRef)
              {
                  auto buttonSample = reinterpret_cast<const InputButtonControlSample*>(inSample);
                  outputSample = {buttonSample->IsPressed() ? -1.0f : 0.0f};
              }
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputDeltaAxisTwoWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputDeltaAxisTwoWayControlSample& nextSample
            )->bool
            {
                currentTimestamp = nextTimestamp;
                currentSample.value += nextSample.value;
                return true;
            },
            [](
                InputDeltaAxisTwoWayControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputDeltaAxisTwoWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputDeltaAxisTwoWayControlSample& nextSample
            )
            {
            },
            [](
                const InputDeltaAxisTwoWayControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputDeltaAxisTwoWayControlSample* inSamples,
                const uint32_t inCount
            ){
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < inCount; ++i)
                      buttonSamples[i] = inSamples[i].value > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.PositiveButton().IngressFrom(controlRef, inTimestamps, buttonSamples, inCount);
              }

              {
                  InputButtonControlSample negativeButtonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < inCount; ++i)
                      negativeButtonSamples[i] = inSamples[i].value < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.NegativeButton().IngressFrom(controlRef, inTimestamps, negativeButtonSamples, inCount);
              }
            },
            [](
                const InputDeltaAxisTwoWayControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputDeltaAxisTwoWayControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
            }
        }
    );
}

void InputDeltaAxisTwoWayFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputDeltaAxisTwoWayControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputDeltaAxisTwoWayControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
)
{
    // do we need to set timestamp to "now"?

    InputControlTimestamp ts;
    InputGetCurrentTime(&ts);

    memset(controlStates, 0, controlStatesCount * sizeof(InputDeltaAxisTwoWayControlState));

    for(uint32_t i = 0; i < controlStatesCount; ++i)
        latestRecordedTimestamps[i] = ts;
    memset(latestRecordedSamples, 0, controlStatesCount * sizeof(InputDeltaAxisTwoWayControlSample));
}
