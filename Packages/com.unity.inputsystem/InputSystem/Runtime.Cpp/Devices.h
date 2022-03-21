#pragma once

#include "PAL.LibraryAPI.h"
#include "PAL.Callbacks.h"
#include "Controls.h"
#include "Guid.h"

INPUT_C_INTERFACE
{

struct InputDevicePersistentIdentifier
{
    uint8_t persistentId[512];
};

struct InputDeviceDescr
{
    InputGuid guid;
    InputDevicePersistentIdentifier persistentIdentifier;
    char displayName[256];
};

struct InputDeviceTraitRef
{
    uint32_t transparent;

    inline bool operator==(const InputDeviceTraitRef o) const noexcept
    {
        return transparent == o.transparent;
    }
};

static const InputDeviceTraitRef InputDeviceTraitRefInvalid = {0};

InputDeviceRef InputInstantiateDeviceInternal(
    const InputDeviceDescr descr,
    const InputDeviceTraitRef* traits,
    const uint32_t* traitSizesInBytes,
    const uint32_t traitsCount
);

// also automatically removes all controls associated with the device
INPUT_LIB_API void InputRemoveDevice(
    const InputDeviceRef deviceRef
);

INPUT_LIB_API void* InputGetDeviceTrait(
    const InputDeviceRef deviceRef,
    const InputDeviceTraitRef traitRef
);

INPUT_LIB_API bool InputGetDeviceDescr(
    const InputDeviceRef deviceRef,
    InputDeviceDescr* outDescr
);

INPUT_LIB_API InputDeviceRef InputFindDeviceForPersistentId(
    const InputDevicePersistentIdentifier devicePersistentIdentifier
);

}

#ifndef INPUT_BINDING_GENERATION

// TODO this is bad because it will crash if trait is not present, can we maybe add default no-op traits or something?
template<typename T>
INPUT_INLINE_API T& AsTrait(const InputDeviceRef deviceRef)
{
    void* ptr = InputGetDeviceTrait(deviceRef, T::traitRef);
    InputAssert(ptr != nullptr, "Trait must be present");
    return *reinterpret_cast<T*>(ptr);
}

#endif