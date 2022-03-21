#pragma once

#include "Devices.h"
#include "Query.h"
#include "DeviceDatabase.h"
#include "ControlsStorage.h"
#include "PAL.Callbacks.h"

INPUT_C_INTERFACE
{

// Bump this version any time you change anything in headers, don't use directly, only for binding generation
//static const uint32_t _Input_APIVersion_InNativeHeader = 1;
//
//INPUT_LIB_API uint32_t Input_GetAPIVersion_ForNative();
//
//INPUT_INLINE_API uint32_t Input_GetAPIVersion_ForManaged()
//{
//    return _Input_APIVersion_InNativeHeader;
//}

INPUT_LIB_API void InputRuntimeInit(const uint32_t ioFramesCount);

INPUT_LIB_API void InputRuntimeDeinit();

INPUT_LIB_API void InputSwapFramebuffer(const InputFramebufferRef framebufferRef);

}

// these are internal methods of data backbone
#ifndef INPUT_BINDING_GENERATION

#include "PAL.SpinLock.h"

#include <vector>
#include <mutex>
#include <atomic>
#include <memory>
#include <thread>
#include <unordered_map>
#include <unordered_set>

template<>
struct std::hash<InputDeviceRef>
{
    std::size_t operator()(InputDeviceRef const& ref) const noexcept
    {
        return std::hash<uint32_t>{}(ref._opaque);
    }
};

template<>
struct std::hash<InputDeviceTraitRef>
{
    std::size_t operator()(InputDeviceTraitRef const& ref) const noexcept
    {
        return std::hash<uint32_t>{}(ref.transparent);
    }
};

template<>
struct std::hash<InputControlTypeRef>
{
    std::size_t operator()(InputControlTypeRef const& ref) const noexcept
    {
        return std::hash<uint32_t>{}(ref.transparent);
    }
};

template<>
struct std::hash<InputQueryRef>
{
    std::size_t operator()(InputQueryRef const& ref) const noexcept
    {
        return std::hash<uint32_t>{}(ref._opaque);
    }
};

template<>
struct std::hash<InputControlRef>
{
    std::size_t operator()(InputControlRef const& ref) const noexcept
    {
        auto h1 = std::hash<uint32_t>{}(ref.usage.transparent);
        auto h2 = std::hash<InputDeviceRef>{}(ref.deviceRef);
        return h1 ^ (h2 << 1);
    }
};

// --------------------------------------------------------------------------------------------------------------------

struct InputDeviceInstance
{
    InputDeviceRef ref;
    InputDeviceDescr descr;
    bool pendingDeletion;
    std::unordered_map<InputDeviceTraitRef, std::vector<uint8_t>> traitRefToOpaqueBinaryBlob;

    InputDeviceInstance(
        const InputDeviceRef& setRef,
        const InputDeviceDescr& setDescr,
        const std::unordered_map<InputDeviceTraitRef, std::vector<uint8_t>>& setTraitRefToOpaqueBinaryBlob
    )
        : ref(setRef)
        , descr(setDescr)
        , pendingDeletion(false)
        , traitRefToOpaqueBinaryBlob(setTraitRefToOpaqueBinaryBlob)
    {
    }

    inline void* GetTraitInstance(const InputDeviceTraitRef traitRef)
    {
        auto it = traitRefToOpaqueBinaryBlob.find(traitRef);
        if (it != traitRefToOpaqueBinaryBlob.end())
            return it->second.data();
        else
        {
            char temp[256];
            _InputDatabaseGetCallbacks()->GetTraitName(traitRef, temp, sizeof(temp));
            InputAssertFormatted(false, "Not found expected device trait '%s' (with ref '%u') at device instance with reference '%u'", temp, traitRef.transparent, ref._opaque);
            return nullptr;
        }
    }
};

// --------------------------------------------------------------------------------------------------------------------

struct InputControlInstance
{
    const InputControlRef ref;
    const InputControlTypeRef typeRef;
    const InputControlRef parentOfVirtualControl;
    InputControlDescr descr;
    InputControlRecordingMode recordingMode;
    uint32_t indexInStorage;
    bool pendingDeletion;

    InputControlInstance(const InputControlRef setRef, const InputControlTypeRef setTypeRef, const InputControlRef setParentOfVirtualControl, const InputControlRecordingMode setRecordingMode, const uint32_t setIndexInStorage)
    : ref(setRef)
    , typeRef(setTypeRef)
    , parentOfVirtualControl(setParentOfVirtualControl)
    , descr(InputControlDescr())
    , recordingMode(setRecordingMode)
    , indexInStorage(setIndexInStorage)
    {
    }
};

// --------------------------------------------------------------------------------------------------------------------

struct InputFramebufferVisiblity
{
    std::unordered_set<InputDeviceRef> visibleDevices;
    std::unordered_set<InputControlRef> visibleControls;
    bool needsUpdatingVisibilityFlags;
};

// --------------------------------------------------------------------------------------------------------------------

struct InputContext
{
    InputFastSpinlock contextLock;

    std::vector<InputFramebufferVisiblity> framebufferVisiblity;
    const uint32_t framebuffersCount;

    std::unordered_map<InputDeviceRef, InputDeviceInstance> devices;
    std::unordered_map<InputControlRef, InputControlInstance> controls;
    std::vector<InputPerControlTypeStorage> controlsStoragePerType;

    std::atomic<uint32_t> nextDeviceRef = { 1 };

    inline InputContext(const uint32_t setFramebuffersCount)
        : framebufferVisiblity(setFramebuffersCount)
        , framebuffersCount(setFramebuffersCount)
    {
    }

    inline InputDeviceInstance* GetDeviceInstance(const InputDeviceRef deviceRef)
    {
        auto it = devices.find(deviceRef);
        if (it != devices.end())
            return &it->second;
        else
        {
            InputAssertFormatted(false, "Not found expected device with instance reference '%u'", deviceRef._opaque);
            return nullptr;
        }
    }

    inline InputControlInstance* GetControlInstance(const InputControlRef controlRef)
    {
        auto it = controls.find(controlRef);
        if(it != controls.end())
        {
            auto& instance = it->second;
            return &instance;
        }
        else
        {
            char temp[256];
            _InputDatabaseGetCallbacks()->GetControlFullName(controlRef.usage, temp, sizeof(temp));
            InputAssertFormatted(false, "Not found expected control '%s' (usage '%u') for device instance ref '%u'", temp, controlRef.usage.transparent, controlRef.deviceRef._opaque);
            return nullptr;
        }
    }

    template<typename ControlStateType, typename ControlSampleType>
    inline InputControlVisitor<InputControlInstance, ControlStateType, ControlSampleType> GetControlVisitor(const InputControlInstance* instance, const InputFramebufferRef framebufferRef, const bool inFrontBuffer)
    {
        auto& controlsStorage = controlsStoragePerType[instance->typeRef.transparent];
        const uint32_t indexInStorage = instance->indexInStorage;
        const uint32_t frameBufferIndex = framebufferRef.transparent;
        return {
            *instance,
            *controlsStorage.controlState.GetElementPtr<ControlStateType>(indexInStorage, frameBufferIndex, inFrontBuffer),
            *controlsStorage.latestRecordedTimestamp.GetElementPtr<InputControlTimestamp>(indexInStorage, frameBufferIndex, inFrontBuffer),
            *controlsStorage.latestRecordedSample.GetElementPtr<ControlSampleType>(indexInStorage, frameBufferIndex, inFrontBuffer),
            controlsStorage.allRecordedTimestamps.GetElementsPtr<InputControlTimestamp>(indexInStorage, frameBufferIndex, inFrontBuffer),
            controlsStorage.allRecordedSamples.GetElementsPtr<ControlSampleType>(indexInStorage, frameBufferIndex, inFrontBuffer),
            controlsStorage.allRecordedSamples.GetElementsCount(indexInStorage, frameBufferIndex, inFrontBuffer)
        };
    }

    // TODO this is very bad, optimize
    template<typename ControlSampleType>
    inline void RecordControlSamples(const InputControlInstance* instance, const InputFramebufferRef framebufferRef, const bool inFrontBuffer, const InputControlTimestamp* timestamps, const ControlSampleType* samples, const uint32_t count)
    {
        auto& controlsStorage = controlsStoragePerType[instance->typeRef.transparent];
        const uint32_t indexInStorage = instance->indexInStorage;
        const uint32_t frameBufferIndex = framebufferRef.transparent;

        auto timestampsStorage = controlsStorage.allRecordedTimestamps.AllocElements<InputControlTimestamp>(indexInStorage, count, frameBufferIndex, inFrontBuffer);
        auto samplesStorage = controlsStorage.allRecordedSamples.AllocElements<ControlSampleType>(indexInStorage, count, frameBufferIndex, inFrontBuffer);

        memcpy(timestampsStorage, timestamps, count * controlsStorage.allRecordedTimestamps.elementSizeInBytes);
        memcpy(samplesStorage, samples, count * controlsStorage.allRecordedSamples.elementSizeInBytes);
    }

    // --------------------------------------------------------------------------------------------------------------------

    class FramebuffersIterator
    {
        typedef InputFramebufferRef     value_type;
        typedef std::ptrdiff_t          difference_type;
        typedef InputFramebufferRef*    pointer;
        typedef InputFramebufferRef&    reference;
        typedef std::input_iterator_tag iterator_category;

        uint32_t index = 0;
    public:
        inline explicit FramebuffersIterator(const uint32_t setIndex)
        : index(setIndex)
        {}
        inline FramebuffersIterator& operator++()
        {
            index++;
            return *this;
        }
        inline bool operator==(FramebuffersIterator o) const {return index == o.index;}
        inline bool operator!=(FramebuffersIterator o) const {return !(*this == o);}
        inline InputFramebufferRef operator*() const {return {index};}
    };

    struct FrameBuffersIteratorContainerProxy
    {
        const uint32_t count;
        inline FrameBuffersIteratorContainerProxy(const uint32_t setCount)
        : count(setCount)
        {}

        inline FramebuffersIterator begin() const {return FramebuffersIterator(0);}
        inline FramebuffersIterator end() const {return FramebuffersIterator(count);}
    };

    inline FrameBuffersIteratorContainerProxy FramebufferRefs() const
    {
        return FrameBuffersIteratorContainerProxy(framebuffersCount);
    }

    // --------------------------------------------------------------------------------------------------------------------

    class ControlTypesIterator
    {
        typedef InputControlTypeRef     value_type;
        typedef std::ptrdiff_t          difference_type;
        typedef InputControlTypeRef*    pointer;
        typedef InputControlTypeRef&    reference;
        typedef std::input_iterator_tag iterator_category;

        uint32_t index = 0;
    public:
        inline explicit ControlTypesIterator(const uint32_t setIndex)
            : index(setIndex)
        {}
        inline ControlTypesIterator& operator++()
        {
            index++;
            return *this;
        }
        inline bool operator==(ControlTypesIterator o) const {return index == o.index;}
        inline bool operator!=(ControlTypesIterator o) const {return !(*this == o);}
        inline InputControlTypeRef operator*() const {return {index};}
    };

    struct ControlTypesIteratorContainerProxy
    {
        const uint32_t count;
        inline ControlTypesIteratorContainerProxy(const uint32_t setCount)
            : count(setCount)
        {}

        inline ControlTypesIterator begin() const {return ControlTypesIterator(0);}
        inline ControlTypesIterator end() const {return ControlTypesIterator(count);}
    };

    inline ControlTypesIteratorContainerProxy ControlTypeRefs() const
    {
        return ControlTypesIteratorContainerProxy(static_cast<uint32_t>(controlsStoragePerType.size()));
    }
};

// using pointer so we have easier time in the future when we gonna transpile C++ to C#
InputContext* _GetCtx();
InputPALCallbacks* _GetPALCallbacks();
InputDeviceDatabaseCallbacks* _GetDeviceDatabaseCallbacks();

struct InputContextGuard
{
    const bool _ownsLock;

    inline InputContextGuard(bool dontLock = false)
        : _ownsLock(!dontLock)
    {
        if(_ownsLock)
            _GetCtx()->contextLock.lock();
    }

    inline ~InputContextGuard()
    {
        if(_ownsLock)
            _GetCtx()->contextLock.unlock();
    }
};

#endif