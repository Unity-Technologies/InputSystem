#pragma once

#ifndef INPUT_BINDING_GENERATION

#include <stdint.h>
#include <vector>

// --------------------------------------------------------------------------------------------------------------------

// [Fixed|Variable] Size - [Single|Multiple] Source - Multiple Sink

// fixed element size - multiple sources in back buffers - multiple sinks in front buffers
// backBuffer0 [element0, element1, ..., elementN]
// ...
// backBufferM [element0, element1, ..., elementN]
// frontBuffer0 [element0, element1, ..., elementN]
// ...
// frontBufferM [element0, element1, ..., elementN]
struct DoubleBuffered_FixedSize_MultipleSource_MultipleSink
{
    const uint32_t elementSizeInBytes;
    const uint32_t framebuffersCount;
    std::vector<std::vector<uint8_t>> data;

    DoubleBuffered_FixedSize_MultipleSource_MultipleSink(const uint32_t setSizeInBytes, const uint32_t setFramebuffersCount)
        : elementSizeInBytes(setSizeInBytes)
        , framebuffersCount(setFramebuffersCount)
        , data(framebuffersCount * 2)
    {
    }

    inline void Resize(const uint32_t newElementCount)
    {
        for(uint32_t i = 0; i < framebuffersCount * 2; ++i)
            data[i].resize(newElementCount * elementSizeInBytes);
    }

    // remove [fromIndex, toIndex]
    inline void Remove(const uint32_t fromIndex, const uint32_t toIndex)
    {
        for(uint32_t i = 0; i < framebuffersCount * 2; ++i)
            data[i].erase(data[i].begin() + fromIndex * elementSizeInBytes, data[i].begin() + (toIndex + 1) * elementSizeInBytes);
    }

    inline void* GetAllElements(const uint32_t framebuffer, const bool inFrontBuffer)
    {
        return data[framebuffer + (inFrontBuffer ? framebuffersCount : 0)].data();
    }

    template<typename T>
    inline T* GetElementPtr(const uint32_t elementIndex, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        return reinterpret_cast<T*>(data[framebuffer + (inFrontBuffer ? framebuffersCount : 0)].data() + elementIndex * elementSizeInBytes);
    }

    inline void MoveDataToFrontBuffer(const uint32_t framebuffer)
    {
        memcpy(data[framebuffer + framebuffersCount].data(), data[framebuffer].data(), data[framebuffer].size());
    }
};

// --------------------------------------------------------------------------------------------------------------------

// TODO this is a poor choice of a data structure, can we do something better with arena allocators?
// vector of elements of fixed size - multiple sources in back buffers - multiple sinks in front buffers
// backBuffer0  [[element0, element1, ...], [element0, element1, ...], ..., [element0, element1, ...]]
// ...
// frontBuffer0 [[element0, element1, ...], [element0, element1, ...], ..., [element0, element1, ...]]
// ...
struct DoubleBuffered_DynamicArrayOfElementsOfFixedSize_MultipleSource_MultipleSink
{
    const uint32_t elementSizeInBytes;
    const uint32_t framebuffersCount;
    std::vector<std::vector<std::vector<uint8_t>>> data;

    DoubleBuffered_DynamicArrayOfElementsOfFixedSize_MultipleSource_MultipleSink(const uint32_t setSizeInBytes, const uint32_t setFramebuffersCount)
        : elementSizeInBytes(setSizeInBytes)
        , framebuffersCount(setFramebuffersCount)
        , data(framebuffersCount * 2)
    {
    }

    inline void ResizeGroups(const uint32_t newGroupCount)
    {
        for(uint32_t i = 0; i < framebuffersCount * 2; ++i)
            data[i].resize(newGroupCount);
    }

    // remove [fromIndex, toIndex]
    inline void RemoveGroups(const uint32_t fromGroupIndex, const uint32_t toGroupIndex)
    {
        for(uint32_t i = 0; i < framebuffersCount * 2; ++i)
            data[i].erase(data[i].begin() + fromGroupIndex, data[i].begin() + (toGroupIndex + 1));
    }

    inline std::vector<uint8_t>* _GetElementsVector(const uint32_t groupIndex, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        return reinterpret_cast<std::vector<uint8_t>*>(data[framebuffer + (inFrontBuffer ? framebuffersCount : 0)].data() + groupIndex);
    }

    inline void ResizeElements(const uint32_t newElementsCount, const uint32_t groupIndex, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        _GetElementsVector(groupIndex, framebuffer, inFrontBuffer)->resize(newElementsCount * elementSizeInBytes);
    }

    inline uint32_t GetElementsCount(const uint32_t groupIndex, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        return static_cast<uint32_t>(_GetElementsVector(groupIndex, framebuffer, inFrontBuffer)->size() / elementSizeInBytes);
    }

    template<typename T>
    inline T* AllocElements(const uint32_t groupIndex, const uint32_t count, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        auto vector = _GetElementsVector(groupIndex, framebuffer, inFrontBuffer);
        const auto offset = vector->size();
        vector->resize(offset + count * elementSizeInBytes);
        return reinterpret_cast<T*>(vector->data() + offset);
    }

    template<typename T>
    inline T* GetElementsPtr(const uint32_t groupIndex, const uint32_t framebuffer, const bool inFrontBuffer)
    {
        return reinterpret_cast<T*>(_GetElementsVector(groupIndex, framebuffer, inFrontBuffer)->data());
    }

    inline void MoveDataToFrontBuffer(const uint32_t framebuffer)
    {
        const auto groupSize = data[framebuffer].size();
        data[framebuffer + framebuffersCount] = std::move(data[framebuffer]);
        data[framebuffer].resize(groupSize);
    }
};

// --------------------------------------------------------------------------------------------------------------------

struct InputPerControlTypeStorage
{
    uint32_t controlsCount;
    std::vector<InputControlRef> controlRefs;
    DoubleBuffered_FixedSize_MultipleSource_MultipleSink controlState;

    DoubleBuffered_FixedSize_MultipleSource_MultipleSink latestRecordedTimestamp;
    DoubleBuffered_FixedSize_MultipleSource_MultipleSink latestRecordedSample;

    DoubleBuffered_DynamicArrayOfElementsOfFixedSize_MultipleSource_MultipleSink allRecordedTimestamps;
    DoubleBuffered_DynamicArrayOfElementsOfFixedSize_MultipleSource_MultipleSink allRecordedSamples;

    InputPerControlTypeStorage(const uint32_t controlStateSizeInBytes, const uint32_t controlSampleSizeInBytes, const uint32_t frameBuffersCount)
        : controlsCount(0)
        , controlRefs(0)
        , controlState(controlStateSizeInBytes, frameBuffersCount)
        , latestRecordedTimestamp(sizeof(InputControlTimestamp), frameBuffersCount)
        , latestRecordedSample(controlSampleSizeInBytes, frameBuffersCount)
        , allRecordedTimestamps(sizeof(InputControlTimestamp), frameBuffersCount)
        , allRecordedSamples(controlSampleSizeInBytes, frameBuffersCount)
    {

    }

    inline uint32_t AllocateControlStorage(const InputControlRef controlRef)
    {
        const uint32_t newIndex = controlsCount++;
        controlRefs.resize(controlsCount);
        controlRefs[newIndex] = controlRef;

        controlState.Resize(controlsCount);
        latestRecordedTimestamp.Resize(controlsCount);
        latestRecordedSample.Resize(controlsCount);
        allRecordedTimestamps.ResizeGroups(controlsCount);
        allRecordedSamples.ResizeGroups(controlsCount);
        return newIndex;
    }

    inline void RemoveControlStorage(const uint32_t index)
    {
        // TODO
    }

    inline void MoveDataToFrontBuffer(const InputFramebufferRef framebufferRef)
    {
        controlState.MoveDataToFrontBuffer(framebufferRef.transparent);
        latestRecordedTimestamp.MoveDataToFrontBuffer(framebufferRef.transparent);
        latestRecordedSample.MoveDataToFrontBuffer(framebufferRef.transparent);
        allRecordedTimestamps.MoveDataToFrontBuffer(framebufferRef.transparent);
        allRecordedSamples.MoveDataToFrontBuffer(framebufferRef.transparent);
    }
};

// --------------------------------------------------------------------------------------------------------------------

template<typename InputControlInstance, typename ControlStateType, typename ControlSampleType>
struct InputControlVisitor
{
    const InputControlInstance& instance;
    ControlStateType& controlState;
    InputControlTimestamp& latestRecordedTimestamp;
    ControlSampleType& latestRecordedSample;
    const InputControlTimestamp* allRecordedTimestamps;
    const ControlSampleType* allRecordedSamples;
    const uint32_t allRecordedCount;
};


#endif