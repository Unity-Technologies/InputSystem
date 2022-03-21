#include "Control.AxisTwoWay.h"
#include "Control.Button.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

static const float InputAxisTwoWayControlSamplingPrecision = 0.0001f;

void InputAxisTwoWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputAxisTwoWayControlRef, InputAxisTwoWayControlState, InputAxisTwoWayControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputAxisTwoWayControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputAxisTwoWayControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.PositiveAxisOneWay().controlRef)
                  outputSample = {reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value};
              else if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.NegativeAxisOneWay().controlRef)
                  outputSample = {-reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.PositiveButton().controlRef)
                  outputSample = {reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? 1.0f : 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.NegativeButton().controlRef)
                  outputSample = {reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? -1.0f : 0.0f};
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputAxisTwoWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputAxisTwoWayControlSample& nextSample
            )->bool
            {
              return fabs(currentSample.value - nextSample.value) < InputAxisTwoWayControlSamplingPrecision;
            },
            [](
                InputAxisTwoWayControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputAxisTwoWayControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputAxisTwoWayControlSample& nextSample
            )
            {},
            [](
                const InputAxisTwoWayControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputAxisTwoWayControlSample* inSamples,
                const uint32_t inCount
            ){},
            [](
                const InputAxisTwoWayControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputAxisTwoWayControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].value > 0.0f ? mergedSamples[i].value : 0.0f;
                  controlRef.PositiveAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].value < 0.0f ? -mergedSamples[i].value : 0.0f;
                  controlRef.NegativeAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].value > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.PositiveButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].value < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.NegativeButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
            }
        }
    );
}
