#include "DeviceDatabase.h"
#include "Context.h"

// #define INPUT_NATIVE_DEVICE_DATABASE_PROVIDER
// #include "_BuiltInDeviceDatabase.h"

// // TODO remove me, this is only for development
// static inline void _EnsureBuiltInIsSet()
// {
//     InputDatabaseSetCallbacks(_InputBuiltInDatabaseGetCallbacks());
// }

void InputDatabaseSetCallbacks(InputDeviceDatabaseCallbacks callbacks)
{
    *_GetDeviceDatabaseCallbacks() = callbacks;
}

InputDeviceDatabaseCallbacks* _InputDatabaseGetCallbacks()
{
    return _GetDeviceDatabaseCallbacks();
}

InputDatabaseControlUsageDescr InputGetControlUsageDescr(const InputControlUsage usage)
{
    return _GetDeviceDatabaseCallbacks()->GetControlUsageDescr(usage);
}

InputDatabaseControlTypeDescr InputGetControlTypeDescr(const InputControlTypeRef controlTypeRef)
{
    return _GetDeviceDatabaseCallbacks()->GetControlTypeDescr(controlTypeRef);
}

template<typename T, typename M>
static inline std::vector<T> ToVector(uint32_t (*callback)(const M ref, T* output, const uint32_t outputCount), const M ref)
{
    std::vector<T> vector(callback(ref, nullptr, 0));
    callback(ref, vector.data(), static_cast<uint32_t>(vector.size()));
    return vector;
}

template<typename T, typename M, typename N>
static inline std::vector<T> ToVector(uint32_t (*callback)(const M ref1, const N ref2, T* output, const uint32_t outputCount), const M ref1, const N ref2)
{
    std::vector<T> vector(callback(ref1, ref2, nullptr, 0));
    callback(ref1, ref2, vector.data(), static_cast<uint32_t>(vector.size()));
    return vector;
}

InputDeviceRef InputInstantiateDevice(const InputGuid guid, const InputDevicePersistentIdentifier identifier)
{
//    _EnsureBuiltInIsSet();

    InputContextGuard guard;

    auto ctx = _GetCtx();
    auto cb = _GetDeviceDatabaseCallbacks();

    auto deviceAssignedRef = cb->GetDeviceAssignedRef(guid);

    // collect traits
    std::vector<InputDeviceTraitRef> traitsRefs = ToVector(cb->GetDeviceTraits, deviceAssignedRef);

    // collect trait sizes
    const uint32_t traitsCount = static_cast<uint32_t>(traitsRefs.size());
    std::vector<uint32_t> traitsSizes(traitsCount);
    for(uint32_t i = 0; i < traitsCount; ++i)
        traitsSizes[i] = cb->GetTraitSizeInBytes(traitsRefs[i]);

    // instantiate the device
    InputDeviceDescr descr = {};
    descr.guid = guid;
    descr.persistentIdentifier = identifier;
    cb->GetDeviceName(deviceAssignedRef, descr.displayName, sizeof(descr.displayName));

    const auto deviceRef = InputInstantiateDeviceInternal(
        descr,
        traitsRefs.data(),
        traitsSizes.data(),
        traitsCount);

    // configure traits
    auto devicesIterator = ctx->devices.find(deviceRef);
    InputAssert(devicesIterator != ctx->devices.end(), "Device should be present");
    for(auto& pair: devicesIterator->second.traitRefToOpaqueBinaryBlob)
        cb->ConfigureTraitInstance(pair.first, pair.second.data(), deviceRef);

    // collect all controls from enabled traits
    std::vector<InputControlRef> controls;
    for(uint32_t i = 0; i < static_cast<uint32_t>(traitsRefs.size()); ++i)
    {
        std::vector<InputControlRef> traitControls = ToVector(cb->GetTraitControls, traitsRefs[i], deviceRef);
        controls.insert(controls.end(), traitControls.begin(), traitControls.end());
    }

    InputInstantiateControlsInternal(controls.data(), static_cast<uint32_t>(controls.size()));
    return deviceRef;
}

void _InputDatabaseControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesType,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
)
{
    _GetDeviceDatabaseCallbacks()->ControlTypeIngress(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl);
}

void _InputDatabaseControlTypeFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    void* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    void* latestRecordedSamples,
    const uint32_t controlCount
)
{
    _GetDeviceDatabaseCallbacks()->ControlTypeFrameBegin(controlTypeRef, controlRefs, controlStates, latestRecordedTimestamps, latestRecordedSamples, controlCount);
}

void _InputDatabaseInitializeControlStorageSpace()
{
    // _EnsureBuiltInIsSet();

    auto ctx = _GetCtx();
    InputAssert(ctx->controlsStoragePerType.size() == 0, "control storage should be empty");

    auto cb = _GetDeviceDatabaseCallbacks();

    const uint32_t currentControlTypeCount = cb->GetControlTypeCount();
    if (ctx->controlsStoragePerType.size() == currentControlTypeCount)
        return;

    ctx->controlsStoragePerType.reserve(currentControlTypeCount);

    for(uint32_t i = 0; i < currentControlTypeCount; ++i)
    {
        const auto descr = cb->GetControlTypeDescr({i});
        ctx->controlsStoragePerType.emplace_back(descr.stateSizeInBytes, descr.sampleSizeInBytes, ctx->framebuffersCount);
    }
}
