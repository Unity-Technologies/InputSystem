#include "Control.DeltaVector2D.h"
#include "Control.DeltaAxisTwoWay.h"
#include "Control.Button.h"
#include "ControlsIngress.h"

#include "_BuiltInDeviceDatabase.h"

void InputDeltaVector2DControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    InputIngressPipeline<InputIngressPipelineProvider<InputDeltaVector2DControlRef, InputDeltaVector2DControlState, InputDeltaVector2DControlSample>>(
        controlTypeRef,
        controlRef,
        samplesTypeRef,
        timestamps,
        samples,
        count,
        fromAnotherControl,
        {
            [](
                const InputDeltaVector2DControlRef controlRef,
                const InputControlTypeRef sampleTypeRef,
                const void* inSample,
                InputDeltaVector2DControlSample& outputSample,
                const InputControlRef fromAnotherControl
            )
            {
              if (sampleTypeRef == InputDeltaAxisTwoWayControlRef::controlTypeRef && fromAnotherControl == controlRef.VerticalDeltaAxisTwoWay().controlRef)
                  outputSample = { 0.0f, reinterpret_cast<const InputDeltaAxisTwoWayControlSample*>(inSample)->value};
              else if (sampleTypeRef == InputDeltaAxisTwoWayControlRef::controlTypeRef && fromAnotherControl == controlRef.HorizontalDeltaAxisTwoWay().controlRef)
                  outputSample = { reinterpret_cast<const InputDeltaAxisTwoWayControlSample*>(inSample)->value, 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.LeftButton().controlRef)
                  outputSample = { reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? -1.0f : 0.0f, 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.RightButton().controlRef)
                  outputSample = { reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? 1.0f : 0.0f, 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.UpButton().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? 1.0f : 0.0f};
              else if (sampleTypeRef == InputButtonControlRef::controlTypeRef && fromAnotherControl == controlRef.DownButton().controlRef)
                  outputSample = {0.0f, reinterpret_cast<const InputButtonControlSample*>(inSample)->IsPressed() ? -1.0f : 0.0f};
              else
                  InputAssert(false, "ingress from unknown type");
            },
            [](
                InputControlTimestamp& currentTimestamp,
                InputDeltaVector2DControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputDeltaVector2DControlSample& nextSample
            )->bool
            {
              currentTimestamp = nextTimestamp;
              currentSample.x += nextSample.x;
              currentSample.y += nextSample.y;
              return true;
            },
            [](
                InputDeltaVector2DControlState& controlState,
                const InputControlTimestamp& currentTimestamp,
                const InputDeltaVector2DControlSample& currentSample,
                const InputControlTimestamp& nextTimestamp,
                const InputDeltaVector2DControlSample& nextSample
            )
            {
            },
            [](
                const InputDeltaVector2DControlRef controlRef,
                const InputControlTimestamp* inTimestamps,
                const InputDeltaVector2DControlSample* inSamples,
                const uint32_t inCount
            ){
              {
                  InputDeltaAxisTwoWayControlSample deltaAxisSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      deltaAxisSamples[i].value = inSamples[i].y;
                  controlRef.VerticalDeltaAxisTwoWay().IngressFrom(controlRef, inTimestamps, deltaAxisSamples, inCount);
              }
              {
                  InputDeltaAxisTwoWayControlSample deltaAxisSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      deltaAxisSamples[i].value = inSamples[i].x;
                  controlRef.HorizontalDeltaAxisTwoWay().IngressFrom(controlRef, inTimestamps, deltaAxisSamples, inCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      buttonSamples[i] = inSamples[i].x < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.LeftButton().IngressFrom(controlRef, inTimestamps, buttonSamples, inCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      buttonSamples[i] = inSamples[i].x > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.RightButton().IngressFrom(controlRef, inTimestamps, buttonSamples, inCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      buttonSamples[i] = inSamples[i].y > 0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.UpButton().IngressFrom(controlRef, inTimestamps, buttonSamples, inCount);
              }
              {
                  InputButtonControlSample buttonSamples[InputIngressPipelineMaxBatchCount];
                  for(uint32_t i = 0; i < inCount; ++i)
                      buttonSamples[i] = inSamples[i].y < -0.5f ? InputButtonControlSamplePressed : InputButtonControlSampleReleased;
                  controlRef.DownButton().IngressFrom(controlRef, inTimestamps, buttonSamples, inCount);
              }
            },
            [](
                const InputDeltaVector2DControlRef controlRef,
                const InputControlTimestamp* mergedTimestamps,
                const InputDeltaVector2DControlSample* mergedSamples,
                const uint32_t mergedCount
            )
            {
            }
        }
    );
}

void InputDeltaVector2DFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputDeltaVector2DControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputDeltaVector2DControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
)
{
    // do we need to set timestamp to "now"?

    InputControlTimestamp ts;
    InputGetCurrentTime(&ts);

    memset(controlStates, 0, controlStatesCount * sizeof(InputDeltaVector2DControlState));

    for(uint32_t i = 0; i < controlStatesCount; ++i)
        latestRecordedTimestamps[i] = ts;
    memset(latestRecordedSamples, 0, controlStatesCount * sizeof(InputDeltaVector2DControlSample));
}
