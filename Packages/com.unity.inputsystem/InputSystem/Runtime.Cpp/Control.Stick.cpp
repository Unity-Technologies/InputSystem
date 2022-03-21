#include "Control.Stick.h"
#include "Control.Button.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

static const float InputStickControlSamplingPrecision = 0.0001f;

void InputStickControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputStickControlRef, InputStickControlState, InputStickControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputStickControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputStickControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputAxisTwoWayControlRef::controlTypeRef && fromAnotherControl == controlRef.VerticalAxisTwoWay().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputAxisTwoWayControlSample*>(inSample)->value};
              if (sampleTypeRef == InputAxisTwoWayControlRef::controlTypeRef && fromAnotherControl == controlRef.HorizontalAxisTwoWay().controlRef)
                  outputSample = { reinterpret_cast<const InputAxisTwoWayControlSample*>(inSample)->value, 0.0f};
              else if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.LeftAxisOneWay().controlRef)
                  outputSample = {-reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value, 0.0f};
              else if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.RightAxisOneWay().controlRef)
                  outputSample = {reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value, 0.0f};
              else if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.UpAxisOneWay().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value};
              else if (sampleTypeRef == InputAxisOneWayControlRef::controlTypeRef && fromAnotherControl == controlRef.DownAxisOneWay().controlRef)
                  outputSample = {0.0f, -reinterpret_cast<const InputAxisOneWayControlSample*>(inSample)->value};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.LeftButton().controlRef)
                  outputSample = {reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? -1.0f : 0.0f, 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.RightButton().controlRef)
                  outputSample = {reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? 1.0f : 0.0f, 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.UpButton().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? 1.0f : 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.DownButton().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? -1.0f : 0.0f};
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputStickControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputStickControlSample& nextSample
            )->bool
            {
                const float distSquared = (currentSample.x - nextSample.x) * (currentSample.x - nextSample.x) + (currentSample.y - nextSample.y) * (currentSample.y - nextSample.y);
                const float precisionSquared = InputStickControlSamplingPrecision * InputStickControlSamplingPrecision;
                return distSquared < precisionSquared;
            },
            [](
                InputStickControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputStickControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputStickControlSample& nextSample
            )
            {},
            [](
                const InputStickControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputStickControlSample* inSamples,
                const uint32_t inCount
            ){},
            [](
                const InputStickControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputStickControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
              {
                  InputAxisTwoWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].y;
                  controlRef.VerticalAxisTwoWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisTwoWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].x;
                  controlRef.HorizontalAxisTwoWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].x < 0.0f ? -mergedSamples[i].x : 0.0f;
                  controlRef.LeftAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].x > 0.0f ? mergedSamples[i].x : 0.0f;
                  controlRef.RightAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].y > 0.0f ? mergedSamples[i].y : 0.0f;
                  controlRef.UpAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputAxisOneWayControlSample axisSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      axisSamples[i].value = mergedSamples[i].y < 0.0f ? -mergedSamples[i].y : 0.0f;
                  controlRef.DownAxisOneWay().IngressFrom(controlRef, mergedTimestamps, axisSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].x < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.LeftButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].x > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.RightButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].y > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.UpButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for (uint32_t i = 0; i < mergedCount; ++i)
                      buttonSamples[i] = mergedSamples[i].y < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.DownButton().IngressFrom(controlRef, mergedTimestamps, buttonSamples, mergedCount);
              }
            }
        }
    );
}
