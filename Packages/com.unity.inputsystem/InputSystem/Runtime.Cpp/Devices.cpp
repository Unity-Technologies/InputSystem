#include "Devices.h"
#include "Context.h"
#include "ArgumentChecks.h"

InputDeviceRef InputInstantiateDeviceInternal(
    const InputDeviceDescr descr,
    const InputDeviceTraitRef* traits,
    const uint32_t* traitSizesInBytes,
    const uint32_t traitsCount
)
{
    auto ctx = _GetCtx();

    const InputDeviceRef deviceRef = {ctx->nextDeviceRef++ };

    std::unordered_map<InputDeviceTraitRef, std::vector<uint8_t>> traitRefToOpaqueBinaryBlob;
    for (uint32_t i = 0; i < traitsCount; ++i)
        traitRefToOpaqueBinaryBlob[traits[i]] = std::vector<uint8_t>(traitSizesInBytes[i]);

    _GetCtx()->devices.emplace(std::make_pair(deviceRef, InputDeviceInstance(deviceRef, descr, traitRefToOpaqueBinaryBlob)));

    for(auto& v : ctx->framebufferVisiblity)
        v.needsUpdatingVisibilityFlags = true;

    return deviceRef;
}

void InputRemoveDevice(const InputDeviceRef deviceRef)
{
    InputContextGuard _guard;

    auto ctx = _GetCtx();
    auto instance = ctx->GetDeviceInstance(deviceRef);
    if (!instance)
        return;

    instance->pendingDeletion = true;

    for(auto pair: _GetCtx()->controls)
        if (pair.first.deviceRef == deviceRef)
            pair.second.pendingDeletion = true;

    for(auto& v : ctx->framebufferVisiblity)
        v.needsUpdatingVisibilityFlags = true;
}

void* InputGetDeviceTrait(
    const InputDeviceRef deviceRef,
    const InputDeviceTraitRef traitRef
)
{
    InputContextGuard guard;

    auto instance = _GetCtx()->GetDeviceInstance(deviceRef);
    if (!instance)
        return nullptr;

    return instance->GetTraitInstance(traitRef);
}

bool InputGetDeviceDescr(
    const InputDeviceRef deviceRef,
    InputDeviceDescr* outDescr
)
{
    if (!NullPtrCheck(outDescr))
        return false;

    InputContextGuard _guard;

    auto instance = _GetCtx()->GetDeviceInstance(deviceRef);
    if (!instance)
        return false;

    *outDescr = instance->descr;
    return true;
}

static inline bool operator==(InputDevicePersistentIdentifier const& a, InputDevicePersistentIdentifier const& b) noexcept
{
    return memcmp(a.persistentId, b.persistentId, sizeof(a.persistentId)) == 0;
}

InputDeviceRef InputFindDeviceForPersistentId(
    const InputDevicePersistentIdentifier devicePersistentIdentifier
)
{
    InputContextGuard _guard;

    for (auto pair: _GetCtx()->devices)
        if (pair.second.descr.persistentIdentifier == devicePersistentIdentifier)
            return pair.first;

    return InputDeviceRefInvalid;
}