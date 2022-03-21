#pragma once

#include "PAL.LibraryAPI.h"

#include "Devices.h"
#include "Guid.h"

#include <stdint.h>

INPUT_C_INTERFACE
{

struct InputDatabaseDeviceAssignedRef
{
    uint32_t _opaque;
    inline bool operator==(const InputDatabaseDeviceAssignedRef o) const noexcept
    {
        return _opaque == o._opaque;
    }
};

static const InputDatabaseDeviceAssignedRef InputDatabaseDeviceAssignedRefInvalid = {0};

struct InputDatabaseControlUsageDescr
{
    InputControlTypeRef controlTypeRef;
    InputControlRecordingMode defaultRecordingMode;
    InputControlUsage parentOfVirtualControl;
};

struct InputDatabaseControlTypeDescr
{
    uint32_t stateSizeInBytes;
    uint32_t sampleSizeInBytes;
};

struct InputDeviceDatabaseCallbacks
{
    void (*ControlTypeIngress)(
        const InputControlTypeRef controlTypeRef,
        const InputControlRef controlRef,
        const InputControlTypeRef samplesType, // doesn't have to match type of controlTypeRef! but that's only in case if fromAnotherControl is not invalid
        const InputControlTimestamp* timestamps,
        const void* samples,
        const uint32_t count,
        const InputControlRef fromAnotherControl
    );

    void (*ControlTypeFrameBegin)(
        const InputControlTypeRef controlTypeRef,
        const InputControlRef* controlRefs,
        void* controlStates,
        InputControlTimestamp* latestRecordedTimestamps,
        void* latestRecordedSamples,
        const uint32_t controlCount
    );

    // returns count of traits, outputTraitGuids can be null
    uint32_t (*GetDeviceTraits)(const InputDatabaseDeviceAssignedRef assignedRef, InputDeviceTraitRef* outputTraitAssignedRefs, const uint32_t outputCount);

    uint32_t (*GetTraitSizeInBytes)   (const InputDeviceTraitRef traitRef);
    uint32_t (*GetTraitControls)      (const InputDeviceTraitRef traitRef, const InputDeviceRef deviceRef, InputControlRef* outputControlRefs, const uint32_t outputCount);
    void     (*ConfigureTraitInstance)(const InputDeviceTraitRef traitRef, void* traitPointer, const InputDeviceRef device);

    InputDatabaseControlUsageDescr (*GetControlUsageDescr)(const InputControlUsage usage);
    InputDatabaseControlTypeDescr (*GetControlTypeDescr)(const InputControlTypeRef controlTypeRef);

    InputDatabaseDeviceAssignedRef (*GetDeviceAssignedRef)(const InputGuid deviceGuid);
    InputDeviceTraitRef            (*GetTraitAssignedRef) (const InputGuid traitGuid);
    InputControlUsage              (*GetControlUsage)     (const InputGuid controlGuid);
    InputControlTypeRef            (*GetControlTypeRef)   (const InputGuid controlTypeGuid);

    InputGuid (*GetDeviceGuid)     (const InputDatabaseDeviceAssignedRef assignedRef);
    InputGuid (*GetTraitGuid)      (const InputDeviceTraitRef            traitRef);
    InputGuid (*GetControlGuid)    (const InputControlUsage              usage);
    InputGuid (*GetControlTypeGuid)(const InputControlTypeRef            controlTypeRef);

    // refs are consecutive from [0, count - 1], while 0 represents invalid ref
    uint32_t (*GetDeviceRefCount)();
    uint32_t (*GetTraitRefCount)();
    uint32_t (*GetControlUsageCount)();
    uint32_t (*GetControlTypeCount)();

    // return counts of chars
    uint32_t (*GetDeviceName)     (const InputDatabaseDeviceAssignedRef assignedRef, char* outputBuffer, const uint32_t outputBufferCount);
    uint32_t (*GetTraitName)      (const InputDeviceTraitRef traitRef,               char* outputBuffer, const uint32_t outputBufferCount);
    uint32_t (*GetControlFullName)(const InputControlUsage usage,                    char* outputBuffer, const uint32_t outputBufferCount);
    uint32_t (*GetControlTypeName)(const InputControlTypeRef controlTypeRef,         char* outputBuffer, const uint32_t outputBufferCount);
};

INPUT_LIB_API void InputDatabaseSetCallbacks(InputDeviceDatabaseCallbacks callbacks);
InputDeviceDatabaseCallbacks* _InputDatabaseGetCallbacks(); // TODO is this a good way to do it?

// TODO
//INPUT_LIB_API InputGuid InputFindDeviceGuidForUSBVidPid(const uint16_t vid, const uint16_t pid);

INPUT_LIB_API InputDatabaseControlUsageDescr InputGetControlUsageDescr(const InputControlUsage usage);
INPUT_LIB_API InputDatabaseControlTypeDescr InputGetControlTypeDescr(const InputControlTypeRef controlTypeRef);
#ifndef INPUT_BINDING_GENERATION
INPUT_INLINE_API InputDatabaseControlUsageDescr InputGetControlUsageDescrForRef(const InputControlRef controlRef) {return InputGetControlUsageDescr(controlRef.usage);}
#endif

INPUT_LIB_API InputDeviceRef InputInstantiateDevice(const InputGuid guid, const InputDevicePersistentIdentifier identifier);

INPUT_LIB_API void _InputDatabaseControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesType,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

void _InputDatabaseControlTypeFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    void* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    void* latestRecordedSamples,
    const uint32_t controlCount
);

void _InputDatabaseInitializeControlStorageSpace();


};