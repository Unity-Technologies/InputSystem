#include "Control.Button.h"
#include "Control.AxisOneWay.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

void InputButtonControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputButtonControlRef, InputButtonControlState, InputButtonControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputButtonControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputButtonControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.AsAxisOneWay().controlRef)
              {
                  auto axisSample = reinterpret_cast<const InputAxisOneWayControlSample*>(inSample);
                  outputSample = (axisSample->value >= 0.5f) ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
              }
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputButtonControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputButtonControlSample& nextSample
            )->bool
            {
              return currentSample == nextSample;
            },
            [](
                InputButtonControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputButtonControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputButtonControlSample& nextSample
            )
            {
              if (currentSample.IsReleased() && nextSample.IsPressed())
                  controlState.wasPressedThisIOFrame = true;
              else if (currentSample.IsPressed() && nextSample.IsReleased())
                  controlState.wasReleasedThisIOFrame = true;
            },
            [](
                const InputButtonControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputButtonControlSample* inSamples,
                const uint32_t inCount
            ){},
            [](
                const InputButtonControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputButtonControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
              InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
              for(uint32_t i = 0; i < mergedCount; ++i)
                  axisSamples[i] = {mergedSamples[i].IsPressed() ? 1.0f : 0.0f};
              controlRef.AsAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
            }
        }
    );
}

void InputButtonControlFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputButtonControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputButtonControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
)
{
    memset(controlStates, 0, controlStatesCount * sizeof(InputButtonControlState));
}
