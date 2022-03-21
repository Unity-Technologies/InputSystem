#include "Control.AxisOneWay.h"
#include "Control.Button.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

static const float InputAxisOneWayControlSamplingPrecision = 0.0001f;

void InputAxisOneWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputAxisOneWayControlRef, InputAxisOneWayControlState, InputAxisOneWayControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputAxisOneWayControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputAxisOneWayControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.AsButton().controlRef)
              {
                  auto buttonSample = reinterpret_cast<const InputButtonControlSample*>(inSample);
                  outputSample = {buttonSample->IsPressed() ? 1.0f : 0.0f};
              }
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputAxisOneWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputAxisOneWayControlSample& nextSample
            )->bool
            {
              return fabs(currentSample.value - nextSample.value) < InputAxisOneWayControlSamplingPrecision;
            },
            [](
                InputAxisOneWayControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputAxisOneWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputAxisOneWayControlSample& nextSample
            )
            {},
            [](
                const InputAxisOneWayControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputAxisOneWayControlSample* inSamples,
                const uint32_t inCount
            ){},
            [](
                const InputAxisOneWayControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputAxisOneWayControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
              InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
              for(uint32_t i = 0; i < mergedCount; ++i)
                  buttonSamples[i] = mergedSamples[i].value > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
              controlRef.AsButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
            }
        }
    );
}
